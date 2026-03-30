using Avalonia.Controls;
using Avalonia.Interactivity;
using System;

namespace Padcher2XQ.Views;

public partial class SettingsWindow : Window
{
    public SettingsWindow()
    {
        InitializeComponent();
    }


    private void OnSaveClick(object? sender, RoutedEventArgs e)
    {
        // У майбутньому тут можна викликати метод ViewModel для збереження в файл
        // var vm = DataContext as SettingsViewModel;
        // vm?.SaveSettings();
        
        this.Close();
    }
    
    private void OnCloseClick(object? sender, RoutedEventArgs e)
    {
        this.Close();
    }
}