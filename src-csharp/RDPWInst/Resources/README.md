# RDPWInst/Resources

Place the compiled binary payloads here before building RDPWInst.exe.
These files are embedded as manifest resources at build time via `<EmbeddedResource Condition="Exists(...)">` entries in `RDPWInst.csproj` — if a file is absent the resource is simply omitted and the build still succeeds (online install mode is used as fallback).

## CI-staged files (automatically copied by `build-and-release.yml`)

| File          | Source                                           | Used when                   |
|---------------|--------------------------------------------------|-----------------------------|
| `rdpw32.dll`  | Build output of `src-x86-x64-Fusix/` (Win32)    | Always — 32-bit install     |
| `rdpw64.dll`  | Build output of `src-x86-x64-Fusix/` (x64)      | Always — 64-bit install     |
| `rdpwrap.ini` | `msi/rdpwrap.ini` from repo                      | Always — offline fallback   |
| `license.txt` | Repo `LICENSE` (copied as plain text)            | `RDPWInst -l` flag          |

## Optional legacy files (not in VCS, not staged by CI)

| File              | Purpose                                    | Status / action required                                                             |
|-------------------|--------------------------------------------|--------------------------------------------------------------------------------------|
| `rdpclip6032.exe` | Updated rdpclip for Vista x86              | Not redistributable by this project — obtain from original stascorp release if needed |
| `rdpclip6064.exe` | Updated rdpclip for Vista x64              | Same as above                                                                        |
| `rdpclip6132.exe` | Updated rdpclip for Win7 x86              | Same as above                                                                        |
| `rdpclip6164.exe` | Updated rdpclip for Win7 x64              | Same as above                                                                        |
| `rfxvmt32.dll`    | RemoteFX codec for Win10 Home x86          | **Not redistributable** — must be extracted from a Windows 10 Home installation at `C:\Windows\System32\rfxvmt.dll`. See [#194](https://github.com/stascorp/rdpwrap/issues/194) for context. |
| `rfxvmt64.dll`    | RemoteFX codec for Win10 Home x64          | Same as above                                                                        |

> **Decision note:** `rfxvmt.dll` is a Microsoft-owned component and cannot be legally bundled.
> `InstallerEngine.cs` handles the missing-rfxvmt case at runtime: if the file is absent from
> the embedded resources and absent from the install directory, a warning is printed and the
> user is directed to copy it manually. No CI step attempts to source or stage these files.

All binary files in this folder are excluded from version control via `.gitignore`.
