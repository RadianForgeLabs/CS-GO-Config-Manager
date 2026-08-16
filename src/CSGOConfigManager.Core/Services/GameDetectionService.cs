using System.Runtime.Versioning;
using CSGOConfigManager.Core.Models;
using Microsoft.Win32;

namespace CSGOConfigManager.Core.Services;

/// <summary>
/// Detects Steam, 7Launcher, and CS:GO Legacy install paths.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class GameDetectionService
{
    private static readonly string[] SteamRegistryKeys =
    {
        @"HKEY_CURRENT_USER\Software\Valve\Steam",
        @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Valve\Steam",
        @"HKEY_LOCAL_MACHINE\SOFTWARE\Valve\Steam"
    };

    private static readonly string[] DefaultSteamPaths =
    {
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Steam"),
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Steam"),
        @"C:\Program Files (x86)\Steam",
        @"C:\Steam"
    };

    private static readonly string[] DefaultSevenLauncherPaths =
    {
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "7Launcher"),
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "7Launcher"),
        @"C:\Program Files\7Launcher",
        @"C:\Games\7Launcher",
        @"C:\7Launcher"
    };

    private static readonly string[] DefaultCsgoRoots =
    {
        @"C:\Games\CSGO",
        @"C:\CSGO",
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Steam", "steamapps", "common", "Counter-Strike Global Offensive")
    };

    public GameInstallation Detect(AppSettings? settings = null)
    {
        var messages = new List<string>();
        string? steamPath = null;
        string? sevenPath = null;
        string? csgoRoot = null;
        var source = "None";

        // 1) User overrides first
        if (!string.IsNullOrWhiteSpace(settings?.CsgoPath) && LooksLikeCsgoRoot(settings.CsgoPath))
        {
            csgoRoot = NormalizePath(settings.CsgoPath);
            source = "User Settings";
            messages.Add($"Using CS:GO path from settings: {csgoRoot}");
        }

        if (!string.IsNullOrWhiteSpace(settings?.SteamPath) && Directory.Exists(settings.SteamPath))
        {
            steamPath = NormalizePath(settings.SteamPath);
            messages.Add($"Using Steam path from settings: {steamPath}");
        }
        else
        {
            steamPath = DetectSteamPath();
            if (steamPath is not null)
                messages.Add($"Steam detected at: {steamPath}");
            else
                messages.Add("Steam not found via registry or default paths.");
        }

        if (!string.IsNullOrWhiteSpace(settings?.SevenLauncherPath) && File.Exists(settings.SevenLauncherPath))
        {
            sevenPath = NormalizePath(settings.SevenLauncherPath);
            messages.Add($"Using 7Launcher path from settings: {sevenPath}");
        }
        else
        {
            sevenPath = DetectSevenLauncher();
            if (sevenPath is not null)
                messages.Add($"7Launcher detected at: {sevenPath}");
            else
                messages.Add("7Launcher not found.");
        }

        // 2) Derive CS:GO from Steam library folders
        if (csgoRoot is null && steamPath is not null)
        {
            csgoRoot = FindCsgoInSteam(steamPath);
            if (csgoRoot is not null)
            {
                source = "Steam";
                messages.Add($"CS:GO found under Steam: {csgoRoot}");
            }
        }

        // 3) Default fallbacks
        if (csgoRoot is null)
        {
            foreach (var candidate in DefaultCsgoRoots)
            {
                if (LooksLikeCsgoRoot(candidate))
                {
                    csgoRoot = NormalizePath(candidate);
                    source = "Default Path";
                    messages.Add($"CS:GO found at default path: {csgoRoot}");
                    break;
                }
            }
        }

        if (csgoRoot is null)
            messages.Add("CS:GO Legacy installation was not found. Set the path manually in Settings.");

        string? cfgPath = null;
        string? exePath = null;
        string? version = null;

        if (csgoRoot is not null)
        {
            cfgPath = ResolveCfgPath(csgoRoot);
            exePath = ResolveExePath(csgoRoot);
            version = TryReadGameVersion(csgoRoot);
        }

        return new GameInstallation
        {
            SteamFound = steamPath is not null && Directory.Exists(steamPath),
            SteamPath = steamPath,
            SevenLauncherFound = sevenPath is not null && File.Exists(sevenPath),
            SevenLauncherPath = sevenPath,
            CsgoFound = csgoRoot is not null,
            CsgoRootPath = csgoRoot,
            CsgoCfgPath = cfgPath,
            CsgoExePath = exePath,
            GameVersion = version,
            DetectionSource = source,
            Messages = messages
        };
    }

    public static bool LooksLikeCsgoRoot(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
            return false;

        // Accept either the game root or the csgo subfolder
        if (File.Exists(Path.Combine(path, "csgo.exe")) ||
            Directory.Exists(Path.Combine(path, "csgo", "cfg")) ||
            Directory.Exists(Path.Combine(path, "cfg")))
        {
            return true;
        }

        return false;
    }

    public static string? ResolveCfgPath(string csgoRoot)
    {
        var candidates = new[]
        {
            Path.Combine(csgoRoot, "csgo", "cfg"),
            Path.Combine(csgoRoot, "cfg"),
            // If user pointed at csgo subfolder
            Path.Combine(Path.GetDirectoryName(csgoRoot) ?? csgoRoot, "csgo", "cfg")
        };

        foreach (var c in candidates)
        {
            if (Directory.Exists(c))
                return c;
        }

        // Create expected path under root/csgo/cfg for new installs
        var expected = Path.Combine(csgoRoot, "csgo", "cfg");
        return Directory.Exists(Path.Combine(csgoRoot, "csgo")) ? expected : Path.Combine(csgoRoot, "cfg");
    }

    public static string? ResolveExePath(string csgoRoot)
    {
        var candidates = new[]
        {
            Path.Combine(csgoRoot, "csgo.exe"),
            Path.Combine(csgoRoot, "bin", "win64", "csgo.exe"),
            Path.Combine(csgoRoot, "..", "csgo.exe")
        };

        foreach (var c in candidates)
        {
            var full = Path.GetFullPath(c);
            if (File.Exists(full))
                return full;
        }

        return null;
    }

    private static string? DetectSteamPath()
    {
        foreach (var keyPath in SteamRegistryKeys)
        {
            try
            {
                var value = Registry.GetValue(keyPath, "SteamPath", null)
                            ?? Registry.GetValue(keyPath, "InstallPath", null);
                if (value is string path && Directory.Exists(path))
                    return NormalizePath(path);
            }
            catch
            {
                // Ignore registry access failures
            }
        }

        foreach (var path in DefaultSteamPaths)
        {
            if (Directory.Exists(path) && File.Exists(Path.Combine(path, "steam.exe")))
                return NormalizePath(path);
        }

        return null;
    }

    private static string? DetectSevenLauncher()
    {
        foreach (var dir in DefaultSevenLauncherPaths)
        {
            var exe = Path.Combine(dir, "7launcher.exe");
            if (File.Exists(exe))
                return NormalizePath(exe);

            // Some installs use different casing
            if (Directory.Exists(dir))
            {
                var match = Directory.GetFiles(dir, "*launcher*.exe", SearchOption.TopDirectoryOnly)
                    .FirstOrDefault(f => Path.GetFileName(f).Contains("7", StringComparison.OrdinalIgnoreCase));
                if (match is not null)
                    return NormalizePath(match);
            }
        }

        return null;
    }

    private static string? FindCsgoInSteam(string steamPath)
    {
        var libraryFolders = new List<string> { Path.Combine(steamPath, "steamapps") };

        var vdf = Path.Combine(steamPath, "steamapps", "libraryfolders.vdf");
        if (File.Exists(vdf))
        {
            try
            {
                foreach (var line in File.ReadLines(vdf))
                {
                    // "path"		"D:\\SteamLibrary"
                    var trimmed = line.Trim();
                    if (!trimmed.Contains("\"path\"", StringComparison.OrdinalIgnoreCase))
                        continue;

                    var parts = trimmed.Split('"', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                    if (parts.Length >= 2)
                    {
                        var libPath = parts[^1].Replace(@"\\", @"\");
                        var apps = Path.Combine(libPath, "steamapps");
                        if (Directory.Exists(apps))
                            libraryFolders.Add(apps);
                    }
                }
            }
            catch
            {
                // Ignore VDF parse issues
            }
        }

        foreach (var apps in libraryFolders.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var candidate = Path.Combine(apps, "common", "Counter-Strike Global Offensive");
            if (LooksLikeCsgoRoot(candidate))
                return NormalizePath(candidate);
        }

        return null;
    }

    private static string? TryReadGameVersion(string csgoRoot)
    {
        var steamInf = Path.Combine(csgoRoot, "csgo", "steam.inf");
        if (!File.Exists(steamInf))
            steamInf = Path.Combine(csgoRoot, "steam.inf");

        if (!File.Exists(steamInf))
            return null;

        try
        {
            foreach (var line in File.ReadLines(steamInf))
            {
                if (line.StartsWith("PatchVersion=", StringComparison.OrdinalIgnoreCase) ||
                    line.StartsWith("ClientVersion=", StringComparison.OrdinalIgnoreCase) ||
                    line.StartsWith("ServerVersion=", StringComparison.OrdinalIgnoreCase))
                {
                    var parts = line.Split('=', 2);
                    if (parts.Length == 2)
                        return parts[1].Trim();
                }
            }
        }
        catch
        {
            return null;
        }

        return null;
    }

    private static string NormalizePath(string path) =>
        Path.GetFullPath(path.Replace('/', Path.DirectorySeparatorChar).TrimEnd('\\', '/'));
}
