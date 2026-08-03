using Microsoft.Extensions.DependencyInjection;
using Padcher2XQ.ViewModels;
using Padcher2XQ.Views;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Controls;

namespace Padcher2XQ.Services;

public interface IWindowService
{
    void ShowSettingsWindow();
    
    Task<SingleZipResult?> ShowSelectZipSingleAsync(IEnumerable<string> entries);
    
    Task<List<MultiZipResult>?> ShowSelectZipMultiAsync(IEnumerable<string> entries);
}

public class WindowService(IServiceProvider serviceProvider) : IWindowService
{
    public void ShowSettingsWindow()
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

    public async Task<SingleZipResult?> ShowSelectZipSingleAsync(IEnumerable<string> entries)
    {
        var app = serviceProvider.GetRequiredService<App>();
        var mainWindow = app.GetMainWindow();
        if (mainWindow is null) return null;

        var vm = new SelectZipViewModel(entries, isMultiMode: false);
        var window = new SelectZipWindow { DataContext = vm };

        return await window.ShowDialog<SingleZipResult?>(mainWindow);
    }

    public async Task<List<MultiZipResult>?> ShowSelectZipMultiAsync(IEnumerable<string> entries)
    {
        var app = serviceProvider.GetRequiredService<App>();
        var mainWindow = app.GetMainWindow();
        if (mainWindow is null) return null;

        var vm = new SelectZipViewModel(entries, isMultiMode: true);
        var window = new SelectZipWindow { DataContext = vm };

        return await window.ShowDialog<List<MultiZipResult>?>(mainWindow);
    }
}