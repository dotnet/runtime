# WASM Native Build Analysis

Analysis of the WASM Browser build pipeline based on three binlogs:
- `appbuild-mono-default.binlog` — `dotnet build` with no native build (`WasmBuildNative` is not set)
- `appbuild-mono-native.binlog` — `dotnet build` with native build (`WasmBuildNative=true`)
- `apppublish-mono-native.binlog` — `dotnet publish` with native build (`WasmBuildNative=true`)

---

## 1. High-Level Architecture

### Two Entry Points

| Scenario | Entry Target | Invoked By |
|----------|-------------|------------|
| `dotnet build` | `WasmBuildApp` | Hooked via `_WasmNativeForBuild` into the build after `AfterBuild` |
| `dotnet publish` | `WasmTriggerPublishApp` | Hooked into publish pipeline; launches a **nested MSBuild** invocation calling `WasmNestedPublishApp` |

### Key Distinction: Build vs Publish

- **Build path**: The WASM native pipeline runs inline in the same project evaluation. `WasmBuildApp` → `_WasmBuildAppCore` → native compilation.
- **Publish path**: `WasmTriggerPublishApp` invokes an `MSBuild` task targeting the same `.csproj` with target `WasmNestedPublishApp`. This creates a **separate project evaluation** (Project ID 177 in the binlog). The nested project does `_GatherWasmFilesToPublish` → `_WasmBuildAppCore` → full native pipeline.

---

## 2. .targets File Layout (Source Files)

The build logic is split across several MSBuild .targets files from different packs/SDKs:

| File | Pack / SDK | Purpose |
|------|-----------|---------|
| `Microsoft.NET.Sdk.WebAssembly.Browser.targets` | `Microsoft.NET.Sdk.WebAssembly.Pack` (NuGet) | Browser-specific DependsOn hookups, `_GatherWasmFilesToBuild`, `_GatherWasmFilesToPublish`, `ProcessPublishFilesForWasm`, `_ResolveWasmConfiguration`, `_ResolveWasmOutputs`, boot JSON generation |
| `Microsoft.NET.Sdk.WebAssembly.Pack.targets` | `Microsoft.NET.Sdk.WebAssembly.Pack` (NuGet) | `ComputeWasmBuildAssets`, `ConvertDllsToWebCil`, `ComputeWasmExtensions`, `GeneratePublishWasmBootJson` |
| `Sdk.targets` | `Microsoft.NET.Runtime.WebAssembly.Sdk` (workload pack) | Core wasm build pipeline: `_WasmBuildAppCore`, `WasmBuildApp`, `_WasmBuildNativeCore`, `WasmLinkDotNet`, native compile/link |
| `BrowserWasmApp.targets` | `Microsoft.NET.Runtime.WebAssembly.Sdk` (workload pack) | Browser-specific native: `_SetupEmscripten`, `_PrepareForBrowserWasmBuildNative`, `_BrowserWasmWriteCompileRsp`, `_BrowserWasmWriteRspForLinking`, `_BrowserWasmLinkDotNet` |
| `WasmApp.Common.targets` | `Microsoft.NET.Runtime.WebAssembly.Sdk` (workload pack) | Common properties, `_InitializeCommonProperties`, `_ReadWasmProps`, `_SetWasmBuildNativeDefaults`, `_WasmCalculateInitialHeapSizeFromBitcodeFiles` |
| `Sdk.targets` | `Microsoft.NET.Runtime.MonoTargets.Sdk` (workload pack) | `_MonoReadAvailableComponentsManifest`, `_MonoComputeAvailableComponentDefinitions`, `_MonoSelectRuntimeComponents` |
| `RuntimeComponentManifest.targets` | `Microsoft.NET.Runtime.MonoTargets.Sdk` (workload pack) | Mono runtime component selection |

---

## 3. Target Dependency Chains

### 3.1 Build (Native) — `WasmBuildApp`

```
WasmBuildApp
  DependsOn: _WasmBuildAppCore

_WasmBuildAppCore
  DependsOn:
    _InitializeCommonProperties
    PrepareInputsForWasmBuild
    _WasmResolveReferences
    _WasmBuildNativeCore
    WasmGenerateAppBundle           (not observed in binlog—skipped when WasmGenerateAppBundle=false)
    _EmitWasmAssembliesFinal

PrepareInputsForWasmBuild
  DependsOn:
    _SetupToolchain
    _ReadWasmProps
    _SetWasmBuildNativeDefaults
    _GetDefaultWasmAssembliesToBundle

_WasmBuildNativeCore
  DependsOn:
    _WasmCommonPrepareForWasmBuildNative
    PrepareForWasmBuildNative
    _ScanAssembliesDecideLightweightMarshaler
    _WasmAotCompileApp              (skipped when RunAOTCompilation!=true)
    _WasmStripAOTAssemblies         (skipped when RunAOTCompilation!=true)
    _GenerateManagedToNative
    WasmLinkDotNet
    WasmAfterLinkSteps

PrepareForWasmBuildNative
  DependsOn:
    _CheckToolchainIsExpectedVersion
    _PrepareForBrowserWasmBuildNative

WasmLinkDotNet
  DependsOn:
    _CheckToolchainIsExpectedVersion
    _WasmSelectRuntimeComponentsForLinking
    _WasmCompileAssemblyBitCodeFilesForAOT  (AOT only)
    _WasmCalculateInitialHeapSizeFromBitcodeFiles
    _WasmWriteRspForCompilingNativeSourceFiles
    _WasmCompileNativeSourceFiles
    _GenerateObjectFilesForSingleFileBundle (not observed)
    _WasmWriteRspForLinking
    _BrowserWasmLinkDotNet
```

