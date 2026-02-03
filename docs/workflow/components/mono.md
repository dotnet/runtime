# Mono Development Guide

This guide covers everything you need to build, test, and debug the Mono runtime - optimized for mobile, WebAssembly, and embedded scenarios.

**Source location:** `src/mono/`

## Quick Reference

| Task | Command |
|------|---------|
| Build Mono + Libraries | `./build.sh mono+libs` |
| Build Mono only | `./build.sh mono` |
| Run library tests on Mono | `dotnet build /t:Test /p:RuntimeFlavor=mono` |

## Building

### First-Time Setup

Build Mono with the libraries it needs:

```bash
./build.sh mono+libs
```

### Building Mono Only

After the initial build, iterate on just Mono:

```bash
./build.sh mono
```

Skip package restore for faster builds:

```bash
./build.sh mono --build
```

### Build for Testing

To run library tests with your Mono changes:

```bash
./build.sh mono+libs.pretest
```

### Build Outputs

Binaries are placed in: `artifacts/bin/mono/<OS>.<Arch>.<Config>/`

### Useful Build Arguments

| Argument | Description |
|----------|-------------|
| `/p:MonoEnableLLVM=true` | Build with LLVM JIT |
| `/p:MonoLLVMDir=path` | Use custom LLVM build |
| `/p:DisableCrossgen=true` | Skip installer build (faster) |
| `/p:KeepNativeSymbols=true` | Keep symbols for debugging |

Example with LLVM:

```bash
./build.sh mono /p:MonoEnableLLVM=true
```

## Testing

### Library Tests on Desktop Mono

```bash
cd src/libraries/System.Foo/tests
dotnet build /t:Test /p:RuntimeFlavor=mono
```

Or use the Makefile:

```bash
cd src/mono
make run-tests-corefx-System.Runtime
```

### Runtime Tests on Desktop Mono

1. Build the test host:
   ```bash
   ./build.sh clr.hosts -c Release
   ```

2. Build the tests:
   ```bash
   cd src/tests
   ./build.sh mono release
   ```

3. Run all tests:
   ```bash
   ./run.sh Release
   ```

4. Run a single test:
   ```bash
   ./artifacts/tests/coreclr/<OS>.<Arch>.Release/JIT/Test/Test.sh \
     -coreroot=$(pwd)/artifacts/tests/coreclr/<OS>.<Arch>.Release/Tests/Core_Root
   ```

### Debugging Tests with LLDB

```bash
./artifacts/tests/coreclr/<OS>.<Arch>.Release/JIT/Test/Test.sh \
  -coreroot=$(pwd)/artifacts/tests/coreclr/<OS>.<Arch>.Release/Tests/Core_Root \
  -debug=/usr/bin/lldb
```

In LLDB, add Mono symbols:

```
add-dsym <CORE_ROOT>/libcoreclr.dylib.dwarf
```

## Platform-Specific Development

### WebAssembly

Build for browser:

```bash
./build.sh mono+libs -os browser
```

Run tests:

```bash
./build.sh libs.tests -test -os browser
```

See [WebAssembly Documentation](wasm-documentation.md) for complete details.

### Android

Build:

```bash
./src/tests/build.sh mono -os android -arch arm64
```

See [Testing on Android](testing/libraries/testing-android.md).

### iOS

Build:

```bash
./src/tests/build.sh mono -os ios -arch arm64
```

See [Testing on Apple](testing/libraries/testing-apple.md).

## Debugging

### VS Code on Mono

See [Debugging with VS Code on Mono](debugging/libraries/debugging-vscode.md#debugging-libraries-with-visual-studio-code-running-on-mono).

### WebAssembly Debugging

See [WebAssembly Debugging](debugging/mono/wasm-debugging.md).

### Android Debugging

See [Android Debugging](debugging/mono/android-debugging.md).

## Samples

Quick test samples are in `src/mono/sample/`:

### Desktop Sample

```bash
cd src/mono/sample/HelloWorld
make run
```

### WebAssembly Sample

```bash
cd src/mono/sample/wasm/browser  # or console
make build && make run
```

### Android Sample

```bash
cd src/mono/sample/Android
make run
```

### iOS Sample

```bash
cd src/mono/sample/iOS
make run
```

## Building Packages

```bash
./build.sh packs -runtimeFlavor mono -c Release
```

Packages are placed in: `artifacts/packages/<Config>/Shipping/`

## Deep Dives

| Topic | Link |
|-------|------|
| Building details | [Building Mono](building/mono/README.md) |
| Testing details | [Testing Mono](testing/mono/testing.md) |
| WebAssembly | [WebAssembly Documentation](wasm-documentation.md) |
| Browser internals | [Browser README](../../src/mono/browser/README.md) |
| WASI support | [WASI README](../../src/mono/wasi/README.md) |
