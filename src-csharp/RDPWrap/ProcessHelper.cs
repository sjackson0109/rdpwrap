// Copyright 2026 sjackson0109 — Apache License 2.0
using System.Runtime.InteropServices;

namespace RDPWrap.Common;

/// <summary>
/// Process creation and termination helpers. Mirrors ExecWait and KillProcess
/// from RDPWInst.dpr (console variant) and RDPConf MainUnit.pas (GUI variant).
/// </summary>
public static class ProcessHelper
{
    /// <summary>
    /// Creates a process from <paramref name="commandLine"/>, waits for it to
    /// exit, then returns <c>true</c>. The process window is hidden.
    /// Mirrors the Delphi ExecWait procedure.
    /// </summary>
    public static bool ExecWait(string commandLine, bool hideWindow = true)
    {
        var si = new NativeMethods.STARTUPINFO
        {
            cb        = (uint)Marshal.SizeOf<NativeMethods.STARTUPINFO>(),
            dwFlags   = hideWindow ? NativeMethods.STARTF_USESHOWWINDOW : 0u,
            wShowWindow = hideWindow ? NativeMethods.SW_HIDE : (ushort)1
        };

        // CommandLine must be mutable — pass a copy
        string cmdCopy = new(commandLine);

        if (!NativeMethods.CreateProcess(null, cmdCopy, IntPtr.Zero, IntPtr.Zero,
            true, 0, IntPtr.Zero, null, ref si, out var pi))
        {
            Console.Error.WriteLine(
                $"[-] CreateProcess error (code {Marshal.GetLastWin32Error()}).");
            return false;
        }

        NativeMethods.WaitForSingleObject(pi.hProcess, 0xFFFFFFFF);
        NativeMethods.CloseHandle(pi.hThread);
        NativeMethods.CloseHandle(pi.hProcess);
        return true;
    }

    /// <summary>
    /// Terminates the process with <paramref name="pid"/>. Mirrors the Delphi
    /// KillProcess procedure.
    /// </summary>
    public static void KillProcess(uint pid)
    {
        var hProc = NativeMethods.OpenProcess(NativeMethods.PROCESS_TERMINATE, false, pid);
        if (hProc == IntPtr.Zero)
        {
            Console.Error.WriteLine(
                $"[-] OpenProcess error (code {Marshal.GetLastWin32Error()}).");
            return;
        }

        if (!NativeMethods.TerminateProcess(hProc, 0))
            Console.Error.WriteLine(
                $"[-] TerminateProcess error (code {Marshal.GetLastWin32Error()}).");

        NativeMethods.CloseHandle(hProc);
    }
}
