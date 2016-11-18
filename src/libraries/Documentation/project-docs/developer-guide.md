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
* Clean
* Sync
* Build
* Run Tests
* Publish packages

For more information about the different options that each task has, use the argument `-?` when calling the script.  For example:
```
sync -?
```

###Build
The CoreFX build has two logical components, the native build which produces the "shims" (which provide a stable interface between the OS and managed code) and
the managed build which produces the MSIL code and nuget packages that make up CoreFX.

Calling the script `build` attempts to build both the native and managed code.
Only use it when the parameters that you are passing to the script apply for both components. Otherwise, use the scripts `build-native` and `build-managed` respectively.

**Examples**

- Building in debug mode for platform x64
```
build -debug -buildArch=x64
```

- The following example is **no** longer acceptable as we changed how we managed the parameters. For specific behaviors use the respectively script, in this case `build-native`.
```
build native debug                 => ERROR!
```

- The following example will **fail**, because the property `/p:skiptests=true` only applies to the build-managed component.
```
build -debug /p:skiptests=true     => ERROR!
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

##Build Managed
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

- Building only the managed libraries with /flp:v=diag. It doesn't restore packages, build tests or build packages.
```
build-managed -binaries -verbose
```

- Building the managed libraries and the tests but not running tests.
```
build-managed -skiptests
```

- The following example shows the argument `--`. Everything that is after it is not going to be processed and it is going to be passed as it is.
Use it to pass extra msbuild properties.
```
build-managed -binaries -- /p:WithoutCategories=IgnoreForCI
```

### Building individual CoreFx DLLs

Under the src directory is a set of directories, each of which represents a particular assembly in CoreFX.
  
For example the src\System.Diagnostics.DiagnosticSource directory holds the source code for the System.Diagnostics.DiagnosticSource.dll assembly. Each of these directories includes two projects, one for the DLL being built and one for the tests, both specified by a .builds file.

You can build the DLL for System.Diagnostics.DiagnosticSource.dll by going to the src\System.Diagnostics.DiagnosticsSource\src directory and typing `msbuild System.Diagnostics.DiagnosticSource.builds`. The DLL ends up in bin\AnyOS.AnyCPU.Debug\System.Diagnostics.DiagnosticSource.

You can build the tests for System.Diagnostics.DiagnosticSource.dll by going to 
src\System.Diagnostics.DiagnosticSource\tests and typing `msbuild System.Diagnostics.DiagnosticSource.Tests.builds`.

There is also a pkg directory for each project, and if you go into it and type `msbuild`, it will build the DLL (if needed)
and then also build the NuGet package for it. The NuGet package ends up in the bin\packages directory.

For libraries that have multiple build configurations, the `.builds` file will have multiple binary outputs (one for each configuration). It is also possible to directly build the `.csproj` project in order to produce only a single configuration. Looking at the `.builds` file will tell you which configuration combinations are valid. For example, a builds file with these entries will have two valid combinations:
```XML
<Project Include="System.Net.NetworkInformation.csproj">
  <OSGroup>Linux</OSGroup>
</Project>
```
`msbuild System.Net.NetworkInformation.csproj /p:OSGroup=Linux`

```XML
<Project Include="System.Net.NetworkInformation.csproj">
  <OSGroup>Windows_NT</OSGroup>
  <TargetGroup>uap101</TargetGroup>
