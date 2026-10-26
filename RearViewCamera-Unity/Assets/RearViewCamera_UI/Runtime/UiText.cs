using System.Collections.Generic;

namespace RearViewCamera.Ui
{
    /// <summary>
    /// Player-facing text of the mod, English only (user rule: no other languages). Shared by the game (registered with Jotunn in Plugin.Awake through Kydl.Common.Loc)
    /// and the Unity preview (English). Every key starts with "rearviewcamera_" so it never clashes with another mod.
    /// </summary>
    internal static class UiText
    {
        internal static readonly Dictionary<string, string> En = new Dictionary<string, string>
        {
            // the rear view has no menu (D-018) and no warning box (D-012); these are one-line game messages
            { "rearviewcamera_msg_stepdown", "Rear view: an error happened, now running with {0}." },
            { "rearviewcamera_msg_key_backup", "Rear view: {0} is also used by {1}. Using {2} this session - set a free key in the config ({3})." },
            { "rearviewcamera_msg_key_off", "Rear view: {0} is used by {1} and {2} is taken too. This rear view is off until you set a free key in the config ({3})." },
            { "rearviewcamera_msg_off", "Rear view: switched off for this session after repeated errors (see the BepInEx log)." },
        };


        /// English text (the Unity preview; the game reads the same table through Jotunn).
        internal static string English(string key)
        {
            string s;
            return En.TryGetValue(key, out s) ? s : key;
        }
    }
}
