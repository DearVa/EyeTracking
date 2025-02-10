using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Avalonia.Styling;
using EyeTracking.Desktop.Converters;
using Color = Avalonia.Media.Color;

namespace EyeTracking.Desktop.Styles;


public static class NeonStyle
{
    /// <summary>
    /// Default Angle of colors
    /// </summary>
    public static int Angle { get; set; } = 120;

    public static void SetAngle(this TemplatedControl templatedControl, int angle)
    {
        templatedControl.SetNeon(TemplatedControl.BorderBrushProperty, GetColor(), Convert(GetColor()));
        return;

        Color GetColor() =>
            templatedControl.BorderBrush is ISolidColorBrush { Color: var color }
                ? color
                : throw new ArgumentException(
                    $"{nameof(templatedControl.BorderBrush)} is not {nameof(ISolidColorBrush)}");

        Color Convert(Color from) => (Color)new RelatedColorConverter().Convert(from, typeof(Color), angle, null!)!;
    }

    public static Action<Color, Color> SetNeon(this
        StyledElement element,
        StyledProperty<IBrush?> styledProperty,
        Color primaryColor,
        Color secondaryColor,
        double opacity = 1d)
    {
        var animation = new Animation
        {
            IterationCount = IterationCount.Infinite,
            Duration       = TimeSpan.FromSeconds(2),
        };
        element.SetValue(styledProperty, new LinearGradientBrush
        {
            Opacity = opacity,
            StartPoint = RelativePoint.TopLeft,
            EndPoint   = RelativePoint.BottomRight,
            GradientStops =
            {
                new GradientStop
                {
                    Color  = primaryColor,
                    Offset = 0
                },
                new GradientStop
                {
                    Color  = primaryColor,
                    Offset = 1
                }
            }
        });
        List<(GradientStop start, GradientStop stop)> stops = [];
        foreach (var (start, end, index) in (((RelativePoint start, RelativePoint end)[])
            [
                (RelativePoint.TopLeft, RelativePoint.BottomRight),
                (RelativePoint.Parse("100% 0%"), RelativePoint.Parse("0% 100%")),
                (RelativePoint.BottomRight, RelativePoint.TopLeft),
                (RelativePoint.Parse("0% 100%"), RelativePoint.Parse("100% 0%")),
                (RelativePoint.TopLeft, RelativePoint.BottomRight),
            ]).Select((x, i) => (x.start, x.end, index: 1 / 4d * i)))
        {
            var gStart = new GradientStop
            {
                Offset = 0,
                Color  = primaryColor
            };
            var gStop = new GradientStop
            {
                Offset = 1,
                Color  = secondaryColor
            };
            animation.Children.Add(new KeyFrame
            {
                Cue = new Cue(index),
                Setters =
                {
                    new Setter(styledProperty, new LinearGradientBrush
                    {
                        Opacity = opacity,
                        StartPoint = start,
                        EndPoint   = end,
                        GradientStops =
                        {
                            gStart,
                            gStop
                        }
                    })
                }
            });
            stops.Add((gStart, gStop));
        }

        element.Styles.Add(new Style
        {
            Animations = { animation }
        });
        return (primary, secondary) =>
        {
            element.SetValue(styledProperty, new LinearGradientBrush
            {
                StartPoint = RelativePoint.TopLeft,
                EndPoint   = RelativePoint.BottomRight,
                GradientStops =
                {
                    new GradientStop
                    {
                        Color  = primary,
                        Offset = 0
                    },
                    new GradientStop
                    {
                        Color  = secondary,
                        Offset = 1
                    }
                }
            });
            foreach (var (start, stop) in stops)
            {
                start.Color = primary;
                stop.Color  = secondary;
            }
        };
    }

}