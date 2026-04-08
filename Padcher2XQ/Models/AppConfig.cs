using Padcher2XQ.Services;
namespace Padcher2XQ.Models;

public class AppConfig
{
    public bool IsDarkMode { get; set; } = true; 
    public string AsarPath { get; set; } = "asar.exe";
    public string XdeltaPath { get; set; } = "xdelta3.exe";
    public string RaUser { get; set; } = "";
    public string RaApiKey { get; set; } = "";
}   