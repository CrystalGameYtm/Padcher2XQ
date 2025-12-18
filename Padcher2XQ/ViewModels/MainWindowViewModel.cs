using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using Padcher2XQ.Services;
using System.Collections.Generic;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Padcher2XQ.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    
    private readonly IWindowService _windowService;
    private readonly IFileDialogService _fileDialogService;
    private readonly PatcherService _patcherService;
    private readonly ChecksumService _checksumService;
    public ObservableCollection<string> SelectedPatches { get; } = new();
    [ObservableProperty] private string? _romPath;
    [ObservableProperty] private string? _patchPath;
    [ObservableProperty] private string? _outputPath;
    [ObservableProperty] private string? _patchPathDisplay; 
    [ObservableProperty] private bool _ignoreChecksums;
    [ObservableProperty] private bool _isMultiPatchMode;
    [ObservableProperty] private string _crc32 = "---";
    [ObservableProperty] private string _md5 = "---";
    [ObservableProperty] private string _sha1 = "---";
    [ObservableProperty] private string _fileInfoGroupName = "File Information";
    [ObservableProperty] private string _statusMessage = "Ready to patch.";
    [ObservableProperty] private string _statusMessageColor = "Gray";

    public MainWindowViewModel(
        IWindowService windowService, 
        IFileDialogService fileDialogService,
        PatcherService patcherService,
        ChecksumService checksumService)
    {
        _windowService = windowService;
        _fileDialogService = fileDialogService;
        _patcherService = patcherService;
        _checksumService = checksumService;
    }

    // Для Design-time
    public MainWindowViewModel()
    {
        _statusMessage = "Design Mode";
        
        // "Заглушки" для компілятора. 
        // null! означає "Я знаю, що це null, але не сварися".
        // Цей код ніколи не виконається під час реальної роботи програми.
        _windowService = null!;
        _fileDialogService = null!;
        _patcherService = null!;
        _checksumService = null!;
    }

    [RelayCommand]
    private async Task SelectRomFile()
    {
        string[]? path = await _fileDialogService.OpenFileAsync("Select ROM File", new[] { "*.sfc", "*.smc", "*.bin", "*.iso", "*.gba", "*.nds" });
        if (path != null)
        {
            RomPath = path.ToString();
            await CalculateRomChecksums();
            StatusMessage = "ROM loaded.";
            StatusMessageColor = "Green";
        }
    }

    private async Task CalculateRomChecksums()
    {
        if (string.IsNullOrEmpty(RomPath)) return;
        
        FileInfoGroupName = "Calculating Checksums...";
        var (c, m, s) = await _checksumService.CalculateChecksumsAsync(RomPath);
        Crc32 = c; Md5 = m; Sha1 = s;
        FileInfoGroupName = "File Information";
    }

    [RelayCommand]
    private async Task SelectPatchFile()
    {
        // Якщо включено MultiPatch, дозволяємо вибір кількох
        bool multi = IsMultiPatchMode;
        
        var paths = await _fileDialogService.OpenFileAsync(
            multi ? "Select Patches" : "Select Patch", 
            new[] { "*.ips", "*.bps", "*.asm", "*.xdelta" }, 
            multi // allowMultiple
        );

        if (paths != null && paths.Length > 0)
        {
            SelectedPatches.Clear();
            foreach (var p in paths) SelectedPatches.Add(p);

            if (paths.Length == 1)
            {
                PatchPathDisplay = paths[0];
            }
            else
            {
                PatchPathDisplay = $"{paths.Length} patches selected";
            }

            // Авто-генерація вихідного шляху
            if (!string.IsNullOrEmpty(RomPath))
            {
                string ext = Path.GetExtension(RomPath);
                string name = Path.GetFileNameWithoutExtension(RomPath);
                OutputPath = Path.Combine(Path.GetDirectoryName(RomPath)!, $"{name}_patched{ext}");
            }
        }
    }
    
    [RelayCommand]
    private async Task SelectOutputFile()
    {
        string defaultExt = "sfc";
        if (!string.IsNullOrEmpty(RomPath)) defaultExt = Path.GetExtension(RomPath).TrimStart('.');

        var path = await _fileDialogService.SaveFileAsync("Save Patched ROM", "patched_rom", defaultExt);
        if (path != null)
        {
            OutputPath = path;
        }
    }
    
    [RelayCommand]
    private async Task ApplyPatch()
    {
        if (string.IsNullOrEmpty(RomPath) || SelectedPatches.Count == 0 || string.IsNullOrEmpty(OutputPath))
        {
            StatusMessage = "Error: Missing files.";
            StatusMessageColor = "Red";
            return;
        }

        StatusMessage = "Applying patches...";
        StatusMessageColor = "DodgerBlue";

        try
        {
            if (SelectedPatches.Count == 1)
            {
                // Одиночний режим
                await _patcherService.PatchSingleFileAsync(RomPath, SelectedPatches[0], OutputPath);
            }
            else
            {
                // Multi-patch (послідовний)
                await _patcherService.ApplyMultiplePatchesAsync(RomPath, SelectedPatches.ToList(), OutputPath);
            }

            StatusMessage = "Success! All patches applied.";
            StatusMessageColor = "Green";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
            StatusMessageColor = "Red";
        }
    }
    partial void OnIsMultiPatchModeChanged(bool value)
    {
        SelectedPatches.Clear();
        PatchPathDisplay = "";
        StatusMessage = value ? "MultiPatch Mode: Select multiple patches." : "Single Mode.";
    }

    [RelayCommand]
    private void OpenSettings()
    {
        _windowService.ShowSettingsWindow();
    }
}
