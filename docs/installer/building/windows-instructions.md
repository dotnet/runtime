Build Core-Setup on Windows
========================

These instructions will lead you through building Core-Setup.

----------------
# Environment

You must install several components to build the Core-Setup repo. These instructions were tested on Windows 8+.

## Visual Studio

Visual Studio must be installed. Supported versions:
- [Visual Studio 2019 RC](https://visualstudio.microsoft.com/downloads/#2019rc) (Community, Professional, Enterprise).  The community version is completely free.
- [Visual Studio 2017](https://visualstudio.microsoft.com/vs/) (Community, Professional, Enterprise).  The community version is completely free.

For Visual Studio:
* Required installer options that need to be manually enabled:
  * Universal Windows App Development Tools: Tools and Windows 10 SDK (10.0.14393) + Windows 10 SDK (10.0.10586)
  * Visual C++

Visual Studio Express is not supported.

## CMake

The Core-Setup repo build has been validated using CMake 3.6.3.
If using Visual Studio 2019, then at least CMake 3.14 is required.

- Install [CMake](http://www.cmake.org/download) for Windows.
- Add its location (e.g. C:\Program Files (x86)\CMake\bin) to the PATH environment variable.  
  The installation script has a check box to do this, but you can do it yourself after the fact 
  following the instructions at [Adding to the Default PATH variable](#adding-to-the-default-path-variable)
  

## Git

For actual user operations, it is often convenient to use the Git features built
into your editor or IDE. However, Core-Setup and the tests use the Git command
line utilities directly, so you need to set them up for the build to work
properly. You can get Git from:

- Install [Git For Windows](https://git-for-windows.github.io/)
- Add its location (e.g. C:\Program Files\Git\cmd) to the PATH environment variable.  
  The installation script has a check box to do this, but you can do it yourself after the fact 
  following the instructions at [Adding to the Default PATH variable](#adding-to-the-default-path-variable)

## PowerShell
PowerShell is used in the build system. Ensure that it is accessible via the PATH environment variable.
Typically this is %SYSTEMROOT%\System32\WindowsPowerShell\v1.0\.

Powershell version must be 3.0 or higher. This should be the case for Windows 8 and later builds.
- Windows 7 SP1 can install Powershell version 4 [here](https://www.microsoft.com/en-us/download/details.aspx?id=40855).

## Adding to the default PATH variable

The commands above need to be on your command lookup path.   Some installers will automatically add them to 
the path as part of installation, but if not here is how you can do it.  

You can of course add a directory to the PATH environment variable with the syntax
```
    set PATH=%PATH%;DIRECTORY_TO_ADD_TO_PATH
```
However the change above will only last until the command windows closes.   You can make your change to
the PATH variable persistent by going to  Control Panel -> System And Security -> System -> Advanced system settings -> Environment Variables, 
and select the 'Path' variable in the 'System variables' (if you want to change it for all users) or 'User variables' (if you only want
to change it for the current user).  Simply edit the PATH variable's value and add the directory (with a semicolon separator).

-------------------------------------
# Building

Once all the necessary tools are in place, building is trivial.  Simply run the
`build.cmd` script that lives at the base of the repository.

If you want to build a subset of the build because `build.cmd` takes too long,
try using the `Subset` property. For example, adding `/p:Subset=CoreHost` to
your build command makes it only build the native project. Read the
documentation in [Subsets.props](/Subsets.props) and try `/p:Subset=help` for
more info.
