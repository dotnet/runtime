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

Build Status
------------

|Ubuntu 14.04 |Ubuntu 16.04 |Debian 8.2 |Windows x64 |Windows x86 |Mac OS X |CentOS 7.1 |RHEL 7.2 |Fedora 23 |OpenSUSE 13.2 |
|:------:|:------:|:------:|:------:|:------:|:------:|:------:|:------:|:------:|:------:|
|[![](https://mseng.visualstudio.com/_apis/public/build/definitions/d09b7a4d-0a51-4c0e-a15a-07921d5b558f/3599/badge)](https://mseng.visualstudio.com/dotnetcore/_build?_a=completed&definitionId=3599)|[![](https://mseng.visualstudio.com/_apis/public/build/definitions/d09b7a4d-0a51-4c0e-a15a-07921d5b558f/3600/badge)](https://mseng.visualstudio.com/dotnetcore/_build?_a=completed&definitionId=3600)|[![](https://mseng.visualstudio.com/_apis/public/build/definitions/d09b7a4d-0a51-4c0e-a15a-07921d5b558f/3592/badge)](https://mseng.visualstudio.com/dotnetcore/_build?_a=completed&definitionId=3592)|[![](https://mseng.visualstudio.com/_apis/public/build/definitions/d09b7a4d-0a51-4c0e-a15a-07921d5b558f/3597/badge)](https://mseng.visualstudio.com/dotnetcore/_build?_a=completed&definitionId=3597)|[![](https://mseng.visualstudio.com/_apis/public/build/definitions/d09b7a4d-0a51-4c0e-a15a-07921d5b558f/3598/badge)](https://mseng.visualstudio.com/dotnetcore/_build?_a=completed&definitionId=3598)|[![](https://mseng.visualstudio.com/_apis/public/build/definitions/d09b7a4d-0a51-4c0e-a15a-07921d5b558f/3595/badge)](https://mseng.visualstudio.com/dotnetcore/_build?_a=completed&definitionId=3595)|[![](https://mseng.visualstudio.com/_apis/public/build/definitions/d09b7a4d-0a51-4c0e-a15a-07921d5b558f/3591/badge)](https://mseng.visualstudio.com/dotnetcore/_build?_a=completed&definitionId=3591)|[![](https://mseng.visualstudio.com/_apis/public/build/definitions/d09b7a4d-0a51-4c0e-a15a-07921d5b558f/3596/badge)](https://mseng.visualstudio.com/dotnetcore/_build?_a=completed&definitionId=3596)|[![](https://mseng.visualstudio.com/_apis/public/build/definitions/d09b7a4d-0a51-4c0e-a15a-07921d5b558f/3593/badge)](https://mseng.visualstudio.com/dotnetcore/_build?_a=completed&definitionId=3593)|[![](https://mseng.visualstudio.com/_apis/public/build/definitions/d09b7a4d-0a51-4c0e-a15a-07921d5b558f/3594/badge)](https://mseng.visualstudio.com/dotnetcore/_build?_a=completed&definitionId=3594)|

Officially Released Builds
--------------------------

They can be downloaded from [here](https://www.microsoft.com/net/download#core).

Daily Builds
------------

*Note: Our Debian packages are put together slightly differently than the other OS specific installers. Instead of combining everything, we have separate component packages that depend on each other. If you're installing these directly from the .deb files (via dpkg or similar), then you'll need to install them in the order presented above.*

*Note: For the platforms where platform specific installers are not available, the binaries are available in an archive.*

|Platform |Master| Release/1.0.0 |
|---------|:----------:|:----------:|
|**Windows x64**|[![](https://dotnetcli.blob.core.windows.net/dotnet/master/Binaries/Latest/sharedfx_Windows_x64_Release_version_badge.svg)](https://dotnetcli.blob.core.windows.net/dotnet/master/Installers/Latest/dotnet-win-x64.latest.exe)|[![](https://dotnetcli.blob.core.windows.net/dotnet/preview/Binaries/Latest/sharedfx_Windows_x64_Release_version_badge.svg)](https://dotnetcli.blob.core.windows.net/dotnet/preview/Installers/Latest/dotnet-win-x64.latest.exe)|
|**Windows x86**|[![](https://dotnetcli.blob.core.windows.net/dotnet/master/Binaries/Latest/sharedfx_Windows_x86_Release_version_badge.svg)](https://dotnetcli.blob.core.windows.net/dotnet/master/Installers/Latest/dotnet-win-x86.latest.exe)|[![](https://dotnetcli.blob.core.windows.net/dotnet/preview/Binaries/Latest/sharedfx_Windows_x86_Release_version_badge.svg)](https://dotnetcli.blob.core.windows.net/dotnet/preview/Installers/Latest/dotnet-win-x86.latest.exe)|
|**Windows Arm32**|[![](https://dotnetcli.blob.core.windows.net/dotnet/master/Binaries/Latest/sharedfx_Windows_arm_Release_version_badge.svg)](https://dotnetcli.blob.core.windows.net/dotnet/master/Installers/Latest/dotnet-win-arm.latest.exe)|N/A|
|**Ubuntu 14.04**|![](https://dotnetcli.blob.core.windows.net/dotnet/master/Binaries/Latest/sharedfx_Ubuntu_x64_Release_version_badge.svg)<br>[Host](https://dotnetcli.blob.core.windows.net/dotnet/master/Binaries/Latest/dotnet-host-ubuntu-x64.latest.deb)<br>[Host FX Resolver](https://dotnetcli.blob.core.windows.net/dotnet/master/Binaries/Latest/dotnet-hostfxr-ubuntu-x64.latest.deb)<br>[Shared Framework](https://dotnetcli.blob.core.windows.net/dotnet/master/Binaries/Latest/dotnet-sharedframework-ubuntu-x64.latest.deb)|![](https://dotnetcli.blob.core.windows.net/dotnet/preview/Binaries/Latest/sharedfx_Ubuntu_x64_Release_version_badge.svg)<br>[Host](https://dotnetcli.blob.core.windows.net/dotnet/preview/Binaries/Latest/dotnet-host-ubuntu-x64.latest.deb)<br>[Host FX Resolver](https://dotnetcli.blob.core.windows.net/dotnet/preview/Binaries/Latest/dotnet-hostfxr-ubuntu-x64.latest.deb)<br>[Shared Framework](https://dotnetcli.blob.core.windows.net/dotnet/preview/Binaries/Latest/dotnet-sharedframework-ubuntu-x64.latest.deb)|
|**Ubuntu 16.04**|![](https://dotnetcli.blob.core.windows.net/dotnet/master/Binaries/Latest/sharedfx_Ubuntu_16_04_x64_Release_version_badge.svg)<br>[Host](https://dotnetcli.blob.core.windows.net/dotnet/master/Binaries/Latest/dotnet-host-ubuntu.16.04-x64.latest.deb)<br>[Host FX Resolver](https://dotnetcli.blob.core.windows.net/dotnet/master/Binaries/Latest/dotnet-hostfxr-ubuntu.16.04-x64.latest.deb)<br>[Shared Framework](https://dotnetcli.blob.core.windows.net/dotnet/master/Binaries/Latest/dotnet-sharedframework-ubuntu.16.04-x64.latest.deb)|![](https://dotnetcli.blob.core.windows.net/dotnet/preview/Binaries/Latest/sharedfx_Ubuntu_16_04_x64_Release_version_badge.svg)<br>[Host](https://dotnetcli.blob.core.windows.net/dotnet/preview/Binaries/Latest/dotnet-host-ubuntu.16.04-x64.latest.deb)<br>[Host FX Resolver](https://dotnetcli.blob.core.windows.net/dotnet/preview/Binaries/Latest/dotnet-hostfxr-ubuntu.16.04-x64.latest.deb)<br>[Shared Framework](https://dotnetcli.blob.core.windows.net/dotnet/preview/Binaries/Latest/dotnet-sharedframework-ubuntu.16.04-x64.latest.deb)|
|**Ubuntu 16.10**|![](https://dotnetcli.blob.core.windows.net/dotnet/master/Binaries/Latest/sharedfx_Ubuntu_16_10_x64_Release_version_badge.svg)<br>[Host](https://dotnetcli.blob.core.windows.net/dotnet/master/Binaries/Latest/dotnet-host-ubuntu.16.10-x64.latest.deb)<br>[Host FX Resolver](https://dotnetcli.blob.core.windows.net/dotnet/master/Binaries/Latest/dotnet-hostfxr-ubuntu.16.10-x64.latest.deb)<br>[Shared Framework](https://dotnetcli.blob.core.windows.net/dotnet/master/Binaries/Latest/dotnet-sharedframework-ubuntu.16.10-x64.latest.deb)|N/A|
|**Debian 8.2**|[![](https://dotnetcli.blob.core.windows.net/dotnet/master/Binaries/Latest/sharedfx_Debian_x64_Release_version_badge.svg)](https://dotnetcli.blob.core.windows.net/dotnet/master/Binaries/Latest/dotnet-debian-x64.latest.tar.gz)|[![](https://dotnetcli.blob.core.windows.net/dotnet/preview/Binaries/Latest/sharedfx_Debian_x64_Release_version_badge.svg)](https://dotnetcli.blob.core.windows.net/dotnet/preview/Binaries/Latest/dotnet-debian-x64.latest.tar.gz)|
|**Mac OS X**|[![](https://dotnetcli.blob.core.windows.net/dotnet/master/Binaries/Latest/sharedfx_OSX_x64_Release_version_badge.svg)](https://dotnetcli.blob.core.windows.net/dotnet/master/Installers/Latest/dotnet-osx-x64.latest.pkg)|[![](https://dotnetcli.blob.core.windows.net/dotnet/preview/Binaries/Latest/sharedfx_OSX_x64_Release_version_badge.svg)](https://dotnetcli.blob.core.windows.net/dotnet/preview/Installers/Latest/dotnet-osx-x64.latest.pkg)|
|**CentOS 7.1**|[![](https://dotnetcli.blob.core.windows.net/dotnet/master/Binaries/Latest/sharedfx_CentOS_x64_Release_version_badge.svg)](https://dotnetcli.blob.core.windows.net/dotnet/master/Binaries/Latest/dotnet-centos-x64.latest.tar.gz)|[![](https://dotnetcli.blob.core.windows.net/dotnet/preview/Binaries/Latest/sharedfx_CentOS_x64_Release_version_badge.svg)](https://dotnetcli.blob.core.windows.net/dotnet/preview/Binaries/Latest/dotnet-centos-x64.latest.tar.gz)|
|**RHEL 7.2**|[![](https://dotnetcli.blob.core.windows.net/dotnet/master/Binaries/Latest/sharedfx_RHEL_x64_Release_version_badge.svg)](https://dotnetcli.blob.core.windows.net/dotnet/master/Binaries/Latest/dotnet-rhel-x64.latest.tar.gz)|[![](https://dotnetcli.blob.core.windows.net/dotnet/preview/Binaries/Latest/sharedfx_RHEL_x64_Release_version_badge.svg)](https://dotnetcli.blob.core.windows.net/dotnet/preview/Binaries/Latest/dotnet-rhel-x64.latest.tar.gz)|
|**Fedora 23**|[![](https://dotnetcli.blob.core.windows.net/dotnet/master/Binaries/Latest/sharedfx_Fedora_23_x64_Release_version_badge.svg)](https://dotnetcli.blob.core.windows.net/dotnet/master/Binaries/Latest/dotnet-fedora.23-x64.latest.tar.gz)|[![](https://dotnetcli.blob.core.windows.net/dotnet/preview/Binaries/Latest/sharedfx_Fedora_23_x64_Release_version_badge.svg)](https://dotnetcli.blob.core.windows.net/dotnet/preview/Binaries/Latest/dotnet-fedora.23-x64.latest.tar.gz)|
|**OpenSUSE 13.2**|[![](https://dotnetcli.blob.core.windows.net/dotnet/master/Binaries/Latest/sharedfx_openSUSE_13_2_x64_Release_version_badge.svg)](https://dotnetcli.blob.core.windows.net/dotnet/master/Binaries/Latest/dotnet-opensuse.13.2-x64.latest.tar.gz)|[![](https://dotnetcli.blob.core.windows.net/dotnet/preview/Binaries/Latest/sharedfx_openSUSE_13_2_x64_Release_version_badge.svg)](https://dotnetcli.blob.core.windows.net/dotnet/preview/Binaries/Latest/dotnet-opensuse.13.2-x64.latest.tar.gz)|
|**OpenSUSE 42.1**|[![](https://dotnetcli.blob.core.windows.net/dotnet/master/Binaries/Latest/sharedfx_openSUSE_13_2_x64_Release_version_badge.svg)](https://dotnetcli.blob.core.windows.net/dotnet/master/Binaries/Latest/dotnet-opensuse.42.1-x64.latest.tar.gz)|N/A|
