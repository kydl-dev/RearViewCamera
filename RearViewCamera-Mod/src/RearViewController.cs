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
        static readonly HudProbe Probe = new HudProbe();
        static readonly KeyBindings Keys = new KeyBindings();
        const int PanelOrderAboveHud = 500;     // UiKit's own default: over every game canvas
        static int _panelOrder = PanelOrderAboveHud;
        static bool _behindWarned;
        static readonly Vector3[] Corners = new Vector3[4];

        static RearViewHudView _hud;
        static RearViewMode _shown = RearViewMode.Off;
        static BlockReason _lastBlock = BlockReason.None;
        static HudRect _lastRect;
        static string _lastOffReason = "";
        static string _lastScreen = "";
        static float _nextDiagnostic;

        static Minimap _map;
        static RectTransform _mapRect;
        static Canvas _mapCanvas;

        /// The keys really used this session (InGameTests).
        internal static KeyBindings KeyUse { get { return Keys; } }

        /// What is shown now (InGameTests, diagnostics).
        internal static RearViewMode Shown { get { return _shown; } }
        /// The last small-window rectangle in screen pixels.
        internal static HudRect LastRect { get { return _lastRect; } }

        static bool _disabledForSession;
        static bool _wasPaused;
        static float _nextFakeError;
        static RearViewNoticeView _notice;
        static float _noticeUntil;
        const float FakeErrorSeconds = 2f;

        /// Every frame (Plugin.Update), behind the safety net (D-028 / D-039). Expected outcome: returns the mode shown after this frame.
        internal static RearViewMode Tick()
        {
            try
            {
                TickNotice();
                if (_disabledForSession) return _shown;
                return TickInner();
            }
            catch (System.Exception e)   // last-resort net: any failure must not repeat every frame (R4)
            {
                StepDown("Update", e);
                return _shown;
            }
        }

        /// User 2026-10-26: a rear-view key that does nothing (switched off for the session, or key + backup taken) shows a note on the
        /// left for NoticeSeconds; it hides itself afterwards. Only the player's own configured keys count, and only in a world.
        static void TickNotice()
        {
            if (_notice != null && _noticeUntil > 0f && Time.unscaledTime >= _noticeUntil)
            {
                _notice.Hide();
                _noticeUntil = 0f;
            }
            if (Player.m_localPlayer == null || !ModSettings.On) return;
            KeyCode small = ModSettings.SmallCameraKey.Value, full = ModSettings.FullScreenKey.Value;
            KeyCode pressed = KeyPressed(small) ? small : KeyPressed(full) ? full : KeyCode.None;
            if (pressed == KeyCode.None) return;
            string text = _disabledForSession ? Loc.T("rearviewcamera_notice_session_off") : Keys.NoticeFor(pressed);
            if (text == null) return;
            if (_notice == null || _notice.Element.panel == null)
            {
                var root = UiKit.Root;
                if (root == null) return;
                _notice = RearViewNoticeView.Build();
                root.Add(_notice.Element);
            }
            _notice.Show(text);
            _noticeUntil = Time.unscaledTime + ModSettings.NoticeSeconds.Value;
            ModLog.Info("Controller: " + pressed + " pressed but does nothing - note shown for " + ModSettings.NoticeSeconds.Value + " s: " + text);
        }

        static RearViewMode TickInner()
        {
            if (ModSettings.FakeError.Value == 1 && _shown != RearViewMode.Off && Time.unscaledTime >= _nextFakeError)
            {
                _nextFakeError = Time.unscaledTime + FakeErrorSeconds;
                throw new System.InvalidOperationException("FakeError test (KydlTools: RearViewCamera Debug.FakeError = 1) at quality " + Renderer.Quality);
            }
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
                Keys.CheckIfNeeded();             // once per session + after the player changes a key (R12)
                bool mapOpen = ScreenState.MapOpen();
                string hiding = ScreenState.HidingScreen(Player.m_localPlayer);
                string screen = hiding ?? (mapOpen ? "large map" : "");
                if (screen != _lastScreen)
                {
                    ModLog.Verbose("Controller: screen '" + _lastScreen + "' -> '" + screen + "'" + (hiding != null ? " (rear view hidden, state kept)" : mapOpen ? " (map: may only hide)" : ""));
                    _lastScreen = screen;
                }
                var keys = new RearViewKeys
                {
                    MapOpen = mapOpen,
                    ScreenHidden = hiding != null,
                    SmallHeld = KeyHeld(Keys.SmallKey),
                    SmallPressed = KeyPressed(Keys.SmallKey),
                    FullHeld = KeyHeld(Keys.FullKey),
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

        /// Every frame after Update (Plugin.LateUpdate), behind the safety net. Expected outcome: returns whether a frame was rendered now.
        internal static bool LateTick()
        {
            if (_disabledForSession) return !_disabledForSession;
            try
            {
                return LateTickInner();
            }
            catch (System.Exception e)   // last-resort net, see Tick
            {
                StepDown("LateUpdate", e);
                return _disabledForSession;
            }
        }

        /// One error = one step down (D-039): Full -> NoGrading -> Plain -> off for this session. The view is hidden and rebuilt at the
        /// lower level on the next frame; the player gets one line on screen per step.
        static void StepDown(string where, System.Exception e)
        {
            if (_disabledForSession)
            {
                ModLog.Error("Controller: error in " + where + " after the rear view was switched off: " + e.Message);
                return;
            }
            ModLog.Error("Controller: error in " + where + " at quality " + Renderer.Quality + ": " + e);
            try { Show(RearViewMode.Off); }
            catch (System.Exception e2) { ModLog.Error("Controller: hiding after the error failed too: " + e2.Message); }
            string message;
            if (Renderer.Quality == RenderQuality.Full) { Renderer.Quality = RenderQuality.NoGrading; message = Loc.T("rearviewcamera_msg_stepdown", "no colour grading"); }
            else if (Renderer.Quality == RenderQuality.NoGrading) { Renderer.Quality = RenderQuality.Plain; message = Loc.T("rearviewcamera_msg_stepdown", "a plain camera"); }
            else { _disabledForSession = true; message = Loc.T("rearviewcamera_msg_off"); ModLog.Error("Controller: rear view switched off for this session (errors at every quality level)"); }
            if (MessageHud.instance != null) MessageHud.instance.ShowMessage(MessageHud.MessageType.TopLeft, message);
        }

        static bool LateTickInner()
        {
            if (_shown == RearViewMode.Off) return _shown != RearViewMode.Off;      // never render what nobody sees (D-029)
            bool paused = ScreenState.Paused();
            if (paused != _wasPaused)
            {
                ModLog.Verbose("Controller: game " + (paused ? "paused - keeping the last frame, no new renders" : "unpaused - rendering again"));
                _wasPaused = paused;
            }
            if (paused) return !paused;
            bool rendered = Renderer.RenderIfDue();
            if (Time.unscaledTime >= _nextDiagnostic)
            {
                _nextDiagnostic = Time.unscaledTime + ModSettings.DiagnosticSeconds.Value;
                var t = Renderer.Texture;
                ModLog.Verbose("Controller: " + _shown + ", texture " + (t != null ? t.width + "x" + t.height : "none") + ", " + Renderer.Renders + " render(s), "
                    + (_shown == RearViewMode.Small ? "window " + _lastRect + ", " : "") + "screen " + Screen.width + "x" + Screen.height
                    + ", toggle " + (Logic.ToggleOn ? "on" : "off") + ", block " + _lastBlock + ", animal lift " + Renderer.Lift.ToString("F2") + " m");
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
                FullScreenSize.For(Screen.width, Screen.height, ModSettings.FullScreenMaxHeight.Value, out int fw, out int fh);
                if (mode != _shown)
                    ModLog.Check("Controller: full-screen render height", fh, System.Math.Min(Screen.height, ModSettings.FullScreenMaxHeight.Value), 0, "px");
                if (!Renderer.Ensure(fw, fh)) { _hud.Hide(); return; }
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
            // D-026 / T-G006: the large map hides the minimap; keep the window where it was instead of jumping to the corner fallback
            if (ScreenState.MapOpen() && _lastRect.UnderMinimap) return _lastRect;
            bool found = TryMinimapEdges(out float right, out float bottom);
            StatusIconPlacement placement = ModSettings.StatusEffects.Value;
            bool icons = Probe.TryStatusIcons(out HudRect iconRect);
            ApplyLayer(placement);
            HudRect r = SmallCameraLayout.Place(Screen.width, Screen.height, ModSettings.SmallHeight.Value,
                found ? right : (float?)null, found ? bottom : (float?)null,
                ModSettings.MinimapGap.Value, ModSettings.ScreenMargin.Value, ModSettings.SmallOffsetX.Value, ModSettings.SmallOffsetY.Value,
                icons ? iconRect : (HudRect?)null, placement);
            if (!r.SameAs(_lastRect))
            {
                ModLog.Verbose("Controller: small window " + _lastRect + " -> " + r + (found ? " (minimap right " + right.ToString("F0") + " px, bottom " + bottom.ToString("F0") + " px)" : "")
                    + ", status icons " + (icons ? iconRect.Width.ToString("F0") + "x" + iconRect.Height.ToString("F0") + " px at (" + iconRect.X.ToString("F0") + ", " + iconRect.Y.ToString("F0") + ")" : "none") + ", placement " + placement);
                ModLog.Check("Controller: small window width", r.Width, Mathf.Round(SmallCameraLayout.ClampHeight(ModSettings.SmallHeight.Value) * 16f / 9f), 0.5, "px");
                _lastRect = r;
            }
            return r;
        }

        /// Behind (D-037): the Kydl panel is sorted just under the HUD canvas so the status icons draw over the picture (UI Toolkit panels and
        /// Screen Space - Overlay canvases share one sorting order, SortOrderProbe). Under / OnTop: over every game canvas.
        static void ApplyLayer(StatusIconPlacement placement)
        {
            int wanted = PanelOrderAboveHud;
            if (placement == StatusIconPlacement.Behind)
            {
                if (Probe.TryHudCanvasOrder(out int hudOrder)) wanted = hudOrder - 1;
                else if (!_behindWarned)
                {
                    ModLog.Warn("Controller: StatusEffects = Behind needs the game's HUD on a Screen Space - Overlay canvas; it is not, so the camera stays on top");
                    _behindWarned = true;
                }
            }
            if (wanted == _panelOrder || UiKit.Settings == null) return;
            ModLog.Verbose("Controller: panel sorting order " + _panelOrder + " -> " + wanted + " (StatusEffects " + placement + ")");
            UiKit.Settings.sortingOrder = wanted;
            _panelOrder = wanted;
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
