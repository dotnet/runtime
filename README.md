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

|Platform |Master| Release/1.0.0 |
|---------|:----------:|:----------:|
|   **Windows x64**    |   [![][win-x64-build-badge-master]][win-x64-build-master]<br>[![][win-x64-badge-master]][win-x64-version-master]<br>[Installer][win-x64-installer-master]<br>[zip][win-x64-zip-master]   |   [![][win-x64-build-badge]][win-x64-build]<br>[![][win-x64-badge-preview]][win-x64-version-preview]<br>[Installer][win-x64-installer-preview]<br>[zip][win-x64-zip-preview]   |
|   **Windows x86**    |   [![][win-x86-build-badge-master]][win-x86-build-master]<br>[![][win-x86-badge-master]][win-x86-version-master]<br>[Installer][win-x86-installer-master]<br>[zip][win-x86-zip-master]   |   [![][win-x86-build-badge-master]][win-x86-build-master]<br>[![][win-x86-badge-preview]][win-x86-version-preview]<br>[Installer][win-x86-installer-preview]<br>[zip][win-x86-zip-preview]   |
|   **Windows Arm32**  |   [![][win-arm-build-badge-master]][win-arm-build-master]<br>[![][win-arm-badge-master]][win-arm-version-master]<br>[Installer][win-arm-installer-master]<br>[zip][win-arm-zip-master]   |   N/A   |
|   **Mac OS X**       |   [![][osx-build-badge-master]][osx-build-master]<br>[![][osx-badge-master]][osx-version-master]<br>[Installer][osx-installer-master]<br>[tar.gz][osx-targz-master]   |   [![][osx-build-badge]][osx-build]<br>[![][osx-badge-preview]][osx-version-preview]<br>[Installer][osx-installer-preview]<br>[tar.gz][osx-targz-preview]   |
|   **Ubuntu 14.04**   |   [![][ubuntu-14.04-build-badge-master]][ubuntu-14.04-build-master]<br>[![][ubuntu-14.04-badge-master]][ubuntu-14.04-version-master]<br>[Host][ubuntu-14.04-host-master]<br>[Host FX Resolver][ubuntu-14.04-hostfxr-master]<br>[Shared Framework][ubuntu-14.04-sharedfx-master]<br>[tar.gz][ubuntu-14.04-targz-master]   |   [![][ubuntu-14.04-build-badge]][ubuntu-14.04-build]<br>[![][ubuntu-14.04-badge-preview]][ubuntu-14.04-version-preview]<br>[Host][ubuntu-14.04-host-preview]<br>[Host FX Resolver][ubuntu-14.04-hostfxr-preview]<br>[Shared Framework][ubuntu-14.04-sharedfx-preview]<br>[tar.gz][ubuntu-14.04-targz-preview]   |
|   **Ubuntu 16.04**   |   [![][ubuntu-16.04-build-badge-master]][ubuntu-16.04-build-master]<br>[![][ubuntu-16.04-badge-master]][ubuntu-16.04-version-master]<br>[Host][ubuntu-16.04-host-master]<br>[Host FX Resolver][ubuntu-16.04-hostfxr-master]<br>[Shared Framework][ubuntu-16.04-sharedfx-master]<br>[tar.gz][ubuntu-16.04-targz-master]   |   [![][ubuntu-16.04-build-badge]][ubuntu-16.04-build]<br>[![][ubuntu-16.04-badge-preview]][ubuntu-16.04-version-preview]<br>[Host][ubuntu-16.04-host-preview]<br>[Host FX Resolver][ubuntu-16.04-hostfxr-preview]<br>[Shared Framework][ubuntu-16.04-sharedfx-preview]<br>[tar.gz][ubuntu-16.04-targz-preview]   |
|   **Ubuntu 16.10**   |   [![][ubuntu-16.10-build-badge-master]][ubuntu-16.10-build-master]<br>[![][ubuntu-16.10-badge-master]][ubuntu-16.10-version-master]<br>[Host][ubuntu-16.10-host-master]<br>[Host FX Resolver][ubuntu-16.10-hostfxr-master]<br>[Shared Framework][ubuntu-16.10-sharedfx-master]<br>[tar.gz][ubuntu-16.10-targz-master]   |   N/A   |
|   **Debian 8.2**     |   [![][debian-8.2-build-badge-master]][debian-8.2-build-master]<br>[![][debian-8.2-badge-master]][debian-8.2-version-master]<br>[tar.gz][debian-8.2-targz-master]   |   [![][debian-8.2-build-badge]][debian-8.2-build]<br>[![][debian-8.2-badge-preview]][debian-8.2-version-preview]<br>[tar.gz][debian-8.2-targz-preview]   |
|   **CentOS 7.1**     |   [![][centos-7.1-build-badge-master]][centos-7.1-build-master]<br>[![][centos-badge-master]][centos-version-master]<br>[tar.gz][centos-targz-master]   |   [![][centos-7.1-build-badge]][centos-7.1-build]<br>[![][centos-badge-preview]][centos-version-preview]<br>[tar.gz][centos-targz-preview]   |
|   **RHEL 7.2**       |   [![][rhel-build-badge-master]][rhel-build-master]<br>[![][rhel-badge-master]][rhel-version-master]<br>[tar.gz][rhel-targz-master]   |   [![][rhel-build-badge]][rhel-build]<br>[![][rhel-badge-preview]][rhel-version-preview]<br>[tar.gz][rhel-targz-preview]   |
|   **Fedora 23**      |   [![][fedora-23-build-badge-master]][fedora-23-build-master]<br>[![][fedora-23-badge-master]][fedora-23-version-master]<br>[tar.gz][fedora-23-targz-master]   |   [![][fedora-23-build-badge]][fedora-23-build]<br>[![][fedora-23-badge-preview]][fedora-23-version-preview]<br>[tar.gz][fedora-23-targz-preview]   |
|   **Fedora 24**      |   [![][fedora-24-build-badge-master]][fedora-24-build-master]<br>[![][fedora-24-badge-master]][fedora-24-version-master]<br>[tar.gz][fedora-24-targz-master]   |   N/A   |
|   **OpenSUSE 13.2**  |   [![][opensuse-13.2-build-badge-master]][opensuse-13.2-build-master]<br>[![][opensuse-13.2-badge-master]][opensuse-13.2-version-master]<br>[tar.gz][opensuse-13.2-targz-master]   |   [![][opensuse-13.2-build-badge]][opensuse-13.2-build]<br>[![][opensuse-13.2-badge-preview]][opensuse-13.2-version-preview]<br>[tar.gz][opensuse-13.2-targz-preview]   |
|   **OpenSUSE 42.1**  |   [![][opensuse-42.1-build-badge-master]][opensuse-42.1-build-master]<br>[![][opensuse-42.1-badge-master]][opensuse-42.1-version-master]<br>[tar.gz][opensuse-42.1-targz-master]   |   N/A   |

