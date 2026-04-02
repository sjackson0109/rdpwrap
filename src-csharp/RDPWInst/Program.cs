// Copyright 2026 sjackson0109 — Apache License 2.0
//
// RDPWInst — RDP Wrapper Library Installer

using RDPWrap.Common;

namespace RDPWInst;

internal static class Program
{
    private static string Banner
    {
        get
        {
            var v = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            string version = v is null ? "unknown" : $"{v.Major}.{v.Minor}.{v.Build}";
            return
                $"RDP Wrapper Library\r\n" +
                $"Installer v{version} (C# edition)\r\n" +
                "Copyright (C) Stas'M Corp. 2018\r\n" +
                "Maintained by sjackson0109 2026\r\n";
        }
    }

    private const string Usage =
        "USAGE:\r\n" +
        "RDPWInst.exe [-l|-i[-s][-o][-f]|-w|-u[-k]|-r]\r\n\r\n" +
        "-l          display the license agreement\r\n" +
        "-i          install wrapper to Program Files folder (default)\r\n" +
        "-i -s       install wrapper to System32 folder\r\n" +
        "-i -o       online install mode (loads latest INI file)\r\n" +
        "-i -f       force install: silently uninstall existing installation first\r\n" +
        "-w          get latest update for INI file\r\n" +
        "-u          uninstall wrapper\r\n" +
        "-u -k       uninstall wrapper and keep settings\r\n" +
        "-r          force restart Terminal Services\r\n";

    internal static int Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        Console.WriteLine(Banner);

        // Validate args
        if (args.Length < 1 ||
            (args[0] != "-l" &&
             args[0] != "-i" &&
             args[0] != "-w" &&
             args[0] != "-u" &&
             args[0] != "-r"))
        {
            Console.WriteLine(Usage);
            Pause();
            return 0;
        }

        // -l  print license
        if (args[0] == "-l")
        {
            var license = ResourceHelper.ReadText("RDPWInst.Resources.license.txt",
                              System.Reflection.Assembly.GetExecutingAssembly());
            Console.WriteLine(license ?? "(license resource not found)");
            Pause();
            return 0;
        }

        // Windows Vista / Server 2008 minimum check
        if (Environment.OSVersion.Version < new Version(6, 0))
        {
            Console.Error.WriteLine("[-] Unsupported Windows version:");
            Console.Error.WriteLine("  only >= 6.0 (Vista, Server 2008 and newer) are supported.");
            Pause();
            return 1;
        }

        if (!ArchHelper.IsSupported)
        {
            Console.Error.WriteLine("[-] Unsupported processor architecture.");
            Pause();
            return 1;
        }

        var engine = new InstallerEngine();
        engine.CheckInstall();

        int rc = args[0] switch
        {
            "-i" => engine.Install(
                        toSystem32: args.Contains("-s"),
                        online:     args.Contains("-o"),
                        force:      args.Contains("-f")),
            "-u" => engine.Uninstall(keepSettings: args.Contains("-k")),
            "-w" => engine.Update(),
            "-r" => engine.Restart(),
            _    => 0
        };

        Pause();
        return rc;
    }

    /// <summary>
    /// Waits for a keypress only when the process owns its console window
    /// (i.e. was launched via double-click or UAC elevation rather than
    /// piped/redirected from a script).
    /// </summary>
    private static void Pause()
    {
        if (!Console.IsInputRedirected && !Console.IsOutputRedirected)
        {
            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey(intercept: true);
        }
    }
}
