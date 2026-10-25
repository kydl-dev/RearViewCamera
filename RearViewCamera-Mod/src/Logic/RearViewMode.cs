namespace RearViewCamera.Logic
{
    /// <summary>What the rear view shows in a frame. Pure type (no Unity / Valheim): unit-tested.</summary>
    internal enum RearViewMode
    {
        /// Nothing shown; camera and render texture are released.
        Off,
        /// The small 16:9 window under the minimap.
        Small,
        /// The whole screen, only while the full-screen key is held (D-005).
        FullScreen
    }

    /// <summary>How the small-camera key works (player config, D-005; default Toggle, D-019).</summary>
    internal enum SmallCameraMode
    {
        /// Press = on, press again = off.
        Toggle,
        /// Visible only while the key is held.
        Hold
    }
}
