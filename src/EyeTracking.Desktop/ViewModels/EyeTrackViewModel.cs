using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
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
using EyeTracking.Windows.Capture;
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

    private readonly UsbKCapture capture = new();

    [ObservableProperty] private EyeTrackContext? tracker;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanNext))]
    [NotifyPropertyChangedFor(nameof(CanPlay))]
    [NotifyPropertyChangedFor(nameof(CanPlay))]
    [NotifyPropertyChangedFor(nameof(PlayVisible))]
    [NotifyPropertyChangedFor(nameof(StopVisible))]
    private IEnumerator<Mat>? enumerator;

    [ObservableProperty] private EyeDetectParameters parameters = new();
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

    [ObservableProperty] private bool     capturing;
    [ObservableProperty] private int fps;
    
    public bool CanNext => CanPlay && !AutoPlay;
    public bool CanPlay => Enumerator is not null;

    public bool PlayVisible => CanPlay && !AutoPlay;
    public bool StopVisible => CanPlay && AutoPlay;
    
    public ObservableCollection<TrackDebugViewModel> Debugs  { get; }= [];

    private bool accepting;

    private void SetMat(ref WriteableBitmap? field, string propName, Mat mat)
    {
        if (field != null)
        {
            var tmp = field;
            field = null;
            OnPropertyChanged(propName);
            tmp.Dispose();
        }
        field = mat.ToWriteableBitmap();
        OnPropertyChanged(propName);
    }
    
    private void ResetTracker()
    {
        if (Tracker != null) return;
        Tracker            = this.ServiceProvider().GetRequiredService<EyeTrackContext>();
        Tracker.Parameters = Parameters;
        Tracker.OnDebug += (hint, args) =>
        {
            var mat = args[0].AsNotNull<Mat>();
            switch (hint)
            {
                case EyeTrackContext.DebugHint.Origin:
                    SetMat(ref origin, nameof(Origin), mat);
                    return;
                case EyeTrackContext.DebugHint.Bin_Subtraction:
                    SetMat(ref binSubtraction, nameof(BinSubtraction), mat);
                    return;
                case EyeTrackContext.DebugHint.Subtraction:
                    SetMat(ref subtraction, nameof(Subtraction), mat);
                    return;
                case EyeTrackContext.DebugHint.Output:
                    SetMat(ref output, nameof(Output), mat);
                    return;
                case EyeTrackContext.DebugHint.Candidate:
                    return;
                    var clone = mat.Clone();
                    Dispatcher.UIThread.Invoke(() =>
                    {
                        Debugs.Add(new(
                            clone,
                            $"X:{args[2].AsNotNull<Point>().X}, Y:{args[2].AsNotNull<Point>().Y}",
                            args[1] is true ? Brushes.CornflowerBlue : Brushes.Red,
                            Tracker.Parameters with { }));
                    });
                    return;
            }
        };
    }
    private void Initialize(string file)
    {
        ResetTracker();
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
        Detect(Source = Enumerator.Current);
    }

    private void Detect(Mat mat)
    {
        var items = Debugs.ToArray();
        Debugs.Clear();
        foreach (var debug in items) debug.Dispose();
        Tracker?.DetectLights(mat, out _, out _);
    }

    [RelayCommand]
    private void StartCapture()
    {
        if (UsbKCapture.EnumUsbDevices(out var names) <= 0)
        {
            MessageBox.Show("未检测到驱动");
            return;
        }
        if (!capture.OpenDevice(0))
        {
            MessageBox.Show("设备 0 启动失败");
            return;
        }
        ResetTracker();
        unsafe
        {
            Capturing = true;
            var watch    = new Stopwatch();
            watch.Start();
            var lastTick = watch.ElapsedMilliseconds;
            capture.Start((buffer, length) =>
            {
                var cur = watch.ElapsedMilliseconds;
                Fps       = (int)( 1000 / (cur - lastTick));
                lastTick  = cur;
                accepting = true;
                var arr = new byte[length];
                var ptr = new IntPtr(buffer);
                Marshal.Copy(ptr, arr, 0, (int)length);
                var mat = Source = Mat.FromPixelData(capture.Height, capture.Width, MatType.CV_8UC1, arr);
                accepting = false; 
                Detect(mat);
            });
        }
    }

    [RelayCommand]
    private void StopCapture()
    {
        capture.Stop();
        Capturing = false;
        Tracker   = null;
    }
}