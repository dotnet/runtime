# Building Mono

## Build Requirements

| Windows  | Linux    | macOS    | FreeBSD  |
| :------: | :------: | :------: | :------: |
| [Requirements](../../requirements/windows-requirements.md) | [Requirements](../../requirements/linux-requirements.md) | [Requirements](../../requirements/macos-requirements.md) |

Before proceeding further, please click on the link above that matches your machine and ensure you have installed all the prerequisites for the build to work.

## Concept

To build a complete runtime environment, you need to build both the Mono runtime and libraries.  At the repo root, simply execute:

```bash
./build.sh mono+libs
```
or on Windows,
```cmd
build.cmd mono+libs
```
Note that the debug configuration is the default option. It generates a 'debug' output and that includes asserts, fewer code optimizations, and is easier for debugging. If you want to make performance measurements, or just want tests to execute more quickly, you can also build the 'release' version which does not have these checks by adding the flag `-configuration release` (or `-c release`).


Once you've built the complete runtime and assuming you want to work with just mono, you want to use the following command:

```bash
./build.sh mono
```
or on Windows,
```cmd
build.cmd mono
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

See the instructions for [Building WebAssembly](../../building/libraries/webassembly-instructions.md).

### Android

See the instructions for [Testing Android](../../testing/libraries/testing-android.md)

### iOS

See the instructions for [Testing iOS](../../testing/libraries/testing-apple.md)

## Packages

To generate nuget packages:

```bash
./build.sh mono -pack (with optional release configuration)
```
or on Windows,
```cmd
build.cmd mono -pack (with optional release configuration)
```

The following packages will be created under `artifacts\packages\<configuration>\Shipping`:

- `Microsoft.NETCore.Runtime.Mono.<version>-dev.<number>.1.nupkg`
- `runtime.<OS>.Microsoft.NETCore.Runtime.Mono.<version>-dev.<number>.1.nupkg`
- `transport.Microsoft.NETCore.Runtime.Mono.<version>-dev.<number>.1.nupkg`
- `transport.runtime.<OS>.Microsoft.NETCore.Runtime.Mono.<version>-dev.<number>.1.nupkg`

## To get started with "Hello World"

Try the sample at `src/mono/sample/HelloWorld`.
To run this sample, from the above folder
```cd ../..
make run-sample
```

## Important Notes

Test binaries are not yet available for mono.

The build places logs in `artifacts\log` and these are useful when the build fails.

The build places all of its output in the `artifacts\obj\mono` directory, so if you remove that directory you can force a
full rebuild.
