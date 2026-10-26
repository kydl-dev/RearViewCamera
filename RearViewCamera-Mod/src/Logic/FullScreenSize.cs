using System;

namespace RearViewCamera.Logic
{
    /// <summary>
    /// Render-texture size of the full-screen rear view (D-033, supersedes D-023 / D-024): at most <c>maxHeight</c> lines (1080), never
    /// more than the screen, always the screen's aspect ratio; the UI stretches it up to the display (bilinear). Pure logic: unit-tested.
    /// </summary>
    internal static class FullScreenSize
    {
        internal const int DefaultMaxHeight = 1080;

        /// Expected outcome: (width, height) in pixels, height = min(screenHeight, maxHeight), width at the screen's aspect.
        internal static void For(int screenWidth, int screenHeight, int maxHeight, out int width, out int height)
        {
            screenWidth = Math.Max(16, screenWidth);
            screenHeight = Math.Max(16, screenHeight);
            height = Math.Max(16, Math.Min(screenHeight, maxHeight));
            width = (int)Math.Round(screenWidth * (double)height / screenHeight);
        }
    }
}
