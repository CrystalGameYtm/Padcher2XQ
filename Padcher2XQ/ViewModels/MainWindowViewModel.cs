using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using Padcher2XQ.Services;
using Padcher2XQ.Models;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System;
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
    public ObservableCollection<string> SelectedPatches { get; } = new();
    [ObservableProperty] [NotifyCanExecuteChangedFor(nameof(ApplyPatchCommand))] private string? _romPath;
    [ObservableProperty] [NotifyCanExecuteChangedFor(nameof(ApplyPatchCommand))] private string? _patchPath;
    [ObservableProperty] [NotifyCanExecuteChangedFor(nameof(ApplyPatchCommand))] private string? _outputPath;
    [ObservableProperty] private string? _patchPathDisplay; 
    [ObservableProperty] private bool _isMultiPatchMode;
    [ObservableProperty] private ObservableCollection<PatcherHistory> _historyEntries = new();
    [ObservableProperty] private string _origCrc32 = "---";
    [ObservableProperty] private string _origMd5 = "---";
    [ObservableProperty] private string _origSha1 = "---";

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
    
        // ОСЬ ТУТ: Запускаємо завантаження історії при старті програми!
        _ = LoadHistoryAsync(); 
    }
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
        OnPropertyChanged(nameof(RomPath));
    }
    [RelayCommand]
    public void SelectPatchHistoryItem(PatchEntry entry)
    {
        if (entry == null) return;
        PatchPath = entry.PatchPath;
        OnPropertyChanged(nameof(PatchPath));
    }
    
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
    
    
    [RelayCommand]
    private async Task SelectOutputFile()
    {
        string ext = string.IsNullOrEmpty(RomPath) ? "sfc" : Path.GetExtension(RomPath).TrimStart('.');
        var path = await _fileDialogService.SaveFileAsync("Save Patched ROM", "patched_rom", ext);
        if (path != null) OutputPath = path;
    }
    
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
                await _patcherService.ApplyMultiplePatchesAsync(RomPath!, SelectedPatches.ToList(), OutputPath!, true); // true = відновлювати SNES чексуму
            else
                await _patcherService.PatchSingleFileAsync(RomPath!, PatchPath!, OutputPath!, true);

            var (c, m, s) = await _checksumService.CalculateChecksumsAsync(OutputPath!);
            PatchedCrc32 = c; PatchedMd5 = m; PatchedSha1 = s;
           
            UpdateStatus("Success! Patches applied.", "Green");
            await CheckRetroAchievementsAsync(m);
            _historyService.AddEntriesAsync(RomPath, PatchPath);
            await LoadHistoryAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine("CRASH TRACE: " + ex.StackTrace);
            UpdateStatus($"Error: {ex.Message}", "Red");
            RaStatus = "Check Failed"; RaStatusColor = "Red";
        }
        
    }

    private async Task CheckRetroAchievementsAsync(string md5)
    {
        RaStatus = "Contacting RA Servers...";
        var (isSupported, gameTitle) = await _raService.CheckHashSupportAsync(md5);

        if (isSupported)
        {
            RaStatus = $"✓ Supported: {gameTitle}";
            RaStatusColor = "LimeGreen";
        }
        else
        {
            RaStatus = "❌ Hash not found in RA Database";
            RaStatusColor = "IndianRed";
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
    private void OpenSettings() => _windowService.ShowSettingsWindow();

    

    private void GenerateDefaultOutputPath()
    {
        if (string.IsNullOrEmpty(RomPath)) return;
        string ext = Path.GetExtension(RomPath);
        var name = Path.GetFileNameWithoutExtension(PatchPath);
        if (string.IsNullOrEmpty(name)) name = Path.GetFileNameWithoutExtension(RomPath) + "_patched";
        string dir = Path.GetDirectoryName(RomPath) ?? string.Empty;
        OutputPath = Path.Combine(dir, $"{name}{ext}");
    }

    private void UpdateStatus(string message, string color)
    {
        StatusMessage = message;
        StatusMessageColor = color;
    }
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

    private void AddPatchToList(string file)
    {
        if (IsMultiPatchMode)
        {
            if (!SelectedPatches.Contains(file)) SelectedPatches.Add(file);
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
            await using var archive = ZipFile.OpenRead(zipPath);
            
            var patchEntries = archive.Entries
                .Where(e => validExtensions.Contains(Path.GetExtension(e.FullName).ToLower()))
                .ToList();

            if (patchEntries.Count == 0)
            {
                UpdateStatus("No valid patches found in ZIP.", "IndianRed");
                return;
            }
            ZipArchiveEntry? selectedEntry = null;

            string? selectedName;
            if (patchEntries.Count == 1)
            {
                selectedEntry = patchEntries[0];
            }
            else
            {

                var entryNames = patchEntries.Select(e => e.FullName).ToList();
                selectedName = _windowService.ShowSelectZip();
                
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
}
