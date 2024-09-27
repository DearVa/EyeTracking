using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.MarkupExtensions;

namespace EyeTracking.Desktop.Views.Windows;

public partial class MessageBox : Window
{
    public MessageBox(string message, Window? window = null)
    {
        InitializeComponent();
        Message.Text = message;
        Width        = 100;
        Height       = 50;
        Owner        = window;
        WindowStartupLocation = window is null
            ? WindowStartupLocation.CenterScreen
            : WindowStartupLocation.CenterOwner;
    }

    public static void Show(string message, Window? owner = null)
    {
        var box = new MessageBox(message, owner);
        if (owner != null) box.Show(owner);
        else box.Show();
    }
}