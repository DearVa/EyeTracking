using System.Runtime.InteropServices;

namespace EyeTracking.Windows.Capture;

internal static class Interop
{
    public const string DllName = "EyeTracking.Windows.Native.Capture";
    
    public delegate void Callback(IntPtr buffer, int length);

    [DllImport(DllName)]
    public static extern IntPtr Run(IntPtr hWnd, IntPtr handler);

    [DllImport(DllName)]
    public static extern void Stop(IntPtr pointer);
}