### 3.2 Build (Default, No Native) — No `WasmBuildApp`

When `WasmBuildNative` is not set, the native pipeline does **NOT** run. Instead:

```
Build → AfterBuild (standard MSBuild)
         ↓
_ResolveWasmConfiguration
_GetWasmRuntimePackVersion
_ResolveWasmOutputs               ← Uses pre-built runtime pack assets directly
ResolveWasmOutputs
GenerateBuildRuntimeConfigurationFiles
_GenerateBuildWasmBootJson
_AddWasmStaticWebAssets
...static web assets pipeline...
```

The key difference: `_ResolveWasmOutputs` uses `ComputeWasmBuildAssets` + `ConvertDllsToWebCil` to produce WebCil files from assemblies, and copies pre-built `dotnet.native.js`/`dotnet.native.wasm` from the runtime pack. No emcc compilation occurs.

### 3.3 Publish (Native) — `WasmTriggerPublishApp`

```
Publish (Project 155)
  ↓
  Build → Compile → ...standard build...
  ↓
  _WasmPrepareForPublish
  PrepareForPublish
  _ComputeManagedAssemblyToLink
  PrepareForILLink
  _RunILLink                       ← ILLink (trimmer) runs BEFORE native build
  ILLink
  ↓
  _GatherWasmFilesToPublish
  _ResolveWasmConfiguration
  WasmTriggerPublishApp            ← MSBuild task → nested project
    ↓ (Project 177)
    WasmNestedPublishApp
      DependsOn:
        _GatherWasmFilesToPublish
        _WasmBuildAppCore          ← same chain as build (see 3.1)
    ↓ (back to Project 155)
  _WasmNative
  ProcessPublishFilesForWasm
  ComputeWasmExtensions
  GeneratePublishWasmBootJson
  _AddPublishWasmBootJsonToStaticWebAssets
  ...publish static web assets + compression pipeline...
  _RunWasmOptPostLink              ← wasm-opt optimization (publish-only)
  CopyFilesToPublishDirectory
```

---

## 4. Key Properties

### 4.1 Build-Controlling Properties

| Property | Default | Description |
|----------|---------|-------------|
| `WasmBuildNative` | `false` (build), `true` (publish) | Master switch for native compilation |
| `RunAOTCompilation` | `false` | Enables AOT compilation of managed assemblies to WASM |
| `WasmEnableExceptionHandling` | `true` | Uses WASM exception handling (`-fwasm-exceptions`) |
| `WasmEnableSIMD` | `true` | Enables SIMD instructions (`-msimd128`) |
| `WasmEnableES6` | (implied `true`) | Emits ES6 module output (`-s EXPORT_ES6=1`) |
| `WasmEnableThreads` | `false` | Enables threads/SharedArrayBuffer support |
| `WasmNativeStrip` | (see below) | Strip debug symbols from native output |
| `WasmNativeDebugSymbols` | (implied) | Include debug symbols |
| `WasmDedup` | `true` | Deduplicate assemblies |
| `WasmEmitSymbolMap` | `false` | Emit symbol map |
| `WasmOptimizationLevel` | `-O0` (debug), `-Oz` (release) | Emscripten optimization level |
| `PublishTrimmed` | `true` | Enables IL trimming (ILLink) before native build |
| `WasmGenerateAppBundle` | `false` | Whether to generate the app bundle folder structure |
| `WasmFingerprintAssets` | (implied) | Fingerprint static web assets |
| `WasmRuntimeAssetsLocation` | `_framework` | Where runtime assets are served from |
| `WasmInitialHeapSize` | Calculated (e.g., `33554432` = 32MB) | Initial WASM memory in bytes |

### 4.2 Path Properties

| Property | Value (from binlog) | Description |
|----------|--------------------|-------------|
| `EmscriptenSdkToolsPath` | `…/Microsoft.NET.Runtime.Emscripten.3.1.56.Sdk.win-x64/10.0.3/tools/` | Emscripten SDK root |
| `EmscriptenNodeToolsPath` | `…/Microsoft.NET.Runtime.Emscripten.3.1.56.Node.win-x64/10.0.3/tools/` | Node.js for emscripten |
| `EmscriptenUpstreamBinPath` | `…/tools/bin/` | Clang/wasm-ld binaries |
| `WasmClang` | `emcc` (set to full path by `_PrepareForBrowserWasmBuildNative`) | Emscripten C compiler |
| `_WasmOutputFileName` | `dotnet.native.wasm` | Output WASM binary name |
| `MicrosoftNetCoreAppRuntimePackDir` | `…/Microsoft.NETCore.App.Runtime.Mono.browser-wasm/10.0.3/` | Runtime pack root |
| `PublishDir` | `bin/Debug/net10.0/publish/` (adjusted by `_ResolveWasmConfiguration`) | Publish output dir |

