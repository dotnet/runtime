# .NET Core Libraries (CoreFX)

[![Build Status](https://dev.azure.com/dnceng/internal/_apis/build/status/dotnet/corefx/corefx-official?branchName=master)](https://dev.azure.com/dnceng/internal/_build/latest?definitionId=283&branchName=master)

This repo contains the library implementation (called "CoreFX") for .NET Core. It includes System.Collections, System.IO, System.Xml, and many other components.
The corresponding [.NET Core Runtime repo](https://github.com/dotnet/coreclr) (called "CoreCLR") contains the runtime implementation for .NET Core. It includes RyuJIT, the .NET GC, and many other components.
Runtime-specific library code ([System.Private.CoreLib](https://github.com/dotnet/coreclr/tree/master/src/System.Private.CoreLib)) lives in the CoreCLR repo. It needs to be built and versioned in tandem with the runtime. The rest of CoreFX is agnostic of runtime-implementation and can be run on any compatible .NET runtime (e.g. [CoreRT](https://github.com/dotnet/corert)).



## .NET Core

Official Starting Page: https://dotnet.microsoft.com/

* [How to use .NET Core](https://docs.microsoft.com/dotnet/core/get-started) (with VS, VS Code, command-line CLI)
  * [Install official releases](https://dotnet.microsoft.com/download)
  * [Documentation](https://docs.microsoft.com/dotnet/core) (Get Started, Tutorials, Porting from .NET Framework, API reference, ...)
    * [Deploying apps](https://docs.microsoft.com/dotnet/core/deploying)
  * [Supported OS versions](https://github.com/dotnet/core/blob/master/os-lifecycle-policy.md)
* [Roadmap](https://github.com/dotnet/core/blob/master/roadmap.md)
* [Releases](https://github.com/dotnet/core/tree/master/release-notes)
* [Bringing more APIs to .NET Core](https://github.com/dotnet/corefx/blob/master/Documentation/project-docs/porting.md) (and why some APIs will be left out)



## How to Engage, Contribute and Provide Feedback

Some of the best ways to contribute are to try things out, file bugs, join in design conversations, and fix issues.

* [Dogfooding daily builds](https://github.com/dotnet/corefx/blob/master/Documentation/project-docs/dogfooding.md)
* If you have a question or idea, [file a new issue](https://github.com/dotnet/corefx/issues/new).

If you are having issues with the "full" .NET Framework (also called "Desktop"), the best way to file a bug is the [Report a Problem](https://aka.ms/vs-rap) tool, which is integrated with the [VS Developer Community Portal](https://developercommunity.visualstudio.com/); or through [Product Support](https://support.microsoft.com/en-us/contactus?ws=support) if you have a contract.

### Issue Guide

This section is **in progress** here: [New contributor Docs - Issues](https://github.com/dotnet/corefx/wiki/New-contributor-Docs#issue-guide) (feel free to make it better - it's easy-to-edit wiki with RW permissions to everyone!)

Each issue area has one or more Microsoft owners, who are [listed here](https://github.com/dotnet/corefx/blob/master/Documentation/project-docs/issue-guide.md).

### Contributing Guide

This section is **in progress** here: [New contributor Docs - Contributing](https://github.com/dotnet/corefx/wiki/New-contributor-Docs#contributing-guide) (feel free to make it better - it's easy-to-edit wiki with RW permissions to everyone!)

### Useful Links

* [.NET Core source index](https://source.dot.net) / [.NET Framework source index](https://referencesource.microsoft.com)
* [API Reference docs](https://docs.microsoft.com/dotnet/api/?view=netcore-3.0)
* [.NET API Catalog](http://apisof.net) (incl. APIs from daily builds and API usage info)
* [API docs writing guidelines](https://github.com/dotnet/dotnet-api-docs/wiki) - useful when writing /// comments

### Community

* General .NET OSS discussions: [.NET Foundation forums](https://forums.dotnetfoundation.org)
* Chat with other community members [![Join the chat at https://gitter.im/dotnet/corefx](https://badges.gitter.im/Join%20Chat.svg)](https://gitter.im/dotnet/corefx?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)

This project has adopted the code of conduct defined by the [Contributor Covenant](https://contributor-covenant.org/)
to clarify expected behavior in our community. For more information, see the [.NET Foundation Code of Conduct](https://www.dotnetfoundation.org/code-of-conduct).

### Reporting security issues and security bugs

Security issues and bugs should be reported privately, via email, to the Microsoft Security Response Center (MSRC) <secure@microsoft.com>. You should receive a response within 24 hours. If for some reason you do not, please follow up via email to ensure we received your original message. Further information, including the MSRC PGP key, can be found in the [Security TechCenter](https://www.microsoft.com/msrc/faqs-report-an-issue).

Also see info about related [Microsoft .NET Core and ASP.NET Core Bug Bounty Program](https://www.microsoft.com/msrc/bounty-dot-net-core).

## License

.NET Core (including the corefx repo) is licensed under the [MIT license](LICENSE.TXT).



## .NET Foundation

.NET Core is a [.NET Foundation](https://www.dotnetfoundation.org/projects) project.

There are many .NET related projects on GitHub.

- [.NET home repo](https://github.com/Microsoft/dotnet) - links to 100s of .NET projects, from Microsoft and the community.
- [ASP.NET Core home](https://github.com/aspnet/home) - the best place to start learning about ASP.NET Core.



## CoreFX Project

### Daily Builds

Daily builds of .NET Core components are published to dotnet-blob feed (https://dotnetfeed.blob.core.windows.net/dotnet-core/index.json).
The latest version number of each library can be seen in that feed.
Currently, there is no website to visualize the contents of the feed, so in order to do so, you have to use a NuGet feed explorer, like Visual Studio.

Note: See officially supported [OS versions](https://github.com/dotnet/core/blob/master/os-lifecycle-policy.md).
