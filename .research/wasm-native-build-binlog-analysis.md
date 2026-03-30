# WASM Native Build Analysis (from binlogs)

Analysis of the WASM build pipeline from three binlogs:
- `appbuild-mono-default.binlog` — `dotnet build` with `WasmNativeBuild=false`
- `appbuild-mono-native.binlog` — `dotnet build` with `WasmNativeBuild=true`
- `apppublish-mono-native.binlog` — `dotnet publish` with `WasmNativeBuild=true`

Sample project: `WasmBrowserMonoNativeBuild.csproj` (net10.0, RID=browser-wasm)

---

## 1. Entry Points

| Scenario | Entry Target | Hooks Into |
|----------|-------------|------------|
| `dotnet build` | `WasmBuildApp` | Runs **after** `Build` target (via `WasmBuildAppAfterThisTarget=Build`) |
| `dotnet publish` | `WasmTriggerPublishApp` | Runs **after** `Publish` target (via `WasmTriggerPublishAppAfterThisTarget=Publish`) |

### Publish Flow
`WasmTriggerPublishApp` re-invokes MSBuild with `WasmBuildingForNestedPublish=true` on the
entry target `WasmNestedPublishApp`. This nested invocation runs ILLink first, then the full
native build pipeline. The property `_WasmNestedPublishAppPreTarget` controls what runs before
the WASM app builder — it's `Publish` in the normal evaluation but becomes `ComputeFilesToPublish`
in the nested publish context.

---

## 2. Default Build (No Native) — What Happens

When `WasmNativeBuild=false` (and the wasm workload is not installed), the build skips all
native compilation. The WASM pipeline is reduced to:

```
_ResolveGlobalizationConfiguration
  └→ _ResolveWasmConfiguration
      └→ _ResolveWasmOutputs          [651ms] — ComputeWasmBuildAssets task
          └→ ResolveWasmOutputs
              └→ _GenerateBuildWasmBootJson [161ms]
                  └→ _AddWasmStaticWebAssets
                      └→ _WasmConfigurePreload → _AddWasmPreloadBuildProperties
```

**Key properties in default build:**
- `WasmNativeWorkload = false`
- `WasmNativeWorkloadAvailable = false`
- No Emscripten SDK paths set
- No `WasmBuildNative` property
- No `_WasmBuildAppCoreDependsOn` chain

The app uses the **precompiled runtime** from the runtime pack
(`Microsoft.NETCore.App.Runtime.Mono.browser-wasm`) without recompilation.

---

## 3. Native Build — Full Target Chain

When `WasmNativeBuild=true` (and `WasmNativeWorkload=true`), the full native compilation
pipeline executes. Here is the complete target dependency tree:

### 3.1 Top-level: `WasmBuildApp`

```
_WasmNativeForBuild                              — entry for build scenario
  └→ _GatherWasmFilesToBuild                     — collects managed assemblies
  └→ WasmBuildApp
      └→ _WasmBuildAppCore                       — main orchestrator
          ├→ _InitializeCommonProperties  [2ms]  — creates obj/.../wasm/for-build/ dir
          ├→ PrepareInputsForWasmBuild    [9ms]  — sets up toolchain + reads props
          │   ├→ _SetupToolchain
          │   │   └→ _SetupEmscripten     [1ms]  — validates Emscripten SDK paths
          │   ├→ _ReadWasmProps           [10ms] — ReadWasmProps task reads wasm.props
          │   └→ _SetWasmBuildNativeDefaults      — sets WasmBuildNative defaults
          ├→ _WasmBuildNativeCore                 — native compilation orchestrator
          │   ├→ _WasmCommonPrepareForWasmBuildNative
          │   ├→ PrepareForWasmBuildNative
          │   │   ├→ _CheckToolchainIsExpectedVersion
          │   │   └→ _PrepareForBrowserWasmBuildNative [5ms]
          │   ├→ _ScanAssembliesDecideLightweightMarshaler [362ms]
          │   │   └→ TASK: MarshalingPInvokeScanner — scans assemblies for P/Invoke
          │   ├→ _GenerateManagedToNative         [1.26s]
          │   │   ├→ TASK: Exec                   — runs helper tool
          │   │   └→ TASK: ManagedToNativeGenerator — scans pinvokes + icalls
          │   ├→ WasmLinkDotNet                   — linking orchestrator
          │   │   ├→ _WasmSelectRuntimeComponentsForLinking
          │   │   │   └→ _MonoSelectRuntimeComponents
          │   │   │       └→ _MonoComputeAvailableComponentDefinitions
          │   │   │           └→ _MonoReadAvailableComponentsManifest [2ms]
          │   │   ├→ _WasmCalculateInitialHeapSizeFromBitcodeFiles [17ms]
          │   │   │   └→ TASK: WasmCalculateInitialHeapSize
          │   │   ├→ _WasmWriteRspForCompilingNativeSourceFiles [1ms]
          │   │   │   └→ _BrowserWasmWriteCompileRsp (BeforeTargets)
          │   │   ├→ _WasmCompileNativeSourceFiles [4.46s] ← MOST EXPENSIVE
          │   │   │   └→ TASK: EmccCompile — compiles pinvoke.c, driver.c, corebindings.c etc.
          │   │   ├→ _WasmWriteRspForLinking
          │   │   │   └→ _BrowserWasmWriteRspForLinking (BeforeTargets)
          │   │   └→ _BrowserWasmLinkDotNet       [4.46s] ← SECOND MOST EXPENSIVE
          │   │       └→ TASK: Exec — runs emcc linker
          │   ├→ WasmAfterLinkSteps
          │   └→ _CompleteWasmBuildNative (AfterTargets)
          └→ _EmitWasmAssembliesFinal
```

