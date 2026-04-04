// Copyright 2026 sjackson0109 — Apache License 2.0
//
// All logic, registry keys, and label text match the Delphi original.

using System.Runtime.InteropServices;
using RDPWrap.Common;

namespace RDPConf;

internal sealed class MainForm : Form
{
    // ── Controls ──────────────────────────────────────────────────────────────
    private readonly CheckBox     cbAllowTSConnections   = new();
    private readonly CheckBox     cbSingleSessionPerUser = new();
    private readonly CheckBox     cbHideUsers            = new();
    private readonly CheckBox     cbCustomPrg            = new();
    private readonly NumericUpDown seRDPPort             = new();
    private readonly Label        lRDPPort               = new();
    private readonly GroupBox     gbGeneral              = new();

    // NLA radio group
    private readonly GroupBox    gbNLA  = new();
    private readonly RadioButton rbNLA0 = new();
    private readonly RadioButton rbNLA1 = new();
    private readonly RadioButton rbNLA2 = new();

    // Shadow radio group
    private readonly GroupBox    gbShadow  = new();
    private readonly RadioButton rbShadow0 = new();
    private readonly RadioButton rbShadow1 = new();
    private readonly RadioButton rbShadow2 = new();
    private readonly RadioButton rbShadow3 = new();
    private readonly RadioButton rbShadow4 = new();

    // Diagnostics
    private readonly GroupBox gbDiag        = new();
    private readonly Label    lService      = new();
    private readonly Label    lsService     = new();
    private readonly Label    lListener     = new();
    private readonly Label    lsListener    = new();
    private readonly Label    lWrapper      = new();
    private readonly Label    lsWrapper     = new();
    private readonly Label    lTSVer        = new();
    private readonly Label    lsTSVer       = new();
    private readonly Label    lWrapVer      = new();
    private readonly Label    lsWrapVer     = new();
    private readonly Label    lsSuppVer     = new();

    // Buttons / Timer
    private readonly Button bOK      = new();
    private readonly Button bCancel  = new();
    private readonly Button bApply   = new();
    private readonly Button bLicense = new();
    private readonly System.Windows.Forms.Timer timer = new();

    // State (mirrors Delphi globals)
    private bool _ready;
    private int  _oldPort;

    // ── Constructor ───────────────────────────────────────────────────────────

    public MainForm()
    {
        SuspendLayout();
        BuildLayout();
        ResumeLayout(false);
        PerformLayout();

        // Wire events
        cbAllowTSConnections.CheckedChanged   += OnAnyChange;
        cbSingleSessionPerUser.CheckedChanged += OnAnyChange;
        cbHideUsers.CheckedChanged            += OnAnyChange;
        cbCustomPrg.CheckedChanged            += OnAnyChange;
        seRDPPort.ValueChanged                += OnAnyChange;
        rbNLA0.CheckedChanged += OnAnyChange;
        rbNLA1.CheckedChanged += OnAnyChange;
        rbNLA2.CheckedChanged += OnAnyChange;
        rbShadow0.CheckedChanged += OnAnyChange;
        rbShadow1.CheckedChanged += OnAnyChange;
        rbShadow2.CheckedChanged += OnAnyChange;
        rbShadow3.CheckedChanged += OnAnyChange;
        rbShadow4.CheckedChanged += OnAnyChange;
        bApply.Click   += (_, _) => { WriteSettings(); bApply.Enabled = false; };
        bOK.Click      += (_, _) => { if (bApply.Enabled) { WriteSettings(); bApply.Enabled = false; } Close(); };
        bCancel.Click  += (_, _) => Close();
        bLicense.Click += OnLicenseClick;
        timer.Interval  = 1000;
        timer.Tick     += TimerTimer;
        FormClosing    += OnFormClosing;
        Shown          += OnShown;
        FormClosed     += (_, _) => { if (ArchHelper.Is64Bit) ArchHelper.RevertWow64Redirection(); timer.Stop(); };

        if (ArchHelper.Is64Bit) ArchHelper.DisableWow64Redirection();
        ReadSettings();
        _ready = true;
    }

    private void OnShown(object? sender, EventArgs e)
    {
        TimerTimer(sender, e);   // immediate first tick
        timer.Start();
    }

    // ── Layout ────────────────────────────────────────────────────────────────

