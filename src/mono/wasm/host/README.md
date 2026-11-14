# Wasm App Host

WasmAppHost is used when `dotnet run` executes for projects targeting wasm.

## CoreCLR application arguments

SDK-created CoreCLR applications use the same launch options on Node.js, JavaScript
shells, Browser, and WASI. These options do not discover assemblies: the application
continues to use its SDK-generated boot configuration or WASI bundle.

```text
--dotnet-env=NAME=VALUE
--dotnet-runtime-property=NAME=JSON_SCALAR
--dotnet-help
--
```

Only leading `--dotnet-*` options are interpreted. The first ordinary argument, or
an explicit `--`, starts the application arguments. Everything after that point is
passed unchanged to managed `Main`, including empty arguments and further `--`
tokens. Application options such as `--help` and `--debug` are not launcher options.
Unknown `--dotnet-*` options and invalid assignments fail before managed startup.
Repeated assignments use the last value.

Runtime-property values must be JSON strings, finite numbers, or booleans. Null,
arrays, and objects are not supported. Host-owned properties, such as the assembly
search list and host contract, cannot be overridden. Environment values are strings;
only the first `=` after the name separates the value.

Quote the whole option when a value contains spaces. For a string runtime property
in PowerShell, use `'--dotnet-runtime-property=MyApp.Mode="fast"'`. Overrides cannot
restore features disabled at build time or removed by trimming.

For example:

```powershell
node .\_framework\dotnet.js --dotnet-env=APP_MODE=test --dotnet-runtime-property=System.Globalization.Invariant=true -- --input data.txt
dotnet run -- --dotnet-env=APP_MODE=test --dotnet-runtime-property=System.Globalization.Invariant=true -- --input data.txt
```

The first `--` in `dotnet run` belongs to the .NET SDK. The second belongs to the
packaged application launcher. Both examples pass `["--input", "data.txt"]` to `Main`.
Without launch options, the usual forms remain `node dotnet.js hello` and
`dotnet run -- hello`.

Direct Node execution of `dotnet.js` starts the packaged entry assembly and returns
its exit code. Importing `dotnet.js` does not start the application. Applications with
custom JavaScript startup can continue to import the builder and call `create`,
`runMain`, or `runMainAndExit`; launch overrides are applied before runtime
initialization and take precedence over packaged and builder configuration.
On Node.js, `process.env` also overrides packaged and builder environment values;
explicit `--dotnet-env` assignments take precedence over `process.env`.

Engine options remain engine options, for example
`node --enable-source-maps dotnet.js ...`. Configure the host selection and engine
arguments through `wasmHostProperties` and `WasmHostArguments`, not application
arguments. For example, `dotnet run -p:WasmHostArguments=--host=node -- hello`
selects the host configuration named `node`.

For Browser, WasmAppHost serves launch inputs from a per-process, non-cacheable
endpoint identified in the printed application URL. The loader reads it before
starting the runtime. No launch settings are written into the application's output,
and production applications without this URL parameter make no additional request.
The development server's inherited environment is not forwarded wholesale.

For the current CoreCLR WASI bundle layout, pass the same launch options after the
entry assembly, for example
`wasmtime run -W exceptions=y -S http --dir . --env CORE_ROOT=/managed managed/corerun managed/MyApp.dll --dotnet-env=APP_MODE=test -- hello`.
`dotnet run` supplies the bundle and entry-assembly arguments itself. Wasmtime flags,
such as `--dir`, remain before the module filename.
The SDK's `managed/corerun` file is the per-app linked `wasihost`, not the CoreRun
development runner. The generated runtime configuration identifies the runtime
flavor, so the launcher does not need to discover assemblies or guess the layout.

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