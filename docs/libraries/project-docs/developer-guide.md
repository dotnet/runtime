Developer Guide
===============

The repo can be built for the following platforms, using the provided setup and the following instructions.

| Chip  | Windows | Linux | OS X | FreeBSD |
| :---- | :-----: | :---: | :--: | :--: |
| x64   | &#x25CF;| &#x25D2;| &#x25D2;| &#x25D2;|
| x86   | &#x25EF;| &#x25EF;| &#x25EF;| &#x25EF;|
| ARM32 | &#x25EF;| &#x25EF;| &#x25EF;| &#x25EF;|
|       | [Instructions](../building/windows-instructions.md) | [Instructions](../building/unix-instructions.md) | [Instructions](../building/unix-instructions.md) | [Instructions](../building/unix-instructions.md) |


Building the repository
=======================

The CoreFX repo can be built from a regular, non-admin command prompt. The build produces multiple binaries that make up the CoreFX libraries and the accompanying tests.

Developer Workflow
------------------
The dev workflow describes the [development process](https://github.com/dotnet/buildtools/blob/master/Documentation/Dev-workflow.md) to follow. It is divided into specific tasks that are fast, transparent and easy to understand.
The tasks are represented in scripts (cmd/sh) in the root of the repo:
* clean - Cleans up the binary output and optionally the working directory (`-all`)
* sync - Pulls down external dependencies needed to build (i.e. build tools, xunit, coreclr, etc)
* build - Builds the shipping libraries in corefx.
* build-tests - Builds and runs the corefx tests.

For more information about the different options that each task has, use the argument `-?` when calling the script.  For example:
```
build -?
```

###Build
The CoreFX build has two logical components, the native build which produces the "shims" (which provide a stable interface between the OS and managed code) and
the managed build which produces the MSIL code and nuget packages that make up CoreFX.

Calling the script `build` attempts to build both the native and managed code.
Only use it when the parameters that you are passing to the script apply for both components. Otherwise, use the scripts `build-native` and `build-managed` respectively.

The build configurations are generally defaulted based on where you are building (i.e. which OS or which architecture) but we have a few shortcuts for the individual properties that can be passed to the build scripts:

- `-framework` identifies the target framework for the build. It defaults to `netcoreapp` but possible values include `netcoreapp`, `netfx` or `uap`. (msbuild property `TargetGroup`)
- `-os` identifies the OS for the build. It defaults to the OS you are running on but possible values include `Windows_NT`, `Unix`, `Linux`, or `OSX`. (msbuild property `OSGroup`)
- `-debug|-release` controls the optimization level the compilers use for the build. It defaults to `Debug`. (msbuild property `ConfigurationGroup`)
- `-buildArch` identifies the architecture for the build. It defaults to `x64` but possible values include `x64`, `x86`, `arm`, or `arm64`. (msbuild property `ArchGroup`)

These options are common for build, build-managed, build-native, and build-tests scripts.

For more details on the build configurations see [project-guidelines](../coding-guidelines/project-guidelines.md#build-pivots).

**Note**: Before working on individual projects or test projects you **must** run `build` from the root once before beginning that work. It is also a good idea to run `build` whenever you pull a large set of unknown changes into your branch.

**Common full clean build and test run**
```
clean --all
build
build-tests
```

**Examples**

- Building in debug mode for platform x64
```
build -debug -buildArch=x64
```

- Building for different target frameworks
```
build -framework netcoreapp
build -framework netfx
build -framework uap
```

###Build Native
The native build produces shims over libc, openssl, gssapi, libcurl and libz.
The build system uses CMake (2.8.12 or higher) to generate Makefiles using clang (3.5 or higher).
The build also uses git for generating some version information.

The native component should be buildable on any system.

**Examples**

- Building in debug mode for platform x64
```
build-native -debug -buildArch=x64
```

- The following example shows the argument `--`. Everything that is after it is not going to be processed, and will be passed as-is.
```
build-native -debug -buildArch=arm -- cross verbose
```

For more information about extra parameters take a look at the scripts `build-native` under src/Native.

###Build Managed
Since the managed build uses the .NET Core CLI (which the build will download), managed components can only be built on a subset of distros.
There are some additional prerequisites from the CLI which need to be installed. Both libicu and
libunwind are used by CoreCLR to execute managed code, so they must be
installed. Since CoreFX does not actually link against these packages, runtime
versions are sufficient.  We also require curl to be present, which we use to
download the .NET Core CLI.

**Examples**

- Building in debug mode for platform x64
```
build-managed -debug -buildArch=x64
```

- Building in debug mode for platform x64 targeting OS Linux
```
build-managed -debug -buildArch=x64 -os=Linux
```

###Build And Run Tests
To build the tests and run them you can call the build-test script. The same parameters you pass to build should also be passed to build-tests script to ensure you are building and running the tests on the same configuration you have build the product on. However to run tests on the same machine you need to ensure the machine supports the configuration you are building.

**Examples**
- The following shows how to build tests but not run them
```
build-tests -skiptests
```

- The following builds and runs all tests for netcoreapp in release configuration.
```
build-tests -release -framework netcoreapp
```

- The following example shows the argument `--`. Everything that is after it is not going to be processed and it is going to be passed as it is.
Use it to pass extra msbuild properties, in this case to ignore tests ignored in CI.
```
build-tests -- /p:WithoutCategories=IgnoreForCI
```

### Building individual CoreFx DLLs

**Note**: Before working on individual projects or test projects you **must** run `build` from the root once before beginning that work. It is also a good idea to run `build` whenever you pull a large set of unknown changes into your branch.

Under the src directory is a set of directories, each of which represents a particular assembly in CoreFX. See Libary Project Guidelines section under [project-guidelines](../coding-guidelines/project-guidelines.md) for more details about the structure.

For example the src\System.Diagnostics.DiagnosticSource directory holds the source code for the System.Diagnostics.DiagnosticSource.dll assembly.

You can build the DLL for System.Diagnostics.DiagnosticSource.dll by going to the `src\System.Diagnostics.DiagnosticsSource\src` directory and typing `msbuild`. The DLL ends up in `bin\AnyOS.AnyCPU.Debug\System.Diagnostics.DiagnosticSource` as well as `bin\runtime\[BuildConfiguration]`.

You can build the tests for System.Diagnostics.DiagnosticSource.dll by going to
`src\System.Diagnostics.DiagnosticSource\tests` and typing `msbuild`.

Some libraries might also have a ref and/or a pkg directory and you can build them in a similar way by typing `msbuild` in that directory.

For libraries that have multiple build configurations the configurations will be listed in the `<BuildConfigurations>` property group, commonly found in a configurations.props file next to the csproj. When building the csproj for a configuration the most compatible one in the list will be choosen and set for the build. For more information about `BuildConfigurations` see [project-guidelines](../coding-guidelines/project-guidelines.md).

**Examples**

- Build project for Linux for netcoreapp
`msbuild System.Net.NetworkInformation.csproj /p:OSGroup=Linux`

- Build project for uap (not if trying to build on non-windows you also need to specify OSGroup=Windows_NT)
`msbuild System.Net.NetworkInformation.csproj /p:TargetGroup=uap`

- Build release version of library
`msbuild System.Net.NetworkInformation.csproj /p:ConfigurationGroup=Release`

**Note:** If building in a non-Windows environment, call `<repo-root>/Tools/msbuild.sh` instead of just `msbuild`.

### Building all for other OSes

By default, building from the root will only build the libraries for the OS you are running on. One can
build for another OS by specifying `build-managed -os=[value]`.

Note that you cannot generally build native components for another OS but you can for managed components so if you need to do that you can do it at the individual project level or build all via build-managed.

### Building in Release or Debug

By default, building from the root or within a project will build the libraries in Debug mode.
One can build in Debug or Release mode from the root by doing `build -release` or `build -debug` or when building a project by specifying `/p:ConfigurationGroup=[Debug|Release]` after the `msbuild` command.

### Building other Architectures

One can build 32- or 64-bit binaries or for any architecture by specifying in the root `build -buildArch=[value]` or in a project `/p:ArchGroup=[value]` after the `msbuild` command.

### Tests

We use the OSS testing framework [xunit](http://xunit.github.io/) with the [BuildTools test targets](https://github.com/dotnet/buildtools/blob/master/Documentation/test-targets-usage.md).

#### Running tests on the command line

By default, the core tests are run as part of `build.cmd` or `build.sh`. If the product binaries are already available, you could do `build-tests` which will build and run the tests.

For more information about cross-platform testing, please take a look [here](https://github.com/dotnet/corefx/blob/master/Documentation/building/cross-platform-testing.md).

If you are interested in building and running the tests only for a specific library, then there are two different ways to do it:

The easiest (and recommended) way to do it, is by simply building the test .csproj file for that library.

```cmd
cd src\System.Collections.Immutable\tests
msbuild /t:BuildAndTest   ::or /t:Test to just run the tests if the binaries are already built
msbuild /t:RebuildAndTest ::this will cause a test project to rebuild and then run tests
```

It is possible to pass parameters to the underlying xunit runner via the `XunitOptions` parameter, e.g.:
```cmd
msbuild /t:Test "/p:XunitOptions=-class Test.ClassUnderTests"
```
**Note:** If building in a non-Windows environment, call `./Tools/msbuild.sh` instead of just `msbuild`.

There may be multiple projects in some directories so you may need to specify the path to a specific test project to get it to build and run the tests.

Tests participate in the incremental build.  This means that if tests have already been run, and inputs to the incremental build have not changed, rerunning the tests target will not execute the test runner again.  To force re-executing tests in this situation, use `/p:ForceRunTests=true`.

#### Running tests in a different target framework

Each test project can potentially have multiple build configurations. There are some tests that might be OS-specific, or might be testing an API that is available only on some target frameworks, so the `BuildConfigurations` property specifies the valid configurations. By default we will build and run only the default build configuration which is `netcoreapp`. The rest of the configurations will need to be built and ran by specifying the configuration options.

```cmd
cd src\System.Runtime\tests
msbuild System.Runtime.Tests.csproj /p:TargetGroup=netfx
```

#### Filtering tests using traits

The tests can also be filtered based on xunit trait attributes defined in [`xunit.netcore.extensions`](https://github.com/dotnet/buildtools/tree/master/src/xunit.netcore.extensions). These attributes are specified above the test method's definition. The available attributes are:

_**`OuterLoop`:**_
Tests marked as `Outerloop` are for scenarios that don't need to run every build. They may take longer than normal tests, cover seldom hit code paths, or require special setup or resources to execute. These tests are excluded by default when testing through msbuild but can be enabled manually by adding the  `Outerloop` property e.g.

```cmd
build-managed -Outerloop
```

To run <b>only</b> the Outerloop tests, use the following command:
```cmd
msbuild <csproj_file> /t:BuildAndTest /p:WithCategories=OuterLoop
```

_**`PlatformSpecific(TestPlatforms platforms)`:**_
Use this attribute on test methods to specify that this test may only be run on the specified platforms. This attribute returns the following categories based on platform
- `nonwindowstests` for tests that don't run on Windows
- `nonlinuxtests` for tests that don't run on Linux
- `nonosxtests` for tests that don't run on OS X

When running tests by building a test project, tests that don't apply to the `OSGroup` are not run. For example, to run Linux-specific tests on a Linux box, use the following command line:
```sh
<repo-root>/Tools/msbuild.sh <csproj_file> /t:BuildAndTest /p:OSGroup=Linux
```

_**`ActiveIssue(int issue, TestPlatforms platforms)`:**_
Use this attribute over test methods to skip failing tests only on the specific platforms; if no platforms are specified, then the test is skipped on all platforms. This attribute returns the 'failing' category, which is disabled by default.

To run all Linux-compatible tests that are failing:
```sh
<repo-root>/Tools/msbuild.sh <csproj_file> /t:BuildAndTest /p:OSGroup=Linux /p:WithCategories=failing
```

_**A few common examples with the above attributes:**_

- Run all tests acceptable on Windows that are not failing:
```cmd
msbuild <csproj_file> /t:BuildAndTest /p:OSGroup=Windows_NT
```
- Run all outer loop tests acceptable on OS X that are currently associated with active issues:
```sh
<repo-root>/Tools/msbuild.sh <csproj_file> /t:BuildAndTest /p:OSGroup=OSX /p:WithCategories="OuterLoop;failing""
```

Alternatively, you can directly invoke the XUnit executable by changing your working directory to the test execution directory at `bin\tests\{OSPlatformConfig)\{Project}.Tests\{TargetGroup}.{TestTFM}\` which is created when the test project is built.  For example, the following command runs all Linux-supported inner-loop tests:
```sh
./corerun xunit.console.netcore.exe <test_dll_file> -notrait category=nonlinuxtests -notrait category=OuterLoop
```

### Code Coverage

Code coverage is built into the corefx build system.  It utilizes OpenCover for generating coverage data and ReportGenerator for generating reports about that data.  To run:

```cmd
:: Run full coverage
build-tests -Coverage

:: To run a single project with code coverage enabled pass the /p:Coverage=true property
cd src\System.Collections.Immutable\tests
msbuild /t:BuildAndTest /p:Coverage=true
```
If coverage succeeds, the code coverage report will be generated automatically and placed in the bin\tests\coverage directory.  You can view the full report by opening index.htm

Code coverage reports from the continuous integration system are available from the links on the front page of the corefx repo.

### Building tests with .NET Native (Windows only)

.NET Native is a technology that allows compiling IL applications down into a native executable and minimal set of native DLLs, containing all needed functionality from the .NET Framework in native format.  For CoreFX tests, .NET Native support in CoreFX is relatively early, but supported.

```cmd
:: To run a single project with the .NET Native toolchain, set the appropriate build flags:
cd src\Microsoft.CSharp\tests
::TODO: The exact properties needed for .NET Native tests runs after engineering work is TBD
msbuild /t:BuildAndTest /p:TargetGroup=uap /p:UseDotNetNativeToolchain=true
```
If native compilation succeeds, the test will build and run as a native executable named "xunit.console.netcore.exe" in a folder named "native" in the test execution folder.  Note many tests in CoreFX are not ready to run though native compilation yet.

A slight variation on these arguments will allow you to build and run against `uap`, the managed version of the UWP Framework subset, used when debugging UWP applications in Visual Studio:
```cmd
:: To run a single project with the .NET Native toolchain, set the appropriate build flags:
cd src\Microsoft.CSharp\tests
msbuild /t:BuildAndTest /p:TargetGroup=uap
```
In this case, your test will get executed within the context of a wrapper UWP application, targeting the Managed uap as opposed to the .NET Native version.

The CoreFX build and test suite is a work in progress, as are the [building and testing instructions](../README.md). The .NET Core team and the community are improving Linux and OS X support on a daily basis and are adding more tests for all platforms. See [CoreFX Issues](https://github.com/dotnet/corefx/issues) to find out about specific work items or report issues.
