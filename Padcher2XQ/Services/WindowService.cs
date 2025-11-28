// Services/WindowService.cs
using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;
using Padcher2XQ.ViewModels;
using Padcher2XQ.Views;
using System;
using Avalonia;

namespace Padcher2XQ.Services;

// Інтерфейс
public interface IWindowService
{
    void ShowSettingsWindow();
}

// Реалізація
public class WindowService : IWindowService
{
    private readonly IServiceProvider _serviceProvider;

    public WindowService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public void ShowSettingsWindow()
    {
        var app = _serviceProvider.GetRequiredService<App>();
        var mainWindow = app.GetMainWindow();
        
        if (mainWindow is not null)
        {
            var settingsWindow = new SettingsWindow
            {
                DataContext = _serviceProvider.GetRequiredService<SettingsViewModel>()
            };
        
            settingsWindow.ShowDialog(mainWindow);
        }
    }
}