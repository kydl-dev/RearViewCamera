using Kydl.Common;
using UnityEngine;

namespace RearViewCamera.Rendering
{
    /// <summary>
    /// Owns the rear camera and its RenderTexture (ported from the old RVC). They are the only expensive things the mod holds, so they
    /// exist only while the rear view is shown and are destroyed (not just disabled) as soon as it is hidden. The camera is never enabled:
    /// it renders by hand at the configured rate, so it costs nothing between frames and never touches the game's camera stack.
    /// </summary>
    /// <summary>How much of the game camera the rear camera copies; lowered step by step after errors (D-039).</summary>
    internal enum RenderQuality
    {
        /// Game camera settings + the game's colour grading.
        Full,
        /// Game camera settings, no colour grading.
        NoGrading,
        /// A plain Unity camera (only FOV, distance and height from the config).
        Plain
    }

    internal sealed class RearViewRenderer
    {
        const int DepthBits = 16;
        const float MinLookSqr = 0.001f;

        Camera _camera;
        RenderTexture _texture;
        readonly RearColorGrading _grading = new RearColorGrading();
        readonly AnimalLift _lift = new AnimalLift();
        GameCamera _gameCameraOwner;
        Camera _gameCamera;
        string _copiedFrom = "";
        float _nextRenderTime;
        bool _warnedNoPlayer;
        bool _creationFailureLogged;
        int _renders;
        int _sameSizeRebuilds;
        string _applied = "";     // camera values from the player config last used for a render (logged when they change: proof of live config)

        RenderQuality _quality = RenderQuality.Full;

        /// Lowering it frees the camera, so the next frame rebuilds it at the new level.
        internal RenderQuality Quality
        {
            get { return _quality; }
            set
            {
                if (value == _quality) return;
                ModLog.Warn("Renderer: quality " + _quality + " -> " + value);
                _quality = value;
                Release();
            }
        }

        internal RenderTexture Texture { get { return _texture; } }
        /// Metres the camera is lifted over an animal right now (diagnostics).
        internal float Lift { get { return _lift.Lift; } }
        internal bool IsCreated { get { return _camera != null && _texture != null; } }
        /// Renders since the resources were created (diagnostic line).
        internal int Renders { get { return _renders; } }

        /// Makes sure camera + texture exist at this size (a cheap no-op when they already do).
        /// Expected outcome: returns whether a texture of exactly width x height exists afterwards.
        internal bool Ensure(int width, int height)
        {
            width = Mathf.Max(16, width);
            height = Mathf.Max(16, height);
            if (_camera != null && _texture != null && _texture.width == width && _texture.height == height && _texture.format == WantedFormat())
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
            CameraDiagnostics.WriteOnce(_camera);    // T-008: game camera vs rear camera, once per session
            if (_quality == RenderQuality.Full) _grading.Attach(_camera, GameCameraNow()); // D-022: the game's colour grading only
            ModLog.Verbose("Renderer: camera made (near " + _camera.nearClipPlane + " m, far " + _camera.farClipPlane + " m, FOV " + _camera.fieldOfView + " deg, culling mask 0x" + _camera.cullingMask.ToString("X8") + ")");
        }

        void EnsureTexture(int width, int height)
        {
            RenderTextureFormat format = WantedFormat();
            if (_texture != null && _texture.width == width && _texture.height == height && _texture.format == format) return;
            string old = _texture != null ? _texture.width + "x" + _texture.height + " " + _texture.format : "none";
            if (_texture != null && _texture.width == width && _texture.height == height)
            {
                _sameSizeRebuilds++;
                // a same-size rebuild only happens when the game camera's HDR switches; more than that is a bug (expected 0, tolerance 1)
                ModLog.Check("Renderer: same-size texture rebuilds while shown", _sameSizeRebuilds, 0, 1, "");
            }
            ReleaseTexture();
            _texture = new RenderTexture(width, height, DepthBits, format)
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
            ModLog.Verbose("Renderer: render texture " + old + " -> " + width + "x" + height + " " + format + " (created " + _texture.IsCreated() + ")");
        }

        /// HDR texture when the game camera renders HDR (so the copied HDR setting has somewhere to go). A concrete format, never
        /// DefaultHDR: a created texture reports the resolved format (ARGBHalf), so comparing with DefaultHDR re-created the texture
        /// every frame (T-G003, session 20261025-051552).
        RenderTextureFormat WantedFormat()
        {
            Camera game = _quality == RenderQuality.Plain ? null : GameCameraNow();
            bool hdr = game != null && game.allowHDR && SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGBHalf);
            return hdr ? RenderTextureFormat.ARGBHalf : RenderTextureFormat.ARGB32;
        }

        /// The game's own camera (GameCamera component), looked up again only when GameCamera.instance changes.
        Camera GameCameraNow()
        {
            GameCamera owner = GameCamera.instance;
            if (owner != _gameCameraOwner)
            {
                _gameCameraOwner = owner;
                _gameCamera = owner != null ? owner.GetComponent<Camera>() : null;
                _copiedFrom = "";
                ModLog.Verbose("Renderer: game camera " + (_gameCamera != null ? "'" + _gameCamera.name + "'" : "not found (GameCamera.instance " + (owner != null) + ")"));
            }
            return _gameCamera;
        }