*Note: Our .deb packages are put together slightly differently than the other OS specific installers. Instead of combining everything, we have separate component packages that depend on each other. If you're installing these directly from the .deb files (via dpkg or similar), then you'll need to install them in the order presented above.*

[win-x64-build-badge-master]: https://mseng.visualstudio.com/_apis/public/build/definitions/d09b7a4d-0a51-4c0e-a15a-07921d5b558f/3548/badge
[win-x64-build-master]: https://mseng.visualstudio.com/dotnetcore/_build?_a=completed&definitionId=3548
[win-x64-badge-master]: https://dotnetcli.blob.core.windows.net/dotnet/master/Binaries/Latest/sharedfx_Windows_x64_Release_version_badge.svg
[win-x64-version-master]: https://dotnetcli.blob.core.windows.net/dotnet/master/dnvm/latest.sharedfx.win.x64.version
[win-x64-installer-master]: https://dotnetcli.blob.core.windows.net/dotnet/master/Installers/Latest/dotnet-win-x64.latest.exe
[win-x64-zip-master]: https://dotnetcli.blob.core.windows.net/dotnet/master/Binaries/Latest/dotnet-win-x64.latest.zip

[win-x64-build-badge]: https://mseng.visualstudio.com/_apis/public/build/definitions/d09b7a4d-0a51-4c0e-a15a-07921d5b558f/3597/badge
[win-x64-build]: https://mseng.visualstudio.com/dotnetcore/_build?_a=completed&definitionId=3597
[win-x64-badge-preview]: https://dotnetcli.blob.core.windows.net/dotnet/preview/Binaries/Latest/sharedfx_Windows_x64_Release_version_badge.svg
[win-x64-version-preview]: https://dotnetcli.blob.core.windows.net/dotnet/preview/dnvm/latest.sharedfx.win.x64.version
[win-x64-installer-preview]: https://dotnetcli.blob.core.windows.net/dotnet/preview/Installers/Latest/dotnet-win-x64.latest.exe
[win-x64-zip-preview]: https://dotnetcli.blob.core.windows.net/dotnet/preview/Binaries/Latest/dotnet-win-x64.latest.zip


