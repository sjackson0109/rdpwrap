// Copyright 2026 sjackson0109 — Apache License 2.0
//
// COM interop helpers for hosting the MsRdpClient2 ActiveX control and
// receiving its disconnection event without requiring pre-generated TLB wrappers.

using System.ComponentModel;
using System.Runtime.InteropServices;

namespace RDPCheck;

// ── COM interfaces needed for connection-point event subscription ────────────

[ComImport]
[Guid("B196B284-BAB4-101A-B69C-00AA00341D07")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IConnectionPointContainer
{
    void EnumConnectionPoints(out IntPtr ppEnum);
    void FindConnectionPoint(ref Guid riid, [MarshalAs(UnmanagedType.Interface)] out IConnectionPoint ppCP);
}

[ComImport]
[Guid("B196B287-BAB4-101A-B69C-00AA00341D07")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IConnectionPoint
{
    void GetConnectionInterface(out Guid pIID);
    void GetConnectionPointContainer([MarshalAs(UnmanagedType.Interface)] out IConnectionPointContainer ppCPC);
    void Advise([MarshalAs(UnmanagedType.Interface)] object pUnkSink, out uint pdwCookie);
    void Unadvise(uint dwCookie);
    void EnumConnections(out IntPtr ppEnum);
}

// ── IDispatch-based event interface (dispinterface) ──────────────────────────
// Matching the DIID of IMsTscAxEvents so the RDP control can route events here.

[ComVisible(true)]
[Guid("336D5562-EBA6-11D0-B0B0-00C04FD610D0")]
[InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
internal interface IRdpEvents
{
    [DispId(1)] void OnConnecting();
    [DispId(2)] void OnConnected();
    [DispId(3)] void OnLoginComplete();
    [DispId(4)] void OnDisconnected(int discReason);
    // Additional high-DISPID events are ignored (not listed here)
}

// ── Managed event sink ────────────────────────────────────────────────────────

[ComVisible(true)]
[ClassInterface(ClassInterfaceType.None)]
internal sealed class RdpEventSink : IRdpEvents
{
    private readonly Action<int> _onDisconnected;

    public RdpEventSink(Action<int> onDisconnected) => _onDisconnected = onDisconnected;

    public void OnConnecting()    { }
    public void OnConnected()     { }
    public void OnLoginComplete() { }
    public void OnDisconnected(int discReason) => _onDisconnected(discReason);
}

// ── AxHost wrapper for MsRdpClient2 ──────────────────────────────────────────

internal sealed class AxRdpClient2 : AxHost
{
    // CLSID for MsRdpClient2      (msrdp.ocx / mstscax.dll)
    private static readonly Guid Clsid  = new("9059F30F-4EB1-4BD2-9FDC-36F43A218F4A");
    // DIID for the default source interface (IMsTscAxEvents / DIMsTscAxEvents)
    private static readonly Guid DiidEvents = new("336D5562-EBA6-11D0-B0B0-00C04FD610D0");

    private dynamic? _ocx;
    private IConnectionPoint? _cp;
    private uint  _cookie;
    private RdpEventSink? _sink;

    /// <summary>Raised on the UI thread when the RDP control disconnects.</summary>
    public event EventHandler<int>? Disconnected;

    public AxRdpClient2() : base(Clsid.ToString("B")) { }

    protected override void AttachInterfaces()
    {
        _ocx = GetOcx() as dynamic;
    }

    /// <summary>
    /// Wires up the COM event sink. Call once after the handle has been created
    /// (i.e. after the form is shown).
    /// </summary>
    public void SubscribeEvents()
    {
        if (_ocx is null) return;
        try
        {
            var cpContainer = (IConnectionPointContainer)_ocx;
            var eventsIid = DiidEvents;
            cpContainer.FindConnectionPoint(ref eventsIid, out var cp);
            _sink = new RdpEventSink(reason => Invoke(
                (Action)(() => Disconnected?.Invoke(this, reason))));
            cp.Advise(_sink, out _cookie);
            _cp = cp;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[RDP] SubscribeEvents failed: {ex.Message}");
        }
    }

    /// <summary>Unsubscribes the event sink.</summary>
    public void UnsubscribeEvents()
    {
        try { _cp?.Unadvise(_cookie); } catch { }
        _cp = null;
    }

    // ── Typed property / method wrappers ────────────────────────────────────

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string Server         { set { if (_ocx != null) _ocx.Server         = value; } }
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string UserName       { set { if (_ocx != null) _ocx.UserName       = value; } }
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string DisconnectedText { set { if (_ocx != null) _ocx.DisconnectedText = value; } }
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string ConnectingText   { set { if (_ocx != null) _ocx.ConnectingText   = value; } }
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string ConnectedStatusText { set { if (_ocx != null) _ocx.ConnectedStatusText = value; } }

    public void SetPort(int port)
    {
        try { if (_ocx != null) _ocx.AdvancedSettings2.RDPPort = port; } catch { }
    }

    public void Connect()
    {
        try { _ocx?.Connect(); } catch { }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) UnsubscribeEvents();
        base.Dispose(disposing);
    }
}
