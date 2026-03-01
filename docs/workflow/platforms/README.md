# Platform Requirements

Set up your development environment for building dotnet/runtime.

## Supported Platforms

| Platform | Architectures | Guide |
|----------|--------------|-------|
| **Windows** | x64, x86, Arm64 | [Windows Setup](../requirements/windows-requirements.md) |
| **Linux** | x64, Arm32, Arm64 | [Linux Setup](../requirements/linux-requirements.md) |
| **macOS** | x64, Arm64 | [macOS Setup](../requirements/macos-requirements.md) |
| **FreeBSD** | x64 | [FreeBSD Setup](../requirements/freebsd-requirements.md) |

## Quick Summary

### Windows

- Visual Studio 2022 (17.0+) with C++ workload
- Windows SDK (10.0.20348.0+)
- CMake 3.20+

### Linux

- Clang 16+ or GCC 13+
- CMake 3.20+
- Various dev packages (see full guide)

### macOS

- Xcode 14.3+ or Command Line Tools
- CMake 3.20+

## Cross-Compilation

You can build for platforms different from your host machine:

| Host | Can Target |
|------|------------|
| Windows x64 | Windows x86, Windows Arm64 |
| Linux x64 | Linux Arm32, Linux Arm64, Alpine, Android |
| macOS x64 | macOS Arm64 |
| macOS Arm64 | macOS x64 |

See [Using Docker](../environments/docker.md) for cross-compilation using containers.

## Alternative Environments

| Environment | Description |
|-------------|-------------|
| [GitHub Codespaces](../environments/codespaces.md) | Cloud-based, pre-configured |
| [Docker](../environments/docker.md) | Containerized builds |
| [WSL](../requirements/linux-requirements.md) | Linux on Windows |
