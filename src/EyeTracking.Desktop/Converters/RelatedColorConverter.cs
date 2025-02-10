using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Color = Avalonia.Media.Color;

namespace EyeTracking.Desktop.Converters;

public class RelatedColorConverter : IValueConverter
{
    private static Color Convert(Color from, int angle)
    {
        var hsl  = from.ToHsl();
        return new HslColor(hsl.A, (hsl.H + angle) % 360, hsl.S, hsl.L).ToRgb();
    }
    
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not Color color) throw new ArgumentException($"{nameof(value)} is not {nameof(Color)}");
        var angle = parameter switch
        {
            null       => 0,
            string str => int.Parse(str),
            _          => (int)parameter,
        };
        return Convert(color, angle);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not Color color) throw new ArgumentException($"{nameof(value)} is not {nameof(Color)}");
        var angle = parameter switch
        {
            null       => 0,
            string str => -int.Parse(str),
            _          => -(int)parameter,
        };
        return Convert(color, angle);
    }
}