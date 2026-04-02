# Building RDP Wrapper Locally

This document covers the full local build pipeline — from prerequisites to the final `./build/` artefacts.

---

## Prerequisites

| Component | Version | Notes |
|---|---|---|
| Windows 10/11 x64 | any | Host OS |
| .NET SDK | 10.0+ | `winget install Microsoft.DotNet.SDK.10` |
| Visual Studio 2019 Build Tools | 16.x | For C++ DLL only |
| MSVC v142 toolset | 14.29+ | Installed via VS Build Tools installer |
| Windows SDK | 10.0.19041+ | Installed via VS Build Tools installer |

> **ARM64 DLL note:** Building `rdpwrap_arm64.dll` locally requires Visual Studio 2022
> with the "MSVC v143 — VS 2022 C++ ARM64 build tools" component. The GitHub Actions
> CI uses a hosted `windows-latest` runner which provides this. For local work, the
> x64 and Win32 DLLs are sufficient for testing.

---

## 1. Clone the repository

```powershell
git clone --recurse-submodules https://github.com/<owner>/rdpwrap.git
cd rdpwrap
```

If you already cloned without `--recurse-submodules`:

```powershell
git submodule update --init --recursive
```

---

## 2. Generate application icons

The C# tool icons are generated programmatically via a helper script and are **not**
committed to source control. Run this once after cloning (and again if you delete them):

```powershell
.\tools\make-icons.ps1
```

This creates:

- `src-csharp/RDPConf/app.ico` — blue "C" icon for RDPConf  
- `src-csharp/RDPCheck/app.ico` — green "K" icon for RDPCheck  

Both files are `.gitignore`-exempt (not listed) so they persist in your working tree.

---

## 3. Build the C++ DLL (`rdpwrap.dll`)

### Locate MSBuild

Visual Studio 2019 Build Tools installs MSBuild to a non-standard path.  
Add it to your session PATH once (or use the full path as shown below):

```powershell
$msbuild = "C:\Program Files (x86)\Microsoft Visual Studio\2019\BuildTools\MSBuild\Current\Bin\amd64\MSBuild.exe"
```

> If you have Visual Studio IDE installed instead of Build Tools, the path is:
> `C:\Program Files\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\amd64\MSBuild.exe`

### One-time vcxproj fix

The Windows SDK 10.0.19041 headers emit a C2338 packing warning that is treated as an
error under `/WX`. It has **no behavioural effect** — the fix is already applied in
`src-x86-x64-Fusix/RDPWrap.vcxproj` via the `WINDOWS_IGNORE_PACKING_MISMATCH` define.
No manual action is required.

### Build x64 and Win32

```powershell
$msbuild = "C:\Program Files (x86)\Microsoft Visual Studio\2019\BuildTools\MSBuild\Current\Bin\amd64\MSBuild.exe"

& $msbuild src-x86-x64-Fusix\RDPWrap.sln `
    /p:Configuration=Release /p:Platform=x64  `
    /p:PlatformToolset=v142   `
    /p:WindowsTargetPlatformVersion=10.0.19041.0 `
    /m /v:m

& $msbuild src-x86-x64-Fusix\RDPWrap.sln `
    /p:Configuration=Release /p:Platform=Win32 `
    /p:PlatformToolset=v142   `
    /p:WindowsTargetPlatformVersion=10.0.19041.0 `
    /m /v:m
```

> Expect `C4244` / `C4267` type-conversion warnings from the Zydis disassembler
> submodule — these are benign and can be ignored.

### Output locations

| Platform | Output path |
|---|---|
| x64 | `src-x86-x64-Fusix/x64/Release/RDPWrap.dll` |
| Win32 | `src-x86-x64-Fusix/Release/RDPWrap.dll` |

---

## 4. Build C# tools

All three C# tools are published as self-contained single-file executables using
`dotnet publish`.

### Architectures

| RID | Description |
|---|---|
| `win-x64` | 64-bit Intel/AMD |
| `win-x86` | 32-bit Intel/AMD |
| `win-arm64` | ARM64 (cross-compiled, no native toolchain needed) |

### Commands

