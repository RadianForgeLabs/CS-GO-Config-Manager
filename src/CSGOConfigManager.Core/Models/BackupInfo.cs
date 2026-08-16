namespace CSGOConfigManager.Core.Models;

public sealed class BackupInfo
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string DirectoryPath { get; init; } = string.Empty;
    public DateTime CreatedUtc { get; init; }
    public bool IsManual { get; init; }
    public List<string> Files { get; init; } = new();
    public string DisplayName => IsManual
        ? $"{Name} ({CreatedUtc.ToLocalTime():g})"
        : $"Auto {CreatedUtc.ToLocalTime():g}";
}
