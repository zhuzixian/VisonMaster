using System;
using System.Globalization;
using System.Windows.Data;
using OpenCvSharp;
using OpenCvSharp.WpfExtensions;

namespace SnowyRiver.VisionMaster.Converters;

public class MatToBitmapSourceConverter:IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is Mat { IsDisposed: false } image)
        {
            return image.ToBitmapSource();
        }

        return false;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}