---

## 5. Key Tasks and What They Do

### 5.1 Non-Native Build Tasks (Default Path)

| Task | Target | Duration | What It Does |
|------|--------|----------|-------------|
| `ComputeWasmBuildAssets` | `_ResolveWasmOutputs` | 198ms | Resolves which assemblies go to the WASM app, maps them to static web assets |
| `ConvertDllsToWebCil` | `_ResolveWasmOutputs` | 299ms | Converts .dll files to WebCil format (`.wasm` extension) for browser compatibility |
| `GenerateWasmBootJson` | `_GenerateBuildWasmBootJson` | ~161ms | Generates `blazor.boot.json` / boot config for the WASM runtime |

### 5.2 Native Build Tasks

| Task | Target | Duration | What It Does |
|------|--------|----------|-------------|
| `MarshalingPInvokeScanner` | `_ScanAssembliesDecideLightweightMarshaler` | 362ms | Scans assemblies for P/Invoke signatures to decide marshaling strategy |
| `Exec (mono-aot-cross)` | `_GenerateManagedToNative` | 140ms | Runs `mono-aot-cross.exe --print-icall-table` to generate `runtime-icall-table.h` |
| `ManagedToNativeGenerator` | `_GenerateManagedToNative` | 1121ms | Generates P/Invoke wrapper C code and registration tables from managed assemblies |
| `EmccCompile` | `_WasmCompileNativeSourceFiles` | 4463ms | Compiles C source files (`pinvoke.c`, `driver.c`, `corebindings.c`, `runtime.c`) to `.o` files using emcc |
| `Exec (emcc link)` | `_BrowserWasmLinkDotNet` | 4461ms | Links all `.o` files + static libraries (`.a`) into `dotnet.native.wasm` + `dotnet.native.js` |
| `WasmCalculateInitialHeapSize` | `_WasmCalculateInitialHeapSizeFromBitcodeFiles` | 17ms | Calculates minimum initial WASM memory from bitcode/object file sizes |

### 5.3 Publish-Only Tasks

| Task | Target | Duration | What It Does |
|------|--------|----------|-------------|
| `ILLink` | `_RunILLink` | 3022ms | Runs the IL Trimmer to remove unused managed code BEFORE native build |
| `MSBuild` | `WasmTriggerPublishApp` | 14441ms | Invokes nested MSBuild with target `WasmNestedPublishApp` |
| `Exec (wasm-opt)` | `_RunWasmOptPostLink` | 686ms | Runs Binaryen `wasm-opt` to optimize the linked `.wasm` binary |

---

## 6. Native Compilation Details

### 6.1 Source Files Compiled (via `EmccCompile`)

These C source files from the runtime pack are compiled during native build:

| Source File | Output | Description |
|-------------|--------|-------------|
| `pinvoke.c` | `pinvoke.o` | P/Invoke bridge code |
| `driver.c` | `driver.o` | WASM runtime driver |
| `corebindings.c` | `corebindings.o` | JS-to-managed interop bindings |
| `runtime.c` | `runtime.o` | Mono runtime initialization |

Source location: `Microsoft.NETCore.App.Runtime.Mono.browser-wasm/<version>/runtimes/browser-wasm/native/src/`

### 6.2 Compile Command Structure

```
emcc @<runtime-pack>/native/src/emcc-default.rsp
     @<obj-dir>/emcc-compile.rsp
     -c -o <output.o> <source.c>
```

The compile response file (`emcc-compile.rsp`) contains flags generated by `_BrowserWasmWriteCompileRsp`.

### 6.3 Link Command Structure

```
emcc @<runtime-pack>/native/src/emcc-default.rsp
     -msimd128
     @<runtime-pack>/native/src/emcc-link.rsp
     @<obj-dir>/emcc-link.rsp
```

The link response file (`emcc-link.rsp`, generated by `_BrowserWasmWriteRspForLinking`) contains:

**Flags:**
- `-O0` (debug) or `-Oz` (release)
- `-fwasm-exceptions` (when `WasmEnableExceptionHandling=true`)
- `-s EXPORT_ES6=1` (ES6 module)
- `-s INITIAL_MEMORY=33554432` (from `WasmInitialHeapSize`)
- `-s STACK_SIZE=5MB`
- `-s WASM_BIGINT=1`
- `-s LLD_REPORT_UNDEFINED`
- `-s ERROR_ON_UNDEFINED_SYMBOLS=1`

**Pre/Post JS files:**
- `--pre-js dotnet.es6.pre.js`
- `--js-library dotnet.es6.lib.js`
- `--extern-post-js dotnet.es6.extpost.js`

