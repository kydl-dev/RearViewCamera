using System.Text;
using Kydl.Common;
using UnityEngine;
using UnityEngine.Rendering;

namespace RearViewCamera.Rendering
{
    /// <summary>
    /// T-008 investigation (user, 2026-10-25: "investigate first"): writes, once per session, everything that makes the game's picture
    /// differ from the rear camera's - the game camera's settings and components, every other camera (sky cameras), fog, skybox and
    /// quality settings - next to the rear camera's own settings. Info lines (not debug-only), so one session log answers it.
    /// </summary>
    internal static class CameraDiagnostics
    {
        static bool _written;

        /// Expected outcome: returns whether the report has been written (once per session).
        internal static bool WriteOnce(Camera rear)
        {
            if (_written) return _written;
            Camera main = GameCamera.instance != null ? GameCamera.instance.GetComponent<Camera>() : Camera.main;
            if (main == null)
            {
                ModLog.Warn("CameraDiagnostics: no game camera found (GameCamera.instance " + (GameCamera.instance != null) + ", Camera.main null) - report not written");
                return _written;
            }
            ModLog.Info("CameraDiagnostics: game camera '" + Path(main.transform) + "' " + Describe(main));
            ModLog.Info("CameraDiagnostics: game camera components: " + Components(main.gameObject));
            ModLog.Info("CameraDiagnostics: game camera layer cull distances (non-zero): " + LayerCull(main));
            ModLog.Info("CameraDiagnostics: rear camera " + (rear != null ? Describe(rear) : "not made"));
            foreach (Camera c in Camera.allCameras)
            {
                if (c == main || c == rear) continue;
                ModLog.Info("CameraDiagnostics: other active camera '" + Path(c.transform) + "' " + Describe(c) + "; components: " + Components(c.gameObject));
            }
            var cams = Resources.FindObjectsOfTypeAll<Camera>();
            ModLog.Info("CameraDiagnostics: cameras loaded (incl. disabled): " + cams.Length + ", active: " + Camera.allCamerasCount);
            ModLog.Info("CameraDiagnostics: fog " + RenderSettings.fog + " " + RenderSettings.fogMode + ", density " + RenderSettings.fogDensity.ToString("F5")
                + ", start/end " + RenderSettings.fogStartDistance + "/" + RenderSettings.fogEndDistance + ", colour " + RenderSettings.fogColor
                + "; skybox " + (RenderSettings.skybox != null ? RenderSettings.skybox.name + " (" + RenderSettings.skybox.shader.name + ")" : "none")
                + "; ambient " + RenderSettings.ambientMode + " " + RenderSettings.ambientLight + "; sun " + (RenderSettings.sun != null ? RenderSettings.sun.name : "none"));
            ModLog.Info("CameraDiagnostics: quality '" + QualitySettings.names[QualitySettings.GetQualityLevel()] + "', lodBias " + QualitySettings.lodBias
                + ", maxLOD " + QualitySettings.maximumLODLevel + ", shadow distance " + QualitySettings.shadowDistance + " m, pixel lights " + QualitySettings.pixelLightCount
                + ", AA " + QualitySettings.antiAliasing + ", colour space " + QualitySettings.activeColorSpace + ", global max LOD " + Shader.globalMaximumLOD);
            ModLog.Info("CameraDiagnostics: grass ClutterSystem " + (ClutterSystem.instance != null ? "present on '" + ClutterSystem.instance.name + "' (enabled " + ClutterSystem.instance.enabled + ")" : "not found"));
            _written = true;
            return _written;
        }

        static string Describe(Camera c)
        {
            return "enabled " + c.enabled + ", depth " + c.depth + ", path " + c.renderingPath + " (actual " + c.actualRenderingPath + "), HDR " + c.allowHDR
                + ", MSAA " + c.allowMSAA + ", clear " + c.clearFlags + " " + c.backgroundColor + ", mask 0x" + c.cullingMask.ToString("X8")
                + ", near/far " + c.nearClipPlane + "/" + c.farClipPlane + " m, FOV " + c.fieldOfView.ToString("F1") + ", depth texture " + c.depthTextureMode
                + ", occlusion " + c.useOcclusionCulling + ", spherical cull " + c.layerCullSpherical + ", target " + (c.targetTexture != null ? c.targetTexture.name : "screen")
                + ", event mask 0x" + c.eventMask.ToString("X8") + ", opaque sort " + c.opaqueSortMode + ", command buffers " + CommandBuffers(c);
        }

        static string CommandBuffers(Camera c)
        {
            var sb = new StringBuilder();
            foreach (CameraEvent e in System.Enum.GetValues(typeof(CameraEvent)))
            {
                CommandBuffer[] bufs = c.GetCommandBuffers(e);
                if (bufs.Length == 0) continue;
                sb.Append(e).Append(':');
                foreach (CommandBuffer b in bufs) sb.Append(' ').Append(b.name);
                sb.Append("; ");
            }
            return sb.Length > 0 ? sb.ToString() : "none";
        }

        static string Components(GameObject go)
        {
            var sb = new StringBuilder();
            foreach (Component comp in go.GetComponents<Component>())
            {
                if (comp == null) { sb.Append("(missing script), "); continue; }
                sb.Append(comp.GetType().FullName);
                if (comp is Behaviour b) sb.Append(b.enabled ? "" : " [disabled]");
                sb.Append(", ");
            }
            return sb.ToString().TrimEnd(',', ' ');
        }

        static string LayerCull(Camera c)
        {
            var sb = new StringBuilder();
            float[] d = c.layerCullDistances;
            for (int i = 0; i < d.Length; i++)
                if (d[i] > 0f) sb.Append(LayerMask.LayerToName(i)).Append('(').Append(i).Append(")=").Append(d[i]).Append(' ');
            return sb.Length > 0 ? sb.ToString() : "none";
        }

        static string Path(Transform t)
        {
            string p = t.name;
            for (Transform x = t.parent; x != null; x = x.parent) p = x.name + "/" + p;
            return p;
        }
    }
}
