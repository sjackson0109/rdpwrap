// Copyright 2026 sjackson0109
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System.Runtime.InteropServices;

namespace RDPWrap.Common;

/// <summary>
/// All P/Invoke declarations used across RDPWInst, RDPConf and RDPCheck.
/// Mirrors the unhooked Win32 imports from the original Delphi sources.
/// </summary>
internal static class NativeMethods
{
    // ─── DLL names ────────────────────────────────────────────────────────────
    internal const string Kernel32  = "kernel32.dll";
    internal const string Advapi32  = "advapi32.dll";
    internal const string WinSta    = "winsta.dll";

    // ─── Constants ────────────────────────────────────────────────────────────

    // Architecture
    internal const ushort PROCESSOR_ARCHITECTURE_INTEL   = 0;
    internal const ushort PROCESSOR_ARCHITECTURE_IA64    = 6;
    internal const ushort PROCESSOR_ARCHITECTURE_AMD64   = 9;

    // Registry
    internal const uint KEY_WOW64_64KEY         = 0x0100;
    internal const uint KEY_WOW64_32KEY         = 0x0200;
    internal const uint KEY_READ                = 0x20019;
    internal const uint KEY_WRITE               = 0x20006;
    internal const uint KEY_QUERY_VALUE         = 0x0001;
    internal const uint KEY_SET_VALUE           = 0x0002;

    // Service control manager
    internal const uint SC_MANAGER_CONNECT              = 0x0001;
    internal const uint SC_MANAGER_CREATE_SERVICE       = 0x0002;
    internal const uint SC_MANAGER_ENUMERATE_SERVICE    = 0x0004;
    internal const uint SC_MANAGER_ALL_ACCESS           = 0xF003F;

    internal const uint SERVICE_QUERY_CONFIG            = 0x0001;
    internal const uint SERVICE_CHANGE_CONFIG           = 0x0002;
    internal const uint SERVICE_QUERY_STATUS            = 0x0004;
    internal const uint SERVICE_START                   = 0x0010;
    internal const uint SERVICE_STOP                    = 0x0020;
    internal const uint SERVICE_ALL_ACCESS              = 0xF01FF;

    internal const uint SERVICE_WIN32                   = 0x30;
    internal const uint SERVICE_STATE_ALL               = 0x03;
    internal const uint SERVICE_NO_CHANGE               = 0xFFFFFFFF;
    internal const uint SERVICE_AUTO_START              = 0x02;
    internal const uint SERVICE_DEMAND_START            = 0x03;
    internal const uint SERVICE_DISABLED                = 0x04;

    internal const uint SERVICE_STOPPED                 = 0x00000001;
    internal const uint SERVICE_START_PENDING           = 0x00000002;
    internal const uint SERVICE_STOP_PENDING            = 0x00000003;
    internal const uint SERVICE_RUNNING                 = 0x00000004;
    internal const uint SERVICE_CONTINUE_PENDING        = 0x00000005;
    internal const uint SERVICE_PAUSE_PENDING           = 0x00000006;
    internal const uint SERVICE_PAUSED                  = 0x00000007;

    internal const uint SC_ENUM_PROCESS_INFO            = 0;
    internal const uint SC_STATUS_PROCESS_INFO          = 0;
    internal const uint ERROR_MORE_DATA                 = 234;
    internal const uint ERROR_SERVICE_DOES_NOT_EXIST    = 1060;
    internal const uint ERROR_SERVICE_NOT_ACTIVE        = 1062;

    // Process/Thread
    internal const uint PROCESS_TERMINATE               = 0x0001;
    internal const uint THREAD_SUSPEND_RESUME           = 0x0002;
    internal const uint TH32CS_SNAPTHREAD               = 0x00000004;

    // Token privileges
    internal const uint TOKEN_ADJUST_PRIVILEGES         = 0x0020;
    internal const uint TOKEN_QUERY                     = 0x0008;
    internal const uint SE_PRIVILEGE_ENABLED            = 0x00000002;

    // Privilege names
    internal const string SE_DEBUG_NAME                 = "SeDebugPrivilege";
    internal const string SE_RESTORE_NAME               = "SeRestorePrivilege";
    internal const string SE_BACKUP_NAME                = "SeBackupPrivilege";

