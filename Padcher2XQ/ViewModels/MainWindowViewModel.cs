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

    [ObservableProperty] [NotifyCanExecuteChangedFor(nameof(ApplyPatchCommand))] private string? _romPath;
    [ObservableProperty] [NotifyCanExecuteChangedFor(nameof(ApplyPatchCommand))] private string? _patchPath;
    [ObservableProperty] [NotifyCanExecuteChangedFor(nameof(ApplyPatchCommand))] private string? _outputPath;

    [ObservableProperty] private string? _patchPathDisplay; 
    [ObservableProperty] private bool _ignoreChecksums;
    [ObservableProperty] private bool _isMultiPatchMode;
    
    // ЄДИНА ЗМІННА ЧЕКБОКСА (саме вона оживить кнопку на гіфці)
    [ObservableProperty] private bool _isOriginalChecksum = true;
    
    [ObservableProperty] private string _crc32 = "---";
    [ObservableProperty] private string _md5 = "---";
    [ObservableProperty] private string _sha1 = "---";
    
    // Кеш для пропатченого файлу
    private string _patchedCrc32 = "---";
    private string _patchedMd5 = "---";
    private string _patchedSha1 = "---";
    
    [ObservableProperty] private string _fileInfoGroupName = "File Information";
    [ObservableProperty] private string _statusMessage = "Ready to patch.";
    [ObservableProperty] private string _statusMessageColor = "Gray";

    public MainWindowViewModel(IWindowService windowService, IFileDialogService fileDialogService, PatcherService patcherService, ChecksumService checksumService)
    {
        _windowService = windowService;
        _fileDialogService = fileDialogService;
        _patcherService = patcherService;
        _checksumService = checksumService;
    }

    [RelayCommand]
    private async Task SelectRomFile()
    {
        var paths = await _fileDialogService.OpenFileAsync("Select ROM File", new[] { "*.nes", "*.iso", "*.gen", "*.n64", "*.gbc", "*.md", "*.z64", "*.sfc", "*.smc", "*.bin", "*.gba", "*.nds" });
        if (paths?.FirstOrDefault() is string path_rom)
        {
            RomPath = path_rom;
            GenerateDefaultOutputPath();
            
            FileInfoGroupName = "Calculating Checksums...";
            var (c, m, s) = await _checksumService.CalculateChecksumsAsync(RomPath);
            
            _checksumService.SetOriginalChecksums(c, m, s);
            _patchedCrc32 = "---"; _patchedMd5 = "---"; _patchedSha1 = "---";
            
            // Завжди ставимо галочку для нового РОМу
            IsOriginalChecksum = true;
            UpdateDisplayFromOriginal();
            UpdateStatus("ROM loaded.", "Green");
        }
    }

    [RelayCommand]
    private async Task SelectPatchFile()
    {
        var paths = await _fileDialogService.OpenFileAsync(IsMultiPatchMode ? "Select Patches" : "Select Patch", new[] { "*.ips", "*.bps", "*.asm", "*.xdelta", "*.ups" }, IsMultiPatchMode );
        if (paths?.FirstOrDefault() is string path_patch) PatchPath = path_patch; 
      
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
    
    private bool CanApplyPatch() 
    {
        if (string.IsNullOrEmpty(RomPath) || string.IsNullOrEmpty(OutputPath)) return false;
        return IsMultiPatchMode ? SelectedPatches.Any() : !string.IsNullOrEmpty(PatchPath);
    }

    [RelayCommand(CanExecute = nameof(CanApplyPatch))]
    private async Task ApplyPatch()
    {
        UpdateStatus("Applying patches...", "DodgerBlue");
        try
        {
            // Передаємо стан чекбокса у патчер (щоб він знав, чи відновлювати чексуму SNES)
            if (IsMultiPatchMode)
                await _patcherService.ApplyMultiplePatchesAsync(RomPath!, SelectedPatches.ToList(), OutputPath!, IsOriginalChecksum);
            else
                await _patcherService.PatchSingleFileAsync(RomPath!, PatchPath!, OutputPath!, IsOriginalChecksum);

            // Рахуємо нові хеші після патчінгу
            var (c, m, s) = await _checksumService.CalculateChecksumsAsync(OutputPath!);
            _patchedCrc32 = c; _patchedMd5 = m; _patchedSha1 = s;

            // Якщо чекбокс вимкнений - показуємо нові хеші
            if (!IsOriginalChecksum)
            {
                Crc32 = _patchedCrc32;
                Md5 = _patchedMd5;
                Sha1 = _patchedSha1;
                FileInfoGroupName = "File Information (Patched ROM)";
            }
            
            UpdateStatus("Success! Patches applied.", "Green");
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

    // Реакція на клацання чекбокса в UI
    partial void OnIsOriginalChecksumChanged(bool value)
    {
        if (value)
        {
            UpdateDisplayFromOriginal();
        }
        else
        {
            if (_patchedCrc32 == "---")
            {
                Crc32 = _checksumService.OriginalCrc32;
                Md5 = _checksumService.OriginalMd5;
                Sha1 = _checksumService.OriginalSha1;
                FileInfoGroupName = "File Information (Original - Not Patched)";
            }
            else
            {
                Crc32 = _patchedCrc32;
                Md5 = _patchedMd5;
                Sha1 = _patchedSha1;
                FileInfoGroupName = "File Information (Patched ROM)";
            }
        }
    }

    private void UpdateDisplayFromOriginal()
    {
        Crc32 = _checksumService.OriginalCrc32;
        Md5 = _checksumService.OriginalMd5;
        Sha1 = _checksumService.OriginalSha1;
        FileInfoGroupName = "File Information (Original ROM)";
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

    private void UpdateStatus(string message, string color)
    {
        StatusMessage = message;
        StatusMessageColor = color;
    }
}