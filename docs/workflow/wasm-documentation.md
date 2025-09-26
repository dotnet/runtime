# WebAssembly (WASM) Documentation

This document serves as a comprehensive index to all WebAssembly-related documentation in the dotnet/runtime repository. Whether you're building, testing, debugging, or deploying .NET applications for WebAssembly, this guide will help you find the right resources.

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

### Quick Start
1. Install the workload: `dotnet workload install wasm-tools`
2. Create a new project: `dotnet new wasmbrowser` or `dotnet new wasiconsole`
3. Build and run: `dotnet run`

## Building for WebAssembly

### Core Runtime Building
- **[Building CoreCLR for WebAssembly](building/coreclr/wasm.md)** - Build the CoreCLR runtime for WebAssembly targets
- **[Building Mono for Browser](../../src/mono/browser/README.md)** - Comprehensive browser build guide with samples and troubleshooting

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
- **[Native WASM Runtime Debugging](debugging/mono/native-wasm-debugging.md)** - Debug the Mono runtime, native crashes, and collect stack traces
- **[VS Code Debugging](debugging/libraries/debugging-vscode.md)** - Set up VS Code for debugging WASM applications

### Common Debugging Scenarios

For consolidated debugging instructions including VS Code and Chrome DevTools setup, see the [WebAssembly Debugging Reference](debugging/wasm-debugging-reference.md).

## Features and Configuration

### Runtime Features
- **[WebAssembly Features](../../src/mono/wasm/features.md)** - Configure browser features, SIMD, threads, AOT, and more
- **[Threading Support](../../src/mono/wasm/threads.md)** - Multi-threading support and limitations

### Build Configuration
Key MSBuild properties for WebAssembly applications:
- `WasmEnableThreads` - Enable multi-threading support
- `WasmEnableSIMD` - Enable SIMD instruction support  
- `RunAOTCompilation` - Enable Ahead-of-Time compilation
- `WasmBuildNative` - Force native rebuild
- `EnableDiagnostics` - Enable diagnostic features

### Globalization and ICU
- **[ICU for WebAssembly](../../design/features/globalization-icu-wasm.md)** - Globalization and ICU database configuration

## Deployment and Hosting

### Application Structure
- **AppBundle folder**: Contains your deployed application
- **_framework folder**: Runtime and managed assemblies
- **dotnet.js**: Main entry point and JavaScript API
- **dotnet.native.wasm**: Compiled .NET runtime binary

### Web Server Configuration
Ensure your web server is configured with proper MIME types:
- `.wasm` files: `application/wasm`
- `.js` files: `text/javascript`
- `.json` files: `application/json`

## Advanced Topics

### Performance and Optimization
- **Profiling**: Use browser dev tools profiler integration
- **AOT Compilation**: Improve runtime performance with ahead-of-time compilation
- **IL Trimming**: Reduce application size by removing unused code

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

See the [WebAssembly Debugging Reference](debugging/wasm-debugging-reference.md#common-debugging-workflow) for detailed instructions on debugging library tests locally.

### How do I build for different WebAssembly targets?

See the [Building for WebAssembly](#building-for-webassembly) section above for comprehensive build instructions for different targets.

### How do I enable multi-threading?

See the [Threading Support](../../src/mono/wasm/threads.md) documentation for detailed multi-threading configuration and limitations.

### How do I optimize my WebAssembly application?

See the [WebAssembly Features](../../src/mono/wasm/features.md) documentation for AOT compilation, IL trimming, and other optimization options.

### What JavaScript engines are supported for testing?

See the [Testing Libraries on WebAssembly](testing/libraries/testing-wasm.md#prerequisites) documentation for JavaScript engine installation and usage.

### How do I collect native stack traces with symbols?

See the [Native WASM Runtime Debugging](debugging/mono/native-wasm-debugging.md#collecting-stack-traces-with-symbols-in-blazor) documentation for symbol configuration.

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
When making changes that affect native code or build configuration:
1. Clean build artifacts: `git clean -xfd`
2. Rebuild: `./build.sh -os browser -subset mono+libs`
3. Test your changes with relevant test suites

### Documentation Updates
When updating WebAssembly documentation:
1. Update this index if adding new documents
2. Ensure cross-references remain valid
3. Test documentation examples locally
4. Follow existing documentation patterns and styles

For questions or additional help, see the main [workflow documentation](README.md) or ask in the dotnet/runtime repository discussions.