    private void BuildLayout()
    {
        Text            = "RDP Wrapper Configuration";
        ClientSize      = new Size(540, 383);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox     = false;
        StartPosition   = FormStartPosition.CenterScreen;
        Font            = new Font("Segoe UI", 9f);

        // ── General GroupBox ──────────────────────────────────────────────────
        gbGeneral.Text     = "General";
        gbGeneral.Location = new Point(8, 8);
        gbGeneral.Size     = new Size(260, 175);

        cbAllowTSConnections.Text     = "Allow Remote Desktop connections";
        cbAllowTSConnections.Location = new Point(10, 22);
        cbAllowTSConnections.AutoSize = true;

        cbSingleSessionPerUser.Text     = "Single session per user";
        cbSingleSessionPerUser.Location = new Point(10, 46);
        cbSingleSessionPerUser.AutoSize = true;

        cbHideUsers.Text     = "Hide users on logon screen";
        cbHideUsers.Location = new Point(10, 70);
        cbHideUsers.AutoSize = true;

        cbCustomPrg.Text     = "Custom program support (HonorLegacySettings)";
        cbCustomPrg.Location = new Point(10, 94);
        cbCustomPrg.AutoSize = true;

        lRDPPort.Text     = "RDP Port:";
        lRDPPort.Location = new Point(10, 124);
        lRDPPort.AutoSize = true;

        seRDPPort.Location = new Point(80, 121);
        seRDPPort.Minimum  = 1;
        seRDPPort.Maximum  = 65535;
        seRDPPort.Value    = 3389;
        seRDPPort.Width    = 70;

        gbGeneral.Controls.AddRange(new Control[]
        {
            cbAllowTSConnections, cbSingleSessionPerUser, cbHideUsers,
            cbCustomPrg, lRDPPort, seRDPPort
        });

        // ── NLA GroupBox ──────────────────────────────────────────────────────
        gbNLA.Text     = "Network Level Authentication";
        gbNLA.Location = new Point(276, 8);
        gbNLA.Size     = new Size(254, 88);

        rbNLA0.Text     = "No NLA";
        rbNLA0.Location = new Point(10, 20);
        rbNLA0.AutoSize = true;

        rbNLA1.Text     = "Negotiate (server-side NLA)";
        rbNLA1.Location = new Point(10, 42);
        rbNLA1.AutoSize = true;

        rbNLA2.Text     = "Require NLA (client + server)";
        rbNLA2.Location = new Point(10, 64);
        rbNLA2.AutoSize = true;

        gbNLA.Controls.AddRange(new Control[] { rbNLA0, rbNLA1, rbNLA2 });

        // ── Shadow GroupBox ───────────────────────────────────────────────────
        gbShadow.Text     = "Shadowing";
        gbShadow.Location = new Point(276, 100);
        gbShadow.Size     = new Size(254, 135);

        rbShadow0.Text     = "Disabled";
        rbShadow0.Location = new Point(10, 20); rbShadow0.AutoSize = true;
        rbShadow1.Text     = "Full Access (with confirmation)";
        rbShadow1.Location = new Point(10, 42); rbShadow1.AutoSize = true;
        rbShadow2.Text     = "Full Access (no confirmation)";
        rbShadow2.Location = new Point(10, 62); rbShadow2.AutoSize = true;
        rbShadow3.Text     = "View Only (with confirmation)";
        rbShadow3.Location = new Point(10, 82); rbShadow3.AutoSize = true;
        rbShadow4.Text     = "View Only (no confirmation)";
        rbShadow4.Location = new Point(10, 102); rbShadow4.AutoSize = true;

        gbShadow.Controls.AddRange(new Control[]
            { rbShadow0, rbShadow1, rbShadow2, rbShadow3, rbShadow4 });

        // ── Diagnostics GroupBox ──────────────────────────────────────────────
        gbDiag.Text     = "Diagnostics";
        gbDiag.Location = new Point(8, 190);
        gbDiag.Size     = new Size(522, 140);

        AddDiagRow(gbDiag, "Service:",          lService,  lsService,  new Point(10, 22));
        AddDiagRow(gbDiag, "Listener:",         lListener, lsListener, new Point(10, 44));
        AddDiagRow(gbDiag, "Wrapper:",          lWrapper,  lsWrapper,  new Point(10, 66));
        AddDiagRow(gbDiag, "TS version:",       lTSVer,    lsTSVer,    new Point(10, 88));
        AddDiagRow(gbDiag, "Wrapper version:",  lWrapVer,  lsWrapVer,  new Point(10, 110));

        lsSuppVer.Location = new Point(280, 88);
        lsSuppVer.AutoSize = true;
        lsSuppVer.Visible  = false;
        gbDiag.Controls.Add(lsSuppVer);

        // ── Buttons ───────────────────────────────────────────────────────────
        bLicense.Text     = "License";
        bLicense.Size     = new Size(80, 26);
        bLicense.Location = new Point(8, 345);

        bApply.Text     = "Apply";
        bApply.Enabled  = false;
        bApply.Size     = new Size(80, 26);
        bApply.Location = new Point(276, 345);

        bOK.Text     = "OK";
        bOK.Size     = new Size(80, 26);
        bOK.Location = new Point(364, 345);

        bCancel.Text     = "Cancel";
        bCancel.Size     = new Size(80, 26);
        bCancel.Location = new Point(452, 345);

        Controls.AddRange(new Control[]
        {
            gbGeneral, gbNLA, gbShadow, gbDiag,
            bLicense, bApply, bOK, bCancel
        });
    }

