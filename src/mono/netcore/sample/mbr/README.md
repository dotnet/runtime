
# Mono Method Body Replacement Samples

## Prerequisites

To run the tests you will need to build a runtime with the Mono metadata update
property set, and you will need the `roslynildiff` commandline tool.

At this time there is only support for Mac and Linux.  No Windows.

## Building

Build the runtime with the `/p:MonoMetadataUpdate=true` option.

Both Debug and Release configurations should work, although mixing a Release
configuration with a Debug runtime configuration (or vice versa) is not
supported.

For desktop:

```console
build.sh -s Mono+Libs /p:MonoMetadataUpdate=true
```


For Wasm:

Make sure `EMSDK_PATH` is set (see [workflow](../../../../../docs/workflow/building/libraries/webassembly-instructions.md))
```console
build.sh --os browser /p:MonoMetadataUpdate=true
```

## Running

Edit [browser/WasmDelta.csproj](./browser/WasmDelta.csproj) or [console/ConsoleDelta.csproj](./console/ConsoleDelta.csproj) and set the `RoslynILDiffFullPath` property to the path to the `roslynildiff` shell script:

```xml
   <!-- Add this before the Import of DeltaHelper.targets -->
   <PropertyGroup>
     <RoslynILDiffFullPath>/home/user/tools/roslyn-ildiff/roslynildiff</RoslynILDiffFullPath>
   </PropertyGroup>
```

(Make sure the configuration is the same as the runtime that you built)

For console:

```
make MONO_CONFIG=Debug publish && make MONO_CONFIG=Debug run
```

The output from `run` should print an old string, apply an update and then print a new string

For wasm:

```
make CONFIG=Debug && make CONFIG=Debug run
```

Then go to http://localhost:8000/ and click the button once or twice (the example has 2 updates prebuilt)


