# Configuring browser features
The WebAssembly version of .NET exposes a number of MSBuild properties that can be used to control which browser features are used by the runtime. If you need to target older browser versions or old hardware, you may need to use some of these flags to disable the use of newer features.

For a support matrix of WebAssembly features see [https://webassembly.org/roadmap/](https://webassembly.org/roadmap/). Note that the 'Chrome' in that support matrix refers to Chrome on Android and desktop/laptop PCs - Chrome on iOS/iPadOS only supports the features available in Safari on that device.

For the full set of MSBuild properties that configure a client application's use of these features, see the top of [WasmApp.targets](./build/WasmApp.targets). All of these properties must be placed in your application's `.csproj` file (inside of a `PropertyGroup`) to have any effect.

Some of these properties require a unique build of the runtime, which means that changing them will produce a different set of `.wasm` and `.js` files for your application. Some of these properties also require you to install the [wasm-tools workload](#wasm-tools-workload).

## Multi-threading

Multi-threading support is enabled by `<WasmEnableThreads>true</WasmEnableThreads>`, and is currently disabled by default. It requires a unique build of the runtime.

Your HTTPS server and/or proxy must be configured to send HTTP headers similar to `Cross-Origin-Embedder-Policy:require-corp` and `Cross-Origin-Opener-Policy:same-origin` in order to enable multi-threading support in end-user web browsers for security reasons.

For more information, see [SharedArrayBuffer security requirements](https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Global_Objects/SharedArrayBuffer#security_requirements).

JavaScript interop with managed code via `[JSExport]`/`[JSImport]` is currently limited to the main thread even if multi-threading support is enabled.

## SIMD - Single instruction, multiple data
WebAssembly SIMD provides significant performance improvements for operations on spans, strings, vectors and arrays. This feature requires a somewhat recent browser and may also not be supported by older hardware. It is currently enabled by default.

It can be enabled with `<WasmEnableSIMD>true</WasmEnableSIMD>` and disabled with `<WasmEnableSIMD>false</WasmEnableSIMD>`. As this feature requires a unique build of the runtime, changing this property may require a native rebuild (described further below).

For more information on this feature, see [SIMD.md](https://github.com/WebAssembly/simd/blob/master/proposals/simd/SIMD.md).

## EH - Exception handling
WebAssembly exception handling provides higher performance for code containing `try` blocks by allowing exceptions to be caught and thrown natively without the use of JavaScript. It is currently enabled by default and can be disabled via `<WasmEnableExceptionHandling>false</WasmEnableExceptionHandling>`.

For more information on this feature, see [Exceptions.md](https://github.com/WebAssembly/exception-handling/blob/master/proposals/exception-handling/Exceptions.md)

## BigInt
Passing Int64 and UInt64 values between JavaScript and C# requires support for the JavaScript `BigInt` type. See [JS-BigInt](https://github.com/WebAssembly/JS-BigInt-integration) for more information on this API.

## fetch
If an application uses the [HttpClient](https://learn.microsoft.com/en-us/dotnet/api/system.net.http.httpclient) managed API, your web browser must support the [fetch](https://developer.mozilla.org/en-US/docs/Web/API/Fetch_API) API for it to run.

Because web browsers do not expose direct access to sockets, we are unable to provide our own implementation of HTTP, and HttpClient's behavior and feature set will as a result depend entirely on the browser you use to run the application.

A prominent limitation is that your application must obey Cross-Origin Resource Sharing (CORS) rules in order to perform network requests successfully - see [CORS on MDN](https://developer.mozilla.org/en-US/docs/Web/HTTP/CORS) for more information.

For your application to be able to perform HTTP requests in a NodeJS host, you need to install the `node-fetch` and `node-abort-controller` npm packages.

## WebSocket
Applications using the [WebSocketClient](https://learn.microsoft.com/en-us/dotnet/api/system.net.websockets.clientwebsocket) managed API will require the browser to support the [WebSocket](https://developer.mozilla.org/en-US/docs/Web/API/WebSockets_API) API.

As with HTTP and HttpClient, we are unable to ship a custom implementation of this feature, so its behavior will depend on the browser being used to run the application.

WebSocket support in NodeJS hosts requires the `ws` npm package.

## Initial Memory Size
By default the .NET runtime will reserve a small amount of memory at startup, and as your application allocates more objects the runtime will attempt to "grow" this memory. This growth operation takes time and could fail if your device's memory is limited, which would result in an application error or "tab crash".

To reduce startup time and increase the odds that your application will work on devices with limited memory, you can set an initial size for the memory allocation, based on an estimate of how much memory your application typically uses. To set an initial memory size, include an MSBuild property like `<EmccInitialHeapSize>16777216</EmccInitialHeapSize>`, where you have changed the number of bytes to an appropriate value for your application. This value must be a multiple of 16384.

This property requires the [wasm-tools workload](#wasm-tools-workload) to be installed.

## JITerpreter
The JITerpreter is a browser-specific compiler which will optimize frequently executed code when running in interpreted (non-AOT) mode. While this significantly improves application performance, it will cause increased memory usage. You can disable it via `<BlazorWebAssemblyJiterpreter>false</BlazorWebAssemblyJiterpreter>`, and configure it in more detail via the use of runtime options.

For more information including a list of relevant runtime options, see [jiterpreter.md](../../../docs/design/mono/jiterpreter.md).

## AOT
AOT compilation greatly improves application performance but will increase the size of the application, resulting in longer downloads and slower startup, so it is currently disabled by default. To enable it, use `<RunAOTCompilation>true</RunAOTCompilation>`. The resulting ahead-of-time compiled code will be included in your application's `dotnet.native.wasm` file.

This feature only works if you have the [wasm-tools workload](#wasm-tools-workload) installed.

## IL trimming
Trimming will remove unused code from your application, which reduces application startup time and memory usage. Trimming also reduces the amount of time spent during AOT compilation if it is in use. To enable trimming of managed code, use `<PublishTrimmed>true</PublishTrimmed>`.

Some applications will break if trimming is used without further configuration due to the trimmer not knowing which code is used, for example any code accessed via reflection or serialization.

One typical source of trimming issues is JSON serialization/deserialization. The solution is to use [Source Generation](https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/source-generation), as shown below:

```
[JsonSerializable(typeof(List<Item>))]
partial class ItemListSerializerContext : JsonSerializerContext { }

var json = JsonSerializer.Serialize(items, ItemListSerializerContext.Default.ListItem);
```

Please ensure that you have thoroughly tested your application with trimming enabled before deployment, as the issues it causes may only appear in obscure parts of your software. For more advice on how to use trimming, see [trimming guidance](https://learn.microsoft.com/en-us/dotnet/core/deploying/trimming/trim-self-contained).

## C code or native linked libraries
Native rebuild will cause the .NET runtime to be re-built alongside your application, which allows you to link additional libraries into the WASM binary or change compiler configuration flags.

You can enable native rebuild via `<WasmBuildNative>true</WasmBuildNative>`.

To add custom C source files into the runtime at compilation time, include native file references inside of an `ItemGroup` in your project, like `<NativeFileReference Include="fibonacci.c" />`.

This requires that you have the [wasm-tools workload](#wasm-tools-workload) installed.

# JavaScript host API
When the .NET runtime is hosted inside of a browser or other JavaScript environment, we expose a JavaScript API that can be used to configure and communicate with the runtime. It is documented in [dotnet.d.ts](https://github.com/dotnet/runtime/blob/main/src/mono/wasm/runtime/dotnet.d.ts) and you can see some examples of its use in our [samples](../sample/wasm/).

## Browser application template
You can create a simple WebAssembly application by running `dotnet new wasmbrowser`. Then to run it, use `dotnet run` and open the URL which it wrote to the console inside your browser of choice. For example `http://localhost:5292/index.html`

Once you are ready to deploy your application, use `dotnet publish -c Release` which will publish your app to the [AppBundle](#Project-folder-structure) folder.

## JavaScript interop
When you want to call JavaScript functions from C# or managed code from JavaScript, you can annotate static C# methods with `[JSImport]` or `[JSExport]` attributes. For more information on how to use these attributes, see:

* [Introductory Blog Post](https://devblogs.microsoft.com/dotnet/use-net-7-from-any-javascript-app-in-net-7/)
* [TODO app sample](https://github.com/pavelsavara/dotnet-wasm-todo-mvc)
* or [the documentation](https://learn.microsoft.com/en-us/aspnet/core/blazor/javascript-interoperability/import-export-interop).

## Embedding dotnet in existing JavaScript applications
To embed the .NET runtime inside of a JavaScript application, you will need to use both the MSBuild toolchain (to build and publish your managed code) and your existing web build toolchain.

The output of the MSBuild toolchain - located in the `AppBundle folder` - must be fed in to your web build toolchain in order to ensure that the runtime and managed binaries are deployed with the rest of your application assets.

For a sample of using the .NET runtime in a React component, [see here](https://github.com/maraf/dotnet-wasm-react).

# Downloaded assets

## Project folder structure

The following paths are relative to a simple application's project directory.

- `./wwwroot` is an optional project folder, into which you can place files which should be also deployed to the web server.
- `./bin/Release/net8.0/browser-wasm/AppBundle` is the folder which should be hosted by the HTTP server.
- `./bin/Release/net8.0/browser-wasm/AppBundle/index.html` - the page which is hosting the application.
- `./bin/Release/net8.0/browser-wasm/AppBundle/main.js` - typically the main JavaScript entry point, it will `import { dotnet } from './_framework/dotnet.js'` to load the runtime and then `await dotnet.run();` in order to start it.
- `./bin/Release/net8.0/browser-wasm/AppBundle/_framework` - contains all the assets of the runtime and the managed application.

You can flatten the `_framework` folder away by putting `<WasmRuntimeAssetsLocation>./</WasmRuntimeAssetsLocation>` in a property group in the project file.

## `_framework` folder structure
- `dotnet.js` - is the main entrypoint with the [JavaScript API](#JavaScript-API). It will load the rest of the runtime.
- `dotnet.native.js` - is posix emulation layer by [emscripten](https://github.com/emscripten-core/emscripten) project
- `dotnet.runtime.js` - is integration of the dotnet with the browser
- `blazor.boot.json` - contains list of all other assets and their integrity hash and also various configuration flags.
- `dotnet.native.wasm` - is the compiled binaries of the dotnet (Mono) runtime.
- `System.Private.CoreLib.wasm`
- `*.wasm` - are .NET assemblies stored in WebCIL format (for compatibility with firewalls and virus scanners).
- `dotnet.js.map` - is a source map file, for easier debugging of the runtime code. It's not included in published apps.
- `dotnet.native.js.symbols` - are debug symbols which help to put C runtime method names back to the .wasm stack traces. To enable generating it, use `<WasmEmitSymbolMap>true</WasmEmitSymbolMap>`.

## Caching and Integrity

Your browser could be caching various files and assets downloaded by the dotnet runtime so that the next application start will be much faster. When you deploy a new version of the application, you need to make sure that the caches in the browser and also in any HTTP proxies will not interfere.

If the end user's browser thinks that a copy of a given URL in its cache is new enough, your server has no way to force the browser to request it again. There are various ways to mitigate this, including changing URLs.

In order to make sure that the application resources are consistent with each other, the .NET runtime will use `integrity` hash for all downloaded resources.

See also [Cache-Control on headers](https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/Cache-Control)

See also [fetch integrity](https://developer.mozilla.org/en-US/docs/Web/API/Request/integrity)

## MIME types

`Content-Type` HTTP headers tell the browser about the type of each downloaded asset. They are necessary for correct and fast processing by the browser, but also by various caches and proxies.

HTTP headers are sent by your HTTP server or proxy, which need to be properly configured. If not set correctly, your application may load slowly or even fail to start.

| file extension  | Content-Type |
|---|---|
|.wasm|application/wasm|
|.json|application/json|
|.js|text/javascript|

See also [Content-Type on MDN](https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/Content-Type)

## Compression
Modern browsers are able to unpack files that have been compressed by the server, allowing for faster downloads and reduced startup time. By default, a Blazor application published will include gzip (`.gz`) and brotli (`.br`) compressed versions of each asset, and by configuring your web server correctly those files can be served to end users for improved performance.

See also [Content-Encoding on MDN](https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/Content-Encoding)

## Content security policy

dotnet runtime for wasm is CSP compliant starting from .Net 8, except legacy JS interop methods.

In order to enable it, please set HTTP headers similar to `Content-Security-Policy: default-src 'self' 'wasm-unsafe-eval'`

HTTP headers are sent by your HTTP server or proxy, which need to be properly configured.

See also [CSP on MDN](https://developer.mozilla.org/en-US/docs/Web/HTTP/CSP)

See also [wasm-unsafe-eval](https://github.com/WebAssembly/content-security-policy/blob/main/proposals/CSP.md#the-wasm-unsafe-eval-source-directive)

## Globalization, ICU

Browsers don't offer full set APIs for working with localization and so we have to bring the data and the logic as part of the application.

In order to make downloades smaller, runtime will detect the locale of the browser and load just shard of the ICU database. You can also create and configure your own custom shards.

For more details see [globalization-icu-wasm.md](../../../docs/design/features/globalization-icu-wasm.md)

If your application doesn't need to work with locatization, you can reduce download size by `<InvariantCulture>true</InvariantCulture>`.

For more details see [globalization-invariant-mode.md](../../../docs/design/features/globalization-invariant-mode.md)

If your application needs to work with locatization but you don't require processing speed for working with large texts, you can reduce download size by `<HybridGlobalization>true</HybridGlobalization>`.
It will use browser APIs instead part of ICU database. This feature is still in development.

For more details see [globalization-hybrid-mode.md](../../../docs/design/features/globalization-hybrid-mode.md)

This requires that you have [wasm-tools workload](#wasm-tools-workload) installed.

## Timezones

Browsers don't offer API for working with time zone database and so we have to bring the time zone data as part of the application.

If your application doesn't need to work with time zones, you can reduce download size by `<InvariantTimezone>true</InvariantTimezone>`.

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