### 3.2 Publish-only additions: `WasmTriggerPublishApp`

The publish flow adds these targets before the native build:

```
Publish
  └→ WasmTriggerPublishApp                       [14.4s] — triggers nested publish
      └→ (nested MSBuild invocation with WasmBuildingForNestedPublish=true)
          └→ WasmNestedPublishApp
              ├→ ComputeFilesToPublish
              │   ├→ ILLink                       [7ms] — IL trimming
              │   │   └→ _RunILLink               [3.02s] — actual ILLinker execution
              │   │       └→ PrepareForILLink
              │   │       └→ _ComputeManagedAssemblyToLink
              │   ├→ ProcessPublishFilesForWasm    [60ms]
              │   │   └→ _WasmNative (BeforeTargets)
              │   │   └→ _GatherWasmFilesToPublish
              │   ├→ ComputeWasmExtensions         [1ms]
              │   └→ _AddWasmWebConfigFile
              ├→ _GatherWasmFilesToPublish
              └→ _WasmBuildAppCore                 — same chain as build (see 3.1)
                  └→ ... (identical native pipeline)

  (post-publish static web asset processing)
  └→ GeneratePublishWasmBootJson                  [13ms]
  └→ _AddPublishWasmBootJsonToStaticWebAssets
  └→ _AddWasmPreloadPublishProperties
  └→ ResolvePublishStaticWebAssets
  └→ CopyFilesToPublishDirectory
```

**Publish also runs `_RunWasmOptPostLink` [686ms]** — wasm-opt optimization pass on the
linked `.wasm` file — which does NOT run in the build scenario.

---

## 4. Key Properties

### Properties that differ between default and native builds

| Property | Default (no native) | Native Build |
|----------|-------------------|--------------|
| `WasmNativeWorkload` | `false` | `true` |
| `WasmNativeWorkloadAvailable` | `false` | `true` |
| `WasmBuildNative` | *(not set)* | `true` |
| `IsWasmProject` | *(not set)* | `true` |
| `IsBrowserWasmProject` | *(not set)* | `true` |
| `WasmIsWorkloadAvailable` | *(not set)* | `true` |
| `EnableDefaultWasmAssembliesToBundle` | `false` | `true` |
| `_WasmNativeWorkloadNeeded` | *(not set)* | `true` |
| All `Emscripten*` paths | *(not set)* | Set to SDK pack paths |

### Properties specific to publish (vs build)

| Property | Build | Publish (nested) |
|----------|-------|-----------------|
| `_IsPublishing` | *(not set)* | `true` |
| `_WasmIsPublishing` | *(not set)* | `true` |
| `WasmBuildingForNestedPublish` | *(not set)* | `true` |
| `WasmBuildOnlyAfterPublish` | *(not set)* | `true` |
| `_WasmInNestedPublish_UniqueProperty_XYZ` | *(not set)* | `true` |
| `_WasmDebuggerSupport` | `true` | `false` |
| `_WasmEnableHotReload` | `true` | `false` |

