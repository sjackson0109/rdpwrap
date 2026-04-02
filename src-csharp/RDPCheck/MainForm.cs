// Copyright 2026 sjackson0109 — Apache License 2.0
//
// Hosts the MsRdpClient2 ActiveX control, connects to 127.0.0.2, and
// reports the disconnect reason, mirroring the Delphi RDPDisconnected handler.

using RDPWrap.Common;

namespace RDPCheck;

internal sealed class MainForm : Form
{
    private readonly AxRdpClient2 _rdp;

    // Registry values saved on load — restored on disconnect
    private int _savedSecurityLayer;
    private int _savedUserAuthentication;

    // ── Constructor / Layout ──────────────────────────────────────────────────

    public MainForm()
    {
        SuspendLayout();

        Text            = "RDP Wrapper Check";
        ClientSize      = new Size(800, 600);
        StartPosition   = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.Sizable;

        _rdp = new AxRdpClient2
        {
            Dock = DockStyle.Fill
        };

        Controls.Add(_rdp);
        ResumeLayout(false);

        _rdp.Disconnected += OnRdpDisconnected;
        Load += OnFormLoad;
    }

    // ── FormLoad — mirrors TFrm.FormCreate ────────────────────────────────────

    private void OnFormLoad(object? sender, EventArgs e)
    {
        const string rdpTcpKey =
            @"SYSTEM\CurrentControlSet\Control\Terminal Server\WinStations\RDP-Tcp";

        _rdp.DisconnectedText      = "Disconnected.";
        _rdp.ConnectingText        = "Connecting...";
        _rdp.ConnectedStatusText   = "Connected.";
        _rdp.UserName              = string.Empty;
        _rdp.Server                = "127.0.0.2";

        // Read, then zero-out SecurityLayer / UserAuthentication
        _savedSecurityLayer      = RegistryHelper.ReadInt(rdpTcpKey, "SecurityLayer",     0);
        _savedUserAuthentication = RegistryHelper.ReadInt(rdpTcpKey, "UserAuthentication", 0);

        try
        {
            RegistryHelper.WriteInt(rdpTcpKey, "SecurityLayer",      0);
            RegistryHelper.WriteInt(rdpTcpKey, "UserAuthentication", 0);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[RDP] Registry write failed: {ex.Message}");
        }

        // Read port
        int port = RegistryHelper.ReadInt(rdpTcpKey, "PortNumber", 3389);
        _rdp.SetPort(port);

        // Subscribe to COM events now that the handle exists
        _rdp.SubscribeEvents();

        // Brief delay then connect — matches Delphi Sleep(1000); RDP.Connect;
        Task.Delay(1000).ContinueWith(_ => Invoke((Action)_rdp.Connect));
    }

    // ── OnDisconnected — mirrors TFrm.RDPDisconnected ─────────────────────────

    private void OnRdpDisconnected(object? sender, int discReason)
    {
        string errStr = discReason switch
        {
            0x001  => "Local disconnection.",
            0x002  => "Disconnected by user.",
            0x003  => "Disconnected by server.",
            0x904  => "Socket closed.",
            0xC08  => "Decompress error.",
            0x108  => "Connection timed out.",
            0xC06  => "Decryption error.",
            0x104  => "DNS name lookup failure.",
            0x508  => "DNS lookup failed.",
            0xB06  => "Encryption error.",
            0x604  => "Windows Sockets gethostbyname() call failed.",
            0x208  => "Host not found error.",
            0x408  => "Internal error.",
            0x906  => "Internal security error.",
            0xA06  => "Internal security error.",
            0x506  => "The encryption method specified is not valid.",
            0x804  => "Bad IP address specified.",
            0x606  => "Server security data is not valid.",
            0x406  => "Security data is not valid.",
            0x308  => "The IP address specified is not valid.",
            0x808  => "License negotiation failed.",
            0x908  => "Licensing time-out.",
            0x106  => "Out of memory.",
            0x206  => "Out of memory.",
            0x306  => "Out of memory.",
            0x706  => "Failed to unpack server certificate.",
            0x204  => "Socket connection failed.",
            0x404  => "Windows Sockets recv() call failed.",
            0x704  => "Time-out occurred.",
            0x608  => "Internal timer error.",
            0x304  => "Windows Sockets send() call failed.",
            0xB07  => "The account is disabled.",
            0xE07  => "The account is expired.",
            0xD07  => "The account is locked out.",
            0xC07  => "The account is restricted.",
            0x1B07 => "The received certificate is expired.",
            0x1607 => "The policy does not support delegation of credentials to the target server.",
            0x2107 => "The server authentication policy does not allow connection requests using saved credentials. The user must enter new credentials.",
            0x807  => "Login failed.",
            0x1807 => "No authority could be contacted for authentication. The domain name of the authenticating party could be wrong, the domain could be unreachable, or there might have been a trust relationship failure.",
            0xA07  => "The specified user has no account.",
            0xF07  => "The password is expired.",
            0x1207 => "The user password must be changed before logging on for the first time.",
            0x1707 => "Delegation of credentials to the target server is not allowed unless mutual authentication has been achieved.",
            0x2207 => "The smart card is blocked.",
            0x1C07 => "An incorrect PIN was presented to the smart card.",
            0xB09  => "Network Level Authentication is required, run RDPCheck as administrator.",
            0x708  => "RDP is working, but the client doesn't allow loopback connections. Try to connect to your PC from another device in the network.",
            _      => $"Unknown code 0x{discReason:X}"
        };

        if (discReason > 2)
        {
            MessageBox.Show(errStr, "Disconnected",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        // Restore registry
        const string rdpTcpKey =
            @"SYSTEM\CurrentControlSet\Control\Terminal Server\WinStations\RDP-Tcp";
        try
        {
            RegistryHelper.WriteInt(rdpTcpKey, "SecurityLayer",     _savedSecurityLayer);
            RegistryHelper.WriteInt(rdpTcpKey, "UserAuthentication", _savedUserAuthentication);
        }
        catch { }

        Application.Exit();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _rdp.Dispose();
        base.Dispose(disposing);
    }
}
