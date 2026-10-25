using RearViewCamera.Logic;

namespace RearViewCamera.Tests.Unit
{
    /// <summary>Full-screen render size: at least 1080 lines, never above the screen, screen aspect.</summary>
    [TestFixture]
    public class FullScreenSizeTests
    {
        [TestCase(3440, 1440, 0.5f, 1080, 2580, 1080)]   // the user's ultrawide: half scale would be 720 -> raised to 1080
        [TestCase(3440, 1440, 1.0f, 1080, 3440, 1440)]   // full scale wins
        [TestCase(1920, 1080, 0.5f, 1080, 1920, 1080)]   // 1080p screen: native
        [TestCase(1280, 720, 0.5f, 1080, 1280, 720)]     // never above the screen
        [TestCase(3840, 2160, 0.5f, 1080, 1920, 1080)]   // 4K at half scale = 1080 already
        [TestCase(3840, 2160, 0.75f, 1080, 2880, 1620)]
        public void Size(int sw, int sh, float scale, int min, int w, int h)
        {
            FullScreenSize.For(sw, sh, scale, min, out int width, out int height);
            Assert.That(height, Is.EqualTo(h));
            Assert.That(width, Is.EqualTo(w));
        }
    }
}
