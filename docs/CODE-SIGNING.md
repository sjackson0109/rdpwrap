# Code Signing Guide

Both `build-and-release.yml` and `build-csharp.yml` include a code-signing step that is already wired up and waiting. The step fires automatically as soon as the two secrets below are present in the repository — no workflow edits are needed.

---

## What gets signed

All six framework-dependent executables and all six self-contained executables produced per release:

| File | Contents |
|---|---|
| `RDPWInst_x64.exe`, `RDPWInst_x86.exe`, `RDPWInst_arm64.exe` | CLI installer |
| `RDPConf_x64.exe`, `RDPConf_x86.exe`, `RDPConf_arm64.exe` | GUI configuration tool |
| `RDPCheck_x64.exe`, `RDPCheck_x86.exe`, `RDPCheck_arm64.exe` | GUI connection tester |
| `*_sc.exe` variants | Self-contained copies of the above |

The signing step runs on every `build-and-release.yml` and `build-csharp.yml` triggered build (tag push and manual dispatch), and is **skipped silently** when the secrets are absent.

---

## Obtaining a code-signing certificate

### Option A — Commercial certificate (recommended for public distribution)

Purchase an **EV (Extended Validation) Code Signing Certificate** from a trusted CA:

- [DigiCert Code Signing](https://www.digicert.com/signing/code-signing-certificates)  
- [Sectigo (Comodo)](https://sectigo.com/ssl-certificates-tls/code-signing)  
- [GlobalSign](https://www.globalsign.com/en/code-signing-certificate/)

> **Windows SmartScreen** initially blocks unsigned or low-reputation executables. A commercial EV certificate builds reputation immediately; a standard OV certificate requires time to accumulate reputation through user downloads. Self-signed certificates (Option B) will always trigger SmartScreen warnings.

### Option B — Self-signed certificate (testing / internal use only)

```powershell
# Run in an elevated PowerShell session
$cert = New-SelfSignedCertificate `
    -Type CodeSigningCert `
    -Subject "CN=RDP Wrapper" `
    -KeySpec Signature `
    -KeyAlgorithm RSA `
    -KeyLength 4096 `
    -HashAlgorithm SHA256 `
    -CertStoreLocation "Cert:\CurrentUser\My" `
    -NotAfter (Get-Date).AddYears(3)

# Export to PFX (set a strong password)
$pw = Read-Host "PFX password" -AsSecureString
Export-PfxCertificate -Cert $cert -FilePath "rdpwrap-codesign.pfx" -Password $pw
```

---

## Preparing the PFX for GitHub Actions

```powershell
# Base64-encode the PFX so it can be stored as a secret
$b64 = [Convert]::ToBase64String([IO.File]::ReadAllBytes(".\rdpwrap-codesign.pfx"))
$b64 | Set-Clipboard
Write-Host "Base64 PFX copied to clipboard"
```

---

## Adding the secrets to GitHub

Navigate to: **Settings → Secrets and variables → Actions → New repository secret**

| Secret name | Value |
|---|---|
| `CODESIGN_CERT_BASE64` | Paste the base64-encoded PFX string |
| `CODESIGN_CERT_PASSWORD` | The password chosen when exporting the PFX |

> ⚠️ **Security:** Never commit the `.pfx` file or raw base64 string to the repository. Revoke and reissue the certificate if it is accidentally exposed.

---

## Verifying a signed executable

After a signed release is published:

```powershell
# Check signature status
Get-AuthenticodeSignature .\RDPWInst_x64.exe | Format-List

# Expected output (commercial cert):
# Status     : Valid
# SignerCertificate : [CN=Your Name, O=Your Org, ...]
# TimeStamperCertificate : [CN=DigiCert Timestamp 2023, ...]
```

---

## Renewing / rotating the certificate

1. Export the new PFX and base64-encode it (see above).
2. Update both `CODESIGN_CERT_BASE64` and `CODESIGN_CERT_PASSWORD` via **Settings → Secrets**.
3. No workflow changes are needed — the `Sign C# executables` step always reads from secrets at runtime.
4. Remove the old PFX from your local machine and revoke the old certificate with the CA if it has not expired.
