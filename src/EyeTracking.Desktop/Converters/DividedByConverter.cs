using System.Globalization;
using Avalonia.Data.Converters;

namespace EyeTracking.Desktop.Converters;

public class DividedByConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => (double)(value ?? 1d) / (double)(parameter ?? 1d);

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => 
        throw new NotSupportedException();
}