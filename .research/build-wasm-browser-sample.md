# Building Wasm.Browser.Sample for CoreCLR

Project: `src/mono/sample/wasm/browser/Wasm.Browser.Sample.csproj`

## Prerequisites

Build the CoreCLR runtime, libraries, and host for `browser-wasm` **before** building the sample. From the repo root:

```bash
# Debug (if needed)
./build.sh clr+libs+host -os browser -c Debug
```

It's already built and it most probably be needed to build again.

This produces artifacts under `artifacts/obj/coreclr/browser.wasm.Debug/libs-native/` which the sample build validates exists via the `_ValidateRuntimeFlavorBuild` target.

⏱️ The prerequisite build takes ~37 minutes (CoreCLR compiles the entire runtime engine from C/C++ source via emcc — 840+ ninja steps).

## Building the Sample

### Option 1: Via MSBuild Target (Recommended)

The `BuildSampleInTree` target handles cleaning, nested property forwarding, and produces a binlog automatically:

```bash
cd src/mono/sample/wasm/browser

# Build with CoreCLR and produce binlog
../../../../../../dotnet.sh build \
  /t:BuildSampleInTree \
  /p:RuntimeFlavor=CoreCLR \
  /p:Configuration=Release \
  Wasm.Browser.Sample.csproj
```

This internally runs `dotnet publish` with:
- `-bl:publish-browser.binlog` (binlog output)
- `/p:TargetArchitecture=wasm /p:TargetOS=browser`
- `/p:ExpandNested=true /p:Nested_RuntimeFlavor=CoreCLR`

**Binlog location:** `src/mono/sample/wasm/browser/publish-browser.binlog`

## How RuntimeFlavor Affects the Build

The `RuntimeFlavor` property controls which targets and props are imported:

| File | Mono | CoreCLR |
|------|------|---------|
| `WasmApp.InTree.props` | Imports `BrowserWasmApp.props` | Sets `InvariantGlobalization=false`, `WasmEnableWebcil=false` |
| `WasmApp.InTree.targets` | Imports `BrowserWasmApp.targets` | Imports `BrowserWasmApp.CoreCLR.targets` |

### Property Differences

| Property | Mono (default) | CoreCLR |
|----------|---------------|---------|
| `InvariantGlobalization` | (unset) | `false` |
| `WasmEnableWebcil` | (default: true) | `false` |
| Native link | Per-app emcc link (EmccCompile + wasm-ld) | Pre-linked in `corehost.proj` |

### Nested Build Property Forwarding

The sample uses a nested build pattern. `RuntimeFlavor` is listed in `NestedBuildProperty` items (line 45 of `Directory.Build.targets`). When `BuildSampleInTree` runs, it:

1. Sets `/p:Nested_RuntimeFlavor=CoreCLR` on the inner `dotnet publish` command
2. The inner build's `_ExpandNestedProps` target copies `Nested_RuntimeFlavor` → `RuntimeFlavor`
3. `WasmApp.InTree.targets` conditionally imports `BrowserWasmApp.CoreCLR.targets`

## Implementation

The CoreCLR native build is implemented in `BrowserWasmApp.CoreCLR.targets`. Unlike Mono (which re-links `dotnet.native.wasm` per-app via emcc), CoreCLR **pre-links** `dotnet.native.wasm` once during the runtime build (`corehost.proj`). The app build copies these pre-built native files from the runtime pack.

### Architecture

```
Mono app build:
  emcc compile (4 C files) → emcc link (.a + .o) → dotnet.native.wasm (per-app)

CoreCLR app build:
  runtime pack (dotnet.native.wasm) → copy to intermediate → WasmNativeAsset → SDK bundle
```

### Key implementation details:

1. **`WasmApp.Common.props`** is imported for shared property definitions (`WasmBuildAppDependsOn`, etc.)
2. **`WasmApp.Common.targets`** is imported for the shared target chain (`WasmBuildApp` → `_WasmBuildAppCore` → `PrepareInputsForWasmBuild` → `WasmGenerateAppBundle` → `_EmitWasmAssembliesFinal`)
3. **`WasmBuildNative=false`** skips the emcc compile/link chain (`_WasmBuildNativeCore`)
4. **`_SetupToolchain`** is overridden to mark emscripten as unavailable, which skips `_ReadWasmProps` (no `src/wasm-props.json` in CoreCLR pack)
5. **`_CoreCLRBrowserCopyNativeAssets`** copies pre-built native files from the runtime pack and adds them as `WasmNativeAsset` items
6. The SDK's `_ResolveWasmOutputs` / `ComputeWasmBuildAssets` / `GenerateWasmBootJson` consume these to produce the final `_framework/` bundle

## Key Files

| File | Purpose |
|------|---------|
| `src/mono/sample/wasm/browser/Wasm.Browser.Sample.csproj` | Sample project |
| `src/mono/sample/wasm/Directory.Build.targets` | `BuildSampleInTree` target, validation, nested prop forwarding |
| `src/mono/sample/wasm/Directory.Build.props` | Sets `TargetOS=browser`, `RuntimeIdentifier=browser-wasm` |
| `src/mono/sample/wasm/wasm.mk` | Makefile with `build`/`publish` targets |
| `src/mono/browser/build/WasmApp.InTree.props` | Conditional import of Mono vs CoreCLR props |
| `src/mono/browser/build/WasmApp.InTree.targets` | Conditional import of Mono vs CoreCLR targets |
| `src/mono/browser/build/BrowserWasmApp.CoreCLR.targets` | CoreCLR placeholder targets (WIP) |
| `src/mono/browser/build/BrowserWasmApp.targets` | Mono WASM app build targets (full implementation) |

## Comparing Binlogs

To compare Mono vs CoreCLR builds, produced binlogs:

- src/mono/sample/wasm/browser/publish-browser.binlog for CoreCLR
- .research/apppublish-mono-native.binlog for Mono (keep in mind some paths might be different because it's not the build the exactly same environment)