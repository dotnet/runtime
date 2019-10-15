Building CoreFX on Windows
==========================

## Required Software

1. **[Visual Studio 2019](https://visualstudio.microsoft.com/vs/preview/)** (Community, Professional, Enterprise) with the latest update must be installed. The Community version is completely free.
2. **[CMake](https://cmake.org/)** must be installed from [the CMake download page](https://cmake.org/download/#latest) and added to your path. CMake 3.16.0-RC1 or later is required.

## Recommended Software
**[.NET Core SDK](https://dotnet.microsoft.com/download/dotnet-core/3.0)** >= v3.0.100 should be installed, which will add the `dotnet` toolchain to your path.

### Visual Studio 2019

#### Visual Studio 2019 - 'Workloads' based install

The following are the minimum requirements:
  * .NET desktop development
    * All Required Components
    * .NET Framework 4.7.2 SDK
    * Verify the required components include:
      * .NET Framework 4.7.2 targeting pack
  * Desktop development with C++
    * All Required Components
    * Windows Universal CRT SDK
    * MSVC v142 - VS 2019 C++ x64/x86 build tools (latest - for example - v14.24)
    * MSVC v141 - VS 2017 C++ x64/x86 build tools (latest - for example - v14.16)
    * Verify the required components include:
      * Windows 10 SDK (latest - for example - 10.0.18362.0)
  * .NET Core cross-platform development
    * All Required Components

#### Visual Studio 2019 - 'Individual components' based install

The following are the minimum requirements:
  * C# and Visual Basic Roslyn Compilers
  * .NET Portable Library Targeting Pack
  * Windows 10 SDK
  * C++ core features
  * MSVC v142 - VS 2019 C++ x64/x86 build tools (latest - for example - v14.24)
  * MSVC v141 - VS 2017 C++ x64/x86 build tools (latest - for example - v14.16)
  * MSBuild
  * .NET Framework 4.7.2 SDK
  * .NET Framework 4.7.2 Targeting Pack
  * Windows Universal CRT SDK

To build binaries for ARM, you need the following additional indivdual components:
* Visual C++ compilers and libraries for ARM
* Visual C++ compilers and libraries for ARM64

## Building From the Command Line

From a (non-admin) Command Prompt window:

- `build.cmd` - Will cause basic tool initialization and build the default configuration for refs, libs, and packages.

For information on different configurations see [project-guidelines](../coding-guidelines/project-guidelines.md).

**Note**: Before working on individual projects or test projects you **must** run `build.cmd` from the root once before beginning that work. It is also a good idea to run `build.cmd` whenever you pull a large set of unknown changes into your branch.

Visual Studio Solution (.sln) files exist for related groups of libraries. These can be loaded to build, debug and test inside the Visual Studio IDE.

Note that when calling the script `build.cmd` attempts to build both the native and managed code.

For more information about the different options when building, run `build.cmd -help` and look at examples in the [developer-guide](../project-docs/developer-guide.md).

### Running tests from the command line

From the root, use `build.cmd -test`.
For more details, or to test an individual project, see the [developer guide topic](https://github.com/dotnet/corefx/blob/master/Documentation/project-docs/developer-guide.md).

### Running tests from Visual Studio

1. Open solution of interest
2. Right click test project and select 'Set as startup project'
3. Select the corresponding launch profile (green arrow, i.e. `.NET Core xUnit Console`)
4. Ctrl+F5 (Run)

### Debugging tests in Visual Studio

1. Open solution of interest
2. Right click test project and select 'Set as startup project'
3. Set breakpoint appropriately
4. Select the corresponding launch profile (green arrow, i.e. `.NET Core xUnit Console`)
5. F5 (Debug)

### Using Test Explorer in Visual Studio

1. Open solution from the build script: `.\build.cmd -vs Microsoft.CSharp`. Alternatively you can also pass in the relative or full path to the solution file.
2. Navigate to the Test Explorer tab and run/debug tests.

VS Test Explorer support is limited to the .NET Core. To switch between Configurations (Debug / Release), Visual Studio needs to be reopened with the command above together with the additional `--configuration/-c` option.

For advanced debugging using WinDBG see [Debugging CoreFX on Windows](https://github.com/dotnet/corefx/blob/master/Documentation/debugging/windows-instructions.md)

### Notes
* At any given time, the corefx repo might be configured to use a [more recent compiler](../../../DotnetCLIVersion.txt) than
the one used by the installed .NET Core SDK. This means the corefx codebase might
be using language features that are not understood by the IDE, which might result in errors that
show up as red squiggles while writing code. Such errors should, however, not affect the actual compilation.

* If your build fails with "[...].dll - Access is denied" errors, it might be because Visual Studio/MSBuild is locking these files. Run `taskkill /im dotnet.exe /f` to shutdown all currently running dotnet instances.
