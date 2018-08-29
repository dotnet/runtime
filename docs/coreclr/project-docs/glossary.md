.NET Core Glossary
===

This glossary defines terms, both common and more niche, that are important to understand when reading .NET Core documents and source code. They are also often used by .NET Core team members and other contributors when conversing on GitHub (issues, PRs), on twitter and other sites.

As much as possible, we should link to the most authoritative and recent source of information for a term. That approach should be the most helpful for people who want to learn more about a topic.

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
| GC | [Garbage Collector](https://github.com/dotnet/coreclr/blob/master/Documentation/botr/garbage-collection.md). |
| IPC | Inter-Process Communication. |
| JIT | [Just-in-Time](https://github.com/dotnet/coreclr/blob/master/Documentation/botr/ryujit-overview.md) compiler. RyuJIT is the code name for the next generation Just-in-Time(aka "JIT") for the .NET runtime. |
| LCG | Lightweight Code Generation. An early name for [dynamic methods](https://github.com/dotnet/coreclr/blob/master/src/System.Private.CoreLib/src/System/Reflection/Emit/DynamicMethod.cs). |
| MD | MetaData. |
| MDA | Managed Debugging Assistant - see [details](https://docs.microsoft.com/en-us/dotnet/framework/debug-trace-profile/diagnosing-errors-with-managed-debugging-assistants) (Note: Not in .NET Core, equivalent diagnostic functionality is made available on a case-by-case basis, e.g. [#15465](https://github.com/dotnet/coreclr/issues/15465)) |
| NGen | Native Image Generator. |
| NYI | Not Yet Implemented. |
| PAL | [Platform Adaptation Layer](http://archive.oreilly.com/pub/a/dotnet/2002/03/04/rotor.html). Provides an abstraction layer between the runtime and the operating system. |
| PE | Portable Executable. |
| PGO | Profile Guided Optimization - see [details](https://blogs.msdn.microsoft.com/vcblog/2008/11/12/pogo/). |
| POGO | Profile Guided Optimization - see [details](https://blogs.msdn.microsoft.com/vcblog/2008/11/12/pogo/). |
| ProjectN | Codename for the first version of [.NET Native for UWP](https://msdn.microsoft.com/en-us/vstudio/dotnetnative.aspx). |
| R2R | Ready-to-Run. A flavor of native images - command line switch of [crossgen](../building/crossgen.md). |
| Redhawk | Codename for experimental minimal managed code runtime that evolved into [CoreRT](https://github.com/dotnet/corert/). |
| SOS | [Son of Strike](http://blogs.msdn.com/b/jasonz/archive/2003/10/21/53581.aspx). The debugging extension for DbgEng based debuggers. Uses the DAC as an abstraction layer for its operation. |
| SuperPMI | JIT component test framework (super fast JIT testing - it mocks/replays EE in EE-JIT interface) - see [SuperPMI details](https://github.com/dotnet/coreclr/blob/master/src/ToolBox/superpmi/readme.txt). |
| SVR | The CLR used to be built as two variants, with one called "mscorsvr.dll", to mean the "server" version. In particular, it contained the server GC implementation, which was intended for multi-threaded apps capable of taking advantage of multiple processors. In the .NET Framework 2 release, the two variants were merged into "mscorwks.dll". The WKS version was the default, however the SVR version remained available. |
| TPA | Trusted Platform Assemblies used to be a special set of assemblies that comprised the platform assemblies, when it was originally designed. As of today, it is simply the set of assemblies known to constitute the application. |
| URT | Universal Runtime. Ancient name for what ended up being .NET, is used in the WinError facility name FACILITY_URT. |
| UTC | [Universal Tuple Compiler](https://blogs.msdn.microsoft.com/vcblog/2013/06/12/optimizing-c-code-overview/). The Microsoft C++ optimizer back-end that that starts by converting the information from the FrontEnd into tuples – a binary stream of instructions. |
| UWP | [Universal Windows Platform (UWP)](https://docs.microsoft.com/en-us/windows/uwp/get-started/universal-application-platform-guide) is a platform-homogeneous application architecture available on every device that runs Windows 10. |
| VSD | [Virtual Stub Dispatch](../botr/virtual-stub-dispatch.md). Technique of using stubs for virtual method invocations instead of the traditional virtual method table. |
| VM | Virtual machine. |
| WKS | The CLR used to be built as two variants, with one called "mscorwks.dll", to mean the "workstation" version. In particular, it contained the client GC implementation, which was intended for single-threaded apps, independent of how many processors were on the machine. In the .NET Framework 2 release, the two variants were merged into "mscorwks.dll". The WKS version was the default, however the SVR version remained available. |
| ZAP | Original code name for NGen. |
