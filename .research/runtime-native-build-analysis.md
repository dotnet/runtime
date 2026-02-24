# Runtime Native Build Analysis — Mono vs CoreCLR

Sources: `runtime-mono.binlog`, `runtime-coreclr.binlog` | Build: `dotnet/runtime` CI | Target: `browser-wasm` / Release | SDK: `11.0.100-preview.1`

| | Mono | CoreCLR |
|--|------|---------|
| Total build duration | ~4,350s (~72.5 min) | ~2,246s (~37.4 min) |
| Build nodes | 2 | 5 |
| CI OS | **Linux** | **Windows** |

---

# Part 1: Mono Native Build

## Two Native Build Paths

The runtime build contains two distinct native compilation paths, both using the Emscripten toolchain (`emcc`):

1. **Native Libs Source Build** — `build-native.proj` compiles C source into `.a` static libraries via CMake + Ninja
2. **Test App Native Builds** — 3 test projects run the same Wasm native pipeline as an app build (same phases from `native-build-analysis.md`)

---

## Path 1: Native Libs Source Build

**Project:** `src/native/libs/build-native.proj` (project 1423)
**Duration:** 108,792ms (~109s) exclusive
**Mechanism:** Single `Exec` task → `build-native.sh` → CMake → Ninja → emcc

### Build Flow

```
MSBuild (build-native.proj)
  └─ BuildNativeUnix target (108,792ms)
       └─ Exec: build-native.sh
            ├─ CMake configure (27.4s)
            │    └─ Emscripten.cmake toolchain
            └─ Ninja build (141 C files, -j 1 serial)
                 └─ emcc compiles each .c → .o
                 └─ ar archives .o → .a static libs
```

### CMake Arguments

```
-DCLR_CMAKE_RUNTIME_MONO=1
-DCMAKE_BUILD_TYPE=RELEASE
-DCMAKE_TOOLCHAIN_FILE=.../Emscripten.cmake
-DCMAKE_ICU_DIR=.../microsoft.netcore.runtime.icu.transport/11.0.0-alpha.1.26063.1/runtimes/browser-wasm/native
-DCMAKE_TZD_DIR=.../system.runtime.timezonedata/11.0.0-beta.26071.1/contentFiles/any/any/data
-DCMAKE_EMCC_EXPORTED_FUNCTIONS=_free,_htons,_malloc,_sbrk,_memalign,_posix_memalign,_memset,_ntohs,stackAlloc,stackRestore,stackSave
-DCMAKE_EMCC_EXPORTED_RUNTIME_METHODS=FS,out,err,ccall,cwrap,setValue,getValue,UTF8ToString,...
```

### Compiled Libraries (141 source files total)

| Library | # Files | Purpose |
|---------|---------|---------|
| `libminipal.a` | 13 | Platform abstraction layer |
| `libaotminipal.a` | 13 | AOT platform abstraction |
| `libz.a` (zlib-ng 2.2.5) | 35 | Compression (zlib-compat) |
| `libzstd.a` (zstd 1.5.7) | 27 | Zstandard compression |
| `libSystem.IO.Compression.Native.a` | 2 | Managed compression P/Invoke |
| `libSystem.Native.a` | 28 | System P/Invoke (IO, networking, process, etc.) |
| `libSystem.Native.TimeZoneData.a` | 1 | Timezone data support |
| `libSystem.Native.TimeZoneData.Invariant.a` | 1 | Invariant timezone |
| `libSystem.Globalization.Native.a` | 11 | ICU globalization bindings |

Additionally installs pre-built ICU libraries and data files:
- `libicuuc.a`, `libicui18n.a`, `libicudata.a`
- `icudt.dat`, `icudt_CJK.dat`, `icudt_EFIGS.dat`, `icudt_no_CJK.dat`

### Time Breakdown (approximate)

