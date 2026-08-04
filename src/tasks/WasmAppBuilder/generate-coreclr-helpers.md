# Generating CoreCLR WebAssembly call helpers

The `generate-coreclr-helpers.cmd` (Windows) and `generate-coreclr-helpers.sh` (Linux/macOS)
scripts in this directory regenerate the checked-in CoreCLR call-helper source files used by the
WebAssembly runtime. They run the `RunGenerator` target in
[`WasmAppBuilder.csproj`](./WasmAppBuilder.csproj), which invokes the
`ManagedToNativeGenerator` MSBuild task to scan the managed framework assemblies and emit the
native P/Invoke, reverse-P/Invoke, and interpreter-to-managed call helpers.

The scripts generate **both** WebAssembly variations:

| Target OS | Output directory                | Default scan path (testhost) |
|-----------|---------------------------------|------------------------------|
| `browser` | `src/coreclr/vm/wasm/browser/`  | `artifacts/bin/testhost/net11.0-browser-<config>-wasm/shared/Microsoft.NETCore.App/11.0.0/` |
| `wasi`    | `src/coreclr/vm/wasm/wasi/`     | `artifacts/bin/testhost/net11.0-wasi-<config>-wasm/shared/Microsoft.NETCore.App/11.0.0/` |

Each run emits three files into the output directory:

- `callhelpers-pinvoke.cpp`
- `callhelpers-reverse.cpp`
- `callhelpers-interp-to-managed.cpp`

## What needs to be built first

The generator scans the **managed framework assemblies** in the `testhost` folder produced by a
`clr+libs` build. Because the scripts generate both the `browser` and `wasi` variations, you must
build **both** WebAssembly flavors before running them. The first build of either flavor also
downloads and provisions the Emscripten SDK (emsdk) automatically.

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
- The `WasmAppBuilder` task itself is built on demand by the generator script (the `RunGenerator`
  target depends on `Build`), so you do not need to build it separately.
- If a required `testhost` scan path is missing, the script stops and prints the exact
  `build` command needed to produce it.

## Running the generator

Once both flavors are built, run the script from anywhere (it resolves the repo root itself):

**Windows:**
```cmd
src\tasks\WasmAppBuilder\generate-coreclr-helpers.cmd -c Debug
```

**Linux/macOS:**
```bash
src/tasks/WasmAppBuilder/generate-coreclr-helpers.sh -c Debug
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