[win-x86-build-badge]: https://mseng.visualstudio.com/_apis/public/build/definitions/d09b7a4d-0a51-4c0e-a15a-07921d5b558f/3598/badge
[win-x86-build]: https://mseng.visualstudio.com/dotnetcore/_build?_a=completed&definitionId=3598
[win-x86-build-badge-master]: https://mseng.visualstudio.com/_apis/public/build/definitions/d09b7a4d-0a51-4c0e-a15a-07921d5b558f/3549/badge
[win-x86-build-master]: https://mseng.visualstudio.com/dotnetcore/_build?_a=completed&definitionId=3549

[win-x86-badge-master]: https://dotnetcli.blob.core.windows.net/dotnet/master/Binaries/Latest/sharedfx_Windows_x86_Release_version_badge.svg
[win-x86-version-master]: https://dotnetcli.blob.core.windows.net/dotnet/master/dnvm/latest.sharedfx.win.x86.version
[win-x86-installer-master]: https://dotnetcli.blob.core.windows.net/dotnet/master/Installers/Latest/dotnet-win-x86.latest.exe
[win-x86-zip-master]: https://dotnetcli.blob.core.windows.net/dotnet/master/Binaries/Latest/dotnet-win-x86.latest.zip

[win-x86-badge-preview]: https://dotnetcli.blob.core.windows.net/dotnet/preview/Binaries/Latest/sharedfx_Windows_x86_Release_version_badge.svg
[win-x86-version-preview]: https://dotnetcli.blob.core.windows.net/dotnet/preview/dnvm/latest.sharedfx.win.x86.version
[win-x86-installer-preview]: https://dotnetcli.blob.core.windows.net/dotnet/preview/Installers/Latest/dotnet-win-x86.latest.exe
[win-x86-zip-preview]: https://dotnetcli.blob.core.windows.net/dotnet/preview/Binaries/Latest/dotnet-win-x86.latest.zip


[win-arm-build-badge-master]: https://mseng.visualstudio.com/_apis/public/build/definitions/d09b7a4d-0a51-4c0e-a15a-07921d5b558f/4371/badge
[win-arm-build-master]: https://mseng.visualstudio.com/dotnetcore/_build?_a=completed&definitionId=4371
[win-arm-badge-master]: https://dotnetcli.blob.core.windows.net/dotnet/master/Binaries/Latest/sharedfx_Windows_arm_Release_version_badge.svg
[win-arm-version-master]: https://dotnetcli.blob.core.windows.net/dotnet/master/dnvm/latest.sharedfx.win.arm.version
[win-arm-installer-master]: https://dotnetcli.blob.core.windows.net/dotnet/master/Installers/Latest/dotnet-win-arm.latest.exe
[win-arm-zip-master]: https://dotnetcli.blob.core.windows.net/dotnet/master/Binaries/Latest/dotnet-win-arm.latest.zip