| Phase | Duration |
|-------|----------|
| CMake configure | ~27s |
| minipal (26 files, 2 libs) | ~10s |
| zlib-ng (35 files) | ~25s |
| zstd (27 files) | ~20s |
| System.IO.Compression.Native (2 files) | ~2s |
| System.Native (28 files) | ~18s |
| System.Globalization.Native (11 files) | ~7s |
| Linking static libs + install | ~1s |

Note: Built with `ninja -j 1` (single-threaded). Parallelism could significantly reduce this.

### Target Chain

```
AcquireEmscriptenSdk → GenerateNativeVersionFile → GenerateEmccExports
  → BuildNativeCommon → BuildNativeUnix (108,792ms)
    → CopyNativeFiles (48ms)
```

### Emscripten SDK (built from source)

```
emsdk path:    src/mono/browser/emsdk/
emcc:          src/mono/browser/emsdk/emscripten/
clang/wasm-ld: src/mono/browser/emsdk/bin/
node:          src/mono/browser/emsdk/node/bin/
```

---

## Path 2: Test App Native Builds (3 projects)

These follow the **identical pipeline** documented in `native-build-analysis.md` for app builds, but run in Release mode with `-O2` optimization.

### Projects

| Project | ID | Project File |
|---------|----|-------------|
| System.Diagnostics.Tracing.Tests | 57367 | `src/libraries/System.Diagnostics.Tracing/tests/System.Diagnostics.Tracing.Tests.csproj` |
| Invariant.Tests | 115075 | `src/libraries/System.Runtime/tests/System.Globalization.Tests/Invariant/Invariant.Tests.csproj` |
| InvariantTimezone.Tests | 117049 | `src/libraries/System.Runtime/tests/System.Runtime.Tests/InvariantTimezone/System.Runtime.InvariantTimezone.Tests.csproj` |

### Phase Timings

| Phase | Target | Tracing (57367) | Invariant (115075) | InvariantTZ (117049) |
|-------|--------|-----------------|--------------------|---------------------|
| Setup | `_SetupEmscripten` | 0ms | 0ms | 0ms |
| Emcc Version Check | `_CheckEmccIsExpectedVersion` | 467ms | 593ms | 476ms |
| Interop Scan | `_ScanAssembliesDecideLightweightMarshaler` | 83ms | 76ms | 30ms |
| Interop Gen | `_GenerateManagedToNative` | 1,101ms | 709ms | 730ms |
| Compile (EmccCompile) | `_WasmCompileNativeSourceFiles` | **988ms** | **1,135ms** | **1,028ms** |
| Link (emcc → wasm-ld) | `_BrowserWasmLinkDotNet` | **11,163ms** | **11,218ms** | **10,688ms** |
| Post-Link Optimize | `_RunWasmOptPostLink` | **968ms** | **920ms** | **900ms** |
| **Total native pipeline** | | **~14.8s** | **~14.7s** | **~13.9s** |

### EmccCompile — Source Files (same 4 files per project)

```
pinvoke.c      → pinvoke.o
driver.c       → driver.o
corebindings.c → corebindings.o
runtime.c      → runtime.o
```

### Emcc Link Command

```
emcc "@emcc-default.rsp" -msimd128 "@emcc-link.rsp" "@emcc-link.rsp(local)"
```

#### Resolved Flags (Release)

```
-O2                            # Release optimization (vs -O0 in app debug build)
-v -g                          # Verbose + debug info
-fwasm-exceptions              # Native WASM exception handling
-s EXPORT_ES6=1
-s INITIAL_MEMORY=45678592     # ~43.6 MB initial heap (vs 32 MB in app build)
-s STACK_SIZE=5MB
-s WASM_BIGINT=1
-s LLD_REPORT_UNDEFINED
-s ERROR_ON_UNDEFINED_SYMBOLS=1
--emit-symbol-map              # Symbol map for debugging (not in app build)
```

#### Static Libraries Linked (27)

