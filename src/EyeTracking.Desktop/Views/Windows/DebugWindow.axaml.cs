using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using EyeTracking.Desktop.ViewModels;

namespace EyeTracking.Desktop.Views.Windows;

public partial class DebugWindow : Window
{
    public DebugWindow(TrackDebugViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}