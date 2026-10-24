using System;
using Kydl.Ui;
using UnityEngine.UIElements;

namespace RearViewCamera.Ui
{
    /// <summary>
    /// WHAT the mod's menu shows (UI Toolkit, shared Kydl.Ui look). Compiled into the mod AND shown by the Unity preview
    /// (Assets/RearViewCamera_UI/Preview/MenuPreview.cs: enter Play mode in the Preview scene / any scene with that component).
    /// Keep game types (Player, ZNet, ...) OUT of this folder: the Unity project does not have them. The game side (keys, input lock,
    /// game data) is src/Ui/MainMenu.cs in the mod; pass data in through Build's parameters.
    /// </summary>
    internal static class MainMenuView
    {
        /// Builds the window. `text` turns a text key into the shown text (game: the mod's localization, Unity: English).
        internal static VisualElement Build(Func<string, string> text, Action onClose)
        {
            var w = UiKit.Window(text("rearviewcamera_menu_title"), text("rearviewcamera_menu_footer"), onClose, out UiScroll body);
            var c = body.Content;
            c.Add(UiControls.Heading(text("rearviewcamera_menu_section")));
            c.Add(UiControls.Note(text("rearviewcamera_menu_note")));
            c.Add(UiControls.Toggle(text("rearviewcamera_menu_example_switch"), true, on => UiLog.Verbose("MainMenuView: example switch " + (on ? "on" : "off"))));
            c.Add(UiControls.Row(text("rearviewcamera_menu_example_slider"), UiControls.Slider(0f, 10f, 5f, "0.0", v => UiLog.Verbose("MainMenuView: example slider " + v))));
            c.Add(UiControls.Line(UiControls.Button(text("rearviewcamera_menu_close"), onClose, true)));
            UiLog.Verbose("MainMenuView: built");
            return w;
        }
    }
}
