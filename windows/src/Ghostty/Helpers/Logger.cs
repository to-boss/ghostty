using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace Ghostty.Helpers;

/// <summary>
/// Static logger that writes to a debug log file and the debugger output window.
/// Log file: %APPDATA%\Ghostty\debug.log (overwritten each launch).
/// </summary>
public static class Logger
{
    public enum Level
    {
        Debug,
        Info,
        Warning,
        Error,
        Critical,
    }

    private static StreamWriter? _writer;
    private static readonly object _lock = new();
    private static string? _logPath;

    public static string? LogPath => _logPath;

    public static void Initialize()
    {
        try
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var logDir = Path.Combine(appData, "Ghostty");
            Directory.CreateDirectory(logDir);

            _logPath = Path.Combine(logDir, "debug.log");
            _writer = new StreamWriter(_logPath, append: false) { AutoFlush = true };

            _writer.WriteLine($"=== Ghostty Debug Log ===");
            _writer.WriteLine($"Time:    {DateTime.Now:O}");
            _writer.WriteLine($"OS:      {Environment.OSVersion}");
            _writer.WriteLine($".NET:    {Environment.Version}");
            _writer.WriteLine($"Process: {Environment.ProcessPath}");
            _writer.WriteLine($"Arch:    {RuntimeInformation.ProcessArchitecture}");
            _writer.WriteLine($"CWD:     {Environment.CurrentDirectory}");
            _writer.WriteLine(new string('=', 40));
            _writer.WriteLine();
        }
        catch
        {
            // If we can't open the log file, fall back to debug output only
            _writer = null;
        }
    }

    public static void Log(Level level, string category, string message)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
        var line = $"[{timestamp}] [{level,-8}] [{category}] {message}";

        Debug.WriteLine(line);

        lock (_lock)
        {
            _writer?.WriteLine(line);
        }
    }

    public static void LogDebug(string category, string message) => Log(Level.Debug, category, message);
    public static void LogInfo(string category, string message) => Log(Level.Info, category, message);
    public static void LogWarning(string category, string message) => Log(Level.Warning, category, message);
    public static void LogError(string category, string message) => Log(Level.Error, category, message);
    public static void LogCritical(string category, string message) => Log(Level.Critical, category, message);

    public static void LogWin32Error(string category, string context)
    {
        var error = Marshal.GetLastWin32Error();
        Log(Level.Error, category, $"{context}: Win32 error {error} (0x{error:X})");
    }

    public static void Shutdown()
    {
        lock (_lock)
        {
            _writer?.Flush();
            _writer?.Dispose();
            _writer = null;
        }
    }
}
