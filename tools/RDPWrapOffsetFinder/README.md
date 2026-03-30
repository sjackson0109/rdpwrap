# RDPWrapOffsetFinder

Pre-built binaries of [llccd/RDPWrapOffsetFinder](https://github.com/llccd/RDPWrapOffsetFinder),
committed here so that the CI pipeline and releases are self-contained and
reproducible without depending on an external release being available at build time.

## Contents

```
x64/
    RDPWrapOffsetFinder.exe   # x86-64 build
    Zydis.dll                 # required runtime (x64)
x86/
    RDPWrapOffsetFinder.exe   # x86 32-bit build
    Zydis.dll                 # required runtime (x86)
VERSION                       # version tag of the upstream release
```

## Usage

Extract the appropriate arch folder and run:

```
.\RDPWrapOffsetFinder.exe C:\Windows\System32\termsrv.dll
```

The output `[10.0.xxxxx.xxxxx]` section can be appended to `res/rdpwrap.ini`
and submitted as a pull request.

## Updating

To update the binaries when llccd releases a new version, trigger the
`Update RDPWrapOffsetFinder tools` workflow from the Actions tab
(`.github/workflows/update-finder-tools.yml`).
It will download the latest release, update this folder, and open a pull request.
