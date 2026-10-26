using System;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using Kydl.Common;
using RearViewCamera.Logic;
using UnityEngine;

namespace RearViewCamera.Game
{
    /// <summary>
    /// R12 / D-035 / D-038: the keys the rear view really uses this session. Once per game session (when the player first exists) and
    /// again whenever the player changes a rear-view key, every key bound by the game (ZInput) and by every other loaded mod's config is
    /// collected; a configured key that is taken is replaced by the fixed backup key (Home = small camera, End = full screen) for the
    /// session, with a message on screen; if the backup is taken too, that rear view stays off until the player picks a free key.
    /// </summary>
    internal sealed class KeyBindings
    {
        const KeyCode SmallBackup = KeyCode.Home;
        const KeyCode FullBackup = KeyCode.End;

        string _checkedFor;

        internal KeyCode SmallKey { get; private set; } = KeyCode.None;
        internal KeyCode FullKey { get; private set; } = KeyCode.None;
        internal KeyState SmallState { get; private set; } = KeyState.Unbound;
        internal KeyState FullState { get; private set; } = KeyState.Unbound;

        /// Expected outcome: returns whether the keys were (re)checked in this call.
        internal bool CheckIfNeeded()
        {
            KeyCode small = ModSettings.SmallCameraKey.Value, full = ModSettings.FullScreenKey.Value;
            string sig = small + "/" + full;
            if (sig == _checkedFor) return sig != _checkedFor;
            _checkedFor = sig;
            Dictionary<KeyCode, string> owners = Collect(new[] { small, full, SmallBackup, FullBackup });

            SmallState = KeyConflict.Resolve((int)small, (int)SmallBackup, k => owners.ContainsKey((KeyCode)k) || (k != 0 && k == (int)full), out int s);
            SmallKey = (KeyCode)s;
            FullState = KeyConflict.Resolve((int)full, (int)FullBackup, k => owners.ContainsKey((KeyCode)k) || (k != 0 && k == s), out int f);
            FullKey = (KeyCode)f;

            ModLog.Info("KeyBindings: small camera " + small + " -> " + SmallKey + " (" + SmallState + "), full screen " + full + " -> " + FullKey + " (" + FullState + ")"
                + "; checked against " + owners.Count + " taken key(s) of interest");
            Tell("SmallCameraKey", small, SmallBackup, SmallState, Owner(owners, small, full));
            Tell("FullScreenKey", full, FullBackup, FullState, Owner(owners, full, SmallKey));
            return sig == _checkedFor;
        }

        static string Owner(Dictionary<KeyCode, string> owners, KeyCode key, KeyCode ourOther)
        {
            if (owners.TryGetValue(key, out string who)) return who;
            return key == ourOther ? "RearViewCamera's other key" : "?";
        }

        static void Tell(string entry, KeyCode configured, KeyCode backup, KeyState state, string owner)
        {
            string msg = null;
            if (state == KeyState.Backup)
                msg = Loc.T("rearviewcamera_msg_key_backup", configured.ToString(), owner, backup.ToString(), entry);
            else if (state == KeyState.Off)
                msg = Loc.T("rearviewcamera_msg_key_off", configured.ToString(), owner, backup.ToString(), entry);
            if (msg == null) return;
            ModLog.Warn("KeyBindings: " + msg);
            if (MessageHud.instance != null) MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, msg);
        }

        /// Who uses each of the given keys: other mods' KeyCode / KeyboardShortcut (without modifiers) config entries and the game's
        /// keyboard bindings. Expected outcome: a map key -> "who" for the taken ones only.
        static Dictionary<KeyCode, string> Collect(KeyCode[] keys)
        {
            var wanted = new HashSet<KeyCode>(keys);
            wanted.Remove(KeyCode.None);
            var owners = new Dictionary<KeyCode, string>();
            int entries = 0;
            foreach (PluginInfo info in Chainloader.PluginInfos.Values)
            {
                if (info == null || info.Instance == null || info.Metadata.GUID == Plugin.ModGuid) continue;
                ConfigFile cfg;
                try { cfg = info.Instance.Config; }
                catch (NullReferenceException) { continue; }
                if (cfg == null) continue;
                foreach (KeyValuePair<ConfigDefinition, ConfigEntryBase> kv in cfg)
                {
                    KeyCode k = KeyCode.None;
                    object v = kv.Value.BoxedValue;
                    if (v is KeyCode kc) k = kc;
                    else if (v is KeyboardShortcut ks && !HasModifiers(ks)) k = ks.MainKey;
                    if (k == KeyCode.None) continue;
                    entries++;
                    if (wanted.Contains(k) && !owners.ContainsKey(k)) owners[k] = info.Metadata.Name + " [" + kv.Key.Section + "] " + kv.Key.Key;
                }
            }
            int game = GameBindings(wanted, owners);
            ModLog.Verbose("KeyBindings: " + entries + " key entries in " + (Chainloader.PluginInfos.Count - 1) + " other plugin(s), " + game + " game binding(s) checked");
            return owners;
        }

        static bool HasModifiers(KeyboardShortcut ks)
        {
            foreach (KeyCode m in ks.Modifiers) return m != KeyCode.None;
            return false;
        }

        static readonly System.Reflection.FieldInfo ButtonsField = HarmonyLib.AccessTools.Field(typeof(ZInput), "m_buttons");
        static readonly System.Reflection.MethodInfo KeyToPath = HarmonyLib.AccessTools.Method(typeof(ZInput), "KeyCodeToPath", new[] { typeof(KeyCode), typeof(bool) });

        /// The game's own keyboard bindings (ZInput.m_buttons, Unity Input System paths; both are private in the game, read by
        /// reflection). Expected outcome: number of bindings looked at (0 when the game changed and they cannot be read - logged).
        static int GameBindings(HashSet<KeyCode> wanted, Dictionary<KeyCode, string> owners)
        {
            ZInput z = ZInput.instance;
            if (z == null || ButtonsField == null || KeyToPath == null)
            {
                ModLog.Warn("KeyBindings: game bindings not checked (ZInput " + (z != null) + ", m_buttons " + (ButtonsField != null) + ", KeyCodeToPath " + (KeyToPath != null) + ")");
                return 0;
            }
            var paths = new Dictionary<string, KeyCode>(StringComparer.OrdinalIgnoreCase);
            foreach (KeyCode k in wanted)
            {
                string p = KeyToPath.Invoke(null, new object[] { k, false }) as string;
                if (!string.IsNullOrEmpty(p)) paths[p] = k;
            }
            if (!(ButtonsField.GetValue(z) is System.Collections.IDictionary buttons)) return 0;
            int n = 0;
            foreach (System.Collections.DictionaryEntry e in buttons)
            {
                var def = e.Value as ZInput.ButtonDef;
                var action = def != null ? def.ButtonAction : null;
                if (action == null) continue;
                foreach (UnityEngine.InputSystem.InputBinding b in action.bindings)
                {
                    n++;
                    string p = b.effectivePath;
                    if (string.IsNullOrEmpty(p) || !paths.TryGetValue(p, out KeyCode k) || owners.ContainsKey(k)) continue;
                    owners[k] = "Valheim (" + e.Key + ")";
                }
            }
            ModLog.Verbose("KeyBindings: key paths checked: " + string.Join(", ", new List<string>(paths.Keys).ToArray()));
            return n;
        }
    }
}
