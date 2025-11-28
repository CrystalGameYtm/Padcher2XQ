using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Padcher2XQ.Services;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Padcher2XQ.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly IWindowService _windowService;
    private readonly IFileDialogService _fileDialogService;
    private readonly PatcherService _patcherService;
    private readonly ChecksumService _checksumService;

    [ObservableProperty] private string? _romPath;
    [ObservableProperty] private string? _patchPath;
    [ObservableProperty] private string? _outputPath;
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
    }

    [RelayCommand]
    private async Task SelectRomFile()
    {
        var path = await _fileDialogService.OpenFileAsync("Select ROM File", new[] { "*.sfc", "*.smc", "*.bin", "*.iso", "*.gba", "*.nds" });
        if (path != null)
        {
            RomPath = path;
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
        var path = await _fileDialogService.OpenFileAsync("Select Patch", new[] { "*.ips", "*.bps" });
        if (path != null)
        {
            PatchPath = path;
            // Автоматично пропонуємо ім'я вихідного файлу
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
        if (string.IsNullOrEmpty(RomPath) || string.IsNullOrEmpty(PatchPath) || string.IsNullOrEmpty(OutputPath))
        {
            StatusMessage = "Error: Please select all files.";
            StatusMessageColor = "Red";
            return;
        }

        StatusMessage = "Applying patch...";
        StatusMessageColor = "DodgerBlue";

        try
        {
            string patchExt = Path.GetExtension(PatchPath).ToLower();

            if (patchExt == ".ips")
            {
                await _patcherService.ApplyIpsPatchAsync(RomPath, PatchPath, OutputPath);
            }
            else if (patchExt == ".bps")
            {
                await _patcherService.ApplyBpsPatchAsync(RomPath, PatchPath, OutputPath);
            }
            else
            {
                throw new Exception("Unknown patch format.");
            }

            StatusMessage = "Success! Patch applied.";
            StatusMessageColor = "Green";
            
            // Оновити інфо, щоб показати хеші нового файлу (опціонально)
            // await CalculateChecksumsForOutput(); 
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
            StatusMessageColor = "Red";
        }
    }

    [RelayCommand]
    private void OpenSettings()
    {
        _windowService.ShowSettingsWindow();
    }
}
