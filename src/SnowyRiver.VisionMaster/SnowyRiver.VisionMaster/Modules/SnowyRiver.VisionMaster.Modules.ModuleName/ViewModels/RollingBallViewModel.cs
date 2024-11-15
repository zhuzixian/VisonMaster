using System.Threading.Tasks;
using OpenCvSharp;
using Prism.Navigation.Regions;
using SnowyRiver.VisionMaster.Services;

namespace SnowyRiver.VisionMaster.Modules.ModuleName.ViewModels;
public class RollingBallViewModel(IRegionManager regionManager) : MultipleToOneViewModel(regionManager)
{
    protected override Task ExecuteAsync()
    {
        var rollingBall = new RollingBall(101);
        var sourceImage = InputImages[0].Depth() == 3
            ?  InputImages[0].CvtColor(ColorConversionCodes.BGR2GRAY)
            : InputImages[0].Clone();
        rollingBall.SubtractBackgroundRollingBall(sourceImage, out var outputImage, out _, false, false, true);
        OutputImage = outputImage;
        return Task.CompletedTask;
    }
}
