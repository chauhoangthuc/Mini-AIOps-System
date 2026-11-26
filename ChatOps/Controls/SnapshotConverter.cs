using System.Globalization;
using System.IO;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace ChatOps.Controls;

public sealed class SnapshotConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if(value is not string encoded || encoded.Length is 0 or > 8000000)return null;
        try
        {
            using var data=new MemoryStream(System.Convert.FromBase64String(encoded));
            var image=new BitmapImage();
            image.BeginInit();
            image.CacheOption=BitmapCacheOption.OnLoad;
            image.DecodePixelWidth=960;
            image.StreamSource=data;
            image.EndInit();
            image.Freeze();
            return image;
        }
        catch(Exception e) when(e is FormatException or IOException or NotSupportedException or ArgumentException)
        { return null; }
    }
    public object ConvertBack(object value,Type type,object parameter,CultureInfo culture)=>throw new NotSupportedException();
}
