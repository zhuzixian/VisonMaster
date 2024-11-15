using System;
using System.Threading.Tasks;
using Prism.Commands;
using Prism.Navigation.Regions;
using SnowyRiver.VisionMaster.Core;
using SnowyRiver.WPF.MaterialDesignInPrism.Mvvm;

namespace SnowyRiver.VisionMaster.ViewModels;
public class MainWindowViewModel(IRegionManager regionManager) : RegionViewModelBase(regionManager)
{
    private string _title = "Vision Master";
    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }

    public DelegateCommand<string> NavigateCommand => new(async (target) => await NavigateAsync(target));

    private async Task NavigateAsync(string target)
    {
        RegionManager.RequestNavigate(RegionNames.ContentRegion, target);
        await Task.CompletedTask;
    }
}
