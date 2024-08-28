# Building CoreCLR Guide

- [The Basics](#the-basics)
  - [Build Results](#build-results)
  - [What to do with the Build](#what-to-do-with-the-build)
  - [Cross Compilation](#cross-compilation)
- [Other Features](#other-features)
  - [Build Drivers](#build-drivers)
  - [Extra Flags](#extra-flags)
  - [Native ARM64 Building on Windows](#native-arm64-building-on-windows)
  - [Debugging Information for macOS](#debugging-information-for-macos)
  - [Native Sanitizers](#native-sanitizers)

Firstly, make sure you've prepared your environment and installed all the requirements for your platform. If not, follow this [link](/docs/workflow/README.md#introduction) for the corresponding instructions.

## The Basics

As explained in the main workflow README, you can build the CoreCLR runtime by passing `-subset clr` as argument to the repo's main `build.sh`/`build.cmd` script:

```bash
./build.sh -subset clr <other args go here>
```

By default, the script builds the _clr_ in *Debug* configuration, which doesn't have any optimizations and has all assertions enabled. If you're aiming to run performance benchmarks, make sure you select the *Release* version with `-configuration Release`, as that one generates the most optimized code. On the other hand, if your goal is to run tests, then you can take the most advantage from CoreCLR's exclusive *Checked* configuration. This one retains the assertions but has the native compiler optimizations enabled, thus making it run faster than *Debug*. This is the usual mode used for running tests in the CI pipelines.

### Build Results

Once the `clr` build completes, the main generated artifacts are placed in `artifacts/bin/coreclr/<OS>.<Architecture>.<Configuration>`. For example, for a Linux x64 Release build, the output path would be `artifacts/bin/coreclr/linux.x64.Release`. Here, you will find a number of different binaries, of which the most important are the following:

- `corerun`: The command-line host executable. This program loads and starts the CoreCLR runtime and receives the managed program you want to run as argument (e.g. `./corerun program.dll`). On Windows, it is called `corerun.exe`.
- `coreclr`: The CoreCLR runtime itself. On Windows, it's called `coreclr.dll`, on macOS it is `libcoreclr.dylib`, and on Linux it is `libcoreclr.so`.
- `System.Private.CoreLib.dll`: The core managed library, containing the definitions of `Object` and the base functionality.

All the generated logs are placed in under `artifacts/log`, and all the intermediate output the build uses is placed in the `artifacts/obj/coreclr` directory.

### What to do with the Build

What to do with the Build Under Construction!

### Cross Compilation

Cross Compilation Under Construction!

## Other Features

### Build Drivers

By default, the CoreCLR build uses *Ninja* as the native build driver on Windows, and *Make* on non-Windows platforms. You can override this behavior by passing the appropriate flags to the build script:

To use Visual Studio's *MSBuild* instead of *Ninja* on Windows:

```cmd
./build.cmd -subset clr -msbuild
```

It is recommended to use *Ninja* on Windows, as it uses the build machine's resources more efficiently in comparison to Visual Studio's *MSBuild*.

To use *Ninja* instead of *Make* on non-Windows:

```bash
./build.sh -subset clr -ninja
```

### Extra Flags

You can also pass some extra compiler/linker flags to the CoreCLR build. Set the `EXTRA_CFLAGS`, `EXTRA_CXXFLAGS`, and `EXTRA_LDFLAGS` as you see fit for this purpose. The build script will consume them and then set the environment variables that will ultimately affect your build (i.e. those same ones without the `EXTRA_` prefix). Don't set the final ones directly yourself, as that is known to lead to potential failures in configure-time tests.

### Native ARM64 Building on Windows

Currently, the runtime repo supports building CoreCLR directly on Windows ARM64 without the need to cross-compile, albeit it is still in an experimental phase. To do this, you need to the ARM64 build tools and Windows SDK for Visual Studio, in addition to all the requirements outlined in the [Windows Requirements doc](/docs/workflow/requirements/windows-requirements.md).

Once those requirements are fulfilled, you have to tell the build script to compile for Arm64 using *MSBuild*. *Ninja* is not yet supported on Arm64 platforms:

```cmd
./build.cmd -subset clr -arch arm64 -msbuild
```

While this is functional at the time of writing this doc, it is still recommended to cross-compile from an x64 machine, as that's the most stable and tested method.

### Debugging Information for macOS

When building on macOS, the build process puts native component symbol and debugging information into `.dwarf` files, one for each built binary. This is not the native format used by macOS, and debuggers like LLDB can't automatically find them. The format macOS uses is `.dSYM` bundles. To generate them and get a better inner-loop developer experience (e.g. have the LLDB debugger automatically find program symbols and display source code lines, etc.), make sure to enable the `DLCR_CMAKE_APPLE_DYSM` flag when calling the build script:

```bash
./build.sh -subset clr -cmakeargs "-DLCR_CMAKE_APPLE_DYSM=TRUE"
```

**NOTE:** Converting the entire build process to build and package `.dSYM` bundles on macOS by default is on the table and tracked by issue #92911 [over here](https://github.com/dotnet/runtime/issues/92911).

### Native Sanitizers

CoreCLR is also in the process of supporting the use of native sanitizers during the build to help catch memory safety issues. To apply them, add the `-fsanitize` flag followed by the name of the sanitizer as argument. As of now, these are the supported sanitizers with plans of adding more in the future:

- Sanitizer Name: `AddressSanitizer`

  Argument to `-fsanitize`: `address`

| Platform | Minimum VS Version | Support Status          |
| :------: | :----------------: | :---------------------: |
| Windows  | Not Yet Released   | Experimental            |
| macOS    | N/A                | Regularly Tested on x64 |
| Linux    | N/A                | Regularly Tested on x64 |

And to use it, the command would look as follows:

```bash
./build.sh -subset clr -fsanitize address
```
