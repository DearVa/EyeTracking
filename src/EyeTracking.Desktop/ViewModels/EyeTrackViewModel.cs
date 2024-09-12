using System.Collections.ObjectModel;
using System.Web;
using Avalonia;
using Avalonia.Controls.Chrome;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OpenCvSharp;
using Point = OpenCvSharp.Point;
using Window = Avalonia.Controls.Window;

namespace EyeTracking.Desktop.ViewModels;

public partial class EyeTrackViewModel(Window window) : ObservableObject
{
    public string? FilePath
    {
        get => filePath;
        set
        {
            SetProperty(ref filePath, value);
            if (value == null)
            {
                Tracker = null;
                Decoder?.Dispose();
                Decoder = null;
            }
            else
            {
                Tracker = new EyeTrackContext
                {
                    Parameters = Parameters
                };
                Tracker.OnDebug += (title, mat, additional) =>
                {
                    switch (title)
                    {
                        case "输出":
                            Detected?.Dispose();
                            Detected = mat.ToWriteableBitmap();
                            return;
                        case "候选" :
                            Dispatcher.UIThread.Invoke(() =>
                            {
                                Debugs.Add(new(mat.ToWriteableBitmap(),(Point)additional![1], additional[0] is true ? Brushes.Cyan : Brushes.Red));
                            });
                            return;
                    }
                };
                Decoder         =  new VideoDecoder(value);
                Enumerator      =  Decoder.Decode().GetEnumerator();
            }
        }
    }

    private string? filePath;

    [ObservableProperty] private EyeTrackContext? tracker;

    [ObservableProperty] private EyeDetectParameters parameters = new();

    [ObservableProperty] private VideoDecoder? decoder;

    [ObservableProperty] private IEnumerator<Mat>? enumerator;

    [ObservableProperty] private Mat? source;

    [ObservableProperty] private WriteableBitmap? bitmap;

    [ObservableProperty] private WriteableBitmap? detected;

    public ObservableCollection<DebugPack> Debugs { get; }= [];

    public record DebugPack(WriteableBitmap Bitmap, Point Point, IImmutableSolidColorBrush Brush);
    
    [RelayCommand]
    private async Task Select()
    {
        
        var result = await window.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            SuggestedStartLocation = await window.StorageProvider.TryGetFolderFromPathAsync(FilePath ?? @"F:\Shared\Files\视线追踪项目材料\明暗瞳视线追踪技术材料\开发板资料\视频1"),
            AllowMultiple     = false
        });
        foreach (var file in result)
        {
            FilePath = HttpUtility.UrlDecode(file.Path.AbsolutePath);
            break;
        }
    }

    [RelayCommand]
    private void Next()
    {
        if (Enumerator is null) return;
        if (!Enumerator.MoveNext())
        {
            Enumerator = null;
            return;
        }
        var mat = Source = Enumerator.Current;
        foreach (var debug in Debugs) debug.Bitmap.Dispose();
        Debugs.Clear();
        Tracker?.DetectLights(mat, out _, out _);
        Bitmap = mat.ToWriteableBitmap();
    }
    
}

file static class BitmapExtension
{
    public static WriteableBitmap ToWriteableBitmap(this Mat mat)
    {
        using var tmp = mat.CvtColor(ColorConversionCodes.GRAY2RGBA);
        return new WriteableBitmap(PixelFormat.Rgb32, AlphaFormat.Opaque, tmp.DataStart,
            new PixelSize(mat.Width, mat.Height), new Vector(96, 96), mat.Width * 4);
    }
}