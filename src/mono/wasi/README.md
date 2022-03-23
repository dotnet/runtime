# Prototype WASI support

This directory contains a build configuration for WASI support, plus a basic sample. This is not intended for production use, nor is it currently supported. This is a step towards possible future support.

## How it works

The mechanism for executing .NET code in a WASI runtime environment is equivalent to how `dotnet.wasm` executes .NET code in a browser environment. That is, it runs the Mono interpreter to execute .NET bytecode that has been built in the normal way. It should also work with AOT but this is not yet attempted.

## How to build the runtime

Currently this can only be built in Linux or WSL (tested on Windows 11). Simply run `make` in this directory. It will automatically download and use [WASI SDK](https://github.com/WebAssembly/wasi-sdk).

The resulting libraries are placed in `(repo_root)/artifacts/bin/mono/Wasi.Release`.

## How to build and run the sample

### 1. Obtain a WASI runtime

To run an application in a WASI environment, you need to have a WASI runtime available. For example, download [wasmtime](https://github.com/bytecodealliance/wasmtime/releases) and make sure it's available on `PATH`:

```
export PATH=~/wasmtime-v0.31.0-x86_64-linux
wasmtime --version
```

Other WASI runtimes also work. Tested: [wamr](https://github.com/bytecodealliance/wasm-micro-runtime), [wasmer](https://wasmer.io/).

### 2. Obtain a suitable .NET build toolchain

You also need to have a working installation of .NET 7 including the `browser-wasm` runtime pack. For example, obtain the [.NET SDK daily build](https://github.com/dotnet/installer/blob/main/README.md#installers-and-binaries) (`main` branch), and ensure the `browser-wasm` pack is installed:

```
dotnet workload install wasm-tools -s https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet7/nuget/v3/index.json
```

To make this available to the build scripts, supply environment variables. Example:

```
export DOTNET_ROOT=~/dotnet7
export BROWSER_WASM_RUNTIME_PATH=$(DOTNET_ROOT)/packs/Microsoft.NETCore.App.Runtime.Mono.browser-wasm/7.0.0-alpha.1.22061.11/runtimes/browser-wasm
```

You'll need to update these paths to match the location where you extracted the .NET daily SDK build and the exact version of the `browser-wasm` pack you received.

### 3. Run it

Finally, you can build and run the sample:

```
cd samples/console
make run
```
