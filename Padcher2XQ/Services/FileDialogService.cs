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

    public async Task<string?> OpenFileAsync(string title, string[] extensions)
    {
        var window = GetMainWindow();
        if (window is null) return null;

        var options = new FilePickerOpenOptions
        {
            Title = title,
            AllowMultiple = false,
            // ВИПРАВЛЕНО ТУТ: new List замість newList
            FileTypeFilter = new List<FilePickerFileType>
            {
                new FilePickerFileType("Supported Files") { Patterns = extensions },
                new FilePickerFileType("All Files") { Patterns = new[] { "*.*" } }
            }
        };

        var result = await window.StorageProvider.OpenFilePickerAsync(options);
        return result.Count > 0 ? result[0].Path.LocalPath : null;
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
            // ВИПРАВЛЕНО ТУТ: new List замість newList
            FileTypeChoices = new List<FilePickerFileType>
            {
                new FilePickerFileType(extension.ToUpper() + " File") { Patterns = new[] { $"*.{extension}" } }
            }
        };

        var result = await window.StorageProvider.SaveFilePickerAsync(options);
        return result?.Path.LocalPath;
    }
}