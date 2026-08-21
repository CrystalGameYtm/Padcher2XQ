# Padcher2XQ
Padcher2XQ is a modern, cross-platform ROM patching utility built with C# and Avalonia UI (.NET 10). Designed for seamless usability on both Linux (KDE Plasma/Wayland & X11) and Windows, it supports multiple patch formats, multi-patch workflows, drag-and-drop file loading, and integrated verification tools.

## Features
+ Multi-Format Support: Patch ROMs using .bps, .ips, .ups, .xdelta, and ASM scripts (via asar).
+Multi-Patch Queue: Apply multiple patches sequentially to a base ROM in a single operation.
+ Drag-to-Reorder & Toggle: Easily arrange patch execution priority and enable/disable individual patches.
+ Archive & Preset System (.p2xq / .zip):
   + Load patches directly from .zip archives with custom selection dialogs.
   + Export and share standalone preset packages (.p2xq) bundled with patches and a manifest.json.
+ Checksum Inspector & Verification: Calculate CRC32, MD5, and SHA-1 hashes for original and patched ROMs in real-time.
+ RetroAchievements Integration: Verify patched ROM compatibility with RetroAchievements hash databases via API.
Adaptive Card-Based UI: Clean, responsive design supporting system-wide Dark and Light themes.

## Supported Formats
| Format | Extensions | Description |
| --- | --- | --- |
| BPS | .bps | Beat / Floating IPS format |
| IPS | .ips | Classic International Patching System |
| UPS | .ups | Universal Patching System |
| XDelta | .xdelta | VCDIFF delta compression |
| ASM | .asm | SNES assembly patches via Asar | 
| Presets | ".p2xq, .zip" | Bundled multi-patch archives |

## Getting Started
__Prerequisites__
+ .NET 10.0 SDK (or .NET 8.0+)
+ (Optional) asar and xdelta3 binaries configured in Settings for ASM and XDelta patching.

Building from Source:
1. Clone the repository:
```Bash
git clone https://github.com/your-username/Padcher2XQ.git
cd Padcher2XQ
```
2. Restore dependencies: 
```Bash
dotnet restore
```
3. Build the project:
```Bash
dotnet build -c Release
```
4. Run the application:
```Bash
dotnet run --project Padcher2XQ
```

Usage
1. Load Base ROM: Drag and drop your clean ROM into the application or click Browse....
2. Choose Mode:
    + Single Patch: Select a single patch file.
    + Multi-Patch: Add multiple patches or load a .zip / .p2xq preset. Reorder them using drag-and-drop or the ▲/▼ buttons.
3. Select Output: Set the target output destination.
4. Apply: Click Apply Patch(es). Check the verification expander for calculated checksums.
