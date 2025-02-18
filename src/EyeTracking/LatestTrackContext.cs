using OpenCvSharp;

namespace EyeTracking;

public class LatestTrackContext : EyeTrackContext
{
    private bool? isLastLight;
    
    public override void DetectLights(Mat thisMat, out Point? leftLightPos, out Point? rightLightPos)
    {
        Debug(DebugHint.Origin, thisMat);
        if (LastMat is not null)
        {
            if (isLastLight is null)
            {
                var s = LastMat.Sum();
                var n = thisMat.Sum();
                isLastLight = s[0] > n[0];
            }

            var light = isLastLight.Value ? LastMat : thisMat;
            var dark  = isLastLight.Value ? thisMat : LastMat;


            using var pupilPosition     = GetPupilPosition(dark, light);
            using var blurredImageLight = ApplyGaussianBlur(light);
            using var binary            = ApplyThreshold(blurredImageLight);
            using var edges             = DetectEdges(binary);

            Cv2.FindContours(edges, out var contours, out _, RetrievalModes.List,
                ContourApproximationModes.ApproxSimple);

            ProcessEllipseAndBlobs(contours, light);
            
            isLastLight = !isLastLight;
            LastMat.Dispose();
        }

        leftLightPos  = null;
        rightLightPos = null;
        LastMat       = thisMat;

    }

    private static Mat GetPupilPosition(Mat darkImage, Mat lightImage)
    {
        var pupilPosition = new Mat();
        Cv2.Absdiff(darkImage, lightImage, pupilPosition);
        Cv2.Normalize(pupilPosition, pupilPosition, 0, 255, NormTypes.MinMax);
        return pupilPosition;
    }
    
    private static Mat ApplyGaussianBlur(Mat image)
    {
        var blurredImage = new Mat();
        Cv2.GaussianBlur(image, blurredImage, new Size(5, 5), 0);
        return blurredImage;
    }
    
    private static Mat ApplyThreshold(Mat image)
    {
        var binary = new Mat();
        Cv2.Threshold(image, binary, 0, 255, ThresholdTypes.Binary | ThresholdTypes.Otsu);
        return binary;
    }

    private static Mat DetectEdges(Mat binaryImage)
    {
        var edges = new Mat();
        Cv2.Canny(binaryImage, edges, 50, 150);
        return edges;
    }

    private List<OpenCvSharp.Point> FindLargestContours(List<List<Point>> contours)
    {
        var largestContours = new List<OpenCvSharp.Point>();
        var maxArea        = 0d;
        foreach (var contour in contours)
        {
            var area = Cv2.ContourArea(contour);
            if (!(area > maxArea)) continue;
            maxArea        = area;
            largestContours = contour;
        }

        return largestContours;
    }

    void ProcessEllipseAndBlobs(Point[][] contours, Mat lightImage)
    {
        var           result        = lightImage.Clone();
        var           detectedBlobs = lightImage.Clone();
        List<Point2f> centers       = [];
        
        foreach (var contour in contours)
        {
            if(contour.Length < 5) continue;
            
            var fittedEllipse = Cv2.FitEllipse(contour);
            var area          = Cv2.ContourArea(contour);
            var aspectRatio   = Math.Abs(fittedEllipse.Size.Height / fittedEllipse.Size.Width);
            if (area is > 100 and < 1000 && aspectRatio > 0.5 && aspectRatio < 2.0)
            {
                var center = fittedEllipse.Center;
                var isDuplicate = false;
                foreach (var existingCenter in centers)
                {
                    if (Cv2.Norm(center.DistanceTo(existingCenter)) < 10)
                    {
                        isDuplicate = true;
                        break;
                    }
                }

                if (!isDuplicate)
                {
                    centers.Add(center);
                    Cv2.Ellipse(result, fittedEllipse, Scalar.Green, 2);
                    Cv2.Circle(result, center.ToPoint(), 2, Scalar.Blue, -1);


                    var maskRaw = Mat.Zeros(lightImage.Size(), MatType.CV_8UC1);
                    var mask    = new Mat();
                    Cv2.EqualizeHist(maskRaw, mask);
                    Cv2.Ellipse(mask, fittedEllipse, Scalar.White, -1);
                    var maskedImage = new Mat();
                    lightImage.CopyTo(maskedImage, mask);
                    
                    const int manualThresholdValue = 105;
                    var       binaryMasked         = new Mat();
                    Cv2.Threshold(mask,binaryMasked,manualThresholdValue,255,ThresholdTypes.Binary);

                    Cv2.FindContours(binaryMasked, out var maskedContours, out _,
                        RetrievalModes.List, ContourApproximationModes.ApproxSimple);

                    var blobCount = 0;
                    foreach (var mContour in maskedContours)
                    {
                        var mArea = Cv2.ContourArea(mContour);
                        if (mArea is > 3 and < 10)
                        {
                            Cv2.DrawContours(detectedBlobs, [mContour], -1, Scalar.Red, 2);
                            blobCount++;

                            var moments = Cv2.Moments(mContour);
                            var centroid = new Point2f((float)(moments.M10 / moments.M00), (float)(moments.M01 / moments.M00));
                            
                            Cv2.Circle(detectedBlobs, centroid.ToPoint(), 2, Scalar.Cyan, -1);
                        }
                    }

                    Cv2.MinMaxLoc(maskedImage, 
                        out _, out var maxVal, 
                        out _, out var brightestPoint, mask);
                    
                }
            }
        }
    }
}