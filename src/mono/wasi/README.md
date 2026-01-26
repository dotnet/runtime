# Prototype WASI support

This directory contains a build configuration for WASI support, plus a basic sample. This is not intended for production use, nor is it currently supported. This is a step towards possible future support.

For WebAssembly documentation, see [WebAssembly Documentation](../../../docs/workflow/wasm-documentation.md).

## Try it out

Here is a quick overview of how to consume published artifacts. Assuming .NET SDK is already installed, you should run:

```
dotnet workload install wasi-experimental
```

This will install workload for building .NET based WASI apps + basic template.
Now you can create a new .NET application that targets WASI

```
dotnet new wasiconsole
```

And run it with

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

## Properties That Trigger Relinking

### What is Relinking?

Relinking in WASI builds refers to regenerating the native WASI runtime artifacts (such as `dotnet.native.wasi`, etc.) to reflect changes in configuration, features, or source. This process is essential for producing correct, optimized binaries for WASI environments.

### Properties That Trigger Relinking in `wasi.proj`

The following MSBuild properties will trigger a relinking during the WASI build process:

- **`WasiBuildNative`** - `/p:WasiBuildNative=true` - Forces a rebuild of all native WASI binaries, regardless of detected changes.
- **`WasiRelinkNative`** - `/p:WasiRelinkNative=true` - Explicitly requests relinking of native WASI artifacts.
- **`RunAOTCompilation`** - `/p:RunAOTCompilation=true` - Enables AOT compilation for WASI. Changing this property requires relinking for correct WASI output.
- **`WasmSingleFileBundle`** - `/p:WasmSingleFileBundle=true` - Bundles all assets into the `.wasm` file, requiring native relinking.
- **`EnableDiagnostics`** - `/p:EnableDiagnostics=true` - Enables or disables diagnostic features in the native runtime.
- **`WasmProfilers`** - `/p:WasmProfilers=...` - Changes profiler configuration in the native runtime.
- **`WasmEnableSIMD`** - `/p:WasmEnableSIMD=true` - Enables or disables SIMD instruction support.
- **`WasiBuildArgs`** - Any change to `/p:WasiBuildArgs=...` (custom build flags or feature toggles) can trigger a relink.
- **Configuration/Target Architecture** - Changing `/p:Configuration=Debug|Release` or `/p:RuntimeIdentifier=wasi-wasm`, etc., may require relinking.

#### Notes

- Correct relinking ensures WASI binaries are up-to-date and accurately reflect the intended configuration.
- Avoid unnecessary relinking for faster incremental builds.
- The relink logic is defined in `wasi.proj` and supporting MSBuild files.
- Source changes to native sources in `src/mono/wasi` or related directories will always trigger a relink.

For more detailed WASI build and configuration guidance, see the [WASI build documentation](../../../docs/workflow/building/libraries/webassembly-instructions.md).

## How it works

The mechanism for executing .NET code in a WASI runtime environment is equivalent to how `dotnet.wasm` executes .NET code in a browser environment. That is, it runs the Mono interpreter to execute .NET bytecode that has been built in the normal way. It should also work with AOT but this is not yet attempted.

## How to build the runtime

on Linux:
```.sh
./build.sh -bl -os wasi -subset mono+libs -c Debug
```
or for just native rebuild
```.sh
./build.sh -bl -os wasi -subset mono.runtime+libs.native+mono.wasiruntime -c Debug
```
You can enable full assertion messages for local release builds using
`-p:MonoEnableAssertMessages=true`

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
