using System;
using System.Text.Json;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Padcher2XQ.Models;
public class HistoryService
{
    private readonly string _filePath;
    private const int MaxEntries = 20;

    public HistoryService()
    {
        _filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "history.json");
    }

    public async Task AddEntriesAsync(string romPath, string patchPath)
    {
        var history = await GetHistoryAsync();

        if (!string.IsNullOrWhiteSpace(romPath))
        {
            var existingRom = history.RomHistory.FirstOrDefault(r => r.RomPath == romPath);
            if (existingRom != null) history.RomHistory.Remove(existingRom);
            
            history.RomHistory.Insert(0, new RomEntry 
            {
                RomName = Path.GetFileName(romPath),
                RomPath = romPath,
                RomFormat = Path.GetExtension(romPath)
            });
        }

        if (!string.IsNullOrWhiteSpace(patchPath))
        {
            var existingPatch = history.PatchHistory.FirstOrDefault(p => p.PatchPath == patchPath);
            if (existingPatch != null) history.PatchHistory.Remove(existingPatch);
            
            history.PatchHistory.Insert(0, new PatchEntry 
            {
                PatchName = Path.GetFileName(patchPath),
                PatchPath = patchPath,
                PatchFormat = Path.GetExtension(patchPath)
            });
        }

        // 3. Обрізаємо списки, щоб файл не розростався безкінечно
        history.RomHistory = history.RomHistory.Take(MaxEntries).ToList();
        history.PatchHistory = history.PatchHistory.Take(MaxEntries).ToList();

        var json = JsonSerializer.Serialize(history, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(_filePath, json);
    }

    public async Task<PatcherHistory> GetHistoryAsync()
    {
        if (!File.Exists(_filePath)) return new PatcherHistory();
        try
        {
            var json = await File.ReadAllTextAsync(_filePath);
            return JsonSerializer.Deserialize<PatcherHistory>(json) ?? new PatcherHistory();
        }
        catch
        {
            return new PatcherHistory();
        }
    }
}