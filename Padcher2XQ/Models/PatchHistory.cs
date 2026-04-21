using System;

namespace Padcher2XQ.Models;

public class PatchHistory
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string RomName { get; set; } = string.Empty;
    public string RomPath { get; set; } = string.Empty;
    public string PatchPath { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.Now;
}