Cross-Platform Guidelines
=========================

This page provides a FAQ for how we handle cross-platform code in CoreFX. (For structuring of interop code, see [interop guidelines](interop-guidelines.md).)

#### Should assemblies be binary-compatible across platforms (e.g. exact same System.IO.dll on Windows, Linux, and Mac)?

Our expectation is that the majority (estimating around 70%) of CoreFX assemblies will have no platform-specific code. These assemblies should be binary-compatible across platforms.

In some cases, the managed binary will be used across all platforms, but it'll come with its own native library that'll be compiled once per platform.

In a few dozen cases, the managed code itself will have differing implementations based on whether you're building for Windows, Linux, etc., and in such cases, the binary will not work from one platform to the next. Which binary gets used will be handled by the NuGet package delivering the libraries.

#### When should an existing platform-specific .NET API be deprecated or removed in favor of a new approach?

It's a case-by-case basis. In some cases, entire contracts that are platform-specific just won't be available on other platforms, as they don't make sense by their very nature (e.g. Microsoft.Win32.Registry.dll). In other cases, a contract will be available, but some members here and there that are platform-specific may throw PlatformNotSupportedException (e.g. Console.get_ForegroundColor on Unix). In general, though, we want to strive for having any APIs that exist on a platform (i.e. the contract is available) actually working on that platform.

#### When should partial classes be used to layer in platform-specific functionality?

Partial classes is the approach we're currently taking when the managed code needs to diverge based on underlying platform. There are a few cases where we've decided to go a different route, but even in some of those cases we may move back towards partial classes.

#### How should the platform-specific files be named (e.g. FileStream.Windows.cs? Win32FileStream.cs?)

When the whole type is for a particular platform, we've been using the prefix, e.g. PlatformFileStream.cs. When the file contains a partial class specialized for a particular platform, we've been using the *.Platform.cs suffix.

#### When should define statements be used rather than including different source files in the build environment?

We're striving to avoid defines whenever possible, instead preferring to include just the source files that are relevant.
