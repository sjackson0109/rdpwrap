# Screenshots — `docs/images/`

This directory holds in-repo screenshot assets referenced by the project README.

---

## Files present

The following PNG screenshots are committed and referenced by the README:

| Filename | Contents |
|---|---|
| `RDPWrapperConfig.png` | RDPConf.exe configuration window |
| `RDPWrapperCheck.png` | RDPCheck.exe showing green "Supported" status |
| `RDPWrapperCheckWarning.png` | RDPCheck.exe showing "Warning" / not-yet-supported state |
| `RDPWrapperMSI1.png` | MSI installer welcome / licence screen |
| `RDPWrapperMSI2.png` | MSI installer completion screen |

---

## Files that would still be welcome

The following screenshots would improve documentation coverage but are not blocking any CI step:

| Filename | What to capture |
|---|---|
| `RDPWrapperConfig-advanced.png` | RDPConf.exe Advanced / License tab — SL policy and licensing status fields |
| `rdpwrapper-gui.png` | sergiye/rdpWrapper GUI — main window with connection status |
| `install-success.png` | Terminal output of `RDPWInst_x64.exe -i -o` completing successfully |

---

## Capture tips

- Use **1:1 scaling** (100 % DPI) so button labels are legible at small display size.
- Crop to exclude desktop wallpaper — show only the tool window, with a 4 px neutral border all round.
- Use PNG format (lossless); aim for < 200 KB each (resize if needed — 900 px wide is more than enough).
- Redact any username, hostname, or licence strings visible in the window before committing.
- Filename convention: `tool-state.png` — all lowercase, hyphen-delimited.

---

## Adding a new screenshot

```bash
# After copying the PNG here:
git add docs/images/<filename>.png
git commit -m "docs: add <description> screenshot"
```

The README references each file with a relative path, so the image will render automatically on GitHub once committed.
