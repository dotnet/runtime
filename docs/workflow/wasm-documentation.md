# WebAssembly Documentation

This document serves as a guide for contributors to the WebAssembly implementation in the dotnet/runtime repository. It provides links and references to technical documentation, workflows, and resources relevant to developing and maintaining WebAssembly support within this codebase.

## Table of Contents

- [Getting Started](#getting-started)
- [Building for WebAssembly](#building-for-webassembly)
- [Testing and Debugging](#testing-and-debugging)
- [Features and Configuration](#features-and-configuration)
- [Deployment and Hosting](#deployment-and-hosting)
- [Advanced Topics](#advanced-topics)
- [FAQ](#faq)
- [Contributing](#contributing)

## Getting Started

### What is .NET WebAssembly?
.NET WebAssembly allows you to run .NET applications in web browsers and other WebAssembly-compatible environments. The runtime uses the Mono runtime to execute .NET bytecode compiled to WebAssembly.

### Supported Environments
- **Browser (browser-wasm)**: Run .NET applications in web browsers
- **WASI (wasi-wasm)**: Run .NET applications in WASI-compatible environments

## Building for WebAssembly

### Core Runtime Building
- **[Building CoreCLR for WebAssembly](building/coreclr/wasm.md)** - Build the CoreCLR runtime for WebAssembly targets
- **[Building Mono for Browser](../../src/mono/browser/README.md)** - Browser build guide with samples and troubleshooting

### Libraries Building
- **[Building Libraries for WebAssembly](building/libraries/webassembly-instructions.md)** - Build .NET libraries for WebAssembly targets
- **[WebAssembly Build System](../../src/mono/browser/build/README.md)** - WasmApp.targets and build system internals

### WASI Support
- **[WASI Support](../../src/mono/wasi/README.md)** - Experimental WASI support, building, and configuration

## Testing and Debugging

### Library Testing
- **[Testing Libraries on WebAssembly](testing/libraries/testing-wasm.md)** - Run library tests with different JavaScript engines and browsers
- **[Debugging WebAssembly Libraries](testing/libraries/debugging-wasm.md)** - Debug library tests in Chrome DevTools and VS Code

### Runtime Debugging
- **[WebAssembly Debugging](debugging/mono/wasm-debugging.md)** - Consolidated debugging reference for WebAssembly applications and native runtime debugging
- **[VS Code Debugging](debugging/libraries/debugging-vscode.md)** - Set up VS Code for debugging WASM applications

### Common Debugging Scenarios

For debugging instructions including VS Code and Chrome DevTools setup, see the [WebAssembly Debugging Reference](debugging/mono/wasm-debugging.md).

## Features and Configuration

### Runtime Features
- **[WebAssembly Features](../../src/mono/wasm/features.md)** - Configure browser features, SIMD, threads, AOT, and more
- **[Threading Support](../../src/mono/wasm/threads.md)** - Multi-threading support and limitations

### JavaScript Interop
- **[JSInterop in Wasm](https://learn.microsoft.com/en-us/aspnet/core/client-side/dotnet-interop/wasm-browser-app)** - JavaScript interoperability for WebAssembly applications

### Globalization and ICU
- **[ICU for WebAssembly](../../design/features/globalization-icu-wasm.md)** - Globalization and ICU database configuration

### Testing WebAssembly Changes
For testing WebAssembly implementation changes end-to-end, see the [testing documentation](../testing/mono/testing.md#testing-webassembly).

## Advanced Topics

### Performance and Optimization
- **[Profiling](../../src/mono/wasm/features.md#profiling-in-the-browser-dev-tools)**: Use browser dev tools profiler integration
- **[AOT Compilation](../../src/mono/wasm/features.md#aot)**: Improve runtime performance with ahead-of-time compilation
- **[IL Trimming](../../src/mono/wasm/features.md#trimming)**: Reduce application size by removing unused code

### Samples and Examples
Located in `src/mono/sample/wasm/`:
- **browser-bench**: Performance benchmarking sample
- **browser-profile**: Profiling sample
- **console**: Console application samples

### Workloads
- `wasm-tools`: Production WebAssembly tools and optimization
- `wasm-experimental`: Experimental features and templates

## FAQ

### How do I debug a library test failure seen on CI?

See the [WebAssembly Debugging Reference](debugging/mono/wasm-debugging.md#common-debugging-workflow) for detailed instructions on debugging library tests locally.

### How do I build for different WebAssembly targets?

See the [Building for WebAssembly](#building-for-webassembly) section above for build instructions for different targets.

### How do I test Wasm changes end to end?

Use Wasm.Build.Tests or Wasi.Build.Tests. See the [Wasm.Build.Tests README](../../src/mono/wasm/Wasm.Build.Tests/README.md) for detailed instructions.

### How do I enable multi-threading?

See the [Threading Support](../../src/mono/wasm/threads.md) documentation for detailed multi-threading configuration and limitations.

### What JavaScript engines are supported for testing?

See the [Testing Libraries on WebAssembly](testing/libraries/testing-wasm.md#prerequisites) documentation for JavaScript engine installation and usage.

### How do I collect native stack traces with symbols?

See the [WebAssembly Debugging](debugging/mono/wasm-debugging.md#collecting-stack-traces-with-symbols-in-blazor) documentation for symbol configuration.

### How do I run tests with different configurations?

- **With AOT**: Add `/p:RunAOTCompilation=true`
- **With trimming**: Add `/p:EnableAggressiveTrimming=true`
- **Different JS engine**: Add `/p:JSEngine=SpiderMonkey`
- **Outer loop tests**: Add `/p:Outerloop=true`

## Contributing

### Code Style
- Runtime JavaScript code uses ESLint rules in `.eslintrc.js`
- Run `npm run lint` in `src/mono/browser/runtime`
- Install VS Code ESLint plugin for real-time checking

### Building Changes
When making changes that affect native code or build configuration, see [Building the Repo](https://github.com/dotnet/runtime/tree/main/docs/workflow#building-the-repo) for detailed instructions.

Then test your changes with [Testing the Repo](https://github.com/dotnet/runtime/tree/main/docs/workflow#testing-the-repo) using the relevant test suites.

### Documentation Updates
When updating WebAssembly documentation:
1. Update this index if adding new documents
2. Ensure cross-references remain valid
3. Test documentation examples locally
4. Follow existing documentation patterns and styles

For questions or additional help, see the main [workflow documentation](README.md) or ask in the dotnet/runtime repository discussions.