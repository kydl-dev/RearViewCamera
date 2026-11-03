using BepInEx.Configuration;
using KydlCore.Common;
using KydlCore.Core;
using RearViewCamera.Features.RearView.Infrastructure;
using RearViewCamera.Presentation.Hub.Registration;

namespace RearViewCamera.Bootstrap
{
    /// <summary>Composition root: the mod's KY hub page (title + settings tab) and its one feature (the rear view) in the FeatureRegistry.</summary>
    internal sealed class ModInstaller
    {
        internal static ModInstaller Current { get; private set; }
        readonly ConfigFile _config;
        internal FeatureRegistry Features { get; }
        internal ModInstaller(ConfigFile config) { _config = config; Features = new FeatureRegistry(line => ModLog.Verbose(line)); }

        internal bool Install()
        {
            Current = this;
            bool page = HubTabs.RegisterModPage(_config);
            OperationResponse<int> add = Features.Add(new RearViewFeature());
            if (!add.IsSuccess) ModLog.Error("ModInstaller: " + add.Reason);
            return page && Refresh();
        }

        internal bool Refresh()
        {
            OperationResponse<int> r = Features.Refresh();
            if (r.Status == ResponseStatus.Failed) ModLog.Warn("ModInstaller: " + r.Reason); else ModLog.Verbose("ModInstaller: refresh - " + r);
            return r.IsSuccess;
        }

        internal bool Uninstall()
        {
            OperationResponse<int> r = Features.StopAll();
            bool gone = HubTabs.UnregisterModPage();
            if (Current == this) Current = null;
            return r.IsSuccess && gone;
        }
    }
}