```powershell
$TOOLS = @("RDPConf", "RDPCheck", "RDPWInst")
$RIDS  = @("win-x64", "win-x86", "win-arm64")

foreach ($tool in $TOOLS) {
    foreach ($rid in $RIDS) {
        $arch = $rid -replace "win-", ""
        $out  = "build\staging\$tool\$rid"
        dotnet publish "src-csharp\$tool\$tool.csproj" `
            -c Release -r $rid `
            --self-contained true `
            -p:PublishSingleFile=true `
            -p:IncludeNativeLibrariesForSelfExtract=true `
            -p:PublishTrimmed=false `
            -p:Version=1.0.0 `
            -o $out
    }
}
```

### Output

After publishing, executables are in `build/staging/<Tool>/<RID>/`:

```
build\staging\RDPConf\win-x64\RDPConf.exe
build\staging\RDPConf\win-x86\RDPConf.exe
build\staging\RDPConf\win-arm64\RDPConf.exe
build\staging\RDPCheck\win-x64\RDPCheck.exe
...
```

---

## 5. Assemble `./build/`

Copy all artefacts to a flat `./build/` directory:

```powershell
New-Item -Force -ItemType Directory build | Out-Null

# DLLs
Copy-Item src-x86-x64-Fusix\x64\Release\RDPWrap.dll  build\rdpwrap_x64.dll
Copy-Item src-x86-x64-Fusix\Release\RDPWrap.dll       build\rdpwrap_x86.dll

# C# executables
foreach ($tool in @("RDPConf","RDPCheck","RDPWInst")) {
    foreach ($rid in @("win-x64","win-x86","win-arm64")) {
        $arch = $rid -replace "win-",""
        Copy-Item "build\staging\$tool\$rid\$tool.exe" `
                  "build\${tool}_${arch}.exe"
    }
}

# Cleanup staging
Remove-Item -Recurse -Force build\staging
```

### Final `./build/` layout

```
build/
  rdpwrap_x64.dll       # x64 termsrv.dll hook
  rdpwrap_x86.dll       # x86 termsrv.dll hook
  RDPConf_x64.exe       # GUI config tool (x64)
  RDPConf_x86.exe       # GUI config tool (x86)
  RDPConf_arm64.exe     # GUI config tool (ARM64)
  RDPCheck_x64.exe      # RDP loopback tester (x64)
  RDPCheck_x86.exe      # RDP loopback tester (x86)
  RDPCheck_arm64.exe    # RDP loopback tester (ARM64)
  RDPWInst_x64.exe      # CLI installer (x64)
  RDPWInst_x86.exe      # CLI installer (x86)
  RDPWInst_arm64.exe    # CLI installer (ARM64)
```

> `./build/` is listed in `.gitignore` — artefacts are not committed.

---

## 6. Automated script

All steps 2–5 are automated in `tools/build-local.ps1`.  
Run it from the repo root:

```powershell
.\tools\build-local.ps1
```

Optional flag to skip the DLL rebuild (faster during C# iteration):

```powershell
.\tools\build-local.ps1 -SkipCpp
```

---

## 7. Troubleshooting

### MSBuild not found
Verify VS 2019 Build Tools are installed.  
Open the **Visual Studio Installer** → Modify → ensure  
"Desktop development with C++" and "MSVC v142 build tools" are checked.

### ARM64 DLL: `error MSB8013`
No ARM64 cross-compiler found. Either install VS 2022 with ARM64 tools or skip ARM64  
(`-SkipArm64Dll` flag in `build-local.ps1`). The ARM64 DLL is produced by CI.

### `error C2338: Windows headers require the default packing option`
`WINDOWS_IGNORE_PACKING_MISMATCH` is missing from `PreprocessorDefinitions`.  
Run the following once then rebuild:

```powershell
(Get-Content src-x86-x64-Fusix\RDPWrap.vcxproj) `
    -replace '(<PreprocessorDefinitions>)([^<]+)(<)', `
             '$1$2;WINDOWS_IGNORE_PACKING_MISMATCH$3' |
    Set-Content src-x86-x64-Fusix\RDPWrap.vcxproj
```

### `dotnet publish` fails with SDK not found
Ensure .NET 10 SDK is installed: `dotnet --version` should print `10.x.x`.

---

## Appendix: CI vs local comparison

| Feature | Local build | GitHub Actions CI |
|---|---|---|
| x64 DLL | ✅ | ✅ |
| x86 DLL | ✅ | ✅ |
| ARM64 DLL | ❌ (needs VS 2022) | ✅ |
| C# x64/x86/arm64 | ✅ (cross-compiled) | ✅ |
| Code signing | ❌ | ✅ (if cert configured) |
| GitHub Release | ❌ | ✅ (on tag push) |
