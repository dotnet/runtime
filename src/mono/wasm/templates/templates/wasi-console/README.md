## .NET WASI app

## Build

You can build the app from Visual Studio or from the command-line:

```
dotnet build -c Debug/Release
```

After building the app, the result is in the `bin/$(Configuration)/netX.0/wasi-wasm/AppBundle` directory.

## Run

You can build the app from Visual Studio or the command-line:

```
dotnet run -c Debug/Release
```

Or directly start node from the AppBundle directory:

```
wasmtime bin/$(Configuration)/netX.0/browser-wasm/AppBundle/wasi-console.0.wasm
```
