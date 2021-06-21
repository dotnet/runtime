Requirements to build dotnet/runtime on Windows.
========================

These instructions will lead you through the requirements to build dotnet/runtime on Windows.

----------------
# Environment

You must install several components to build the dotnet/runtime repository. These instructions were tested on Windows 10 Pro, version 1903.

## Enable Long Paths

The runtime repository requires long paths to be enabled. Follow [the instructions provided here](https://docs.microsoft.com/en-us/windows/win32/fileio/maximum-file-path-limitation#enable-long-paths-in-windows-10-version-1607-and-later) to enable that feature.

If using Git for Windows you might need to also configure long paths there. Using an admin terminal simply type:
```cmd
git config --system core.longpaths true
```

## Visual Studio

- Install [Visual Studio 2019](https://visualstudio.microsoft.com/downloads/). The Community edition is available free of charge.

Visual Studio 2019 installation process:
- It's recommended to use 'Workloads' installation approach. The following are the minimum requirements:
  - .NET Desktop Development with all default components.
  - Desktop Development with C++ with all default components.
- To build for Arm32 or Arm64, make sure that you have the right architecture-specific compilers installed. In the "Individual components" window, in the "Compilers, build tools, and runtimes" section:
  - For Arm32, check the box for "MSVC v142 - VS 2019 C++ ARM build tools (v14.23 or newer)".
  - For Arm64, check the box for "MSVC v142 - VS 2019 C++ ARM64 build tools (v14.23 or newer)".
- To build the tests, you will need some additional components:
  - Windows 10 SDK component version 10.0.18362 or newer. This component is installed by default as a part of 'Desktop Development with C++' workload.
  - C++/CLI support for v142 build tools (v14.23 or newer)

A `.vsconfig` file is included in the root of the dotnet/runtime repository that includes all components needed to build the dotnet/runtime repository. You can [import `.vsconfig` in your Visual Studio installer](https://docs.microsoft.com/en-us/visualstudio/install/import-export-installation-configurations?view=vs-2019#import-a-configuration) to install all necessary components.

Visual Studio 2019 16.6 or later is required for building the repository. Visual Studio 2019 16.10 is required to work with the libraries projects inside the Visual Studio IDE.

## CMake

- Install [CMake](https://cmake.org/download) for Windows.
- Add its location (e.g. C:\Program Files (x86)\CMake\bin) to the PATH environment variable.
  The installation script has a check box to do this, but you can do it yourself after the fact following the instructions at [Adding to the Default PATH variable](#adding-to-the-default-path-variable).

The dotnet/runtime repository recommends using CMake 3.16.4 or newer, but it may work with CMake 3.15.5.

## Ninja

- Install Ninja in one of the two following ways
  - [Download the executable](https://github.com/ninja-build/ninja/releases) and add its location to [the Default PATH variable](#adding-to-the-default-path-variable).
  - [Install via a package manager](https://github.com/ninja-build/ninja/wiki/Pre-built-Ninja-packages), which should automatically add it to the PATH environment variable.

## Python

- Install [Python](https://www.python.org/downloads/) for Windows.
- Add its location (e.g. C:\Python*\) to the PATH environment variable.
  The installation script has a check box to do this, but you can do it yourself after the fact following the instructions at [Adding to the Default PATH variable](#adding-to-the-default-path-variable).

The dotnet/runtime repository requires at least Python 3.7.4.

## Git

- Install [Git](https://git-for-windows.github.io/) for Windows.
- Add its location (e.g. C:\Program Files\Git\cmd) to the PATH environment variable.
  The installation script has a check box to do this, but you can do it yourself after the fact following the instructions at [Adding to the Default PATH variable](#adding-to-the-default-path-variable).

The dotnet/runtime repository requires at least Git 2.22.0.

## PowerShell

- Ensure that `powershell.exe` is accessible via the PATH environment variable. Typically this is `%SYSTEMROOT%\System32\WindowsPowerShell\v1.0\` and its automatically set upon Windows installation.
- Powershell version must be 3.0 or higher. Use `$PSVersionTable.PSVersion` to determine the engine version.

## .NET SDK

While not strictly needed to build or test this repository, having the .NET SDK installed lets you browse solution files in this repository with Visual Studio and use the dotnet.exe command to run .NET applications in the 'normal' way.
We use this in the [Using Your Build](../testing/using-your-build.md) instructions.
The minimum required version of the SDK is specified in the [global.json file](https://github.com/dotnet/runtime/blob/main/global.json#L3). [You can find the installers and binaries for nightly builds of .NET SDK here](https://github.com/dotnet/installer#installers-and-binaries).

Alternatively, to avoid modifying your machine state, you can use the repository's locally acquired SDK by passing in the solution to load via the `-vs` switch:

```
build.cmd -vs System.Text.RegularExpressions
```

This will set the `DOTNET_ROOT` and `PATH` environment variables to point to the locally acquired SDK under `runtime\.dotnet\` and will launch the Visual Studio instance which is registered for the `sln` extension.

## Adding to the default PATH variable

The commands above need to be on your command lookup path.   Some installers will automatically add them to the path as part of the installation, but if not here is how you can do it.

You can, of course, add a directory to the PATH environment variable with the syntax `set PATH=%PATH%;DIRECTORY_TO_ADD_TO_PATH`.

However, the change above will only last until the command windows close.
You can make your change to the PATH variable persistent by going to  Control Panel -> System And Security -> System -> Advanced system settings -> Environment Variables,
and select the 'Path' variable in the 'System variables' (if you want to change it for all users) or 'User variables' (if you only want to change it for the current user).
Simply edit the PATH variable's value and add the directory (with a semicolon separator).
