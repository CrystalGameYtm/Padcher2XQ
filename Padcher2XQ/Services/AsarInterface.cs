using System;
using System.Runtime.InteropServices;

namespace Padcher2XQ.Services;

public static class AsarInterface
{
    // Імпортуємо функції з asar.dll
    // Переконайтесь, що asar.dll лежить у папці bin/Debug/netX.X/
    
    [DllImport("asar.dll", EntryPoint = "asar_init", CallingConvention = CallingConvention.Cdecl)]
    public static extern bool Init();

    [DllImport("asar.dll", EntryPoint = "asar_close", CallingConvention = CallingConvention.Cdecl)]
    public static extern void Close();

    [DllImport("asar.dll", EntryPoint = "asar_patch", CallingConvention = CallingConvention.Cdecl)]
    public static extern bool Patch(string patchloc, string romloc, int buflen, int romlen, out int outlen);

    [DllImport("asar.dll", EntryPoint = "asar_geterrors", CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr GetErrors(out int count);

    public static string GetErrorString()
    {
        int count;
        IntPtr ptr = GetErrors(out count);
        if (count == 0) return string.Empty;
        
        // Тут спрощена обробка помилок. У повній версії треба розбирати структуру error data
        return "Asar reported errors. Check your ASM file.";
    }
}