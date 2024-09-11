using OpenCvSharp;

namespace EyeTracking;

public record EyeDetectParameters
{
    public int DesiredEyeRadius  { get; set; } = 20;
    public int AveragePupilLight { get; set; } = 100;
    public int MinLightThreshold { get; set; } = 165;
    public int MotionRadius      { get; set; } = 100;

    public int MaxVerticalDistance { get; set; } = 20;
    
    public int AccumulatePupilLight => DesiredEyeRadius * DesiredEyeRadius * AveragePupilLight * 4;

    public Rect GetDesiredEyeRect(Point center, Size size)
    {
        var radius = DesiredEyeRadius;
        var left   = center.X   - radius;
        left = Math.Max(left, 0);
        var top    = center.Y   - radius;
        top = Math.Max(0, top);
        var width  = size.Width  - left;
        var height = size.Height - top;
        return new Rect(left, top, Math.Min(2 * radius, width), Math.Min(2 * radius, height));
    }
}