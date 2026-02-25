using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using Ghostty.Helpers;
using Ghostty.Interop;

namespace Ghostty.Controls;

/// <summary>
/// HwndHost subclass that hosts a native HWND with an OpenGL context
/// and a ghostty surface for terminal rendering.
/// </summary>
public class TerminalControl : HwndHost
{
    private const string LogTag = "Terminal";

    private readonly GhosttyApp _ghosttyApp;
    private IntPtr _hwnd;
    private IntPtr _surface;
    private OpenGLContext? _glContext;
    private bool _surfaceCreated;
    private bool _disposed;

    public TerminalControl(GhosttyApp ghosttyApp)
    {
        _ghosttyApp = ghosttyApp;

        // Wire up GL callbacks
        _ghosttyApp.GlMakeCurrentAction = () => _glContext?.MakeCurrent();
        _ghosttyApp.GlSwapBuffersAction = () => _glContext?.SwapBuffers();
    }

    protected override HandleRef BuildWindowCore(HandleRef hwndParent)
    {
        Logger.LogInfo(LogTag, $"BuildWindowCore: parent=0x{hwndParent.Handle:X}, size={ActualWidth}x{ActualHeight}");

        var hInstance = Win32Interop.GetModuleHandle(null);
        Logger.LogInfo(LogTag, $"hInstance=0x{hInstance:X}");

        // Create a child window to host the terminal
        _hwnd = Win32Interop.CreateWindowEx(
            0,
            "Static",
            "",
            Win32Interop.WS_CHILD | Win32Interop.WS_VISIBLE |
            Win32Interop.WS_CLIPCHILDREN | Win32Interop.WS_CLIPSIBLINGS,
            0, 0,
            (int)ActualWidth, (int)ActualHeight,
            hwndParent.Handle,
            IntPtr.Zero,
            hInstance,
            IntPtr.Zero);

        if (_hwnd == IntPtr.Zero)
        {
            Logger.LogWin32Error(LogTag, "CreateWindowEx failed");
            throw new InvalidOperationException("Failed to create child window.");
        }
        Logger.LogInfo(LogTag, $"Child HWND created: 0x{_hwnd:X}");

        // Initialize OpenGL context
        _glContext = new OpenGLContext();
        if (!_glContext.Initialize(_hwnd))
        {
            Logger.LogCritical(LogTag, "OpenGL context initialization failed");
            throw new InvalidOperationException("Failed to initialize OpenGL context.");
        }
        Logger.LogInfo(LogTag, "OpenGL context initialized");

        // Hook into the HwndSource for raw Win32 messages
        // NOTE: Surface creation is deferred to OnRenderSizeChanged,
        // where ActualWidth/ActualHeight are available.
        var source = HwndSource.FromHwnd(hwndParent.Handle);
        source?.AddHook(WndProcHook);

        Logger.LogInfo(LogTag, "BuildWindowCore complete");
        return new HandleRef(this, _hwnd);
    }

    private void CreateSurface(uint width, uint height, double dpiScale)
    {
        Logger.LogInfo(LogTag, "CreateSurface: begin");

        var surfaceConfig = NativeMethods.GhosttySurfaceConfigNew();

        // Set up Windows platform
        surfaceConfig.PlatformTag = GhosttyPlatform.Windows;
        surfaceConfig.Platform = new GhosttyPlatformUnion
        {
            Windows = new GhosttyPlatformWindows { Hwnd = _hwnd }
        };

        surfaceConfig.ScaleFactor = dpiScale;
        surfaceConfig.Context = GhosttySurfaceContext.Window;
        surfaceConfig.InitialWidth = width;
        surfaceConfig.InitialHeight = height;

        Logger.LogInfo(LogTag, $"Surface config: hwnd=0x{_hwnd:X}, dpi={dpiScale}, size={width}x{height}, context={surfaceConfig.Context}");

        _surface = NativeMethods.GhosttySurfaceNew(_ghosttyApp.AppHandle, ref surfaceConfig);
        if (_surface == IntPtr.Zero)
        {
            Logger.LogCritical(LogTag, "ghostty_surface_new returned null");
            throw new InvalidOperationException("Failed to create ghostty surface.");
        }
        Logger.LogInfo(LogTag, $"Surface created: 0x{_surface:X}");

        // Still call SetSize to trigger sizeCallback, but since the size
        // matches what we passed in the config, it will be a no-op.
        NativeMethods.GhosttySurfaceSetSize(_surface, width, height);
        NativeMethods.GhosttySurfaceSetContentScale(_surface, dpiScale, dpiScale);
        NativeMethods.GhosttySurfaceSetFocus(_surface, true);

        Logger.LogInfo(LogTag, "CreateSurface: complete");
    }

