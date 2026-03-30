// Copyright 2024 sjackson0109 — Apache License 2.0
//
// InstallerEngine — translates every procedure in RDPWInst.dpr to C#.
// Consult src-installer/RDPWInst.dpr for the authoritative Delphi source.

using System.Reflection;
using RDPWrap.Common;

namespace RDPWInst;

/// <summary>
/// Orchestrates install / uninstall / update / restart of the RDP Wrapper.
/// All public methods return an exit code (0 = success).
/// </summary>
internal sealed class InstallerEngine
{
    // ── State (mirrors Delphi globals) ────────────────────────────────────────

    private bool   _installed;
    private bool   _online;
    private string _wrapPath        = string.Empty;
    private string _termServicePath = string.Empty;
    private string _termSrvVerTxt   = string.Empty;
    private uint   _termServicePid;
    private string[] _shareServices = Array.Empty<string>();

    private const string TermService = "TermService";

    // Latest release download base URL
    private const string ReleaseBaseUrl =
        "https://github.com/sjackson0109/rdpwrap/releases/latest/download/";

    // ── Public entry points ───────────────────────────────────────────────────

    /// <summary>
    /// Install the wrapper. Mirrors the <c>-i</c> branch in RDPWInst.dpr.
    /// </summary>
    public int Install(bool toSystem32, bool online)
    {
        if (_installed)
        {
            Console.WriteLine("[*] RDP Wrapper Library is already installed.");
            return unchecked((int)NativeMethods.ERROR_ACCESS_DENIED);
        }

        Console.WriteLine("[*] Notice to user:");
        Console.WriteLine("  - By using all or any portion of this software, you are agreeing");
        Console.WriteLine("  to be bound by all the terms and conditions of the license agreement.");
        Console.WriteLine("  - To read the license agreement, run the installer with -l parameter.");
        Console.WriteLine("  - If you do not agree to any terms of the license agreement,");
        Console.WriteLine("  do not use the software.");
        Console.WriteLine("[*] Installing...");

        _wrapPath = toSystem32
            ? @"%SystemRoot%\system32\rdpwrap.dll"
            : @"%ProgramFiles%\RDP Wrapper\rdpwrap.dll";

        if (ArchHelper.Is64Bit) ArchHelper.DisableWow64Redirection();

        CheckTermsrvVersion();
        CheckTermsrvProcess();

        Console.WriteLine("[*] Extracting files...");
        _online = online;
        ExtractFiles();

        Console.WriteLine("[*] Checking INI coverage for installed termsrv.dll version...");
        TryAutoGenerateOffsets();

        Console.WriteLine("[*] Configuring service library...");
        SetWrapperDll();

        Console.WriteLine("[*] Checking dependencies...");
        CheckTermsrvDependencies();

        Console.WriteLine("[*] Terminating service...");
        SecurityHelper.AddPrivilege(NativeMethods.SE_DEBUG_NAME);
        ProcessHelper.KillProcess(_termServicePid);
        Thread.Sleep(1000);

        RestartSharedServices();
        Thread.Sleep(500);
        ServiceHelper.StartService(TermService);
        Thread.Sleep(500);

        Console.WriteLine("[*] Configuring registry...");
        TSConfigRegistry(enable: true);
        Console.WriteLine("[*] Configuring firewall...");
        TSConfigFirewall(enable: true);

        Console.WriteLine("[+] Successfully installed.");

        if (ArchHelper.Is64Bit) ArchHelper.RevertWow64Redirection();
        return 0;
    }

