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
    private readonly GhosttyApp _ghosttyApp;
    private IntPtr _hwnd;
    private IntPtr _surface;
    private OpenGLContext? _glContext;
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
        var hInstance = Win32Interop.GetModuleHandle(null);

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
            throw new InvalidOperationException("Failed to create child window.");

        // Initialize OpenGL context
        _glContext = new OpenGLContext();
        if (!_glContext.Initialize(_hwnd))
            throw new InvalidOperationException("Failed to initialize OpenGL context.");

        // Make current so we can create the surface
        _glContext.MakeCurrent();

        // Create the ghostty surface
        CreateSurface();

        // Release GL context from UI thread so the renderer thread can use it
        _glContext.ReleaseCurrent();

        // Hook into the HwndSource for raw Win32 messages
        var source = HwndSource.FromHwnd(hwndParent.Handle);
        source?.AddHook(WndProcHook);

        return new HandleRef(this, _hwnd);
    }

    private void CreateSurface()
    {
        var surfaceConfig = NativeMethods.GhosttySurfaceConfigNew();

        // Set up Windows platform
        surfaceConfig.PlatformTag = GhosttyPlatform.Windows;
        surfaceConfig.Platform = new GhosttyPlatformUnion
        {
            Windows = new GhosttyPlatformWindows { Hwnd = _hwnd }
        };

        // Set DPI scale
        var source = PresentationSource.FromVisual(this);
        double dpiScale = source?.CompositionTarget?.TransformToDevice.M11 ?? 1.0;
        surfaceConfig.ScaleFactor = dpiScale;

        surfaceConfig.Context = GhosttySurfaceContext.Window;

        _surface = NativeMethods.GhosttySurfaceNew(_ghosttyApp.AppHandle, ref surfaceConfig);
        if (_surface == IntPtr.Zero)
            throw new InvalidOperationException("Failed to create ghostty surface.");

        // Set initial size
        var width = (uint)Math.Max(1, ActualWidth * dpiScale);
        var height = (uint)Math.Max(1, ActualHeight * dpiScale);
        NativeMethods.GhosttySurfaceSetSize(_surface, width, height);
        NativeMethods.GhosttySurfaceSetContentScale(_surface, dpiScale, dpiScale);
        NativeMethods.GhosttySurfaceSetFocus(_surface, true);
    }

    protected override void DestroyWindowCore(HandleRef hwnd)
    {
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

        if (_hwnd == IntPtr.Zero || _surface == IntPtr.Zero)
            return;

        var source = PresentationSource.FromVisual(this);
        double dpiScale = source?.CompositionTarget?.TransformToDevice.M11 ?? 1.0;

        var width = (int)Math.Max(1, sizeInfo.NewSize.Width * dpiScale);
        var height = (int)Math.Max(1, sizeInfo.NewSize.Height * dpiScale);

        // Resize the child HWND
        Win32Interop.MoveWindow(_hwnd, 0, 0, width, height, true);

        // Notify ghostty of the new size
        NativeMethods.GhosttySurfaceSetSize(_surface, (uint)width, (uint)height);
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
