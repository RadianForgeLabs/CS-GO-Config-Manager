using System;
using System.Windows.Input;
using CSGOConfigManager.Services;
using CSGOConfigManager.Views;
using System.Windows;

namespace CSGOConfigManager.ViewModels;

public sealed class MainViewModel : ViewModelBase, IDisposable
{
    private object? _currentPage;
    private string _currentPageName = "Home";
    private OverlayWindow? _overlay;
    private GlobalHotkeyService? _hotkeyService;

    public AppState State { get; }

    public HomeViewModel Home { get; }
    public LaunchViewModel Launch { get; }
    public GameModesViewModel GameModes { get; }
    public BotManagerViewModel Bots { get; }
    public PracticeViewModel Practice { get; }
    public CommandBrowserViewModel Commands { get; }
    public ConfigEditorViewModel ConfigEditor { get; }
    public ProfilesViewModel Profiles { get; }
    public BackupsViewModel Backups { get; }
    public ConflictsViewModel Conflicts { get; }
    public SettingsViewModel Settings { get; }

    public object? CurrentPage
    {
        get => _currentPage;
        set => SetProperty(ref _currentPage, value);
    }

    public string CurrentPageName
    {
        get => _currentPageName;
        set
        {
            SetProperty(ref _currentPageName, value);
            State.ActivePage = value;
            OnPropertyChanged(nameof(IsHome));
            OnPropertyChanged(nameof(IsLaunch));
            OnPropertyChanged(nameof(IsModes));
            OnPropertyChanged(nameof(IsBots));
            OnPropertyChanged(nameof(IsPractice));
            OnPropertyChanged(nameof(IsCommands));
            OnPropertyChanged(nameof(IsConfig));
            OnPropertyChanged(nameof(IsProfiles));
            OnPropertyChanged(nameof(IsBackups));
            OnPropertyChanged(nameof(IsConflicts));
            OnPropertyChanged(nameof(IsSettings));
        }
    }

    public bool IsHome => CurrentPageName == "Home";
    public bool IsLaunch => CurrentPageName == "Launch";
    public bool IsModes => CurrentPageName == "Modes";
    public bool IsBots => CurrentPageName == "Bots";
    public bool IsPractice => CurrentPageName == "Practice";
    public bool IsCommands => CurrentPageName == "Commands";
    public bool IsConfig => CurrentPageName == "Config";
    public bool IsProfiles => CurrentPageName == "Profiles";
    public bool IsBackups => CurrentPageName == "Backups";
    public bool IsConflicts => CurrentPageName == "Conflicts";
    public bool IsSettings => CurrentPageName == "Settings";

    public string StatusMessage => State.StatusMessage;

    public ICommand NavigateCommand { get; }
    public ICommand ToggleOverlayCommand { get; }

    public MainViewModel(AppState state, Window mainWindow)
    {
        State = state;
        state.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(AppState.StatusMessage))
                OnPropertyChanged(nameof(StatusMessage));
        };

        Home = new HomeViewModel(state);
        Launch = new LaunchViewModel(state);
        GameModes = new GameModesViewModel(state);
        Bots = new BotManagerViewModel(state);
        Practice = new PracticeViewModel(state);
        Commands = new CommandBrowserViewModel(state);
        ConfigEditor = new ConfigEditorViewModel(state);
        Profiles = new ProfilesViewModel(state);
        Backups = new BackupsViewModel(state);
        Conflicts = new ConflictsViewModel(state);
        Settings = new SettingsViewModel(state);

        NavigateCommand = new RelayCommand(p => Navigate(p?.ToString() ?? "Home"));
        ToggleOverlayCommand = new RelayCommand(ToggleOverlay);

        // Register global F10 hotkey (works even when CS:GO has focus)
        try
        {
            _hotkeyService = new GlobalHotkeyService(mainWindow, Key.F10);
            _hotkeyService.HotkeyPressed += () =>
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    ToggleOverlay();
                });
            };
            State.SetStatus("Global F10 hotkey registered successfully. ⚠️ Overlay requires insecure mode.");
        }
        catch (Exception ex)
        {
            State.SetStatus($"Failed to register global hotkey: {ex.Message}");
            System.Windows.MessageBox.Show($"Failed to register global F10 hotkey: {ex.Message}\n\n⚠️ Note: The overlay requires CS:GO to be launched in insecure mode.\nYou can still toggle the overlay using the Overlay button in the navigation.", "Hotkey Registration Failed", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
        }

        Navigate("Home");
    }

    public void Navigate(string page)
    {
        if (page == "Overlay")
        {
            ToggleOverlay();
            return;
        }

        CurrentPageName = page;
        CurrentPage = page switch
        {
            "Home" => Home,
            "Launch" => Launch,
            "Modes" => GameModes,
            "Bots" => Bots,
            "Practice" => Practice,
            "Commands" => Commands,
            "Config" => ConfigEditor,
            "Profiles" => Profiles,
            "Backups" => Backups,
            "Conflicts" => Conflicts,
            "Settings" => Settings,
            _ => Home
        };

        switch (page)
        {
            case "Home":
                Home.Refresh();
                break;
            case "Modes":
                GameModes.LoadCommands();
                break;
            case "Bots":
                Bots.Load();
                break;
            case "Practice":
                Practice.Load();
                break;
            case "Commands":
                Commands.Reload();
                break;
            case "Config":
                ConfigEditor.ReloadList();
                break;
            case "Profiles":
                Profiles.Reload();
                Launch.ReloadProfiles();
                break;
            case "Backups":
                Backups.Reload();
                break;
            case "Conflicts":
                Conflicts.Reload();
                break;
            case "Launch":
                Launch.ReloadProfiles();
                break;
        }
    }

    private void ToggleOverlay()
    {
        System.Diagnostics.Debug.WriteLine($"ToggleOverlay called at {DateTime.Now:HH:mm:ss.fff}");
        
        if (_overlay is null)
        {
            // Do not set Owner — an owned window stays above the main app
            // but drops behind CS:GO the moment the game is focused.
            _overlay = new OverlayWindow(State, Bots);
            // Don't set _overlay to null on close since we use Hide() instead
            System.Diagnostics.Debug.WriteLine("Created new overlay window");
        }

        if (_overlay.IsVisible)
        {
            _overlay.Hide();
            State.SetStatus("Overlay hidden. Press F10 to show.");
            System.Diagnostics.Debug.WriteLine("Overlay hidden");
        }
        else
        {
            _overlay.Show();
            _overlay.Topmost = true;
            _overlay.Activate(); // Bring overlay to front and give it focus
            State.SetStatus("Config generator overlay shown. ⚠️ Insecure mode required. F10 toggles.");
            System.Diagnostics.Debug.WriteLine("Overlay shown and activated");
        }
    }

    public void Dispose()
    {
        _hotkeyService?.Dispose();
        _hotkeyService = null;
    }
}
