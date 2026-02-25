using System.ComponentModel;
using System.Windows;
using System.Windows.Interop;
using Ghostty.Controls;
using Ghostty.Helpers;
using Ghostty.Interop;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace Ghostty;

public partial class MainWindow : FluentWindow
{
    private const string LogTag = "MainWindow";

    private GhosttyApp? _ghosttyApp;
    private TerminalControl? _terminalControl;

    public MainWindow()
    {
        Logger.LogInfo(LogTag, "Constructor");
        InitializeComponent();
        Loaded += OnLoaded;
        Closing += OnClosing;
        Activated += OnActivated;
        Deactivated += OnDeactivated;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        Logger.LogInfo(LogTag, "OnLoaded: begin");

        // Detect color scheme
        var colorScheme = ApplicationThemeManager.GetAppTheme() == ApplicationTheme.Dark
            ? GhosttyColorScheme.Dark
            : GhosttyColorScheme.Light;
        Logger.LogInfo(LogTag, $"Color scheme: {colorScheme}");

        // Initialize the ghostty app
        _ghosttyApp = new GhosttyApp();
        _ghosttyApp.TitleChanged += title => Dispatcher.Invoke(() => Title = title);
        _ghosttyApp.CloseRequested += () => Dispatcher.Invoke(Close);

        Logger.LogInfo(LogTag, "Calling GhosttyApp.Initialize...");
        if (!_ghosttyApp.Initialize(colorScheme))
        {
            var logPath = Logger.LogPath ?? "(unknown)";
            Logger.LogCritical(LogTag, "GhosttyApp.Initialize failed");
            System.Windows.MessageBox.Show(
                $"Failed to initialize Ghostty.\n\nCheck the debug log:\n{logPath}",
                "Error",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            Close();
            return;
        }
        Logger.LogInfo(LogTag, "GhosttyApp initialized successfully");

        // Create the terminal control
        Logger.LogInfo(LogTag, "Creating TerminalControl...");
        _terminalControl = new TerminalControl(_ghosttyApp);
        TerminalHost.Child = _terminalControl;
        Logger.LogInfo(LogTag, "TerminalControl assigned to host");

        // Set initial focus
        _ghosttyApp.SetFocus(true);
        Logger.LogInfo(LogTag, "OnLoaded: complete");
    }

    private void OnClosing(object? sender, CancelEventArgs e)
    {
        Logger.LogInfo(LogTag, "OnClosing");

        if (_ghosttyApp != null && _ghosttyApp.NeedsConfirmQuit())
        {
            var result = System.Windows.MessageBox.Show(
                "A process is still running. Are you sure you want to close?",
                "Ghostty",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Warning);

            if (result != System.Windows.MessageBoxResult.Yes)
            {
                Logger.LogInfo(LogTag, "Close cancelled by user");
                e.Cancel = true;
                return;
            }
        }

        Logger.LogInfo(LogTag, "Disposing terminal and app");
        _terminalControl?.Dispose();
        _terminalControl = null;

        _ghosttyApp?.Dispose();
        _ghosttyApp = null;
    }

    private void OnActivated(object? sender, EventArgs e)
    {
        _ghosttyApp?.SetFocus(true);
    }

    private void OnDeactivated(object? sender, EventArgs e)
    {
        _ghosttyApp?.SetFocus(false);
    }
}
