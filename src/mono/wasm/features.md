# Browser or JS engine features

dotnet for wasm can be compiled with various MSBuild flags which enable the use of browser features. If you need to target an older version of the browser, then you may need to disable some of the dotnet features or optimizations.

For set of browser WASM features see [https://webassembly.org/roadmap/](https://webassembly.org/roadmap/)

For full set of MSBuild properties which could be used in client application see also top of [WasmApp.targets](./build/WasmApp.targets) file

## Multi-threading

Is enabled by `<WasmEnableThreads>true</WasmEnableThreads>`.

It requires HTTP headers similar to `Cross-Origin-Embedder-Policy:require-corp` and `Cross-Origin-Opener-Policy:same-origin`.

HTTP headers are sent by your HTTP server or proxy, which need to be properly configured.

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

Typical problem is JSON de/serialization, which does reflection. The solution is to use [Roslyn code generators](https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/source-generation).

```
[JsonSerializable(typeof(List<Item>))]
partial class ItemListSerializerContext : JsonSerializerContext { }

var json = JsonSerializer.Serialize(items, ItemListSerializerContext.Default.ListItem);

```

WARNING: Make sure that you tested trimmed/published release version of your application.

See also [trimming guidance](https://learn.microsoft.com/en-us/dotnet/core/deploying/trimming/trim-self-contained)

## C code or native linked libraries

Native rebuild will cause the .NET runtime to be re-built alongside your application, which allows you to link additional libraries into the WASM binary or change compiler configuration flags.

You can enable native rebuild by `<WasmBuildNative>true</WasmBuildNative>`.
This requires that you have [wasm-tools workload](#wasm-tools-workload) installed.

# JavaScript API

We maintain description of JavaScript embedding API in [dotnet.d.ts](https://github.com/dotnet/runtime/blob/main/src/mono/wasm/runtime/dotnet.d.ts).

## Browser application template

You can create simple application template by running `dotnet new wasmbrowser`.

Then you could `dotnet run` and open the URL which it printed to test the app in your browser. For example `http://localhost:5292/index.html`

You can also `dotnet publish -c Release` which will publish your app to [AppBundle](#Project-folder-structure) folder.

# Downloaded assets

## Project folder structure

Described as relative to simple application project.

- `./wwwroot` is optional project folder, into which you can place files which should be also deployed to the web server.
- `./bin/Release/net8.0/browser-wasm/AppBundle` folder which should be hosted by the HTTP server.
- `./bin/Release/net8.0/browser-wasm/AppBundle/index.html` - the page which is hosting the application.
- `./bin/Release/net8.0/browser-wasm/AppBundle/main.js` - typically the main JavaScript entry point, it will `import { dotnet } from './_framework/dotnet.js'` and `await dotnet.run();`.
- `./bin/Release/net8.0/browser-wasm/AppBundle/_framework` - contains all the assets of the runtime and the managed application.

You can flatten the `_framework` folder away by `<WasmRuntimeAssetsLocation>./</WasmRuntimeAssetsLocation>` in the project file.

## `_framework` folder structure
- `dotnet.js` - is the main entrypoint with the [JavaScript API](#JavaScript-API). It will load the rest of the runtime.
- `dotnet.native.js` - is posix emulation layer by [emscripten](https://github.com/emscripten-core/emscripten) project
- `dotnet.runtime.js` - is integration of the dotnet with the browser
- `blazor.boot.json` - contains list of all other assets and their integrity hash and also various configuration flags.
- `dotnet.native.wasm` - is the compiled binaries of the dotnet (Mono) runtime.
- `System.Private.CoreLib.wasm`
- `*.wasm` - are .NET assemblies wrapped as .wasm files in WebCIL forma for better compatibility with antivirus solutions.
- `dotnet.js.map` - is source map file, for easier debugging of the runtime code. It's not included in published apps.
- `dotnet.native.js.symbols` - are debug symbols which help to put C runtime method names back to the .wasm stack traces. You could enable it by `<WasmEmitSymbolMap>true</WasmEmitSymbolMap>`.

## Caching, Integrity

Your browser could be caching various files and assets downloaded by the dotnet runtime so that the next application start will be much faster.

When you deploy a new version of the application, you need to make sure that the caches in the browser and also in any HTTP proxies will not interfere.

WARNING: Your server can't force the browser to ask for the file on the same URL again, if the browser thinks that the file is fresh enough.

_TODO_ discuss cache busting strategies.

In order to make sure that the application resources are consistent with each other, dotnet runtime will use `integrity` hash for all downloaded resources.

See also [Cache-Control on headers](https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/Cache-Control)

See also [fetch integrity](https://developer.mozilla.org/en-US/docs/Web/API/Request/integrity)

## MIME types

Are `Content-Type` HTTP headers which tell the browser about the type of downloaded asset. They are necessary for correct and fast processing by the browser, but also by various caches and proxies.

HTTP headers are sent by your HTTP server or proxy, which need to be properly configured.

| file extension  | Content-Type |
|---|---|
|.wasm|application/wasm|
|.json|application/json|
|.js|text/javascript|

See also [Content-Type on MDN](https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/Content-Type)

## Compression

Modern browsers are able to unpack files compressed for the transport. We recommend brotli compression for best results.

See also [Content-Encoding on MDN](https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/Content-Encoding)

## Content security policy

dotnet runtime for wasm is CSP compliant starting from .Net 8, except legacy JS interop methods.

In order to enable it, please set HTTP headers similar to `Content-Security-Policy: default-src 'self' 'wasm-unsafe-eval'`

HTTP headers are sent by your HTTP server or proxy, which need to be properly configured.

See also [CSP on MDN](https://developer.mozilla.org/en-US/docs/Web/HTTP/CSP)

See also [wasm-unsafe-eval](https://github.com/WebAssembly/content-security-policy/blob/main/proposals/CSP.md#the-wasm-unsafe-eval-source-directive)

## ICU

Browsers don't offer full set APIs for working with localization and so we have to bring the data and the logic as part of the application.

If your application doesn't need to work with locatization, you can reduce download size by `<InvariantCulture>true</InvariantCulture>`.

If your application needs to work with locatization but you don't require processing speed for processing of large texts, you can reduce download size by `<HybridGlobalization>true</HybridGlobalization>`.
It will use browser APIs instead part of ICU database. This feature is still in development.

If your application needs to work with specific subset of locales, you can create your own ICU database.

_TODO_ HOW_TO custom ?

## Timezones

Browsers don't offer API for working with time zone database and so we have to bring the time zone data as part of the application.

If your application doesn't need to work with TZ, you can reduce download size by `<InvariantTimezone>true</InvariantTimezone>`.

This requires that you have [wasm-tools workload](#wasm-tools-workload) installed.

## Bundling JavaScript and other assets

In the web ecosystem it's usual that developers use assets bundlers like [webpack](https://github.com/webpack/webpack) or [rollup](https://github.com/rollup/rollup) to bundle many files into one large .js file.

You can bundle the `dotnet.js` ES6 module with the rest of your JavaScript application.

The other assets and JS modules of the dotnet are loaded via dynamic `import` or via `fetch` APIs. They are not ready to be bundled in Net8.
We consider that the dotnet application is usually large as is and giving the browser chance to start compiling it in parallel with other downloads is better.

We would like to [hear from the community](https://github.com/dotnet/runtime/issues/86162) more about the use-cases when it would be benefitial.

# Resources consumed on the target device

dotnet is complex and large application, it consists of
- dotnet runtime, including garbage collector, IL interpreter and browser specific JIT
- dotnet base class library
- emulation layer which is bringing missing features of the OS, which the browser doesn't provide. Like timezone database or ICU.
- integration with the browser JavaScript APIs, for example HTTP and WebSocket client
- application code

All of the mentioned code and data need to be downloaded and loaded into the browser memory during the dotnet startup sequence.
Browser itself will run JIT compilation of the WASM and JS code, which consumes memory and CPU cycles too.

## Mobile phones

Recent mobile phones distribute their browser as an application that can be upgraded separately from the operating system.

Note that all browsers on iOS and iPadOS are required to use the Safari browser engine, so their level of support for WASM features depends on the version of Safari installed on the device.

Mobile browsers typically have strict limits on the amount of memory they can use, and many users are on slow internet connections. A WebAssembly application that works well on desktop PCs may take minutes to download or run out of memory before it is able to start.

## Shell environments - NodeJS & V8
We pass most of the unit tests with NodeJS v 14 but it's not fully supported target platform. We would like to hear about community use-cases.

We also use the d8 command-line shell, version 11 or higher, to run some of the tests. This shell lacks most browser APIs and features.

## Trade-offs compared to native JavaScript applications

There is trade-off between download size, application performance and complexity of application maintanance.
If your business logic is very complex, changes often or you already have existing C# codebase and skillset, running the same code dotnet on wasm is probably the right choice.
If your application is simple and you have JavaScript skills on your team, it may be better if you re/write your logic in Web native technologies like HTML/JavaScript or typescript/webpack stack.

# wasm-tools workload

The wasm-tools workload contains all of the tools and libraries necessary to perform native rebuild or AOT compilation and other optimizations of your application.

Although it's optional for Blazor, we strongly recommend using it!

You can install it by running `dotnet workload install wasm-tools` on your command line.

You can also install `dotnet workload install wasm-experimental`.
