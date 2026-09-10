# Generating CoreCLR WebAssembly call helpers

The `generate-coreclr-helpers.cmd` (Windows) and `generate-coreclr-helpers.sh` (Linux/macOS)
scripts in this directory regenerate the checked-in CoreCLR call-helper source files used by the
WebAssembly runtime. They parse their arguments and hand the work to
`generate-coreclr-helpers.proj` next to them, which runs crossgen2 in
`--generate-portable-callhelpers` mode over the managed framework assemblies to emit the native
P/Invoke, reverse-P/Invoke, and interpreter-to-managed call helpers. The generator lives in
[`ILCompiler.ReadyToRun/PortableCallHelpers`](../../tools/aot/ILCompiler.ReadyToRun/PortableCallHelpers) so it can use crossgen2's
type system to compute the wasm ABI layout of the structs that cross the boundary.

Keeping the scan paths, the crossgen2 lookup and the module list in the project rather than in the
scripts means they are stated once instead of once per shell language.

Each run leaves the exact generator invocation in a response file under
`artifacts/obj/wasm-callhelpers/<target os>/generate-coreclr-helpers.rsp`, which is the first thing
to look at when a regenerated table is not what was expected.

The relink targets for browser and wasi apps run the same crossgen2 mode over the app's own
assembly closure, so these checked-in files and a relinked app are produced by one code path.

The scripts generate **both** WebAssembly variations:

| Target OS | Output directory                | Default scan path (testhost) |
|-----------|---------------------------------|------------------------------|
| `browser` | `src/coreclr/vm/wasm/browser/`  | `artifacts/bin/testhost/net11.0-browser-<config>-wasm/shared/Microsoft.NETCore.App/11.0.0/` |
| `wasi`    | `src/coreclr/vm/wasm/wasi/`     | `artifacts/bin/testhost/net11.0-wasi-<config>-wasm/shared/Microsoft.NETCore.App/11.0.0/` |

Each run emits three files into the output directory:

- `callhelpers-pinvoke.cpp`
- `callhelpers-reverse.cpp`
- `callhelpers-interp-to-managed.cpp`

## The P/Invoke module list

Only the framework native libraries the runtime links statically get an entry in the generated
P/Invoke table. `generate-coreclr-helpers.proj` imports that list from
[`eng/wasm/WasmPInvokeModules.props`](../../../../eng/wasm/WasmPInvokeModules.props), which
`CLRTest.WasmCorerun.targets` imports too when it links a test-specific corerun, so the checked-in
tables and the tests' own cannot be edited apart.

`BrowserWasmApp.CoreCLR.targets` keeps a copy of the list on purpose: it ships in the WebAssembly
workload and is evaluated inside the user's SDK, where the props file does not exist. That copy has
to be updated alongside the props file.

Adding a module means editing both, rerunning these scripts, and committing the regenerated files
in the same change. A module missing from one of them surfaces as a `DllNotFoundException` at run
time rather than as a build failure.

## What needs to be built first

The generator scans the **managed framework assemblies** in the `testhost` folder produced by a
`clr+libs` build, and runs the self-contained crossgen2 that the same build produces. Because the
scripts generate both the `browser` and `wasi` variations, you must build **both** WebAssembly
flavors before running them. The first build of either flavor also downloads and
provisions the Emscripten SDK (emsdk) automatically.

From the repository root:

**Windows:**
```cmd
.\build.cmd clr+libs -os browser -c Debug
.\build.cmd clr+libs -os wasi    -c Debug
```

**Linux/macOS:**
```bash
./build.sh clr+libs -os browser -c Debug
./build.sh clr+libs -os wasi    -c Debug
```

Notes:

- Use a matching `-c <Debug|Release|Checked>` for the configuration you intend to pass to the
  generator script (the script derives the scan path from the configuration name).
- If a required `testhost` scan path or crossgen2 is missing, the script stops and prints the
  exact `build` command needed to produce it.

## Running the generator

Once both flavors are built, run the script from anywhere (it resolves the repo root itself):

**Windows:**
```cmd
src\coreclr\vm\wasm\generate-coreclr-helpers.cmd -c Debug
```

**Linux/macOS:**
```bash
src/coreclr/vm/wasm/generate-coreclr-helpers.sh -c Debug
```

### Options

| Option | Description |
|--------|-------------|
| `-c`, `--configuration <Checked\|Debug\|Release>` | Build configuration (default: `Debug`). Determines the default scan paths. |
| `-s`, `--scan-path <path>` | Override the default **browser** scan path. |
| `-w`, `--wasi-scan-path <path>` | Override the default **wasi** scan path. |
| `-h`, `--help` | Show usage. |

After running, review and commit any changes to the generated files under
`src/coreclr/vm/wasm/browser/` and `src/coreclr/vm/wasm/wasi/`.
