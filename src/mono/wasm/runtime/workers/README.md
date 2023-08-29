# .NET for WebAssembly Web Workers

This directory contains the sources for web workers that support .NET for WebAssembly.

The [`rollup.config.js`](../rollup.config.js) in the parent directory
defines the outputs (generally: `dotnet-xyz-worker.js`)

As a matter of convention, each worker has a toplevel `xyz-worker.ts`
file (or `.js`) and a `xyz/` subdirectory with additional sources.

To add a new web worker, add a definition here and modify the
`rollup.config.js` to add a new configuration.

## Caveats: a note about pthreads

The workers in this directory are completely standalone from the Emscripten pthreads! they do not have access to the shared instance memory, and do not load the Emscripten `dotnet.native.js`.  As a result, the workers in this directory cannot use any of pthread APIs or otherwise interact with the runtime in any way, except through message passing, or by having something in the runtime set up their own shared array buffer (which would be inaccessible from wasm).

On the other hand, the workers in this directory also do not depend on a .NET runtime compiled with `-s USE_PTHREADS` and are thus usable on sufficiently new browser using the single-threaded builds of .NET for WebAssembly.

For workers that need to interact with native code, follow the model of `../pthreads/` or `../diagnostic_server/`:

- create `xyz/shared/`, `xyz/browser/` and `xyz/worker/` directories that have `index.ts` and `tsconfig.json` files that are set up for common ES, ES with DOM APIs and ES with WebWorker APIs, respectively
- call the apropriate functions (browser or worker) from the C code or from JS.

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
* `src/mono/wasm//Wasm.Build.Tests/BuildTestBase.cs`
* etc