Same set as app build, plus:
- `libzstd.a` — Zstandard compression (not in app build)
- `libSystem.Native.TimeZoneData.a` — timezone data (not in app build)
- `libmono-component-diagnostics_tracing-static.a` — full diagnostics (app uses stub)
- `libmono-component-marshal-ilgen-static.a` — full marshal IL gen (app uses stub)

#### Post-Link: wasm-opt Pass

Invoked within the `_BrowserWasmLinkDotNet` Exec task:

```
wasm-opt --strip-target-features --post-emscripten -O2 \
  --low-memory-unused --zero-filled-memory \
  --pass-arg=directize-initial-contents-immutable \
  dotnet.native.wasm -o dotnet.native.wasm \
  -g --mvp-features --enable-bulk-memory --enable-exception-handling \
  --enable-multivalue --enable-mutable-globals --enable-reference-types \
  --enable-sign-ext --enable-simd
```

### _RunWasmOptPostLink — Additional wasm-opt Pass (NEW)

Not present in the app build. Runs **after** linking as a separate target:

```
wasm-opt --enable-simd --enable-exception-handling --enable-bulk-memory \
  --enable-simd --strip-dwarf \
  dotnet.native.wasm -o dotnet.native.wasm
```

**Purpose:** Strips DWARF debug info while preserving WASM feature flags. Takes ~900–968ms.

### Environment Variables

```
WASM_ENABLE_SIMD=1
WASM_ENABLE_EH=1
WASM_ENABLE_EVENTPIPE=1         # Enabled (vs 0 in app build)
ENABLE_JS_INTEROP_BY_VALUE=0
RUN_AOT_COMPILATION=0
ENABLE_AOT_PROFILER=0
ENABLE_DEVTOOLS_PROFILER=0
ENABLE_LOG_PROFILER=0
EM_FROZEN_CACHE=1
```

---

## Comparison: App Build vs Runtime Build

| Aspect | App Build (`native-build-analysis.md`) | Runtime Test Build |
|--------|---------------------------------------|-------------------|
| Optimization | `-O0` (debug) | **`-O2`** (release) |
| EmccCompile time | 4,463ms | ~1,000ms (fewer files? caching?) |
| Link time | 4,461ms | **10,688–11,218ms** (2.5× slower) |
| `_RunWasmOptPostLink` | absent | **900–968ms** |
| `_CheckEmccIsExpectedVersion` | not present | **467–593ms** |
| Initial heap | 32 MB | **~43.6 MB** |
| `--emit-symbol-map` | no | **yes** |
| `libzstd.a` | not linked | **linked** |
| Diagnostics components | stubs | **full implementations** |
| EventPipe | disabled | **enabled** |
| Native libs source | prebuilt from SDK packs | **built from source** (109s) |

### Why Link Is Slower

The `-O2` flag causes emcc to run LLVM optimization passes and `wasm-opt` during linking, which is absent at `-O0`. The link step internally runs:
1. `clang --version` check
2. `node compiler.mjs` — Emscripten JS compiler (symbol resolution)
3. `wasm-ld` — WebAssembly linker
4. `llvm-objcopy` — strip producers section
5. `node compiler.mjs` — JS glue generation
6. `wasm-opt -O2` — Binaryen optimization pass (~additional seconds)
7. `wasm-opt --print-function-map` — symbol map generation

Steps 6–7 do not execute at `-O0`, explaining the ~2.5× difference.

---

## Total Native Build Cost in Runtime CI

| Component | Duration | Notes |
|-----------|----------|-------|
| Native libs source build | **108,792ms** | CMake + 141 C files via ninja -j 1 |
| Test app #1 native pipeline | **~14,800ms** | System.Diagnostics.Tracing.Tests |
| Test app #2 native pipeline | **~14,700ms** | Invariant.Tests |
| Test app #3 native pipeline | **~13,900ms** | InvariantTimezone.Tests |
| **Total native work** | **~152s** | |

These run on different build nodes so wall-clock impact depends on parallelism. The native libs build (109s) is on the critical path as test apps depend on the produced `.a` files.

