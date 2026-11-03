using RearViewCamera.Logic;

namespace RearViewCamera.Tests.Unit
{
    /// <summary>Small camera size and place (D-006, D-010, D-017).</summary>
    [TestFixture]
    public class SmallCameraLayoutTests
    {
        [TestCase(360, 640)]
        [TestCase(450, 800)]
        [TestCase(540, 960)]
        [TestCase(100, 640)]
        [TestCase(2000, 960)]
        public void Width_Is16By9_OfClampedHeight(int height, int width)
        {
            Assert.That(SmallCameraLayout.WidthFor(height), Is.EqualTo(width));
        }

        [Test]
        public void Default_Is800x450()
        {
            Assert.That(SmallCameraLayout.DefaultHeight, Is.EqualTo(450));
            Assert.That(SmallCameraLayout.WidthFor(SmallCameraLayout.DefaultHeight), Is.EqualTo(800));
        }

        [Test]
        public void UnderMinimap_RightAligned_WithGap()
        {
            HudRect r = SmallCameraLayout.Place(1920, 1080, 450, 1900f, 300f, 10f, 20f, 0f, 0f);
            Assert.That(r.UnderMinimap, Is.True);
            Assert.That(r.X, Is.EqualTo(1100f));
            Assert.That(r.Y, Is.EqualTo(310f));
            Assert.That(r.Width, Is.EqualTo(800f));
            Assert.That(r.Height, Is.EqualTo(450f));
            Assert.That(r.Clamped, Is.False);
        }

        [Test]
        public void NoMinimap_TopRightWithMargin()
        {
            HudRect r = SmallCameraLayout.Place(1920, 1080, 360, null, null, 10f, 20f, 0f, 0f);
            Assert.That(r.UnderMinimap, Is.False);
            Assert.That(r.X, Is.EqualTo(1920f - 20f - 640f));
            Assert.That(r.Y, Is.EqualTo(20f));
        }

        [Test]
        public void Offsets_AreAdded()
        {
            HudRect r = SmallCameraLayout.Place(1920, 1080, 450, 1900f, 300f, 10f, 20f, -100f, 50f);
            Assert.That(r.X, Is.EqualTo(1000f));
            Assert.That(r.Y, Is.EqualTo(360f));
        }

        [Test]
        public void StaysOnScreen()
        {
            HudRect r = SmallCameraLayout.Place(1920, 1080, 540, 1900f, 900f, 10f, 20f, 500f, 0f);
            Assert.That(r.Clamped, Is.True);
            Assert.That(r.X + r.Width, Is.LessThanOrEqualTo(1920f));
            Assert.That(r.Y + r.Height, Is.LessThanOrEqualTo(1080f));
            Assert.That(r.X, Is.GreaterThanOrEqualTo(0f));
        }

        [Test]
        public void ScreenSmallerThanWindow_PinsToTopLeft()
        {
            HudRect r = SmallCameraLayout.Place(800, 400, 540, null, null, 10f, 20f, 0f, 0f);
            Assert.That(r.X, Is.EqualTo(0f));
            Assert.That(r.Y, Is.EqualTo(0f));
        }

        // --- D-027 status-effect icons --------------------------------------------------------------------------------------------
        static readonly HudRect Icons = new HudRect { X = 1700f, Y = 300f, Width = 200f, Height = 60f };   // under the minimap, right-aligned

        [Test]
        public void Under_MovesBelowIcons()
        {
            HudRect r = SmallCameraLayout.Place(1920, 1080, 450, 1900f, 290f, 10f, 20f, 0f, 0f, Icons, StatusIconPlacement.Under);
            Assert.That(r.UnderIcons, Is.True);
            Assert.That(r.Y, Is.EqualTo(370f), "icons bottom 360 + gap 10");
        }

        [Test]
        public void Under_NoIcons_StaysUnderMinimap()
        {
            HudRect r = SmallCameraLayout.Place(1920, 1080, 450, 1900f, 290f, 10f, 20f, 0f, 0f, null, StatusIconPlacement.Under);
            Assert.That(r.UnderIcons, Is.False);
            Assert.That(r.Y, Is.EqualTo(300f));
        }

        [Test]
        public void Under_IconsInOtherColumns_Ignored()
        {
            var left = new HudRect { X = 10f, Y = 300f, Width = 200f, Height = 60f };
            HudRect r = SmallCameraLayout.Place(1920, 1080, 450, 1900f, 290f, 10f, 20f, 0f, 0f, left, StatusIconPlacement.Under);
            Assert.That(r.Y, Is.EqualTo(300f));
        }

        [TestCase(1)]   // StatusIconPlacement.Behind (the enum is internal: passed as int)
        [TestCase(2)]   // StatusIconPlacement.OnTop
        public void BehindAndOnTop_KeepPlace(int placement)
        {
            var p = (StatusIconPlacement)placement;
            HudRect r = SmallCameraLayout.Place(1920, 1080, 450, 1900f, 290f, 10f, 20f, 0f, 0f, Icons, p);
            Assert.That(r.Y, Is.EqualTo(300f));
            Assert.That(r.UnderIcons, Is.False);
        }

        [Test]
        public void Under_StillStaysOnScreen()
        {
            var tall = new HudRect { X = 1700f, Y = 300f, Width = 200f, Height = 600f };
            HudRect r = SmallCameraLayout.Place(1920, 1080, 540, 1900f, 290f, 10f, 20f, 0f, 0f, tall, StatusIconPlacement.Under);
            Assert.That(r.Bottom, Is.LessThanOrEqualTo(1080f));
            Assert.That(r.Clamped, Is.True);
        }
    }
}
