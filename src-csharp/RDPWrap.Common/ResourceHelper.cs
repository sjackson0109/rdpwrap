// Copyright 2024 sjackson0109 — Apache License 2.0
using System.Reflection;

namespace RDPWrap.Common;

/// <summary>
/// Helpers for reading and extracting managed embedded resources.
/// Mirrors the Delphi ExtractRes / ExtractResText procedures from RDPWInst.dpr
/// and RDPConf MainUnit.pas, translated to the .NET manifest-resource model.
/// </summary>
public static class ResourceHelper
{
    /// <summary>
    /// Reads the contents of the embedded resource named
    /// <paramref name="resourceName"/> from <paramref name="assembly"/>
    /// and returns it as a UTF-8 string. Returns <c>null</c> if not found.
    /// </summary>
    public static string? ReadText(string resourceName,
        Assembly? assembly = null)
    {
        assembly ??= Assembly.GetCallingAssembly();
        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream is null) return null;
        using var reader = new StreamReader(stream, System.Text.Encoding.UTF8);
        return reader.ReadToEnd();
    }

    /// <summary>
    /// Reads the contents of the embedded resource named
    /// <paramref name="resourceName"/> and returns the raw bytes.
    /// Returns <c>null</c> if not found.
    /// </summary>
    public static byte[]? ReadBytes(string resourceName,
        Assembly? assembly = null)
    {
        assembly ??= Assembly.GetCallingAssembly();
        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream is null) return null;
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        return ms.ToArray();
    }

    /// <summary>
    /// Extracts the embedded resource <paramref name="resourceName"/> to disk
    /// at <paramref name="destPath"/>, creating parent directories as needed.
    /// Returns <c>true</c> on success. Mirrors the Delphi ExtractRes procedure.
    /// </summary>
    public static bool ExtractToDisk(string resourceName, string destPath,
        Assembly? assembly = null)
    {
        assembly ??= Assembly.GetCallingAssembly();
        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream is null)
        {
            Console.Error.WriteLine($"[-] Resource not found: {resourceName}");
            return false;
        }

        var dir = Path.GetDirectoryName(destPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        try
        {
            using var file = File.Create(destPath);
            stream.CopyTo(file);
            Console.WriteLine($"[+] Extracted {resourceName} -> {destPath}");
            return true;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(
                $"[-] Failed to extract resource {resourceName} to {destPath}: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Lists all manifest resource names in <paramref name="assembly"/>
    /// — useful for debugging resource name mismatches.
    /// </summary>
    public static IEnumerable<string> ListResources(Assembly? assembly = null)
    {
        assembly ??= Assembly.GetCallingAssembly();
        return assembly.GetManifestResourceNames();
    }
}
