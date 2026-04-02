// Copyright 2026 sjackson0109 — Apache License 2.0
//
// RDPConf — WinForms configuration GUI entry point.

namespace RDPConf;

internal static class Program
{
    [STAThread]
    internal static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
    }
}
