using System.Runtime.InteropServices;

namespace Ghostty.Interop;

// ghostty_platform_e
public enum GhosttyPlatform : int
{
    Invalid = 0,
    MacOS = 1,
    IOS = 2,
    Windows = 3,
}

// ghostty_clipboard_e
public enum GhosttyClipboard : int
{
    Standard = 0,
    Selection = 1,
}

// ghostty_clipboard_request_e
public enum GhosttyClipboardRequest : int
{
    Paste = 0,
    Osc52Read = 1,
    Osc52Write = 2,
}

// ghostty_input_mouse_state_e
public enum GhosttyMouseState : int
{
    Release = 0,
    Press = 1,
}

// ghostty_input_mouse_button_e
public enum GhosttyMouseButton : int
{
    Unknown = 0,
    Left = 1,
    Right = 2,
    Middle = 3,
    Four = 4,
    Five = 5,
    Six = 6,
    Seven = 7,
    Eight = 8,
    Nine = 9,
    Ten = 10,
    Eleven = 11,
}

// ghostty_color_scheme_e
public enum GhosttyColorScheme : int
{
    Light = 0,
    Dark = 1,
}

// ghostty_input_action_e
public enum GhosttyInputAction : int
{
    Release = 0,
    Press = 1,
    Repeat = 2,
}

// ghostty_input_mods_e
[Flags]
public enum GhosttyMods : int
{
    None = 0,
    Shift = 1 << 0,
    Ctrl = 1 << 1,
    Alt = 1 << 2,
    Super = 1 << 3,
    Caps = 1 << 4,
    Num = 1 << 5,
    ShiftRight = 1 << 6,
    CtrlRight = 1 << 7,
    AltRight = 1 << 8,
    SuperRight = 1 << 9,
}

// ghostty_build_mode_e
public enum GhosttyBuildMode : int
{
    Debug = 0,
    ReleaseSafe = 1,
    ReleaseFast = 2,
    ReleaseSmall = 3,
}

// ghostty_target_tag_e
public enum GhosttyTargetTag : int
{
    App = 0,
    Surface = 1,
}

// ghostty_action_tag_e
public enum GhosttyActionTag : int
{
    Quit = 0,
    NewWindow = 1,
    NewTab = 2,
    CloseTab = 3,
    NewSplit = 4,
    CloseAllWindows = 5,
    ToggleMaximize = 6,
    ToggleFullscreen = 7,
    ToggleTabOverview = 8,
    ToggleWindowDecorations = 9,
    ToggleQuickTerminal = 10,
    ToggleCommandPalette = 11,
    ToggleVisibility = 12,
    ToggleBackgroundOpacity = 13,
    MoveTab = 14,
    GotoTab = 15,
    GotoSplit = 16,
    GotoWindow = 17,
    ResizeSplit = 18,
    EqualizeSplits = 19,
    ToggleSplitZoom = 20,
    PresentTerminal = 21,
    SizeLimit = 22,
    ResetWindowSize = 23,
    InitialSize = 24,
    CellSize = 25,
    Scrollbar = 26,
    Render = 27,
    Inspector = 28,
    ShowGtkInspector = 29,
    RenderInspector = 30,
    DesktopNotification = 31,
    SetTitle = 32,
    PromptTitle = 33,
    Pwd = 34,
    MouseShape = 35,
    MouseVisibility = 36,
    MouseOverLink = 37,
    RendererHealth = 38,
    OpenConfig = 39,
    QuitTimer = 40,
    FloatWindow = 41,
    SecureInput = 42,
    KeySequence = 43,
    KeyTable = 44,
    ColorChange = 45,
    ReloadConfig = 46,
    ConfigChange = 47,
    CloseWindow = 48,
    RingBell = 49,
    Undo = 50,
    Redo = 51,
    CheckForUpdates = 52,
    OpenUrl = 53,
    ShowChildExited = 54,
    ProgressReport = 55,
    ShowOnScreenKeyboard = 56,
    CommandFinished = 57,
    StartSearch = 58,
    EndSearch = 59,
    SearchTotal = 60,
    SearchSelected = 61,
    Readonly = 62,
    CopyTitleToClipboard = 63,
}

// ghostty_mouse_shape (terminal.MouseShape)
public enum GhosttyMouseShape : int
{
    Default = 0,
    ContextMenu = 1,
    Help = 2,
    Pointer = 3,
    Progress = 4,
    Wait = 5,
    Cell = 6,
    Crosshair = 7,
    Text = 8,
    VerticalText = 9,
    Alias = 10,
    Copy = 11,
    Move = 12,
    NoDrop = 13,
    NotAllowed = 14,
    Grab = 15,
    Grabbing = 16,
    AllScroll = 17,
    ColResize = 18,
    RowResize = 19,
    NResize = 20,
    EResize = 21,
    SResize = 22,
    WResize = 23,
    NeResize = 24,
    NwResize = 25,
    SeResize = 26,
    SwResize = 27,
    EwResize = 28,
    NsResize = 29,
    NeswResize = 30,
    NwseResize = 31,
    ZoomIn = 32,
    ZoomOut = 33,
}

