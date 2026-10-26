namespace RearViewCamera.Game
{
    /// <summary>
    /// Which game screen is open right now, for the rear view (D-026 large map, D-036 hide-and-restore screens). Only reads static
    /// game state; returns names so the controller can log what changed.
    /// </summary>
    internal static class ScreenState
    {
        /// Single player paused (escape menu). On a server Esc does not pause, so this stays false there (D-029).
        internal static bool Paused()
        {
            return global::Game.IsPaused();   // global:: - this namespace is RearViewCamera.Game
        }

        /// The large map (not the minimap) is open.
        internal static bool MapOpen()
        {
            Minimap m = Minimap.instance;
            return m != null && m.m_mode == Minimap.MapMode.Large;
        }

        /// Name of the open screen that hides the rear view (inventory incl. crafting / containers, escape menu, teleport, loading
        /// screen, HUD hidden with Ctrl+F3 - D-029), or null when none is open. Hidden = camera released, nothing rendered.
        internal static string HidingScreen(Player player)
        {
            if (Hud.IsUserHidden()) return "HUD hidden";
            if (InventoryGui.IsVisible()) return "inventory";
            if (Menu.IsVisible()) return "menu";
            if (player != null && player.IsTeleporting()) return "teleport";
            Hud hud = Hud.instance;
            if (hud != null && hud.m_loadingScreen != null && hud.m_loadingScreen.gameObject.activeInHierarchy && hud.m_loadingScreen.alpha > 0.01f)
                return "loading screen";
            return null;
        }
    }
}
