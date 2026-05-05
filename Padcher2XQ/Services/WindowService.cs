using Microsoft.Extensions.DependencyInjection;
using Padcher2XQ.ViewModels;
using Padcher2XQ.Views;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Padcher2XQ.Services;

public interface IWindowService
{
    void ShowSettingsWindow();

    Task<string?> ShowSelectZipAsync(List<string> entries);
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

    public async Task<string?> ShowSelectZipAsync(List<string> entries)
    {
        var viewarchive = new SelectZipViewModel(entries);
        var zipwindow = new SelectZipWindow()
        {
            DataContext = viewarchive
        };
        var mainWindow = (Avalonia.Application.Current?.ApplicationLifetime as Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime)?.MainWindow;
        if (mainWindow != null)
        {
            return await zipwindow.ShowDialog<string>(mainWindow);
        }
        return null;
    }

   
}