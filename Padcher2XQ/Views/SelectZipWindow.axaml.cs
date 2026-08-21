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
        if (DataContext is SelectZipViewModel vm)
        {
            if (vm.IsMultiMode)
            {
                Close(vm.GetMultiResults());
            }
            else
            {
                Close(vm.GetSingleResult());
            }
        }
        else
        {
            Close(null);
        }
    }

    private void CancelButton_Click(object? sender, RoutedEventArgs e)
    {
        Close(null);
    }
}