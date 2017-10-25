# .NET Core Libraries (CoreFX)

This repo contains the library implementation (called "CoreFX") for .NET Core. It includes System.Collections, System.IO, System.Xml, and many other components.
The corresponding [.NET Core Runtime repo](https://github.com/dotnet/coreclr) (called "CoreCLR") contains the runtime implementation for .NET Core. It includes RyuJIT, the .NET GC, and many other components.
Runtime-specific library code ([mscorlib](https://github.com/dotnet/coreclr/tree/master/src/mscorlib)) lives in the CoreCLR repo. It needs to be built and versioned in tandem with the runtime. The rest of CoreFX is agnostic of runtime-implementation and can be run on any compatible .NET runtime (e.g. [CoreRT](https://github.com/dotnet/corert)).



## .NET Core

Official Starting Page: http://dotnet.github.io

* [How to use .NET Core](https://github.com/dotnet/core/#get-started) (with VS, VS Code, command-line CLI)
  * [Install official releases](https://www.microsoft.com/net/core)
  * [Documentation](https://docs.microsoft.com/en-us/dotnet) (Get Started, Tutorials, Porting from .NET Framework, API reference, ...)
    * [Deploying apps](https://docs.microsoft.com/en-us/dotnet/articles/core/preview3/deploying)
  * [Supported OS versions](https://github.com/dotnet/core/blob/master/roadmap.md#technology-roadmaps)
* [Roadmap](https://github.com/dotnet/core/blob/master/roadmap.md)
* [Releases](https://github.com/dotnet/core/tree/master/release-notes)
* [Bringing more APIs to .NET Core](https://github.com/dotnet/corefx/blob/master/Documentation/project-docs/porting.md) (and why some APIs will be left out)



## How to Engage, Contribute and Provide Feedback

Some of the best ways to contribute are to try things out, file bugs, join in design conversations, and fix issues.

* [Dogfooding daily builds](https://github.com/dotnet/corefx/blob/master/Documentation/project-docs/dogfooding.md)
* If you have a question or idea, [file a new issue](https://github.com/dotnet/corefx/issues/new).

If you are having issues with the "full" .NET Framework (also called "Desktop"), the best way to file a bug is at [Connect](http://connect.microsoft.com/VisualStudio) or through [Product Support](https://support.microsoft.com/en-us/contactus?ws=support) if you have a contract.

### Issue Guide

This section is **in progress** here: [New contributor Docs - Issues](https://github.com/dotnet/corefx/wiki/New-contributor-Docs#issue-guide) (feel free to make it better - it's easy-to-edit wiki with RW permissions to everyone!)

Each issue area has one or more Microsoft owners, who are [listed here](https://github.com/dotnet/corefx/blob/master/Documentation/project-docs/issue-guide.md).

### Contributing Guide

This section is **in progress** here: [New contributor Docs - Contributing](https://github.com/dotnet/corefx/wiki/New-contributor-Docs#contributing-guide) (feel free to make it better - it's easy-to-edit wiki with RW permissions to everyone!) 

### Useful Links

* [.NET Core source index](https://source.dot.net) / [.NET Framework source index](https://referencesource.microsoft.com)
* [API Reference docs](https://docs.microsoft.com/en-us/dotnet/core/api)
* [.NET API Catalog](http://apisof.net) (incl. APIs from daily builds and API usage info)

### Community

* General .NET OSS discussions: [.NET Foundation forums](http://forums.dotnetfoundation.org)
* Chat with other community members [![Join the chat at https://gitter.im/dotnet/corefx](https://badges.gitter.im/Join%20Chat.svg)](https://gitter.im/dotnet/corefx?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge) [![.NET Slack Status](https://aspnetcoreslack.herokuapp.com/badge.svg?2)](http://tattoocoder.com/aspnet-slack-sign-up)

This project has adopted the code of conduct defined by the [Contributor Covenant](http://contributor-covenant.org/)
to clarify expected behavior in our community. For more information, see the [.NET Foundation Code of Conduct](http://www.dotnetfoundation.org/code-of-conduct).

### Reporting security issues and security bugs

Security issues and bugs should be reported privately, via email, to the Microsoft Security Response Center (MSRC) <secure@microsoft.com>. You should receive a response within 24 hours. If for some reason you do not, please follow up via email to ensure we received your original message. Further information, including the MSRC PGP key, can be found in the [Security TechCenter](https://technet.microsoft.com/en-us/security/ff852094.aspx).

Also see info about related [Microsoft .NET Core and ASP.NET Core Bug Bounty Program](https://technet.microsoft.com/en-us/mt764065.aspx).

## License

.NET Core (including the corefx repo) is licensed under the [MIT license](LICENSE.TXT).



## .NET Foundation

.NET Core is a [.NET Foundation](http://www.dotnetfoundation.org/projects) project.

There are many .NET related projects on GitHub.

- [.NET home repo](https://github.com/Microsoft/dotnet) - links to 100s of .NET projects, from Microsoft and the community.
- [ASP.NET Core home](https://github.com/aspnet/home) - the best place to start learning about ASP.NET Core.



## CoreFX Project

### Daily Builds

Daily builds of .NET Core components are published to [dotnet-core MyGet gallery](https://dotnet.myget.org/gallery/dotnet-core).
The latest version number of each library can be seen in that gallery.

### Build & Test Status

Note: See officially supported [OS versions](https://github.com/dotnet/core/blob/master/roadmap.md#technology-roadmaps).

|    | Inner x64 Debug | Inner x64 Release | Outer x64 Debug | Outer x64 Release |
|:---|----------------:|------------------:|----------------:|------------------:|
|**CentOS 7.1**|[![x64-debug](https://ci.dot.net/job/dotnet_corefx/job/master/job/centos7.1_debug/badge/icon)](https://ci.dot.net/job/dotnet_corefx/job/master/job/centos7.1_debug/lastCompletedBuild/testReport)|[![x64-release](https://ci.dot.net/job/dotnet_corefx/job/master/job/centos7.1_release/badge/icon)](https://ci.dot.net/job/dotnet_corefx/job/master/job/centos7.1_release/lastCompletedBuild/testReport)|[![x64-debug](https://ci.dot.net/job/dotnet_corefx/job/master/job/outerloop_netcoreapp_centos7.1_debug/badge/icon)](https://ci.dot.net/job/dotnet_corefx/job/master/job/outerloop_netcoreapp_centos7.1_debug/lastCompletedBuild/testReport)|[![x64-release](https://ci.dot.net/job/dotnet_corefx/job/master/job/outerloop_netcoreapp_centos7.1_release/badge/icon)](https://ci.dot.net/job/dotnet_corefx/job/master/job/outerloop_netcoreapp_centos7.1_release/lastCompletedBuild/testReport)|
|**Debian 8**|[![x64-debug](https://ci.dot.net/job/dotnet_corefx/job/master/job/debian8.4_debug/badge/icon)](https://ci.dot.net/job/dotnet_corefx/job/master/job/debian8.4_debug/lastCompletedBuild/testReport)|[![x64-release](https://ci.dot.net/job/dotnet_corefx/job/master/job/debian8.4_release/badge/icon)](https://ci.dot.net/job/dotnet_corefx/job/master/job/debian8.4_release/lastCompletedBuild/testReport)|[![x64-debug](https://ci.dot.net/job/dotnet_corefx/job/master/job/outerloop_netcoreapp_debian8.4_debug/badge/icon)](https://ci.dot.net/job/dotnet_corefx/job/master/job/outerloop_netcoreapp_debian8.4_debug/lastCompletedBuild/testReport)|[![x64-release](https://ci.dot.net/job/dotnet_corefx/job/master/job/outerloop_netcoreapp_debian8.4_release/badge/icon)](https://ci.dot.net/job/dotnet_corefx/job/master/job/outerloop_netcoreapp_debian8.4_release/lastCompletedBuild/testReport)|
|**Fedora 24**|[![x64-debug](https://ci.dot.net/job/dotnet_corefx/job/master/job/fedora24_debug/badge/icon)](https://ci.dot.net/job/dotnet_corefx/job/master/job/fedora24_debug/lastCompletedBuild/testReport)|[![x64-release](https://ci.dot.net/job/dotnet_corefx/job/master/job/fedora24_release/badge/icon)](https://ci.dot.net/job/dotnet_corefx/job/master/job/fedora24_release/lastCompletedBuild/testReport)|[![x64-debug](https://ci.dot.net/job/dotnet_corefx/job/master/job/outerloop_netcoreapp_fedora24_debug/badge/icon)](https://ci.dot.net/job/dotnet_corefx/job/master/job/outerloop_netcoreapp_fedora24_debug/lastCompletedBuild/testReport)|[![x64-release](https://ci.dot.net/job/dotnet_corefx/job/master/job/outerloop_netcoreapp_fedora24_release/badge/icon)](https://ci.dot.net/job/dotnet_corefx/job/master/job/outerloop_netcoreapp_fedora24_release/lastCompletedBuild/testReport)|
|**OS X 10.12**|[![x64-debug](https://ci.dot.net/job/dotnet_corefx/job/master/job/osx10.12_debug/badge/icon)](https://ci.dot.net/job/dotnet_corefx/job/master/job/osx10.12_debug/lastCompletedBuild/testReport)|[![x64-release](https://ci.dot.net/job/dotnet_corefx/job/master/job/osx10.12_release/badge/icon)](https://ci.dot.net/job/dotnet_corefx/job/master/job/osx10.12_release/lastCompletedBuild/testReport)|[![x64-debug](https://ci.dot.net/job/dotnet_corefx/job/master/job/outerloop_netcoreapp_osx_debug/badge/icon)](https://ci.dot.net/job/dotnet_corefx/job/master/job/outerloop_netcoreapp_osx_debug/lastCompletedBuild/testReport)|[![x64-release](https://ci.dot.net/job/dotnet_corefx/job/master/job/outerloop_netcoreapp_osx_release/badge/icon)](https://ci.dot.net/job/dotnet_corefx/job/master/job/outerloop_netcoreapp_osx_release/lastCompletedBuild/testReport)|
|**Red Hat 7.2**|[![x64-debug](https://ci.dot.net/job/dotnet_corefx/job/master/job/rhel7.2_debug/badge/icon)](https://ci.dot.net/job/dotnet_corefx/job/master/job/rhel7.2_debug/lastCompletedBuild/testReport)|[![x64-release](https://ci.dot.net/job/dotnet_corefx/job/master/job/rhel7.2_release/badge/icon)](https://ci.dot.net/job/dotnet_corefx/job/master/job/rhel7.2_release/lastCompletedBuild/testReport)|[![x64-debug](https://ci.dot.net/job/dotnet_corefx/job/master/job/outerloop_netcoreapp_rhel7.2_debug/badge/icon)](https://ci.dot.net/job/dotnet_corefx/job/master/job/outerloop_netcoreapp_rhel7.2_debug/lastCompletedBuild/testReport)|[![x64-release](https://ci.dot.net/job/dotnet_corefx/job/master/job/outerloop_netcoreapp_rhel7.2_release/badge/icon)](https://ci.dot.net/job/dotnet_corefx/job/master/job/outerloop_netcoreapp_rhel7.2_release/lastCompletedBuild/testReport)|
|**Ubuntu 14.04**|[![x64-debug](https://ci.dot.net/job/dotnet_corefx/job/master/job/ubuntu14.04_debug/badge/icon)](https://ci.dot.net/job/dotnet_corefx/job/master/job/ubuntu14.04_debug/lastCompletedBuild/testReport)|[![x64-release](https://ci.dot.net/job/dotnet_corefx/job/master/job/ubuntu14.04_release/badge/icon)](https://ci.dot.net/job/dotnet_corefx/job/master/job/ubuntu14.04_release/lastCompletedBuild/testReport)|[![x64-debug](https://ci.dot.net/job/dotnet_corefx/job/master/job/outerloop_netcoreapp_ubuntu14.04_debug/badge/icon)](https://ci.dot.net/job/dotnet_corefx/job/master/job/outerloop_netcoreapp_ubuntu14.04_debug/lastCompletedBuild/testReport)|[![x64-release](https://ci.dot.net/job/dotnet_corefx/job/master/job/outerloop_netcoreapp_ubuntu14.04_release/badge/icon)](https://ci.dot.net/job/dotnet_corefx/job/master/job/outerloop_netcoreapp_ubuntu14.04_release/lastCompletedBuild/testReport)|
|**Ubuntu 16.04**|[![x64-debug](https://ci.dot.net/job/dotnet_corefx/job/master/job/ubuntu16.04_debug/badge/icon)](https://ci.dot.net/job/dotnet_corefx/job/master/job/ubuntu16.04_debug/lastCompletedBuild/testReport)|[![x64-release](https://ci.dot.net/job/dotnet_corefx/job/master/job/ubuntu16.04_release/badge/icon)](https://ci.dot.net/job/dotnet_corefx/job/master/job/ubuntu16.04_release/lastCompletedBuild/testReport)|[![x64-debug](https://ci.dot.net/job/dotnet_corefx/job/master/job/outerloop_netcoreapp_ubuntu16.04_debug/badge/icon)](https://ci.dot.net/job/dotnet_corefx/job/master/job/outerloop_netcoreapp_ubuntu16.04_debug/lastCompletedBuild/testReport)|[![x64-release](https://ci.dot.net/job/dotnet_corefx/job/master/job/outerloop_netcoreapp_ubuntu16.04_release/badge/icon)](https://ci.dot.net/job/dotnet_corefx/job/master/job/outerloop_netcoreapp_ubuntu16.04_release/lastCompletedBuild/testReport)|
|**Ubuntu 16.10**|[![x64-debug](https://ci.dot.net/job/dotnet_corefx/job/master/job/ubuntu16.10_debug/badge/icon)](https://ci.dot.net/job/dotnet_corefx/job/master/job/ubuntu16.10_debug/lastCompletedBuild/testReport)|[![x64-release](https://ci.dot.net/job/dotnet_corefx/job/master/job/ubuntu16.10_release/badge/icon)](https://ci.dot.net/job/dotnet_corefx/job/master/job/ubuntu16.10_release/lastCompletedBuild/testReport)|[![x64-debug](https://ci.dot.net/job/dotnet_corefx/job/master/job/outerloop_netcoreapp_ubuntu16.10_debug/badge/icon)](https://ci.dot.net/job/dotnet_corefx/job/master/job/outerloop_netcoreapp_ubuntu16.10_debug/lastCompletedBuild/testReport)|[![x64-release](https://ci.dot.net/job/dotnet_corefx/job/master/job/outerloop_netcoreapp_ubuntu16.10_release/badge/icon)](https://ci.dot.net/job/dotnet_corefx/job/master/job/outerloop_netcoreapp_ubuntu16.10_release/lastCompletedBuild/testReport)|
|**PortableLinux**|[![x64-debug](https://ci.dot.net/job/dotnet_corefx/job/master/job/portablelinux_debug/badge/icon)](https://ci.dot.net/job/dotnet_corefx/job/master/job/portablelinux_debug/lastCompletedBuild/testReport)|[![x64-release](https://ci.dot.net/job/dotnet_corefx/job/master/job/portablelinux_release/badge/icon)](https://ci.dot.net/job/dotnet_corefx/job/master/job/portablelinux_release/lastCompletedBuild/testReport)|[![x64-debug](https://ci.dot.net/job/dotnet_corefx/job/master/job/outerloop_netcoreapp_portablelinux_debug/badge/icon)](https://ci.dot.net/job/dotnet_corefx/job/master/job/outerloop_netcoreapp_portablelinux_debug/lastCompletedBuild/testReport)|[![x64-release](https://ci.dot.net/job/dotnet_corefx/job/master/job/outerloop_netcoreapp_portablelinux_release/badge/icon)](https://ci.dot.net/job/dotnet_corefx/job/master/job/outerloop_netcoreapp_portablelinux_release/lastCompletedBuild/testReport)|
|**Windows 7**| | |[![x64-debug](https://ci.dot.net/job/dotnet_corefx/job/master/job/outerloop_netcoreapp_win7_debug/badge/icon)](https://ci.dot.net/job/dotnet_corefx/job/master/job/outerloop_netcoreapp_win7_debug/lastCompletedBuild/testReport)|[![x64-release](https://ci.dot.net/job/dotnet_corefx/job/master/job/outerloop_netcoreapp_win7_release/badge/icon)](https://ci.dot.net/job/dotnet_corefx/job/master/job/outerloop_netcoreapp_win7_release/lastCompletedBuild/testReport)|
|**Windows 8.1**|(x86) [![x64-debug](https://ci.dot.net/job/dotnet_corefx/job/master/job/windows_nt_debug/badge/icon)](https://ci.dot.net/job/dotnet_corefx/job/master/job/windows_nt_debug/lastCompletedBuild/testReport)|[![x64-release](https://ci.dot.net/job/dotnet_corefx/job/master/job/windows_nt_release/badge/icon)](https://ci.dot.net/job/dotnet_corefx/job/master/job/windows_nt_release/lastCompletedBuild/testReport)|[![x64-debug](https://ci.dot.net/job/dotnet_corefx/job/master/job/outerloop_netcoreapp_windows_nt_debug/badge/icon)](https://ci.dot.net/job/dotnet_corefx/job/master/job/outerloop_netcoreapp_windows_nt_debug/lastCompletedBuild/testReport)|[![x64-release](https://ci.dot.net/job/dotnet_corefx/job/master/job/outerloop_netcoreapp_windows_nt_release/badge/icon)](https://ci.dot.net/job/dotnet_corefx/job/master/job/outerloop_netcoreapp_windows_nt_release/lastCompletedBuild/testReport)|
|**Code Coverage (Windows)**| | |[![code coverage](https://ci.dot.net/job/dotnet_corefx/job/master/job/code_coverage_windows/badge/icon)](https://ci.dot.net/job/dotnet_corefx/job/master/job/code_coverage_windows/Code_Coverage_Report)|
