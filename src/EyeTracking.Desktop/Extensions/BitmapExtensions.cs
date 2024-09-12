using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using OpenCvSharp;

namespace EyeTracking.Desktop.Extensions;

internal static class BitmapExtensions
{
    public static WriteableBitmap ToWriteableBitmap(this Mat mat)
    {
        using var tmp = mat.CvtColor(ColorConversionCodes.GRAY2RGBA);
        return new WriteableBitmap(PixelFormat.Rgb32, AlphaFormat.Opaque, tmp.DataStart,
            new PixelSize(mat.Width, mat.Height), new Vector(96, 96), mat.Width * 4);
    }
}