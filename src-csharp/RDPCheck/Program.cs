// Copyright 2026 sjackson0109 — Apache License 2.0
//
// RDPCheck — WinForms RDP tester entry point.

namespace RDPCheck;

internal static class Program
{
    [STAThread]
    internal static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
    }
}
