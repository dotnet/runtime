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

You can disable it by `<WasmEnableSIMD>false</WasmEnableSIMD>`.

See also WebAssembly proposal [SIMD.md](https://github.com/WebAssembly/simd/blob/master/proposals/simd/SIMD.md)

Some older devices or operating systems don't have the necessary CPU instructions to make this optimization bring the expected perf boost.

## EH - Exception handling
Is performance optimization enabled by default. It requires recent version of browser.

You can disable it by `<WasmEnableExceptionHandling>false</WasmEnableExceptionHandling>`.

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

See also [wasm-unsafe-eval](https://github.com/WebAssembly/content-security-policy/blob/main/proposals/CSP.md#the-wasm-unsafe-eval-source-directive)

## ICU

_TODO_

## Timezones

Browsers don't offer API for working with time zone database and so we have to bring the time zone data as part of the application.

If your application doesn't need to work with TZ, you can reduce download size by `<InvariantTimezone>true</InvariantTimezone>`.
This requires that you have [wasm-tools workload](#wasm-tools-workload) installed.

# Shell environments - NodeJS & V8
We pass most of the unit tests with NodeJS v 14 but it's not fully supported target platform. We would like to hear about community use-cases.

We also use the d8 command-line shell, version 11 or higher, to run some of the tests. This shell lacks most browser APIs and features.

# Mobile phones

Recent mobile phones distribute their browser as an application that can be upgraded separately from the operating system.

Note that all browsers on iOS and iPadOS are required to use the Safari browser engine, so their level of support for WASM features depends on the version of Safari installed on the device.

Mobile browsers typically have strict limits on the amount of memory they can use, and many users are on slow internet connections. A WebAssembly application that works well on desktop PCs may take minutes to download or run out of memory before it is able to start.

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

Setting an initial size based on how much memory your application typically uses will reduce the number of times the heap needs to grow, which may enable it to run on devices with lower memory.

You can override initial size of the WASM linear memory by `<EmccInitialHeapSize>16777216</EmccInitialHeapSize>`. Where number of bytes must be aligned to next 16KB page size.

This requires that you have [wasm-tools workload](#wasm-tools-workload) installed.

## JITerpreter

Is browser specific JIT compiler which optimizes small fragments of code which are otherwise interpreted by dotnet mono interpreter. It's enabled by default.

It boosts performance of simple methods and consumes some WASM linear and browser memory.

You can disable it by `<BlazorWebAssemblyJiterpreter>false</BlazorWebAssemblyJiterpreter>`.

For detailed design see also [jiterpreter.md](../../../docs/design/mono/jiterpreter.md)

## AOT

AOT compilation greatly improves application performance but will increase the size of the application, resulting in longer downloads and slower startup.

You can enable Ahead Of Time compilation by `<RunAOTCompilation>true</RunAOTCompilation>`.

It will compile managed code as native WASM instructions and include them in the `dotnet.native.wasm` file.

This requires that you have [wasm-tools workload](#wasm-tools-workload) installed.

## IL trimming

Trimming will remove unused code from your application, which reduces download time and memory usage.

When AOT compilation is in use, trimming will also reduce the amount of time spent to compile the application.

You can trim size of the managed code in the assemblies by `<PublishTrimmed>true</PublishTrimmed>`.

Some applications will break if trimming is used without further configuration due to the trimmer not knowing which code is used, for example via reflection.

WARNING: Make sure that you tested trimmed/published release version of your application.

See also [trimming guidance](https://learn.microsoft.com/en-us/dotnet/core/deploying/trimming/trim-self-contained)

## C code or native linked libraries

Native rebuild will cause the .NET runtime to be re-built alongside your application, which allows you to link additional libraries into the WASM binary or change compiler configuration flags.

You can enable native rebuild by `<WasmBuildNative>true</WasmBuildNative>`.
This requires that you have [wasm-tools workload](#wasm-tools-workload) installed.

## wasm-tools workload

The wasm-tools workload contains all of the tools and libraries necessary to perform native rebuild or AOT compilation and other optimizations of your application.

Although it's optional for Blazor, we strongly recommend using it!

You can install it by running `dotnet workload install wasm-tools` on your command line.