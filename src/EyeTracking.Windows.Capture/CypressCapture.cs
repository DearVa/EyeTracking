using System.Runtime.InteropServices;

namespace EyeTracking.Windows.Capture;

public class CypressCapture : IDisposable
{
    private readonly Interop.Callback callback;
    private readonly NativeHandle     handle;

    public CypressCapture(IntPtr hWnd, Action<byte[]> handle)
    {
        callback = (buffer, length) =>
        {
            var managed = new byte[length];
            Marshal.Copy(buffer, managed, 0, length);
            handle(managed);
        };
        var pointer = Interop.Run(hWnd, Marshal.GetFunctionPointerForDelegate(callback));
        this.handle = new NativeHandle(pointer);
    }

    public void Dispose() => handle.Dispose();
}