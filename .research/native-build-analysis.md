# WasmBuildNative=true Build Analysis

Project: `WasmBrowserMonoNativeBuild.csproj` | Target: `net10.0` / `browser-wasm` | SDK: `10.0.103`

## Build Times

| Build | Total | Build (excl. restore) |
|-------|-------|-----------------------|
| Default | 7,459ms | ~4,000ms |
| Native (`WasmBuildNative=true`) | 16,094ms | ~12,700ms |
| **Delta** | **+8,635ms (+116%)** | |

## Native Build Pipeline

32 targets execute exclusively in the native build. Listed in execution order with source files and durations.

### Phase 1: Setup & Configuration

| Target | Duration | Source File |
|--------|----------|-------------|
| `_GatherWasmFilesToBuild` | 0ms | `Microsoft.NET.Sdk.WebAssembly.Browser.targets` (NuGet) |
| `_InitializeCommonProperties` | 2ms | `WasmApp.Common.targets` |
| `_SetupEmscripten` | 1ms | `BrowserWasmApp.targets` |
| `_SetupToolchain` | 0ms | `WasmApp.Common.targets` |
| `_ReadWasmProps` | 10ms | `WasmApp.Common.targets` |
| `_SetWasmBuildNativeDefaults` | 0ms | `WasmApp.Common.targets` |

