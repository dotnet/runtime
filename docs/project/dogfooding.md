# Dogfooding nightly builds of .NET

This document provides the steps necessary to consume a nightly build of .NET runtime and SDK.

## Obtaining nightly builds of NuGet packages

If you are only looking to get fixes for an individual NuGet package, and don't need a preview version of the entire runtime, you can add the nightly build package feed to your `NuGet.config` file.  The easiest way to do this is by using the dotnet CLI:

**(Recommended)** Create a local NuGet.Config file for your solution, if don't already have one.  Using a local NuGet.Config file will enable the nightly feed as a package source for projects in the current directory only.
```
dotnet new nugetconfig
```

Next, add the package source to NuGet.Config with the [dotnet nuget add source](https://docs.microsoft.com/en-us/dotnet/core/tools/dotnet-nuget-add-source) command:
```
dotnet nuget add source -n dotnet6 https://dnceng.pkgs.visualstudio.com/public/_packaging/dotnet6/nuget/v3/index.json
```

Then, you will be able to add the latest prerelease version of the desired package to your project.

**Example:** To add version 6.0.0-alpha.1.20468.7 of the System.Data.OleDb package, use the [dotnet add package](https://docs.microsoft.com/en-us/dotnet/core/tools/dotnet-add-package) command:
```
dotnet add package System.Data.OleDb -v 6.0.0-alpha.1.20468.7
```

To use nightly builds of the entire runtime, follow the steps given in the rest of this document instead.

## Install prerequisites

1. Acquire the latest nightly .NET SDK by downloading and extracting a zip/tarball or using an installer from the [installers and binaries table in dotnet/installer](https://github.com/dotnet/installer#installers-and-binaries) (for example, https://aka.ms/dotnet/6.0/daily/dotnet-sdk-win-x64.zip).

2. By default, the dotnet CLI will use the globally installed SDK if it matches the major/minor version you request and has a higher revision. To force it to use a locally installed SDK, you must set an environment variable `DOTNET_MULTILEVEL_LOOKUP=0` in your shell. You can use `dotnet --info` to verify what version of the Shared Framework it is using.

3. Reminder: if you are using a local copy of the dotnet CLI, take care that when you type `dotnet` you do not inadvertently pick up a different copy that you may have in your path. On Windows, for example, if you use a Command Prompt, a global copy may be in the path, so use the fully qualified path to your local `dotnet` (e.g. `C:\dotnet\dotnet.exe`). If you receive an error "error NETSDK1045:  The current .NET SDK does not support targeting .NET 6.0." then you may be executing an older `dotnet`.

After setting up dotnet you can verify you are using the dogfooding version by executing `dotnet --info`. Here is an example output at the time of writing:
```
>dotnet --info
.NET SDK (reflecting any global.json):
 Version:   6.0.100-alpha.1.20514.11
 Commit:    69ee2fdd13

Runtime Environment:
 OS Name:     Windows
 OS Version:  10.0.19042
 OS Platform: Windows
 RID:         win10-x64
 Base Path:   c:\dotnet\sdk\6.0.100-alpha.1.20514.11\

Host (useful for support):
  Version: 6.0.0-alpha.1.20468.7
  Commit:  a820ca1c4f

.NET Core SDKs installed:
  6.0.100-alpha.1.20514.11 [c:\dotnet\sdk]

.NET Core runtimes installed:
  Microsoft.AspNetCore.App 5.0.0-rc.2.20466.8 [c:\dotnet\shared\Microsoft.AspNetCore.App]
  Microsoft.NETCore.App 6.0.0-alpha.1.20468.7 [c:\dotnet\shared\Microsoft.NETCore.App]
  Microsoft.WindowsDesktop.App 5.0.0-rc.1.20417.4 [c:\dotnet\shared\Microsoft.WindowsDesktop.App]

To install additional .NET Core runtimes or SDKs:
  https://aka.ms/dotnet-download
```

4. Our nightly builds are uploaded to dotnet-blob feeds, not NuGet - so ensure the .NET Core blob feed is in your nuget configuration in case you need other packages from .NET Core that aren't included in the download. For example, on Windows you could edit `%userprofile%\appdata\roaming\nuget\nuget.config` or on Linux edit `~/.nuget/NuGet/NuGet.Config` to add these lines:
```xml
<packageSources>
    <add key="dotnet6" value="https://dnceng.pkgs.visualstudio.com/public/_packaging/dotnet6/nuget/v3/index.json" />
    <add key="gRPC repository" value="https://grpc.jfrog.io/grpc/api/nuget/v3/grpc-nuget-dev" />
    ...
</packageSources>
```
(Documentation for configuring feeds is [here](https://docs.microsoft.com/en-us/nuget/consume-packages/configuring-nuget-behavior).)

## Setup the project

1. Create a new project
    - Create a new folder for your app and change to that folder
    - Create project file by running `dotnet new console`

2. Restore packages so that you're ready to play:

```
$ dotnet restore
```

## Consume the new build

```
$ dotnet run
```

Rinse and repeat!

## Advanced Scenario - Using a nightly build of Microsoft.NETCore.App

When using the above instructions, your application will run against the same
.NET runtime that comes with the SDK. That works fine to get up and
running quickly. However, there are times when you need to use a nightly build
of Microsoft.NETCore.App which hasn't made its way into the SDK yet. To enable
this, there are two options you can take.

### Option 1: Framework-dependent

This is the default case for applications - running against an installed .NET runtime.

1. You still need to install the prerequisite .NET SDK from above.
2. Optionally, install the specific .NET runtime you require globally or download get the latest one available from the [nightly build table](#nightly-builds-table)
3. Modify your .csproj to reference the nightly build of Microsoft.NETCore.App

```XML
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <!-- Ensure that the target framework is correct e.g. 'net6.0' -->
    <TargetFramework>net6.0</TargetFramework>
    <!-- modify version in this line with one reported by `dotnet --info` under ".NET runtimes installed" -> Microsoft.NETCore.App -->
    <RuntimeFrameworkVersion>6.0.0-alpha.1.20468.7</RuntimeFrameworkVersion>
  </PropertyGroup>
```

```
$ dotnet restore
$ dotnet run
```

### Option 2: Self-contained

In this case, the .NET runtime will be published along with your application.

1. You still need to install the prerequisite .NET SDK from above.
2. Modify your .csproj to reference the nightly build of Microsoft.NETCore.App *and*
make it self-contained by adding a RuntimeIdentifier (RID).

```XML
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <!-- Ensure that the target framework is correct e.g. 'net6.0' -->
    <TargetFramework>net6.0</TargetFramework>
    <!-- modify build in this line with version reported by `dotnet --info` as above under ".NET runtimes installed" -> Microsoft.NETCore.App -->
    <!-- moreover, this can be any valid Microsoft.NETCore.App package version from https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet6/nuget/v3/index.json -->
    <RuntimeFrameworkVersion>6.0.0-alpha.1.20468.7</RuntimeFrameworkVersion>
    <RuntimeIdentifier>win-x64</RuntimeIdentifier> <!-- RID to make it self-contained -->
  </PropertyGroup>
```

```
$ dotnet restore
$ dotnet publish
$ bin\Debug\net6.0\win-x64\publish\App.exe
```

### Nightly builds table

<!--
  To update this table, run 'build.sh/cmd RegenerateDownloadTable'. See
  'tools-local/regenerate-readme-table.proj' to add or remove rows or columns,
  and add links below to fill out the table's contents.
-->
<!-- BEGIN generated table -->

| Platform | Main |
| --- |  :---: |
| **Windows (x64)** | <br>[Installer][win-x64-installer-6.0.X] ([Checksum][win-x64-installer-checksum-6.0.X])<br>[zip][win-x64-zip-6.0.X] ([Checksum][win-x64-zip-checksum-6.0.X]) |
| **Windows (x86)** | <br>[Installer][win-x86-installer-6.0.X] ([Checksum][win-x86-installer-checksum-6.0.X])<br>[zip][win-x86-zip-6.0.X] ([Checksum][win-x86-zip-checksum-6.0.X]) |
| **Windows (arm64)** | <br>[Installer][win-arm64-installer-6.0.X] ([Checksum][win-arm64-installer-checksum-6.0.X])<br>[zip][win-arm64-zip-6.0.X] ([Checksum][win-arm64-zip-checksum-6.0.X]) |
| **macOS (x64)** | <br>[Installer][osx-x64-installer-6.0.X] ([Checksum][osx-x64-installer-checksum-6.0.X])<br>[tar.gz][osx-x64-targz-6.0.X] ([Checksum][osx-x64-targz-checksum-6.0.X]) |
| **macOS (arm64)** | <br>[Installer][osx-arm64-installer-6.0.X] ([Checksum][osx-arm64-installer-checksum-6.0.X])<br>[tar.gz][osx-arm64-targz-6.0.X] ([Checksum][osx-arm64-targz-checksum-6.0.X]) |
| **Linux (x64)** (for glibc based OS) | <br>[tar.gz][linux-x64-targz-6.0.X] ([Checksum][linux-x64-targz-checksum-6.0.X]) |
| **Linux (armhf)** (for glibc based OS) | <br>[tar.gz][linux-arm-targz-6.0.X] ([Checksum][linux-arm-targz-checksum-6.0.X]) |
| **Linux (arm64)** (for glibc based OS) | <br>[tar.gz][linux-arm64-targz-6.0.X] ([Checksum][linux-arm64-targz-checksum-6.0.X]) |
| **Linux-musl (x64)** | <br>[tar.gz][linux-musl-x64-targz-6.0.X] ([Checksum][linux-musl-x64-targz-checksum-6.0.X]) |
| **Linux-musl (arm)** | <br>[tar.gz][linux-musl-arm-targz-6.0.X] ([Checksum][linux-musl-arm-targz-checksum-6.0.X]) |
| **Linux-musl (arm64)** | <br>[tar.gz][linux-musl-arm64-targz-6.0.X] ([Checksum][linux-musl-arm64-targz-checksum-6.0.X]) |
| **Dpkg Based Systems (x64)** | <br>[Runtime-Deps][deb-runtime-deps-6.0.X] ([Checksum][deb-runtime-deps-checksum-6.0.X])<br>[Host][deb-host-6.0.X] ([Checksum][deb-host-checksum-6.0.X])<br>[App Hosts][deb-apphost-pack-6.0.X] ([Checksum][deb-apphost-pack-checksum-6.0.X])<br>[Host FX Resolver][deb-hostfxr-6.0.X] ([Checksum][deb-hostfxr-checksum-6.0.X])<br>[Targeting Pack][deb-targeting-pack-6.0.X] ([Checksum][deb-targeting-pack-checksum-6.0.X])<br>[Shared Framework][deb-sharedfx-6.0.X] ([Checksum][deb-sharedfx-checksum-6.0.X]) |
| **CentOS 7 (x64)** | <br>[Runtime-Deps][centos-7-runtime-deps-6.0.X] ([Checksum][centos-7-runtime-deps-checksum-6.0.X])<br>[Host][centos-7-host-6.0.X] ([Checksum][centos-7-host-checksum-6.0.X])<br>[App Hosts][centos-7-apphost-pack-6.0.X] ([Checksum][centos-7-apphost-pack-checksum-6.0.X])<br>[Host FX Resolver][centos-7-hostfxr-6.0.X] ([Checksum][centos-7-hostfxr-checksum-6.0.X])<br>[Targeting Pack][centos-7-targeting-pack-6.0.X] ([Checksum][centos-7-targeting-pack-checksum-6.0.X])<br>[Shared Framework][centos-7-sharedfx-6.0.X] ([Checksum][centos-7-sharedfx-checksum-6.0.X]) |
| **RHEL 7.2 (x64)** | <br>[Host][rhel7-host-6.0.X] ([Checksum][rhel7-host-checksum-6.0.X])<br>[App Hosts][rhel7-apphost-pack-6.0.X] ([Checksum][rhel7-apphost-pack-checksum-6.0.X])<br>[Host FX Resolver][rhel7-hostfxr-6.0.X] ([Checksum][rhel7-hostfxr-checksum-6.0.X])<br>[Targeting Pack][rhel7-targeting-pack-6.0.X] ([Checksum][rhel7-targeting-pack-checksum-6.0.X])<br>[Shared Framework][rhel7-sharedfx-6.0.X] ([Checksum][rhel7-sharedfx-checksum-6.0.X]) |
| **Fedora 27 (x64)** | <br>[Runtime-Deps][fedora-27-runtime-deps-6.0.X] ([Checksum][fedora-27-runtime-deps-checksum-6.0.X])<br>[Host][fedora-27-host-6.0.X] ([Checksum][fedora-27-host-checksum-6.0.X])<br>[App Hosts][fedora-27-apphost-pack-6.0.X] ([Checksum][fedora-27-apphost-pack-checksum-6.0.X])<br>[Host FX Resolver][fedora-27-hostfxr-6.0.X] ([Checksum][fedora-27-hostfxr-checksum-6.0.X])<br>[Targeting Pack][fedora-27-targeting-pack-6.0.X] ([Checksum][fedora-27-targeting-pack-checksum-6.0.X])<br>[Shared Framework][fedora-27-sharedfx-6.0.X] ([Checksum][fedora-27-sharedfx-checksum-6.0.X]) |
| **SLES 12 (x64)** | <br>[Runtime-Deps][sles-12-runtime-deps-6.0.X] ([Checksum][sles-12-runtime-deps-checksum-6.0.X])<br>[Host][sles-12-host-6.0.X] ([Checksum][sles-12-host-checksum-6.0.X])<br>[App Hosts][sles-12-apphost-pack-6.0.X] ([Checksum][sles-12-apphost-pack-checksum-6.0.X])<br>[Host FX Resolver][sles-12-hostfxr-6.0.X] ([Checksum][sles-12-hostfxr-checksum-6.0.X])<br>[Targeting Pack][sles-12-targeting-pack-6.0.X] ([Checksum][sles-12-targeting-pack-checksum-6.0.X])<br>[Shared Framework][sles-12-sharedfx-6.0.X] ([Checksum][sles-12-sharedfx-checksum-6.0.X]) |
| **OpenSUSE 42 (x64)** | <br>[Runtime-Deps][OpenSUSE-42-runtime-deps-6.0.X] ([Checksum][OpenSUSE-42-runtime-deps-checksum-6.0.X])<br>[Host][OpenSUSE-42-host-6.0.X] ([Checksum][OpenSUSE-42-host-checksum-6.0.X])<br>[App Hosts][OpenSUSE-42-apphost-pack-6.0.X] ([Checksum][OpenSUSE-42-apphost-pack-checksum-6.0.X])<br>[Host FX Resolver][OpenSUSE-42-hostfxr-6.0.X] ([Checksum][OpenSUSE-42-hostfxr-checksum-6.0.X])<br>[Targeting Pack][OpenSUSE-42-targeting-pack-6.0.X] ([Checksum][OpenSUSE-42-targeting-pack-checksum-6.0.X])<br>[Shared Framework][OpenSUSE-42-sharedfx-6.0.X] ([Checksum][OpenSUSE-42-sharedfx-checksum-6.0.X]) |

<!-- END generated table -->

*Note: Our Linux packages (.deb and .rpm) are put together slightly differently than the Windows and Mac specific installers. Instead of combining everything, we have separate component packages that depend on each other. If you're installing these directly from the installer files (via dpkg or similar), then you'll need to install them in the order presented above.*

<!-- BEGIN links to include in table -->

[win-x64-badge-6.0.X]: https://aka.ms/dotnet/6.0/daily/sharedfx_win-x64_Release_version_badge.svg?no-cache
[win-x64-version-6.0.X]: https://aka.ms/dotnet/6.0/daily/runtime-productVersion.txt
[win-x64-installer-6.0.X]: https://aka.ms/dotnet/6.0/daily/dotnet-runtime-win-x64.exe
[win-x64-installer-checksum-6.0.X]: https://aka.ms/dotnet/6.0/daily/dotnet-runtime-win-x64.exe.sha512
[win-x64-zip-6.0.X]: https://aka.ms/dotnet/6.0/daily/dotnet-runtime-win-x64.zip
[win-x64-zip-checksum-6.0.X]: https://aka.ms/dotnet/6.0/daily/dotnet-runtime-win-x64.zip.sha512
[win-x64-nethost-zip-6.0.X]: https://aka.ms/dotnet/6.0/daily/dotnet-nethost-win-x64.zip
[win-x64-symbols-zip-6.0.X]: https://aka.ms/dotnet/6.0/daily/dotnet-runtime-symbols-win-x64.zip

[win-x86-badge-6.0.X]: https://aka.ms/dotnet/6.0/daily/sharedfx_win-x86_Release_version_badge.svg?no-cache
[win-x86-version-6.0.X]: https://aka.ms/dotnet/6.0/daily/runtime-productVersion.txt
[win-x86-installer-6.0.X]: https://aka.ms/dotnet/6.0/daily/dotnet-runtime-win-x86.exe
[win-x86-installer-checksum-6.0.X]: https://aka.ms/dotnet/6.0/daily/dotnet-runtime-win-x86.exe.sha512
[win-x86-zip-6.0.X]: https://aka.ms/dotnet/6.0/daily/dotnet-runtime-win-x86.zip
[win-x86-zip-checksum-6.0.X]: https://aka.ms/dotnet/6.0/daily/dotnet-runtime-win-x86.zip.sha512
[win-x86-nethost-zip-6.0.X]: https://aka.ms/dotnet/6.0/daily/dotnet-nethost-win-x86.zip
[win-x86-symbols-zip-6.0.X]: https://aka.ms/dotnet/6.0/daily/dotnet-runtime-symbols-win-x86.zip

[win-arm64-badge-6.0.X]: https://aka.ms/dotnet/6.0/daily/sharedfx_win-arm64_Release_version_badge.svg?no-cache
[win-arm64-version-6.0.X]: https://aka.ms/dotnet/6.0/daily/runtime-productVersion.txt
[win-arm64-installer-6.0.X]: https://aka.ms/dotnet/6.0/daily/dotnet-runtime-win-arm64.exe
[win-arm64-installer-checksum-6.0.X]: https://aka.ms/dotnet/6.0/daily/dotnet-runtime-win-arm64.exe.sha512
[win-arm64-zip-6.0.X]: https://aka.ms/dotnet/6.0/daily/dotnet-runtime-win-arm64.zip
[win-arm64-zip-checksum-6.0.X]: https://aka.ms/dotnet/6.0/daily/dotnet-runtime-win-arm64.zip.sha512
[win-arm64-nethost-zip-6.0.X]: https://aka.ms/dotnet/6.0/daily/dotnet-nethost-win-arm64.zip
[win-arm64-symbols-zip-6.0.X]: https://aka.ms/dotnet/6.0/daily/dotnet-runtime-symbols-win-arm64.zip

[osx-x64-badge-6.0.X]: https://aka.ms/dotnet/6.0/daily/sharedfx_osx-x64_Release_version_badge.svg?no-cache
[osx-x64-version-6.0.X]: https://aka.ms/dotnet/6.0/daily/runtime-productVersion.txt
[osx-x64-installer-6.0.X]: https://aka.ms/dotnet/6.0/daily/dotnet-runtime-osx-x64.pkg
[osx-x64-installer-checksum-6.0.X]: https://aka.ms/dotnet/6.0/daily/dotnet-runtime-osx-x64.pkg.sha512
[osx-x64-targz-6.0.X]: https://aka.ms/dotnet/6.0/daily/dotnet-runtime-osx-x64.tar.gz
[osx-x64-targz-checksum-6.0.X]: https://aka.ms/dotnet/6.0/daily/dotnet-runtime-osx-x64.tar.gz.sha512
[osx-x64-nethost-targz-6.0.X]: https://aka.ms/dotnet/6.0/daily/dotnet-nethost-osx-x64.tar.gz
[osx-x64-symbols-targz-6.0.X]: https://aka.ms/dotnet/6.0/daily/dotnet-runtime-symbols-osx-x64.tar.gz

[osx-arm64-badge-6.0.X]: https://aka.ms/dotnet/6.0/daily/sharedfx_osx-arm64_Release_version_badge.svg?no-cache
[osx-arm64-version-6.0.X]: https://aka.ms/dotnet/6.0/daily/runtime-productVersion.txt
[osx-arm64-installer-6.0.X]: https://aka.ms/dotnet/6.0/daily/dotnet-runtime-osx-arm64.pkg
[osx-arm64-installer-checksum-6.0.X]: https://aka.ms/dotnet/6.0/daily/dotnet-runtime-osx-arm64.pkg.sha512
[osx-arm64-targz-6.0.X]: https://aka.ms/dotnet/6.0/daily/dotnet-runtime-osx-arm64.tar.gz
[osx-arm64-targz-checksum-6.0.X]: https://aka.ms/dotnet/6.0/daily/dotnet-runtime-osx-arm64.tar.gz.sha512
[osx-arm64-nethost-targz-6.0.X]: https://aka.ms/dotnet/6.0/daily/dotnet-nethost-osx-arm64.tar.gz
[osx-arm64-symbols-targz-6.0.X]: https://aka.ms/dotnet/6.0/daily/dotnet-runtime-symbols-osx-arm64.tar.gz

[linux-x64-badge-6.0.X]: https://aka.ms/dotnet/6.0/daily/sharedfx_linux-x64_Release_version_badge.svg?no-cache
[linux-x64-version-6.0.X]: https://aka.ms/dotnet/6.0/daily/runtime-productVersion.txt
[linux-x64-targz-6.0.X]: https://aka.ms/dotnet/6.0/daily/dotnet-runtime-linux-x64.tar.gz
[linux-x64-targz-checksum-6.0.X]: https://aka.ms/dotnet/6.0/daily/dotnet-runtime-linux-x64.tar.gz.sha512
[linux-x64-nethost-targz-6.0.X]: https://aka.ms/dotnet/6.0/daily/dotnet-nethost-linux-x64.tar.gz
[linux-x64-symbols-targz-6.0.X]: https://aka.ms/dotnet/6.0/daily/dotnet-runtime-symbols-linux-x64.tar.gz

[linux-arm-badge-6.0.X]: https://aka.ms/dotnet/6.0/daily/sharedfx_linux-arm_Release_version_badge.svg?no-cache
[linux-arm-version-6.0.X]: https://aka.ms/dotnet/6.0/daily/runtime-productVersion.txt
[linux-arm-targz-6.0.X]: https://aka.ms/dotnet/6.0/daily/dotnet-runtime-linux-arm.tar.gz
[linux-arm-targz-checksum-6.0.X]: https://aka.ms/dotnet/6.0/daily/dotnet-runtime-linux-arm.tar.gz.sha512
[linux-arm-nethost-targz-6.0.X]: https://aka.ms/dotnet/6.0/daily/dotnet-nethost-linux-arm.tar.gz
[linux-arm-symbols-targz-6.0.X]: https://aka.ms/dotnet/6.0/daily/dotnet-runtime-symbols-linux-arm.tar.gz

[linux-arm64-badge-6.0.X]: https://aka.ms/dotnet/6.0/daily/sharedfx_linux-arm64_Release_version_badge.svg?no-cache
[linux-arm64-version-6.0.X]: https://aka.ms/dotnet/6.0/daily/runtime-productVersion.txt
[linux-arm64-targz-6.0.X]: https://aka.ms/dotnet/6.0/daily/dotnet-runtime-linux-arm64.tar.gz
[linux-arm64-targz-checksum-6.0.X]: https://aka.ms/dotnet/6.0/daily/dotnet-runtime-linux-arm64.tar.gz.sha512
[linux-arm64-nethost-targz-6.0.X]: https://aka.ms/dotnet/6.0/daily/dotnet-nethost-linux-arm64.tar.gz
[linux-arm64-symbols-targz-6.0.X]: https://aka.ms/dotnet/6.0/daily/dotnet-runtime-symbols-linux-arm64.tar.gz

[deb-badge-6.0.X]: https://aka.ms/dotnet/6.0/daily/sharedfx_ubuntu.14.04-x64_Release_version_badge.svg?no-cache
[deb-version-6.0.X]: https://aka.ms/dotnet/6.0/daily/runtime-productVersion.txt
[deb-apphost-pack-6.0.X]: https://aka.ms/dotnet/6.0/daily/dotnet-apphost-pack-x64.deb
[deb-apphost-pack-checksum-6.0.X]: https://aka.ms/dotnet/6.0/daily/dotnet-apphost-pack-x64.deb.sha512
[deb-host-6.0.X]: https://aka.ms/dotnet/6.0/daily/dotnet-host-x64.deb
[deb-runtime-deps-6.0.X]: https://aka.ms/dotnet/6.0/daily/dotnet-runtime-deps-x64.deb
[deb-runtime-deps-checksum-6.0.X]: https://aka.ms/dotnet/6.0/daily/dotnet-runtime-deps-x64.deb.sha512
[deb-host-checksum-6.0.X]: https://aka.ms/dotnet/6.0/daily/dotnet-host-x64.deb.sha512
[deb-hostfxr-6.0.X]: https://aka.ms/dotnet/6.0/daily/dotnet-hostfxr-x64.deb
[deb-hostfxr-checksum-6.0.X]: https://aka.ms/dotnet/6.0/daily/dotnet-hostfxr-x64.deb.sha512
[deb-sharedfx-6.0.X]: https://aka.ms/dotnet/6.0/daily/dotnet-runtime-x64.deb
[deb-sharedfx-checksum-6.0.X]: https://aka.ms/dotnet/6.0/daily/dotnet-runtime-x64.deb.sha512
[deb-targeting-pack-6.0.X]: https://aka.ms/dotnet/6.0/daily/dotnet-targeting-pack-x64.deb
[deb-targeting-pack-checksum-6.0.X]: https://aka.ms/dotnet/6.0/daily/dotnet-targeting-pack-x64.deb.sha512

[rhel7-badge-6.0.X]: https://aka.ms/dotnet/6.0/daily/sharedfx_rhel.7-x64_Release_version_badge.svg?no-cache
[rhel7-version-6.0.X]: https://aka.ms/dotnet/6.0/daily/runtime-productVersion.txt
[rhel7-runtime-deps-checksum-6.0.X]: https://aka.ms/dotnet/6.0/daily/dotnet-runtime-deps-centos.7-x64.rpm
[rhel7-runtime-deps-checksum-6.0.X]: https://aka.ms/dotnet/6.0/daily/dotnet-runtime-deps-centos.7-x64.rpm.sha512
[rhel7-apphost-pack-6.0.X]: https://aka.ms/dotnet/6.0/daily/dotnet-apphost-pack-x64.rpm
[rhel7-apphost-pack-checksum-6.0.X]: https://aka.ms/dotnet/6.0/daily/dotnet-apphost-pack-x64.rpm.sha512
[rhel7-host-6.0.X]: https://aka.ms/dotnet/6.0/daily/dotnet-host-x64.rpm
[rhel7-host-checksum-6.0.X]: https://aka.ms/dotnet/6.0/daily/dotnet-host-x64.rpm.sha512
[rhel7-hostfxr-6.0.X]: https://aka.ms/dotnet/6.0/daily/dotnet-hostfxr-x64.rpm
[rhel7-hostfxr-checksum-6.0.X]: https://aka.ms/dotnet/6.0/daily/dotnet-hostfxr-x64.rpm.sha512
[rhel7-sharedfx-6.0.X]: https://aka.ms/dotnet/6.0/daily/dotnet-runtime-x64.rpm
[rhel7-sharedfx-checksum-6.0.X]: https://aka.ms/dotnet/6.0/daily/dotnet-runtime-x64.rpm.sha512
[rhel7-targeting-pack-6.0.X]: https://aka.ms/dotnet/6.0/daily/dotnet-targeting-pack-x64.rpm
[rhel7-targeting-pack-checksum-6.0.X]: https://aka.ms/dotnet/6.0/daily/dotnet-targeting-pack-x64.rpm.sha512

[centos-7-badge-6.0.X]: https://aka.ms/dotnet/6.0/daily/sharedfx_centos.7-x64_Release_version_badge.svg?no-cache
[centos-7-version-6.0.X]: https://aka.ms/dotnet/6.0/daily/runtime-productVersion.txt
[centos-7-runtime-deps-6.0.X]: https://aka.ms/dotnet/6.0/daily/dotnet-runtime-deps-centos.7-x64.rpm
[centos-7-runtime-deps-checksum-6.0.X]: https://aka.ms/dotnet/6.0/daily/dotnet-runtime-deps-centos.7-x64.rpm.sha512
[centos-7-apphost-pack-6.0.X]: https://aka.ms/dotnet/6.0/daily/dotnet-apphost-pack-x64.rpm
[centos-7-apphost-pack-checksum-6.0.X]: https://aka.ms/dotnet/6.0/daily/dotnet-apphost-pack-x64.rpm.sha512
[centos-7-host-6.0.X]: https://aka.ms/dotnet/6.0/daily/dotnet-host-x64.rpm
[centos-7-host-checksum-6.0.X]: https://aka.ms/dotnet/6.0/daily/dotnet-host-x64.rpm.sha512
[centos-7-hostfxr-6.0.X]: https://aka.ms/dotnet/6.0/daily/dotnet-hostfxr-x64.rpm
[centos-7-hostfxr-checksum-6.0.X]: https://aka.ms/dotnet/6.0/daily/dotnet-hostfxr-x64.rpm.sha512
[centos-7-sharedfx-6.0.X]: https://aka.ms/dotnet/6.0/daily/dotnet-runtime-x64.rpm
[centos-7-sharedfx-checksum-6.0.X]: https://aka.ms/dotnet/6.0/daily/dotnet-runtime-x64.rpm.sha512
[centos-7-targeting-pack-6.0.X]: https://aka.ms/dotnet/6.0/daily/dotnet-targeting-pack-x64.rpm
[centos-7-targeting-pack-checksum-6.0.X]: https://aka.ms/dotnet/6.0/daily/dotnet-targeting-pack-x64.rpm.sha512

[fedora-27-badge-6.0.X]: https://aka.ms/dotnet/6.0/daily/sharedfx_fedora.27-x64_Release_version_badge.svg?no-cache
[fedora-27-version-6.0.X]: https://aka.ms/dotnet/6.0/daily/runtime-productVersion.txt
[fedora-27-runtime-deps-6.0.X]: https://aka.ms/dotnet/6.0/daily/dotnet-runtime-deps-fedora.27-x64.rpm
[fedora-27-runtime-deps-checksum-6.0.X]: https://aka.ms/dotnet/6.0/daily/dotnet-runtime-deps-fedora.27-x64.rpm.sha512
[fedora-27-apphost-pack-6.0.X]: https://aka.ms/dotnet/6.0/daily/dotnet-apphost-pack-x64.rpm
[fedora-27-apphost-pack-checksum-6.0.X]: https://aka.ms/dotnet/6.0/daily/dotnet-apphost-pack-x64.rpm.sha512
[fedora-27-host-6.0.X]: https://aka.ms/dotnet/6.0/daily/dotnet-host-x64.rpm
[fedora-27-host-checksum-6.0.X]: https://aka.ms/dotnet/6.0/daily/dotnet-host-x64.rpm.sha512
[fedora-27-hostfxr-6.0.X]: https://aka.ms/dotnet/6.0/daily/dotnet-hostfxr-x64.rpm
[fedora-27-hostfxr-checksum-6.0.X]: https://aka.ms/dotnet/6.0/daily/dotnet-hostfxr-x64.rpm.sha512
[fedora-27-sharedfx-6.0.X]: https://aka.ms/dotnet/6.0/daily/dotnet-runtime-x64.rpm
[fedora-27-sharedfx-checksum-6.0.X]: https://aka.ms/dotnet/6.0/daily/dotnet-runtime-x64.rpm.sha512
[fedora-27-targeting-pack-6.0.X]: https://aka.ms/dotnet/6.0/daily/dotnet-targeting-pack-x64.rpm
[fedora-27-targeting-pack-checksum-6.0.X]: https://aka.ms/dotnet/6.0/daily/dotnet-targeting-pack-x64.rpm.sha512

[sles-12-badge-6.0.X]: https://aka.ms/dotnet/6.0/daily/sharedfx_sles.12-x64_Release_version_badge.svg?no-cache
[sles-12-version-6.0.X]: https://aka.ms/dotnet/6.0/daily/runtime-productVersion.txt
[sles-12-runtime-deps-6.0.X]: https://aka.ms/dotnet/6.0/daily/dotnet-runtime-deps-sles.12-x64.rpm
[sles-12-runtime-deps-checksum-6.0.X]: https://aka.ms/dotnet/6.0/daily/dotnet-runtime-deps-sles.12-x64.rpm.sha512
[sles-12-apphost-pack-6.0.X]: https://aka.ms/dotnet/6.0/daily/dotnet-apphost-pack-x64.rpm
[sles-12-apphost-pack-checksum-6.0.X]: https://aka.ms/dotnet/6.0/daily/dotnet-apphost-pack-x64.rpm.sha512
[sles-12-host-6.0.X]: https://aka.ms/dotnet/6.0/daily/dotnet-host-x64.rpm
[sles-12-host-checksum-6.0.X]: https://aka.ms/dotnet/6.0/daily/dotnet-host-x64.rpm.sha512
[sles-12-hostfxr-6.0.X]: https://aka.ms/dotnet/6.0/daily/dotnet-hostfxr-x64.rpm
[sles-12-hostfxr-checksum-6.0.X]: https://aka.ms/dotnet/6.0/daily/dotnet-hostfxr-x64.rpm.sha512
[sles-12-sharedfx-6.0.X]: https://aka.ms/dotnet/6.0/daily/dotnet-runtime-x64.rpm
[sles-12-sharedfx-checksum-6.0.X]: https://aka.ms/dotnet/6.0/daily/dotnet-runtime-x64.rpm.sha512
[sles-12-targeting-pack-6.0.X]: https://aka.ms/dotnet/6.0/daily/dotnet-targeting-pack-x64.rpm
[sles-12-targeting-pack-checksum-6.0.X]: https://aka.ms/dotnet/6.0/daily/dotnet-targeting-pack-x64.rpm.sha512

[OpenSUSE-42-badge-6.0.X]: https://aka.ms/dotnet/6.0/daily/sharedfx_opensuse.42-x64_Release_version_badge.svg?no-cache
[OpenSUSE-42-version-6.0.X]: https://aka.ms/dotnet/6.0/daily/runtime-productVersion.txt
[OpenSUSE-42-runtime-deps-6.0.X]: https://aka.ms/dotnet/6.0/daily/dotnet-runtime-deps-opensuse.42-x64.rpm
[OpenSUSE-42-runtime-deps-checksum-6.0.X]: https://aka.ms/dotnet/6.0/daily/dotnet-runtime-deps-opensuse.42-x64.rpm.sha512
[OpenSUSE-42-apphost-pack-6.0.X]: https://aka.ms/dotnet/6.0/daily/dotnet-apphost-pack-x64.rpm
[OpenSUSE-42-apphost-pack-checksum-6.0.X]: https://aka.ms/dotnet/6.0/daily/dotnet-apphost-pack-x64.rpm.sha512
[OpenSUSE-42-host-6.0.X]: https://aka.ms/dotnet/6.0/daily/dotnet-host-x64.rpm
[OpenSUSE-42-host-checksum-6.0.X]: https://aka.ms/dotnet/6.0/daily/dotnet-host-x64.rpm.sha512
[OpenSUSE-42-hostfxr-6.0.X]: https://aka.ms/dotnet/6.0/daily/dotnet-hostfxr-x64.rpm
[OpenSUSE-42-hostfxr-checksum-6.0.X]: https://aka.ms/dotnet/6.0/daily/dotnet-hostfxr-x64.rpm.sha512
[OpenSUSE-42-sharedfx-6.0.X]: https://aka.ms/dotnet/6.0/daily/dotnet-runtime-x64.rpm
[OpenSUSE-42-sharedfx-checksum-6.0.X]: https://aka.ms/dotnet/6.0/daily/dotnet-runtime-x64.rpm.sha512
[OpenSUSE-42-targeting-pack-6.0.X]: https://aka.ms/dotnet/6.0/daily/dotnet-targeting-pack-x64.rpm
[OpenSUSE-42-targeting-pack-checksum-6.0.X]: https://aka.ms/dotnet/6.0/daily/dotnet-targeting-pack-x64.rpm.sha512

[linux-musl-x64-badge-6.0.X]: https://aka.ms/dotnet/6.0/daily/sharedfx_linux-musl-x64_Release_version_badge.svg?no-cache
[linux-musl-x64-version-6.0.X]: https://aka.ms/dotnet/6.0/daily/runtime-productVersion.txt
[linux-musl-x64-targz-6.0.X]: https://aka.ms/dotnet/6.0/daily/dotnet-runtime-linux-musl-x64.tar.gz
[linux-musl-x64-targz-checksum-6.0.X]: https://aka.ms/dotnet/6.0/daily/dotnet-runtime-linux-musl-x64.tar.gz.sha512
[linux-musl-x64-nethost-targz-6.0.X]: https://aka.ms/dotnet/6.0/daily/dotnet-nethost-linux-musl-x64.tar.gz
[linux-musl-x64-symbols-targz-6.0.X]: https://aka.ms/dotnet/6.0/daily/dotnet-runtime-symbols-linux-musl-x64.tar.gz

[linux-musl-arm-badge-6.0.X]: https://aka.ms/dotnet/6.0/daily/sharedfx_linux-musl-arm_Release_version_badge.svg?no-cache
[linux-musl-arm-version-6.0.X]: https://aka.ms/dotnet/6.0/daily/runtime-productVersion.txt
[linux-musl-arm-targz-6.0.X]: https://aka.ms/dotnet/6.0/daily/dotnet-runtime-linux-musl-arm.tar.gz
[linux-musl-arm-targz-checksum-6.0.X]: https://aka.ms/dotnet/6.0/daily/dotnet-runtime-linux-musl-arm.tar.gz.sha512
[linux-musl-arm-nethost-targz-6.0.X]: https://aka.ms/dotnet/6.0/daily/dotnet-nethost-linux-musl-arm.tar.gz
[linux-musl-arm-symbols-targz-6.0.X]: https://aka.ms/dotnet/6.0/daily/dotnet-runtime-symbols-linux-musl-arm.tar.gz

[linux-musl-arm64-badge-6.0.X]: https://aka.ms/dotnet/6.0/daily/sharedfx_linux-musl-arm64_Release_version_badge.svg?no-cache
[linux-musl-arm64-version-6.0.X]: https://aka.ms/dotnet/6.0/daily/runtime-productVersion.txt
[linux-musl-arm64-targz-6.0.X]: https://aka.ms/dotnet/6.0/daily/dotnet-runtime-linux-musl-arm64.tar.gz
[linux-musl-arm64-targz-checksum-6.0.X]: https://aka.ms/dotnet/6.0/daily/dotnet-runtime-linux-musl-arm64.tar.gz.sha512
[linux-musl-arm64-nethost-targz-6.0.X]: https://aka.ms/dotnet/6.0/daily/dotnet-nethost-linux-musl-arm64.tar.gz
[linux-musl-arm64-symbols-targz-6.0.X]: https://aka.ms/dotnet/6.0/daily/dotnet-runtime-symbols-linux-musl-arm64.tar.gz

<!-- END links to include in table -->
