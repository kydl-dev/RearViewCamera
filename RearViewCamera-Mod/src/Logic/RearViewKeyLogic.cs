using System;

namespace RearViewCamera.Logic
{
    /// <summary>
    /// Turns the two keys into a <see cref="RearViewMode"/> (D-005, D-012). Pure logic, no Unity / Valheim types: unit-tested.
    ///  * Small-camera key: Toggle or Hold (player config). Full-screen key: shown only while held, wins over the small camera.
    ///  * While text entry is open (or a UI-opening key goes down) the keys are ignored; an already-on toggle stays on.
    ///  * After a block, input is accepted again only once both keys are released - a key held while closing chat must not
    ///    switch anything.
    ///  * Switching the config between Toggle and Hold resets the toggle to off, so a stale toggle never shows up unasked.
    ///  * Large map open (D-026): a shown small camera stays (toggle on / key held); its key can only hide it; nothing new is shown and
    ///    the full-screen view never opens.
    ///  * Inventory / menu / loading screen open (D-036): nothing is shown, presses are ignored, the toggle is kept, so the same view
    ///    returns when the screen closes.
    /// Logs every state change through the sink it gets (the controller passes ModLog.Verbose).
    /// </summary>
    internal sealed class RearViewKeyLogic
    {
        readonly Action<string> _log;
        bool _toggleOn;
        bool _suppressed;
        bool _modeKnown;
        SmallCameraMode _mode;
        RearViewMode _current = RearViewMode.Off;

        internal RearViewKeyLogic(Action<string> log)
        {
            _log = log ?? (s => { });
            _log("RearViewKeyLogic: created (toggle off, mode Off)");
        }

        internal RearViewMode Current { get { return _current; } }
        internal bool ToggleOn { get { return _toggleOn; } }
        internal bool Suppressed { get { return _suppressed; } }

        /// One frame. Expected outcome: returns the mode for this frame (also kept in <see cref="Current"/>).
        internal RearViewMode Step(RearViewKeys keys, SmallCameraMode mode)
        {
            TrackModeChange(mode);
            string why;
            RearViewMode next;
            if (keys.Blocked || keys.UiOpeningKey)
            {
                if (!_suppressed)
                    _log("RearViewKeyLogic: keys suppressed (" + (keys.Blocked ? "text entry open" : "UI-opening key pressed") + "), toggle stays " + OnOff(_toggleOn));
                _suppressed = true;
                next = KeptMode();
                why = "keys suppressed";
            }
            else if (_suppressed && (keys.SmallHeld || keys.FullHeld))
            {
                next = KeptMode();
                why = "waiting until both keys are released after a block";
            }
            else if (keys.ScreenHidden)
            {
                next = RearViewMode.Off;
                why = "inventory / menu / loading screen open (state kept: toggle " + OnOff(_toggleOn) + ")";
            }
            else
            {
                if (_suppressed)
                {
                    _suppressed = false;
                    _log("RearViewKeyLogic: keys released after a block, input accepted again");
                }
                next = keys.MapOpen ? DecideOnMap(keys, out why) : Decide(keys, out why);
            }
            if (next != _current)
            {
                _log("RearViewKeyLogic: " + _current + " -> " + next + " (" + why + "; " + keys + ", mode " + _mode + ")");
                _current = next;
            }
            return _current;
        }

        /// Free input (not suppressed). Expected outcome: the mode the keys ask for, with the reason in `why`.
        RearViewMode Decide(RearViewKeys keys, out string why)
        {
            if (_mode == SmallCameraMode.Toggle && keys.SmallPressed)
            {
                _toggleOn = !_toggleOn;
                _log("RearViewKeyLogic: small-camera key pressed, toggle now " + OnOff(_toggleOn));
            }
            if (keys.FullHeld) { why = "full-screen key held"; return RearViewMode.FullScreen; }
            if (_mode == SmallCameraMode.Hold && keys.SmallHeld) { why = "small-camera key held (Hold mode)"; return RearViewMode.Small; }
            if (_mode == SmallCameraMode.Toggle && _toggleOn) { why = "toggle on"; return RearViewMode.Small; }
            why = _mode == SmallCameraMode.Toggle ? "toggle off, full-screen key not held" : "no key held";
            return RearViewMode.Off;
        }

        /// Large map open: only what is already shown as the small camera may stay; the key can only hide it. Expected outcome: Off or Small.
        RearViewMode DecideOnMap(RearViewKeys keys, out string why)
        {
            bool wasSmall = _current == RearViewMode.Small;
            if (_mode == SmallCameraMode.Toggle && keys.SmallPressed)
            {
                if (_toggleOn)
                {
                    _toggleOn = false;
                    _log("RearViewKeyLogic: small-camera key pressed on the large map, toggle now off");
                }
                else
                {
                    _log("RearViewKeyLogic: small-camera key pressed on the large map while off - ignored (the map may only hide it)");
                }
            }
            if (keys.FullHeld && _current != RearViewMode.FullScreen)
                _log("RearViewKeyLogic: full-screen key held on the large map - ignored");
            if (_mode == SmallCameraMode.Toggle && _toggleOn && (wasSmall || _current == RearViewMode.FullScreen))
            {
                why = "large map open, small camera was shown and stays";
                return RearViewMode.Small;
            }
            if (_mode == SmallCameraMode.Hold && keys.SmallHeld && wasSmall)
            {
                why = "large map open, small-camera key still held";
                return RearViewMode.Small;
            }
            why = "large map open, nothing new is shown";
            return RearViewMode.Off;
        }

        /// While keys are ignored only an on toggle keeps showing; a held key (Hold / full screen) does not.
        RearViewMode KeptMode()
        {
            return _mode == SmallCameraMode.Toggle && _toggleOn ? RearViewMode.Small : RearViewMode.Off;
        }

        void TrackModeChange(SmallCameraMode mode)
        {
            if (_modeKnown && mode == _mode) return;
            if (_modeKnown)
            {
                _log("RearViewKeyLogic: small-camera mode " + _mode + " -> " + mode + (_toggleOn ? ", toggle reset to off" : ""));
                _toggleOn = false;
            }
            else
            {
                _log("RearViewKeyLogic: small-camera mode " + mode);
            }
            _mode = mode;
            _modeKnown = true;
        }

        static string OnOff(bool on) { return on ? "on" : "off"; }
    }
}
