using KydlCore.Common;
using KydlCore.Core;

namespace RearViewCamera.Features.RearView.Infrastructure
{
    /// <summary>
    /// The rear view (small window + full screen). Gate: [General] EnableAll (one functionality). RearViewController.Tick already hides and frees everything
    /// while EnableAll is off; Stop does the same at once so nothing waits for the next frame.
    /// </summary>
    internal sealed class RearViewFeature : IFeature
    {
        internal const string FeatureId = "rear-view";
        internal static bool Running { get; private set; }
        public string Id { get { return FeatureId; } }
        public bool IsEnabled() { return ModSettings.On; }
        public bool Start() { Running = true; ModLog.Verbose("RearViewFeature: started"); return Running; }
        public bool Stop()
        {
            Running = false;
            bool released = RearViewController.Shutdown();
            ModLog.Verbose("RearViewFeature: stopped (camera released " + released + ")");
            return !Running;
        }
    }
}
