using System;
using System.Collections.Generic;

namespace Padcher2XQ.Models;

public class PatcherHistory
{
    public List<RomEntry> RomHistory { get; set; } = new();
    public List<PatchEntry> PatchHistory { get; set; } = new();
}
public class RomEntry
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string RomName { get; set; } = string.Empty;
    public string RomPath { get; set; } = string.Empty;
    public string RomFormat { get; set; } = string.Empty;
    public string RomCRC32 { get; set; } = string.Empty;
    public string RomMD5 { get; set; } = string.Empty;
    public string RomSHA1 { get; set; } = string.Empty;
}

public class PatchEntry
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string PatchName { get; set; } = string.Empty;
    public string PatchPath { get; set; } = string.Empty;
    public string PatchFormat { get; set; } = string.Empty;
}