// ghostty_surface_context_e
public enum GhosttySurfaceContext : int
{
    Window = 0,
    Tab = 1,
    Split = 2,
}

// ghostty_info_s
[StructLayout(LayoutKind.Sequential)]
public struct GhosttyInfo
{
    public GhosttyBuildMode BuildMode;
    public IntPtr Version;
    public nuint VersionLen;
}

// ghostty_input_key_s
[StructLayout(LayoutKind.Sequential)]
public struct GhosttyKeyEvent
{
    public GhosttyInputAction Action;
    public GhosttyMods Mods;
    public GhosttyMods ConsumedMods;
    public uint Keycode;
    public IntPtr Text;
    public uint UnshiftedCodepoint;
    [MarshalAs(UnmanagedType.U1)]
    public bool Composing;
}

// ghostty_platform_windows_s
[StructLayout(LayoutKind.Sequential)]
public struct GhosttyPlatformWindows
{
    public IntPtr Hwnd;
}

// ghostty_platform_u (only Windows member used)
[StructLayout(LayoutKind.Explicit)]
public struct GhosttyPlatformUnion
{
    [FieldOffset(0)]
    public GhosttyPlatformWindows Windows;
}

// ghostty_surface_config_s
[StructLayout(LayoutKind.Sequential)]
public struct GhosttySurfaceConfig
{
    public GhosttyPlatform PlatformTag;
    public GhosttyPlatformUnion Platform;
    public IntPtr Userdata;
    public double ScaleFactor;
    public float FontSize;
    public IntPtr WorkingDirectory;
    public IntPtr Command;
    public IntPtr EnvVars;
    public nuint EnvVarCount;
    public IntPtr InitialInput;
    [MarshalAs(UnmanagedType.U1)]
    public bool WaitAfterCommand;
    public GhosttySurfaceContext Context;
    public uint InitialWidth;
    public uint InitialHeight;
}

// ghostty_surface_size_s
[StructLayout(LayoutKind.Sequential)]
public struct GhosttySurfaceSize
{
    public ushort Columns;
    public ushort Rows;
    public uint WidthPx;
    public uint HeightPx;
    public uint CellWidthPx;
    public uint CellHeightPx;
}

// ghostty_target_s
[StructLayout(LayoutKind.Sequential)]
public struct GhosttyTarget
{
    public GhosttyTargetTag Tag;
    public IntPtr Surface; // ghostty_surface_t (union with single member)
}

// ghostty_action_set_title_s
[StructLayout(LayoutKind.Sequential)]
public struct GhosttyActionSetTitle
{
    public IntPtr Title;
}

// ghostty_action_initial_size_s
[StructLayout(LayoutKind.Sequential)]
public struct GhosttyActionInitialSize
{
    public uint Width;
    public uint Height;
}

// ghostty_action_cell_size_s
[StructLayout(LayoutKind.Sequential)]
public struct GhosttyActionCellSize
{
    public uint Width;
    public uint Height;
}

// ghostty_action_u - we use explicit layout to read specific action types
[StructLayout(LayoutKind.Explicit, Size = 32)]
public struct GhosttyActionUnion
{
    [FieldOffset(0)]
    public GhosttyActionSetTitle SetTitle;

    [FieldOffset(0)]
    public GhosttyMouseShape MouseShape;

    [FieldOffset(0)]
    public GhosttyActionInitialSize InitialSize;

    [FieldOffset(0)]
    public GhosttyActionCellSize CellSize;
}

// ghostty_action_s
[StructLayout(LayoutKind.Sequential)]
public struct GhosttyAction
{
    public GhosttyActionTag Tag;
    public GhosttyActionUnion Action;
}

// ghostty_clipboard_content_s
[StructLayout(LayoutKind.Sequential)]
public struct GhosttyClipboardContent
{
    public IntPtr Mime;
    public IntPtr Data;
}

// ghostty_diagnostic_s
[StructLayout(LayoutKind.Sequential)]
public struct GhosttyDiagnostic
{
    public IntPtr Message;
}

// ghostty_string_s
[StructLayout(LayoutKind.Sequential)]
public struct GhosttyString
{
    public IntPtr Ptr;
    public nuint Len;
    [MarshalAs(UnmanagedType.U1)]
    public bool Sentinel;
}
