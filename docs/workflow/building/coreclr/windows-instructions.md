# Build CoreCLR on Windows

* [Environment](#environment)
* [Build the Runtime](#build-the-runtime)
  * [Cross-Compilation](#cross-compilation)
* [Core_Root](#core_root)
* [Native ARM64 (Experimental)](#native-arm64-experimental)

This guide will walk you through building CoreCLR on Windows.

## Environment

Ensure you have all of the prerequisites installed from the [Windows Requirements](/docs/workflow/requirements/windows-requirements.md).

## Build the Runtime

To build CoreCLR on Windows, run `build.cmd` while specifying the `clr` subset:

```cmd
.\build.cmd -subset clr <other args>
```

After the build has completed, there should be some files placed in `artifacts/bin/coreclr/windows.<arch>.<configuration>` (for example `artifacts/bin/coreclr/windows.x64.Release`). The most important binaries are the following:

* `corerun.exe`: The command line host. This program loads and starts the CoreCLR runtime and passes the managed program (e.g. `program.dll`) you want to run with it.
* `coreclr.dll`: The CoreCLR runtime itself.
* `System.Private.CoreLib.dll`: The core managed library, containing definitions of `Object` and base functionality.

### Cross-Compilation

It is possible to get Windows x86, ARM32, and ARM64 builds using an x64 machine. Instructions on how to do this are in the [cross-building doc](/docs/workflow/building/coreclr/cross-building.md#windows-cross-building).

## Core_Root

The Core_Root provides one of the main ways to test your build. Full instructions on how to build it in the [CoreCLR testing doc](/docs/workflow/testing/coreclr/testing.md), and we also have a detailed guide on how to use it for your own testing in [its own dedicated doc](/docs/workflow/testing/using-corerun-and-coreroot.md).

## Native ARM64 (Experimental)

Building natively on ARM64 requires you to have installed the appropriate ARM64 build tools and Windows SDK, as specified in the [Windows requirements doc](/docs/workflow/requirements/windows-requirements.md).

Once those requirements are satisfied, you have to specify you are doing an Arm64 build, and explicitly tell the build script you want to use `MSBuild`. `Ninja` is not yet supported on Arm64 platforms.

```cmd
build.cmd -s clr -c Release -arch arm64 -msbuild
```

Since this is still in an experimental phase, the recommended way for building ARM64 is cross-compiling from an x64 machine. Instructions on how to do this can be found at the [cross-building doc](/docs/workflow/building/coreclr/cross-building.md#cross-compiling-for-arm32-and-arm64).

## Native Sanitizers

CoreCLR can be built with native sanitizers like AddressSanitizer to help catch memory safety issues. To build the project with native sanitizers, add the `-fsanitize address` argument to the build script like the following:

```cmd
build.cmd -s clr -fsanitize address
```

When building the repo with any native sanitizers, you should build all native components in the repo with the same set of sanitizers.

The following sanitizers are supported for CoreCLR on Windows:

| Sanitizer Name | Minimum VS Version | `-fsanitize` argument | Support Status |
|----------------|--------------------|-----------------------|----------------|
| AddressSanitizer | not yet released | `address` | experimental |

## Using a custom compiler environment

If you ever need to use a custom compiler environment for the native builds on Windows, you can set the `SkipVCEnvInit` environment variable to `1`. The build system will skip discovering Visual Studio and initializing its development environment when this flag is used. This is only required for very advanced scenarios and should be used rarely.
