using System.Globalization;
using Avalonia.Data.Converters;

namespace EyeTracking.Desktop.Converters;

public class BoolInverseConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value is false;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => value is false;
}