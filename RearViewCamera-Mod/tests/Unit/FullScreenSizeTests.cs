using RearViewCamera.Logic;

namespace RearViewCamera.Tests.Unit
{
    /// <summary>Full-screen render size (D-033): at most 1080 lines, never above the screen, screen aspect; upscaled by the UI.</summary>
    [TestFixture]
    public class FullScreenSizeTests
    {
        [TestCase(3440, 1440, 1080, 2580, 1080)]   // the user's ultrawide
        [TestCase(1920, 1080, 1080, 1920, 1080)]   // 1080p: native
        [TestCase(1280, 720, 1080, 1280, 720)]     // smaller screen: native, never above it
        [TestCase(3840, 2160, 1080, 1920, 1080)]   // 4K
        [TestCase(2560, 1440, 1080, 1920, 1080)]
        public void Size(int sw, int sh, int max, int w, int h)
        {
            FullScreenSize.For(sw, sh, max, out int width, out int height);
            Assert.That(height, Is.EqualTo(h));
            Assert.That(width, Is.EqualTo(w));
        }
    }
}
