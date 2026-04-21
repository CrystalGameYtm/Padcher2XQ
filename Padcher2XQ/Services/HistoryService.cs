using System;
using System.Collections.Generic;
using System.Text.Json;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Padcher2XQ.Models;

namespace Padcher2XQ.Services;
public class HistoryService
{
    private readonly string _filePath;
    private const int MaxEntries = 10;

    public HistoryService()
    {
        var configDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 
            "Padcher2XQ"
        );

        if (!Directory.Exists(configDir)) Directory.CreateDirectory(configDir);
        _filePath = Path.Combine(configDir, "history.json");
    }

    public async Task AddEntryAsync(string romPath, string patchPath)
    {
        var history = await GetHistoryAsync();
        
        var newEntry = new PatchHistory
        { 
            RomPath = romPath, 
            RomName = Path.GetFileName(romPath),
            PatchPath = patchPath 
        };

        history.Insert(0, newEntry);
        
        var updatedHistory = history.Take(MaxEntries).ToList();

        var json = JsonSerializer.Serialize(updatedHistory, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(_filePath, json);
    }

    public async Task<List<PatchHistory>> GetHistoryAsync()
    {
        if (!File.Exists(_filePath)) return new List<PatchHistory>();

        try
        {
            var json = await File.ReadAllTextAsync(_filePath);
            return JsonSerializer.Deserialize<List<PatchHistory>>(json) ?? new List<PatchHistory>();
        }
        catch
        {
            return new List<PatchHistory>();
        }
    }
}