**Input files (linked in order):**
1. Compiled object files: `pinvoke.o`, `driver.o`, `corebindings.o`, `runtime.o`
2. Static libraries from runtime pack:
   - `libbrotlicommon.a`, `libbrotlidec.a`, `libbrotlienc.a`
   - `libicudata.a`, `libicui18n.a`, `libicuuc.a`
   - `libmono-component-debugger-static.a`
   - `libmono-component-diagnostics_tracing-stub-static.a`
   - `libmono-component-hot_reload-static.a`
   - `libmono-component-marshal-ilgen-stub-static.a`
   - `libmono-ee-interp.a`
   - `libmono-icall-table.a`
   - `libmono-profiler-aot.a`, `libmono-profiler-browser.a`, `libmono-profiler-log.a`
   - `libmono-wasm-eh-wasm.a`, `libmono-wasm-simd.a`
   - `libmonosgen-2.0.a`
   - `libSystem.Globalization.Native.a`
   - `libSystem.IO.Compression.Native.a`
   - `libSystem.Native.a`
   - `libz.a`
   - `wasm-bundled-timezones.a`

**Output:** `dotnet.native.js` + `dotnet.native.wasm`

**Exported Functions:**
`_free`, `_malloc`, `_sbrk`, `_memalign`, `_memset`, `stackAlloc`, `stackRestore`, `stackSave`,
`_emscripten_force_exit`, math functions (`_fmod`, `_atan2`, `_pow`, trig functions, etc.),
`___cpp_exception`

### 6.4 Environment Variables Set for emcc

| Variable | Value | Purpose |
|----------|-------|---------|
| `EMSDK_PYTHON` | `…/python.exe` | Python for emscripten |
| `DOTNET_EMSCRIPTEN_LLVM_ROOT` | `…/tools/bin` | LLVM tools path |
| `DOTNET_EMSCRIPTEN_BINARYEN_ROOT` | `…/tools/` | Binaryen tools |
| `DOTNET_EMSCRIPTEN_NODE_JS` | `…/node.exe` | Node.js binary |
| `EM_CACHE` | `…/emscripten/cache/` | Emscripten cache dir |
| `EM_FROZEN_CACHE` | `1` | Don't regenerate cache |
| `ENABLE_JS_INTEROP_BY_VALUE` | `0` | JS interop mode |
| `WASM_ENABLE_SIMD` | `1` | Runtime SIMD flag |
| `WASM_ENABLE_EH` | `1` | Runtime EH flag |
| `WASM_ENABLE_EVENTPIPE` | `0` | Diagnostics |
| `RUN_AOT_COMPILATION` | `0` | AOT compilation flag |

### 6.5 wasm-opt Post-Link (Publish Only)

After linking, `_RunWasmOptPostLink` runs Binaryen's `wasm-opt`:

```
wasm-opt --enable-simd --enable-exception-handling --enable-bulk-memory
         --enable-simd --strip-dwarf
         <obj>/dotnet.native.wasm -o <obj>/dotnet.native.wasm
```

This optimizes the WASM binary and strips debug info for release.

---

## 7. Runtime Component Selection

The `_MonoSelectRuntimeComponents` / `_WasmSelectRuntimeComponentsForLinking` targets select which Mono runtime components to link. This determines which `.a` files are included:

- **debugger** (`libmono-component-debugger-static.a`)
- **diagnostics_tracing** (stub in debug build)
- **hot_reload** (`libmono-component-hot_reload-static.a`)
- **marshal-ilgen** (stub — lightweight marshaler selected by `MarshalingPInvokeScanner`)

The component manifest is read from `_MonoReadAvailableComponentsManifest`.

---

## 8. Outputs

### 8.1 Native Build Outputs

All outputs go to `obj/<Configuration>/net10.0/wasm/for-build/` (build) or `obj/<Configuration>/net10.0/wasm/for-publish/` (publish):

| File | Description |
|------|-------------|
| `dotnet.native.wasm` | The compiled WASM binary (Mono runtime + native libs + P/Invoke stubs) |
| `dotnet.native.js` | Emscripten-generated JS glue code |
| `pinvoke.o` | Compiled P/Invoke bridge |
| `driver.o` | Compiled WASM driver |
| `corebindings.o` | Compiled JS interop bindings |
| `runtime.o` | Compiled runtime init |
| `pinvoke-table.h` | Generated P/Invoke lookup table |
| `runtime-icall-table.h` | Internal call table from `mono-aot-cross` |
| `emcc-compile.rsp` | Compile response file |
| `emcc-link.rsp` | Link response file |

### 8.2 Non-Native Build Outputs

When `WasmBuildNative=false`, the outputs are copied from the runtime pack directly:

| File | Source |
|------|--------|
| `dotnet.native.wasm` | `<runtime-pack>/runtimes/browser-wasm/native/dotnet.native.wasm` |
| `dotnet.native.js` | `<runtime-pack>/runtimes/browser-wasm/native/dotnet.native.js` |
| `*.wasm` (WebCil) | Generated by `ConvertDllsToWebCil` from managed assemblies |

