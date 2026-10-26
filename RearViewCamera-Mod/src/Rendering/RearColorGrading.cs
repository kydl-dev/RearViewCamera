using Kydl.Common;
using UnityEngine;
using UnityEngine.PostProcessing;

namespace RearViewCamera.Rendering
{
    /// <summary>
    /// T-008 step 3 (user, 2026-10-25: "game's colour grading", D-022): gives the rear camera the game camera's post-processing
    /// profile, reduced to colour grading (+ user LUT). A COPY of the profile is used, so the game's own profile is never changed;
    /// everything else in it (bloom, AO, motion blur, anti-aliasing, eye adaptation, ...) is switched off in the copy, because the user
    /// asked for colours only and those effects cost more or smear with a camera that renders 15 times a second.
    /// </summary>
    internal sealed class RearColorGrading
    {
        const float RefreshSeconds = 1f;

        PostProcessingBehaviour _behaviour;
        PostProcessingProfile _copy;
        PostProcessingProfile _source;
        string _sourceName = "";
        string _signature = "";
        float _nextRefresh;

        internal bool Active { get { return _behaviour != null && _behaviour.enabled && _copy != null; } }

        /// Adds the grading to the rear camera (once per rear camera). Expected outcome: returns whether grading is active afterwards.
        internal bool Attach(Camera rear, Camera game)
        {
            if (rear == null) return Active;
            if (Active && _behaviour.gameObject == rear.gameObject) return Active;
            PostProcessingBehaviour source = game != null ? game.GetComponent<PostProcessingBehaviour>() : null;
            if (source == null || source.profile == null)
            {
                ModLog.Warn("ColorGrading: the game camera has no post-processing profile (" + (game != null ? game.name : "no game camera") + ") - rear view stays ungraded");
                return Active;
            }
            CopyProfile(source.profile);
            _behaviour = rear.gameObject.AddComponent<PostProcessingBehaviour>();
            _behaviour.profile = _copy;
            return Active;
        }

        /// PROJECT_RULES 17 (D-030): a graphics-option change in the game (another profile, colour grading on / off, new values) reaches
        /// the rear view at once - checked once a second while it is shown. Expected outcome: returns whether the copy matches the game now.
        internal bool Refresh(Camera game)
        {
            if (!Active || Time.unscaledTime < _nextRefresh) return Active;
            _nextRefresh = Time.unscaledTime + RefreshSeconds;
            PostProcessingBehaviour source = game != null ? game.GetComponent<PostProcessingBehaviour>() : null;
            PostProcessingProfile profile = source != null ? source.profile : null;
            if (profile == null) return Active;
            string sig = Signature(profile);
            if (profile == _source && sig == _signature) return Active;
            ModLog.Verbose("ColorGrading: game profile changed ('" + _sourceName + "' -> '" + profile.name + "', " + _signature + " -> " + sig + "), copying again");
            Object.Destroy(_copy);
            CopyProfile(profile);
            _behaviour.profile = _copy;
            return Active && _signature == sig;
        }

        void CopyProfile(PostProcessingProfile profile)
        {
            _source = profile;
            _sourceName = profile.name;
            _signature = Signature(profile);
            _copy = Object.Instantiate(profile);
            _copy.name = "RearViewCamera_" + _sourceName;
            string before = Enabled(_copy);
            KeepColourOnly(_copy);
            ModLog.Verbose("ColorGrading: profile '" + _sourceName + "' copied; game has [" + before + "], rear keeps [" + Enabled(_copy) + "]"
                + (_copy.colorGrading.enabled ? ", tonemapper " + _copy.colorGrading.settings.tonemapping.tonemapper + ", post exposure "
                + _copy.colorGrading.settings.basic.postExposure + ", saturation " + _copy.colorGrading.settings.basic.saturation
                + ", contrast " + _copy.colorGrading.settings.basic.contrast + ", temperature " + _copy.colorGrading.settings.basic.temperature : ""));
            if (!_copy.colorGrading.enabled) ModLog.Verbose("ColorGrading: colour grading is OFF in the game profile '" + _sourceName + "' - rear view ungraded too");
        }

        /// What the rear view takes from the game profile: colour grading / LUT on-off and the basic grading values.
        static string Signature(PostProcessingProfile p)
        {
            var b = p.colorGrading.settings.basic;
            return p.colorGrading.enabled + "/" + p.userLut.enabled + "/" + p.colorGrading.settings.tonemapping.tonemapper + "/" + b.postExposure + "/"
                + b.temperature + "/" + b.tint + "/" + b.hueShift + "/" + b.saturation + "/" + b.contrast;
        }

        /// Removes the grading and the profile copy. Expected outcome: returns whether nothing is left.
        internal bool Detach()
        {
            if (_behaviour != null) Object.Destroy(_behaviour);
            if (_copy != null) Object.Destroy(_copy);
            bool had = _behaviour != null || _copy != null;
            _behaviour = null;
            _copy = null;
            if (had) ModLog.Verbose("ColorGrading: removed (profile copy of '" + _sourceName + "' destroyed)");
            return _behaviour == null && _copy == null;
        }

        static void KeepColourOnly(PostProcessingProfile p)
        {
            p.debugViews.enabled = false;
            p.fog.enabled = false;
            p.antialiasing.enabled = false;
            p.ambientOcclusion.enabled = false;
            p.screenSpaceReflection.enabled = false;
            p.depthOfField.enabled = false;
            p.motionBlur.enabled = false;
            p.eyeAdaptation.enabled = false;
            p.bloom.enabled = false;
            p.chromaticAberration.enabled = false;
            p.grain.enabled = false;
            p.vignette.enabled = false;
            p.dithering.enabled = false;
            // colorGrading and userLut keep the game's on/off state
        }

        static string Enabled(PostProcessingProfile p)
        {
            var sb = new System.Text.StringBuilder();
            void Add(string n, bool on) { if (on) sb.Append(sb.Length > 0 ? ", " : "").Append(n); }
            Add("fog", p.fog.enabled); Add("AA", p.antialiasing.enabled); Add("AO", p.ambientOcclusion.enabled);
            Add("SSR", p.screenSpaceReflection.enabled); Add("DoF", p.depthOfField.enabled); Add("motion blur", p.motionBlur.enabled);
            Add("eye adaptation", p.eyeAdaptation.enabled); Add("bloom", p.bloom.enabled); Add("colour grading", p.colorGrading.enabled);
            Add("user LUT", p.userLut.enabled); Add("chromatic aberration", p.chromaticAberration.enabled); Add("grain", p.grain.enabled);
            Add("vignette", p.vignette.enabled); Add("dithering", p.dithering.enabled);
            return sb.Length > 0 ? sb.ToString() : "nothing";
        }
    }
}
