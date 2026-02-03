# Building the Runtime

Guides for building the different components of dotnet/runtime.

## Quick Start

For most developers, start with the [Getting Started Guide](../getting-started.md).

## By Component

| Component | Guide | Description |
|-----------|-------|-------------|
| **CoreCLR** | [Building CoreCLR](coreclr/README.md) | The primary .NET runtime |
| **Libraries** | [Building Libraries](libraries/README.md) | Base Class Library (BCL) |
| **Mono** | [Building Mono](mono/README.md) | Mobile/WASM/embedded runtime |

## Common Build Commands

```bash
# Build everything
./build.sh

# CoreCLR only
./build.sh clr

# Libraries only
./build.sh libs

# Mono only
./build.sh mono

# Combine subsets
./build.sh clr+libs
```

## Build Configurations

| Configuration | Flag | Use Case |
|---------------|------|----------|
| Debug | `-c Debug` | Development (default) |
| Release | `-c Release` | Performance testing |
| Checked | `-c Checked` | Testing with assertions (CoreCLR) |

Per-component configuration:

```bash
# Release CoreCLR, Debug libraries
./build.sh clr+libs -rc Release -lc Debug
```

## Platform-Specific Builds

| Topic | Guide |
|-------|-------|
| Cross-compilation (CoreCLR) | [Cross-Building CoreCLR](coreclr/cross-building.md) |
| Cross-compilation (Libraries) | [Cross-Building Libraries](libraries/cross-building.md) |
| WebAssembly | [WebAssembly Instructions](libraries/webassembly-instructions.md) |
| Android | [Android Build](coreclr/android.md) |
| iOS | [iOS Build](coreclr/ios.md) |
| FreeBSD | [FreeBSD Build](coreclr/freebsd-instructions.md) |
| NativeAOT | [NativeAOT Build](coreclr/nativeaot.md) |

## Build Help

Get full help on build options:

```bash
./build.sh -h
./build.sh -subset help
```

## Troubleshooting

| Issue | Solution |
|-------|----------|
| Missing prerequisites | Check [Platform Requirements](../platforms/README.md) |
| Out of disk space | Builds need 10-20 GB |
| Build timeout | Full builds take 30-40 minutes |

Build logs are in: `artifacts/log/`
