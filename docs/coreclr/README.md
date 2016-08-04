Documents Index
===============

This repo includes several documents that explain both high-level and low-level concepts about the .NET runtime. These are very useful for contributors, to get context that can be very difficult to acquire from just reading code.

Intro to .NET Core
==================

.NET Core is a self-contained .NET runtime and framework that implements [ECMA 335](project-docs/dotnet-standards.md). It can be (and has been) ported to multiple architectures and platforms. It supports a variety of installation options, having no specific deployment requirements itself.

Getting Started
===============

- [Installing the .NET Core SDK](https://www.microsoft.com/net/core)
- [[WIP] Official .NET Core Docs](http://dotnet.github.io/docs/)

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
- [Performance Tracing on Windows](project-docs/windows-performance-tracing.md)
- [Performance Tracing on Linux](project-docs/linux-performance-tracing.md)
- [Creating native images](building/crossgen.md)

Book of the Runtime
===================

The Book of the Runtime is a set of chapters that go in depth into various 
interesting aspects of the design of the .NET Framework.  

- [Book of the Runtime](botr/_tableOfContents.md)

For your convenience, here are a few quick links to popular chapters:

- [Introduction to the Common Language Runtime](botr/intro-to-clr.md)
- [Garbage Collection Design](botr/garbage-collection.md)
- [Type System](botr/type-system.md)

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
