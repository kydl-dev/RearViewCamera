using Kydl.Ui;
using UnityEngine;
using UnityEngine.UI;

namespace RearViewCamera.Ui
{
    /// <summary>
    /// Unity-only experiment for R3 "behind the status effects" (D-037): does a uGUI Screen Space - Overlay canvas (like Valheim's HUD)
    /// draw over or under the Kydl UI Toolkit panel, and does PanelSettings.sortingOrder change that? Shows a red square (uGUI) on the
    /// top-left part of the rear-view window. Not compiled into the mod.
    /// </summary>
    public sealed class SortOrderProbe : MonoBehaviour
    {
        [SerializeField] private int canvasSortingOrder = 0;
        [SerializeField] private int panelSortingOrder = 500;
        Canvas _canvas;

        void Start()
        {
            var go = new GameObject("Probe_Canvas");
            _canvas = go.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var img = new GameObject("Probe_Red").AddComponent<Image>();
            img.transform.SetParent(go.transform, false);
            img.color = new Color(1f, 0f, 0f, 1f);
            var rt = img.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 1f);
            rt.sizeDelta = new Vector2(Screen.width * 0.35f, Screen.height * 0.35f);
            rt.anchoredPosition = new Vector2(-Screen.width * 0.25f, -Screen.height * 0.1f);
        }

        void Update()
        {
            if (_canvas != null) _canvas.sortingOrder = canvasSortingOrder;
            if (UiKit.Settings != null && UiKit.Settings.sortingOrder != panelSortingOrder)
            {
                UiKit.Settings.sortingOrder = panelSortingOrder;
                Debug.Log("[SortOrderProbe] panel sortingOrder " + panelSortingOrder + ", canvas sortingOrder " + canvasSortingOrder);
            }
        }
    }
}
