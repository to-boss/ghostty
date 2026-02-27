using System.Runtime.InteropServices;
using System.Windows;
using Ghostty.Helpers;

namespace Ghostty.Interop;

/// <summary>
/// Managed wrapper around the ghostty app lifecycle.
/// Owns the ghostty_app_t and ghostty_config_t handles and all callback delegates.
/// </summary>
public sealed class GhosttyApp : IDisposable
{
    private const string Tag = "GhosttyApp";

    private IntPtr _app;
    private IntPtr _config;
    private bool _disposed;

    // Keep delegate instances alive to prevent GC from collecting them
    // while native code still holds function pointers.
    private WakeupCallback? _wakeupDelegate;
    private ActionCallback? _actionDelegate;
    private ReadClipboardCallback? _readClipboardDelegate;
    private ConfirmReadClipboardCallback? _confirmReadClipboardDelegate;
    private WriteClipboardCallback? _writeClipboardDelegate;
    private CloseSurfaceCallback? _closeSurfaceDelegate;
    private GlMakeCurrentCallback? _glMakeCurrentDelegate;
    private GlSwapBuffersCallback? _glSwapBuffersDelegate;

    // Events for the frontend
    public event Action<string>? TitleChanged;
    public event Action? CloseRequested;
    public event Action<GhosttyMouseShape>? MouseShapeChanged;
    public event Action<uint, uint>? InitialSizeReceived;
    public event Action<uint, uint>? CellSizeReceived;

    // GL context callbacks to be set by the TerminalControl
    public Action? GlMakeCurrentAction { get; set; }
    public Action? GlSwapBuffersAction { get; set; }

    public IntPtr AppHandle => _app;

    public bool Initialize(GhosttyColorScheme colorScheme)
    {
        try
        {
            Logger.LogInfo(Tag, "Calling ghostty_init...");
            var result = NativeMethods.GhosttyInit(0, IntPtr.Zero);
            Logger.LogInfo(Tag, $"ghostty_init returned {result}");
            if (result != 0)
            {
                Logger.LogCritical(Tag, $"ghostty_init failed with code {result}");
                return false;
            }

            // Log version info
            try
            {
                var info = NativeMethods.GhosttyInfo();
                var version = info.Version != IntPtr.Zero && (ulong)info.VersionLen > 0
                    ? Marshal.PtrToStringUTF8(info.Version, (int)info.VersionLen) ?? "unknown"
                    : "unknown";
                Logger.LogInfo(Tag, $"libghostty version={version} build={info.BuildMode}");
            }
            catch (Exception ex)
            {
                Logger.LogWarning(Tag, $"Failed to read version info: {ex.Message}");
            }

            Logger.LogInfo(Tag, "Creating config...");
            _config = NativeMethods.GhosttyConfigNew();
            if (_config == IntPtr.Zero)
            {
                Logger.LogCritical(Tag, "ghostty_config_new returned null");
                return false;
            }
            Logger.LogInfo(Tag, $"Config handle: 0x{_config:X}");

            Logger.LogInfo(Tag, "Loading config files...");
            NativeMethods.GhosttyConfigLoadDefaultFiles(_config);
            NativeMethods.GhosttyConfigLoadRecursiveFiles(_config);
            NativeMethods.GhosttyConfigFinalize(_config);
            Logger.LogInfo(Tag, "Config finalized");

            // Check config diagnostics
            var diagCount = NativeMethods.GhosttyConfigDiagnosticsCount(_config);
            if (diagCount > 0)
            {
                Logger.LogWarning(Tag, $"Config has {diagCount} diagnostic(s):");
                for (uint i = 0; i < diagCount; i++)
                {
                    var diag = NativeMethods.GhosttyConfigGetDiagnostic(_config, i);
                    var msg = diag.Message != IntPtr.Zero
                        ? Marshal.PtrToStringUTF8(diag.Message) ?? "(null)"
                        : "(null)";
                    Logger.LogWarning(Tag, $"  [{i}] {msg}");
                }
            }
            else
            {
                Logger.LogInfo(Tag, "Config has no diagnostics");
            }

            Logger.LogInfo(Tag, "Setting up callbacks...");
            _wakeupDelegate = OnWakeup;
            _actionDelegate = OnAction;
            _readClipboardDelegate = OnReadClipboard;
            _confirmReadClipboardDelegate = OnConfirmReadClipboard;
            _writeClipboardDelegate = OnWriteClipboard;
            _closeSurfaceDelegate = OnCloseSurface;
            _glMakeCurrentDelegate = OnGlMakeCurrent;
            _glSwapBuffersDelegate = OnGlSwapBuffers;

            var runtimeConfig = new GhosttyRuntimeConfig
            {
                Userdata = IntPtr.Zero,
                SupportsSelectionClipboard = false,
                WakeupCb = Marshal.GetFunctionPointerForDelegate(_wakeupDelegate),
                ActionCb = Marshal.GetFunctionPointerForDelegate(_actionDelegate),
                ReadClipboardCb = Marshal.GetFunctionPointerForDelegate(_readClipboardDelegate),
                ConfirmReadClipboardCb = Marshal.GetFunctionPointerForDelegate(_confirmReadClipboardDelegate),
                WriteClipboardCb = Marshal.GetFunctionPointerForDelegate(_writeClipboardDelegate),
                CloseSurfaceCb = Marshal.GetFunctionPointerForDelegate(_closeSurfaceDelegate),
                GlMakeCurrentCb = Marshal.GetFunctionPointerForDelegate(_glMakeCurrentDelegate),
                GlSwapBuffersCb = Marshal.GetFunctionPointerForDelegate(_glSwapBuffersDelegate),
            };

            Logger.LogInfo(Tag, "Creating app...");
            _app = NativeMethods.GhosttyAppNew(ref runtimeConfig, _config);
            Logger.LogInfo(Tag, $"App handle: 0x{_app:X}");
            if (_app == IntPtr.Zero)
            {
                Logger.LogCritical(Tag, "ghostty_app_new returned null");
                NativeMethods.GhosttyConfigFree(_config);
                _config = IntPtr.Zero;
                return false;
            }

            Logger.LogInfo(Tag, $"Setting color scheme: {colorScheme}");
            NativeMethods.GhosttyAppSetColorScheme(_app, colorScheme);

            Logger.LogInfo(Tag, "Initialize complete!");
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogCritical(Tag, $"Initialize exception: {ex}");
            return false;
        }
    }

