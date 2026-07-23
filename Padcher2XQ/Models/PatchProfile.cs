using System;
using System.Collections.Generic;

namespace Padcher2XQ.Models;

public class PatchProfile
{
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public List<PatchProfileItem> Patches { get; set; } = new();
}

public class PatchProfileItem
{
    public string Path { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;
}