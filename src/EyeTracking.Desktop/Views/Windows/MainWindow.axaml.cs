using Avalonia.Controls;
using Avalonia.Threading;
using EyeTracking.Desktop.ViewModels;
using EyeTracking.Windows.Capture;

namespace EyeTracking.Desktop.Views.Windows;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new EyeTrackViewModel(this);
        Loaded += (o, e) =>
        {
            var handle = TryGetPlatformHandle();
            if (handle == null) return;
            Dispatcher.UIThread.Invoke(() =>
            {
                unsafe
                {
                    var capture = new CypressCapture(handle.Handle);
                    capture.Start((buffer, length) =>
                    {
                        var a = new IntPtr(buffer);
                    });
                }
            });
        };
    }
}