    public void Tick()
    {
        if (_app != IntPtr.Zero)
            NativeMethods.GhosttyAppTick(_app);
    }

    public void SetFocus(bool focused)
    {
        if (_app != IntPtr.Zero)
            NativeMethods.GhosttyAppSetFocus(_app, focused);
    }

    public bool NeedsConfirmQuit()
    {
        return _app != IntPtr.Zero && NativeMethods.GhosttyAppNeedsConfirmQuit(_app);
    }

    // --- Callback implementations ---

    private void OnWakeup(IntPtr userdata)
    {
        // Marshal to the WPF UI thread to call tick
        Application.Current?.Dispatcher.BeginInvoke(() => Tick());
    }

    private bool OnAction(IntPtr app, GhosttyTarget target, GhosttyAction action)
    {
        Logger.LogDebug(Tag, $"OnAction: {action.Tag}");

        switch (action.Tag)
        {
            case GhosttyActionTag.SetTitle:
            {
                var titlePtr = action.Action.SetTitle.Title;
                if (titlePtr != IntPtr.Zero)
                {
                    var title = Marshal.PtrToStringUTF8(titlePtr) ?? "Ghostty";
                    TitleChanged?.Invoke(title);
                }
                return true;
            }

            case GhosttyActionTag.MouseShape:
            {
                MouseShapeChanged?.Invoke(action.Action.MouseShape);
                return true;
            }

            case GhosttyActionTag.Quit:
            {
                CloseRequested?.Invoke();
                return true;
            }

            case GhosttyActionTag.CloseWindow:
            {
                CloseRequested?.Invoke();
                return true;
            }

            case GhosttyActionTag.InitialSize:
            {
                Logger.LogInfo(Tag, $"InitialSize: {action.Action.InitialSize.Width}x{action.Action.InitialSize.Height}");
                InitialSizeReceived?.Invoke(
                    action.Action.InitialSize.Width,
                    action.Action.InitialSize.Height);
                return true;
            }

            case GhosttyActionTag.CellSize:
            {
                Logger.LogInfo(Tag, $"CellSize: {action.Action.CellSize.Width}x{action.Action.CellSize.Height}");
                CellSizeReceived?.Invoke(
                    action.Action.CellSize.Width,
                    action.Action.CellSize.Height);
                return true;
            }

            case GhosttyActionTag.OpenConfig:
            {
                var path = NativeMethods.GhosttyConfigOpenPath();
                if (path.Ptr != IntPtr.Zero && (ulong)path.Len > 0)
                {
                    var configPath = Marshal.PtrToStringUTF8(path.Ptr, (int)path.Len);
                    if (!string.IsNullOrEmpty(configPath))
                    {
                        try
                        {
                            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                            {
                                FileName = configPath,
                                UseShellExecute = true,
                            });
                        }
                        catch
                        {
                            // Ignore errors opening config
                        }
                    }
                    NativeMethods.GhosttyStringFree(path);
                }
                return true;
            }

            case GhosttyActionTag.Render:
                return true;

            case GhosttyActionTag.RingBell:
                System.Media.SystemSounds.Beep.Play();
                return true;

            default:
                return false;
        }
    }

    private void OnReadClipboard(IntPtr surfaceUserdata, GhosttyClipboard clipboard, IntPtr request)
    {
        Application.Current?.Dispatcher.BeginInvoke(() =>
        {
            try
            {
                var text = System.Windows.Clipboard.GetText();
                // We need to get the surface handle from the request to complete it.
                // For now we complete it directly if we can find it.
                // The surface pointer is stored as surfaceUserdata in the surface options.
            }
            catch
            {
                // Clipboard access can fail
            }
        });
    }

    private void OnConfirmReadClipboard(
        IntPtr surfaceUserdata, string content, IntPtr request, GhosttyClipboardRequest requestType)
    {
        // For MVP, auto-confirm clipboard reads
    }

    private void OnWriteClipboard(
        IntPtr surfaceUserdata, GhosttyClipboard clipboard,
        IntPtr contents, nuint contentCount, bool confirm)
    {
        if (contentCount == 0 || contents == IntPtr.Zero)
            return;

        // Read the first clipboard content entry
        var content = Marshal.PtrToStructure<GhosttyClipboardContent>(contents);
        if (content.Data == IntPtr.Zero)
            return;

        var text = Marshal.PtrToStringUTF8(content.Data);
        if (string.IsNullOrEmpty(text))
            return;

        Application.Current?.Dispatcher.BeginInvoke(() =>
        {
            try
            {
                System.Windows.Clipboard.SetText(text);
            }
            catch
            {
                // Clipboard access can fail
            }
        });
    }

    private void OnCloseSurface(IntPtr surfaceUserdata, bool processAlive)
    {
        if (processAlive)
        {
            Application.Current?.Dispatcher.BeginInvoke(() =>
            {
                var result = MessageBox.Show(
                    "A process is still running. Close anyway?",
                    "Ghostty",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                    CloseRequested?.Invoke();
            });
        }
        else
        {
            CloseRequested?.Invoke();
        }
    }

    private void OnGlMakeCurrent(IntPtr surfaceUserdata)
    {
        GlMakeCurrentAction?.Invoke();
    }

    private void OnGlSwapBuffers(IntPtr surfaceUserdata)
    {
        GlSwapBuffersAction?.Invoke();
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        Logger.LogInfo(Tag, "Disposing GhosttyApp");

        if (_app != IntPtr.Zero)
        {
            NativeMethods.GhosttyAppFree(_app);
            _app = IntPtr.Zero;
        }

        if (_config != IntPtr.Zero)
        {
            NativeMethods.GhosttyConfigFree(_config);
            _config = IntPtr.Zero;
        }
    }
}
