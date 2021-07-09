
# Mono Method Body Replacement Samples

## Prerequisites

To run the tests you will need to build a Mono runtime with the `hot_reload` component.

## Building

Both Debug and Release configurations should work.

For desktop:

```console
build.sh -s Mono+Libs
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
