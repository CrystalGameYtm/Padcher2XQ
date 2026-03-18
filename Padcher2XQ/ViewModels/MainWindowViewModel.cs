using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using Padcher2XQ.Services;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace Padcher2XQ.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly IWindowService _windowService;
    private readonly IFileDialogService _fileDialogService;
    private readonly PatcherService _patcherService;
    private readonly ChecksumService _checksumService;

    public ObservableCollection<string> SelectedPatches { get; } = new();

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ApplyPatchCommand))]
    private string? _romPath;

    [ObservableProperty] 
    [NotifyCanExecuteChangedFor(nameof(ApplyPatchCommand))]
    private string? _outputPath;

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

    [RelayCommand]
    private async Task SelectRomFile()
    {
        var paths = await _fileDialogService.OpenFileAsync("Select ROM File", new[] { "*.sfc", "*.smc", "*.bin", "*.iso", "*.gba", "*.nds" });
        if (paths?.FirstOrDefault() is string path)
        {
            RomPath = path;
            GenerateDefaultOutputPath();
            await CalculateRomChecksums();
            UpdateStatus("ROM loaded.", "Green");
        }
    }

    [RelayCommand]
    private async Task SelectPatchFile()
    {
        var paths = await _fileDialogService.OpenFileAsync(
            IsMultiPatchMode ? "Select Patches" : "Select Patch", 
            new[] { "*.ips", "*.bps", "*.asm", "*.xdelta" "*.ups" }, 
            IsMultiPatchMode
        );

        if (paths != null && paths.Length > 0)
        {
            SelectedPatches.Clear();
            foreach (var p in paths) SelectedPatches.Add(p);

            PatchPathDisplay = paths.Length == 1 ? paths[0] : $"{paths.Length} patches selected";
            GenerateDefaultOutputPath();
            
            // Оновлюємо стан команди
            ApplyPatchCommand.NotifyCanExecuteChanged();
        }
    }
    
    [RelayCommand]
    private async Task SelectOutputFile()
    {
        string ext = string.IsNullOrEmpty(RomPath) ? "sfc" : Path.GetExtension(RomPath).TrimStart('.');
        var path = await _fileDialogService.SaveFileAsync("Save Patched ROM", "patched_rom", ext);
        
        if (path != null) OutputPath = path;
    }
    
    // Кнопка буде активною тільки тоді, коли цей метод повертає true
    private bool CanApplyPatch() => !string.IsNullOrEmpty(RomPath) && SelectedPatches.Any() && !string.IsNullOrEmpty(OutputPath);

    [RelayCommand(CanExecute = nameof(CanApplyPatch))]
    private async Task ApplyPatch()
    {
        UpdateStatus("Applying patches...", "DodgerBlue");

        try
        {
            if (SelectedPatches.Count == 1)
            {
                await _patcherService.PatchSingleFileAsync(RomPath!, SelectedPatches[0], OutputPath!);
            }
            else
            {
                await _patcherService.ApplyMultiplePatchesAsync(RomPath!, SelectedPatches.ToList(), OutputPath!);
            }

            UpdateStatus("Success! All patches applied.", "Green");
        }
        catch (Exception ex)
        {
            UpdateStatus($"Error: {ex.Message}", "Red");
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

    private async Task CalculateRomChecksums()
    {
        if (string.IsNullOrEmpty(RomPath)) return;
        
        FileInfoGroupName = "Calculating Checksums...";
        var (c, m, s) = await _checksumService.CalculateChecksumsAsync(RomPath);
        Crc32 = c; Md5 = m; Sha1 = s;
        FileInfoGroupName = "File Information";
    }

    private void GenerateDefaultOutputPath()
    {
        if (string.IsNullOrEmpty(RomPath) || SelectedPatches.Count == 0) return;
        
        string ext = Path.GetExtension(RomPath);
        string name = Path.GetFileNameWithoutExtension(RomPath);
        string dir = Path.GetDirectoryName(RomPath) ?? string.Empty;
        
        OutputPath = Path.Combine(dir, $"{name}_patched{ext}");
    }

    private void UpdateStatus(string message, string color)
    {
        StatusMessage = message;
        StatusMessageColor = color;
    }
}