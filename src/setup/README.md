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

|   Platform   |   Master<br>[![][build-badge-master]][build-master]   |   Release/2.0.X<br>[![][build-badge-2.0.X]][build-2.0.X]   |   Release/1.1.X<br>[![][build-badge-1.X.X]][build-1.X.X]   |   Release/1.0.X<br>[![][build-badge-1.0.X]][build-1.0.X]   |
|---------|:----------:|:----------:|:----------:|:----------:|
|   **Windows (x64)**    |   [![][win-x64-badge-master]][win-x64-version-master]<br>[Installer][win-x64-installer-master] ([Checksum][win-x64-installer-checksum-master])<br>[zip][win-x64-zip-master] ([Checksum][win-x64-zip-checksum-master])<br>[Symbols (zip)][win-x64-symbols-zip-master]   |   [![][win-x64-badge-2.0.X]][win-x64-version-2.0.X]<br>[Installer][win-x64-installer-2.0.X] ([Checksum][win-x64-installer-checksum-2.0.X])<br>[zip][win-x64-zip-2.0.X] ([Checksum][win-x64-zip-checksum-2.0.X])<br>[Symbols (zip)][win-x64-symbols-zip-2.0.X]   |   [![][win-x64-badge-1.1.X]][win-x64-version-1.1.X]<br>[Installer][win-x64-installer-1.1.X]<br>[zip][win-x64-zip-1.1.X]   |   [![][win-x64-badge-preview]][win-x64-version-preview]<br>[Installer][win-x64-installer-preview]<br>[zip][win-x64-zip-preview]   |
|   **Windows (x86)**    |   [![][win-x86-badge-master]][win-x86-version-master]<br>[Installer][win-x86-installer-master] ([Checksum][win-x86-installer-checksum-master])<br>[zip][win-x86-zip-master] ([Checksum][win-x86-zip-checksum-master])<br>[Symbols (zip)][win-x86-symbols-zip-master]   |   [![][win-x86-badge-2.0.X]][win-x86-version-2.0.X]<br>[Installer][win-x86-installer-2.0.X] ([Checksum][win-x86-installer-checksum-2.0.X])<br>[zip][win-x86-zip-2.0.X] ([Checksum][win-x86-zip-checksum-2.0.X])<br>[Symbols (zip)][win-x86-symbols-zip-2.0.X]   |[![][win-x86-badge-1.1.X]][win-x86-version-1.1.X]<br>[Installer][win-x86-installer-1.1.X]<br>[zip][win-x86-zip-1.1.X]   |   [![][win-x86-badge-preview]][win-x86-version-preview]<br>[Installer][win-x86-installer-preview]<br>[zip][win-x86-zip-preview]   |
|   **Windows (arm32)**  |   [![][win-arm-badge-master]][win-arm-version-master]<br>[zip][win-arm-zip-master] ([Checksum][win-arm-zip-checksum-master])<br>[Symbols (zip)][win-arm-symbols-zip-master]   |   [![][win-arm-badge-2.0.X]][win-arm-version-2.0.X]<br>[zip][win-arm-zip-2.0.X] ([Checksum][win-arm-zip-checksum-2.0.X])<br>[Symbols (zip)][win-arm-symbols-zip-2.0.X]   |   N/A   |   N/A   |
|   **Windows (arm64)**  |   [![][win-arm64-badge-master]][win-arm64-version-master]<br>[zip][win-arm64-zip-master] ([Checksum][win-arm64-zip-checksum-master])<br>[Symbols (zip)][win-arm64-symbols-zip-master]   |   [![][win-arm64-badge-2.0.X]][win-arm64-version-2.0.X]<br>[zip][win-arm64-zip-2.0.X] ([Checksum][win-arm64-zip-checksum-2.0.X])<br>[Symbols (zip)][win-arm64-symbols-zip-2.0.X]   |   N/A   |   N/A   |
|   **Mac OS X (x64)**       |   [![][osx-badge-master]][osx-version-master]<br>[Installer][osx-installer-master] ([Checksum][osx-installer-checksum-master])<br>[tar.gz][osx-targz-master] ([Checksum][osx-targz-checksum-master])<br>[Symbols (tar.gz)][osx-symbols-targz-master]   |   [![][osx-badge-2.0.X]][osx-version-2.0.X]<br>[Installer][osx-installer-2.0.X] ([Checksum][osx-installer-checksum-2.0.X])<br>[tar.gz][osx-targz-2.0.X] ([Checksum][osx-targz-checksum-2.0.X])<br>[Symbols (tar.gz)][osx-symbols-targz-2.0.X]   |   [![][osx-badge-1.1.X]][osx-version-1.1.X]<br>[Installer][osx-installer-1.1.X]<br>[tar.gz][osx-targz-1.1.X]   |   [![][osx-badge-preview]][osx-version-preview]<br>[Installer][osx-installer-preview]<br>[tar.gz][osx-targz-preview]   |
|   **Linux (x64)** (for glibc based OS)  |   [![][linux-x64-badge-master]][linux-x64-version-master]<br>[tar.gz][linux-x64-targz-master] ([Checksum][linux-x64-targz-checksum-master])<br>[Symbols (tar.gz)][linux-x64-symbols-targz-master]   |   [![][linux-x64-badge-2.0.X]][linux-x64-version-2.0.X]<br>[tar.gz][linux-x64-targz-2.0.X] ([Checksum][linux-x64-targz-checksum-2.0.X])<br>[Symbols (tar.gz)][linux-x64-symbols-targz-2.0.X]   |   N/A   |   N/A   |
|   **Linux (armhf)** (for glibc based OS)  |   [![][linux-arm-badge-master]][linux-arm-version-master]<br>[tar.gz][linux-arm-targz-master] ([Checksum][linux-arm-targz-checksum-master])<br>[Symbols (tar.gz)][linux-arm-symbols-targz-master]   |   [![][linux-arm-badge-2.0.X]][linux-arm-version-2.0.X]<br>[tar.gz][linux-arm-targz-2.0.X] ([Checksum][linux-arm-targz-checksum-2.0.X])<br>[Symbols (tar.gz)][linux-arm-symbols-targz-2.0.X]   |   N/A   |   N/A   |
|   **Ubuntu 14.04 (x64)**   |   [![][ubuntu-14.04-badge-master]][ubuntu-14.04-version-master]<br>[Runtime-Deps][ubuntu-14.04-runtime-deps-master] ([Checksum][ubuntu-14.04-runtime-deps-checksum-master])<br>[Host][ubuntu-14.04-host-master] ([Checksum][ubuntu-14.04-host-checksum-master])<br>[Host FX Resolver][ubuntu-14.04-hostfxr-master] ([Checksum][ubuntu-14.04-hostfxr-checksum-master])<br>[Shared Framework][ubuntu-14.04-sharedfx-master] ([Checksum][ubuntu-14.04-sharedfx-checksum-master])<br>  |   [![][ubuntu-14.04-badge-2.0.X]][ubuntu-14.04-version-2.0.X]<br>[Host][ubuntu-14.04-host-2.0.X] ([Checksum][ubuntu-14.04-host-checksum-2.0.X])<br>[Host FX Resolver][ubuntu-14.04-hostfxr-2.0.X] ([Checksum][ubuntu-14.04-hostfxr-checksum-2.0.X])<br>[Shared Framework][ubuntu-14.04-sharedfx-2.0.X] ([Checksum][ubuntu-14.04-sharedfx-checksum-2.0.X])<br>  |   [![][ubuntu-14.04-badge-1.1.X]][ubuntu-14.04-version-1.1.X]<br>[Host][ubuntu-14.04-host-1.1.X]<br>[Host FX Resolver][ubuntu-14.04-hostfxr-1.1.X]<br>[Shared Framework][ubuntu-14.04-sharedfx-1.1.X]<br>[tar.gz][ubuntu-14.04-targz-1.1.X]   |   [![][ubuntu-14.04-badge-preview]][ubuntu-14.04-version-preview]<br>[Host][ubuntu-14.04-host-preview]<br>[Host FX Resolver][ubuntu-14.04-hostfxr-preview]<br>[Shared Framework][ubuntu-14.04-sharedfx-preview]<br>[tar.gz][ubuntu-14.04-targz-preview]   |
|   **Ubuntu 16.04 (x64)**   |   [![][ubuntu-16.04-badge-master]][ubuntu-16.04-version-master]<br>[Runtime-Deps][ubuntu-16.04-runtime-deps-master] ([Checksum][ubuntu-16.04-runtime-deps-checksum-master])<br>[Host][ubuntu-16.04-host-master] ([Checksum][ubuntu-16.04-host-checksum-master])<br>[Host FX Resolver][ubuntu-16.04-hostfxr-master] ([Checksum][ubuntu-16.04-hostfxr-checksum-master])<br>[Shared Framework][ubuntu-16.04-sharedfx-master] ([Checksum][ubuntu-16.04-sharedfx-checksum-master])<br>  |   [![][ubuntu-16.04-badge-2.0.X]][ubuntu-16.04-version-2.0.X]<br>[Host][ubuntu-16.04-host-2.0.X] ([Checksum][ubuntu-16.04-host-checksum-2.0.X])<br>[Host FX Resolver][ubuntu-16.04-hostfxr-2.0.X] ([Checksum][ubuntu-16.04-hostfxr-checksum-2.0.X])<br>[Shared Framework][ubuntu-16.04-sharedfx-2.0.X] ([Checksum][ubuntu-16.04-sharedfx-checksum-2.0.X])<br>  |   [![][ubuntu-16.04-badge-1.1.X]][ubuntu-16.04-version-1.1.X]<br>[Host][ubuntu-16.04-host-1.1.X]<br>[Host FX Resolver][ubuntu-16.04-hostfxr-1.1.X]<br>[Shared Framework][ubuntu-16.04-sharedfx-1.1.X]<br>[tar.gz][ubuntu-16.04-targz-1.1.X]   |   [![][ubuntu-16.04-badge-preview]][ubuntu-16.04-version-preview]<br>[Host][ubuntu-16.04-host-preview]<br>[Host FX Resolver][ubuntu-16.04-hostfxr-preview]<br>[Shared Framework][ubuntu-16.04-sharedfx-preview]<br>[tar.gz][ubuntu-16.04-targz-preview]   |
|   **Ubuntu 16.10 (x64)**   |  N/A|   [![][ubuntu-16.10-badge-2.0.X]][ubuntu-16.10-version-2.0.X]<br>[Host][ubuntu-16.10-host-2.0.X] ([Checksum][ubuntu-16.10-host-checksum-2.0.X])<br>[Host FX Resolver][ubuntu-16.10-hostfxr-2.0.X] ([Checksum][ubuntu-16.10-hostfxr-checksum-2.0.X])<br>[Shared Framework][ubuntu-16.10-sharedfx-2.0.X] ([Checksum][ubuntu-16.10-sharedfx-checksum-2.0.X])<br>  |   [![][ubuntu-16.10-badge-1.1.X]][ubuntu-16.10-version-1.1.X]<br>[Host][ubuntu-16.10-host-1.1.X]<br>[Host FX Resolver][ubuntu-16.10-hostfxr-1.1.X]<br>[Shared Framework][ubuntu-16.10-sharedfx-1.1.X]<br>[tar.gz][ubuntu-16.10-targz-1.1.X]   |   N/A   |
|   **Ubuntu 17.04 (x64)**   |   [![][ubuntu-17.04-badge-master]][ubuntu-17.04-version-master]<br>[Runtime-Deps][ubuntu-17.04-runtime-deps-master] ([Checksum][ubuntu-17.04-runtime-deps-checksum-master])<br>[Host][ubuntu-17.04-host-master] ([Checksum][ubuntu-17.04-host-checksum-master])<br>[Host FX Resolver][ubuntu-17.04-hostfxr-master] ([Checksum][ubuntu-17.04-hostfxr-checksum-master])<br>[Shared Framework][ubuntu-17.04-sharedfx-master] ([Checksum][ubuntu-17.04-sharedfx-checksum-master])<br>  |N/A |   N/A  |   N/A   |
|   **Ubuntu 17.10 (x64)**   |   [![][ubuntu-17.10-badge-master]][ubuntu-17.10-version-master]<br>[Runtime-Deps][ubuntu-17.10-runtime-deps-master] ([Checksum][ubuntu-17.10-runtime-deps-checksum-master])<br>[Host][ubuntu-17.10-host-master] ([Checksum][ubuntu-17.10-host-checksum-master])<br>[Host FX Resolver][ubuntu-17.10-hostfxr-master] ([Checksum][ubuntu-17.10-hostfxr-checksum-master])<br>[Shared Framework][ubuntu-17.10-sharedfx-master] ([Checksum][ubuntu-17.10-sharedfx-checksum-master])<br>  |N/A |   N/A  |   N/A   |
|   **Debian 8.2 (x64)**     |   [![][debian-8.2-badge-master]][debian-8.2-version-master]<br>[Runtime-Deps][debian-8.2-runtime-deps-master] ([Checksum][debian-8.2-runtime-deps-checksum-master])<br>[Host][debian-8.2-host-master] ([Checksum][debian-8.2-host-checksum-master])<br>[Host FX Resolver][debian-8.2-hostfxr-master] ([Checksum][debian-8.2-hostfxr-checksum-master])<br>[Shared Framework][debian-8.2-sharedfx-master] ([Checksum][debian-8.2-sharedfx-checksum-master])<br>  |   [![][debian-8.2-badge-2.0.X]][debian-8.2-version-2.0.X]<br>[Host][debian-8.2-host-2.0.X] ([Checksum][debian-8.2-host-checksum-2.0.X])<br>[Host FX Resolver][debian-8.2-hostfxr-2.0.X] ([Checksum][debian-8.2-hostfxr-checksum-2.0.X])<br>[Shared Framework][debian-8.2-sharedfx-2.0.X] ([Checksum][debian-8.2-sharedfx-checksum-2.0.X])<br>  |   [![][debian-8.2-badge-1.1.X]][debian-8.2-version-1.1.X]<br>[Host][debian-8.2-host-1.1.X]<br>[Host FX Resolver][debian-8.2-hostfxr-1.1.X]<br>[Shared Framework][debian-8.2-sharedfx-1.1.X]<br>[tar.gz][debian-8.2-targz-1.1.X]   |   [![][debian-8.2-badge-preview]][debian-8.2-version-preview]<br>[Host][debian-8.2-host-preview]<br>[Host FX Resolver][debian-8.2-hostfxr-preview]<br>[Shared Framework][debian-8.2-sharedfx-preview]<br>[tar.gz][debian-8.2-targz-preview]   |
|   **Debian 9 (x64)**     |   [![][debian-9-badge-master]][debian-9-version-master]<br>[Runtime-Deps][debian-9-runtime-deps-master] ([Checksum][debian-9-runtime-deps-checksum-master])<br>[Host][debian-9-host-master] ([Checksum][debian-9-host-checksum-master])<br>[Host FX Resolver][debian-9-hostfxr-master] ([Checksum][debian-9-hostfxr-checksum-master])<br>[Shared Framework][debian-9-sharedfx-master] ([Checksum][debian-9-sharedfx-checksum-master])<br>  | N/A | N/A | N/A |
|   **CentOS 7.1 (x64)**     |   N/A   |   N/A   |   [![][centos-badge-1.1.X]][centos-version-1.1.X]<br>[tar.gz][centos-targz-1.1.X]   |   [![][centos-badge-preview]][centos-version-preview]<br>[tar.gz][centos-targz-preview]   |
|   **RHEL 7.2 (x64)**       |   [![][rhel7-badge-master]][rhel7-version-master]<br>[Host][rhel7-host-master] ([Checksum][rhel7-host-checksum-master])<br>[Host FX Resolver][rhel7-hostfxr-master] ([Checksum][rhel7-hostfxr-checksum-master])<br>[Shared Framework][rhel7-sharedfx-master] ([Checksum][rhel7-sharedfx-checksum-master])<br>  |   [![][rhel7-badge-2.0.X]][rhel7-version-2.0.X]<br>[Host][rhel7-host-2.0.X] ([Checksum][rhel7-host-checksum-2.0.X])<br>[Host FX Resolver][rhel7-hostfxr-2.0.X] ([Checksum][rhel7-hostfxr-checksum-2.0.X])<br>[Shared Framework][rhel7-sharedfx-2.0.X] ([Checksum][rhel7-sharedfx-checksum-2.0.X])<br>   |   [![][rhel-badge-1.1.X]][rhel-version-1.1.X]<br>[tar.gz][rhel-targz-1.1.X]   |    [![][rhel-badge-preview]][rhel-version-preview]<br>[tar.gz][rhel-targz-preview]   |
|   **Fedora 23 (x64)**      |   N/A   |   N/A   |   [![][fedora-23-badge-1.1.X]][fedora-23-version-1.1.X]<br>[tar.gz][fedora-23-targz-1.1.X]   |   [![][fedora-23-badge-preview]][fedora-23-version-preview]<br>[tar.gz][fedora-23-targz-preview]   |
|   **Fedora 24 (x64)**      |   N/A   |   N/A   |   [![][fedora-24-badge-1.1.X]][fedora-24-version-1.1.X]<br>[tar.gz][fedora-24-targz-1.1.X]   |   N/A   |
|   **OpenSUSE 42.1 (x64)**  |   N/A   |   N/A   |   [![][opensuse-42.1-badge-1.1.X]][opensuse-42.1-version-1.1.X]<br>[tar.gz][opensuse-42.1-targz-1.1.X]   |   N/A   |

