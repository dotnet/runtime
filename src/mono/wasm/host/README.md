# Wasm App Host

WasmAppHost is used when `dotnet run` executes for projects targeting wasm.

## Dev server

For projects built with the WebAssembly SDK (`--use-staticwebassets`), the browser host serves the app
through the shared [Blazor Gateway](https://github.com/dotnet/aspnetcore/tree/main/src/Components/Gateway)
(`Microsoft.AspNetCore.Components.Gateway`). The Gateway ships as a self-contained application; the WasmAppHost
launches it as a subprocess and reads its listening URLs from the process output. Static web assets, including
the SPA fallback, are served from the generated static web assets endpoints manifest via `MapStaticAssets`. The
WebAssembly SDK enables `StaticWebAssetSpaFallbackEnabled` by default for root apps, so `index.html` is served
as the fallback route (e.g. for `/`), matching the fallback the in-process dev server used to provide.

Some runtime/testing behaviors the previous in-process dev server provided are handled without adding code to
the shared Gateway (see [dotnet/aspnetcore#67814](https://github.com/dotnet/aspnetcore/issues/67814)):

- **Cross-origin isolation headers (COOP/COEP)** for multi-threaded runtimes are baked into the static web
  assets endpoints manifest at build time (see `Microsoft.NET.Sdk.WebAssembly.CrossOriginIsolation.targets`),
  so the Gateway emits them via `MapStaticAssets` with no host code. Enabled by default when
  `WasmEnableThreads` is `true`; override with `WasmEnableCrossOriginIsolation`.
- **Browser console forwarding (`/console` WebSocket)** and the **`DEVSERVER_UPLOAD_PATH` upload endpoint
  (`POST /upload/{filename}`)** are hosted in-process by WasmAppHost and reached through the Gateway's built-in
  YARP reverse proxy, which is pointed back at them via command-line configuration. These dev/test-only
  endpoints are only started when needed (console forwarding requested, or the upload environment variables set).

WebAssembly debugging is not yet available through the Gateway and is expected to be added upstream. See
[dotnet/runtime#122144](https://github.com/dotnet/runtime/issues/122144).

## Command line arguments

- **--debug** | **-d**: Whether to start debug server. [More on debugging](../debugger/debugger.md).
- **--host** | **-h**: A host configuration name.
- **--runtime-config** | **-r**: A path for the runtimeconfig.json to use.

## Runtime config

The `runtimeconfig.template.json` is a template that used by the .NET runtime to run the application. The configuration for Wasm App Host is defined int the `wasmHostProperties`. Following properties can be applied.

- `webServerPort`: A port number to start HTTP server on, defaults to `9000`.
- `perHostConfig`: An array of configuration per host type.
- `defaultConfig`: A name of the default per-host configuration.
- `firefoxProxyPort`: A port number where Mono debug proxy for Firefox is listening.
- `firefoxDebuggingPort`: A port number where Firefox is listening for remote debugging.
- `chromeProxyPort`: A port number where Mono debug proxy for Chrome is listening.
- `chromeDebuggingPort`: A port number where Chrome is listening for remote debugging.

## Per host configuration

Wasm App Host supports running the application on various hosts, like browser, node js, v8, etc. These hosts has configuration in the `wasmHostProperties/perHostConfig`. Each of the configurations in this array has a name that can be passed as a command-line argument `--host` (`-h`) when starting the application, or set as a `defaultConfig` in the `runtimeconfig.template.json`. If it's not specified, the first declaration is used.

> To use JavaScript engines or node, the executable needs to be present in the `PATH`.

Depending on the host type, various properties can be set.

### Browser

- `host: browser`: specifies that this configuration is for running on the browser.
- `html-path`: A relative path of an HTML file to open in the browser. Eg.: `index.html`

### V8

- `host: v8`: specifies that this configuration is for running with `V8`.
- `js-path`: A relative path of a JavaScript file to execute. Eg.: `main.mjs`

### Node JS

- `host: nodejs`: specifies that this configuration is for running with `NodeJS`.
- `js-path`: A relative path of a JavaScript file to execute. Eg.: `main.mjs`

### SpiderMonkey

- `host: spidermonkey`: specifies that this configuration is for running with `SpiderMonkey`.
- `js-path`: A relative path of a JavaScript file to execute. Eg.: `main.mjs`

### JavaScriptCore

- `host: javascriptcore`: specifies that this configuration is for running with `JavaScriptCore`.
- `js-path`: A relative path of a JavaScript file to execute. Eg.: `main.mjs`