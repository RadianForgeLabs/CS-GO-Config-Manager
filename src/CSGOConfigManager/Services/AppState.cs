using System.ComponentModel;
using System.Runtime.CompilerServices;
using CSGOConfigManager.Core.Models;
using CSGOConfigManager.Core.Services;

namespace CSGOConfigManager.Services;

/// <summary>
/// Shared application state for the WPF shell.
/// </summary>
public sealed class AppState : INotifyPropertyChanged
{
    private GameInstallation _installation = new();
    private string _statusMessage = "Ready";
    private string _activePage = "Home";

    public AppServices Services { get; }
    public ConfigGenerationService ConfigGeneration { get; }

    public GameInstallation Installation
    {
        get => _installation;
        set
        {
            _installation = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CfgDirectory));
            OnPropertyChanged(nameof(IsGameReady));
        }
    }

    public string? CfgDirectory => Installation.CsgoCfgPath;
    public bool IsGameReady => Installation.IsReady;

    public string StatusMessage
    {
        get => _statusMessage;
        set
        {
            _statusMessage = value;
            OnPropertyChanged();
        }
    }

    public string ActivePage
    {
        get => _activePage;
        set
        {
            _activePage = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler? InstallationChanged;

    public AppState(AppServices services)
    {
        Services = services;
        var clipboardService = new ClipboardService();
        ConfigGeneration = new ConfigGenerationService(services.ConfigFile, clipboardService);
    }

    public void RefreshDetection()
    {
        var settings = Services.Settings.Load();
        Installation = Services.Detection.Detect(settings);
        Services.Log.Info($"Detection complete. Source={Installation.DetectionSource}, Ready={Installation.IsReady}");
        StatusMessage = Installation.IsReady
            ? $"CS:GO ready · {Installation.CsgoRootPath}"
            : "CS:GO not detected — set path in Settings";
        InstallationChanged?.Invoke(this, EventArgs.Empty);
    }

    public void SetStatus(string message)
    {
        StatusMessage = message;
        Services.Log.Info(message);
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