        /// T-008 (user: "settings only"): copy the game camera's render settings - HDR, rendering path, view distance, depth textures,
        /// culling - so terrain, sky and distance look like the front view. No image effects, fog command buffer or post-processing.
        /// FOV stays the player's config value. Without a game camera the Unity defaults stay.
        void ApplyCameraSettings()
        {
            _camera.fieldOfView = ModSettings.FieldOfView.Value;
            Camera game = _quality == RenderQuality.Plain ? null : GameCameraNow();
            if (game == null) return;
            _camera.renderingPath = game.renderingPath;
            _camera.allowHDR = game.allowHDR;
            _camera.allowMSAA = game.allowMSAA;
            _camera.clearFlags = game.clearFlags;
            _camera.backgroundColor = game.backgroundColor;
            _camera.cullingMask = game.cullingMask;
            _camera.nearClipPlane = game.nearClipPlane;
            _camera.farClipPlane = game.farClipPlane;
            _camera.depthTextureMode = game.depthTextureMode;
            _camera.useOcclusionCulling = game.useOcclusionCulling;
            _camera.layerCullSpherical = game.layerCullSpherical;
            _camera.layerCullDistances = game.layerCullDistances;
            string from = game.GetInstanceID().ToString();
            if (from == _copiedFrom) return;
            _copiedFrom = from;
            ModLog.Verbose("Renderer: settings copied from game camera '" + game.name + "': path " + game.renderingPath + ", HDR " + game.allowHDR + ", MSAA " + game.allowMSAA
                + ", clear " + game.clearFlags + ", mask 0x" + game.cullingMask.ToString("X8") + ", near/far " + game.nearClipPlane + "/" + game.farClipPlane + " m, depth texture "
                + game.depthTextureMode + ", occlusion " + game.useOcclusionCulling + "; FOV " + _camera.fieldOfView + " deg (config)");
            ModLog.Check("Renderer: rear far clip vs game camera", _camera.farClipPlane, game.farClipPlane, 0.01, "m");
            ModLog.Check("Renderer: rear near clip vs game camera", _camera.nearClipPlane, game.nearClipPlane, 0.001, "m");
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
            Quaternion rotation = Quaternion.LookRotation(-look, Vector3.up);
            _lift.Apply(player, eye, look, ref position, ref rotation);     // R7: over an animal that is in the way
            position = OutOfTerrain(eye, position);                         // user 2026-10-31: behind a hill the camera sat INSIDE the ground and showed the void through it

            ApplyCameraSettings();
            if (_quality == RenderQuality.Full) _grading.Refresh(GameCameraNow());
            string applied = "FOV " + _camera.fieldOfView + " deg, " + ModSettings.CameraDistance.Value + " m behind, " + ModSettings.CameraHeight.Value + " m up, "
                + ModSettings.UpdateFps.Value + " fps";
            if (applied != _applied)
            {
                if (_applied.Length > 0) ModLog.Info("Renderer: now rendering with " + applied + " (was " + _applied + ")");
                _applied = applied;
            }
            _camera.transform.SetPositionAndRotation(position, rotation);
            _camera.Render();
            _renders++;
            if (_renders == 1)
                ModLog.Verbose("Renderer: first frame rendered at " + position.ToString("F2") + ", eye " + eye.ToString("F2") + ", looking " + (-look).ToString("F2")
                    + ", " + ModSettings.CameraDistance.Value + " m behind, " + ModSettings.CameraHeight.Value + " m up");
            return _renders > 0;
        }

        // Terrain and solid pieces are single-sided: a camera that ends up inside a hill (the player stands with a slope or wall right behind) sees the inside of the ground, i.e. through it
        // into the empty sky ("a clear cut through the ground into oblivion"). The camera therefore (1) stops in front of whatever solid lies between the eye and the wanted
        // position (a line cast), and (2) never sits lower than GroundClearance above the terrain at its place.
        const float GroundClearance = 0.5f, WallClearance = 0.3f;
        static int _solidMask;
        internal static Vector3 OutOfTerrain(Vector3 eye, Vector3 wanted)
        {
            if (_solidMask == 0) _solidMask = LayerMask.GetMask("terrain", "static_solid", "Default", "piece");
            var to = wanted - eye;
            float len = to.magnitude;
            if (len > 0.01f)
            {
                RaycastHit hit;
                if (Physics.Raycast(eye, to / len, out hit, len, _solidMask, QueryTriggerInteraction.Ignore))
                    wanted = eye + (to / len) * Mathf.Max(0f, hit.distance - WallClearance);
            }
            var zs = ZoneSystem.instance;
            if (zs != null)
            {
                float ground = zs.GetGroundHeight(wanted) + GroundClearance;
                if (wanted.y < ground) wanted.y = ground;
            }
            return wanted;
        }

        /// Destroys camera and texture. Expected outcome: returns whether both are gone.
        internal bool Release()
        {
            if (_camera == null && _texture == null) return !IsCreated;
            ReleaseTexture();
            _grading.Detach();
            _lift.Reset();
            if (_camera != null)
            {
                Object.Destroy(_camera.gameObject);
                _camera = null;
            }
            ModLog.Verbose("Renderer: camera and render texture released after " + _renders + " render(s), " + _sameSizeRebuilds + " same-size rebuild(s)");
            _sameSizeRebuilds = 0;
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