[osx-build-badge-master]: https://mseng.visualstudio.com/_apis/public/build/definitions/d09b7a4d-0a51-4c0e-a15a-07921d5b558f/3544/badge
[osx-build-master]: https://mseng.visualstudio.com/dotnetcore/_build?_a=completed&definitionId=3544
[osx-badge-master]: https://dotnetcli.blob.core.windows.net/dotnet/master/Binaries/Latest/sharedfx_OSX_x64_Release_version_badge.svg
[osx-version-master]: https://dotnetcli.blob.core.windows.net/dotnet/master/dnvm/latest.sharedfx.osx.x64.version
[osx-installer-master]: https://dotnetcli.blob.core.windows.net/dotnet/master/Installers/Latest/dotnet-osx-x64.latest.pkg
[osx-targz-master]: https://dotnetcli.blob.core.windows.net/dotnet/master/Binaries/Latest/dotnet-osx-x64.latest.tar.gz

[osx-build-badge]: https://mseng.visualstudio.com/_apis/public/build/definitions/d09b7a4d-0a51-4c0e-a15a-07921d5b558f/3595/badge
[osx-build]: https://mseng.visualstudio.com/dotnetcore/_build?_a=completed&definitionId=3595
[osx-badge-preview]: https://dotnetcli.blob.core.windows.net/dotnet/preview/Binaries/Latest/sharedfx_OSX_x64_Release_version_badge.svg
[osx-version-preview]: https://dotnetcli.blob.core.windows.net/dotnet/preview/dnvm/latest.sharedfx.osx.x64.version
[osx-installer-preview]: https://dotnetcli.blob.core.windows.net/dotnet/preview/Installers/Latest/dotnet-osx-x64.latest.pkg
[osx-targz-preview]: https://dotnetcli.blob.core.windows.net/dotnet/preview/Binaries/Latest/dotnet-osx-x64.latest.tar.gz


[ubuntu-14.04-build-badge-master]: https://mseng.visualstudio.com/_apis/public/build/definitions/d09b7a4d-0a51-4c0e-a15a-07921d5b558f/3546/badge
[ubuntu-14.04-build-master]: https://mseng.visualstudio.com/dotnetcore/_build?_a=completed&definitionId=3546
[ubuntu-14.04-badge-master]: https://dotnetcli.blob.core.windows.net/dotnet/master/Binaries/Latest/sharedfx_Ubuntu_x64_Release_version_badge.svg
[ubuntu-14.04-version-master]: https://dotnetcli.blob.core.windows.net/dotnet/master/dnvm/latest.sharedfx.ubuntu.x64.version
[ubuntu-14.04-host-master]: https://dotnetcli.blob.core.windows.net/dotnet/master/Installers/Latest/dotnet-host-ubuntu-x64.latest.deb
[ubuntu-14.04-hostfxr-master]: https://dotnetcli.blob.core.windows.net/dotnet/master/Installers/Latest/dotnet-hostfxr-ubuntu-x64.latest.deb
[ubuntu-14.04-sharedfx-master]: https://dotnetcli.blob.core.windows.net/dotnet/master/Installers/Latest/dotnet-sharedframework-ubuntu-x64.latest.deb
[ubuntu-14.04-targz-master]: https://dotnetcli.blob.core.windows.net/dotnet/master/Binaries/Latest/dotnet-ubuntu-x64.latest.tar.gz

