.NET Core Runtime (CoreCLR)
===========================

The CoreCLR repo contains the complete runtime implementation for [.NET Core](http://github.com/dotnet/core). It includes RyuJIT, the .NET GC, native interop and many other components. It is cross-platform, with multiple OS and CPU ports in progress.

Note that the library implementation CoreFX (System.Collections, System.IO, System.Xml and so on) lives in another repo [dotnet/corefx](https://github.com/dotnet/corefx).

Build Status
------------

<table>
  <tr>
    <th width="9%" />
    <th width="13%">Debian 8.2</th> 
    <th width="13%">Ubuntu 14.04</th>
    <th width="13%">Centos 7.1</th>
    <th width="13%">OpenSuSE 13.2</th>
    <th width="13%">Windows</th>
    <th width="13%">Mac OS X</th>
    <th width="13%">FreeBSD</th>
  </tr>
  <tr>
    <td><b>Debug</b></td>
    <td>
      <a href="http://dotnet-ci.cloudapp.net/job/dotnet_coreclr/job/debug_debian8.2">
        <img src="http://dotnet-ci.cloudapp.net/job/dotnet_coreclr/job/debug_debian8.2/badge/icon" alt="Build status" />
      </a>
    </td>
    <td>
      <a href="http://dotnet-ci.cloudapp.net/job/dotnet_coreclr/job/debug_ubuntu">
        <img src="http://dotnet-ci.cloudapp.net/job/dotnet_coreclr/job/debug_ubuntu/badge/icon" alt="Build status" />
      </a>
    </td>
    <td>
      <a href="http://dotnet-ci.cloudapp.net/job/dotnet_coreclr/job/debug_centos7.1">
        <img src="http://dotnet-ci.cloudapp.net/job/dotnet_coreclr/job/debug_centos7.1/badge/icon" alt="Build status" />
      </a>
    </td>
    <td>
      <a href="http://dotnet-ci.cloudapp.net/job/dotnet_coreclr/job/debug_opensuse13.2">
        <img src="http://dotnet-ci.cloudapp.net/job/dotnet_coreclr/job/debug_opensuse13.2/badge/icon" alt="Build status" />
      </a>
    </td>
    <td>
      <a href="http://dotnet-ci.cloudapp.net/job/dotnet_coreclr/job/debug_windows_nt">
        <img src="http://dotnet-ci.cloudapp.net/job/dotnet_coreclr/job/debug_windows_nt/badge/icon" alt="Build status" />
      </a>
      <br />
      <a href="http://dotnet-ci.cloudapp.net/job/dotnet_coreclr/job/arm64_cross_debug_windows_nt">
        <img src="https://img.shields.io/jenkins/s/http/dotnet-ci.cloudapp.net/job/dotnet_coreclr/arm64_cross_debug_windows_nt.svg?label=arm64" alt="Arm64 status" />
      </a>
    </td>
    <td>
      <a href="http://dotnet-ci.cloudapp.net/job/dotnet_coreclr/job/debug_osx">
        <img src="http://dotnet-ci.cloudapp.net/job/dotnet_coreclr/job/debug_osx/badge/icon" alt="Build status" />
      </a>
    </td>
    <td>
      <a href="http://dotnet-ci.cloudapp.net/job/dotnet_coreclr/job/debug_freebsd">
        <img src="http://dotnet-ci.cloudapp.net/job/dotnet_coreclr/job/debug_freebsd/badge/icon" alt="Build status" />
      </a>
    </td>
  </tr>
  <tr>
    <td><b>Release</b></td>
    <td>
      <a href="http://dotnet-ci.cloudapp.net/job/dotnet_coreclr/job/release_debian8.2">
        <img src="http://dotnet-ci.cloudapp.net/job/dotnet_coreclr/job/release_debian8.2/badge/icon" alt="Build status" />
      </a>
    </td>
    <td>
      <a href="http://dotnet-ci.cloudapp.net/job/dotnet_coreclr/job/release_ubuntu">
        <img src="http://dotnet-ci.cloudapp.net/job/dotnet_coreclr/job/release_ubuntu/badge/icon" alt="Build status" />
      </a>
    </td>
    <td>
      <a href="http://dotnet-ci.cloudapp.net/job/dotnet_coreclr/job/release_centos7.1">
        <img src="http://dotnet-ci.cloudapp.net/job/dotnet_coreclr/job/release_centos7.1/badge/icon" alt="Build status" />
      </a>
    </td>
    <td>
      <a href="http://dotnet-ci.cloudapp.net/job/dotnet_coreclr/job/release_opensuse13.2">
        <img src="http://dotnet-ci.cloudapp.net/job/dotnet_coreclr/job/release_opensuse13.2/badge/icon" alt="Build status" />
      </a>
    </td>
    <td>
      <a href="http://dotnet-ci.cloudapp.net/job/dotnet_coreclr/job/release_windows_nt">
        <img src="http://dotnet-ci.cloudapp.net/job/dotnet_coreclr/job/release_windows_nt/badge/icon" alt="Build status" />
      </a>
      <br />
      <a href="http://dotnet-ci.cloudapp.net/job/dotnet_coreclr/job/arm64_cross_release_windows_nt">
        <img src="https://img.shields.io/jenkins/s/http/dotnet-ci.cloudapp.net/job/dotnet_coreclr/arm64_cross_release_windows_nt.svg?label=arm64" alt="Arm64 status" />
      </a>
    </td>
    <td>
      <a href="http://dotnet-ci.cloudapp.net/job/dotnet_coreclr/job/release_osx">
        <img src="http://dotnet-ci.cloudapp.net/job/dotnet_coreclr/job/release_osx/badge/icon" alt="Build status" />
      </a>
    </td>
    <td>
      <a href="http://dotnet-ci.cloudapp.net/job/dotnet_coreclr/job/release_freebsd">
        <img src="http://dotnet-ci.cloudapp.net/job/dotnet_coreclr/job/release_freebsd/badge/icon" alt="Build status" />
      </a>
    </td>
   </tr>
</table>

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
