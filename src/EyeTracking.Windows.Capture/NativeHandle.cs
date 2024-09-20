using System.Runtime.InteropServices;

namespace EyeTracking.Windows.Capture;

internal class NativeHandle(IntPtr handle) : SafeHandle(handle, true)
{
    protected override bool ReleaseHandle()
    {
        Interop.Stop(handle);
        return true;
    }

    public override bool IsInvalid => true;
}