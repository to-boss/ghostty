using System.Runtime.InteropServices;

namespace Ghostty.Interop;

// Delegate types matching the ghostty_runtime_config_s callbacks.
// All delegates use CallingConvention.Cdecl to match the C ABI.

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public delegate void WakeupCallback(IntPtr userdata);

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
[return: MarshalAs(UnmanagedType.U1)]
public delegate bool ActionCallback(
    IntPtr app,
    GhosttyTarget target,
    GhosttyAction action);

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public delegate void ReadClipboardCallback(
    IntPtr surfaceUserdata,
    GhosttyClipboard clipboard,
    IntPtr clipboardRequest);

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public delegate void ConfirmReadClipboardCallback(
    IntPtr surfaceUserdata,
    [MarshalAs(UnmanagedType.LPStr)] string content,
    IntPtr clipboardRequest,
    GhosttyClipboardRequest requestType);

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public delegate void WriteClipboardCallback(
    IntPtr surfaceUserdata,
    GhosttyClipboard clipboard,
    IntPtr contents,
    nuint contentCount,
    [MarshalAs(UnmanagedType.U1)] bool confirm);

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public delegate void CloseSurfaceCallback(
    IntPtr surfaceUserdata,
    [MarshalAs(UnmanagedType.U1)] bool processAlive);

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public delegate void GlMakeCurrentCallback(IntPtr surfaceUserdata);

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public delegate void GlSwapBuffersCallback(IntPtr surfaceUserdata);

// ghostty_runtime_config_s - the C struct layout for runtime configuration
[StructLayout(LayoutKind.Sequential)]
public struct GhosttyRuntimeConfig
{
    public IntPtr Userdata;
    [MarshalAs(UnmanagedType.U1)]
    public bool SupportsSelectionClipboard;
    public IntPtr WakeupCb;
    public IntPtr ActionCb;
    public IntPtr ReadClipboardCb;
    public IntPtr ConfirmReadClipboardCb;
    public IntPtr WriteClipboardCb;
    public IntPtr CloseSurfaceCb;
    public IntPtr GlMakeCurrentCb;
    public IntPtr GlSwapBuffersCb;
}
