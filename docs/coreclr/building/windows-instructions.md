Build CoreCLR on Windows
========================

These instructions will lead you through building CoreCLR.

----------------
# Environment

You must install several components to build the CoreCLR and CoreFX repos. These instructions were tested on Windows 7+.

## Visual Studio

Visual Studio must be installed. Supported versions:
- [Visual Studio 2017](https://www.visualstudio.com/downloads/) (Community, Professional, Enterprise).  The community version is completely free.  
- [Visual Studio 2019 Preview](https://visualstudio.microsoft.com/vs/preview/) (Community, Professional, Enterprise).  The community version is completely free.  

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
  * Under ".NET":
    * .NET Framework 4.6 targeting pack
    * .NET Portable Library targeting pack
  * Under "Code tools":
    * Static analysis tools
  * Under "Compilers, build tools, and runtimes":
    * C# and Visual Basic Roslyn Compilers
    * MSBuild
    * VC++ 2015.3 v140 toolset (x86, x64)
    * VC++ 2017 v141 toolset (x86, x64)
    * Windows Universal CRT SDK
  * Under "Development activities":
    * Visual Studio C++ core features
  * Under "SDKs, libraries, and frameworks":
    * Windows 10 SDK or Windows 8.1 SDK
* To build for Arm32, Make sure that you have the Windows 10 SDK installed (or selected to be installed as part of VS installation). To explicitly install Windows SDK, download it from here: [Windows SDK for Windows 10](https://developer.microsoft.com/en-us/windows/downloads).
  * In addition, ensure you install the ARM tools. In the "Individual components" window, in the "Compilers, build tools, and runtimes" section, check the box for "Visual C++ compilers and libraries for ARM".
* To build the tests, make sure you have a Windows 10 SDK for at least version 10.0.17763 or newer.
* **Important:** You must have the `msdia120.dll` COM Library registered in order to build the repository.
  * This binary is registered by default when installing the "VC++ Tools" with Visual Studio 2015
  * You can also manually register the binary by launching the "Developer Command Prompt for VS2017" with Administrative privileges and running `regsvr32.exe "%VSINSTALLDIR%\Common7\IDE\msdia120.dll"`

Visual Studio Express is not supported.

## CMake

The CoreCLR repo build has been validated using CMake 3.9.3. When using Visual Studio 2019 at least version 3.14.1 is required. 

- Install [CMake](http://www.cmake.org/download) for Windows.
- Add its location (e.g. C:\Program Files (x86)\CMake\bin) to the PATH environment variable.  
  The installation script has a check box to do this, but you can do it yourself after the fact 
  following the instructions at [Adding to the Default PATH variable](#adding-to-the-default-path-variable)
  

## Python

Python is used in the build system. We are currently using Python 2.7.9, although
any recent (2.4+) version of Python should work, including Python 3.
- Install [Python](https://www.python.org/downloads/) for Windows.
- Add its location (e.g. C:\Python*\) to the PATH environment variable.  
  The installation script has a check box to do this, but you can do it yourself after the fact 
  following the instructions at [Adding to the Default PATH variable](#adding-to-the-default-path-variable)

## Git

For actual user operations, it is often more convenient to use the GIT features built into Visual Studio.
However the CoreCLR and the tests use the GIT command line utilities directly so you need to install them
for these to work properly.   You can get it from 

- Install [Git For Windows](https://git-for-windows.github.io/)
- Add its location (e.g. C:\Program Files\Git\cmd) to the PATH environment variable.  
  The installation script has a check box to do this, but you can do it yourself after the fact 
  following the instructions at [Adding to the Default PATH variable](#adding-to-the-default-path-variable)

## PowerShell
PowerShell is used in the build system. Ensure that it is accessible via the PATH environment variable.
Typically this is %SYSTEMROOT%\System32\WindowsPowerShell\v1.0\.

Powershell version must be 3.0 or higher. This should be the case for Windows 8 and later builds.
- Windows 7 SP1 can install Powershell version 4 [here](https://www.microsoft.com/en-us/download/details.aspx?id=40855).

## DotNet Core SDK
While not strictly needed to build or test the .NET Core repository, having the .NET Core SDK installed lets you use the dotnet.exe command to run .NET Core applications in the 'normal' way.   We use this in the 
[Using Your Build](../workflow/UsingYourBuild.md) instructions.  Visual Studio should have
installed the .NET Core SDK, but in case it did not you can get it from the [Installing the .NET Core SDK](https://dotnet.microsoft.com/download) page.  

## Adding to the default PATH variable

The commands above need to be on your command lookup path.   Some installers will automatically add them to the path as part of the installation, but if not here is how you can do it.  

You can, of course, add a directory to the PATH environment variable with the syntax
```
    set PATH=%PATH%;DIRECTORY_TO_ADD_TO_PATH
```
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
- Test binaries will be dropped under `bin\Tests\<OS>.<arch>.<flavor>` folder

By default, build generates a 'Debug' build type, that has extra checking (assert) compiled into it. You can
also build the 'release' version which does not have these checks

The build places logs in `bin\Logs` and these are useful when the build fails.

The build places all of its output in the `bin` directory, so if you remove that directory you can force a 
full rebuild.    

The build has a number of options that you can learn about using build -?.   Some of the more important options are

 * -skiptests - don't build the tests.   This can shorten build times quite a bit, but means you can't run tests.
 * -release - build the 'Release' build type that does not have extra development-time checking compiled in.
 You want this if you are going to do performance testing on your build. 

See [Using Your Build](../workflow/UsingYourBuild.md) for instructions on running code with your build.  

See [Running Tests](../workflow/RunningTests.md) for instructions on running the tests.  