</Project>
```
`msbuild System.Net.NetworkInformation.csproj /p:OSGroup=Windows_NT /p:TargetGroup=uap101`

**Note:** If building in a non-Windows environment, call `<repo-root>/Tools/msbuild.sh` instead of just `msbuild`.

### Building other OSes

By default, building from the root will only build the libraries for the OS you are running on. One can
build for another OS by specifying `build-managed -FilterToOSGroup=[value]` or build for all by specifying `build-managed -BuildAllOSGroups`.

### Building in Release or Debug

By default, building from the root or within a project will build the libraries in Debug mode. 
One can build in Debug or Release mode from the root by doing `build -release` or `build -debug` or when building a project by specifying `/p:ConfigurationGroup=[Debug|Release]` after the `msbuild` command.

### Building other Architectures

One can build 32- or 64-bit binaries or for any architecture by specifying in the root `build -buildArch=[value]` or in a project `/p:Platform=[value]` after the `msbuild` command.


### Tests

We use the OSS testing framework [xunit](http://xunit.github.io/).

#### Running tests on the command line

By default, the core tests are run as part of `build.cmd` or `build.sh`. If the product binaries are already available, you could do `build-tests` which will build and run the tests.
If the tests are already built and you only want to run them, invoke `run-tests`.

For more information about cross-platform testing, please take a look [here](https://github.com/dotnet/corefx/blob/master/Documentation/building/cross-platform-testing.md).

If you are interested in building and running the tests only for a specific library, then there are two different ways to do it:

The easiest (and recommended) way to do it, is by simply building the test .builds file for that library.

```cmd
cd src\System.Collections.Immutable\tests
msbuild System.Collections.Immutable.Tests.builds
```

What that will do is to build the tests against all of the available configurations, and then apply some filtering to run only the project configurations that support the default TestTFM(more about this on the next section). This is the prefered way since even though we only run the tests on a specific TFM, we will cross compile the tests against all its available configurations, which is good in order to make sure that you are not breaking the test build. This is also how CI will run the tests, so if you are able to build the .builds file succesfully, chances are that CI will too.

The second way to do it, is to build the .csproj and select to run the `BuildAndTest` target:

```cmd
cd src\System.Collections.Immutable\tests
msbuild /t:BuildAndTest (or /t:Test to just run the tests if the binaries are already built)
```

It is possible to pass parameters to the underlying xunit runner via the `XunitOptions` parameter, e.g.:
```cmd
msbuild /t:Test "/p:XunitOptions=-class Test.ClassUnderTests"
```
**Note:** If building in a non-Windows environment, call `./Tools/msbuild.sh` instead of just `msbuild`.

There may be multiple projects in some directories so you may need to specify the path to a specific test project to get it to build and run the tests.

Tests participate in the incremental build.  This means that if tests have already been run, and inputs to the incremental build have not changed, rerunning the tests target will not execute the test runner again.  To force re-executing tests in this situation, use `msbuild /t:RebuildAndTest`.

#### What is TestTFM and what possible values can it have

`TestTFM` is the Framework that we will use to run your tests on. The same test assembly can be used to run tests on two different frameworks, for example, AssemblyWithMyTests.dll can be used to run your tests in TFM `A` and `B` as long as framworks `A` and `B` provide/support the NetStandard surface area needed for the test assembly to compile. For this reason, when you write your tests, you might want to run them against different frameworks, and the next section will point out how to do that.

Some of the possible values for `TestTFM` are:

_**`netcoreapp1.1` or `netcoreapp1.0`**_
NetStandard implementations that run in CoreCLR.

_**`netcore50` or `uap101aot`**_
NetStandard implementations for UWP.

-**`net46` or `net462` or `net463`**-
NetStandard implementations for Desktop.

#### Running tests in a different TFM

Each test project corresponds to a test .builds file. There are some tests that might be OS specific, or might be testing API that is available only on some TFMs, which is what this tests.builds files are for. By default, we will build all of these different configurations always, but we will only execute the tests on one TFM. By default, our `TestTFM` is set to `netcoreapp1.1` [here](https://github.com/dotnet/corefx/blob/master/dir.props#L495) which means that we will run tests for the configurations that either:
- Have `netcoreapp1.1` inside their `<TestTFMs>...</TestTFMs>` clause on the .builds files, or
- Don't have a `<TestTFMs>...</TestTFMs>` metadata in the .builds file at all because we default it to netcoreapp1.1 [here](https://github.com/dotnet/corefx/blob/master/dir.traversal.targets#L146-L147)

The rest of the configurations will still get built, but they won't be executed by default, or as part of the CI. In order to use a different TestTFM, pass in the `FilterToTestTFM` property like:

```cmd
cd src\System.Runtime\tests
msbuild System.Runtime.Tests.builds /p:FilterToTestTFM=net462
```

The previous example will again build the System.Runtime csproj in all of it's different configurations, but will only execute the tests that have `net462` in their `<TestTFMs>...</TestTFMs>` metadata on System.Runtime.Tests.builds

One more way to run tests on a specific TFM, is to do it by building the test csproj directly and set the value of `TestTFM` like:

```cmd
cd src\System.Runtime\tests
msbuild System.Runtime.Tests.csproj /t:BuildAndTest /p:TestTFM=net462
```

#### Filtering tests using traits

The tests can also be filtered based on xunit trait attributes defined in [`xunit.netcore.extensions`](https://github.com/dotnet/buildtools/tree/master/src/xunit.netcore.extensions). These attributes are to be specified over the test method. The available attributes are:

_**`OuterLoop`:**_
Tests marked as `Outerloop` are for scenarios that don't need to run every build. They may take longer than normal tests, cover seldom hit code paths, or require special setup or resources to execute. These tests are excluded by default when testing through msbuild but can be enabled manually by adding the  `Outerloop` property e.g. 

```cmd
build-managed -Outerloop
```

To run <b>only</b> the Outerloop tests, use the following command:
```cmd
xunit.console.netcore.exe *.dll -trait category=outerloop
msbuild *.csproj /p:WithCategories=OuterLoop
```

_**`PlatformSpecific(TestPlatforms platforms)`:**_
Use this attribute on test methods to specify that this test may only be run on the specified platforms. This attribute returns the following categories based on platform

       - `nonwindowstests`: for tests that don't run on Windows
       - `nonlinuxtests`: for tests that don't run on Linux
       - `nonosxtests`: for tests that don't run on OS X

To run Linux-specific tests on a Linux box, use the following command line:
```sh
xunit.console.netcore.exe *.dll -notrait category=nonlinuxtests
```

_**`ActiveIssue(int issue, TestPlatforms platforms)`:**_
Use this attribute over test methods, to skip failing tests only on the specific platforms, if no platforms are specified, then the test is skipped on all platforms. This attribute returns the 'failing' category, so to run all acceptable tests on Linux that are not failing, use the following commandline,
```sh
xunit.console.netcore.exe *.dll -notrait category=failing -notrait category=nonlinuxtests
```

And to run all Linux-compatible tests that are failing:
```sh
xunit.console.netcore.exe *.dll -trait category=failing -notrait category=nonlinuxtests
```

_**A few common examples with the above attributes:**_

- Run all tests acceptable on Windows:
```cmd
xunit.console.netcore.exe *.dll -notrait category=nonwindowstests
```
- Run all inner loop tests acceptable on Linux:
```sh
xunit.console.netcore.exe *.dll -notrait category=nonlinuxtests -notrait category=OuterLoop
```
- Run all outer loop tests acceptable on OS X that are not currently associated with active issues:
```sh
xunit.console.netcore.exe *.dll -notrait category=nonosxtests -trait category=OuterLoop -notrait category=failing
```
- Run all tests acceptable on Linux that are currently associated with active issues:
```sh
xunit.console.netcore.exe *.dll -notrait category=nonlinuxtests -trait category=failing
```

All the required dlls to run a test project can be found in `bin\tests\{Configration}\{Project}.Tests\netcoreapp1.0\` which should be created when the test project is built.

### Code Coverage

Code coverage is built into the corefx build system.  It utilizes OpenCover for generating coverage data and ReportGenerator for generating reports about that data.  To run:

```cmd
// Run full coverage
build-managed -Coverage

