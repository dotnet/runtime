# Requirements to Set Up the Build Environment on Windows

- [Tools and Configuration](#tools-and-configuration)
  - [Git for Windows](#git-for-windows)
  - [Enable Long Paths](#enable-long-paths)
  - [Visual Studio](#visual-studio)
    - [Workloads](#workloads)
    - [Individual Development Tools](#individual-development-tools)
  - [Powershell](#powershell)
  - [The .NET SDK](#the-net-sdk)
- [Setting Environment Variables on Windows](#setting-environment-variables-on-windows)

To build the runtime repo on *Windows*, you will need to install *Visual Studio*, as well as certain development tools that go with it, independently of the IDE, which are described in the following sections.

## Tools and Configuration

### Git for Windows

- First of all, download and install [Git for Windows](https://git-scm.com/download/win) (minimum required version is 2.22.0).
- The installer by default should add `Git` to your `PATH` environment variable, or at least have a checkbox where you can instruct it to do so. If it doesn't, or you'd prefer to set it later yourself, you can follow the instructions in the [Setting Environment Variables on Windows](#setting-environment-variables-on-windows) section of this doc.

### Enable Long Paths

The runtime repo requires long paths to be enabled both, on Windows itself and on *Git*. To configure them on *Git*, open a terminal with administrator privileges and enter the following command:

```powershell
git config --system core.longpaths true
```

The reason this has to be done is that *Git for Windows* is compiled with **MSYS**, which uses a version of the Windows API that has a filepath limit of 260 characters total, as opposed to the usual limit of 4096 on macOS and Linux.

Next, to configure the long paths for Windows itself, follow the instructions provided [in this link](https://learn.microsoft.com/windows/win32/fileio/maximum-file-path-limitation?tabs=registry#enable-long-paths-in-windows-10-version-1607-and-later).

If long paths are not enabled, you might start running into issues since trying to clone the repo. Especially with libraries that have very long filenames, you might get errors like `Unable to create file: Filename too long` during the cloning process.

### Visual Studio

Download and install the [latest version of Visual Studio](https://visualstudio.microsoft.com/downloads/) (minimum version required is VS 2022 17.8). The **Community Edition** is available free of charge. Note that as we ramp up on a given release, the libraries code may start using preview language features. While older versions of the IDE may still succeed in building the projects, the IDE may report mismatched diagnostics in the *Errors and Warnings* window. Using the latest public preview of Visual Studio fixes these cases and helps ensure the IDE experience is well behaved and displays what we would expect it to properly.

Note that Visual Studio and its development tools are required, regardless of whether you plan to use the IDE or not.

#### Workloads

It is highly recommended to use the *Workloads* approach, as that installs the full bundles, which include all the necessary tools for the repo to work properly. Open up *Visual Studio Installer*, and click on *Modify* on the Visual Studio installation you plan to use. There, click on the *Workloads* tab (usually selected by default), and install the following bundles:

- .NET desktop development
- Desktop development with C++

To build the tests and do ARM64 development, you'll need some additional components. You can find them by clicking on the *Individual components* tab in the *Visual Studio Installer*:

- For Arm64: *MSVC v143 - VS 2022 C++ ARM64/ARM64EC build tools (Latest)*
- For building tests: *C++/CLI support for v143 build tools (Latest)*

Alternatively, there is also a `.vsconfig` file included at the root of the runtime repo. It includes all the necessary components required, outlined in a JSON format that Visual Studio can read and parse. You can boot up Visual Studio directly and [import this `.vsconfig` file](https://learn.microsoft.com/visualstudio/install/import-export-installation-configurations?view=vs-2022#import-a-configuration) instead of installing the workloads yourself. It is worth mentioning however, that while we are very careful in maintaining this file up-to-date, sometimes it might get a tad obsolete and miss important components. So, it is always a good idea to double check that the full workloads are installed.

#### Individual Development Tools

All the tools you need should've been installed by Visual Studio at this point. Some of those tools, however, may not have been installed or you might prefer installing them yourself from their own sources. The main process for this is to download their installers and follow their setup. Said installers usually also prompt you to add them automatically to your `PATH` environment variable. If you miss this option, or prefer to set them yourself later on, you can follow the instructions in the [Setting Environment Variables on Windows](#setting-environment-variables-on-windows) section of this doc.

Here are the links where you can download these tools:

- *CMake*: https://cmake.org/download (minimum required version is 3.20)
- *Ninja*: https://github.com/ninja-build/ninja/releases (latest version is most recommended)
- *Python*: https://www.python.org/downloads/windows (minimum required version is 3.7.4)

**NOTE:** If you plan on using *MSBuild* instead of *Ninja* to build the native components, then the minimum required CMake version is 3.21 instead. This is because the VS2022 generator doesn't exist in CMake until said version.

### Powershell

The runtime repo also uses some `powershell` scripts as part of the Windows builds, so ensure it is accessible via your `PATH` environment variable. It is located in `%SYSTEMROOT%\System32\WindowsPowerShell\v1.0` and should be all set since you first installed Windows, but it never hurts to double check.

<!-- TODO: Talk about the new Powershell, which is multi-platform and is in active development, as opposed to Windows Powershell that is in just maintenance mode now. -->
The minimum required version is 3.0, and your Windows installation should have it. You can verify this by checking the `$PSVersionTable.PSVersion` variable in a Powershell terminal.

### The .NET SDK

While not strictly needed to build or test this repository, having the .NET SDK installed lets you browse solution files in the codebase with Visual Studio and use the `dotnet.exe` command to build and run .NET applications in the 'normal' way.

We use this in the [build testing with the installed SDK](/docs/workflow/testing/using-your-build-with-installed-sdk.md), and [build testing with dev shipping packages](/docs/workflow/testing/using-dev-shipping-packages.md) instructions. The minimum required version of the SDK is specified in the [global.json file](https://github.com/dotnet/runtime/blob/main/global.json#L3). You can find the nightly installers and binaries for the latest development builds over in the [SDK repo](https://github.com/dotnet/sdk#installing-the-sdk).

Alternatively, if you would rather avoid modifying your machine state, you can use the repository's locally acquired SDK by passing in the solution to load via the `-vs` switch. For example:

```cmd
.\build.cmd -vs System.Text.RegularExpressions
```

This will set the `DOTNET_ROOT` and `PATH` environment variables to point to the locally acquired SDK under the `.dotnet` directory found at the root of the repo for the duration of this terminal session. Then, it will launch the Visual Studio instance that is registered for the `.slnx` extension, and open the solution you passed as argument to the command-line.

## Installing dependencies with winget

All the tools mentioned above can be installed with the [Windows Package Manager](https://learn.microsoft.com/windows/package-manager/winget/):
```ps1
winget install -e --id Kitware.CMake
winget install -e --id Python.Python.3.11
winget install -e --id Git.Git
winget install -e --id Ninja-build.Ninja
winget install -e --id Microsoft.VisualStudio.2022.Community --override "--add Microsoft.VisualStudio.Workload.NativeDesktop --add Microsoft.VisualStudio.Workload.ManagedDesktop --includeRecommended"
```

## Setting Environment Variables on Windows

As mentioned in the sections above, the commands that run the development tools have to be in your `PATH` environment variable. Their installers usually have the option to do it automatically for you enabled by the default, but if for any reason you need to set them yourself, here is how you can do it. There are two options. You can make them last only for that terminal instance, or you can set them directly to the system to make them permanent.

**Temporary for the Duration of the Terminal Session**

If you're on *Command Prompt*, issue the following command:

```cmd
set PATH=%PATH%;<path_to_directory_you_want_to_add>
```

If you're on *Powershell*, then the command looks like this:

```powershell
$Env:PATH += ";<path_to_directory_you_want_to_add>"
```

**Permanently on the System**

To make your environment variables changes persistent, open *Control Panel*. There, click on *System and Security* -> *System* -> *Advanced System Settings* -> *Environment Variables*. Then, there you'll notice there are two `PATH` environment variables: One under `User Variables`, and one under `System Variables`. If you want to make the changes persistent only for your current user, then edit the former one, and if you want them to spread across all accounts in that machine, then edit the latter one.
