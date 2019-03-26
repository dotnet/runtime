# Porting to .NET Core

The purpose of this document is to share the plans on how we're going to port more APIs to .NET Core.

## Summary

Compared to other .NET stacks, specifically the .NET Framework, Mono, Unity, and Xamarin the .NET Core platform doesn't expose enough APIs. This makes porting assets to .NET Core quite challenging, especially for areas which don't have replacements.

So we want to bring more APIs to .NET Core. In order to keep our promise of being open and transparent, we want to:

* Clearly communicate what we plan on porting, how we prioritize, what we don't want to port, and how we're thinking of cross-platform and compatibility.
* Avoid being the bottleneck by allowing community members to help with porting.

## Prioritization

As of today, .NET Core supports three app models:

* **ASP.NET apps and services**
* **Console apps**
* **Universal Windows Platform (UWP) apps**

We'll prioritize APIs that affect the ability to write those kind of apps over app models that .NET Core currently doesn't support (such as building desktop applications). Please note that this doesn't mean that we'll never port the other APIs -- it just means those rank lower.

The same is true for platforms: the implementation of .NET Framework only runs on Windows. Our goal for .NET Core is to support the following platforms:

* Windows
* OS X
* Linux

(Please note that the specific version numbers, editions, and distros aren't fully finalized yet.)

While we try to be mindful of other ports actively happening (such a BSD Unix), we prioritize those three platforms. It's also worth pointing out that we generally don't prioritize within these platforms; in other words we consider them all as equals. Of course, we start from a Windows implementation so the support is currently more complete there. Also, we're mindful of the ways developers are most likely to use the platforms as well. For instance, many web developers debug and test on OS X but deploy to Linux. This guides us when prioritizing work to address point-in-time issues.

We determine which APIs are critical by leveraging various data sources:

* **Community feedback** (such as GitHub, UserVoice)
* **Partner feedback** (1st party and 3rd party applications)
* **API usage of existing applications** (such as submissions from API Port, ASP.NET applications, and Silverlight based phone apps)

We obviously aren't going to use a fixed formula to determine the stack rank of an API. We'll also leverage the experience from long term members on the team and combine it with the data and feedback to make intelligent decisions. For the sake of transparency we'll share all the data we have and the factors that lead to a specific decision. So if a decision we make is problematic for a part of the .NET ecosystem, such as not including the interfaces of ADO.NET for ORM providers, we can adjust as we go.

## Mechanics

For the most part, the source code for .NET Framework and .NET Core is disjoint. In other words, porting code to .NET Core isn't an exercise of checking a box -- it requires active work.

Here is the summary of the activities involved:

* Decide which APIs should go into .NET Core
* Assess implications on componentization (see guidelines for more details)
* Assess cross-platform impact. If necessary, find out how, and even if, the current design can be supported on all supported platforms
* Get the code onto GitHub (this includes some minor clean up, such as running the code formatting tool)
* Produce packages
* Port corresponding tests

## Porting Guidelines

Here are the high level guidelines:

* **Do not** port APIs of technologies we don't want to support moving forward (see list below).
* **Do not** port APIs marked as obsolete.
* **Consider** porting APIs even if considered legacy, problematic, or otherwise inadequate.
* **Do** factor assemblies appropriately in order to preserve the componentization, specifically:
	- **Do not** add dependencies from non-legacy to legacy components.
	- **Do not** expose Windows-only technologies in otherwise fully portable assemblies.
	- **Do** factor large chunks of functionality that cannot be supported everywhere into their own assemblies.
	- **Do** consider using tester-doer patterns if only a very small number of APIs cannot be supported in all environments.
* **Consider** using extension methods and partial facades to accelerate bringing revised APIs to the .NET Framework.
* **Avoid** making changes that result in loss of binary and/or source compatibility between the .NET Framework and .NET Core.
* **Avoid** "franken-designs" with extension methods and partial facades purely for the sake of .NET Framework compatibility.

## Unsupported Technologies

Feature owners reserve the right to call out what they don't want to support on .NET Core. Since .NET Core is a new platform, we don't want to penalize our ability to build a componentized and cross-platform stack by signing up for technologies that are expensive to bring into this world and we don't think are sensible for today's needs.

This list, while not complete, is meant as a reference point. We'll add to it as we refine our porting plan. Also, just because something is currently not implemented, doesn't imply it's intentionally unsupported. Feel free to [file an issue](https://github.com/dotnet/corefx/issues/new) to ask for specific APIs and technologies. Porting requests are generally marked as [port-to-core](https://github.com/dotnet/corefx/issues?q=is%3Aopen+is%3Aissue+label%3Aport-to-core).

Binary Serialization is supported post 1.1, however we do not support cross-platform binary serialization. For new code you may want to consider other serialization approaches such as data contract serialization, XML serialization, JSON.NET, and protobuf-net.

Technology                 | More information
---------------------------|-----------------------------------
AppDomains                 | [Details](#app_domains)
Remoting                   | [Details](#remoting)
Code Access Security (CAS) | [Details](#code-access-security-cas)
Security Transparency      | [Details](#security-transparency)

### App Domains

**Justification**. AppDomains require runtime support and are generally quite expensive. They are not implemented in .NET Core or .NET Native. We do not plan on adding this capability in future.

**Replacement**. AppDomains were used for different features; for isolation we recommend processes and/or containers. For dynamic loading, we provide `AssemblyLoadContext`. Information (such as the name and base directory) is provided by APIs on other types, for instance `AppContext.BaseDirectory`. Some scenarios, such as getting the list of loaded assemblies are unsupported as they are inherently fragile.

To make code migration from .NET Framework easier, we have exposed some of the `AppDomain` API surface in .NET Core and .NET Native. Some of the APIs work fine (e.g. `UnhandledException`), some of them do nothing (e.g. `SetCachePath`) and some of them throw `PlatformNotSupportedException` (e.g. `CreateDomain`). See more details about particular API differences in [TODO](https://github.com/dotnet/corefx/issues/18405).

### Remoting

**Justification**. The idea of .NET remoting -- which is transparent remote procedure calls -- has been identified as a problematic architecture. Outside of that realm, it's also used for cross AppDomain communication which is no longer supported. On top of that, remoting requires runtime support and is quite heavyweight.

**Replacement**. For communication across processes, inter-process communication (IPC) should be used, such as pipes or memory mapped files. Across machines, you should use a network based solution, preferably `System.Net.Sockets` (see sample - [TODO](https://github.com/dotnet/corefx/issues/18394)).

### Code Access Security (CAS)

**Justification**. Sandboxing, i.e. relying on the runtime or the framework to constrain which resources a managed application or library can run is [not supported on .NET Framework](https://msdn.microsoft.com/en-us/library/c5tk9z76(v=vs.110).aspx) and therefore is also not supported on .NET Core or .NET Native. We believe that there are simply too many pieces in the .NET Framework and runtime that can result in elevation of privileges. Thus we don't treat [CAS as security boundary](https://msdn.microsoft.com/en-us/library/c5tk9z76(v=vs.110).aspx) anymore. On top of that, it makes the implementation more complicated and often has correctness performance implications for applications that don’t intend to use it.

**Replacement**. Use operating system provided security boundaries, such as virtualization, containers, or user accounts for running processes with the least set of privileges.

### Security Transparency

**Justification**. Similar to CAS, this feature allows separating sandboxed code from security critical code in a declarative fashion, but is [no longer supported as a security boundary](https://msdn.microsoft.com/en-us/library/ee191569(v=vs.110).aspx). This feature was heavily used by Silverlight. 

**Replacement**. Use operating system provided security boundaries, such as virtualization, containers, or user accounts for running processes with the least set of privileges.
