using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Padcher2XQ.Models;
using SharpCompress.Archives;
using SharpCompress.Compressors.Deflate;

namespace Padcher2XQ.Services;

public class ArchiveService
{
    private static readonly HashSet<string> ValidExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".ips", ".bps", ".ups", ".xdelta", ".asm"
    };

    public record ArchiveEntryInfo(string FullKey, string FileName, long Size);

    public List<ArchiveEntryInfo> GetPatchEntries(string archivePath)
    {
        using var archive = ArchiveFactory.OpenArchive(archivePath);
        
        return archive.Entries
            .Where(e => !e.IsDirectory && ValidExtensions.Contains(Path.GetExtension(e.Key).ToLower()))
            .Select(e => new ArchiveEntryInfo(e.Key, Path.GetFileName(e.Key), e.Size))
            .ToList();
    }

    public async Task<string> ExtractPatchAsync(string archivePath, string entryKey)
    {
        string tempFolder = Path.Combine(Path.GetTempPath(), "Padcher2XQ_Temp", Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempFolder);

        string fileName = Path.GetFileName(entryKey);
        string destinationPath = Path.Combine(tempFolder, fileName);

        using (var archive = ArchiveFactory.OpenArchive(archivePath))
        {
            var entry = archive.Entries.FirstOrDefault(e => e.Key == entryKey);
            if (entry == null)
                throw new FileNotFoundException($"Entry {entryKey} not found in archive.");

            await using (var entryStream = entry.OpenEntryStream())
            await using (var fileStream = new FileStream(
                destinationPath, 
                FileMode.Create, 
                FileAccess.Write, 
                FileShare.None, 
                bufferSize: 81920, 
                useAsync: true))
            {
                await entryStream.CopyToAsync(fileStream);
                await fileStream.FlushAsync();
                fileStream.Flush(flushToDisk: true); 
            }
        }
        return destinationPath;
    }
}