[ubuntu-14.04-build-badge]: https://mseng.visualstudio.com/_apis/public/build/definitions/d09b7a4d-0a51-4c0e-a15a-07921d5b558f/3599/badge
[ubuntu-14.04-build]: https://mseng.visualstudio.com/dotnetcore/_build?_a=completed&definitionId=3599
[ubuntu-14.04-badge-preview]: https://dotnetcli.blob.core.windows.net/dotnet/preview/Binaries/Latest/sharedfx_Ubuntu_x64_Release_version_badge.svg
[ubuntu-14.04-version-preview]: https://dotnetcli.blob.core.windows.net/dotnet/preview/dnvm/latest.sharedfx.ubuntu.x64.version
[ubuntu-14.04-host-preview]: https://dotnetcli.blob.core.windows.net/dotnet/preview/Installers/Latest/dotnet-host-ubuntu-x64.latest.deb
[ubuntu-14.04-hostfxr-preview]: https://dotnetcli.blob.core.windows.net/dotnet/preview/Installers/Latest/dotnet-hostfxr-ubuntu-x64.latest.deb
[ubuntu-14.04-sharedfx-preview]: https://dotnetcli.blob.core.windows.net/dotnet/preview/Installers/Latest/dotnet-sharedframework-ubuntu-x64.latest.deb
[ubuntu-14.04-targz-preview]: https://dotnetcli.blob.core.windows.net/dotnet/preview/Binaries/Latest/dotnet-ubuntu-x64.latest.tar.gz


[ubuntu-16.04-build-badge-master]: https://mseng.visualstudio.com/_apis/public/build/definitions/d09b7a4d-0a51-4c0e-a15a-07921d5b558f/3583/badge
[ubuntu-16.04-build-master]: https://mseng.visualstudio.com/dotnetcore/_build?_a=completed&definitionId=3583
[ubuntu-16.04-badge-master]: https://dotnetcli.blob.core.windows.net/dotnet/master/Binaries/Latest/sharedfx_Ubuntu_16_04_x64_Release_version_badge.svg
[ubuntu-16.04-version-master]: https://dotnetcli.blob.core.windows.net/dotnet/master/dnvm/latest.sharedfx.ubuntu.16.04.x64.version
[ubuntu-16.04-host-master]: https://dotnetcli.blob.core.windows.net/dotnet/master/Installers/Latest/dotnet-host-ubuntu.16.04-x64.latest.deb
[ubuntu-16.04-hostfxr-master]: https://dotnetcli.blob.core.windows.net/dotnet/master/Installers/Latest/dotnet-hostfxr-ubuntu.16.04-x64.latest.deb
[ubuntu-16.04-sharedfx-master]: https://dotnetcli.blob.core.windows.net/dotnet/master/Installers/Latest/dotnet-sharedframework-ubuntu.16.04-x64.latest.deb
[ubuntu-16.04-targz-master]: https://dotnetcli.blob.core.windows.net/dotnet/master/Binaries/Latest/dotnet-ubuntu.16.04-x64.latest.tar.gz

[ubuntu-16.04-build-badge]: https://mseng.visualstudio.com/_apis/public/build/definitions/d09b7a4d-0a51-4c0e-a15a-07921d5b558f/3600/badge
[ubuntu-16.04-build]: https://mseng.visualstudio.com/dotnetcore/_build?_a=completed&definitionId=3600
[ubuntu-16.04-badge-preview]: https://dotnetcli.blob.core.windows.net/dotnet/preview/Binaries/Latest/sharedfx_Ubuntu_16_04_x64_Release_version_badge.svg
[ubuntu-16.04-version-preview]: https://dotnetcli.blob.core.windows.net/dotnet/preview/dnvm/latest.sharedfx.ubuntu.16.04.x64.version
[ubuntu-16.04-host-preview]: https://dotnetcli.blob.core.windows.net/dotnet/preview/Installers/Latest/dotnet-host-ubuntu.16.04-x64.latest.deb
[ubuntu-16.04-hostfxr-preview]: https://dotnetcli.blob.core.windows.net/dotnet/preview/Installers/Latest/dotnet-hostfxr-ubuntu.16.04-x64.latest.deb
[ubuntu-16.04-sharedfx-preview]: https://dotnetcli.blob.core.windows.net/dotnet/preview/Installers/Latest/dotnet-sharedframework-ubuntu.16.04-x64.latest.deb
[ubuntu-16.04-targz-preview]: https://dotnetcli.blob.core.windows.net/dotnet/preview/Binaries/Latest/dotnet-ubuntu.16.04-x64.latest.tar.gz


