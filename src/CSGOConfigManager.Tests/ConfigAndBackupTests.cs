using CSGOConfigManager.Core.Services;

namespace CSGOConfigManager.Tests;

public class ConfigAndBackupTests : IDisposable
{
    private readonly string _root;
    private readonly string _cfgDir;
    private readonly AppServices _services;

    public ConfigAndBackupTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "CSGOCfgMgrTests_" + Guid.NewGuid().ToString("N"));
        _cfgDir = Path.Combine(_root, "game", "csgo", "cfg");
        Directory.CreateDirectory(_cfgDir);

        // Seed Data files from project
        var projectData = FindProjectData();
        var dataTarget = Path.Combine(_root, "Data");
        Directory.CreateDirectory(dataTarget);
        foreach (var file in Directory.GetFiles(projectData, "*.json", SearchOption.AllDirectories))
        {
            var rel = Path.GetRelativePath(projectData, file);
            var dest = Path.Combine(dataTarget, rel);
            Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
            File.Copy(file, dest, true);
        }

        File.WriteAllText(Path.Combine(_cfgDir, "autoexec.cfg"), "bot_quota 5\nmp_warmuptime 30\n");
        File.WriteAllText(Path.Combine(_cfgDir, "gamemode_casual.cfg"), "bot_quota 1\n");

        _services = new AppServices(_root);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_root))
                Directory.Delete(_root, true);
        }
        catch
        {
            // ignore cleanup failures on Windows file locks
        }
    }

    [Fact]
    public void ApplyValues_WritesValidatedCommands()
    {
        var values = new Dictionary<string, string>
        {
            ["bot_quota"] = "8",
            ["volume"] = "0.5"
        };

        var touched = _services.Config.ApplyValues(_cfgDir, values);
        Assert.NotEmpty(touched);

        var quota = _services.Config.GetCurrentValue(_cfgDir, "bot_quota", "autoexec.cfg");
        Assert.Equal("8", quota);
    }

    [Fact]
    public void ApplyValues_RejectsOutOfRange()
    {
        var values = new Dictionary<string, string> { ["bot_quota"] = "999" };
        Assert.Throws<InvalidOperationException>(() => _services.Config.ApplyValues(_cfgDir, values));
    }

    [Fact]
    public void Backup_CreateRestore_Works()
    {
        var backup = _services.Backups.CreateFullCfgBackup("unit_test", _cfgDir);
        Assert.True(Directory.Exists(backup.DirectoryPath));
        Assert.Contains(backup.Files, f => f.Contains("autoexec", StringComparison.OrdinalIgnoreCase));

        File.WriteAllText(Path.Combine(_cfgDir, "autoexec.cfg"), "bot_quota 99\n");
        _services.Backups.Restore(backup, _cfgDir);

        var text = File.ReadAllText(Path.Combine(_cfgDir, "autoexec.cfg"));
        Assert.Contains("bot_quota 5", text);
    }

    [Fact]
    public void ConflictService_DetectsOverrides()
    {
        var conflicts = _services.Conflicts.DetectConflicts(_cfgDir, new[] { "bot_quota" });
        var bot = Assert.Single(conflicts);
        Assert.True(bot.HasConflict);
        Assert.Equal("1", bot.EffectiveValue); // gamemode has higher priority than autoexec
        Assert.Equal("gamemode_casual.cfg", bot.EffectiveSource);
    }

    [Fact]
    public void Profile_SaveApply_Works()
    {
        var profile = new Core.Models.ProfileDefinition
        {
            Name = "TestProfile",
            Description = "unit",
            Values = new Dictionary<string, string> { ["bot_quota"] = "7", ["bot_difficulty"] = "2" }
        };

        _services.Profiles.SaveProfile(profile);
        var loaded = _services.Profiles.GetProfile("TestProfile");
        Assert.NotNull(loaded);

        _services.Profiles.ApplyProfile(loaded!, _cfgDir);
        Assert.Equal("7", _services.Config.GetCurrentValue(_cfgDir, "bot_quota", "autoexec.cfg"));
    }

    private static string FindProjectData()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "src", "CSGOConfigManager", "Data");
            if (Directory.Exists(candidate) && File.Exists(Path.Combine(candidate, "Commands.json")))
                return candidate;

            dir = dir.Parent;
        }

        var fallback = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "CSGOConfigManager", "Data"));
        if (!Directory.Exists(fallback))
            throw new DirectoryNotFoundException("Could not locate project Data folder for tests.");
        return fallback;
    }
}