## Toolchain

| Component | Version/Path |
|-----------|-------------|
| Emscripten SDK | Built from source at `src/mono/browser/emsdk/` |
| Clang/LLD | LLD 19.1.0 (from dotnet-llvm-project) |
| CMake | 3.30.3 |
| Ninja | `/usr/bin/ninja` |
| Node.js | `emsdk/node/bin/node` |
| .NET SDK | 11.0.100-preview.1.26104.118 |

---

# Part 2: CoreCLR Native Build

The CoreCLR build has **three** native build components, all using CMake + Ninja + Emscripten. Unlike Mono, the CoreCLR build runs on **Windows** and does **not** produce per-test-app native wasm binaries — there are zero `EmccCompile` task invocations and zero `_BrowserWasmLinkDotNet` targets.

## Native Build Components

| Component | Project | Script | Duration | Ninja Steps |
|-----------|---------|--------|----------|-------------|
| CoreCLR Runtime Engine | `runtime.proj` | `build-runtime.cmd` | **911,479ms** (~15.2 min) | **840** |
| Native Libs | `build-native.proj` | `build-native.cmd` | **376,193ms** (~6.3 min) | **144** |
| CoreHost / BrowserHost | `corehost.proj` | `build.cmd` | **88,533ms** (~89s) | **14** |
| **Total native** | | | **~1,376s** (~23 min) | **998** |

---

## Component 1: CoreCLR Runtime Engine (911s)

**Project:** `src/coreclr/runtime.proj` (project 111)
**Target:** `BuildRuntime` → Exec: `build-runtime.cmd`

This is the **biggest difference from Mono** — CoreCLR compiles the entire runtime engine (GC, JIT/interpreter, type system, etc.) from C/C++ source via emcc. Mono does not need this step because pre-built `.a` static libraries are provided.

### Build Flow

```
MSBuild (runtime.proj)
  ├─ _AcquireLocalEmscriptenSdk (4,646ms)
  ├─ GenerateEmccExports
  └─ BuildRuntime (911,479ms)
       └─ Exec: build-runtime.cmd
            ├─ CMake configure (525.1s = ~8.75 min!)
            └─ Ninja build (840 steps)
                 └─ emcc compiles C/C++ → .o → .a
```

### CMake Arguments (CoreCLR-specific)

```
-S src/coreclr                          # Source: CoreCLR (not src/mono!)
-DCLR_CMAKE_RUNTIME_CORECLR=1          # CoreCLR runtime mode
-DFEATURE_INTERPRETER=1                 # IL Interpreter enabled
-DCLR_CMAKE_KEEP_NATIVE_SYMBOLS=true   # Keep debug symbols
-DCDAC_BUILD_TOOL_BINARY_PATH=...      # cDAC build tool
-DCLR_DOTNET_RID=browser-wasm
-DCLR_CMAKE_PGO_INSTRUMENT=0
-DCLR_CMAKE_PGO_OPTIMIZE=0
```

Common flags shared with Mono:
```
-DFEATURE_SINGLE_THREADED=1
-DFEATURE_PERFTRACING_PAL_WS=1
-DFEATURE_PERFTRACING_DISABLE_THREADS=1
-DFEATURE_PERFTRACING_DISABLE_DEFAULT_LISTEN_PORT=1
-DFEATURE_PERFTRACING_DISABLE_PERFTRACING_LISTEN_PORTS=1
-DGEN_PINVOKE=1
-DBUILD_LIBS_NATIVE_BROWSER=1
```

### Why CMake Configure Takes 525s

The CoreCLR source tree (`src/coreclr`) is much larger than `src/native/libs`. On Windows with emcmake, each `try_compile` / `check_type_size` / feature detection test invokes emcc as a subprocess, which is significantly slower than on Linux due to process spawn overhead.

---

## Component 2: Native Libs (376s)

**Project:** `src/native/libs/build-native.proj` (project 1106)
**Target:** `BuildNativeWindows` → Exec: `build-native.cmd`

