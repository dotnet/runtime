# DllImport Generator

Work on this project can be tracked and discussed via the [official issue](https://github.com/dotnet/runtime/issues/43060) in `dotnet/runtime`. The [P/Invoke source generator proposal](https://github.com/dotnet/runtime/blob/master/docs/design/features/source-generator-pinvokes.md) contains additional details.

## Example

The [Demo project](./DllImportGenerator/Demo) is designed to be immediately consumable by everyone. It demonstrates a simple use case where the marshalling code is generated and a native function call with only [blittable types](https://docs.microsoft.com/dotnet/framework/interop/blittable-and-non-blittable-types) is made. A managed assembly with [native exports](./DllImportGenerator/TestAssets/NativeExports) is used in the P/Invoke scenario.

### Recommended scenarios:

* Step into the `Demo` application and observe the generated code for the `Sum` functions.
* Find the implementation of the `sumrefi` function and set a breakpoint. Run the debugger and explore the stack.
* Add a new export in the `NativeExports` project and update the `Demo` application to call the new export.
* Try the above scenarios when building in `Debug` or `Release`. Consider the differences.

## Designs

- [Code generation pipeline](./designs/Pipeline.md)
- [Struct Marshalling](./designs/StructMarshalling.md)

## Workflow

All features of the [`dotnet` command line tool](https://docs.microsoft.com/dotnet/core/tools/) are supported for the respective project types (e.g. `build`, `run`, `test`). A consistent cross-platform inner dev loop with an IDE is available using [Visual Studio Code](https://code.visualstudio.com/) when appropriate .NET extensions are loaded.

On Windows, loading the [solution](./DllImportGenerator.sln) in [Visual Studio](https://visualstudio.microsoft.com/) 2019 or later will enable the edit, build, debug inner dev loop. All features of Visual Studio are expected to operate correctly (e.g. Debugger, Test runner, Profiler).

Most of the above options have [official tutorials](https://docs.microsoft.com/dotnet/core/tutorials/). It is an aim of this project to follow canonical workflows that are intuitive to all developers.

### Testing assets

This project has no explicit native build system and should remain that way. The [`DNNE`](https://github.com/AaronRobinsonMSFT/DNNE/) project is used to create native exports that can be called from the P/Invokes during testing.
