// Copyright 2024 sjackson0109 — Apache License 2.0

namespace RDPWrap.Common;

/// <summary>
/// Lightweight INI-file helpers used to check whether a specific version
/// section exists in rdpwrap.ini. Mirrors the INIHasSection function and
/// CheckSupport version-lookup logic from RDPWInst.dpr and RDPConf MainUnit.pas.
/// </summary>
public static class IniHelper
{
    /// <summary>
    /// Returns <c>true</c> when the INI file at <paramref name="iniPath"/>
    /// contains the section header <c>[<paramref name="section"/>]</c>.
    /// Mirrors the Delphi INIHasSection function.
    /// </summary>
    public static bool HasSection(string iniPath, string section)
    {
        if (!File.Exists(iniPath)) return false;
        var needle = $"[{section}]";
        foreach (var line in File.ReadLines(iniPath))
        {
            if (line.Contains(needle, StringComparison.Ordinal))
                return true;
        }
        return false;
    }

    /// <summary>
    /// Loads the full text of <paramref name="iniPath"/> and returns it,
    /// or an empty string if the file does not exist.
    /// </summary>
    public static string LoadText(string iniPath)
        => File.Exists(iniPath) ? File.ReadAllText(iniPath) : string.Empty;

    /// <summary>
    /// Checks the support level of a given termsrv.dll version against the
    /// INI content string <paramref name="iniContent"/>.
    /// </summary>
    /// <returns>
    /// 0 = not supported, 1 = partially supported (Vista/7 legacy),
    /// 2 = fully supported (entry found in ini).
    /// </returns>
    public static int CheckSupportLevel(string iniContent,
        FileVersionHelper.FileVersionInfo fv)
    {
        int level = 0;

        // Vista (6.0) and Windows 7 (6.1) are "partially" supported without
        // a specific INI entry — mirrors the Delphi CheckSupport logic.
        if ((fv.Major == 6 && fv.Minor == 0) ||
            (fv.Major == 6 && fv.Minor == 1))
            level = 1;

        var verTxt = fv.ToString();   // "major.minor.release.build"
        if (iniContent.Contains($"[{verTxt}]", StringComparison.Ordinal))
            level = 2;

        return level;
    }
}
