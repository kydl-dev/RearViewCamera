namespace RearViewCamera.Logic
{
    /// <summary>Where the small camera goes relative to the game's status-effect icons under the minimap (player config, D-027 / D-037).</summary>
    internal enum StatusIconPlacement
    {
        /// The window moves down below the icons that are showing (default, user 2026-10-26).
        Under,
        /// The window stays in place; the icons are drawn over the camera picture.
        Behind,
        /// The window stays in place and covers the icons.
        OnTop
    }
}
