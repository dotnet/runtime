# Generating CoreCLR WebAssembly call helpers

The `generate-coreclr-helpers.cmd` (Windows) and `generate-coreclr-helpers.sh` (Linux/macOS)
scripts in this directory regenerate the checked-in CoreCLR call-helper source files used by the
WebAssembly runtime. They parse their arguments and hand the work to
`generate-coreclr-helpers.proj` next to them, which runs crossgen2 in
`--generate-portable-callhelpers` mode over the managed framework assemblies to emit the native
P/Invoke, reverse-P/Invoke, and interpreter-to-managed call helpers, including the shared static
QCall/FCall entrypoint table. The generator lives in
[`ILCompiler.ReadyToRun/PortableCallHelpers`](../../tools/aot/ILCompiler.ReadyToRun/PortableCallHelpers) so it can use crossgen2's
type system to compute the wasm ABI layout of the structs that cross the boundary.

Keeping the scan paths, the crossgen2 lookup and the module list in the project rather than in the
scripts means they are stated once instead of once per shell language.

Each run leaves the exact generator invocation in a response file under
`artifacts/obj/wasm-callhelpers/<target os>/generate-coreclr-helpers.rsp`, which is the first thing
to look at when a regenerated table is not what was expected.

The relink targets for browser and wasi apps run the same crossgen2 mode over the app's own
assembly closure, so these checked-in files and a relinked app are produced by one code path.

By default the scripts generate **both** WebAssembly variations; pass `--target-os browser` or
`--target-os wasi` to regenerate only one:

| Target OS | Output directory                | Default scan path (testhost) |
|-----------|---------------------------------|------------------------------|
| `browser` | `src/coreclr/vm/wasm/browser/`  | `artifacts/bin/testhost/net12.0-browser-<config>-wasm/shared/Microsoft.NETCore.App/12.0.0/` |
| `wasi`    | `src/coreclr/vm/wasm/wasi/`     | `artifacts/bin/testhost/net12.0-wasi-<config>-wasm/shared/Microsoft.NETCore.App/12.0.0/` |

Each run emits three files into the output directory:

- `callhelpers-pinvoke.cpp`
- `callhelpers-reverse.cpp`
- `callhelpers-interp-to-managed.cpp`

For the experimental browser wasm64 interpreter, invoke the project directly with
`-p:CallHelperTargetOS=browser -p:CallHelperTargetArchitecture=wasm64`. Use
`-p:BrowserScanPath=/path/to/il/` to supply a directory containing the wasm64 IL
CoreLib and its managed framework closure. The output goes to `browser-wasm64/`
and does not replace the browser wasm32 tables. Build the browser crossgen2
host tool first with `./build.sh clr.tools -os browser -arch wasm -c Release`.
This helper build uses crossgen2 only for signature lowering and helper
generation.

The browser-wasm64 build follows browser-wasm: `./build.sh -os browser -arch wasm64`
builds the `clrjit_universal_wasm64_<host-arch>` cross-JIT and crossgen2, compiles a
ReadyToRun `System.Private.CoreLib.wasm`, and adds ReadyToRun images for the shared
framework to the runtime pack under `native/r2r`. ReadyToRun is enabled by default,
as on wasm32; set `DOTNET_ReadyToRun=0` to run CoreLib and the framework in the
interpreter. NativeAOT does not support wasm64, so the build skips it.
To compile by hand, use `--targetos browser --targetarch wasm64 --obj-format wasm`
and pass the cross-JIT via `--jitpath`; add `--composite` for a composite image,
which may include CoreLib. Add `--singlemethodtypename` and `--singlemethodname`
to compile only one method. ReadyToRun code outside CoreLib requires a ReadyToRun
CoreLib. Crossgen2 recognizes the same
baseline Wasm intrinsics on browser-wasm and browser-wasm64: Packed SIMD is
available to compiled code, while relaxed SIMD is not enabled by default.
A non-composite ReadyToRun CoreLib starts under both Node/CoreRun and Chrome.

The wasm64 runtime uses the same GC configuration as wasm32: workstation GC with
segments, not regions, and wasm32's initial segment sizes. Wasm has no
reserve-only memory, so any reservation grows linear memory, which is never
returned. Objects are 8-byte aligned (`DATA_ALIGNMENT`), which matches
crossgen2's object and `Vector<T>` layout for wasm64. When R2R code calls an
interpreted string constructor, the runtime keys the R2R-to-interpreter thunk on
the lowered static factory signature, as crossgen2 does, on both widths; the
computed wasm32 keys equal the fixed ones they replaced. Closed static delegates
whose target returns through a hidden buffer use a pointer-width
`WasmClosedStaticRetBufThunk`, which crossgen2 emits for both widths. Value-type
methods entered with a boxed `this` use the shared `U`, `UG` and `UM` unboxing
stubs on both widths; on these portable-entrypoint targets the runtime does not
use crossgen2's IL unboxing thunks, so without a stub it interprets an IL stub.
When CoreLib is part of the compilation, both widths finalize speculative
direct calls after code generation (`DeferredWasmCallProcessor`); on wasm64 the
frame identity that a direct call passes in the entry-point parameter is an
`i64.const`, and fallback calls load the entry point with 64-bit loads.

