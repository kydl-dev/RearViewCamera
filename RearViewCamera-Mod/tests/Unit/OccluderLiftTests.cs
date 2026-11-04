using RearViewCamera.Logic;

namespace RearViewCamera.Tests.Unit;

/// <summary>R7 / D-031: raise and tilt over an animal between player and camera.</summary>
[TestFixture]
public class OccluderLiftTests
{
    [Test]
    public void AnimalBelowEye_NoLift()
    {
        OccluderLift.For(1.6f, 1.0f, 0.5f, 3f, 8f, out float raise, out float pitch);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(raise, Is.EqualTo(0f));
            Assert.That(pitch, Is.EqualTo(0f));
        }
    }

    [Test]
    public void Lox_LiftsOverItsBack_AndTiltsDown()
    {
        OccluderLift.For(2.5f, 3.2f, 0.5f, 3f, 8f, out float raise, out float pitch);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(raise, Is.EqualTo(1.2f).Within(0.001f));
            Assert.That(pitch, Is.EqualTo(8.53f).Within(0.05f));
        }
    }

    [TestCase(0f)]
    [TestCase(-5f)]
    public void NoLookAheadDistance_NoTilt(float lookAhead)
    {
        OccluderLift.For(1.6f, 3f, 0.5f, 3f, lookAhead, out float raise, out float pitch);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(raise, Is.GreaterThan(0f));
            Assert.That(pitch, Is.EqualTo(0f));
        }
    }

    [Test]
    public void Tilt_IsCappedAt89Degrees()
    {
        OccluderLift.For(1.6f, 3f, 0.5f, 3f, 0.01f, out _, out float pitch);
        Assert.That(pitch, Is.EqualTo(89f).Within(0.0001f));
    }

    [Test]
    public void Lift_IsCapped()
    {
        OccluderLift.For(1.6f, 10f, 0.5f, 3f, 8f, out float raise, out _);
        Assert.That(raise, Is.EqualTo(3f));
    }

    [Test]
    public void Toward_MovesAtSpeed_AndStopsAtTarget()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(OccluderLift.Toward(0f, 2f, 4f, 0.1f), Is.EqualTo(0.4f).Within(0.0001f));
            Assert.That(OccluderLift.Toward(1.9f, 2f, 4f, 0.1f), Is.EqualTo(2f));
            Assert.That(OccluderLift.Toward(2f, 0f, 4f, 0.25f), Is.EqualTo(1f).Within(0.0001f));
        }
    }
}
