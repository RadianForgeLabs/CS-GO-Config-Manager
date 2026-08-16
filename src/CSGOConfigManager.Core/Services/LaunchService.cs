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
        
        // Add borderless windowed mode args for overlay compatibility
        // -windowed -noborder makes the game run in borderless windowed mode
        // which allows the WPF overlay to show on top
        var borderlessArgs = "-windowed -noborder -insecure";
        if (string.IsNullOrWhiteSpace(args))
            args = borderlessArgs;
        else if (!args.Contains("-windowed", StringComparison.OrdinalIgnoreCase))
            args = args + " " + borderlessArgs;
        else if (!args.Contains("-insecure", StringComparison.OrdinalIgnoreCase))
            args = args + " -insecure";

        if (!string.IsNullOrWhiteSpace(settings.CustomLaunchArgs))
            args = args + " " + settings.CustomLaunchArgs.Trim();

        if (!string.IsNullOrWhiteSpace(extraArgs))
            args = args + " " + extraArgs.Trim();

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

        var args = string.Empty;

        // Add borderless windowed mode args for overlay compatibility
        // -windowed -noborder makes the game run in borderless windowed mode
        // which allows the WPF overlay to show on top
        // -insecure disables VAC to allow overlay without triggering anti-cheat
        args = "-windowed -noborder -insecure";

        if (!string.IsNullOrWhiteSpace(settings.CustomLaunchArgs))
            args = args + " " + settings.CustomLaunchArgs.Trim();

        if (!string.IsNullOrWhiteSpace(extraArgs))
            args = args + " " + extraArgs.Trim();

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

        // RevLoader needs windowed mode for overlay to work
        // Note: If RevLoader has its own config file, these args may be ignored
        // You may need to configure RevLoader's settings manually
        args = "-windowed -noborder -insecure";

        Process.Start(new ProcessStartInfo
        {
            FileName = exe,
            Arguments = args,
            UseShellExecute = true,
            WorkingDirectory = Path.GetDirectoryName(exe)
        });

        return LaunchResult.Ok($"Launched RevLoader: {exe} {args}");
    }
}

public sealed class LaunchResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;

    public static LaunchResult Ok(string message) => new() { Success = true, Message = message };
    public static LaunchResult Fail(string message) => new() { Success = false, Message = message };
}
