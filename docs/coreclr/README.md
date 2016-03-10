Documents Index
===============

This repo includes several documents that explain both high-level and low-level concepts about the .NET runtime. These are very useful for contributors, to get context that can be very difficult to acquire from just reading code.

Intro to .NET Core
==================

.NET Core is a self-contained .NET runtime and framework that implements [ECMA 335](project-docs/dotnet-standards.md). It can be (and has been) ported to multiple architectures and platforms. It supports a variety of installation options, having no specific deployment requirements itself.

Learn about .NET Core
====================

- [[WIP] Official .NET Core Docs](http://dotnet.github.io/docs/)

Get .NET Core
=============

- [Get .NET Core DNX SDK on Windows](install/get-dotnetcore-dnx-windows.md)
- [Get .NET Core DNX SDK on OS X](install/get-dotnetcore-dnx-osx.md)
- [Get .NET Core DNX SDK on Linux](install/get-dotnetcore-dnx-linux.md)
- [Get .NET Core (Raw) on Windows](install/get-dotnetcore-windows.md)

Project Docs
============

- [Developer Guide](project-docs/developer-guide.md)
- [Project priorities](project-docs/project-priorities.md)
- [Contributing to .NET Core](project-docs/contributing.md)
- [Contributing Workflow](project-docs/contributing-workflow.md)
- [Performance Guidelines](project-docs/performance-guidelines.md)
- [Garbage Collector Guidelines](project-docs/garbage-collector-guidelines.md)
- [Adding new public APIs to mscorlib](project-docs/adding_new_public_apis.md)
- [Project NuGet Dependencies](https://github.com/dotnet/corefx/blob/master/Documentation/project-docs/project-nuget-dependencies.md)

Coding Guidelines
=================

- [CLR Coding Guide](coding-guidelines/clr-code-guide.md)
- [CLR JIT Coding Conventions](coding-guidelines/clr-jit-coding-conventions.md)
- [Cross Platform Performance and Eventing Design](coding-guidelines/cross-platform-performance-and-eventing.md)
- [Adding New Events to the VM](coding-guidelines/EventLogging.md)

Build CoreCLR from Source
=========================

- [Building CoreCLR on FreeBSD](building/freebsd-instructions.md)
- [Building CoreCLR on Linux](building/linux-instructions.md)
- [Building CoreCLR on OS X](building/osx-instructions.md)
- [Building CoreCLR on Windows](building/windows-instructions.md)

Testing and Debugging CoreCLR
=============================

- [Debugging CoreCLR](building/debugging-instructions.md)
- [Testing Changes on Windows](building/windows-test-instructions.md)
- [Testing Changes on Linux, OS X, and FreeBSD](building/unix-test-instructions.md)
- [Testing with CoreFX](building/testing-with-corefx.md)
- [.NET Performance Data Collection Script](https://raw.githubusercontent.com/dotnet/corefx-tools/master/src/performance/perfcollect/perfcollect)

Book of the Runtime
===================

- [Book of the Runtime FAQ](botr/botr-faq.md)
- [Introduction to the Common Language Runtime](botr/intro-to-clr.md)
- [Garbage Collection Design](botr/garbage-collection.md)
- [Threading](botr/threading.md)
- [RyuJIT Overview](botr/ryujit-overview.md)
- [Type System](botr/type-system.md)
- [Type Loader](botr/type-loader.md)
- [Method Descriptor](botr/method-descriptor.md)
- [Virtual Stub Dispatch](botr/virtual-stub-dispatch.md)
- [Stack Walking](botr/stackwalking.md)
- [Mscorlib and Calling Into the Runtime](botr/mscorlib.md)
- [Data Access Component (DAC) Notes](botr/dac-notes.md)
- [Profiling](botr/profiling.md)
- [Implementing Profilability](botr/profilability.md)
- [What Every Dev needs to Know About Exceptions in the Runtime](botr/exceptions.md)
- [ReadyToRun Overview](botr/readytorun-overview.md)

Decoder Rings
=============

- [.NET Core Glossary](project-docs/glossary.md)
- [.NET Filename Encyclopedia](project-docs/dotnet-filenames.md)

Other Information
=================

- [CoreFX Repo documentation](https://github.com/dotnet/corefx/tree/master/Documentation)
- [Porting to .NET Core](https://github.com/dotnet/corefx/blob/master/Documentation/project-docs/support-dotnet-core-instructions.md)
- [.NET Standards (Ecma)](project-docs/dotnet-standards.md)
- [CLR Configuration Knobs](project-docs/clr-configuration-knobs.md)
- [MSDN Entry for the CLR](http://msdn.microsoft.com/library/8bs2ecf4.aspx)
- [Wikipedia Entry for the CLR](http://en.wikipedia.org/wiki/Common_Language_Runtime)
