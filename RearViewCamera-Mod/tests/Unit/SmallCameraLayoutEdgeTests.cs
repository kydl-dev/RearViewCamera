using System;
using RearViewCamera.Logic;

namespace RearViewCamera.Tests.Unit;

/// <summary>The rectangle helpers and the edges of the layout rules (icon columns, partial minimap, the log text).</summary>
[TestFixture]
public class SmallCameraLayoutEdgeTests
{
    static readonly ScreenSize Screen = new ScreenSize(1920, 1080);
    static MinimapAnchor Map { get { return new MinimapAnchor(1900f, 290f, 10f, 20f); } }

    static HudRect Rect(float x, float y, float w, float h)
    {
        return new HudRect { X = x, Y = y, Width = w, Height = h };
    }

    [Test]
    public void RightAndBottom_AreTheFarEdges()
    {
        HudRect r = Rect(10f, 20f, 30f, 40f);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(r.Right, Is.EqualTo(40f));
            Assert.That(r.Bottom, Is.EqualTo(60f));
        }
    }

    [TestCase(0.4f, 0f, 0f, 0f, true)]
    [TestCase(0f, 0.4f, 0f, 0f, true)]
    [TestCase(0f, 0f, 0.4f, 0f, true)]
    [TestCase(0f, 0f, 0f, 0.4f, true)]
    [TestCase(-0.4f, -0.4f, -0.4f, -0.4f, true)]
    [TestCase(0.5f, 0f, 0f, 0f, false)]
    [TestCase(0f, 0.5f, 0f, 0f, false)]
    [TestCase(0f, 0f, 0.5f, 0f, false)]
    [TestCase(0f, 0f, 0f, 0.5f, false)]
    [TestCase(-0.5f, 0f, 0f, 0f, false)]
    [TestCase(0f, 0f, 0f, -0.6f, false)]
    public void SameAs_ToleratesLessThanHalfAPixelPerSide(float dx, float dy, float dw, float dh, bool same)
    {
        HudRect a = Rect(100f, 200f, 300f, 400f);
        HudRect b = Rect(100f + dx, 200f + dy, 300f + dw, 400f + dh);
        Assert.That(a.SameAs(b), Is.EqualTo(same));
    }

    [Test]
    public void ToString_DescribesSizePlaceAndHowItWasFound()
    {
        HudRect plain = Rect(1100f, 310f, 800f, 450f);
        plain.UnderMinimap = true;
        HudRect all = Rect(5f, 6f, 640f, 360f);
        all.UnderIcons = true;
        all.Clamped = true;
        using (Assert.EnterMultipleScope())
        {
            Assert.That(plain.ToString(), Is.EqualTo("800x450 px at (1100, 310) under the minimap"));
            Assert.That(Rect(1,2,3,4).ToString(), Is.EqualTo("3x4 px at (1, 2) top-right (no minimap)"));
            Assert.That(all.ToString(), Is.EqualTo("640x360 px at (5, 6) top-right (no minimap), below the status icons, pushed inside the screen"));
        }
    }

    [Test]
    public void OnlyHalfAMinimapAnchor_CountsAsNoMinimap()
    {
        HudRect noBottom = SmallCameraLayout.Place(Screen, 450, new MinimapAnchor(1900f, null, 10f, 20f), 0f, 0f);
        HudRect noRight = SmallCameraLayout.Place(Screen, 450, new MinimapAnchor(null, 290f, 10f, 20f), 0f, 0f);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(noBottom.UnderMinimap, Is.False);
            Assert.That(noBottom.X, Is.EqualTo(1920f - 20f - 800f));
            Assert.That(noRight.UnderMinimap, Is.False);
            Assert.That(noRight.Y, Is.EqualTo(20f));
        }
    }

    [TestCase(0f, 60f)]
    [TestCase(200f, 0f)]
    public void Under_AnEmptyIconRectangle_IsIgnored(float width, float height)
    {
        HudRect r = SmallCameraLayout.Place(Screen, 450, Map, 0f, 0f, Rect(1750f, 300f, width, height), StatusIconPlacement.Under);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(r.UnderIcons, Is.False);
            Assert.That(r.Y, Is.EqualTo(300f));
        }
    }

    [Test]
    public void Under_IconsTouchingTheRightEdgeOfTheWindow_AreNotInItsColumns()
    {
        // window columns 1100..1900; an icon block starting exactly at 1900 is next to it
        HudRect r = SmallCameraLayout.Place(Screen, 450, Map, 0f, 0f, Rect(1900f, 300f, 20f, 60f), StatusIconPlacement.Under);
        Assert.That(r.UnderIcons, Is.False);
    }

    [Test]
    public void Under_IconsTouchingTheLeftEdgeOfTheWindow_AreNotInItsColumns()
    {
        HudRect r = SmallCameraLayout.Place(Screen, 450, Map, 0f, 0f, Rect(1000f, 300f, 100f, 60f), StatusIconPlacement.Under);
        Assert.That(r.UnderIcons, Is.False);
    }

    [Test]
    public void Under_TheOffsetMovesTheColumnsAlong()
    {
        // offset -100: the window spans 1000..1800
        HudRect right = SmallCameraLayout.Place(Screen, 450, Map, -100f, 0f, Rect(1850f, 300f, 50f, 60f), StatusIconPlacement.Under);
        HudRect left = SmallCameraLayout.Place(Screen, 450, Map, -100f, 0f, Rect(1100f, 300f, 50f, 60f), StatusIconPlacement.Under);
        HudRect leftOut = SmallCameraLayout.Place(Screen, 450, Map, -100f, 0f, Rect(900f, 300f, 50f, 60f), StatusIconPlacement.Under);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(right.UnderIcons, Is.False, "right of the shifted window");
            Assert.That(left.UnderIcons, Is.True, "inside the shifted window");
            Assert.That(leftOut.UnderIcons, Is.False, "left of the shifted window");
        }
    }

    [Test]
    public void Under_IconsEndingJustAboveTheGap_DoNotPushTheWindow()
    {
        // top = 290 + 10 = 300; icons bottom 290 + gap 10 = 300: exactly enough room
        HudRect fits = SmallCameraLayout.Place(Screen, 450, Map, 0f, 0f, Rect(1750f, 230f, 200f, 60f), StatusIconPlacement.Under);
        // icons bottom 305: 305 + 10 > 300, the window goes below them
        HudRect pushed = SmallCameraLayout.Place(Screen, 450, Map, 0f, 0f, Rect(1750f, 245f, 200f, 60f), StatusIconPlacement.Under);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(fits.UnderIcons, Is.False);
            Assert.That(fits.Y, Is.EqualTo(300f));
            Assert.That(pushed.UnderIcons, Is.True);
            Assert.That(pushed.Y, Is.EqualTo(315f));
        }
    }
}