    // Security
    internal const uint DACL_SECURITY_INFORMATION       = 0x00000004;
    internal const uint SE_FILE_OBJECT                  = 1;
    internal const uint GRANT_ACCESS                    = 1;
    internal const uint SUB_CONTAINERS_AND_OBJECTS_INHERIT = 0x3;
    internal const uint NO_MULTIPLE_TRUSTEE             = 0;
    internal const uint TRUSTEE_IS_SID                  = 0;
    internal const uint TRUSTEE_IS_WELL_KNOWN_GROUP     = 5;
    internal const uint GENERIC_ALL                     = 0x10000000;

    // LoadLibraryEx flags
    internal const uint LOAD_LIBRARY_AS_DATAFILE        = 0x00000002;

    // Version resource type
    internal const uint RT_VERSION                      = 16;

    // CreateProcess - STARTUPINFO flags
    internal const uint STARTF_USESHOWWINDOW            = 0x00000001;
    internal const ushort SW_HIDE                       = 0;

    // GetModuleHandleEx
    internal const uint GET_MODULE_HANDLE_EX_FLAG_FROM_ADDRESS      = 0x00000004;
    internal const uint GET_MODULE_HANDLE_EX_FLAG_UNCHANGED_REFCOUNT = 0x00000002;

    // Error codes
    internal const uint ERROR_SUCCESS                   = 0;
    internal const uint ERROR_ACCESS_DENIED             = 5;
    internal const uint ERROR_NOT_SUPPORTED             = 50;
    internal const uint ERROR_SERVICE_ALREADY_RUNNING   = 1056;

    // ─── Structures ───────────────────────────────────────────────────────────

