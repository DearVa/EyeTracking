using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace EyeTracking.Desktop.ViewModels;

public partial class EyeTrackViewModel(Window window) : ObservableObject
{
    public  string? FilePath
    {
        get => filePath;
        set
        {
            SetProperty(ref filePath, value);
            if (value == null)
            {
                Context = null;
                Decoder?.Dispose();
                Decoder = null;
            }
            else
            {
                Context = new EyeTrackContext
                {
                    Parameters = Parameters
                };
                Decoder = new VideoDecoder(value);
            }
        }
    }

    private string? filePath;

    [ObservableProperty] private EyeTrackContext? context;

    [ObservableProperty] private EyeDetectParameters parameters = new();

    [ObservableProperty] private VideoDecoder? decoder;

    [ObservableProperty]
    private WriteableBitmap? detected;
    
    [RelayCommand]
    private async Task Select()
    {
        var result = await window.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            SuggestedFileName = FilePath ?? string.Empty,
            AllowMultiple     = false
        });
        foreach (var file  in result)
        {
            FilePath = file.Path.AbsolutePath;
            break;
        }
    }
    
    private void Next()
    {
        
    }
}