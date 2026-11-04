using System.Collections.Generic;
using System.Linq;
using RearViewCamera.Logic;

namespace RearViewCamera.Tests.Unit;

/// <summary>What the key logic writes to the log (the first thing a bug report shows): reasons, transitions and one-time messages.</summary>
[TestFixture]
public class RearViewKeyLogicLogTests
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
    void ToggleOn() { Toggle(Keys(smallHeld: true, smallPressed: true)); Toggle(Keys()); }
    string Last { get { return _log[_log.Count - 1]; } }
    int Count(string part) { return _log.Count(l => l.Contains(part)); }

    [Test]
    public void Created_LogsTheStartState()
    {
        Assert.That(_log, Is.EqualTo(new[] { "RearViewKeyLogic: created (toggle off, mode Off)" }));
    }

    [Test]
    public void Current_FollowsTheLastStep()
    {
        Assert.That(_logic.Current, Is.EqualTo(RearViewMode.Off));
        Toggle(Keys(smallHeld: true, smallPressed: true));
        Assert.That(_logic.Current, Is.EqualTo(RearViewMode.Small));
    }

    [TestCase(true)]
    [TestCase(false)]
    public void FirstStep_LogsTheConfiguredMode(bool toggle)
    {
        SmallCameraMode mode = toggle ? SmallCameraMode.Toggle : SmallCameraMode.Hold;
        _logic.Step(Keys(), mode);
        Assert.That(_log, Does.Contain("RearViewKeyLogic: small-camera mode " + mode));
        _log.Clear();
        _logic.Step(Keys(), mode);
        Assert.That(_log, Is.Empty, "the same mode again says nothing");
    }

    [Test]
    public void ModeChange_LogsIt_AndMentionsAToggleThatWasReset()
    {
        Toggle(Keys());
        _log.Clear();
        Hold(Keys());
        Assert.That(Last, Is.EqualTo("RearViewKeyLogic: small-camera mode Toggle -> Hold"));
        _log.Clear();
        ToggleOn();           // back to Toggle (a change, toggle was off) and turn it on
        _log.Clear();
        Hold(Keys());
        Assert.That(_log, Does.Contain("RearViewKeyLogic: small-camera mode Toggle -> Hold, toggle reset to off"));
        Assert.That(_logic.ToggleOn, Is.False);
    }

    [Test]
    public void Blocked_LogsOnce_WithTheReason_AndAnOnToggleStays()
    {
        ToggleOn();
        _log.Clear();
        Toggle(Keys(blocked: true));
        Toggle(Keys(blocked: true));
        Assert.That(Count("keys suppressed ("), Is.EqualTo(1));
        Assert.That(_log, Does.Contain("RearViewKeyLogic: keys suppressed (text entry open), toggle stays on"));
        Assert.That(_logic.Suppressed, Is.True);
        Assert.That(_logic.Current, Is.EqualTo(RearViewMode.Small));
    }

    [Test]
    public void UiOpeningKey_IsNamedInTheLog()
    {
        Toggle(Keys(uiKey: true));
        Assert.That(_log, Does.Contain("RearViewKeyLogic: keys suppressed (UI-opening key pressed), toggle stays off"));
    }

    [Test]
    public void ReleaseAfterABlock_LogsOnce_AndIsAcceptedAgain()
    {
        Toggle(Keys(blocked: true));
        _log.Clear();
        Toggle(Keys());
        Toggle(Keys());
        Assert.That(Count("keys released after a block, input accepted again"), Is.EqualTo(1));
        Assert.That(_logic.Suppressed, Is.False);
    }

    [Test]
    public void Transition_Logs_FromTo_Reason_Keys_AndMode()
    {
        Toggle(Keys(smallHeld: true, smallPressed: true));
        Assert.That(Last, Is.EqualTo("RearViewKeyLogic: Off -> Small (toggle on; small held True, small pressed True, full held False, blocked False, ui key False, map False, screen hidden False, mode Toggle)"));
    }

    [Test]
    public void Reasons_OfTheFreeInput()
    {
        Hold(Keys(smallHeld: true));
        Assert.That(Last, Does.Contain("Off -> Small (small-camera key held (Hold mode); "));
        Hold(Keys());
        Assert.That(Last, Does.Contain("Small -> Off (no key held; "));
        Hold(Keys(fullHeld: true));
        Assert.That(Last, Does.Contain("Off -> FullScreen (full-screen key held; "));
        Toggle(Keys(smallPressed: true, smallHeld: true));
        Toggle(Keys(smallPressed: true, smallHeld: true));
        Assert.That(Last, Does.Contain("(toggle off, full-screen key not held; "));
        Assert.That(Count("small-camera key pressed, toggle now on"), Is.EqualTo(1));
        Assert.That(Count("small-camera key pressed, toggle now off"), Is.EqualTo(1));
    }

    [Test]
    public void Reason_WhileBlocked_AFullScreenViewEnds()
    {
        Hold(Keys(fullHeld: true));
        Hold(Keys(fullHeld: true, blocked: true));
        Assert.That(Last, Does.Contain("FullScreen -> Off (keys suppressed; "));
    }

    [Test]
    public void Reason_WaitingForBothKeysToBeReleased()
    {
        ToggleOn();
        Toggle(Keys(blocked: true));                      // suppressed, the toggle keeps the small view
        Hold(Keys(fullHeld: true));                       // not blocked any more but a key is still held; the mode change resets the toggle
        Assert.That(Last, Does.Contain("Small -> Off (waiting until both keys are released after a block; "));
    }

    [Test]
    public void Reason_InventoryOpen_NamesTheKeptToggle()
    {
        ToggleOn();
        Toggle(Keys(hidden: true));
        Assert.That(Last, Does.Contain("Small -> Off (inventory / menu / loading screen open (state kept: toggle on); "));
        Assert.That(_logic.ToggleOn, Is.True, "the toggle is kept while the screen is open");
        Toggle(Keys());
        Assert.That(_logic.Current, Is.EqualTo(RearViewMode.Small), "the same view returns when the screen closes");
    }

    [Test]
    public void LargeMap_ToggleKeyOnAShownCamera_HidesItAndSaysSo()
    {
        ToggleOn();
        _log.Clear();
        Toggle(Keys(map: true, smallPressed: true, smallHeld: true));
        Assert.That(_log, Does.Contain("RearViewKeyLogic: small-camera key pressed on the large map, toggle now off"));
        Assert.That(Last, Does.Contain("Small -> Off (large map open, nothing new is shown; "));
    }

    [Test]
    public void LargeMap_ToggleKeyWhileOff_IsIgnoredAndSaysSo()
    {
        Toggle(Keys());
        _log.Clear();
        Toggle(Keys(map: true, smallPressed: true, smallHeld: true));
        Assert.That(_log, Does.Contain("RearViewKeyLogic: small-camera key pressed on the large map while off - ignored (the map may only hide it)"));
        Assert.That(_logic.Current, Is.EqualTo(RearViewMode.Off));
    }

    [Test]
    public void LargeMap_FullScreenKey_IsIgnoredAndSaysSo_UnlessFullScreenIsAlreadyShown()
    {
        Toggle(Keys());
        _log.Clear();
        Toggle(Keys(map: true, fullHeld: true));
        Assert.That(_log, Does.Contain("RearViewKeyLogic: full-screen key held on the large map - ignored"));
        Assert.That(_logic.Current, Is.EqualTo(RearViewMode.Off));

        ToggleOn();
        Toggle(Keys(fullHeld: true));
        Assert.That(_logic.Current, Is.EqualTo(RearViewMode.FullScreen));
        _log.Clear();
        Toggle(Keys(map: true, fullHeld: true));
        Assert.That(Count("full-screen key held on the large map"), Is.EqualTo(0), "already full screen: nothing to ignore");
        Assert.That(Last, Does.Contain("FullScreen -> Small (large map open, small camera was shown and stays; "));
    }

    [Test]
    public void LargeMap_FullScreenKeyWithoutAnythingShown_NeverOpensIt()
    {
        Toggle(Keys(map: true, fullHeld: true));
        Assert.That(_logic.Current, Is.EqualTo(RearViewMode.Off));
        Toggle(Keys(map: true, fullHeld: false, smallHeld: true, smallPressed: true));
        Assert.That(_logic.Current, Is.EqualTo(RearViewMode.Off));
    }

    [Test]
    public void ToStringOfTheKeys_ListsEveryFlag()
    {
        var k = new RearViewKeys { SmallHeld = true, FullHeld = true, MapOpen = true };
        Assert.That(k.ToString(), Is.EqualTo("small held True, small pressed False, full held True, blocked False, ui key False, map True, screen hidden False"));
    }
}