The build links ICU from the ICU transport package's
`runtimes/browser-wasm64/native` assets, as browser-wasm uses
`runtimes/browser-wasm/native`. Like the wasm32 one, the Node CoreRun host
uses invariant globalization.

The experimental CoreCLR `browser-wasm64` app host requires a
memory64-capable browser. Otherwise the in-tree Wasm SDK treats
browser-wasm64 like browser-wasm, including its globalization defaults:
assemblies are staged as
Webcil, using a wrapper that imports an i64 memory and `__memory_base`, and
`PublishReadyToRun` (including composite) selects the same ReadyToRun images.

In Chrome, an SDK-published compiled caller invoked an interpreted method in
an excluded IL assembly with a native-integer byref. A Wasm-only change to the
caller changed the exit code only with ReadyToRun enabled. The full browser
sample also runs with non-composite ReadyToRun: the Promise passed to the
managed export and the managed entry point both reach the JS import with `42`,
and the app exits with code `0`. This was tested with a local runtime-pack
overlay containing the rebuilt native host and its matching managed CoreLib;
an updated runtime-pack `.nupkg` was not tested. Keep those two components
matched when overlaying a restored pack.

Two additional in-tree publishes ran the sample in Chrome with ReadyToRun:

- **Native relink, without trimming:** Set `WasmBuildNative=true`. The
  runtime-pack overlay must also contain the matching `libBrowserHost.js`
  and CoreCLR static archives. Use clean native publish intermediates when
  overlaying files with preserved timestamps; an older `libBrowserHost.js`
  passes a Number to the memory64 table and fails at startup.
- **Trimming, with the prebuilt host:** Set `PublishTrimmed=true`. If in-tree
  static-graph restore reports a cycle through `ILLink.Tasks.csproj`, use the
  already-built linker: set `_RequiresLiveILLink=false`, `ILLinkTargetsPath`
  to `src/tools/illink/src/ILLink.Tasks/build/Microsoft.NET.ILLink.targets`,
  and `ILLinkTasksAssembly` to
  `artifacts/bin/ILLink.Tasks/Release/net/ILLink.Tasks.dll` (use absolute
  paths for both). Setting the assembly alone does not import the targets
  and can leave the publish untrimmed.

Stock SDK RID and pack registration, distribution of the wasm64 crossgen2
toolchain, and browser diagnostics are not validated. WASI is outside this
experiment. Composite ReadyToRun of the app assemblies passes the JavaScript
interop library tests in V8; CoreRun runtime tests also cover whole-program
composite images.

With the wasm64 runtime pack built, publish the in-tree browser sample with:

```sh
./dotnet.sh publish src/mono/sample/wasm/browser/Wasm.Browser.Sample.csproj \
  -p:RuntimeFlavor=CoreCLR -p:TargetArchitecture=wasm64 -p:PublishTrimmed=false \
  -p:WasmBuildNative=false -p:WasmEmitSymbolMap=false -p:WasmTestSupport=false
```

Serve `bin/publish/wwwroot` from the sample directory. The page should show
`42`. This smoke test leaves diagnostics off; the diagnostic WebSocket bridge
has not been ported to memory64.

Wasm64 ReadyToRun and the interpreter share an ABI: pointers,
native integers, and indirect aggregate addresses are 64-bit, but managed
32-bit primitives stay 32-bit and interpreter stack slots stay 8 bytes.
The browser smoke exercises memory and table operand widths and a byref call.
Broader coverage is still needed for hidden return/context/async arguments,
aggregate lowering, unwinding, and GC across the generated thunks.

## The shared QCall/FCall table