[ubuntu-16.10-build-badge-master]: https://mseng.visualstudio.com/_apis/public/build/definitions/d09b7a4d-0a51-4c0e-a15a-07921d5b558f/4149/badge
[ubuntu-16.10-build-master]: https://mseng.visualstudio.com/dotnetcore/_build?_a=completed&definitionId=4149
[ubuntu-16.10-badge-master]: https://dotnetcli.blob.core.windows.net/dotnet/master/Binaries/Latest/sharedfx_Ubuntu_16_10_x64_Release_version_badge.svg
[ubuntu-16.10-version-master]: https://dotnetcli.blob.core.windows.net/dotnet/master/dnvm/latest.sharedfx.ubuntu.16.10.x64.version
[ubuntu-16.10-host-master]: https://dotnetcli.blob.core.windows.net/dotnet/master/Installers/Latest/dotnet-host-ubuntu.16.10-x64.latest.deb
[ubuntu-16.10-hostfxr-master]: https://dotnetcli.blob.core.windows.net/dotnet/master/Installers/Latest/dotnet-hostfxr-ubuntu.16.10-x64.latest.deb
[ubuntu-16.10-sharedfx-master]: https://dotnetcli.blob.core.windows.net/dotnet/master/Installers/Latest/dotnet-sharedframework-ubuntu.16.10-x64.latest.deb
[ubuntu-16.10-targz-master]: https://dotnetcli.blob.core.windows.net/dotnet/master/Binaries/Latest/dotnet-ubuntu.16.10-x64.latest.tar.gz


[debian-8.2-build-badge-master]: https://mseng.visualstudio.com/_apis/public/build/definitions/d09b7a4d-0a51-4c0e-a15a-07921d5b558f/3543/badge
[debian-8.2-build-master]: https://mseng.visualstudio.com/dotnetcore/_build?_a=completed&definitionId=3543
[debian-8.2-badge-master]: https://dotnetcli.blob.core.windows.net/dotnet/master/Binaries/Latest/sharedfx_Debian_x64_Release_version_badge.svg
[debian-8.2-version-master]: https://dotnetcli.blob.core.windows.net/dotnet/master/dnvm/latest.sharedfx.debian.x64.version
[debian-8.2-targz-master]: https://dotnetcli.blob.core.windows.net/dotnet/master/Binaries/Latest/dotnet-debian-x64.latest.tar.gz

[debian-8.2-build-badge]: https://mseng.visualstudio.com/_apis/public/build/definitions/d09b7a4d-0a51-4c0e-a15a-07921d5b558f/3592/badge
[debian-8.2-build]: https://mseng.visualstudio.com/dotnetcore/_build?_a=completed&definitionId=3592
[debian-8.2-badge-preview]: https://dotnetcli.blob.core.windows.net/dotnet/preview/Binaries/Latest/sharedfx_Debian_x64_Release_version_badge.svg
[debian-8.2-version-preview]: https://dotnetcli.blob.core.windows.net/dotnet/preview/dnvm/latest.sharedfx.debian.x64.version
[debian-8.2-targz-preview]: https://dotnetcli.blob.core.windows.net/dotnet/preview/Binaries/Latest/dotnet-debian-x64.latest.tar.gz


[centos-7.1-build-badge-master]: https://mseng.visualstudio.com/_apis/public/build/definitions/d09b7a4d-0a51-4c0e-a15a-07921d5b558f/3542/badge
[centos-7.1-build-master]: https://mseng.visualstudio.com/dotnetcore/_build?_a=completed&definitionId=3542
[centos-badge-master]: https://dotnetcli.blob.core.windows.net/dotnet/master/Binaries/Latest/sharedfx_CentOS_x64_Release_version_badge.svg
[centos-version-master]: https://dotnetcli.blob.core.windows.net/dotnet/master/dnvm/latest.sharedfx.centos.x64.version
[centos-targz-master]: https://dotnetcli.blob.core.windows.net/dotnet/master/Binaries/Latest/dotnet-centos-x64.latest.tar.gz

