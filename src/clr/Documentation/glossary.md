.NET Core Glossary
===

This glossary defines terms, both common and more niche, that are important to understand when reading .NET Core documents and source code. They are also often used by .NET Core team members and other contributers when conversing on GitHub (issues, PRs), on twitter and other sites.

As much as possible, we should link to the most authoritative and recent source of information for a term. That approach should be the most helpful for people who want to learn more about a topic.

* CLR: Common Language Runtime
* COR: [Common Object Runtime](http://www.danielmoth.com/Blog/mscorlibdll.aspx). The name of .NET before it was named .NET.
* DAC: Data Access Component. An abstraction layer over the internal structures in the runtime.
* EE: Execution Engine.
* PAL: [Platform Adaptation Layer](http://archive.oreilly.com/pub/a/dotnet/2002/03/04/rotor.html). Provides an abstraction layer between the runtime and the operating system
* SOS: [Son of Strike](http://blogs.msdn.com/b/jasonz/archive/2003/10/21/53581.aspx). The debugging extension for DbgEng based debuggers. Uses the DAC as an abstraction layer for its operation.
* SVR: The CLR used to be built as two variants, with one called "mscorsvr.dll", to mean the "server" version. In particular, it contained the server GC implementation, which was intended for multi-threaded apps capable of taking advantage of multiple processors. In the .NET Framework 2 release, the two variants were merged into "mscorwks.dll". The WKS version was the default, however the SVR version remained available.
* URT: Universal Runtime. Ancient name for what ended up being .NET, is used in the WinError facility name FACILITY_URT.
* VM: Virtual machine.
* WKS: The CLR used to be built as two variants, with one called "mscorwks.dll", to mean the "workstation" version. In particular, it contained the client GC implementation, which was intended for single-threaded apps, independent of how many processors were on the machine. In the .NET Framework 2 release, the two variants were merged into "mscorwks.dll". The WKS version was the default, however the SVR version remained available.