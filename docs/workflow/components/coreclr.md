# CoreCLR Development Guide

This guide covers everything you need to build, test, and debug CoreCLR - the primary .NET runtime.

**Source location:** `src/coreclr/`

## Quick Reference

| Task | Command |
|------|---------|
| Build CoreCLR | `./build.sh clr` |
| Build CoreCLR + Libraries | `./build.sh clr+libs` |
| Build Core_Root for testing | `./src/tests/build.sh generatelayoutonly` |
| Run all CoreCLR tests | `./src/tests/run.sh` |

## Building

### Basic Build

```bash
./build.sh -subset clr
```

This builds:
- The CoreCLR runtime (`libcoreclr.so`/`coreclr.dll`)
- System.Private.CoreLib
- The `corerun` host executable

### Build Configurations

| Configuration | Use Case | Flag |
|---------------|----------|------|
| **Debug** | Development (default) | `-c Debug` |
| **Checked** | Testing with assertions | `-c Checked` |
| **Release** | Performance testing | `-c Release` |

**Recommendation:** Use Checked for running tests (faster than Debug, keeps assertions).

### Build Outputs

Binaries are placed in: `artifacts/bin/coreclr/<OS>.<Arch>.<Config>/`

Key files:
- `corerun` / `corerun.exe` - Host executable
- `libcoreclr.so` / `coreclr.dll` - The runtime
- `System.Private.CoreLib.dll` - Core managed library

### Building for Testing

To run CoreCLR tests, you need both the runtime and libraries:

```bash
# Build runtime (Checked) + libraries (Release)
./build.sh clr+libs -rc Checked -lc Release
```

Then generate the Core_Root (test execution environment):

```bash
./src/tests/build.sh generatelayoutonly
```

Core_Root location: `artifacts/tests/coreclr/<OS>.<Arch>.<Config>/Tests/Core_Root`

## Testing

### Building Tests

```bash
cd src/tests
./build.sh checked        # Builds tests for Checked runtime
```

Build specific tests:

```bash
# Single test
./build.sh -test:JIT/Methodical/Test.csproj

# Directory of tests
./build.sh -dir:JIT/Methodical

# Subtree of tests
./build.sh -tree:JIT
```

### Running Tests

Run all tests:

```bash
./src/tests/run.sh checked
```

Run a single test:

```bash
export CORE_ROOT=/path/to/Core_Root
./artifacts/tests/coreclr/<OS>.<Arch>.<Config>/JIT/Test/Test.sh
```

### Test Priorities

By default, only Priority 0 tests build. Include Priority 1:

```bash
./src/tests/build.sh -priority=1
```

### Test Results

- HTML report: `artifacts/log/TestRun_<Arch>_<Config>.html`
- Failed tests: `artifacts/log/TestRunResults_<OS>_<Arch>_<Config>.err`
- Individual results: `artifacts/tests/coreclr/<OS>.<Arch>.<Config>/Reports/`

## Debugging

### Visual Studio (Windows)

1. Build with the `-msbuild` flag or run:
   ```cmd
   build.cmd -vs CoreCLR.slnx -a x64 -c Debug
   ```
2. Set INSTALL as startup project
3. Configure debugging properties:
   - Command: `$(SolutionDir)\..\..\..\bin\coreclr\windows.$(Platform).$(Configuration)\corerun.exe`
   - Arguments: `YourApp.dll`
   - Working Directory: Path to Core_Root
   - Environment: `CORE_LIBRARIES=<path to libraries>`

### LLDB (Linux/macOS)

```bash
lldb -- /path/to/corerun /path/to/app.dll

# In LLDB:
process handle -s false SIGUSR1
breakpoint set -n coreclr_execute_assembly
process launch -s
```

For detailed debugging instructions, see [Debugging CoreCLR](debugging/coreclr/debugging-runtime.md).

### Debugging with SOS

SOS is the debugger extension for .NET. Install it from the [diagnostics repo](https://github.com/dotnet/diagnostics).

Common commands:
- `clrstack` - Show managed call stack
- `dumpobj` - Dump object contents
- `VerifyHeap` - Verify GC heap integrity

### macOS Debug Symbols

For better LLDB experience on macOS, generate `.dSYM` bundles:

```bash
./build.sh clr -cmakeargs "-DCLR_CMAKE_APPLE_DYSM=TRUE"
```

## Common Tasks

### Rebuilding Just CoreLib

```bash
./build.sh clr.corelib+clr.nativecorelib
```

### Cross-Compilation

| From | To | Supported |
|------|-----|-----------|
| Windows x64 | x86, Arm64 | ✓ |
| Linux x64 | Arm32, Arm64 | ✓ |
| macOS x64/Arm64 | Arm64/x64 | ✓ |

See [Cross-Building CoreCLR](building/coreclr/cross-building.md).

### Using Native Sanitizers

```bash
./build.sh clr -fsanitize address
```

### Building with Different Build Drivers

```bash
# Use MSBuild on Windows (instead of Ninja)
./build.cmd clr -msbuild

# Use Ninja on Linux/macOS (instead of Make)
./build.sh clr -ninja
```

## Specialized Builds

| Target | Documentation |
|--------|---------------|
| NativeAOT | [NativeAOT Guide](building/coreclr/nativeaot.md) |
| WebAssembly | [WASM Guide](building/coreclr/wasm.md) |
| Android | [Android Guide](building/coreclr/android.md) |
| iOS | [iOS Guide](building/coreclr/ios.md) |
| FreeBSD | [FreeBSD Guide](building/coreclr/freebsd-instructions.md) |

## Deep Dives

| Topic | Link |
|-------|------|
| Building details | [Building CoreCLR](building/coreclr/README.md) |
| Testing details | [Testing CoreCLR](testing/coreclr/testing.md) |
| Debugging details | [Debugging CoreCLR](debugging/coreclr/debugging-runtime.md) |
| JIT debugging | [Debugging AOT Compilers](debugging/coreclr/debugging-aot-compilers.md) |
| PAL tests (Unix) | [Testing Guide](testing/coreclr/testing.md#pal-tests-macos-and-linux-only) |
