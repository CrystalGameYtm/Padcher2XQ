using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Diagnostics;

namespace Padcher2XQ.Services;

public class PatcherService
{
    private readonly Dictionary<string, Func<string, string, string, Task>> _patchStrategies;

    public PatcherService()
    {
        // Реєструємо всі підтримувані формати
        _patchStrategies = new Dictionary<string, Func<string, string, string, Task>>(StringComparer.OrdinalIgnoreCase)
        {
            { ".ips", ApplyIpsPatchAsync },
            { ".bps", ApplyBpsPatchAsync },
            { ".asm", ApplyAsmPatchAsync },
            {".ups", }
            { ".xdelta", ApplyXdeltaPatchAsync }
        };
    }

    public async Task PatchSingleFileAsync(string romPath, string patchPath, string outputPath)
    {
        string ext = Path.GetExtension(patchPath);
        
        if (_patchStrategies.TryGetValue(ext, out var patchFunc))
        {
            await patchFunc(romPath, patchPath, outputPath);
        }
        else
        {
            throw new NotSupportedException($"Format '{ext}' is not supported.");
        }
    }

    public async Task ApplyMultiplePatchesAsync(string romPath, List<string> patchPaths, string finalOutputPath)
    {
        string tempFile1 = Path.Combine(Path.GetTempPath(), $"padcher_temp1_{Guid.NewGuid()}.sfc");
        string tempFile2 = Path.Combine(Path.GetTempPath(), $"padcher_temp2_{Guid.NewGuid()}.sfc");
        
        File.Copy(romPath, tempFile1, true);
        string currentSource = tempFile1;
        string currentTarget = tempFile2;

        try
        {
            for (int i = 0; i < patchPaths.Count; i++)
            {
                currentTarget = (i % 2 == 0) ? tempFile2 : tempFile1;
                currentSource = (i % 2 == 0) ? tempFile1 : tempFile2;

                await PatchSingleFileAsync(currentSource, patchPaths[i], currentTarget);
            }

            if (File.Exists(finalOutputPath)) File.Delete(finalOutputPath);
            File.Move(currentTarget, finalOutputPath);
        }
        finally
        {
            if (File.Exists(tempFile1)) File.Delete(tempFile1);
            if (File.Exists(tempFile2)) File.Delete(tempFile2);
        }
    }

    private async Task ApplyAsmPatchAsync(string romPath, string patchPath, string outputPath)
    {
        if (!File.Exists("asar.exe"))
            throw new FileNotFoundException("asar.exe not found! Please place it in the app directory.");

        File.Copy(romPath, outputPath, true);

        var startInfo = new ProcessStartInfo
        {
            FileName = "asar.exe",
            Arguments = $"\"{patchPath}\" \"{outputPath}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(startInfo);
        if (process == null) throw new Exception("Failed to start asar.exe process.");
        
        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            string error = await process.StandardError.ReadToEndAsync();
            throw new Exception($"Asar Error: {error}");
        }
    }
    private async Task ApplyUpsPatchAsync(string romPath, string patchPath, string outputPath)
    {
        await Task.Run(() =>
        {
            byte[] sourceData = File.ReadAllBytes(romPath);
            byte[] patchData = File.ReadAllBytes(patchPath);

            // Перевірка заголовка "UPS1"
            if (patchData.Length < 16 || Encoding.ASCII.GetString(patchData, 0, 4) != "UPS1")
                throw new Exception("Invalid UPS file header.");

            int patchOffset = 4;

            // Декодуємо розміри (використовуємо існуючий метод для VLI)
            ulong sourceSize = DecodeBpsNumber(patchData, ref patchOffset);
            ulong targetSize = DecodeBpsNumber(patchData, ref patchOffset);

            byte[] targetData = new byte[targetSize];
        
            Array.Copy(sourceData, targetData, Math.Min((long)sourceSize, (long)targetSize));

            long romOffset = 0;

            while (patchOffset < patchData.Length - 12)
            {
                romOffset += (long)DecodeBpsNumber(patchData, ref patchOffset);
            
                while (true)
                {
                    byte xorByte = patchData[patchOffset++];
                
                    if (xorByte == 0) break;
                
                    targetData[romOffset] ^= xorByte;
                    romOffset++;
                }
                romOffset++;
            }

            File.WriteAllBytes(outputPath, targetData);
        });
    }
    private async Task ApplyXdeltaPatchAsync(string romPath, string patchPath, string outputPath)
    {
        if (!File.Exists("xdelta3.exe"))
            throw new FileNotFoundException("xdelta3.exe not found! Please place it in the app directory.");

        var startInfo = new ProcessStartInfo
        {
            FileName = "xdelta3.exe",
            Arguments = $"-d -f -s \"{romPath}\" \"{patchPath}\" \"{outputPath}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(startInfo);
        if (process == null) throw new Exception("Failed to start xdelta3.exe process.");
        
        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            string error = await process.StandardError.ReadToEndAsync();
            if (string.IsNullOrWhiteSpace(error)) error = await process.StandardOutput.ReadToEndAsync();
            throw new Exception($"Xdelta Error (Code {process.ExitCode}): {error}");
        }
    }
    private async Task ApplyIpsPatchAsync(string romPath, string patchPath, string outputPath) { /* ... Ваш існуючий код ... */ }
    private async Task ApplyBpsPatchAsync(string romPath, string patchPath, string outputPath) { /* ... Ваш існуючий код ... */ }
}