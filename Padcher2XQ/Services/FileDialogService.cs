using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Padcher2XQ.Services;

public class FileDialogService : IFileDialogService
{
    private Window? GetMainWindow()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            return desktop.MainWindow;
        }
        return null;
    }

    // Реалізація з 3 аргументами та поверненням масиву
    public async Task<string[]?> OpenFileAsync(string title, string[] extensions, bool allowMultiple = false)
    {
        var window = GetMainWindow();
        if (window is null) return null;

        var options = new FilePickerOpenOptions
        {
            Title = title,
            AllowMultiple = allowMultiple,
            FileTypeFilter = new List<FilePickerFileType>
            {
                new FilePickerFileType("Supported Files") { Patterns = extensions },
                new FilePickerFileType("All Files") { Patterns = new[] { "*.*" } }
            }
        };

        var result = await window.StorageProvider.OpenFilePickerAsync(options);
        
        // Перетворюємо результат у масив рядків
        return result.Count > 0 ? result.Select(x => x.Path.LocalPath).ToArray() : null;
    }

    public async Task<string?> SaveFileAsync(string title, string defaultName, string extension)
    {
        var window = GetMainWindow();
        if (window is null) return null;

        var options = new FilePickerSaveOptions
        {
            Title = title,
            SuggestedFileName = defaultName,
            DefaultExtension = extension,
            FileTypeChoices = new List<FilePickerFileType>
            {
                new FilePickerFileType(extension.ToUpper() + " File") { Patterns = new[] { $"*.{extension}" } }
            }
        };

        var result = await window.StorageProvider.SaveFilePickerAsync(options);
        return result?.Path.LocalPath;
    }
}