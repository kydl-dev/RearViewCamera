using System;

namespace RearViewCamera.Logic
{
    /// <summary>A rectangle in screen pixels, origin top-left (like UI Toolkit), plus how it was found.</summary>
    internal struct HudRect
    {
        internal float X, Y, Width, Height;
        /// The wanted place did not fit on the screen and was pushed inside.
        internal bool Clamped;
        /// Placed under the minimap (false = minimap not found / hidden: top-right corner).
        internal bool UnderMinimap;

        internal bool SameAs(HudRect o)
        {
            return Math.Abs(X - o.X) < 0.5f && Math.Abs(Y - o.Y) < 0.5f && Math.Abs(Width - o.Width) < 0.5f && Math.Abs(Height - o.Height) < 0.5f;
        }

        public override string ToString()
        {
            return Width.ToString("F0") + "x" + Height.ToString("F0") + " px at (" + X.ToString("F0") + ", " + Y.ToString("F0") + ")"
                + (UnderMinimap ? " under the minimap" : " top-right (no minimap)") + (Clamped ? ", pushed inside the screen" : "");
        }
    }

    /// <summary>
    /// Size and place of the small camera (D-006, D-010, D-017): 16:9, height 360..540 screen pixels, right edge on the minimap's right
    /// edge, a gap below it, then the player's offsets; always fully on screen. Pure logic: unit-tested.
    /// </summary>
    internal static class SmallCameraLayout
    {
        internal const int MinHeight = 360;
        internal const int MaxHeight = 540;
        internal const int DefaultHeight = 450;

        /// Expected outcome: the height inside 360..540.
        internal static int ClampHeight(int height)
        {
            return Math.Max(MinHeight, Math.Min(MaxHeight, height));
        }

        /// 16:9 width for a (clamped) height, in whole pixels.
        internal static int WidthFor(int height)
        {
            return (int)Math.Round(ClampHeight(height) * 16.0 / 9.0);
        }

        /// <param name="mapRight">Minimap right edge in px from the left, or null when there is no visible minimap.</param>
        /// <param name="mapBottom">Minimap bottom edge in px from the top, or null.</param>
        /// <param name="gap">Space between minimap and camera, px.</param>
        /// <param name="margin">Distance to the top / right screen edge when there is no minimap, px.</param>
        internal static HudRect Place(int screenWidth, int screenHeight, int height, float? mapRight, float? mapBottom,
            float gap, float margin, float offsetX, float offsetY)
        {
            int h = ClampHeight(height);
            int w = WidthFor(h);
            bool underMap = mapRight.HasValue && mapBottom.HasValue;
            float right = underMap ? mapRight.Value : screenWidth - margin;
            float top = underMap ? mapBottom.Value + gap : margin;
            float x = right - w + offsetX;
            float y = top + offsetY;
            float cx = Clamp(x, 0f, Math.Max(0f, screenWidth - w));
            float cy = Clamp(y, 0f, Math.Max(0f, screenHeight - h));
            return new HudRect
            {
                X = cx,
                Y = cy,
                Width = w,
                Height = h,
                UnderMinimap = underMap,
                Clamped = Math.Abs(cx - x) > 0.01f || Math.Abs(cy - y) > 0.01f
            };
        }

        static float Clamp(float v, float min, float max)
        {
            return v < min ? min : (v > max ? max : v);
        }
    }
}
