Build CoreCLR on macOS
=====================

This guide will walk you through building CoreCLR on macOS. We'll start by showing how to set up your environment from scratch.

Environment
===========

Ensure you have all of the prerequisites installed from the [macOS Requirements](/docs/workflow/requirements/macos-requirements.md).

Build the Runtime and System.Private.CoreLib
============================================

To Build CoreCLR, run build.sh to build the CoreCLR subset category of the runtime:

```
./build.sh -subset clr
```

After the build has completed, there should some files placed in `artifacts/bin/coreclr/OSX.x64.Debug`. The ones we are interested in are:

- `corerun`: The command line host. This program loads and starts the CoreCLR runtime and passes the managed program you want to run to it.
- `libcoreclr.dylib`: The CoreCLR runtime itself.
- `System.Private.CoreLib.dll`: Microsoft Core Library.

Create the Core_Root
===================

The Core_Root folder will have the built binaries, from `src/coreclr/build.sh` and it will also include the CoreFX packages required to run tests.

```
./src/tests/build.sh generatelayoutonly
```

After the build is complete you will be able to find the output in the `artifacts/tests/coreclr/OSX.x64.Debug/Tests/Core_Root` folder.

Running a single test
===================

After `src/tests/build.sh` is run, corerun from the Core_Root folder is ready to be run. This can be done by using the full absolute path to corerun, or by setting
an environment variable to the Core_Root folder.

```sh
export CORE_ROOT=/path/to/runtime/artifacts/tests/coreclr/OSX.x64.Debug/Tests/Core_Root
$CORE_ROOT/corerun hello_world.dll
```
