# Dogfooding daily builds of .NET

This document provides the steps necessary to consume a latest development build of .NET runtime and SDK.
Example below is for 8.0 but similar steps should work for other versions as well.

## Obtaining daily builds of NuGet packages

If you are only looking to get fixes for an individual NuGet package, and don't need a preview version of the entire runtime, you can add the development package feed to your `NuGet.config` file.  The easiest way to do this is by using the dotnet CLI:

**(Recommended)** Create a local NuGet.Config file for your solution, if don't already have one.  Using a local NuGet.Config file will enable the development feed as a package source for projects in the current directory only.
```
dotnet new nugetconfig
```

Next, add the package source to NuGet.Config with the [dotnet nuget add source](https://docs.microsoft.com/en-us/dotnet/core/tools/dotnet-nuget-add-source) command:
```
dotnet nuget add source -n dotnet8 https://dnceng.pkgs.visualstudio.com/public/_packaging/dotnet8/nuget/v3/index.json
```

Then, you will be able to add the latest prerelease version of the desired package to your project.

**Example:** To add version 8.0-preview.5.22226.4 of the System.Data.OleDb package, use the [dotnet add package](https://docs.microsoft.com/en-us/dotnet/core/tools/dotnet-add-package) command:
```
dotnet add package System.Data.OleDb -v 8.0-preview.5.22226.4
```

To use daily builds of the entire runtime, follow the steps given in the rest of this document instead.

## Configuring upstream feeds in ADO

If you're using private Azure DevOps feeds for your projects, you might need to add the preview feed through the Azure-specific feed URI format, which is `azure-feed://organization/optionalProject/feed@view`. In this case, you can add the .NET development package feed as follows:

```
azure-feed://dnceng/public/dotnet8@Local
```

## Install prerequisites

1. Acquire the latest development .NET SDK by downloading and extracting a zip/tarball or using an installer from the [installers and binaries table in dotnet/installer](https://github.com/dotnet/installer#installers-and-binaries) (for example, https://aka.ms/dotnet/8.0/daily/dotnet-sdk-win-x64.zip).

2. If you are using a local copy of the dotnet CLI, take care that when you type `dotnet` you do not inadvertently pick up a different copy that you may have in your path. On Windows, for example, if you use a Command Prompt, a global copy may be in the path, so use the fully qualified path to your local `dotnet` (e.g. `C:\dotnet\dotnet.exe`). If you receive an error "error NETSDK1045:  The current .NET SDK does not support targeting .NET 8.0." then you may be executing an older `dotnet`.

After setting up dotnet you can verify you are using the dogfooding version by executing `dotnet --info`. Here is an example output at the time of writing:
```
>dotnet --info
.NET SDK:
 Version:   8.0.100-preview.5.22226.4
 Commit:    fc127ac5a4

Runtime Environment:
 OS Name:     Windows
 OS Version:  10.0.22616
 OS Platform: Windows
 RID:         win10-x64
 Base Path:   C:\Program Files\dotnet\sdk\8.0.100-preview.5.22226.4\

global.json file:
  Not found

Host:
  Version:      8.0.0-preview.5.22224.3
  Architecture: x64
  Commit:       47d9c43ab1

.NET SDKs installed:
  8.0.100-preview.5.22226.4 [C:\Program Files\dotnet\sdk]

.NET runtimes installed:
  Microsoft.NETCore.App 8.0.0-preview.5.22224.3 [C:\Program Files\dotnet\shared\Microsoft.NETCore.App]

Download .NET:
  https://aka.ms/dotnet-download

Learn about .NET Runtimes and SDKs:
  https://aka.ms/dotnet/runtimes-sdk-info

```

3. Our daily builds are uploaded to development feed, not NuGet - so ensure the development feed is in your nuget configuration in case you need other packages that aren't included in the download. For example, on Windows you could edit `%userprofile%\appdata\roaming\nuget\nuget.config` or on Linux edit `~/.nuget/NuGet/NuGet.Config` to add these lines:
```xml
<packageSources>
    <add key="dotnet8" value="https://dnceng.pkgs.visualstudio.com/public/_packaging/dotnet8/nuget/v3/index.json" />
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

## Advanced Scenario - Using a daily build of Microsoft.NETCore.App

When using the above instructions, your application will run against the same
.NET runtime that comes with the SDK. That works fine to get up and
running quickly. However, there are times when you need to use a daily build
of Microsoft.NETCore.App which hasn't made its way into the SDK yet. To enable
this, there are two options you can take.

### Option 1: Framework-dependent

This is the default case for applications - running against an installed .NET runtime.

1. You still need to install the prerequisite .NET SDK from above.
2. Optionally, install the specific .NET runtime you require globally or download get the latest one available from the [daily build table](#daily-builds-table)
3. Modify your .csproj to reference the daily build of Microsoft.NETCore.App

```XML
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <!-- Ensure that the target framework is correct e.g. 'net8.0' -->
    <TargetFramework>net8.0</TargetFramework>
    <!-- modify version in this line with one reported by `dotnet --info` under ".NET runtimes installed" -> Microsoft.NETCore.App -->
    <RuntimeFrameworkVersion>8.0.0-preview.5.22224.3</RuntimeFrameworkVersion>
  </PropertyGroup>
```

```
$ dotnet restore
$ dotnet run
```

### Option 2: Self-contained

In this case, the .NET runtime will be published along with your application.

1. You still need to install the prerequisite .NET SDK from above.
2. Modify your .csproj to reference the daily build of Microsoft.NETCore.App *and*
make it self-contained by adding a RuntimeIdentifier (RID).

```XML
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <!-- Ensure that the target framework is correct e.g. 'net8.0' -->
    <TargetFramework>net8.0</TargetFramework>
    <!-- modify build in this line with version reported by `dotnet --info` as above under ".NET runtimes installed" -> Microsoft.NETCore.App -->
    <!-- moreover, this can be any valid Microsoft.NETCore.App package version from https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet8/nuget/v3/index.json -->
    <RuntimeFrameworkVersion>8.0.0-preview.5.22224.3</RuntimeFrameworkVersion>
    <RuntimeIdentifier>win-x64</RuntimeIdentifier> <!-- RID to make it self-contained -->
  </PropertyGroup>
```

```
$ dotnet restore
$ dotnet publish
$ bin\Debug\net8.0\win-x64\publish\App.exe
```

### Daily builds table

<!--
  To update this table, run 'build.sh/cmd RegenerateDownloadTable'. See
  'tools-local/regenerate-readme-table.proj' to add or remove rows or columns,
  and add links below to fill out the table's contents.
-->
<!-- BEGIN generated table -->

| Platform | Main |
| --- |  :---: |
| **Windows (x64)** | <br>[Installer][win-x64-installer-8.0.X] ([Checksum][win-x64-installer-checksum-8.0.X])<br>[zip][win-x64-zip-8.0.X] ([Checksum][win-x64-zip-checksum-8.0.X]) |
| **Windows (x86)** | <br>[Installer][win-x86-installer-8.0.X] ([Checksum][win-x86-installer-checksum-8.0.X])<br>[zip][win-x86-zip-8.0.X] ([Checksum][win-x86-zip-checksum-8.0.X]) |
| **Windows (arm64)** | <br>[Installer][win-arm64-installer-8.0.X] ([Checksum][win-arm64-installer-checksum-8.0.X])<br>[zip][win-arm64-zip-8.0.X] ([Checksum][win-arm64-zip-checksum-8.0.X]) |
| **macOS (x64)** | <br>[Installer][osx-x64-installer-8.0.X] ([Checksum][osx-x64-installer-checksum-8.0.X])<br>[tar.gz][osx-x64-targz-8.0.X] ([Checksum][osx-x64-targz-checksum-8.0.X]) |
| **macOS (arm64)** | <br>[Installer][osx-arm64-installer-8.0.X] ([Checksum][osx-arm64-installer-checksum-8.0.X])<br>[tar.gz][osx-arm64-targz-8.0.X] ([Checksum][osx-arm64-targz-checksum-8.0.X]) |
| **Linux (x64)** (for glibc based OS) | <br>[tar.gz][linux-x64-targz-8.0.X] ([Checksum][linux-x64-targz-checksum-8.0.X]) |
| **Linux (armhf)** (for glibc based OS) | <br>[tar.gz][linux-arm-targz-8.0.X] ([Checksum][linux-arm-targz-checksum-8.0.X]) |
| **Linux (arm64)** (for glibc based OS) | <br>[tar.gz][linux-arm64-targz-8.0.X] ([Checksum][linux-arm64-targz-checksum-8.0.X]) |
| **Linux-musl (x64)** | <br>[tar.gz][linux-musl-x64-targz-8.0.X] ([Checksum][linux-musl-x64-targz-checksum-8.0.X]) |
| **Linux-musl (arm)** | <br>[tar.gz][linux-musl-arm-targz-8.0.X] ([Checksum][linux-musl-arm-targz-checksum-8.0.X]) |
| **Linux-musl (arm64)** | <br>[tar.gz][linux-musl-arm64-targz-8.0.X] ([Checksum][linux-musl-arm64-targz-checksum-8.0.X]) |
| **Dpkg Based Systems (x64)** | <br>[Runtime-Deps][deb-runtime-deps-8.0.X] ([Checksum][deb-runtime-deps-checksum-8.0.X])<br>[Host][deb-host-8.0.X] ([Checksum][deb-host-checksum-8.0.X])<br>[App Hosts][deb-apphost-pack-8.0.X] ([Checksum][deb-apphost-pack-checksum-8.0.X])<br>[Host FX Resolver][deb-hostfxr-8.0.X] ([Checksum][deb-hostfxr-checksum-8.0.X])<br>[Targeting Pack][deb-targeting-pack-8.0.X] ([Checksum][deb-targeting-pack-checksum-8.0.X])<br>[Shared Framework][deb-sharedfx-8.0.X] ([Checksum][deb-sharedfx-checksum-8.0.X]) |
| **CentOS 8 (x64)** | <br>[Runtime-Deps][centos-8-runtime-deps-8.0.X] ([Checksum][centos-8-runtime-deps-checksum-8.0.X])<br>[Host][centos-8-host-8.0.X] ([Checksum][centos-8-host-checksum-8.0.X])<br>[App Hosts][centos-8-apphost-pack-8.0.X] ([Checksum][centos-8-apphost-pack-checksum-8.0.X])<br>[Host FX Resolver][centos-8-hostfxr-8.0.X] ([Checksum][centos-8-hostfxr-checksum-8.0.X])<br>[Targeting Pack][centos-8-targeting-pack-8.0.X] ([Checksum][centos-8-targeting-pack-checksum-8.0.X])<br>[Shared Framework][centos-8-sharedfx-8.0.X] ([Checksum][centos-8-sharedfx-checksum-8.0.X]) |
| **RHEL 8 (x64)** | <br>[Host][rhel8-host-8.0.X] ([Checksum][rhel8-host-checksum-8.0.X])<br>[App Hosts][rhel8-apphost-pack-8.0.X] ([Checksum][rhel8-apphost-pack-checksum-8.0.X])<br>[Host FX Resolver][rhel8-hostfxr-8.0.X] ([Checksum][rhel8-hostfxr-checksum-8.0.X])<br>[Targeting Pack][rhel8-targeting-pack-8.0.X] ([Checksum][rhel8-targeting-pack-checksum-8.0.X])<br>[Shared Framework][rhel8-sharedfx-8.0.X] ([Checksum][rhel8-sharedfx-checksum-8.0.X]) |
| **Fedora 27 (x64)** | <br>[Runtime-Deps][fedora-27-runtime-deps-8.0.X] ([Checksum][fedora-27-runtime-deps-checksum-8.0.X])<br>[Host][fedora-27-host-8.0.X] ([Checksum][fedora-27-host-checksum-8.0.X])<br>[App Hosts][fedora-27-apphost-pack-8.0.X] ([Checksum][fedora-27-apphost-pack-checksum-8.0.X])<br>[Host FX Resolver][fedora-27-hostfxr-8.0.X] ([Checksum][fedora-27-hostfxr-checksum-8.0.X])<br>[Targeting Pack][fedora-27-targeting-pack-8.0.X] ([Checksum][fedora-27-targeting-pack-checksum-8.0.X])<br>[Shared Framework][fedora-27-sharedfx-8.0.X] ([Checksum][fedora-27-sharedfx-checksum-8.0.X]) |
| **SLES 12 (x64)** | <br>[Runtime-Deps][sles-12-runtime-deps-8.0.X] ([Checksum][sles-12-runtime-deps-checksum-8.0.X])<br>[Host][sles-12-host-8.0.X] ([Checksum][sles-12-host-checksum-8.0.X])<br>[App Hosts][sles-12-apphost-pack-8.0.X] ([Checksum][sles-12-apphost-pack-checksum-8.0.X])<br>[Host FX Resolver][sles-12-hostfxr-8.0.X] ([Checksum][sles-12-hostfxr-checksum-8.0.X])<br>[Targeting Pack][sles-12-targeting-pack-8.0.X] ([Checksum][sles-12-targeting-pack-checksum-8.0.X])<br>[Shared Framework][sles-12-sharedfx-8.0.X] ([Checksum][sles-12-sharedfx-checksum-8.0.X]) |
| **OpenSUSE 42 (x64)** | <br>[Runtime-Deps][OpenSUSE-42-runtime-deps-8.0.X] ([Checksum][OpenSUSE-42-runtime-deps-checksum-8.0.X])<br>[Host][OpenSUSE-42-host-8.0.X] ([Checksum][OpenSUSE-42-host-checksum-8.0.X])<br>[App Hosts][OpenSUSE-42-apphost-pack-8.0.X] ([Checksum][OpenSUSE-42-apphost-pack-checksum-8.0.X])<br>[Host FX Resolver][OpenSUSE-42-hostfxr-8.0.X] ([Checksum][OpenSUSE-42-hostfxr-checksum-8.0.X])<br>[Targeting Pack][OpenSUSE-42-targeting-pack-8.0.X] ([Checksum][OpenSUSE-42-targeting-pack-checksum-8.0.X])<br>[Shared Framework][OpenSUSE-42-sharedfx-8.0.X] ([Checksum][OpenSUSE-42-sharedfx-checksum-8.0.X]) |

<!-- END generated table -->

*Note: Our Linux packages (.deb and .rpm) are put together slightly differently than the Windows and Mac specific installers. Instead of combining everything, we have separate component packages that depend on each other. If you're installing these directly from the installer files (via dpkg or similar), then you'll need to install them in the order presented above.*

<!-- BEGIN links to include in table -->

[win-x64-badge-8.0.X]: https://aka.ms/dotnet/8.0/daily/sharedfx_win-x64_Release_version_badge.svg?no-cache
[win-x64-version-8.0.X]: https://aka.ms/dotnet/8.0/daily/runtime-productVersion.txt
[win-x64-sdkinstaller-8.0.X]: https://aka.ms/dotnet/8.0/daily/dotnet-sdk-win-x64.exe
[win-x64-installer-8.0.X]: https://aka.ms/dotnet/8.0/daily/dotnet-runtime-win-x64.exe
[win-x64-installer-checksum-8.0.X]: https://aka.ms/dotnet/8.0/daily/dotnet-runtime-win-x64.exe.sha512
[win-x64-zip-8.0.X]: https://aka.ms/dotnet/8.0/daily/dotnet-runtime-win-x64.zip
[win-x64-zip-checksum-8.0.X]: https://aka.ms/dotnet/8.0/daily/dotnet-runtime-win-x64.zip.sha512
[win-x64-nethost-zip-8.0.X]: https://aka.ms/dotnet/8.0/daily/dotnet-nethost-win-x64.zip
[win-x64-symbols-zip-8.0.X]: https://aka.ms/dotnet/8.0/daily/dotnet-runtime-symbols-win-x64.zip

[win-x86-badge-8.0.X]: https://aka.ms/dotnet/8.0/daily/sharedfx_win-x86_Release_version_badge.svg?no-cache
[win-x86-version-8.0.X]: https://aka.ms/dotnet/8.0/daily/runtime-productVersion.txt
[win-x86-sdkinstaller-8.0.X]: https://aka.ms/dotnet/8.0/daily/dotnet-sdk-win-x86.exe
[win-x86-installer-8.0.X]: https://aka.ms/dotnet/8.0/daily/dotnet-runtime-win-x86.exe
[win-x86-installer-checksum-8.0.X]: https://aka.ms/dotnet/8.0/daily/dotnet-runtime-win-x86.exe.sha512
[win-x86-zip-8.0.X]: https://aka.ms/dotnet/8.0/daily/dotnet-runtime-win-x86.zip
[win-x86-zip-checksum-8.0.X]: https://aka.ms/dotnet/8.0/daily/dotnet-runtime-win-x86.zip.sha512
[win-x86-nethost-zip-8.0.X]: https://aka.ms/dotnet/8.0/daily/dotnet-nethost-win-x86.zip
[win-x86-symbols-zip-8.0.X]: https://aka.ms/dotnet/8.0/daily/dotnet-runtime-symbols-win-x86.zip

[win-arm64-badge-8.0.X]: https://aka.ms/dotnet/8.0/daily/sharedfx_win-arm64_Release_version_badge.svg?no-cache
[win-arm64-version-8.0.X]: https://aka.ms/dotnet/8.0/daily/runtime-productVersion.txt
[win-arm64-sdkinstaller-8.0.X]: https://aka.ms/dotnet/8.0/daily/dotnet-sdk-win-arm64.exe
[win-arm64-installer-8.0.X]: https://aka.ms/dotnet/8.0/daily/dotnet-runtime-win-arm64.exe
[win-arm64-installer-checksum-8.0.X]: https://aka.ms/dotnet/8.0/daily/dotnet-runtime-win-arm64.exe.sha512
[win-arm64-zip-8.0.X]: https://aka.ms/dotnet/8.0/daily/dotnet-runtime-win-arm64.zip
[win-arm64-zip-checksum-8.0.X]: https://aka.ms/dotnet/8.0/daily/dotnet-runtime-win-arm64.zip.sha512
[win-arm64-nethost-zip-8.0.X]: https://aka.ms/dotnet/8.0/daily/dotnet-nethost-win-arm64.zip
[win-arm64-symbols-zip-8.0.X]: https://aka.ms/dotnet/8.0/daily/dotnet-runtime-symbols-win-arm64.zip

[osx-x64-badge-8.0.X]: https://aka.ms/dotnet/8.0/daily/sharedfx_osx-x64_Release_version_badge.svg?no-cache
[osx-x64-version-8.0.X]: https://aka.ms/dotnet/8.0/daily/runtime-productVersion.txt
[osx-x64-sdkinstaller-8.0.X]: https://aka.ms/dotnet/8.0/daily/dotnet-sdk-osx-x64.pkg
[osx-x64-installer-8.0.X]: https://aka.ms/dotnet/8.0/daily/dotnet-runtime-osx-x64.pkg
[osx-x64-installer-checksum-8.0.X]: https://aka.ms/dotnet/8.0/daily/dotnet-runtime-osx-x64.pkg.sha512
[osx-x64-targz-8.0.X]: https://aka.ms/dotnet/8.0/daily/dotnet-runtime-osx-x64.tar.gz
[osx-x64-targz-checksum-8.0.X]: https://aka.ms/dotnet/8.0/daily/dotnet-runtime-osx-x64.tar.gz.sha512
[osx-x64-nethost-targz-8.0.X]: https://aka.ms/dotnet/8.0/daily/dotnet-nethost-osx-x64.tar.gz
[osx-x64-symbols-targz-8.0.X]: https://aka.ms/dotnet/8.0/daily/dotnet-runtime-symbols-osx-x64.tar.gz

[osx-arm64-badge-8.0.X]: https://aka.ms/dotnet/8.0/daily/sharedfx_osx-arm64_Release_version_badge.svg?no-cache
[osx-arm64-version-8.0.X]: https://aka.ms/dotnet/8.0/daily/runtime-productVersion.txt
[osx-arm64-sdkinstaller-8.0.X]: https://aka.ms/dotnet/8.0/daily/dotnet-sdk-osx-arm64.pkg
[osx-arm64-installer-8.0.X]: https://aka.ms/dotnet/8.0/daily/dotnet-runtime-osx-arm64.pkg
[osx-arm64-installer-checksum-8.0.X]: https://aka.ms/dotnet/8.0/daily/dotnet-runtime-osx-arm64.pkg.sha512
[osx-arm64-targz-8.0.X]: https://aka.ms/dotnet/8.0/daily/dotnet-runtime-osx-arm64.tar.gz
[osx-arm64-targz-checksum-8.0.X]: https://aka.ms/dotnet/8.0/daily/dotnet-runtime-osx-arm64.tar.gz.sha512
[osx-arm64-nethost-targz-8.0.X]: https://aka.ms/dotnet/8.0/daily/dotnet-nethost-osx-arm64.tar.gz
[osx-arm64-symbols-targz-8.0.X]: https://aka.ms/dotnet/8.0/daily/dotnet-runtime-symbols-osx-arm64.tar.gz

[linux-x64-badge-8.0.X]: https://aka.ms/dotnet/8.0/daily/sharedfx_linux-x64_Release_version_badge.svg?no-cache
[linux-x64-version-8.0.X]: https://aka.ms/dotnet/8.0/daily/runtime-productVersion.txt
[linux-x64-targz-8.0.X]: https://aka.ms/dotnet/8.0/daily/dotnet-runtime-linux-x64.tar.gz
[linux-x64-targz-checksum-8.0.X]: https://aka.ms/dotnet/8.0/daily/dotnet-runtime-linux-x64.tar.gz.sha512
[linux-x64-nethost-targz-8.0.X]: https://aka.ms/dotnet/8.0/daily/dotnet-nethost-linux-x64.tar.gz
[linux-x64-symbols-targz-8.0.X]: https://aka.ms/dotnet/8.0/daily/dotnet-runtime-symbols-linux-x64.tar.gz

[linux-arm-badge-8.0.X]: https://aka.ms/dotnet/8.0/daily/sharedfx_linux-arm_Release_version_badge.svg?no-cache
[linux-arm-version-8.0.X]: https://aka.ms/dotnet/8.0/daily/runtime-productVersion.txt
[linux-arm-targz-8.0.X]: https://aka.ms/dotnet/8.0/daily/dotnet-runtime-linux-arm.tar.gz
[linux-arm-targz-checksum-8.0.X]: https://aka.ms/dotnet/8.0/daily/dotnet-runtime-linux-arm.tar.gz.sha512
[linux-arm-nethost-targz-8.0.X]: https://aka.ms/dotnet/8.0/daily/dotnet-nethost-linux-arm.tar.gz
[linux-arm-symbols-targz-8.0.X]: https://aka.ms/dotnet/8.0/daily/dotnet-runtime-symbols-linux-arm.tar.gz

[linux-arm64-badge-8.0.X]: https://aka.ms/dotnet/8.0/daily/sharedfx_linux-arm64_Release_version_badge.svg?no-cache
[linux-arm64-version-8.0.X]: https://aka.ms/dotnet/8.0/daily/runtime-productVersion.txt
[linux-arm64-targz-8.0.X]: https://aka.ms/dotnet/8.0/daily/dotnet-runtime-linux-arm64.tar.gz
[linux-arm64-targz-checksum-8.0.X]: https://aka.ms/dotnet/8.0/daily/dotnet-runtime-linux-arm64.tar.gz.sha512
[linux-arm64-nethost-targz-8.0.X]: https://aka.ms/dotnet/8.0/daily/dotnet-nethost-linux-arm64.tar.gz
[linux-arm64-symbols-targz-8.0.X]: https://aka.ms/dotnet/8.0/daily/dotnet-runtime-symbols-linux-arm64.tar.gz

[deb-badge-8.0.X]: https://aka.ms/dotnet/8.0/daily/sharedfx_ubuntu.14.04-x64_Release_version_badge.svg?no-cache
[deb-version-8.0.X]: https://aka.ms/dotnet/8.0/daily/runtime-productVersion.txt
[deb-apphost-pack-8.0.X]: https://aka.ms/dotnet/8.0/daily/dotnet-apphost-pack-x64.deb
[deb-apphost-pack-checksum-8.0.X]: https://aka.ms/dotnet/8.0/daily/dotnet-apphost-pack-x64.deb.sha512
[deb-host-8.0.X]: https://aka.ms/dotnet/8.0/daily/dotnet-host-x64.deb
[deb-runtime-deps-8.0.X]: https://aka.ms/dotnet/8.0/daily/dotnet-runtime-deps-x64.deb
[deb-runtime-deps-checksum-8.0.X]: https://aka.ms/dotnet/8.0/daily/dotnet-runtime-deps-x64.deb.sha512
[deb-host-checksum-8.0.X]: https://aka.ms/dotnet/8.0/daily/dotnet-host-x64.deb.sha512
[deb-hostfxr-8.0.X]: https://aka.ms/dotnet/8.0/daily/dotnet-hostfxr-x64.deb
[deb-hostfxr-checksum-8.0.X]: https://aka.ms/dotnet/8.0/daily/dotnet-hostfxr-x64.deb.sha512
[deb-sharedfx-8.0.X]: https://aka.ms/dotnet/8.0/daily/dotnet-runtime-x64.deb
[deb-sharedfx-checksum-8.0.X]: https://aka.ms/dotnet/8.0/daily/dotnet-runtime-x64.deb.sha512
[deb-targeting-pack-8.0.X]: https://aka.ms/dotnet/8.0/daily/dotnet-targeting-pack-x64.deb
[deb-targeting-pack-checksum-8.0.X]: https://aka.ms/dotnet/8.0/daily/dotnet-targeting-pack-x64.deb.sha512

[rhel8-badge-8.0.X]: https://aka.ms/dotnet/8.0/daily/sharedfx_rhel.8-x64_Release_version_badge.svg?no-cache
[rhel8-version-8.0.X]: https://aka.ms/dotnet/8.0/daily/runtime-productVersion.txt
[rhel8-runtime-deps-checksum-8.0.X]: https://aka.ms/dotnet/8.0/daily/dotnet-runtime-deps-centos.8-x64.rpm
[rhel8-runtime-deps-checksum-8.0.X]: https://aka.ms/dotnet/8.0/daily/dotnet-runtime-deps-centos.8-x64.rpm.sha512
[rhel8-apphost-pack-8.0.X]: https://aka.ms/dotnet/8.0/daily/dotnet-apphost-pack-x64.rpm
[rhel8-apphost-pack-checksum-8.0.X]: https://aka.ms/dotnet/8.0/daily/dotnet-apphost-pack-x64.rpm.sha512
[rhel8-host-8.0.X]: https://aka.ms/dotnet/8.0/daily/dotnet-host-x64.rpm
[rhel8-host-checksum-8.0.X]: https://aka.ms/dotnet/8.0/daily/dotnet-host-x64.rpm.sha512
[rhel8-hostfxr-8.0.X]: https://aka.ms/dotnet/8.0/daily/dotnet-hostfxr-x64.rpm
[rhel8-hostfxr-checksum-8.0.X]: https://aka.ms/dotnet/8.0/daily/dotnet-hostfxr-x64.rpm.sha512
[rhel8-sharedfx-8.0.X]: https://aka.ms/dotnet/8.0/daily/dotnet-runtime-x64.rpm
[rhel8-sharedfx-checksum-8.0.X]: https://aka.ms/dotnet/8.0/daily/dotnet-runtime-x64.rpm.sha512
[rhel8-targeting-pack-8.0.X]: https://aka.ms/dotnet/8.0/daily/dotnet-targeting-pack-x64.rpm
[rhel8-targeting-pack-checksum-8.0.X]: https://aka.ms/dotnet/8.0/daily/dotnet-targeting-pack-x64.rpm.sha512

[centos-8-badge-8.0.X]: https://aka.ms/dotnet/8.0/daily/sharedfx_centos.8-x64_Release_version_badge.svg?no-cache
[centos-8-version-8.0.X]: https://aka.ms/dotnet/8.0/daily/runtime-productVersion.txt
[centos-8-runtime-deps-8.0.X]: https://aka.ms/dotnet/8.0/daily/dotnet-runtime-deps-centos.8-x64.rpm
[centos-8-runtime-deps-checksum-8.0.X]: https://aka.ms/dotnet/8.0/daily/dotnet-runtime-deps-centos.8-x64.rpm.sha512
[centos-8-apphost-pack-8.0.X]: https://aka.ms/dotnet/8.0/daily/dotnet-apphost-pack-x64.rpm
[centos-8-apphost-pack-checksum-8.0.X]: https://aka.ms/dotnet/8.0/daily/dotnet-apphost-pack-x64.rpm.sha512
[centos-8-host-8.0.X]: https://aka.ms/dotnet/8.0/daily/dotnet-host-x64.rpm
[centos-8-host-checksum-8.0.X]: https://aka.ms/dotnet/8.0/daily/dotnet-host-x64.rpm.sha512
[centos-8-hostfxr-8.0.X]: https://aka.ms/dotnet/8.0/daily/dotnet-hostfxr-x64.rpm
[centos-8-hostfxr-checksum-8.0.X]: https://aka.ms/dotnet/8.0/daily/dotnet-hostfxr-x64.rpm.sha512
[centos-8-sharedfx-8.0.X]: https://aka.ms/dotnet/8.0/daily/dotnet-runtime-x64.rpm
[centos-8-sharedfx-checksum-8.0.X]: https://aka.ms/dotnet/8.0/daily/dotnet-runtime-x64.rpm.sha512
[centos-8-targeting-pack-8.0.X]: https://aka.ms/dotnet/8.0/daily/dotnet-targeting-pack-x64.rpm
[centos-8-targeting-pack-checksum-8.0.X]: https://aka.ms/dotnet/8.0/daily/dotnet-targeting-pack-x64.rpm.sha512

[fedora-27-badge-8.0.X]: https://aka.ms/dotnet/8.0/daily/sharedfx_fedora.27-x64_Release_version_badge.svg?no-cache
[fedora-27-version-8.0.X]: https://aka.ms/dotnet/8.0/daily/runtime-productVersion.txt
[fedora-27-runtime-deps-8.0.X]: https://aka.ms/dotnet/8.0/daily/dotnet-runtime-deps-fedora.27-x64.rpm
[fedora-27-runtime-deps-checksum-8.0.X]: https://aka.ms/dotnet/8.0/daily/dotnet-runtime-deps-fedora.27-x64.rpm.sha512
[fedora-27-apphost-pack-8.0.X]: https://aka.ms/dotnet/8.0/daily/dotnet-apphost-pack-x64.rpm
[fedora-27-apphost-pack-checksum-8.0.X]: https://aka.ms/dotnet/8.0/daily/dotnet-apphost-pack-x64.rpm.sha512
[fedora-27-host-8.0.X]: https://aka.ms/dotnet/8.0/daily/dotnet-host-x64.rpm
[fedora-27-host-checksum-8.0.X]: https://aka.ms/dotnet/8.0/daily/dotnet-host-x64.rpm.sha512
[fedora-27-hostfxr-8.0.X]: https://aka.ms/dotnet/8.0/daily/dotnet-hostfxr-x64.rpm
[fedora-27-hostfxr-checksum-8.0.X]: https://aka.ms/dotnet/8.0/daily/dotnet-hostfxr-x64.rpm.sha512
[fedora-27-sharedfx-8.0.X]: https://aka.ms/dotnet/8.0/daily/dotnet-runtime-x64.rpm
[fedora-27-sharedfx-checksum-8.0.X]: https://aka.ms/dotnet/8.0/daily/dotnet-runtime-x64.rpm.sha512
[fedora-27-targeting-pack-8.0.X]: https://aka.ms/dotnet/8.0/daily/dotnet-targeting-pack-x64.rpm
[fedora-27-targeting-pack-checksum-8.0.X]: https://aka.ms/dotnet/8.0/daily/dotnet-targeting-pack-x64.rpm.sha512

[sles-12-badge-8.0.X]: https://aka.ms/dotnet/8.0/daily/sharedfx_sles.12-x64_Release_version_badge.svg?no-cache
[sles-12-version-8.0.X]: https://aka.ms/dotnet/8.0/daily/runtime-productVersion.txt
[sles-12-runtime-deps-8.0.X]: https://aka.ms/dotnet/8.0/daily/dotnet-runtime-deps-sles.12-x64.rpm
[sles-12-runtime-deps-checksum-8.0.X]: https://aka.ms/dotnet/8.0/daily/dotnet-runtime-deps-sles.12-x64.rpm.sha512
[sles-12-apphost-pack-8.0.X]: https://aka.ms/dotnet/8.0/daily/dotnet-apphost-pack-x64.rpm
[sles-12-apphost-pack-checksum-8.0.X]: https://aka.ms/dotnet/8.0/daily/dotnet-apphost-pack-x64.rpm.sha512
[sles-12-host-8.0.X]: https://aka.ms/dotnet/8.0/daily/dotnet-host-x64.rpm
[sles-12-host-checksum-8.0.X]: https://aka.ms/dotnet/8.0/daily/dotnet-host-x64.rpm.sha512
[sles-12-hostfxr-8.0.X]: https://aka.ms/dotnet/8.0/daily/dotnet-hostfxr-x64.rpm
[sles-12-hostfxr-checksum-8.0.X]: https://aka.ms/dotnet/8.0/daily/dotnet-hostfxr-x64.rpm.sha512
[sles-12-sharedfx-8.0.X]: https://aka.ms/dotnet/8.0/daily/dotnet-runtime-x64.rpm
[sles-12-sharedfx-checksum-8.0.X]: https://aka.ms/dotnet/8.0/daily/dotnet-runtime-x64.rpm.sha512
[sles-12-targeting-pack-8.0.X]: https://aka.ms/dotnet/8.0/daily/dotnet-targeting-pack-x64.rpm
[sles-12-targeting-pack-checksum-8.0.X]: https://aka.ms/dotnet/8.0/daily/dotnet-targeting-pack-x64.rpm.sha512

[OpenSUSE-42-badge-8.0.X]: https://aka.ms/dotnet/8.0/daily/sharedfx_opensuse.42-x64_Release_version_badge.svg?no-cache
[OpenSUSE-42-version-8.0.X]: https://aka.ms/dotnet/8.0/daily/runtime-productVersion.txt
[OpenSUSE-42-runtime-deps-8.0.X]: https://aka.ms/dotnet/8.0/daily/dotnet-runtime-deps-opensuse.42-x64.rpm
[OpenSUSE-42-runtime-deps-checksum-8.0.X]: https://aka.ms/dotnet/8.0/daily/dotnet-runtime-deps-opensuse.42-x64.rpm.sha512
[OpenSUSE-42-apphost-pack-8.0.X]: https://aka.ms/dotnet/8.0/daily/dotnet-apphost-pack-x64.rpm
[OpenSUSE-42-apphost-pack-checksum-8.0.X]: https://aka.ms/dotnet/8.0/daily/dotnet-apphost-pack-x64.rpm.sha512
[OpenSUSE-42-host-8.0.X]: https://aka.ms/dotnet/8.0/daily/dotnet-host-x64.rpm
[OpenSUSE-42-host-checksum-8.0.X]: https://aka.ms/dotnet/8.0/daily/dotnet-host-x64.rpm.sha512
[OpenSUSE-42-hostfxr-8.0.X]: https://aka.ms/dotnet/8.0/daily/dotnet-hostfxr-x64.rpm
[OpenSUSE-42-hostfxr-checksum-8.0.X]: https://aka.ms/dotnet/8.0/daily/dotnet-hostfxr-x64.rpm.sha512
[OpenSUSE-42-sharedfx-8.0.X]: https://aka.ms/dotnet/8.0/daily/dotnet-runtime-x64.rpm
[OpenSUSE-42-sharedfx-checksum-8.0.X]: https://aka.ms/dotnet/8.0/daily/dotnet-runtime-x64.rpm.sha512
[OpenSUSE-42-targeting-pack-8.0.X]: https://aka.ms/dotnet/8.0/daily/dotnet-targeting-pack-x64.rpm
[OpenSUSE-42-targeting-pack-checksum-8.0.X]: https://aka.ms/dotnet/8.0/daily/dotnet-targeting-pack-x64.rpm.sha512

[linux-musl-x64-badge-8.0.X]: https://aka.ms/dotnet/8.0/daily/sharedfx_linux-musl-x64_Release_version_badge.svg?no-cache
[linux-musl-x64-version-8.0.X]: https://aka.ms/dotnet/8.0/daily/runtime-productVersion.txt
[linux-musl-x64-targz-8.0.X]: https://aka.ms/dotnet/8.0/daily/dotnet-runtime-linux-musl-x64.tar.gz
[linux-musl-x64-targz-checksum-8.0.X]: https://aka.ms/dotnet/8.0/daily/dotnet-runtime-linux-musl-x64.tar.gz.sha512
[linux-musl-x64-nethost-targz-8.0.X]: https://aka.ms/dotnet/8.0/daily/dotnet-nethost-linux-musl-x64.tar.gz
[linux-musl-x64-symbols-targz-8.0.X]: https://aka.ms/dotnet/8.0/daily/dotnet-runtime-symbols-linux-musl-x64.tar.gz

[linux-musl-arm-badge-8.0.X]: https://aka.ms/dotnet/8.0/daily/sharedfx_linux-musl-arm_Release_version_badge.svg?no-cache
[linux-musl-arm-version-8.0.X]: https://aka.ms/dotnet/8.0/daily/runtime-productVersion.txt
[linux-musl-arm-targz-8.0.X]: https://aka.ms/dotnet/8.0/daily/dotnet-runtime-linux-musl-arm.tar.gz
[linux-musl-arm-targz-checksum-8.0.X]: https://aka.ms/dotnet/8.0/daily/dotnet-runtime-linux-musl-arm.tar.gz.sha512
[linux-musl-arm-nethost-targz-8.0.X]: https://aka.ms/dotnet/8.0/daily/dotnet-nethost-linux-musl-arm.tar.gz
[linux-musl-arm-symbols-targz-8.0.X]: https://aka.ms/dotnet/8.0/daily/dotnet-runtime-symbols-linux-musl-arm.tar.gz

[linux-musl-arm64-badge-8.0.X]: https://aka.ms/dotnet/8.0/daily/sharedfx_linux-musl-arm64_Release_version_badge.svg?no-cache
[linux-musl-arm64-version-8.0.X]: https://aka.ms/dotnet/8.0/daily/runtime-productVersion.txt
[linux-musl-arm64-targz-8.0.X]: https://aka.ms/dotnet/8.0/daily/dotnet-runtime-linux-musl-arm64.tar.gz
[linux-musl-arm64-targz-checksum-8.0.X]: https://aka.ms/dotnet/8.0/daily/dotnet-runtime-linux-musl-arm64.tar.gz.sha512
[linux-musl-arm64-nethost-targz-8.0.X]: https://aka.ms/dotnet/8.0/daily/dotnet-nethost-linux-musl-arm64.tar.gz
[linux-musl-arm64-symbols-targz-8.0.X]: https://aka.ms/dotnet/8.0/daily/dotnet-runtime-symbols-linux-musl-arm64.tar.gz

<!-- END links to include in table -->
