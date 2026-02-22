# Debugging the Runtime

Guides for debugging the different components of dotnet/runtime.

## By Component

| Component | Guide | Description |
|-----------|-------|-------------|
| **CoreCLR** | [Debugging CoreCLR](coreclr/debugging-runtime.md) | Native runtime debugging |
| **Libraries** | [Debugging Libraries](libraries/) | Managed code debugging |
| **Mono** | [Debugging Mono](mono/) | Mono-specific debugging |

## Quick Start by Platform

### Windows

**Visual Studio** is recommended for both native and managed debugging.

- [Debugging CoreCLR in VS](coreclr/debugging-runtime.md#using-visual-studio)
- [Debugging Libraries in VS](libraries/windows-instructions.md)

### Linux/macOS

**LLDB** for native code, **VS Code** for managed code.

- [Debugging with LLDB](coreclr/debugging-runtime.md#debugging-coreclr-with-lldb)
- [Debugging with VS Code](libraries/debugging-vscode.md)

## Common Scenarios

### Debugging a Library Test

```bash
# In VS Code or Visual Studio
# 1. Open the test project
# 2. Set breakpoint
# 3. Debug test
```

See [Debugging Libraries](libraries/) for detailed setup.

### Debugging the Runtime (Native)

Windows with Visual Studio:
```cmd
build.cmd -vs CoreCLR.slnx -a x64 -c Debug
```

Linux/macOS with LLDB:
```bash
lldb -- /path/to/corerun app.dll
```

See [Debugging CoreCLR](coreclr/debugging-runtime.md).

### Debugging System.Private.CoreLib

See [Debugging CoreLib](libraries/debugging-corelib.md).

## Specialized Debugging

| Topic | Guide |
|-------|-------|
| AOT compilers | [Debugging AOT Compilers](coreclr/debugging-aot-compilers.md) |
| Compiler dependency analysis | [Dependency Analysis](coreclr/debugging-compiler-dependency-analysis.md) |
| WebAssembly | [WASM Debugging](mono/wasm-debugging.md) |
| Android | [Android Debugging](mono/android-debugging.md) |

## Tools

### SOS Debugger Extension

SOS provides .NET-specific debugging commands.

- [Installing SOS](https://github.com/dotnet/diagnostics)
- [SOS Commands](https://github.com/dotnet/diagnostics/blob/master/documentation/sos-debugging-extension-windows.md)

Common commands:
- `clrstack` - Managed call stack
- `dumpobj` - Object contents
- `VerifyHeap` - GC heap verification

### PerfView

For performance analysis and profiling.

- [PerfView Tutorial](https://github.com/Microsoft/perfview/blob/main/documentation/Downloading.md)
- Example usage: See [debugging libraries](libraries/perfview_example.gif)

## Troubleshooting

### Visual Studio Signature Validation Error (VS 2022 17.5+)

When debugging local builds, you may see signature validation errors.

**Quick fix:** Set environment variable before launching VS:
```cmd
set VSDebugger_ValidateDotnetDebugLibSignatures=0
devenv.exe
```

See [full details](coreclr/debugging-runtime.md#resolving-signature-validation-errors-in-visual-studio).

### Symbols Not Loading

- Ensure you built in Debug configuration
- On macOS, use `-DCLR_CMAKE_APPLE_DYSM=TRUE` for `.dSYM` bundles
- Load SOS manually if needed: `plugin load /path/to/libsosplugin.so`
