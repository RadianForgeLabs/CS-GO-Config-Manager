using System.Runtime.Versioning;
using CSGOConfigManager.Core.Models;
using CSGOConfigManager.Core.Services;

namespace CSGOConfigManager.Tests;

[SupportedOSPlatform("windows")]
public class GameDetectionTests
{
    [Fact]
    public void LooksLikeCsgoRoot_RequiresCfgOrExe()
    {
        var temp = Path.Combine(Path.GetTempPath(), "CsgoDetect_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temp);
        try
        {
            Assert.False(GameDetectionService.LooksLikeCsgoRoot(temp));

            Directory.CreateDirectory(Path.Combine(temp, "csgo", "cfg"));
            Assert.True(GameDetectionService.LooksLikeCsgoRoot(temp));

            var cfg = GameDetectionService.ResolveCfgPath(temp);
            Assert.NotNull(cfg);
            Assert.EndsWith(Path.Combine("csgo", "cfg"), cfg!.Replace('/', Path.DirectorySeparatorChar));
        }
        finally
        {
            Directory.Delete(temp, true);
        }
    }

    [Fact]
    public void Detect_UsesSettingsOverride()
    {
        var temp = Path.Combine(Path.GetTempPath(), "CsgoDetect2_" + Guid.NewGuid().ToString("N"));
        var cfg = Path.Combine(temp, "csgo", "cfg");
        Directory.CreateDirectory(cfg);
        File.WriteAllText(Path.Combine(temp, "csgo.exe"), "");

        try
        {
            var service = new GameDetectionService();
            var result = service.Detect(new AppSettings { CsgoPath = temp });
            Assert.True(result.CsgoFound);
            Assert.Equal("User Settings", result.DetectionSource);
            Assert.True(result.IsReady);
        }
        finally
        {
            Directory.Delete(temp, true);
        }
    }
}