    protected override void DestroyWindowCore(HandleRef hwnd)
    {
        Logger.LogInfo(LogTag, "DestroyWindowCore");

        if (_surface != IntPtr.Zero)
        {
            NativeMethods.GhosttySurfaceFree(_surface);
            _surface = IntPtr.Zero;
        }

        _glContext?.Dispose();
        _glContext = null;

        if (_hwnd != IntPtr.Zero)
        {
            Win32Interop.DestroyWindow(_hwnd);
            _hwnd = IntPtr.Zero;
        }
    }

    protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
    {
        base.OnRenderSizeChanged(sizeInfo);

        if (_hwnd == IntPtr.Zero)
            return;

        var source = PresentationSource.FromVisual(this);
        double dpiScale = source?.CompositionTarget?.TransformToDevice.M11 ?? 1.0;
        var width = (uint)Math.Max(1, sizeInfo.NewSize.Width * dpiScale);
        var height = (uint)Math.Max(1, sizeInfo.NewSize.Height * dpiScale);

        if (!_surfaceCreated)
        {
            // Resize the child HWND first so the renderer thread sees the correct viewport.
            Win32Interop.MoveWindow(_hwnd, 0, 0, (int)width, (int)height, true);

            // First layout pass — now we have the real size.
            // Make GL current, create surface, release for renderer thread.
            _glContext!.MakeCurrent();
            CreateSurface(width, height, dpiScale);
            _glContext.ReleaseCurrent();
            _surfaceCreated = true;
            return;
        }

        if (_surface == IntPtr.Zero)
            return;

        // Subsequent resizes
        Win32Interop.MoveWindow(_hwnd, 0, 0, (int)width, (int)height, true);
        NativeMethods.GhosttySurfaceSetSize(_surface, width, height);
        NativeMethods.GhosttySurfaceSetContentScale(_surface, dpiScale, dpiScale);
    }

    private IntPtr WndProcHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (_surface == IntPtr.Zero)
            return IntPtr.Zero;

        switch (msg)
        {
            case Win32Interop.WM_KEYDOWN:
            case Win32Interop.WM_SYSKEYDOWN:
                handled = HandleKeyEvent(wParam, lParam, GhosttyInputAction.Press);
                break;

            case Win32Interop.WM_KEYUP:
            case Win32Interop.WM_SYSKEYUP:
                handled = HandleKeyEvent(wParam, lParam, GhosttyInputAction.Release);
                break;

            case Win32Interop.WM_CHAR:
                handled = HandleCharEvent(wParam);
                break;

            case Win32Interop.WM_LBUTTONDOWN:
                handled = HandleMouseButton(GhosttyMouseState.Press, GhosttyMouseButton.Left);
                break;
            case Win32Interop.WM_LBUTTONUP:
                handled = HandleMouseButton(GhosttyMouseState.Release, GhosttyMouseButton.Left);
                break;
            case Win32Interop.WM_RBUTTONDOWN:
                handled = HandleMouseButton(GhosttyMouseState.Press, GhosttyMouseButton.Right);
                break;
            case Win32Interop.WM_RBUTTONUP:
                handled = HandleMouseButton(GhosttyMouseState.Release, GhosttyMouseButton.Right);
                break;
            case Win32Interop.WM_MBUTTONDOWN:
                handled = HandleMouseButton(GhosttyMouseState.Press, GhosttyMouseButton.Middle);
                break;
            case Win32Interop.WM_MBUTTONUP:
                handled = HandleMouseButton(GhosttyMouseState.Release, GhosttyMouseButton.Middle);
                break;

            case Win32Interop.WM_MOUSEMOVE:
                HandleMouseMove(lParam);
                break;

            case Win32Interop.WM_MOUSEWHEEL:
                HandleMouseWheel(wParam, vertical: true);
                handled = true;
                break;

            case Win32Interop.WM_MOUSEHWHEEL:
                HandleMouseWheel(wParam, vertical: false);
                handled = true;
                break;

            case Win32Interop.WM_SETFOCUS:
                NativeMethods.GhosttySurfaceSetFocus(_surface, true);
                break;

            case Win32Interop.WM_KILLFOCUS:
                NativeMethods.GhosttySurfaceSetFocus(_surface, false);
                break;
        }

