
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

### Installer Tests

All builds are

- Windows 10 x64 Client VS2019
  - x64 Release
  - Cross x86 Release
  - Cross arm32 Release
  - Cross arm64 Release
- Ubuntu 18.04 x64
  - Linux x64 Release
  - Cross Linux arm64 Release
  - Cross Linux arm64 musl Release
  - Cross Linux arm32 Release
  - Cross Linux arm32 musl Release
  - Cross FreeBSD x64 (build only)
- MacOS 11.6.4 x64
  - x64 Release
  - arm64 Release

## Rolling Builds