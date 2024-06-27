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

After the build has completed, there should be some files placed in `artifacts/bin/coreclr/osx.<arch>.<configuration>` (for example `artifacts/bin/coreclr/osx.x64.Release`). The most important binaries are the following:

* `corerun`: The command line host. This program loads and starts the CoreCLR runtime and passes the managed program (e.g. `program.dll`) you want to run with it.
* `libcoreclr.dylib`: The CoreCLR runtime itself.
* `System.Private.CoreLib.dll`: The core managed library, containing definitions of `Object` and base functionality.

### Cross-Compilation

It is possible to get a macOS ARM64 build using an Intel x64 Mac and vice versa, an x64 one using an M1 Mac. Instructions on how to do this are in the [cross-building doc](/docs/workflow/building/coreclr/cross-building.md#macos-cross-building).

## Create the Core_Root

The Core_Root provides one of the main ways to test your build. Full instructions on how to build it in the [CoreCLR testing doc](/docs/workflow/testing/coreclr/testing.md), and we also have a detailed guide on how to use it for your own testing in [its own dedicated doc](/docs/workflow/testing/using-corerun-and-coreroot.md).

## Debugging information

The build process puts native component symbol and debugging information into `.dwarf` files, one for each built binary. This is not the native format used by macOS, and debuggers like LLDB can't automatically find them. The native format used by macOS is `.dSYM` bundles. To build `.dSYM` bundles and get a better inner-loop developer experience on macOS (e.g., have the LLDB debugger automatically find program symbols and display source code lines, etc.), build as follows:

```bash
./build.sh --subset clr --cmakeargs "-DCLR_CMAKE_APPLE_DSYM=TRUE"
```

(Note: converting the entire build process to build and package `.dSYM` bundles on macOS by default is tracked by [this](https://github.com/dotnet/runtime/issues/92911) issue.)

## Native Sanitizers

CoreCLR can be built with native sanitizers like AddressSanitizer to help catch memory safety issues. To build the project with native sanitizers, add the `-fsanitize address` argument to the build script like the following:

```bash
build.sh -s clr -fsanitize address
```

When building the repo with any native sanitizers, you should build all native components in the repo with the same set of sanitizers.

The following sanitizers are supported for CoreCLR on macOS:

| Sanitizer Name  | `-fsanitize` argument | Support Status |
|-----------------|-----------------------|----------------|
| AddressSanitize | `address` | regularly tested on x64 |
