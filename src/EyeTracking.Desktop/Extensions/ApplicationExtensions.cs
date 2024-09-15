using Avalonia;
using EyeTracking.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace EyeTracking.Desktop.Extensions;

public static class ApplicationExtensions
{
    private static readonly IServiceProvider serviceProvider = new ServiceCollection()
        .AddEyeTrackContext()
        .BuildServiceProviderEx();

    public static IServiceProvider ServiceProvider<T>(this T? any) => serviceProvider;
}