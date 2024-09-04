# Building CoreCLR Guide

- [The Basics](#the-basics)
  - [Build Results](#build-results)
  - [What to do with the Build](#what-to-do-with-the-build)
    - [The Core_Root for Testing Your Build](#the-core-root-for-testing-your-build)
    - [The Dev Shipping Packs](#the-dev-shipping-packs)
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

CoreCLR is one of the most important components of the runtime repo, as it is one of the main engines of the .NET product. That said, while you can test and use it on its own, it becomes easiest to do this when used in conjuction with the libraries subset. When you build both subsets, you can get access to the *Core_Root*. This includes all the libraries, as well as the clr alongside other tools like *Crossgen2*, *R2RDump*, and the *ILC* compiler, and the main command-line host executable `corerun`. The *Core_Root* is one of the most reliable ways of testing changes to the runtime, running external apps with your build, and it is the way clr tests are run in the CI pipelines.

#### The Core Root for Testing Your Build

As described in the [workflow README](/docs/workflow/README.md#building-the-repo), you can build multiple subsets by concatenating them with a `+` sign in the `-subset` argument. So, in this case, it would be `clr+libs`. Usually, the recommended workflow is to build the clr in *Debug* configuration and the libraries in *Release*:

```bash
./build.sh -subset clr+libs -runtimeConfiguration Debug -librariesConfiguration Release
```

Once you have both subsets built, you can generate the *Core_Root*, which as mentioned above, is the most flexible way of testing your changes. You can generate the *Core_Root* by running the following command, assuming a *Checked* clr build on an x64 machine:

```bash
./src/tests/build.sh -x64 -checked -generatelayoutonly
```

Since this is more related to testing, you can find the full details and instructions in the CoreCLR testing doc [over here](/docs/workflow/testing/coreclr/testing.md).

#### The Dev Shipping Packs

<!-- TODO: Link to the "using your build with the sdk" and "using your build with the shipping packages" docs, and rephrase accordingly, if needed. -->
It is also possible to generate the full runtime NuGet packages and installer that you can use to test in a more production-esque scenario. To generate these shipping artifacts, you have to build the `clr`, `libs`, `host`, and `packs` subsets:

```bash
./build.sh -subset clr+libs+host+packs -configuration Release
```

The shipping artifacts are placed in the `artifacts/packages/<Configuration>/Shipping` directory. Here, you will find several NuGet packages, as well as their respective symbols packages, generated from your build. More importantly, you will find a zipped archive with the full contents of the runtime, organized in the same layout as they are in the official dotnet installations. This archive includes the following files:

- `host/fxr/<net-version>-dev/hostfxr` (`hostfxr` is named differently depending on the platform: `hostfxr.dll` on Windows, `libhostfxr.dylib` on macOS, and `libhostfxr.so` on Linux)
- `shared/Microsoft.NETCore.App/<net-version>-dev/*` (The `*` here refers to all the libraries dll's, as well as all the binaries necessary for the runtime to function)
- `dotnet (dotnet.exe on Windows)` (The main `dotnet` executable you usually use to run your apps)

Note that this package only includes the runtime, therefore you will only be able to run apps but not build them. For that, you would need the full SDK.

**NOTE:** On Windows, this will also include `.exe` and `.msi` installers, which you can use in case you want to test your build machine-wide. This is the closest you can get to an official build installation.

### Cross Compilation

Using an x64 machine, it is possible to generate builds for other architectures. Not all architectures are supported for cross-compilation however, and it's also dependant on the OS you are using to build and target. Refer to the table below for the compatibility matrix.

| Operating System | To x86   | To Arm32 | To Arm64 |
| :--------------: | :------: | :------: | :------: |
| Windows          | &#x2714; | &#x2714; | &#x2714; |
| macOS            | &#x2718; | &#x2718; | &#x2714; |
| Linux            | &#x2718; | &#x2714; | &#x2714; |

**NOTE:** On macOS, it is also possible to cross-compile from ARM64 to x64 using an Apple Silicon Mac.

<!-- TODO: Review the Cross-Building doc -->
Detailed instructions on how to do cross-compilation can be found in the cross-building doc [over here](/docs/workflow/building/cross-building.md).

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

Currently, the runtime repo supports building CoreCLR directly on Windows ARM64 without the need to cross-compile, albeit it is still in an experimental phase. To do this, you need to install the ARM64 build tools and Windows SDK for Visual Studio, in addition to all the requirements outlined in the [Windows Requirements doc](/docs/workflow/requirements/windows-requirements.md).

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
