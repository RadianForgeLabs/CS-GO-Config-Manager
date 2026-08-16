using System.IO.Compression;
using System.Text.Json;
using CSGOConfigManager.Core.Models;

namespace CSGOConfigManager.Core.Services;

public sealed class BackupService
{
    private readonly AppPaths _paths;
    private readonly SettingsService _settingsService;

    public BackupService(AppPaths paths, SettingsService settingsService)
    {
        _paths = paths;
        _settingsService = settingsService;
    }

    public BackupInfo CreateAutoBackup(IEnumerable<string> filePaths)
    {
        var stamp = DateTime.UtcNow.ToString("yyyy-MM-dd_HHmmss");
        return CreateBackup($"auto_{stamp}", filePaths, isManual: false);
    }

    public BackupInfo CreateManualBackup(string name, IEnumerable<string> filePaths)
    {
        var safeName = string.Join("_", name.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries)).Trim();
        if (string.IsNullOrWhiteSpace(safeName))
            safeName = "manual";

        var stamp = DateTime.UtcNow.ToString("yyyy-MM-dd_HHmmss");
        return CreateBackup($"{safeName}_{stamp}", filePaths, isManual: true);
    }

    public BackupInfo CreateFullCfgBackup(string name, string cfgDirectory)
    {
        if (!Directory.Exists(cfgDirectory))
            throw new DirectoryNotFoundException($"Config directory not found: {cfgDirectory}");

        var files = Directory.GetFiles(cfgDirectory, "*.cfg", SearchOption.TopDirectoryOnly);
        return CreateManualBackup(name, files);
    }

    public IReadOnlyList<BackupInfo> ListBackups()
    {
        _paths.EnsureDirectories();
        var results = new List<BackupInfo>();

        foreach (var dir in Directory.GetDirectories(_paths.Backups))
        {
            var metaPath = Path.Combine(dir, "backup.json");
            if (File.Exists(metaPath))
            {
                try
                {
                    var json = File.ReadAllText(metaPath);
                    var info = JsonSerializer.Deserialize<BackupInfo>(json, DataService.SharedJsonOptions);
                    if (info is not null)
                    {
                        results.Add(info);
                        continue;
                    }
                }
                catch
                {
                    // Fall through to inferred metadata
                }
            }

            var name = Path.GetFileName(dir);
            var created = Directory.GetCreationTimeUtc(dir);
            results.Add(new BackupInfo
            {
                Id = name,
                Name = name,
                DirectoryPath = dir,
                CreatedUtc = created,
                IsManual = !name.StartsWith("auto_", StringComparison.OrdinalIgnoreCase),
                Files = Directory.GetFiles(dir, "*.cfg*", SearchOption.TopDirectoryOnly)
                    .Select(Path.GetFileName)
                    .Where(f => f is not null && !f.Equals("backup.json", StringComparison.OrdinalIgnoreCase))
                    .Cast<string>()
                    .ToList()
            });
        }

        return results.OrderByDescending(b => b.CreatedUtc).ToList();
    }

    public void Restore(BackupInfo backup, string cfgDirectory, bool overwrite = true)
    {
        if (!Directory.Exists(backup.DirectoryPath))
            throw new DirectoryNotFoundException($"Backup folder not found: {backup.DirectoryPath}");

        Directory.CreateDirectory(cfgDirectory);

        foreach (var file in Directory.GetFiles(backup.DirectoryPath))
        {
            var name = Path.GetFileName(file);
            if (name.Equals("backup.json", StringComparison.OrdinalIgnoreCase))
                continue;

            // Strip .bak suffix if present
            if (name.EndsWith(".bak", StringComparison.OrdinalIgnoreCase))
                name = name[..^4];

            var dest = Path.Combine(cfgDirectory, name);
            if (!overwrite && File.Exists(dest))
                continue;

            File.Copy(file, dest, overwrite: true);
        }
    }

    public void Delete(BackupInfo backup)
    {
        if (Directory.Exists(backup.DirectoryPath))
            Directory.Delete(backup.DirectoryPath, recursive: true);
    }

    public string ExportZip(BackupInfo backup, string destinationZipPath)
    {
        if (File.Exists(destinationZipPath))
            File.Delete(destinationZipPath);

        ZipFile.CreateFromDirectory(backup.DirectoryPath, destinationZipPath);
        return destinationZipPath;
    }

    public string Diff(string currentFilePath, BackupInfo backup)
    {
        var fileName = Path.GetFileName(currentFilePath);
        var backupFile = Path.Combine(backup.DirectoryPath, fileName);
        if (!File.Exists(backupFile))
            backupFile = Path.Combine(backup.DirectoryPath, fileName + ".bak");

        if (!File.Exists(backupFile))
            return $"No backup copy of '{fileName}' found in this backup.";

        var currentLines = File.Exists(currentFilePath)
            ? File.ReadAllLines(currentFilePath)
            : Array.Empty<string>();
        var backupLines = File.ReadAllLines(backupFile);

        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"--- backup/{fileName}");
        sb.AppendLine($"+++ current/{fileName}");

        var max = Math.Max(currentLines.Length, backupLines.Length);
        for (var i = 0; i < max; i++)
        {
            var left = i < backupLines.Length ? backupLines[i] : null;
            var right = i < currentLines.Length ? currentLines[i] : null;

            if (left == right)
            {
                sb.AppendLine($"  {right}");
            }
            else
            {
                if (left is not null)
                    sb.AppendLine($"- {left}");
                if (right is not null)
                    sb.AppendLine($"+ {right}");
            }
        }

        return sb.ToString();
    }

    private BackupInfo CreateBackup(string folderName, IEnumerable<string> filePaths, bool isManual)
    {
        _paths.EnsureDirectories();
        var dir = Path.Combine(_paths.Backups, folderName);
        Directory.CreateDirectory(dir);

        var copied = new List<string>();
        foreach (var path in filePaths.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (!File.Exists(path))
                continue;

            var destName = Path.GetFileName(path);
            var dest = Path.Combine(dir, destName);
            File.Copy(path, dest, overwrite: true);
            copied.Add(destName);
        }

        var info = new BackupInfo
        {
            Id = folderName,
            Name = folderName,
            DirectoryPath = dir,
            CreatedUtc = DateTime.UtcNow,
            IsManual = isManual,
            Files = copied
        };

        var meta = JsonSerializer.Serialize(info, DataService.SharedJsonOptions);
        File.WriteAllText(Path.Combine(dir, "backup.json"), meta);

        EnforceRetention();
        return info;
    }

    private void EnforceRetention()
    {
        var max = Math.Max(5, _settingsService.Current.MaxBackupCount);
        var all = ListBackups();
        foreach (var old in all.Skip(max))
        {
            try
            {
                Delete(old);
            }
            catch
            {
                // Best-effort cleanup
            }
        }
    }
}