### All WASM Properties (native build)

```
WasmAppBuilderTasksAssemblyPath = ...packs/Microsoft.NET.Runtime.WebAssembly.Sdk/.../WasmAppBuilder.dll
WasmAppHostDir = ...packs/Microsoft.NET.Runtime.WebAssembly.Sdk/.../WasmAppHost/
WasmAssemblyExtension = .wasm
WasmBuildAppAfterThisTarget = Build
WasmBuildNative = true
WasmBuildTasksAssemblyPath = ...packs/Microsoft.NET.Runtime.WebAssembly.Sdk/.../WasmBuildTasks.dll
WasmCachePath = ...packs/Microsoft.NET.Runtime.Emscripten.3.1.56.Cache.win-x64/.../emscripten/cache/
WasmClang = emcc
WasmDedup = true
WasmEmitSymbolMap = false
WasmEnableExceptionHandling = true
WasmEnableJsInteropByValue = false
WasmEnableSIMD = true
WasmEnableStreamingResponse = true
WasmEnableWebcil = true
WasmGenerateAppBundle = false
WasmNativeWorkload = true
WasmRuntimeAssetsLocation = _framework
WasmSingleFileBundle = false
WasmStripAOTAssemblies = false
WasmStripILAfterAOT = true
WasmTriggerPublishAppAfterThisTarget = Publish
_WasmDebuggerSupport = true
_WasmDefaultFlags = -msimd128
_WasmEnableHotReload = true
_WasmOutputFileName = dotnet.native.wasm
```

---

## 5. Key Items

### Emscripten Environment

```xml
@(EmscriptenEnvVars):
  EMSDK_PYTHON=...packs/Microsoft.NET.Runtime.Emscripten.3.1.56.Python.win-x64/.../python.exe
  PYTHONUTF8=1

@(EmscriptenPrependPATH):
  .../Emscripten.3.1.56.Python.win-x64/.../tools/
  .../Emscripten.3.1.56.Node.win-x64/.../tools/bin/
  .../Emscripten.3.1.56.Sdk.win-x64/.../tools/bin/
  .../Emscripten.3.1.56.Sdk.win-x64/.../tools/emscripten/
```

---

## 6. Key Tasks and What They Do

| Task | Target | Duration | Description |
|------|--------|----------|-------------|
| `ReadWasmProps` | `_ReadWasmProps` | 10ms | Reads wasm.props from runtime pack, sets native source/header/lib paths |
| `MarshalingPInvokeScanner` | `_ScanAssembliesDecideLightweightMarshaler` | 362ms | Scans managed assemblies for P/Invoke signatures to decide marshaling strategy |
| `ManagedToNativeGenerator` | `_GenerateManagedToNative` | 1.1s | Scans all assemblies for pinvokes and icalls, generates native bridge code |
| `MonoRuntimeComponentManifestReadTask` | `_MonoReadAvailableComponentsManifest` | 2ms | Reads available Mono runtime components manifest |
| `WasmCalculateInitialHeapSize` | `_WasmCalculateInitialHeapSizeFromBitcodeFiles` | 17ms | Calculates WASM initial heap size from bitcode files |
| `EmccCompile` | `_WasmCompileNativeSourceFiles` | 4.5s | Compiles C sources (pinvoke.c, driver.c, corebindings.c, etc.) with emcc |
| `Exec` (emcc link) | `_BrowserWasmLinkDotNet` | 4.5s | Links all objects into final `dotnet.native.wasm` with emcc |
| `ComputeWasmBuildAssets` | `_ResolveWasmOutputs` | 20ms | Resolves output file list for static web assets |
| `ILLink` (publish only) | `_RunILLink` | 3.0s | Trims unused IL from managed assemblies |
| wasm-opt (publish only) | `_RunWasmOptPostLink` | 686ms | Post-link optimization of the .wasm binary |

### emcc Linker Flags (from `_BrowserWasmLinkDotNet`)

