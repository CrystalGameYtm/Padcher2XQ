using System;
using System.IO;
using System.Text.Json;
using Avalonia;
using Avalonia.Styling;
using Padcher2XQ.Models;
namespace Padcher2XQ.Services;

public class SettingsService
{
    private readonly string _settingsFilePath;
    public AppConfig Config { get; private set; }

    public SettingsService()
    {
        _settingsFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json");
        Config = new AppConfig();
        LoadSettings();
    }

    public void LoadSettings()
    {
        if (File.Exists(_settingsFilePath))
        {
            try
            {
                string json = File.ReadAllText(_settingsFilePath);
                var loadedConfig = JsonSerializer.Deserialize<AppConfig>(json);
                if (loadedConfig != null)
                {
                    Config = loadedConfig;
                }
            }
            catch (Exception)
            {
                Config = new AppConfig(); 
            }
        }
        ApplyTheme(Config.IsDarkMode);
    }

    public void SaveSettings()
    {
        var options = new JsonSerializerOptions { WriteIndented = true };
        string json = JsonSerializer.Serialize(Config, options);
        File.WriteAllText(_settingsFilePath, json);
    }

    public void ApplyTheme(bool isDark)
    {
        if (Application.Current != null)
        {
            Application.Current.RequestedThemeVariant = isDark ? ThemeVariant.Dark : ThemeVariant.Light;
        }
    }
}