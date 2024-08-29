# Requirements to Set Up the Build Environment on Windows

- [Essential Tools and Configuration](#essential-tools-and-configuration)
  - [Git for Windows](#git-for-windows)
  - [Enable Long Paths](#enable-long-paths)
  - [Visual Studio](#visual-studio)
    - [Workloads](#workloads)
    - [Individual Development Tools](#individual-development-tools)
- [Additional Tools](#additional-tools)
- [Setting Environment Variables on Windows](#setting-environment-variables-on-windows)
- [Windows Development on ARM64](#windows-development-on-arm64)

To build the runtime repo on *Windows*, you will need to install *Visual Studio*, as well as certain development tools that go with it, independently of the IDE, which are described in the following sections.

## Essential Tools and Configuration

### Git for Windows

- First of all, download and install [Git for Windows](https://git-scm.com/download/win) (minimum required version is 2.22.0).
- The installer by default should add `Git` to your `PATH` environment variable, or at least have a checkbox where you can instruct it to do so. If it doesn't, or you'd prefer to set it later yourself, you can follow the instructions in the [Setting Environment Variables on Windows](#setting-environment-variables-on-windows) section of this doc.

### Enable Long Paths

The runtime repo requires long paths to be enabled both, on Windows itself and on *Git*. To configure them on *Git*, open a terminal with administrator privileges and enter the following command:

```powershell
git config --system core.longpaths true
```

The reason this has to be done is that *Git for Windows* is compiled with **MSYS**, which uses a version of the Windows API that has a filepath limit of 260 characters total, as opposed to the usual limit of 4096 filepath characters on macOS and Linux.

Next, to configure the long paths for Windows itself, follow the instructions provided [in this link](https://learn.microsoft.com/windows/win32/fileio/maximum-file-path-limitation?tabs=registry#enable-long-paths-in-windows-10-version-1607-and-later)

If long paths are not enabled, you might start running into issues since trying to clone the repo. Especially with libraries that have very long filenames, you might get errors like `Unable to create file: Filename too long` during the cloning process.

### Visual Studio

Download and install the [latest version of Visual Studio](https://visualstudio.microsoft.com/downloads/) (minimum version required is VS 2022 17.8). The **Community Edition** is available free of charge. Note that as we ramp up on a given release, the libraries code may start using preview language features. While older versions of the IDE may still succeed in building the projects, the IDE may report mismatched diagnostics in the *Errors and Warnings* window. Using the latest public preview of Visual Studio fixes these cases and helps ensure the IDE experience is well behaved and displays what we would expect it to properly.

Note that Visual Studio and its development tools are required, regardless of whether you plan to use the IDE or not.

#### Workloads

It is highly recommended to use the *Workloads* approach, as that installs the full bundles, which include all the necessary tools for the repo to work properly. Open up *Visual Studio Installer*, and click on *Modify* on the Visual Studio installation you plan to use. There, click on the *Workloads* tab (usually selected by default), and install the following bundles:

- .NET desktop development
- Desktop development with C++

To build the tests and do ARM32/ARM64 development, you'll need some additional individual components. You can find them by clicking on the *Individual components* tab in the *Visual Studio Installer*:

- For ARM stuff: *MSVC v143 - VS 2022 C++ ARM64/ARM64EC build tools (Latest)* for Arm64, and *MSVC v143 - VS 2022 C++ ARM build tools (Latest)* for Arm32.
- For building tests: *C++/CLI support for v143 build tools (Latest)*

Alternatively, there is also a `.vsconfig` file included at the root of the runtime repo. It includes all the necessary components required, outlined in a JSON format that Visual Studio can read and parse. You can boot up Visual Studio directly and [import this `.vsconfig` file](https://learn.microsoft.com/visualstudio/install/import-export-installation-configurations?view=vs-2022#import-a-configuration) instead of installing the workloads yourself. It is worth mentioning however, that while we are very careful in maintaining this file up-to-date, sometimes it might get a tad obsolete and miss important components. So, it is always a good idea to double check that the full workloads are installed.

#### Individual Development Tools

All the tools you need should've been installed by Visual Studio at this point. Some of those tools, however, may not have been installed or you might prefer installing them yourself from their own sources. See the following list for instructions on how to do this.

*CMake*

- You can download CMake for Windows [from their website](https://cmake.org/download/) (minimum required version is 3.20).
- Just like with *Git*, its installer should prompt you to add it to your `PATH` environment variable. If it doesn't, or you need to do it later yourself, you can follow the instructions in the [Setting Environment Variables on Windows](#setting-environment-variables-on-windows) section of this doc.

**NOTE:** If you plan on using *MSBuild* instead of *Ninja* to build the native components, the minimum required CMake version is 3.21 instead. This is because the VS2022 generator doesn't exist in CMake until said version.

## Additional Tools

## Setting Environment Variables on Windows

## Windows Development on ARM64
