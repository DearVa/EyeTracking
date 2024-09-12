using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EyeTracking.Desktop.Extensions;
using EyeTracking.Desktop.Views.Windows;
using OpenCvSharp;
using Window = Avalonia.Controls.Window;

namespace EyeTracking.Desktop.ViewModels;

public partial class TrackDebugViewModel(
    Mat mat, 
    string point, 
    IImmutableSolidColorBrush brush,
    EyeDetectParameters parameters) : ObservableObject, IDisposable
{
    public IImmutableSolidColorBrush Brush  => brush;
    public string                    Point  => point;
    public WriteableBitmap           Origin { get; } = mat.ToWriteableBitmap();

    public EyeDetectParameters Parameters { get; } = parameters with { };

    [RelayCommand]
    private void Detail() => new DebugWindow(this).Show();

    public void Dispose()
    {
        Origin.Dispose();
        mat.Dispose();
    }
}