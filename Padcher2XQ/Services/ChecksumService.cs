using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace Padcher2XQ.Services;

public class ChecksumService
{
    private static readonly uint[] Crc32Table;

    public string OriginalCrc32 { get; private set; } = "---";
    public string OriginalMd5 { get; private set; } = "---";
    public string OriginalSha1 { get; private set; } = "---";

    public void SetOriginalChecksums(string crc32, string md5, string sha1)
    {
        OriginalCrc32 = crc32;
        OriginalMd5 = md5;
        OriginalSha1 = sha1;
    }

    public bool VerifyChecksum(string currentHash, string expectedHash)
    {
        if (string.IsNullOrEmpty(currentHash) || string.IsNullOrEmpty(expectedHash)) 
            return false;
        return currentHash.Equals(expectedHash, StringComparison.OrdinalIgnoreCase);
    }

    static ChecksumService()
    {
        Crc32Table = new uint[256];
        const uint poly = 0xEDB88320;
        for (uint i = 0; i < 256; i++)
        {
            uint crc = i;
            for (int j = 8; j > 0; j--)
            {
                if ((crc & 1) == 1) crc = (crc >> 1) ^ poly;
                else crc >>= 1;
            }
            Crc32Table[i] = crc;
        }
    }

    public async Task<(string crc32, string md5, string sha1)> CalculateChecksumsAsync(string filePath)
    {
        return await Task.Run(() =>
        {
            if (!File.Exists(filePath)) return ("N/A", "N/A", "N/A");

            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 8192);
            using var md5 = MD5.Create();
            using var sha1 = SHA1.Create();

            byte[] buffer = new byte[4096];
            int bytesRead;
            uint crcValue = 0xFFFFFFFF;

            while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
            {
                md5.TransformBlock(buffer, 0, bytesRead, null, 0);
                sha1.TransformBlock(buffer, 0, bytesRead, null, 0);

                for (int i = 0; i < bytesRead; i++)
                {
                    byte index = (byte)((crcValue & 0xFF) ^ buffer[i]);
                    crcValue = (crcValue >> 8) ^ Crc32Table[index];
                }
            }

            md5.TransformFinalBlock(buffer, 0, 0);
            sha1.TransformFinalBlock(buffer, 0, 0);

            string crc32Result = (~crcValue).ToString("X8");
            string md5Result = BitConverter.ToString(md5.Hash!).Replace("-", "");
            string sha1Result = BitConverter.ToString(sha1.Hash!).Replace("-", "");

            return (crc32Result, md5Result, sha1Result);
        });
    }
}