    /// <summary>
    /// Uninstall the wrapper. Mirrors the <c>-u</c> branch.
    /// </summary>
    public int Uninstall(bool keepSettings)
    {
        if (!_installed)
        {
            Console.WriteLine("[*] RDP Wrapper Library is not installed.");
            return unchecked((int)NativeMethods.ERROR_ACCESS_DENIED);
        }

        Console.WriteLine("[*] Uninstalling...");
        if (ArchHelper.Is64Bit) ArchHelper.DisableWow64Redirection();

        CheckTermsrvProcess();

        Console.WriteLine("[*] Resetting service library...");
        ResetServiceDll();

        Console.WriteLine("[*] Terminating service...");
        SecurityHelper.AddPrivilege(NativeMethods.SE_DEBUG_NAME);
        ProcessHelper.KillProcess(_termServicePid);
        Thread.Sleep(1000);

        Console.WriteLine("[*] Removing files...");
        DeleteFiles();

        RestartSharedServices();
        Thread.Sleep(500);
        ServiceHelper.StartService(TermService);
        Thread.Sleep(500);

        if (!keepSettings)
        {
            Console.WriteLine("[*] Configuring registry...");
            TSConfigRegistry(enable: false);
            Console.WriteLine("[*] Configuring firewall...");
            TSConfigFirewall(enable: false);
        }

        if (ArchHelper.Is64Bit) ArchHelper.RevertWow64Redirection();
        Console.WriteLine("[+] Successfully uninstalled.");
        return 0;
    }

    /// <summary>
    /// Download the latest rdpwrap.ini. Mirrors the <c>-w</c> / CheckUpdate branch.
    /// </summary>
    public int Update()
    {
        if (!_installed)
        {
            Console.WriteLine("[*] RDP Wrapper Library is not installed.");
            return unchecked((int)NativeMethods.ERROR_ACCESS_DENIED);
        }

        Console.WriteLine("[*] Checking for updates...");
        return CheckUpdate();
    }

    /// <summary>
    /// Force-restart Terminal Services. Mirrors the <c>-r</c> branch.
    /// </summary>
    public int Restart()
    {
        Console.WriteLine("[*] Restarting...");
        CheckTermsrvProcess();

        Console.WriteLine("[*] Terminating service...");
        SecurityHelper.AddPrivilege(NativeMethods.SE_DEBUG_NAME);
        ProcessHelper.KillProcess(_termServicePid);
        Thread.Sleep(1000);

        RestartSharedServices();
        Thread.Sleep(500);
        ServiceHelper.StartService(TermService);

        Console.WriteLine("[+] Done.");
        return 0;
    }

    // ── CheckInstall ──────────────────────────────────────────────────────────

    /// <summary>
    /// Validates the TermService registry image path and sets
    /// <see cref="_installed"/> and <see cref="_termServicePath"/>.
    /// Mirrors the Delphi CheckInstall procedure.
    /// </summary>
    public void CheckInstall()
    {
        const string svcKey    = @"SYSTEM\CurrentControlSet\Services\TermService";
        const string paramsKey = @"SYSTEM\CurrentControlSet\Services\TermService\Parameters";

        var imagePath = RegistryHelper.ReadString(svcKey, "ImagePath") ?? string.Empty;
        if (!imagePath.Contains("svchost.exe", StringComparison.OrdinalIgnoreCase) &&
            !imagePath.Contains("svchost -k",  StringComparison.OrdinalIgnoreCase))
        {
            Console.Error.WriteLine("[-] TermService is hosted in a custom application (BeTwin, etc.) - unsupported.");
            Console.Error.WriteLine($"[*] ImagePath: \"{imagePath}\".");
            Environment.Exit(unchecked((int)NativeMethods.ERROR_NOT_SUPPORTED));
        }

        var serviceDll = RegistryHelper.ReadString(paramsKey, "ServiceDll") ?? string.Empty;
        if (!serviceDll.Contains("termsrv.dll", StringComparison.OrdinalIgnoreCase) &&
            !serviceDll.Contains("rdpwrap.dll",  StringComparison.OrdinalIgnoreCase))
        {
            Console.Error.WriteLine("[-] Another third-party TermService library is installed.");
            Console.Error.WriteLine($"[*] ServiceDll: \"{serviceDll}\".");
            Environment.Exit(unchecked((int)NativeMethods.ERROR_NOT_SUPPORTED));
        }

        _termServicePath = serviceDll;
        _installed       = serviceDll.Contains("rdpwrap.dll", StringComparison.OrdinalIgnoreCase);
    }

