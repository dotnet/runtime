# Getting Started

This guide helps you set up your development environment and make your first contribution to dotnet/runtime.

## Quick Start (5 minutes to first build)

### 1. Clone the Repository

```bash
git clone https://github.com/dotnet/runtime.git
cd runtime
```

**Note:** The repo requires ~1.5 GB for a clone and 10-20 GB for a build.

### 2. Install Prerequisites

Choose your platform:

| Platform | Guide |
|----------|-------|
| Windows | [Windows Setup](requirements/windows-requirements.md) |
| Linux | [Linux Setup](requirements/linux-requirements.md) |
| macOS | [macOS Setup](requirements/macos-requirements.md) |
| FreeBSD | [FreeBSD Setup](requirements/freebsd-requirements.md) |

### 3. Build the Runtime

From the repository root:

```bash
# Build everything (first time, ~30-40 minutes)
./build.sh        # Linux/macOS
./build.cmd       # Windows
```

For faster iteration, build only what you need:

```bash
# CoreCLR runtime only
./build.sh -subset clr

# Libraries only (requires runtime built first)
./build.sh -subset libs

# Mono runtime only
./build.sh -subset mono
```

### 4. Verify Your Build

```bash
# Set up the dotnet CLI from your build
export PATH="$(pwd)/.dotnet:$PATH"  # Linux/macOS
set PATH=%CD%\.dotnet;%PATH%        # Windows

dotnet --version
```

## What's Next?

### Choose Your Component

| Component | Best For | Guide |
|-----------|----------|-------|
| **Libraries** | Most contributors - BCL, networking, IO, collections | [Libraries Guide](components/libraries.md) |
| **CoreCLR** | JIT, GC, type system, interop | [CoreCLR Guide](components/coreclr.md) |
| **Mono** | Mobile, WebAssembly, embedded | [Mono Guide](components/mono.md) |
| **Host** | App startup, SDK integration | [Host Guide](components/host.md) |

### Common Workflows

- **Edit and test a library**: See [Libraries Guide](components/libraries.md)
- **Debug the runtime**: See [Debugging Guide](debugging/README.md)
- **Run tests**: See [Testing Guide](testing/README.md)
- **Submit a PR**: See [PR Guide](ci/pr-guide.md)

## Development Environments

| Environment | Description |
|-------------|-------------|
| [GitHub Codespaces](environments/codespaces.md) | Cloud-based, pre-configured environment |
| [Docker](environments/docker.md) | Containerized builds for cross-compilation |
| [Visual Studio](editing-and-debugging.md) | Full IDE experience on Windows |
| [VS Code](debugging/libraries/debugging-vscode.md) | Lightweight, cross-platform |

## Build Configurations

| Configuration | Use Case |
|---------------|----------|
| **Debug** | Development and debugging (default) |
| **Release** | Performance testing |
| **Checked** | Testing with assertions (CoreCLR only) |

Specify configuration with `-c` or `-configuration`:

```bash
./build.sh -subset clr -c Release
```

Use different configurations per component:

```bash
# Release runtime, Debug libraries (common for library work)
./build.sh -subset clr+libs -rc Release -lc Debug
```

## Troubleshooting

### Build Fails Immediately

- Ensure all [prerequisites](#2-install-prerequisites) are installed
- On Windows, run from a Developer Command Prompt
- Check available disk space (need 10-20 GB)

### "testhost" or "shared framework" Errors

Run a full baseline build first:

```bash
./build.sh clr+libs -rc release
```

### Need More Help?

- Review [build logs](../project/glossary.md) in `artifacts/log/`
- Check [CI failure analysis](ci/failure-analysis.md) for known issues
- Ask in [GitHub Discussions](https://github.com/dotnet/runtime/discussions)

## Key Concepts

### Subsets

The build is organized into subsets you can build independently:

| Subset | Description |
|--------|-------------|
| `clr` | CoreCLR runtime + System.Private.CoreLib |
| `mono` | Mono runtime + System.Private.CoreLib |
| `libs` | All libraries |
| `host` | .NET hosts and installers |
| `packs` | Shipping packages |

Combine with `+`: `./build.sh -subset clr+libs`

### Build vs. Target Platform

- **Build Platform**: Where you're running the build (your machine)
- **Target Platform**: What you're building for (may be different for cross-compilation)

See [Cross-Compilation](building/coreclr/cross-building.md) for building for other platforms.
