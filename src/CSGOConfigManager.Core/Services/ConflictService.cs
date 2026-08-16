using CSGOConfigManager.Core.Models;

namespace CSGOConfigManager.Core.Services;

/// <summary>
/// Detects command overrides across CS:GO config layers.
/// Priority (lowest → highest): config.cfg → autoexec.cfg → gamemode_*.cfg → practice.cfg
/// </summary>
public sealed class ConflictService
{
    private static readonly Dictionary<string, int> FilePriority = new(StringComparer.OrdinalIgnoreCase)
    {
        ["config.cfg"] = 10,
        ["autoexec.cfg"] = 20,
        ["gamemode_casual.cfg"] = 30,
        ["gamemode_competitive.cfg"] = 30,
        ["gamemode_deathmatch.cfg"] = 30,
        ["gamemode_armsrace.cfg"] = 30,
        ["gamemode_demolition.cfg"] = 30,
        ["gamemode_cooperative.cfg"] = 30,
        ["gamemode_custom.cfg"] = 30,
        ["practice.cfg"] = 40
    };

    private readonly ConfigService _configService;

    public ConflictService(ConfigService configService)
    {
        _configService = configService;
    }

    public IReadOnlyList<ConflictInfo> DetectConflicts(string cfgDirectory, IEnumerable<string>? commandFilter = null)
    {
        if (string.IsNullOrWhiteSpace(cfgDirectory) || !Directory.Exists(cfgDirectory))
            return Array.Empty<ConflictInfo>();

        var files = _configService.ListConfigFiles(cfgDirectory);
        var byCommand = new Dictionary<string, List<ConfigSourceValue>>(StringComparer.OrdinalIgnoreCase);

        foreach (var filePath in files)
        {
            var fileName = Path.GetFileName(filePath);
            var priority = FilePriority.TryGetValue(fileName, out var p) ? p : 25;
            var doc = _configService.Load(filePath);

            foreach (var (command, value) in doc.GetCommandValues())
            {
                if (commandFilter is not null &&
                    !commandFilter.Contains(command, StringComparer.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (!byCommand.TryGetValue(command, out var list))
                {
                    list = new List<ConfigSourceValue>();
                    byCommand[command] = list;
                }

                list.Add(new ConfigSourceValue
                {
                    SourceFile = fileName,
                    Value = value,
                    Priority = priority
                });
            }
        }

        var results = new List<ConflictInfo>();
        foreach (var (command, sources) in byCommand)
        {
            var ordered = sources
                .OrderBy(s => s.Priority)
                .ThenBy(s => s.SourceFile, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var effective = ordered.Last();
            var marked = ordered.Select(s => new ConfigSourceValue
            {
                SourceFile = s.SourceFile,
                Value = s.Value,
                Priority = s.Priority,
                IsEffective = ReferenceEquals(s, effective) ||
                              (s.SourceFile == effective.SourceFile && s.Value == effective.Value && s.Priority == effective.Priority)
            }).ToList();

            // Fix effective marking for value equality edge cases
            for (var i = 0; i < marked.Count; i++)
            {
                marked[i] = new ConfigSourceValue
                {
                    SourceFile = ordered[i].SourceFile,
                    Value = ordered[i].Value,
                    Priority = ordered[i].Priority,
                    IsEffective = i == ordered.Count - 1
                };
            }

            results.Add(new ConflictInfo
            {
                CommandName = command,
                Sources = marked,
                EffectiveValue = effective.Value,
                EffectiveSource = effective.SourceFile
            });
        }

        return results
            .OrderByDescending(c => c.HasConflict)
            .ThenBy(c => c.CommandName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public ConflictInfo? GetConflict(string cfgDirectory, string commandName) =>
        DetectConflicts(cfgDirectory, new[] { commandName })
            .FirstOrDefault(c => string.Equals(c.CommandName, commandName, StringComparison.OrdinalIgnoreCase));
}
