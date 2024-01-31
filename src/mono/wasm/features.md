# Configuring and hosting .NET WebAssembly applications

## Table of contents
- [Configuring browser features](#Configuring-browser-features)
- [Project folder structure](#Project-folder-structure)
- [Hosting the application](#Hosting-the-application)
- [Resources consumed on the target device](#Resources-consumed-on-the-target-device)
- [Choosing the right platform target](#Choosing-the-right-platform-target)
- [Developer tools](#Developer-tools)


## Configuring browser features

The WebAssembly version of .NET exposes a number of MSBuild properties that can be used to control which browser features are used by the runtime. If you need to target older browser versions or old hardware, you may need to use some of these flags to disable the use of newer features.

For a support matrix of WebAssembly features see [https://webassembly.org/roadmap/](https://webassembly.org/roadmap/). †

For the full set of MSBuild properties that configure a client application's use of these features, see the top of [BrowserWasmApp.targets](../browser/build/BrowserWasmApp.targets). All of these properties must be placed in your application's `.csproj` file (inside of a `PropertyGroup`) to have any effect.

Some of these properties require a unique build of the runtime, which means that changing them will produce a different set of `.wasm` and `.js` files for your application. Some of these properties also require you to install the [wasm-tools workload](#wasm-tools-workload).

<sub><sup>† Note: that the 'Chrome' in that support matrix refers to Chrome on Android and desktop/laptop PCs - Chrome on iOS/iPadOS only supports the features available in Safari browser on that device.</sup></sub>

### Multi-threading

Multi-threading support is enabled by `<WasmEnableThreads>true</WasmEnableThreads>`, and is currently disabled by default. It requires a unique build of the runtime.

Your HTTPS server and/or proxy must be configured to send HTTP headers similar to `Cross-Origin-Embedder-Policy:require-corp` and `Cross-Origin-Opener-Policy:same-origin` in order to enable multi-threading support in end-user web browsers for security reasons.

For more information, see [SharedArrayBuffer security requirements](https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Global_Objects/SharedArrayBuffer#security_requirements).

JavaScript interop with managed code via `[JSExport]`/`[JSImport]` is currently limited to the main thread even if multi-threading support is enabled.

Blocking on the main thread with operations like `Task.Wait` or `Monitor.Enter` are not supported by browsers and are very dangerous. The work on the proper design for this is still in progress.

### SIMD - Single instruction, multiple data
WebAssembly SIMD provides significant performance improvements for operations on spans, strings, vectors and arrays. This feature requires a somewhat recent browser and may also not be supported by older hardware. It is currently enabled by default.

It can be enabled with `<WasmEnableSIMD>true</WasmEnableSIMD>` and disabled with `<WasmEnableSIMD>false</WasmEnableSIMD>`. As this feature requires a unique build of the runtime, changing this property may require a native rebuild (described further below).

For more information on this feature, see [SIMD.md](https://github.com/WebAssembly/simd/blob/master/proposals/simd/SIMD.md).

Older versions of NodeJS hosts may need `--experimental-wasm-simd` command line option.

### EH - Exception handling
WebAssembly exception handling provides higher performance for code containing `try` blocks by allowing exceptions to be caught and thrown natively without the use of JavaScript. It is currently enabled by default and can be disabled via `<WasmEnableExceptionHandling>false</WasmEnableExceptionHandling>`.

For more information on this feature, see [Exceptions.md](https://github.com/WebAssembly/exception-handling/blob/master/proposals/exception-handling/Exceptions.md)

Older versions of NodeJS hosts may need `--experimental-wasm-eh` command line option.

### BigInt
Passing Int64 and UInt64 values between JavaScript and C# requires support for the JavaScript `BigInt` type. See [JS-BigInt](https://github.com/WebAssembly/JS-BigInt-integration) for more information on this API.

### fetch - HTTP client
If an application uses the [HttpClient](https://learn.microsoft.com/en-us/dotnet/api/system.net.http.httpclient) managed API, your web browser must support the [fetch](https://developer.mozilla.org/en-US/docs/Web/API/Fetch_API) API for it to run.

Because web browsers do not expose direct access to sockets, we are unable to provide our own implementation of HTTP, and HttpClient's behavior and feature set will as a result depend also on the browser you use to run the application.

A prominent limitation is that your application must obey `Cross-Origin Resource Sharing` (CORS) rules in order to perform network requests successfully - see [CORS on MDN](https://developer.mozilla.org/en-US/docs/Web/HTTP/CORS) for more information.

For your application to be able to perform HTTP requests in a NodeJS host, you need to install the `node-fetch` and `node-abort-controller` npm packages.

### WebSocket
Applications using the [WebSocketClient](https://learn.microsoft.com/en-us/dotnet/api/system.net.websockets.clientwebsocket) managed API will require the browser to support the [WebSocket](https://developer.mozilla.org/en-US/docs/Web/API/WebSockets_API) API.

As with HTTP and HttpClient, we are unable to ship a custom implementation of this feature, so its behavior will depend on the browser being used to run the application.

WebSocket support in NodeJS hosts requires the `ws` npm package.

### Initial Memory Size
By default the .NET runtime will reserve a small amount of memory at startup, and as your application allocates more objects the runtime will attempt to "grow" this memory. This growth operation takes time and could fail if your device's memory is limited, which would result in an application error or "tab crash".

To reduce startup time and increase the odds that your application will work on devices with limited memory, you can set an initial size for the memory allocation, based on an estimate of how much memory your application typically uses. To set an initial memory size, include an MSBuild property like `<EmccInitialHeapSize>16777216</EmccInitialHeapSize>`, where you have changed the number of bytes to an appropriate value for your application. This value must be a multiple of 16384.

This property requires the [wasm-tools workload](#wasm-tools-workload) to be installed.

### JITerpreter
The JITerpreter is a browser-specific compiler which will optimize frequently executed code when running in interpreted (non-AOT) mode. While this significantly improves application performance, it will cause increased memory usage. You can disable it via `<BlazorWebAssemblyJiterpreter>false</BlazorWebAssemblyJiterpreter>`, and configure it in more detail via the use of runtime options.

For more information including a list of relevant runtime options, see [jiterpreter.md](../../../docs/design/mono/jiterpreter.md).

### AOT
AOT compilation greatly improves application performance but will increase the size of the application, resulting in longer downloads and slower startup, so it is currently disabled by default. To enable it, use `<RunAOTCompilation>true</RunAOTCompilation>`. This is effective only when publishing the project. The resulting ahead-of-time compiled code will be included in your application's `dotnet.native.wasm` file.

This feature only works if you have the [wasm-tools workload](#wasm-tools-workload) installed.

### IL trimming
Trimming will remove unused code from your application, which reduces application startup time and memory usage. Trimming also reduces the amount of time spent during AOT compilation if it is in use. To enable trimming of managed code, use `<PublishTrimmed>true</PublishTrimmed>` and `<TrimMode>full</TrimMode>`.

Some applications will break if trimming is used without further configuration due to the trimmer not knowing which code is used, for example any code accessed via reflection or serialization or dependency injection.

One typical source of trimming issues is JSON serialization/deserialization. The solution is to use [Source Generation](https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/source-generation), as shown below:

```csharp
[JsonSerializable(typeof(List<Item>))]
partial class ItemListSerializerContext : JsonSerializerContext { }

var json = JsonSerializer.Serialize(items, ItemListSerializerContext.Default.ListItem);
```

Please ensure that you have thoroughly tested your application with trimming enabled before deployment, as the issues it causes may only appear in obscure parts of your software. For more advice on how to use trimming, see [trimming guidance](https://learn.microsoft.com/en-us/dotnet/core/deploying/trimming/trim-self-contained).

### C code or native linked libraries
Native rebuild will cause the .NET runtime to be re-built alongside your application, which allows you to link additional libraries into the WASM binary or change compiler configuration flags.

You can enable native rebuild via `<WasmBuildNative>true</WasmBuildNative>`.

To add custom C source files into the runtime at compilation time, include native file references inside of an `ItemGroup` in your project, like `<NativeFileReference Include="fibonacci.c" />`.

This requires that you have the [wasm-tools workload](#wasm-tools-workload) installed.

## JavaScript host API
When the .NET runtime is hosted inside of a browser or other JavaScript environment, we expose a JavaScript API that can be used to configure and communicate with the runtime. It is documented in [dotnet.d.ts](../browser/runtime/dotnet.d.ts) and you can see some examples of its use in our [samples](../sample/wasm/).

### Browser application template
You can create a simple WebAssembly application by running `dotnet new wasmbrowser`. Then to run it, use `dotnet run` and open the URL which it wrote to the console inside your browser of choice. For example `http://localhost:5292/index.html`

Once you are ready to deploy your application, use `dotnet publish -c Release` which will publish your app optimized to the [AppBundle](#Project-folder-structure) folder.

### JavaScript interop
When you want to call JavaScript functions from C# or managed code from JavaScript, you can annotate `static` C# methods with `[JSImport]` or `[JSExport]` attributes. For more information on how to use these attributes, see:

* [Introductory Blog Post](https://devblogs.microsoft.com/dotnet/use-net-7-from-any-javascript-app-in-net-7/)
* [Todo-MVC sample](https://github.com/pavelsavara/dotnet-wasm-todo-mvc)
* or [the documentation](https://learn.microsoft.com/aspnet/core/client-side/dotnet-interop).

### Embedding dotnet in existing JavaScript applications
To embed the .NET runtime inside of a JavaScript application, you will need to use both the MSBuild toolchain (to build and publish your managed code) and your existing web build toolchain.

The output of the MSBuild toolchain - located in the [AppBundle](#Project-folder-structure) folder - must be fed in to your web build toolchain in order to ensure that the runtime and managed binaries are deployed with the rest of your application assets.

For a sample of using the .NET runtime in a React component, [see here](https://github.com/maraf/dotnet-wasm-react).

## Project folder structure


#### Source directory
The following paths are relative to a simple application's project directory.

You can use `dotnet new wasmbrowser` template to create this folder structure.

- `./MyApplication.csproj` - project file
- `./*.cs` - C# source files
- `./wwwroot` is an optional project folder, into which you can place files which should be also deployed to the web server.
- `./wwwroot/index.html` - could be your initial page. It typically contains `<script type='module' src="./main.js"></script>` and other HTML.
- `./wwwroot/main.js` - could be your initial script. It typically contains `import { dotnet } from './_framework/dotnet.js'` `await dotnet.run();` and other JavaScript.

#### Build output directories
The following shows the structure for a Release build, but except the name in the various paths, the rest is applicable for a `Debug` build too.

- `./bin/Release/net8.0/browser-wasm/AppBundle` - is the folder which should be hosted by the HTTP server.
- `./bin/Release/net8.0/browser-wasm/AppBundle/index.html` - the page which is hosting the application.
- `./bin/Release/net8.0/browser-wasm/AppBundle/main.js` - typically the main JavaScript entry point, it will `import { dotnet } from './_framework/dotnet.js'` to load the runtime and then `await dotnet.run();` in order to start it.
- `./bin/Release/net8.0/browser-wasm/AppBundle/_framework` - contains all the assets of the runtime and the managed application.


Note: You can flatten the `_framework` folder away by putting `<WasmRuntimeAssetsLocation>./</WasmRuntimeAssetsLocation>` in a property group in the project file.

Note: You can replace the location of `AppBundle` directory by  `<WasmAppDir>../my-frontend/wwwroot</WasmAppDir>` in a property group in the project file.

#### `_framework` folder structure
- `dotnet.js` - is the main entrypoint with the [JavaScript API](#JavaScript-API). It will load the rest of the runtime.
- `dotnet.native.js` - is posix emulation layer provided by the [Emscripten](https://github.com/emscripten-core/emscripten) project
- `dotnet.runtime.js` - is integration of the dotnet with the browser
- `blazor.boot.json` - contains list of all other assets and their integrity hash and also various configuration flags.
- `dotnet.native.wasm` - is the compiled binary of the dotnet (Mono) runtime.
- `System.Private.CoreLib.*` - is NET assembly with the core implementation of dotnet runtime and class library
- `*.wasm` - are .NET assemblies stored in `WebCIL` format (for better compatibility with firewalls and virus scanners).
- `*.dll` - are .NET assemblies stored in Portable Executable format (only used when you use `<WasmEnableWebcil>false</WasmEnableWebcil>`).
- `dotnet.js.map` - is a source map file, for easier debugging of the runtime code. It's not included in the published applications.
- `dotnet.native.js.symbols` - are debug symbols which help to put `C` runtime method names back to the `.wasm` stack traces. To enable generating it, use `<WasmEmitSymbolMap>true</WasmEmitSymbolMap>`.

## Hosting the application

### Caching and Integrity

Your browser could be caching various files and assets downloaded by the dotnet runtime so that the next application start will be much faster. When you deploy a new version of the application, you need to make sure that the caches in the browser and also in any HTTP proxies will not interfere.

If the end user's browser thinks that a copy of a given URL in its cache is new enough, your server has no way to force the browser to request it again.

There are various ways to mitigate this, including changing URLs.

To address that .NET 8 WebAssembly uses `no-cache` fetch directive and maintains it's own resource cache. We may change this in the future releases to use just browser's HTTP cache.

Configuring your web server to use send `ETag`, and read `If-Match` headers may save bandwidth and improve performance. The default Blazor server is configured already.

See also [Cache-Control on MDN](https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/Cache-Control)

See also [ETag header on MDN](https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/ETag)

In order to make sure that the application resources are consistent with each other, the .NET runtime will use `integrity` hashes for most the downloaded resources.

See also [fetch integrity on MDN](https://developer.mozilla.org/en-US/docs/Web/API/Request/integrity)

### Pre-fetching
In order to start downloading application resources as soon as possible you can add HTML elements to `<head>` of your page similar to:
Adding too many files into prefetch could be counterproductive.
Please benchmark your startup performance on real target devices and with realistic network conditions.

```html
<link rel="preload" href="./_framework/blazor.boot.json" as="fetch" crossorigin="use-credentials">
<link rel="prefetch" href="./_framework/dotnet.native.js" as="fetch" crossorigin="anonymous">
<link rel="prefetch" href="./_framework/dotnet.runtime.js" as="fetch" crossorigin="anonymous">
```

See also [link rel prefetch on MDN](https://developer.mozilla.org/en-US/docs/Web/HTML/Attributes/rel/prefetch)

See also [link rel preload on MDN](https://developer.mozilla.org/en-US/docs/Web/HTML/Attributes/rel/preload)

### MIME types

`Content-Type` HTTP headers tell the browser about the type of each downloaded asset. They are necessary for correct and fast processing by the browser, but also by various caches and proxies. HTTP headers are sent by your HTTP server or proxy, which need to be properly configured. If not set correctly, your application may load slowly or even fail to start.

| file extension  | Content-Type |
|---|---|
|.html|text/html|
|.js|text/javascript|
|.json|application/json|
|.wasm|application/wasm|
|.bin|application/octet-stream|
|.dat|application/octet-stream|

Optionally also

| file extension  | Content-Type |
|---|---|
|.map|application/json|
|.symbols|text/plain|
|.pdb|application/octet-stream|
|.dll|application/octet-stream|
|.webcil|application/octet-stream|

See also [Content-Type on MDN](https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/Content-Type)

### Compression
Modern browsers are able to automatically decompress files that have been compressed by the server, allowing for faster downloads and reduced startup time. By default, a Blazor application published will include gzip (`.gz`) and brotli (`.br`) compressed versions of each asset, and by configuring your web server correctly those files can be delivered to end users for reduced download and startup time.

See also [Content-Encoding on MDN](https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/Content-Encoding)

### Content security policy
dotnet runtime for wasm is more CSP compliant starting with .Net 8. In order to enable it, please set HTTP headers similar to `Content-Security-Policy: default-src 'self' 'wasm-unsafe-eval'`.

HTTP headers are sent by your HTTP server or proxy, which need to be properly configured.

Legacy JS interop methods of .Net 6 and below are not CSP compliant.

See also [CSP on MDN](https://developer.mozilla.org/en-US/docs/Web/HTTP/CSP)

See also [wasm-unsafe-eval](https://github.com/WebAssembly/content-security-policy/blob/main/proposals/CSP.md#the-wasm-unsafe-eval-source-directive)

### Globalization, ICU
Browsers do not offer full support for the globalization APIs available in .NET, so by default we provide our own version of the [ICU library](https://icu.unicode.org/) and databases. To reduce download sizes, by default the runtime will detect the end user's locale at startup, and load an appropriate slice of the ICU database only containing information for that locale.

For some use cases, you may wish to override this behavior or create a custom ICU database. For more information on doing this, see [globalization-icu-wasm.md](../../../docs/design/features/globalization-icu-wasm.md).

There are also rare use cases where your application does not rely on the contents of the ICU databases. In those scenarios, you can make your application smaller by enabling Invariant Globalization via the `<InvariantGlobalization>true</InvariantGlobalization>` msbuild property. For more details see [globalization-invariant-mode.md](../../../docs/design/features/globalization-invariant-mode.md).

We are currently developing a third approach for locales where we offer a more limited feature set by relying on browser APIs, called "Hybrid Globalization". This provides more functionality than Invariant Culture mode without the need to ship the ICU library or its databases, which improves startup time. You can use the msbuild property `<HybridGlobalization>true</HybridGlobalization>` to test this in-development feature, but be aware that it is currently incomplete and may have performance issues. For more details see [globalization-hybrid-mode.md](../../../docs/design/features/globalization-hybrid-mode.md).

Customized globalization settings require [wasm-tools workload](#wasm-tools-workload) to be installed.

### Timezones
Browsers do not offer a way to access the contents of their time zone database, so we deploy our own time zone database automatically as a part of your application. For applications that do not need to work with times or dates, you can use the `<InvariantTimezone>true</InvariantTimezone>` msbuild property to omit the database and reduce download size.

This requires that you have the [wasm-tools workload](#wasm-tools-workload) installed.

### Bundling JavaScript and other assets
Many web developers use tools like [webpack](https://github.com/webpack/webpack) or [rollup](https://github.com/rollup/rollup) to bundle many files into one large .js file. When deploying a .NET application to the web, you can safely bundle the `dotnet.js` ES6 module with the rest of your JavaScript application, but the other assets and modules in the `_framework` folder may not be bundled as they are loaded dynamically.

In our testing the dynamic loading of assets provides faster startup and shorter download times. We would like to [hear from the community](https://github.com/dotnet/runtime/issues/86162) if there are scenarios where you need the ability to bundle the rest of an application.

## Resources consumed on the target device
When you deploy a .NET application to the browser, many necessary components and databases are included:
- The .NET runtime, including a garbage collector, interpreter, and JIT compiler
- The .NET base class library
- An OS emulation layer that supplements browser features to provide a platform suitable for applications, like timezones and globalization
- Browser integration for features like HTTP and WebSockets
- And finally, your application binaries

All of the above must be downloaded and loaded into memory before your application can start. The browser must also perform its own JIT compilation at startup in order to run your application.

The result is that running a .NET application in the browser may require more memory and CPU resources than it would to run it natively using the .NET runtime outside of the browser.

### Mobile phones
Recent mobile phones distribute their browser as an application that can be upgraded separately from the mobile operating system. Therefore they receive latest features.

Note that all browsers on iOS and iPadOS are required to use the Safari browser engine, so their level of support for WASM features depends on the version of Safari installed on the device.

Mobile browsers typically have strict limits on the amount of memory they can use, and many users are on slow internet connections.

A WebAssembly application that works well on desktop PCs browser may take minutes to download or run out of memory before it is able to start on a mobile device, and the same is true for .NET.

### Shell environments - NodeJS & V8
While our primary target is web browsers, we have partial support for Node.JS v14 sufficient to pass most of our automated tests. We also have partial support for the D8 command-line shell, version 11 or higher, sufficient to pass most of our automated tests. Both of these environments may lack support for features that are available in the browser.

#### NodeJS < 20
Until node version 20, you may need to pass these arguments when running the application `--experimental-wasm-simd --experimental-wasm-eh`. When you run the application using `dotnet run`, you can add these to the runtimeconfig template

```json
"wasmHostProperties": {
    "perHostConfig": [
        {
            "name": "node",
            ...
            "host-args": [
                "--experimental-wasm-simd", // 👈 Enable SIMD support
                "--experimental-wasm-eh" // 👈 Enable exception handling support
            ]
        }
    ]
}
```

## Choosing the right platform target
Every end user has different needs, so the right platform for every application may differ.

Typical trade-offs of any web application are:
- startup time
- download size
- performance
- complexity of application maintenance

When compared .NET WebAssembly with native JavaScript applications:
- if your business logic is complex
- you already have existing C# codebase and skill-set

running the same code dotnet on wasm is probably the right choice.

If your application:
- is relatively simple
- you have JavaScript skills on your team
- require very small download and fast start time
- you need to support legacy devices or browsers

it may be better if you re/write your logic in Web native technologies like HTML/CSS and typescript/webpack stack.

Sometimes it makes sense to implement a mix of both.

## Developer tools

### wasm-tools workload
The `wasm-tools` workload contains all of the tools and libraries necessary to perform native rebuild or AOT compilation and other optimizations of your application.

Although it's optional for Blazor, **we strongly recommend using it!**

You can install it by running `dotnet workload install wasm-tools` from the command line.

You can also install `dotnet workload install wasm-experimental` to test out new experimental features and templates.
It includes the WASM templates for `dotnet new` and also preview version of multi-threading flavor of the runtime pack.

### Debugging

You can use browser dev tools to debug the JavaScript of the application and the runtime.

You could also debug the C# code using our integration with browser dev tools or Visual Studio.
See detailed [documentation](https://learn.microsoft.com/en-us/aspnet/core/blazor/debug)

You could also use it to debug the WASM code. In order to see `C` function names and debug symbols DWARF, see [Debug symbols](#Native-debug-symbols)

### Native debug symbols

You can add following elements in your .csproj
```xml
<PropertyGroup>
  <WasmNativeDebugSymbols>true</WasmNativeDebugSymbols>
  <WasmNativeStrip>false</WasmNativeStrip>
</PropertyGroup>
```

See also DWARF [WASM debugging](https://developer.chrome.com/blog/wasm-debugging-2020/) in Chrome.
For more details see also [debugger.md](../browser/debugger/debugger.md) and [wasm-debugging.md](../../../docs/workflow/debugging/mono/wasm-debugging.md)

### Runtime logging and tracing

You can enable detailed runtime logging.

```javascript
import { dotnet } from './dotnet.js'
await dotnet
        .withDiagnosticTracing(true) // enable JavaScript tracing
        .withConfig({environmentVariables: {
            "MONO_LOG_LEVEL":"debug", //enable Mono VM detailed logging by
            "MONO_LOG_MASK":"all", // categories, could be also gc,aot,type,...
        }})
        .run();
```

See also log mask [categories](https://github.com/dotnet/runtime/blob/88633ae045e7741fffa17710dc48e9032e519258/src/mono/mono/utils/mono-logger.c#L273-L308)

### Profiling

You can enable integration with browser profiler via following elements in your .csproj
```xml
<PropertyGroup>
  <WasmProfilers>browser;</WasmProfilers>
</PropertyGroup>
```

In Blazor, you can customize the startup in your index.html
```html
<script src="_framework/blazor.webassembly.js" autostart="false"></script>
<script>
Blazor.start({
    configureRuntime: function (builder) {
        builder.withConfig({
            browserProfilerOptions: {}
        });
    }
});
</script>
```

In simple browser template, you can add following to your `main.js`

```javascript
import { dotnet } from './dotnet.js'
await dotnet.withConfig({browserProfilerOptions: {}}).run();
```

### Diagnostic tools

We have initial implementation of diagnostic server and [event pipe](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/eventpipe)

At the moment it requires multi-threaded build of the runtime.

For more details see [diagnostic-server.md](../browser/runtime/diagnostics/diagnostic-server.md)
