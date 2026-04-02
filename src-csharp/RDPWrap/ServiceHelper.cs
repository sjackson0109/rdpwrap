// Copyright 2026 sjackson0109 — Apache License 2.0
using System.Runtime.InteropServices;

namespace RDPWrap.Common;

/// <summary>
/// Wrappers around the Windows Service Control Manager API, mirroring the
/// SvcGetStart / SvcConfigStart / SvcStart / CheckTermsrvProcess helpers
/// in RDPWInst.dpr and GetTermSrvState in RDPConf MainUnit.pas.
/// </summary>
public static class ServiceHelper
{
    // ── Start-type query / change ─────────────────────────────────────────────

    /// <summary>
    /// Returns the configured start type of <paramref name="serviceName"/>
    /// (e.g. <c>SERVICE_AUTO_START</c>) or <c>-1</c> on failure.
    /// </summary>
    public static int GetStartType(string serviceName)
    {
        var hSC = NativeMethods.OpenSCManager(null, null, NativeMethods.SC_MANAGER_CONNECT);
        if (hSC == IntPtr.Zero) return -1;
        try
        {
            var hSvc = NativeMethods.OpenService(hSC, serviceName,
                NativeMethods.SERVICE_QUERY_CONFIG);
            if (hSvc == IntPtr.Zero) return -1;
            try
            {
                NativeMethods.QueryServiceConfig(hSvc, IntPtr.Zero, 0, out uint needed);
                var buf = Marshal.AllocHGlobal((int)needed);
                try
                {
                    if (!NativeMethods.QueryServiceConfig(hSvc, buf, needed, out _))
                        return -1;
                    // dwStartType is the second DWORD in QUERY_SERVICE_CONFIG
                    return Marshal.ReadInt32(buf, 4);
                }
                finally { Marshal.FreeHGlobal(buf); }
            }
            finally { NativeMethods.CloseServiceHandle(hSvc); }
        }
        finally { NativeMethods.CloseServiceHandle(hSC); }
    }

    /// <summary>
    /// Changes the start type of <paramref name="serviceName"/> to
    /// <paramref name="startType"/> (one of the <c>SERVICE_*_START</c> constants).
    /// </summary>
    public static bool SetStartType(string serviceName, uint startType)
    {
        var hSC = NativeMethods.OpenSCManager(null, null, NativeMethods.SC_MANAGER_CONNECT);
        if (hSC == IntPtr.Zero) return false;
        try
        {
            var hSvc = NativeMethods.OpenService(hSC, serviceName,
                NativeMethods.SERVICE_CHANGE_CONFIG);
            if (hSvc == IntPtr.Zero) return false;
            try
            {
                return NativeMethods.ChangeServiceConfig(hSvc,
                    NativeMethods.SERVICE_NO_CHANGE, startType,
                    NativeMethods.SERVICE_NO_CHANGE,
                    null, null, IntPtr.Zero, null, null, null, null);
            }
            finally { NativeMethods.CloseServiceHandle(hSvc); }
        }
        finally { NativeMethods.CloseServiceHandle(hSC); }
    }

    // ── Start a service ───────────────────────────────────────────────────────

    /// <summary>
    /// Starts <paramref name="serviceName"/>. If the service is already
    /// running (error 1056) it waits 2 s and retries once, matching the
    /// original Delphi SvcStart behaviour.
    /// Returns <c>true</c> on success.
    /// </summary>
    public static bool StartService(string serviceName)
    {
        var hSC = NativeMethods.OpenSCManager(null, null, NativeMethods.SC_MANAGER_CONNECT);
        if (hSC == IntPtr.Zero) return false;
        try
        {
            var hSvc = NativeMethods.OpenService(hSC, serviceName,
                NativeMethods.SERVICE_START);
            if (hSvc == IntPtr.Zero) return false;
            try
            {
                if (NativeMethods.StartService(hSvc, 0, null)) return true;

                var err = Marshal.GetLastWin32Error();
                if (err == (int)NativeMethods.ERROR_SERVICE_ALREADY_RUNNING)
                {
                    Thread.Sleep(2000);
                    return NativeMethods.StartService(hSvc, 0, null);
                }
                return false;
            }
            finally { NativeMethods.CloseServiceHandle(hSvc); }
        }
        finally { NativeMethods.CloseServiceHandle(hSC); }
    }

    // ── Status query ──────────────────────────────────────────────────────────

