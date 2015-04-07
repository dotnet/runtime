# .NET Core Libraries (CoreFX)

|   |Linux|Windows|Mac OSX|
|:-:|:-:|:-:|:-:|
|**Debug**|[![Build status](http://dotnet-ci.cloudapp.net/job/dotnet_corefx_linux_debug/badge/icon)](http://dotnet-ci.cloudapp.net/job/dotnet_corefx_linux_debug/)|[![Build status](http://dotnet-ci.cloudapp.net/job/dotnet_corefx_windows_debug/badge/icon)](http://dotnet-ci.cloudapp.net/job/dotnet_corefx_windows_debug/)|[![Build status](http://dotnet-ci.cloudapp.net/job/dotnet_corefx_mac_debug/badge/icon)](http://dotnet-ci.cloudapp.net/job/dotnet_corefx_mac_debug/)|
|**Release**|[![Build status](http://dotnet-ci.cloudapp.net/job/dotnet_corefx_linux_release/badge/icon)](http://dotnet-ci.cloudapp.net/job/dotnet_corefx_linux_release/)|[![Build status](http://dotnet-ci.cloudapp.net/job/dotnet_corefx_windows_release/badge/icon)](http://dotnet-ci.cloudapp.net/job/dotnet_corefx_windows_release/)|[![Build status](http://dotnet-ci.cloudapp.net/job/dotnet_corefx_mac_release/badge/icon)](http://dotnet-ci.cloudapp.net/job/dotnet_corefx_mac_release/)|
|**Coverage Report**||[![Coverage Status](https://coveralls.io/repos/dotnet/corefx/badge.svg?branch=master)](http://dotnet-ci.cloudapp.net/job/dotnet_corefx_coverage_windows/lastStableBuild/Code_Coverage_Report/)||

The corefx repo contains the library implementation (called "CoreFX") for [.NET Core](http://github.com/dotnet/core). It includes System.Collections, System.IO, System.Xml and many other components. It builds and runs on Windows. You can ['watch'](https://github.com/dotnet/corefx/subscription) the repo to see Linux and Mac support being added over the next few months.

.NET Core is a modular implementation of .NET that can be used as the base stack for a wide variety of scenarios, today scaling from console utilities to web apps in the cloud.  You can learn more about .NET Core and how and where you can use it in the [.NET Core is open source][.NET Core oss] and [Introducing .NET Core][Introducing .NET Core] blog posts. 

The [.NET Core Runtime repo](https://github.com/dotnet/coreclr) contains the  runtime implementation (called "CoreCLR") for .NET Core. It includes RyuJIT, the .NET GC, native interop and many other components. 

Runtime-specific library code - namely [mscorlib][mscorlib] - lives in the CoreCLR repo. It needs to be built and versioned in tandem with the runtime. The rest of CoreFX is agnostic of runtime-implementation and can be run on any compatible .NET runtime. These characteristics were the primary motivation for the 2-repo structure.

[.NET Core oss]: http://blogs.msdn.com/b/dotnet/archive/2014/11/12/net-core-is-open-source.aspx
[Introducing .NET Core]: http://blogs.msdn.com/b/dotnet/archive/2014/12/04/introducing-net-core.aspx
[mscorlib]: https://github.com/dotnet/coreclr/tree/master/src/mscorlib

## How to Engage, Contribute and Provide Feedback

Some of the best ways to contribute are to try things out, file bugs, and join in design conversations. 

Want to get more familiar with what's going on in the code?
* [Pull requests](https://github.com/dotnet/corefx/pulls): [Open](https://github.com/dotnet/corefx/pulls?q=is%3Aopen+is%3Apr)/[Closed](https://github.com/dotnet/corefx/pulls?q=is%3Apr+is%3Aclosed)
* [![Backlog](https://cloud.githubusercontent.com/assets/1302850/6260412/38987b1e-b793-11e4-9ade-d3fef4c6bf48.png)](https://github.com/dotnet/corefx/issues?q=is%3Aopen+is%3Aissue+label%3A%220+-+Backlog%22), [![Up Next](https://cloud.githubusercontent.com/assets/1302850/6260418/4c2c7a54-b793-11e4-8ce1-a27ff5378d08.png)](https://github.com/dotnet/corefx/issues?q=is%3Aopen+is%3Aissue+label%3A%221+-+Up+Next%22) and [![In Progress](https://cloud.githubusercontent.com/assets/1302850/6260414/41b0fc30-b793-11e4-9d50-d09563cd138a.png)](https://github.com/dotnet/corefx/issues?q=is%3Aopen+is%3Aissue+label%3A%222+-+In+Progress%22) changes

Looking for something to work on? The list of [up-for-grabs issues](https://github.com/dotnet/corefx/labels/up%20for%20grabs) is a great place to start or for larger items see the list of [feature aproved](https://github.com/dotnet/corefx/labels/feature%20approved). See some of our guides for more details:

* [Contributing Guide](https://github.com/dotnet/corefx/wiki/Contributing)
* [Developer Guide](https://github.com/dotnet/corefx/wiki/Developer-Guide)
* [Issue Guide](https://github.com/dotnet/corefx/wiki/Issue-Guide)

You are also encouraged to start a discussion by filing an issue or creating a
gist.

You can discuss .NET OSS more generally in the [.NET Foundation forums].

Want to chat with other members of the CoreFX community?

[![Join the chat at https://gitter.im/dotnet/corefx](https://badges.gitter.im/Join%20Chat.svg)](https://gitter.im/dotnet/corefx?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)

[.NET Foundation forums]: http://forums.dotnetfoundation.org/

## .NET Core Library Components

The repo contains the following components. More libraries are coming soon. ['Watch'](https://github.com/dotnet/corefx/subscription) the repo to be notified.

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

* **System.ComponentModel**. Provides interfaces for the editing and change tracking of objects used as data sources.

* **System.ComponentModel.Annotations**. Provides attributes that are used to define metadata for objects used as data sources.

* **System.ComponentModel.EventBasedAsync**. Provides support classes and delegates for the event-based asynchronous pattern. 
  This pattern and these supporting types exist in .NET Core primarily for backwards compatibility and generally should be
  avoided when writing new code.
  
* **System.ComponentModel.Primitives**. Provides interfaces that are used to implement the run-time and design-time behavior 
  of components.

* **System.ComponentModel.TypeConverter**. Provides the System.ComponentModel.TypeConverter class, which represents a unified 
  way of converting types of values to other types.

* **System.Console**. Provides the Console class, which enables access to the standard input,
  output, and error streams for console-based applications.

* **System.Diagnostics.Debug**. Provides a class to interact with the debugger as well as methods for performing runtime assertions.

* **System.Diagnostics.FileVersionInfo**. Provides useful functionality for querying
  and examining the version information of physical files on disk.

* **System.Diagnostics.Process**. Provides access to local and remote processes, and enables the starting and
  stopping of local system processes.

* **System.Diagnostics.TextWriterTraceListener**. Provides trace listeners for directing tracing output to a text writer, 
  such as System.IO.StreamWriter.

* **System.Diagnostics.TraceSource**. Provides classes that help you trace the execution of your code.

* **System.Globalization.Extensions**. Provides classes for performing unicode string normalization, culture-specific string 
  comparisons and support the use of non-ASCII characters for Internet domain names.

* **System.IO.FileSystem**. Provides access to the file system, including support for enumerating and manipulating 
  file system objects and for reading and writing files via streams.

* **System.IO.FileSystem.DriveInfo**. Provides the System.IO.DriveInfo class, which enables developers to query 
  local drive information.

* **System.IO.FileSystem.Primitives**. Provides common enumerations and exceptions for path-based I/O libraries.

* **System.IO.FileSystem.Watcher**. Provides the System.IO.Watcher class, which listens to the system directory change 
  notifications and raises events when a directory or file within a directory changes.

* **System.IO.MemoryMappedFiles**. Provides access to memory-mapped files, enabling code to read and write files by
  reading and writing memory.

* **System.IO.Pipes**. Provides types that enable a means for interprocess communication through anonymous 
  and/or named pipes.

* **System.IO.UnmanagedMemoryStream**. Provides a stream for accessing unmanaged memory as represented by a pointer, 
  as well as an accessor for reading and writing primitive types from unmanaged memory.

* **System.Linq**. Provides the foundation of Language-Integrated Query (LINQ), including LINQ standard query operators 
  that operate on objects that implement ```IEnumerable<T>```.

* **System.Linq.Expressions**. Provides classes, interfaces, and enumerations that enable language-level code expressions
  to be represented as objects in the form of expression trees.

* **System.Linq.Parallel**.  Provides a parallelized implementation of LINQ to Objects. "Parallel LINQ" (PLINQ) 
  implements the full set of LINQ standard query operators as well as additional operators specific to parallel operations.

* **System.Linq.Queryable**. Provides LINQ standard query operators that operate on objects that implement ```IQueryable<T>```.

* **System.Numerics.Vectors**. Provides a set of basic vector types that leverage single instruction, 
  multiple data (SIMD) CPU instructions. See our [recent][simd-post-1] [announcements][simd-post-2] for more details.

* **System.ObjectModel**. Provides types and interfaces that enable the creation of observable types that provide
  notifications to clients when changes are made.

* **System.Reflection.Metadata**. Provides a highly-tuned, low-level ECMA-335 metadata reader.  This is the same
  reader used by "[Roslyn]" C# and Visual Basic compilers to parse assemblies.

* **System.Resources.ResourceWriter**. Provides the System.Resources.ResourceWriter class, which writes resources in the 
  system-default format to an output stream.

* **System.Runtime**. Provides a set of unit tests for basic run-time types such as String and Int32.

* **System.Runtime.Extensions**. Provides a set of unit tests for extensions to the basic runtime functionality
  such as System.Convert and System.IO.Path.
 
* **System.Runtime.Numerics**. Provides two useful numeric structures, BigInteger and Complex.

* **System.Runtime.Serialization.Json**. Provides classes for serializing objects to the JavaScript Object 
  Notation (JSON) and for deserializing JSON data to objects.

* **System.Runtime.Serialization.Primitives**. Provides common types, including System.Runtime.Serialization.DataContractAttribute, 
  for libraries that support data contract serialization.

* **System.Runtime.Serialization.Xml**. Provides classes for serializing objects to the Extensible Markup Language (XML) 
  and deserializing XML data to objects.

* **System.ServiceProcess.ServiceController**. Provides the ServiceController class that represents a Windows service
  and allows you to connect to a running or stopped service, manipulate it, or get information about it.

* **System.Text.Encoding.CodePages**. Provides the ability to access existing encoding types for string manipulation 
  across common cultural standards, as well as support to create custom Encoding Providers.

* **System.Text.RegularExpressions**. Provides a regular expression engine. The types in this library provide useful 
  functionality for running common operations using regular expressions.

* **System.Threading.Tasks.Dataflow**.  Provides a set of types that support actor/agent-oriented designs through 
  primitives for in-process message passing, dataflow, and pipelining.
    
* **System.Threading.Tasks.Parallel**.  Provides library-based data parallel replacements for common
  operations such as for loops, for each loops, and execution of a set of statements.

* **System.Xml.ReaderWriter**. Provides types for reading and writing streams of XML.

* **System.Xml.XDocument**. Provides XML-related types for querying XML documents using LINQ.

* **System.Xml.XPath**.  Provides classes that define a cursor model for navigating and editing XML information items 
  as instances of the XQuery 1.0 and XPath 2.0 Data Model.

* **System.Xml.XPath.XDocument**. Provides extension methods that add System.Xml.XPath support to the System.Xml.XDocument package.

* **System.Xml.XPath.XmlDocument**. Provides extension methods that add System.Xml.XPath support to the System.Xml.XmlDocument package.

* **System.Xml.XmlDocument**.  Provides types for manipulating an XML Document Object Model (DOM).

* **System.Xml.XmlSerializer**. Provides classes for serializing objects to XML and for deserializing XML data to objects.

* **Microsoft.CSharp**. Provides support for compilation and code generation, including dynamic, using the C# language.

* **Microsoft.Win32.Primitives**. Provides common types supporting the implementation of Win32-based libraries.

* **Microsoft.Win32.Registry**. Provides support for accessing and modifying the Windows Registry.

* **System.Security.SecureString**. Provides support for accessing and modifying text that should be kept confidential.

* The overall list of items we currently plan to move onto GitHub is [here][typelist].

[roslyn]: https://roslyn.codeplex.com/
[immutable-msdn]: http://msdn.microsoft.com/en-us/library/dn385366(v=vs.110).aspx
[simd-post-1]: http://blogs.msdn.com/b/dotnet/archive/2014/04/07/the-jit-finally-proposed-jit-and-simd-are-getting-married.aspx
[simd-post-2]: http://blogs.msdn.com/b/dotnet/archive/2014/05/13/update-to-simd-support.aspx
[typelist]: http://blogs.msdn.com/cfs-file.ashx/__key/communityserver-components-postattachments/00-10-58-94-19/NetCore_5F00_OpenSourceUpdate.xlsx

## License

.NET Core (including the corefx repo) is licensed under the [MIT license](LICENSE).

## .NET Foundation

.NET Core is a [.NET Foundation](http://www.dotnetfoundation.org/projects) project.

## Related Projects
There are many .NET related projects on GitHub.

- The
[.NET home repo](https://github.com/Microsoft/dotnet) links to 100s of .NET projects, from Microsoft and the community.
- The [.NET Core repo](https://github.com/dotnet/core) links to .NET Core related projects from Microsoft.
- The [ASP.NET home repo](https://github.com/aspnet/home) is the best place to start learning about ASP.NET 5.
- [dotnet.github.io](http://dotnet.github.io) is a good place to discover .NET Foundation projects.
