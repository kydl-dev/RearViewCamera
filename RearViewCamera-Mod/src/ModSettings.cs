using BepInEx.Configuration;
using Kydl.Common;
using RearViewCamera.Logic;
using UnityEngine;

namespace RearViewCamera
{
    /// <summary>
    /// All settings in one place (user rule, every mod):
    ///  * PLAYER CONFIG (BepInEx entries below, bound once from Plugin.Awake): only what a player should change. Clear descriptions, ranges,
    ///    drop-downs (enums). Descriptions never mention older versions, old names or old values. (D-016)
    ///  * DEV-ONLY values (Tunable fields below): fixed values chosen by the user, NOT in the player's file. The user's KydlTools mod (F7) finds
    ///    every static Tunable here by itself and lets the user change it in game - nothing to register.
    /// When the user settles a value in game, it becomes the new fixed value / default here.
    /// </summary>
    internal static class ModSettings
    {
        // [General]
        public static ConfigEntry<bool> EnableAll;
        // [Controls]
        public static ConfigEntry<KeyCode> SmallCameraKey;
        public static ConfigEntry<SmallCameraMode> SmallCameraKeyMode;
        public static ConfigEntry<KeyCode> FullScreenKey;
        // [SmallCamera]
        public static ConfigEntry<int> SmallHeight;
        public static ConfigEntry<int> SmallOffsetX;
        public static ConfigEntry<int> SmallOffsetY;
        // [Camera]
        public static ConfigEntry<float> CameraDistance;
        public static ConfigEntry<float> CameraHeight;
        public static ConfigEntry<float> FieldOfView;
        public static ConfigEntry<int> UpdateFps;
        public static ConfigEntry<bool> MirrorImage;
        // [DebugLog]
        public static ConfigEntry<bool> DebugLog;

        /// Is the mod switched on ([General] EnableAll)? Off = inert: nothing shown, camera released.
        public static bool On { get { return EnableAll == null || EnableAll.Value; } }

        // --- dev-only fixed values (Tunables): not in the config file, KydlTools changes them for testing -----------------------------------
        // Starting values picked by Claude-Agent-1 (render 1:1 for the small view); the user settles them in game. Near / far clip, HDR, rendering path,
        // depth textures, culling are copied from the game camera (T-008, user: "settings only"), so they are no Tunables.
        public static readonly Tunable<float> SmallRenderScale = new Tunable<float>("Rendering", "SmallRenderScale", 1f, 0.25f, 1f,
            "Render-texture size of the small camera as a fraction of its on-screen size (1 = one rendered pixel per screen pixel).");
        public static readonly Tunable<float> FullScreenRenderScale = new Tunable<float>("Rendering", "FullScreenRenderScale", 1f, 0.25f, 1f,   // user settled 1 in game (2026-10-26, D-024)

            "Render-texture size of the full-screen view as a fraction of the screen resolution (lower = cheaper, blurrier).");
        public static readonly Tunable<int> FullScreenMinHeight = new Tunable<int>("Rendering", "FullScreenMinHeight", FullScreenSize.DefaultMinHeight, 360, 2160,
            "Lowest render height of the full-screen view, pixels (user: at least 1080p); never above the screen height.");
        public static readonly Tunable<float> MinimapGap = new Tunable<float>("Layout", "MinimapGap", 10f, 0f, 60f,
            "Space between the bottom of the minimap and the small camera, screen pixels.");
        public static readonly Tunable<float> ScreenMargin = new Tunable<float>("Layout", "ScreenMargin", 20f, 0f, 100f,
            "Distance of the small camera to the top / right screen edge when the minimap is hidden or not found, screen pixels.");
        public static readonly Tunable<float> BorderWidth = new Tunable<float>("Layout", "BorderWidth", 2f, 0f, 8f,
            "Gold-brown frame around the small camera, screen pixels (0 = no frame).");
        public static readonly Tunable<float> DiagnosticSeconds = new Tunable<float>("Debug", "DiagnosticSeconds", 3f, 1f, 30f,
            "How often the per-frame diagnostic line is written while the rear view is shown (debug log on), seconds.");