### 8.3 Common Outputs (Both Paths)

| File | Generated By | Description |
|------|-------------|-------------|
| `blazor.boot.json` | `GenerateWasmBootJson` | Boot configuration listing assemblies, resources, config |
| `_framework/*` | Static Web Assets pipeline | Framework files served to browser |
| `*.gz`, `*.br` | Compression pipeline | Compressed static assets |

---

## 9. Flow Comparison Summary

```
┌─────────────────────────────────────────────────────────────┐
│                    DEFAULT BUILD (no native)                  │
│                                                               │
│  Build → Compile → ... → _ResolveWasmConfiguration            │
│                           _ResolveWasmOutputs                 │
│                             ├─ ComputeWasmBuildAssets          │
│                             └─ ConvertDllsToWebCil            │
│                           _GenerateBuildWasmBootJson           │
│                           _AddWasmStaticWebAssets              │
│                           ...compression & copy...             │
│                                                               │
│  NO emcc, NO linking, pre-built dotnet.native.* from pack     │
└─────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────┐
│                    NATIVE BUILD                               │
│                                                               │
│  Build → Compile → ... → _GatherWasmFilesToBuild              │
│                           WasmBuildApp                        │
│                             └─ _WasmBuildAppCore              │
│                                 ├─ _InitializeCommonProperties│
│                                 ├─ PrepareInputsForWasmBuild  │
│                                 │    ├─ _SetupToolchain       │
│                                 │    ├─ _ReadWasmProps        │
│                                 │    └─ _SetWasmBuildNativeDefaults│
│                                 ├─ _WasmBuildNativeCore       │
│                                 │    ├─ PrepareForWasmBuildNative│
│                                 │    │    ├─ _CheckToolchainIsExpectedVersion│
│                                 │    │    └─ _PrepareForBrowserWasmBuildNative│
│                                 │    ├─ _ScanAssembliesDecideLightweightMarshaler│
│                                 │    ├─ _GenerateManagedToNative│
│                                 │    │    ├─ mono-aot-cross --print-icall-table│
│                                 │    │    └─ ManagedToNativeGenerator│
│                                 │    └─ WasmLinkDotNet        │
│                                 │         ├─ _WasmSelectRuntimeComponentsForLinking│
│                                 │         ├─ _WasmCalculateInitialHeapSizeFromBitcodeFiles│
│                                 │         ├─ _WasmWriteRspForCompilingNativeSourceFiles│
│                                 │         ├─ _WasmCompileNativeSourceFiles (EmccCompile)│
│                                 │         ├─ _WasmWriteRspForLinking│
│                                 │         └─ _BrowserWasmLinkDotNet (Exec emcc link)│
│                                 └─ _EmitWasmAssembliesFinal   │
│                           _WasmNativeForBuild                 │
│                           _ResolveWasmOutputs                 │
│                           _GenerateBuildWasmBootJson           │
│                           ...compression & copy...             │
└─────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────┐
│                    PUBLISH (NATIVE)                           │
│                                                               │
│  Publish → Build (full compile)                               │
│          → ILLink (trim managed code)                         │
│          → WasmTriggerPublishApp                              │
│              └─ MSBuild → WasmNestedPublishApp (project 177) │
│                            ├─ _GatherWasmFilesToPublish       │
│                            └─ _WasmBuildAppCore (same as above)│
│                                ... native compile + link ...   │
│                                _RunWasmOptPostLink (wasm-opt)  │
│          → ProcessPublishFilesForWasm                         │
│          → ComputeWasmExtensions                              │
│          → GeneratePublishWasmBootJson                        │
│          → ...publish static web assets + compression...      │
│          → CopyFilesToPublishDirectory                        │
└─────────────────────────────────────────────────────────────┘
```

---

## 10. Timing Summary

### Build (Native) — Total ~16s

| Phase | Duration | Key Target |
|-------|----------|-----------|
| C# Compile | ~10ms | `CoreCompile` (cached/incremental) |
| P/Invoke Scan | 362ms | `_ScanAssembliesDecideLightweightMarshaler` |
| M2N Code Gen | 1261ms | `_GenerateManagedToNative` |
| emcc Compile | 4463ms | `_WasmCompileNativeSourceFiles` |
| emcc Link | 4461ms | `_BrowserWasmLinkDotNet` |
| Wasm Outputs | 219ms | `_ResolveWasmOutputs` |
| Boot JSON | 124ms | `_GenerateBuildWasmBootJson` |
| Static Assets | ~500ms | Compression + manifests |

### Build (Default) — Total ~7.5s

| Phase | Duration | Key Target |
|-------|----------|-----------|
| C# Compile | 2283ms | `CoreCompile` |
| Wasm Outputs | 651ms | `_ResolveWasmOutputs` (ComputeWasmBuildAssets + ConvertDllsToWebCil) |
| Boot JSON | 161ms | `_GenerateBuildWasmBootJson` |
| Static Assets | ~600ms | Compression + copy |

### Publish (Native) — Total ~29s

