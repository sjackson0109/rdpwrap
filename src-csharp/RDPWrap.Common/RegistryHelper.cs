// Copyright 2024 sjackson0109 — Apache License 2.0
using Microsoft.Win32;

namespace RDPWrap.Common;

/// <summary>
/// Thin wrappers around <see cref="Microsoft.Win32.Registry"/> that mirror the
/// Delphi TRegistry usage in RDPWInst and RDPConf, with optional WOW64 flag
/// support (KEY_WOW64_64KEY) for 64-bit registry views from 32-bit processes.
/// </summary>
public static class RegistryHelper
{
    // On 64-bit Windows we always open the 64-bit view to match the Delphi code
    // that passes KEY_WOW64_64KEY when Arch = 64.
    private static RegistryView ViewForArch() =>
        ArchHelper.Is64Bit ? RegistryView.Registry64 : RegistryView.Default;

    // ── Convenience open helpers ──────────────────────────────────────────────

    /// <summary>Opens a read-only key under HKLM, respecting the host architecture.</summary>
    public static RegistryKey? OpenHklmRead(string subKey)
    {
        using var hive = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, ViewForArch());
        return hive.OpenSubKey(subKey, writable: false);
    }

    /// <summary>Opens a writable key under HKLM (creates if absent).</summary>
    public static RegistryKey OpenHklmWrite(string subKey)
    {
        using var hive = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, ViewForArch());
        return hive.CreateSubKey(subKey, writable: true)
               ?? throw new InvalidOperationException($"Cannot open/create HKLM\\{subKey}");
    }

    // ── Typed read helpers ────────────────────────────────────────────────────

    /// <summary>
    /// Reads a string value from HKLM. Returns <c>null</c> if the key or
    /// value does not exist.
    /// </summary>
    public static string? ReadString(string subKey, string valueName)
    {
        using var key = OpenHklmRead(subKey);
        return key?.GetValue(valueName) as string;
    }

    /// <summary>
    /// Reads a DWORD value from HKLM. Returns <paramref name="defaultValue"/>
    /// if the key or value does not exist.
    /// </summary>
    public static int ReadInt(string subKey, string valueName, int defaultValue = 0)
    {
        using var key = OpenHklmRead(subKey);
        if (key is null) return defaultValue;
        var raw = key.GetValue(valueName);
        return raw is int i ? i : defaultValue;
    }

    /// <summary>
    /// Reads a DWORD as a bool (non-zero = true). Returns <paramref name="defaultValue"/>
    /// if absent.
    /// </summary>
    public static bool ReadBool(string subKey, string valueName, bool defaultValue = false)
    {
        using var key = OpenHklmRead(subKey);
        if (key is null) return defaultValue;
        var raw = key.GetValue(valueName);
        return raw is int i ? i != 0 : defaultValue;
    }

    // ── Typed write helpers ───────────────────────────────────────────────────

    /// <summary>Writes a REG_SZ string value.</summary>
    public static void WriteString(string subKey, string valueName, string value)
    {
        using var key = OpenHklmWrite(subKey);
        key.SetValue(valueName, value, RegistryValueKind.String);
    }

    /// <summary>Writes a REG_EXPAND_SZ string value (mirrors Delphi WriteExpandString).</summary>
    public static void WriteExpandString(string subKey, string valueName, string value)
    {
        using var key = OpenHklmWrite(subKey);
        key.SetValue(valueName, value, RegistryValueKind.ExpandString);
    }

    /// <summary>Writes a DWORD integer value.</summary>
    public static void WriteInt(string subKey, string valueName, int value)
    {
        using var key = OpenHklmWrite(subKey);
        key.SetValue(valueName, value, RegistryValueKind.DWord);
    }

    /// <summary>Writes a boolean as a DWORD (1/0).</summary>
    public static void WriteBool(string subKey, string valueName, bool value)
        => WriteInt(subKey, valueName, value ? 1 : 0);
}
