
# Test Configurations

This document lists the configurations (platforms and settings) that we expect the various runtime components to be tested in, and how often changes are tested.

## In pull requests

These configurations are tested in every pull request.

### CLR Tests

- Windows 11 Client
  - x86 checked
  - x64 checked
  - arm64 checked
- Ubuntu 18.04 Open
  - x64 checked
  - arm64 checked
  - arm32 checked

### Mono Tests

### Libraries Tests

#### On coreclr change

| **Platform**     | **Runtime config** | **Libraries config** |
|------------------|--------------------|----------------------|
| windows-x86      | Checked            | Debug                |
| linux-musl-x64   | Checked            | Debug                |
| OSX-x64          | Checked            | Debug                |
| linux-arm64      | Checked            | Debug                |
| linux-musl-arm   | Checked            | Release              |
| linux-musl-arm64 | Checked            | Release              |
| linux-x64        | Checked            | Release              |
| windows-x64      | Checked            | Release              |

### Installer Tests

All builds are

- Windows 10 x64 Client VS2019
  - x64 Release
  - x86 Release
  - arm32 Release
  - arm64 Release
- Ubuntu 18.04 x64
  - Linux x64 Release
  - Linux arm64 Release
  - Linux arm64 musl Release
  - Linux arm32 Release
  - Linux arm32 musl Release
  - FreeBSD x64 (build only)
- MacOS 11.6.4 x64
  - x64 Release
  - arm64 Release

## Rolling Builds
