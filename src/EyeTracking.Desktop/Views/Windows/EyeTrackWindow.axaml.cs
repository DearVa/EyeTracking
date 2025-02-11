using System.Diagnostics.CodeAnalysis;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Platform;
using Avalonia.Threading;
using EyeTracking.Desktop.ViewModels;
using Point = System.Drawing.Point;

namespace EyeTracking.Desktop.Views.Windows;

public partial class EyeTrackWindow : Window
{
    public EyeTrackWindow(EyeTrackViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
        Loaded += (_, _) =>
        {
            scale              =  Screens.ScreenFromWindow(this)!.Scaling;
            
            offset             =  Canvas.PointToScreen(new Avalonia.Point());
            var ellipse = this.FindControl<Ellipse>("EyeSight") ?? throw new KeyNotFoundException();
            vm.PropertyChanged += (o, e) =>
            {
                if (e.PropertyName is not nameof(EyeTrackViewModel.MousePos)) return;
                var point = vm.MousePos;
                Dispatcher.UIThread.Invoke(() =>
                {
                    var p = Canvas.PointToClient(new PixelPoint(point.X, point.Y));
                    Canvas.SetLeft(ellipse, p.X - ellipse.Width  / 2);
                    Canvas.SetTop(ellipse, p.Y  - ellipse.Height / 2);
                    vm.CanvasPos = p;
                });
            };
        };
    }

    private Avalonia.Point Map(Point point)
    {
        if (ratio is not null)
        {
            var r = ratio.Value;
            return new Avalonia.Point(point.X * r.XR, point.Y * r.YR);
        }

        var p      = Canvas.PointToClient(new PixelPoint(point.X, point.Y));
        ratio = ((p.X - offset.X) / point.X, (p.Y - offset.Y) / point.Y);
        return p;
    }

    private (double XR, double YR)? ratio;
    private PixelPoint              offset;
    private double                  scale;
}