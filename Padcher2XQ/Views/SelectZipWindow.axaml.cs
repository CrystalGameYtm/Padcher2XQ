using Avalonia.Controls;
using Avalonia.Interactivity;
using Padcher2XQ.ViewModels;

namespace Padcher2XQ.Views;

public partial class SelectZipWindow : Window
{
    public SelectZipWindow()
    {
        InitializeComponent();
    }

    private void SelectButton_Click(object? sender, RoutedEventArgs e)
    {
        var vm = DataContext as SelectZipViewModel;
        Close(vm?.SelectedEntry); 
    }

    private void CancelButton_Click(object? sender, RoutedEventArgs e)
    {
        Close(null); 
    }
}