`callhelpers-pinvoke.cpp` contains an entrypoint table for all P/Invokes in the application, as well as all [QCalls and FCalls in System.Private.CoreLib](../../../../docs/design/coreclr/botr/corelib.md#Calling-from-managed-to-native-code). A few specialized FCalls have separate internal runtime implementations that are not included in this table. The QCalls and FCalls are discovered by scanning the members of System.Private.CoreLib to find the corresponding methods.

## The P/Invoke module list

Only the framework native libraries the runtime links statically get an entry in the generated
P/Invoke table. `generate-coreclr-helpers.proj` imports that list from
[`eng/wasm/WasmPInvokeModules.props`](../../../../eng/wasm/WasmPInvokeModules.props), which
`CLRTest.WasmCorerun.targets` imports
too when it links a test-specific corerun, so the checked-in tables and the tests' own cannot be
edited apart.

The browser runtime SDK packages the same props file beside `BrowserWasmApp.CoreCLR.targets`, so
in-tree helper generation, runtime tests, and SDK app builds all consume the canonical list.

Adding a module means editing the props file, rerunning these scripts, and committing the
regenerated files in the same change. A module missing from the list surfaces as a
`DllNotFoundException` at run time rather than as a build failure.

## What needs to be built first

The generator scans the **managed framework assemblies** in the `testhost` folder produced by a
`clr+libs` build, and runs the self-contained crossgen2 that the same build produces. Because the
scripts generate both the `browser` and `wasi` variations by default, you must build **both**
WebAssembly flavors before running them, or pass `--target-os` to regenerate only the flavor you
built. The first build of either flavor also downloads and
provisions the Emscripten SDK (emsdk) automatically.

From the repository root:

**Windows:**
```cmd
.\build.cmd clr+libs -os browser -c Release
.\build.cmd clr+libs -os wasi    -c Release
```

**Linux/macOS:**
```bash
./build.sh clr+libs -os browser -c Release
./build.sh clr+libs -os wasi    -c Release
```

Notes:

- Use a matching `-c <Debug|Release|Checked>` for the configuration you intend to pass to the
  generator script (the script derives the scan path from the configuration name).
- If a required `testhost` scan path or crossgen2 is missing, the script stops and prints the
  exact `build` command needed to produce it.

## Which configuration to generate from

Generate the checked-in tables from a **Release** build. The CI check regenerates them from
Release and compares exactly.

The tables are linked into every configuration's corerun, so their entries must not depend on the
configuration. Debug and Release libraries, a Checked or Release CoreLib, and a Checked or Release
crossgen2 all produce the same P/Invoke, reverse-thunk and interpreter-to-managed entries. The
trailing `// <assembly>, ...` comments on the P/Invoke table are the exception. They list the
assemblies that still reference each import after the library build trims them, and that set
differs by configuration. For example, Debug `System.Net.NameResolution` keeps a reference to
`SystemNative_GetErrNo` that Release trims. Tables regenerated from Debug therefore show
comment-only diffs that the CI check rejects.

## Running the generator

Once both flavors are built, run the script from anywhere (it resolves the repo root itself):

**Windows:**
```cmd
src\coreclr\vm\wasm\generate-coreclr-helpers.cmd -c Release
```

**Linux/macOS:**
```bash
src/coreclr/vm/wasm/generate-coreclr-helpers.sh -c Release
```

### Options

| Option | Description |
|--------|-------------|
| `-c`, `--configuration <Checked\|Debug\|Release>` | Build configuration (default: `Release`). Determines the default scan paths. |
| `-s`, `--scan-path <path>` | Override the default **browser** scan path. |
| `-w`, `--wasi-scan-path <path>` | Override the default **wasi** scan path. |
| `-t`, `--target-os <browser\|wasi>` | Regenerate only this flavor (default: both). |
| `-h`, `--help` | Show usage. |

After running, review and commit any changes to the generated files under
`src/coreclr/vm/wasm/browser/` and `src/coreclr/vm/wasm/wasi/`.

## CI check

The Release `browser_wasm` CoreCLR build leg and the `wasi_wasm` CoreCLR build-only leg in
`eng/pipelines/runtime.yml` run
[`wasm-coreclr-callhelpers-check.yml`](../../../../eng/pipelines/common/templates/wasm-coreclr-callhelpers-check.yml)
after building. It regenerates the leg's own flavor with `--target-os` and fails if
`src/coreclr/vm/wasm/<os>/` differs from what is committed, printing the diff and the commands to
fix it. A stale QCall table can fail native linking if it still references a removed entrypoint.
Other stale entries can crash corerun at run time; a missing
`[UnmanagedCallersOnly]` thunk, for example, fails startup with
`GetUnmanagedCallersOnlyThunk: unknown thunk`. Library tests do not catch this because the app
relink generates its own tables.

The legs are triggered by the `wasm_coreclr_callhelpers` path subset in
`eng/pipelines/common/evaluate-default-paths.yml`, which covers the library sources, CoreLib and
the generator, so a change that only touches managed interop still runs the check.
