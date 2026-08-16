using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using CSGOConfigManager.Services;
using CSGOConfigManager.Views;

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

        // Pre-create overlay on startup to avoid creation issues when game has focus
        try
        {
            _overlay = new OverlayWindow(State, Bots);
            _overlay.Closed += (_, _) => _overlay = null;
            System.Diagnostics.Debug.WriteLine("Overlay pre-created on startup");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to pre-create overlay: {ex.Message}");
        }

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
            State.SetStatus("Global F10 hotkey registered successfully. Overlay may require windowed mode.");
        }
        catch (Exception ex)
        {
            State.SetStatus($"Failed to register global hotkey: {ex.Message}");
            System.Windows.MessageBox.Show($"Failed to register global F10 hotkey: {ex.Message}\n\nNote: The overlay may require CS:GO to be launched in windowed mode.\nYou can still toggle the overlay using the Overlay button in the navigation.", "Hotkey Registration Failed", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
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
        
        // If overlay doesn't exist, create it
        if (_overlay == null)
        {
            try
            {
                _overlay = new OverlayWindow(State, Bots);
                _overlay.Closed += (_, _) => _overlay = null;
                System.Diagnostics.Debug.WriteLine("Created new overlay window");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to create overlay: {ex.Message}");
                State.SetStatus($"Failed to create overlay: {ex.Message}");
                return;
            }
        }
        
        // Toggle visibility
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
            
            System.Diagnostics.Debug.WriteLine("Show() called");
            
            // Force the overlay to appear on top of the game using aggressive methods
            _overlay.ForceShowWindow();
            _overlay.Activate();
            _overlay.BringIntoView();
            
            System.Diagnostics.Debug.WriteLine("ForceShowWindow, Activate, BringIntoView called");
            
            // Additional Windows API call to force focus
            var hwnd = new System.Windows.Interop.WindowInteropHelper(_overlay).Handle;
            if (hwnd != IntPtr.Zero)
            {
                GameWindowService.FocusWindow(hwnd);
                System.Diagnostics.Debug.WriteLine($"FocusWindow called with hwnd: {hwnd}");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("HWND is Zero - window handle not available");
            }
            
            State.SetStatus("Config generator overlay shown. Overlay may require windowed mode. F10 toggles.");
            System.Diagnostics.Debug.WriteLine("Overlay shown and activated");
        }
    }

    public void Dispose()
    {
        _hotkeyService?.Dispose();
        _hotkeyService = null;
    }
}
