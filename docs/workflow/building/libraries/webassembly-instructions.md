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
./dotnet.sh build --configuration release /p:TargetArchitecture=wasm /p:TargetOS=Browser src/libraries/src.proj /t:NativeBinPlace 
```

