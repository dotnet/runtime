Build CoreCLR on Windows
========================

These instructions will lead you through building CoreCLR.

----------------
# Environment

You must install several components to build the CoreCLR and CoreFX repos. These instructions were tested on Windows 10 Pro, version 1903.

## Visual Studio

- Install [Visual Studio 2019](https://visualstudio.microsoft.com/downloads/). The Community version is completely free.

Visual Studio 2019 installation process:
* Its recommended to use 'Workloads' installation approach. The following are the minimum requirements:
  * .NET Desktop Development with all default components.
  * Desktop Development with C++ with all default components.
* To build for Arm32, Make sure that you have the Windows 10 SDK installed (or selected to be installed as part of VS installation). To explicitly install Windows SDK, download it from here: [Windows SDK for Windows 10](https://developer.microsoft.com/en-us/windows/downloads).
  * In addition, ensure you install the ARM tools. In the "Individual components" window, in the "Compilers, build tools, and runtimes" section, check the box for "Visual C++ compilers and libraries for ARM".
* To build the tests, make sure you have a Windows 10 SDK component for at least version 10.0.17763 or newer. This component is installed by default as a part of 'Desktop Development with C++' workload.
* **Important:** You must have the `msdia120.dll` COM Library registered in order to build the repository.
  * This binary is registered by default when installing the "VC++ Tools" with Visual Studio 2019.
  * You can also manually register the binary by launching the "Developer Command Prompt for VS2019" with Administrative privileges and running `regsvr32.exe "%VSINSTALLDIR%\Common7\IDE\msdia120.dll"`.

The CoreCLR repo build has been validated using Visual Studio 2019 16.1.6.

## CMake

- Install [CMake](http://www.cmake.org/download) for Windows.
- Add its location (e.g. C:\Program Files (x86)\CMake\bin) to the PATH environment variable.
  The installation script has a check box to do this, but you can do it yourself after the fact following the instructions at [Adding to the Default PATH variable](#adding-to-the-default-path-variable).

The CoreCLR repo build has been validated using CMake 3.15.0.

## Python

- Install [Python](https://www.python.org/downloads/) for Windows.
- Add its location (e.g. C:\Python*\) to the PATH environment variable.
  The installation script has a check box to do this, but you can do it yourself after the fact following the instructions at [Adding to the Default PATH variable](#adding-to-the-default-path-variable).

The CoreCLR repo build has been validated using Python 3.7.4.

## Git

- Install [Git](https://git-for-windows.github.io/) for Windows.
- Add its location (e.g. C:\Program Files\Git\cmd) to the PATH environment variable.
  The installation script has a check box to do this, but you can do it yourself after the fact following the instructions at [Adding to the Default PATH variable](#adding-to-the-default-path-variable).

The CoreCLR repo build has been validated using Git 2.22.0.

## PowerShell

- Ensure that it is accessible via the PATH environment variable. Typically this is `%SYSTEMROOT%\System32\WindowsPowerShell\v1.0\` and its automatically set upon Windows installation.
- Powershell version must be 3.0 or higher. Use `$PSVersionTable.PSVersion` to determine the engine version.

The CoreCLR repo build has been validated using PowerShell 5.1.

## DotNet Core SDK

While not strictly needed to build or test the .NET Core repository, having the .NET Core SDK installed lets you use the dotnet.exe command to run .NET Core applications in the 'normal' way.   We use this in the
[Using Your Build](../workflow/UsingYourBuild.md) instructions.  Visual Studio should have
installed the .NET Core SDK, but in case it did not you can get it from the [Installing the .NET Core SDK](https://dotnet.microsoft.com/download) page.

## Adding to the default PATH variable

The commands above need to be on your command lookup path.   Some installers will automatically add them to the path as part of the installation, but if not here is how you can do it.

You can, of course, add a directory to the PATH environment variable with the syntax `set PATH=%PATH%;DIRECTORY_TO_ADD_TO_PATH`.

However, the change above will only last until the command windows close.   You can make your change to
the PATH variable persistent by going to  Control Panel -> System And Security -> System -> Advanced system settings -> Environment Variables,
and select the 'Path' variable in the 'System variables' (if you want to change it for all users) or 'User variables' (if you only want
to change it for the current user).  Simply edit the PATH variable's value and add the directory (with a semicolon separator).

-------------------------------------
# Building

Once all the necessary tools are in place, building is trivial.  Simply run build build.cmd script that lives at
the base of the repository.

```bat
    .\build

    [Lots of build spew]

    Product binaries are available at C:\git\coreclr\bin\Product\Windows_NT.x64.debug
    Test binaries are available at C:\git\coreclr\bin\tests\Windows_NT.x64.debug
```

As shown above, the product will be placed in

- Product binaries will be dropped in `bin\Product\<OS>.<arch>.<flavor>` folder.
- A NuGet package, Microsoft.Dotnet.CoreCLR, will be created under `bin\Product\<OS>.<arch>.<flavor>\.nuget` folder.
- Test binaries will be dropped under `bin\Tests\<OS>.<arch>.<flavor>` folder.

By default, build generates a 'Debug' build type, that has extra checking (assert) compiled into it. You can
also build the 'release' version which does not have these checks.

The build places logs in `bin\Logs` and these are useful when the build fails.

The build places all of its output in the `bin` directory, so if you remove that directory you can force a
full rebuild.

The build has a number of options that you can learn about using build -?.   Some of the more important options are

 * -skiptests - don't build the tests.   This can shorten build times quite a bit, but means you can't run tests.
 * -release - build the 'Release' build type that does not have extra development-time checking compiled in.
 You want this if you are going to do performance testing on your build.

See [Using Your Build](../workflow/UsingYourBuild.md) for instructions on running code with your build.

See [Running Tests](../workflow/RunningTests.md) for instructions on running the tests.
