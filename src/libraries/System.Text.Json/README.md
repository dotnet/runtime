# System.Text.Json

This project defines the `System.Text.Json` serialization library. It provides low-level, forward-only [JSON reader and writer components](https://learn.microsoft.com/dotnet/standard/serialization/system-text-json/use-dom-utf8jsonreader-utf8jsonwriter#use-utf8jsonwriter) and a JSON serialization layer for .NET types using both [runtime reflection and compile-time source generation](https://learn.microsoft.com/dotnet/standard/serialization/system-text-json/source-generation-modes). The library also includes a [couple of JSON DOM implementations](https://learn.microsoft.com/en-gb/dotnet/standard/serialization/system-text-json/use-dom-utf8jsonreader-utf8jsonwriter?pivots=dotnet-7-0#json-dom-choices) that can be used for introspection and manipulation of JSON documents.

Documentation can be found at https://learn.microsoft.com/dotnet/standard/serialization/system-text-json/overview.

## Contribution Bar

- [x] [We consider new features, new APIs and performance changes](../README.md#primary-bar)
- [x] [We consider PRs that target this library for new source code analyzers](../README.md#secondary-bars)

See the [Help Wanted](https://github.com/dotnet/runtime/issues?q=is:issue+is:open+label:area-System.Text.Json+label:%22help+wanted%22) issues.

## Source

* System.Text.Json runtime: [src/](src/)
* System.Text.Json source generator: [gen/](gen/)

## Building & Testing

Building and testing System.Text.Json follows the same workflow as other libraries. Instructions can be found in the relevant section in the [workflow guide](/docs/workflow/README.md).

### Debugging the Source Generator

Developers wishing to debug the compile-time source generator itself should build the source generator using the `LaunchDebugger` property:

```bash
$ cd src/libraries/System.Text.Json/
$ dotnet build -p:LaunchDebugger=true gen/System.Text.Json.SourceGeneration.Roslyn4.0.csproj # replace with appropriate Roslyn version
```

Subsequent runs of the source generator will attempt to attach themselves to a debugger. Please ensure that any IDEs consuming the source generator are restarted after the above command is run.

## Deployment

The library is shipped as part of the .NET shared framework and as a [NuGet package](https://www.nuget.org/packages/System.Text.Json). Daily builds of the NuGet package can be obtained following [these instructions](/docs/project/dogfooding.md#obtaining-daily-builds-of-nuget-packages).