```
-O0 -v -g
-fwasm-exceptions
-s EXPORT_ES6=1
-lexports.js
-s LLD_REPORT_UNDEFINED
-s ERROR_ON_UNDEFINED_SYMBOLS=1
-s INITIAL_MEMORY=33554432          ← WasmInitialHeapSize (32MB)
-s STACK_SIZE=5MB
-s WASM_BIGINT=1
--pre-js <runtime-pack>/src/es6/dotnet.pre.js
--js-library <runtime-pack>/src/es6/dotnet.lib.js
--extern-pre-js <runtime-pack>/src/es6/dotnet.extpre.js
--extern-post-js <runtime-pack>/src/es6/dotnet.extpost.js
```

Environment variables for emcc:
```
EMSDK_PYTHON=<python-pack>/python.exe
PYTHONUTF8=1
DOTNET_EMSCRIPTEN_LLVM_ROOT=<sdk-pack>/tools/bin
DOTNET_EMSCRIPTEN_BINARYEN_ROOT=<sdk-pack>/tools/
```

---

## 7. Target Dependency Chains (DependsOn properties)

These MSBuild properties define the target ordering:

```
WasmBuildAppDependsOn = _WasmBuildAppCore

_WasmBuildAppCoreDependsOn =
    _InitializeCommonProperties
    PrepareInputsForWasmBuild
    _WasmResolveReferences
    _WasmBuildNativeCore
    WasmGenerateAppBundle
    _EmitWasmAssembliesFinal

_WasmBuildNativeCoreDependsOn =
    _WasmCommonPrepareForWasmBuildNative
    PrepareForWasmBuildNative
    _ScanAssembliesDecideLightweightMarshaler
    _WasmAotCompileApp              ← only when RunAOTCompilation=true
    _WasmStripAOTAssemblies         ← only when RunAOTCompilation=true
    _GenerateManagedToNative
    WasmLinkDotNet
    WasmAfterLinkSteps

PrepareInputsForWasmBuildDependsOn =
    _SetupToolchain
    _ReadWasmProps
    _SetWasmBuildNativeDefaults
    _GetDefaultWasmAssembliesToBundle

PrepareForWasmBuildNativeDependsOn =
    _CheckToolchainIsExpectedVersion
    _PrepareForBrowserWasmBuildNative

WasmLinkDotNetDependsOn =
    _CheckToolchainIsExpectedVersion
    _WasmSelectRuntimeComponentsForLinking
    _WasmCompileAssemblyBitCodeFilesForAOT  ← only when AOT
    _WasmCalculateInitialHeapSizeFromBitcodeFiles
    _WasmWriteRspForCompilingNativeSourceFiles
    _WasmCompileNativeSourceFiles
    _GenerateManagedToNative                ← generates pinvoke tables
    _WasmWriteRspForLinking
    _BrowserWasmLinkDotNet                  ← actual emcc link

WasmNestedPublishAppDependsOn =
    _GatherWasmFilesToPublish
    _WasmBuildAppCore                       ← same core pipeline
```

---

## 8. SDK Packs Used

The native build requires these workload packs (Emscripten 3.1.56, .NET 10.0.3):

| Pack | Content |
|------|---------|
| `Microsoft.NET.Runtime.WebAssembly.Sdk` | MSBuild targets, tasks (WasmAppBuilder.dll, WasmBuildTasks.dll) |
| `Microsoft.NET.Runtime.MonoTargets.Sdk` | Mono-specific tasks (MonoTargetsTasks.dll) |
| `Microsoft.NETCore.App.Runtime.Mono.browser-wasm` | Precompiled runtime, native sources (pinvoke.c, driver.c), JS glue |
| `Microsoft.NET.Runtime.Emscripten.3.1.56.Sdk.win-x64` | emcc, clang, lld, binaryen |
| `Microsoft.NET.Runtime.Emscripten.3.1.56.Node.win-x64` | Node.js for Emscripten |
| `Microsoft.NET.Runtime.Emscripten.3.1.56.Python.win-x64` | Python for Emscripten |
| `Microsoft.NET.Runtime.Emscripten.3.1.56.Cache.win-x64` | Prebuilt Emscripten cache (libc, etc.) |
| `Microsoft.NET.Sdk.WebAssembly.Pack` | WebAssembly SDK integration, boot JSON generation |

---

## 9. Timing Summary

