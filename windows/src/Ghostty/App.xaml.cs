using System.Windows;
using System.Windows.Threading;
using Ghostty.Helpers;
using Wpf.Ui.Appearance;

namespace Ghostty;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Initialize logging first — everything else depends on it
        Logger.Initialize();
        Logger.LogInfo("App", "OnStartup begin");

        // Enable libghostty stderr logging (disabled by default in library mode)
        Environment.SetEnvironmentVariable("GHOSTTY_LOG", "stderr");
        Logger.LogInfo("App", "Set GHOSTTY_LOG=stderr");

        // Global exception handlers
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            Logger.LogCritical("App", $"Unhandled exception: {args.ExceptionObject}");
            Logger.Shutdown();
        };

        DispatcherUnhandledException += (_, args) =>
        {
            Logger.LogCritical("App", $"Dispatcher unhandled exception: {args.Exception}");
            args.Handled = false; // let the app crash after logging
        };

        // Apply the system theme
        ApplicationThemeManager.Apply(ApplicationTheme.Dark);
        Logger.LogInfo("App", "OnStartup complete");
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Logger.LogInfo("App", $"OnExit (code={e.ApplicationExitCode})");
        Logger.Shutdown();
        base.OnExit(e);
    }
}
