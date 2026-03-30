// Copyright 2024 sjackson0109 — Apache License 2.0
using System.Diagnostics;

namespace RDPWrap.Common;

/// <summary>
/// File-version reading helper. Mirrors the GetFileVersion function used in
/// RDPWInst.dpr and RDPConf MainUnit.pas.
/// </summary>
public static class FileVersionHelper
{
    /// <summary>
    /// Strongly-typed representation of a Windows file version.
    /// </summary>
    public record FileVersionInfo(
        ushort Major,
        ushort Minor,
        ushort Release,
        ushort Build,
        bool   IsDebug,
        bool   IsPrerelease,
        bool   IsPrivate,
        bool   IsSpecial)
    {
        /// <summary>e.g. "10.0.26100.3476"</summary>
        public override string ToString() => $"{Major}.{Minor}.{Release}.{Build}";
    }

    /// <summary>
    /// Returns the file version of <paramref name="filePath"/>, or <c>null</c>
    /// if the file does not exist or has no version resource.
    /// Uses the BCL <see cref="System.Diagnostics.FileVersionInfo"/> which does
    /// not require loading the DLL as executable — safe for locked DLLs.
    /// </summary>
    public static FileVersionInfo? GetVersion(string filePath)
    {
        if (!File.Exists(filePath)) return null;

        try
        {
            var fvi = System.Diagnostics.FileVersionInfo.GetVersionInfo(filePath);
            return new FileVersionInfo(
                (ushort)(fvi.FileMajorPart),
                (ushort)(fvi.FileMinorPart),
                (ushort)(fvi.FileBuildPart),
                (ushort)(fvi.FilePrivatePart),
                fvi.IsDebug,
                fvi.IsPreRelease,
                fvi.IsPrivateBuild,
                fvi.IsSpecialBuild);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Convenience overload: resolves the path via
    /// <see cref="ArchHelper.ExpandPath"/> before reading.
    /// </summary>
    public static FileVersionInfo? GetVersionExpanded(string pathWithEnvVars)
        => GetVersion(ArchHelper.ExpandPath(pathWithEnvVars));
}
