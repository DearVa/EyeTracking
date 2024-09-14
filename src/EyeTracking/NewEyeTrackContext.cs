using OpenCvSharp;

namespace EyeTracking;

public class NewEyeTrackContext : EyeTrackContext
{
    public override void DetectLights(Mat thisMat, out Point? leftLightPos, out Point? rightLightPos)
    {
        leftLightPos  = null;
        rightLightPos = null;
        Debug(DebugHint.Origin, thisMat);
        DetectLightsInternal(thisMat, ref leftLightPos, ref rightLightPos);
        LeftLight  = leftLightPos;
        RightLight = rightLightPos;
        using var clone = thisMat.Clone();
        DisplayResult(clone, leftLightPos, rightLightPos);
        LastMat?.Dispose();
        LastMat = thisMat;
    }

    private void DetectLightsInternal(Mat thisMat, ref Point? leftLightPos, ref Point? rightLightPos)
    {
        if (LastMat == null) return;

        using var subMat = PositiveSubtract(thisMat, LastMat, out _, out var darker);
        Debug(DebugHint.Subtraction, subMat);
        using var binMat = Parameters.Threshold(darker);
        Debug(DebugHint.Bin_Subtraction, binMat);

        if (LeftLight  != null) leftLightPos  = CheckLight(thisMat, binMat, LeftLight.Value, false);
        if (RightLight != null) rightLightPos = CheckLight(thisMat, binMat, RightLight.Value, false);

        if (leftLightPos != null && rightLightPos != null) return;

        using var   tmp        = binMat.Clone();
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
    }

    private Point? CheckLight(Mat origin, Mat binMat, Point lightPos, bool isPointDetected = true)
    {
        var       rect  = Parameters.GetDesiredEyeRect(lightPos, binMat.Size());
        using var ori   = origin.SubMat(rect);
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
                var ret = max.Add(rect.TopLeft);
                Debug(DebugHint.Candidate, ori, true, ret);
                return ret;
            }
            last  = value;

        }
        Debug(DebugHint.Candidate, ori, false, lightPos);
        return null;
    }

    private static Mat PositiveSubtract(Mat one, Mat another, out Mat brighter, out Mat darker) =>
        one.Sum().Val0 > another.Sum().Val0
            ? (brighter = one)     - (darker = another)
            : (brighter = another) - (darker = one);
    
    #if DEBUG

#endif
}