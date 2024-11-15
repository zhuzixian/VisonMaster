using System.Threading.Tasks;
using OpenCvSharp;
using Prism.Navigation.Regions;

namespace SnowyRiver.VisionMaster.Modules.ModuleName.ViewModels;
public class AbsdiffViewModel(IRegionManager regionManager) : MultipleToOneViewModel(regionManager)
{
    protected override Task ExecuteAsync()
    {
        var outputImage = new Mat();
        Cv2.Absdiff(InputImages[0], InputImages[1], outputImage);
        OutputImage = outputImage;
        return Task.CompletedTask;
    }
}
