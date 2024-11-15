using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Microsoft.Win32;
using OpenCvSharp;
using Prism.Commands;
using Prism.Navigation.Regions;
using SnowyRiver.WPF.MaterialDesignInPrism.Mvvm;

namespace SnowyRiver.VisionMaster.Modules.ModuleName.ViewModels;
public class MultipleToOneViewModel(IRegionManager regionManager) : RegionViewModelBase(regionManager)
{
    public DelegateCommand AddImageCommand => new(async () => await AddImageAsync());

    private async Task AddImageAsync()
    {
        var openFileDialog = new OpenFileDialog
        {
            Filter = "Image files (*.jpg, *.jpeg, *.png, *.bmp)|*.jpg;*.jpeg;*.png;*.bmp|All files (*.*)|*.*"
        };
        if (openFileDialog.ShowDialog() == true)
        {
            var image = new Mat(openFileDialog.FileName);
            InputImages.Add(image);
        }
    }

    public DelegateCommand RemoveImageCommand => new(async () => await RemoveImageAsync());

    private async Task RemoveImageAsync()
    {
        InputImages.Clear();
    }


    public DelegateCommand ExecuteCommand => new(async () => await ExecuteAsync());

    protected virtual Task ExecuteAsync()
    {
        var outputImage = new Mat();
        OutputImage = outputImage;
        return Task.CompletedTask;
    }

    public DelegateCommand SaveCommand => new DelegateCommand(async () => await SaveAsync());

    private async Task SaveAsync()
    {
        var saveFileDialog = new SaveFileDialog
        {
            Filter = "Image files (*.jpg, *.jpeg, *.png, *.bmp)|*.jpg;*.jpeg;*.png;*.bmp|All files (*.*)|*.*"
        };
        if (saveFileDialog.ShowDialog() == true)
        {
            OutputImage.SaveImage(saveFileDialog.FileName);
        }
    }

    private ObservableCollection<Mat> _inputImages = new ();
    public ObservableCollection<Mat> InputImages
    {
        get => _inputImages;
        set => SetProperty(ref _inputImages, value);
    }

    private Mat _outputImage;
    public Mat OutputImage
    {
        get => _outputImage;
        set => SetProperty(ref _outputImage, value);
    }
}
