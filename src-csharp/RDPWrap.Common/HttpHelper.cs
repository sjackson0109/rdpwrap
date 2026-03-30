// Copyright 2024 sjackson0109 — Apache License 2.0

namespace RDPWrap.Common;

/// <summary>
/// HTTP download helpers that replace the WinInet-based GitINIFile /
/// DownloadFileToDisk procedures from RDPWInst.dpr.
/// Uses <see cref="HttpClient"/> with a shared static instance.
/// </summary>
public static class HttpHelper
{
    // Single shared instance — HttpClient is designed to be reused.
    private static readonly HttpClient _client = new(new HttpClientHandler
    {
        AllowAutoRedirect = true,
        MaxAutomaticRedirections = 5,
    })
    {
        Timeout = TimeSpan.FromSeconds(60),
        DefaultRequestHeaders = { { "User-Agent", "RDP-Wrapper-Updater/1.0" } }
    };

    /// <summary>
    /// Downloads the text content at <paramref name="url"/> and returns it as
    /// a string. Returns <c>null</c> on any failure.
    /// Mirrors the Delphi GitINIFile function.
    /// </summary>
    public static async Task<string?> DownloadStringAsync(string url)
    {
        try
        {
            return await _client.GetStringAsync(url).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[-] HTTP download failed ({url}): {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Downloads the binary content at <paramref name="url"/> and saves it to
    /// <paramref name="destPath"/>. Returns <c>true</c> when the file exists
    /// and is non-empty after download.
    /// Mirrors the Delphi DownloadFileToDisk function.
    /// </summary>
    public static async Task<bool> DownloadFileAsync(string url, string destPath)
    {
        try
        {
            using var response = await _client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead)
                                              .ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
            await using var file   = File.Create(destPath);
            await stream.CopyToAsync(file).ConfigureAwait(false);

            return new FileInfo(destPath).Length > 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[-] HTTP file download failed ({url}): {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Synchronous wrapper for <see cref="DownloadStringAsync"/> — suitable
    /// for the installer's purely-sequential flow.
    /// </summary>
    public static string? DownloadString(string url)
        => DownloadStringAsync(url).GetAwaiter().GetResult();

    /// <summary>
    /// Synchronous wrapper for <see cref="DownloadFileAsync"/>.
    /// </summary>
    public static bool DownloadFile(string url, string destPath)
        => DownloadFileAsync(url, destPath).GetAwaiter().GetResult();
}
