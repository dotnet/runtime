Developer Guide
===============

This guide provides instructions (mostly as links) on how to build the repo and implement improvements. It will expand over time.

Building the repository
=======================

The CoreFX repo can be built from a regular, non-admin command prompt. The build produces multiple managed binaries that make up the CoreFX libraries and the accompanying tests. The repo can be built for the following platforms, using the provided instructions.

| Chip  | Windows | Linux | OS X | FreeBSD |
| :---- | :-----: | :---: | :--: | :--: |
| x64   | &#x25CF;| &#x25D2;| &#x25D2;| &#x25D2;|
| x86   | &#x25EF;| &#x25EF;| &#x25EF;| &#x25EF;|
| ARM32 | &#x25EF;| &#x25EF;| &#x25EF;| &#x25EF;|
|       | [Instructions](../building/windows-instructions.md) | [Instructions](../building/unix-instructions.md) | [Instructions](../building/unix-instructions.md) | [Instructions](../building/unix-instructions.md) |


The CoreFX build and test suite is a work in progress, as are the [building and testing instructions](../README.md). The .NET Core team and the community are improving Linux and OS X support on a daily basis are and adding more tests for all platforms. See [CoreFX Issues](https://github.com/dotnet/corefx/issues) to find out about specific work items or report issues.
