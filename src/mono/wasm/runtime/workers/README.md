# .NET for WebAssembly Web Workers

This directory contains the sources for web workers that support .NET for WebAssembly.

The [`rollup.config.js`](../rollup.config.js) in the parent directory
defines the outputs (generally: `dotnet-xyz-worker.js`)

As a matter of convention, each worker has a toplevel `xyz-worker.ts`
file (or `.js`) and a `xyz/` subdirectory with additional sources.

To add a new web worker, add a definition here and modify the
`rollup.config.js` to add a new configuration.

## Typescript modules

Typescript workers can use the modules from [`..`](..) but bear in
mind that they will be rolled up into the worker which may increase
the size of the overall bundle.

## Additional changes when adding a new worker

There are additional changes that are needed beyond just adding a new `dotnet-*-worker.[tj]s` file in this directory.

Some other places that may need to be modified include:
* [`../../wasm.proj`](../../wasm.proj)
* `eng/liveBuilds.targets`
* `src/installer/pkg/sfx/Microsoft.NETCore.App/Directory.Build.prop`
* [`../../build/WasmApp.targets`](../../build/WasmApp.targets)
* `src/tests/BuildWasmApps/Wasm.Build.Tests/BuildTestBase.cs`
* etc

