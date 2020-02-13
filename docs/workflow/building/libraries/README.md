# Build

## Quick Start

Here is one example of a daily workflow for a developer working mainly on the libraries, in this case using Windows:

```bat
:: From root:
git clean -xdf
git pull upstream master & git push origin master
build -subsetCategory coreclr -c Release
build -subsetCategory libraries -runtimeConfiguration release

:: The above you may only perform once in a day, or when
:: you pull down significant new changes.

:: Switch to working on a given library (RegularExpressions in this case)
cd src\libraries\System.Text.RegularExpressions

:: If you use Visual Studio, you might open System.Text.RegularExpressions.sln here.

:: Change to test directory
cd tests

:: Then inner loop build / test
:: (If using Visual Studio, you might run tests inside it instead)
pushd ..\src & dotnet build & popd & dotnet build /t:buildandtest
```

The instructions for Linux and macOS are essentially the same:

```bash
# From root:
git clean -xdf
git pull upstream master & git push origin master
./build.sh -subsetcategory coreclr -c Release
./build.sh -subsetcategory libraries -runtimeconfiguration Release

# The above you may only perform once in a day, or when
# you pull down significant new changes.

# Switch to working on a given library (RegularExpressions in this case)
cd src/libraries/System.Text.RegularExpressions

# Change to test directory
cd tests

# Then inner loop build / test:
pushd ../src & dotnet build & popd & dotnet build /t:buildandtest
```

The steps above may be all you need to know to make a change. Want more details about what this means? Read on.

## Building everything

This document explains how to work on libraries. In order to work on library projects or run library tests it is necessary to have built the runtime to give the libraries something to run on. You should normally build CoreCLR runtime in release configuration and libraries in debug configuration. If you haven't already done so, please read [this document](../../README.md#Configurations) to understand configurations.

These example commands will build a release CoreCLR (and CoreLib), debug libraries, and debug installer:

For Linux:
```bash
./build.sh -runtimeconfiguration Release
```

For Windows:
```bat
./build.cmd -runtimeconfiguration Release
```

Detailed information about building and testing runtimes and the libraries is in the documents linked below.

### More details if you need them

The above commands will give you libraries in "debug" configuration (the default) using a runtime in "release" configuration which hopefully you built earlier.

The libraries build has two logical components, the native build which produces the "shims" (which provide a stable interface between the OS and managed code) and the managed build which produces the MSIL code and NuGet packages that make up Libraries. The commands above will build both.

The build settings(TargetFramework, OSGroup, Configuration, Architecture) are generally defaulted based on where you are building (i.e. which OS or which architecture) but we have a few shortcuts for the individual properties that can be passed to the build scripts:

- `-framework|-f` identifies the target framework for the build. Possible values include `netcoreapp5.0` (currently the latest .NET Core version) or `net472`. (msbuild property `BuildTargetFramework`)
- `-os` identifies the OS for the build. It defaults to the OS you are running on but possible values include `Windows_NT`, `Unix`, `Linux`, or `OSX`. (msbuild property `OSGroup`)
- `-configuration|-c Debug|Release` controls the optimization level the compilers use for the build. It defaults to `Debug`. (msbuild property `Configuration`)
- `-arch` identifies the architecture for the build. It defaults to `x64` but possible values include `x64`, `x86`, `arm`, or `arm64`. (msbuild property `ArchGroup`)

