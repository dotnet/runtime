Build CoreCLR on OS X
=====================

This guide will walk you through building CoreCLR on OS X. We'll start by showing how to set up your environment from scratch.

Environment
===========

These instructions were validated on macOS 10.12. Sierra. On older versions coreFX will fail to build properly because of SSL API changes.

If your machine has Command Line Tools for XCode 6.3 installed, you'll need to update them to the 6.3.1 version or higher in order to successfully build. There was an issue with the headers that shipped with version 6.3 that was subsequently fixed in 6.3.1.

Git Setup
---------

Clone the CoreCLR and CoreFX repositories (either upstream or a fork).

```sh
git clone https://github.com/dotnet/runtime
# Cloning into 'runtime'...
```

CMake
-----

CoreCLR has a dependency on CMake for the build. You can install it with [Homebrew](https://brew.sh/).

```sh
brew install cmake
```

ICU
---
ICU (International Components for Unicode) is also required to build and run. It can be obtained via [Homebrew](https://brew.sh/).

```sh
brew install icu4c
```

pkg-config
----------
pkg-config is also required to build. It can be obtained via [Homebrew](https://brew.sh/).

```sh
brew install pkg-config
```

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
./src/coreclr/build-test.sh generatelayoutonly
```

After the build is complete you will be able to find the output in the `artifacts/tests/coreclr/OSX.x64.Debug/Tests/Core_Root` folder.

Running a single test
===================

After `src/coreclr/build-test.sh` is run, corerun from the Core_Root folder is ready to be run. This can be done by using the full absolute path to corerun, or by setting
an environment variable to the Core_Root folder.

```sh
export CORE_ROOT=/path/to/runtime/artifacts/tests/coreclr/OSX.x64.Debug/Tests/Core_Root
$CORE_ROOT/corerun hello_world.dll
```