    // ── CheckTermsrvProcess ────────────────────────────────────────────────────

    /// <summary>
    /// Finds the TermService process ID, auto-starts the service if needed,
    /// and collects co-hosted service names. Mirrors CheckTermsrvProcess.
    /// </summary>
    private void CheckTermsrvProcess()
    {
        bool started = false;
    retry:
        var services = ServiceHelper.EnumServiceProcesses();
        var ts = services.FirstOrDefault(s =>
            s.ServiceName.Equals(TermService, StringComparison.OrdinalIgnoreCase));

        if (ts is null)
        {
            Console.Error.WriteLine($"[-] {TermService} not found.");
            Environment.Exit(unchecked((int)NativeMethods.ERROR_SERVICE_DOES_NOT_EXIST));
            return;
        }

        if (ts.ProcessId == 0)
        {
            if (started)
            {
                Console.Error.WriteLine("[-] Failed to set up TermService. Unknown error.");
                Environment.Exit(unchecked((int)NativeMethods.ERROR_SERVICE_NOT_ACTIVE));
                return;
            }
            ServiceHelper.SetStartType(TermService, NativeMethods.SERVICE_AUTO_START);
            ServiceHelper.StartService(TermService);
            started = true;
            goto retry;
        }

        _termServicePid = ts.ProcessId;
        Console.WriteLine($"[+] TermService found (pid {_termServicePid}).");

        _shareServices = services
            .Where(s => s.ProcessId == _termServicePid &&
                        !s.ServiceName.Equals(TermService, StringComparison.OrdinalIgnoreCase))
            .Select(s => s.ServiceName)
            .ToArray();

        if (_shareServices.Length > 0)
            Console.WriteLine($"[*] Shared services found: {string.Join(", ", _shareServices)}");
        else
            Console.WriteLine("[*] No shared services found.");
    }

    // ── CheckTermsrvDependencies ───────────────────────────────────────────────

    /// <summary>
    /// Ensures CertPropSvc and SessionEnv are not disabled.
    /// Mirrors the Delphi CheckTermsrvDependencies procedure.
    /// </summary>
    private static void CheckTermsrvDependencies()
    {
        foreach (var svc in new[] { "CertPropSvc", "SessionEnv" })
        {
            if (ServiceHelper.GetStartType(svc) == (int)NativeMethods.SERVICE_DISABLED)
                ServiceHelper.SetStartType(svc, NativeMethods.SERVICE_DEMAND_START);
        }
    }

    // ── CheckTermsrvVersion ────────────────────────────────────────────────────

    /// <summary>
    /// Reads the termsrv.dll version and classifies support level.
    /// Mirrors the Delphi CheckTermsrvVersion procedure.
    /// </summary>
    private void CheckTermsrvVersion()
    {
        var fv = FileVersionHelper.GetVersionExpanded(_termServicePath);
        if (fv is null)
        {
            Console.Error.WriteLine("[-] Could not read termsrv.dll version.");
            return;
        }

        _termSrvVerTxt = fv.ToString();
        Console.WriteLine($"[*] Terminal Services version: {_termSrvVerTxt}");

        // Unsupported legacy versions
        if (fv.Major == 5)
        {
            var label = (ArchHelper.Arch == 32) ? "x86" : "x64";
            Console.WriteLine($"[!] Windows XP / Server 2003 ({label}) is not supported.");
            return;
        }

        // Load the built-in INI to check support level
        var builtInIni = ResourceHelper.ReadText(
            "RDPWInst.Resources.rdpwrap.ini",
            Assembly.GetExecutingAssembly()) ?? string.Empty;

        int level = IniHelper.CheckSupportLevel(builtInIni, fv);

        switch (level)
        {
            case 0:
                Console.WriteLine("[-] This version of Terminal Services is not supported.");
                Console.WriteLine("Try running \"update.bat\" or \"RDPWInst -w\" to download latest INI file.");
                break;
            case 1:
                Console.WriteLine("[!] This version of Terminal Services is supported partially.");
                Console.WriteLine("It means you may have some limitations such as only 2 concurrent sessions.");
                Console.WriteLine("Try running \"update.bat\" or \"RDPWInst -w\" to download latest INI file.");
                break;
            case 2:
                Console.WriteLine("[+] This version of Terminal Services is fully supported.");
                break;
        }
    }

