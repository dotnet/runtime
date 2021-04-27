
# Mono Method Body Replacement Samples

## Prerequisites

To run the tests you will need to:

1. Build a runtime with the `MonoMetadataUpdate` property set to true: `/p:MonoMetadataUpdate=true`.
2. Put the [hotreload-delta-gen](https://github.com/dotnet/hotreload-utils/tree/main/src/hotreload-delta-gen) command line tool somewhere on your `PATH`.

   Build the tool with `dotnet publish --self-contained -r <RID>` and then add the `publish/hotreload-delta-gen` directory to your `PATH`.
   If you don't want to add it to your `PATH`, you can set the `HotReloadDeltaGenFullPath` msbuild property in `DeltaHelper.targets` to the full path name of the published executable.

The tool, runtime changes and samples should work on Mac and Linux.  Windows might work, but it hasn't been tested.

## Building

Build the runtime with the `/p:MonoMetadataUpdate=true` option.

Both Debug and Release configurations should work.

For desktop:

```console
build.sh -s Mono+Libs /p:MonoMetadataUpdate=true
```


For WebAssembly:

Make sure `EMSDK_PATH` is set (see [workflow](../../../../docs/workflow/building/libraries/webassembly-instructions.md))
```console
build.sh --os browser
```

For Apple targets:

```console
build.sh --os MacCatalyst -s Mono+Libs
```

or

```console
build.sh --os iOSSimulator -s Mono+Libs
```

## Running

For console (desktop):

```
make CONFIG=Debug publish && make CONFIG=Debug run
```

The output from `run` should print an old string, apply an update and then print a new string

For browser (WebAssembly):

```
make CONFIG=Debug && make CONFIG=Debug run
```

Then go to http://localhost:8000/ and click the button once or twice (the example has 2 updates prebuilt)

For Apple targets:

for ios simulator
```
make run-sim
```

for Mac Catalyst

```
make run-catalyst
```
