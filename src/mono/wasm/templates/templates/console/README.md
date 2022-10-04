## Node application

## Build

You can build the application from Visual Studio or by dotnet cli

```
dotnet build -c Debug/Release
```

After building the application, the result is in the `bin/$(Configuration)/net7.0/browser-wasm/AppBundle` directory.

## Run

You can build the application from Visual Studio or by dotnet cli

```
dotnet run -c Debug/Release
```

Or you can start any static file server from the AppBundle directory

```
node bin/$(Configuration)/net7.0/browser-wasm/AppBundle/main.mjs
```