Same source code as the Mono build but runs on Windows.

### Build Flow

```
MSBuild (build-native.proj)
  └─ BuildNativeWindows (376,193ms)
       └─ Exec: build-native.cmd
            ├─ CMake configure (313.2s = ~5.2 min)
            └─ Ninja build (144 steps)
```

### CMake Arguments

Identical to Mono's `build-native.proj` except:
```
-DCLR_CMAKE_RUNTIME_CORECLR=1    # (Mono uses -DCLR_CMAKE_RUNTIME_MONO=1)
```

### Libraries Produced

Same set as Mono: `libz.a`, `libzstd.a`, `libSystem.Native.a`, `libSystem.IO.Compression.Native.a`, `libSystem.Globalization.Native.a`, `libminipal.a`, etc., plus ICU data.

---

## Component 3: CoreHost / BrowserHost (89s)

**Project:** `src/native/corehost/corehost.proj` (project 12968)
**Target:** `BuildCoreHostOnWindows` → Exec: `build.cmd`

This component **does not exist in the Mono build**. It produces the browser host entry point and **links `dotnet.native.wasm` and `dotnet.native.js` once**, rather than per-app.

### Build Flow

```
MSBuild (corehost.proj)
  └─ BuildCoreHostOnWindows (88,533ms)
       └─ Exec: build.cmd
            ├─ Visual Studio 2026 Developer Command Prompt (v18.3.0-insiders)
            ├─ CMake configure (11.7s)
            └─ Ninja build (14 steps)
                 ├─ browserhost: empty.c, browserhost.cpp → libBrowserHost.a
                 ├─ hostmisc: trace.cpp, fx_ver.cpp, utils.cpp, pal.unix.cpp → libhostmisc.a
                 └─ Link: emcc → dotnet.native.js + dotnet.native.wasm
```

### CMake Arguments (CoreHost-specific)

```
-S src/native/corehost
-DCLR_CMAKE_RUNTIME_CORECLR=1
-DCLI_CMAKE_COMMIT_HASH=572c02ab52c237e84056eb68906bb89860bd2086
-DCLI_CMAKE_FALLBACK_OS=browser
-DCLR_CMAKE_TARGET_ARCH=wasm
-DCLR_CMAKE_TARGET_OS=browser
```

### Output

```
artifacts/bin/browser-wasm.Release/
  ├─ corehost/
  │   ├─ dotnet.native.js
  │   ├─ dotnet.native.wasm
  │   └─ dotnet.native.js.symbols
  └─ sharedFramework/
      ├─ libBrowserHost.a
      ├─ dotnet.native.js
      ├─ dotnet.native.wasm
      └─ dotnet.native.js.symbols
```

---

# Part 3: Mono vs CoreCLR Comparison

## Architecture

```
MONO:
  build-native.proj (libs)  →  .a static libs
                                    ↓
  Per-test-app:  EmccCompile (4 C files) → emcc link (.a + .o) → dotnet.native.wasm
                                    ↑
  Mono runtime .a libs (pre-built: libmonosgen-2.0.a, libmono-ee-interp.a, etc.)

CORECLR:
  runtime.proj (CoreCLR engine)  →  .a static libs
  build-native.proj (libs)       →  .a static libs
  corehost.proj (browserhost)    →  dotnet.native.wasm (linked once)
```

**Key difference:** Mono links `dotnet.native.wasm` **per app** (embedding runtime + libs + app glue). CoreCLR links it **once** in the corehost step.

## Timing Comparison

