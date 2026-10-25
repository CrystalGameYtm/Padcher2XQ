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
    private readonly SettingsService _settings;

    public PatcherService(SettingsService settings)
    {
        _settings = settings;
        _patchStrategies = new Dictionary<string, Func<string, string, string, Task>>(StringComparer.OrdinalIgnoreCase)
        {
            { ".ips", ApplyIpsPatchAsync },
            { ".bps", ApplyBpsPatchAsync },
            { ".ups", ApplyUpsPatchAsync },
            { ".asm", ApplyAsmPatchAsync },
            { ".xdelta", ApplyXdeltaPatchAsync }
        };
    }

    public async Task PatchSingleFileAsync(string romPath, string patchPath, string outputPath, bool restoreInternalChecksum = false)
    {
        string ext = Path.GetExtension(patchPath);
        
        if (_patchStrategies.TryGetValue(ext, out var patchFunc))
        {
            await patchFunc(romPath, patchPath, outputPath);

            if (restoreInternalChecksum)
            {
                await RestoreInternalChecksumAsync(romPath, outputPath);
            }
        }
        else
        {
            throw new NotSupportedException($"Format '{ext}' is not supported.");
        }
    }

    public async Task ApplyMultiplePatchesAsync(string romPath, List<string> patchPaths, string finalOutputPath, bool restoreInternalChecksum = false)
    {
        if (patchPaths.Count == 0)
            throw new InvalidOperationException("No patches to apply.");

        string tempExt = Path.GetExtension(romPath);
        string tempFile1 = Path.Combine(Path.GetTempPath(), $"padcher_temp1_{Guid.NewGuid()}{tempExt}");
        string tempFile2 = Path.Combine(Path.GetTempPath(), $"padcher_temp2_{Guid.NewGuid()}{tempExt}");

        File.Copy(romPath, tempFile1, true);
        string currentSource = tempFile1;
        string currentTarget = tempFile2;

        try
        {
            for (int i = 0; i < patchPaths.Count; i++)
            {
                currentTarget = (i % 2 == 0) ? tempFile2 : tempFile1;
                currentSource = (i % 2 == 0) ? tempFile1 : tempFile2;
                await PatchSingleFileAsync(currentSource, patchPaths[i], currentTarget, false);
            }
            if (File.Exists(finalOutputPath)) File.Delete(finalOutputPath);
            File.Move(currentTarget, finalOutputPath);
            if (restoreInternalChecksum)
            {
                await RestoreInternalChecksumAsync(romPath, finalOutputPath);
            }
        }
        finally
        {
            if (File.Exists(tempFile1)) File.Delete(tempFile1);
            if (File.Exists(tempFile2)) File.Delete(tempFile2);
        }
    }

    private async Task RestoreInternalChecksumAsync(string originalRomPath, string patchedRomPath)
    {
        await Task.Run(() =>
        {
            try
            {
                if (!File.Exists(originalRomPath) || !File.Exists(patchedRomPath)) return;
                byte[] origBytes = File.ReadAllBytes(originalRomPath);
                byte[] patchedBytes = File.ReadAllBytes(patchedRomPath);
                int headerOffset = (origBytes.Length % 0x400 == 0x200) ? 0x200 : 0;
                bool checksumRestored = false;
                int loRomInverse = headerOffset + 0x7FDC;
                int loRomCheck = headerOffset + 0x7FDE;
                if (origBytes.Length > loRomCheck + 1 && patchedBytes.Length > loRomCheck + 1)
                {
                    if ((origBytes[loRomInverse] ^ origBytes[loRomCheck]) == 0xFF &&
                        (origBytes[loRomInverse + 1] ^ origBytes[loRomCheck + 1]) == 0xFF)
                    {
                        patchedBytes[loRomInverse] = origBytes[loRomInverse];
                        patchedBytes[loRomInverse + 1] = origBytes[loRomInverse + 1];
                        patchedBytes[loRomCheck] = origBytes[loRomCheck];
                        patchedBytes[loRomCheck + 1] = origBytes[loRomCheck + 1];
                        checksumRestored = true;
                    }
                }
                int hiRomInverse = headerOffset + 0xFFDC;
                int hiRomCheck = headerOffset + 0xFFDE;
                if (!checksumRestored && origBytes.Length > hiRomCheck + 1 && patchedBytes.Length > hiRomCheck + 1)
                {
                    if ((origBytes[hiRomInverse] ^ origBytes[hiRomCheck]) == 0xFF &&
                        (origBytes[hiRomInverse + 1] ^ origBytes[hiRomCheck + 1]) == 0xFF)
                    {
                        patchedBytes[hiRomInverse] = origBytes[hiRomInverse];
                        patchedBytes[hiRomInverse + 1] = origBytes[hiRomInverse + 1];
                        patchedBytes[hiRomCheck] = origBytes[hiRomCheck];
                        patchedBytes[hiRomCheck + 1] = origBytes[hiRomCheck + 1];
                        checksumRestored = true;
                    }
                }
                int genCheck = 0x18E;
                if (!checksumRestored && origBytes.Length > genCheck + 1 && patchedBytes.Length > genCheck + 1)
                {
                    if (origBytes[0x100] == 'S' && origBytes[0x101] == 'E' && origBytes[0x102] == 'G' && origBytes[0x103] == 'A')
                    {
                        patchedBytes[genCheck] = origBytes[genCheck];
                        patchedBytes[genCheck + 1] = origBytes[genCheck + 1];
                        checksumRestored = true;
                    }
                }
                if (checksumRestored)
                {
                    File.WriteAllBytes(patchedRomPath, patchedBytes);
                }
            }
            catch (Exception) { }
        });
    }

    private async Task ApplyAsmPatchAsync(string romPath, string patchPath, string outputPath)
    {
        string asarPath = ResolveTool(_settings.Config.AsarPath, "asar");
        File.Copy(romPath, outputPath, true);
        await RunToolAsync(asarPath, new[] { patchPath, outputPath }, "Asar");
    }

    /// <summary>
    /// Empty setting -> "asar" / "xdelta3" (+ ".exe" on Windows).
    /// Old configs stored "asar.exe" / "xdelta3.exe" as default, which does not exist on Linux/macOS.
    /// </summary>
    private static string ResolveTool(string? configured, string baseName)
    {
        if (string.IsNullOrWhiteSpace(configured))
            return OperatingSystem.IsWindows() ? baseName + ".exe" : baseName;

        if (!OperatingSystem.IsWindows() &&
            string.Equals(configured, baseName + ".exe", StringComparison.OrdinalIgnoreCase))
            return baseName;

        return configured;
    }

    /// <summary>
    /// Runs an external tool. stdout/stderr are read concurrently with waiting for exit,
    /// otherwise the process can block forever once its pipe buffer fills up.
    /// ArgumentList avoids quoting problems with paths that contain quotes/spaces.
    /// </summary>
    private static async Task RunToolAsync(string exePath, IEnumerable<string> args, string toolName)
    {
        if (Path.IsPathRooted(exePath) && !File.Exists(exePath))
            throw new FileNotFoundException($"{toolName} executable not found at: {exePath}");

        var startInfo = new ProcessStartInfo
        {
            FileName = exePath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        foreach (var arg in args) startInfo.ArgumentList.Add(arg);

        Process process;
        try
        {
            process = Process.Start(startInfo) ?? throw new Exception($"Failed to start {toolName}.");
        }
        catch (System.ComponentModel.Win32Exception)
        {
            throw new FileNotFoundException(
                $"{toolName} was not found ('{exePath}'). Set its path in Settings or add it to PATH.");
        }

        using (process)
        {
            var stdoutTask = process.StandardOutput.ReadToEndAsync();
            var stderrTask = process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            string stdout = await stdoutTask;
            string stderr = await stderrTask;

            if (process.ExitCode != 0)
            {
                string message = string.IsNullOrWhiteSpace(stderr) ? stdout : stderr;
                throw new Exception($"{toolName} error (code {process.ExitCode}): {message.Trim()}");
            }
        }
    }

    private async Task ApplyUpsPatchAsync(string romPath, string patchPath, string outputPath)
    {
        await Task.Run(() =>
        {
            byte[] sourceData = File.ReadAllBytes(romPath);
            byte[] patchData = File.ReadAllBytes(patchPath);
            if (patchData.Length < 16 || Encoding.ASCII.GetString(patchData, 0, 4) != "UPS1")
                throw new Exception("Invalid UPS file header.");
            int patchOffset = 4;
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
        string xdeltaPath = ResolveTool(_settings.Config.XdeltaPath, "xdelta3");
        await RunToolAsync(xdeltaPath, new[] { "-d", "-f", "-s", romPath, patchPath, outputPath }, "xdelta3");
    }
    public async Task ApplyIpsPatchAsync(string romPath, string patchPath, string outputPath)
    {
        await Task.Run(() =>
        {
            File.Copy(romPath, outputPath, true);
            using var patchStream = new FileStream(patchPath, FileMode.Open, FileAccess.Read);
            using var outputStream = new FileStream(outputPath, FileMode.Open, FileAccess.Write);
            using var reader = new BinaryReader(patchStream);
            using var writer = new BinaryWriter(outputStream);
            byte[] header = reader.ReadBytes(5);
            if (Encoding.ASCII.GetString(header) != "PATCH")
                throw new Exception("Invalid IPS file header.");
            while (patchStream.Position < patchStream.Length)
            {
                byte[] offsetBytes = reader.ReadBytes(3);
                
                if (Encoding.ASCII.GetString(offsetBytes) == "EOF")
                    break;
                int offset = (offsetBytes[0] << 16) | (offsetBytes[1] << 8) | offsetBytes[2];
                ushort size = (ushort)((reader.ReadByte() << 8) | reader.ReadByte());
                if (size == 0)
                {
                    ushort rleSize = (ushort)((reader.ReadByte() << 8) | reader.ReadByte());
                    byte rleByte = reader.ReadByte();
                    outputStream.Seek(offset, SeekOrigin.Begin);
                    for (int i = 0; i < rleSize; i++)
                    {
                        writer.Write(rleByte);
                    }
                }
                else 
                {
                    byte[] data = reader.ReadBytes(size);
                    outputStream.Seek(offset, SeekOrigin.Begin);
                    writer.Write(data);
                }
            }
        });
    }
    
   public async Task ApplyBpsPatchAsync(string romPath, string patchPath, string outputPath)
{
    await Task.Run(() =>
    {
        byte[] sourceData = File.ReadAllBytes(romPath);
        byte[] patchData = File.ReadAllBytes(patchPath);
        if (Encoding.ASCII.GetString(patchData, 0, 4) != "BPS1")
            throw new Exception("Invalid BPS file header.");
        int patchOffset = 4;
        ulong sourceSize = DecodeBpsNumber(patchData, ref patchOffset);
        ulong targetSize = DecodeBpsNumber(patchData, ref patchOffset);
        ulong metaSize = DecodeBpsNumber(patchData, ref patchOffset);
        patchOffset += (int)metaSize;
        byte[] targetData = new byte[targetSize];
        int outputOffset = 0;
        int sourceOffset = 0;
        int targetOffset = 0;
        while (patchOffset < patchData.Length - 12)
        {
            ulong data = DecodeBpsNumber(patchData, ref patchOffset);
            ulong command = data & 3;
            ulong length = (data >> 2) + 1;
            switch (command)
            {
                case 0: 
                    while (length > 0)
                    {
                        targetData[outputOffset] = sourceData[outputOffset];
                        outputOffset++;
                        length--;
                    }
                    break;
                case 1: 
                    while (length > 0)
                    {
                        targetData[outputOffset] = patchData[patchOffset];
                        outputOffset++;
                        patchOffset++;
                        length--;
                    }
                    break;
                case 2:
                    long dataOffset = (long)DecodeBpsNumber(patchData, ref patchOffset);
                    sourceOffset += (dataOffset & 1) != 0 ? -(int)(dataOffset >> 1) : (int)(dataOffset >> 1);

                    while (length > 0)
                    {
                        targetData[outputOffset] = sourceData[sourceOffset];
                        outputOffset++;
                        sourceOffset++;
                        length--;
                    }
                    break;

                case 3:
                    long dataOffset3 = (long)DecodeBpsNumber(patchData, ref patchOffset);
                    targetOffset += (dataOffset3 & 1) != 0 ? -(int)(dataOffset3 >> 1) : (int)(dataOffset3 >> 1);

                    while (length > 0)
                    {
                        targetData[outputOffset] = targetData[targetOffset];
                        outputOffset++;
                        targetOffset++; 
                        length--;
                    }
                    break;
            }
        }
        File.WriteAllBytes(outputPath, targetData);
    });
}

    private ulong DecodeBpsNumber(byte[] data, ref int offset)
    {
        ulong result = 0;
        ulong shift = 1;
        while (true)
        {
            byte x = data[offset++];
            result += (ulong)(x & 0x7f) * shift;
            if ((x & 0x80) != 0) break;
            shift <<= 7;
            result += shift;
        }
        return result;
    }
}