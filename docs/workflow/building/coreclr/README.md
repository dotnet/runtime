# Building CoreCLR

* [Introduction](#introduction)
* [Common Building Options](#common-building-options)
  * [Build Drivers](#build-drivers)
  * [Extra Flags](#extra-flags)
  * [Build Results Layout](#build-results-layout)
* [Platform-Specific Instructions](#platform-specific-instructions)
* [Testing CoreCLR](#testing-coreclr)

## Introduction

Here is a brief overview on how to build the common form of CoreCLR in general. For further specific instructions on each platform, we have links to instructions later on in [Platform-Specific Instructions](#platform-specific-instructions).

To build just CoreCLR, use the `subset` flag to the `build.sh` or `build.cmd` script at the repo root. Note that specifying `-subset` explicitly is not necessary if it is the first argument (i.e. `./build.sh --subset clr` and `./build.sh clr` are equivalent). However, if you specify any other argument beforehand, then you must specify the `-subset` flag.

For Linux and macOS:

```bash
./build.sh --subset clr
```

For Windows:

```cmd
.\build.cmd -subset clr
```

## Common Building Options

By default, the script generates a _Debug_ build type, which is not optimized code and includes asserts. As its name suggests, this makes it easier and friendlier to debug the code. If you want to make performance measurements, you ought to build the _Release_ version instead, which doesn't have any asserts and has all code optimizations enabled. Likewise, if you plan on running tests, the _Release_ configuration is more suitable since it's considerably faster than the _Debug_ one. For this, you add the flag `-configuration release` (or `-c release`). For example:

```bash
./build.sh --subset clr --configuration release
```

As mentioned before in the [general building document](/docs/workflow/README.md#configurations-and-subsets), CoreCLR also supports a _Checked_ build type which has asserts enabled like _Debug_, but is built with the native compiler optimizer enabled, so it runs much faster. This is the usual mode used for running tests in the CI system.

Now, it is also possible to select a different configuration for each subset when building them together. The `--configuration` flag applies universally to all subsets, but it can be overridden with any one or more of the following ones:

* `--runtimeConfiguration (-rc)`: Flag for the CLR build configuration.
* `--librariesConfiguration (-lc)`: Flag for the libraries build configuration.
* `--hostConfiguration (-hc)`: Flag for the host build configuration.

For example, a very common scenario used by developers and the repo's test scripts with default options, is to build the _clr_ in _Debug_ mode, and the _libraries_ in _Release_ mode. To achieve this, the command-line would look like the following:

```bash
./build.sh --subset clr+libs --configuration Release --runtimeConfiguration Debug
```

Or alternatively:

```bash
./build.sh --subset clr+libs --librariesConfiguration Release --runtimeConfiguration Debug
```

For more information about all the different options available, supply the argument `-help|-h` when invoking the build script. On Unix-like systems, non-abbreviated arguments can be passed in with a single `-` or double hyphen `--`.

### Build Drivers

If you want to use _Ninja_ to drive the native build instead of _Make_ on non-Windows platforms, you can pass the `-ninja` flag to the build script as follows:

```bash
./build.sh --subset clr --ninja
```

If you want to use Visual Studio's _MSBuild_ to drive the native build on Windows, you can pass the `-msbuild` flag to the build script similarly to the `-ninja` flag.

We recommend using _Ninja_ for building the project on Windows since it more efficiently uses the build machine's resources for the native runtime build in comparison to Visual Studio's _MSBuild_.

### Extra Flags

To pass extra compiler/linker flags to the coreclr build, set the environment variables `EXTRA_CFLAGS`, `EXTRA_CXXFLAGS` and `EXTRA_LDFLAGS` as needed. Don't set `CFLAGS`/`CXXFLAGS`/`LDFLAGS` directly as that might lead to configure-time tests failing.

### Build Results Layout

Once the build has concluded, it will have produced its output artifacts in the following structure:

* Product binaries will be dropped in `artifacts\bin\coreclr\<OS>.<arch>.<configuration>` folder.
* A NuGet package, _Microsoft.Dotnet.CoreCLR_, will be created under `artifacts\bin\coreclr\<OS>.<arch>.<configuration>\.nuget` folder.
* Test binaries (if built) will be dropped under `artifacts\tests\coreclr\<OS>.<arch>.<configuration>` folder. However, remember the root build script will not build the tests. The instructions for working with tests (building and running) are [in the testing doc](/docs/workflow/testing/coreclr/testing.md).
* The build places logs in `artifacts\log` and these are useful when the build fails.
* The build places all of its intermediate output in the `artifacts\obj\coreclr` directory.

If you want to force a full rebuild of the subsets you specified when calling the build script, pass the `-rebuild` flag to it, in addition to any other arguments you might require.

## Platform-Specific Instructions

Now that you've got the general idea on how the _CoreCLR_ builds work, here are some further documentation links on platform-specific caveats and features.

* [Build CoreCLR on Windows](windows-instructions.md)
* [Build CoreCLR on macOS](macos-instructions.md)
* [Build CoreCLR on Linux](linux-instructions.md)
* [Build CoreCLR on FreeBSD](freebsd-instructions.md)

We also have specific instructions for building _NativeAOT_ [here](/docs/workflow/building/coreclr/nativeaot.md).

## Testing CoreCLR

For testing your build, the [testing docs](/docs/workflow/testing/coreclr/testing.md) have detailed instructions on how to do it.
