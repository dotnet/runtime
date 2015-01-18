# .NET Core

[![Build status][build-status-image]][build-status]  [![Issue Stats][pull-requests-image]][pull-requests]  [![Issue Stats][issues-closed-image]][issues-closed]

[build-status-image]: http://corefx-ci.cloudapp.net/jenkins/job/dotnet_corefx/badge/icon
[build-status]: http://corefx-ci.cloudapp.net/jenkins/job/dotnet_corefx/
[pull-requests-image]: http://www.issuestats.com/github/dotnet/corefx/badge/pr
[pull-requests]: http://www.issuestats.com/github/dotnet/corefx
[issues-closed-image]: http://www.issuestats.com/github/dotnet/corefx/badge/issue
[issues-closed]: http://www.issuestats.com/github/dotnet/corefx

This repository contains the class libraries for .NET Core. This is a
work in progress, and does not currently contain the entire set of libraries
that we plan on open sourcing. Make sure to watch this repository in order to be
notified as we make changes to and expand it. Check out this [blog post] that
explains our OSS strategy and road map in more detail.

Today, the repository contains the following components:

* **Microsoft.Win32.Primitives**. Provides common types supporting the implementation of Win32-based libraries.

* **Microsoft.Win32.Registry**. Provides support for accessing and modifying the Windows Registry.

* **System.Collections.Concurrent**.  Provides a set of thread-safe collection types, instances of which may be used 
  concurrently from multiple threads.

* **System.Collections.Immutable**. Provides a set of immutable collection types that make it easy to keep
  mutable state under control without sacrificing performance or memory
  footprint. You can read more about them on [MSDN][immutable-msdn].

* **System.Collections.NonGeneric**.  Provides classes that define various collections of objects, such as ArrayList, 
  Hashtable, SortedList, Stack, and Queue. These collections exist in .NET Core primarily for backwards compatibility
  and generally should be avoided when writing new code.

* **System.Collections.Specialized**. Provides classes that define specialized collections of objects, for example, 
  a linked list dictionary, a bit vector, and collections that contain only strings. These collections exist in 
  .NET Core primarily for backwards compatibility and generally should be avoided when writing new code.

* **System.ComponentModel**. Provides types used to implement the run-time and design-time behavior of components 
  and controls.

* **System.Console**. Provides the Console class, which enables access to the standard input, 
  output, and error streams for console-based applications.

* **System.Diagnostics.FileVersionInfo**. Provides useful functionality for querying
  and examining the version information of physical files on disk.

* **System.Diagnostics.Process**. Provides access to local and remote processes, and enables the starting and
  stopping of local system processes.

* **System.IO.FileSystem**. Provides access to the file system, including support for enumerating and manipulating 
  file system objects and for reading and writing files via streams.

* **System.IO.MemoryMappedFiles**. Provides access to memory-mapped files, enabling code to read and write files by
  reading and writing memory.

* **System.IO.Pipes**. Provides types that enable a means for interprocess communication through anonymous 
  and/or named pipes.

* **System.IO.UnmanagedMemoryStream**. Provides a stream for accessing unmanaged memory as represented by a pointer, 
  as well as an accessor for reading and writing primitive types from unmanaged memory.

* **System.Linq.Parallel**.  Provides a parallelized implementation of LINQ to Objects. "Parallel LINQ" (PLINQ) 
  implements the full set of LINQ standard query operators as well as additional operators specific to parallel operations.

* **System.Numerics.Vectors**. Provides a set of basic vector types that leverage single instruction, 
  multiple data (SIMD) CPU instructions. See our [recent][simd-post-1] [announcements][simd-post-2] for more details.

* **System.Reflection.Metadata**. Provides a highly-tuned, low-level ECMA-335 metadata reader.  This is the same
  reader used by "[Roslyn]" C# and Visual Basic compilers to parse assemblies.

* **System.Text.Encoding.CodePages**. Provides the ability to access existing encoding types for string manipulation 
  across common cultural standards, as well as support to create custom Encoding Providers.

* **System.Text.RegularExpressions**. Provides a regular expression engine. The types in this library provide useful 
  functionality for running common operations using regular expressions.

* **System.Threading.Tasks.Dataflow**.  Provides a set of types that support actor/agent-oriented designs through 
  primitives for in-process message passing, dataflow, and pipelining. "TPL Dataflow" builds 
  upon the APIs and scheduling infrastructure provided by the Task Parallel Library
  (TPL), and integrates with the language support for asynchrony provided by C#, Visual Basic, and F#.

* **System.Xml**. Provides DOM APIs such as the `XDocument` and `XmlDocument`
  types, XLinq, and the corresponding XPath extension methods.


More libraries are coming soon. Stay tuned!

[blog post]: http://blogs.msdn.com/b/dotnet/archive/2014/11/12/net-core-is-open-source.aspx
[roslyn]: https://roslyn.codeplex.com/
[immutable-msdn]: http://msdn.microsoft.com/en-us/library/dn385366(v=vs.110).aspx
[simd-post-1]: http://blogs.msdn.com/b/dotnet/archive/2014/04/07/the-jit-finally-proposed-jit-and-simd-are-getting-married.aspx
[simd-post-2]: http://blogs.msdn.com/b/dotnet/archive/2014/05/13/update-to-simd-support.aspx

## How to Engage, Contribute and Provide Feedback

Some of the best ways to contribute are to try things out, file bugs, and join in design conversations. 

Want to get more familiar with what's going on in the code?
* [Pull requests](https://github.com/dotnet/corefx/pulls): [Open](https://github.com/dotnet/corefx/pulls?q=is%3Aopen+is%3Apr)/[Closed](https://github.com/dotnet/corefx/pulls?q=is%3Apr+is%3Aclosed)

Looking for something to work on? The list of [up-for-grabs issues](https://github.com/dotnet/corefx/issues?q=is%3Aopen+is%3Aissue+label%3Aup-for-grabs) is a great place to start.

* [How to Contribute][Contributing Guide]
    * [Contributing Guide][Contributing Guide]
    * [Developer Guide]

You are also encouraged to start a discussion by filing an issue or creating a
gist. See the [contributing guides][Contributing Guide] for more details. 

[Contributing Guide]: https://github.com/dotnet/corefx/wiki/Contributing
[Developer Guide]: https://github.com/dotnet/corefx/wiki/Developer-Guide

You can discuss .NET OSS more generally in the [.NET Foundation forums].

[.NET Foundation forums]: http://forums.dotnetfoundation.org/

## Related Projects

For an overview of all the .NET related projects, have a look at the
[.NET home repository](https://github.com/Microsoft/dotnet).

## License

This project is licensed under the [MIT license](LICENSE).

## .NET Foundation

This project is part of the [.NET Foundation](http://www.dotnetfoundation.org/projects).
