using Ghostty.Helpers;

namespace Ghostty.Controls;

/// <summary>
/// Manages the WGL OpenGL context lifecycle on a given HWND.
/// </summary>
public sealed class OpenGLContext : IDisposable
{
    private IntPtr _hwnd;
    private IntPtr _hdc;
    private IntPtr _hglrc;
    private bool _disposed;

    public IntPtr HDC => _hdc;
    public IntPtr HGLRC => _hglrc;
    public bool IsInitialized => _hglrc != IntPtr.Zero;

    public bool Initialize(IntPtr hwnd)
    {
        _hwnd = hwnd;
        _hdc = Win32Interop.GetDC(hwnd);
        if (_hdc == IntPtr.Zero)
            return false;

        var pfd = new Win32Interop.PixelFormatDescriptor
        {
            nSize = (ushort)System.Runtime.InteropServices.Marshal.SizeOf<Win32Interop.PixelFormatDescriptor>(),
            nVersion = 1,
            dwFlags = Win32Interop.PFD_DRAW_TO_WINDOW |
                      Win32Interop.PFD_SUPPORT_OPENGL |
                      Win32Interop.PFD_DOUBLEBUFFER,
            iPixelType = Win32Interop.PFD_TYPE_RGBA,
            cColorBits = 32,
            cDepthBits = 24,
            cStencilBits = 8,
            iLayerType = Win32Interop.PFD_MAIN_PLANE,
        };

        var pixelFormat = Win32Interop.ChoosePixelFormat(_hdc, ref pfd);
        if (pixelFormat == 0)
            return false;

        if (!Win32Interop.SetPixelFormat(_hdc, pixelFormat, ref pfd))
            return false;

        _hglrc = Win32Interop.wglCreateContext(_hdc);
        if (_hglrc == IntPtr.Zero)
            return false;

        return true;
    }

    public bool MakeCurrent()
    {
        if (_hdc == IntPtr.Zero || _hglrc == IntPtr.Zero)
            return false;
        return Win32Interop.wglMakeCurrent(_hdc, _hglrc);
    }

    public void ReleaseCurrent()
    {
        Win32Interop.wglMakeCurrent(IntPtr.Zero, IntPtr.Zero);
    }

    public bool SwapBuffers()
    {
        if (_hdc == IntPtr.Zero)
            return false;
        return Win32Interop.SwapBuffers(_hdc);
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;

        if (_hglrc != IntPtr.Zero)
        {
            Win32Interop.wglMakeCurrent(IntPtr.Zero, IntPtr.Zero);
            Win32Interop.wglDeleteContext(_hglrc);
            _hglrc = IntPtr.Zero;
        }

        if (_hdc != IntPtr.Zero && _hwnd != IntPtr.Zero)
        {
            Win32Interop.ReleaseDC(_hwnd, _hdc);
            _hdc = IntPtr.Zero;
        }
    }
}
