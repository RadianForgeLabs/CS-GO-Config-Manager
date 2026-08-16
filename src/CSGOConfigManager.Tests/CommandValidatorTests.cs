using System.Text.Json;
using CSGOConfigManager.Core.Models;
using CSGOConfigManager.Core.Services;

namespace CSGOConfigManager.Tests;

public class CommandValidatorTests
{
    private static CommandDefinition Cmd(string type, double? min = null, double? max = null, params string[] enums) =>
        new()
        {
            Name = "test",
            Type = type,
            Min = min,
            Max = max,
            EnumValues = enums.Length == 0 ? null : enums.ToList(),
            Default = JsonDocument.Parse("0").RootElement.Clone()
        };

    [Theory]
    [InlineData("1", true, "1")]
    [InlineData("0", true, "0")]
    [InlineData("true", true, "1")]
    [InlineData("false", true, "0")]
    [InlineData("maybe", false, null)]
    public void Validate_Boolean(string input, bool valid, string? expected)
    {
        var result = CommandValidator.Validate(Cmd("boolean"), input);
        Assert.Equal(valid, result.IsValid);
        if (valid)
            Assert.Equal(expected, result.NormalizedValue);
    }

    [Fact]
    public void Validate_Integer_ClampsByRange()
    {
        var ok = CommandValidator.Validate(Cmd("integer", 0, 10), "5");
        Assert.True(ok.IsValid);
        Assert.Equal("5", ok.NormalizedValue);

        var low = CommandValidator.Validate(Cmd("integer", 0, 10), "-1");
        Assert.False(low.IsValid);

        var high = CommandValidator.Validate(Cmd("integer", 0, 10), "11");
        Assert.False(high.IsValid);
    }

    [Fact]
    public void Validate_Float()
    {
        var ok = CommandValidator.Validate(Cmd("float", 0, 1), "0.75");
        Assert.True(ok.IsValid);
        Assert.Equal("0.75", ok.NormalizedValue);
    }

    [Fact]
    public void Validate_Enum()
    {
        var ok = CommandValidator.Validate(Cmd("enum", enums: new[] { "fill", "normal" }), "FILL");
        Assert.True(ok.IsValid);
        Assert.Equal("fill", ok.NormalizedValue);

        var bad = CommandValidator.Validate(Cmd("enum", enums: new[] { "fill", "normal" }), "nope");
        Assert.False(bad.IsValid);
    }
}
