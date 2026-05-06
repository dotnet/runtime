## .NET WASI app

## Build
### Default

You can build the app from Visual Studio or from the command-line:

```
dotnet build -c Debug/Release
```

After building the app, the result is in the `bin/$(Configuration)/netX.0/wasi-wasm/AppBundle` directory.

### As a single file bundle

Add `<WasmSingleFileBundle>true</WasmSingleFileBundle>` to your project file to enable this. It will result in a single `<name_of_the_main_assembly>.wasm` file which contains all the assemblies.

## Run

You can build the app from Visual Studio or the command-line:

```
dotnet run -c Debug/Release
```

Or directly start `wasmtime` from the AppBundle directory:

### default case

```
cd bin/$(Configuration)/netX.0/wasi-wasm/AppBundle
wasmtime run --dir .  -- dotnet.wasm <name_of_the_main_assembly>
```

### for single file bundle

```
cd bin/$(Configuration)/netX.0/wasi-wasm/AppBundle
wasmtime --dir . -- <name_of_the_main_assembly>.wasm
```
