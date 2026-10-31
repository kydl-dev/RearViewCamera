using System.IO;
using System.Reflection;
using BepInEx;
using HarmonyLib;
using Jotunn.Utils;
using Kydl.Common;
using RearViewCamera.Ui;
using UnityEngine;

namespace RearViewCamera
{
    /// <summary>
    /// RearViewCamera: client-only rear view - a small camera window under the minimap (key N, toggle or hold) and a full-screen rear view
    /// while J is held; the character keeps facing forward. The work is in RearViewController.
    /// Entry point. Order in Awake matters: log first, then the config (player settings), then text, then everything that uses them.
    /// </summary>
    [BepInPlugin(ModGuid, ModName, ModVersion)]
    [BepInDependency(Jotunn.Main.ModGuid)]
    [BepInDependency(KydlCore.Plugin.ModGuid, "0.1.0")]     // the UI (panel, input lock, KY hub): manifest KydlDev-KydlCore-0.1.0, same version
    [NetworkCompatibility(CompatibilityLevel.NotEnforced, VersionStrictness.None)]   // client-only (D-004, D-020)
    public class Plugin : BaseUnityPlugin
    {
        // ModGuid never changes after the first release. ModVersion = manifest.json version_number = newest CHANGELOG heading (tests check it).
        public const string ModGuid = "com.kydldev.RearViewCamera";
        public const string ModName = "RearViewCamera";
        public const string ModVersion = "2.0.0";
        public const string BundleName = "rearviewcamera";

        internal static Plugin Instance;
        /// This mod's AssetBundle (null while the mod has none yet: Unity has not built one).
        internal static AssetBundle Bundle;
        ConfigReloader _cfgWatcher;

        void Awake()
        {
            Instance = this;
            ModLog.Init(Logger, ModName, () => ModSettings.DebugLog != null && ModSettings.DebugLog.Value);
            ModLog.Info(ModName + " " + ModVersion + " loaded (dll built " + File.GetLastWriteTime(Assembly.GetExecutingAssembly().Location).ToString("yyyy-MM-dd HH:mm:ss") + ")");

            // 2.0.0: KydlCore owns the UI root, the log sinks of the UI kit and the input lock - this mod sets none of them
            ModSettings.Bind(Config);
            _cfgWatcher = new ConfigReloader(Config);
            Loc.Register(UiText.En);

            Bundle = LoadBundle();
            new Harmony(ModGuid).PatchAll(Assembly.GetExecutingAssembly());
            ModLog.Verbose("Plugin: Awake finished (Harmony patches applied, small key " + ModSettings.SmallCameraKey.Value + " " + ModSettings.SmallCameraKeyMode.Value + ", full-screen key " + ModSettings.FullScreenKey.Value + ")");
        }

        // 1) embedded in the DLL (Kydl.Mod.targets embeds Assets\<bundle>; read fully into memory: LoadFromStream breaks once the stream
        //    is disposed), 2) the file next to the DLL (the Gale test profile gets both). No bundle at all is normal for a new mod.
        static AssetBundle LoadBundle()
        {
            var asm = Assembly.GetExecutingAssembly();
            foreach (var res in asm.GetManifestResourceNames())
            {
                if (res != BundleName && !res.EndsWith("." + BundleName)) continue;
                using (var stream = asm.GetManifestResourceStream(res))
                using (var ms = new MemoryStream())
                {
                    if (stream != null) stream.CopyTo(ms);
                    var b = AssetBundle.LoadFromMemory(ms.ToArray());
                    if (b != null) { ModLog.Verbose("Plugin: bundle '" + BundleName + "' loaded from the DLL (" + b.GetAllAssetNames().Length + " asset(s))"); return b; }
                    ModLog.Warn("Plugin: the embedded bundle '" + res + "' could not be loaded");
                }
            }
            var path = Path.Combine(Path.GetDirectoryName(asm.Location) ?? "", BundleName);
            if (!File.Exists(path)) { ModLog.Verbose("Plugin: no bundle '" + BundleName + "' (not embedded, not at " + path + ") - fine until the mod has assets"); return null; }
            var fromFile = AssetBundle.LoadFromFile(path);
            if (fromFile == null) ModLog.Warn("Plugin: bundle file " + path + " could not be loaded");
            else ModLog.Verbose("Plugin: bundle loaded from " + path);
            return fromFile;
        }

        void Update()
        {
            if (_cfgWatcher != null) _cfgWatcher.Tick();
            RearViewController.Tick();       // also handles EnableAll off (hides and frees everything)
        }

        void LateUpdate()
        {
            RearViewController.LateTick();
        }

        void OnDestroy()
        {
            if (_cfgWatcher != null) { _cfgWatcher.Dispose(); _cfgWatcher = null; }
            RearViewController.Shutdown();
            ModLog.Verbose("Plugin: destroyed");
        }
    }
}
