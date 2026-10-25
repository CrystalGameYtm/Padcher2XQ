using Padcher2XQ.Services;
namespace Padcher2XQ.Models;

public class AppConfig
{
    public bool IsDarkMode { get; set; } = true; 
    // empty = look the tool up in PATH (asar / xdelta3, with ".exe" on Windows)
    public string AsarPath { get; set; } = "";
    public string XdeltaPath { get; set; } = "";
    public string RaUser { get; set; } = "";
    public string RaApiKey { get; set; } = "";
}   