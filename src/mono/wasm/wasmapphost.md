# Wasm App Host

Wasm App Host allows to use `dotnet run` to start a wasm application.

## Command line arguments

- **--debug** | **-d**: Whether to start debug server
- **--host** | **-h**: A host configuration name
- **--runtime-config** | **-r**: A path for the runtimeconfig.json to use

## Host configuration

Wasm App Host supports starting the application on various engines, like browser, node js or v8. These engines has configuration in the `runtimeconfig.template.json` under `wasmHostProperties/perHostConfig`. Each of the configuration in this array has a name that can be passed as a command-line argument `--host` or `-h` when starting the application. If it's not specified, a first one is used. 

Depending on the engine, various properties can be set. 

### Browser (`host: browser`)

- **html-path**: A relative path of an HTML file to open in the browser. Eg.: `index.html`

### V8 (`host: v8`)

- **js-path**: A relative path of a JavaScript file to execute. Eg.: `main.mjs`

### Node JS (`host: nodejs`)

- **js-path**: A relative path of a JavaScript file to execute. Eg.: `main.mjs`

