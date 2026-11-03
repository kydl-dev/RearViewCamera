using KydlCore.Common;

namespace RearViewCamera.Testing
{
    /// <summary>
    /// DrakesTestLab hooks (tests/TestLab/suites/RearViewCamera); names "RearViewCamera.*". They read the REAL controller state; key presses come from the
    /// lab's own pressKey step (the game reads them through ZInput like real keys).
    /// </summary>
    internal static class TestHooks
    {
        /// Every test starts here (MIGRATION_DECISIONS C26).
        [TestHook("RearViewCamera.arena")]
        static string Arena() { return TestArena.MovePlayer(); }

        [TestHook("RearViewCamera.selftest")]
        static string SelfTest() { return TestLabSupport.RunInGameTests(typeof(InGameTests)); }

        /// What the rear view shows now: Off / Small / FullScreen.
        [TestHook("RearViewCamera.shown")]
        static string Shown() { return RearViewController.Shown.ToString(); }

        [TestHook("RearViewCamera.featureRunning")]
        static bool FeatureRunning(string id) { return Bootstrap.ModInstaller.Current != null && Bootstrap.ModInstaller.Current.Features.IsRunning(id); }
    }
}
