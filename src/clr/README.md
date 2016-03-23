.NET Core Runtime (CoreCLR)
===========================

The CoreCLR repo contains the complete runtime implementation for [.NET Core](http://github.com/dotnet/core). It includes RyuJIT, the .NET GC, native interop and many other components. It is cross-platform, with multiple OS and CPU ports in progress.

Note that the library implementation CoreFX (System.Collections, System.IO, System.Xml and so on) lives in another repo [dotnet/corefx](https://github.com/dotnet/corefx).

Build Status
------------

|   | Debug | Release |
|---|:-----:|:-------:|
|**CentOS 7.1**|[![x64 status](https://img.shields.io/jenkins/s/http/dotnet-ci.cloudapp.net/job/dotnet_coreclr/job/master/debug_centos7.1.svg?label=x64)](http://dotnet-ci.cloudapp.net/job/dotnet_coreclr/job/master/job/debug_centos7.1)|[![x64 status](https://img.shields.io/jenkins/s/http/dotnet-ci.cloudapp.net/job/dotnet_coreclr/job/master/release_centos7.1.svg?label=x64)](http://dotnet-ci.cloudapp.net/job/dotnet_coreclr/job/master/job/release_centos7.1)|
|**Debian 8.2**|[![x64 status](https://img.shields.io/jenkins/s/http/dotnet-ci.cloudapp.net/job/dotnet_coreclr/job/master/debug_debian8.2.svg?label=x64)](http://dotnet-ci.cloudapp.net/job/dotnet_coreclr/job/master/job/debug_debian8.2)|[![x64 status](https://img.shields.io/jenkins/s/http/dotnet-ci.cloudapp.net/job/dotnet_coreclr/job/master/release_debian8.2.svg?label=x64)](http://dotnet-ci.cloudapp.net/job/dotnet_coreclr/job/master/job/release_debian8.2)|
|**FreeBSD 10.1**|[![x64 status](https://img.shields.io/jenkins/s/http/dotnet-ci.cloudapp.net/job/dotnet_coreclr/job/master/debug_freebsd.svg?label=x64)](http://dotnet-ci.cloudapp.net/job/dotnet_coreclr/job/master/job/debug_freebsd)|[![x64 status](https://img.shields.io/jenkins/s/http/dotnet-ci.cloudapp.net/job/dotnet_coreclr/job/master/release_freebsd.svg?label=x64)](http://dotnet-ci.cloudapp.net/job/dotnet_coreclr/job/master/job/release_freebsd)|
|**openSUSE 13.2**|[![x64 status](https://img.shields.io/jenkins/s/http/dotnet-ci.cloudapp.net/job/dotnet_coreclr/job/master/debug_opensuse13.2.svg?label=x64)](http://dotnet-ci.cloudapp.net/job/dotnet_coreclr/job/master/job/debug_opensuse13.2)|[![x64 status](https://img.shields.io/jenkins/s/http/dotnet-ci.cloudapp.net/job/dotnet_coreclr/job/master/release_opensuse13.2.svg?label=x64)](http://dotnet-ci.cloudapp.net/job/dotnet_coreclr/job/master/job/release_opensuse13.2)|
|**OS X 10.11**|[![x64 status](https://img.shields.io/jenkins/s/http/dotnet-ci.cloudapp.net/job/dotnet_coreclr/job/master/debug_osx.svg?label=x64)](http://dotnet-ci.cloudapp.net/job/dotnet_coreclr/job/master/job/debug_osx)|[![x64 status](https://img.shields.io/jenkins/s/http/dotnet-ci.cloudapp.net/job/dotnet_coreclr/job/master/release_osx.svg?label=x64)](http://dotnet-ci.cloudapp.net/job/dotnet_coreclr/job/master/job/release_osx)|
|**Red Hat 7.2**|[![x64 status](https://img.shields.io/jenkins/s/http/dotnet-ci.cloudapp.net/job/dotnet_coreclr/job/master/debug_rhel7.2.svg?label=x64)](http://dotnet-ci.cloudapp.net/job/dotnet_coreclr/debug_rhel7.2)|[![x64 status](https://img.shields.io/jenkins/s/http/dotnet-ci.cloudapp.net/job/dotnet_coreclr/job/master/release_rhel7.2.svg?label=x64)](http://dotnet-ci.cloudapp.net/job/dotnet_coreclr/job/master/job/release_rhel7.2)|
|**Ubuntu 14.04**|[![x64 status](https://img.shields.io/jenkins/s/http/dotnet-ci.cloudapp.net/job/dotnet_coreclr/job/master/debug_ubuntu.svg?label=x64)](http://dotnet-ci.cloudapp.net/job/dotnet_coreclr/job/master/job/debug_ubuntu)|[![x64 status](https://img.shields.io/jenkins/s/http/dotnet-ci.cloudapp.net/job/dotnet_coreclr/job/master/release_ubuntu.svg?label=x64)](http://dotnet-ci.cloudapp.net/job/dotnet_coreclr/job/master/job/release_ubuntu)|
|**Ubuntu 15.10**|[![x64 status](https://img.shields.io/jenkins/s/http/dotnet-ci.cloudapp.net/job/dotnet_coreclr/job/master/debug_ubuntu15.10.svg?label=x64)](http://dotnet-ci.cloudapp.net/job/dotnet_coreclr/job/master/job/debug_ubuntu15.10)|[![x64 status](https://img.shields.io/jenkins/s/http/dotnet-ci.cloudapp.net/job/dotnet_coreclr/job/master/release_ubuntu15.10.svg?label=x64)](http://dotnet-ci.cloudapp.net/job/dotnet_coreclr/job/master/job/release_ubuntu15.10)|
|**Windows 8.1**|[![x64 status](https://img.shields.io/jenkins/s/http/dotnet-ci.cloudapp.net/job/dotnet_coreclr/job/master/debug_windows_nt.svg?label=x64)](http://dotnet-ci.cloudapp.net/job/dotnet_coreclr/job/master/job/debug_windows_nt)<br/>[![arm64 status](https://img.shields.io/jenkins/s/http/dotnet-ci.cloudapp.net/job/dotnet_coreclr/job/master/arm64_cross_debug_windows_nt.svg?label=arm64)](http://dotnet-ci.cloudapp.net/job/dotnet_coreclr/job/master/job/arm64_cross_debug_windows_nt)|[![x64 status](https://img.shields.io/jenkins/s/http/dotnet-ci.cloudapp.net/job/dotnet_coreclr/job/master/release_windows_nt.svg?label=x64)](http://dotnet-ci.cloudapp.net/job/dotnet_coreclr/job/master/job/release_windows_nt)<br/>[![arm64 status](https://img.shields.io/jenkins/s/http/dotnet-ci.cloudapp.net/job/dotnet_coreclr/job/master/arm64_cross_release_windows_nt.svg?label=arm64)](http://dotnet-ci.cloudapp.net/job/dotnet_coreclr/job/master/job/arm64_cross_release_windows_nt)|

Get .NET Core
-------------

|                     |Linux   |Windows |Mac OS X |FreeBSD  |
|---------------------|--------|--------|---------|---------|
|Build from **Source**| [Instructions](Documentation/building/linux-instructions.md) | [Instructions](Documentation/building/windows-instructions.md) | [Instructions](Documentation/building/osx-instructions.md) | [Instructions](Documentation/building/freebsd-instructions.md) |
|Get **Binaries**     | [DNX SDK](Documentation/install/get-dotnetcore-dnx-linux.md)|[DNX SDK](Documentation/install/get-dotnetcore-dnx-windows.md) <br> [Raw](Documentation/install/get-dotnetcore-windows.md)|[DNX SDK](Documentation/install/get-dotnetcore-dnx-osx.md)||

Chat Room
---------

Want to chat with other members of the CoreCLR community?

[![Join the chat at https://gitter.im/dotnet/coreclr](https://badges.gitter.im/Join%20Chat.svg)](https://gitter.im/dotnet/coreclr?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)

Learn about CoreCLR and .NET Core
---------------------------------

The best ways to learn about CoreCLR are to try out the product instructions and to read the "Book of the Runtime" architecture documents that describe the inner workings of the product. New devs to the CLR team are encouraged to read these documents before making substative changes to the product. They are equally useful for open source contributors.

- [.NET Core Roadmap](https://github.com/dotnet/core/blob/master/roadmap.md)
- [Product instructions](Documentation/README.md)
- [Introduction to the Common Language Runtime](Documentation/botr/intro-to-clr.md)
- [Book of the Runtime](Documentation/README.md#book-of-the-runtime)
- [CoreCLR Documents](Documentation)

.NET Core is part of ASP.NET 5 and is a subset of the .NET Framework. You can learn more about .NET Core and how and where you can use it in the [CoreCLR is open source][coreclr blog post] blog post.

The [.NET Core Libraries][corefx] repo contains the base class libraries, which provides data types and base functionality (ex: String, Collections, HttpClient) on top of CoreCLR. The two repos together make up .NET Core. The [.NET Core is Open Source][.NET Core oss] and [Introducing .NET Core][Introducing .NET Core] blog posts describes our .NET Core OSS strategy and road map in more detail.

Engage, Contribute and Provide Feedback
---------------------------------------

Some of the best ways to contribute are to try things out, file bugs, and join in design conversations. You are encouraged to start a discussion by filing an issue, or starting a thread in the [.NET Foundation forums](http://forums.dotnetfoundation.org/). If you are having issues with the Full .NET Framework or .NET Runtime the best ways to file a bug are at [Connect](http://connect.microsoft.com/VisualStudio) or through [Product Support](https://support.microsoft.com/en-us/contactus?ws=support) if you have a contract.

Looking for something to work on? The list of [up-for-grabs issues](https://github.com/dotnet/coreclr/issues?q=is%3Aopen+is%3Aissue+label%3Aup-for-grabs) is a great place to start.

Please read the following documents to get started.

* [Contributing Guide](Documentation/project-docs/contributing.md)
* [Developer Guide](Documentation/project-docs/developer-guide.md)

License
-------

.NET Core (including the coreclr repo) is licensed under the [MIT license](LICENSE.TXT).

.NET Foundation
---------------

.NET Core is a [.NET Foundation](http://www.dotnetfoundation.org/projects) project.

Related Projects
----------------

There are many .NET projects on GitHub.

- The
[.NET home repo](https://github.com/Microsoft/dotnet) links to 100s of .NET projects, from Microsoft and the community.
- The [.NET Core repo](https://github.com/dotnet/core) links to .NET Core related projects from Microsoft.
- The [ASP.NET home repo](https://github.com/aspnet/home) is the best place to start learning about ASP.NET 5.
- [dotnet.github.io](http://dotnet.github.io) is a good place to discover .NET Foundation projects.

[.NET Core oss]: http://blogs.msdn.com/b/dotnet/archive/2014/11/12/net-core-is-open-source.aspx
[Introducing .NET Core]: http://blogs.msdn.com/b/dotnet/archive/2014/12/04/introducing-net-core.aspx
[coreclr blog post]: http://blogs.msdn.com/b/dotnet/archive/2015/02/03/coreclr-is-now-open-source.aspx
[corefx]: http://github.com/dotnet/corefx
[coreclr]: http://github.com/dotnet/coreclr
