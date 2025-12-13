# GlooMeClasses

[![Vintage Story Version](https://img.shields.io/badge/Vintage%20Story-1.21.5-green)](https://www.vintagestory.at/)
[![Mod Version](https://img.shields.io/badge/Version-1.2.0-blue)](https://mods.vintagestory.at/gloomeclasses)
[![Downloads](https://img.shields.io/badge/Downloads-15k%2B-brightgreen)](https://mods.vintagestory.at/gloomeclasses)
![Discord](https://img.shields.io/discord/1410023876993482879)

A standalone class mod focused on making each class desireable in their own right, with unique appeals for each.

[Join the Official GlooMeClasses Discord Server for bug reporting and community assistance!](https://discord.gg/kP5tEreuMD)


---

## Installation

### Requirements

- Vintage Story 1.21.0 or higher
- .NET 8.0 Runtime (for development)

### For Players

1. Download the latest release from the [Vintage Story Mod DB](https://mods.vintagestory.at/gloomeclasses)
3. Place the `.zip` file in your `Mods` folder
4. Launch Vintage Story

### For Devs

```bash
# Clone the repository
git clone https://github.com/GlooMeGlo/GlooMeClasses.git
cd GlooMeClasses

# Build the mod (Linux/macOS)
./build.sh

# Build the mod (Windows)
.\build.ps1

# Or build directly with dotnet
dotnet build GloomeClasses.csproj
```

**Build Output:** `bin/Release/Mods/mod/`

---

## Dev Setup

### Prerequisites

- .NET 8.0 SDK
- Vintage Story 1.21.0+ installed
- C# IDE (VS Code, Visual Studio, etc.)

### Environment Config

The project auto-detects your Vintage Story installation:

- **Windows:** `%APPDATA%/Vintagestory`
- **Linux (Flatpak):** `/var/lib/flatpak/app/at.vintagestory.VintageStory/...`
- **Linux (Native):** `~/.config/VintagestoryData`

Override with environment variable:
```bash
export VINTAGE_STORY="/path/to/vintagestory"
```

### Build Tasks

```bash
# Full build with JSON validation
./build.sh

# Skip JSON validation (faster development builds)
./build.sh --skipJsonValidation

# Release package (creates zip in Releases/)
./build.sh --target=Package
```

---

## Diagnostic Features (v1.1.0+)

GloomeClasses now includes comprehensive diagnostic logging to help troubleshoot mod conflicts and issues.

### Viewing Diagnostic Logs

**Client-side**: Check `VintagestoryData/Logs/client-*.txt`
**Server-side**: Check `VintagestoryData/Logs/server-*.txt`

Look for messages with `[GloomeClasses]` prefix.

### Reporting Bugs

When reporting issues, please include:
1. Full `[GloomeClasses]` log section from client-debug.txt or server-debug.txt
2. List of other mods installed (especially character/class mods)
3. Game version and whether singleplayer or multiplayer
4. Exact steps to reproduce the issue

The diagnostic logging is designed to help identify what's happening on your system.

---
