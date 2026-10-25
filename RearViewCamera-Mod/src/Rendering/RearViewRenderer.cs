using Kydl.Common;
using UnityEngine;

namespace RearViewCamera.Rendering
{
    /// <summary>
    /// Owns the rear camera and its RenderTexture (ported from the old RVC). They are the only expensive things the mod holds, so they
    /// exist only while the rear view is shown and are destroyed (not just disabled) as soon as it is hidden. The camera is never enabled:
    /// it renders by hand at the configured rate, so it costs nothing between frames and never touches the game's camera stack.
    /// </summary>
    internal sealed class RearViewRenderer
    {
        const int DepthBits = 16;
        const float MinLookSqr = 0.001f;

        Camera _camera;
        RenderTexture _texture;
        float _nextRenderTime;
        bool _warnedNoPlayer;
        bool _creationFailureLogged;
        int _renders;

        internal RenderTexture Texture { get { return _texture; } }
        internal bool IsCreated { get { return _camera != null && _texture != null; } }
        /// Renders since the resources were created (diagnostic line).
        internal int Renders { get { return _renders; } }

        /// Makes sure camera + texture exist at this size (a cheap no-op when they already do).
        /// Expected outcome: returns whether a texture of exactly width x height exists afterwards.
        internal bool Ensure(int width, int height)
        {
            width = Mathf.Max(16, width);
            height = Mathf.Max(16, height);
            if (_camera != null && _texture != null && _texture.width == width && _texture.height == height)
                return IsCreated;
            try
            {
                EnsureCamera();
                EnsureTexture(width, height);
                _creationFailureLogged = false;
            }
            catch (UnityException e)
            {
                if (!_creationFailureLogged) ModLog.Error("Renderer: camera / render texture " + width + "x" + height + " could not be made: " + e.Message);
                _creationFailureLogged = true;
            }
            bool ok = _texture != null && _texture.width == width && _texture.height == height;
            if (ok) ModLog.Check("Renderer: texture width", _texture.width, width, 0, "px");
            return ok;
        }

        void EnsureCamera()
        {
            if (_camera != null) return;
            var go = new GameObject("RearViewCamera_Camera") { hideFlags = HideFlags.HideAndDontSave };
            Object.DontDestroyOnLoad(go);
            _camera = go.AddComponent<Camera>();
            _camera.enabled = false;
            _camera.allowHDR = false;
            _camera.allowMSAA = false;
            _camera.depth = -100f;
            ApplyCameraSettings();
            ModLog.Verbose("Renderer: camera made (near " + _camera.nearClipPlane + " m, far " + _camera.farClipPlane + " m, FOV " + _camera.fieldOfView + " deg, culling mask 0x" + _camera.cullingMask.ToString("X8") + ")");
        }

        void EnsureTexture(int width, int height)
        {
            if (_texture != null && _texture.width == width && _texture.height == height) return;
            string old = _texture != null ? _texture.width + "x" + _texture.height : "none";
            ReleaseTexture();
            _texture = new RenderTexture(width, height, DepthBits, RenderTextureFormat.ARGB32)
            {
                name = "RearViewCamera_RenderTexture",
                filterMode = FilterMode.Bilinear,
                useMipMap = false,
                autoGenerateMips = false,
                antiAliasing = 1
            };
            _texture.Create();
            _camera.targetTexture = _texture;
            _nextRenderTime = 0f;          // draw the first frame at once, never show an empty texture for a whole interval
            _renders = 0;
            ModLog.Verbose("Renderer: render texture " + old + " -> " + width + "x" + height + " (created " + _texture.IsCreated() + ")");
        }

        void ApplyCameraSettings()
        {
            _camera.fieldOfView = ModSettings.FieldOfView.Value;
            _camera.nearClipPlane = ModSettings.NearClip.Value;
            _camera.farClipPlane = ModSettings.FarClip.Value;
        }

        /// Renders this frame if the configured interval has passed. Expected outcome: returns whether a frame was rendered now.
        internal bool RenderIfDue()
        {
            if (_camera == null || _texture == null) return _camera != null && _texture != null;
            if (Time.unscaledTime < _nextRenderTime) return _nextRenderTime <= Time.unscaledTime;
            _nextRenderTime = Time.unscaledTime + 1f / Mathf.Max(1, ModSettings.UpdateFps.Value);
            return PlaceAndRender();
        }

        /// Puts the camera behind the eye point looking backwards along the look direction and renders once.
        /// Expected outcome: returns whether the camera rendered (false only without a local player).
        bool PlaceAndRender()
        {
            Player player = Player.m_localPlayer;
            if (player == null)
            {
                if (!_warnedNoPlayer) ModLog.Warn("Renderer: the rear view is shown but there is no local player");
                _warnedNoPlayer = true;
                return player != null;
            }
            _warnedNoPlayer = false;

            Vector3 look = player.GetLookDir();
            if (look.sqrMagnitude < MinLookSqr) look = player.transform.forward;
            look.Normalize();
            Vector3 eye = player.GetEyePoint();
            Vector3 position = eye - look * ModSettings.CameraDistance.Value + Vector3.up * ModSettings.CameraHeight.Value;

            ApplyCameraSettings();
            _camera.transform.SetPositionAndRotation(position, Quaternion.LookRotation(-look, Vector3.up));
            _camera.Render();
            _renders++;
            if (_renders == 1)
                ModLog.Verbose("Renderer: first frame rendered at " + position.ToString("F2") + ", eye " + eye.ToString("F2") + ", looking " + (-look).ToString("F2")
                    + ", " + ModSettings.CameraDistance.Value + " m behind, " + ModSettings.CameraHeight.Value + " m up");
            return _renders > 0;
        }

        /// Destroys camera and texture. Expected outcome: returns whether both are gone.
        internal bool Release()
        {
            if (_camera == null && _texture == null) return !IsCreated;
            ReleaseTexture();
            if (_camera != null)
            {
                Object.Destroy(_camera.gameObject);
                _camera = null;
            }
            ModLog.Verbose("Renderer: camera and render texture released after " + _renders + " render(s)");
            return !IsCreated;
        }

        void ReleaseTexture()
        {
            if (_texture == null) return;
            if (_camera != null) _camera.targetTexture = null;
            _texture.Release();
            Object.Destroy(_texture);
            _texture = null;
        }
    }
}
