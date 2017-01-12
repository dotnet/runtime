Building CoreFX on Windows
==========================

You can build .NET Core either via the command line or by using Visual Studio.

## Required Software

Visual Studio 2015 is required.

The following free downloads are compatible:
* [Visual Studio Community 2015](http://www.visualstudio.com/en-us/downloads/visual-studio-2015-downloads-vs)

Note: In order to build our C++ projects be sure to select "Programming Languages | Visual C++ | Common Tools for Visual C++ 2015" while installing VS 2015 (or modify your install to include it).

We also require that [Visual Studio 2015 Update 1](https://www.visualstudio.com/en-us/news/vs2015-update1-vs.aspx) be installed.

[CMake](https://cmake.org/) is required to build the native libraries for Windows. To build these libraries cmake must be installed from [the CMake download page](https://cmake.org/download/) and added to your path.

## Building From the Command Line

Open a [Visual Studio Command Prompt](http://msdn.microsoft.com/en-us/library/ms229859(v=vs.110).aspx).

- `build.cmd` - Will cause basic tool initialization and build the default configuration for refs, libs, and packages.
- `build-tests.cmd` - Will build and run tests for the default configuration.

For information on different configurations see [project-guidelines](../coding-guidelines/project-guidelines.md).

**Note**: Before working on individual projects or test projects you **must** run `build.cmd` from the root once before beginning that work. It is also a good idea to run `build.cmd` whenever pull a large set of unknown changes into your branch.

Visual Studio Solution (.sln) files exist for related groups of libraries. These can be loaded to build, debug and test inside
the Visual Studio IDE.

Note that when calling the script `build.cmd` attempts to build both the native and managed code.
Only use it when the parameters that you are passing to the script apply for both components. Otherwise, use the scripts `build-native.cmd` and `build-managed.cmd` respectively.

For more information about the different options when building, run `build.cmd -?` and look at examples in the [developer-guide](../project-docs/developer-guide.md).

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

### Notes
* Running tests from using the VS test explorer does not currently work after we switched to running on CoreCLR. [We will be working on enabling full VS test integration](https://github.com/dotnet/corefx/issues/1318) but we don't have an ETA yet. In the meantime, use the steps above to launch/debug the tests using the console runner.

* VS 2015 is required to debug tests running on CoreCLR as the CoreCLR
debug engine is a VS 2015 component.

* If the Xamarin PCL profiles are installed, the build will fail due to [issue #449](https://github.com/dotnet/corefx/issues/449).  A possible workaround is listed [in the issue](https://github.com/dotnet/corefx/issues/449#issuecomment-95117040) itself.
