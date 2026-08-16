using System.Windows;
using System.Windows.Interop;
using CSGOConfigManager.Core.Services;
using CSGOConfigManager.Services;
using CSGOConfigManager.ViewModels;

namespace CSGOConfigManager;

public partial class MainWindow : Window
{
    private MainViewModel? _viewModel;

    public MainWindow()
    {
        InitializeComponent();

        var services = new AppServices();
        var state = new AppState(services);
        state.RefreshDetection();

        // RegisterHotKey needs a real HWND. The handle is normally created
        // only when the window is shown — force it now so F10 works in-game.
        _ = new WindowInteropHelper(this).EnsureHandle();

        _viewModel = new MainViewModel(state, this);
        DataContext = _viewModel;
        services.Log.Info("Application started.");

        // Cleanup on close
        Closed += (_, _) => _viewModel?.Dispose();
    }
}
