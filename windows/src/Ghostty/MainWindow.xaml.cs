using System.ComponentModel;
using System.Windows;
using System.Windows.Interop;
using Ghostty.Controls;
using Ghostty.Interop;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace Ghostty;

public partial class MainWindow : FluentWindow
{
    private GhosttyApp? _ghosttyApp;
    private TerminalControl? _terminalControl;

    public MainWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Closing += OnClosing;
        Activated += OnActivated;
        Deactivated += OnDeactivated;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        // Detect color scheme
        var colorScheme = ApplicationThemeManager.GetAppTheme() == ApplicationTheme.Dark
            ? GhosttyColorScheme.Dark
            : GhosttyColorScheme.Light;

        // Initialize the ghostty app
        _ghosttyApp = new GhosttyApp();
        _ghosttyApp.TitleChanged += title => Dispatcher.Invoke(() => Title = title);
        _ghosttyApp.CloseRequested += () => Dispatcher.Invoke(Close);

        if (!_ghosttyApp.Initialize(colorScheme))
        {
            System.Windows.MessageBox.Show("Failed to initialize Ghostty.", "Error",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            Close();
            return;
        }

        // Create the terminal control
        _terminalControl = new TerminalControl(_ghosttyApp);
        TerminalHost.Child = _terminalControl;

        // Set initial focus
        _ghosttyApp.SetFocus(true);
    }

    private void OnClosing(object? sender, CancelEventArgs e)
    {
        if (_ghosttyApp != null && _ghosttyApp.NeedsConfirmQuit())
        {
            var result = System.Windows.MessageBox.Show(
                "A process is still running. Are you sure you want to close?",
                "Ghostty",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Warning);

            if (result != System.Windows.MessageBoxResult.Yes)
            {
                e.Cancel = true;
                return;
            }
        }

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
