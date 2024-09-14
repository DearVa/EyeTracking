using OpenCvSharp;

namespace EyeTracking;

public abstract class EyeTrackContext
{
    public Mat?                LastMat    { get; protected set; }
    public Point?              LeftLight  { get; protected set; }
    public Point?              RightLight { get; protected set; }
    public EyeDetectParameters Parameters { get; init; } = new();

    public abstract void DetectLights(Mat thisMat, out Point? leftLightPos, out Point? rightLightPos);

    protected void DisplayResult(Mat mat, Point? left, Point? right)
    {
        if (left != null)
        {
            mat.PutText($"X:{left.Value.X},{left.Value.Y}", left.Value, HersheyFonts.HersheySimplex, 1, Scalar.White);
            mat.Circle(left.Value, Parameters.DesiredEyeRadius, Scalar.Black, 3);
            mat.Circle(left.Value, 2, Scalar.Black, 4);
        }

        if (right != null)
        {
            mat.PutText($"X:{right.Value.X},{right.Value.Y}", right.Value, HersheyFonts.HersheySimplex, 1,
                Scalar.White);
            mat.Circle(right.Value, Parameters.DesiredEyeRadius, Scalar.Black, 3);
            mat.Circle(right.Value, 2, Scalar.Black, 4);
        }

        OnDebug?.Invoke(DebugHint.Output, mat);
    }

    public event DebugHandler? OnDebug;

    public delegate void DebugHandler(DebugHint hint, params object[] args);

    protected void Debug(DebugHint hint, params object[] args) => OnDebug?.Invoke(hint, args);

    public enum DebugHint
    {
        Origin,
        Output,
        Candidate,
        Subtraction,
        Bin_Subtraction
    }
}