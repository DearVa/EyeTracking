using System.Diagnostics.CodeAnalysis;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;
using EyeTracking.Desktop.Extensions;
using EyeTracking.Desktop.Styles;
using Color = Avalonia.Media.Color;

namespace EyeTracking.Desktop.Views.Controls;

public class NeonBorder : Border
{

    public NeonBorder()
    {
        Loaded += (_, _) =>
        {
            var borderUpdate = this.SetNeon(BorderBrushProperty,
                PrimaryBorderColor,
                SecondaryBorderColor);
           
            var p = this.GetObservable(PrimaryBorderColorProperty)
                .Subscribe(primary => borderUpdate(primary, SecondaryBorderColor));
            var s = this.GetObservable(SecondaryBorderColorProperty)
                .Subscribe(secondary => borderUpdate(PrimaryBorderColor, secondary));

            DetachedFromVisualTree += (_, _) =>
            {
                p.Dispose();
                s.Dispose();
            };
        };
    }

    public static readonly StyledProperty<Color> PrimaryBorderColorProperty =
        AvaloniaProperty.Register<NeonBorder, Color>(
            nameof(PrimaryBorderColor));

    public Color PrimaryBorderColor
    {
        get => GetValue(PrimaryBorderColorProperty);
        set => SetValue(PrimaryBorderColorProperty, value);
    }

    public static readonly StyledProperty<Color> SecondaryBorderColorProperty =
        AvaloniaProperty.Register<NeonBorder, Color>(
            nameof(SecondaryBorderColor));

    public Color SecondaryBorderColor
    {
        get => GetValue(SecondaryBorderColorProperty);
        set => SetValue(SecondaryBorderColorProperty, value);
    }
}