    // ── TSConfigRegistry ──────────────────────────────────────────────────────

    /// <summary>
    /// Writes (or clears) the TS-enable registry values.
    /// Mirrors the Delphi TSConfigRegistry procedure.
    /// </summary>
    private static void TSConfigRegistry(bool enable)
    {
        const string tsKey      = @"SYSTEM\CurrentControlSet\Control\Terminal Server";
        const string licKey     = @"SYSTEM\CurrentControlSet\Control\Terminal Server\Licensing Core";
        const string winlogon   = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon";
        const string addInsBase = @"SYSTEM\CurrentControlSet\Control\Terminal Server\AddIns";

        RegistryHelper.WriteBool(tsKey, "fDenyTSConnections", !enable);

        if (enable)
        {
            RegistryHelper.WriteBool(licKey,   "EnableConcurrentSessions", true);
            RegistryHelper.WriteBool(winlogon, "AllowMultipleTSSessions",  true);

            // AddIns sub-keys (only create if the parent key is absent)
            if (Microsoft.Win32.Registry.LocalMachine.OpenSubKey(addInsBase) is null)
            {
                RegistryHelper.WriteString(addInsBase + @"\Clip Redirector", "Name", "RDPClip");
                RegistryHelper.WriteInt   (addInsBase + @"\Clip Redirector", "Type", 3);
                RegistryHelper.WriteString(addInsBase + @"\DND Redirector",  "Name", "RDPDND");
                RegistryHelper.WriteInt   (addInsBase + @"\DND Redirector",  "Type", 3);
                RegistryHelper.WriteInt   (addInsBase + @"\Dynamic VC",      "Type", -1);
            }
        }
    }

    // ── TSConfigFirewall ──────────────────────────────────────────────────────

    private static void TSConfigFirewall(bool enable)
    {
        if (enable)
        {
            ProcessHelper.ExecWait(
                "netsh advfirewall firewall add rule name=\"Remote Desktop\" " +
                "dir=in protocol=tcp localport=3389 profile=any action=allow");
            ProcessHelper.ExecWait(
                "netsh advfirewall firewall add rule name=\"Remote Desktop\" " +
                "dir=in protocol=udp localport=3389 profile=any action=allow");
        }
        else
        {
            ProcessHelper.ExecWait(
                "netsh advfirewall firewall delete rule name=\"Remote Desktop\"");
        }
    }

    // ── ExtractFiles ──────────────────────────────────────────────────────────

    /// <summary>
    /// Creates the install directory, sets ACLs, downloads or extracts the
    /// INI file, and extracts the correct rdpwrap DLL + optional helpers.
    /// Mirrors the Delphi ExtractFiles procedure.
    /// </summary>
    private void ExtractFiles()
    {
        var asm      = Assembly.GetExecutingAssembly();
        var fullPath = ArchHelper.ExpandPath(_wrapPath);
        var dir      = Path.GetDirectoryName(fullPath)!;

        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
            Console.WriteLine($"[+] Folder created: {dir}");
            SecurityHelper.GrantSidFullAccess(dir, "S-1-5-18"); // Local System
            SecurityHelper.GrantSidFullAccess(dir, "S-1-5-6");  // Service group
        }

        // ── INI file ──
        var iniPath = Path.Combine(dir, "rdpwrap.ini");
        if (_online)
        {
            Console.WriteLine("[*] Downloading latest INI file...");
            var content = HttpHelper.DownloadString(ReleaseBaseUrl + "rdpwrap.ini");
            if (content is not null)
            {
                File.WriteAllText(iniPath, content, System.Text.Encoding.UTF8);
                Console.WriteLine($"[+] Latest INI file -> {iniPath}");
            }
            else
            {
                Console.WriteLine("[-] Failed to get online INI file, using built-in.");
                _online = false;
            }
        }

