.NET Core Runtime & Host Setup Repo
===================================

This repo contains the code to build the .NET Core runtime, libraries and shared host (`dotnet`) installers for 
all supported platforms. It **does not** contain the actual sources to .NET Core runtime; this source is split across 
the dotnet/coreclr repo (runtime) and dotnet/corefx repo (libraries). 

## Installation experience
The all-up installation experience is described in the [installation scenarios](https://github.com/dotnet/cli/blob/rel/1.0.0/Documentation/cli-installation-scenarios.md) 
document in the dotnet/cli repo. That is the first step to get acquantied with the overall plan and experience we have
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

|   Platform   |   Master<br>[![][build-badge-master]][build-master]   |   Release/1.1.X<br>[![][build-badge-1.X.X]][build-1.X.X]   |   Release/1.0.X<br>[![][build-badge-1.0.X]][build-1.0.X]   |
|---------|:----------:|:----------:|:----------:|
|   **Windows x64**    |   [![][win-x64-badge-master]][win-x64-version-master]<br>[Installer][win-x64-installer-master]<br>[zip][win-x64-zip-master]   |   [![][win-x64-badge-1.1.X]][win-x64-version-1.1.X]<br>[Installer][win-x64-installer-1.1.X]<br>[zip][win-x64-zip-1.1.X]   |   [![][win-x64-badge-preview]][win-x64-version-preview]<br>[Installer][win-x64-installer-preview]<br>[zip][win-x64-zip-preview]   |
|   **Windows x86**    |   [![][win-x86-badge-master]][win-x86-version-master]<br>[Installer][win-x86-installer-master]<br>[zip][win-x86-zip-master]   |   [![][win-x86-badge-1.1.X]][win-x86-version-1.1.X]<br>[Installer][win-x86-installer-1.1.X]<br>[zip][win-x86-zip-1.1.X]   |   [![][win-x86-badge-preview]][win-x86-version-preview]<br>[Installer][win-x86-installer-preview]<br>[zip][win-x86-zip-preview]   |
|   **Windows arm32**  |   [![][win-arm-badge-master]][win-arm-version-master]<br>[zip][win-arm-zip-master]   |   N/A   |   N/A   |
|   **Windows arm64**  |   [![][win-arm64-badge-master]][win-arm64-version-master]<br>[zip][win-arm64-zip-master]   |   N/A   |   N/A   |
|   **Mac OS X**       |   [![][osx-badge-master]][osx-version-master]<br>[Installer][osx-installer-master]<br>[tar.gz][osx-targz-master]   |   [![][osx-badge-1.1.X]][osx-version-1.1.X]<br>[Installer][osx-installer-1.1.X]<br>[tar.gz][osx-targz-1.1.X]   |   [![][osx-badge-preview]][osx-version-preview]<br>[Installer][osx-installer-preview]<br>[tar.gz][osx-targz-preview]   |
|   **Linux X64** (for glibc based OS)  |   [![][linux-x64-badge-master]][linux-x64-version-master]<br>[zip][linux-x64-zip-master]   |   N/A   |   N/A   |
|   **Ubuntu 14.04**   |   [![][ubuntu-14.04-badge-master]][ubuntu-14.04-version-master]<br>[Host][ubuntu-14.04-host-master]<br>[Host FX Resolver][ubuntu-14.04-hostfxr-master]<br>[Shared Framework][ubuntu-14.04-sharedfx-master]<br>[tar.gz][ubuntu-14.04-targz-master]   |   [![][ubuntu-14.04-badge-1.1.X]][ubuntu-14.04-version-1.1.X]<br>[Host][ubuntu-14.04-host-1.1.X]<br>[Host FX Resolver][ubuntu-14.04-hostfxr-1.1.X]<br>[Shared Framework][ubuntu-14.04-sharedfx-1.1.X]<br>[tar.gz][ubuntu-14.04-targz-1.1.X]   |   [![][ubuntu-14.04-badge-preview]][ubuntu-14.04-version-preview]<br>[Host][ubuntu-14.04-host-preview]<br>[Host FX Resolver][ubuntu-14.04-hostfxr-preview]<br>[Shared Framework][ubuntu-14.04-sharedfx-preview]<br>[tar.gz][ubuntu-14.04-targz-preview]   |
|   **Ubuntu 16.04**   |   [![][ubuntu-16.04-badge-master]][ubuntu-16.04-version-master]<br>[Host][ubuntu-16.04-host-master]<br>[Host FX Resolver][ubuntu-16.04-hostfxr-master]<br>[Shared Framework][ubuntu-16.04-sharedfx-master]<br>[tar.gz][ubuntu-16.04-targz-master]   |   [![][ubuntu-16.04-badge-1.1.X]][ubuntu-16.04-version-1.1.X]<br>[Host][ubuntu-16.04-host-1.1.X]<br>[Host FX Resolver][ubuntu-16.04-hostfxr-1.1.X]<br>[Shared Framework][ubuntu-16.04-sharedfx-1.1.X]<br>[tar.gz][ubuntu-16.04-targz-1.1.X]   |   [![][ubuntu-16.04-badge-preview]][ubuntu-16.04-version-preview]<br>[Host][ubuntu-16.04-host-preview]<br>[Host FX Resolver][ubuntu-16.04-hostfxr-preview]<br>[Shared Framework][ubuntu-16.04-sharedfx-preview]<br>[tar.gz][ubuntu-16.04-targz-preview]   |
|   **Ubuntu 16.10**   |   [![][ubuntu-16.10-badge-master]][ubuntu-16.10-version-master]<br>[Host][ubuntu-16.10-host-master]<br>[Host FX Resolver][ubuntu-16.10-hostfxr-master]<br>[Shared Framework][ubuntu-16.10-sharedfx-master]<br>[tar.gz][ubuntu-16.10-targz-master]   |   [![][ubuntu-16.10-badge-1.1.X]][ubuntu-16.10-version-1.1.X]<br>[Host][ubuntu-16.10-host-1.1.X]<br>[Host FX Resolver][ubuntu-16.10-hostfxr-1.1.X]<br>[Shared Framework][ubuntu-16.10-sharedfx-1.1.X]<br>[tar.gz][ubuntu-16.10-targz-1.1.X]   |   N/A   |
|   **Debian 8.2**     |   [![][debian-8.2-badge-master]][debian-8.2-version-master]<br>[tar.gz][debian-8.2-targz-master]   |   [![][debian-8.2-badge-1.1.X]][debian-8.2-version-1.1.X]<br>[tar.gz][debian-8.2-targz-1.1.X]   |   [![][debian-8.2-badge-preview]][debian-8.2-version-preview]<br>[tar.gz][debian-8.2-targz-preview]   |
|   **CentOS 7.1**     |   [![][centos-badge-master]][centos-version-master]<br>[tar.gz][centos-targz-master]   |   [![][centos-badge-1.1.X]][centos-version-1.1.X]<br>[tar.gz][centos-targz-1.1.X]   |   [![][centos-badge-preview]][centos-version-preview]<br>[tar.gz][centos-targz-preview]   |
|   **RHEL 7.2**       |   [![][rhel-badge-master]][rhel-version-master]<br>[tar.gz][rhel-targz-master]   |   [![][rhel-badge-1.1.X]][rhel-version-1.1.X]<br>[tar.gz][rhel-targz-1.1.X]   |   [![][rhel-badge-preview]][rhel-version-preview]<br>[tar.gz][rhel-targz-preview]   |
|   **Fedora 23**      |   [![][fedora-23-badge-master]][fedora-23-version-master]<br>[tar.gz][fedora-23-targz-master]   |   [![][fedora-23-badge-1.1.X]][fedora-23-version-1.1.X]<br>[tar.gz][fedora-23-targz-1.1.X]   |   [![][fedora-23-badge-preview]][fedora-23-version-preview]<br>[tar.gz][fedora-23-targz-preview]   |
|   **Fedora 24**      |   [![][fedora-24-badge-master]][fedora-24-version-master]<br>[tar.gz][fedora-24-targz-master]   |   [![][fedora-24-badge-1.1.X]][fedora-24-version-1.1.X]<br>[tar.gz][fedora-24-targz-1.1.X]   |   N/A   |
|   **OpenSUSE 13.2**  |   [![][opensuse-13.2-badge-master]][opensuse-13.2-version-master]<br>[tar.gz][opensuse-13.2-targz-master]   |   [![][opensuse-13.2-badge-1.1.X]][opensuse-13.2-version-1.1.X]<br>[tar.gz][opensuse-13.2-targz-1.1.X]   |   [![][opensuse-13.2-badge-preview]][opensuse-13.2-version-preview]<br>[tar.gz][opensuse-13.2-targz-preview]   |
|   **OpenSUSE 42.1**  |   [![][opensuse-42.1-badge-master]][opensuse-42.1-version-master]<br>[tar.gz][opensuse-42.1-targz-master]   |   [![][opensuse-42.1-badge-1.1.X]][opensuse-42.1-version-1.1.X]<br>[tar.gz][opensuse-42.1-targz-1.1.X]   |   N/A   |

*Note: Our .deb packages are put together slightly differently than the other OS specific installers. Instead of combining everything, we have separate component packages that depend on each other. If you're installing these directly from the .deb files (via dpkg or similar), then you'll need to install them in the order presented above.*

[build-badge-master]: https://devdiv.visualstudio.com/_apis/public/build/definitions/0bdbc590-a062-4c3f-b0f6-9383f67865ee/3160/badge
[build-master]: https://devdiv.visualstudio.com/DevDiv/_build/index?definitionId=3160&_a=completed

[build-badge-1.X.X]: https://devdiv.visualstudio.com/_apis/public/build/definitions/0bdbc590-a062-4c3f-b0f6-9383f67865ee/4188/badge
[build-1.X.X]: https://devdiv.visualstudio.com/DevDiv/_build/index?definitionId=4188&_a=completed

[build-badge-1.0.X]: https://devdiv.visualstudio.com/_apis/public/build/definitions/0bdbc590-a062-4c3f-b0f6-9383f67865ee/4187/badge
[build-1.0.X]: https://devdiv.visualstudio.com/DevDiv/_build/index?definitionId=4187&_a=completed


[win-x64-badge-master]: https://dotnetcli.blob.core.windows.net/dotnet/master/Binaries/Latest/sharedfx_Windows_x64_Release_version_badge.svg
[win-x64-version-master]: https://dotnetcli.blob.core.windows.net/dotnet/master/dnvm/latest.sharedfx.win.x64.version
[win-x64-installer-master]: https://dotnetcli.blob.core.windows.net/dotnet/master/Installers/Latest/dotnet-win-x64.latest.exe
[win-x64-zip-master]: https://dotnetcli.blob.core.windows.net/dotnet/master/Binaries/Latest/dotnet-win-x64.latest.zip

[win-x64-badge-1.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/release/1.1.0/Binaries/Latest/sharedfx_Windows_x64_Release_version_badge.svg
[win-x64-version-1.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/release/1.1.0/dnvm/latest.sharedfx.win.x64.version
[win-x64-installer-1.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/release/1.1.0/Installers/Latest/dotnet-win-x64.latest.exe
[win-x64-zip-1.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/release/1.1.0/Binaries/Latest/dotnet-win-x64.latest.zip

[win-x64-badge-preview]: https://dotnetcli.blob.core.windows.net/dotnet/preview/Binaries/Latest/sharedfx_Windows_x64_Release_version_badge.svg
[win-x64-version-preview]: https://dotnetcli.blob.core.windows.net/dotnet/preview/dnvm/latest.sharedfx.win.x64.version
[win-x64-installer-preview]: https://dotnetcli.blob.core.windows.net/dotnet/preview/Installers/Latest/dotnet-win-x64.latest.exe
[win-x64-zip-preview]: https://dotnetcli.blob.core.windows.net/dotnet/preview/Binaries/Latest/dotnet-win-x64.latest.zip

[win-x86-badge-master]: https://dotnetcli.blob.core.windows.net/dotnet/master/Binaries/Latest/sharedfx_Windows_x86_Release_version_badge.svg
[win-x86-version-master]: https://dotnetcli.blob.core.windows.net/dotnet/master/dnvm/latest.sharedfx.win.x86.version
[win-x86-installer-master]: https://dotnetcli.blob.core.windows.net/dotnet/master/Installers/Latest/dotnet-win-x86.latest.exe
[win-x86-zip-master]: https://dotnetcli.blob.core.windows.net/dotnet/master/Binaries/Latest/dotnet-win-x86.latest.zip

[win-x86-badge-1.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/release/1.1.0/Binaries/Latest/sharedfx_Windows_x86_Release_version_badge.svg
[win-x86-version-1.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/release/1.1.0/dnvm/latest.sharedfx.win.x86.version
[win-x86-installer-1.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/release/1.1.0/Installers/Latest/dotnet-win-x86.latest.exe
[win-x86-zip-1.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/release/1.1.0/Binaries/Latest/dotnet-win-x86.latest.zip

[win-x86-badge-preview]: https://dotnetcli.blob.core.windows.net/dotnet/preview/Binaries/Latest/sharedfx_Windows_x86_Release_version_badge.svg
[win-x86-version-preview]: https://dotnetcli.blob.core.windows.net/dotnet/preview/dnvm/latest.sharedfx.win.x86.version
[win-x86-installer-preview]: https://dotnetcli.blob.core.windows.net/dotnet/preview/Installers/Latest/dotnet-win-x86.latest.exe
[win-x86-zip-preview]: https://dotnetcli.blob.core.windows.net/dotnet/preview/Binaries/Latest/dotnet-win-x86.latest.zip

[win-arm-badge-master]: https://dotnetcli.blob.core.windows.net/dotnet/master/Binaries/Latest/sharedfx_Windows_arm_Release_version_badge.svg
[win-arm-version-master]: https://dotnetcli.blob.core.windows.net/dotnet/master/dnvm/latest.sharedfx.win.arm.version
[win-arm-zip-master]: https://dotnetcli.blob.core.windows.net/dotnet/master/Binaries/Latest/dotnet-win-arm.latest.zip

[win-arm64-badge-master]: https://dotnetcli.blob.core.windows.net/dotnet/master/Binaries/Latest/sharedfx_Windows_arm64_Release_version_badge.svg
[win-arm64-version-master]: https://dotnetcli.blob.core.windows.net/dotnet/master/dnvm/latest.sharedfx.win.arm64.version
[win-arm64-zip-master]: https://dotnetcli.blob.core.windows.net/dotnet/master/Binaries/Latest/dotnet-win-arm64.latest.zip


[osx-badge-master]: https://dotnetcli.blob.core.windows.net/dotnet/master/Binaries/Latest/sharedfx_OSX_x64_Release_version_badge.svg
[osx-version-master]: https://dotnetcli.blob.core.windows.net/dotnet/master/dnvm/latest.sharedfx.osx.x64.version
[osx-installer-master]: https://dotnetcli.blob.core.windows.net/dotnet/master/Installers/Latest/dotnet-osx-x64.latest.pkg
[osx-targz-master]: https://dotnetcli.blob.core.windows.net/dotnet/master/Binaries/Latest/dotnet-osx-x64.latest.tar.gz

[osx-badge-1.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/release/1.1.0/Binaries/Latest/sharedfx_OSX_x64_Release_version_badge.svg
[osx-version-1.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/release/1.1.0/dnvm/latest.sharedfx.osx.x64.version
[osx-installer-1.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/release/1.1.0/Installers/Latest/dotnet-osx-x64.latest.pkg
[osx-targz-1.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/release/1.1.0/Binaries/Latest/dotnet-osx-x64.latest.tar.gz

[osx-badge-preview]: https://dotnetcli.blob.core.windows.net/dotnet/preview/Binaries/Latest/sharedfx_OSX_x64_Release_version_badge.svg
[osx-version-preview]: https://dotnetcli.blob.core.windows.net/dotnet/preview/dnvm/latest.sharedfx.osx.x64.version
[osx-installer-preview]: https://dotnetcli.blob.core.windows.net/dotnet/preview/Installers/Latest/dotnet-osx-x64.latest.pkg
[osx-targz-preview]: https://dotnetcli.blob.core.windows.net/dotnet/preview/Binaries/Latest/dotnet-osx-x64.latest.tar.gz

[linux-x64-badge-master]: https://dotnetcli.blob.core.windows.net/dotnet/master/Binaries/Latest/sharedfx_Linux_x64_Release_version_badge.svg
[linux-x64-version-master]: https://dotnetcli.blob.core.windows.net/dotnet/master/dnvm/latest.sharedfx.linux.x64.version
[linux-x64-zip-master]: https://dotnetcli.blob.core.windows.net/dotnet/master/Binaries/Latest/dotnet-linux-x64.latest.tar.gz

[ubuntu-14.04-badge-master]: https://dotnetcli.blob.core.windows.net/dotnet/master/Binaries/Latest/sharedfx_Ubuntu_x64_Release_version_badge.svg
[ubuntu-14.04-version-master]: https://dotnetcli.blob.core.windows.net/dotnet/master/dnvm/latest.sharedfx.ubuntu.x64.version
[ubuntu-14.04-host-master]: https://dotnetcli.blob.core.windows.net/dotnet/master/Installers/Latest/dotnet-host-ubuntu-x64.latest.deb
[ubuntu-14.04-hostfxr-master]: https://dotnetcli.blob.core.windows.net/dotnet/master/Installers/Latest/dotnet-hostfxr-ubuntu-x64.latest.deb
[ubuntu-14.04-sharedfx-master]: https://dotnetcli.blob.core.windows.net/dotnet/master/Installers/Latest/dotnet-sharedframework-ubuntu-x64.latest.deb
[ubuntu-14.04-targz-master]: https://dotnetcli.blob.core.windows.net/dotnet/master/Binaries/Latest/dotnet-ubuntu-x64.latest.tar.gz

[ubuntu-14.04-badge-1.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/release/1.1.0/Binaries/Latest/sharedfx_Ubuntu_x64_Release_version_badge.svg
[ubuntu-14.04-version-1.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/release/1.1.0/dnvm/latest.sharedfx.ubuntu.x64.version
[ubuntu-14.04-host-1.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/release/1.1.0/Installers/Latest/dotnet-host-ubuntu-x64.latest.deb
[ubuntu-14.04-hostfxr-1.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/release/1.1.0/Installers/Latest/dotnet-hostfxr-ubuntu-x64.latest.deb
[ubuntu-14.04-sharedfx-1.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/release/1.1.0/Installers/Latest/dotnet-sharedframework-ubuntu-x64.latest.deb
[ubuntu-14.04-targz-1.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/release/1.1.0/Binaries/Latest/dotnet-ubuntu-x64.latest.tar.gz

[ubuntu-14.04-badge-preview]: https://dotnetcli.blob.core.windows.net/dotnet/preview/Binaries/Latest/sharedfx_Ubuntu_x64_Release_version_badge.svg
[ubuntu-14.04-version-preview]: https://dotnetcli.blob.core.windows.net/dotnet/preview/dnvm/latest.sharedfx.ubuntu.x64.version
[ubuntu-14.04-host-preview]: https://dotnetcli.blob.core.windows.net/dotnet/preview/Installers/Latest/dotnet-host-ubuntu-x64.latest.deb
[ubuntu-14.04-hostfxr-preview]: https://dotnetcli.blob.core.windows.net/dotnet/preview/Installers/Latest/dotnet-hostfxr-ubuntu-x64.latest.deb
[ubuntu-14.04-sharedfx-preview]: https://dotnetcli.blob.core.windows.net/dotnet/preview/Installers/Latest/dotnet-sharedframework-ubuntu-x64.latest.deb
[ubuntu-14.04-targz-preview]: https://dotnetcli.blob.core.windows.net/dotnet/preview/Binaries/Latest/dotnet-ubuntu-x64.latest.tar.gz


[ubuntu-16.04-badge-master]: https://dotnetcli.blob.core.windows.net/dotnet/master/Binaries/Latest/sharedfx_Ubuntu_16_04_x64_Release_version_badge.svg
[ubuntu-16.04-version-master]: https://dotnetcli.blob.core.windows.net/dotnet/master/dnvm/latest.sharedfx.ubuntu.16.04.x64.version
[ubuntu-16.04-host-master]: https://dotnetcli.blob.core.windows.net/dotnet/master/Installers/Latest/dotnet-host-ubuntu.16.04-x64.latest.deb
[ubuntu-16.04-hostfxr-master]: https://dotnetcli.blob.core.windows.net/dotnet/master/Installers/Latest/dotnet-hostfxr-ubuntu.16.04-x64.latest.deb
[ubuntu-16.04-sharedfx-master]: https://dotnetcli.blob.core.windows.net/dotnet/master/Installers/Latest/dotnet-sharedframework-ubuntu.16.04-x64.latest.deb
[ubuntu-16.04-targz-master]: https://dotnetcli.blob.core.windows.net/dotnet/master/Binaries/Latest/dotnet-ubuntu.16.04-x64.latest.tar.gz

[ubuntu-16.04-badge-1.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/release/1.1.0/Binaries/Latest/sharedfx_Ubuntu_16_04_x64_Release_version_badge.svg
[ubuntu-16.04-version-1.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/release/1.1.0/dnvm/latest.sharedfx.ubuntu.16.04.x64.version
[ubuntu-16.04-host-1.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/release/1.1.0/Installers/Latest/dotnet-host-ubuntu.16.04-x64.latest.deb
[ubuntu-16.04-hostfxr-1.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/release/1.1.0/Installers/Latest/dotnet-hostfxr-ubuntu.16.04-x64.latest.deb
[ubuntu-16.04-sharedfx-1.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/release/1.1.0/Installers/Latest/dotnet-sharedframework-ubuntu.16.04-x64.latest.deb
[ubuntu-16.04-targz-1.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/release/1.1.0/Binaries/Latest/dotnet-ubuntu.16.04-x64.latest.tar.gz

[ubuntu-16.04-badge-preview]: https://dotnetcli.blob.core.windows.net/dotnet/preview/Binaries/Latest/sharedfx_Ubuntu_16_04_x64_Release_version_badge.svg
[ubuntu-16.04-version-preview]: https://dotnetcli.blob.core.windows.net/dotnet/preview/dnvm/latest.sharedfx.ubuntu.16.04.x64.version
[ubuntu-16.04-host-preview]: https://dotnetcli.blob.core.windows.net/dotnet/preview/Installers/Latest/dotnet-host-ubuntu.16.04-x64.latest.deb
[ubuntu-16.04-hostfxr-preview]: https://dotnetcli.blob.core.windows.net/dotnet/preview/Installers/Latest/dotnet-hostfxr-ubuntu.16.04-x64.latest.deb
[ubuntu-16.04-sharedfx-preview]: https://dotnetcli.blob.core.windows.net/dotnet/preview/Installers/Latest/dotnet-sharedframework-ubuntu.16.04-x64.latest.deb
[ubuntu-16.04-targz-preview]: https://dotnetcli.blob.core.windows.net/dotnet/preview/Binaries/Latest/dotnet-ubuntu.16.04-x64.latest.tar.gz


[ubuntu-16.10-badge-1.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/release/1.1.0/Binaries/Latest/sharedfx_Ubuntu_16_10_x64_Release_version_badge.svg
[ubuntu-16.10-version-1.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/release/1.1.0/dnvm/latest.sharedfx.ubuntu.16.10.x64.version
[ubuntu-16.10-host-1.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/release/1.1.0/Installers/Latest/dotnet-host-ubuntu.16.10-x64.latest.deb
[ubuntu-16.10-hostfxr-1.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/release/1.1.0/Installers/Latest/dotnet-hostfxr-ubuntu.16.10-x64.latest.deb
[ubuntu-16.10-sharedfx-1.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/release/1.1.0/Installers/Latest/dotnet-sharedframework-ubuntu.16.10-x64.latest.deb
[ubuntu-16.10-targz-1.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/release/1.1.0/Binaries/Latest/dotnet-ubuntu.16.10-x64.latest.tar.gz

[ubuntu-16.10-badge-master]: https://dotnetcli.blob.core.windows.net/dotnet/master/Binaries/Latest/sharedfx_Ubuntu_16_10_x64_Release_version_badge.svg
[ubuntu-16.10-version-master]: https://dotnetcli.blob.core.windows.net/dotnet/master/dnvm/latest.sharedfx.ubuntu.16.10.x64.version
[ubuntu-16.10-host-master]: https://dotnetcli.blob.core.windows.net/dotnet/master/Installers/Latest/dotnet-host-ubuntu.16.10-x64.latest.deb
[ubuntu-16.10-hostfxr-master]: https://dotnetcli.blob.core.windows.net/dotnet/master/Installers/Latest/dotnet-hostfxr-ubuntu.16.10-x64.latest.deb
[ubuntu-16.10-sharedfx-master]: https://dotnetcli.blob.core.windows.net/dotnet/master/Installers/Latest/dotnet-sharedframework-ubuntu.16.10-x64.latest.deb
[ubuntu-16.10-targz-master]: https://dotnetcli.blob.core.windows.net/dotnet/master/Binaries/Latest/dotnet-ubuntu.16.10-x64.latest.tar.gz


[debian-8.2-badge-master]: https://dotnetcli.blob.core.windows.net/dotnet/master/Binaries/Latest/sharedfx_Debian_x64_Release_version_badge.svg
[debian-8.2-version-master]: https://dotnetcli.blob.core.windows.net/dotnet/master/dnvm/latest.sharedfx.debian.x64.version
[debian-8.2-targz-master]: https://dotnetcli.blob.core.windows.net/dotnet/master/Binaries/Latest/dotnet-debian-x64.latest.tar.gz

[debian-8.2-badge-1.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/release/1.1.0/Binaries/Latest/sharedfx_Debian_x64_Release_version_badge.svg
[debian-8.2-version-1.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/release/1.1.0/dnvm/latest.sharedfx.debian.x64.version
[debian-8.2-targz-1.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/release/1.1.0/Binaries/Latest/dotnet-debian-x64.latest.tar.gz

[debian-8.2-badge-preview]: https://dotnetcli.blob.core.windows.net/dotnet/preview/Binaries/Latest/sharedfx_Debian_x64_Release_version_badge.svg
[debian-8.2-version-preview]: https://dotnetcli.blob.core.windows.net/dotnet/preview/dnvm/latest.sharedfx.debian.x64.version
[debian-8.2-targz-preview]: https://dotnetcli.blob.core.windows.net/dotnet/preview/Binaries/Latest/dotnet-debian-x64.latest.tar.gz


[centos-badge-master]: https://dotnetcli.blob.core.windows.net/dotnet/master/Binaries/Latest/sharedfx_CentOS_x64_Release_version_badge.svg
[centos-version-master]: https://dotnetcli.blob.core.windows.net/dotnet/master/dnvm/latest.sharedfx.centos.x64.version
[centos-targz-master]: https://dotnetcli.blob.core.windows.net/dotnet/master/Binaries/Latest/dotnet-centos-x64.latest.tar.gz

[centos-badge-1.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/release/1.1.0/Binaries/Latest/sharedfx_CentOS_x64_Release_version_badge.svg
[centos-version-1.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/release/1.1.0/dnvm/latest.sharedfx.centos.x64.version
[centos-targz-1.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/release/1.1.0/Binaries/Latest/dotnet-centos-x64.latest.tar.gz

[centos-badge-preview]: https://dotnetcli.blob.core.windows.net/dotnet/preview/Binaries/Latest/sharedfx_CentOS_x64_Release_version_badge.svg
[centos-version-preview]: https://dotnetcli.blob.core.windows.net/dotnet/preview/dnvm/latest.sharedfx.centos.x64.version
[centos-targz-preview]: https://dotnetcli.blob.core.windows.net/dotnet/preview/Binaries/Latest/dotnet-centos-x64.latest.tar.gz


[rhel-badge-master]: https://dotnetcli.blob.core.windows.net/dotnet/master/Binaries/Latest/sharedfx_RHEL_x64_Release_version_badge.svg
[rhel-version-master]: https://dotnetcli.blob.core.windows.net/dotnet/master/dnvm/latest.sharedfx.rhel.x64.version
[rhel-targz-master]: https://dotnetcli.blob.core.windows.net/dotnet/master/Binaries/Latest/dotnet-rhel-x64.latest.tar.gz

[rhel-badge-1.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/release/1.1.0/Binaries/Latest/sharedfx_RHEL_x64_Release_version_badge.svg
[rhel-version-1.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/release/1.1.0/dnvm/latest.sharedfx.rhel.x64.version
[rhel-targz-1.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/release/1.1.0/Binaries/Latest/dotnet-rhel-x64.latest.tar.gz

[rhel-badge-preview]: https://dotnetcli.blob.core.windows.net/dotnet/preview/Binaries/Latest/sharedfx_RHEL_x64_Release_version_badge.svg
[rhel-version-preview]: https://dotnetcli.blob.core.windows.net/dotnet/preview/dnvm/latest.sharedfx.rhel.x64.version
[rhel-targz-preview]: https://dotnetcli.blob.core.windows.net/dotnet/preview/Binaries/Latest/dotnet-rhel-x64.latest.tar.gz


[fedora-23-badge-master]: https://dotnetcli.blob.core.windows.net/dotnet/master/Binaries/Latest/sharedfx_Fedora_23_x64_Release_version_badge.svg
[fedora-23-version-master]: https://dotnetcli.blob.core.windows.net/dotnet/master/dnvm/latest.sharedfx.fedora.x64.version
[fedora-23-targz-master]: https://dotnetcli.blob.core.windows.net/dotnet/master/Binaries/Latest/dotnet-fedora-x64.latest.tar.gz

[fedora-23-badge-1.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/release/1.1.0/Binaries/Latest/sharedfx_Fedora_23_x64_Release_version_badge.svg
[fedora-23-version-1.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/release/1.1.0/dnvm/latest.sharedfx.fedora.x64.version
[fedora-23-targz-1.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/release/1.1.0/Binaries/Latest/dotnet-fedora-x64.latest.tar.gz

[fedora-23-badge-preview]: https://dotnetcli.blob.core.windows.net/dotnet/preview/Binaries/Latest/sharedfx_Fedora_23_x64_Release_version_badge.svg
[fedora-23-version-preview]: https://dotnetcli.blob.core.windows.net/dotnet/preview/dnvm/latest.sharedfx.fedora.x64.version
[fedora-23-targz-preview]: https://dotnetcli.blob.core.windows.net/dotnet/preview/Binaries/Latest/dotnet-fedora-x64.latest.tar.gz


[fedora-24-badge-master]: https://dotnetcli.blob.core.windows.net/dotnet/master/Binaries/Latest/sharedfx_Fedora_24_x64_Release_version_badge.svg
[fedora-24-version-master]: https://dotnetcli.blob.core.windows.net/dotnet/master/dnvm/latest.sharedfx.fedora.24.x64.version
[fedora-24-targz-master]: https://dotnetcli.blob.core.windows.net/dotnet/master/Binaries/Latest/dotnet-fedora.24-x64.latest.tar.gz

[fedora-24-badge-1.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/release/1.1.0/Binaries/Latest/sharedfx_Fedora_24_x64_Release_version_badge.svg
[fedora-24-version-1.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/release/1.1.0/dnvm/latest.sharedfx.fedora.24.x64.version
[fedora-24-targz-1.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/release/1.1.0/Binaries/Latest/dotnet-fedora.24-x64.latest.tar.gz


[opensuse-13.2-badge-master]: https://dotnetcli.blob.core.windows.net/dotnet/master/Binaries/Latest/sharedfx_openSUSE_13_2_x64_Release_version_badge.svg
[opensuse-13.2-version-master]: https://dotnetcli.blob.core.windows.net/dotnet/master/dnvm/latest.sharedfx.opensuse.13.2.x64.version
[opensuse-13.2-targz-master]: https://dotnetcli.blob.core.windows.net/dotnet/master/Binaries/Latest/dotnet-opensuse.13.2-x64.latest.tar.gz

[opensuse-13.2-badge-1.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/release/1.1.0/Binaries/Latest/sharedfx_openSUSE_13_2_x64_Release_version_badge.svg
[opensuse-13.2-version-1.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/release/1.1.0/dnvm/latest.sharedfx.opensuse.13.2.x64.version
[opensuse-13.2-targz-1.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/release/1.1.0/Binaries/Latest/dotnet-opensuse.13.2-x64.latest.tar.gz

[opensuse-13.2-badge-preview]: https://dotnetcli.blob.core.windows.net/dotnet/preview/Binaries/Latest/sharedfx_openSUSE_13_2_x64_Release_version_badge.svg
[opensuse-13.2-version-preview]: https://dotnetcli.blob.core.windows.net/dotnet/preview/dnvm/latest.sharedfx.opensuse.13.2.x64.version
[opensuse-13.2-targz-preview]: https://dotnetcli.blob.core.windows.net/dotnet/preview/Binaries/Latest/dotnet-opensuse.13.2-x64.latest.tar.gz

[opensuse-42.1-badge-master]: https://dotnetcli.blob.core.windows.net/dotnet/master/Binaries/Latest/sharedfx_openSUSE_42_1_x64_Release_version_badge.svg
[opensuse-42.1-version-master]: https://dotnetcli.blob.core.windows.net/dotnet/master/dnvm/latest.sharedfx.opensuse.42.1.x64.version
[opensuse-42.1-targz-master]: https://dotnetcli.blob.core.windows.net/dotnet/master/Binaries/Latest/dotnet-opensuse.42.1-x64.latest.tar.gz

[opensuse-42.1-badge-1.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/release/1.1.0/Binaries/Latest/sharedfx_openSUSE_42_1_x64_Release_version_badge.svg
[opensuse-42.1-version-1.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/release/1.1.0/dnvm/latest.sharedfx.opensuse.42.1.x64.version
[opensuse-42.1-targz-1.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/release/1.1.0/Binaries/Latest/dotnet-opensuse.42.1-x64.latest.tar.gz
