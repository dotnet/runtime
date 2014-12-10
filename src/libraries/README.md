# .NET Core

[![Build status][build-status-image]][build-status]  [![Issue Stats][pull-requests-image]][pull-requests]  [![Issue Stats][issues-closed-image]][issues-closed]

[build-status-image]: https://ci.appveyor.com/api/projects/status/xje8bkekyu130e9y/branch/master?svg=true
[build-status]: https://ci.appveyor.com/project/dotnet-bot/corefx/branch/master
[pull-requests-image]: http://www.issuestats.com/github/dotnet/corefx/badge/pr
[pull-requests]: http://www.issuestats.com/github/dotnet/corefx
[issues-closed-image]: http://www.issuestats.com/github/dotnet/corefx/badge/issue
[issues-closed]: http://www.issuestats.com/github/dotnet/corefx

This repository contains the class libraries for .NET Core. This is currently a
work in progress, and does not currently contain the entire set of libraries
that we plan on open sourcing. Make sure to watch this repository in order to be
notified as we make changes to and expand it. Check out this [blog post] that
explains our OSS strategy and road map in more detail.

Today, it contains the following components:

* **Immutable Collections**. A set of collection types that make it easy to keep
  mutable state under control without sacrificing performance or memory
  footprint. You can read more about them on [MSDN][immutable-msdn].

* **ECMA-335 Metadata Reader**. This is a highly tuned low-level metadata reader
  that allows [Roslyn] to parse assemblies.

* **SIMD enabled vector types**. We've recently added a set of basic vector
  types that leverage single instruction, multiple data (SIMD) CPU instructions.
  See our [recent][simd-post-1] [announcements][simd-post-2] for more details.

* **XML**. This includes the DOM APIs such as the `XDocument` and `XmlDocument`
  types, XLinq as well the corresponding XPath extension methods.

* **Parallel LINQ**.  Parallel LINQ (PLINQ) is a parallel implementation of LINQ
  to Objects. PLINQ implements the full set of LINQ standard query operators as 
  extension methods for the System.Linq namespace and has additional operators
  for parallel operations.

* **TPL Dataflow**.  TPL Dataflow promotes actor/agent-oriented designs through 
  primitives for in-process message passing, dataflow, and pipelining. TDF builds 
  upon the APIs and scheduling infrastructure provided by the Task Parallel Library
  (TPL), and integrates with the language support for asynchrony provided by 
  C#, Visual Basic, and F#.

* **Console**. This represents the standard input, output, and error streams for console applications.

More is coming soon. Stay tuned!

[blog post]: http://blogs.msdn.com/b/dotnet/archive/2014/11/12/net-core-is-open-source.aspx
[roslyn]: https://roslyn.codeplex.com/
[immutable-msdn]: http://msdn.microsoft.com/en-us/library/dn385366(v=vs.110).aspx
[simd-post-1]: http://blogs.msdn.com/b/dotnet/archive/2014/04/07/the-jit-finally-proposed-jit-and-simd-are-getting-married.aspx
[simd-post-2]: http://blogs.msdn.com/b/dotnet/archive/2014/05/13/update-to-simd-support.aspx

## Related Projects

For an overview of all the .NET related projects, have a look at the
[.NET home repository](https://github.com/Microsoft/dotnet).

## License

This project is licensed under the [MIT license](LICENSE).

## .NET Foundation

This project is a part of the [.NET Foundation].

[.NET Foundation]: http://www.dotnetfoundation.org/projects

## How to Engage, Contribute and Provide Feedback

To contribute to .NET Core, see the [Contributing Guide].

[Contributing Guide]: https://github.com/dotnet/corefx/wiki/Contributing

You are also encouraged to start a discussion by filing an issue or creating a
gist. See the contributing guides for more details. You can discuss .NET OSS
more generally in the [.NET Foundation forums].

[.NET Foundation forums]: http://forums.dotnetfoundation.org/

## Building and Testing

To find out how you can build and test .NET Core, see the [Developer Guide].

[Developer Guide]: https://github.com/dotnet/corefx/wiki/Developer-Guide