| Component | Mono (Linux) | CoreCLR (Windows) | Ratio |
|-----------|-------------|-------------------|-------|
| **Runtime engine build** | — (pre-built) | **911s** | ∞ |
| **Native libs build** | **109s** | **376s** | 3.5× |
| ↳ CMake configure | 27.4s | 313.2s | 11.4× |
| ↳ Ninja compile | ~81s | ~63s | 0.8× |
| ↳ Ninja steps | 141 | 144 | ~same |
| **CoreHost/BrowserHost** | — | **89s** | N/A |
| ↳ CMake configure | — | 11.7s | — |
| ↳ Ninja (14 steps) | — | ~77s | — |
| **Per-app EmccCompile** | **~1,050ms** × 3 | — (none) | N/A |
| **Per-app emcc link** | **~11,000ms** × 3 | — (none) | N/A |
| **Per-app wasm-opt** | **~930ms** × 3 | — (none) | N/A |
| **Total native** | **~152s** | **~1,376s** | **9.1×** |

## Why CoreCLR Native Build Is 9× Slower

1. **CoreCLR compiles the runtime engine from source** (840 ninja steps, 911s). Mono's runtime is pre-built as `.a` files in the SDK pack — it only needs to link them.

2. **CMake configure is dramatically slower on Windows** — 313s vs 27.4s for the same `build-native.proj` (11.4× slower). Each `try_compile` test spawns `emcc.bat` → python → clang, which has high process creation overhead on Windows.

3. **CoreCLR runtime CMake configure alone takes 525s** — nearly as long as the Mono build's entire native work (152s).

4. **No per-app native compilation in CoreCLR** — CoreCLR produces `dotnet.native.wasm` once in the corehost step. Mono compiles and links per test app (~14.8s each × 3 apps = ~44s), but this is dwarfed by CoreCLR's upfront cost.

## CMake Feature Flags Comparison

| Flag | Mono | CoreCLR |
|------|------|---------|
| `CLR_CMAKE_RUNTIME_MONO` | ✅ | — |
| `CLR_CMAKE_RUNTIME_CORECLR` | — | ✅ |
| `FEATURE_INTERPRETER` | — | ✅ |
| `FEATURE_SINGLE_THREADED` | ✅ (shared) | ✅ (shared) |
| `FEATURE_PERFTRACING_*` | ✅ (shared) | ✅ (shared) |
| `GEN_PINVOKE` | ✅ (shared) | ✅ (shared) |
| `BUILD_LIBS_NATIVE_BROWSER` | ✅ (shared) | ✅ (shared) |
| `CLR_CMAKE_PGO_INSTRUMENT` | — | ✅ (=0) |
| `CLR_CMAKE_PGO_OPTIMIZE` | — | ✅ (=0) |
| `CLR_CMAKE_KEEP_NATIVE_SYMBOLS` | — | ✅ |
| `CDAC_BUILD_TOOL_BINARY_PATH` | — | ✅ |

## Toolchain Comparison

| Component | Mono (Linux) | CoreCLR (Windows) |
|-----------|-------------|-------------------|
| CMake | 3.30.3 (`/usr/bin/cmake`) | VS 2026 Insiders bundled CMake |
| Ninja | `/usr/bin/ninja` (system) | VS 2026 Insiders bundled |
| Emscripten | `src/mono/browser/emsdk/` | `src/mono/browser/emsdk/` (same) |
| Clang/LLD | LLD 19.1.0 | LLD 19.1.0 (same) |
| Node.js | `emsdk/node/bin/node` | `emsdk/node/bin/node` (same) |
| .NET SDK | 11.0.100-preview.1 | 11.0.100-preview.1 (same) |
| Build script | `build-native.sh` | `build-native.cmd` |
| Dev prompt | N/A | Visual Studio 2026 v18.3.0-insiders |

## Other Non-Native Expensive Targets (CoreCLR)

| Target | Duration | Count | Notes |
|--------|----------|-------|-------|
| `CoreCompile` (Csc) | 682s | 580 invocations | C# compilation |
| `ILLinkTrimAssembly` | 189s | 91 invocations | IL trimming |
| `Restore` | 105s | 2 invocations | NuGet restore |
| `IlcCompile` | 46s | 2 invocations | NativeAOT |
| `DownloadAndInstallFirefox` | 43s | 1 | Browser test infra |