| Phase | Duration | Key Target |
|-------|----------|-----------|
| C# Compile | 2781ms | `CoreCompile` |
| ILLink | 3022ms | `_RunILLink` |
| Native (nested) | 14441ms | `WasmTriggerPublishApp` (includes all native phases) |
| wasm-opt | 686ms | `_RunWasmOptPostLink` |
| Publish Assets | ~5900ms | Compression + copy |

---

## 8. Mono vs CoreCLR WASM Native Build Comparison

### 8.1 Build System Architecture

When building for WASM browser, the build system selects the native pipeline based on `RuntimeFlavor`:

**Import chain** (in `WasmApp.InTree.targets`):
```xml
<!-- Mono: Full native build pipeline -->
<Import Project="BrowserWasmApp.targets" Condition="'$(RuntimeFlavor)' == 'Mono'" />
<!-- CoreCLR: Stub targets (not yet implemented) -->
<Import Project="BrowserWasmApp.CoreCLR.targets" Condition="'$(RuntimeFlavor)' == 'CoreCLR'" />
```

**CoreCLR stub** (`BrowserWasmApp.CoreCLR.targets`):
```xml
<Target Name="WasmBuildApp">
  <Message Importance="high" Text="WasmBuildApp for CoreCLR not implemented" />
</Target>
<Target Name="WasmTriggerPublishApp">
  <Message Importance="high" Text="WasmTriggerPublishApp for CoreCLR not implemented" />
</Target>
```

Additionally, `BrowserWasmApp.props` (which sets up `RuntimeIdentifier=browser-wasm`, `TargetOS=browser`, and imports `WasmApp.Common.props`) is **only imported when `RuntimeFlavor == 'Mono'`**:
```xml
<Import Project="BrowserWasmApp.props" Condition="'$(RuntimeFlavor)' == 'Mono'" />
```

For CoreCLR, some defaults are set directly in `WasmApp.InTree.props`:
```xml
<PropertyGroup Condition="'$(RuntimeFlavor)' == 'CoreCLR'">
  <InvariantGlobalization>false</InvariantGlobalization>
  <WasmEnableWebcil>false</WasmEnableWebcil>
</PropertyGroup>
```

### 8.2 Runtime Pack Name

Both flavors produce the **same pack name**: `Microsoft.NETCore.App.Runtime.{flavor}.browser-wasm`
- Mono: `Microsoft.NETCore.App.Runtime.Mono.browser-wasm`
- CoreCLR: `Microsoft.NETCore.App.Runtime.browser-wasm` (uses CoreCLR artifacts)

Both are placed in: `artifacts/bin/microsoft.netcore.app.runtime.browser-wasm/{Configuration}/runtimes/browser-wasm/native/`

### 8.3 Runtime Pack Native Directory Contents

#### Files SHARED between Mono and CoreCLR

| File | Purpose |
|------|---------|
| `dotnet.js` | Main runtime JavaScript |
| `dotnet.js.map` | Source map |
| `dotnet.runtime.js` | Runtime helper JS |
| `dotnet.runtime.js.map` | Source map |
| `dotnet.diagnostics.js` | Diagnostics support JS |
| `dotnet.diagnostics.js.map` | Source map |
| `dotnet.native.js` | Native bindings wrapper JS |
| `dotnet.native.js.symbols` | Symbol table for debugging |
| `dotnet.native.wasm` | Pre-linked WASM binary |
| `dotnet.d.ts` | TypeScript type definitions |
| `package.json` | NPM package metadata |
| `icudt.dat`, `icudt_CJK.dat`, etc. | ICU globalization data |
| **Shared native libs:** | |
| `libbrotlicommon.a` | Brotli compression |
| `libbrotlidec.a` | Brotli decompression |
| `libbrotlienc.a` | Brotli encoding |
| `libicudata.a` | ICU data |
| `libicui18n.a` | ICU internationalization |
| `libicuuc.a` | ICU unicode |
| `libz.a` | zlib compression |

#### Files ONLY in Mono runtime pack

| File | Purpose |
|------|---------|
| `dotnet.native.worker.mjs` | Threading web worker (conditional) |
| `wasm-bundled-timezones.a` | Bundled timezone data |
| **Mono runtime libraries:** | |
| `libmonosgen-2.0.a` | Mono runtime (SGen GC) |
| `libmono-ee-interp.a` | Mono interpreter |
| `libmono-icall-table.a` | Mono internal call table |
| `libmono-profiler-aot.a` | AOT profiler |
| `libmono-profiler-browser.a` | Browser profiler |
| `libmono-profiler-log.a` | Log profiler |
| `libmono-wasm-eh-wasm.a` | WASM exception handling |
| `libmono-wasm-simd.a` | WASM SIMD support |
| `libmono-component-debugger-static.a` | Debugger component |
| `libmono-component-diagnostics_tracing-stub-static.a` | Diagnostics tracing stub |
| `libmono-component-hot_reload-static.a` | Hot reload component |
| `libmono-component-marshal-ilgen-stub-static.a` | Marshal IL gen stub |
| **Source files for app re-linking (in `src/` subdir):** | |
| `src/pinvoke.c` | P/Invoke binding source |
| `src/driver.c` | WASM driver entry point |
| `src/runtime.c` | Runtime core |
| `src/corebindings.c` | .NET binding source |
| `src/emcc-default.rsp` | Emscripten compile flags |
| `src/emcc-link.rsp` | Emscripten linker flags |
| `src/wasm-props.json` | Feature detection/config |
| `src/es6/dotnet.es6.pre.js` | ES6 pre-JS module |
| `src/es6/dotnet.es6.lib.js` | ES6 JS library |
| `src/es6/dotnet.es6.extpost.js` | ES6 external post-JS |
| **Include headers (in `include/` subdir):** | |
| `include/wasm/*.h` | C headers for native compilation |

