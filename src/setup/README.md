.NET Core Runtime & Host Setup Repo
===================================

This repo contains the code to build the .NET Core runtime, libraries and shared host (`dotnet`) installers for
all supported platforms. It **does not** contain the actual sources to .NET Core runtime; this source is split across
the dotnet/coreclr repo (runtime) and dotnet/corefx repo (libraries).

## Installation experience
The all-up installation experience is described in the [installation scenarios](https://github.com/dotnet/cli/blob/rel/1.0.0/Documentation/cli-installation-scenarios.md)
document in the dotnet/cli repo. That is the first step to get acquainted with the overall plan and experience we have
thought up for installing .NET Core bits.

## Filing issues
This repo should contain issues that are tied to the installation of the "muxer" (the `dotnet` binary) and installation
of the .NET Core runtime and libraries.

For other issues, please use the following repos:

- For overall .NET Core SDK issues, file on [dotnet/cli](https://github.com/dotnet/cli) repo
- For class library and framework functioning issues, file on [dotnet/corefx](https://github.com/dotnet/corefx) repo
- For runtime issues, file on [dotnet/coreclr](https://github.com/dotnet/coreclr) issues

This project has adopted the code of conduct defined by the [Contributor Covenant](http://contributor-covenant.org/) to clarify expected behavior in our community. For more information, see the [.NET Foundation Code of Conduct](http://www.dotnetfoundation.org/code-of-conduct).

Officially Released Builds
--------------------------

They can be downloaded from [here](https://www.microsoft.com/net/download#core).

Daily Builds
------------

|   Platform   |   Release/2.0.X<br>[![][build-badge-2.0.x]][build-2.0.x] |
|---------|:----------:|
|   **Windows (x64)**    |   [![][win-x64-badge-2.0.X]][win-x64-version-2.0.X]<br>[Installer][win-x64-installer-2.0.X]<br>[zip][win-x64-zip-2.0.X]<br>[Symbols (zip)][win-x64-symbols-zip-2.0.X]   |
|   **Windows (x86)**    |   [![][win-x86-badge-2.0.X]][win-x86-version-2.0.X]<br>[Installer][win-x86-installer-2.0.X]<br>[zip][win-x86-zip-2.0.X]<br>[Symbols (zip)][win-x86-symbols-zip-2.0.X]   |
|   **Windows (arm32)**  |   [![][win-arm-badge-2.0.X]][win-arm-version-2.0.X]<br>[zip][win-arm-zip-2.0.X]<br>[Symbols (zip)][win-arm-symbols-zip-2.0.X]   |
|   **Windows (arm64)**  |   [![][win-arm64-badge-2.0.X]][win-arm64-version-2.0.X]<br>[zip][win-arm64-zip-2.0.X]<br>[Symbols (zip)][win-arm64-symbols-zip-2.0.X]   |
|   **Mac OS X (x64)**       |   [![][osx-badge-2.0.X]][osx-version-2.0.X]<br>[Installer][osx-installer-2.0.X]<br>[tar.gz][osx-targz-2.0.X]<br>[Symbols (tar.gz)][osx-symbols-targz-2.0.X]   |
|   **Linux (x64)** (for glibc based OS)  |   [![][linux-x64-badge-2.0.X]][linux-x64-version-2.0.X]<br>[tar.gz][linux-x64-targz-2.0.X]<br>[Symbols (tar.gz)][linux-x64-symbols-targz-2.0.X]   |
|   **Linux (armhf)** (for glibc based OS)  |   [![][linux-arm-badge-2.0.X]][linux-arm-version-2.0.X]<br>[tar.gz][linux-arm-targz-2.0.X]<br>[Symbols (tar.gz)][linux-arm-symbols-targz-2.0.X]   |
|   **Ubuntu 14.04 (x64)**   |   [![][ubuntu-14.04-badge-2.0.X]][ubuntu-14.04-version-2.0.X]<br>[Host][ubuntu-14.04-host-2.0.X]<br>[Host FX Resolver][ubuntu-14.04-hostfxr-2.0.X]<br>[Shared Framework][ubuntu-14.04-sharedfx-2.0.X]<br>  |
|   **Ubuntu 16.04 (x64)**   |   [![][ubuntu-16.04-badge-2.0.X]][ubuntu-16.04-version-2.0.X]<br>[Host][ubuntu-16.04-host-2.0.X]<br>[Host FX Resolver][ubuntu-16.04-hostfxr-2.0.X]<br>[Shared Framework][ubuntu-16.04-sharedfx-2.0.X]<br>  |
|   **Ubuntu 16.10 (x64)**   |   [![][ubuntu-16.10-badge-2.0.X]][ubuntu-16.10-version-2.0.X]<br>[Host][ubuntu-16.10-host-2.0.X]<br>[Host FX Resolver][ubuntu-16.10-hostfxr-2.0.X]<br>[Shared Framework][ubuntu-16.10-sharedfx-2.0.X]<br>  |
|   **Debian 8.2 (x64)**     |   [![][debian-8.2-badge-2.0.X]][debian-8.2-version-2.0.X]<br>[Host][debian-8.2-host-2.0.X]<br>[Host FX Resolver][debian-8.2-hostfxr-2.0.X]<br>[Shared Framework][debian-8.2-sharedfx-2.0.X]<br>  |
|   **RHEL 7.2 (x64)**       |   [![][rhel7-badge-2.0.X]][rhel7-version-2.0.X]<br>[Host][rhel7-host-2.0.X]<br>[Host FX Resolver][rhel7-hostfxr-2.0.X]<br>[Shared Framework][rhel7-sharedfx-2.0.X]<br>   |

*Note: Our .deb packages are put together slightly differently than the other OS specific installers. Instead of combining everything, we have separate component packages that depend on each other. If you're installing these directly from the .deb files (via dpkg or similar), then you'll need to install them in the order presented above.*

[build-badge-2.0.X]: https://devdiv.visualstudio.com/_apis/public/build/definitions/0bdbc590-a062-4c3f-b0f6-9383f67865ee/6161/badge
[build-2.0.X]: https://devdiv.visualstudio.com/DevDiv/_build/index?definitionId=6161&_a=completed

[win-x64-badge-2.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/2.0.0/sharedfx_win-x64_Release_version_badge.svg
[win-x64-version-2.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/2.0.0/latest.sharedfx.win.x64.version
[win-x64-installer-2.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/2.0.0/dotnet-win-x64.latest.exe
[win-x64-zip-2.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/2.0.0/dotnet-win-x64.latest.zip
[win-x64-symbols-zip-2.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/2.0.0/dotnet-sharedframework-symbols-win-x64.latest.zip

[win-x86-badge-2.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/2.0.0/sharedfx_win-x86_Release_version_badge.svg
[win-x86-version-2.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/2.0.0/latest.sharedfx.win.x86.version
[win-x86-installer-2.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/2.0.0/dotnet-win-x86.latest.exe
[win-x86-zip-2.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/2.0.0/dotnet-win-x86.latest.zip
[win-x86-symbols-zip-2.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/2.0.0/dotnet-sharedframework-symbols-win-x86.latest.zip

[win-arm-badge-2.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/2.0.0/sharedfx_win-arm_Release_version_badge.svg
[win-arm-version-2.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/2.0.0/latest.sharedfx.win.arm.version
[win-arm-zip-2.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/2.0.0/dotnet-win-arm.latest.zip
[win-arm-symbols-zip-2.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/2.0.0/dotnet-sharedframework-symbols-win-arm.latest.zip

[win-arm64-badge-2.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/2.0.0/sharedfx_win-arm64_Release_version_badge.svg
[win-arm64-version-2.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/2.0.0/latest.sharedfx.win.arm64.version
[win-arm64-zip-2.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/2.0.0/dotnet-win-arm64.latest.zip
[win-arm64-symbols-zip-2.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/2.0.0/dotnet-sharedframework-symbols-win-arm64.latest.zip

[osx-badge-2.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/2.0.0/sharedfx_osx-x64_Release_version_badge.svg
[osx-version-2.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/2.0.0/latest.sharedfx.osx.x64.version
[osx-installer-2.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/2.0.0/dotnet-osx-x64.latest.pkg
[osx-targz-2.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/2.0.0/dotnet-osx-x64.latest.tar.gz
[osx-symbols-targz-2.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/2.0.0/dotnet-sharedframework-symbols-osx-x64.latest.tar.gz

[linux-x64-badge-2.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/2.0.0/sharedfx_linux-x64_Release_version_badge.svg
[linux-x64-version-2.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/2.0.0/latest.sharedfx.linux.x64.version
[linux-x64-targz-2.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/2.0.0/dotnet-linux-x64.latest.tar.gz
[linux-x64-symbols-targz-2.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/2.0.0/dotnet-sharedframework-symbols-linux-x64.latest.tar.gz

[linux-arm-badge-2.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/2.0.0/sharedfx_linux-arm_Release_version_badge.svg
[linux-arm-version-2.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/2.0.0/latest.sharedfx.linux.arm.version
[linux-arm-targz-2.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/2.0.0/dotnet-linux-arm.latest.tar.gz
[linux-arm-symbols-targz-2.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/2.0.0/dotnet-sharedframework-symbols-linux-arm.latest.tar.gz

[ubuntu-14.04-badge-2.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/2.0.0/sharedfx_ubuntu.14.04-x64_Release_version_badge.svg
[ubuntu-14.04-version-2.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/2.0.0/latest.sharedfx.ubuntu.x64.version
[ubuntu-14.04-host-2.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/2.0.0/dotnet-host-ubuntu.14.04-x64.latest.deb
[ubuntu-14.04-hostfxr-2.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/2.0.0/dotnet-hostfxr-ubuntu.14.04-x64.latest.deb
[ubuntu-14.04-sharedfx-2.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/2.0.0/dotnet-sharedframework-ubuntu.14.04-x64.latest.deb

[ubuntu-16.04-badge-2.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/2.0.0/sharedfx_ubuntu.16.04-x64_Release_version_badge.svg
[ubuntu-16.04-version-2.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/2.0.0/latest.sharedfx.ubuntu.16.04.x64.version
[ubuntu-16.04-host-2.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/2.0.0/dotnet-host-ubuntu.16.04-x64.latest.deb
[ubuntu-16.04-hostfxr-2.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/2.0.0/dotnet-hostfxr-ubuntu.16.04-x64.latest.deb
[ubuntu-16.04-sharedfx-2.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/2.0.0/dotnet-sharedframework-ubuntu.16.04-x64.latest.deb

[ubuntu-16.10-badge-2.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/2.0.0/sharedfx_ubuntu.16.10-x64_Release_version_badge.svg
[ubuntu-16.10-version-2.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/2.0.0/latest.sharedfx.ubuntu.16.10.x64.version
[ubuntu-16.10-host-2.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/2.0.0/dotnet-host-ubuntu.16.10-x64.latest.deb
[ubuntu-16.10-hostfxr-2.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/2.0.0/dotnet-hostfxr-ubuntu.16.10-x64.latest.deb
[ubuntu-16.10-sharedfx-2.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/2.0.0/dotnet-sharedframework-ubuntu.16.10-x64.latest.deb

[debian-8.2-badge-2.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/2.0.0/sharedfx_debian.8-x64_Release_version_badge.svg
[debian-8.2-version-2.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/2.0.0/latest.sharedfx.debian.8.x64.version
[debian-8.2-host-2.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/2.0.0/dotnet-host-debian.8-x64.latest.deb
[debian-8.2-hostfxr-2.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/2.0.0/dotnet-hostfxr-debian.8-x64.latest.deb
[debian-8.2-sharedfx-2.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/2.0.0/dotnet-sharedframework-debian.8-x64.latest.deb

[rhel7-badge-2.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/2.0.0/sharedfx_rhel.7-x64_Release_version_badge.svg
[rhel7-version-2.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/2.0.0/latest.sharedfx.rhel.7.x64.version
[rhel7-host-2.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/2.0.0/dotnet-host-rhel.7-x64.latest.rpm
[rhel7-hostfxr-2.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/2.0.0/dotnet-hostfxr-rhel.7-x64.latest.rpm
[rhel7-sharedfx-2.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/2.0.0/dotnet-sharedframework-rhel.7-x64.latest.rpm

# Debian daily feed

Newest Runtime binaries for 2.0.0 in debian feed may be delayed due to external issues by up to 24h.

## Obtaining binaries

Add debian feed:

For ubuntu 14.04 : trusty , ubuntu 16.04:xenial
```
sudo sh -c 'echo "deb [arch=amd64] http://apt-mo.trafficmanager.net/repos/dotnet/ trusty main" > /etc/apt/sources.list.d/dotnetdev.list'

sudo apt-key adv --keyserver apt-mo.trafficmanager.net --recv-keys 417A0893

sudo apt-get update
```

Install:
```
sudo apt-get install <DebianPackageName>=<Version>
```

To list available packages:
```
apt-cache search dotnet-sharedframework | grep 2.0.0
```
