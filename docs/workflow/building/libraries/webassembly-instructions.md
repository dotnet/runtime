# Build libraries for WebAssembly

## Prerequisites

If you haven't already done so, please read [this document](../../README.md#Build_Requirements) to understand the build requirements for your operating system.

## Building

At this time no other build dependencies are necessary to start building for WebAssembly. Emscripten will be downloaded and installed automatically in the build process. To read how to build on specific platforms, see [Building](../../../../src/mono/browser/README.md#building).

This document explains how to work on the runtime or libraries. If you haven't already done so, please read [this document](../../README.md#Configurations) to understand configurations.

When rebuilding with `build.sh` after a code change, you need to ensure that the `mono.wasmruntime` and `libs.pretest` subsets are included even for a Mono-only change or this directory will not be updated (details below).

### Note: do not mix runtime and library configurations

At this time, it is not possible to specify different configurations for the runtime and libraries. That is mixing a Release `-runtimeConfiguration` with a Debug `-libraryConfiguration` (or `-configuration`), or vice versa will not work. The same applies to single and multithreaded configurations.

Please only use the `-configuration` option with `Debug` or `Release`, and do not specify a `-runtimeConfiguration` and `-libraryConfiguration`.

This is tracked in https://github.com/dotnet/runtime/issues/42553


## Building Mono's System.Private.CoreLib or runtime

If you are working on core parts of Mono you will probably need to build the Mono runtime and [System.Private.CoreLib](../../../design/coreclr/botr/corelib.md) which can be built with the following:

```bash
./build.sh mono -os browser -c Debug|Release
```

To build just System.Private.CoreLib without the Mono runtime you can use the `Mono.CoreLib` subset:

```bash
./build.sh mono.corelib -os browser -c Debug|Release
```

To build just the Mono runtime without System.Private.CoreLib use the `Mono.Runtime` subset:

```bash
./build.sh mono.runtime -os browser -c Debug|Release
```

Building both Mono/System.Private.CoreLib and the managed libraries:

```bash
./build.sh mono+libs -os browser -c Debug|Release
```

## Building the WebAssembly runtime files

The WebAssembly implementation files are built after the libraries source build and made available in the artifacts folder. If you are working on the code base and need to compile just these modules then building the `Mono.WasmRuntime` subset will allow one to do that:

```bash
./build.sh mono.wasmruntime -os browser -c Debug|Release
```

## Updating in-tree runtime pack

If you don't run the full `Libs` subset then you can use the `Libs.PreTest` subset to copy updated runtime/corelib binaries to the runtime pack which is used for running tests:

```bash
./build.sh libs.pretest -os browser -c Debug|Release
```

## Building libraries native components only

The libraries build contains some native code. This includes shims over libc, openssl, gssapi, and zlib. The build system uses CMake to generate Makefiles using clang. The build also uses git for generating some version information.

```bash
./build.sh libs.native -os browser -c Debug|Release
```

## Building individual libraries

Individual projects and libraries can be build by specifying the build configuration.

**Examples**

- Build all projects for a given library (e.g.: System.Net.Http) including the tests

```bash
./build.sh -os browser -c Release --projects <full-repository-path>/src/libraries/System.Net.Http/System.Net.Http.slnx
```

- Build only the source project of a given library (e.g.: System.Net.Http)

```bash
 ./build.sh -os browser -c Release --projects <full-repository-path>/src/libraries/System.Net.Http/src/System.Net.Http.csproj
```

More information and examples can be found in the [libraries](./README.md#building-individual-libraries) document.

## Notes

A `Debug` build sets the following environment variables by default:

- debugging and logging which will log garbage collection information to the console.

```
MONO_LOG_LEVEL=debug
MONO_LOG_MASK=gc
```

  #### Example:
```
L: GC_MAJOR_SWEEP: major size: 752K in use: 39K
L: GC_MAJOR: (user request) time 3.00ms, stw 3.00ms los size: 0K in use: 0K
```

- Redirects the `System.Diagnostics.Debug` output to `stderr` which will show up on the console.

```
    // Setting this env var allows Diagnostic.Debug to write to stderr.  In a browser environment this
    // output will be sent to the console.  Right now this is the only way to emit debug logging from
    // corlib assemblies.
    monoeg_g_setenv ("DOTNET_DebugWriteToStdErr", "1", 0);
```

## Updating Emscripten version in Docker image

First update the Emscripten version in the current WebAssembly image definition in
[dotnet-buildtools-prereqs-docker](https://github.com/dotnet/dotnet-buildtools-prereqs-docker/tree/main/src/azurelinux).
The active WebAssembly images are tracked in the
[Azure Linux manifest](https://github.com/dotnet/dotnet-buildtools-prereqs-docker/blob/main/src/azurelinux/manifest.json).

Submit a PR with the Dockerfile change and wait for the image publishing flow to complete.
Once the image is published, find the new tag in the corresponding
[image-info file](https://github.com/dotnet/versions/blob/main/build-info/docker/image-info.dotnet-dotnet-buildtools-prereqs-docker-main.json)
in `dotnet/versions`.

Then update the WebAssembly image references in this repo:

- [eng/pipelines/helix-platforms.yml](https://github.com/dotnet/runtime/blob/main/eng/pipelines/helix-platforms.yml)
- [eng/pipelines/libraries/helix-queues-setup.yml](https://github.com/dotnet/runtime/blob/main/eng/pipelines/libraries/helix-queues-setup.yml)

Open a PR with the updated image references.

# Test libraries

You can read about running library tests in [Libraries tests](../../../../src/mono/browser/README.md#libraries-tests).
