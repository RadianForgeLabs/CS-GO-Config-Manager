using CSGOConfigManager.Core.Services;

namespace CSGOConfigManager.Services;

/// <summary>
/// Orchestrates the config generation workflow: creates .cfg files, saves them, and copies exec commands to clipboard.
/// </summary>
public sealed class ConfigGenerationService
{
    private readonly ConfigFileService _configFileService;
    private readonly ClipboardService _clipboardService;

    public ConfigGenerationService(ConfigFileService configFileService, ClipboardService clipboardService)
    {
        _configFileService = configFileService;
        _clipboardService = clipboardService;
    }

    /// <summary>
    /// Generates a config file and copies the exec command to clipboard.
    /// </summary>
    /// <param name="cfgDirectory">The CS:GO cfg directory.</param>
    /// <param name="fileName">The config filename.</param>
    /// <param name="commands">The commands to include in the config.</param>
    /// <returns>A result object containing the file path, exec command, and success status.</returns>
    public ConfigGenerationResult GenerateAndCopy(string cfgDirectory, string fileName, IEnumerable<string> commands)
    {
        var result = new ConfigGenerationResult();

        try
        {
            // Validate inputs
            if (string.IsNullOrWhiteSpace(cfgDirectory))
            {
                result.Success = false;
                result.ErrorMessage = "CS:GO cfg directory not configured.";
                return result;
            }

            if (!Directory.Exists(cfgDirectory))
            {
                result.Success = false;
                result.ErrorMessage = $"CS:GO cfg directory does not exist: {cfgDirectory}";
                return result;
            }

            var commandList = commands.ToList();
            if (!commandList.Any())
            {
                result.Success = false;
                result.ErrorMessage = "No commands to generate.";
                return result;
            }

            // Generate the config file
            var filePath = _configFileService.GenerateConfig(cfgDirectory, fileName, commandList);
            result.FilePath = filePath;

            // Generate and copy the exec command
            var execCommand = ConfigFileService.GenerateExecCommand(fileName);
            result.ExecCommand = execCommand;

            _clipboardService.SetText(execCommand);
            result.Success = true;
            result.StatusMessage = "Config generated successfully and exec command copied to clipboard.";
        }
        catch (UnauthorizedAccessException ex)
        {
            result.Success = false;
            result.ErrorMessage = $"Permission denied: {ex.Message}";
        }
        catch (IOException ex)
        {
            result.Success = false;
            result.ErrorMessage = $"File error: {ex.Message}";
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = $"Error: {ex.Message}";
        }

        return result;
    }

    /// <summary>
    /// Copies the exec command for a given filename to the clipboard.
    /// </summary>
    /// <param name="fileName">The config filename.</param>
    /// <returns>True if successful, false otherwise.</returns>
    public bool CopyExecCommand(string fileName)
    {
        try
        {
            var execCommand = ConfigFileService.GenerateExecCommand(fileName);
            _clipboardService.SetText(execCommand);
            return true;
        }
        catch
        {
            return false;
        }
    }
}

/// <summary>
/// Result of a config generation operation.
/// </summary>
public sealed class ConfigGenerationResult
{
    public bool Success { get; set; }
    public string? FilePath { get; set; }
    public string? ExecCommand { get; set; }
    public string? StatusMessage { get; set; }
    public string? ErrorMessage { get; set; }
}