[centos-7.1-build-badge]: https://mseng.visualstudio.com/_apis/public/build/definitions/d09b7a4d-0a51-4c0e-a15a-07921d5b558f/3591/badge
[centos-7.1-build]: https://mseng.visualstudio.com/dotnetcore/_build?_a=completed&definitionId=3591
[centos-badge-preview]: https://dotnetcli.blob.core.windows.net/dotnet/preview/Binaries/Latest/sharedfx_CentOS_x64_Release_version_badge.svg
[centos-version-preview]: https://dotnetcli.blob.core.windows.net/dotnet/preview/dnvm/latest.sharedfx.centos.x64.version
[centos-targz-preview]: https://dotnetcli.blob.core.windows.net/dotnet/preview/Binaries/Latest/dotnet-centos-x64.latest.tar.gz


[rhel-build-badge-master]: https://mseng.visualstudio.com/_apis/public/build/definitions/d09b7a4d-0a51-4c0e-a15a-07921d5b558f/3545/badge
[rhel-build-master]: https://mseng.visualstudio.com/dotnetcore/_build?_a=completed&definitionId=3545
[rhel-badge-master]: https://dotnetcli.blob.core.windows.net/dotnet/master/Binaries/Latest/sharedfx_RHEL_x64_Release_version_badge.svg
[rhel-version-master]: https://dotnetcli.blob.core.windows.net/dotnet/master/dnvm/latest.sharedfx.rhel.x64.version
[rhel-targz-master]: https://dotnetcli.blob.core.windows.net/dotnet/master/Binaries/Latest/dotnet-rhel-x64.latest.tar.gz

[rhel-build-badge]: https://mseng.visualstudio.com/_apis/public/build/definitions/d09b7a4d-0a51-4c0e-a15a-07921d5b558f/3596/badge
[rhel-build]: https://mseng.visualstudio.com/dotnetcore/_build?_a=completed&definitionId=3596
[rhel-badge-preview]: https://dotnetcli.blob.core.windows.net/dotnet/preview/Binaries/Latest/sharedfx_RHEL_x64_Release_version_badge.svg
[rhel-version-preview]: https://dotnetcli.blob.core.windows.net/dotnet/preview/dnvm/latest.sharedfx.rhel.x64.version
[rhel-targz-preview]: https://dotnetcli.blob.core.windows.net/dotnet/preview/Binaries/Latest/dotnet-rhel-x64.latest.tar.gz


[fedora-23-build-badge-master]: https://mseng.visualstudio.com/_apis/public/build/definitions/d09b7a4d-0a51-4c0e-a15a-07921d5b558f/3584/badge
[fedora-23-build-master]: https://mseng.visualstudio.com/dotnetcore/_build?_a=completed&definitionId=3584
[fedora-23-badge-master]: https://dotnetcli.blob.core.windows.net/dotnet/master/Binaries/Latest/sharedfx_Fedora_23_x64_Release_version_badge.svg
[fedora-23-version-master]: https://dotnetcli.blob.core.windows.net/dotnet/master/dnvm/latest.sharedfx.fedora.x64.version
[fedora-23-targz-master]: https://dotnetcli.blob.core.windows.net/dotnet/master/Binaries/Latest/dotnet-fedora-x64.latest.tar.gz

[fedora-23-build-badge]: https://mseng.visualstudio.com/_apis/public/build/definitions/d09b7a4d-0a51-4c0e-a15a-07921d5b558f/3593/badge
[fedora-23-build]: https://mseng.visualstudio.com/dotnetcore/_build?_a=completed&definitionId=3593
[fedora-23-badge-preview]: https://dotnetcli.blob.core.windows.net/dotnet/preview/Binaries/Latest/sharedfx_Fedora_23_x64_Release_version_badge.svg
[fedora-23-version-preview]: https://dotnetcli.blob.core.windows.net/dotnet/preview/dnvm/latest.sharedfx.fedora.x64.version
[fedora-23-targz-preview]: https://dotnetcli.blob.core.windows.net/dotnet/preview/Binaries/Latest/dotnet-fedora-x64.latest.tar.gz


