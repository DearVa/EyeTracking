using System.Reflection;
using System.Runtime.CompilerServices;
using Antelcat.AutoGen.ComponentModel.Diagnostic;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EyeTracking.Desktop.Extensions;
using EyeTracking.Desktop.Views.Windows;
using OpenCvSharp;

namespace EyeTracking.Desktop.ViewModels;

[AutoMetadataFrom(typeof(EyeDetectParameters), MemberTypes.Property,
    Template =
        """
        #if {GetMethod.IsPublic}
        public {PropertyType} {Name} { get => parameters.{Name};
        #if {CanWrite}
            set {
                parameters.{Name} = value;
                OnPropertyChanged();
            }
        #endif
        }
        #endif

        """)]
public partial class TrackDebugViewModel : ObservableObject, IDisposable
{
    public TrackDebugViewModel(
        Mat mat,
        string point,
        IImmutableSolidColorBrush brush,
        EyeDetectParameters parameters)
    {
        Brush           = brush;
        Point           = point;
        Origin          = mat.ToWriteableBitmap();
        this.parameters = parameters;
        this.mat        = mat.Clone();
        PropertyChanged += (o, e) =>
        {
            if(e.PropertyName == nameof(Output)) return;
            Output?.Dispose();
            using var tmp = this.parameters.Threshold(this.mat);
            Output = tmp.ToWriteableBitmap();
        };
    }

    public IImmutableSolidColorBrush Brush  { get; }
    public string                    Point  { get; }
    public WriteableBitmap           Origin { get; }

    private readonly EyeDetectParameters parameters;
    private readonly Mat                 mat;

    [ObservableProperty] private WriteableBitmap? output;

    [RelayCommand]
    private void Detail() => new DebugWindow(this).Show();

    public void Dispose()
    {
        Origin.Dispose();
        mat.Dispose();
    }
}