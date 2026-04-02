#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Fetches the latest sergiye/rdpWrapper release, downloads the x64 and x86 GUI
    executables, computes their SHA-256 hashes, and writes them to
    tools/sergiye-hashes.json — ready to commit.

.DESCRIPTION
    Run this script locally (or in CI) immediately after a new sergiye/rdpWrapper
    release is published, then commit the updated JSON file.  Once the file contains
    a non-empty 'release' key, build-and-release.yml will enforce the hashes on every
    subsequent release run and fail the workflow if the downloaded files differ.

.PARAMETER OutFile
    Path to the JSON hash file to update.  Defaults to tools/sergiye-hashes.json
    relative to the script's parent directory.

.PARAMETER GithubToken
    Optional GitHub personal access token (or value of GITHUB_TOKEN env var) to
    avoid API rate-limiting during repeated local runs.

.EXAMPLE
    # Run from the repo root:
    pwsh tools/update-sergiye-hashes.ps1

.EXAMPLE
    # Override output path:
    pwsh tools/update-sergiye-hashes.ps1 -OutFile ./my-hashes.json
#>
[CmdletBinding()]
param(
    [string] $OutFile    = $null,
    [string] $GithubToken = $env:GITHUB_TOKEN
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# ── Resolve paths ─────────────────────────────────────────────────────────────
$repoRoot = Split-Path $PSScriptRoot -Parent
if (-not $OutFile) {
    $OutFile = Join-Path $repoRoot 'tools\sergiye-hashes.json'
}

# ── Fetch latest release metadata from GitHub API ─────────────────────────────
$apiUrl  = 'https://api.github.com/repos/sergiye/rdpWrapper/releases/latest'
$headers = @{ Accept = 'application/vnd.github+json'; 'X-GitHub-Api-Version' = '2022-11-28' }
if ($GithubToken) { $headers['Authorization'] = "Bearer $GithubToken" }

Write-Host "Querying GitHub API: $apiUrl"
$release = Invoke-RestMethod -Uri $apiUrl -Headers $headers -UseBasicParsing
$tag     = $release.tag_name
Write-Host "Latest sergiye release: $tag"

# ── Locate the two assets we pin ──────────────────────────────────────────────
$needed  = @('rdpWrapper_x64.exe', 'rdpWrapper_x86.exe')
$assets  = @{}
foreach ($name in $needed) {
    $asset = $release.assets | Where-Object { $_.name -eq $name }
    if (-not $asset) {
        throw "Asset '$name' not found in release $tag.  Available: $($release.assets.name -join ', ')"
    }
    $assets[$name] = $asset.browser_download_url
    Write-Host "  Found $name  →  $($asset.browser_download_url)"
}

# ── Download to a temp directory and hash ─────────────────────────────────────
$tmp    = Join-Path ([IO.Path]::GetTempPath()) "sergiye-hash-$([guid]::NewGuid().ToString('N'))"
$null   = New-Item -ItemType Directory -Path $tmp -Force
$hashes = @{}

try {
    foreach ($name in $needed) {
        $dest = Join-Path $tmp $name
        Write-Host "Downloading $name …"
        Invoke-WebRequest -Uri $assets[$name] -OutFile $dest -UseBasicParsing
        $hash = (Get-FileHash $dest -Algorithm SHA256).Hash
        $hashes[$name] = $hash
        Write-Host "  SHA-256: $hash"
    }
} finally {
    Remove-Item $tmp -Recurse -Force -ErrorAction SilentlyContinue
}

# ── Build and write the JSON file ─────────────────────────────────────────────
$existing = @{ '_comment' = @() }
if (Test-Path $OutFile) {
    $existing = Get-Content $OutFile -Raw | ConvertFrom-Json -AsHashtable
}

$output = [ordered]@{
    '_comment'         = $existing['_comment']  # preserve explanatory comment
    'release'          = $tag
    'rdpWrapper_x64.exe' = $hashes['rdpWrapper_x64.exe']
    'rdpWrapper_x86.exe' = $hashes['rdpWrapper_x86.exe']
}

$json = $output | ConvertTo-Json -Depth 5
Set-Content -Path $OutFile -Value $json -Encoding UTF8
Write-Host ""
Write-Host "Written: $OutFile"
Write-Host ""
Write-Host "Next steps:"
Write-Host "  1. Review the diff: git diff tools/sergiye-hashes.json"
Write-Host "  2. Commit: git add tools/sergiye-hashes.json && git commit -m 'chore: pin sergiye hashes to $tag'"
Write-Host "  3. Push: git push"
