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
    private string? _patchPath;
    [ObservableProperty] 
    [NotifyCanExecuteChangedFor(nameof(ApplyPatchCommand))]
    private string? _outputPath;

    [ObservableProperty] private string? _patchPathDisplay; 
    [ObservableProperty] private bool _ignoreChecksums;
    [ObservableProperty] private bool _isMultiPatchMode;
    [ObservableProperty] private bool _isOriginalChecksum = true;
    [ObservableProperty] private string _crc32 = "---";
    [ObservableProperty] private string _md5 = "---";
    [ObservableProperty] private string _sha1 = "---";
    private string _patchedCrc32 = "---";
    private string _patchedMd5 = "---";
    private string _patchedSha1 = "---";
    
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
        var paths = await _fileDialogService.OpenFileAsync("Select ROM File", new[]
        {
            "*.nes", "*.iso", "*.gen", "*.n64", "*.gbc", "*.md", "*.z64", "*.sfc", "*.smc", "*.bin", "*.iso", "*.gba", "*.nds"
        });
        if (paths?.FirstOrDefault() is string path_rom)
        {
            RomPath = path_rom;
            GenerateDefaultOutputPath();
            FileInfoGroupName = "Calculate Checksums...";
            var (c, m, s) = await _checksumService.CalculateChecksumsAsync(RomPath);
            _checksumService.SetOriginalChecksums(c,m,s);
            _patchedCrc32 = "---"; _patchedMd5 = "---"; _patchedSha1 = "---";
            IsOriginalChecksum = true;
            CalculateRomChecksums(c, m, s, "File Information (Original ROM)");
            UpdateStatus("ROM loaded.", "Green");
        }
    }

    [RelayCommand]
    private async Task SelectPatchFile()
    {
        var paths = await _fileDialogService.OpenFileAsync(
            IsMultiPatchMode ? "Select Patches" : "Select Patch", new[] { "*.ips", "*.bps", "*.asm", "*.xdelta", "*.ups" }, IsMultiPatchMode );
            if (paths?.FirstOrDefault() is string path_patch)
            {
                PatchPath = path_patch; 
            }
      

        if (paths != null && paths.Length > 0)
        {
            SelectedPatches.Clear();
            foreach (var p in paths) SelectedPatches.Add(p);

            PatchPathDisplay = paths.Length == 1 ? paths[0] : $"{paths.Length} patches selected";
            GenerateDefaultOutputPath();
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

            FileInfoGroupName = "Calculate Checksums...";
            var (c, m, s) = await _checksumService.CalculateChecksumsAsync(RomPath);
            _patchedCrc32 = "---"; _patchedMd5 = "---"; _patchedSha1 = "---";
            IsOriginalChecksum = false;
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

    partial void OnIsOriginalChecksumChanged(bool value)
    {
        if (value)
        {
            CalculateRomChecksums(
                _checksumService.OriginalCrc32, 
                _checksumService.OriginalMd5, 
                _checksumService.OriginalSha1, 
                "File Information (Original ROM)");
        }
        else
        {
            CalculateRomChecksums(
                _patchedCrc32, 
                _patchedMd5, 
                _patchedSha1, 
                "File Information (Patched ROM)");
        }
    }
    private async Task CalculateRomChecksums(string crc, string md5, string sha1, string header)
    {
        Crc32 = crc; 
        Md5 = md5; 
        Sha1 = sha1;
        FileInfoGroupName = header;
    }

    private void GenerateDefaultOutputPath()
    {
        if (string.IsNullOrEmpty(RomPath) || SelectedPatches.Count == 0) return;
        
        string ext = Path.GetExtension(RomPath);
        var name = Path.GetFileNameWithoutExtension(PatchPath);
        string dir = Path.GetDirectoryName(RomPath) ?? string.Empty;
        
        OutputPath = Path.Combine(dir, $"{name}{ext}");
    }

    private void UpdateStatus(string message, string color)
    {
        StatusMessage = message;
        StatusMessageColor = color;
    }
}