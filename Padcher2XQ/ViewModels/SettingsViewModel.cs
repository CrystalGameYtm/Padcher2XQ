using CommunityToolkit.Mvvm.ComponentModel;

namespace Padcher2XQ.ViewModels;

public partial class SettingsViewModel : ViewModelBase
{
    [ObservableProperty]
    private bool _isDarkMode;

    [ObservableProperty]
    private string _asarPath = "asar.exe";
}