        /// Expected outcome: every player entry is bound (returns that check).
        public static bool Bind(ConfigFile c)
        {
            var savedLines = ConfigTools.Capture(c);     // before the first Bind: every Bind saves the file with what BepInEx parsed

            EnableAll = c.Bind("General", "EnableAll", true,
                "Switches the whole mod on / off while playing. Off: no rear view is shown.");

            SmallCameraKey = c.Bind("Controls", "SmallCameraKey", KeyCode.N,
                "Key for the small rear-view camera under the minimap. None = no small camera.");
            SmallCameraKeyMode = c.Bind("Controls", "SmallCameraKeyMode", SmallCameraMode.Toggle,
                "Toggle: press the key to show the small camera, press again to hide it. Hold: the small camera shows only while the key is held.");
            FullScreenKey = c.Bind("Controls", "FullScreenKey", KeyCode.J,
                "Hold this key to see behind you on the whole screen. Your character keeps facing forward. None = no full-screen view.");

            SmallHeight = c.Bind("SmallCamera", "Height", SmallCameraLayout.DefaultHeight, new ConfigDescription(
                "Height of the small camera in screen pixels; the width follows at 16:9 (450 = 800x450).",
                new AcceptableValueRange<int>(SmallCameraLayout.MinHeight, SmallCameraLayout.MaxHeight)));
            SmallOffsetX = c.Bind("SmallCamera", "OffsetX", 0, new ConfigDescription(
                "Moves the small camera sideways from its place under the minimap, screen pixels (negative = left, positive = right). It always stays on screen.",
                new AcceptableValueRange<int>(-1500, 1500)));
            SmallOffsetY = c.Bind("SmallCamera", "OffsetY", 0, new ConfigDescription(
                "Moves the small camera up or down from its place under the minimap, screen pixels (negative = up, positive = down). It always stays on screen.",
                new AcceptableValueRange<int>(-1000, 1000)));

            CameraDistance = c.Bind("Camera", "DistanceBehind", 0.65f, new ConfigDescription(
                "How far behind your eyes the rear camera sits, metres.", new AcceptableValueRange<float>(0.05f, 3f)));
            CameraHeight = c.Bind("Camera", "HeightOffset", 0f, new ConfigDescription(
                "Raises (positive) or lowers (negative) the rear camera from eye height, metres.", new AcceptableValueRange<float>(-1f, 1f)));
            FieldOfView = c.Bind("Camera", "FieldOfView", 75f, new ConfigDescription(
                "Vertical field of view of the rear camera, degrees.", new AcceptableValueRange<float>(40f, 110f)));
            UpdateFps = c.Bind("Camera", "UpdateFPS", 15, new ConfigDescription(
                "How many times per second the rear view is redrawn. Higher = smoother, costs more performance.", new AcceptableValueRange<int>(5, 60)));
            MirrorImage = c.Bind("Camera", "MirrorImage", false,
                "Flips the rear view left-right, like a car mirror.");

            DebugLog = c.Bind("DebugLog", "Enable", false,
                "Writes detailed lines about everything the mod does to the BepInEx log (for bug reports). Leave off while playing normally.");

            ConfigTools.ApplyCommaFix(c, savedLines);    // after every Bind: "3,5" typed by hand means 3.5, not 35
            ConfigTools.DropRemoved(c);

            EnableAll.SettingChanged += (s, e) => ModLog.Info("Config: EnableAll = " + EnableAll.Value + (EnableAll.Value ? ", the mod is on" : ", the mod is inert (no rear view)"));
            SmallCameraKeyMode.SettingChanged += (s, e) => ModLog.Verbose("Config: SmallCameraKeyMode = " + SmallCameraKeyMode.Value);
            ModLog.Verbose("Config: bound (EnableAll " + EnableAll.Value + ", small key " + SmallCameraKey.Value + " " + SmallCameraKeyMode.Value
                + ", full-screen key " + FullScreenKey.Value + ", height " + SmallHeight.Value + " px, offset " + SmallOffsetX.Value + "/" + SmallOffsetY.Value
                + " px, distance " + CameraDistance.Value + " m, height offset " + CameraHeight.Value + " m, FOV " + FieldOfView.Value + " deg, "
                + UpdateFps.Value + " fps, mirror " + MirrorImage.Value + ", debug log " + DebugLog.Value + ")");
            return EnableAll != null && SmallCameraKey != null && SmallCameraKeyMode != null && FullScreenKey != null && SmallHeight != null
                && SmallOffsetX != null && SmallOffsetY != null && CameraDistance != null && CameraHeight != null && FieldOfView != null
                && UpdateFps != null && MirrorImage != null && DebugLog != null;
        }
    }
}
