using Kydl.Common;
using RearViewCamera.Logic;
using UnityEngine;

namespace RearViewCamera.Rendering
{
    /// <summary>
    /// R7 / D-031, game side: finds a creature between the player's eyes and the rear camera (or around the camera point) and lifts /
    /// tilts the camera over it (<see cref="OccluderLift"/>). Allocation-free: fixed hit buffers, run once per rear-view render.
    /// </summary>
    internal sealed class AnimalLift
    {
        static readonly RaycastHit[] Hits = new RaycastHit[16];
        static readonly Collider[] Overlaps = new Collider[16];
        static int _mask = -1;

        float _lift;
        float _lastTime = -1f;
        int _lastBlocker;   // instance id, 0 = none: log only when the animal changes (T-G006: the old text with heights logged every render)

        /// Metres the camera is lifted right now (diagnostics).
        internal float Lift { get { return _lift; } }

        /// Expected outcome: the camera position / rotation for this render, lifted and tilted when an animal is in the way.
        internal void Apply(Player player, Vector3 eye, Vector3 look, ref Vector3 position, ref Quaternion rotation)
        {
            if (_mask == -1)
            {
                _mask = LayerMask.GetMask("character", "character_net", "character_ghost", "character_noenv");
                ModLog.Verbose("AnimalLift: character layer mask 0x" + _mask.ToString("X8"));
            }
            Character blocker = FindBlocker(player, eye, position, out float top);
            float target = 0f;
            if (blocker != null)
                OccluderLift.For(eye.y, top, ModSettings.LiftClearance.Value, ModSettings.LiftMax.Value, ModSettings.LiftLookAhead.Value, out target, out _);
            int id = blocker != null ? blocker.GetInstanceID() : 0;
            if (id != _lastBlocker)
            {
                ModLog.Verbose("AnimalLift: " + (blocker != null ? "animal in the way: " + blocker.m_name + " (top " + top.ToString("F2") + " m, eye " + eye.y.ToString("F2")
                    + " m), lift to " + target.ToString("F2") + " m" : "no animal in the way, lift back to 0"));
                _lastBlocker = id;
            }
            float now = Time.unscaledTime;
            float dt = _lastTime < 0f ? 1f : now - _lastTime;
            _lastTime = now;
            _lift = OccluderLift.Toward(_lift, target, ModSettings.LiftSpeed.Value, dt);
            if (_lift <= 0f) return;
            OccluderLift.For(0f, _lift - ModSettings.LiftClearance.Value, ModSettings.LiftClearance.Value, ModSettings.LiftMax.Value, ModSettings.LiftLookAhead.Value,
                out _, out float pitch);
            position += Vector3.up * _lift;
            rotation *= Quaternion.Euler(pitch, 0f, 0f);    // positive X = look down
        }

        /// Resets the smoothing when the rear view is hidden.
        internal void Reset()
        {
            _lift = 0f;
            _lastTime = -1f;
            _lastBlocker = 0;
        }

        Character FindBlocker(Player player, Vector3 eye, Vector3 camera, out float top)
        {
            top = float.MinValue;
            Character best = null;
            Vector3 dir = camera - eye;
            float dist = dir.magnitude + ModSettings.LiftProbeRadius.Value;
            if (dist > 0.01f)
            {
                int n = Physics.SphereCastNonAlloc(eye, ModSettings.LiftProbeRadius.Value, dir.normalized, Hits, dist, _mask, QueryTriggerInteraction.Ignore);
                for (int i = 0; i < n; i++) best = Higher(Hits[i].collider, player, best, ref top);
            }
            int m = Physics.OverlapSphereNonAlloc(camera, ModSettings.LiftProbeRadius.Value, Overlaps, _mask, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < m; i++) best = Higher(Overlaps[i], player, best, ref top);
            return best;
        }

        static Character Higher(Collider c, Player player, Character best, ref float top)
        {
            if (c == null) return best;
            Character ch = c.GetComponentInParent<Character>();
            if (ch == null || ch == player) return best;
            float y = c.bounds.max.y;
            if (y <= top) return best;
            top = y;
            return ch;
        }
    }
}
