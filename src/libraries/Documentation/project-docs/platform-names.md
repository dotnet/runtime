# Platform Names

Over the years, we've accumulated quite a few names, platforms, and components that can make it hard for folks (including us) to understand what we're referring to. This document has a list that will help to qualify what we mean by what.

This will also list some aliases. As you'll see the aliases aren't always correct -- they are merely listed to help you find the better and less confusing terminology.

## .NET Framework

**Also referred to as**: Desktop, full framework, in-box framework

This refers to the .NET Framework that first shipped in 2002 and is now up to version 4.6.1. It's the main framework folks target today and allows you to build a wide variety of applications, such as WinForms, WPF, ASP.NET, and command line tools.

The .NET Framework was designed to run on Windows only. Some versions of the .NET Framework come pre-installed with Windows, some require to be installed. However, in both cases the .NET Framework is a system-wide component. Application do not include .NET Framework DLLs when deploying; the correct .NET version must be on the machine.

## .NET Core

**Also referred to as**: UWP, ~~Store~~

Originally, .NET Core was the identifier we used to describe the .NET APIs Windows 8 store applications could use. When we designed the API set, we wanted to create a foundation for .NET where portability is a first class concern for the layering and componentization. For more details, read [this blog post](http://blogs.msdn.com/b/dotnet/archive/2014/12/04/introducing-net-core.aspx).

Today, .NET Core no longer just for store applications. .NET Core is the name for the open source, cross-platform stack that ASP.NET Core and UWP applications are built on. The stack includes a set of framework libraries (CoreFX), a JIT based runtime (CoreCLR), an AOT based runtime (CoreRT), and a set of tooling (such as the dotnet CLI).

That's why referring to .NET Core as 'Store' is no longer correct. But you can think of today's .NET Core as an evolution of the original APIs available for store applications. Many of the original design goals are still relevant, especially around layering and portability.

## Universal Windows Platform (UWP)

**Also referred to as**: Store, WinRT, Metro

The Universal Windows Platform (UWP) is the platform that is used for building modern, touch-enabled Windows applications as well as headless devices for Internet of Things (IoT). It's designed to unify the different types of devices that you may want to target, including PCs, tablets, phablets, phones, and even the Xbox.

UWP provides many services, such as a centralized app store, an execution environment (AppContainer), and a set of Windows APIs to use instead of Win32 (WinRT). UWP has no dependancy on .NET, apps can be written in C++, C#, VB.NET, and JavaScript. When using C# and VB.NET the ".NET" APIs are provided by .NET Core.

## .NET Native

**Also referred to as**: ahead-of-time (AOT), IL compiler (ILC)

.NET Native is a compiler tool chain that will produce native code ahead-of-time (AOT), as opposed to just-in-time (JIT). The compilation can happen on the developer machine as well as on the store side, which allows blending AOT with the benefits of servicing.

You can think of .NET Native as an evolution of NGEN: NGEN basically simply runs the JIT up front, the code quality and behavior is identical to the JITed version. Another downside of NGEN is that it happens on the user's machine, rather than the developer's machine. NGEN is also at the module level, i.e. for each MSIL assembly there is a corresponding NEGEN'ed assembly that contains the native code. .NET Native on the other hand is a C++ like compiler and linker. It will remove unused code, spend more time optimizing it, and produce a single, merged module that represents the closure of the application.

UWP was the first application model that was supported by .NET Native. We now also support building native console applications for Windows, Mac and Linux.

## Rotor

**Also referred to as**: Shared Source Common Language Infrastructure (SSCLI)

Pretty much at the same time the .NET Framework was released, Microsoft also published Rotor, which is the source code for an implementation of ECMA 335 (Common Language Infrastructure), which is the specification behind .NET.

While parts of the source were identical with the .NET Framework, many pieces had prototypic implementations instead: the purpose of Rotor wasn't to provide a production ready .NET implementation but to provide a platform for research, academia, and validation that the ECMA 335 specification itself can be implemented.

It's also worth pointing out that the source code of Rotor was not an open source license (i.e. not approved by OSI) and has not been offically updated since .NET Framework 2.0.

## Mono

Mono is an open source alternative to the .NET Framework. Mono started around the same time the .NET Framework was first released. Since Microsoft didn't release Rotor as open source, Mono was forced to start from scratch and is thus a complete re-implementation of the .NET Framework with no shared code.

When .NET Core was released under the MIT license, Microsoft also released large chunks of the .NET Framework under the MIT license as well, which can be found [here](https://github.com/microsoft/referencesource). This enabled the Mono community to use the same code the .NET Framework uses in order to close gaps and avoid behavioral differences.

Mono is primarily used to run .NET application on Linux and Mac OS X (though to get into the Mac App Store you need Xamarin, see below). There are ports of Mono to other platforms including PlayStation and Blackberry.

Mono has implementations (though not necessarily complete) of WinForms, ASP.NET, and System.Drawing.

## Xamarin

Xamarin is a commercial offering for building mobile applications targeting Android, iOS and Mac OS X Store. It's based on Mono, and on iOS and Android surfaces a different API profile, called the mobile profile. The subsetting was necessary to reduce the footprint, both by shipping smaller versions of the system libraries as well as making them more linker friendly. While Mono runs on Mac OS X without Xamarin, they're linker is required make the app package for the Mac App Store.  Xamarin ships a full static compiler on iOS, as the platform does not support dynamic code generation.
