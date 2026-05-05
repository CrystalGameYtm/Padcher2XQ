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

    public void SetOriginalChecksums(string crc, string md5, string sha)
    {
        OriginalCrc32 = crc;
        OriginalMd5 = md5;
        OriginalSha1 = sha;
    }

    public async Task<(string crc32, string md5, string sha1)> CalculateChecksumsAsync(string filePath)
    {
        return await Task.Run(() =>
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath)) 
                return ("---", "---", "---");

            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 65536);
            
            uint crcValue = 0xFFFFFFFF;
            byte[] buffer = new byte[65536];
            int bytesRead;
            while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
            {
                for (int i = 0; i < bytesRead; i++)
                {
                    byte index = (byte)((crcValue & 0xFF) ^ buffer[i]);
                    crcValue = (crcValue >> 8) ^ Crc32Table[index];
                }
            }
            string crc32Result = (~crcValue).ToString("X8");

            stream.Position = 0;
            using var md5 = MD5.Create();
            string md5Result = BitConverter.ToString(md5.ComputeHash(stream)).Replace("-", "");

            stream.Position = 0;
            using var sha1 = SHA1.Create();
            string sha1Result = BitConverter.ToString(sha1.ComputeHash(stream)).Replace("-", "");

            return (crc32Result, md5Result, sha1Result);
        });
    }
}
