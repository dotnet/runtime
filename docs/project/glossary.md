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
| CLR | Common Language Runtime. |
| COMPlus | An early name for the .NET platform, back when it was envisioned as a successor to the COM platform (hence, "COM+"). Used in various places in the CLR infrastructure, most prominently as a common prefix for the names of internal configuration settings. Note that this is different from the product that eventually ended up being named [COM+](https://msdn.microsoft.com/en-us/library/windows/desktop/ms685978.aspx). |
| COR | [Common Object Runtime](http://www.danielmoth.com/Blog/mscorlibdll.aspx). The name of .NET before it was named .NET. |
| DAC | Data Access Component. An abstraction layer over the internal structures in the runtime. |
| EE | Execution Engine. |
| GC | [Garbage Collector](https://github.com/dotnet/runtime/blob/master/docs/design/coreclr/botr/garbage-collection.md). |
| IPC | Inter-Process Communication. |
| JIT | [Just-in-Time](https://github.com/dotnet/runtime/blob/master/docs/design/coreclr/jit/ryujit-overview.md) compiler. RyuJIT is the code name for the next generation Just-in-Time(aka "JIT") for the .NET runtime. |
| LCG | Lightweight Code Generation. An early name for [dynamic methods](https://github.com/dotnet/runtime/blob/master/src/coreclr/src/System.Private.CoreLib/src/System/Reflection/Emit/DynamicMethod.cs). |
| MD | MetaData. |
| MDA | Managed Debugging Assistant - see [details](https://docs.microsoft.com/en-us/dotnet/framework/debug-trace-profile/diagnosing-errors-with-managed-debugging-assistants) (Note: Not in .NET Core, equivalent diagnostic functionality is made available on a case-by-case basis, e.g. [#9418](https://github.com/dotnet/runtime/issues/9418)) |
| NGen | Native Image Generator. |
| NYI | Not Yet Implemented. |
| PAL | [Platform Adaptation Layer](http://archive.oreilly.com/pub/a/dotnet/2002/03/04/rotor.html). Provides an abstraction layer between the runtime and the operating system. |
| PE | Portable Executable. |
| PGO | Profile Guided Optimization - see [details](https://blogs.msdn.microsoft.com/vcblog/2008/11/12/pogo/). |
| POGO | Profile Guided Optimization - see [details](https://blogs.msdn.microsoft.com/vcblog/2008/11/12/pogo/). |
| ProjectN | Codename for the first version of [.NET Native for UWP](https://msdn.microsoft.com/en-us/vstudio/dotnetnative.aspx). |
| R2R | Ready-to-Run. A flavor of native images - command line switch of [crossgen](../workflow/building/coreclr/crossgen.md). |
| Redhawk | Codename for experimental minimal managed code runtime that evolved into [CoreRT](https://github.com/dotnet/corert/). |
| SOS | [Son of Strike](https://docs.microsoft.com/en-us/archive/blogs/jasonz/sos-debugging-of-the-clr-part-1). The debugging extension for DbgEng based debuggers. Uses the DAC as an abstraction layer for its operation. |
| SuperPMI | JIT component test framework (super fast JIT testing - it mocks/replays EE in EE-JIT interface) - see [SuperPMI details](https://github.com/dotnet/runtime/blob/master/src/coreclr/src/ToolBox/superpmi/readme.txt). |
| SVR | The CLR used to be built as two variants, with one called "mscorsvr.dll", to mean the "server" version. In particular, it contained the server GC implementation, which was intended for multi-threaded apps capable of taking advantage of multiple processors. In the .NET Framework 2 release, the two variants were merged into "mscorwks.dll". The WKS version was the default, however the SVR version remained available. |
| TPA | Trusted Platform Assemblies used to be a special set of assemblies that comprised the platform assemblies, when it was originally designed. As of today, it is simply the set of assemblies known to constitute the application. |
| URT | Universal Runtime. Ancient name for what ended up being .NET, is used in the WinError facility name FACILITY_URT. |
| UTC | [Universal Tuple Compiler](https://blogs.msdn.microsoft.com/vcblog/2013/06/12/optimizing-c-code-overview/). The Microsoft C++ optimizer back-end that that starts by converting the information from the FrontEnd into tuples – a binary stream of instructions. |
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

* **AOT**. Ahead of time compiler. Similar to JIT, this compiler also translates
  IL to machine code. In contrast to JIT compilation, AOT compilation happens
  before the application is executed and is usually performed on a different
  machine. AOT tool chains don't trade runtime for compile time and thus can
  spend more time optimizing. Since the context of AOT is the entire
  application, the AOT compiler can also perform cross module linking and whole
  program analysis, which means that all references are followed and a single
  executable is produced.

* **NGEN**. Native (image) generation. You can think of this technology as a
  persistent JIT compiler. It usually compiles code on the machine where the
  code will be executed, but compilation typically occurs at install time.

* **CoreFX**. Core framework. Conceptually a set of `System.*` (and to a limited
  extent `Microsoft.*`) libraries that make up the lower layer of the .NET
  library stack. It's what most people would think of as the Base Class Library
  (BCL). The BCL is a general purpose, lower level set of functionality that
  higher-level frameworks, such as WCF and ASP.NET, build on. The source code of
  the .NET Core library stack is contained in the [CoreFX repo][corefx].
  However, the majority of the .NET Core APIs are also available in the .NET
  Framework, so you can think of CoreFX as a fork of the .NET Framework library
  stack.

* **CLI**. Command Line Interface --or-- Common Language Infastructure.
  * Command Line Interface: A tool that has no graphical interface and is
    intended to be used completely from a console/terminal. Also is commonly
    used as shorthand for the [.NET tooling][dotnet-tooling].
  * Common Language Infastructure: The [open specification][ECMA-355] that defines
    IL, how to store IL as binary (assemblies), and how the runtime works. AKA:
    [ECMA-355][ECMA-355].

* **CLR**. Common Language Runtime: The runtime/environment that .NET code
  executes in. Is also commonly used to refer to the Microsoft Windows only
  implementation

## Runtimes

### Common Language Runtime

**Also referred to as**: CLR, VM, runtime

The CLR is a virtual machine, i.e. it includes the facilities to generate and
compile code on-the-fly using a JIT compiler. The existing Microsoft CLR
implementation is Windows only.

### Core Common Language Runtime

**Also referred to as**: CoreCLR, VM, runtime

It's built from the same code base as the CLR. Originally, CoreCLR was the
runtime of Silverlight and was designed to run on multiple platforms,
specifically Windows and OS X. CoreCLR is now part of .NET Core and represents a
simplified version of the CLR. It's still a [cross platform][core-build-status]
runtime. CoreCLR is also a virtual machine with a JIT.

### Core Runtime

**Also referred to as**: CoreRT

In contrast to the CLR/CoreCLR, CoreRT is not a virtual machine, i.e. it doesn't
include the facilities to generate and run code on-the-fly because it doesn't
include a JIT. It does, however, include the GC and the ability for runtime type
identification (RTTI) as well as reflection. However, its type system is
designed so that metadata for reflection can be omitted. This enables having an
AOT tool chain that can link away superfluous metadata and (more importantly)
identify code that the application doesn't use.

## Platforms

### .NET Framework

**Also referred to as**: Desktop, full framework, in-box framework, ~~CLR~~

This refers to the .NET Framework that first shipped in 2002 and has been
updated on a regular basis since then. It's the main framework folks target
today and allows you to build a wide variety of applications, such as WinForms,
WPF, ASP.NET, and command line tools.

The .NET Framework was designed to run on Windows only. Some versions of the
.NET Framework come pre-installed with Windows, some require to be installed.
However, in both cases the .NET Framework is a system-wide component.
Applications do not include .NET Framework DLLs when deploying; the correct .NET
version must be on the machine.

### .NET Core

**Also referred to as**: UWP, ~~Store~~

Originally, .NET Core was the identifier we used to describe the .NET APIs
Windows 8 store applications could use. When we designed the API set, we wanted
to create a foundation for .NET where portability is a first class concern for
the layering and componentization. For more details, read [this blog
post][introducing-net-core].

Today, .NET Core is no longer just for store applications. .NET Core is the name
for the open source, cross-platform stack that ASP.NET Core and UWP applications
are built on. The stack includes a set of framework libraries (CoreFX), a JIT
based runtime (CoreCLR), an AOT based runtime (CoreRT), and a set of tooling
(such as the dotnet CLI).

That's why referring to .NET Core as 'Store' is no longer correct. But you can
think of today's .NET Core as an evolution of the original APIs available for
store applications. Many of the original design goals are still relevant,
especially around layering and portability.

### Universal Windows Platform (UWP)

**Also referred to as**: Store, WinRT, Metro

The Universal Windows Platform (UWP) is the platform that is used for building
modern, touch-enabled Windows applications as well as headless devices for
Internet of Things (IoT). It's designed to unify the different types of devices
that you may want to target, including PCs, tablets, phablets, phones, and even
the Xbox.

UWP provides many services, such as a centralized app store, an execution
environment (AppContainer), and a set of Windows APIs to use instead of Win32
(WinRT). UWP has no dependency on .NET; apps can be written in C++, C#, VB.NET,
and JavaScript. When using C# and VB.NET the .NET APIs are provided by .NET
Core.

**Also referred to as**: ahead-of-time (AOT), IL compiler (ILC)

.NET Native is a compiler tool chain that will produce native code ahead-of-time
(AOT), as opposed to just-in-time (JIT). The compilation can happen on the
developer machine as well as on the store side, which allows blending AOT with
the benefits of servicing.

You can think of .NET Native as an evolution of NGEN (Native Image Generator):
NGEN basically simply runs the JIT up front, the code quality and behavior is
identical to the JITed version. Another downside of NGEN is that it happens on
the user's machine, rather than the developer's machine. NGEN is also at the
module level, i.e. for each MSIL assembly there is a corresponding NGEN'ed
assembly that contains the native code. .NET Native on the other hand is a C++
like compiler and linker. It will remove unused code, spend more time optimizing
it, and produce a single, merged module that represents the closure of the
application.

UWP was the first application model that was supported by .NET Native. We now
also support building native console applications for Windows, OS X and Linux.

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

Mono is an open source alternative to the .NET Framework. Mono started around
the same time the .NET Framework was first released. Since Microsoft didn't
release Rotor as open source, Mono was forced to start from scratch and is thus
a complete re-implementation of the .NET Framework with no shared code.

When .NET Core was released under the MIT license, Microsoft also released large
chunks of the .NET Framework under the MIT license as well, which can be found
[here][referencesource]. This enabled the Mono community to use the same code
the .NET Framework uses in order to close gaps and avoid behavioral differences.

Mono is primarily used to run .NET applications on Linux and macOS (though to
get into the Mac App Store you need Xamarin, see below). There are ports of Mono
to other platforms, see [Mono's Supported Platforms][mono-supported-platforms]

Mono has implementations (though not necessarily complete) of WinForms, ASP.NET,
and System.Drawing.

### Xamarin

Xamarin is a commercial offering for building mobile applications targeting
Android, iOS and Mac OS X Store. It's based on Mono, and on iOS and Android
surfaces a different API profile, called the mobile profile. The subsetting was
necessary to reduce the footprint, both by shipping smaller versions of the
system libraries as well as making them more linker friendly. While Mono runs on
macOS without Xamarin, their linker is required make the app package for the
Mac App Store.  Xamarin ships a full static compiler on iOS, as the platform
does not support dynamic code generation.


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
[core-build-status]: https://github.com/dotnet/coreclr#build-status
[corefx]: http://github.com/dotnet/corefx
[referencesource]: https://github.com/microsoft/referencesource
[mono-supported-platforms]: http://www.mono-project.com/docs/about-mono/supported-platforms/
[mono-winforms]: http://www.mono-project.com/docs/gui/winforms/
[xunit]: https://github.com/xunit
[mc.dot.net]: https://mc.dot.net/
[ECMA-355]: http://www.ecma-international.org/publications/standards/Ecma-335.htm
[dotnet-tooling]: https://docs.microsoft.com/en-us/dotnet/core/tools/
[WinForms]: https://github.com/dotnet/winforms
[Wpf]: https://github.com/dotnet/wpf
[ui-oss]: https://devblogs.microsoft.com/dotnet/announcing-net-core-3-preview-1-and-open-sourcing-windows-desktop-frameworks/
