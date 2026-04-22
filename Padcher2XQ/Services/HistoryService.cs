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
    
    private readonly string _historyfilePath;
    private const int MaxEntries = 10;

    public HistoryService()
    { 
        _historyfilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "history.json");
        
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
        await File.WriteAllTextAsync(_historyfilePath, json);
    }

    public async Task<List<PatchHistory>> GetHistoryAsync()
    {
        if (!File.Exists(_historyfilePath)) 
            return new List<PatchHistory>();

        try
        {
            var json = await File.ReadAllTextAsync(_historyfilePath);
            return JsonSerializer.Deserialize<List<PatchHistory>>(json) 
                   ?? new List<PatchHistory>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Помилка читання історії: {ex.Message}");
            return new List<PatchHistory>();
        }
    }
}