.NET Core Runtime & host setup repo
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

Build Status
------------

|Ubuntu 14.04 |Ubuntu 16.04 |Debian 8.2 |Windows x64 |Windows x86 |Mac OS X |CentOS 7.1 |RHEL 7.2 |Fedora 23 |OpenSUSE 13.2 |
|:------:|:------:|:------:|:------:|:------:|:------:|:------:|:------:|:------:|:------:|
|[![](https://mseng.visualstudio.com/_apis/public/build/definitions/d09b7a4d-0a51-4c0e-a15a-07921d5b558f/3599/badge)](https://mseng.visualstudio.com/dotnetcore/_build?_a=completed&definitionId=3599)|[![](https://mseng.visualstudio.com/_apis/public/build/definitions/d09b7a4d-0a51-4c0e-a15a-07921d5b558f/3600/badge)](https://mseng.visualstudio.com/dotnetcore/_build?_a=completed&definitionId=3600)|[![](https://mseng.visualstudio.com/_apis/public/build/definitions/d09b7a4d-0a51-4c0e-a15a-07921d5b558f/3592/badge)](https://mseng.visualstudio.com/dotnetcore/_build?_a=completed&definitionId=3592)|[![](https://mseng.visualstudio.com/_apis/public/build/definitions/d09b7a4d-0a51-4c0e-a15a-07921d5b558f/3597/badge)](https://mseng.visualstudio.com/dotnetcore/_build?_a=completed&definitionId=3597)|[![](https://mseng.visualstudio.com/_apis/public/build/definitions/d09b7a4d-0a51-4c0e-a15a-07921d5b558f/3598/badge)](https://mseng.visualstudio.com/dotnetcore/_build?_a=completed&definitionId=3598)|[![](https://mseng.visualstudio.com/_apis/public/build/definitions/d09b7a4d-0a51-4c0e-a15a-07921d5b558f/3595/badge)](https://mseng.visualstudio.com/dotnetcore/_build?_a=completed&definitionId=3595)|[![](https://mseng.visualstudio.com/_apis/public/build/definitions/d09b7a4d-0a51-4c0e-a15a-07921d5b558f/3591/badge)](https://mseng.visualstudio.com/dotnetcore/_build?_a=completed&definitionId=3591)|[![](https://mseng.visualstudio.com/_apis/public/build/definitions/d09b7a4d-0a51-4c0e-a15a-07921d5b558f/3596/badge)](https://mseng.visualstudio.com/dotnetcore/_build?_a=completed&definitionId=3596)|[![](https://mseng.visualstudio.com/_apis/public/build/definitions/d09b7a4d-0a51-4c0e-a15a-07921d5b558f/3593/badge)](https://mseng.visualstudio.com/dotnetcore/_build?_a=completed&definitionId=3593)|[![](https://mseng.visualstudio.com/_apis/public/build/definitions/d09b7a4d-0a51-4c0e-a15a-07921d5b558f/3594/badge)](https://mseng.visualstudio.com/dotnetcore/_build?_a=completed&definitionId=3594)|

Latest versions
-----------------------

|Platform |Version |
|---------|:------:|
|**Windows x64**|[![](https://dotnetcli.blob.core.windows.net/dotnet/preview/Binaries/Latest/sharedfx_Windows_x64_Release_version_badge.svg)](https://dotnetcli.blob.core.windows.net/dotnet/preview/dnvm/latest.sharedfx.win.x64.version)|
|**Windows x86**|[![](https://dotnetcli.blob.core.windows.net/dotnet/preview/Binaries/Latest/sharedfx_Windows_x86_Release_version_badge.svg)](https://dotnetcli.blob.core.windows.net/dotnet/preview/dnvm/latest.sharedfx.win.x86.version)|
|**Ubuntu 14.04**|[![](https://dotnetcli.blob.core.windows.net/dotnet/preview/Binaries/Latest/sharedfx_Ubuntu_x64_Release_version_badge.svg)](https://dotnetcli.blob.core.windows.net/dotnet/preview/dnvm/latest.sharedfx.ubuntu.x64.version)|
|**Ubuntu 16.04**|[![](https://dotnetcli.blob.core.windows.net/dotnet/preview/Binaries/Latest/sharedfx_Ubuntu_16_04_x64_Release_version_badge.svg)](https://dotnetcli.blob.core.windows.net/dotnet/preview/dnvm/latest.sharedfx.ubuntu.16.04.x64.version)|
|**Debian 8.2**|[![](https://dotnetcli.blob.core.windows.net/dotnet/preview/Binaries/Latest/sharedfx_Debian_x64_Release_version_badge.svg)](https://dotnetcli.blob.core.windows.net/dotnet/preview/dnvm/latest.sharedfx.debian.x64.version)|
|**Mac OS X**|[![](https://dotnetcli.blob.core.windows.net/dotnet/preview/Binaries/Latest/sharedfx_OSX_x64_Release_version_badge.svg)](https://dotnetcli.blob.core.windows.net/dotnet/preview/dnvm/latest.sharedfx.osx.x64.version)|
|**CentOS 7.1**|[![](https://dotnetcli.blob.core.windows.net/dotnet/preview/Binaries/Latest/sharedfx_CentOS_x64_Release_version_badge.svg)](https://dotnetcli.blob.core.windows.net/dotnet/preview/dnvm/latest.sharedfx.centos.x64.version)|
|**RHEL 7.2**|[![](https://dotnetcli.blob.core.windows.net/dotnet/preview/Binaries/Latest/sharedfx_RHEL_x64_Release_version_badge.svg)](https://dotnetcli.blob.core.windows.net/dotnet/preview/dnvm/latest.sharedfx.rhel.x64.version)|
|**Fedora 23**|[![](https://dotnetcli.blob.core.windows.net/dotnet/preview/Binaries/Latest/sharedfx_Fedora_23_x64_Release_version_badge.svg)](https://dotnetcli.blob.core.windows.net/dotnet/preview/dnvm/latest.sharedfx.fedora.23.x64.version)|
|**OpenSUSE 13.2**|[![](https://dotnetcli.blob.core.windows.net/dotnet/preview/Binaries/Latest/sharedfx_openSUSE_13_2_x64_Release_version_badge.svg)](https://dotnetcli.blob.core.windows.net/dotnet/preview/dnvm/latest.sharedfx.opensuse.13.2.x64.version)|
