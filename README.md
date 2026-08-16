# CS:GO Config Manager

Professional **Windows 11** desktop app for **CS:GO Legacy (1.38.x)** configuration management.

Data-driven GUI for console commands/ConVars, game-mode editors, profiles, backups, multi-launcher support (Steam / 7Launcher / direct `csgo.exe` / custom), conflict detection, and an optional **non-injective** practice overlay.

> Scope: CS:GO Legacy only. No Steam API bypass, no VAC injection, no memory cheats.

## Features (MVP)

| Area | What you get |
|------|----------------|
| **Home** | Detect Steam / 7Launcher / CS:GO, quick launch, open game/cfg/userdata folders |
| **Launch Center** | Steam, 7Launcher, EXE, Custom + offline flag + launch-with-profile |
| **Game Modes** | Filterable command grid per mode (Casual, Competitive, DM, …) |
| **Bot Manager** | Quota, difficulty, team, presets |
| **Practice** | `sv_cheats`, infinite ammo, trajectories, buy anywhere, etc. |
| **Command Browser** | Search all commands, edit, show conflict sources |
| **Conflict Detector** | Cross-file override list (effective value highlighted) |
| **Config Editor** | Raw `.cfg` text edit with auto-backup on save |
| **Profiles** | Built-in presets + user profiles (import/export JSON) |
| **Backups** | Manual/auto snapshots, restore, ZIP export, simple diff |
| **Overlay** | Transparent always-on-top window (writes cfg only, no injection) |
| **Settings** | Paths, launch defaults, auto-backup retention |

## Requirements

- Windows 10/11 x64  
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (to build)  
- CS:GO Legacy install (Steam and/or 7Launcher) optional for path detection tests

## Build & Run

```powershell
cd D:\Githup_Repository\CS_GO_Config_Manager
dotnet build CS_GO_Config_Manager.slnx
dotnet run --project src\CSGOConfigManager\CSGOConfigManager.csproj
```

Run tests:

```powershell
dotnet test src\CSGOConfigManager.Tests\CSGOConfigManager.Tests.csproj
```

### Portable publish

```powershell
.\publish.ps1
```

Output: `dist\CSGOConfigManager\` (requires [.NET 8 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/8.0) on the target PC).

Debug output folder:

```
src\CSGOConfigManager\bin\Debug\net8.0-windows\
  CSGOConfigManager.exe
  Data\...
```

Copy that entire folder to use the app without installing.

### Shortcuts

| Key | Action |
|-----|--------|
| **F10** | Toggle practice overlay |

## First-time setup

1. Start the app.  
2. Open **Settings** if auto-detect fails.  
3. Set **CS:GO Path** (folder containing `csgo.exe` or the game root with `csgo\cfg`).  
4. Optionally set Steam / 7Launcher paths.  
5. Save → Home should show **CS:GO ready**.

### Detection heuristics

- **Steam:** registry `HKCU\Software\Valve\Steam\SteamPath` (+ LM WOW6432Node), then `%ProgramFiles(x86)%\Steam`, then `libraryfolders.vdf` for extra libraries.  
- **CS:GO:** `steamapps\common\Counter-Strike Global Offensive`, or manual path.  
- **cfg folder:** `...\csgo\cfg\`.  
- **7Launcher:** common Program Files / Games paths, or manual browse.

## Data-driven commands

All editable settings live in JSON (no recompile needed to add commands):

```
Data/
  Commands.json      # command metadata (type, min/max, modes, file, …)
  GameModes.json     # mode → cfg file map
  Launchers.json     # launch method definitions
  Presets/           # Casual, Competitive, Practice, …
```

Example entry:

```json
{
  "name": "bot_quota",
  "type": "integer",
  "default": 10,
  "min": 0,
  "max": 32,
  "description": "Total number of bots to maintain.",
  "category": "Bots",
  "requires_restart": false,
  "requires_sv_cheats": false,
  "mode": ["Casual", "Competitive", "Custom/Practice"],
  "file": "autoexec.cfg"
}
```

## Config priority (conflict detection)

```
config.cfg  <  autoexec.cfg  <  gamemode_*.cfg  <  practice.cfg
```

The Conflict Detector and Command Browser show every source and mark the **effective** value.

## Portable layout (runtime)

Created next to the EXE:

| Folder | Purpose |
|--------|---------|
| `Data/` | Read-only JSON shipped with the app |
| `Config/Settings.json` | User preferences |
| `Config/Profiles/` | User profiles |
| `Backups/` | Auto + manual cfg snapshots |
| `Logs/` | Application log files |

## Overlay safety

The practice overlay is a **separate transparent WPF window** (`AllowsTransparency`, `Topmost`). It does **not** hook DirectX, inject DLLs, or read game memory. It only writes local `.cfg` files. Toggle from the main window (**Overlay** nav) or keep it for offline practice only.

## Security / legal

- Does not emulate Steam or bypass licensing.  
- Does not inject into CS:GO (VAC-safe overlay design).  
- Practice “cheats” are standard `sv_cheats` offline ConVars only.  
- 7Launcher is detected/launched only if you already installed it.

## Solution structure

```
src/
  CSGOConfigManager/           # WPF UI
  CSGOConfigManager.Core/      # models, cfg parser, services
  CSGOConfigManager.Tests/     # xUnit tests
```

## Milestone status (from prompt.md)

| Milestone | Status |
|-----------|--------|
| M1 Project setup | Done |
| M2 Game detection | Done |
| M3 Commands DB | Done (extensible JSON) |
| M4 Config editor | Done |
| M5 Game modes UI | Done |
| M6 Controls & binding | Done (grid + dedicated pages) |
| M7 Profiles / backups | Done |
| M8 Launcher integration | Done |
| M9 Conflict detector | Done |
| M10 Command browser | Done |
| M11 Overlay MVP | Done (basic widgets) |
| M12 Tests | Done (22 unit tests) |
| M13 Documentation | This README |

## License

Choose an open-source license when publishing (MIT recommended for community command JSON contributions).
