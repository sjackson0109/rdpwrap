# RDPWInst/Resources

Place the compiled binary payloads here before building RDPWInst.exe.
These files are embedded as manifest resources at build time.

## Required files

| File                | Source                                         | Used when                     |
|---------------------|------------------------------------------------|-------------------------------|
| `rdpw32.dll`        | Build output of `src-x86-x64-Fusix/` (x86)    | Always (32-bit install)       |
| `rdpw64.dll`        | Build output of `src-x86-x64-Fusix/` (x64)    | Always (64-bit install)       |
| `rdpwrap.ini`       | `res/rdpwrap.ini` (bundled baseline)           | Always (fallback / offline)   |
| `rdpclip6032.exe`   | Original Stas'M redistributable (x86)          | Vista x86 install             |
| `rdpclip6064.exe`   | Original Stas'M redistributable (x64)          | Vista x64 install             |
| `rdpclip6132.exe`   | Original Stas'M redistributable (x86)          | Win7 x86 install              |
| `rdpclip6164.exe`   | Original Stas'M redistributable (x64)          | Win7 x64 install              |
| `rfxvmt32.dll`      | Original Stas'M redistributable (x86)          | Win10 x86 install             |
| `rfxvmt64.dll`      | Original Stas'M redistributable (x64)          | Win10 x64 install             |
| `license.txt`       | Repo `LICENSE` file (copy here as text)        | `-l` flag                     |

Binary files in this folder are intentionally excluded from version control
via `.gitignore`. The CI pipeline copies them from the native build output
before invoking `dotnet publish`.
