// Copyright 2024 sjackson0109 — Apache License 2.0
//
// RDPWInst — RDP Wrapper Library Installer
// Direct C# translation of src-installer/RDPWInst.dpr

using RDPWrap.Common;

namespace RDPWInst;

internal static class Program
{
    private const string Banner =
        "RDP Wrapper Library v1.6.2\r\n" +
        "Installer v3.0 (C# edition)\r\n" +
        "Copyright (C) Stas'M Corp. 2018 / sjackson0109 2024\r\n";

    private const string Usage =
        "USAGE:\r\n" +
        "RDPWInst.exe [-l|-i[-s][-o]|-w|-u[-k]|-r]\r\n\r\n" +
        "-l          display the license agreement\r\n" +
        "-i          install wrapper to Program Files folder (default)\r\n" +
        "-i -s       install wrapper to System32 folder\r\n" +
        "-i -o       online install mode (loads latest INI file)\r\n" +
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
            return 0;
        }

        // -l  print license
        if (args[0] == "-l")
        {
            var license = ResourceHelper.ReadText("RDPWInst.Resources.license.txt",
                              System.Reflection.Assembly.GetExecutingAssembly());
            Console.WriteLine(license ?? "(license resource not found)");
            return 0;
        }

        // Windows Vista / Server 2008 minimum check
        if (Environment.OSVersion.Version < new Version(6, 0))
        {
            Console.Error.WriteLine("[-] Unsupported Windows version:");
            Console.Error.WriteLine("  only >= 6.0 (Vista, Server 2008 and newer) are supported.");
            return 1;
        }

        if (!ArchHelper.IsSupported)
        {
            Console.Error.WriteLine("[-] Unsupported processor architecture.");
            return 1;
        }

        var engine = new InstallerEngine();
        engine.CheckInstall();

        return args[0] switch
        {
            "-i" => engine.Install(
                        toSystem32: args.Contains("-s"),
                        online:     args.Contains("-o")),
            "-u" => engine.Uninstall(keepSettings: args.Contains("-k")),
            "-w" => engine.Update(),
            "-r" => engine.Restart(),
            _    => 0
        };
    }
}