*Note: Our .deb packages are put together slightly differently than the other OS specific installers. Instead of combining everything, we have separate component packages that depend on each other. If you're installing these directly from the .deb files (via dpkg or similar), then you'll need to install them in the order presented above.*

[build-badge-master]: https://devdiv.visualstudio.com/_apis/public/build/definitions/0bdbc590-a062-4c3f-b0f6-9383f67865ee/3160/badge
[build-master]: https://devdiv.visualstudio.com/DevDiv/_build/index?definitionId=3160&_a=completed

[build-badge-2.0.X]: https://devdiv.visualstudio.com/_apis/public/build/definitions/0bdbc590-a062-4c3f-b0f6-9383f67865ee/6161/badge
[build-2.0.X]: https://devdiv.visualstudio.com/DevDiv/_build/index?definitionId=6161&_a=completed

[build-badge-1.X.X]: https://devdiv.visualstudio.com/_apis/public/build/definitions/0bdbc590-a062-4c3f-b0f6-9383f67865ee/4188/badge
[build-1.X.X]: https://devdiv.visualstudio.com/DevDiv/_build/index?definitionId=4188&_a=completed

[build-badge-1.0.X]: https://devdiv.visualstudio.com/_apis/public/build/definitions/0bdbc590-a062-4c3f-b0f6-9383f67865ee/4187/badge
[build-1.0.X]: https://devdiv.visualstudio.com/DevDiv/_build/index?definitionId=4187&_a=completed


