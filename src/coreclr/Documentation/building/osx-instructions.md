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
git clone https://github.com/dotnet/coreclr
# Cloning into 'coreclr'...
```

This guide assumes that you've cloned the coreclr and corefx repositories into `~/git/coreclr` and `~/git/corefx` on your OS X machine. If your setup is different, you'll need to pay careful attention to the commands you run. In this guide, I'll always show what directory I'm in.

CMake
-----

CoreCLR has a dependency on CMake for the build. You can download it from [CMake downloads](http://www.cmake.org/download/).

Alternatively, you can install CMake from [Homebrew](http://brew.sh/).

```sh
brew install cmake
```

ICU
---
ICU (International Components for Unicode) is also required to build and run. It can be obtained via [Homebrew](http://brew.sh/).

```sh
brew install icu4c
brew link --force icu4c
```

Build the Runtime and Microsoft Core Library
============================================

To Build CoreCLR, run build.sh from the root of the coreclr repo.

```sh
./build.sh
```

After the build has completed, there should some files placed in `bin/Product/OSX.x64.Debug`. The ones we are interested in are:

- `corerun`: The command line host. This program loads and starts the CoreCLR runtime and passes the managed program you want to run to it.
- `libcoreclr.dylib`: The CoreCLR runtime itself.
- `System.Private.CoreLib.dll`: Microsoft Core Library.

Create the Core_Root
===================

The Core_Root folder will have the built binaries, from `build.sh` and it will also include the CoreFX packages required to run tests.

```
./build-test.sh generatelayoutonly
```

After the build is complete you will be able to find the output in the `bin/tests/OSX.x64.Debug/Tests/Core_Root` folder.

Running a single test
===================

After `build-test.sh` is run, corerun from the Core_Root folder is ready to be run. This can be done by using the full absolute path to corerun, or by setting
an environment variable to the Core_Root folder.

```sh
export CORE_ROOT=/path/to/coreclr/bin/tests/OSX.x64.Debug/Tests/Core_Root
$CORE_ROOT/corerun hello_world.dll
```