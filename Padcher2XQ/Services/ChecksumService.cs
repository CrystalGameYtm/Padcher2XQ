using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Padcher2XQ.Services;

public class ChecksumService
{
    public async Task<(string crc32, string md5, string sha1)> CalculateChecksumsAsync(string filePath)
    {
        return await Task.Run(() =>
        {
            if (!File.Exists(filePath)) return ("N/A", "N/A", "N/A");

            using var stream = File.OpenRead(filePath);
            
            // CRC32
            var crc32 = CalculateCrc32(stream);
            stream.Position = 0;

            // MD5
            using var md5 = MD5.Create();
            var md5Bytes = md5.ComputeHash(stream);
            var md5String = BitConverter.ToString(md5Bytes).Replace("-", "");
            stream.Position = 0;

            // SHA1
            using var sha1 = SHA1.Create();
            var sha1Bytes = sha1.ComputeHash(stream);
            var sha1String = BitConverter.ToString(sha1Bytes).Replace("-", "");

            return (crc32, md5String, sha1String);
        });
    }

    private string CalculateCrc32(Stream stream)
    {
        // Базова таблична реалізація CRC32
        uint[] table = new uint[256];
        const uint poly = 0xEDB88320;
        for (uint i = 0; i < 256; i++)
        {
            uint crc = i;
            for (int j = 8; j > 0; j--)
            {
                if ((crc & 1) == 1) crc = (crc >> 1) ^ poly;
                else crc >>= 1;
            }
            table[i] = crc;
        }

        uint crcValue = 0xFFFFFFFF;
        int byteValue;
        while ((byteValue = stream.ReadByte()) != -1)
        {
            byte index = (byte)(((crcValue) & 0xFF) ^ byteValue);
            crcValue = (crcValue >> 8) ^ table[index];
        }

        return (~crcValue).ToString("X8");
    }
}