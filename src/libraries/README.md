# .NET Core Libraries (CoreFX)

|   |Linux|Windows|Mac OSX|
|:-:|:-:|:-:|:-:|
|**Debug**|[![Build status](http://dotnet-ci.cloudapp.net/job/dotnet_corefx_linux_debug/badge/icon)](http://dotnet-ci.cloudapp.net/job/dotnet_corefx_linux_debug/)|[![Build status](http://dotnet-ci.cloudapp.net/job/dotnet_corefx_windows_debug/badge/icon)](http://dotnet-ci.cloudapp.net/job/dotnet_corefx_windows_debug/)|[![Build status](http://dotnet-ci.cloudapp.net/job/dotnet_corefx_mac_debug/badge/icon)](http://dotnet-ci.cloudapp.net/job/dotnet_corefx_mac_debug/)|
|**Release**|[![Build status](http://dotnet-ci.cloudapp.net/job/dotnet_corefx_linux_release/badge/icon)](http://dotnet-ci.cloudapp.net/job/dotnet_corefx_linux_release/)|[![Build status](http://dotnet-ci.cloudapp.net/job/dotnet_corefx_windows_release/badge/icon)](http://dotnet-ci.cloudapp.net/job/dotnet_corefx_windows_release/)|[![Build status](http://dotnet-ci.cloudapp.net/job/dotnet_corefx_mac_release/badge/icon)](http://dotnet-ci.cloudapp.net/job/dotnet_corefx_mac_release/)|
|**Coverage Report**||[![Coverage Status](https://img.shields.io/badge/corefx-code_coverage-blue.svg)](http://dotnet-ci.cloudapp.net/job/dotnet_corefx_coverage_windows/lastStableBuild/Code_Coverage_Report/)||

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

Looking for something to work on? The list of [up-for-grabs issues](https://github.com/dotnet/corefx/labels/up%20for%20grabs) is a great place to start or for larger items see the list of [feature approved](https://github.com/dotnet/corefx/labels/feature%20approved). See some of our guides for more details:

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

The repo currently contains the source for the following components.
More libraries are coming soon (the overall list of items we currently plan to move onto GitHub is [here][typelist]).
['Watch'](https://github.com/dotnet/corefx/subscription) the repo to be notified.

|Component|Description|
|---------|-----------|
|**System.Collections**<br/>[![MyGet Package](https://img.shields.io/myget/dotnet-core/v/System.Collections.svg)](https://www.myget.org/gallery/dotnet-core)|<sub>Provides classes that define generic collections, which allow developers to create strongly-typed collections.</sub>|
|**System.Collections.Concurrent**<br/>[![MyGet Package](https://img.shields.io/myget/dotnet-core/v/System.Collections.Concurrent.svg)](https://www.myget.org/gallery/dotnet-core)|<sub>Provides a set of thread-safe collection types, instances of which may be used concurrently from multiple threads.</sub>|
|**System.Collections.Immutable**<br/>[![MyGet Package](https://img.shields.io/nuget/vpre/System.Collections.Immutable.svg)](http://www.nuget.org/packages/System.Collections.Immutable/)|<sub>Provides a set of immutable collection types that are safe to use concurrently.</sub>|
|**System.Collections.NonGeneric**<br/>[![MyGet Package](https://img.shields.io/myget/dotnet-core/v/System.Collections.NonGeneric.svg)](https://www.myget.org/gallery/dotnet-core)|<sub>Provides classes that define various collections of objects, such as ArrayList and Hashtable. _These collections exist in .NET Core primarily for backwards compatibility and generally should be avoided when writing new code_.</sub>|
|**System.Collections.Specialized**<br/>[![MyGet Package](https://img.shields.io/myget/dotnet-core/v/System.Collections.Specialized.svg)](https://www.myget.org/gallery/dotnet-core)|<sub>Provides classes that define specialized collections of objects, for example, a linked list dictionary and collections that contain only strings. _These collections exist in .NET Core primarily for backwards compatibility and generally should be avoided when writing new code_.</sub>|
|**System.ComponentModel**<br/>[![MyGet Package](https://img.shields.io/myget/dotnet-core/v/System.ComponentModel.svg)](https://www.myget.org/gallery/dotnet-core)|<sub>Provides interfaces for the editing and change tracking of objects used as data sources.</sub>|
|**System.ComponentModel.Annotations**<br/>[![MyGet Package](https://img.shields.io/myget/dotnet-core/v/System.ComponentModel.Annotations.svg)](https://www.myget.org/gallery/dotnet-core)|<sub>Provides attributes that are used to define metadata for objects used as data sources.</sub>|
|**System.ComponentModel.EventBasedAsync**<br/>[![MyGet Package](https://img.shields.io/myget/dotnet-core/v/System.ComponentModel.EventBasedAsync.svg)](https://www.myget.org/gallery/dotnet-core)|<sub>Provides support classes and delegates for the event-based asynchronous pattern. _This pattern and these supporting types exist in .NET Core primarily for backwards compatibility and generally should be avoided when writing new code_.</sub>|
|**System.ComponentModel.Primitives**<br/>[![MyGet Package](https://img.shields.io/myget/dotnet-core/v/System.ComponentModel.Primitives.svg)](https://www.myget.org/gallery/dotnet-core)|<sub>Provides interfaces that are used to implement the run-time and design-time behavior of components.</sub>|
|**System.ComponentModel.TypeConverter**<br/>[![MyGet Package](https://img.shields.io/myget/dotnet-core/v/System.ComponentModel.TypeConverter.svg)](https://www.myget.org/gallery/dotnet-core)|<sub>Provides the System.ComponentModel.TypeConverter class, which represents a unified way of converting types of values to other types.</sub>|
|**System.Console**<br/>[![MyGet Package](https://img.shields.io/myget/dotnet-core/v/System.Console.svg)](https://www.myget.org/gallery/dotnet-core)|<sub>Provides the Console class, which enables access to the standard input, output, and error streams for console-based applications.</sub>|
|**System.Diagnostics.Contracts**<br/>[![MyGet Package](https://img.shields.io/myget/dotnet-core/v/System.Diagnostics.Contracts.svg)](https://www.myget.org/gallery/dotnet-core)|<sub>Provides types and methods for representing program contracts such as preconditions, postconditions, and invariants.</sub>|
|**System.Diagnostics.Debug**<br/>[![MyGet Package](https://img.shields.io/myget/dotnet-core/v/System.Diagnostics.Debug.svg)](https://www.myget.org/gallery/dotnet-core)|<sub>Provides a class to interact with the debugger as well as methods for performing runtime assertions.</sub>|
|**System.Diagnostics.FileVersionInfo**<br/>[![MyGet Package](https://img.shields.io/myget/dotnet-core/v/System.Diagnostics.FileVersionInfo.svg)](https://www.myget.org/gallery/dotnet-core)|<sub>Provides useful functionality for querying and examining the version information of physical files on disk.</sub>|
|**System.Diagnostics.Process**<br/>[![MyGet Package](https://img.shields.io/myget/dotnet-core/v/System.Diagnostics.Process.svg)](https://www.myget.org/gallery/dotnet-core)|<sub>Provides access to local and remote processes, and enables the starting and stopping of local system processes.</sub>|
|**System.Diagnostics.TextWriterTraceListener**<br/>[![MyGet Package](https://img.shields.io/myget/dotnet-core/v/System.Diagnostics.TextWriterTraceListener.svg)](https://www.myget.org/gallery/dotnet-core)|<sub>Provides trace listeners for directing tracing output to a text writer, such as System.IO.StreamWriter.</sub>|
|**System.Diagnostics.Tools**<br/>[![MyGet Package](https://img.shields.io/myget/dotnet-core/v/System.Diagnostics.Tools.svg)](https://www.myget.org/gallery/dotnet-core)|<sub>Provides attributes, such as GeneratedCodeAttribute, that are emitted or consumed by analysis tools.</sub>|
|**System.Diagnostics.TraceSource**<br/>[![MyGet Package](https://img.shields.io/myget/dotnet-core/v/System.Diagnostics.TraceSource.svg)](https://www.myget.org/gallery/dotnet-core)|<sub>Provides classes that help you trace the execution of your code.</sub>|
|**System.Dynamic.Runtime**<br/>[![MyGet Package](https://img.shields.io/myget/dotnet-core/v/System.Dynamic.Runtime.svg)](https://www.myget.org/gallery/dotnet-core)|<sub>Provides classes and interfaces that support the Dynamic Language Runtime (DLR).</sub>|
|**System.Globalization.Extensions**<br/>[![MyGet Package](https://img.shields.io/myget/dotnet-core/v/System.Globalization.Extensions.svg)](https://www.myget.org/gallery/dotnet-core)|<sub>Provides classes for performing unicode string normalization, culture-specific string comparisons and support the use of non-ASCII characters for Internet domain names.</sub>|
|**System.IO**<br/>[![MyGet Package](https://img.shields.io/myget/dotnet-core/v/System.IO.svg)](https://www.myget.org/gallery/dotnet-core)|<sub>Provides base input and output (I/O) types that enable reading and writing data streams.</sub>|
|**System.IO.Compression**<br/>[![MyGet Package](https://img.shields.io/myget/dotnet-core/v/System.IO.Compression.svg)](https://www.myget.org/gallery/dotnet-core)|<sub>Provides classes that support the compression and decompression of streams.</sub>|
|**System.IO.Compression.ZipFile**<br/>[![MyGet Package](https://img.shields.io/myget/dotnet-core/v/System.IO.Compression.ZipFile.svg)](https://www.myget.org/gallery/dotnet-core)|<sub>Provides static methods for creating and using Zip files.</sub>|
|**System.IO.FileSystem**<br/>[![MyGet Package](https://img.shields.io/myget/dotnet-core/v/System.IO.FileSystem.svg)](https://www.myget.org/gallery/dotnet-core)|<sub>Provides access to the file system, including support for enumerating and manipulating file system objects and for reading and writing files via streams.</sub>|
|**System.IO.FileSystem.DriveInfo**<br/>[![MyGet Package](https://img.shields.io/myget/dotnet-core/v/System.IO.FileSystem.DriveInfo.svg)](https://www.myget.org/gallery/dotnet-core)|<sub>Provides the System.IO.DriveInfo class, which enables developers to query local drive information.</sub>|
|**System.IO.FileSystem.Primitives**<br/>[![MyGet Package](https://img.shields.io/myget/dotnet-core/v/System.IO.FileSystem.Primitives.svg)](https://www.myget.org/gallery/dotnet-core)|<sub>Provides common enumerations and exceptions for path-based I/O libraries.</sub>|
|**System.IO.FileSystem.Watcher**<br/>[![MyGet Package](https://img.shields.io/myget/dotnet-core/v/System.IO.FileSystem.Watcher.svg)](https://www.myget.org/gallery/dotnet-core)|<sub>Provides the System.IO.Watcher class, which listens to the system directory change notifications and raises events when a directory or file within a directory changes.</sub>|
|**System.IO.MemoryMappedFiles**<br/>[![MyGet Package](https://img.shields.io/myget/dotnet-core/v/System.IO.MemoryMappedFiles.svg)](https://www.myget.org/gallery/dotnet-core)|<sub>Provides access to memory-mapped files, enabling code to read and write files by reading and writing memory.</sub>|
|**System.IO.Packaging**<br/>[![MyGet Package](https://img.shields.io/myget/dotnet-core/v/System.IO.Packaging.svg)](https://www.myget.org/gallery/dotnet-core)|<sub>Provides classes that support storage of multiple data objects in a single container.</sub>|
|**System.IO.Pipes**<br/>[![MyGet Package](https://img.shields.io/myget/dotnet-core/v/System.IO.Pipes.svg)](https://www.myget.org/gallery/dotnet-core)|<sub>Provides types that enable a means for interprocess communication through anonymous and/or named pipes.</sub>|
|**System.IO.UnmanagedMemoryStream**<br/>[![MyGet Package](https://img.shields.io/myget/dotnet-core/v/System.IO.UnmanagedMemoryStream.svg)](https://www.myget.org/gallery/dotnet-core)|<sub>Provides a stream for accessing unmanaged memory as represented by a pointer, as well as an accessor for reading and writing primitive types from unmanaged memory.</sub>|
|**System.Linq**<br/>[![MyGet Package](https://img.shields.io/myget/dotnet-core/v/System.Linq.svg)](https://www.myget.org/gallery/dotnet-core)|<sub>Provides the foundation of Language-Integrated Query (LINQ), including LINQ standard query operators that operate on objects that implement ```IEnumerable<T>```.</sub>|
|**System.Linq.Expressions**<br/>[![MyGet Package](https://img.shields.io/myget/dotnet-core/v/System.Linq.Expressions.svg)](https://www.myget.org/gallery/dotnet-core)|<sub>Provides classes, interfaces, and enumerations that enable language-level code expressions to be represented as objects in the form of expression trees.</sub>|
|**System.Linq.Parallel**<br/>[![MyGet Package](https://img.shields.io/myget/dotnet-core/v/System.Linq.Parallel.svg)](https://www.myget.org/gallery/dotnet-core)|<sub> Provides a parallelized implementation of LINQ to Objects. "Parallel LINQ" (PLINQ) implements the full set of LINQ standard query operators as well as additional operators specific to parallel operations.</sub>|
|**System.Linq.Queryable**<br/>[![MyGet Package](https://img.shields.io/myget/dotnet-core/v/System.Linq.Queryable.svg)](https://www.myget.org/gallery/dotnet-core)|<sub>Provides LINQ standard query operators that operate on objects that implement ```IQueryable<T>```.</sub>|
|**System.Net.Http**<br/>[![MyGet Package](https://img.shields.io/myget/dotnet-core/v/System.Net.Http.svg)](https://www.myget.org/gallery/dotnet-core)|<sub>Provides a programming interface for modern HTTP applications, including HTTP client components that allow applications to consume web services over HTTP and HTTP components that can be used by both clients and servers for parsing HTTP headers.</sub>|
|**System.Numerics.Vectors**<br/>[![MyGet Package](https://img.shields.io/nuget/vpre/System.Numerics.Vectors.svg)](http://www.nuget.org/packages/System.Numerics.Vectors/)|<sub>Provides a set of basic vector types that leverage single instruction, multiple data (SIMD) CPU instructions.</sub>|
|**System.ObjectModel**<br/>[![MyGet Package](https://img.shields.io/myget/dotnet-core/v/System.ObjectModel.svg)](https://www.myget.org/gallery/dotnet-core)|<sub>Provides types and interfaces that enable the creation of observable types that provide notifications to clients when changes are made.</sub>|
|**System.Reflection**<br/>[![MyGet Package](https://img.shields.io/myget/dotnet-core/v/System.Reflection.svg)](https://www.myget.org/gallery/dotnet-core)|<sub>Provides types that retrieve information about assemblies, modules, members, parameters, and other entities in managed code by examining their metadata.</sub>|
|**System.Reflection.DispatchProxy**<br/>[![MyGet Package](https://img.shields.io/myget/dotnet-core/v/System.Reflection.DispatchProxy.svg)](https://www.myget.org/gallery/dotnet-core)|<sub>Provides a mechanism for dynamically creating proxy types that implement a specified interface and derive from a specified DispatchProxy type.</sub>|
|**System.Reflection.Emit.Lightweight**<br/>[![MyGet Package](https://img.shields.io/myget/dotnet-core/v/System.Reflection.Emit.Lightweight.svg)](https://www.myget.org/gallery/dotnet-core)|<sub>Provides the System.Reflection.Emit.DynamicMethod class, which represents a dynamic method that can be compiled, executed, and discarded.</sub>|
|**System.Reflection.Extensions**<br/>[![MyGet Package](https://img.shields.io/myget/dotnet-core/v/System.Reflection.Extensions.svg)](https://www.myget.org/gallery/dotnet-core)|<sub>Provides custom attribute extension methods for System.Reflection types.</sub>|
|**System.Reflection.Metadata**<br/>[![MyGet Package](https://img.shields.io/nuget/vpre/System.Reflection.Metadata.svg)](http://www.nuget.org/packages/System.Reflection.Metadata/)|<sub>Provides a highly-tuned, low-level ECMA-335 metadata reader.  This is the same reader used by "[Roslyn]" C# and Visual Basic compilers to parse assemblies.</sub>|
|**System.Reflection.TypeExtensions**<br/>[![MyGet Package](https://img.shields.io/myget/dotnet-core/v/System.Reflection.TypeExtensions.svg)](https://www.myget.org/gallery/dotnet-core)|<sub>Provides extension methods for types in the System.Reflection namespace. _These extensions are designed to be source-compatible with older reflection-based APIs_.</sub>|
|**System.Resources.ReaderWriter**<br/>[![MyGet Package](https://img.shields.io/myget/dotnet-core/v/System.Resources.ReaderWriter.svg)](https://www.myget.org/gallery/dotnet-core)|<sub>Provides classes for reading and writing resources in the system-default format.</sub>|
|**System.Runtime**<br/>[![MyGet Package](https://img.shields.io/myget/dotnet-core/v/System.Runtime.svg)](https://www.myget.org/gallery/dotnet-core)|<sub>Provides the fundamental primitives, classes, and base classes that define commonly-used value and reference data types, events and event handlers, interfaces, attributes, and exceptions.</sub>|
|**System.Runtime.Environment**<br/>[![MyGet Package](https://img.shields.io/myget/dotnet-core/v/System.Runtime.Environment.svg)](https://www.myget.org/gallery/dotnet-core)|<sub>Provides types that expose information about the runtime and operating system environment in which code is executing.</sub>|
|**System.Runtime.Extensions**<br/>[![MyGet Package](https://img.shields.io/myget/dotnet-core/v/System.Runtime.Extensions.svg)](https://www.myget.org/gallery/dotnet-core)|<sub>Provides commonly-used classes for performing mathematical functions, conversions, string comparisons, and querying environment information.</sub>|
|**System.Runtime.Handles**<br/>[![MyGet Package](https://img.shields.io/myget/dotnet-core/v/System.Runtime.Handles.svg)](https://www.myget.org/gallery/dotnet-core)|<sub>Provides base classes, including CriticalHandle and SafeHandle, for types that represent operating system handles.</sub>|
|**System.Runtime.InteropServices**<br/>[![MyGet Package](https://img.shields.io/myget/dotnet-core/v/System.Runtime.InteropServices.svg)](https://www.myget.org/gallery/dotnet-core)|<sub>Provides types that support platform invoke (P/Invoke) and COM interop.</sub>|
|**System.Runtime.Numerics**<br/>[![MyGet Package](https://img.shields.io/myget/dotnet-core/v/System.Runtime.Numerics.svg)](https://www.myget.org/gallery/dotnet-core)|<sub>Provides two useful numeric structures, BigInteger and Complex.</sub>|
|**System.Runtime.Serialization.Json**<br/>[![MyGet Package](https://img.shields.io/myget/dotnet-core/v/System.Runtime.Serialization.Json.svg)](https://www.myget.org/gallery/dotnet-core)|<sub>Provides classes for serializing objects to the JavaScript Object Notation (JSON) and for deserializing JSON data to objects.</sub>|
|**System.Runtime.Serialization.Primitives**<br/>[![MyGet Package](https://img.shields.io/myget/dotnet-core/v/System.Runtime.Serialization.Primitives.svg)](https://www.myget.org/gallery/dotnet-core)|<sub>Provides common types, including System.Runtime.Serialization.DataContractAttribute, for libraries that support data contract serialization.</sub>|
|**System.Runtime.Serialization.Xml**<br/>[![MyGet Package](https://img.shields.io/myget/dotnet-core/v/System.Runtime.Serialization.Xml.svg)](https://www.myget.org/gallery/dotnet-core)|<sub>Provides classes for serializing objects to the Extensible Markup Language (XML) and deserializing XML data to objects.</sub>|
|**System.Security.Cryptography.Encoding**<br/>[![MyGet Package](https://img.shields.io/myget/dotnet-core/v/System.Security.Cryptography.Encoding.svg)](https://www.myget.org/gallery/dotnet-core)|<sub>Provides types for representing Abstract Syntax Notation One (ASN.1)-encoded data.</sub>|
|**System.Security.Cryptography.Encryption**<br/>[![MyGet Package](https://img.shields.io/myget/dotnet-core/v/System.Security.Cryptography.Encryption.svg)](https://www.myget.org/gallery/dotnet-core)|<sub>Provides base types for symmetric and asymmetric cryptographic algorithms.</sub>|
|**System.Security.Cryptography.Hashing**<br/>[![MyGet Package](https://img.shields.io/myget/dotnet-core/v/System.Security.Cryptography.Hashing.svg)](https://www.myget.org/gallery/dotnet-core)|<sub>Provides base types for cryptographic hashing and hash-based message authentication code (HMAC).</sub>|
|**System.Security.Cryptography.Hashing.Algorithms**<br/>[![MyGet Package](https://img.shields.io/myget/dotnet-core/v/System.Security.Cryptography.Hashing.Algorithms.svg)](https://www.myget.org/gallery/dotnet-core)|<sub>Provides concrete implementations of cryptographic hashing and hash-based message authentication code (HMAC), including MD5, SHA-1, and SHA-2.</sub>|
|**System.Security.Principal**<br/>[![MyGet Package](https://img.shields.io/myget/dotnet-core/v/System.Security.Principal.svg)](https://www.myget.org/gallery/dotnet-core)|<sub>Provides the base interfaces for principal and identity objects that represent the security context under which code is running.</sub>|
|**System.Security.Principal.Windows**<br/>[![MyGet Package](https://img.shields.io/myget/dotnet-core/v/System.Security.Principal.Windows.svg)](https://www.myget.org/gallery/dotnet-core)|<sub>Provides classes for retrieving the current Windows user and for interacting with Windows users and groups.</sub>|
|**System.Security.SecureString**<br/>[![MyGet Package](https://img.shields.io/myget/dotnet-core/v/System.Security.SecureString.svg)](https://www.myget.org/gallery/dotnet-core)|<sub>Provides support for accessing and modifying text that should be kept confidential.</sub>|
|**System.ServiceProcess.ServiceController**<br/>[![MyGet Package](https://img.shields.io/myget/dotnet-core/v/System.ServiceProcess.ServiceController.svg)](https://www.myget.org/gallery/dotnet-core)|<sub>Provides the ServiceController class that represents a Windows service and allows you to connect to a running or stopped service, manipulate it, or get information about it.</sub>|
|**System.Text.Encoding**<br/>[![MyGet Package](https://img.shields.io/myget/dotnet-core/v/System.Text.Encoding.svg)](https://www.myget.org/gallery/dotnet-core)|<sub>Provides base abstract encoding classes for converting blocks of characters to and from blocks of bytes.</sub>|
|**System.Text.Encoding.CodePages**<br/>[![MyGet Package](https://img.shields.io/myget/dotnet-core/v/System.Text.Encoding.CodePages.svg)](https://www.myget.org/gallery/dotnet-core)|<sub>Provides the ability to access existing encoding types for string manipulation across common cultural standards, as well as support to create custom Encoding Providers.</sub>|
|**System.Text.Encoding.Extensions**<br/>[![MyGet Package](https://img.shields.io/myget/dotnet-core/v/System.Text.Encoding.Extensions.svg)](https://www.myget.org/gallery/dotnet-core)|<sub>Provides support for specific encodings, including ASCII, UTF-7, UTF-8, UTF-16, and UTF-32.</sub>|
|**System.Text.Encodings.Web**<br/>[![MyGet Package](https://img.shields.io/myget/dotnet-core/v/System.Text.Encodings.Web.svg)](https://www.myget.org/gallery/dotnet-core)|<sub> Provides support for encodings related to HTML, JavaScript, and URLs.</sub>|
|**System.Text.RegularExpressions**<br/>[![MyGet Package](https://img.shields.io/myget/dotnet-core/v/System.Text.RegularExpressions.svg)](https://www.myget.org/gallery/dotnet-core)|<sub>Provides a regular expression engine. The types in this library provide useful functionality for running common operations using regular expressions.</sub>|
|**System.Threading**<br/>[![MyGet Package](https://img.shields.io/myget/dotnet-core/v/System.Threading.svg)](https://www.myget.org/gallery/dotnet-core)|<sub>Provides synchronization primitives used when writing multi-threaded and asynchronous code.</sub>|
|**System.Threading.Tasks**<br/>[![MyGet Package](https://img.shields.io/myget/dotnet-core/v/System.Threading.Tasks.svg)](https://www.myget.org/gallery/dotnet-core)|<sub>Provides types that simplify the work of writing concurrent and asynchronous code.</sub>|
|**System.Threading.Tasks.Dataflow**<br/>[![MyGet Package](https://img.shields.io/nuget/vpre/Microsoft.Tpl.Dataflow.svg)](http://www.nuget.org/packages/Microsoft.Tpl.Dataflow/)|<sub> Provides a set of types that support actor/agent-oriented designs through primitives for in-process message passing, dataflow, and pipelining.</sub>|
|**System.Threading.Tasks.Parallel**<br/>[![MyGet Package](https://img.shields.io/myget/dotnet-core/v/System.Threading.Tasks.Parallel.svg)](https://www.myget.org/gallery/dotnet-core)|<sub> Provides library-based data parallel replacements for common operations such as for loops, for each loops, and execution of a set of statements.</sub>|
|**System.Xml.ReaderWriter**<br/>[![MyGet Package](https://img.shields.io/myget/dotnet-core/v/System.Xml.ReaderWriter.svg)](https://www.myget.org/gallery/dotnet-core)|<sub>Provides types for reading and writing streams of XML.</sub>|
|**System.Xml.XDocument**<br/>[![MyGet Package](https://img.shields.io/myget/dotnet-core/v/System.Xml.XDocument.svg)](https://www.myget.org/gallery/dotnet-core)|<sub>Provides XML-related types for querying XML documents using LINQ.</sub>|
|**System.Xml.XmlDocument**<br/>[![MyGet Package](https://img.shields.io/myget/dotnet-core/v/System.Xml.XmlDocument.svg)](https://www.myget.org/gallery/dotnet-core)|<sub> Provides types for manipulating an XML Document Object Model (DOM).</sub>|
|**System.Xml.XmlSerializer**<br/>[![MyGet Package](https://img.shields.io/myget/dotnet-core/v/System.Xml.XmlSerializer.svg)](https://www.myget.org/gallery/dotnet-core)|<sub>Provides classes for serializing objects to XML and for deserializing XML data to objects.</sub>|
|**System.Xml.XPath**<br/>[![MyGet Package](https://img.shields.io/myget/dotnet-core/v/System.Xml.XPath.svg)](https://www.myget.org/gallery/dotnet-core)|<sub> Provides classes that define a cursor model for navigating and editing XML information items as instances of the XQuery 1.0 and XPath 2.0 Data Model.</sub>|
|**System.Xml.XPath.XDocument**<br/>[![MyGet Package](https://img.shields.io/myget/dotnet-core/v/System.Xml.XPath.XDocument.svg)](https://www.myget.org/gallery/dotnet-core)|<sub>Provides extension methods that add System.Xml.XPath support to the System.Xml.XDocument package.</sub>|
|**System.Xml.XPath.XmlDocument**<br/>[![MyGet Package](https://img.shields.io/myget/dotnet-core/v/System.Xml.XPath.XmlDocument.svg)](https://www.myget.org/gallery/dotnet-core)|<sub>Provides extension methods that add System.Xml.XPath support to the System.Xml.XmlDocument package.</sub>|
|**Microsoft.CSharp**<br/>[![MyGet Package](https://img.shields.io/myget/dotnet-core/v/Microsoft.CSharp.svg)](https://www.myget.org/gallery/dotnet-core)|<sub>Provides support for compilation and code generation, including dynamic, using the C# language.</sub>|
|**Microsoft.VisualBasic**<br/>[![MyGet Package](https://img.shields.io/myget/dotnet-core/v/Microsoft.VisualBasic.svg)](https://www.myget.org/gallery/dotnet-core)|<sub>Provides types that support the Visual Basic runtime.</sub>|
|**Microsoft.Win32.Primitives**<br/>[![MyGet Package](https://img.shields.io/myget/dotnet-core/v/Microsoft.Win32.Primitives.svg)](https://www.myget.org/gallery/dotnet-core)|<sub>Provides common types supporting the implementation of Win32-based libraries.</sub>|
|**Microsoft.Win32.Registry**<br/>[![MyGet Package](https://img.shields.io/myget/dotnet-core/v/Microsoft.Win32.Registry.svg)](https://www.myget.org/gallery/dotnet-core)|<sub>Provides support for accessing and modifying the Windows Registry.</sub>|

[roslyn]: https://github.com/dotnet/roslyn
[typelist]: https://github.com/dotnet/corefx-progress/blob/master/src-diff/README.md

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