    private static void AddDiagRow(GroupBox parent, string labelText,
        Label lbl, Label status, Point origin)
    {
        lbl.Text      = labelText;
        lbl.Location  = new Point(origin.X, origin.Y);
        lbl.AutoSize  = true;

        status.Text      = "...";
        status.Location  = new Point(origin.X + 120, origin.Y);
        status.AutoSize  = true;
        status.ForeColor = SystemColors.GrayText;

        parent.Controls.Add(lbl);
        parent.Controls.Add(status);
    }

    private void OnAnyChange(object? sender, EventArgs e)
    {
        if (_ready) bApply.Enabled = true;
    }

    // ── ReadSettings (mirrors Delphi ReadSettings) ────────────────────────────

    private void ReadSettings()
    {
        const string tsKey    = @"SYSTEM\CurrentControlSet\Control\Terminal Server";
        const string rdpTcp   = @"SYSTEM\CurrentControlSet\Control\Terminal Server\WinStations\RDP-Tcp";
        const string policies = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System";

        cbAllowTSConnections.Checked   = !RegistryHelper.ReadBool(tsKey, "fDenyTSConnections", false);
        cbSingleSessionPerUser.Checked =  RegistryHelper.ReadBool(tsKey, "fSingleSessionPerUser", false);
        cbCustomPrg.Checked            =  RegistryHelper.ReadBool(tsKey, "HonorLegacySettings", false);

        int port = RegistryHelper.ReadInt(rdpTcp, "PortNumber", 3389);
        seRDPPort.Value = Math.Clamp(port, 1, 65535);
        _oldPort        = port;

        int secLayer  = RegistryHelper.ReadInt(rdpTcp, "SecurityLayer",     0);
        int userAuth  = RegistryHelper.ReadInt(rdpTcp, "UserAuthentication", 0);
        int shadow    = RegistryHelper.ReadInt(rdpTcp, "Shadow", -1);

        _ = (secLayer, userAuth) switch
        {
            (0, 0) => rbNLA0.Checked = true,
            (1, 0) => rbNLA1.Checked = true,
            (2, 1) => rbNLA2.Checked = true,
            _      => rbNLA0.Checked = true
        };

        SetShadowRadio(shadow);

        cbHideUsers.Checked = RegistryHelper.ReadBool(policies, "dontdisplaylastusername", false);
    }

    private void SetShadowRadio(int idx)
    {
        RadioButton[] rbs = { rbShadow0, rbShadow1, rbShadow2, rbShadow3, rbShadow4 };
        if (idx >= 0 && idx < rbs.Length)
            rbs[idx].Checked = true;
    }

    private int GetShadowIndex()
    {
        RadioButton[] rbs = { rbShadow0, rbShadow1, rbShadow2, rbShadow3, rbShadow4 };
        for (int i = 0; i < rbs.Length; i++)
            if (rbs[i].Checked) return i;
        return -1;
    }

    // ── WriteSettings (mirrors Delphi WriteSettings) ──────────────────────────

