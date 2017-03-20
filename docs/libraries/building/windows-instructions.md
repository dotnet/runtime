Building CoreFX on Windows
==========================

You can build .NET Core either via the command line or by using Visual Studio.

## Required Software

Visual Studio must be installed. Supported versions:
* [Visual Studio 2015](https://www.visualstudio.com/vs/older-downloads/) (Community, Professional, Enterprise).  The community version is completely free.  
* [Visual Studio 2017](https://www.visualstudio.com/downloads/) (Community, Professional, Enterprise).  The community version is completely free.

For Visual Studio 2015:
* In order to build our C++ projects be sure to select "Programming Languages | Visual C++ | Common Tools for Visual C++ 2015" while installing VS 2015 (or modify your install to include it).
* We also require that [Visual Studio 2015 Update 1](https://www.visualstudio.com/en-us/news/vs2015-update1-vs.aspx) be installed.

For Visual Studio 2017:
* When doing a 'Workloads' based install, the following are the minimum requirements:
  * .NET Desktop Development
    * All Required Components
    * .NET Framework 4-4.6 Development Tools
  * Desktop Development with C++
    * All Required Components
    * VC++ 2017 v141 Toolset (x86, x64)
    * Windows 8.1 SDK and UCRT SDK
    * VC++ 2015.3 v140 Toolset (x86, x64)
* When doing an 'Individual Components' based install, the following are the minimum requirements:
  * C# and Visual Basic Roslyn Compilers
  * Static Analysis Tools
  * .NET Portable Library Targeting Pack
  * Windows 10 SDK or Windows 8.1 SDK
  * Visual Studio C++ Core Features
  * VC++ 2017 v141 Toolset (x86, x64)
  * MSBuild
  * .NET Framework 4.6 Targeting Pack
  * Windows Universal CRT SDK
  * VC++ 2015.3 v140 Toolset (x86, x64)
* Ensure you are running from the "Developer Command Prompt for VS2017"; Otherwise, the build will attempt to locate and use the VS2015 toolset.

[CMake](https://cmake.org/) is required to build the native libraries for Windows. To build these libraries cmake must be installed from [the CMake download page](https://cmake.org/download/#latest) and added to your path.

## Building From the Command Line

Open a [Visual Studio Command Prompt](http://msdn.microsoft.com/en-us/library/ms229859(v=vs.110).aspx).

- `build.cmd` - Will cause basic tool initialization and build the default configuration for refs, libs, and packages.
- `build-tests.cmd` - Will build and run tests for the default configuration.

For information on different configurations see [project-guidelines](../coding-guidelines/project-guidelines.md).

**Note**: Before working on individual projects or test projects you **must** run `build.cmd` from the root once before beginning that work. It is also a good idea to run `build.cmd` whenever you pull a large set of unknown changes into your branch.

Visual Studio Solution (.sln) files exist for related groups of libraries. These can be loaded to build, debug and test inside
the Visual Studio IDE.

Note that when calling the script `build.cmd` attempts to build both the native and managed code.
Only use it when the parameters that you are passing to the script apply for both components. Otherwise, use the scripts `build-native.cmd` and `build-managed.cmd` respectively.

For more information about the different options when building, run `build.cmd -?` and look at examples in the [developer-guide](../project-docs/developer-guide.md).

### Running tests from the command line

From the root, use `build-tests.cmd`.
For more details, or to test an individual project, see the [developer guide topic](https://github.com/dotnet/corefx/blob/master/Documentation/project-docs/developer-guide.md).

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
