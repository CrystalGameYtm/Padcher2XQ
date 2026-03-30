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
        string asarPath = _settings.Config.AsarPath;
        if (string.IsNullOrWhiteSpace(asarPath)) asarPath = "asar.exe";

        if (Path.IsPathRooted(asarPath) && !File.Exists(asarPath))
            throw new FileNotFoundException($"Asar executable not found at: {asarPath}");

        File.Copy(romPath, outputPath, true);

        var startInfo = new ProcessStartInfo
        {
            FileName = asarPath,
            Arguments = $"\"{patchPath}\" \"{outputPath}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(startInfo);
        if (process == null) throw new Exception("Failed to start Asar process.");
        
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
        string xdeltaPath = _settings.Config.AsarPath;
        if (string.IsNullOrWhiteSpace(xdeltaPath)) xdeltaPath = "xDelta3.exe";

        if (Path.IsPathRooted(xdeltaPath) && !File.Exists(xdeltaPath))
            throw new FileNotFoundException($"xDelta3 executable not found at: {xdeltaPath}");

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
    public async Task ApplyIpsPatchAsync(string romPath, string patchPath, string outputPath)
    {
        await Task.Run(() =>
        {
            // Копіюємо ROM у вихідний файл (або створюємо новий, якщо IPS розширює його)
            // IPS зазвичай працює "поверх" файлу, тому спочатку копіюємо
            File.Copy(romPath, outputPath, true);

            using var patchStream = new FileStream(patchPath, FileMode.Open, FileAccess.Read);
            using var outputStream = new FileStream(outputPath, FileMode.Open, FileAccess.Write);
            using var reader = new BinaryReader(patchStream);
            using var writer = new BinaryWriter(outputStream);

            // 1. Перевірка заголовка "PATCH"
            byte[] header = reader.ReadBytes(5);
            if (Encoding.ASCII.GetString(header) != "PATCH")
                throw new Exception("Invalid IPS file header.");

            while (patchStream.Position < patchStream.Length)
            {
                // IPS Offset is 3 bytes (Big Endian)
                byte[] offsetBytes = reader.ReadBytes(3);
                
                // Перевірка на "EOF" (45 4F 46)
                if (Encoding.ASCII.GetString(offsetBytes) == "EOF")
                    break;

                int offset = (offsetBytes[0] << 16) | (offsetBytes[1] << 8) | offsetBytes[2];

                // Size is 2 bytes (Big Endian)
                ushort size = (ushort)((reader.ReadByte() << 8) | reader.ReadByte());

                if (size == 0) // RLE Encoding
                {
                    ushort rleSize = (ushort)((reader.ReadByte() << 8) | reader.ReadByte());
                    byte rleByte = reader.ReadByte();

                    outputStream.Seek(offset, SeekOrigin.Begin);
                    for (int i = 0; i < rleSize; i++)
                    {
                        writer.Write(rleByte);
                    }
                }
                else // Normal Record
                {
                    byte[] data = reader.ReadBytes(size);
                    outputStream.Seek(offset, SeekOrigin.Begin);
                    writer.Write(data);
                }
            }
        });
    }

    // --- BPS Implementation ---
    public async Task ApplyBpsPatchAsync(string romPath, string patchPath, string outputPath)
    {
        await Task.Run(() =>
        {
            byte[] sourceData = File.ReadAllBytes(romPath);
            byte[] patchData = File.ReadAllBytes(patchPath);

            // BPS Header: "BPS1"
            if (Encoding.ASCII.GetString(patchData, 0, 4) != "BPS1")
                throw new Exception("Invalid BPS file header.");

            int patchOffset = 4;

            ulong sourceSize = DecodeBpsNumber(patchData, ref patchOffset);
            ulong targetSize = DecodeBpsNumber(patchData, ref patchOffset);
            ulong metaSize = DecodeBpsNumber(patchData, ref patchOffset);

            // Пропускаємо метадані
            patchOffset += (int)metaSize;

            byte[] targetData = new byte[targetSize];
            int outputOffset = 0;
            int sourceOffset = 0;
            int targetOutputOffset = 0; // Для TargetCopy

            // Цикл читання дій
            while (patchOffset < patchData.Length - 12) // Останні 12 байт - чексуми
            {
                ulong data = DecodeBpsNumber(patchData, ref patchOffset);
                ulong command = data & 3;
                ulong length = (data >> 2) + 1;

                switch (command)
                {
                    case 0: // SourceRead: Copy from source file
                        while (length > 0)
                        {
                            targetData[outputOffset] = sourceData[targetOutputOffset];
                            outputOffset++;
                            targetOutputOffset++;
                            length--;
                        }
                        break;

                    case 1: // TargetRead: Read immediate bytes from patch
                        while (length > 0)
                        {
                            targetData[outputOffset] = patchData[patchOffset];
                            outputOffset++;
                            patchOffset++;
                            length--;
                        }
                        break;

                    case 2: // SourceCopy: Copy from source with offset
                        long dataOffset = (long)DecodeBpsNumber(patchData, ref patchOffset);
                        // Декодування зіг-заг числа (signed/unsigned mapping)
                        if ((dataOffset & 1) != 0)
                            sourceOffset -= (int)(dataOffset >> 1);
                        else
                            sourceOffset += (int)(dataOffset >> 1);

                        while (length > 0)
                        {
                            targetData[outputOffset] = sourceData[sourceOffset];
                            outputOffset++;
                            sourceOffset++;
                            length--;
                        }
                        break;

                    case 3: // TargetCopy: Copy from output (already written data)
                        long dataOffset3 = (long)DecodeBpsNumber(patchData, ref patchOffset);
                        if ((dataOffset3 & 1) != 0)
                            sourceOffset -= (int)(dataOffset3 >> 1);
                        else
                            sourceOffset += (int)(dataOffset3 >> 1);

                        while (length > 0)
                        {
                            // Читаємо з буфера, який ми зараз пишемо
                            targetData[outputOffset] = targetData[sourceOffset];
                            outputOffset++;
                            sourceOffset++;
                            length--;
                        }
                        break;
                }
            }
            
            // Тут можна додати перевірку CRC32 (останні 12 байт: SourceCRC, TargetCRC, PatchCRC)

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