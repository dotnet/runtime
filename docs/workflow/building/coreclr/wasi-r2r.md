# CoreCLR-WASI composite ReadyToRun

This is an experimental in-tree publishing and runtime-test workflow. The shipping WASI SDK
does not yet select the CoreCLR app builder.

WASI requires the compiled composite to be composed into the host component before execution.
Copying `composite-r2r.wasm` beside an unmodified host is not sufficient. The composer lives
alongside the WASI app-builder targets in `src/mono/wasi/build/compose-r2r.py`.
The [WebCIL design document](../../../design/mono/webcil.md#wasi-host-composition) describes
the image layout and host contract.

## Prerequisites

Build prerequisites are described in the [CoreCLR build guide](README.md).
Composition additionally requires Python 3.8+, `wasm-tools`, and Binaryen's `wasm-merge` and
`wasm-opt` on `PATH`. Running the result requires wasmtime with WebAssembly exception support.
CI provisions pinned tool versions through `eng/testing/wasi-r2r-provisioning.targets`.

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

## Runtime tests

The WASI runtime-test pipeline keeps separate interpreter and `R2R_CG2` jobs. The R2R job uses
Checked CoreCLR with Release libraries. For a local merged-runner example:

```bash
./build.sh -s clr+libs+packs -os wasi -arch wasm -c Release -rc Checked -lc Release -hc Release
src/tests/build.sh -os wasi -arch wasm Checked -dir:JIT/CodeGenBringUpTests \
  -priority1 -crossgen2 /p:LibrariesConfiguration=Release /p:HostConfiguration=Release
export CORE_ROOT="$PWD/artifacts/tests/coreclr/wasi.wasm.Checked/Tests/Core_Root"
export __TestDotNetCmd="$PWD/.dotnet/dotnet"
RunCrossGen2=1 bash artifacts/tests/coreclr/wasi.wasm.Checked/JIT/CodeGenBringUpTests/JIT.CodeGenBringUpTests_ro/JIT.CodeGenBringUpTests_ro.sh
```

The wrapper compiles the runner and referenced test assemblies into a composite, then invokes
`CORE_ROOT/wasi-r2r/compose-r2r.py`. It launches `IL-CG2/wasi-r2r/corerun-composite.wasm` with
`APP_ASSEMBLIES=EXTERNAL` and `CORE_LIBRARIES=/IL-CG2/wasi-r2r` so the guest probes the private
`comp/` stubs. `TEST_READY_TO_RUN_MODE=1` enables R2R-specific test conditions.

Without `RunCrossGen2`, the wrapper uses the original host and does not probe those stubs.
Composition failures fail the test rather than silently falling back to interpretation.
Helix receives the composition tools through a separate correlation payload.

## Composer interface and diagnostics

The build targets generate Crossgen2 response files; a hand-maintained response-file template
is not required. To inspect or compose existing outputs directly:

```bash
python3 src/mono/wasi/build/compose-r2r.py --describe <composite.wasm>
COMP=<composite.wasm> CORERUN=<host-component> OUTDIR=<output-directory> \
  python3 src/mono/wasi/build/compose-r2r.py
```

`--describe` reports `functionCount,payloadBytes`. Composition requires all three environment
variables and writes `corerun-composite.wasm` into `OUTDIR`. The host supplies the image and table
bases; the script rejects an undersized buffer or overlapping table reservation.

A valid composed module and passing tests alone do not prove R2R was used. Enable the guest's
`DOTNET_ReadyToRunLogFile` and look for `Ready to Run initialized successfully` for the test
assemblies. This confirms image loading; proving a particular method executes compiled code
requires a breakpoint or trace in that method's wasm body.
