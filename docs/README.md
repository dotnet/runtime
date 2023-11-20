Documents Index
===============

This repo includes several documents that explain both high-level and low-level concepts about the .NET runtime and libraries. These are very useful for contributors, to get context that can be very difficult to acquire from just reading code.

Intro to .NET
==================

.NET is a self-contained .NET runtime and framework that implements ECMA 335. It can be (and has been) ported to multiple architectures and platforms. It support a variety of installation options, having no specific deployment requirements itself.

Getting Started
===============

- [Installing the .NET SDK](https://dotnet.microsoft.com/download)
- [Official .NET Docs](https://docs.microsoft.com/dotnet/core/)

Workflow (Building, testing, benchmarking, profiling, etc.)
===============

If you want to contribute a code change to this repo, start here.

- [Workflow Instructions](workflow/README.md)

Design Docs
=================

- [.NET Globalization Invariant Mode](design/features/globalization-invariant-mode.md)
- [WASM Globalization Icu](design/features/globalization-icu-wasm.md)
- Many more under [design/features](design/features/)

The Book of the Runtime is a set of chapters that go in depth into various
interesting aspects of the design of the .NET Framework.

- [Book of the Runtime](design/coreclr/botr/README.md)

For your convenience, here are a few quick links to popular chapters:

- [Introduction to the Common Language Runtime](design/coreclr/botr/intro-to-clr.md)
- [Garbage Collection Design](design/coreclr/botr/garbage-collection.md)
- [Type System](design/coreclr/botr/type-system.md)

For additional information, see this list of blog posts that provide a ['deep-dive' into the CoreCLR source code](deep-dive-blog-posts.md)

Coding Guidelines
=================

- [CLR Coding Guide](coding-guidelines/clr-code-guide.md)
- [CLR JIT Coding Conventions](coding-guidelines/clr-jit-coding-conventions.md)
- [Cross Platform Performance and Eventing Design](coding-guidelines/cross-platform-performance-and-eventing.md)
- [Adding New Events to the VM](coding-guidelines/EventLogging.md)
- [C# coding style](coding-guidelines/coding-style.md)
- [Framework Design Guidelines](coding-guidelines/framework-design-guidelines-digest.md)
- [Cross-Platform Guidelines](coding-guidelines/cross-platform-guidelines.md)
- [Performance Guidelines](coding-guidelines/performance-guidelines.md)
- [Interop Guidelines](coding-guidelines/interop-guidelines.md)
- [Breaking Changes](coding-guidelines/breaking-changes.md)
- [Breaking Change Definitions](coding-guidelines/breaking-change-definitions.md)
- [Breaking Change Rules](coding-guidelines/breaking-change-rules.md)
- [Project Guidelines](coding-guidelines/project-guidelines.md)
- [Adding APIs Guidelines](coding-guidelines/adding-api-guidelines.md)

Project Docs
=================

To be added. Visit the [project docs folder](project/) directly meanwhile.

Other Information
=================

- [.NET Glossary](project/glossary.md)
- [.NET Filename Encyclopedia](project/dotnet-filenames.md)
- [Porting to .NET Core](https://docs.microsoft.com/en-us/dotnet/standard/analyzers/portability-analyzer)
- [.NET Standards (Ecma)](project/dotnet-standards.md)
- [CLR Configuration Knobs](../src/coreclr/inc/clrconfigvalues.h)
- [CLR overview](https://docs.microsoft.com/dotnet/standard/clr)
- [Wikipedia Entry for the CLR](https://en.wikipedia.org/wiki/Common_Language_Runtime)
