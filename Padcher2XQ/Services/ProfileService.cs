using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Padcher2XQ.Models;

namespace Padcher2XQ.Services;

public class ProfileService
{
    private readonly string _configDir;
    private readonly string _sessionFile;
    private readonly string _profilesDir;

    public ProfileService()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        _configDir = Path.Combine(appData, "Padcher2XQ");
        _sessionFile = Path.Combine(_configDir, "last_session.json");
        _profilesDir = Path.Combine(_configDir, "Profiles");

        Directory.CreateDirectory(_configDir);
        Directory.CreateDirectory(_profilesDir);
    }

    
    public async Task SaveLastSessionAsync(IEnumerable<PatchProfileItem> patches)
    {
        try
        {
            var profile = new PatchProfile { Name = "LastSession", Patches = new List<PatchProfileItem>(patches) };
            var json = JsonSerializer.Serialize(profile, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(_sessionFile, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to save session: {ex.Message}");
        }
    }

    public async Task<List<PatchProfileItem>?> LoadLastSessionAsync()
    {
        if (!File.Exists(_sessionFile)) return null;

        try
        {
            var json = await File.ReadAllTextAsync(_sessionFile);
            var profile = JsonSerializer.Deserialize<PatchProfile>(json);
            return profile?.Patches;
        }
        catch
        {
            return null;
        }
    }

    
    public async Task SaveProfileAsync(string name, IEnumerable<PatchProfileItem> patches)
    {
        var safeName = string.Join("_", name.Split(Path.GetInvalidFileNameChars()));
        var filePath = Path.Combine(_profilesDir, $"{safeName}.json");

        var profile = new PatchProfile { Name = name, Patches = new List<PatchProfileItem>(patches) };
        var json = JsonSerializer.Serialize(profile, new JsonSerializerOptions { WriteIndented = true });
        
        await File.WriteAllTextAsync(filePath, json);
    }

    public async Task<PatchProfile?> LoadProfileAsync(string filePath)
    {
        if (!File.Exists(filePath)) return null;

        var json = await File.ReadAllTextAsync(filePath);
        return JsonSerializer.Deserialize<PatchProfile>(json);
    }
}