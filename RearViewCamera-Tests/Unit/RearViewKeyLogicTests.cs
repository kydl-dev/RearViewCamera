using System.Collections.Generic;
using RearViewCamera.Logic;

namespace RearViewCamera.Tests.Unit
{
    /// <summary>The two-key rules (D-005, D-012, D-019): toggle / hold, full screen while held, blocking, release after a block.</summary>
    [TestFixture]
    public class RearViewKeyLogicTests
    {
        readonly List<string> _log = new List<string>();
        RearViewKeyLogic _logic;

        [SetUp]
        public void SetUp()
        {
            _log.Clear();
            _logic = new RearViewKeyLogic(_log.Add);
        }

        static RearViewKeys Keys(bool smallHeld = false, bool smallPressed = false, bool fullHeld = false, bool blocked = false, bool uiKey = false,
            bool map = false, bool hidden = false)
        {
            return new RearViewKeys { SmallHeld = smallHeld, SmallPressed = smallPressed, FullHeld = fullHeld, Blocked = blocked, UiOpeningKey = uiKey,
                MapOpen = map, ScreenHidden = hidden };
        }

        RearViewMode Toggle(RearViewKeys k) { return _logic.Step(k, SmallCameraMode.Toggle); }
        RearViewMode Hold(RearViewKeys k) { return _logic.Step(k, SmallCameraMode.Hold); }
        void PressAndRelease() { Toggle(Keys(smallHeld: true, smallPressed: true)); Toggle(Keys()); }

        [Test]
        public void StartsOff()
        {
            Assert.That(Toggle(Keys()), Is.EqualTo(RearViewMode.Off));
        }

        [Test]
        public void Toggle_PressTurnsOnAndStaysOn_PressAgainTurnsOff()
        {
            Assert.That(Toggle(Keys(smallHeld: true, smallPressed: true)), Is.EqualTo(RearViewMode.Small));
            Assert.That(Toggle(Keys(smallHeld: true)), Is.EqualTo(RearViewMode.Small), "holding the key does not toggle again");
            Assert.That(Toggle(Keys()), Is.EqualTo(RearViewMode.Small), "stays on after release");
            Assert.That(Toggle(Keys(smallHeld: true, smallPressed: true)), Is.EqualTo(RearViewMode.Off));
        }

        [Test]
        public void Hold_ShowsOnlyWhileHeld()
        {
            Assert.That(Hold(Keys(smallHeld: true, smallPressed: true)), Is.EqualTo(RearViewMode.Small));
            Assert.That(Hold(Keys(smallHeld: true)), Is.EqualTo(RearViewMode.Small));
            Assert.That(Hold(Keys()), Is.EqualTo(RearViewMode.Off));
        }

        [Test]
        public void FullScreen_WhileHeld_WinsOverSmall_ThenBackToSmall()
        {
            PressAndRelease();
            Assert.That(Toggle(Keys(fullHeld: true)), Is.EqualTo(RearViewMode.FullScreen));
            Assert.That(Toggle(Keys()), Is.EqualTo(RearViewMode.Small), "toggle is still on after the full-screen key is released");
        }

        [Test]
        public void FullScreen_FromOff_ReturnsToOff()
        {
            Assert.That(Toggle(Keys(fullHeld: true)), Is.EqualTo(RearViewMode.FullScreen));
            Assert.That(Toggle(Keys()), Is.EqualTo(RearViewMode.Off));
        }

        [Test]
        public void Blocked_PressIsIgnored()
        {
            Assert.That(Toggle(Keys(smallHeld: true, smallPressed: true, blocked: true)), Is.EqualTo(RearViewMode.Off));
            Assert.That(_logic.ToggleOn, Is.False);
        }

        [Test]
        public void Blocked_OnToggleStaysOn_FullScreenIgnored()
        {
            PressAndRelease();
            Assert.That(Toggle(Keys(blocked: true)), Is.EqualTo(RearViewMode.Small));
            Assert.That(Toggle(Keys(fullHeld: true, blocked: true)), Is.EqualTo(RearViewMode.Small));
            Assert.That(Toggle(Keys(smallHeld: true, smallPressed: true, blocked: true)), Is.EqualTo(RearViewMode.Small));
        }

        [Test]
        public void UiOpeningKey_CountsAsBlock()
        {
            Assert.That(Toggle(Keys(smallHeld: true, smallPressed: true, uiKey: true)), Is.EqualTo(RearViewMode.Off));
            Assert.That(_logic.Suppressed, Is.True);
        }

