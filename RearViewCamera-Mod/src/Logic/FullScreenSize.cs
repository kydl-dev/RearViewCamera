using System;

namespace RearViewCamera.Logic
{
    /// <summary>
    /// Render-texture size of the full-screen rear view (user, 2026-10-25: "at least 1080p"): the screen size times the render scale,
    /// but never fewer than <c>minHeight</c> lines, and never more than the screen itself; always the screen's aspect ratio, so the
    /// picture is not stretched. Pure logic: unit-tested.
    /// </summary>
    internal static class FullScreenSize
    {
        internal const int DefaultMinHeight = 1080;

        /// Expected outcome: (width, height) in pixels, height = clamp(screenHeight * scale, minHeight, screenHeight).
        internal static void For(int screenWidth, int screenHeight, float scale, int minHeight, out int width, out int height)
        {
            screenWidth = Math.Max(16, screenWidth);
            screenHeight = Math.Max(16, screenHeight);
            int scaled = (int)Math.Round(screenHeight * (double)scale);
            height = Math.Min(screenHeight, Math.Max(minHeight, scaled));
            width = (int)Math.Round(screenWidth * (double)height / screenHeight);
        }
    }
}