        if (!_online)
        {
            // Try a local rdpwrap.ini beside the installer first
            var localIni = Path.Combine(
                Path.GetDirectoryName(Environment.ProcessPath ?? string.Empty) ?? ".",
                "rdpwrap.ini");

            if (File.Exists(localIni))
            {
                File.Copy(localIni, iniPath, overwrite: true);
                Console.WriteLine($"[+] Current INI file -> {iniPath}");
            }
            else
            {
                ResourceHelper.ExtractToDisk("RDPWInst.Resources.rdpwrap.ini", iniPath, asm);
            }
        }

        // ── Core DLL ──
        var dllRes = ArchHelper.Is64Bit ? "RDPWInst.Resources.rdpw64.dll"
                                        : "RDPWInst.Resources.rdpw32.dll";
        ResourceHelper.ExtractToDisk(dllRes, fullPath, asm);

        // ── Optional helpers (Vista / Win7 clipboard redirect, Win10 RFX codec) ──
        ExtractOptionalHelper(asm, dir);
    }

    private void ExtractOptionalHelper(Assembly asm, string dir)
    {
        var fv = FileVersionHelper.GetVersionExpanded(_termServicePath);
        if (fv is null) return;

        var arch = ArchHelper.Is64Bit ? "64" : "32";

        // rdpclip: Vista 6.0 and Win7 6.1
        string? clipRes = (fv.Major, fv.Minor) switch
        {
            (6, 0) => $"RDPWInst.Resources.rdpclip60{arch}.exe",
            (6, 1) => $"RDPWInst.Resources.rdpclip61{arch}.exe",
            _      => null
        };
        if (clipRes is not null)
        {
            var dest = ArchHelper.ExpandPath(@"%SystemRoot%\System32\rdpclip.exe");
            if (!File.Exists(dest))
                ResourceHelper.ExtractToDisk(clipRes, dest, asm);
        }

        // rfxvmt.dll: Windows 10 (6.10.x maps to 10.0 in NT versioning)
        if (fv.Major == 10 && fv.Minor == 0)
        {
            var rfxRes  = $"RDPWInst.Resources.rfxvmt{arch}.dll";
            var rfxDest = ArchHelper.ExpandPath(@"%SystemRoot%\System32\rfxvmt.dll");
            if (!File.Exists(rfxDest))
                ResourceHelper.ExtractToDisk(rfxRes, rfxDest, asm);
        }
    }

    // ── SetWrapperDll / ResetServiceDll ──────────────────────────────────────

    private void SetWrapperDll()
    {
        const string key = @"SYSTEM\CurrentControlSet\Services\TermService\Parameters";
        RegistryHelper.WriteExpandString(key, "ServiceDll", _wrapPath);

        // Vista 6.0 workaround — reg.exe write to bypass WOW64 quirk
        var fv = FileVersionHelper.GetVersionExpanded(_termServicePath);
        if (fv is { Major: 6, Minor: 0 } && ArchHelper.Is64Bit)
        {
            ProcessHelper.ExecWait(
                $"\"{ArchHelper.ExpandPath("%SystemRoot%")}\\system32\\reg.exe\" " +
                $"add HKLM\\SYSTEM\\CurrentControlSet\\Services\\TermService\\Parameters " +
                $"/v ServiceDll /t REG_EXPAND_SZ /d \"{_wrapPath}\" /f");
        }
    }

    private static void ResetServiceDll()
    {
        const string key = @"SYSTEM\CurrentControlSet\Services\TermService\Parameters";
        RegistryHelper.WriteExpandString(key, "ServiceDll",
            @"%SystemRoot%\System32\termsrv.dll");
    }

    // ── DeleteFiles ───────────────────────────────────────────────────────────

    private void DeleteFiles()
    {
        var fullPath = ArchHelper.ExpandPath(_termServicePath);
        var dir      = Path.GetDirectoryName(fullPath)!;
        var iniPath  = Path.Combine(dir, "rdpwrap.ini");

        TryDelete(iniPath);
        TryDelete(fullPath);
        TryRemoveDir(dir);
    }

    private static void TryDelete(string path)
    {
        try
        {
            File.Delete(path);
            Console.WriteLine($"[+] Removed file: {path}");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[-] DeleteFile error: {ex.Message}");
        }
    }

    private static void TryRemoveDir(string dir)
    {
        try
        {
            Directory.Delete(dir);
            Console.WriteLine($"[+] Removed folder: {dir}");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[-] RemoveDirectory error: {ex.Message}");
        }
    }

    // ── TryAutoGenerateOffsets ────────────────────────────────────────────────

    /// <summary>
    /// If the running termsrv.dll version is absent from rdpwrap.ini, downloads
    /// RDPWrapOffsetFinder + Zydis from the release assets and runs the finder
    /// to append generated offsets. Mirrors the Delphi TryAutoGenerateOffsets.
    /// </summary>
    private void TryAutoGenerateOffsets()
    {
        if (string.IsNullOrEmpty(_termSrvVerTxt)) return;

        var fullPath = ArchHelper.ExpandPath(_wrapPath);
        var iniPath  = Path.Combine(Path.GetDirectoryName(fullPath)!, "rdpwrap.ini");

        if (IniHelper.HasSection(iniPath, _termSrvVerTxt))
        {
            Console.WriteLine($"[+] Version {_termSrvVerTxt} is covered in INI.");
            return;
        }

        Console.WriteLine($"[!] Version {_termSrvVerTxt} not found in INI.");
        Console.WriteLine("[*] Attempting automatic offset generation via RDPWrapOffsetFinder...");

        var archSuffix = ArchHelper.Is64Bit ? "_x64" : "_x86";
        var tempDir    = Path.Combine(Path.GetTempPath(), "rdpwrapoffset");

        try { Directory.CreateDirectory(tempDir); }
        catch
        {
            Console.Error.WriteLine("[-] Could not create temp directory. Skipping auto-generation.");
            return;
        }

        var exePath  = Path.Combine(tempDir, "RDPWrapOffsetFinder.exe");
        var dllPath  = Path.Combine(tempDir, "Zydis.dll");

        Console.WriteLine($"[*] Downloading RDPWrapOffsetFinder{archSuffix}.exe ...");
        if (!HttpHelper.DownloadFile(ReleaseBaseUrl + $"RDPWrapOffsetFinder{archSuffix}.exe", exePath))
        {
            Console.Error.WriteLine("[-] Download failed. The release asset may not yet be published.");
            Console.Error.WriteLine("[!] Run the publish-ini workflow on the sjackson0109/rdpwrap repository,");
            Console.Error.WriteLine("[!] then re-run this installer to enable auto-generation.");
            return;
        }

        Console.WriteLine($"[*] Downloading Zydis{archSuffix}.dll ...");
        if (!HttpHelper.DownloadFile(ReleaseBaseUrl + $"Zydis{archSuffix}.dll", dllPath))
        {
            Console.Error.WriteLine("[-] Zydis download failed. Skipping auto-generation.");
            File.Delete(exePath);
            return;
        }

        Console.WriteLine($"[*] Running offset finder for termsrv.dll {_termSrvVerTxt} ...");
        // Run via cmd.exe so that >> redirect to the INI file functions correctly
        var sysCmd = ArchHelper.ExpandPath(@"%SystemRoot%\System32\cmd.exe");
        ProcessHelper.ExecWait($"\"{sysCmd}\" /c \"\"{exePath}\" >> \"{iniPath}\"\"");

        if (IniHelper.HasSection(iniPath, _termSrvVerTxt))
            Console.WriteLine($"[+] Offsets generated successfully for version {_termSrvVerTxt}");
        else
            Console.WriteLine($"[!] Offset finder ran but [{_termSrvVerTxt}] was not added. " +
                              "Session may be limited or unstable for this build.");

        // Clean up temporary tool files
        try
        {
            File.Delete(exePath);
            File.Delete(dllPath);
            Directory.Delete(tempDir);
        }
        catch { /* best-effort */ }
    }

    // ── CheckUpdate (GitINIFile path) ─────────────────────────────────────────

    private int CheckUpdate()
    {
        var fullPath = ArchHelper.ExpandPath(_termServicePath);
        var iniPath  = Path.Combine(Path.GetDirectoryName(fullPath)!, "rdpwrap.ini");

        if (!TryGetIniDate(iniPath, null, out int oldDate))
            return unchecked((int)NativeMethods.ERROR_ACCESS_DENIED);

        Console.WriteLine($"[*] Current update date: {FormatDate(oldDate)}");

        var latest = HttpHelper.DownloadString(ReleaseBaseUrl + "rdpwrap.ini");
        if (latest is null)
        {
            Console.Error.WriteLine("[-] Failed to download latest INI from GitHub.");
            return unchecked((int)NativeMethods.ERROR_ACCESS_DENIED);
        }

        if (!TryGetIniDate(null, latest, out int newDate))
            return unchecked((int)NativeMethods.ERROR_ACCESS_DENIED);

        Console.WriteLine($"[*] Latest update date:  {FormatDate(newDate)}");

        if (newDate == oldDate)
        {
            Console.WriteLine("[*] Everything is up to date.");
            return 0;
        }

        if (newDate > oldDate)
        {
            Console.WriteLine("[+] New update is available, updating...");
            CheckTermsrvProcess();

            Console.WriteLine("[*] Terminating service...");
            SecurityHelper.AddPrivilege(NativeMethods.SE_DEBUG_NAME);
            ProcessHelper.KillProcess(_termServicePid);
            Thread.Sleep(1000);

            RestartSharedServices();
            Thread.Sleep(500);

            File.WriteAllText(iniPath, latest, System.Text.Encoding.UTF8);
            Console.WriteLine($"[+] INI file updated: {iniPath}");

            // Recompute version for offset generation
            var fv = FileVersionHelper.GetVersionExpanded(_termServicePath);
            if (fv is not null) _termSrvVerTxt = fv.ToString();

            Console.WriteLine("[*] Checking INI coverage for installed termsrv.dll version...");
            TryAutoGenerateOffsets();

            ServiceHelper.StartService(TermService);
            Console.WriteLine("[+] Update completed.");
        }
        else
        {
            Console.WriteLine("[*] Your INI file is newer than public file. Are you a developer? :)");
        }

        return 0;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void RestartSharedServices()
    {
        foreach (var svc in _shareServices)
            ServiceHelper.StartService(svc);
    }

    /// <summary>
    /// Parses the <c>Updated=YYYYMMDD</c> line from an INI file or string.
    /// Mirrors the Delphi CheckINIDate function.
    /// </summary>
    private static bool TryGetIniDate(string? filePath, string? content, out int date)
    {
        date = 0;
        IEnumerable<string> lines;

        if (filePath is not null)
        {
            if (!File.Exists(filePath))
            {
                Console.Error.WriteLine("[-] Failed to read INI file.");
                return false;
            }
            lines = File.ReadLines(filePath);
        }
        else
        {
            lines = (content ?? string.Empty).Split('\n');
        }

        foreach (var line in lines)
        {
            var trimmed = line.TrimEnd('\r');
            if (!trimmed.StartsWith("Updated=", StringComparison.Ordinal)) continue;

            var raw = trimmed["Updated=".Length..].Replace("-", "");
            if (int.TryParse(raw, out date)) return true;

            Console.Error.WriteLine("[-] Wrong INI date format.");
            return false;
        }

        Console.Error.WriteLine("[-] Failed to check INI date (Updated= line not found).");
        return false;
    }

    private static string FormatDate(int d)
    {
        int y = d / 10000, m = (d / 100) % 100, day = d % 100;
        return $"{y}.{m:D2}.{day:D2}";
    }
}
