using Kydl.Common;
using RearViewCamera.Logic;
using UnityEngine;

namespace RearViewCamera.Game
{
    /// <summary>
    /// Reads where the game (or another mod) has really put the status-effect icons, and the HUD canvas' draw order (D-027 / D-037):
    /// no guessed pixel offsets - the icons' own RectTransforms are measured every frame, so mods that move or resize them are followed.
    /// </summary>
    internal sealed class HudProbe
    {
        static readonly Vector3[] Corners = new Vector3[4];
        static readonly System.Collections.Generic.List<UnityEngine.UI.Graphic> Graphics = new System.Collections.Generic.List<UnityEngine.UI.Graphic>();

        Hud _hud;
        RectTransform _iconRoot;
        Canvas _canvas;

        /// Screen rectangle (pixels, top-left origin) around everything drawn for the visible status effects: the icon, its name above
        /// and its timer below (T-G006: measuring only the icon let the window cover the timer). Expected outcome: whether any is visible.
        internal bool TryStatusIcons(out HudRect rect)
        {
            rect = default(HudRect);
            if (!Resolve() || !_iconRoot.gameObject.activeInHierarchy) return rect.Width > 0f;
            Camera cam = CanvasCamera();
            float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
            int count = 0;
            for (int i = 0; i < _iconRoot.childCount; i++)
            {
                var child = _iconRoot.GetChild(i) as RectTransform;
                if (child == null || !child.gameObject.activeInHierarchy) continue;
                child.GetComponentsInChildren(false, Graphics);    // icon image, name text, timer text (reused list: no allocation)
                foreach (UnityEngine.UI.Graphic g in Graphics)
                {
                    if (!g.enabled || g.color.a <= 0.01f) continue;
                    g.rectTransform.GetWorldCorners(Corners);       // 0 bottom-left, 1 top-left, 2 top-right, 3 bottom-right
                    Vector2 tl = RectTransformUtility.WorldToScreenPoint(cam, Corners[1]);
                    Vector2 br = RectTransformUtility.WorldToScreenPoint(cam, Corners[3]);
                    minX = Mathf.Min(minX, tl.x); maxX = Mathf.Max(maxX, br.x);
                    minY = Mathf.Min(minY, Screen.height - tl.y); maxY = Mathf.Max(maxY, Screen.height - br.y);
                    count++;
                }
            }
            if (count == 0) return count > 0;
            rect = new HudRect { X = minX, Y = minY, Width = maxX - minX, Height = maxY - minY };
            return rect.Width > 0f && rect.Height > 0f;
        }

        /// The sorting order of the canvas the icons are on. Expected outcome: whether it is a Screen Space - Overlay canvas, the only
        /// kind a UI Toolkit panel can be sorted under (verified in Unity 6000.0.75f1, SortOrderProbe).
        internal bool TryHudCanvasOrder(out int order)
        {
            order = 0;
            if (!Resolve() || _canvas == null) return _canvas != null;
            Canvas root = _canvas.rootCanvas;
            order = root.sortingOrder;
            return root.renderMode == RenderMode.ScreenSpaceOverlay;
        }

        bool Resolve()
        {
            Hud hud = Hud.instance;
            if (hud == _hud && (_hud == null || _iconRoot != null)) return _iconRoot != null;
            _hud = hud;
            _iconRoot = hud != null ? hud.m_statusEffectListRoot as RectTransform : null;
            _canvas = _iconRoot != null ? _iconRoot.GetComponentInParent<Canvas>() : null;
            Canvas root = _canvas != null ? _canvas.rootCanvas : null;
            ModLog.Verbose("HudProbe: status icons root " + (_iconRoot != null ? _iconRoot.name + " (" + _iconRoot.childCount + " children)" : "not found")
                + ", canvas " + (root != null ? root.name + " " + root.renderMode + " order " + root.sortingOrder : "none"));
            return _iconRoot != null;
        }

        Camera CanvasCamera()
        {
            Canvas root = _canvas != null ? _canvas.rootCanvas : null;
            return root != null && root.renderMode != RenderMode.ScreenSpaceOverlay ? root.worldCamera : null;
        }
    }
}
