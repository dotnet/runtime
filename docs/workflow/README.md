# Workflow Guide

Your guide to building, testing, and contributing to dotnet/runtime.

## 🚀 Start Here

| I want to... | Go to... |
|--------------|----------|
| **Get started** (new contributor) | [Getting Started Guide](getting-started.md) |
| **Work on Libraries** (most common) | [Libraries Guide](components/libraries.md) |
| **Work on CoreCLR** | [CoreCLR Guide](components/coreclr.md) |
| **Work on Mono** | [Mono Guide](components/mono.md) |
| **Work on Host** | [Host Guide](components/host.md) |
| **Submit a PR** | [PR Guide](ci/pr-guide.md) |

## 📚 Documentation Index

### Setup & Environment

| Topic | Description |
|-------|-------------|
| [Getting Started](getting-started.md) | First-time setup and first build |
| [Platform Requirements](platforms/README.md) | OS-specific prerequisites |
| [Development Environments](environments/README.md) | Codespaces, Docker, IDE setup |

### Building

| Topic | Description |
|-------|-------------|
| [Building Overview](building/README.md) | Build commands and configurations |
| [Building CoreCLR](building/coreclr/README.md) | CoreCLR-specific build details |
| [Building Libraries](building/libraries/README.md) | Libraries-specific build details |
| [Building Mono](building/mono/README.md) | Mono-specific build details |

### Testing

| Topic | Description |
|-------|-------------|
| [Testing Overview](testing/README.md) | Test commands and filtering |
| [Testing CoreCLR](testing/coreclr/testing.md) | Runtime tests |
| [Testing Libraries](testing/libraries/testing.md) | Library unit tests |
| [Testing Mono](testing/mono/testing.md) | Mono-specific tests |
| [Testing Host](testing/host/testing.md) | Host activation tests |

### Debugging

| Topic | Description |
|-------|-------------|
| [Debugging Overview](debugging/README.md) | Debugging tools and setup |
| [Debugging CoreCLR](debugging/coreclr/debugging-runtime.md) | Native runtime debugging |
| [Debugging Libraries](debugging/libraries/) | Managed code debugging |
| [Editing and Debugging](editing-and-debugging.md) | Visual Studio setup |

### CI & Contributing

| Topic | Description |
|-------|-------------|
| [CI Overview](ci/README.md) | CI system and workflows |
| [PR Guide](ci/pr-guide.md) | Creating and managing PRs |
| [Failure Analysis](ci/failure-analysis.md) | Investigating CI failures |

### Specialized Topics

| Topic | Description |
|-------|-------------|
| [WebAssembly](wasm-documentation.md) | WASM development guide |
| [NativeAOT](building/coreclr/nativeaot.md) | Ahead-of-time compilation |
| [Trimming](trimming/) | IL trimming configuration |

## Quick Reference

### Build Commands

```bash
# Full build (first time)
./build.sh                    # Linux/macOS
./build.cmd                   # Windows

# Component builds
./build.sh clr                # CoreCLR only
./build.sh libs               # Libraries only
./build.sh mono               # Mono only
./build.sh clr+libs           # CoreCLR + Libraries

# With configuration
./build.sh clr+libs -rc Release -lc Debug
```

### Test Commands

```bash
# Library tests
cd src/libraries/System.Foo/tests
dotnet build /t:Test

# CoreCLR tests
./src/tests/build.sh generatelayoutonly
./src/tests/run.sh
```

### Build Configurations

| Configuration | Flag | Description |
|---------------|------|-------------|
| Debug | `-c Debug` | Development, full debugging (default) |
| Release | `-c Release` | Optimized, performance testing |
| Checked | `-c Checked` | Optimized with assertions (CoreCLR only) |

### Key Subsets

| Subset | Description |
|--------|-------------|
| `clr` | CoreCLR runtime + CoreLib |
| `mono` | Mono runtime + CoreLib |
| `libs` | All libraries |
| `host` | .NET host and installers |
| `packs` | Shipping packages |

## Performance

- [Benchmarking Workflow](https://github.com/dotnet/performance/blob/master/docs/benchmarking-workflow-dotnet-runtime.md)
- [Profiling Workflow](https://github.com/dotnet/performance/blob/master/docs/profiling-workflow-dotnet-runtime.md)

## Tips

### Disable Warnings-as-Errors

```bash
export TreatWarningsAsErrors=false   # Linux/macOS
set TreatWarningsAsErrors=false      # Windows
```

### Get Help

```bash
./build.sh -h                 # Build help
./build.sh -subset help       # List all subsets
```

## See Also

- [Project Glossary](../project/glossary.md) - Terms and acronyms
- [Contribution Guidelines](/CONTRIBUTING.md) - How to contribute
- [GitHub Discussions](https://github.com/dotnet/runtime/discussions) - Ask questions
