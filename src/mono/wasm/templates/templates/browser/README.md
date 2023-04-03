## .NET WebAssembly Browser app

## Build

You can build the app from Visual Studio or from the command-line:

```
dotnet build -c Debug/Release
```

After building the app, the result is in the `bin/$(Configuration)/net7.0/browser-wasm/AppBundle` directory.

## Run

You can build the app from Visual Studio or the command-line:

```
dotnet run -c Debug/Release
```

Or you can start any static file server from the AppBundle directory:

```
dotnet tool install dotnet-serve
dotnet serve -d:bin/$(Configuration)/net7.0/browser-wasm/AppBundle
```