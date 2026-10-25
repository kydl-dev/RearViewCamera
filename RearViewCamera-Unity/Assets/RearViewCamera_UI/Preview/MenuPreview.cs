using Kydl.Ui;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace RearViewCamera.Ui
{
    /// <summary>
    /// Unity-only preview of the rear-view HUD (NOT compiled into the mod: only Runtime/ is). Add this component to any GameObject of a
    /// scene and press Play: the small window shows at the place it has under the game's minimap (a 1920x1080 screen, minimap edges
    /// from the fields below), in the same look as in game. Tick `fullScreen` to see the full-screen view, `mirror` to flip it.
    /// With a Main Camera in the scene the window shows what that camera sees backwards; without one, a test pattern.
    /// Set the Game view to 1920x1080 (the panel scales from that like in game).
    /// </summary>
    public sealed class MenuPreview : MonoBehaviour
    {
        const string FontFolder = "Assets/Kydl.Ui/Preview/GameAssets/";

        [SerializeField] private bool fullScreen;
        [SerializeField] private bool mirror;
        [SerializeField, Range(360, 540)] private int height = 450;
        [SerializeField] private float minimapRight = 1900f;
        [SerializeField] private float minimapBottom = 290f;
        [SerializeField] private float gap = 10f;
        [SerializeField] private float border = 2f;

        RearViewHudView _hud;
        Texture _texture;
        Camera _rearCamera;

        void OnEnable()
        {
            LoadGameFonts();
            UiKit.Reset();
            var root = UiKit.Root;
            if (root == null) { Debug.LogError("[MenuPreview] UI Toolkit panel could not be made"); return; }
            _hud = RearViewHudView.Build();
            root.Add(_hud.Element);
            _texture = MakeSource();
            Debug.Log("[MenuPreview] rear-view HUD shown (source " + _texture.name + " " + _texture.width + "x" + _texture.height + ")");
        }

        void Update()
        {
            if (_hud == null) return;
            if (fullScreen) { _hud.ShowFullScreen(_texture, mirror); return; }
            int h = Mathf.Clamp(height, 360, 540);
            float w = Mathf.Round(h * 16f / 9f);
            float toPanel = 1080f / Mathf.Max(1, Screen.height);
            float left = Mathf.Clamp(minimapRight - w, 0f, Mathf.Max(0f, Screen.width - w));
            float top = Mathf.Clamp(minimapBottom + gap, 0f, Mathf.Max(0f, Screen.height - h));
            _hud.ShowSmall(_texture, left * toPanel, top * toPanel, w * toPanel, h * toPanel, border * toPanel, mirror);
        }

        void OnDisable()
        {
            UiKit.Reset();
            _hud = null;
            if (_rearCamera != null) Destroy(_rearCamera.gameObject);
            if (_texture is RenderTexture rt) rt.Release();
        }

        /// The scene seen backwards from the Main Camera, or a checker pattern when the scene has no camera.
        Texture MakeSource()
        {
            var main = Camera.main;
            if (main != null)
            {
                var rt = new RenderTexture(800, 450, 16) { name = "Preview_RearView" };
                var go = new GameObject("Preview_RearCamera") { hideFlags = HideFlags.DontSave };
                go.transform.SetPositionAndRotation(main.transform.position, Quaternion.LookRotation(-main.transform.forward, Vector3.up));
                _rearCamera = go.AddComponent<Camera>();
                _rearCamera.fieldOfView = 75f;
                _rearCamera.targetTexture = rt;
                return rt;
            }
            var tex = new Texture2D(16, 9) { name = "Preview_Checker", filterMode = FilterMode.Point };
            for (int y = 0; y < 9; y++)
                for (int x = 0; x < 16; x++)
                    tex.SetPixel(x, y, ((x + y) & 1) == 0 ? new Color(0.25f, 0.35f, 0.25f) : new Color(0.55f, 0.65f, 0.75f));
            tex.SetPixel(0, 8, Color.red);             // top-left marker: shows whether the mirror flip works
            tex.Apply();
            return tex;
        }

        // UiKit finds the game fonts by name among the loaded fonts (Norse, Norsebold, NotoEmoji-Regular): load them first.
        static void LoadGameFonts()
        {
#if UNITY_EDITOR
            foreach (var f in new[] { "Norse.otf", "Norsebold.otf", "NotoEmoji-Regular.ttf" })
                if (AssetDatabase.LoadAssetAtPath<Font>(FontFolder + f) == null) Debug.LogWarning("[MenuPreview] font missing: " + FontFolder + f + " (Kydl > Sync shared UI kit)");
#endif
        }
    }
}
