using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Padcher2XQ.Models;
using Padcher2XQ.ViewModels;

namespace Padcher2XQ.Services;

public class PresetArchiveService
{
    private readonly string _tempExtractDir;

    public PresetArchiveService()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        _tempExtractDir = Path.Combine(appData, "Padcher2XQ", "TempPresets");
        Directory.CreateDirectory(_tempExtractDir);
    }

    public async Task ExportPresetArchiveAsync(string destinationZipPath, IEnumerable<PatchItemViewModel> patches)
    {
        if (File.Exists(destinationZipPath))
            File.Delete(destinationZipPath);

        using var zip = ZipFile.Open(destinationZipPath, ZipArchiveMode.Create);
        
        var manifest = new PatchProfile
        {
            Name = Path.GetFileNameWithoutExtension(destinationZipPath),
            CreatedAt = DateTime.Now,
            Patches = new List<PatchProfileItem>()
        };

        int index = 0;
        foreach (var patch in patches)
        {
            if (File.Exists(patch.FilePath))
            {
                string archiveFileName = $"{index}_{Path.GetFileName(patch.FilePath)}";
                zip.CreateEntryFromFile(patch.FilePath, archiveFileName);

                manifest.Patches.Add(new PatchProfileItem
                {
                    Path = archiveFileName, 
                    IsEnabled = patch.IsEnabled
                });
                index++;
            }
        }
        var manifestEntry = zip.CreateEntry("manifest.json");
        using var writer = new StreamWriter(manifestEntry.Open());
        var json = JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true });
        await writer.WriteAsync(json);
    }

    public async Task<List<PatchItemViewModel>> ImportPresetArchiveAsync(string sourceZipPath)
    {
        var result = new List<PatchItemViewModel>();
        string sessionExtractPath = Path.Combine(_tempExtractDir, Guid.NewGuid().ToString());
        Directory.CreateDirectory(sessionExtractPath);

        ZipFile.ExtractToDirectory(sourceZipPath, sessionExtractPath);

        string manifestPath = Path.Combine(sessionExtractPath, "manifest.json");
        if (!File.Exists(manifestPath))
            return result;

        var json = await File.ReadAllTextAsync(manifestPath);
        var manifest = JsonSerializer.Deserialize<PatchProfile>(json);

        if (manifest?.Patches != null)
        {
            foreach (var item in manifest.Patches)
            {
                string fullPath = Path.Combine(sessionExtractPath, item.Path);
                if (File.Exists(fullPath))
                {
                    result.Add(new PatchItemViewModel
                    {
                        FilePath = fullPath,
                        PatchName = Path.GetFileName(item.Path).Substring(item.Path.IndexOf('_') + 1), 
                        IsEnabled = item.IsEnabled,
                        Format = Path.GetExtension(fullPath).TrimStart('.').ToUpper()
                    });
                }
            }
        }
        return result;
    }
}