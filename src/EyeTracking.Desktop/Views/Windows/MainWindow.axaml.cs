using EyeTracking.Desktop.ViewModels;
using Window = Avalonia.Controls.Window;

namespace EyeTracking.Desktop.Views.Windows;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new EyeTrackViewModel(this);
        
    }
}