#### Files ONLY in CoreCLR runtime pack

| File | Purpose |
|------|---------|
| `System.Private.CoreLib.dll` | CoreLib (managed runtime) |
| `System.Private.CoreLib.pdb` | CoreLib debug symbols |
| `System.Private.CoreLib.xml` | CoreLib XML docs |
| `System.Private.CoreLib.deps.json` | CoreLib dependencies |
| **CoreCLR runtime libraries:** | |
| `libcoreclr_static.a` | CoreCLR runtime (static) |
| `libcoreclrminipal.a` | CoreCLR minimal PAL |
| `libcoreclrpal.a` | CoreCLR platform abstraction |
| `libgcinfo_unix_wasm.a` | GC info for WASM |
| `libnativeresourcestring.a` | Native resource strings |
| `libminipal.a` | Minimal PAL |
| **Host libraries:** | |
| `libBrowserHost.a` | Browser host integration |
| `libBrowserHost.js` | Browser host JS |
| `libBrowserHost.js.map` | Source map |
| **Interop libraries:** | |
| `libSystem.Runtime.InteropServices.JavaScript.Native.a` | JS interop native |
| `libSystem.Runtime.InteropServices.JavaScript.Native.js` | JS interop JS |
| `libSystem.Runtime.InteropServices.JavaScript.Native.js.map` | Source map |
| `libSystem.Globalization.Native.a` | Globalization PAL |
| `libSystem.IO.Compression.Native.a` | Compression PAL |
| **Other libraries:** | |
| `libSystem.Native.a` | System PAL |
| `libSystem.Native.Browser.a` | Browser-specific system PAL |
| `libSystem.Native.Browser.js` | Browser-specific system JS |
| `libSystem.Native.Browser.Utils.js` | Browser-specific system utility JS |
| `libSystem.Native.Browser.js.map` | Source map |
| `libSystem.Native.Browser.Utils.js.map` | Source map |
| `libSystem.Native.TimeZoneData.a` | Timezone data |
| `libSystem.Native.TimeZoneData.Invariant.a` | Invariant timezone data |

### 8.4 Native Build Pipeline Comparison

| Aspect | Mono | CoreCLR |
|--------|------|---------|
| **Native Build Implemented** | ✅ Yes | ❌ No (stub targets) |
| **`WasmBuildNative` default** | `false` (build), `true` (publish/AOT) | `false` (always) |
| **Re-linking per-app** | ✅ Via emcc (recompiles C sources + links .a libs) | ❌ Uses pre-linked `dotnet.native.wasm` |
| **C source files in pack** | ✅ Yes (`src/` subdir: driver.c, runtime.c, etc.) | ❌ No source files |
| **RSP files in pack** | ✅ Yes (emcc-default.rsp, emcc-link.rsp) | ❌ No RSP files |
| **Header files in pack** | ✅ Yes (`include/wasm/`) | ❌ No headers |
| **JavaScript ES6 files** | ✅ Yes (`src/es6/`) | ❌ No ES6 sources |
| **Mono component selection** | ✅ Via `_MonoSelectRuntimeComponents` | N/A |
| **Key MSBuild target** | `_BrowserWasmLinkDotNet` (emcc compile+link) | No equivalent |
| **Build entry point** | `WasmBuildApp` → `_WasmBuildAppCore` | `WasmBuildApp` (prints "not implemented") |
| **Publish entry point** | `WasmTriggerPublishApp` → nested MSBuild | `WasmTriggerPublishApp` (prints "not implemented") |

### 8.5 Mono App Native Build: Detailed Link Command

When building a Mono app with `WasmBuildNative=true`, the following happens:

**1. Compile step** (`_WasmCompileNativeSourceFiles` via `EmccCompile` task):
- Compiles 4 C source files from runtime pack `src/` directory:
  - `pinvoke.c` → `pinvoke.o`
  - `driver.c` → `driver.o`
  - `corebindings.c` → `corebindings.o`
  - `runtime.c` → `runtime.o`
- Uses flags from `emcc-default.rsp` + optimization flags

