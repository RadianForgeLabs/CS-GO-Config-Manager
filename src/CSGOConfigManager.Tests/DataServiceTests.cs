using CSGOConfigManager.Core.Services;

namespace CSGOConfigManager.Tests;

public class DataServiceTests
{
    private static string FindRepoDataPath()
    {
        // Walk up from test output to find source Data folder
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "src", "CSGOConfigManager", "Data", "Commands.json");
            if (File.Exists(candidate))
                return Path.GetDirectoryName(Path.GetDirectoryName(candidate))!; // CSGOConfigManager folder

            // Also check if Data is already next to us (copied output)
            var outputData = Path.Combine(dir.FullName, "Data", "Commands.json");
            if (File.Exists(outputData))
                return dir.FullName;

            dir = dir.Parent;
        }

        // Fallback: use a temp copy from known relative path
        var relative = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "CSGOConfigManager"));
        return relative;
    }

    [Fact]
    public void Loads_Commands_GameModes_And_Launchers()
    {
        var root = FindRepoDataPath();
        var dataDir = Path.Combine(root, "Data");
        Assert.True(Directory.Exists(dataDir), $"Expected Data at {dataDir}");

        var paths = new AppPaths(root);
        var data = new DataService(paths);

        var commands = data.GetCommands();
        Assert.NotEmpty(commands);
        Assert.Contains(commands, c => c.Name == "bot_quota");

        var modes = data.GetGameModes();
        Assert.True(modes.ContainsKey("Casual"));
        Assert.Equal("gamemode_casual.cfg", modes["Casual"]);

        var launchers = data.GetLaunchers();
        Assert.True(launchers.ContainsKey("steam"));
    }

    [Fact]
    public void GetCommandsForMode_FiltersCorrectly()
    {
        var root = FindRepoDataPath();
        var data = new DataService(new AppPaths(root));
        var practice = data.GetCommandsForMode("Custom/Practice");
        Assert.Contains(practice, c => c.Name == "sv_cheats");

        var competitive = data.GetCommandsForMode("Competitive");
        Assert.DoesNotContain(competitive, c => c.Name == "sv_grenade_trajectory");
    }

    [Fact]
    public void FindCommand_IsCaseInsensitive()
    {
        var root = FindRepoDataPath();
        var data = new DataService(new AppPaths(root));
        var cmd = data.FindCommand("BOT_QUOTA");
        Assert.NotNull(cmd);
        Assert.Equal("bot_quota", cmd!.Name);
    }
}
