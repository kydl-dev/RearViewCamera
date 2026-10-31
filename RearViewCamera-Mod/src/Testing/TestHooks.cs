using Kydl.Common;

namespace RearViewCamera.Testing
{
    /// <summary>
    /// KydlCore D-084: hooks for DrakesTestLab suites (RearViewCamera-Tests/TestLab/suites/RearViewCamera). A hook is a static method with [TestHook("name")]; DrakesTestLab finds
    /// it by the attribute's NAME (the mod does not reference DrakesTestLab). Hook names carry the mod id so two mods never clash.
    /// Actions call the REAL mod code (never a copy of it); reads return short values (int / bool / short text) a suite compares with `expect` / `contains` / `min` / `max`.
    /// A GameObject parameter binds to a tag made with `"as": "<tag>"` on a `spawn` step or on a hook step that returns a GameObject.
    /// </summary>
    internal static class TestHooks
    {
        /// <summary>All Startup_* / World_* checks of InGameTests (the ones Kydl.GameProbe runs): "PASS: ..." or "FAIL: ...".</summary>
        [TestHook("RearViewCamera.selftest")]
        static string SelfTest() { return TestLabSupport.RunInGameTests(typeof(InGameTests)); }

        // Add one hook per thing a suite must read or do that no built-in step can: a saved value, a decision, a state flag.
    }
}

