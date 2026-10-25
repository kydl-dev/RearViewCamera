using System;
using System.Reflection;
using BepInEx.Bootstrap;
using Kydl.Common;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RearViewCamera.Game
{
    /// <summary>Why the rear-view keys are ignored right now (D-012).</summary>
    internal enum BlockReason
    {
        None,
        Chat,
        Console,
        ConfigurationManager,
        TextField
    }

    /// <summary>
    /// Finds out whether the keyboard belongs to a text-entry window, so typing an N or J never switches the rear view (ported from the
    /// old RVC, D-012: no warning box). Chat and console through the game API; Configuration Manager through reflection, because it is
    /// another mod that may or may not be installed; any focused uGUI / TextMeshPro input field (signs, other mods' text boxes).
    /// </summary>
    internal sealed class RearViewInputBlocker
    {
        const BindingFlags InstanceAny = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        static readonly string[] ConfigWindowMembers = { "DisplayingWindow", "IsWindowFullscreen", "IsWindowVisible" };

        object _configManager;
        PropertyInfo _configWindowProperty;
        bool _configManagerResolved;

        /// Expected outcome: returns the reason the keys are blocked now (None = free).
        internal BlockReason Current()
        {
            if (Chat.instance != null && Chat.instance.HasFocus()) return BlockReason.Chat;
            if (global::Console.IsVisible()) return BlockReason.Console;
            if (IsConfigManagerOpen()) return BlockReason.ConfigurationManager;
            if (IsTextFieldFocused()) return BlockReason.TextField;
            return BlockReason.None;
        }

        /// Keys that OPEN chat (Enter), the console (F5, /) or Configuration Manager (F2): on that frame the window does not report
        /// focus yet, so a binding on the same key would otherwise switch the rear view. Expected outcome: whether one went down this frame.
        internal static bool UiOpeningKeyPressed()
        {
            return Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.F5)
                || Input.GetKeyDown(KeyCode.Slash) || Input.GetKeyDown(KeyCode.F2);
        }

        bool IsConfigManagerOpen()
        {
            ResolveConfigManager();
            if (_configManager == null || _configWindowProperty == null) return _configManager != null && _configWindowProperty != null;
            try
            {
                object value = _configWindowProperty.GetValue(_configManager, null);
                return value is bool open && open;
            }
            catch (TargetInvocationException e)
            {
                ModLog.Warn("InputBlocker: reading " + _configManager.GetType().FullName + "." + _configWindowProperty.Name + " failed, no longer checked: " + e.InnerException?.Message);
                _configWindowProperty = null;
                return _configWindowProperty != null;
            }
        }

        /// Once per session: the first loaded plugin whose type name contains "ConfigurationManager" and that has a bool window property.
        void ResolveConfigManager()
        {
            if (_configManagerResolved) return;
            _configManagerResolved = true;
            foreach (var info in Chainloader.PluginInfos.Values)
            {
                if (info == null || info.Instance == null) continue;
                Type type = info.Instance.GetType();
                string name = type.FullName ?? type.Name;
                if (name.IndexOf("ConfigurationManager", StringComparison.OrdinalIgnoreCase) < 0) continue;
                foreach (string member in ConfigWindowMembers)
                {
                    PropertyInfo p = type.GetProperty(member, InstanceAny);
                    if (p == null || p.PropertyType != typeof(bool) || p.GetIndexParameters().Length != 0) continue;
                    _configManager = info.Instance;
                    _configWindowProperty = p;
                    ModLog.Verbose("InputBlocker: Configuration Manager found (" + name + "." + member + ", plugin " + info.Metadata.GUID + ")");
                    return;
                }
                ModLog.Verbose("InputBlocker: " + name + " looks like a configuration manager but has none of " + string.Join("/", ConfigWindowMembers));
            }
            ModLog.Verbose("InputBlocker: no Configuration Manager loaded (" + Chainloader.PluginInfos.Count + " plugins checked)");
        }

        static bool IsTextFieldFocused()
        {
            EventSystem es = EventSystem.current;
            GameObject selected = es != null ? es.currentSelectedGameObject : null;
            bool focused = false;
            if (selected != null && selected.TryGetComponent(out InputField field)) focused = field.isFocused;
            else if (selected != null && selected.TryGetComponent(out TMPro.TMP_InputField tmp)) focused = tmp.isFocused;
            return focused;
        }
    }
}
