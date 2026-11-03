using BepInEx.Configuration;
using Kydl.Common;

namespace RearViewCamera.Presentation.Hub.Registration
{
    /// <summary>What RearViewCamera declares to the KY hub (F6, MIGRATION_DECISIONS B11): its title and its settings tab.</summary>
    internal static class HubTabs
    {
        internal const string SettingsId = "settings";

        internal static bool RegisterModPage(ConfigFile config)
        {
            global::KydlCore.KyHub.SetModTitle(Plugin.ModGuid, "Rear View Camera");
            var tab = global::KydlCore.KyHub.AddTab(Plugin.ModGuid, SettingsId, "Settings", () => global::KydlCore.KyHub.ConfigEntries(config), 0);
            ModLog.Verbose("HubTabs: settings tab " + (tab != null ? "registered" : "FAILED"));
            return tab != null;
        }

        internal static bool UnregisterModPage()
        {
            global::KydlCore.KyHub.RemoveTab(Plugin.ModGuid, SettingsId);
            return !global::KydlCore.KyHub.HasTab(Plugin.ModGuid, SettingsId);
        }
    }
}
