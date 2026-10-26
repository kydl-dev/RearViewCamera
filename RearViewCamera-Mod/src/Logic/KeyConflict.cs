using System;

namespace RearViewCamera.Logic
{
    /// <summary>Result of the key check for one rear-view key (D-035 / D-038).</summary>
    internal enum KeyState
    {
        /// The configured key is free and used.
        Free,
        /// The configured key is taken; the fixed backup key (Home / End) is used this session.
        Backup,
        /// The configured key and the backup key are both taken: this rear view stays off until the player picks a free key.
        Off,
        /// The player set the key to None.
        Unbound
    }

    /// <summary>
    /// Picks the key a rear-view action really uses this session. Key codes are ints here (UnityEngine.KeyCode values) so the rule is
    /// testable without Unity; 0 = KeyCode.None. Pure logic: unit-tested.
    /// </summary>
    internal static class KeyConflict
    {
        /// <param name="configured">The player's key (0 = none).</param>
        /// <param name="backup">The fixed backup key (Home for the small camera, End for full screen).</param>
        /// <param name="taken">Whether a key is used by the game or another mod (or by this mod's other key).</param>
        /// <param name="used">The key to read this session (0 = none).</param>
        /// Expected outcome: the state, with <paramref name="used"/> set accordingly.
        internal static KeyState Resolve(int configured, int backup, Func<int, bool> taken, out int used)
        {
            if (configured == 0) { used = 0; return KeyState.Unbound; }
            if (!taken(configured)) { used = configured; return KeyState.Free; }
            if (!taken(backup)) { used = backup; return KeyState.Backup; }
            used = 0;
            return KeyState.Off;
        }
    }
}
