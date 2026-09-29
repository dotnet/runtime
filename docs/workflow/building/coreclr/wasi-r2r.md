# CoreCLR-WASI composite ReadyToRun

This is an experimental in-tree publishing workflow. The shipping WASI SDK
does not yet select the CoreCLR app builder.

WASI requires the compiled composite to be composed into the host component before execution.
Copying `composite-r2r.wasm` beside an unmodified host is not sufficient. Composition is
implemented by the in-tree `ComposeWasiReadyToRun` MSBuild task.
The [WebCIL design document](../../../design/mono/webcil.md#wasi-host-composition) describes
the image layout and host contract.

## Prerequisites

Build prerequisites are described in the [CoreCLR build guide](README.md).
Composition additionally requires `wasm-tools` and Binaryen's `wasm-merge` and `wasm-opt`.
Running the result requires wasmtime with WebAssembly exception support.
In-tree publishing acquires pinned tool versions into the shared wasm tool cache through
`eng/AcquireWasiR2RTools.targets`.

Framework-sized composites can require several GiB of memory during composition. Allow for the
host and composite working sets when sizing build containers.

## Publishing

Build the runtime, libraries, and packs, then publish an in-tree WASI project:

```bash
./build.sh -s clr+libs+packs -os wasi -arch wasm -c Release
./dotnet.sh publish <project> -c Release -p:TargetOS=wasi \
  -p:RuntimeFlavor=CoreCLR -p:PublishReadyToRun=true
```

The app builder enables composite R2R, compiles the app/framework closure, sizes the host's image
buffer and table reservation, links the host, and invokes the composer. It deploys the composed
host and the per-assembly stubs under the app bundle's `managed/` directory.
Non-composite R2R and `WasmSingleFileBundle` are not supported by this path.

## Composer diagnostics

The build targets generate Crossgen2 response files; a hand-maintained response-file template
is not required. The host supplies the image and table bases, and the C# composer rejects an
undersized buffer, overlapping table reservation, or a non-self-installing composite.

A valid composed module and a successful run alone do not prove R2R was used. Enable the guest's
`DOTNET_ReadyToRunLogFile` and look for `Ready to Run initialized successfully` for the app
assemblies. This confirms image loading; proving a particular method executes compiled code
requires a breakpoint or trace in that method's wasm body.
