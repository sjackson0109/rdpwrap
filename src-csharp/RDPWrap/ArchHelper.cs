// Copyright 2026 sjackson0109 — Apache License 2.0
using System.Runtime.InteropServices;

namespace RDPWrap.Common;

/// <summary>
/// Architecture detection and WOW64 file-system redirection control.
/// Mirrors the Arch / DisableWowRedirection / RevertWowRedirection logic
/// from RDPWInst.dpr and RDPConf MainUnit.pas.
/// </summary>
public static class ArchHelper
{
    private static readonly Lazy<byte> _arch = new(DetectArch);

    /// <summary>Raw architecture byte: 32 or 64. 0 = unsupported.</summary>
    public static byte Arch => _arch.Value;

    /// <summary><c>true</c> when running on a 64-bit Windows installation.</summary>
    public static bool Is64Bit => Arch == 64;

    /// <summary>
    /// Returns <c>true</c> when the processor architecture is supported
    /// (x86 or x64). Itanium and unknown architectures return <c>false</c>.
    /// </summary>
    public static bool IsSupported => Arch != 0;

    private static byte DetectArch()
    {
        NativeMethods.GetNativeSystemInfo(out var si);
        return si.wProcessorArchitecture switch
        {
            NativeMethods.PROCESSOR_ARCHITECTURE_INTEL => 32,
            NativeMethods.PROCESSOR_ARCHITECTURE_AMD64 => 64,
            _ => 0    // Itanium or unknown — unsupported
        };
    }

    // ── WOW64 filesystem redirection ─────────────────────────────────────────

    private static IntPtr _wow64OldValue = IntPtr.Zero;

    /// <summary>
    /// Disables WOW64 filesystem redirection so that 32-bit processes can
    /// reach the real <c>%SystemRoot%\System32</c>. Call only on 64-bit hosts.
    /// Returns <c>true</c> on success.
    /// </summary>
    public static bool DisableWow64Redirection()
    {
        if (!Is64Bit) return false;
        return NativeMethods.Wow64DisableWow64FsRedirection(out _wow64OldValue);
    }

    /// <summary>
    /// Reverts the WOW64 filesystem redirection state saved by the last call
    /// to <see cref="DisableWow64Redirection"/>.
    /// </summary>
    public static bool RevertWow64Redirection()
    {
        if (!Is64Bit) return false;
        return NativeMethods.Wow64RevertWow64FsRedirection(_wow64OldValue);
    }

    // ── Environment path expansion ────────────────────────────────────────────

    /// <summary>
    /// Expands environment strings in <paramref name="path"/>, replacing
    /// <c>%ProgramFiles%</c> with <c>%ProgramW6432%</c> on 64-bit hosts
    /// to avoid redirection to the x86 Program Files folder.
    /// </summary>
    public static string ExpandPath(string path)
    {
        if (Is64Bit)
            path = path.Replace("%ProgramFiles%", "%ProgramW6432%",
                                StringComparison.OrdinalIgnoreCase);

        var buf = new System.Text.StringBuilder(1024);
        NativeMethods.ExpandEnvironmentStrings(path, buf, (uint)buf.Capacity);
        return buf.ToString();
    }
}