### Build (WasmNativeBuild=true)

| Phase | Duration |
|-------|----------|
| Restore | 3.4s |
| Normal build (Compile, etc.) | ~1s |
| `_ScanAssembliesDecideLightweightMarshaler` | 362ms |
| `_GenerateManagedToNative` | 1.26s |
| `_WasmCompileNativeSourceFiles` (EmccCompile) | **4.46s** |
| `_BrowserWasmLinkDotNet` (emcc link) | **4.46s** |
| `_ResolveWasmOutputs` | 219ms |
| **Total** | ~12s |

### Publish (WasmNativeBuild=true)

| Phase | Duration |
|-------|----------|
| Restore | ~1s |
| Normal build | ~2s |
| `_RunILLink` (IL trimming) | **3.02s** |
| `_WasmCompileNativeSourceFiles` | **3.90s** |
| `_BrowserWasmLinkDotNet` + `_RunWasmOptPostLink` | **10.45s** |
| **Total** | ~28s |

---

## 10. How to Recreate the Build

To reproduce what `dotnet build` with `WasmNativeBuild=true` does:

1. **Scan assemblies** for P/Invoke signatures → `MarshalingPInvokeScanner`
2. **Generate native bridge** code (pinvoke tables, icall tables) → `ManagedToNativeGenerator`
3. **Read runtime component manifest** and select components for linking
4. **Calculate initial heap size** from bitcode files
5. **Compile native sources** with `emcc`:
   - Sources: `pinvoke.c`, `driver.c`, `corebindings.c` from runtime pack
   - Plus generated pinvoke/icall tables
   - Flags: `-msimd128`, platform-specific
6. **Link with emcc** into `dotnet.native.wasm`:
   - Objects from step 5
   - Runtime bitcode libraries
   - JS glue files (`dotnet.pre.js`, `dotnet.lib.js`, etc.)
   - Flags: `-fwasm-exceptions`, SIMD, ES6 exports, 32MB initial memory
7. **Resolve outputs** and register as static web assets
8. **Generate boot JSON** for the browser runtime loader

For **publish**, insert before step 1:
- **IL Link/Trim** managed assemblies (removes unused code)
- After step 6: **Run wasm-opt** for binary optimization

---

## 11. Targets Unique to Each Scenario

### Only in native build (not in default)
```
WasmBuildApp, _WasmBuildAppCore, _WasmBuildNativeCore, _WasmNativeForBuild
_InitializeCommonProperties, PrepareInputsForWasmBuild, _SetupToolchain, _SetupEmscripten
_ReadWasmProps, _SetWasmBuildNativeDefaults, PrepareForWasmBuildNative
_WasmCommonPrepareForWasmBuildNative, _CheckToolchainIsExpectedVersion
_PrepareForBrowserWasmBuildNative, _ScanAssembliesDecideLightweightMarshaler
_GenerateManagedToNative, WasmLinkDotNet, _WasmSelectRuntimeComponentsForLinking
_MonoSelectRuntimeComponents, _MonoComputeAvailableComponentDefinitions
_MonoReadAvailableComponentsManifest, _WasmCalculateInitialHeapSizeFromBitcodeFiles
_BrowserWasmWriteCompileRsp, _WasmWriteRspForCompilingNativeSourceFiles
_WasmCompileNativeSourceFiles, _BrowserWasmWriteRspForLinking, _WasmWriteRspForLinking
_BrowserWasmLinkDotNet, _CompleteWasmBuildNative, WasmAfterLinkSteps
_EmitWasmAssembliesFinal, _GatherWasmFilesToBuild
```

### Only in publish (not in build)
```
WasmTriggerPublishApp, WasmNestedPublishApp, _WasmPrepareForPublish
_WasmNative, ProcessPublishFilesForWasm, _GatherWasmFilesToPublish
ComputeWasmExtensions, _AddWasmWebConfigFile, GeneratePublishWasmBootJson
_AddPublishWasmBootJsonToStaticWebAssets, _AddWasmPreloadPublishProperties
ILLink, _RunILLink, PrepareForILLink, _ComputeManagedAssemblyToLink
_RunWasmOptPostLink
ComputeFilesToPublish, CopyFilesToPublishDirectory, Publish
(+ all static web asset publish targets)
```
