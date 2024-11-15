using Prism.Ioc;
using Prism.Modularity;
using Prism.Navigation.Regions;
using SnowyRiver.VisionMaster.Core;
using SnowyRiver.VisionMaster.Modules.ModuleName.ViewModels;
using SnowyRiver.VisionMaster.Modules.ModuleName.Views;

namespace SnowyRiver.VisionMaster.Modules.ModuleName;

public class ModuleNameModule(IRegionManager regionManager) : IModule
{
    public void OnInitialized(IContainerProvider containerProvider)
    {
        regionManager.RequestNavigate(RegionNames.ContentRegion, ViewNames.AbsdiffView);
    }

    public void RegisterTypes(IContainerRegistry containerRegistry)
    {
        containerRegistry.RegisterForNavigation<SubtractView>(ViewNames.SubtractView);
        containerRegistry.RegisterForNavigation<AbsdiffView>(ViewNames.AbsdiffView);
        containerRegistry.RegisterForNavigation<RollingBallView>(ViewNames.RollingBallView);
    }
}