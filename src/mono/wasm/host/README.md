# Wasm App Host

WasmAppHost is used when `dotnet run` executes for projects targeting wasm.

## Dev server

For projects built with the WebAssembly SDK (`--use-staticwebassets`), the browser host serves the app
through the shared [Blazor Gateway](https://github.com/dotnet/aspnetcore/tree/main/src/Components/Gateway)
(`Microsoft.AspNetCore.Components.Gateway`). The Gateway ships as a self-contained application; the WasmAppHost
launches it as a subprocess and reads its listening URLs from the process output. Static web assets, including
the SPA fallback, are served from the generated static web assets endpoints manifest via `MapStaticAssets`, so
the served application should be built with `StaticWebAssetSpaFallbackEnabled` enabled to get a fallback route.

Some runtime/testing behaviors the previous in-process dev server provided are not yet available through the
Gateway and are expected to be added upstream: browser console output forwarding (`/console` WebSocket),
cross-origin isolation headers (COOP/COEP) for multi-threaded runtimes, WebAssembly debugging, and the
`DEVSERVER_UPLOAD_PATH` file upload endpoint. Scenarios relying on these will regress until the upstream gaps
are closed. See [dotnet/runtime#122144](https://github.com/dotnet/runtime/issues/122144).

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