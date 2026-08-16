using CSGOConfigManager.Core.Models;
using CSGOConfigManager.Core.Parsing;

namespace CSGOConfigManager.Tests;

public class CfgParserTests
{
    [Fact]
    public void Parse_PreservesCommentsAndCommands()
    {
        const string content = """
            // header
            bot_quota 10
            bot_difficulty 2
            sensitivity "2.5"

            mp_friendlyfire 1 // inline
            """;

        var doc = CfgParser.Parse("autoexec.cfg", content);

        Assert.Contains(doc.Entries, e => e.Kind == ConfigLineKind.Comment);
        Assert.Equal("10", doc.GetValue("bot_quota"));
        Assert.Equal("2", doc.GetValue("bot_difficulty"));
        Assert.Equal("2.5", doc.GetValue("sensitivity"));
        Assert.Equal("1", doc.GetValue("mp_friendlyfire"));
    }

    [Fact]
    public void SetValue_UpdatesExistingAndAddsMissing()
    {
        var doc = CfgParser.Parse("test.cfg", "bot_quota 5\n");
        doc.SetValue("bot_quota", "12");
        doc.SetValue("sv_cheats", "1");

        Assert.Equal("12", doc.GetValue("bot_quota"));
        Assert.Equal("1", doc.GetValue("sv_cheats"));
        Assert.True(doc.IsDirty);
    }

    [Fact]
    public void Serialize_RoundTripsCommands()
    {
        var doc = CfgParser.Parse("test.cfg", "volume 0.8\nsensitivity 1.5\n");
        doc.SetValue("volume", "1.0");
        var text = CfgParser.Serialize(doc);
        var again = CfgParser.Parse("test.cfg", text);

        Assert.Equal("1.0", again.GetValue("volume"));
        Assert.Equal("1.5", again.GetValue("sensitivity"));
    }

    [Fact]
    public void Parse_HandlesQuotedValuesWithSpaces()
    {
        var doc = CfgParser.Parse("test.cfg", "name \"Player One\"\n");
        Assert.Equal("Player One", doc.GetValue("name"));
    }
}
