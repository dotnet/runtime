# Build CoreCLR on macOS

* [Environment](#environment)
* [Build the Runtime](#build-the-runtime)
  * [Cross-Compilation](#cross-compilation)
* [Create the Core_Root](#create-the-core_root)

This guide will walk you through building CoreCLR on macOS.

## Environment

Ensure you have all of the prerequisites installed from the [macOS Requirements](/docs/workflow/requirements/macos-requirements.md).

## Build the Runtime

To build CoreCLR on macOS, run `build.sh` while specifying the `clr` subset:

```bash
./build.sh --subset clr <other args>
```

After the build has completed, there should be some files placed in `artifacts/bin/coreclr/OSX.<arch>.<configuration>` (for example `artifacts/bin/coreclr/OSX.x64.Release`). The most important binaries are the following:

* `corerun`: The command line host. This program loads and starts the CoreCLR runtime and passes the managed program (e.g. `program.dll`) you want to run with it.
* `libcoreclr.dylib`: The CoreCLR runtime itself.
* `System.Private.CoreLib.dll`: The core managed library, containing definitions of `Object` and base functionality.

### Cross-Compilation

It is possible to get a macOS ARM64 build using an Intel x64 Mac and vice versa, an x64 one using an M1 Mac. Instructions on how to do this are in the [cross-building doc](/docs/workflow/building/coreclr/cross-building.md#macos-cross-building).

## Create the Core_Root

The Core_Root provides one of the main ways to test your build. Full instructions on how to build it in the [CoreCLR testing doc](/docs/workflow/testing/coreclr/testing.md), and we also have a detailed guide on how to use it for your own testing in [its own dedicated doc](/docs/workflow/testing/using-corerun-and-coreroot.md).
