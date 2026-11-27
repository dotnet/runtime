# Experimental WASI support

This directory contains the CoreCLR-WASI app build targets and sample projects. This is not intended for production use, nor is it currently supported.

For WebAssembly documentation, see [WebAssembly Documentation](../../../docs/workflow/wasm-documentation.md).

## Try it out

Here is a quick overview of how to consume published artifacts. Assuming .NET SDK is already installed, you should run:

```
dotnet workload install wasi-experimental
```

This will install the workload for building .NET based WASI apps.
You can start from the sample projects under `src/mono/sample/wasi`, or author a project that targets `wasi-wasm`, and run it with

```
dotnet run
```

The `runtimeconfig.template.json` contains `perHostConfig` section where wasm hosts can be configured

### Wasi SDK

The workload for the time being doesn't include Wasi SDK, which is responsible for native compilation.
If you don't need to modify runtime configuration, you can omit this step. In case you get:

```
error : Could not find wasi-sdk. Either set $(WASI_SDK_PATH), or use workloads to get the sdk. SDK is required for building native files.
```

you will need to separately download a WASI SDK from https://github.com/WebAssembly/wasi-sdk and point an environment variable `WASI_SDK_PATH` to a location where you extract it.

### Optional build flags

- `WasmSingleFileBundle` - bundle all assets into the `.wasm`. The output file name will match the project name.
- `InvariantGlobalization` - remove globalization support, decrease the publish size.
- More details can be found at https://github.com/dotnet/runtime/blob/main/src/mono/wasm/build/WasmApp.Common.targets and https://github.com/dotnet/runtime/blob/main/src/mono/wasi/build/WasiApp.targets

## Native linking and ReadyToRun

CoreCLR-WASI links the `wasihost` corehost for each application. The implementation is in
[`WasiApp.CoreCLR.targets`](build/WasiApp.CoreCLR.targets), which generates the application's
call-helper tables and runtime configuration source before linking the host.

ReadyToRun compilation is enabled with `/p:PublishReadyToRun=true`. CoreCLR-WASI requires a
composite WebAssembly ReadyToRun image and does not support `WasmSingleFileBundle`.

For more detailed WASI build and configuration guidance, see the [WASI build documentation](../../../docs/workflow/building/libraries/webassembly-instructions.md).

## How it works

The application bundle contains the managed framework and application assemblies alongside a
per-application linked `wasihost` CoreCLR host. Wasmtime loads the component and the host starts
CoreCLR using the bundled managed framework.

## How to build the runtime

on Linux:
```.sh
./build.sh -bl -os wasi -subset clr+libs+host -c Debug
```

And you can use that runtime pack when building outside of the `dotnet/runtime` tree by overriding the runtime pack via


```xml
    <PropertyGroup>
        <DotnetRuntimeRepoRoot>../path/to/dotnet/runtime</DotnetRuntimeRepoRoot>
    </PropertyGroup>
	<ItemGroup>
		<!-- update runtime pack to local build -->
		<ResolvedRuntimePack PackageDirectory="$(DotnetRuntimeRepoRoot)/artifacts/bin/microsoft.netcore.app.runtime.wasi-wasm/Release"
			Condition="'%(ResolvedRuntimePack.FrameworkName)' == 'Microsoft.NETCore.App'" />
	</ItemGroup>
```

### 3. Run it

Finally, you can build and run the sample:

```
./dotnet.sh build /p:TargetOS=wasi /p:Configuration=Debug /t:RunSample src/mono/sample/wasi/console
```

### 4. Debug it

For detailed WASI debugging instructions, see the [WebAssembly Debugging Reference](../../../docs/workflow/debugging/mono/wasm-debugging.md#for-wasi-applications).
