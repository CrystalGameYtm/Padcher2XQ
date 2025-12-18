using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;  
using System.Diagnostics; 


namespace Padcher2XQ.Services;

public class PatcherService
{
    // --- IPS Implementation ---
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

    // Допоміжний метод для декодування чисел змінної довжини (Variable-length integer)
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

public async Task ApplyAsmPatchAsync(string romPath, string patchPath, string outputPath)
    {
        await Task.Run(() =>
        {
            if (!File.Exists("asar.dll"))
                throw new FileNotFoundException("asar.dll not found! Please place it in the app directory.");

            // Asar патчить "на місці", тому спочатку копіюємо
            File.Copy(romPath, outputPath, true);

            if (!AsarInterface.Init())
                throw new Exception("Failed to initialize Asar.");

            try
            {
                // Asar вимагає, щоб файл існував.
                // Ми передаємо шлях до патча і шлях до РОМу
                // Увага: Asar API в C# трохи складний, бо працює з пам'яттю.
                // Найпростіший варіант для новачків - викликати asar.exe як процес,
                // але якщо ми вже зробили DLL wrapper, спробуємо його використати (або спростимо до процесу).
                
                // ПРОСТИЙ ВАРІАНТ (через процес, надійніше для новачків):
                if (File.Exists("asar.exe"))
                {
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
                    process!.WaitForExit();
                    
                    if (process.ExitCode != 0)
                    {
                        string error = process.StandardError.ReadToEnd();
                        throw new Exception($"Asar Error: {error}");
                    }
                }
                else
                {
                    throw new FileNotFoundException("asar.exe not found.");
                }
            }
            finally
            {
                AsarInterface.Close();
            }
        });
    }

    // --- NEW: Xdelta Patching ---
    public async Task ApplyXdeltaPatchAsync(string romPath, string patchPath, string outputPath)
    {
        await Task.Run(() =>
        {
            // Для xdelta найкраще використовувати xdelta3.exe
            if (!File.Exists("xdelta3.exe"))
                throw new FileNotFoundException("xdelta3.exe not found! Please place it in the app directory.");

            // Аргументи: -d (decompress/apply) -s (source) [SOURCE] [PATCH] [OUTPUT]
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
            process!.WaitForExit();

            if (process.ExitCode != 0)
            {
                // xdelta іноді пише помилки в stdout
                string error = process.StandardError.ReadToEnd(); 
                if (string.IsNullOrEmpty(error)) error = process.StandardOutput.ReadToEnd();
                
                throw new Exception($"Xdelta Error (Code {process.ExitCode}): {error}");
            }
        });
    }

    // --- MASTER METHOD: Визначає тип патча ---
    public async Task PatchSingleFileAsync(string romPath, string patchPath, string outputPath)
    {
        string ext = Path.GetExtension(patchPath).ToLower();
        switch (ext)
        {
            case ".ips":
                await ApplyIpsPatchAsync(romPath, patchPath, outputPath);
                break;
            case ".bps":
                await ApplyBpsPatchAsync(romPath, patchPath, outputPath);
                break;
            case ".asm":
                await ApplyAsmPatchAsync(romPath, patchPath, outputPath);
                break;
            case ".xdelta":
                await ApplyXdeltaPatchAsync(romPath, patchPath, outputPath);
                break;
            default:
                throw new NotSupportedException($"Format {ext} is not supported.");
        }
    }

    // --- MULTI PATCHING (Sequential) ---
    // Застосовує список патчів один за одним до одного ROMу
    public async Task ApplyMultiplePatchesAsync(string romPath, List<string> patchPaths, string finalOutputPath)
    {
        string tempFile1 = Path.Combine(Path.GetTempPath(), "padcher_temp_1.sfc");
        string tempFile2 = Path.Combine(Path.GetTempPath(), "padcher_temp_2.sfc");
        
        // Спочатку копіюємо оригінал у temp1
        File.Copy(romPath, tempFile1, true);

        string currentSource = tempFile1;
        string currentTarget = tempFile2;

        try
        {
            for (int i = 0; i < patchPaths.Count; i++)
            {
                // Визначаємо, куди писати результат (чергуємо файли)
                currentTarget = (i % 2 == 0) ? tempFile2 : tempFile1;
                currentSource = (i % 2 == 0) ? tempFile1 : tempFile2;

                await PatchSingleFileAsync(currentSource, patchPaths[i], currentTarget);
            }

            // Копіюємо останній результат у фінальний шлях
            if (File.Exists(finalOutputPath)) File.Delete(finalOutputPath);
            File.Move(currentTarget, finalOutputPath);
        }
        finally
        {
            // Чистимо сміття
            if (File.Exists(tempFile1)) File.Delete(tempFile1);
            if (File.Exists(tempFile2)) File.Delete(tempFile2);
        }
    }
}