All source files under `C:\Program Files\dotnet\packs\Microsoft.NET.Runtime.WebAssembly.Sdk\10.0.3\Sdk\` unless noted.

### Phase 2: Preparation

| Target | Duration | Source File |
|--------|----------|-------------|
| `PrepareInputsForWasmBuild` | 9ms | `WasmApp.Common.targets` |
| `_WasmCommonPrepareForWasmBuildNative` | 0ms | `WasmApp.Common.targets` |
| `_CheckToolchainIsExpectedVersion` | 0ms | `WasmApp.Common.targets` |
| `_PrepareForBrowserWasmBuildNative` | 5ms | `BrowserWasmApp.targets` |
| `PrepareForWasmBuildNative` | 0ms | `WasmApp.Common.targets` |

### Phase 3: Interop Generation

| Target | Duration | Source File |
|--------|----------|-------------|
| `_ScanAssembliesDecideLightweightMarshaler` | **362ms** | `WasmApp.Common.targets` |
| `_GenerateManagedToNative` | **1,261ms** | `WasmApp.Common.targets` |

- `MarshalingPInvokeScanner` task (344ms) scans assemblies for P/Invoke marshaling decisions.
- `ManagedToNativeGenerator` task (1,112ms) generates C interop code from managed assemblies.

### Phase 4: Runtime Component Selection

| Target | Duration | Source File |
|--------|----------|-------------|
| `_MonoReadAvailableComponentsManifest` | 2ms | `RuntimeComponentManifest.targets` |
| `_MonoComputeAvailableComponentDefinitions` | 0ms | `RuntimeComponentManifest.targets` |
| `_MonoSelectRuntimeComponents` | 1ms | `RuntimeComponentManifest.targets` |
| `_WasmSelectRuntimeComponentsForLinking` | 0ms | `WasmApp.Common.targets` |

Source: `C:\Program Files\dotnet\packs\Microsoft.NET.Runtime.MonoTargets.Sdk\10.0.3\Sdk\`

Selected components:
- `libmono-component-debugger-static.a`
- `libmono-component-diagnostics_tracing-stub-static.a`
- `libmono-component-hot_reload-static.a`
- `libmono-component-marshal-ilgen-stub-static.a`

### Phase 5: Emscripten Compile (4,463ms)

| Target | Duration | Source File |
|--------|----------|-------------|
| `_WasmCalculateInitialHeapSizeFromBitcodeFiles` | 17ms | `WasmApp.Common.targets` |
| `_BrowserWasmWriteCompileRsp` | 0ms | `BrowserWasmApp.targets` |
| `_WasmWriteRspForCompilingNativeSourceFiles` | 1ms | `WasmApp.Common.targets` |
| `_WasmCompileNativeSourceFiles` | **4,463ms** | `WasmApp.Common.targets` |

The `EmccCompile` task (from `WasmAppBuilder.dll`) compiles 4 C source files to `.o` object files:

```
pinvoke.c   → pinvoke.o
driver.c    → driver.o
corebindings.c → corebindings.o
runtime.c   → runtime.o
```

### Phase 6: Emscripten Link (4,461ms)

| Target | Duration | Source File |
|--------|----------|-------------|
| `_BrowserWasmWriteRspForLinking` | 1ms | `BrowserWasmApp.targets` |
| `_WasmWriteRspForLinking` | 0ms | `WasmApp.Common.targets` |
| `_BrowserWasmLinkDotNet` | **4,461ms** | `BrowserWasmApp.targets` |

Invokes `emcc` which calls `wasm-ld.exe` to produce `dotnet.native.wasm` and `dotnet.native.js`.

#### Linker Command

```
emcc "@emcc-default.rsp" -msimd128 "@emcc-link.rsp" "@emcc-link.rsp(local)"
```

#### Resolved Flags

```
-O0                            # Debug, no optimization
-v -g                          # Verbose + debug info
-fwasm-exceptions              # Native WASM exception handling
-s EXPORT_ES6=1                # ES6 module output
-s INITIAL_MEMORY=33554432     # 32 MB initial heap
-s STACK_SIZE=5MB
-s WASM_BIGINT=1
-s LLD_REPORT_UNDEFINED
-s ERROR_ON_UNDEFINED_SYMBOLS=1
```

#### Input Object Files

```
obj/Debug/net10.0/wasm/for-build/pinvoke.o
obj/Debug/net10.0/wasm/for-build/driver.o
obj/Debug/net10.0/wasm/for-build/corebindings.o
obj/Debug/net10.0/wasm/for-build/runtime.o
```

#### Static Libraries Linked (27)

| Library | Purpose |
|---------|---------|
| `libmonosgen-2.0.a` | Mono SGen GC runtime |
| `libmono-ee-interp.a` | Mono interpreter engine |
| `libmono-icall-table.a` | Internal call table |
| `libmono-wasm-eh-wasm.a` | WASM exception handling |
| `libmono-wasm-simd.a` | SIMD support |
| `libmono-component-debugger-static.a` | Debugger component |
| `libmono-component-diagnostics_tracing-stub-static.a` | Diagnostics stub |
| `libmono-component-hot_reload-static.a` | Hot reload component |
| `libmono-component-marshal-ilgen-stub-static.a` | Marshal IL gen stub |
| `libmono-profiler-aot.a` | AOT profiler |
| `libmono-profiler-browser.a` | Browser profiler |
| `libmono-profiler-log.a` | Log profiler |
| `libicudata.a` | ICU data |
| `libicui18n.a` | ICU internationalization |
| `libicuuc.a` | ICU common |
| `libSystem.Globalization.Native.a` | System.Globalization native |
| `libSystem.IO.Compression.Native.a` | System.IO.Compression native |
| `libSystem.Native.a` | System.Native |
| `libbrotlicommon.a` | Brotli common |
| `libbrotlidec.a` | Brotli decoder |
| `libbrotlienc.a` | Brotli encoder |
| `libz.a` | zlib |
| `wasm-bundled-timezones.a` | Timezone data |

Plus Emscripten sysroot libraries: `-lGL-getprocaddr -lal -lhtml5 -lbulkmemory -lstubs-debug -lc-debug -ldlmalloc -lcompiler_rt-wasm-sjlj -lc++-except -lc++abi-debug-except -lunwind-except -lsockets`

#### JavaScript Glue

```
--pre-js        dotnet.es6.pre.js
--js-library    dotnet.es6.lib.js
--extern-post-js dotnet.es6.extpost.js
```

#### Exported Functions

Math: `_fmod`, `_atan2`, `_fma`, `_pow`, `_sin`, `_cos`, `_tan`, `_exp`, `_log`, `_log2`, `_log10`, `_asin`, `_asinh`, `_acos`, `_acosh`, `_atan`, `_atanh`, `_cbrt`, `_cosh`, `_sinh`, `_tanh` + float variants (`_sinf`, `_cosf`, etc.)

Runtime: `_free`, `_malloc`, `_sbrk`, `_memalign`, `_posix_memalign`, `_memset`, `_htons`, `_ntohs`, `stackAlloc`, `stackRestore`, `stackSave`, `_emscripten_force_exit`, `___cpp_exception`

#### Exported JS Runtime Methods

```
FS, out, err, ccall, cwrap, setValue, getValue,
UTF8ToString, UTF8ArrayToString, lengthBytesUTF8, stringToUTF8Array,
FS_createPath, FS_createDataFile,
removeRunDependency, addRunDependency, addFunction,
safeSetTimeout, runtimeKeepalivePush, runtimeKeepalivePop,
maybeExit, abort, wasmExports
```

#### wasm-ld Flags

```
--initial-memory=33554432      # 32 MB
--max-memory=2147483648        # 2 GB
--stack-first
--no-entry
--growable-table
--table-base=1
-z stack-size=5242880          # 5 MB stack
```

#### Environment Variables

```
WASM_ENABLE_SIMD=1
WASM_ENABLE_EH=1
WASM_ENABLE_EVENTPIPE=0
ENABLE_JS_INTEROP_BY_VALUE=0
RUN_AOT_COMPILATION=0
ENABLE_AOT_PROFILER=0
ENABLE_DEVTOOLS_PROFILER=0
ENABLE_LOG_PROFILER=0
EM_FROZEN_CACHE=1
```

#### Output

```
obj/Debug/net10.0/wasm/for-build/dotnet.native.wasm
obj/Debug/net10.0/wasm/for-build/dotnet.native.js
```

Post-link: `llvm-objcopy` strips the `producers` section from `dotnet.native.wasm`.

### Phase 7: Finalize

| Target | Duration | Source File |
|--------|----------|-------------|
| `WasmLinkDotNet` | 0ms | `WasmApp.Common.targets` |
| `_CompleteWasmBuildNative` | 0ms | `BrowserWasmApp.targets` |
| `WasmAfterLinkSteps` | 0ms | `WasmApp.Common.targets` |
| `_WasmBuildNativeCore` | 0ms | `WasmApp.Common.targets` |
| `_EmitWasmAssembliesFinal` | 0ms | `WasmApp.Common.targets` |
| `_WasmBuildAppCore` | 0ms | `WasmApp.Common.targets` |
| `WasmBuildApp` | 0ms | `WasmApp.Common.targets` |
| `_WasmNativeForBuild` | 0ms | `Microsoft.NET.Sdk.WebAssembly.Browser.targets` (NuGet) |

## Task Comparison: Default vs Native

| Task | Default | Native | Delta |
|------|---------|--------|-------|
| `Csc` | 2,282ms | 10ms | -2,272ms (incremental) |
| `EmccCompile` | — | 4,456ms | 🆕 |
| `Exec` (emcc linker) | — | 4,460ms | 🆕 |
| `ManagedToNativeGenerator` | — | 1,112ms | 🆕 |
| `MarshalingPInvokeScanner` | — | 344ms | 🆕 |
| `ConvertDllsToWebCil` | 299ms | 35ms | -264ms |
| `ComputeWasmBuildAssets` | 198ms | — | removed |
| `GenerateWasmBootJson` | 126ms | 94ms | -32ms |
| `GZipCompress` | 99ms | 210ms | +111ms |

## Toolchain

| Component | Version | Path |
|-----------|---------|------|
| Emscripten SDK | 3.1.56 | `Microsoft.NET.Runtime.Emscripten.3.1.56.Sdk.win-x64\10.0.3` |
| Emscripten Node | 3.1.56 | `Microsoft.NET.Runtime.Emscripten.3.1.56.Node.win-x64\10.0.3` |
| Emscripten Python | 3.1.56 | `Microsoft.NET.Runtime.Emscripten.3.1.56.Python.win-x64\10.0.3` |
| Emscripten Cache | 3.1.56 | `Microsoft.NET.Runtime.Emscripten.3.1.56.Cache.win-x64\10.0.3` |
| Mono browser-wasm runtime | 10.0.3 | `Microsoft.NETCore.App.Runtime.Mono.browser-wasm\10.0.3` |
| WebAssembly SDK | 10.0.3 | `Microsoft.NET.Runtime.WebAssembly.Sdk\10.0.3` |
| MonoTargets SDK | 10.0.3 | `Microsoft.NET.Runtime.MonoTargets.Sdk\10.0.3` |
| WebAssembly Pack (NuGet) | 10.0.3 | `microsoft.net.sdk.webassembly.pack\10.0.3` |

## MSBuild Source Files

| File | # Targets | Pack |
|------|-----------|------|
| `WasmApp.Common.targets` | 21 | `Microsoft.NET.Runtime.WebAssembly.Sdk` |
| `BrowserWasmApp.targets` | 6 | `Microsoft.NET.Runtime.WebAssembly.Sdk` |
| `RuntimeComponentManifest.targets` | 3 | `Microsoft.NET.Runtime.MonoTargets.Sdk` |
| `Microsoft.NET.Sdk.WebAssembly.Browser.targets` | 2 | `microsoft.net.sdk.webassembly.pack` (NuGet) |
