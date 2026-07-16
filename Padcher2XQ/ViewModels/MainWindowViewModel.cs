using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using Padcher2XQ.Services;
using Padcher2XQ.Models;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.IO.Compression; 

namespace Padcher2XQ.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly HistoryService _historyService;
    private readonly IWindowService _windowService;
    private readonly IFileDialogService _fileDialogService;
    private readonly PatcherService _patcherService;
    private readonly ChecksumService _checksumService;
    private readonly RetroAchievementsService _raService; 
    public ObservableCollection<PatchItemViewModel> SelectedPatches { get; } = new();
    [ObservableProperty] [NotifyCanExecuteChangedFor(nameof(ApplyPatchCommand))] private string? _romPath;
    [ObservableProperty] [NotifyCanExecuteChangedFor(nameof(ApplyPatchCommand))] private string? _patchPath;
    [ObservableProperty] [NotifyCanExecuteChangedFor(nameof(ApplyPatchCommand))] private string? _outputPath;
    [ObservableProperty] private string? _patchPathDisplay; 
    [ObservableProperty] private bool _isMultiPatchMode;
    [ObservableProperty] private ObservableCollection<PatcherHistory> _historyEntries = new();
    [ObservableProperty] public string _origCrc32 = "---";
    [ObservableProperty] public string _origMd5 = "---";
    [ObservableProperty] public string _origSha1 = "---";
    [ObservableProperty] private string _patchedCrc32 = "---";
    [ObservableProperty] private string _patchedMd5 = "---";
    [ObservableProperty] private string _patchedSha1 = "---";
    [ObservableProperty] private Func<Task> _loadHistory;
    [ObservableProperty] private string _raStatus = "Waiting for patch...";
    [ObservableProperty] private string _raStatusColor = "Gray";
    public ObservableCollection<RomEntry> RomHistoryEntries { get; } = new();
    public ObservableCollection<PatchEntry> PatchHistoryEntries { get; } = new();
    [ObservableProperty] private string _statusMessage = "Ready to patch.";
    [ObservableProperty] private string _statusMessageColor = "Gray";
    [ObservableProperty] private bool _fixInternalChecksum = false;
    public MainWindowViewModel(
        IWindowService windowService, 
        IFileDialogService fileDialogService, 
        PatcherService patcherService, 
        ChecksumService checksumService,
        RetroAchievementsService raService) 
    {
        _historyService = new HistoryService();
        _windowService = windowService;
        _fileDialogService = fileDialogService;
        _patcherService = patcherService;
        _checksumService = checksumService;
        _raService = raService;
        _ = LoadHistoryAsync(); 
    }
    
    //Rom file
    
    [RelayCommand]
    private async Task SelectRomFile()
    {
        var paths = await _fileDialogService.OpenFileAsync("Select ROM File", new[] { "*.nes", "*.iso", "*.gen", "*.n64", "*.gbc", "*.md", "*.z64", "*.sfc", "*.smc", "*.bin", "*.gba", "*.nds" });
        if (paths?.FirstOrDefault() is string path_rom)
        {
            RomPath = path_rom;
            GenerateDefaultOutputPath();
            PatchedCrc32 = "---"; PatchedMd5 = "---"; PatchedSha1 = "---";
            RaStatus = "Waiting for patch..."; RaStatusColor = "Gray";
            UpdateStatus("Calculating Original Checksums...", "DodgerBlue");
            var (c, m, s) = await _checksumService.CalculateChecksumsAsync(RomPath);
            OrigCrc32 = c; OrigMd5 = m; OrigSha1 = s;
            UpdateStatus("ROM loaded.", "Green");
        }
    }
    
    //Patch File
    
    private bool CanApplyPatch() 
    {
        if (string.IsNullOrEmpty(RomPath) || string.IsNullOrEmpty(OutputPath)) return false;
        return IsMultiPatchMode ? SelectedPatches.Any() : !string.IsNullOrEmpty(PatchPath); 
    }
    [RelayCommand(CanExecute = nameof(CanApplyPatch))]
    private async Task ApplyPatch()
    {
        UpdateStatus("Applying patches...", "DodgerBlue");
        RaStatus = "Checking..."; RaStatusColor = "Goldenrod";

        try
        {
            if (IsMultiPatchMode)
            {
                var activePatches = SelectedPatches.Where(p => p.IsEnabled).Select(p => p.FilePath).ToList();
                await _patcherService.ApplyMultiplePatchesAsync(RomPath!, activePatches, OutputPath!, FixInternalChecksum);
            }
            else
            {
                await _patcherService.PatchSingleFileAsync(RomPath!, PatchPath!, OutputPath!, FixInternalChecksum);
            }
            var (c, m, s) = await _checksumService.CalculateChecksumsAsync(OutputPath!);
            PatchedCrc32 = c; PatchedMd5 = m; PatchedSha1 = s;
            UpdateStatus("Success! Patches applied.", "Green");
            await CheckRetroAchievementsAsync(m);
            await _historyService.AddEntriesAsync(RomPath, PatchPath, OrigCrc32, OrigMd5, OrigSha1);
            await LoadHistoryAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine("CRASH TRACE: " + ex.StackTrace);
            UpdateStatus($"Error: {ex.Message}", "Red");
            RaStatus = "Check Failed"; RaStatusColor = "Red";
        }
    }
    partial void OnIsMultiPatchModeChanged(bool value)
    {
        SelectedPatches.Clear();
        PatchPathDisplay = "";
        UpdateStatus(value ? "MultiPatch Mode: Select multiple patches." : "Single Mode.", "Gray");
        ApplyPatchCommand.NotifyCanExecuteChanged();
    }
    [RelayCommand]
    private async Task SelectPatchFile()
    {
        var paths = await _fileDialogService.OpenFileAsync(
            IsMultiPatchMode ? "Select Patches or ZIP" : "Select Patch or ZIP", 
            new[] { "*.ips", "*.bps", "*.asm", "*.xdelta", "*.ups", "*.zip" },
            IsMultiPatchMode);
            
        if (paths == null || paths.Length == 0) return;

        foreach (var path in paths)
        {
            if (Path.GetExtension(path).ToLower() == ".zip")
            {
                await HandleZipFileAsync(path);
            }
            else
            {
                AddPatchToList(path);
            }
        }
    }
    
    //Output File
    
    [RelayCommand]
    private async Task SelectOutputFile()
    {
        string ext = string.IsNullOrEmpty(RomPath) ? "sfc" : Path.GetExtension(RomPath).TrimStart('.');
        var path = await _fileDialogService.SaveFileAsync("Save Patched ROM", "patched_rom", ext);
        if (path != null) OutputPath = path;
    }
    private void GenerateDefaultOutputPath()
    {
        if (string.IsNullOrEmpty(RomPath)) return;
        string ext = Path.GetExtension(RomPath);
        var name = Path.GetFileNameWithoutExtension(PatchPath);
        if (string.IsNullOrEmpty(name)) name = Path.GetFileNameWithoutExtension(RomPath) + "_patched";
        string dir = Path.GetDirectoryName(RomPath) ?? string.Empty;
        OutputPath = Path.Combine(dir, $"{name}{ext}");
    }
    
    //Rom & Patcher History 
    
    public async Task LoadHistoryAsync()
    {
        var history = await _historyService.GetHistoryAsync();
    
        RomHistoryEntries.Clear();
        foreach (var rom in history.RomHistory) RomHistoryEntries.Add(rom);
    
        PatchHistoryEntries.Clear();
        foreach (var patch in history.PatchHistory) PatchHistoryEntries.Add(patch);
    }
    [RelayCommand]
    public void SelectRomHistoryItem(RomEntry entry)
    {
        if (entry == null) return;
        RomPath = entry.RomPath;
        OrigCrc32 = entry.RomCRC32 ?? "---";
        OrigMd5 = entry.RomMD5 ?? "---";
        OrigSha1 = entry.RomSHA1 ?? "---";
        PatchedCrc32 = "---"; 
        PatchedMd5 = "---"; 
        PatchedSha1 = "---";
        RaStatus = "Waiting for patch..."; 
        RaStatusColor = "Gray";
        UpdateStatus("ROM loaded from History.", "Green");
        GenerateDefaultOutputPath();
    }
    [RelayCommand]
    public void SelectPatchHistoryItem(PatchEntry entry)
    {
        if (entry == null) return;
        PatchPath = entry.PatchPath;
        OnPropertyChanged(nameof(PatchPath));
        
    }
    
    // Drag & Drop
    
    public void HandleDroppedFiles(string[] files)
    {
        var romExtensions = new[] { ".nes", ".iso", ".gen", ".n64", ".gbc", ".md", ".z64", ".sfc", ".smc", ".bin", ".gba", ".nds" };
        var patchExtensions = new[] { ".ips", ".bps", ".ups", ".xdelta", ".asm" };
        foreach (var file in files)
        {
            var ext = Path.GetExtension(file).ToLower();

            if (romExtensions.Contains(ext))
            {
                RomPath = file;
                GenerateDefaultOutputPath();
                PatchedCrc32 = "---"; PatchedMd5 = "---"; PatchedSha1 = "---";
                RaStatus = "Waiting for patch..."; RaStatusColor = "Gray";
                UpdateStatus("Calculating Original Checksums...", "DodgerBlue");
                var (c, m, s) = _checksumService.CalculateChecksumsAsync(RomPath).Result; 
                OrigCrc32 = c; OrigMd5 = m; OrigSha1 = s;
                UpdateStatus("ROM loaded via Drag&Drop.", "Green");
            }
            else if (ext == ".zip") 
            {
                _ = HandleZipFileAsync(file);
            }
            else if (patchExtensions.Contains(ext))
            {
                AddPatchToList(file);
            }
        }
    }
    private void AddPatchToList(string file)
    {
        if (IsMultiPatchMode)
        {
            if (!SelectedPatches.Any(p => p.FilePath == file))
            {
                SelectedPatches.Add(new PatchItemViewModel
                {
                    FilePath = file,
                    PatchName = Path.GetFileName(file),
                    Format = Path.GetExtension(file).TrimStart('.').ToUpper()
                });
            }
            PatchPathDisplay = $"{SelectedPatches.Count} patches selected";
        }
        else
        {
            PatchPath = file;
            PatchPathDisplay = file;
        }
        GenerateDefaultOutputPath();
        ApplyPatchCommand.NotifyCanExecuteChanged();
    }
    private async Task HandleZipFileAsync(string zipPath)
    {
        var validExtensions = new[] { ".ips", ".bps", ".ups", ".xdelta", ".asm" };
        try
        {
            using var archive = ZipFile.OpenRead(zipPath); 
            var patchEntries = archive.Entries
                .Where(e => validExtensions.Contains(Path.GetExtension(e.FullName).ToLower()))
                .ToList();

            if (patchEntries.Count == 0)
            {
                UpdateStatus("No valid patches found in ZIP.", "IndianRed");
                return;
            }
            
            ZipArchiveEntry? selectedEntry = null;

            if (patchEntries.Count == 1)
            {
                selectedEntry = patchEntries[0];
            }
            else
            {
                var entryNames = patchEntries.Select(e => e.FullName).ToList();
                string? selectedName = await _windowService.ShowSelectZipAsync(entryNames);
                
                if (string.IsNullOrEmpty(selectedName)) return; 
                
                selectedEntry = patchEntries.First(e => e.FullName == selectedName);
            }

            string extractPath = Path.Combine(Path.GetTempPath(), selectedEntry.Name);
            
            selectedEntry.ExtractToFile(extractPath, overwrite: true);

            AddPatchToList(extractPath);
            UpdateStatus($"Loaded {selectedEntry.Name} from ZIP.", "LimeGreen");
        }
        catch (Exception ex)
        {
            UpdateStatus($"ZIP Error: {ex.Message}", "Red");
        }
    }
    
    //Utils
   
    private async Task CheckRetroAchievementsAsync(string md5)
    {
        RaStatus = "Contacting RA Servers...";
        var (isSupported, resultText) = await _raService.CheckHashSupportAsync(md5);

        if (isSupported)
        {
            RaStatus = $"✓ Supported: {resultText}";
            RaStatusColor = "LimeGreen";
        }
        else
        {
            RaStatus = $"❌ {resultText}"; 
            RaStatusColor = "IndianRed";
        }
    }
    
    private void UpdateStatus(string message, string color)
    {
        StatusMessage = message;
        StatusMessageColor = color;
    }
    [RelayCommand]
    private void OpenSettings() => _windowService.ShowSettingsWindow();
    // Launcher Mover
    [RelayCommand]
    private void MovePatchUp(PatchItemViewModel item)
    {
        int index = SelectedPatches.IndexOf(item);
        if (index > 0) SelectedPatches.Move(index, index - 1);
    }

    [RelayCommand]
    private void MovePatchDown(PatchItemViewModel item)
    {
        int index = SelectedPatches.IndexOf(item);
        if (index >= 0 && index < SelectedPatches.Count - 1) SelectedPatches.Move(index, index + 1);
    }

    [RelayCommand]
    private void RemovePatch(PatchItemViewModel item)
    {
        SelectedPatches.Remove(item);
        ApplyPatchCommand.NotifyCanExecuteChanged();
    }
}
