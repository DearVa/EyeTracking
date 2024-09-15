using System.Collections.ObjectModel;
using System.Web;
using Antelcat.AutoGen.ComponentModel;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EyeTracking.Desktop.Extensions;
using EyeTracking.Extensions;
using Microsoft.Extensions.DependencyInjection;
using OpenCvSharp;
using Point = OpenCvSharp.Point;
using Window = Avalonia.Controls.Window;

namespace EyeTracking.Desktop.ViewModels;

[AutoKeyAccessor]
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

    [ObservableProperty] 
  
    private EyeTrackContext? tracker;
    
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanNext))]
    [NotifyPropertyChangedFor(nameof(CanPlay))]
    [NotifyPropertyChangedFor(nameof(CanPlay))]
    [NotifyPropertyChangedFor(nameof(PlayVisible))]
    [NotifyPropertyChangedFor(nameof(StopVisible))]
    private IEnumerator<Mat>?   enumerator;
    
    [ObservableProperty] private EyeDetectParameters parameters = new()
    {
    };
    [ObservableProperty] private VideoDecoder?       decoder;
    [ObservableProperty] private Mat?                source;
    [ObservableProperty] private WriteableBitmap?    bitmap;
    [ObservableProperty] private WriteableBitmap?    output;
    [ObservableProperty] private WriteableBitmap?    subtraction;
    [ObservableProperty] private WriteableBitmap?    binSubtraction;
    [ObservableProperty] private WriteableBitmap?    origin;

    [NotifyPropertyChangedFor(nameof(CanNext))]
    [NotifyPropertyChangedFor(nameof(PlayVisible))]
    [NotifyPropertyChangedFor(nameof(StopVisible))]
    [ObservableProperty] private bool autoPlay;
    
    public bool CanNext => CanPlay && !AutoPlay;
    public bool CanPlay => Enumerator is not null;

    public bool PlayVisible => CanPlay && !AutoPlay;
    public bool StopVisible => CanPlay && AutoPlay;
    
    public ObservableCollection<TrackDebugViewModel> Debugs  { get; }= [];

    private void Initialize(string file)
    {
        if (Tracker == null)
        {
            Tracker            = this.ServiceProvider().GetRequiredService<EyeTrackContext>();
            Tracker.Parameters = Parameters;
            Tracker.OnDebug += (hint, args) =>
            {
                var mat = args[0].AsNotNull<Mat>();
                switch (hint)
                {
                    case EyeTrackContext.DebugHint.Origin:
                        Origin?.Dispose();
                        Origin = mat.ToWriteableBitmap();
                        return;
                    case EyeTrackContext.DebugHint.Bin_Subtraction:
                        BinSubtraction?.Dispose();
                        BinSubtraction = mat.ToWriteableBitmap();
                        return;
                    case EyeTrackContext.DebugHint.Subtraction:
                        Subtraction?.Dispose();
                        Subtraction = mat.ToWriteableBitmap();
                        return;
                    case EyeTrackContext.DebugHint.Output:
                        Output?.Dispose();
                        Output = mat.ToWriteableBitmap();
                        return;
                    case EyeTrackContext.DebugHint.Candidate:
                        Dispatcher.UIThread.Invoke(() =>
                        {
                            Debugs.Add(new(
                                mat,
                                $"X:{args[2].AsNotNull<Point>().X}, Y:{args[2].AsNotNull<Point>().Y}",
                                args[1] is true ? Brushes.CornflowerBlue : Brushes.Red,
                                Tracker.Parameters with { }));
                        });
                        return;
                }
            };
        }
        Decoder?.Dispose();
        Decoder    = new VideoDecoder(file);
        Enumerator?.Dispose();
        Enumerator = Decoder.Decode().GetEnumerator();
    }

    [RelayCommand]
    private async Task Select()
    {
        var result = await window.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            SuggestedStartLocation = await window.StorageProvider.TryGetFolderFromPathAsync(
                FilePath is null ? @"F:\Shared\Files\视线追踪项目材料\明暗瞳视线追踪技术材料\开发板资料\视频1" : ((FilePath)FilePath).DirectoryName!),
            AllowMultiple     = false
        });
        foreach (var file in result)
        {
            FilePath = HttpUtility.UrlDecode(file.Path.AbsolutePath);
            break;
        }
    }

    [RelayCommand]
    private async Task Start()
    {
        if (AutoPlay) return;
        AutoPlay = true;
        while (Enumerator is not null && AutoPlay)
        {
            Next();
            await Task.Delay(20);
        }
    }

    [RelayCommand]
    private void Stop() => AutoPlay = false;
    
    [RelayCommand]
    private void Next()
    {
        if (Enumerator is null) return;
        if (!Enumerator.MoveNext())
        {
            Enumerator.Dispose();
            Enumerator = null;
            Decoder?.Dispose();
            Decoder  = null;
            AutoPlay = false;
            return;
        }
        var mat = Source = Enumerator.Current;
        foreach (var debug in Debugs) debug.Dispose();
        Debugs.Clear();
        Tracker?.DetectLights(mat, out _, out _);
    }
}