# Submodule Update Guide

This repository includes one git submodule (with nested sub-submodules):

| Submodule | Upstream | Current tag |
|---|---|---|
| `src-csharp/RDPOffsetFinder` | [llccd/RDPWrapOffsetFinder](https://github.com/llccd/RDPWrapOffsetFinder) | `v0.9` |
| `src-csharp/RDPOffsetFinder/zydis` | [zyantific/zydis](https://github.com/zyantific/zydis) | (pinned by upstream) |
| `src-csharp/RDPOffsetFinder/zydis/dependencies/zycore` | [zyantific/zycore-c](https://github.com/zyantific/zycore-c) | (pinned by upstream) |

The nested `zydis` and `zycore` submodule commits are controlled by llccd's repository — update only the outer submodule and the inner ones follow automatically.

---

## Cloning with submodules

```bash
# Full clone (recommended for builds)
git clone --recurse-submodules https://github.com/sjackson0109/rdpwrap

# If you already cloned without --recurse-submodules:
git submodule update --init --recursive
```

---

## Checking current submodule version

```powershell
git submodule status --recursive
# Expected output (one line per submodule):
#  68da37acab6593c329776644944f55695a131731 src-csharp/RDPOffsetFinder (v0.9)
#  5a68f639e4f01604cc7bfc8d313f583a8137e3d3 src-csharp/RDPOffsetFinder/zydis (...)
#  fb69402566a15a719e5df7a64a3db95105590b7e src-csharp/RDPOffsetFinder/zydis/dependencies/zycore (...)
```

---

## Updating to a new upstream release

> **Do this only when a new tagged release of [llccd/RDPWrapOffsetFinder](https://github.com/llccd/RDPWrapOffsetFinder) is published** and you have verified that the new version produces correct INI sections for a known Windows build.

```powershell
# 1. Fetch all tags from upstream
cd src-csharp/RDPOffsetFinder
git fetch --tags origin

# 2. List available tags to find the new release
git tag --list --sort=-version:refname | Select-Object -First 10

# 3. Check out the desired tag
git checkout <new-tag>        # e.g. v1.0

# 4. Update nested submodules to match the new tag's references
git submodule update --init --recursive

# 5. Return to repo root and record the new pointer
cd ../..
git add src-csharp/RDPOffsetFinder
git commit -m "chore: update RDPOffsetFinder submodule to <new-tag>"
git push
```

---

## Verifying the updated submodule builds

```powershell
# From repo root — builds x64 and x86 offset finder binaries
cd src-csharp/RDPOffsetFinder

cmake -B build-x64 -A x64 .
cmake --build build-x64 --config Release

cmake -B build-x86 -A Win32 .
cmake --build build-x86 --config Release
```

Or trigger the `build-offsetfinder.yml` workflow on your push branch to let CI validate it.

---

## Rollback

If the update causes build failures, revert the submodule pointer:

```powershell
git revert HEAD        # creates a revert commit
git push
```

Or manually:

```powershell
cd src-csharp/RDPOffsetFinder
git checkout v0.9      # previous known-good tag
cd ../..
git add src-csharp/RDPOffsetFinder
git commit -m "chore: rollback RDPOffsetFinder submodule to v0.9"
git push
```