**2. Link step** (`_BrowserWasmLinkDotNet` via emcc `Exec` task):
```
emcc @emcc-default.rsp @emcc-link.rsp @emcc-link-app.rsp
```
Where `emcc-link-app.rsp` contains:
- Object files: `pinvoke.o`, `driver.o`, `corebindings.o`, `runtime.o`
- All `.a` static libraries from the runtime pack native directory
- JavaScript files: `--pre-js`, `--js-library`, `--extern-post-js`
- Exported functions and runtime methods
- Memory settings: `INITIAL_MEMORY`, `STACK_SIZE`
- Output: `dotnet.native.js` + `dotnet.native.wasm`

**3. Environment variables set during linking:**
```
ENABLE_JS_INTEROP_BY_VALUE=0
WASM_ENABLE_SIMD=1
WASM_ENABLE_EVENTPIPE=0
WASM_ENABLE_EH=1
ENABLE_AOT_PROFILER=0
ENABLE_DEVTOOLS_PROFILER=0
ENABLE_LOG_PROFILER=0
RUN_AOT_COMPILATION=0
```

### 8.6 What's Needed to Implement CoreCLR App Native Build

To recreate the app native build for CoreCLR, the following gaps must be addressed:

**1. Source files needed:**
The CoreCLR runtime pack does NOT include C source files (`driver.c`, `runtime.c`, `pinvoke.c`, `corebindings.c`) or their equivalents. These would need to be:
- Created as CoreCLR-specific implementations, OR
- Adapted from the Mono versions to work with CoreCLR's runtime initialization

**2. RSP files needed:**
CoreCLR runtime pack lacks `emcc-default.rsp` and `emcc-link.rsp`. These define Emscripten flags for compile and link. They would need to be generated/adapted for CoreCLR's requirements.

**3. JavaScript ES6 modules:**
CoreCLR runtime pack lacks the `src/es6/` JavaScript files (`dotnet.es6.pre.js`, `dotnet.es6.lib.js`, `dotnet.es6.extpost.js`) needed for the emcc `--pre-js`, `--js-library`, `--extern-post-js` flags.

**4. Header files:**
CoreCLR runtime pack lacks the `include/wasm/` headers used during native compilation.

**5. `wasm-props.json`:**
CoreCLR runtime pack lacks this metadata file used by `_ReadWasmProps` to detect Emscripten version and configure relinking triggers.

**6. MSBuild targets implementation:**
`BrowserWasmApp.CoreCLR.targets` currently has only stub targets. It would need to implement:
- `WasmBuildApp` with the full emcc compile+link chain
- `WasmTriggerPublishApp` for the nested publish build
- Or adapt the existing Mono pipeline to work with CoreCLR libraries

**7. Library substitutions:**
The link command would need to replace all `libmono-*` libraries with their CoreCLR equivalents:

| Mono Library | CoreCLR Equivalent |
|-------------|-------------------|
| `libmonosgen-2.0.a` | `libcoreclr_static.a` + `libcoreclrpal.a` + `libcoreclrminipal.a` |
| `libmono-ee-interp.a` | N/A (CoreCLR uses JIT on WASM) |
| `libmono-icall-table.a` | Part of `libcoreclr_static.a` |
| `libmono-profiler-*.a` | Part of `libcoreclr_static.a` |
| `libmono-wasm-eh-wasm.a` | Part of `libcoreclr_static.a` |
| `libmono-wasm-simd.a` | Part of `libcoreclr_static.a` |
| `libmono-component-*.a` | Part of `libcoreclr_static.a` |
| N/A | `libgcinfo_unix_wasm.a` (CoreCLR-specific) |
| N/A | `libnativeresourcestring.a` (CoreCLR-specific) |
| N/A | `libminipal.a` (CoreCLR-specific) |
| N/A | `libBrowserHost.a` (CoreCLR-specific) |
| N/A | `libSystem.Runtime.InteropServices.JavaScript.Native.a` (CoreCLR-specific) |

**8. JavaScript library substitutions:**
The link command would need additional JS libraries for CoreCLR:
- `libBrowserHost.js` (replaces some of the Mono ES6 JS)
- `libSystem.Runtime.InteropServices.JavaScript.Native.js` (JS interop)

### 8.7 CoreCLR Sample Build (binlog: `appbuild-coreclr-sample.binlog`)

The CoreCLR sample (`Wasm.Browser.Sample.csproj`) was built with:
```
dotnet publish -p:RuntimeFlavor=CoreCLR -p:TargetOS=browser -p:TargetArchitecture=wasm
```

Key observations:
- `RuntimeFlavor=CoreCLR` is set as a **global property** (cannot be overridden)
- `WasmBuildNative=false` — no native compilation occurs
- `BrowserWasmApp.props` and `BrowserWasmApp.targets` are **NOT imported** (condition `RuntimeFlavor == 'Mono'` fails)
- `BrowserWasmApp.CoreCLR.targets` IS imported but only prints "not implemented" messages
- The pre-linked `dotnet.native.wasm` from the runtime pack is used directly
- `DefaultPrimaryRuntimeFlavor` is reassigned from `CoreCLR` to `Mono` at `eng/Subsets.props:67` (this is the default for browser-wasm target)
- `WasmEnableWebcil=false` is set for CoreCLR flavor
