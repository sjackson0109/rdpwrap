// Copyright 2024 sjackson0109 — Apache License 2.0
using System.Runtime.InteropServices;

namespace RDPWrap.Common;

/// <summary>
/// Security helpers: granting SID-based DACL entries (GrantSidFullAccess) and
/// adjusting process token privileges (AddPrivilege). Mirrors the Delphi
/// implementations in RDPWInst.dpr.
/// </summary>
public static class SecurityHelper
{
    // ── DACL: grant a well-known SID full access to a file/folder ─────────────

    /// <summary>
    /// Grants GENERIC_ALL access to the well-known SID string
    /// <paramref name="stringSid"/> on the file/directory at
    /// <paramref name="path"/>. Mirrors the Delphi GrantSidFullAccess
    /// procedure (used for "S-1-5-18" = Local System, "S-1-5-6" = Service).
    /// </summary>
    public static void GrantSidFullAccess(string path, string stringSid)
    {
        if (!NativeMethods.ConvertStringSidToSid(stringSid, out IntPtr pSid))
        {
            Console.Error.WriteLine(
                $"[-] ConvertStringSidToSid error (code {Marshal.GetLastWin32Error()}) " +
                $"for SID {stringSid}.");
            return;
        }

        try
        {
            var ea = new NativeMethods.EXPLICIT_ACCESS
            {
                grfAccessPermissions = NativeMethods.GENERIC_ALL,
                grfAccessMode        = NativeMethods.GRANT_ACCESS,
                grfInheritance       = NativeMethods.SUB_CONTAINERS_AND_OBJECTS_INHERIT,
                Trustee = new NativeMethods.TRUSTEE
                {
                    pMultipleTrustee         = IntPtr.Zero,
                    MultipleTrusteeOperation = NativeMethods.NO_MULTIPLE_TRUSTEE,
                    TrusteeForm              = NativeMethods.TRUSTEE_IS_SID,
                    TrusteeType              = NativeMethods.TRUSTEE_IS_WELL_KNOWN_GROUP,
                    ptstrName                = pSid
                }
            };

            uint result = NativeMethods.SetEntriesInAcl(1, ref ea, IntPtr.Zero, out IntPtr pNewAcl);
            if (result == NativeMethods.ERROR_SUCCESS)
            {
                uint secResult = NativeMethods.SetNamedSecurityInfo(
                    path,
                    NativeMethods.SE_FILE_OBJECT,
                    NativeMethods.DACL_SECURITY_INFORMATION,
                    IntPtr.Zero, IntPtr.Zero, pNewAcl, IntPtr.Zero);

                if (secResult != NativeMethods.ERROR_SUCCESS)
                    Console.Error.WriteLine(
                        $"[-] SetNamedSecurityInfo error (code {secResult}).");

                NativeMethods.LocalFree(pNewAcl);
            }
            else
            {
                Console.Error.WriteLine($"[-] SetEntriesInAcl error (code {result}).");
            }
        }
        finally
        {
            NativeMethods.LocalFree(pSid);
        }
    }

    // ── Token privileges ──────────────────────────────────────────────────────

    /// <summary>
    /// Enables the named privilege (e.g. <c>SeDebugPrivilege</c>) for the
    /// current process token. Mirrors the Delphi AddPrivilege function.
    /// Returns <c>true</c> on success.
    /// </summary>
    public static bool AddPrivilege(string privilegeName)
    {
        if (!NativeMethods.OpenProcessToken(
            System.Diagnostics.Process.GetCurrentProcess().Handle,
            NativeMethods.TOKEN_ADJUST_PRIVILEGES | NativeMethods.TOKEN_QUERY,
            out IntPtr hToken))
        {
            Console.Error.WriteLine(
                $"[-] OpenProcessToken error (code {Marshal.GetLastWin32Error()}).");
            return false;
        }

        try
        {
            if (!NativeMethods.LookupPrivilegeValue(null, privilegeName, out var luid))
            {
                Console.Error.WriteLine(
                    $"[-] LookupPrivilegeValue error (code {Marshal.GetLastWin32Error()}).");
                return false;
            }

            var tp = new NativeMethods.TOKEN_PRIVILEGES
            {
                PrivilegeCount = 1,
                Privileges     = new[]
                {
                    new NativeMethods.LUID_AND_ATTRIBUTES
                    {
                        Luid       = luid,
                        Attributes = NativeMethods.SE_PRIVILEGE_ENABLED
                    }
                }
            };

            if (!NativeMethods.AdjustTokenPrivileges(
                hToken, false, ref tp,
                (uint)Marshal.SizeOf(tp),
                IntPtr.Zero, IntPtr.Zero))
            {
                Console.Error.WriteLine(
                    $"[-] AdjustTokenPrivileges error (code {Marshal.GetLastWin32Error()}).");
                return false;
            }

            return true;
        }
        finally
        {
            NativeMethods.CloseHandle(hToken);
        }
    }

    // ── RDP-Tcp listener ──────────────────────────────────────────────────────

    /// <summary>
    /// Returns <c>true</c> when there is an active "RDP-Tcp" WTS listener on
    /// the local machine — i.e. <c>WinStationEnumerateW</c> returns a session
    /// named "RDP-Tcp". Mirrors the Delphi IsListenerWorking function.
    /// </summary>
    public static bool IsRdpListenerWorking()
    {
        if (!NativeMethods.WinStationEnumerate(IntPtr.Zero,
            out IntPtr ppInfo, out uint count))
            return false;

        try
        {
            int ptrSize   = IntPtr.Size;
            // WTS_SESSION_INFO is: DWORD SessionId + 34 WCHARs (Name) + DWORD State
            // = 4 + 68 + 4 = 76 bytes, but aligned to 4-byte boundary → 76 bytes.
            int entrySize = 4 + 34 * 2 + 4;   // = 76

            for (uint i = 0; i < count; i++)
            {
                IntPtr entry = ppInfo + (int)(i * (uint)entrySize);
                // Name starts at offset 4
                string name = Marshal.PtrToStringUni(entry + 4, 34).TrimEnd('\0');
                if (name == "RDP-Tcp") return true;
            }
        }
        finally
        {
            NativeMethods.WinStationFreeMemory(ppInfo);
        }

        return false;
    }
}
