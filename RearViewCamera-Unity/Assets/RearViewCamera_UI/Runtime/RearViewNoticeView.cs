using Kydl.Ui;
using UnityEngine;
using UnityEngine.UIElements;

namespace RearViewCamera.Ui
{
    /// <summary>
    /// A short note on the left side of the screen (user, 2026-10-26: "every time the key is pressed and is not working, show a message
    /// on the left side, active for 5 seconds"). Shared Kydl look, never takes input. No game types: the mod shows / hides it.
    /// </summary>
    internal sealed class RearViewNoticeView
    {
        readonly VisualElement _box;
        readonly Label _text;

        RearViewNoticeView(VisualElement box, Label text)
        {
            _box = box;
            _text = text;
        }

        internal VisualElement Element { get { return _box; } }

        internal static RearViewNoticeView Build()
        {
            var box = new VisualElement { name = "RearViewCamera_Notice", pickingMode = PickingMode.Ignore };
            box.style.position = Position.Absolute;
            box.style.left = Length.Percent(2);
            box.style.top = Length.Percent(40);
            box.style.maxWidth = Length.Percent(30);
            box.style.backgroundColor = UiKit.Panel;
            box.style.paddingLeft = 14; box.style.paddingRight = 14; box.style.paddingTop = 10; box.style.paddingBottom = 10;
            box.style.borderLeftWidth = 3; box.style.borderLeftColor = UiKit.Gold;
            box.style.display = DisplayStyle.None;
            var text = new Label { pickingMode = PickingMode.Ignore };
            text.style.color = UiKit.Text;
            text.style.whiteSpace = WhiteSpace.Normal;
            box.Add(text);
            UiLog.Verbose("RearViewNoticeView: built (hidden)");
            return new RearViewNoticeView(box, text);
        }

        internal void Show(string message)
        {
            _text.text = message;
            _box.style.display = DisplayStyle.Flex;
            UiLog.Verbose("RearViewNoticeView: shown '" + message + "'");
        }

        internal void Hide()
        {
            if (_box.style.display == DisplayStyle.None) return;
            _box.style.display = DisplayStyle.None;
            UiLog.Verbose("RearViewNoticeView: hidden");
        }
    }
}
