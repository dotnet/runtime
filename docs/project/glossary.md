# Glossary

Over the years, we've accumulated quite a few terms, platforms, and components
that can make it hard for folks (including us) to understand what we're
referring to. This document has a list that will help to qualify what we mean by
what.

This will also list some aliases. As you'll see the aliases aren't always
correct -- they are merely listed to help you find the better and less confusing
terminology.

| Term  | Description |
| ----- | ------------- |
| AOT | Ahead-of-time compiler. Converts the MSIL bytecode to native machine code for a specific target CPU architecture. |
| BBT | Microsoft internal early version of C/C++ PGO. See https://www.microsoft.com/windows/cse/bit_projects.mspx. |
| BOTR | Book Of The Runtime. |
| BCL | Base Class Library. A set of `System.*` (and to a limited extent `Microsoft.*`) libraries that make up the lower layer of the .NET library stack. |
| CIL | Common Intermediate Language. Equivalent to IL, also equivalent to [MSIL](https://docs.microsoft.com/dotnet/standard/managed-execution-process#compiling-to-msil). |
| CLI | Command Line Interface, or Common Language Infastructure. |
| CLR | [Common Language Runtime](https://docs.microsoft.com/dotnet/standard/clr). |
| COMPlus | An early name for the .NET platform, back when it was envisioned as a successor to the COM platform (hence, "COM+"). Used in various places in the CLR infrastructure, most prominently as a common prefix for the names of internal configuration settings. Note that this is different from the product that eventually ended up being named [COM+](https://msdn.microsoft.com/library/windows/desktop/ms685978.aspx). |
| COR | [Common Object Runtime](http://www.danielmoth.com/Blog/mscorlibdll.aspx). The name of .NET before it was named .NET. |
| CoreFX | Core Framework. Original project name for open source and cross-platform version of [.NET runtime libraries](https://github.com/dotnet/runtime/tree/main/src/libraries) |
| DAC | Data Access Component. An abstraction layer over the internal structures in the runtime. |
| EE | [Execution Engine](https://docs.microsoft.com/dotnet/standard/managed-execution-process#running_code). |
| GC | [Garbage Collector](https://github.com/dotnet/runtime/blob/main/docs/design/coreclr/botr/garbage-collection.md). |
| IBC | Instrumented Block Counts - used as extension (`*.ibc`) for old PGO files. |
| IPC | Inter-Process Communication. |
| IL | Intermediate Language. Equivalent to CIL, also equivalent to [MSIL](https://docs.microsoft.com/dotnet/standard/managed-execution-process#compiling-to-msil). |
| JIT | [Just-in-Time](https://github.com/dotnet/runtime/blob/main/docs/design/coreclr/jit/ryujit-overview.md) compiler. RyuJIT is the code name for the next generation Just-in-Time(aka "JIT") for the .NET runtime. |
| LCG | Lightweight Code Generation. An early name for [dynamic methods](https://github.com/dotnet/runtime/blob/main/src/coreclr/System.Private.CoreLib/src/System/Reflection/Emit/DynamicMethod.cs). |
| MD | MetaData. |
| MDA | Managed Debugging Assistant - see [details](https://docs.microsoft.com/dotnet/framework/debug-trace-profile/diagnosing-errors-with-managed-debugging-assistants) (Note: Not in .NET Core, equivalent diagnostic functionality is made available on a case-by-case basis, e.g. [#9418](https://github.com/dotnet/runtime/issues/9418)) |
| MIBC | Managed Instrumented Block Counts - used as extension (`*.mibc`) for managed PGO files. |
| MSIL | [Microsoft Intermediate Language](https://docs.microsoft.com/dotnet/standard/managed-execution-process#compiling-to-msil).Common Intermediate Language. Equivalent to IL, also equivalent to CIL. |
| NGen | Native Image Generator. |
| NYI | Not Yet Implemented. |
| PAL | [Platform Adaptation Layer](http://archive.oreilly.com/pub/a/dotnet/2002/03/04/rotor.html). Provides an abstraction layer between the runtime and the operating system. |
| PE | Portable Executable. |
| PGO | Profile Guided Optimization - see [details](https://blogs.msdn.microsoft.com/vcblog/2008/11/12/pogo/). |
| POGO | Profile Guided Optimization - see [details](https://blogs.msdn.microsoft.com/vcblog/2008/11/12/pogo/). |
| ProjectN | Codename for the first version of [.NET Native for UWP](https://msdn.microsoft.com/vstudio/dotnetnative.aspx). |
| R2R | Ready-to-Run. A flavor of native images - command line switch of [crossgen](../workflow/building/coreclr/crossgen.md). |
| Redhawk | Codename for experimental minimal managed code runtime that evolved into [CoreRT](https://github.com/dotnet/corert/). |
| SDK | Software Development Kit. The [.NET SDK](https://docs.microsoft.com/dotnet/core/sdk) contains the .NET CLI, .NET libraries and runtime, and the dotnet driver. |
| SEH | [Structured Exception Handling](https://docs.microsoft.com/windows/win32/debug/structured-exception-handling). Unified mechanism for handling hardware and software exceptions on Windows. |
| SOS | [Son of Strike](https://learn.microsoft.com/dotnet/framework/tools/sos-dll-sos-debugging-extension). The debugging extension for DbgEng based debuggers. Uses the DAC as an abstraction layer for its operation. The obscure name derives from the original nickname for the CLR team at Microsoft: "Lightning". The team built a debugging tool humorously named "Strike", and a subset of that dubbed "Son of Strike". |
| SPCL | `System.Private.CoreLib` - the lowest managed assembly in the libraries stack that contains `System.Object`, `String`, etc. |
| SuperPMI | JIT component test framework (super fast JIT testing - it mocks/replays EE in EE-JIT interface) - see [SuperPMI details](https://github.com/dotnet/runtime/blob/main/src/coreclr/tools/superpmi/readme.md). |
| SVR | The CLR used to be built as two variants, with one called "mscorsvr.dll", to mean the "server" version. In particular, it contained the server GC implementation, which was intended for multi-threaded apps capable of taking advantage of multiple processors. In the .NET Framework 2 release, the two variants were merged into "mscorwks.dll". The WKS version was the default, however the SVR version remained available. |
| TFM | [Target Framework Moniker](https://docs.microsoft.com/dotnet/standard/frameworks) such as `net6.0` or `netstandard2.0`. |
| TPA | Trusted Platform Assemblies used to be a special set of assemblies that comprised the platform assemblies, when it was originally designed. As of today, it is simply the set of assemblies known to constitute the application. |
| URT | Universal Runtime. Ancient name for what ended up being .NET, is used in the WinError facility name FACILITY_URT. |
| UTC | [Universal Tuple Compiler](https://blogs.msdn.microsoft.com/vcblog/2013/06/12/optimizing-c-code-overview/). The Microsoft C++ optimizer back-end that starts by converting the information from the FrontEnd into tuples – a binary stream of instructions. |
| UWP | [Universal Windows Platform (UWP)](https://docs.microsoft.com/en-us/windows/uwp/get-started/universal-application-platform-guide) is a platform-homogeneous application architecture available on every device that runs Windows 10. |
| VSD | [Virtual Stub Dispatch](../design/coreclr/botr/virtual-stub-dispatch.md). Technique of using stubs for virtual method invocations instead of the traditional virtual method table. |
| VM | Virtual machine. |
| WKS | The CLR used to be built as two variants, with one called "mscorwks.dll", to mean the "workstation" version. In particular, it contained the client GC implementation, which was intended for single-threaded apps, independent of how many processors were on the machine. In the .NET Framework 2 release, the two variants were merged into "mscorwks.dll". The WKS version was the default, however the SVR version remained available. |
| ZAP | Original code name for NGen. |

## Terms

In this document, the following terms are used:

* **IL**. Intermediate language. Higher level .NET languages, such as C#,
  compile down to a hardware agnostic instruction set, which is called
  Intermediate Language (IL). IL is sometimes referred to as MSIL (Microsoft IL)
  or CIL (Common IL).

* **GC**. Garbage collector. Garbage collection is an implementation of
  automatic memory management. .NET Framework and .NET Core currently uses a
  generational garbage collector, i.e. it groups objects into generations to
  limit the number of nodes it has to walk for determining which objects are
  alive. This speeds up collection times.

* **JIT**. Just in time compiler. This technology compiles IL to machine code
  that the processor understands. It's called JIT because compilation happens on
  demand and is performed on the same machine the code needs to run on. Since
  JIT compilation occurs during execution of the application, compile time is
  part of the run time. Thus, JIT compilers have to trade spending more time
  optimizing code with the savings the resulting code can produce. But a JIT
  knows the actual hardware and can free developers from having to ship
  different implementations. For instance, our vector library relies on the JIT
  to use the highest available SIMD instruction set.

* **CLI**. Command Line Interface --or-- Common Language Infastructure.
  * Command Line Interface: A tool that has no graphical interface and is
    intended to be used completely from a console/terminal. Also is commonly
    used as shorthand for the [.NET tooling][dotnet-tooling].
  * Common Language Infastructure: The [open specification][ECMA-355] that defines
    IL, how to store IL as binary (assemblies), and how the runtime works. AKA:
    [ECMA-355][ECMA-355].

* **CLR**. Common Language Runtime: The runtime/environment that .NET code
  executes in. It is also commonly used to refer to the Microsoft .NET Framework
  Windows-only implementation.

## .NET Runtimes

### .NET Core / .NET

.NET Core has been the name for the open source, cross-platform stack that
ASP.NET Core and UWP applications are built on. For more details,
read [Introducing .NET Core][introducing-net-core].

.NET Core has become future of the platform, and we refer to it just as .NET today.
For more details, read [Introducing .NET 5][introducing-net-5].

### .NET Framework

**Also referred to as**: Desktop, full framework

.NET Framework was the very first .NET runtime. It first shipped in 2002, and it has been
updated on a regular basis since then.

The .NET Framework was designed to run on Windows only. Some versions of the
.NET Framework come pre-installed with Windows, some require to be installed.
However, in both cases the .NET Framework is a system-wide component.

### Rotor

**Also referred to as**: Shared Source Common Language Infrastructure (SSCLI)

Pretty much at the same time the .NET Framework was released, Microsoft also
published Rotor, which is the source code for an implementation of ECMA 335
(Common Language Infrastructure), which is the specification behind .NET.

While parts of the source were identical with the .NET Framework, many pieces
had prototypical implementations instead: the purpose of Rotor wasn't to provide
a production ready .NET implementation but to provide a platform for research,
academia, and validation that the ECMA 335 specification itself can be
implemented.

It's also worth pointing out that the source code of Rotor was not released
under an open source license (i.e. not approved by OSI) and has not been
officially updated since .NET Framework 2.0.

### Mono

[Mono][mono] is an open source alternative to the .NET Framework. Mono started around
the same time the .NET Framework was first released. Since Microsoft didn't
release Rotor as open source, Mono was forced to start from scratch and was thus
a complete re-implementation of the .NET Framework with no shared code.

Today, the [Mono VM](https://github.com/dotnet/runtime/tree/main/src/mono) is part
of the unified .NET platform. It is optimized for mobile (e.g. Xamarin) and browser (e.g. Blazor) scenarios.

"C# powered by Mono" has been scripting engine of choice for a number of game engines.
Unity - the world's most popular game engine - is scripted by C#, powered by a customized Mono runtime.

### CoreCLR

Originally, CoreCLR was the runtime of Silverlight and was designed to run on multiple
platforms, specifically Windows and OS X.

Today, the [CoreCLR runtime](https://github.com/dotnet/runtime/tree/main/src/coreclr)
is part of unified .NET platform. It is optimized for cloud (e.g. ASP.NET) and
desktop (e.g. WinForms, WPF) scenarios.

## Ahead-Of-Time Compilation (AOT)

Most flavors of .NET runtime come with at least partial AOT compilation. A variety of AOT technologies
with unique characteristics were developed for .NET runtimes over the years.

### ReadyToRun

**Also referred to as**: R2R

[ReadyToRun](../design/coreclr/botr/readytorun-overview.md)
is a file format used by the CoreCLR runtime to store AOT compiled code. `crossgen` is the AOT compiler that
produces binaries in the ReadyToRun file format.

### NGen

[NGen](https://docs.microsoft.com/en-us/dotnet/framework/tools/ngen-exe-native-image-generator)
is AOT technology included in .NET Framework. It usually compiles code at install time on the machine where
the code will be executed.

### Full AOT

[Full AOT](https://docs.microsoft.com/en-us/xamarin/ios/internals/architecture) is used
by Mono runtime in environments that prohibit fallback to JIT.

### Hybrid AOT

[Hybrid AOT](https://docs.microsoft.com/en-us/xamarin/mac/internals/aot#hybrid-aot) is used
by Mono runtime in environments that allow fallback to JIT or need IL interpreter.

### Native AOT

[Native AOT](https://github.com/dotnet/designs/blob/main/accepted/2020/form-factors.md#native-aot-form-factors) is
a .NET runtime form factor with key performance characteristics (startup time, binary size and steady state throughput and predictability)
competitive with statically compiled languages.

## Frameworks

### Language-Integrated Query

**Also referred to as**: LINQ

Introduced in .NET Framework 3.5, Language-Integrated Query's (LINQ) goal to
make data processing easier. LINQ is primarily a collection of methods that
extend `IEnumerable` and `IEnumerable<T>`. LINQ is intended to be used with
extension methods and Lambda functions (added in C# 3.0 and VB 9.0 at the same
time as .NET Framework 3.5 was released) allowing for a function style of
programming.

A simple example of LINQ is

```csharp
var odds = source.Where(obj => obj.Id == 1).ToArray();
```

#### IQueryable&lt;T&gt; and Expressions

One of the big advantages of using LINQ over more common data processing
patterns is that the function given to the LINQ function can be converted to an
expression and then executed in some other form, like SQL or on another machine
across the network. An expression is a in-memory representation of some logic to
follow.

For example, in the above sample `source` could actually be a database
connection and the function call `Where(obj => obj.Id == 1)` would be converted
to a SQL WHERE clause: `WHERE ID = 1`, and then executed on the SQL server.

#### Parallel LINQ

**Also referred to as**: PLINQ

Also introduced in .NET Framework 3.5 Parallel LINQ. Parallel LINQ has a subset
of the methods the LINQ does but may execute the iterations on different threads
in any order. Generally to use Parallel LINQ you would just call the
`AsParallel()` method on a collection implementing `IEnumerable`. And if at any
point you wanted to return to "normal LINQ you can just call `AsSequential()`.

### Dynamic Language Runtime

**Also referred to as**: DLR

Introduced in .NET Framework 4.0, Dynamic Language Runtime's (DLR) goal is to
develop dynamic languages to run on the .NET Framework and to add dynamic
features to statically typed languages, primarily C# 4.0.
[Its features][dlr-architecture] include expression trees, call site caching,
and dynamic object interoperability.

[Microsoft has Open Sourced the Dynamic Language Runtime][dlr-source], along
with two examples of languages developed by using it:
[IronPython](https://github.com/IronLanguages/ironpython2)
and [IronRuby](https://github.com/IronLanguages/ironruby).

### Windows Forms

**Also referred to as**: WinForms

Windows Forms is an API provided by the .NET Framework (mostly in the
`System.Windows.Forms` namespace) for creating desktop applications. Windows
Forms provides an event-driven model for application development on top of the
native loop-driven Win32 model. Mono [has an implementation][mono-winforms] of
Windows Forms, though it is not complete, since some parts of Windows Forms are
tied to the Windows platform.

[Microsoft has Open Sourced Windows Forms][ui-oss]. This
included moving the code to [GitHub under the stewardship of the .NET
Foundation][WinForms] and enabling support for running Windows Forms on .NET
Core (Windows Only).

### Windows Presentation Foundation

**Also referred to as**: WPF, Avalon

Introduced in .NET Framework 3.0, Windows Presentation Foundation (WPF) was a
new API for creating desktop applications. Like Windows Forms, WPF is
event-driven. However, instead of using GDI/GDI+ for drawing applications, WPF
used DirectX. Using DirectX allowed WPF applications to use the GPU for
rendering, freeing the CPU for other tasks. WPF also introduced XAML, an
XML-based language which allows a declarative way to describe user interfaces
and data binding to models (XAML is used by Silverlight, UWP, and Xamarin as
well).

[Microsoft has Open Sourced WPF][ui-oss]. This included
moving the code to [GitHub under the stewardship of the .NET Foundation][Wpf]
and enabling support for running WPF on .NET Core (Windows Only).

## Engineering system

* **Helix**. It's a massively-parallel, general-purpose job processing and
  result aggregation system running in the cloud. The work items that corefx
  sends to Helix are [xunit][xunit] tests. Test results are shown through the
  [*Mission Control* reporting site][mc.dot.net]; to go to the test results in a
  PR from Azure DevOps, you can click on the *Send to Helix* step in the build,
  and the logs will have the URL.


[introducing-net-core]: https://devblogs.microsoft.com/dotnet/introducing-net-core/
[introducing-net-5]: https://devblogs.microsoft.com/dotnet/introducing-net-5/
[mono]: http://github.com/mono/mono
[referencesource]: https://github.com/microsoft/referencesource
[mono-supported-platforms]: http://www.mono-project.com/docs/about-mono/supported-platforms/
[mono-winforms]: http://www.mono-project.com/docs/gui/winforms/
[xunit]: https://github.com/xunit
[mc.dot.net]: https://mc.dot.net/
[ECMA-355]: https://www.ecma-international.org/publications-and-standards/standards/ecma-335
[dotnet-tooling]: https://docs.microsoft.com/en-us/dotnet/core/tools/
[dlr-architecture]: https://docs.microsoft.com/en-us/dotnet/framework/reflection-and-codedom/dynamic-language-runtime-overview#dlr-architecture
[dlr-source]: https://github.com/IronLanguages/dlr
[WinForms]: https://github.com/dotnet/winforms
[Wpf]: https://github.com/dotnet/wpf
[ui-oss]: https://devblogs.microsoft.com/dotnet/announcing-net-core-3-preview-1-and-open-sourcing-windows-desktop-frameworks/
