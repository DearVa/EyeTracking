using System.Collections.ObjectModel;
using System.Web;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EyeTracking.Desktop.Extensions;
using EyeTracking.Extensions;
using OpenCvSharp;
using Point = OpenCvSharp.Point;
using Window = Avalonia.Controls.Window;

namespace EyeTracking.Desktop.ViewModels;

public partial class EyeTrackViewModel(Window window) : ObservableObject
{
    private string? FilePath
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
            else Initialize(value);
        }
    }

    private string? filePath;

    [ObservableProperty] private EyeTrackContext?    tracker;
    [ObservableProperty] private EyeDetectParameters parameters = new();
    [ObservableProperty] private VideoDecoder?       decoder;
    [ObservableProperty] private IEnumerator<Mat>?   enumerator;
    [ObservableProperty] private Mat?                source;
    [ObservableProperty] private WriteableBitmap?    bitmap;
    [ObservableProperty] private WriteableBitmap?    detected;

    public ObservableCollection<TrackDebugViewModel> Debugs { get; }= [];

    private void Initialize(string file)
    {
        Tracker = new EyeTrackContext
        {
            Parameters = Parameters
        };
        Tracker.OnDebug += args =>
        {
            switch (args[0])
            {
                case "输出":
                    Detected?.Dispose();
                    Detected = args[1].AsNotNull<Mat>().ToWriteableBitmap();
                    return;
                case "候选" :
                    Dispatcher.UIThread.Invoke(() =>
                    {
                        Debugs.Add(new(
                            args[1].AsNotNull<Mat>().Clone(),
                            $"X:{args[3].AsNotNull<Point>().X}, Y:{args[3].AsNotNull<Point>().Y}",
                            args[2] is true ? Brushes.CornflowerBlue : Brushes.Red,
                            Tracker.Parameters));
                    });
                    return;
            }
        };
        Decoder    = new VideoDecoder(file);
        Enumerator = Decoder.Decode().GetEnumerator();
    }

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
        foreach (var debug in Debugs) debug.Dispose();
        Debugs.Clear();
        Tracker?.DetectLights(mat, out _, out _);
    }
    
}