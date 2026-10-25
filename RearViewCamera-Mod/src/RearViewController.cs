using Kydl.Common;
using Kydl.Ui;
using RearViewCamera.Game;
using RearViewCamera.Logic;
using RearViewCamera.Rendering;
using RearViewCamera.Ui;
using UnityEngine;

namespace RearViewCamera
{
    /// <summary>
    /// The game side of the rear view: reads the keys (ZInput) and the text-entry block every frame, asks <see cref="RearViewKeyLogic"/> what
    /// to show, places the small window under the minimap (<see cref="SmallCameraLayout"/>), keeps the renderer's texture at the needed size
    /// and renders in LateUpdate (after the player and camera moved). Nothing exists until the rear view is first shown; hiding it frees
    /// camera and texture again.
    /// </summary>
    internal static class RearViewController
    {
        static readonly RearViewKeyLogic Logic = new RearViewKeyLogic(s => ModLog.Verbose(s));
        static readonly RearViewRenderer Renderer = new RearViewRenderer();
        static readonly RearViewInputBlocker Blocker = new RearViewInputBlocker();
        static readonly Vector3[] Corners = new Vector3[4];

        static RearViewHudView _hud;
        static RearViewMode _shown = RearViewMode.Off;
        static BlockReason _lastBlock = BlockReason.None;
        static HudRect _lastRect;
        static string _lastOffReason = "";
        static float _nextDiagnostic;

        static Minimap _map;
        static RectTransform _mapRect;
        static Canvas _mapCanvas;

        /// What is shown now (InGameTests, diagnostics).
        internal static RearViewMode Shown { get { return _shown; } }
        /// The last small-window rectangle in screen pixels.
        internal static HudRect LastRect { get { return _lastRect; } }

        /// Every frame (Plugin.Update). Expected outcome: returns the mode shown after this frame.
        internal static RearViewMode Tick()
        {
            string offReason = OffReason();
            RearViewMode wanted = RearViewMode.Off;
            if (offReason == null)
            {
                BlockReason block = Blocker.Current();
                if (block != _lastBlock)
                {
                    ModLog.Verbose("Controller: key block " + _lastBlock + " -> " + block);
                    _lastBlock = block;
                }
                var keys = new RearViewKeys
                {
                    SmallHeld = KeyHeld(ModSettings.SmallCameraKey.Value),
                    SmallPressed = KeyPressed(ModSettings.SmallCameraKey.Value),
                    FullHeld = KeyHeld(ModSettings.FullScreenKey.Value),
                    Blocked = block != BlockReason.None,
                    UiOpeningKey = RearViewInputBlocker.UiOpeningKeyPressed()
                };
                wanted = Logic.Step(keys, ModSettings.SmallCameraKeyMode.Value);
                _lastOffReason = "";
            }
            else if (offReason != _lastOffReason)
            {
                ModLog.Verbose("Controller: rear view off - " + offReason);
                _lastOffReason = offReason;
            }
            Show(wanted);
            return _shown;
        }

        /// Every frame after Update (Plugin.LateUpdate). Expected outcome: returns whether a frame was rendered now.
        internal static bool LateTick()
        {
            if (_shown == RearViewMode.Off) return _shown != RearViewMode.Off;
            bool rendered = Renderer.RenderIfDue();
            if (Time.unscaledTime >= _nextDiagnostic)
            {
                _nextDiagnostic = Time.unscaledTime + ModSettings.DiagnosticSeconds.Value;
                var t = Renderer.Texture;
                ModLog.Verbose("Controller: " + _shown + ", texture " + (t != null ? t.width + "x" + t.height : "none") + ", " + Renderer.Renders + " render(s), "
                    + (_shown == RearViewMode.Small ? "window " + _lastRect + ", " : "") + "screen " + Screen.width + "x" + Screen.height
                    + ", toggle " + (Logic.ToggleOn ? "on" : "off") + ", block " + _lastBlock);
            }
            return rendered;
        }

        /// Frees everything (Plugin.OnDestroy). Expected outcome: returns whether nothing is shown afterwards.
        internal static bool Shutdown()
        {
            Show(RearViewMode.Off);
            ModLog.Verbose("Controller: shut down");
            return _shown == RearViewMode.Off && !Renderer.IsCreated;
        }

        /// null = the rear view may be shown; otherwise why not.
        static string OffReason()
        {
            if (!ModSettings.On) return "EnableAll is off";
            Player p = Player.m_localPlayer;
            if (p == null) return "no local player";
            if (p.IsDead()) return "player is dead";
            return null;
        }

