namespace RearViewCamera.Logic
{
    /// <summary>The key state of one frame, read by the controller from the game and handed to <see cref="RearViewKeyLogic"/>.</summary>
    internal struct RearViewKeys
    {
        /// Small-camera key is down this frame (held).
        internal bool SmallHeld;
        /// Small-camera key went down this frame (edge).
        internal bool SmallPressed;
        /// Full-screen key is down this frame (held).
        internal bool FullHeld;
        /// Chat / console / Configuration Manager / a text field has the keyboard (D-012).
        internal bool Blocked;
        /// A key that opens chat / console / config (Enter, /, F5, F2) went down this frame: the UI may not report focus yet.
        internal bool UiOpeningKey;
        /// The large map is open (D-026): a shown small camera may stay or be hidden by its key, nothing may be shown newly, no full screen.
        internal bool MapOpen;
        /// Inventory / escape menu / loading or teleport screen is open (D-036): nothing is shown, presses are ignored, the state is kept.
        internal bool ScreenHidden;

        public override string ToString()
        {
            return "small held " + SmallHeld + ", small pressed " + SmallPressed + ", full held " + FullHeld + ", blocked " + Blocked + ", ui key " + UiOpeningKey + ", map " + MapOpen + ", screen hidden " + ScreenHidden;
        }
    }
}