    private void WriteSettings()
    {
        const string tsKey    = @"SYSTEM\CurrentControlSet\Control\Terminal Server";
        const string rdpTcp   = @"SYSTEM\CurrentControlSet\Control\Terminal Server\WinStations\RDP-Tcp";
        const string policiesRdp = @"SOFTWARE\Policies\Microsoft\Windows NT\Terminal Services";
        const string policies    = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System";

        RegistryHelper.WriteBool(tsKey, "fDenyTSConnections",  !cbAllowTSConnections.Checked);
        RegistryHelper.WriteBool(tsKey, "fSingleSessionPerUser", cbSingleSessionPerUser.Checked);
        RegistryHelper.WriteBool(tsKey, "HonorLegacySettings",  cbCustomPrg.Checked);

        int newPort = (int)seRDPPort.Value;
        RegistryHelper.WriteInt(rdpTcp, "PortNumber", newPort);
        if (_oldPort != newPort)
        {
            _oldPort = newPort;
            ProcessHelper.ExecWait(
                $"netsh advfirewall firewall set rule name=\"Remote Desktop\" new localport={newPort}");
        }

        // NLA
        (int sl, int ua) = (rbNLA0.Checked, rbNLA1.Checked, rbNLA2.Checked) switch
        {
            (true,  _,    _   ) => (0, 0),
            (_,     true, _   ) => (1, 0),
            (_,     _,    true) => (2, 1),
            _                   => (-1, -1)
        };
        if (sl >= 0)
        {
            RegistryHelper.WriteInt(rdpTcp, "SecurityLayer",     sl);
            RegistryHelper.WriteInt(rdpTcp, "UserAuthentication", ua);
        }

        // Shadow
        int shadowIdx = GetShadowIndex();
        if (shadowIdx >= 0)
        {
            RegistryHelper.WriteInt(rdpTcp,      "Shadow", shadowIdx);
            RegistryHelper.WriteInt(policiesRdp, "Shadow", shadowIdx);
        }

        RegistryHelper.WriteBool(policies, "dontdisplaylastusername", cbHideUsers.Checked);
    }

    // ── TimerTimer (mirrors Delphi TimerTimer) ────────────────────────────────

    private void TimerTimer(object? sender, EventArgs e)
    {
        // ── Wrapper state ──
        string wrapperPath = string.Empty;
        int wrapState = IsWrapperInstalled(ref wrapperPath);
        bool checkSupp = false;
        string iniPath  = string.Empty;

        switch (wrapState)
        {
            case -1:
                SetStatus(lsWrapper, "Unknown",        SystemColors.GrayText);
                break;
            case 0:
                SetStatus(lsWrapper, "Not installed",  SystemColors.GrayText);
                break;
            case 1:
                SetStatus(lsWrapper, "Installed",      Color.Green);
                iniPath   = Path.Combine(
                    Path.GetDirectoryName(ArchHelper.ExpandPath(wrapperPath))!,
                    "rdpwrap.ini");
                checkSupp = File.Exists(iniPath);
                break;
            case 2:
                SetStatus(lsWrapper, "3rd-party",      Color.Red);
                break;
        }

        // ── Service state ──
        int svcState = GetTermSrvState();
        // dwCurrentState constants (mirrors NativeMethods SERVICE_* constants)
        string svcText  = svcState switch
        {
            1 => "Stopped",
            2 => "Starting...",
            3 => "Stopping...",
            4 => "Running",
            5 => "Resuming...",
            6 => "Suspending...",
            7 => "Suspended",
            _ => "Unknown"
        };
        Color svcColor = svcState == 4 ? Color.Green   // SERVICE_RUNNING
                       : svcState == 1 ? Color.Red     // SERVICE_STOPPED
                       : SystemColors.GrayText;
        SetStatus(lsService, svcText, svcColor);

        // ── Listener ──
        bool listening = IsListenerWorking();
        SetStatus(lsListener,
            listening ? "Listening" : "Not listening",
            listening ? Color.Green : Color.Red);

        // ── Wrapper version ──
        string wrapExp = ArchHelper.ExpandPath(wrapperPath);
        var wrapVer = string.IsNullOrEmpty(wrapperPath) ? null
                    : FileVersionHelper.GetVersion(wrapExp);
        if (wrapVer is null)
            SetStatus(lsWrapVer, "N/A", SystemColors.GrayText);
        else
            SetStatus(lsWrapVer, wrapVer.ToString(), SystemColors.WindowText);

        // ── TS version ──
        var tsVer = FileVersionHelper.GetVersionExpanded(@"%SystemRoot%\System32\termsrv.dll");
        if (tsVer is null)
        {
            SetStatus(lsTSVer, "N/A", SystemColors.GrayText);
            lsSuppVer.Visible = false;
            return;
        }

        SetStatus(lsTSVer, tsVer.ToString(), SystemColors.WindowText);
        lsSuppVer.Visible = checkSupp;

        if (checkSupp)
        {
            string iniContent = IniHelper.LoadText(iniPath);
            int level = IniHelper.CheckSupportLevel(iniContent, tsVer);
            (string suppText, Color suppColor) = level switch
            {
                0 => ("[not supported]",        Color.Red),
                1 => ("[supported partially]",  Color.Olive),
                _ => ("[fully supported]",      Color.Green)
            };
            lsSuppVer.Text      = suppText;
            lsSuppVer.ForeColor = suppColor;
        }
    }

