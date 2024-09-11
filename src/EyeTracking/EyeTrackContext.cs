using EyeTracking.Extensions;
using OpenCvSharp;

namespace EyeTracking;

public class EyeTrackContext
{
    public Mat?   LastMat  { get; private set; }
    public Point? LeftLight  { get; private set; }
    public Point? RightLight { get; private set; }

    public EyeDetectParameters Parameters { get; set; } = new();


    public void DetectLights(Mat thisMat, out Point? leftLightPos, out Point? rightLightPos)
    {
        rightLightPos = null;
        leftLightPos  = null;
        if (LastMat == null)
        {
            LastMat = thisMat;
            return;
        }

        using var subMat = PositiveSubtract(thisMat, LastMat, out _, out var darker);
        subMat.Show();
        using var binMat = darker.Threshold(Parameters.MinLightThreshold, byte.MaxValue, ThresholdTypes.Tozero);
        var       clone  = thisMat.Clone();

        if (LeftLight  != null) leftLightPos  = CheckLight(thisMat, binMat, LeftLight.Value, false);
        if (RightLight != null) rightLightPos = CheckLight(thisMat, binMat, RightLight.Value, false);

        if (leftLightPos != null && rightLightPos != null)
        {
            LeftLight  = leftLightPos;
            RightLight = rightLightPos;
            DisplayResult(clone, leftLightPos, rightLightPos);
            return;
        }

        using var tmp = binMat.Clone();
        List<Point> candidates = [];
        while (true)
        {
            tmp.MinMaxLoc(out _, out var maxVal, out _, out var maxPoint);
            if (maxVal <= 0) break;
            tmp.Circle(maxPoint, Parameters.DesiredEyeRadius, Scalar.Black, 2 * Parameters.DesiredEyeRadius);
            var result = CheckLight(thisMat, binMat, maxPoint);
            if (result != null) candidates.Add(maxPoint);
        }

        var motionRadius = Parameters.MotionRadius;
        foreach (var candidate in candidates)
        {
            if (leftLightPos != null && rightLightPos != null) break;
            if (leftLightPos != null)
            {
                if (leftLightPos.Value.DistanceTo(candidate) < motionRadius) continue; //符合左侧
            }
            else if (LeftLight != null && LeftLight.Value.DistanceTo(candidate) < motionRadius) //有上一个参考
            {
                leftLightPos = candidate;
                continue;
            }

            if (rightLightPos != null)
            {
                if (rightLightPos.Value.DistanceTo(candidate) < motionRadius) continue; //符合右侧
            }
            else if (RightLight != null && RightLight.Value.DistanceTo(candidate) < motionRadius) //有上一个参考
            {
                rightLightPos = candidate;
                continue;
            }

            if (leftLightPos == null && rightLightPos == null)
            {
                if (candidate.X <= thisMat.Width / 2)
                    leftLightPos = candidate;
                else
                    rightLightPos = candidate;
                continue;
            }

            var horizon = (leftLightPos ?? rightLightPos)!.Value.Y;
            if (Math.Abs(horizon - candidate.Y) < Parameters.MaxVerticalDistance)
            {
                leftLightPos  ??= candidate;
                rightLightPos ??= candidate;
            }

        }

        DisplayResult(clone, leftLightPos, rightLightPos);
        LeftLight  = leftLightPos;
        RightLight = rightLightPos;
        LastMat  = thisMat;
    }

    private Point? CheckLight(Mat origin, Mat binMat, Point lightPos, bool isPointDetected = true)
    {
        var       rect  = Parameters.GetDesiredEyeRect(lightPos, binMat.Size());
        using var sub   = binMat.SubMat(rect);
        var       x     = sub.Width;
        var       last  = 0d;
        var       state = 0;
        while (x-- > 0)
        {
            using var tmp   = sub.Col(x);
            var       value = tmp.Sum().Val0;
            switch (state)
            {
                case 0: //initial
                    state = 1;
                    break;
                case 1: //decreasing
                    if (value < last) state = 2;
                    break;
                case 2 : //increasing
                    if (value > last) state = 3;
                    break;
                case 3: // to zero
                    if (value == 0) state = 4;
                    break;
            }

            if (state == 4)
            {
                if (isPointDetected) return lightPos;
                using var t = origin.SubMat(rect);
                t.MinMaxLoc(out _, out Point max);
                return max.Add(rect.TopLeft);
            }
            last  = value;

        }
        using var failed = binMat.SubMat(rect);
        failed.Resize(new Size(400,400)).Show();
        return null;
    }

    private static Mat PositiveSubtract(Mat one, Mat another, out Mat brighter, out Mat darker) =>
        one.Sum().Val0 > another.Sum().Val0
            ? (brighter = one)     - (darker = another)
            : (brighter = another) - (darker = one);


    private void DisplayResult(Mat mat, Point? left, Point? right)
    {
        if (left != null)
        {
            mat.PutText($"X:{left.Value.X},{left.Value.Y}", left.Value, HersheyFonts.HersheySimplex, 1, Scalar.White);
            mat.Circle(left.Value,Parameters.DesiredEyeRadius, Scalar.Black, 3);
            mat.Circle(left.Value, 2, Scalar.Black, 4);
        }

        if (right != null)
        {
            mat.PutText($"X:{right.Value.X},{right.Value.Y}", right.Value, HersheyFonts.HersheySimplex, 1, Scalar.White);
            mat.Circle(right.Value,Parameters.DesiredEyeRadius, Scalar.Black, 3);
            mat.Circle(right.Value, 2, Scalar.Black, 4);
        }
        mat.Show();
    }
}