[fedora-24-build-badge-master]: https://mseng.visualstudio.com/_apis/public/build/definitions/d09b7a4d-0a51-4c0e-a15a-07921d5b558f/4339/badge
[fedora-24-build-master]: https://mseng.visualstudio.com/dotnetcore/_build?_a=completed&definitionId=4339
[fedora-24-badge-master]: https://dotnetcli.blob.core.windows.net/dotnet/master/Binaries/Latest/sharedfx_Fedora_24_x64_Release_version_badge.svg
[fedora-24-version-master]: https://dotnetcli.blob.core.windows.net/dotnet/master/dnvm/latest.sharedfx.fedora.x64.version
[fedora-24-targz-master]: https://dotnetcli.blob.core.windows.net/dotnet/master/Binaries/Latest/dotnet-fedora-x64.latest.tar.gz


[opensuse-13.2-build-badge-master]: https://mseng.visualstudio.com/_apis/public/build/definitions/d09b7a4d-0a51-4c0e-a15a-07921d5b558f/3587/badge
[opensuse-13.2-build-master]: https://mseng.visualstudio.com/dotnetcore/_build?_a=completed&definitionId=3587
[opensuse-13.2-badge-master]: https://dotnetcli.blob.core.windows.net/dotnet/master/Binaries/Latest/sharedfx_openSUSE_13_2_x64_Release_version_badge.svg
[opensuse-13.2-version-master]: https://dotnetcli.blob.core.windows.net/dotnet/master/dnvm/latest.sharedfx.opensuse.13.2.x64.version
[opensuse-13.2-targz-master]: https://dotnetcli.blob.core.windows.net/dotnet/master/Binaries/Latest/dotnet-opensuse.13.2-x64.latest.tar.gz

[opensuse-13.2-build-badge]: https://mseng.visualstudio.com/_apis/public/build/definitions/d09b7a4d-0a51-4c0e-a15a-07921d5b558f/3594/badge
[opensuse-13.2-build]: https://mseng.visualstudio.com/dotnetcore/_build?_a=completed&definitionId=3594
[opensuse-13.2-badge-preview]: https://dotnetcli.blob.core.windows.net/dotnet/preview/Binaries/Latest/sharedfx_openSUSE_13_2_x64_Release_version_badge.svg
[opensuse-13.2-version-preview]: https://dotnetcli.blob.core.windows.net/dotnet/preview/dnvm/latest.sharedfx.opensuse.13.2.x64.version
[opensuse-13.2-targz-preview]: https://dotnetcli.blob.core.windows.net/dotnet/preview/Binaries/Latest/dotnet-opensuse.13.2-x64.latest.tar.gz

[opensuse-42.1-build-badge-master]: https://mseng.visualstudio.com/_apis/public/build/definitions/d09b7a4d-0a51-4c0e-a15a-07921d5b558f/4147/badge
[opensuse-42.1-build-master]: https://mseng.visualstudio.com/dotnetcore/_build?_a=completed&definitionId=4147
[opensuse-42.1-badge-master]: https://dotnetcli.blob.core.windows.net/dotnet/master/Binaries/Latest/sharedfx_openSUSE_42_1_x64_Release_version_badge.svg
[opensuse-42.1-version-master]: https://dotnetcli.blob.core.windows.net/dotnet/master/dnvm/latest.sharedfx.opensuse.42.1.x64.version
[opensuse-42.1-targz-master]: https://dotnetcli.blob.core.windows.net/dotnet/master/Binaries/Latest/dotnet-opensuse.42.1-x64.latest.tar.gz