        return IntPtr.Zero;
    }

    private bool HandleKeyEvent(IntPtr wParam, IntPtr lParam, GhosttyInputAction action)
    {
        var virtualKey = (int)wParam;
        var scanCode = KeyMapper.GetScanCode(virtualKey);
        var mods = KeyMapper.GetMods();

        // Check for repeat (bit 30 of lParam)
        if (action == GhosttyInputAction.Press && ((lParam.ToInt64() >> 30) & 1) == 1)
            action = GhosttyInputAction.Repeat;

        var keyEvent = new GhosttyKeyEvent
        {
            Action = action,
            Mods = mods,
            ConsumedMods = GhosttyMods.None,
            Keycode = scanCode,
            Text = IntPtr.Zero,
            UnshiftedCodepoint = KeyMapper.GetUnshiftedCodepoint(virtualKey),
            Composing = false,
        };

        return NativeMethods.GhosttySurfaceKey(_surface, keyEvent);
    }

    private bool HandleCharEvent(IntPtr wParam)
    {
        var c = (char)(int)wParam;
        if (char.IsControl(c) && c != '\r' && c != '\n' && c != '\t' && c != '\b' && c != 0x1B)
            return false;

        var str = c.ToString();
        var utf8Bytes = Encoding.UTF8.GetBytes(str);

        unsafe
        {
            fixed (byte* ptr = utf8Bytes)
            {
                NativeMethods.GhosttySurfaceText(_surface, (IntPtr)ptr, (nuint)utf8Bytes.Length);
            }
        }

        return true;
    }

    private bool HandleMouseButton(GhosttyMouseState state, GhosttyMouseButton button)
    {
        var mods = KeyMapper.GetMods();
        return NativeMethods.GhosttySurfaceMouseButton(_surface, state, button, mods);
    }

    private void HandleMouseMove(IntPtr lParam)
    {
        var x = (short)(lParam.ToInt64() & 0xFFFF);
        var y = (short)((lParam.ToInt64() >> 16) & 0xFFFF);
        var mods = KeyMapper.GetMods();
        NativeMethods.GhosttySurfaceMousePos(_surface, x, y, mods);
    }

    private void HandleMouseWheel(IntPtr wParam, bool vertical)
    {
        var delta = (short)((wParam.ToInt64() >> 16) & 0xFFFF);
        double scrollAmount = delta / 120.0;

        if (vertical)
            NativeMethods.GhosttySurfaceMouseScroll(_surface, 0, scrollAmount, 0);
        else
            NativeMethods.GhosttySurfaceMouseScroll(_surface, scrollAmount, 0, 0);
    }

    protected override void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            _disposed = true;
            Logger.LogInfo(LogTag, "Disposing TerminalControl");

            if (_surface != IntPtr.Zero)
            {
                NativeMethods.GhosttySurfaceFree(_surface);
                _surface = IntPtr.Zero;
            }

            _glContext?.Dispose();
            _glContext = null;
        }

        base.Dispose(disposing);
    }
}
