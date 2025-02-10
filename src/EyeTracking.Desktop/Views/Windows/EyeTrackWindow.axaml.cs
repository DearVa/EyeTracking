using Avalonia.Controls;
using EyeTracking.Desktop.ViewModels;

namespace EyeTracking.Desktop.Views.Windows;

public partial class EyeTrackWindow : Window
{
    public EyeTrackWindow(EyeTrackViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}