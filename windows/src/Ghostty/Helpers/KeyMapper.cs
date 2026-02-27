using System.Windows.Input;
using Ghostty.Helpers;
using Ghostty.Interop;

namespace Ghostty.Helpers;

/// <summary>
/// Maps WPF key codes to Win32 scan codes for ghostty's key handling.
/// </summary>
public static class KeyMapper
{
    /// <summary>
    /// Get the Win32 virtual key code from a WPF Key.
    /// </summary>
    public static int GetVirtualKey(Key key)
    {
        return KeyInterop.VirtualKeyFromKey(key);
    }

    /// <summary>
    /// Get the hardware scan code from a virtual key code.
    /// Uses MapVirtualKey with MAPVK_VK_TO_VSC_EX for extended scan codes.
    /// </summary>
    public static uint GetScanCode(int virtualKey)
    {
        return Win32Interop.MapVirtualKey((uint)virtualKey, Win32Interop.MAPVK_VK_TO_VSC_EX);
    }

    /// <summary>
    /// Convert WPF modifier keys to ghostty mods flags.
    /// </summary>
    public static GhosttyMods GetMods()
    {
        var mods = GhosttyMods.None;

        if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
            mods |= GhosttyMods.Shift;
        if (Keyboard.IsKeyDown(Key.RightShift))
            mods |= GhosttyMods.ShiftRight;

        if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
            mods |= GhosttyMods.Ctrl;
        if (Keyboard.IsKeyDown(Key.RightCtrl))
            mods |= GhosttyMods.CtrlRight;

        if (Keyboard.IsKeyDown(Key.LeftAlt) || Keyboard.IsKeyDown(Key.RightAlt))
            mods |= GhosttyMods.Alt;
        if (Keyboard.IsKeyDown(Key.RightAlt))
            mods |= GhosttyMods.AltRight;

        if (Keyboard.IsKeyDown(Key.LWin) || Keyboard.IsKeyDown(Key.RWin))
            mods |= GhosttyMods.Super;
        if (Keyboard.IsKeyDown(Key.RWin))
            mods |= GhosttyMods.SuperRight;

        if (Keyboard.IsKeyToggled(Key.CapsLock))
            mods |= GhosttyMods.Caps;
        if (Keyboard.IsKeyToggled(Key.NumLock))
            mods |= GhosttyMods.Num;

        return mods;
    }

    /// <summary>
    /// Get the unshifted codepoint for a virtual key.
    /// This returns the character that would be produced without shift held.
    /// </summary>
    public static uint GetUnshiftedCodepoint(int virtualKey)
    {
        // For printable ASCII keys, return the lowercase character
        if (virtualKey >= 0x41 && virtualKey <= 0x5A) // A-Z
            return (uint)(virtualKey + 32); // lowercase

        if (virtualKey >= 0x30 && virtualKey <= 0x39) // 0-9
            return (uint)virtualKey;

        return 0;
    }
}
