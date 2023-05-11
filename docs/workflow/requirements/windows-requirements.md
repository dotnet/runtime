# Requirements to build dotnet/runtime on Windows

* [Environment](#environment)
  * [Enable Long Paths](#enable-long-paths)
  * [Visual Studio](#visual-studio)
  * [Build Tools](#build-tools)
    * [CMake](#cmake)
    * [Ninja](#ninja)
    * [Python](#python)
  * [Git](#git)
  * [PowerShell](#powershell)
  * [.NET SDK](#net-sdk)
  * [Adding to the default PATH variable](#adding-to-the-default-path-variable)

These instructions will lead you through the requirements to build _dotnet/runtime_ on Windows.

## Environment

Here are the components you will need to install and setup to work with the repo.

### Enable Long Paths

The runtime repository requires long paths to be enabled. Follow [the instructions provided here](https://docs.microsoft.com/windows/win32/fileio/maximum-file-path-limitation#enable-long-paths-in-windows-10-version-1607-and-later) to enable that feature.

If using Git for Windows you might need to also configure long paths there. Using an administrator terminal simply type:

```cmd
git config --system core.longpaths true
```

### Visual Studio

Install [Visual Studio 2022](https://visualstudio.microsoft.com/downloads/). The Community edition is available free of charge. Visual Studio 2022 17.3 or later is required. Note that Visual Studio and the development tools described below are required, regardless of whether you plan to use the IDE or not. The installation process goes as follows:

* It's recommended to use **Workloads** installation approach. The following are the minimum requirements:
  * **.NET Desktop Development** with all default components,
  * **Desktop Development with C++** with all default components.
* To build for Arm64, make sure that you have the right architecture-specific compilers installed. In the **Individual components** window, in the **Compilers, build tools, and runtimes** section:
  * For Arm64, check the box for _MSVC v143* VS 2022 C++ ARM64 build tools (Latest)_.
* To build the tests, you will need some additional components:
  * **C++/CLI support for v142 build tools (Latest)**.

A `.vsconfig` file is included in the root of the _dotnet/runtime_ repository that includes all components needed to build the _dotnet/runtime_ repository. You can [import `.vsconfig` in your Visual Studio installer](https://docs.microsoft.com/visualstudio/install/import-export-installation-configurations?view=vs-2022#import-a-configuration) to install all necessary components.

### Build Tools

These steps are required only in case the tools have not been installed as Visual Studio **Individual Components** (described above).

#### CMake

* Install [CMake](https://cmake.org/download) for Windows.
* Add its location (e.g. C:\Program Files (x86)\CMake\bin) to the PATH environment variable. The installation script has a check box to do this, but you can do it yourself after the fact following the instructions at [Adding to the Default PATH variable](#adding-to-the-default-path-variable).

The _dotnet/runtime_ repository recommends using CMake 3.16.4 or newer, but it may work with CMake 3.15.5.

#### Ninja

* Install Ninja in one of the three following ways
  * Ninja is included with Visual Studio. ARM64 Windows should use this method as other options are currently not available for ARM64.
  * [Download the executable](https://github.com/ninja-build/ninja/releases) and add its location to [the Default PATH variable](#adding-to-the-default-path-variable).
  * [Install via a package manager](https://github.com/ninja-build/ninja/wiki/Pre-built-Ninja-packages), which should automatically add it to the PATH environment variable.

#### Python

* Install [Python](https://www.python.org/downloads/) for Windows.
* Add its location (e.g. C:\Python*\\) to the PATH environment variable.
  The installation script has a check box to do this, but you can do it yourself after the fact following the instructions at [Adding to the Default PATH variable](#adding-to-the-default-path-variable).

The _dotnet/runtime_ repository requires at least Python 3.7.4.

### Git

* Install [Git](https://git-for-windows.github.io/) for Windows.
* Add its location (e.g. C:\Program Files\Git\cmd) to the PATH environment variable.
  The installation script has a check box to do this, but you can do it yourself after the fact following the instructions at [Adding to the Default PATH variable](#adding-to-the-default-path-variable).

The _dotnet/runtime_ repository requires at least Git 2.22.0.

### PowerShell

* Ensure that `powershell.exe` is accessible via the PATH environment variable. Typically this is `%SYSTEMROOT%\System32\WindowsPowerShell\v1.0\` and its automatically set upon Windows installation.
* Powershell version must be 3.0 or higher. Use `$PSVersionTable.PSVersion` to determine the engine version.

### .NET SDK

While not strictly needed to build or test this repository, having the .NET SDK installed lets you browse solution files in this repository with Visual Studio and use the `dotnet.exe` command to run .NET applications in the 'normal' way.

We use this in the [build testing with the installed SDK](/docs/workflow/testing/using-your-build-with-installed-sdk.md), and [build testing with dev shipping packages](/docs/workflow/testing/using-dev-shipping-packages.md) instructions. The minimum required version of the SDK is specified in the [global.json file](https://github.com/dotnet/runtime/blob/main/global.json#L3). You can find the installers and binaries for latest development builds of .NET SDK in the [installer repo](https://github.com/dotnet/installer#installers-and-binaries).

Alternatively, to avoid modifying your machine state, you can use the repository's locally acquired SDK by passing in the solution to load via the `-vs` switch. For example:

```cmd
.\build.cmd -vs System.Text.RegularExpressions
```

This will set the `DOTNET_ROOT` and `PATH` environment variables to point to the locally acquired SDK under `runtime\.dotnet` and will launch the Visual Studio instance that is registered for the `sln` extension.

### Adding to the default PATH variable

The commands above need to be on your command lookup path. Some installers will automatically add them to the path as part of the installation, but if not, here is how you can do it.

You can also temporarily add a directory to the PATH environment variable with the command-prompt syntax `set PATH=%PATH%;DIRECTORY_TO_ADD_TO_PATH`. If you're working with Powershell, then the syntax would be `$Env:PATH += ";DIRECTORY_TO_ADD_TO_PATH"`. However, this change will only last until the command windows close.

You can make your change to the PATH variable persistent by going to _Control Panel -> System And Security -> System -> Advanced system settings -> Environment Variables_, and select the `Path` variable under `System Variables` (if you want to change it for all users) or `User Variables` (if you only want to change it for the current user).

Simply edit the PATH variable's value and add the directory (with a semicolon separator).
