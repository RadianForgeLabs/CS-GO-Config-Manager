namespace CSGOConfigManager.Core.Services;

public sealed class LogService
{
    private readonly AppPaths _paths;
    private readonly object _sync = new();

    public LogService(AppPaths paths)
    {
        _paths = paths;
    }

    public void Info(string message) => Write("INFO", message);
    public void Warn(string message) => Write("WARN", message);
    public void Error(string message, Exception? ex = null)
    {
        if (ex is null)
            Write("ERROR", message);
        else
            Write("ERROR", $"{message} | {ex.GetType().Name}: {ex.Message}");
    }

    private void Write(string level, string message)
    {
        try
        {
            _paths.EnsureDirectories();
            var file = Path.Combine(_paths.Logs, $"app_{DateTime.UtcNow:yyyyMMdd}.log");
            var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{level}] {message}{Environment.NewLine}";
            lock (_sync)
            {
                File.AppendAllText(file, line);
            }
        }
        catch
        {
            // Logging must never crash the app
        }
    }
}