        static void Show(RearViewMode mode)
        {
            if (mode == RearViewMode.Off)
            {
                if (_shown != RearViewMode.Off) ModLog.Verbose("Controller: " + _shown + " -> Off");
                if (_hud != null) _hud.Hide();
                Renderer.Release();
                _shown = RearViewMode.Off;
                return;
            }
            if (!EnsureHud()) { _shown = RearViewMode.Off; return; }

            float toPanel = PanelUnitsPerPixel();
            bool mirror = ModSettings.MirrorImage.Value;
            if (mode == RearViewMode.Small)
            {
                HudRect r = PlaceSmall();
                float scale = ModSettings.SmallRenderScale.Value;
                if (!Renderer.Ensure(Mathf.RoundToInt(r.Width * scale), Mathf.RoundToInt(r.Height * scale))) { _hud.Hide(); return; }
                _hud.ShowSmall(Renderer.Texture, r.X * toPanel, r.Y * toPanel, r.Width * toPanel, r.Height * toPanel, ModSettings.BorderWidth.Value * toPanel, mirror);
            }
            else
            {
                float scale = ModSettings.FullScreenRenderScale.Value;
                if (!Renderer.Ensure(Mathf.RoundToInt(Screen.width * scale), Mathf.RoundToInt(Screen.height * scale))) { _hud.Hide(); return; }
                _hud.ShowFullScreen(Renderer.Texture, mirror);
            }
            if (mode != _shown) ModLog.Verbose("Controller: " + _shown + " -> " + mode + " (panel units per px " + toPanel.ToString("F3") + ")");
            _shown = mode;
        }

        /// Expected outcome: whether the HUD element is on the shared panel.
        static bool EnsureHud()
        {
            if (_hud == null || _hud.Element.panel == null) AddHud();
            return _hud != null && _hud.Element.panel != null;
        }

        static void AddHud()
        {
            var root = UiKit.Root;
            if (root == null)
            {
                ModLog.Warn("Controller: UI Toolkit panel not available, the rear view cannot be shown");
                return;
            }
            _hud = RearViewHudView.Build();
            root.Add(_hud.Element);
            ModLog.Verbose("Controller: HUD element added to the shared panel (root " + root.layout.width.ToString("F0") + "x" + root.layout.height.ToString("F0") + " units)");
        }

        /// The Kydl panel scales with the screen height from 1920x1080 (UiKit): config sizes are screen pixels (D-010), so convert.
        static float PanelUnitsPerPixel()
        {
            float rootHeight = UiKit.Root != null ? UiKit.Root.layout.height : 0f;
            if (rootHeight > 1f && !float.IsNaN(rootHeight)) return rootHeight / Screen.height;
            return 1080f / Mathf.Max(1, Screen.height);
        }

        static HudRect PlaceSmall()
        {
            bool found = TryMinimapEdges(out float right, out float bottom);
            HudRect r = SmallCameraLayout.Place(Screen.width, Screen.height, ModSettings.SmallHeight.Value,
                found ? right : (float?)null, found ? bottom : (float?)null,
                ModSettings.MinimapGap.Value, ModSettings.ScreenMargin.Value, ModSettings.SmallOffsetX.Value, ModSettings.SmallOffsetY.Value);
            if (!r.SameAs(_lastRect))
            {
                ModLog.Verbose("Controller: small window " + _lastRect + " -> " + r + (found ? " (minimap right " + right.ToString("F0") + " px, bottom " + bottom.ToString("F0") + " px)" : ""));
                ModLog.Check("Controller: small window width", r.Width, Mathf.Round(SmallCameraLayout.ClampHeight(ModSettings.SmallHeight.Value) * 16f / 9f), 0.5, "px");
                _lastRect = r;
            }
            return r;
        }

        /// The small minimap's right and bottom edge in screen pixels from the top-left. Expected outcome: whether a visible minimap was found.
        internal static bool TryMinimapEdges(out float right, out float bottom)
        {
            right = 0f; bottom = 0f;
            Minimap map = Minimap.instance;
            if (map != _map)
            {
                _map = map;
                _mapRect = null;
                _mapCanvas = null;
                if (map != null)
                {
                    _mapRect = map.m_mapImageSmall != null ? map.m_mapImageSmall.rectTransform : (map.m_smallRoot != null ? map.m_smallRoot.transform as RectTransform : null);
                    _mapCanvas = _mapRect != null ? _mapRect.GetComponentInParent<Canvas>() : null;
                    ModLog.Verbose("Controller: minimap " + (_mapRect != null ? _mapRect.name : "rect not found") + ", canvas " + (_mapCanvas != null ? _mapCanvas.name + " " + _mapCanvas.renderMode : "none"));
                }
            }
            if (_mapRect == null || !_mapRect.gameObject.activeInHierarchy) return _mapRect != null && _mapRect.gameObject.activeInHierarchy;
            Camera cam = _mapCanvas != null && _mapCanvas.renderMode != RenderMode.ScreenSpaceOverlay ? _mapCanvas.worldCamera : null;
            _mapRect.GetWorldCorners(Corners);                 // 0 bottom-left, 1 top-left, 2 top-right, 3 bottom-right
            Vector2 br = RectTransformUtility.WorldToScreenPoint(cam, Corners[3]);
            right = br.x;
            bottom = Screen.height - br.y;
            return right > 0f && bottom > 0f;
        }

        static bool KeyHeld(KeyCode key) { return key != KeyCode.None && ZInput.GetKey(key, true); }
        static bool KeyPressed(KeyCode key) { return key != KeyCode.None && ZInput.GetKeyDown(key, true); }
    }
}
