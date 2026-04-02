# TODO

Items ordered by priority.

---

## High priority

- [x] **Add `concurrency:` guard to `build-and-release.yml`** — two rapid pushes to `main` (e.g. a merge immediately followed by a Dependabot merge) will race and both attempt to create a release, corrupting or duplicating assets. Add:
  ```yaml
  concurrency:
    group: release
    cancel-in-progress: true
  ```
  at the top-level of the workflow so only one release job runs at a time.

- [x] **Add `msi/**` to `build-and-release.yml` path filter** — changes to `msi/RDPWInst.wxs`, `msi/RDPWInst.wixproj`, or `msi/global.json` currently do not trigger a release. A WiX fix merged to `main` would silently produce no new release. Add `'msi/**'` to the `paths:` list.

- [x] **Add NuGet / dotnet package cache** — every `build-and-release.yml` and `build-csharp.yml` run re-downloads all NuGet packages from scratch (~30–60 s penalty per run). Add an `actions/cache` step keyed on `**/packages.lock.json` or the project files hash before the `dotnet publish` steps to restore/save the `~/.nuget/packages` directory.

- [x] **Add PR check for MSI build** — there is no CI validation that `msi/RDPWInst.wxs` / `msi/RDPWInst.wixproj` compiles when a PR changes them, only at release time. Create a lightweight `build-msi-check.yml` (or add a `pull_request` trigger to cover `msi/**`) that builds the WiX project without publishing a release.

- [ ] **Code-sign release binaries** — set repository variable `USE_CERT_SIGNING=true` (Settings → Variables → Actions) and add `CODESIGN_CERT_BASE64` (PFX as base64) and `CODESIGN_CERT_PASSWORD` as repository secrets; both CI workflows with signing steps (`build-and-release.yml`, `build-csharp.yml`) already have the signing step wired up, gated on `vars.USE_CERT_SIGNING == 'true'`. See [`docs/CODE-SIGNING.md`](docs/CODE-SIGNING.md) for the full certificate acquisition, PFX export, and secret upload procedure.

---

## Medium priority

- [x] **Add `CODEOWNERS` file** — create `.github/CODEOWNERS` mapping `src-x86-x64-Fusix/` and `src-csharp/` to `@sjackson0109` so PRs automatically request review from the maintainer. Optionally require approval before merging via branch protection rules.

- [x] **Dynamic version in banner** — `Program.cs` banner hardcodes `"v1.6.2"`. Replace with a runtime read of the assembly version so released binaries automatically display the correct `yyyy.M.d` stamp:
  ```csharp
  var v = Assembly.GetExecutingAssembly().GetName().Version;
  string version = v is null ? "unknown" : $"{v.Major}.{v.Minor}.{v.Build}";
  ```

- [x] **Update `Directory.Build.props` default version** — the fallback `<Version>2026.3.30</Version>` is already stale and will mislead developers who build locally without passing `/p:Version=`. Either update it to the current date periodically, or derive it dynamically:
  ```xml
  <Version>$([System.DateTime]::Now.ToString("yyyy.M.d"))</Version>
  ```

- [x] **Split `build-and-release.yml` into parallel jobs** — the ~500-line single job runs everything sequentially (DLL builds → C# publishes → self-contained publishes → OffsetFinder → sergiye download → MSI → release). Split into 6 jobs: `build-dll`, `build-offsetfinder`, `download-sergiye` (all parallel), then `build-csharp` (waits for DLLs), `build-msi` (waits for C#), and `release` (waits for all). Makes failures easy to identify at a glance.

- [x] **Pin `softprops/action-gh-release` to a SHA** — Dependabot covers `actions/*` and NuGet packages but not third-party actions like `softprops/action-gh-release@v2`. Pinned to `153bb8e04406b158c6c84fc1615b65b24149a1fe` (v2) with `# v2` comment so Dependabot can track it via the existing `actions-minor` group.

- [ ] **Add in-repo screenshots** — `docs/images/` directory and README scaffold are in place; five PNGs are committed but three additional shots would improve coverage. Capture the files described in [`docs/images/README.md`](docs/images/README.md) on a Windows 10/11 machine with a working install and commit them.

---

## Low priority

- [x] **Add a GitHub Environment for releases** — configured `environment: release` on the `release` job in `build-and-release.yml`. The environment is created automatically if absent (no gates). To require a reviewer: Settings → Environments → release → Required reviewers → add `@sjackson0109`.

- [x] **Dependabot for submodules** — `dependabot.yml` covers `github-actions` and `nuget` but not git submodules (`src-csharp/RDPOffsetFinder` / `zydis`). Added a `gitsubmodule` ecosystem entry (Dependabot beta); activate once the feature is publicly available or monitor submodule versions manually.

- [x] **Add `packages.lock.json` for reproducible NuGet restores** — enabled `<RestorePackagesWithLockFile>true</RestorePackagesWithLockFile>` in `Directory.Build.props`. Run `dotnet restore` locally in `src-csharp/` and commit the generated `packages.lock.json` files so CI restores become deterministic.

- [x] **Lint `msi/rdpwrap.ini` in CI** — the existing INI validation step in `build-and-release.yml` checks for three required sections. Extended to also parse every `[x.x.xxxxx.xxxxx]` section and assert it contains `LocalOnlyPatch` and `SLInitHook`, reporting all failures at once before aborting.
