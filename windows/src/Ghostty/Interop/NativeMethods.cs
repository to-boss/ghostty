using System.Runtime.InteropServices;

namespace Ghostty.Interop;

/// <summary>
/// P/Invoke declarations for the ghostty.dll C API.
/// </summary>
internal static partial class NativeMethods
{
    private const string GhosttyLib = "libghostty";

    // --- Initialization ---

    [DllImport(GhosttyLib, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ghostty_init")]
    public static extern int GhosttyInit(nuint argc, IntPtr argv);

    [DllImport(GhosttyLib, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ghostty_info")]
    public static extern GhosttyInfo GhosttyInfo();

    // --- String ---

    [DllImport(GhosttyLib, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ghostty_string_free")]
    public static extern void GhosttyStringFree(GhosttyString str);

    // --- Config ---

    [DllImport(GhosttyLib, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ghostty_config_new")]
    public static extern IntPtr GhosttyConfigNew();

    [DllImport(GhosttyLib, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ghostty_config_free")]
    public static extern void GhosttyConfigFree(IntPtr config);

    [DllImport(GhosttyLib, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ghostty_config_load_default_files")]
    public static extern void GhosttyConfigLoadDefaultFiles(IntPtr config);

    [DllImport(GhosttyLib, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ghostty_config_load_recursive_files")]
    public static extern void GhosttyConfigLoadRecursiveFiles(IntPtr config);

    [DllImport(GhosttyLib, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ghostty_config_finalize")]
    public static extern void GhosttyConfigFinalize(IntPtr config);

    [DllImport(GhosttyLib, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ghostty_config_get")]
    [return: MarshalAs(UnmanagedType.U1)]
    public static extern bool GhosttyConfigGet(
        IntPtr config,
        IntPtr value,
        [MarshalAs(UnmanagedType.LPStr)] string key,
        nuint keyLen);

    [DllImport(GhosttyLib, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ghostty_config_diagnostics_count")]
    public static extern uint GhosttyConfigDiagnosticsCount(IntPtr config);

    [DllImport(GhosttyLib, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ghostty_config_get_diagnostic")]
    public static extern GhosttyDiagnostic GhosttyConfigGetDiagnostic(IntPtr config, uint index);

    [DllImport(GhosttyLib, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ghostty_config_open_path")]
    public static extern GhosttyString GhosttyConfigOpenPath();

    // --- App ---

    [DllImport(GhosttyLib, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ghostty_app_new")]
    public static extern IntPtr GhosttyAppNew(ref GhosttyRuntimeConfig runtimeConfig, IntPtr config);

    [DllImport(GhosttyLib, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ghostty_app_free")]
    public static extern void GhosttyAppFree(IntPtr app);

    [DllImport(GhosttyLib, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ghostty_app_tick")]
    public static extern void GhosttyAppTick(IntPtr app);

    [DllImport(GhosttyLib, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ghostty_app_set_focus")]
    public static extern void GhosttyAppSetFocus(IntPtr app, [MarshalAs(UnmanagedType.U1)] bool focused);

    [DllImport(GhosttyLib, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ghostty_app_set_color_scheme")]
    public static extern void GhosttyAppSetColorScheme(IntPtr app, GhosttyColorScheme scheme);

    [DllImport(GhosttyLib, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ghostty_app_needs_confirm_quit")]
    [return: MarshalAs(UnmanagedType.U1)]
    public static extern bool GhosttyAppNeedsConfirmQuit(IntPtr app);

    // --- Surface Config ---

    [DllImport(GhosttyLib, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ghostty_surface_config_new")]
    public static extern GhosttySurfaceConfig GhosttySurfaceConfigNew();

    // --- Surface ---

    [DllImport(GhosttyLib, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ghostty_surface_new")]
    public static extern IntPtr GhosttySurfaceNew(IntPtr app, ref GhosttySurfaceConfig config);

    [DllImport(GhosttyLib, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ghostty_surface_free")]
    public static extern void GhosttySurfaceFree(IntPtr surface);

    [DllImport(GhosttyLib, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ghostty_surface_set_size")]
    public static extern void GhosttySurfaceSetSize(IntPtr surface, uint width, uint height);

    [DllImport(GhosttyLib, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ghostty_surface_set_content_scale")]
    public static extern void GhosttySurfaceSetContentScale(IntPtr surface, double x, double y);

    [DllImport(GhosttyLib, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ghostty_surface_set_focus")]
    public static extern void GhosttySurfaceSetFocus(IntPtr surface, [MarshalAs(UnmanagedType.U1)] bool focused);

    [DllImport(GhosttyLib, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ghostty_surface_key")]
    [return: MarshalAs(UnmanagedType.U1)]
    public static extern bool GhosttySurfaceKey(IntPtr surface, GhosttyKeyEvent keyEvent);

    [DllImport(GhosttyLib, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ghostty_surface_text")]
    public static extern void GhosttySurfaceText(IntPtr surface, IntPtr text, nuint len);

    [DllImport(GhosttyLib, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ghostty_surface_mouse_button")]
    [return: MarshalAs(UnmanagedType.U1)]
    public static extern bool GhosttySurfaceMouseButton(
        IntPtr surface,
        GhosttyMouseState state,
        GhosttyMouseButton button,
        GhosttyMods mods);

    [DllImport(GhosttyLib, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ghostty_surface_mouse_pos")]
    public static extern void GhosttySurfaceMousePos(
        IntPtr surface,
        double x,
        double y,
        GhosttyMods mods);

    [DllImport(GhosttyLib, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ghostty_surface_mouse_scroll")]
    public static extern void GhosttySurfaceMouseScroll(
        IntPtr surface,
        double x,
        double y,
        int scrollMods);

    [DllImport(GhosttyLib, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ghostty_surface_refresh")]
    public static extern void GhosttySurfaceRefresh(IntPtr surface);

    [DllImport(GhosttyLib, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ghostty_surface_draw")]
    public static extern void GhosttySurfaceDraw(IntPtr surface);

    [DllImport(GhosttyLib, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ghostty_surface_request_close")]
    public static extern void GhosttySurfaceRequestClose(IntPtr surface);

    [DllImport(GhosttyLib, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ghostty_surface_set_color_scheme")]
    public static extern void GhosttySurfaceSetColorScheme(IntPtr surface, GhosttyColorScheme scheme);

    [DllImport(GhosttyLib, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ghostty_surface_size")]
    public static extern GhosttySurfaceSize GhosttySurfaceSize(IntPtr surface);

    [DllImport(GhosttyLib, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ghostty_surface_complete_clipboard_request")]
    public static extern void GhosttySurfaceCompleteClipboardRequest(
        IntPtr surface,
        [MarshalAs(UnmanagedType.LPStr)] string? text,
        IntPtr request,
        [MarshalAs(UnmanagedType.U1)] bool confirmed);
}
