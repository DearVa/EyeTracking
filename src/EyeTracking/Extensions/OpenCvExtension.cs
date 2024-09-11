using OpenCvSharp;

namespace EyeTracking.Extensions;

public static class OpenCvExtension
{
    public static void Show(this Mat mat, string winName = nameof(OpenCvSharp))
    {
        Cv2.ImShow(winName, mat);
        Cv2.WaitKeyEx();
    }
}