# .NET Core Libraries (CoreFX)

The corefx repo contains the library implementation (called "CoreFX") for [.NET Core](http://github.com/dotnet/core). It includes System.Collections, System.IO, System.Xml, and many other components. You can see more information in [Documentation](Documentation/README.md). The corresponding [.NET Core Runtime repo](https://github.com/dotnet/coreclr) contains the runtime implementation (called "CoreCLR") for .NET Core. It includes RyuJIT, the .NET GC, and many other components. Runtime-specific library code - namely [System.Private.Corelib](https://github.com/dotnet/coreclr/tree/master/src/mscorlib) - lives in the CoreCLR repo. It needs to be built and versioned in tandem with the runtime. The rest of CoreFX is agnostic of runtime-implementation and can be run on any compatible .NET runtime.

## Source Index

.NET Core now has a source index: https://source.dot.net

## Build & Test Status

|    | Inner x64 Debug | Inner x64 Release | Outer x64 Debug | Outer x64 Release |
|:---|----------------:|------------------:|----------------:|------------------:|
|**CentOS 7.1**|[![x64-debug](https://ci.dot.net/job/dotnet_corefx/job/master/job/centos7.1_debug/badge/icon)](https://ci.dot.net/job/dotnet_corefx/job/master/job/centos7.1_debug/lastCompletedBuild/testReport)|[![x64-release](https://ci.dot.net/job/dotnet_corefx/job/master/job/centos7.1_release/badge/icon)](https://ci.dot.net/job/dotnet_corefx/job/master/job/centos7.1_release/lastCompletedBuild/testReport)|[![x64-debug](https://ci.dot.net/job/dotnet_corefx/job/master/job/outerloop_centos7.1_debug/badge/icon)](https://ci.dot.net/job/dotnet_corefx/job/master/job/outerloop_centos7.1_debug/lastCompletedBuild/testReport)|[![x64-release](https://ci.dot.net/job/dotnet_corefx/job/master/job/outerloop_centos7.1_release/badge/icon)](https://ci.dot.net/job/dotnet_corefx/job/master/job/outerloop_centos7.1_release/lastCompletedBuild/testReport)|
|**Debian 8**|[![x64-debug](https://ci.dot.net/job/dotnet_corefx/job/master/job/debian8.4_debug/badge/icon)](https://ci.dot.net/job/dotnet_corefx/job/master/job/debian8.4_debug/lastCompletedBuild/testReport)|[![x64-release](https://ci.dot.net/job/dotnet_corefx/job/master/job/debian8.4_release/badge/icon)](https://ci.dot.net/job/dotnet_corefx/job/master/job/debian8.4_release/lastCompletedBuild/testReport)|[![x64-debug](https://ci.dot.net/job/dotnet_corefx/job/master/job/debian8.4_debug/badge/icon)](https://ci.dot.net/job/dotnet_corefx/job/master/job/debian8.4_debug/lastCompletedBuild/testReport)|[![x64-release](https://ci.dot.net/job/dotnet_corefx/job/master/job/debian8.4_release/badge/icon)](https://ci.dot.net/job/dotnet_corefx/job/master/job/debian8.4_release/lastCompletedBuild/testReport)|
|**Fedora 23**|[![x64-debug](https://ci.dot.net/job/dotnet_corefx/job/master/job/fedora23_debug/badge/icon)](https://ci.dot.net/job/dotnet_corefx/job/master/job/fedora23_debug/lastCompletedBuild/testReport)|[![x64-release](https://ci.dot.net/job/dotnet_corefx/job/master/job/fedora23_release/badge/icon)](https://ci.dot.net/job/dotnet_corefx/job/master/job/fedora23_release/lastCompletedBuild/testReport)|[![x64-debug](https://ci.dot.net/job/dotnet_corefx/job/master/job/outerloop_fedora23_debug/badge/icon)](https://ci.dot.net/job/dotnet_corefx/job/master/job/outerloop_fedora23_debug/lastCompletedBuild/testReport)|[![x64-release](https://ci.dot.net/job/dotnet_corefx/job/master/job/outerloop_fedora23_release/badge/icon)](https://ci.dot.net/job/dotnet_corefx/job/master/job/outerloop_fedora23_release/lastCompletedBuild/testReport)|
|**Fedora 24**|[![x64-debug](https://ci.dot.net/job/dotnet_corefx/job/master/job/fedora24_debug/badge/icon)](https://ci.dot.net/job/dotnet_corefx/job/master/job/fedora24_debug/lastCompletedBuild/testReport)|[![x64-release](https://ci.dot.net/job/dotnet_corefx/job/master/job/fedora24_release/badge/icon)](https://ci.dot.net/job/dotnet_corefx/job/master/job/fedora24_release/lastCompletedBuild/testReport)|[![x64-debug](https://ci.dot.net/job/dotnet_corefx/job/master/job/outerloop_fedora24_debug/badge/icon)](https://ci.dot.net/job/dotnet_corefx/job/master/job/outerloop_fedora24_debug/lastCompletedBuild/testReport)|[![x64-release](https://ci.dot.net/job/dotnet_corefx/job/master/job/outerloop_fedora24_release/badge/icon)](https://ci.dot.net/job/dotnet_corefx/job/master/job/outerloop_fedora24_release/lastCompletedBuild/testReport)|
|**openSUSE 42.1**|[![x64-debug](https://ci.dot.net/job/dotnet_corefx/job/master/job/opensuse42.1_debug/badge/icon)](https://ci.dot.net/job/dotnet_corefx/job/master/job/opensuse42.1_debug/lastCompletedBuild/testReport)|[![x64-release](https://ci.dot.net/job/dotnet_corefx/job/master/job/opensuse42.1_release/badge/icon)](https://ci.dot.net/job/dotnet_corefx/job/master/job/opensuse42.1_release/lastCompletedBuild/testReport)|[![x64-debug](https://ci.dot.net/job/dotnet_corefx/job/master/job/outerloop_opensuse42.1_debug/badge/icon)](https://ci.dot.net/job/dotnet_corefx/job/master/job/outerloop_opensuse42.1_debug/lastCompletedBuild/testReport)|[![x64-release](https://ci.dot.net/job/dotnet_corefx/job/master/job/outerloop_opensuse42.1_release/badge/icon)](https://ci.dot.net/job/dotnet_corefx/job/master/job/outerloop_opensuse42.1_release/lastCompletedBuild/testReport)|
|**OS X 10.12**|[![x64-debug](https://ci.dot.net/job/dotnet_corefx/job/master/job/osx10.12_debug/badge/icon)](https://ci.dot.net/job/dotnet_corefx/job/master/job/osx10.12_debug/lastCompletedBuild/testReport)|[![x64-release](https://ci.dot.net/job/dotnet_corefx/job/master/job/osx10.12_release/badge/icon)](https://ci.dot.net/job/dotnet_corefx/job/master/job/osx10.12_release/lastCompletedBuild/testReport)|[![x64-debug](https://ci.dot.net/job/dotnet_corefx/job/master/job/outerloop_osx_debug/badge/icon)](https://ci.dot.net/job/dotnet_corefx/job/master/job/outerloop_osx_debug/lastCompletedBuild/testReport)|[![x64-release](https://ci.dot.net/job/dotnet_corefx/job/master/job/outerloop_osx_release/badge/icon)](https://ci.dot.net/job/dotnet_corefx/job/master/job/outerloop_osx_release/lastCompletedBuild/testReport)|
|**Red Hat 7.2**|[![x64-debug](https://ci.dot.net/job/dotnet_corefx/job/master/job/rhel7.2_debug/badge/icon)](https://ci.dot.net/job/dotnet_corefx/job/master/job/rhel7.2_debug/lastCompletedBuild/testReport)|[![x64-release](https://ci.dot.net/job/dotnet_corefx/job/master/job/rhel7.2_release/badge/icon)](https://ci.dot.net/job/dotnet_corefx/job/master/job/rhel7.2_release/lastCompletedBuild/testReport)|[![x64-debug](https://ci.dot.net/job/dotnet_corefx/job/master/job/outerloop_rhel7.2_debug/badge/icon)](https://ci.dot.net/job/dotnet_corefx/job/master/job/outerloop_rhel7.2_debug/lastCompletedBuild/testReport)|[![x64-release](https://ci.dot.net/job/dotnet_corefx/job/master/job/outerloop_rhel7.2_release/badge/icon)](https://ci.dot.net/job/dotnet_corefx/job/master/job/outerloop_rhel7.2_release/lastCompletedBuild/testReport)|
|**Ubuntu 14.04**|[![x64-debug](https://ci.dot.net/job/dotnet_corefx/job/master/job/ubuntu14.04_debug/badge/icon)](https://ci.dot.net/job/dotnet_corefx/job/master/job/ubuntu14.04_debug/lastCompletedBuild/testReport)|[![x64-release](https://ci.dot.net/job/dotnet_corefx/job/master/job/ubuntu14.04_release/badge/icon)](https://ci.dot.net/job/dotnet_corefx/job/master/job/ubuntu14.04_release/lastCompletedBuild/testReport)|[![x64-debug](https://ci.dot.net/job/dotnet_corefx/job/master/job/outerloop_ubuntu14.04_debug/badge/icon)](https://ci.dot.net/job/dotnet_corefx/job/master/job/outerloop_ubuntu14.04_debug/lastCompletedBuild/testReport)|[![x64-release](https://ci.dot.net/job/dotnet_corefx/job/master/job/outerloop_ubuntu14.04_release/badge/icon)](https://ci.dot.net/job/dotnet_corefx/job/master/job/outerloop_ubuntu14.04_release/lastCompletedBuild/testReport)|
|**Ubuntu 16.04**|[![x64-debug](https://ci.dot.net/job/dotnet_corefx/job/master/job/ubuntu16.04_debug/badge/icon)](https://ci.dot.net/job/dotnet_corefx/job/master/job/ubuntu16.04_debug/lastCompletedBuild/testReport)|[![x64-release](https://ci.dot.net/job/dotnet_corefx/job/master/job/ubuntu16.04_release/badge/icon)](https://ci.dot.net/job/dotnet_corefx/job/master/job/ubuntu16.04_release/lastCompletedBuild/testReport)|[![x64-debug](https://ci.dot.net/job/dotnet_corefx/job/master/job/outerloop_ubuntu16.04_debug/badge/icon)](https://ci.dot.net/job/dotnet_corefx/job/master/job/outerloop_ubuntu16.04_debug/lastCompletedBuild/testReport)|[![x64-release](https://ci.dot.net/job/dotnet_corefx/job/master/job/outerloop_ubuntu16.04_release/badge/icon)](https://ci.dot.net/job/dotnet_corefx/job/master/job/outerloop_ubuntu16.04_release/lastCompletedBuild/testReport)|
|**Ubuntu 16.10**|[![x64-debug](https://ci.dot.net/job/dotnet_corefx/job/master/job/ubuntu16.10_debug/badge/icon)](https://ci.dot.net/job/dotnet_corefx/job/master/job/ubuntu16.10_debug/lastCompletedBuild/testReport)|[![x64-release](https://ci.dot.net/job/dotnet_corefx/job/master/job/ubuntu16.10_release/badge/icon)](https://ci.dot.net/job/dotnet_corefx/job/master/job/ubuntu16.10_release/lastCompletedBuild/testReport)|[![x64-debug](https://ci.dot.net/job/dotnet_corefx/job/master/job/outerloop_ubuntu16.10_debug/badge/icon)](https://ci.dot.net/job/dotnet_corefx/job/master/job/outerloop_ubuntu16.10_debug/lastCompletedBuild/testReport)|[![x64-release](https://ci.dot.net/job/dotnet_corefx/job/master/job/outerloop_ubuntu16.10_release/badge/icon)](https://ci.dot.net/job/dotnet_corefx/job/master/job/outerloop_ubuntu16.10_release/lastCompletedBuild/testReport)|
|**PortableLinux**|[![x64-debug](https://ci.dot.net/job/dotnet_corefx/job/master/job/portablelinux_debug/badge/icon)](https://ci.dot.net/job/dotnet_corefx/job/master/job/portablelinux_debug/lastCompletedBuild/testReport)|[![x64-release](https://ci.dot.net/job/dotnet_corefx/job/master/job/portablelinux_release/badge/icon)](https://ci.dot.net/job/dotnet_corefx/job/master/job/portablelinux_release/lastCompletedBuild/testReport)|[![x64-debug](https://ci.dot.net/job/dotnet_corefx/job/master/job/outerloop_portablelinux_debug/badge/icon)](https://ci.dot.net/job/dotnet_corefx/job/master/job/outerloop_portablelinux_debug/lastCompletedBuild/testReport)|[![x64-release](https://ci.dot.net/job/dotnet_corefx/job/master/job/outerloop_portablelinux_release/badge/icon)](https://ci.dot.net/job/dotnet_corefx/job/master/job/outerloop_portablelinux_release/lastCompletedBuild/testReport)|
|**Windows 7**| | |[![x64-debug](https://ci.dot.net/job/dotnet_corefx/job/master/job/outerloop_win7_debug/badge/icon)](https://ci.dot.net/job/dotnet_corefx/job/master/job/outerloop_win7_debug/lastCompletedBuild/testReport)|[![x64-release](https://ci.dot.net/job/dotnet_corefx/job/master/job/outerloop_win7_release/badge/icon)](https://ci.dot.net/job/dotnet_corefx/job/master/job/outerloop_win7_release/lastCompletedBuild/testReport)|
|**Windows 8.1**|(x86) [![x64-debug](https://ci.dot.net/job/dotnet_corefx/job/master/job/windows_nt_debug/badge/icon)](https://ci.dot.net/job/dotnet_corefx/job/master/job/windows_nt_debug/lastCompletedBuild/testReport)|[![x64-release](https://ci.dot.net/job/dotnet_corefx/job/master/job/windows_nt_release/badge/icon)](https://ci.dot.net/job/dotnet_corefx/job/master/job/windows_nt_release/lastCompletedBuild/testReport)|[![x64-debug](https://ci.dot.net/job/dotnet_corefx/job/master/job/outerloop_windows_nt_debug/badge/icon)](https://ci.dot.net/job/dotnet_corefx/job/master/job/outerloop_windows_nt_debug/lastCompletedBuild/testReport)|[![x64-release](https://ci.dot.net/job/dotnet_corefx/job/master/job/outerloop_windows_nt_release/badge/icon)](https://ci.dot.net/job/dotnet_corefx/job/master/job/outerloop_windows_nt_release/lastCompletedBuild/testReport)|
|**Windows 10**| | |[![x64-debug](https://ci.dot.net/job/dotnet_corefx/job/master/job/outerloop_win10_debug/badge/icon)](https://ci.dot.net/job/dotnet_corefx/job/master/job/outerloop_win10_debug/lastCompletedBuild/testReport)|[![x64-release](https://ci.dot.net/job/dotnet_corefx/job/master/job/outerloop_win10_release/badge/icon)](https://ci.dot.net/job/dotnet_corefx/job/master/job/outerloop_win10_release/lastCompletedBuild/testReport)|
|**Windows Nano Server 2016**| | |[![x64-debug](https://ci.dot.net/job/dotnet_corefx/job/master/job/outerloop_winnano16_debug/badge/icon)](https://ci.dot.net/job/dotnet_corefx/job/master/job/outerloop_winnano16_debug/lastCompletedBuild/testReport)|[![x64-release](https://ci.dot.net/job/dotnet_corefx/job/master/job/outerloop_winnano16_release/badge/icon)](https://ci.dot.net/job/dotnet_corefx/job/master/job/outerloop_winnano16_release/lastCompletedBuild/testReport)|
|**Code Coverage (Windows)**| | |[![code coverage](https://ci.dot.net/job/dotnet_corefx/job/master/job/code_coverage_windows/badge/icon)](https://ci.dot.net/job/dotnet_corefx/job/master/job/code_coverage_windows/Code_Coverage_Report)||

## How to Engage, Contribute and Provide Feedback

Some of the best ways to contribute are to try things out, file bugs, and join in design conversations. If you are having issues with the Full .NET Framework or .NET Runtime, the best way to file a bug is at [Connect](http://connect.microsoft.com/VisualStudio) or through [Product Support](https://support.microsoft.com/en-us/contactus?ws=support) if you have a contract.

Want to get more familiar with what's going on in the code?
* [Pull requests](https://github.com/dotnet/corefx/pulls): [Open](https://github.com/dotnet/corefx/pulls?q=is%3Aopen+is%3Apr)/[Closed](https://github.com/dotnet/corefx/pulls?q=is%3Apr+is%3Aclosed)

Looking for something to work on? The list of [up-for-grabs issues](https://github.com/dotnet/corefx/labels/up%20for%20grabs) is a great place to start. See some of our guides for more details:

* [Contributing Guide](Documentation/project-docs/contributing.md)
* [Developer Guide](Documentation/project-docs/developer-guide.md)
* [Issue Guide](Documentation/project-docs/issue-guide.md)

We've also started to share some of our direction via more higher-level documentation, specifically:

* [How we triage](Documentation/project-docs/triage.md)
* [Porting to .NET Core](Documentation/project-docs/porting.md)

You are also encouraged to start a discussion by filing an issue or creating a
gist.

You can discuss .NET OSS more generally in the [.NET Foundation forums].

Want to chat with other members of the CoreFX community?

[![.NET Slack Status](https://aspnetcoreslack.herokuapp.com/badge.svg?2)](http://tattoocoder.com/aspnet-slack-sign-up/) [![Join the chat at https://gitter.im/dotnet/corefx](https://badges.gitter.im/Join%20Chat.svg)](https://gitter.im/dotnet/corefx?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)

[.NET Foundation forums]: http://forums.dotnetfoundation.org/

This project has adopted the code of conduct defined by the Contributor Covenant
to clarify expected behavior in our community. For more information, see the [.NET Foundation Code of Conduct](http://www.dotnetfoundation.org/code-of-conduct).

### Reporting security issues and security bugs

Security issues and bugs should be reported privately, via email, to the
Microsoft Security Response Center (MSRC) <secure@microsoft.com>. You should
receive a response within 24 hours. If for some reason you do not, please follow
up via email to ensure we received your original message. Further information,
including the MSRC PGP key, can be found in the
[Security TechCenter](https://technet.microsoft.com/en-us/security/ff852094.aspx).

## .NET Core Library Components

The repo contains the source for each of the assemblies that comprises .NET Core.  Each ```Microsoft.*``` or ```System.*``` folder under
[src](https://github.com/dotnet/corefx/tree/master/src) represents an individual library.  Each such folder may contain a ```ref``` folder,
which contains the source representing the "contract" or "reference assembly" for that library.  It may also contain a ```src``` folder,
which contains the source for some or all of the implementation for that library (some implementation may live in System.Private.Corelib in the 
[coreclr repo](https://github.com/dotnet/coreclr), with the build tooling generating type forwards from the library assembly to System.Private.Corelib.)  
It may also contain a ```test``` folder containing the tests associated with that library, whether the implementation source lives in corefx 
or in coreclr.

## Daily Builds

Daily builds of .NET Core components are published to [dotnet-core MyGet gallery](https://dotnet.myget.org/gallery/dotnet-core).
The latest version number of each library can be seen in that gallery.

## License

.NET Core (including the corefx repo) is licensed under the [MIT license](LICENSE).

## .NET Foundation

.NET Core is a [.NET Foundation](http://www.dotnetfoundation.org/projects) project.

## Related Projects
There are many .NET related projects on GitHub.

- The [.NET home repo](https://github.com/Microsoft/dotnet) links to 100s of .NET projects, from Microsoft and the community.
- The [.NET Core repo](https://github.com/dotnet/core) links to .NET Core related projects from Microsoft.
- The [ASP.NET home repo](https://github.com/aspnet/home) is the best place to start learning about ASP.NET Core.
- [dotnet.github.io](http://dotnet.github.io) is a good place to discover .NET Foundation projects.
