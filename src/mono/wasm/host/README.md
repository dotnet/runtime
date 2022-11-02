# Wasm App Host

WasmAppHost is used when `dotnet run` executes for wasm targeting projects.

## Command line arguments

- **--debug** | **-d**: Whether to start debug server.
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

Wasm App Host supports running the application on various hosts, like browser, node js, v8, etc. These hosts has configuration in the `wasmHostProperties/perHostConfig`. Each of the configuration in this array has a name that can be passed as a command-line argument `--host` (`-h`) when starting the application, or as a `defaultConfig` in the `runtimeconfig.template.json`. If it's not specified, a first one declaration is used. 

> To use JavaScript engines or node, the executable needs to be present in the `PATH`.

Depending on the host type, various properties can be set. 

### Browser

- `host: browser`: A identify this configuration to run on the browser.
- `html-path`: A relative path of an HTML file to open in the browser. Eg.: `index.html`

### V8

- `host: v8`: A identify this configuration to run on the V8.
- `js-path`: A relative path of a JavaScript file to execute. Eg.: `main.mjs`

### Node JS

- `host: v8`: A identify this configuration to run on the node.
- `js-path`: A relative path of a JavaScript file to execute. Eg.: `main.mjs`

### SpiderMonkey

- `host: spidermonkey`: A identify this configuration to run on the SpiderMonkey.
- `js-path`: A relative path of a JavaScript file to execute. Eg.: `main.mjs`

### JavaScriptCore

- `host: javascriptcore`: A identify this configuration to run on the JavaScriptCore.
- `js-path`: A relative path of a JavaScript file to execute. Eg.: `main.mjs`