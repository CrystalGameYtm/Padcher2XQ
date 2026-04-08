using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Padcher2XQ.Views;

public partial class SettingsWindow : Window
{
    public SettingsWindow()
    {
        InitializeComponent();
    }
    
    private void OnCloseClick(object? sender, RoutedEventArgs e)
    {
        this.Close();
    }
}