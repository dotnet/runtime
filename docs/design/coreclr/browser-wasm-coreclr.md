# CoreCLR on WebAssembly (Browser)

> **Status:** Experimental / In Development. Not yet available as a supported public workload.

For build and debugging instructions, see [Building CoreCLR for WebAssembly](../../workflow/building/coreclr/wasm.md).
For the JIT design document, see [WebAssembly overview for JIT](jit/WebAssembly%20overview%20for%20JIT.md).
For the existing Mono-based WebAssembly documentation, see [Mono WASM features](../../../src/mono/wasm/features.md).

## Table of contents

- [Configuring browser features](#configuring-browser-features)
- [Project folder structure](#project-folder-structure)
- [Hosting the application](#hosting-the-application)
- [Resources consumed on the target device](#resources-consumed-on-the-target-device)
- [Key differences from Mono WASM](#key-differences-from-mono-wasm)
- [JavaScript host API](#javascript-host-api)
- [Developer tools](#developer-tools)

## Configuring browser features

CoreCLR on WebAssembly brings the full CoreCLR runtime to the browser, compiled to WebAssembly via [Emscripten](https://emscripten.org/). It uses the CoreCLR interpreter as its primary execution engine, the standard CoreCLR garbage collector in workstation mode, and the same `[JSImport]`/`[JSExport]` interop system used by Mono WASM.

For a support matrix of WebAssembly features see [https://webassembly.org/roadmap/](https://webassembly.org/roadmap/).

### Execution model

CoreCLR on WASM uses the **CoreCLR interpreter** to execute IL bytecode directly. WebAssembly modules cannot be generated and executed dynamically in the browser, so there is no runtime JIT.

The runtime is **statically linked** — all native code (CoreCLR, GC, PAL, system native libraries) is compiled and linked into a single `dotnet.native.wasm` binary.

A **RyuJIT WASM backend** for ahead-of-time (AOT) compilation is under active development, targeting Crossgen2 integration. For more details, see [WebAssembly overview for JIT](jit/WebAssembly%20overview%20for%20JIT.md).

### SIMD - Single instruction, multiple data

WebAssembly SIMD is **required** by the CoreCLR WASM runtime. The browser host validates SIMD support at startup and fails fast if it is missing.

For more information on this feature, see [SIMD.md](https://github.com/WebAssembly/simd/blob/master/proposals/simd/SIMD.md).

### EH - Exception handling

WebAssembly exception handling is **required** and enabled by default (`-fwasm-exceptions`). It provides native exception support without the use of JavaScript, giving higher performance than the JavaScript-based fallback.

The implementation aligns with Emscripten's C++ exception ABI. WASM traps (stack overflow, out-of-bounds memory access) result in fast failures and are not catchable by managed exception handlers.

For more information on this feature, see [Exceptions.md](https://github.com/WebAssembly/exception-handling/blob/master/proposals/exception-handling/Exceptions.md).

### BigInt

Passing Int64 and UInt64 values between JavaScript and C# requires support for the JavaScript `BigInt` type. This is **required** by the CoreCLR WASM runtime. See [JS-BigInt](https://github.com/WebAssembly/JS-BigInt-integration) for more information.

### Multi-threading

Multi-threading is **not yet supported** for CoreCLR on WASM. The runtime operates in single-threaded mode:

- No pthread linking (browser pthreads via Emscripten would require Web Workers, SharedArrayBuffer, and cross-origin isolation headers, which are not currently enabled).
- No background GC or finalizer thread — all GC work runs on the main thread via the browser event loop.
- `Task.Wait` and other blocking operations on the main thread are dangerous and should be avoided.

### fetch - HTTP client

If an application uses the [HttpClient](https://learn.microsoft.com/dotnet/api/system.net.http.httpclient) managed API, your web browser must support the [fetch](https://developer.mozilla.org/en-US/docs/Web/API/Fetch_API) API. HttpClient behavior depends on the browser. Applications must obey `Cross-Origin Resource Sharing` (CORS) rules — see [CORS on MDN](https://developer.mozilla.org/en-US/docs/Web/HTTP/CORS).

Streaming HTTP responses are supported by default.

Direct socket access is not available in the browser. `System.Net.Sockets` throws `PlatformNotSupportedException`.

### WebSocket

Applications using the [WebSocketClient](https://learn.microsoft.com/dotnet/api/system.net.websockets.clientwebsocket) managed API require the browser to support the [WebSocket](https://developer.mozilla.org/en-US/docs/Web/API/WebSockets_API) API.

### Initial Memory Size

The CoreCLR WASM runtime reserves **128 MB** of memory at startup, with memory growth enabled up to a maximum of **2 GB**. The stack size is **5 MB**.

### Garbage collection

CoreCLR on WASM uses the **standard CoreCLR garbage collector** in workstation mode. The GC runs on the main thread. Finalization is scheduled via the browser event loop rather than a dedicated finalizer thread — the runtime calls `SystemJS_ScheduleFinalization()` to post finalization work back to the JavaScript event loop.

WASM linear memory doesn't support page-level memory hints (`madvise`), so some GC optimizations that rely on virtual memory features are disabled.

### Globalization, ICU

CoreCLR WASM supports full globalization via the [ICU library](https://icu.unicode.org/), which is statically linked into the WASM binary. By default the runtime will detect the end user's locale at startup, and load an appropriate shard of the ICU database.

You can configure globalization via the `globalizationMode` loader config option:
- `"sharded"` (default) — Load locale-specific ICU data.
- `"all"` — Load all locale data.
- `"invariant"` — Disable ICU entirely (via `<InvariantGlobalization>true</InvariantGlobalization>`).
- `"custom"` — Provide your own ICU data file.

For more information, see [globalization-icu-wasm.md](../features/globalization-icu-wasm.md).

### MSBuild properties

When building applications targeting CoreCLR on WASM, the following properties are relevant:

| Property | Default | Description |
|----------|---------|-------------|
| `RuntimeFlavor` | `Mono` | Set to `CoreCLR` to use CoreCLR runtime |
| `WasmEnableExceptionHandling` | `true` | Enable native WASM exception handling |
| `WasmEnableSIMD` | `true` | Enable WASM SIMD instructions |
| `InvariantGlobalization` | `false` | Disable ICU for smaller binary size |
| `WasmEnableJsInteropByValue` | `true` (CoreCLR) | Enable by-value JS interop marshalling |
| `WasmEnableWebcil` | `false` (CoreCLR) | WebCIL format support (in progress) |

## JavaScript host API

The CoreCLR browser host exposes a JavaScript API through `dotnet.js`, which serves as the entry point for loading and running .NET applications in the browser. It is documented in [dotnet.d.ts](../../../src/native/corehost/browserhost/loader/dotnet.d.ts).

### JavaScript interop

CoreCLR WASM supports the same `[JSImport]`/`[JSExport]` interop system as Mono WASM, allowing bidirectional calls between C# and JavaScript. For more information on how to use these attributes, see:

* [Introductory Blog Post](https://devblogs.microsoft.com/dotnet/use-net-7-from-any-javascript-app-in-net-7/)
* [Todo-MVC sample](https://github.com/pavelsavara/dotnet-wasm-todo-mvc)
* or [the documentation](https://learn.microsoft.com/aspnet/core/client-side/dotnet-interop).

Source generators (`JSImportGenerator`, `JSExportGenerator`) produce compile-time binding code, keeping the interop efficient without runtime code generation.

### Embedding dotnet in existing JavaScript applications

The default build output relies on exact file names produced during the .NET build. JavaScript tools like [webpack](https://github.com/webpack/webpack) or [rollup](https://github.com/rollup/rollup) can be used for further file modifications.

## Project folder structure

### Build output directories

The following shows the structure for a Debug build:

- `artifacts/bin/coreclr/browser.wasm.Debug/` — console host (corerun) output
- `artifacts/bin/coreclr/browser.wasm.Debug/corehost/` — browser host output (the folder which should be hosted by the HTTP server)

### `corehost` folder structure

- `dotnet.js` — the main entrypoint with the [JavaScript API](#javascript-host-api). It will load the rest of the runtime.
- `dotnet.native.js` — POSIX emulation layer provided by [Emscripten](https://github.com/emscripten-core/emscripten).
- `dotnet.runtime.js` — integration of dotnet with the browser.
- `dotnet.boot.js` — asset manifest with integrity hashes and configuration flags.
- `dotnet.native.wasm` — the compiled binary of the CoreCLR runtime (VM + interpreter + GC + native libraries).
- `System.Private.CoreLib.*` — .NET assembly with the core implementation of the runtime and class library.
- `*.dll` — .NET assemblies in Portable Executable format.

## Hosting the application

### MIME types

`Content-Type` HTTP headers are necessary for correct processing by the browser.

| file extension | Content-Type |
|---|---|
|.html|text/html|
|.js|text/javascript|
|.json|application/json|
|.wasm|application/wasm|
|.mjs|text/javascript|
|.dll|application/octet-stream|
|.pdb|application/octet-stream|

### Content security policy

The runtime is compatible with `Content-Security-Policy: default-src 'self' 'wasm-unsafe-eval'`.

See also [CSP on MDN](https://developer.mozilla.org/en-US/docs/Web/HTTP/CSP).

## Resources consumed on the target device

When you deploy a .NET application to the browser using CoreCLR, the following components are included:
- The CoreCLR runtime, including the garbage collector and interpreter
- The .NET base class library
- An OS emulation layer provided by Emscripten that supplements browser features (timezones, globalization, filesystem)
- Browser integration for features like HTTP and WebSockets
- And your application binaries

All of the above must be downloaded and loaded into memory before your application can start. The browser must also perform its own compilation of the WASM binary at startup.

Running a .NET application in the browser may require more memory and CPU resources than running it natively. Mobile browsers typically have strict limits on memory and many users are on slow internet connections.

## Key differences from Mono WASM

| Aspect | CoreCLR WASM | Mono WASM |
|--------|-------------|-----------|
| **Runtime** | CoreCLR (VM + interpreter) | Mono runtime |
| **Execution** | CoreCLR interpreter (+ RyuJIT AOT in progress) | Mono interpreter + JITerpreter + optional AOT |
| **GC** | CoreCLR workstation GC | Mono SGen GC |
| **Type system** | CoreCLR type system | Mono type system |
| **Assembly loading** | `AssemblyLoadContext` | Mono native loader |
| **JS interop** | `[JSImport]` / `[JSExport]` (CoreCLR WASM implementation) | `[JSImport]` / `[JSExport]` (shared system with CoreCLR WASM) |
| **Export binding** | Reflection-based (`__GeneratedInitializer`) | Direct C interop |
| **Threading** | Not yet supported | Experimental (`WasmEnableThreads`) |
| **WebCIL** | In progress ([#120248](https://github.com/dotnet/runtime/issues/120248)) | Supported |
| **Browser host** | `src/native/corehost/browserhost/` | `src/mono/browser/` |
| **Workload** | Not yet available | `wasm-tools` / `wasm-experimental` |
| **Maturity** | Experimental | Production-supported |

### Known limitations

- **Single-threaded only** — No multi-threading or Web Workers support.
- **No public workload** — Cannot be used via `dotnet new wasmbrowser` yet; requires building from source.
- **WebCIL not yet supported** — Assemblies use `.dll` format, not `.wasm` WebCIL format ([#120248](https://github.com/dotnet/runtime/issues/120248)).
- **No profiling** — Profiler callbacks are stubbed out.
- **No EventPipe** — `FEATURE_EVENT_TRACE` is disabled for browser targets.
- **No COM interop** — `FEATURE_COMWRAPPERS` is disabled.
- **GC/Finalizer issues** — Finalizer integration with the browser event loop has known issues ([#123712](https://github.com/dotnet/runtime/issues/123712)).
- **PAL limitations** — No process/signal/pipe support ([#122506](https://github.com/dotnet/runtime/issues/122506)).

## Developer tools

### Building the runtime

From the repository root:

```bash
# Build CoreCLR for WebAssembly (installs Emscripten SDK automatically)
./build.sh -os browser -c Debug -subset clr.runtime

# Build with libraries and host
./build.sh clr+libs+host -os browser
```

### Running in browser

```bash
dotnet tool install --global dotnet-serve
dotnet-serve --directory "artifacts/bin/coreclr/browser.wasm.Debug/corehost"
```

### Running in Node.js

```bash
# Using corerun
cd artifacts/bin/coreclr/browser.wasm.Debug/
node ./corerun.js -c /path/to/IL /path/to/IL/MyApp.dll

# Using browserhost
cd artifacts/bin/coreclr/browser.wasm.Debug/corehost
node ./main.mjs
```

### Debugging

You can use browser dev tools to debug the JavaScript of the application and the runtime.

#### Chrome DevTools with DWARF

1. Install the [C/C++ DevTools Support (DWARF)](https://goo.gle/wasm-debugging-extension) Chrome extension.
2. Open Chrome DevTools (F12) while running the WASM application.
3. Set breakpoints in the Sources tab — you can step through C/C++ runtime code with source-level debugging.

**Tip**: To display `WCHAR*` strings in the debugger, cast to `char16_t*` (e.g., `(char16_t*)pModule->m_fileName`).

#### VS Code with Node.js

Install the optional [WebAssembly DWARF Debugging](https://marketplace.visualstudio.com/items?itemName=ms-vscode.wasm-dwarf-debugging) extension for source-level WASM debugging in VS Code.

For full debugging instructions including `launch.json` configuration, see [Building CoreCLR for WebAssembly](../../workflow/building/coreclr/wasm.md).

### CoreCLR runtime logging

```xml
<ItemGroup>
  <WasmEnvironmentVariable Include="COMPlus_LogEnable" Value="1" />
  <WasmEnvironmentVariable Include="COMPlus_LogToConsole" Value="1" />
  <WasmEnvironmentVariable Include="COMPlus_LogLevel" Value="10" />
  <WasmEnvironmentVariable Include="COMPlus_LogFacility" Value="410" />
</ItemGroup>
```