[win-x64-badge-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/sharedfx_win-x64_Release_version_badge.svg
[win-x64-version-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/latest.sharedfx.win.x64.version
[win-x64-installer-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/dotnet-runtime-latest-win-x64.exe
[win-x64-installer-checksum-master]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/master/dotnet-runtime-latest-win-x64.exe.sha512
[win-x64-zip-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/dotnet-runtime-latest-win-x64.zip
[win-x64-zip-checksum-master]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/master/dotnet-runtime-latest-win-x64.zip.sha512
[win-x64-symbols-zip-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/dotnet-runtime-symbols-latest-win-x64.zip

[win-x64-badge-2.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.0.0/sharedfx_win-x64_Release_version_badge.svg
[win-x64-version-2.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.0.0/latest.sharedfx.win.x64.version
[win-x64-installer-2.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.0.0/dotnet-runtime-latest-win-x64.exe
[win-x64-installer-checksum-2.0.X]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/release/2.0.0/dotnet-runtime-latest-win-x64.exe.sha512
[win-x64-zip-2.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.0.0/dotnet-runtime-latest-win-x64.zip
[win-x64-zip-checksum-2.0.X]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/release/2.0.0/dotnet-runtime-latest-win-x64.zip.sha512
[win-x64-symbols-zip-2.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.0.0/dotnet-runtime-symbols-latest-win-x64.zip

[win-x64-badge-1.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/release/1.1.0/Binaries/Latest/sharedfx_Windows_x64_Release_version_badge.svg
[win-x64-version-1.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/release/1.1.0/dnvm/latest.sharedfx.win.x64.version
[win-x64-installer-1.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/release/1.1.0/Installers/Latest/dotnet-win-x64.latest.exe
[win-x64-zip-1.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/release/1.1.0/Binaries/Latest/dotnet-win-x64.latest.zip

[win-x64-badge-preview]: https://dotnetcli.blob.core.windows.net/dotnet/preview/Binaries/Latest/sharedfx_Windows_x64_Release_version_badge.svg
[win-x64-version-preview]: https://dotnetcli.blob.core.windows.net/dotnet/preview/dnvm/latest.sharedfx.win.x64.version
[win-x64-installer-preview]: https://dotnetcli.blob.core.windows.net/dotnet/preview/Installers/Latest/dotnet-win-x64.latest.exe
[win-x64-zip-preview]: https://dotnetcli.blob.core.windows.net/dotnet/preview/Binaries/Latest/dotnet-win-x64.latest.zip

[win-x86-badge-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/sharedfx_win-x86_Release_version_badge.svg
[win-x86-version-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/latest.sharedfx.win.x86.version
[win-x86-installer-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/dotnet-runtime-latest-win-x86.exe
[win-x86-installer-checksum-master]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/master/dotnet-runtime-latest-win-x86.exe.sha512
[win-x86-zip-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/dotnet-runtime-latest-win-x86.zip
[win-x86-zip-checksum-master]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/master/dotnet-runtime-latest-win-x86.zip.sha512
[win-x86-symbols-zip-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/dotnet-runtime-symbols-latest-win-x86.zip

[win-x86-badge-2.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.0.0/sharedfx_win-x86_Release_version_badge.svg
[win-x86-version-2.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.0.0/latest.sharedfx.win.x86.version
[win-x86-installer-2.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.0.0/dotnet-runtime-latest-win-x86.exe
[win-x86-installer-checksum-2.0.X]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/release/2.0.0/dotnet-runtime-latest-win-x86.exe.sha512
[win-x86-zip-2.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.0.0/dotnet-runtime-latest-win-x86.zip
[win-x86-zip-checksum-2.0.X]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/release/2.0.0/dotnet-runtime-latest-win-x86.zip.sha512
[win-x86-symbols-zip-2.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.0.0/dotnet-runtime-symbols-latest-win-x86.zip

[win-x86-badge-1.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/release/1.1.0/Binaries/Latest/sharedfx_Windows_x86_Release_version_badge.svg
[win-x86-version-1.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/release/1.1.0/dnvm/latest.sharedfx.win.x86.version
[win-x86-installer-1.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/release/1.1.0/Installers/Latest/dotnet-win-x86.latest.exe
[win-x86-zip-1.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/release/1.1.0/Binaries/Latest/dotnet-win-x86.latest.zip

[win-x86-badge-preview]: https://dotnetcli.blob.core.windows.net/dotnet/preview/Binaries/Latest/sharedfx_Windows_x86_Release_version_badge.svg
[win-x86-version-preview]: https://dotnetcli.blob.core.windows.net/dotnet/preview/dnvm/latest.sharedfx.win.x86.version
[win-x86-installer-preview]: https://dotnetcli.blob.core.windows.net/dotnet/preview/Installers/Latest/dotnet-win-x86.latest.exe
[win-x86-zip-preview]: https://dotnetcli.blob.core.windows.net/dotnet/preview/Binaries/Latest/dotnet-win-x86.latest.zip

[win-arm-badge-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/sharedfx_win-arm_Release_version_badge.svg
[win-arm-version-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/latest.sharedfx.win.arm.version
[win-arm-zip-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/dotnet-runtime-latest-win-arm.zip
[win-arm-zip-checksum-master]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/master/dotnet-runtime-latest-win-arm.zip.sha512
[win-arm-symbols-zip-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/dotnet-runtime-symbols-latest-win-arm.zip

[win-arm-badge-2.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.0.0/sharedfx_win-arm_Release_version_badge.svg
[win-arm-version-2.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.0.0/latest.sharedfx.win.arm.version
[win-arm-zip-2.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.0.0/dotnet-runtime-latest-win-arm.zip
[win-arm-zip-checksum-2.0.X]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/release/2.0.0/dotnet-runtime-latest-win-arm.zip.sha512
[win-arm-symbols-zip-2.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.0.0/dotnet-runtime-symbols-latest-win-arm.zip

[win-arm64-badge-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/sharedfx_win-arm64_Release_version_badge.svg
[win-arm64-version-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/latest.sharedfx.win.arm64.version
[win-arm64-zip-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/dotnet-runtime-latest-win-arm64.zip
[win-arm64-zip-checksum-master]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/master/dotnet-runtime-latest-win-arm64.zip.sha512
[win-arm64-symbols-zip-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/dotnet-runtime-symbols-latest-win-arm64.zip

[win-arm64-badge-2.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.0.0/sharedfx_win-arm64_Release_version_badge.svg
[win-arm64-version-2.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.0.0/latest.sharedfx.win.arm64.version
[win-arm64-zip-2.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.0.0/dotnet-runtime-latest-win-arm64.zip
[win-arm64-zip-checksum-2.0.X]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/release/2.0.0/dotnet-runtime-latest-win-arm64.zip.sha512
[win-arm64-symbols-zip-2.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.0.0/dotnet-runtime-symbols-latest-win-arm64.zip

[osx-badge-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/sharedfx_osx-x64_Release_version_badge.svg
[osx-version-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/latest.sharedfx.osx.x64.version
[osx-installer-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/dotnet-runtime-latest-osx-x64.pkg
[osx-installer-checksum-master]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/master/dotnet-runtime-latest-osx-x64.pkg.sha512
[osx-targz-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/dotnet-runtime-latest-osx-x64.tar.gz
[osx-targz-checksum-master]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/master/dotnet-runtime-latest-osx-x64.tar.gz.sha512
[osx-symbols-targz-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/dotnet-runtime-symbols-latest-osx-x64.tar.gz

[osx-badge-2.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.0.0/sharedfx_osx-x64_Release_version_badge.svg
[osx-version-2.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.0.0/latest.sharedfx.osx.x64.version
[osx-installer-2.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.0.0/dotnet-runtime-latest-osx-x64.pkg
[osx-installer-checksum-2.0.X]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/release/2.0.0/dotnet-runtime-latest-osx-x64.pkg.sha512
[osx-targz-2.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.0.0/dotnet-runtime-latest-osx-x64.tar.gz
[osx-targz-checksum-2.0.X]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/release/2.0.0/dotnet-runtime-latest-osx-x64.tar.gz.sha512
[osx-symbols-targz-2.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.0.0/dotnet-runtime-symbols-latest-osx-x64.tar.gz

[osx-badge-1.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/release/1.1.0/Binaries/Latest/sharedfx_OSX_x64_Release_version_badge.svg
[osx-version-1.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/release/1.1.0/dnvm/latest.sharedfx.osx.x64.version
[osx-installer-1.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/release/1.1.0/Installers/Latest/dotnet-osx-x64.latest.pkg
[osx-targz-1.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/release/1.1.0/Binaries/Latest/dotnet-osx-x64.latest.tar.gz

[osx-badge-preview]: https://dotnetcli.blob.core.windows.net/dotnet/preview/Binaries/Latest/sharedfx_OSX_x64_Release_version_badge.svg
[osx-version-preview]: https://dotnetcli.blob.core.windows.net/dotnet/preview/dnvm/latest.sharedfx.osx.x64.version
[osx-installer-preview]: https://dotnetcli.blob.core.windows.net/dotnet/preview/Installers/Latest/dotnet-osx-x64.latest.pkg
[osx-targz-preview]: https://dotnetcli.blob.core.windows.net/dotnet/preview/Binaries/Latest/dotnet-osx-x64.latest.tar.gz


[linux-x64-badge-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/sharedfx_linux-x64_Release_version_badge.svg
[linux-x64-version-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/latest.sharedfx.linux.x64.version
[linux-x64-targz-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/dotnet-runtime-latest-linux-x64.tar.gz
[linux-x64-targz-checksum-master]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/master/dotnet-runtime-latest-linux-x64tar.gz.sha512
[linux-x64-symbols-targz-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/dotnet-runtime-symbols-latest-linux-x64.tar.gz

[linux-x64-badge-2.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.0.0/sharedfx_linux-x64_Release_version_badge.svg
[linux-x64-version-2.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.0.0/latest.sharedfx.linux.x64.version
[linux-x64-targz-2.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.0.0/dotnet-runtime-latest-linux-x64.tar.gz
[linux-x64-targz-checksum-2.0.X]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/release/2.0.0/dotnet-runtime-latest-linux-x64.tar.gz.sha512
[linux-x64-symbols-targz-2.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.0.0/dotnet-runtime-symbols-latest-linux-x64.tar.gz

[linux-arm-badge-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/sharedfx_linux-arm_Release_version_badge.svg
[linux-arm-version-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/latest.sharedfx.linux.arm.version
[linux-arm-targz-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/dotnet-runtime-latest-linux-arm.tar.gz
[linux-arm-targz-checksum-master]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/master/dotnet-runtime-latest-linux-arm.tar.gz.sha512
[linux-arm-symbols-targz-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/dotnet-runtime-symbols-latest-linux-arm.tar.gz

[linux-arm-badge-2.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.0.0/sharedfx_linux-arm_Release_version_badge.svg
[linux-arm-version-2.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.0.0/latest.sharedfx.linux.arm.version
[linux-arm-targz-2.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.0.0/dotnet-runtime-latest-linux-arm.tar.gz
[linux-arm-targz-checksum-2.0.X]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/release/2.0.0/dotnet-runtime-latest-linux-arm.tar.gz.sha512
[linux-arm-symbols-targz-2.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.0.0/dotnet-runtime-symbols-latest-linux-arm.tar.gz

[ubuntu-14.04-badge-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/sharedfx_ubuntu.14.04-x64_Release_version_badge.svg
[ubuntu-14.04-version-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/latest.sharedfx.ubuntu.x64.version
[ubuntu-14.04-runtime-deps-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/dotnet-runtime-deps-latest-ubuntu.14.04-x64.deb
[ubuntu-14.04-runtime-deps-checksum-master]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/master/dotnet-runtime-deps-latest-ubuntu.14.04-x64.deb.sha512
[ubuntu-14.04-host-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/dotnet-host-latest-ubuntu.14.04-x64.deb
[ubuntu-14.04-host-checksum-master]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/master/dotnet-host-latest-ubuntu.14.04-x64.deb.sha512
[ubuntu-14.04-hostfxr-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/dotnet-hostfxr-latest-ubuntu.14.04-x64.deb
[ubuntu-14.04-hostfxr-checksum-master]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/master/dotnet-hostfxr-latest-ubuntu.14.04-x64.deb.sha512
[ubuntu-14.04-sharedfx-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/dotnet-runtime-latest-ubuntu.14.04-x64.deb
[ubuntu-14.04-sharedfx-checksum-master]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/master/dotnet-runtime-latest-ubuntu.14.04-x64.deb.sha512

[ubuntu-14.04-badge-2.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.0.0/sharedfx_ubuntu.14.04-x64_Release_version_badge.svg
[ubuntu-14.04-version-2.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.0.0/latest.sharedfx.ubuntu.x64.version
[ubuntu-14.04-host-2.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.0.0/dotnet-host-latest-ubuntu.14.04-x64.deb
[ubuntu-14.04-host-checksum-2.0.X]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/release/2.0.0/dotnet-host-latest-ubuntu.14.04-x64.deb.sha512
[ubuntu-14.04-hostfxr-2.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.0.0/dotnet-hostfxr-latest-ubuntu.14.04-x64.deb
[ubuntu-14.04-hostfxr-checksum-2.0.X]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/release/2.0.0/dotnet-hostfxr-latest-ubuntu.14.04-x64.deb.sha512
[ubuntu-14.04-sharedfx-2.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.0.0/dotnet-runtime-latest-ubuntu.14.04-x64.deb
[ubuntu-14.04-sharedfx-checksum-2.0.X]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/release/2.0.0/dotnet-runtime-latest-ubuntu.14.04-x64.deb.sha512

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


[ubuntu-16.04-badge-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/sharedfx_ubuntu.16.04-x64_Release_version_badge.svg
[ubuntu-16.04-version-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/latest.sharedfx.ubuntu.16.04.x64.version
[ubuntu-16.04-host-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/dotnet-host-latest-ubuntu.16.04-x64.deb
[ubuntu-16.04-runtime-deps-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/dotnet-runtime-deps-latest-ubuntu.16.04-x64.deb
[ubuntu-16.04-runtime-deps-checksum-master]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/master/dotnet-runtime-deps-latest-ubuntu.16.04-x64.deb.sha512
[ubuntu-16.04-host-checksum-master]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/master/dotnet-host-latest-ubuntu.16.04-x64.deb.sha512
[ubuntu-16.04-hostfxr-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/dotnet-hostfxr-latest-ubuntu.16.04-x64.deb
[ubuntu-16.04-hostfxr-checksum-master]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/master/dotnet-hostfxr-latest-ubuntu.16.04-x64.deb.sha512
[ubuntu-16.04-sharedfx-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/dotnet-runtime-latest-ubuntu.16.04-x64.deb
[ubuntu-16.04-sharedfx-checksum-master]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/master/dotnet-runtime-latest-ubuntu.16.04-x64.deb.sha512

[ubuntu-16.04-badge-2.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.0.0/sharedfx_ubuntu.16.04-x64_Release_version_badge.svg
[ubuntu-16.04-version-2.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.0.0/latest.sharedfx.ubuntu.16.04.x64.version
[ubuntu-16.04-host-2.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.0.0/dotnet-host-latest-ubuntu.16.04-x64.deb
[ubuntu-16.04-host-checksum-2.0.X]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/release/2.0.0/dotnet-host-latest-ubuntu.16.04-x64.deb.sha512
[ubuntu-16.04-hostfxr-2.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.0.0/dotnet-hostfxr-latest-ubuntu.16.04-x64.deb
[ubuntu-16.04-hostfxr-checksum-2.0.X]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/release/2.0.0/dotnet-hostfxr-latest-ubuntu.16.04-x64.deb.sha512
[ubuntu-16.04-sharedfx-2.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.0.0/dotnet-runtime-latest-ubuntu.16.04-x64.deb
[ubuntu-16.04-sharedfx-checksum-2.0.X]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/release/2.0.0/dotnet-runtime-latest-ubuntu.16.04-x64.deb.sha512

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

[ubuntu-17.04-badge-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/sharedfx_ubuntu.17.04-x64_Release_version_badge.svg
[ubuntu-17.04-version-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/latest.sharedfx.ubuntu.17.04.x64.version
[ubuntu-17.04-runtime-deps-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/dotnet-runtime-deps-latest-ubuntu.17.04-x64.deb
[ubuntu-17.04-runtime-deps-checksum-master]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/master/dotnet-runtime-deps-latest-ubuntu.17.04-x64.deb.sha512
[ubuntu-17.04-host-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/dotnet-host-latest-ubuntu.17.04-x64.deb
[ubuntu-17.04-host-checksum-master]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/master/dotnet-host-latest-ubuntu.17.04-x64.deb.sha512
[ubuntu-17.04-hostfxr-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/dotnet-hostfxr-latest-ubuntu.17.04-x64.deb
[ubuntu-17.04-hostfxr-checksum-master]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/master/dotnet-hostfxr-latest-ubuntu.17.04-x64.deb.sha512
[ubuntu-17.04-sharedfx-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/dotnet-runtime-latest-ubuntu.17.04-x64.deb
[ubuntu-17.04-sharedfx-checksum-master]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/master/dotnet-runtime-latest-ubuntu.17.04-x64.deb.sha512

[ubuntu-17.10-badge-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/sharedfx_ubuntu.17.10-x64_Release_version_badge.svg
[ubuntu-17.10-version-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/latest.sharedfx.ubuntu.17.10.x64.version
[ubuntu-17.10-runtime-deps-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/dotnet-runtime-deps-latest-ubuntu.17.10-x64.deb
[ubuntu-17.10-runtime-deps-checksum-master]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/master/dotnet-runtime-deps-latest-ubuntu.17.10-x64.deb.sha512
[ubuntu-17.10-host-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/dotnet-host-latest-ubuntu.17.10-x64.deb
[ubuntu-17.10-host-checksum-master]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/master/dotnet-host-latest-ubuntu.17.10-x64.deb.sha512
[ubuntu-17.10-hostfxr-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/dotnet-hostfxr-latest-ubuntu.17.10-x64.deb
[ubuntu-17.10-hostfxr-checksum-master]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/master/dotnet-hostfxr-latest-ubuntu.17.10-x64.deb.sha512
[ubuntu-17.10-sharedfx-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/dotnet-runtime-latest-ubuntu.17.10-x64.deb
[ubuntu-17.10-sharedfx-checksum-master]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/master/dotnet-runtime-latest-ubuntu.17.10-x64.deb.sha512

[ubuntu-16.10-badge-2.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.0.0/sharedfx_ubuntu.16.10-x64_Release_version_badge.svg
[ubuntu-16.10-version-2.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.0.0/latest.sharedfx.ubuntu.16.10.x64.version
[ubuntu-16.10-host-2.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.0.0/dotnet-host-latest-ubuntu.16.10-x64.deb
[ubuntu-16.10-host-checksum-2.0.X]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/release/2.0.0/dotnet-host-latest-ubuntu.16.10-x64.deb.sha512
[ubuntu-16.10-hostfxr-2.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.0.0/dotnet-hostfxr-latest-ubuntu.16.10-x64.deb
[ubuntu-16.10-hostfxr-checksum-2.0.X]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/release/2.0.0/dotnet-hostfxr-latest-ubuntu.16.10-x64.deb.sha512
[ubuntu-16.10-sharedfx-2.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.0.0/dotnet-runtime-latest-ubuntu.16.10-x64.deb
[ubuntu-16.10-sharedfx-checksum-2.0.X]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/release/2.0.0/dotnet-runtime-latest-ubuntu.16.10-x64.deb.sha512

[ubuntu-16.10-badge-1.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/release/1.1.0/Binaries/Latest/sharedfx_Ubuntu_16_10_x64_Release_version_badge.svg
[ubuntu-16.10-version-1.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/release/1.1.0/dnvm/latest.sharedfx.ubuntu.16.10.x64.version
[ubuntu-16.10-host-1.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/release/1.1.0/Installers/Latest/dotnet-host-ubuntu.16.10-x64.latest.deb
[ubuntu-16.10-hostfxr-1.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/release/1.1.0/Installers/Latest/dotnet-hostfxr-ubuntu.16.10-x64.latest.deb
[ubuntu-16.10-sharedfx-1.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/release/1.1.0/Installers/Latest/dotnet-sharedframework-ubuntu.16.10-x64.latest.deb
[ubuntu-16.10-targz-1.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/release/1.1.0/Binaries/Latest/dotnet-ubuntu.16.10-x64.latest.tar.gz

[debian-8.2-badge-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/sharedfx_debian.8-x64_Release_version_badge.svg
[debian-8.2-version-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/latest.sharedfx.debian.8.x64.version
[debian-8.2-runtime-deps-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/dotnet-runtime-deps-latest-debian.8-x64.deb
[debian-8.2-runtime-deps-checksum-master]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/master/dotnet-runtime-deps-latest-debian.8-x64.deb.sha512
[debian-8.2-host-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/dotnet-host-latest-debian.8-x64.deb
[debian-8.2-host-checksum-master]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/master/dotnet-host-latest-debian.8-x64.deb.sha512
[debian-8.2-hostfxr-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/dotnet-hostfxr-latest-debian.8-x64.deb
[debian-8.2-hostfxr-checksum-master]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/master/dotnet-hostfxr-latest-debian.8-x64.deb.sha512
[debian-8.2-sharedfx-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/dotnet-runtime-latest-debian.8-x64.deb
[debian-8.2-sharedfx-checksum-master]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/master/dotnet-runtime-latest-debian.8-x64.deb.sha512

[debian-9-badge-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/sharedfx_debian.9-x64_Release_version_badge.svg
[debian-9-version-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/latest.sharedfx.debian.9.x64.version
[debian-9-runtime-deps-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/dotnet-runtime-deps-latest-debian.9-x64.deb
[debian-9-runtime-deps-checksum-master]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/master/dotnet-runtime-deps-latest-debian.9-x64.deb.sha512
[debian-9-host-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/dotnet-host-latest-debian.9-x64.deb
[debian-9-host-checksum-master]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/master/dotnet-host-latest-debian.9-x64.deb.sha512
[debian-9-hostfxr-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/dotnet-hostfxr-latest-debian.9-x64.deb
[debian-9-hostfxr-checksum-master]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/master/dotnet-hostfxr-latest-debian.9-x64.deb.sha512
[debian-9-sharedfx-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/dotnet-runtime-latest-debian.9-x64.deb
[debian-9-sharedfx-checksum-master]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/master/dotnet-runtime-latest-debian.9-x64.deb.sha512

[debian-8.2-badge-2.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.0.0/sharedfx_debian.8-x64_Release_version_badge.svg
[debian-8.2-version-2.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.0.0/latest.sharedfx.debian.8.x64.version
[debian-8.2-host-2.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.0.0/dotnet-host-latest-debian.8-x64.deb
[debian-8.2-host-checksum-2.0.X]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/release/2.0.0/dotnet-host-latest-debian.8-x64.deb.sha512
[debian-8.2-hostfxr-2.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.0.0/dotnet-hostfxr-latest-debian.8-x64.deb
[debian-8.2-hostfxr-checksum-2.0.X]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/release/2.0.0/dotnet-hostfxr-latest-debian.8-x64.deb.sha512
[debian-8.2-sharedfx-2.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.0.0/dotnet-runtime-latest-debian.8-x64.deb
[debian-8.2-sharedfx-checksum-2.0.X]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/release/2.0.0/dotnet-runtime-latest-debian.8-x64.deb.sha512

[debian-8.2-badge-1.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/release/1.1.0/Binaries/Latest/sharedfx_Debian_x64_Release_version_badge.svg
[debian-8.2-version-1.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/release/1.1.0/dnvm/latest.sharedfx.debian.x64.version
[debian-8.2-host-1.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/release/1.1.0/Installers/Latest/dotnet-host-debian-x64.latest.deb
[debian-8.2-hostfxr-1.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/release/1.1.0/Installers/Latest/dotnet-hostfxr-debian-x64.latest.deb
[debian-8.2-sharedfx-1.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/release/1.1.0/Installers/Latest/dotnet-sharedframework-debian-x64.latest.deb
[debian-8.2-targz-1.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/release/1.1.0/Binaries/Latest/dotnet-debian-x64.latest.tar.gz

[debian-8.2-badge-preview]: https://dotnetcli.blob.core.windows.net/dotnet/preview/Binaries/Latest/sharedfx_Debian_x64_Release_version_badge.svg
[debian-8.2-version-preview]: https://dotnetcli.blob.core.windows.net/dotnet/preview/dnvm/latest.sharedfx.debian.x64.version
[debian-8.2-host-preview]: https://dotnetcli.blob.core.windows.net/dotnet/preview/Installers/Latest/dotnet-host-debian-x64.latest.deb
[debian-8.2-hostfxr-preview]: https://dotnetcli.blob.core.windows.net/dotnet/preview/Installers/Latest/dotnet-hostfxr-debian-x64.latest.deb
[debian-8.2-sharedfx-preview]: https://dotnetcli.blob.core.windows.net/dotnet/preview/Installers/Latest/dotnet-sharedframework-debian-x64.latest.deb
[debian-8.2-targz-preview]: https://dotnetcli.blob.core.windows.net/dotnet/preview/Binaries/Latest/dotnet-debian-x64.latest.tar.gz

[centos-badge-1.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/release/1.1.0/Binaries/Latest/sharedfx_CentOS_x64_Release_version_badge.svg
[centos-version-1.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/release/1.1.0/dnvm/latest.sharedfx.centos.x64.version
[centos-targz-1.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/release/1.1.0/Binaries/Latest/dotnet-centos-x64.latest.tar.gz

[centos-badge-preview]: https://dotnetcli.blob.core.windows.net/dotnet/preview/Binaries/Latest/sharedfx_CentOS_x64_Release_version_badge.svg
[centos-version-preview]: https://dotnetcli.blob.core.windows.net/dotnet/preview/dnvm/latest.sharedfx.centos.x64.version
[centos-targz-preview]: https://dotnetcli.blob.core.windows.net/dotnet/preview/Binaries/Latest/dotnet-centos-x64.latest.tar.gz

[rhel7-badge-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/sharedfx_rhel.7-x64_Release_version_badge.svg
[rhel7-version-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/latest.sharedfx.rhel.7.x64.version
[rhel7-host-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/dotnet-host-latest-rhel.7-x64.rpm
[rhel7-host-checksum-master]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/master/dotnet-host-latest-rhel.7-x64.rpm.sha512
[rhel7-hostfxr-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/dotnet-hostfxr-latest-rhel.7-x64.rpm
[rhel7-hostfxr-checksum-master]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/master/dotnet-hostfxr-latest-rhel.7-x64.rpm.sha512
[rhel7-sharedfx-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/dotnet-runtime-latest-rhel.7-x64.rpm
[rhel7-sharedfx-checksum-master]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/master/dotnet-runtime-latest-rhel.7-x64.rpm.sha512

[rhel7-badge-2.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.0.0/sharedfx_rhel.7-x64_Release_version_badge.svg
[rhel7-version-2.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.0.0/latest.sharedfx.rhel.7.x64.version
[rhel7-host-2.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.0.0/dotnet-host-latest-rhel.7-x64.rpm
[rhel7-host-checksum-2.0.X]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/release/2.0.0/dotnet-host-latest-rhel.7-x64.rpm.sha512
[rhel7-hostfxr-2.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.0.0/dotnet-hostfxr-latest-rhel.7-x64.rpm
[rhel7-hostfxr-checksum-2.0.X]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/release/2.0.0/dotnet-hostfxr-latest-rhel.7-x64.rpm.sha512
[rhel7-sharedfx-2.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.0.0/dotnet-runtime-latest-rhel.7-x64.rpm
[rhel7-sharedfx-checksum-2.0.X]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/release/2.0.0/dotnet-runtime-latest-rhel.7-x64.rpm.sha512

[rhel-badge-1.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/release/1.1.0/Binaries/Latest/sharedfx_RHEL_x64_Release_version_badge.svg
[rhel-version-1.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/release/1.1.0/dnvm/latest.sharedfx.rhel.x64.version
[rhel-targz-1.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/release/1.1.0/Binaries/Latest/dotnet-rhel-x64.latest.tar.gz

[rhel-badge-preview]: https://dotnetcli.blob.core.windows.net/dotnet/preview/Binaries/Latest/sharedfx_RHEL_x64_Release_version_badge.svg
[rhel-version-preview]: https://dotnetcli.blob.core.windows.net/dotnet/preview/dnvm/latest.sharedfx.rhel.x64.version
[rhel-targz-preview]: https://dotnetcli.blob.core.windows.net/dotnet/preview/Binaries/Latest/dotnet-rhel-x64.latest.tar.gz


[fedora-23-badge-1.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/release/1.1.0/Binaries/Latest/sharedfx_Fedora_23_x64_Release_version_badge.svg
[fedora-23-version-1.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/release/1.1.0/dnvm/latest.sharedfx.fedora.23.x64.version
[fedora-23-targz-1.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/release/1.1.0/Binaries/Latest/dotnet-fedora.23-x64.latest.tar.gz

[fedora-23-badge-preview]: https://dotnetcli.blob.core.windows.net/dotnet/preview/Binaries/Latest/sharedfx_Fedora_23_x64_Release_version_badge.svg
[fedora-23-version-preview]: https://dotnetcli.blob.core.windows.net/dotnet/preview/dnvm/latest.sharedfx.fedora.23.x64.version
[fedora-23-targz-preview]: https://dotnetcli.blob.core.windows.net/dotnet/preview/Binaries/Latest/dotnet-fedora.23-x64.latest.tar.gz


[fedora-24-badge-1.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/release/1.1.0/Binaries/Latest/sharedfx_Fedora_24_x64_Release_version_badge.svg
[fedora-24-version-1.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/release/1.1.0/dnvm/latest.sharedfx.fedora.24.x64.version
[fedora-24-targz-1.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/release/1.1.0/Binaries/Latest/dotnet-fedora.24-x64.latest.tar.gz


[opensuse-42.1-badge-1.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/release/1.1.0/Binaries/Latest/sharedfx_openSUSE_42_1_x64_Release_version_badge.svg
[opensuse-42.1-version-1.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/release/1.1.0/dnvm/latest.sharedfx.opensuse.42.1.x64.version
[opensuse-42.1-targz-1.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/release/1.1.0/Binaries/Latest/dotnet-opensuse.42.1-x64.latest.tar.gz

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
