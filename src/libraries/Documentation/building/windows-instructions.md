Building CoreFX on Windows
==========================

You can build .NET Core either via the command line or by using Visual Studio.
We currently only support building and running on Windows. Other platforms will
come later.

## Required Software

Visual Studio 2015 is required.

The following free downloads are compatible:
* [Visual Studio Community 2015](http://www.visualstudio.com/en-us/downloads/visual-studio-2015-downloads-vs)

Note: In order to build our C++ projects be sure to select "Programming Languages | Visual C++ | Common Tools for Visual C++ 2015" while installing VS 2015 (or modify your install to include it).

[CMake](https://cmake.org/) is required to build the native libraries for Windows. To build these libraries cmake must be installed from [the CMake download page](https://cmake.org/download/) and added to your path.

## Building From the Command Line

Open a [Visual Studio Command Prompt](http://msdn.microsoft.com/en-us/library/ms229859(v=vs.110).aspx).
From the root of the repository, type `build`. This will build everything and run
the core tests for the project. Visual Studio Solution (.sln) files exist for
related groups of libraries. These can be loaded to build, debug and test inside
the Visual Studio IDE.

### Building individual DLLs of the CoreFX

Under the src directory is a set of directortories, each of which reprsents a particular Assembly in the CoreFX.  
For example the src\System.Diagnostics.DiagnosticSource directory holds the source code for the System.Diagnostics.DiagnosticSource.dll  assembly.   Each of these Directories has a .SLN solution 
file that typically includes two projects, one for the DLL being built and one for the tests.   Thus
you can build both the DLL and Tests for System.Diagnostics.DiagnosticSource.dll by going to 
src\System.Diagnostics.DiagnosticSource and typing 

    msbuild
 
You can build just the System.Diagnostics.DiagnosticsSource.dll (without the tests) by going to the src\System.Diagnostics.DiagnosticsSource\src directory and again typing

    msbuild

The DLL ends up as  bin\AnyOS.AnyCPU.Debug\System.Diagnostics.DiagnosticSource\System.DiagnosticSource.dll 

There is also a pkg directory and if you go to directory and type msbuild, it will build the DLL (if needed)
and the also build the Nuget package for it.   The Nuget package ends up in the bin\pkg directory.  

### Building other OSes

By default building from the root will only build the libraries for the OS you are running on. One can
build for another OS by specifying `/p:FilterToOSGroup=[Windows_NT|Linux|OSX|FreeBSD]` or build for all by specifying
`/p:BuildAllOSGroups=true`.

[Building CoreFX on FreeBSD, Linux and OS X](unix-instructions.md)
## Tests

We use the OSS testing framework [xunit](http://xunit.github.io/).

### Running tests on the command line

By default, the core tests are run as part of the build. Running the tests from
the command line is as simple as invoking `build.cmd` on windows, and `run-test.sh` on linux and osx.

You can also run the tests for an individual project by building it individually, e.g.:

```
cd src\System.Collections.Immutable\tests
msbuild /t:BuildAndTest (or /t:Test to just run the tests if the binaries are already built)
```

It is possible to pass parameters to the underlying xunit runner via the `XunitOptions` parameter, e.g.:
```cmd
msbuild /t:Test "/p:XunitOptions=-class Test.ClassUnderTests"
```

There may be multiple projects in some directories so you may need to specify the path to a specific test project to get it to build and run the tests.

Tests participate in the incremental build.  This means that if tests have already been run, and inputs to the incremental build have not changed, rerunning the tests target will not execute the test runner again.  To force re-executing tests in this situation, use `msbuild /t:clean;build;test`.

The tests can also be filtered based on xunit trait attributes defined in [`xunit.netcore.extensions`](https://github.com/dotnet/buildtools/tree/master/src/xunit.netcore.extensions). These attributes are to be specified over the test method. The available attributes are:

_**`OuterLoop`:**_
This attribute applies the 'outerloop' category; to run outerloop tests, use the following commandline
```cmd
xunit.console.netcore.exe *.dll -trait category=outerloop
build.cmd *.csproj /p:WithCategories=OuterLoop
```

_**`PlatformSpecific(Xunit.PlatformID platforms)`:**_
Use this attribute on test methods to specify that this test may only be run on the specified platforms. This attribute returns the following categories based on platform

       - `nonwindowstests`: for tests that don't run on Windows
       - `nonlinuxtests`: for tests that don't run on Linux
       - `nonosxtests`: for tests that don't run on OS X

To run Linux specific tests on a Linux box, use the following commandline,
```sh
xunit.console.netcore.exe *.dll -notrait category=nonlinuxtests
```

_**`ActiveIssue(int issue, Xunit.PlatformID platforms)`:**_
Use this attribute over tests methods, to skip failing tests only on the specific platforms, if no platforms is specified, then the test is skipped on all platforms. This attribute returns the 'failing' category, so to run all acceptable tests on Linux that are not failing, use the following commandline,
```sh
xunit.console.netcore.exe *.dll -notrait category=failing -notrait category=nonlinuxtests
```

And to run all Linux-compatible tests that are failing,
```sh
xunit.console.netcore.exe *.dll -trait category=failing -notrait category=nonlinuxtests
```

_**A few common examples with the above attributes:**_

- Run all tests acceptable on Windows
```cmd
xunit.console.netcore.exe *.dll -notrait category=nonwindowstests
```
- Run all inner loop tests acceptable on Linux
```sh
xunit.console.netcore.exe *.dll -notrait category=nonlinuxtests -notrait category=OuterLoop
```
- Run all outer loop tests acceptable on OS X that are not currently associated with active issues
```sh
xunit.console.netcore.exe *.dll -notrait category=nonosxtests -trait category=OuterLoop -notrait category=failing
```
- Run all tests acceptable on Linux that are currently associated with active issues
```sh
xunit.console.netcore.exe *.dll -notrait category=nonlinuxtests -trait category=failing
```

All the required dlls to run a test project can be found in `bin\tests\{Flavor}\{Project}.Tests\aspnetcore50\` which should be created when the test project is built.

To skip an entire test project on a specific platform, for example, to skip running registry tests on Linux and Mac OS X, use the `<UnsupportedPlatforms>` MSBuild property in the csproj. Valid platform values are
```xml
<UnsupportedPlatforms>Windows_NT;Linux;OSX</UnsupportedPlatforms>
```

### Running tests from Visual Studio

1. Open solution of interest
2. Right click test project and select 'Set as startup project'
3. Ctrl+F5 (Run)

### Debugging tests in Visual Studio

1. Install VS 2015 Preview or later including Web Developer Tools
2. Open solution of interest in VS 2015
3. Right click test project and select 'Set as startup project'
4. Set breakpoint appropriately
5. F5 (Debug)

For advanced debugging using WinDBG see [Debugging CoreFX on Windows](https://github.com/dotnet/corefx/blob/master/Documentation/debugging/windows-instructions.md)

### Code Coverage

Code coverage is built into the corefx build system.  It utilizes OpenCover for generating coverage data and ReportGenerator for generating reports about that data.  To run:

```cmd
// Run full coverage
build.cmd /p:Coverage=true

// To run a single project with code coverage enabled pass the /p:Coverage=true property
cd src\System.Collections.Immutable\tests
msbuild /t:BuildAndTest /p:Coverage=true
```
If coverage succeeds, the code coverage report will be generated automatically and placed in the bin\tests\coverage directory.  You can view the full report by opening index.htm

Code coverage reports from the continuous integration system are available from the links on the front page of the corefx repo.

### Notes
* Running tests from using the VS test explorer does not currently work after we switched to running on CoreCLR. [We will be working on enabling full VS test integration](https://github.com/dotnet/corefx/issues/1318) but we don't have an ETA yet. In the meantime, use the steps above to launch/debug the tests using the console runner.

* VS 2015 is required to debug tests running on CoreCLR as the CoreCLR
debug engine is a VS 2015 component.

* If the Xamarin PCL profiles are installed, the build will fail due to [issue #449](https://github.com/dotnet/corefx/issues/449).  A possible workaround is listed [in the issue](https://github.com/dotnet/corefx/issues/449#issuecomment-95117040) itself.
