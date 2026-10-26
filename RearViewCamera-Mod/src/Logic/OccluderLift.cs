using System;

namespace RearViewCamera.Logic
{
    /// <summary>
    /// R7 / D-031: when a creature (a lox you ride, a wolf following you) is between the player and the rear camera, the camera goes up
    /// over its back and tilts down so it looks past the animal instead of into it. Pure logic: unit-tested.
    /// </summary>
    internal static class OccluderLift
    {
        /// <param name="eyeY">Player eye height, metres (world).</param>
        /// <param name="occluderTopY">Top of the animal's collider, metres (world).</param>
        /// <param name="clearance">How far above the animal's top the camera goes, metres.</param>
        /// <param name="maxRaise">Highest raise allowed, metres.</param>
        /// <param name="lookAhead">Distance behind the player the tilted camera aims at, metres.</param>
        /// <param name="raise">Metres to move the camera up (0..maxRaise).</param>
        /// <param name="pitchDegrees">Degrees to tilt the camera down so it aims at eye height lookAhead metres behind (0..89).</param>
        internal static void For(float eyeY, float occluderTopY, float clearance, float maxRaise, float lookAhead, out float raise, out float pitchDegrees)
        {
            raise = Math.Max(0f, Math.Min(maxRaise, occluderTopY + clearance - eyeY));
            pitchDegrees = raise <= 0f || lookAhead <= 0f ? 0f : (float)Math.Min(89.0, Math.Atan2(raise, lookAhead) * 180.0 / Math.PI);
        }

        /// Moves `current` towards `target` by at most speed * seconds (smooth lift, no jumps at 15 renders a second).
        internal static float Toward(float current, float target, float speed, float seconds)
        {
            float step = Math.Max(0f, speed * seconds);
            if (Math.Abs(target - current) <= step) return target;
            return current + Math.Sign(target - current) * step;
        }
    }
}
