# Building Mono

## Build Requirements

| Windows  | Linux    | macOS    | FreeBSD  |
| :------: | :------: | :------: | :------: |
| [Requirements](../../requirements/windows-requirements.md) | [Requirements](../../requirements/linux-requirements.md) | [Requirements](../../requirements/macos-requirements.md) |

Before proceeding further, please click on the link above that matches your machine and ensure you have installed all the prerequisites for the build to work.

## Concept

To build a complete runtime environment, you need to build both the Mono runtime and libraries.  At the repo root, simply execute:

```bash
./build.sh --subset mono+libs
```
or on Windows,
```bat
build.cmd -subset mono+libs
```
Note that the debug configuration is the default option. It generates a 'debug' output and that includes asserts, fewer code optimizations, and is easier for debugging. If you want to make performance measurements, or just want tests to execute more quickly, you can also build the 'release' version which does not have these checks by adding the flag `-configuration release` (or `-c release`).


Once you've built the complete runtime and assuming you want to work with just mono, you want to use the following command:

```bash
./build.sh --subset mono
```
or on Windows,
```bat
build.cmd -subset mono
```
When the build completes, product binaries will be dropped in the `artifacts\bin\mono\<OS>.<arch>.<flavor>` folder.

### Useful Build Arguments
Here are a list of build arguments that may be of use:

`/p:MonoEnableLlvm=true` - Builds mono w/ LLVM

`/p:MonoEnableLlvm=true /p:MonoLLVMDir=path/to/llvm` - Builds mono w/ LLVM from a custom path

`/p:MonoEnableLlvm=true /p:MonoLLVMDir=path/to/llvm /p:MonoLLVMUseCxx11Abi=true` - Builds mono w/ LLVM
from a custom path (and that LLVM was built with C++11 ABI)

For `build.sh`

`/p:DisableCrossgen=true` - Skips building the installer if you don't need it (builds faster)

The build has a number of options that you can learn about using build -?.

### WebAssembly

In addition to the normal build requirements, WebAssembly builds require a local emsdk to be downloaded. This can either be external or acquired via a make target.

To acquire it externally, move to a directory outside of the runtime repository and run:
```bash
git clone https://github.com/emscripten-core/emsdk.git
```

To use the make target, from the root of the runtime repo:
```bash
cd src/mono/wasm
make provision-wasm
cd ../../..
```

When building for WebAssembly, regardless of the machine architecture, you must set the `EMSDK_PATH` environmental variable and architecture/os, calling build.sh like so:
```bash
EMSDK_PATH={path to emsdk repo} ./build.sh --subset mono+libs --arch wasm --os browser -c release
```

If using the locally provisioned emsdk, this will be:
```bash
EMSDK_PATH={path to runtime repo}/src/mono/wasm/emsdk ./build.sh --subset mono+libs --arch wasm --os browser -c release
```

Artifacts will be placed in `artifacts/bin/microsoft.netcore.app.runtime.browser-wasm/Release/`. When rebuilding with `build.sh`, you _must_ rebuild with `mono+libs` even for mono-only changes, or this directory will not be updated. Alternative, you can rebuild just the runtime-specific bits from the `src/mono/wasm` directory by running either `make runtime` or `make corlib` when modifying Mono or System.Private.CoreLib respectively.

## Packages

To generate nuget packages:

```bash
./build.sh --subset mono -pack (with optional release configuration)
```
or on Windows,
```bat
build.cmd -subset mono -pack (with optional release configuration)
```

The following packages will be created under `artifacts\packages\<configuration>\Shipping`:

- `Microsoft.NETCore.Runtime.Mono.<version>-dev.<number>.1.nupkg`
- `runtime.<OS>.Microsoft.NETCore.Runtime.Mono.<version>-dev.<number>.1.nupkg`
- `transport.Microsoft.NETCore.Runtime.Mono.<version>-dev.<number>.1.nupkg`
- `transport.runtime.<OS>.Microsoft.NETCore.Runtime.Mono.<version>-dev.<number>.1.nupkg`

## Important Notes

Test binaries are not yet available for mono.

The build places logs in `artifacts\log` and these are useful when the build fails.

The build places all of its output in the `artifacts\obj\mono` directory, so if you remove that directory you can force a
full rebuild.
