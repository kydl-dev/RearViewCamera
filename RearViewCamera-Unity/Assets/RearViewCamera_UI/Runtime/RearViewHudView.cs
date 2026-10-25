using Kydl.Ui;
using UnityEngine;
using UnityEngine.UIElements;

namespace RearViewCamera.Ui
{
    /// <summary>
    /// WHAT the rear view looks like on screen (UI Toolkit, shared Kydl.Ui panel and colours, D-011). HUD, not a menu (D-015): not movable,
    /// never takes input (picking ignored), no input lock; full screen covers the whole panel. Compiled into the mod AND shown by the Unity
    /// preview (Assets/RearViewCamera_UI/Preview/MenuPreview.cs). No game types here; the game side is src/RearViewController.cs.
    /// All sizes passed in are PANEL units (the controller converts from screen pixels).
    /// </summary>
    internal sealed class RearViewHudView
    {
        readonly VisualElement _frame;
        readonly VisualElement _image;
        Texture _texture;
        string _state = "hidden";

        RearViewHudView(VisualElement frame, VisualElement image)
        {
            _frame = frame;
            _image = image;
        }

        internal VisualElement Element { get { return _frame; } }
        internal bool Visible { get { return _frame.style.display == DisplayStyle.Flex; } }

        /// Builds the (hidden) element. Add <see cref="Element"/> to UiKit.Root.
        internal static RearViewHudView Build()
        {
            var frame = new VisualElement { name = "RearViewCamera_Hud", pickingMode = PickingMode.Ignore };
            frame.style.position = Position.Absolute;
            frame.style.backgroundColor = UiKit.Panel;
            frame.style.display = DisplayStyle.None;
            var image = new VisualElement { name = "RearViewCamera_Image", pickingMode = PickingMode.Ignore };
            image.style.position = Position.Absolute;
            image.style.left = 0; image.style.top = 0; image.style.right = 0; image.style.bottom = 0;
            frame.Add(image);
            UiLog.Verbose("RearViewHudView: built (hidden)");
            return new RearViewHudView(frame, image);
        }

        /// The small window: rectangle in panel units, frame width in panel units.
        internal void ShowSmall(Texture texture, float left, float top, float width, float height, float border, bool mirror)
        {
            var s = _frame.style;
            s.left = left; s.top = top; s.width = width; s.height = height; s.right = StyleKeyword.Auto; s.bottom = StyleKeyword.Auto;
            SetBorder(border);
            Apply(texture, mirror, "small " + width.ToString("F0") + "x" + height.ToString("F0") + " at (" + left.ToString("F0") + ", " + top.ToString("F0") + ") panel units, border " + border.ToString("F1"));
        }

        /// The whole panel.
        internal void ShowFullScreen(Texture texture, bool mirror)
        {
            var s = _frame.style;
            s.left = 0; s.top = 0; s.right = 0; s.bottom = 0; s.width = StyleKeyword.Auto; s.height = StyleKeyword.Auto;
            SetBorder(0f);
            Apply(texture, mirror, "full screen");
        }

        internal void Hide()
        {
            if (_frame.style.display == DisplayStyle.None) return;
            _frame.style.display = DisplayStyle.None;
            _image.style.backgroundImage = StyleKeyword.None;
            _texture = null;
            Log("hidden");
        }

        void Apply(Texture texture, bool mirror, string state)
        {
            if (texture != _texture)
            {
                _image.style.backgroundImage = ToBackground(texture);
                _texture = texture;
                UiLog.Verbose("RearViewHudView: texture " + (texture != null ? texture.name + " " + texture.width + "x" + texture.height : "none"));
            }
            _image.style.scale = new Scale(new Vector3(mirror ? -1f : 1f, 1f, 1f));
            _frame.style.display = DisplayStyle.Flex;
            Log(state + (mirror ? ", mirrored" : ""));
        }

        static StyleBackground ToBackground(Texture texture)
        {
            if (texture is RenderTexture rt) return new StyleBackground(Background.FromRenderTexture(rt));
            if (texture is Texture2D t2) return new StyleBackground(t2);
            return new StyleBackground(StyleKeyword.None);
        }

        void SetBorder(float width)
        {
            var s = _frame.style;
            s.borderLeftWidth = width; s.borderRightWidth = width; s.borderTopWidth = width; s.borderBottomWidth = width;
            s.borderLeftColor = UiKit.Border; s.borderRightColor = UiKit.Border; s.borderTopColor = UiKit.Border; s.borderBottomColor = UiKit.Border;
        }

        void Log(string state)
        {
            if (state == _state) return;
            UiLog.Verbose("RearViewHudView: " + _state + " -> " + state);
            _state = state;
        }
    }
}
