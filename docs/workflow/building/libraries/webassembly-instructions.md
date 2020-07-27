# Build for WebAssembly

## Prerequisites

If you haven't already done so, please read [this document](../../README.md#Build_Requirements) to understand the build requirements for your operating system.

An installation of the emsdk needs to be installed.  Follow the installation guide [here](https://emscripten.org/docs/getting_started/downloads.html#sdk-download-and-install).  Once installed the EMSDK_PATH needs to be set:

On Linux and MacOSX:

```bash
export EMSDK_PATH=PATH_TO_SDK_INSTALL/emsdk
```

## Building everything

At this time no other build configurations are necessary to start building for WebAssembly.  The CoreLib for WebAssembly build configurations will be built by default using the WebAssembly configuration shown below. 

This document explains how to work on libraries. In order to work on library projects or run library tests it is necessary to have built the runtime to give the libraries something to run on. If you haven't already done so, please read [this document](../../README.md#Configurations) to understand configurations.


For Linux and MacOSX:
```bash
./build.sh --arch wasm --os Browser --configuration release
```

Detailed information about building and testing runtimes and the libraries is in the documents linked below.

## How to build native components only

The libraries build contains some native code. This includes shims over libc, openssl, gssapi, and zlib. The build system uses CMake to generate Makefiles using clang. The build also uses git for generating some version information.

**Examples**

- Building in debug mode for platform wasm and Browser operating system
```bash
./build.sh --arch wasm --os Browser --subset Libs.Native --configuration Debug
```

- Building in release mode for platform wasm and Browser operating system
```bash
./build.sh --arch wasm --os Browser --subset Libs.Native --configuration Release
```

## How to build mono System.Private.CoreLib

If you are working on core parts of mono libraries you will probably need to build the [System.Private.CoreLib](../../../design/coreclr/botr/corelib.md) which can be built with the following:


```bash
./build.sh --arch wasm --os Browser --configuration release --subset Mono
```

To build just SPC without mono you can use the Mono.CoreLib subset.

```bash
./build.sh --arch wasm --os Browser --configuration release --subset Mono.CoreLib
```


Building the managed libraries as well:

```bash
./build.sh --arch wasm --os Browser --configuration release --subset Mono+Libs
```

## Building individual libraries

Individual projects and libraries can be build by specifying the build configuration.

Building individual libraries
**Examples**

- Build all projects for a given library (e.g.: System.Net.Http) including running the tests

```bash
 ./build.sh --arch wasm --os Browser --configuration release --projects src/libraries/System.Net.Http/System.Net.Http.sln
```

- Build only the source project of a given library (e.g.: System.Net.Http)

```bash
 ./build.sh --arch wasm --os Browser --configuration release --projects src/libraries/System.Net.Http/src/System.Net.Http.csproj
```

More information and examples can be found in the [libraries](./README.md#building-individual-libraries) document.

## Building the WebAssembly runtime files

The WebAssembly implementation files are built and made available in the artifacts folder.  If you are working on the code base and need to compile just these modules then the following will allow one to do that.

For Linux and MacOSX:
```bash
./dotnet.sh build /p:Configuration=Debug|Release /p:TargetArchitecture=wasm /p:TargetOS=Browser src/libraries/src.proj /t:BuildWasmRuntimes 
```

__Note__: A `Debug` build sets the following environment variables by default.  When built from the command line this way the `Configuration` value is case sensitive.

- debugging and logging which will log garbage collection information to the console.

```
   monoeg_g_setenv ("MONO_LOG_LEVEL", "debug", 0);
   monoeg_g_setenv ("MONO_LOG_MASK", "gc", 0);
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
    monoeg_g_setenv ("COMPlus_DebugWriteToStdErr", "1", 0);
```

## Updating Emscripten version in Docker image

First update emscripten version in the [webassembly Dockerfile](https://github.com/dotnet/dotnet-buildtools-prereqs-docker/blob/master/src/ubuntu/18.04/webassembly/Dockerfile#L19).

```
ENV EMSCRIPTEN_VERSION=1.39.16
```

Submit a PR request with the updated version, wait for all checks to pass and for the request to be merged. A [master.json file](https://github.com/dotnet/versions/blob/master/build-info/docker/image-info.dotnet-dotnet-buildtools-prereqs-docker-master.json#L1126) will be updated with the a new docker image.  

```
{
  "platforms": [
    {
      "dockerfile": "src/ubuntu/18.04/webassembly/Dockerfile",
      "simpleTags": [
        "ubuntu-18.04-webassembly-20200529220811-6a6da63"
      ],
      "digest": "sha256:1f2d920a70bd8d55bbb329e87c3bd732ef930d64ff288dab4af0aa700c25cfaf",
      "osType": "Linux",
      "osVersion": "Ubuntu 18.04",
      "architecture": "amd64",
      "created": "2020-05-29T22:16:52.5716294Z",
      "commitUrl": "https://github.com/dotnet/dotnet-buildtools-prereqs-docker/blob/6a6da637580ec557fd3708f86291f3ead2422697/src/ubuntu/18.04/webassembly/Dockerfile"
    }
  ]
},
```

Copy the docker image tag and replace it in [platform-matrix.yml](https://github.com/dotnet/runtime/blob/master/eng/pipelines/common/platform-matrix.yml#L172)

```
container:
    image: ubuntu-18.04-webassembly-20200409132031-f70ea41
    registry: mcr
```

Open a PR request with the new image. 
