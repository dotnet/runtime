# .NET Core Runtime (CoreCLR)

The coreclr repo contains the complete runtime implementation (called "CoreCLR") for [.NET Core](http://github.com/dotnet/core). It includes RyuJIT, the .NET GC, native interop and many other components. It builds and runs on Windows. You can 'watch' the repo to see Linux and Mac support being added over the next few months.

.NET Core is part of ASP.NET 5 and is a subset of the .NET Framework. You can learn more about .NET Core and how and where you can use it in the [CoreCLR is open source][coreclr blog post] blog post. 

The [.NET Core Libraries][corefx] repo contains the base class libraries, which provides data types and base functionality (ex: String, Collections, HttpClient) on top of CoreCLR. The two repos together make up .NET Core. The [.NET Core is Open Source][.NET Core oss] and [Introducing .NET Core][Introducing .NET Core] blog posts describes our .NET Core OSS strategy and road map in more detail.

## How to Engage, Contribute and Provide Feedback

Some of the best ways to contribute are to try things out, file bugs, and join in design conversations. 

Want to get more familiar with what's going on in the code?
* [Pull requests](https://github.com/dotnet/coreclr/pulls): [Open](https://github.com/dotnet/coreclr/pulls?q=is%3Aopen+is%3Apr)/[Closed](https://github.com/dotnet/coreclr/pulls?q=is%3Apr+is%3Aclosed)

Looking for something to work on? The list of [up-for-grabs issues](https://github.com/dotnet/coreclr/issues?q=is%3Aopen+is%3Aissue+label%3Aup-for-grabs) is a great place to start.

* [Contributing Guide][Contributing Guide]
* [Developer Guide]

You are encouraged to start a discussion by filing an issue, creating a
gist or starting a thread in the [.NET Foundation forums]. For broader topics, please use the forums.

[Contributing Guide]: https://github.com/dotnet/coreclr/wiki/Contributing
[Developer Guide]: https://github.com/dotnet/coreclr/wiki/Developer-Guide

[.NET Foundation forums]: http://forums.dotnetfoundation.org/

## CoreCLR Build Artifacts
The build for this repo will produce the following artifacts:

1. coreclr.dll and required native binaries
2. mscorlib.dll
3. Tests that are required for validating any changes to the product

## Code Flow
CoreCLR is a subset of the .NET Framework CLR. They share the same codebase and are updated together. For example, an update to the .NET GC improves both CoreCLR and the .NET Framework CLR.

We setup a live 2-way mirror between the coreclr repo on GitHub and the .NET Framework TFS server within Microsoft. The latency of the mirror is low, measurable in minutes.

Contributions made to the coreclr repo are integrated to the Microsoft TFS server automatically and will become part of both the .NET Framework and .NET Core products. The same is true in reverse, that .NET Framework CLR changes (within the CoreCLR subset) are mirrored to the CoreCLR repo. These changes will sometimes result in large commits to unrelated components.

## License

.NET Core (including the coreclr repo) is licensed under the [MIT license](LICENSE).

## .NET Foundation

.NET Core is a [.NET Foundation](http://www.dotnetfoundation.org/projects) project.

## Related Projects
There are many .NET projects on GitHub.

- The
[.NET home repo](https://github.com/Microsoft/dotnet) links to 100s of .NET projects, from Microsoft and the community.
- The [.NET Core repo](https://github.com/dotnet/core) links to .NET Core related projects from Microsoft.
- The [ASP.NET home repo](https://github.com/aspnet/home) is the best place to start learning about ASP.NET 5.
- [dotnet.github.io](http://dotnet.github.io) is a good place to discover .NET Foundation projects.

[.NET Core oss]: http://blogs.msdn.com/b/dotnet/archive/2014/11/12/net-core-is-open-source.aspx
[Introducing .NET Core]: http://blogs.msdn.com/b/dotnet/archive/2014/12/04/introducing-net-core.aspx
[coreclr blog post]: http://blogs.msdn.com/b/dotnet/archive/2015/02/03/coreclr-is-open-source.aspx
[corefx]: http://github.com/dotnet/corefx
[coreclr]: http://github.com/dotnet/coreclr