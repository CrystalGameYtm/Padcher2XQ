using Avalonia;
using System;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
namespace Padcher2XQ;

class Program
{
    [DllImport("kernel32.dll", SetLastError = true)] private static extern bool AttachConsole(uint dwProcessId); private const uint ATTACH_PARENT_PROCESS = 0x0ffffffff; 
    
    [STAThread]
    public static void Main(string[] args)
    {
        if (args.Length > 0)
        {
            if (OperatingSystem.IsWindows())
            {
                AttachConsole(ATTACH_PARENT_PROCESS);
            }

            int exitCode = RunCliModeAsync(args).GetAwaiter().GetResult();

            Environment.Exit(exitCode);
        }
        else
        {
            BuildAvaloniaApp()
                .StartWithClassicDesktopLifetime(args);
        }
    }

    private static async Task<int> RunCliModeAsync(string[] args)
    {
        Console.WriteLine("Padcher2XQ v2.1 - CLI Mode");
        
        string? romPath = null;
        string? patchPath = null;
        string? outPath = null;

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i].ToLower())
            {
                case "-rom":
                case "-r":
                    if (i + 1 < args.Length) romPath = args[++i];
                    break;
                case "-patch":
                case "-p":
                    if (i + 1 < args.Length) patchPath = args[++i];
                    break;
                case "-out":
                case "-o":
                    if (i + 1 < args.Length) outPath = args[++i];
                    break;
                case "-help":
                case "-h":
                    PrintHelp();
                    return 0;
            }
        }
        if (string.IsNullOrEmpty(romPath) || string.IsNullOrEmpty(patchPath))
        {
            Console.WriteLine("Error: Both -rom and -patch arguments are required!");
            PrintHelp();
            return 1;
        }

        if (!System.IO.File.Exists(romPath))
        {
            Console.WriteLine($"Error: ROM not found: {romPath}");
            return 1;
        }

        if (!System.IO.File.Exists(patchPath))
        {
            Console.WriteLine($"Error: Patch not found: {patchPath}");
            return 1;
        }

        if (string.IsNullOrEmpty(outPath))
        {
            var directory = System.IO.Path.GetDirectoryName(romPath);
            var filename = System.IO.Path.GetFileNameWithoutExtension(romPath);
            var extension = System.IO.Path.GetExtension(romPath);
            outPath = System.IO.Path.Combine(directory ?? "", $"{filename}_patched{extension}");
        }

        Console.WriteLine($"ROM:   {romPath}");
        Console.WriteLine($"Patch: {patchPath}");
        Console.WriteLine($"Out:   {outPath}");
        Console.WriteLine("Patching...");

        if (string.Equals(System.IO.Path.GetFullPath(outPath), System.IO.Path.GetFullPath(romPath),
                OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
        {
            Console.WriteLine("Error: output path must differ from the ROM path.");
            return 1;
        }

        try
        {
            var settings = new Padcher2XQ.Services.SettingsService();
            var patcher = new Padcher2XQ.Services.PatcherService(settings);
            await patcher.PatchSingleFileAsync(romPath, patchPath, outPath);

            Console.WriteLine("Success! Patch applied correctly.");
            return 0;
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Critical Error: {ex.Message}");
            Console.ResetColor();
            return 1;
        }
    }

    private static void PrintHelp()
    {
        Console.WriteLine("\nUsage: Padcher2XQ [options]");
        Console.WriteLine("Options:");
        Console.WriteLine("  -rom,   -r <path>   Path to the original ROM file (Required)");
        Console.WriteLine("  -patch, -p <path>   Path to the patch file (.ips, .bps, etc.) (Required)");
        Console.WriteLine("  -out,   -o <path>   Path to save the patched ROM (Optional)");
        Console.WriteLine("  -help,  -h          Show this help message\n");
    }

    public static AppBuilder BuildAvaloniaApp()
    {
        var builder = AppBuilder.Configure<App>().UsePlatformDetect();

        // Wayland backend only makes sense on Linux
        if (OperatingSystem.IsLinux())
            builder = builder.UseWayland();

        return builder.WithInterFont().LogToTrace();
    }
}