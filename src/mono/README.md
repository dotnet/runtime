# Shared workload infrastructure

The Mono runtime, CoreLib, and AOT compiler sources have been removed from .NET 12.
This change does not affect .NET 11 or .NET 10.

This directory still contains WebAssembly and mobile build infrastructure,
workload packaging, samples, and tests shared with CoreCLR and NativeAOT.
These files must be preserved until their remaining consumers are migrated.

## Building and testing

- [Testing libraries on WebAssembly](/docs/workflow/testing/libraries/testing-wasm.md)
- [Testing libraries on Android](/docs/workflow/testing/libraries/testing-android.md)
- [Testing libraries on Apple platforms](/docs/workflow/testing/libraries/testing-apple.md)
