# Phase 1 — Solution & Shared Library (RDPWrap.Common)
Create src-csharp/ solution with four projects: RDPWrap.Common (class lib), RDPWInst (console), RDPConf (WinForms), RDPCheck (WinForms)
NativeMethods.cs — all P/Invoke declarations: kernel32 (GetNativeSystemInfo, LoadLibraryEx, FindResource, Wow64Disable/RevertFsRedirection, CreateProcess, OpenProcess, TerminateProcess, CreateToolhelp32Snapshot, Thread32First/Next, OpenThread, SuspendThread/ResumeThread), advapi32 (all SCM + ACL + token functions), winsta.dll (WinStationEnumerateW, WinStationFreeMemory)
RegistryHelper.cs — HKLM read/write helpers with KEY_WOW64_64KEY flag support for 64-bit hosts
ServiceHelper.cs — OpenSCManager/OpenService/QueryServiceConfig/QueryServiceStatusEx/ChangeServiceConfig/StartService wrappers (or wrap System.ServiceProcess.ServiceController where sufficient)
ArchHelper.cs — GetNativeSystemInfo-based arch detection + Wow64DisableWow64FsRedirection/RevertWow64FsRedirection helpers
FileVersionHelper.cs — GetFileVersion via LoadLibraryEx + manual VS_VERSIONINFO parsing (or FileVersionInfo.GetVersionInfo())
ProcessHelper.cs — ExecWait (hidden Process.Start + WaitForExit), KillProcess
HttpHelper.cs — replace WinInet with HttpClient: DownloadStringAsync (for INI content), DownloadFileAsync (for binary assets)
ResourceHelper.cs — Assembly.GetManifestResourceStream → extract to file path
IniHelper.cs — INIHasSection(path, section) string search
SecurityHelper.cs — ConvertStringSidToSid + SetEntriesInAcl + SetNamedSecurityInfo (grant SID full access), AddPrivilege (token privilege adjustment)

# Phase 2 — RDPWInst (Console Installer)
Argument parsing + main dispatch (/install, /uninstall, /update, /wraponly)
CheckInstall() — verify TermService ImagePath (svchost) and ServiceDll (not third-party)
CheckTermsrvProcess() — EnumServicesStatusEx loop to find TermService PID + co-hosted services; auto-start if PID=0
CheckTermsrvDependencies() — ensure CertPropSvc and SessionEnv are not disabled
CheckTermsrvVersion() — read termsrv.dll version, classify as unsupported / partial / full using built-in INI
TSConfigRegistry(enable) — write fDenyTSConnections, EnableConcurrentSessions, AllowMultipleTSSessions, AllowRemoteRPC, EnableLinkedConnections
ExtractFiles() — pull rdpw32/rdpw64, rdpclip, rfxvmt, config out of embedded resources; create install dir; set ACLs for S-1-5-18 and S-1-5-6
SetWrapperDll() / ResetServiceDll() — write/restore ServiceDll registry value (REG_EXPAND_SZ); reg.exe workaround for Vista
DeleteFiles() — remove rdpwrap.ini, rdpwrap.dll, install folder on uninstall
GitINIFile() / DownloadFileToDisk() — HttpClient-based downloads from releases/latest/download/rdpwrap.ini
TryAutoGenerateOffsets() — download RDPWrapOffsetFinder_x64/x86.exe + Zydis_x64/x86.dll, run via cmd.exe /c "... >> rdpwrap.ini", clean up temp files
AddPrivilege() / KillProcess() / full install/uninstall/update orchestration wiring
Embed binary resources (rdpw32.dll, rdpw64.dll, rdpclip*, rfxvmt*, rdpwrap.ini) into the .csproj as EmbeddedResource
Add UAC app manifest: requestedExecutionLevel = requireAdministrator

# Phase 3 — RDPConf (WinForms Configuration GUI)
MainForm layout — CheckBox (AllowTSConnections, SingleSessionPerUser, HideUsers, CustomPrg), two GroupBox+RadioButton clusters (NLA ×3, Shadow ×5), NumericUpDown for port, status Label pairs for Service/Listener/Wrapper/TS version/Wrapper version, OK/Cancel/Apply/License Button, System.Windows.Forms.Timer
ReadSettings() — pull all values from HKLM\...\Terminal Server and RDP-Tcp registry keys into controls
WriteSettings() — write all controls back to registry; on port change call netsh advfirewall firewall set rule name="Remote Desktop" new localport=…
TimerTimer() — periodic refresh of all status labels (wrapper installed?, service state, listener active, file versions, support level)
IsWrapperInstalled() / GetTermSrvState() (via ServiceController) / IsListenerWorking() (via WinStationEnumerateW)
CheckSupport() — load rdpwrap.ini from install path, search for [major.minor.release.build] section
LicenseForm — TextBox (multiline, readonly) populated from embedded LICENSE resource + Accept/Decline buttons
FormCreate — arch detection, Wow64DisableWow64FsRedirection; FormClosed — RevertWow64FsRedirection; unsaved-changes guard on close
UAC manifest + app.manifest (requireAdministrator)

# Phase 4 — RDPCheck (WinForms RDP Tester)
Add COM interop reference for mstscax.dll (AxMSTSCLib) — either tlbimp-generated assembly or NuGet Microsoft.Rdp.Client
MainForm layout — AxMsRdpClient2 ActiveX host filling the form
FormLoad() — read then zero-out SecurityLayer/UserAuthentication in registry, read PortNumber, Sleep(1000), call .Connect()
OnDisconnected() — full 50-entry reason-code → English string table (matching the Delphi source exactly), MessageBox for codes >2, restore SecurityLayer/UserAuthentication, Application.Exit()
UAC manifest (requireAdministrator — needed for HKLM registry writes)

# Phase 5 — Build & CI
Directory.Build.props — shared <TargetFramework>net481</TargetFramework> (or net8.0-windows), <Platforms>x86;x64</Platforms>, <Nullable>enable</Nullable>, <ImplicitUsings>enable</ImplicitUsings>
Update GitHub Actions workflows — replace Delphi compiler steps with dotnet build / dotnet publish -r win-x64 -r win-x86
Remove Delphi compiler steps, Delphi CI caching, .dproj/.dfm artifact handling from all workflows
Code-sign configuration — signtool.exe step in release workflow for all four output binaries
Update README.md with new build prerequisites (.NET SDK), build commands, and note that Delphi is no longer required