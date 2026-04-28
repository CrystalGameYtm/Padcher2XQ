using Microsoft.Extensions.DependencyInjection;
using Padcher2XQ.ViewModels;
using Padcher2XQ.Views;
using System;

namespace Padcher2XQ.Services;

public interface IWindowService
{
    void ShowSettingsWindow();
    string ShowSelectZip();
}

public class WindowService(IServiceProvider serviceProvider) : IWindowService
{
    void IWindowService.ShowSettingsWindow()
    {
        var app = serviceProvider.GetRequiredService<App>();
        var mainWindow = app.GetMainWindow();
        
        if (mainWindow is not null)
        {
            var settingsWindow = new SettingsWindow
            {
                DataContext = serviceProvider.GetRequiredService<SettingsViewModel>()
            };
        
            settingsWindow.ShowDialog(mainWindow);
        }
    }

    string IWindowService.ShowSelectZip()
    {
        var app = serviceProvider.GetRequiredService<App>();
        var mainWindow = app.GetMainWindow();
        
        if (mainWindow is not null)
        {
            var selectZipWindow = new SelectZipWindow
            {
                DataContext = serviceProvider.GetRequiredService<SelectZipViewModel>()
            };
        
            selectZipWindow.ShowDialog(mainWindow);
        }

        return null!;
    }
}