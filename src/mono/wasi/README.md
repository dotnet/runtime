# Prototype WASI support

This directory contains a build configuration for WASI support, plus a basic sample. This is not intended for production use, nor is it currently supported. This is a step towards possible future support.

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

Also, you can build and debug the sample:

```
cd sample/console
make debug
```

Using Visual Studio code, add a breakpoint on Program.cs line 17.
Download the Mono Debug extension and configure a launch.json like this:
```
{
    "version": "0.2.0",
    "configurations": [
        {
            "name": "Attach",
            "type": "mono",
            "request": "attach",
            "address": "localhost",
            "port": 64000
        }
    ]
}
```