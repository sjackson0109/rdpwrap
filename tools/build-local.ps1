#!/usr/bin/env pwsh
# tools/build-local.ps1
# Full local build: C++ DLLs + C# tools → ./build/
#
# Usage (run from repo root):
#   .\tools\build-local.ps1              # full build
#   .\tools\build-local.ps1 -SkipCpp    # skip C++ DLL (faster for C# iteration)
#   .\tools\build-local.ps1 -SkipMsi    # skip MSI build (no WiX needed)
#
# See docs/BUILDING.md for prerequisites and troubleshooting.

#Requires -Version 5

param(
    [switch]$SkipCpp,
    [switch]$SkipIcons,
    [switch]$SkipMsi
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$REPO  = $PSScriptRoot | Split-Path -Parent
$BUILD = Join-Path $REPO "build"

# ─── Helpers ──────────────────────────────────────────────────────────────────

function Step([string]$msg) {
    Write-Host "`n==> $msg" -ForegroundColor Cyan
}

function Fail([string]$msg) {
    Write-Host "[FAIL] $msg" -ForegroundColor Red
    exit 1
}

# ─── Locate MSBuild ───────────────────────────────────────────────────────────

function Find-MSBuild {
    $candidates = @(
        # VS 2022 Build Tools / Community
        "${env:ProgramFiles}\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\amd64\MSBuild.exe",
        "${env:ProgramFiles}\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\amd64\MSBuild.exe",
        "${env:ProgramFiles}\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\amd64\MSBuild.exe",
        # VS 2019 Build Tools / Community
        "${env:ProgramFiles(x86)}\Microsoft Visual Studio\2019\BuildTools\MSBuild\Current\Bin\amd64\MSBuild.exe",
        "${env:ProgramFiles(x86)}\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\amd64\MSBuild.exe",
        "${env:ProgramFiles(x86)}\Microsoft Visual Studio\2019\Enterprise\MSBuild\Current\Bin\amd64\MSBuild.exe"
    )
    foreach ($p in $candidates) {
        if (Test-Path $p) { return $p }
    }
    # Fall back to PATH
    $found = Get-Command msbuild -ErrorAction SilentlyContinue
    if ($found) { return $found.Source }
    return $null
}

# ─── 0. Stamp INI date ────────────────────────────────────────────────────────

Step "Stamping rdpwrap.ini with today's date"
$today   = Get-Date -Format "yyyy-MM-dd"
$iniFile = "$REPO\msi\rdpwrap.ini"
(Get-Content $iniFile) -replace '^Updated=.*', "Updated=$today" | Set-Content $iniFile
Write-Host "  Updated= $today"

# Stage INI into RDPWInst embedded resources so it gets compiled in
$iniDest = "$REPO\src-csharp\RDPWInst\Resources\rdpwrap.ini"
New-Item -Force -ItemType Directory (Split-Path $iniDest) | Out-Null
Copy-Item $iniFile $iniDest -Force
Write-Host "  Staged:  src-csharp/RDPWInst/Resources/rdpwrap.ini"

# ─── 1. Icons ─────────────────────────────────────────────────────────────────

if (-not $SkipIcons) {
    Step "Generating application icons"
    & "$REPO\tools\make-icons.ps1"
}

# ─── 2. C++ DLLs ──────────────────────────────────────────────────────────────

if (-not $SkipCpp) {
    Step "Building C++ DLLs (x64 + Win32)"

    $msbuild = Find-MSBuild
    if (-not $msbuild) {
        Fail "MSBuild not found. Install Visual Studio 2019 or 2022 Build Tools with 'Desktop development with C++'."
    }
    Write-Host "  Using MSBuild: $msbuild"

    $msbuildArgs = @(
        "$REPO\src-x86-x64-Fusix\RDPWrap.sln",
        "/p:Configuration=Release",
        "/p:PlatformToolset=v143",
        "/p:WindowsTargetPlatformVersion=10.0",
        "/m", "/v:m"
    )

    foreach ($platform in @("x64", "Win32", "ARM64")) {
        Write-Host "  Building $platform..."
        & $msbuild @msbuildArgs "/p:Platform=$platform"
        if ($LASTEXITCODE -ne 0) { Fail "MSBuild failed for $platform" }
    }
} else {
    Write-Host "[skip] C++ DLL build (-SkipCpp)" -ForegroundColor Yellow
}

# ─── 3. C# tools ──────────────────────────────────────────────────────────────

Step "Publishing C# tools"

# Verify dotnet
$dotnetVer = (dotnet --version 2>&1)
Write-Host "  .NET SDK: $dotnetVer"

$tools = @("RDPConf", "RDPCheck", "RDPWInst")
$rids  = @("win-x64", "win-x86", "win-arm64")

foreach ($tool in $tools) {
    foreach ($rid in $rids) {
        $arch    = $rid -replace "win-", ""
        $staging = Join-Path $BUILD "staging\$tool\$rid"
        Write-Host "  $tool / $rid → $staging"

        dotnet publish "$REPO\src-csharp\$tool\$tool.csproj" `
            -c Release -r $rid `
            --self-contained true `
            -p:PublishSingleFile=true `
            -p:IncludeNativeLibrariesForSelfExtract=true `
            -p:PublishTrimmed=false `
            -p:Version=1.0.0 `
            -o $staging `
            --nologo -v quiet

        if ($LASTEXITCODE -ne 0) { Fail "dotnet publish failed for $tool/$rid" }
    }
}

# ─── 4. Assemble build/ ───────────────────────────────────────────────────────

Step "Assembling ./build/"

New-Item -Force -ItemType Directory $BUILD | Out-Null

# C++ DLLs
if (-not $SkipCpp) {
    $dllMap = @{
        "rdpwrap_x64.dll"   = "$REPO\src-x86-x64-Fusix\Release\x64\RDPWrap.dll"
        "rdpwrap_x86.dll"   = "$REPO\src-x86-x64-Fusix\Release\x86\RDPWrap.dll"
        "rdpwrap_arm64.dll" = "$REPO\src-x86-x64-Fusix\Release\arm64\RDPWrap.dll"
    }
    foreach ($dest in $dllMap.Keys) {
        $src = $dllMap[$dest]
        if (Test-Path $src) {
            Copy-Item $src "$BUILD\$dest" -Force
            Write-Host "  Copied: $dest"
        } else {
            Write-Warning "  Not found (DLL build skipped?): $src"
        }
    }
}

# C# executables
foreach ($tool in $tools) {
    foreach ($rid in $rids) {
        $arch = $rid -replace "win-", ""
        $src  = "$BUILD\staging\$tool\$rid\$tool.exe"
        $dest = "$BUILD\${tool}_${arch}.exe"
        if (Test-Path $src) {
            Copy-Item $src $dest -Force
            Write-Host "  Copied: ${tool}_${arch}.exe"
        } else {
            Write-Warning "  Not found: $src"
        }
    }
}

# INI file (side-by-side with installer, and for reference)
Copy-Item $iniFile "$BUILD\rdpwrap.ini" -Force
Write-Host "  Copied: rdpwrap.ini"

# Remove staging area
Remove-Item -Recurse -Force "$BUILD\staging" -ErrorAction SilentlyContinue

# ─── 5. MSI ───────────────────────────────────────────────────────────────────

if (-not $SkipMsi) {
    Step "Building MSI (WiX v5)"

    $wixProj  = "$REPO\msi\RDPWInst.wixproj"
    $pkgVer   = Get-Date -Format "yy.M.d"   # MSI version: major<=255, e.g. 26.4.1

    foreach ($arch in @('x64', 'x86', 'arm64')) {
        # Verify this arch's inputs exist in build/
        $required = @(
            "$BUILD\RDPWInst_$arch.exe",
            "$BUILD\RDPConf_$arch.exe",
            "$BUILD\RDPCheck_$arch.exe",
            "$BUILD\rdpwrap_$arch.dll",
            "$BUILD\rdpwrap.ini"
        )
        $missing = $required | Where-Object { -not (Test-Path $_) }
        if ($missing) {
            Write-Warning "  Skipping $arch MSI  --  missing inputs:`n    $($missing -join "`n    ")"
            continue
        }

        # Stage inputs next to the .wixproj (WiX resolves Source= relative to the project)
        foreach ($inputFile in $required) {
            Copy-Item $inputFile "$REPO\msi\" -Force -ErrorAction Continue
        }

        # Explicit OutputPath per-arch avoids WiX placing all builds in the same bin/x86 dir.
        # Explicit OutputName overrides the $(Platform) token which WiX doesn't expand in OutputName.
        $outDir  = Join-Path $REPO "msi_out\$arch"
        $wixOut  = Join-Path $outDir "RDPWrapper-$arch.msi"
        New-Item -Force -ItemType Directory $outDir | Out-Null

        # Use Continue so a non-zero exit from dotnet/WiX does NOT throw under ErrorActionPreference=Stop
        $buildArgs = @('build', $wixProj, '-c', 'Release',
            "/p:Platform=$arch",
            "/p:OutputName=RDPWrapper-$arch",
            "/p:OutputPath=$outDir",
            "/p:PackageVersion=$pkgVer",
            '--nologo')
        & dotnet @buildArgs
        $wixExit = $LASTEXITCODE
        if ($wixExit -ne 0) {
            Write-Warning "  MSI build failed for $arch (exit $wixExit)  --  continuing."
            continue
        }

        $msiSrc = if (Test-Path $wixOut) {
            Get-Item $wixOut
        } else {
            # Fallback: WiX may insert a locale subdir (e.g. msi_out/x64/en-US/…)
            Get-ChildItem $outDir -Recurse -Filter "RDPWrapper-$arch.msi" -ErrorAction SilentlyContinue |
                Sort-Object LastWriteTime -Descending | Select-Object -First 1
        }
        if ($msiSrc) {
            $dest = Join-Path $BUILD "RDPWrapper-$pkgVer-$arch.msi"
            Copy-Item $msiSrc.FullName $dest -Force
            Write-Host ("  Produced: RDPWrapper-$pkgVer-$arch.msi  ({0:N1} MB)" -f ($msiSrc.Length / 1MB))
        } else {
            Write-Warning "  MSI output not found after $arch build."
        }
    }
} else {
    Write-Host "[skip] MSI build (-SkipMsi)" -ForegroundColor Yellow
}

# ─── 6. Summary ───────────────────────────────────────────────────────────────

Step "Build complete"
Get-ChildItem $BUILD | Sort-Object Name | ForEach-Object {
    $kb = [math]::Round($_.Length / 1KB, 0)
    Write-Host ("  {0,-28} {1,6} KB" -f $_.Name, $kb)
}
Write-Host ""