// To run a single project with code coverage enabled pass the /p:Coverage=true property
cd src\System.Collections.Immutable\tests
msbuild /t:BuildAndTest /p:Coverage=true
```
If coverage succeeds, the code coverage report will be generated automatically and placed in the bin\tests\coverage directory.  You can view the full report by opening index.htm

Code coverage reports from the continuous integration system are available from the links on the front page of the corefx repo.

### Building tests with .NET Native (Windows only)

.NET Native is a technology that allows compiling IL applications down into a native executable and minimal set of native DLLs, containing all needed functionality from the .NET Framework in native format.  For CoreFX tests, .NET Native support in CoreFX is relatively early, but supported.  

```cmd
// To run a single project with the .NET Native toolchain, set the appropriate build flags:
cd src\Microsoft.CSharp\tests
msbuild /t:BuildAndTest /p:TestTFM=netcore50aot  /p:TestNugetRuntimeId=win10-x64-aot /p:UseDotNetNativeToolchain=true
```
If native compilation succeeds, the test will build and run as a native executable named "xunit.console.netcore.exe" in a folder named "native" in the test execution folder.  Note many tests in CoreFX are not ready to run though native compilation yet.

A slight variation on these arguments will allow you to build and run against netcore50, the managed version of the UWP Framework subset, used when debugging UWP applications in Visual Studio:
```cmd
// To run a single project with the .NET Native toolchain, set the appropriate build flags:
cd src\Microsoft.CSharp\tests
msbuild /t:BuildAndTest /p:TestTFM=netcore50  /p:TestNugetRuntimeId=win10-x64
```
In this case, your test will get executed within the context of a wrapper UWP application, targeting the Managed netcore50.

The CoreFX build and test suite is a work in progress, as are the [building and testing instructions](../README.md). The .NET Core team and the community are improving Linux and OS X support on a daily basis and are adding more tests for all platforms. See [CoreFX Issues](https://github.com/dotnet/corefx/issues) to find out about specific work items or report issues.