        [Test]
        public void AfterBlock_KeysMustBeReleasedFirst()
        {
            Toggle(Keys(blocked: true));
            Assert.That(Toggle(Keys(fullHeld: true)), Is.EqualTo(RearViewMode.Off), "full-screen key held while chat closed: ignored");
            Assert.That(_logic.Suppressed, Is.True);
            Assert.That(Toggle(Keys()), Is.EqualTo(RearViewMode.Off));
            Assert.That(_logic.Suppressed, Is.False);
            Assert.That(Toggle(Keys(fullHeld: true)), Is.EqualTo(RearViewMode.FullScreen));
        }

        [Test]
        public void ModeChange_ResetsToggle()
        {
            PressAndRelease();
            Assert.That(Hold(Keys()), Is.EqualTo(RearViewMode.Off));
            Assert.That(Toggle(Keys()), Is.EqualTo(RearViewMode.Off), "back to Toggle: still off");
        }

        [Test]
        public void EveryModeChangeIsLogged()
        {
            PressAndRelease();
            Toggle(Keys(fullHeld: true));
            Assert.That(_log.FindAll(l => l.Contains(" -> ")).Count, Is.GreaterThanOrEqualTo(2));
        }

        // --- D-026 large map ------------------------------------------------------------------------------------------------------
        [Test]
        public void Map_ShownSmallStays_KeyHidesIt_CannotReopen()
        {
            PressAndRelease();
            Assert.That(Toggle(Keys(map: true)), Is.EqualTo(RearViewMode.Small));
            Assert.That(Toggle(Keys(smallHeld: true, smallPressed: true, map: true)), Is.EqualTo(RearViewMode.Off));
            Toggle(Keys(map: true));
            Assert.That(Toggle(Keys(smallHeld: true, smallPressed: true, map: true)), Is.EqualTo(RearViewMode.Off), "the map may only hide it");
            Assert.That(_logic.ToggleOn, Is.False);
        }

        [Test]
        public void Map_OffStaysOff_NoFullScreen()
        {
            Assert.That(Toggle(Keys(fullHeld: true, map: true)), Is.EqualTo(RearViewMode.Off));
            Assert.That(Toggle(Keys(smallHeld: true, smallPressed: true, map: true)), Is.EqualTo(RearViewMode.Off));
        }

        [Test]
        public void Map_OpenedDuringFullScreen_FallsBackToSmallIfToggleOn()
        {
            PressAndRelease();
            Assert.That(Toggle(Keys(fullHeld: true)), Is.EqualTo(RearViewMode.FullScreen));
            Assert.That(Toggle(Keys(fullHeld: true, map: true)), Is.EqualTo(RearViewMode.Small));
        }

        [Test]
        public void Map_HoldMode_StaysWhileHeld_NoNewActivation()
        {
            Assert.That(Hold(Keys(smallHeld: true, smallPressed: true)), Is.EqualTo(RearViewMode.Small));
            Assert.That(Hold(Keys(smallHeld: true, map: true)), Is.EqualTo(RearViewMode.Small));
            Assert.That(Hold(Keys(map: true)), Is.EqualTo(RearViewMode.Off));
            Assert.That(Hold(Keys(smallHeld: true, smallPressed: true, map: true)), Is.EqualTo(RearViewMode.Off));
        }

        // --- D-036 inventory / menu / loading: hide, then restore -----------------------------------------------------------------
        [Test]
        public void HiddenScreen_HidesAndRestores()
        {
            PressAndRelease();
            Assert.That(Toggle(Keys(hidden: true)), Is.EqualTo(RearViewMode.Off));
            Assert.That(_logic.ToggleOn, Is.True, "state kept");
            Assert.That(Toggle(Keys()), Is.EqualTo(RearViewMode.Small), "restored");
        }

        [Test]
        public void HiddenScreen_PressesIgnored()
        {
            Assert.That(Toggle(Keys(smallHeld: true, smallPressed: true, hidden: true)), Is.EqualTo(RearViewMode.Off));
            Assert.That(_logic.ToggleOn, Is.False);
            Assert.That(Toggle(Keys(fullHeld: true, hidden: true)), Is.EqualTo(RearViewMode.Off));
        }

        [Test]
        public void HiddenScreen_HoldStillHeldAfterClose_ShowsAgain()
        {
            Assert.That(Hold(Keys(smallHeld: true, smallPressed: true)), Is.EqualTo(RearViewMode.Small));
            Assert.That(Hold(Keys(smallHeld: true, hidden: true)), Is.EqualTo(RearViewMode.Off));
            Assert.That(Hold(Keys(smallHeld: true)), Is.EqualTo(RearViewMode.Small));
        }
    }
}
