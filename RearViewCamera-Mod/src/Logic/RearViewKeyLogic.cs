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
            else
            {
                if (_suppressed)
                {
                    _suppressed = false;
                    _log("RearViewKeyLogic: keys released after a block, input accepted again");
                }
                next = Decide(keys, out why);
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