For more details on the build settings see [project-guidelines](../../../coding-guidelines/project-guidelines.md#build-pivots).

If you invoke the build script without any actions, the default action chain `-restore -build` is executed. You can chain multiple actions together (e.g., `-restore -build -buildtests`) and they will execute in the appropriate order. Note that if you specify actions like `-build` explicitly, you likely need to explicitly add `-restore` as well.

By default build only builds the product libraries and none of the tests. If you want to build the tests you can add the flag `-buildtests`. If you want to run the tests you can add the flag `-test`. To build and run the tests combine both arguments: `-buildtests -test`. To specify just the libraries, use `-subcategory libraries`.

**Examples**
- Building in release mode for platform x64 (restore and build are implicit here as no actions are passed in)
```bash
./build.sh -subsetCategory libraries -c Release -arch x64
```

- Building the src assemblies and build and run tests (running all tests takes a considerable amount of time!)
```bash
./build.sh -subsetCategory libraries -restore -build -buildtests -test
```

- Building for different target frameworks (restore and build are implicit again as no action is passed in)
```bash
./build.sh -subsetCategory libraries -framework netcoreapp5.0
./build.sh -subsetCategory libraries -framework net472
```

- Build only managed components and skip the native build
```bash
./build.sh -subsetCategory libraries /p:BuildNative=false
```

- Clean the entire solution
```bash
./build.sh -subsetCategory libraries -clean
```

For Windows, replace `./build.sh` with `build.cmd`.

### How to building native components only

The libraries build contains some native code. This includes shims over libc, openssl, gssapi, and zlib. The build system uses CMake to generate Makefiles using clang. The build also uses git for generating some version information.

**Examples**

- Building in debug mode for platform x64
```bash
./src/libraries/Native/build-native.sh debug x64
```

- The following example shows how you would do an arm cross-compile build.
```bash
./src/libraries/Native/build-native.sh debug arm cross verbose
```

For Windows, replace `build-native.sh` with `build-native.cmd`.

## Building individual libraries

Similar to building the entire repo with `build.cmd` or `build.sh` in the root you can build projects based on our directory structure by passing in the directory. We also support shortcuts for libraries so you can omit the root src folder from the path. When given a directory we will build all projects that we find recursively under that directory. Some examples may help here.

**Examples**

- Build all projects for a given library (e.g.: System.Collections) including running the tests

```bash
 ./build.sh -subsetCategory libraries src/libraries/System.Collections
```

- Build just the tests for a library project.
```bash
 ./build.sh -subsetCategory libraries src/libraries/System.Collections/tests
```

- All the options listed above like framework and configuration are also supported (note they must be after the directory)
```bash
 ./build.sh -subsetCategory libraries System.Collections -f net472 -c Release
```

As `dotnet build` works on both Unix and Windows and calls the restore target implicitly, we will use it throughout this guide.

Under the src directory is a set of directories, each of which represents a particular assembly in Libraries. See Library Project Guidelines section under [project-guidelines](../../../coding-guidelines/project-guidelines.md) for more details about the structure.

For example the src\libraries\System.Diagnostics.DiagnosticSource directory holds the source code for the System.Diagnostics.DiagnosticSource.dll assembly.

You can build the DLL for System.Diagnostics.DiagnosticSource.dll by going to the `src\libraries\System.Diagnostics.DiagnosticsSource\src` directory and typing `dotnet build`. The DLL ends up in `artifacts\bin\AnyOS.AnyCPU.Debug\System.Diagnostics.DiagnosticSource` as well as `artifacts\bin\runtime\[$(BuildTargetFramework)-$(OSGroup)-$(Configuration)-$(ArchGroup)]`.

You can build the tests for System.Diagnostics.DiagnosticSource.dll by going to
`src\libraries\System.Diagnostics.DiagnosticSource\tests` and typing `dotnet build`.

Some libraries might also have a ref and/or a pkg directory and you can build them in a similar way by typing `dotnet build` in that directory.

For libraries that have multiple target frameworks the target frameworks will be listed in the `<TargetFrameworks>` property group. When building the csproj for a BuildTargetFramework the most compatible target framework in the list will be chosen and set for the build. For more information about `TargetFrameworks` see [project-guidelines](../../../coding-guidelines/project-guidelines.md).

**Examples**

- Build project for Linux for .NET Core
```
dotnet build System.Net.NetworkInformation.csproj /p:OSGroup=Linux
```

- Build release version of library
```
dotnet build -c Release System.Net.NetworkInformation.csproj
```

### Building all for other OSes

By default, building from the root will only build the libraries for the OS you are running on. One can
build for another OS by specifying `./build.sh -subsetCategory libraries -os [value]`.

Note that you cannot generally build native components for another OS but you can for managed components so if you need to do that you can do it at the individual project level or build all via passing `/p:BuildNative=false`.

### Building in Release or Debug

By default, building from the root or within a project will build the libraries in Debug mode.
One can build in Debug or Release mode from the root by doing `./build.sh -subsetCategory libraries  -c Release` or `./build.sh -subsetCategory libraries -c Debug`.

### Building other Architectures

One can build 32- or 64-bit binaries or for any architecture by specifying in the root `./build.sh -subsetCategory libraries -arch [value]` or in a project `/p:ArchGroup=[value]` after the `dotnet build` command.

## Working in Visual Studio

If you are working on Windows, and use Visual Studio, you can open individual libraries projects into it. From within Visual Studio you can then build, debug, and run tests.

## Running tests

For more details about running tests inside Visual Studio, [go here](../../testing/libraries/testing-vs.md)

For more about running tests, read the [running tests](../../testing/libraries/testing.md) document.
