using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Padcher2XQ.Services;
using System.Threading.Tasks;

namespace Padcher2XQ.ViewModels;

public partial class SettingsViewModel : ViewModelBase
{
    private readonly SettingsService _settingsService;
    private readonly IFileDialogService _fileDialogService; // Додаємо сервіс діалогів

    [ObservableProperty] private bool _isDarkMode;
    [ObservableProperty] private string _asarPath = string.Empty;
    [ObservableProperty] private string _xdeltaPath = string.Empty;

    public SettingsViewModel(SettingsService settingsService, IFileDialogService fileDialogService)
    {
        _settingsService = settingsService;
        _fileDialogService = fileDialogService;
        
        IsDarkMode = _settingsService.Config.IsDarkMode;
        AsarPath = _settingsService.Config.AsarPath;
        XdeltaPath = _settingsService.Config.XdeltaPath;
    }

    // Команда для вибору Asar
    [RelayCommand]
    private async Task SelectAsarFile()
    {
        var paths = await _fileDialogService.OpenFileAsync("Select Asar Executable", new[] { "*.*", "*.exe" });
        if (paths != null && paths.Length > 0)
        {
            AsarPath = paths[0]; // Призначаємо шлях, і OnAsarPathChanged автоматично його збереже
        }
    }

    // Команда для вибору Xdelta3
    [RelayCommand]
    private async Task SelectXdeltaFile()
    {
        var paths = await _fileDialogService.OpenFileAsync("Select Xdelta3 Executable", new[] { "*.*", "*.exe" });
        if (paths != null && paths.Length > 0)
        {
            XdeltaPath = paths[0];
        }
    }

    partial void OnIsDarkModeChanged(bool value)
    {
        _settingsService.Config.IsDarkMode = value;
        _settingsService.ApplyTheme(value);
        _settingsService.SaveSettings();
    }

    partial void OnAsarPathChanged(string value)
    {
        _settingsService.Config.AsarPath = value;
        _settingsService.SaveSettings();
    }

    partial void OnXdeltaPathChanged(string value)
    {
        _settingsService.Config.XdeltaPath = value;
        _settingsService.SaveSettings();
    }
}