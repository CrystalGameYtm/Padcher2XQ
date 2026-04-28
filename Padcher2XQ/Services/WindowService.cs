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
    Task<string> ShowSelectZip(List<string> entryNames);
}

public class WindowService : IWindowService
{
    private readonly IServiceProvider _serviceProvider;

    public WindowService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    void IWindowService.ShowSettingsWindow()
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

    Task<string> IWindowService.ShowSelectZip(List<string> entryNames)
    {
        var app = _serviceProvider.GetRequiredService<App>();
        var mainWindow = app.GetMainWindow();
        
        if (mainWindow is not null)
        {
            var selectZipWindow = new SelectZipWindow
            {
                DataContext = _serviceProvider.GetRequiredService<SelectZipViewModel>()
            };
        
            selectZipWindow.ShowDialog(mainWindow);
        }
    }
}