using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using Padcher2XQ.Services;
using Padcher2XQ.ViewModels;
using Padcher2XQ.Views;
using System;
using Padcher.Core.Patching;
using Padcher.Core.RetroAchievements;

namespace Padcher2XQ;

public class App : Application
{
    public new static App? Current => Application.Current as App;
    public IServiceProvider? Services { get; private set; }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var services = new ServiceCollection();
        
        services.AddSingleton<App>(this);
        services.AddSingleton<SettingsService>();
        services.AddSingleton<IWindowService, WindowService>();
        services.AddSingleton<IFileDialogService, FileDialogService>();
        services.AddSingleton<ChecksumService>();

        services.AddSingleton<RomPatcher>(sp => 
        {
            var settings = sp.GetRequiredService<SettingsService>();
            return new RomPatcher 
            { 
                AsarPath = settings.Config.AsarPath, 
                XdeltaPath = settings.Config.XdeltaPath 
            };
        });

        services.AddSingleton<RaClient>(sp => 
        {
            var settings = sp.GetRequiredService<SettingsService>();
            string user = string.IsNullOrWhiteSpace(settings.Config.RaUser) ? "Guest" : settings.Config.RaUser;
            string key = string.IsNullOrWhiteSpace(settings.Config.RaApiKey) ? "None" : settings.Config.RaApiKey;
            
            return new RaClient(user, key);
        });

        services.AddTransient<MainWindowViewModel>();
        services.AddTransient<SettingsViewModel>();
        Services = services.BuildServiceProvider();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = Services.GetRequiredService<MainWindowViewModel>()
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    public Window? GetMainWindow()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            return desktop.MainWindow;
        }
        return null;
    }
}