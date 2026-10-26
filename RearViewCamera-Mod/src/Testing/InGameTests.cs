using Kydl.Common;

namespace RearViewCamera.Testing
{
    /// <summary>
    /// In-game tests run by the user's Kydl.GameProbe (dev tool, never a dependency: it finds this class by its NAME `InGameTests`).
    ///   Startup_*  run when the main menu is reached;   World_*  run 5 s after a world is loaded; both again on GameProbe's RetestKey (PageDown).
    /// Each method: static, no parameters, returns a text starting with PASS / FAIL / SKIP and the detail with numbers + units,
    /// e.g. "FAIL: coil radius 0.900 m, expected 0.180 m". null or an exception counts as FAIL. Results land in the session report.
    /// Keep them cheap and read-only: they also run in players' games when GameProbe is installed there (it never is for players).
    /// </summary>
    internal static class InGameTests
    {
        /// The player config is bound and the mod is switched on.
        static string Startup_ConfigBound()
        {
            if (ModSettings.EnableAll == null) return "FAIL: ModSettings.Bind did not run (EnableAll is null)";
            return ModSettings.On
                ? "PASS: EnableAll on, small key " + ModSettings.SmallCameraKey.Value + " (" + ModSettings.SmallCameraKeyMode.Value + "), full-screen key " + ModSettings.FullScreenKey.Value + ", height " + ModSettings.SmallHeight.Value + " px"
                : "SKIP: EnableAll is off in the config";
        }

        /// The small window would sit fully on screen, 16:9, under the minimap (computed with the controller's minimap lookup, nothing is shown).
        static string World_SmallCameraLayout()
        {
            bool map = RearViewController.TryMinimapEdges(out float right, out float bottom);
            var r = RearViewCamera.Logic.SmallCameraLayout.Place(UnityEngine.Screen.width, UnityEngine.Screen.height, ModSettings.SmallHeight.Value,
                map ? right : (float?)null, map ? bottom : (float?)null, ModSettings.MinimapGap.Value, ModSettings.ScreenMargin.Value,
                ModSettings.SmallOffsetX.Value, ModSettings.SmallOffsetY.Value, null, ModSettings.StatusEffects.Value);
            ModLog.Verbose("InGameTests: World_SmallCameraLayout " + r + (map ? " (minimap right " + right.ToString("F0") + ", bottom " + bottom.ToString("F0") + " px)" : " (no minimap)"));
            bool inside = r.X >= 0 && r.Y >= 0 && r.X + r.Width <= UnityEngine.Screen.width + 0.5f && r.Y + r.Height <= UnityEngine.Screen.height + 0.5f;
            bool ratio = System.Math.Abs(r.Width - System.Math.Round(r.Height * 16.0 / 9.0)) < 0.5;
            if (!inside) return "FAIL: small window " + r + " is not fully on the " + UnityEngine.Screen.width + "x" + UnityEngine.Screen.height + " screen";
            if (!ratio) return "FAIL: small window " + r.Width + "x" + r.Height + " px is not 16:9";
            if (!r.UnderMinimap) return "SKIP: no visible minimap, the window would be top-right: " + r;
            return "PASS: " + r;
        }

        /// R12: which keys the rear view really uses this session (backup Home / End when the configured key is taken).
        static string World_KeyBindings()
        {
            var k = RearViewController.KeyUse;
            string detail = "small " + ModSettings.SmallCameraKey.Value + " -> " + k.SmallKey + " (" + k.SmallState + "), full " + ModSettings.FullScreenKey.Value
                + " -> " + k.FullKey + " (" + k.FullState + ")";
            if (k.SmallState == RearViewCamera.Logic.KeyState.Off || k.FullState == RearViewCamera.Logic.KeyState.Off) return "FAIL: a key and its backup are taken - " + detail;
            return "PASS: " + detail;
        }

        /// What is shown right now (manual test: press N / hold J, then PageDown to re-run).
        static string World_ShownMode()
        {
            var mode = RearViewController.Shown;
            return mode == RearViewCamera.Logic.RearViewMode.Off ? "SKIP: rear view is off (press N or hold J, then PageDown)" : "PASS: " + mode + (mode == RearViewCamera.Logic.RearViewMode.Small ? ", window " + RearViewController.LastRect : "");
        }
    }
}
