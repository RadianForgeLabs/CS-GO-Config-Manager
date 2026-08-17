using System.Diagnostics;
using CSGOConfigManager.Core.Models;

namespace CSGOConfigManager.Core.Services;

public sealed class LaunchService
{
    private readonly DataService _dataService;

    public LaunchService(DataService dataService)
    {
        _dataService = dataService;
    }

    public LaunchResult Launch(string method, GameInstallation installation, AppSettings settings, string? extraArgs = null)
    {
        return method.ToLowerInvariant() switch
        {
            "exe" => LaunchExe(installation, settings, extraArgs, null),
            "7launcher" => Launch7Launcher(installation, settings, extraArgs),
            "revloader" => LaunchRevLoader(installation, settings, extraArgs),
            _ => LaunchResult.Fail($"Unknown launch method: {method}")
        };
    }

    private static LaunchResult LaunchExe(
        GameInstallation installation,
        AppSettings settings,
        string? extraArgs,
        LauncherDefinition? definition)
    {
        var exe = installation.CsgoExePath;
        if (string.IsNullOrWhiteSpace(exe) || !File.Exists(exe))
            return LaunchResult.Fail("csgo.exe not found. Set the CS:GO path in Settings.");

        var args = definition?.Args ?? string.Empty;

        // Borderless windowed so the transparent overlay can sit on top of the game.
        args = AppendArg(args, "-windowed");
        args = AppendArg(args, "-noborder");
        
        // Always add insecure mode for overlay compatibility
        // Note: This prevents match creation, use for practice only
        args = AppendArg(args, "-insecure");

        if (!string.IsNullOrWhiteSpace(settings.CustomLaunchArgs))
            args = AppendArgs(args, settings.CustomLaunchArgs);

        if (!string.IsNullOrWhiteSpace(extraArgs))
            args = AppendArgs(args, extraArgs);

        Process.Start(new ProcessStartInfo
        {
            FileName = exe,
            Arguments = args,
            UseShellExecute = true,
            WorkingDirectory = Path.GetDirectoryName(exe)
        });

        return LaunchResult.Ok($"Launched csgo.exe: {exe} {args}");
    }

    private static LaunchResult Launch7Launcher(
        GameInstallation installation,
        AppSettings settings,
        string? extraArgs)
    {
        var exe = settings.SevenLauncherPath;
        if (string.IsNullOrWhiteSpace(exe) || !File.Exists(exe))
            return LaunchResult.Fail("7Launcher not found. Set the 7Launcher path in Settings.");

        var args = "-windowed -noborder -insecure";

        if (!string.IsNullOrWhiteSpace(settings.CustomLaunchArgs))
            args = AppendArgs(args, settings.CustomLaunchArgs);

        if (!string.IsNullOrWhiteSpace(extraArgs))
            args = AppendArgs(args, extraArgs);

        Process.Start(new ProcessStartInfo
        {
            FileName = exe,
            Arguments = args,
            UseShellExecute = true,
            WorkingDirectory = Path.GetDirectoryName(exe)
        });

        return LaunchResult.Ok($"Launched 7Launcher: {exe} {args}");
    }

    private static LaunchResult LaunchRevLoader(
        GameInstallation installation,
        AppSettings settings,
        string? extraArgs)
    {
        var exe = settings.RevLoaderPath;
        if (string.IsNullOrWhiteSpace(exe) || !File.Exists(exe))
            return LaunchResult.Fail("RevLoader not found. Set the RevLoader path in Settings.");

        var args = string.Empty;

        // RevLoader does not support command-line arguments
        // Users must configure windowed mode in RevLoader's settings manually
        // if they want the overlay to appear over the game

        Process.Start(new ProcessStartInfo
        {
            FileName = exe,
            Arguments = args,
            UseShellExecute = true,
            WorkingDirectory = Path.GetDirectoryName(exe)
        });

        return LaunchResult.Ok($"Launched RevLoader: {exe}");
    }

    private static string AppendArgs(string current, string extra)
    {
        foreach (var token in extra.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            current = AppendArg(current, token);
        return current;
    }

    private static string AppendArg(string current, string arg)
    {
        if (string.IsNullOrWhiteSpace(arg))
            return current;
        if (current.Contains(arg, StringComparison.OrdinalIgnoreCase))
            return current;
        return string.IsNullOrWhiteSpace(current) ? arg : current + " " + arg;
    }
}

public sealed class LaunchResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;

    public static LaunchResult Ok(string message) => new() { Success = true, Message = message };
    public static LaunchResult Fail(string message) => new() { Success = false, Message = message };
}
