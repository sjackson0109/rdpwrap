// Copyright 2026 sjackson0109 — Apache License 2.0
//
// LicenseForm — mirrors LicenseUnit.pas (TLicenseForm).
// Shows a readonly multiline license text with Accept / Decline buttons.

namespace RDPConf;

internal sealed class LicenseForm : Form
{
    public LicenseForm(string licenseText)
    {
        Text            = "License Agreement";
        ClientSize      = new Size(600, 440);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox     = false;
        MinimizeBox     = false;
        StartPosition   = FormStartPosition.CenterParent;
        Font            = new Font("Segoe UI", 9f);

        var mText = new TextBox
        {
            Multiline   = true,
            ReadOnly    = true,
            ScrollBars  = ScrollBars.Vertical,
            Text        = licenseText,
            Location    = new Point(8, 8),
            Size        = new Size(576, 380),
            Font        = new Font("Courier New", 8.5f),
            BackColor   = SystemColors.Window
        };

        var bAccept = new Button
        {
            Text         = "Accept",
            DialogResult = DialogResult.OK,
            Location     = new Point(428, 398),
            Size         = new Size(75, 26)
        };

        var bDecline = new Button
        {
            Text         = "Decline",
            DialogResult = DialogResult.Cancel,
            Location     = new Point(509, 398),
            Size         = new Size(75, 26)
        };

        AcceptButton = bAccept;
        CancelButton = bDecline;

        Controls.AddRange(new Control[] { mText, bAccept, bDecline });
    }
}
