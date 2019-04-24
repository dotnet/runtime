Building CoreFX on Windows
==========================

## Required Software

1. **[Visual Studio 2017](https://www.visualstudio.com/downloads/)** or **[Visual Studio 2019](https://visualstudio.microsoft.com/vs/preview/)** (Community, Professional, Enterprise) with the latest update must be installed. The Community version is completely free.
1. **[CMake](https://cmake.org/)** must be installed from [the CMake download page](https://cmake.org/download/#latest) and added to your path. CMake 3.14 or later is required to build with VS 2019.

## Recommended Software
1. **[Visual Studio 2019 Preview](https://visualstudio.microsoft.com/vs/preview/)** (Community, Professional, Enterprise) with the latest update should be installed. The Community version is completely free.
1. **[.NET Core SDK](https://dotnet.microsoft.com/download/dotnet-core/3.0)** >= v3.0.0-preview3 should be installed, which will add the `dotnet` toolchain to your path.

### Visual Studio 2019

#### Visual Studio 2019 - 'Workloads' based install

The following are the minimum requirements:
  * .NET desktop development
    * All Required Components
    * .NET Framework 4.7.2 Development Tools
  * Desktop development with C++
    * All Required Components
    * VC++ 2019 v142 Toolset (x86, x64)
    * Windows 8.1 SDK and UCRT SDK
    * VC++ 2017 v141 Toolset (x86, x64)
  * .NET Core cross-platform development
    * All Required Components

#### Visual Studio 2019 - 'Individual components' based install

The following are the minimum requirements:
  * C# and Visual Basic Roslyn Compilers
  * Static Analysis Tools
  * .NET Portable Library Targeting Pack
  * Windows 10 SDK or Windows 8.1 SDK
  * Visual Studio C++ Core Features
  * VC++ 2019 v142 Toolset (x86, x64)
  * VC++ 2017 v141 Toolset (x86, x64)
  * MSBuild
  * .NET Framework 4.7.2 Targeting Pack
  * Windows Universal CRT SDK

To build binaries for ARM, you need the following additional indivdual components:
* Visual C++ compilers and libraries for ARM
* Visual C++ compilers and libraries for ARM64

### Visual Studio 2017

#### Visual Studio 2017 - 'Workloads' based install

The following are the minimum requirements:
  * .NET desktop development
    * All Required Components
    * .NET Framework 4.7.2 Development Tools
  * Desktop development with C++
    * All Required Components
    * VC++ 2017 v141 Toolset (x86, x64)
    * Windows 8.1 SDK and UCRT SDK
    * VC++ 2015.3 v140 Toolset (x86, x64)
  * .NET Core cross-platform development
    * All Required Components

Note: If you have both VS 2017 and 2015 installed, you need to copy DIA SDK directory from VS 2015 installation into VS 2017 (VS installer bug).

#### Visual Studio 2017 - 'Individual components' based install

The following are the minimum requirements:
  * C# and Visual Basic Roslyn Compilers
  * Static Analysis Tools
  * .NET Portable Library Targeting Pack
  * Windows 10 SDK or Windows 8.1 SDK
  * Visual Studio C++ Core Features
  * VC++ 2017 v141 Toolset (x86, x64)
  * MSBuild
  * .NET Framework 4.7.2 Targeting Pack
  * Windows Universal CRT SDK
  * VC++ 2015.3 v140 Toolset (x86, x64)

To build binaries for ARM, you need the following additional indivdual components:
* Visual C++ compilers and libraries for ARM
* Visual C++ compilers and libraries for ARM64

#### Visual Studio 2017 - Command line install

If you've installed Visual Studio 2017 already, go to `C:\Program Files (x86)\Microsoft Visual Studio\Installer` and run

     vs_installer.exe modify --installPath "C:\Program Files (x86)\Microsoft Visual Studio\2017\Community" --add Microsoft.VisualStudio.Component.NuGet --add Microsoft.Net.Component.4.6.TargetingPack --add Microsoft.VisualStudio.Component.PortableLibrary --add Microsoft.VisualStudio.Component.Static.Analysis.Tools --add Microsoft.VisualStudio.Component.Roslyn.Compiler --add Microsoft.Component.MSBuild --add Microsoft.VisualStudio.Component.VC.Tools.x86.x64 --add Microsoft.VisualStudio.Component.VC.CoreIde --add Microsoft.VisualStudio.Component.Windows10SDK.17134 --add Microsoft.VisualStudio.Component.VC.140

This will install all the components needed.

Note that you will need to adjust the install path to reflect your version, "Community", "Professional", "Enterprise" or "Preview"

For the best possible experience make sure to have the latest version of Visual Studio 2017 installed.

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

For advanced debugging using WinDBG see [Debugging CoreFX on Windows](https://github.com/dotnet/corefx/blob/master/Documentation/debugging/windows-instructions.md)

### Notes
* At any given time, the corefx repo might be configured to use a [more recent compiler](../../../DotnetCLIVersion.txt) than
the one used by the installed .NET Core SDK. This means the corefx codebase might
be using language features that are not understood by the IDE, which might result in errors that
show up as red squiggles while writing code. Such errors should, however, not affect the actual compilation.

* Running tests from using the VS test explorer does not currently work after we switched to running on CoreCLR. [We are actively working on enabling full VS test integration](https://github.com/dotnet/corefx/issues/20627) but we don't have an ETA yet. In the meantime, use the steps above to launch/debug the tests using the console runner.

* If your build fails with "[...].dll - Access is denied" errors, it might be because Visual Studio/MSBuild is locking these files. Try shutting down `VBCSCompiler.exe` and `dotnet.exe` from the task manager before building again.