    /// <summary>
    /// Returns the <c>dwCurrentState</c> for <paramref name="serviceName"/>
    /// (e.g. <c>SERVICE_RUNNING = 4</c>) or <c>-1</c> on failure.
    /// </summary>
    public static int GetCurrentState(string serviceName)
    {
        var hSC = NativeMethods.OpenSCManager(null, null, NativeMethods.SC_MANAGER_CONNECT);
        if (hSC == IntPtr.Zero) return -1;
        try
        {
            var hSvc = NativeMethods.OpenService(hSC, serviceName,
                NativeMethods.SERVICE_QUERY_STATUS);
            if (hSvc == IntPtr.Zero) return -1;
            try
            {
                NativeMethods.QueryServiceStatusEx(hSvc,
                    NativeMethods.SC_STATUS_PROCESS_INFO,
                    IntPtr.Zero, 0, out uint needed);

                var buf = Marshal.AllocHGlobal((int)needed);
                try
                {
                    if (!NativeMethods.QueryServiceStatusEx(hSvc,
                        NativeMethods.SC_STATUS_PROCESS_INFO,
                        buf, needed, out _)) return -1;

                    // dwCurrentState is the second DWORD in SERVICE_STATUS_PROCESS
                    return Marshal.ReadInt32(buf, 4);
                }
                finally { Marshal.FreeHGlobal(buf); }
            }
            finally { NativeMethods.CloseServiceHandle(hSvc); }
        }
        finally { NativeMethods.CloseServiceHandle(hSC); }
    }

    // ── Process-info enumeration ──────────────────────────────────────────────

    /// <summary>
    /// Represents the name and PID of a service process returned by
    /// <see cref="EnumServiceProcesses"/>.
    /// </summary>
    public record ServiceProcessInfo(string ServiceName, string DisplayName, uint ProcessId, uint CurrentState);

    /// <summary>
    /// Enumerates all Win32 services (all states) and returns their
    /// process information. Mirrors the EnumServicesStatusEx loop in
    /// CheckTermsrvProcess.
    /// </summary>
    public static IReadOnlyList<ServiceProcessInfo> EnumServiceProcesses()
    {
        var result = new List<ServiceProcessInfo>();

        var hSC = NativeMethods.OpenSCManager(null, null,
            NativeMethods.SC_MANAGER_CONNECT | NativeMethods.SC_MANAGER_ENUMERATE_SERVICE);
        if (hSC == IntPtr.Zero) return result;

        try
        {
            uint resumeHandle = 0;
            // Ask for the required buffer size
            NativeMethods.EnumServicesStatusEx(hSC,
                NativeMethods.SC_ENUM_PROCESS_INFO,
                NativeMethods.SERVICE_WIN32,
                NativeMethods.SERVICE_STATE_ALL,
                IntPtr.Zero, 0,
                out uint needed, out _, ref resumeHandle, null);

            if (needed == 0) return result;

            var buf = Marshal.AllocHGlobal((int)needed);
            try
            {
                resumeHandle = 0;
                if (!NativeMethods.EnumServicesStatusEx(hSC,
                    NativeMethods.SC_ENUM_PROCESS_INFO,
                    NativeMethods.SERVICE_WIN32,
                    NativeMethods.SERVICE_STATE_ALL,
                    buf, needed,
                    out _, out uint returned, ref resumeHandle, null))
                    return result;

                // ENUM_SERVICE_STATUS_PROCESS layout (Unicode, packed):
                //   IntPtr  lpServiceName  (pointer to string)
                //   IntPtr  lpDisplayName  (pointer to string)
                //   SERVICE_STATUS_PROCESS (9 × DWORD = 36 bytes)
                int ptrSize   = IntPtr.Size;
                int entrySize = ptrSize * 2 + 36;   // 2 string pointers + status struct

                for (int i = 0; i < (int)returned; i++)
                {
                    var entryPtr = buf + i * entrySize;
                    var namePtr  = Marshal.ReadIntPtr(entryPtr);
                    var dispPtr  = Marshal.ReadIntPtr(entryPtr + ptrSize);

                    var svcName  = Marshal.PtrToStringUni(namePtr) ?? string.Empty;
                    var dispName = Marshal.PtrToStringUni(dispPtr) ?? string.Empty;

                    // dwCurrentState at offset 4, dwProcessId at offset 28
                    var statusBase = entryPtr + ptrSize * 2;
                    uint currentState = (uint)Marshal.ReadInt32(statusBase + 4);
                    uint pid          = (uint)Marshal.ReadInt32(statusBase + 28);

                    result.Add(new ServiceProcessInfo(svcName, dispName, pid, currentState));
                }
            }
            finally { Marshal.FreeHGlobal(buf); }
        }
        finally { NativeMethods.CloseServiceHandle(hSC); }

        return result;
    }
}
