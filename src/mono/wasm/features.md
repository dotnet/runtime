# Browser or JS engine features

dotnet for wasm can be compiled with various MSBuild flags which enable the use of browser features. If you need to target an older version of the browser, then you may need to disable some of the dotnet features or optimizations.

For set of browser WASM features see [https://webassembly.org/roadmap/](https://webassembly.org/roadmap/)

For full set of MSBuild properties see also top of [WasmApp.targets](./build/WasmApp.targets) file

## Multi-threading

Is enabled by `<WasmEnableThreads>true</WasmEnableThreads>`.

It requires HTTP headers similar to `Cross-Origin-Embedder-Policy:require-corp` and `Cross-Origin-Opener-Policy:same-origin`.

See also [SharedArrayBuffer security requirements](https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Global_Objects/SharedArrayBuffer#security_requirements)

JavaScript interop with managed code via `[JSExport]`/`[JSImport]` on the WebWorker is still in development.

## SIMD - Single instruction, multiple data

Is performance optimization enabled by default. It requires recent version of browser.

You can disable it by `<WasmEnableSIMD>false</WasmEnableSIMD><WasmBuildNative>true</WasmBuildNative>`.

See also WebAssembly proposal [SIMD.md](https://github.com/WebAssembly/simd/blob/master/proposals/simd/SIMD.md)

Some older devices or operating systems don't have the necessary CPU instructions to make this optimization bring the expected perf boost.

## EH - Exception handling
Is performance optimization enabled by default. It requires recent version of browser.

You can disable it by `<WasmEnableExceptionHandling>false</WasmEnableExceptionHandling><WasmBuildNative>true</WasmBuildNative>`.

See also WebAssembly proposal [Exceptions.md](https://github.com/WebAssembly/exception-handling/blob/master/proposals/exception-handling/Exceptions.md)

## BigInt
Is required if the application uses Int64 marshaling in JS interop.
See also WebAssembly proposal [JS-BigInt](https://github.com/WebAssembly/JS-BigInt-integration)

## fetch browser API

Is required if the application uses [HttpClient](https://learn.microsoft.com/en-us/dotnet/api/system.net.http.httpclient)

NodeJS needs to install `node-fetch` and `node-abort-controller` npm packages.

## WebSocket browser API

Is required if the application uses [WebSocketClient](https://learn.microsoft.com/en-us/dotnet/api/system.net.websockets.clientwebsocket)

NodeJS needs to install `ws` npm package.

## Content security policy

dotnet runtime for wasm is CSP compliant starting from .Net 8, except legacy JS interop methods.

In order to enable it, please set HTTP headers similar to `Content-Security-Policy: default-src 'self' 'wasm-unsafe-eval'`

See also [CSP on MDN](https://developer.mozilla.org/en-US/docs/Web/HTTP/CSP)

## ICU

_TODO_

## Timezones

Browsers don't offer API for working with time zone database and so we have to bring the time zone data as part of the application.

If your application doesn't need to work with TZ, you can reduce download size by `<InvariantTimezone>true</InvariantTimezone>`.
This requires that you have [wasm-tools workload](#wasm-tools-workload) installed.

# Shell environments - NodeJS & V8
We pass most of the unit tests with NodeJS v 14 but it's not fully supported target platform. We would like to hear about community use-cases.

We also use v8 engine version 11 or higher to run some of the tests. The engine is lacking many APIs and features.

# Mobile phones

Recent mobile phones have browser integration which could be upgraded separately from the operating system.

Also note that all browsers on iOS are just wrapper for Safari engine and so it's limited by Safari features and version, rather than wrapper version and brand.

Mobile phones usually have limited resources. Memory and download speed are major concern.

## Resources consumed on the target device

dotnet is complex and large application, it consists of
- dotnet runtime, including garbage collector, IL interpreter and browser specific JIT
- dotnet base class library
- emulation layer which is bringing missing features of the OS, which the browser doesn't provide. Like timezone database or ICU.
- integration with the browser JavaScript APIs, for example HTTP and WebSocket client
- application code

All of the mentioned code and data need to be downloaded and loaded into the browser memory during the dotnet startup sequence.
Browser itself will run JIT compilation of the WASM and JS code, which consumes memory and CPU cycles too.

## WASM linear memory

You can override initial size of the WASM linear memory by `<EmccInitialHeapSize>16777216</EmccInitialHeapSize>`.
This requires that you have [wasm-tools workload](#wasm-tools-workload) installed.

## JITerpreter

Is browser specific JIT compiler which optimizes small fragments of code which are otherwise interpreted by dotnet mono interpreter. It's enabled by default.

## AOT

You can enable Ahead Of Time compilation by `<RunAOTCompilation>true</RunAOTCompilation>`.
This requires that you have [wasm-tools workload](#wasm-tools-workload) installed.

It will compile managed code as native WASM instructions and include them in the `dotnet.native.wasm` file.
AOT compiled code is running faster but it will significantly increase size of the file and the download time.

## IL trimming

You can trim size of the managed code in the assemblies by `<PublishTrimmed>true</PublishTrimmed>`.

## C code or native linked libraries

You can enable native rebuild by `<WasmBuildNative>true</WasmBuildNative>`.
This requires that you have [wasm-tools workload](#wasm-tools-workload) installed.

## wasm-tools workload

Is set of compiler tools which allow you to optimize your wasm application.

You can install it by running `dotnet workload install wasm-tools` on your command line.