namespace RearViewCamera.Game
{
    /// <summary>
    /// Which game screen is open right now, for the rear view (D-026 large map, D-036 hide-and-restore screens). Only reads static
    /// game state; returns names so the controller can log what changed.
    /// </summary>
    internal static class ScreenState
    {
        /// The large map (not the minimap) is open.
        internal static bool MapOpen()
        {
            Minimap m = Minimap.instance;
            return m != null && m.m_mode == Minimap.MapMode.Large;
        }

        /// Name of the open screen that hides the rear view (inventory incl. crafting / containers, escape menu, teleport, loading
        /// screen), or null when none is open.
        internal static string HidingScreen(Player player)
        {
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
