using Avalonia.Controls;
using Avalonia.Interactivity;
using Padcher2XQ.ViewModels;
using System.Collections.Generic;
using System.Linq;

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
            var selectedFiles = vm.Entries
                .Where(x => x.IsSelected)
                .Select(x => x.FullName)
                .ToList();
            
            Close(selectedFiles);
        }
    }

    private void CancelButton_Click(object? sender, RoutedEventArgs e)
    {
        Close(null); 
    }
}