    // ── Helper: IsWrapperInstalled ────────────────────────────────────────────

    /// <returns>-1=error, 0=not installed, 1=installed, 2=3rd-party</returns>
    private static int IsWrapperInstalled(ref string wrapperPath)
    {
        wrapperPath = string.Empty;
        var host = RegistryHelper.ReadString(
            @"SYSTEM\CurrentControlSet\Services\TermService", "ImagePath") ?? string.Empty;

        if (!host.Contains("svchost.exe", StringComparison.OrdinalIgnoreCase)) return 2;

        var svcDll = RegistryHelper.ReadString(
            @"SYSTEM\CurrentControlSet\Services\TermService\Parameters", "ServiceDll") ?? string.Empty;

        if (svcDll.Length == 0) return -1;

        if (!svcDll.Contains("termsrv.dll",  StringComparison.OrdinalIgnoreCase) &&
            !svcDll.Contains("rdpwrap.dll",  StringComparison.OrdinalIgnoreCase))
            return 2;

        if (svcDll.Contains("rdpwrap.dll", StringComparison.OrdinalIgnoreCase))
        {
            wrapperPath = svcDll;
            return 1;
        }
        return 0;
    }

    // ── Helper: GetTermSrvState (via SCM) ─────────────────────────────────────

    private static int GetTermSrvState()
        => ServiceHelper.GetCurrentState("TermService");

    // ── Helper: IsListenerWorking (via WinStationEnumerateW) ──────────────────

    private static bool IsListenerWorking()
    {
        if (!NativeMethods.WinStationEnumerate(IntPtr.Zero,
            out IntPtr pSessions, out uint count)) return false;

        bool found = false;
        try
        {
            // Each entry is { DWORD SessionId; WCHAR[34] Name; DWORD State }
            // = 4 + 68 + 4 = 76 bytes on x64 (with natural alignment the struct is 76 bytes)
            const int stride = 76; // sizeof(WTS_SESSION_INFOW) — matches Delphi packed array
            for (uint i = 0; i < count; i++)
            {
                IntPtr entry = pSessions + (int)(i * stride);
                // Name is at offset 4, WCHAR[34]
                string name = Marshal.PtrToStringUni(entry + 4, 34).TrimEnd('\0');
                if (name.Equals("RDP-Tcp", StringComparison.Ordinal))
                {
                    found = true;
                    break;
                }
            }
        }
        finally
        {
            NativeMethods.WinStationFreeMemory(pSessions);
        }

        return found;
    }

    // ── Helper: SetStatus ─────────────────────────────────────────────────────

    private static void SetStatus(Label lbl, string text, Color color)
    {
        lbl.Text      = text;
        lbl.ForeColor = color;
    }

    // ── OnFormClosing — unsaved-changes guard ─────────────────────────────────

    private void OnFormClosing(object? sender, FormClosingEventArgs e)
    {
        if (bApply.Enabled)
        {
            var result = MessageBox.Show(
                "Settings are not saved. Do you want to exit?",
                "Warning",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);
            e.Cancel = result != DialogResult.Yes;
        }
    }

    // ── License button ────────────────────────────────────────────────────────

    private void OnLicenseClick(object? sender, EventArgs e)
    {
        var text = ResourceHelper.ReadText(
            "RDPConf.Resources.license.txt",
            System.Reflection.Assembly.GetExecutingAssembly())
            ?? "(license not found)";

        using var dlg = new LicenseForm(text);
        if (dlg.ShowDialog(this) != DialogResult.OK)
            Application.Exit();
    }
}
