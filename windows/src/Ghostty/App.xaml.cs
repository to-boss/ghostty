using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
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

        // Register native DLL resolver before any P/Invoke call
        RegisterNativeResolver();

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

    private static void RegisterNativeResolver()
    {
        var assembly = typeof(Ghostty.Interop.NativeMethods).Assembly;
        NativeLibrary.SetDllImportResolver(assembly, ResolveDll);
        Logger.LogInfo("App", "Registered native DLL import resolver");
    }

    private static IntPtr ResolveDll(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
    {
        if (libraryName != "libghostty")
            return IntPtr.Zero;

        // Candidate paths to search, in priority order
        var candidates = new List<string>();

        // 1. Production: DLL next to the exe (zig build copies it here)
        var baseDir = AppContext.BaseDirectory;
        candidates.Add(Path.Combine(baseDir, "libghostty.dll"));

        // 2. Development: walk up to repo root and check zig-out/lib/ghostty.dll
        var repoRoot = FindRepoRoot(baseDir);
        if (repoRoot != null)
        {
            candidates.Add(Path.Combine(repoRoot, "zig-out", "lib", "ghostty.dll"));
            candidates.Add(Path.Combine(repoRoot, "zig-out", "lib", "libghostty.dll"));
        }

        foreach (var candidate in candidates)
        {
            Logger.LogInfo("App", $"DLL search: trying {candidate}");
            if (NativeLibrary.TryLoad(candidate, out var handle))
            {
                Logger.LogInfo("App", $"DLL search: loaded {candidate}");
                return handle;
            }
        }

        Logger.LogCritical("App", "DLL search: libghostty not found in any search path");
        return IntPtr.Zero;
    }

    /// <summary>
    /// Walk up from <paramref name="startDir"/> looking for a directory that contains build.zig
    /// (indicating the Ghostty repo root).
    /// </summary>
    private static string? FindRepoRoot(string startDir)
    {
        var dir = startDir;
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir, "build.zig")))
                return dir;
            dir = Path.GetDirectoryName(dir);
        }
        return null;
    }
}
