Developer Guide
===============

This guide provides instructions (mostly as links) on how to build the repo and implement improvements. It will expand over time.

Building the repository
=======================

The CoreCLR repo can be built from a regular, non-admin command prompt. The build produces CoreCLR (multiple native binaries), the mscorlib managed library and the accompanying tests. The repo can be built for the following platforms, using the provided instructions.

| Chip  | Windows | Linux | OS X |
| :---- | :-----: | :---: | :--: |
| x64   | &#x25CF;| &#x25D2;| &#x25D2; |
| x86   | &#x25EF;| &#x25EF;| &#x25EF;|
| ARM32 | &#x25EF; | &#x25EF;| &#x25EF; |
|       | [Instructions](../building/windows-instructions.md) | [Instructions](../building/linux-instructions.md) | [Instructions](../building/osx-instructions.md) |  

The CoreCLR build and test suite is a work in progress, as are the [building and testing instructions](../README.md). The .NET Core team and the community are improving Linux and OS X support on a daily basis and are adding more tests for all platforms. See [CoreCLR Issues](https://github.com/dotnet/coreclr/issues) to find out about specific work items or report issues.

Understanding the TFS-Git Mirror
================================

The Microsoft team maintains a Microsoft-internal TFS server of CoreCLR. An automated system is used to flow changes in/out of GitHub. The mirroring infrastructure uses the following hint files to mirror a given TFS folder into GitHub and back:

1. `.gitmirror` - any folder containing this file will **only** have its contained files mirrored. Subfolders are **not** mirrored.
2. `.gitmirrorall` - any folder containing this file will have all of its files **and** subfolders mirrored recursively. The subfolders do not need to have any hint files.

Thus, if you add a new folder to be included as part of the CoreCLR build, it will also need to have one of the two hint files mentioned above.
