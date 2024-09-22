namespace EyeTracking.Extensions;

public static class ObjectExtensions
{
    public static T? As<T>(this object? instance) => (T?)instance;

    public static T NotNull<T>(this T? instance) =>
        instance is not null ? instance : throw new ArgumentNullException(nameof(instance));

    public static T AsNotNull<T>(this object? instance) =>
        instance is T t ? t : throw new ArgumentNullException(nameof(instance));
    
    public static T DisposeThen<T>(this T disposable) where T : IDisposable
    {
        disposable.Dispose();
        return disposable;
    }
}