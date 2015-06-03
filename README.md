.NET Core Runtime (CoreCLR)
===========================

The CoreCLR repo contains the complete runtime implementation for [.NET Core](http://github.com/dotnet/core). It includes RyuJIT, the .NET GC, native interop and many other components. It is cross-platform, with multiple OS and CPU ports in progress.

Build Status
------------

|         |Linux   |Windows |Mac OS X |FreeBSD  |
|---------|:------:|:------:|:-------:|:-------:|
|**Debug**|[![Build status](http://dotnet-ci.cloudapp.net/job/dotnet_coreclr_linux_debug/badge/icon)](http://dotnet-ci.cloudapp.net/job/dotnet_coreclr_linux_debug/)|[![Build status](http://dotnet-ci.cloudapp.net/job/dotnet_coreclr_windows_debug/badge/icon)](http://dotnet-ci.cloudapp.net/job/dotnet_coreclr_windows_debug/)|[![Build Status](http://dotnet-ci.cloudapp.net/job/dotnet_coreclr_mac_debug/badge/icon)](http://dotnet-ci.cloudapp.net/job/dotnet_coreclr_mac_debug/) |[![Build Status](http://dotnet-ci.cloudapp.net/job/dotnet_coreclr_freebsd_debug/badge/icon)](http://dotnet-ci.cloudapp.net/job/dotnet_coreclr_freebsd_debug/) |
|**Release**|[![Build status](http://dotnet-ci.cloudapp.net/job/dotnet_coreclr_linux_release/badge/icon)](http://dotnet-ci.cloudapp.net/job/dotnet_coreclr_linux_release/)|[![Build status](http://dotnet-ci.cloudapp.net/job/dotnet_coreclr_windows_release/badge/icon)](http://dotnet-ci.cloudapp.net/job/dotnet_coreclr_windows_release/)|[![Build status](http://dotnet-ci.cloudapp.net/job/dotnet_coreclr_mac_release/badge/icon)](http://dotnet-ci.cloudapp.net/job/dotnet_coreclr_mac_release/) |[![Build status](http://dotnet-ci.cloudapp.net/job/dotnet_coreclr_freebsd_release/badge/icon)](http://dotnet-ci.cloudapp.net/job/dotnet_coreclr_freebsd_release/) |

Get .NET Core
-------------

|                     |Linux   |Windows |Mac OS X |FreeBSD  |
|---------------------|--------|--------|---------|---------|
|Build from **Source**| [Instructions](Documentation/linux-instructions.md) | [Instructions](Documentation/windows-instructions.md) | [Instructions](Documentation/osx-instructions.md) | [Instructions](Documentation/freebsd-instructions.md) |
|Get **Binaries**     | [DNX SDK](Documentation/get-dotnetcore-dnx-linux.md)|[DNX SDK](Documentation/get-dotnetcore-dnx-windows.md) <br> [Raw](Documentation/get-dotnetcore-windows.md)|[DNX SDK](Documentation/get-dotnetcore-dnx-osx.md)||

Chat Room
---------

Want to chat with other members of the CoreCLR community?

[![Join the chat at https://gitter.im/dotnet/coreclr](https://badges.gitter.im/Join%20Chat.svg)](https://gitter.im/dotnet/coreclr?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)

Learn about CoreCLR and .NET Core
---------------------------------

The best ways to learn about CoreCLR are to try out the product instructions and to read the "Book of the Runtime" architecture documents that describe the inner workings of the product. New devs to the CLR team are encouraged to read these documents before making substative changes to the product. They are equally useful for open source contributors.

- [Product instructions](Documentation/README.md)
- [Introduction to the Common Language Runtime](Documentation/intro-to-clr.md)
- [Book of the Runtime](Documentation/README.md#book-of-the-runtime)
- [CoreCLR Documents](Documentation)

.NET Core is part of ASP.NET 5 and is a subset of the .NET Framework. You can learn more about .NET Core and how and where you can use it in the [CoreCLR is open source][coreclr blog post] blog post. 

The [.NET Core Libraries][corefx] repo contains the base class libraries, which provides data types and base functionality (ex: String, Collections, HttpClient) on top of CoreCLR. The two repos together make up .NET Core. The [.NET Core is Open Source][.NET Core oss] and [Introducing .NET Core][Introducing .NET Core] blog posts describes our .NET Core OSS strategy and road map in more detail.

Engage, Contribute and Provide Feedback
---------------------------------------

Some of the best ways to contribute are to try things out, file bugs, and join in design conversations. You are encouraged to start a discussion by filing an issue, or starting a thread in the [.NET Foundation forums].

Looking for something to work on? The list of [up-for-grabs issues](https://github.com/dotnet/coreclr/issues?q=is%3Aopen+is%3Aissue+label%3Aup-for-grabs) is a great place to start.

Please read the following documents to get started.

* [Contributing Guide](Documentation/contributing.md)
* [Developer Guide](Documentation/developer-guide.md)

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