    [StructLayout(LayoutKind.Sequential)]
    internal struct SYSTEM_INFO
    {
        internal ushort wProcessorArchitecture;
        internal ushort wReserved;
        internal uint   dwPageSize;
        internal IntPtr lpMinimumApplicationAddress;
        internal IntPtr lpMaximumApplicationAddress;
        internal UIntPtr dwActiveProcessorMask;
        internal uint   dwNumberOfProcessors;
        internal uint   dwProcessorType;
        internal uint   dwAllocationGranularity;
        internal ushort wProcessorLevel;
        internal ushort wProcessorRevision;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal struct STARTUPINFO
    {
        internal uint   cb;
        internal string? lpReserved;
        internal string? lpDesktop;
        internal string? lpTitle;
        internal uint   dwX, dwY, dwXSize, dwYSize, dwXCountChars, dwYCountChars;
        internal uint   dwFillAttribute;
        internal uint   dwFlags;
        internal ushort wShowWindow;
        internal ushort cbReserved2;
        internal IntPtr lpReserved2;
        internal IntPtr hStdInput, hStdOutput, hStdError;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct PROCESS_INFORMATION
    {
        internal IntPtr hProcess;
        internal IntPtr hThread;
        internal uint   dwProcessId;
        internal uint   dwThreadId;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct THREADENTRY32
    {
        internal uint dwSize;
        internal uint cntUsage;
        internal uint th32ThreadID;
        internal uint th32OwnerProcessID;
        internal int  tpBasePri;
        internal int  tpDeltaPri;
        internal uint dwFlags;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct SERVICE_STATUS_PROCESS
    {
        internal uint dwServiceType;
        internal uint dwCurrentState;
        internal uint dwControlsAccepted;
        internal uint dwWin32ExitCode;
        internal uint dwServiceSpecificExitCode;
        internal uint dwCheckPoint;
        internal uint dwWaitHint;
        internal uint dwProcessId;
        internal uint dwServiceFlags;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal struct ENUM_SERVICE_STATUS_PROCESS
    {
        internal string               lpServiceName;
        internal string               lpDisplayName;
        internal SERVICE_STATUS_PROCESS ServiceStatusProcess;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal struct QUERY_SERVICE_CONFIG
    {
        internal uint   dwServiceType;
        internal uint   dwStartType;
        internal uint   dwErrorControl;
        internal string lpBinaryPathName;
        internal string lpLoadOrderGroup;
        internal uint   dwTagId;
        internal string lpDependencies;
        internal string lpServiceStartName;
        internal string lpDisplayName;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct LUID
    {
        internal uint  LowPart;
        internal int   HighPart;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct LUID_AND_ATTRIBUTES
    {
        internal LUID  Luid;
        internal uint  Attributes;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct TOKEN_PRIVILEGES
    {
        internal uint              PrivilegeCount;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
        internal LUID_AND_ATTRIBUTES[] Privileges;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal struct EXPLICIT_ACCESS
    {
        internal uint      grfAccessPermissions;
        internal uint      grfAccessMode;
        internal uint      grfInheritance;
        internal TRUSTEE   Trustee;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal struct TRUSTEE
    {
        internal IntPtr pMultipleTrustee;
        internal uint   MultipleTrusteeOperation;
        internal uint   TrusteeForm;
        internal uint   TrusteeType;
        internal IntPtr ptstrName;        // SID pointer or string pointer
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal struct WTS_SESSION_INFO
    {
        internal uint   SessionId;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 34)]
        internal string Name;
        internal uint   State;
    }

    // ─── kernel32.dll ─────────────────────────────────────────────────────────

    [DllImport(Kernel32, SetLastError = true)]
    internal static extern void GetNativeSystemInfo(out SYSTEM_INFO lpSystemInfo);

    [DllImport(Kernel32, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool Wow64DisableWow64FsRedirection(out IntPtr oldValue);

    [DllImport(Kernel32, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool Wow64RevertWow64FsRedirection(IntPtr oldValue);

    [DllImport(Kernel32, CharSet = CharSet.Unicode, SetLastError = true)]
    internal static extern IntPtr LoadLibraryEx(string lpFileName, IntPtr hFile, uint dwFlags);

    [DllImport(Kernel32, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool FreeLibrary(IntPtr hModule);

    [DllImport(Kernel32, CharSet = CharSet.Unicode, SetLastError = true)]
    internal static extern IntPtr FindResource(IntPtr hModule, IntPtr lpName, IntPtr lpType);

    [DllImport(Kernel32, SetLastError = true)]
    internal static extern IntPtr LoadResource(IntPtr hModule, IntPtr hResInfo);

    [DllImport(Kernel32, SetLastError = true)]
    internal static extern IntPtr LockResource(IntPtr hResData);

    [DllImport(Kernel32, SetLastError = true)]
    internal static extern uint SizeofResource(IntPtr hModule, IntPtr hResInfo);

    [DllImport(Kernel32, CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool CreateProcess(
        string?           lpApplicationName,
        string            lpCommandLine,
        IntPtr            lpProcessAttributes,
        IntPtr            lpThreadAttributes,
        [MarshalAs(UnmanagedType.Bool)] bool bInheritHandles,
        uint              dwCreationFlags,
        IntPtr            lpEnvironment,
        string?           lpCurrentDirectory,
        ref STARTUPINFO   lpStartupInfo,
        out PROCESS_INFORMATION lpProcessInformation);

    [DllImport(Kernel32, SetLastError = true)]
    internal static extern uint WaitForSingleObject(IntPtr hHandle, uint dwMilliseconds);

    [DllImport(Kernel32, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool CloseHandle(IntPtr hObject);

    [DllImport(Kernel32, SetLastError = true)]
    internal static extern IntPtr OpenProcess(uint dwDesiredAccess,
        [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle, uint dwProcessId);

    [DllImport(Kernel32, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool TerminateProcess(IntPtr hProcess, uint uExitCode);

    [DllImport(Kernel32, SetLastError = true)]
    internal static extern uint GetCurrentProcessId();

    [DllImport(Kernel32, SetLastError = true)]
    internal static extern uint GetCurrentThreadId();

    [DllImport(Kernel32, SetLastError = true)]
    internal static extern IntPtr CreateToolhelp32Snapshot(uint dwFlags, uint th32ProcessID);

    [DllImport(Kernel32, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool Thread32First(IntPtr hSnapshot, ref THREADENTRY32 lpte);

    [DllImport(Kernel32, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool Thread32Next(IntPtr hSnapshot, ref THREADENTRY32 lpte);

    [DllImport(Kernel32, SetLastError = true)]
    internal static extern IntPtr OpenThread(uint dwDesiredAccess,
        [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle, uint dwThreadId);

    [DllImport(Kernel32, SetLastError = true)]
    internal static extern uint SuspendThread(IntPtr hThread);

    [DllImport(Kernel32, SetLastError = true)]
    internal static extern uint ResumeThread(IntPtr hThread);

    [DllImport(Kernel32, CharSet = CharSet.Unicode, SetLastError = true)]
    internal static extern uint GetModuleFileName(IntPtr hModule,
        System.Text.StringBuilder lpFilename, uint nSize);

    [DllImport(Kernel32, CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool GetModuleHandleEx(uint dwFlags, IntPtr lpModuleName,
        out IntPtr phModule);

    [DllImport(Kernel32, CharSet = CharSet.Unicode, SetLastError = true)]
    internal static extern uint ExpandEnvironmentStrings(string lpSrc,
        System.Text.StringBuilder lpDst, uint nSize);

    [DllImport(Kernel32, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool DeleteFile(string lpFileName);

    [DllImport(Kernel32, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool RemoveDirectory(string lpPathName);

    [DllImport(Kernel32, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool OpenProcessToken(IntPtr processHandle,
        uint desiredAccess, out IntPtr tokenHandle);

    // ─── advapi32.dll ─────────────────────────────────────────────────────────

    [DllImport(Advapi32, SetLastError = true)]
    internal static extern IntPtr OpenSCManager(string? lpMachineName,
        string? lpDatabaseName, uint dwDesiredAccess);

    [DllImport(Advapi32, CharSet = CharSet.Unicode, SetLastError = true)]
    internal static extern IntPtr OpenService(IntPtr hSCManager,
        string lpServiceName, uint dwDesiredAccess);

    [DllImport(Advapi32, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool CloseServiceHandle(IntPtr hSCObject);

    [DllImport(Advapi32, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool QueryServiceConfig(IntPtr hService,
        IntPtr lpServiceConfig, uint cbBufSize, out uint pcbBytesNeeded);

    [DllImport(Advapi32, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool QueryServiceStatusEx(IntPtr hService,
        uint InfoLevel, IntPtr lpBuffer, uint cbBufSize, out uint pcbBytesNeeded);

    [DllImport(Advapi32, CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool ChangeServiceConfig(IntPtr hService,
        uint dwServiceType, uint dwStartType, uint dwErrorControl,
        string? lpBinaryPathName, string? lpLoadOrderGroup, IntPtr lpdwTagId,
        string? lpDependencies, string? lpServiceStartName, string? lpPassword,
        string? lpDisplayName);

    [DllImport(Advapi32, CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool StartService(IntPtr hService,
        uint dwNumServiceArgs, string[]? lpServiceArgVectors);

    [DllImport(Advapi32, CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool EnumServicesStatusEx(
        IntPtr hSCManager, uint InfoLevel, uint dwServiceType, uint dwServiceState,
        IntPtr lpServices, uint cbBufSize,
        out uint pcbBytesNeeded, out uint lpServicesReturned,
        ref uint lpResumeHandle, string? pszGroupName);

    [DllImport(Advapi32, CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool LookupPrivilegeValue(string? lpSystemName,
        string lpName, out LUID lpLuid);

    [DllImport(Advapi32, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool AdjustTokenPrivileges(IntPtr tokenHandle,
        [MarshalAs(UnmanagedType.Bool)] bool disableAllPrivileges,
        ref TOKEN_PRIVILEGES newState, uint bufferLength,
        IntPtr previousState, IntPtr returnLength);

    [DllImport(Advapi32, CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool ConvertStringSidToSid(string stringSid, out IntPtr sid);

    [DllImport(Advapi32, SetLastError = true)]
    internal static extern uint SetEntriesInAcl(uint cCountOfExplicitEntries,
        ref EXPLICIT_ACCESS pListOfExplicitEntries, IntPtr oldAcl, out IntPtr newAcl);

    [DllImport(Advapi32, CharSet = CharSet.Unicode, SetLastError = true)]
    internal static extern uint SetNamedSecurityInfo(string pObjectName,
        uint ObjectType, uint SecurityInfo,
        IntPtr psidOwner, IntPtr psidGroup, IntPtr pDacl, IntPtr pSacl);

    [DllImport(Advapi32, SetLastError = true)]
    internal static extern IntPtr LocalFree(IntPtr hMem);

    // ─── winsta.dll ───────────────────────────────────────────────────────────

    /// <summary>
    /// Enumerates WTS sessions on the local server.
    /// Pass <c>IntPtr.Zero</c> as hServer for the local machine.
    /// </summary>
    [DllImport(WinSta, EntryPoint = "WinStationEnumerateW",
        CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool WinStationEnumerate(IntPtr hServer,
        out IntPtr ppSessionInfo, out uint pCount);

    [DllImport(WinSta, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool WinStationFreeMemory(IntPtr p);
}
