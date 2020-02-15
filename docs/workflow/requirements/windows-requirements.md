Requirements to build dotnet/runtime on Windows.
========================

These instructions will lead you through the requirements to build dotnet/runtime on Windows.

----------------
# Environment

You must install several components to build the dotnet/runtime repository. These instructions were tested on Windows 10 Pro, version 1903.

## Enable Long Paths

The runtime repository requires long paths to be enabled. Follow the instructions provided here to modify the registry to opt into that feature: https://docs.microsoft.com/en-us/windows/win32/fileio/naming-a-file#enable-long-paths-in-windows-10-version-1607-and-later.

If using msysgit (aka Git for Windows) you might need to also configure long paths there. Using an admin terminal simply type:
```powershell
PS> git config --system core.longpaths true
```

## Visual Studio

- Install [Visual Studio 2019](https://visualstudio.microsoft.com/downloads/). The Community version is completely free.

Visual Studio 2019 installation process:
* It's recommended to use 'Workloads' installation approach. The following are the minimum requirements:
  * .NET Desktop Development with all default components.
  * Desktop Development with C++ with all default components.
* To build for Arm32 or Arm64, Make sure that you have the Windows 10 SDK installed (or selected to be installed as part of VS installation). To explicitly install Windows SDK, download it from here: [Windows SDK for Windows 10](https://developer.microsoft.com/en-us/windows/downloads).
  * In addition, ensure you install the ARM tools. In the "Individual components" window, in the "Compilers, build tools, and runtimes" section, check the box for "MSVC v142 - VS 2019 C++ ARM build tools (v14.23)".
  * Also, ensure you install the ARM64 tools. In the "Individual components" window, in the "Compilers, build tools, and runtimes" section, check the box for "MSVC v142 - VS 2019 C++ ARM64 build tools (v14.23)".
* To build the tests, you will need some additional components:
  * Windows 10 SDK component version 10.0.18362 or newer. This component is installed by default as a part of 'Desktop Development with C++' workload.
  * C++/CLI support for v142 build tools (14.23)

A `.vsconfig` file is included in the root of the dotnet/runtime repository that includes all components needed to build the dotnet/runtime repository.

The dotnet/runtime repository requires at least Visual Studio 2019 16.3.

## CMake

- Install [CMake](http://www.cmake.org/download) for Windows.
- Add its location (e.g. C:\Program Files (x86)\CMake\bin) to the PATH environment variable.
  The installation script has a check box to do this, but you can do it yourself after the fact following the instructions at [Adding to the Default PATH variable](#adding-to-the-default-path-variable).

The dotnet/runtime repository requires at least CMake 3.15.5.

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

- Ensure that it is accessible via the PATH environment variable. Typically this is `%SYSTEMROOT%\System32\WindowsPowerShell\v1.0\` and its automatically set upon Windows installation.
- Powershell version must be 3.0 or higher. Use `$PSVersionTable.PSVersion` to determine the engine version.

## DotNet Core SDK

While not strictly needed to build or test the .NET Core repository, having the .NET Core SDK installed lets you use the dotnet.exe command to run .NET Core applications in the 'normal' way.   We use this in the
[Using Your Build](../testing/using-your-build.md) instructions.  Visual Studio should have
installed the .NET Core SDK, but in case it did not you can get it from the [Installing the .NET Core SDK](https://dotnet.microsoft.com/download) page.

## Adding to the default PATH variable

The commands above need to be on your command lookup path.   Some installers will automatically add them to the path as part of the installation, but if not here is how you can do it.

You can, of course, add a directory to the PATH environment variable with the syntax `set PATH=%PATH%;DIRECTORY_TO_ADD_TO_PATH`.

However, the change above will only last until the command windows close.   You can make your change to
the PATH variable persistent by going to  Control Panel -> System And Security -> System -> Advanced system settings -> Environment Variables,
and select the 'Path' variable in the 'System variables' (if you want to change it for all users) or 'User variables' (if you only want
to change it for the current user).  Simply edit the PATH variable's value and add the directory (with a semicolon separator).

