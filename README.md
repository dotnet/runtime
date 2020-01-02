# .NET Runtime

[![Build Status](https://dnceng.visualstudio.com/public/_apis/build/status/dotnet/runtime/runtime-libraries?branchName=master)](https://dnceng.visualstudio.com/public/_build/latest?definitionId=647&branchName=master)
[![Gitter](https://badges.gitter.im/Join%20Chat.svg)](https://gitter.im/dotnet/runtime)

This repo contains the code to build the .NET Core runtime, libraries and shared host (`dotnet`) installers for
all supported platforms, as well as the sources to .NET Core runtime and libraries.

## What is .NET Core?

Official Starting Page: https://dotnet.microsoft.com/

* [How to use .NET](https://docs.microsoft.com/dotnet/core/get-started) (with VS, VS Code, command-line CLI)
  * [Install official releases](https://dotnet.microsoft.com/download)
  * [Documentation](https://docs.microsoft.com/dotnet/core) (Get Started, Tutorials, Porting from .NET Framework, API reference, ...)
    * [Deploying apps](https://docs.microsoft.com/dotnet/core/deploying)
  * [Supported OS versions](https://github.com/dotnet/core/blob/master/os-lifecycle-policy.md)
* [Roadmap](https://github.com/dotnet/core/blob/master/roadmap.md)
* [Releases](https://github.com/dotnet/core/tree/master/release-notes)

## How can I contribute?

We welcome contributions! Many people all over the world have helped make this project better.

* [Contributing](CONTRIBUTING.md) explains what kinds of changes we welcome
- [Workflow Instructions](docs/workflow/README.md) explains how to build and test

## Reporting security issues and security bugs

Security issues and bugs should be reported privately, via email, to the Microsoft Security Response Center (MSRC) <secure@microsoft.com>. You should receive a response within 24 hours. If for some reason you do not, please follow up via email to ensure we received your original message. Further information, including the MSRC PGP key, can be found in the [Security TechCenter](https://www.microsoft.com/msrc/faqs-report-an-issue).

Also see info about related [Microsoft .NET Core and ASP.NET Core Bug Bounty Program](https://www.microsoft.com/msrc/bounty-dot-net-core).

## Filing issues

This repo should contain issues that are tied to the runtime, the class libraries and frameworks, the installation of the `dotnet` binary (sometimes known as the `muxer`) and installation of the .NET Core runtime and libraries.

For other issues, please use the following repos:

- For overall .NET Core SDK issues, file in the [dotnet/toolset](https://github.com/dotnet/toolset) repo
- For ASP.NET issues, file in the [aspnet/aspnetcore](http://github.com/aspnet/aspnetcore) repo.

## Useful Links

* [.NET Core source index](https://source.dot.net) / [.NET Framework source index](https://referencesource.microsoft.com)
* [API Reference docs](https://docs.microsoft.com/dotnet/api/?view=netcore-3.0)
* [.NET API Catalog](http://apisof.net) (incl. APIs from daily builds and API usage info)
* [API docs writing guidelines](https://github.com/dotnet/dotnet-api-docs/wiki) - useful when writing /// comments

## Daily Builds

<!--
  To update this table, run 'build.sh/cmd /p:Subset=RegenerateReadmeTable'. See
  'tools-local/regenerate-readme-table.proj' to add or remove rows or columns,
  and add links below to fill out the table's contents.
-->
<!-- BEGIN generated table -->

| Platform | Master | Release/3.0.X | Release/2.2.X | Release/2.1.X |
| --- |  :---: | :---: | :---: | :---: |
| **Windows (x64)** | [![][win-x64-badge-master]][win-x64-version-master]<br>[Installer][win-x64-installer-master] ([Checksum][win-x64-installer-checksum-master])<br>[zip][win-x64-zip-master] ([Checksum][win-x64-zip-checksum-master])<br>[NetHost (zip)][win-x64-nethost-zip-master]<br>[Symbols (zip)][win-x64-symbols-zip-master] | [![][win-x64-badge-3.0.X]][win-x64-version-3.0.X]<br>[Installer][win-x64-installer-3.0.X] ([Checksum][win-x64-installer-checksum-3.0.X])<br>[zip][win-x64-zip-3.0.X] ([Checksum][win-x64-zip-checksum-3.0.X])<br>[NetHost (zip)][win-x64-nethost-zip-3.0.X]<br>[Symbols (zip)][win-x64-symbols-zip-3.0.X] | [![][win-x64-badge-2.2.X]][win-x64-version-2.2.X]<br>[Installer][win-x64-installer-2.2.X] ([Checksum][win-x64-installer-checksum-2.2.X])<br>[zip][win-x64-zip-2.2.X] ([Checksum][win-x64-zip-checksum-2.2.X])<br>[Symbols (zip)][win-x64-symbols-zip-2.2.X] | [![][win-x64-badge-2.1.X]][win-x64-version-2.1.X]<br>[Installer][win-x64-installer-2.1.X] ([Checksum][win-x64-installer-checksum-2.1.X])<br>[zip][win-x64-zip-2.1.X] ([Checksum][win-x64-zip-checksum-2.1.X])<br>[Symbols (zip)][win-x64-symbols-zip-2.1.X] |
| **Windows (x86)** | [![][win-x86-badge-master]][win-x86-version-master]<br>[Installer][win-x86-installer-master] ([Checksum][win-x86-installer-checksum-master])<br>[zip][win-x86-zip-master] ([Checksum][win-x86-zip-checksum-master])<br>[NetHost (zip)][win-x86-nethost-zip-master]<br>[Symbols (zip)][win-x86-symbols-zip-master] | [![][win-x86-badge-3.0.X]][win-x86-version-3.0.X]<br>[Installer][win-x86-installer-3.0.X] ([Checksum][win-x86-installer-checksum-3.0.X])<br>[zip][win-x86-zip-3.0.X] ([Checksum][win-x86-zip-checksum-3.0.X])<br>[NetHost (zip)][win-x86-nethost-zip-3.0.X]<br>[Symbols (zip)][win-x86-symbols-zip-3.0.X] | [![][win-x86-badge-2.2.X]][win-x86-version-2.2.X]<br>[Installer][win-x86-installer-2.2.X] ([Checksum][win-x86-installer-checksum-2.2.X])<br>[zip][win-x86-zip-2.2.X] ([Checksum][win-x86-zip-checksum-2.2.X])<br>[Symbols (zip)][win-x86-symbols-zip-2.2.X] | [![][win-x86-badge-2.1.X]][win-x86-version-2.1.X]<br>[Installer][win-x86-installer-2.1.X] ([Checksum][win-x86-installer-checksum-2.1.X])<br>[zip][win-x86-zip-2.1.X] ([Checksum][win-x86-zip-checksum-2.1.X])<br>[Symbols (zip)][win-x86-symbols-zip-2.1.X] |
| **Windows (arm32)** | [![][win-arm-badge-master]][win-arm-version-master]<br>[zip][win-arm-zip-master] ([Checksum][win-arm-zip-checksum-master])<br>[NetHost (zip)][win-arm-nethost-zip-master]<br>[Symbols (zip)][win-arm-symbols-zip-master] | [![][win-arm-badge-3.0.X]][win-arm-version-3.0.X]<br>[zip][win-arm-zip-3.0.X] ([Checksum][win-arm-zip-checksum-3.0.X])<br>[NetHost (zip)][win-arm-nethost-zip-3.0.X]<br>[Symbols (zip)][win-arm-symbols-zip-3.0.X] | [![][win-arm-badge-2.2.X]][win-arm-version-2.2.X]<br>[zip][win-arm-zip-2.2.X] ([Checksum][win-arm-zip-checksum-2.2.X])<br>[Symbols (zip)][win-arm-symbols-zip-2.2.X] | [![][win-arm-badge-2.1.X]][win-arm-version-2.1.X]<br>[zip][win-arm-zip-2.1.X] ([Checksum][win-arm-zip-checksum-2.1.X])<br>[Symbols (zip)][win-arm-symbols-zip-2.1.X] |
| **Windows (arm64)** | [![][win-arm64-badge-master]][win-arm64-version-master]<br>[zip][win-arm64-zip-master] ([Checksum][win-arm64-zip-checksum-master])<br>[NetHost (zip)][win-arm64-nethost-zip-master]<br>[Symbols (zip)][win-arm64-symbols-zip-master] | [![][win-arm64-badge-3.0.X]][win-arm64-version-3.0.X]<br>[zip][win-arm64-zip-3.0.X] ([Checksum][win-arm64-zip-checksum-3.0.X])<br>[NetHost (zip)][win-arm64-nethost-zip-3.0.X]<br>[Symbols (zip)][win-arm64-symbols-zip-3.0.X] | [![][win-arm64-badge-2.2.X]][win-arm64-version-2.2.X]<br>[zip][win-arm64-zip-2.2.X] ([Checksum][win-arm64-zip-checksum-2.2.X])<br>[Symbols (zip)][win-arm64-symbols-zip-2.2.X] | [![][win-arm64-badge-2.1.X]][win-arm64-version-2.1.X]<br>[zip][win-arm64-zip-2.1.X] ([Checksum][win-arm64-zip-checksum-2.1.X])<br>[Symbols (zip)][win-arm64-symbols-zip-2.1.X] |
| **Mac OS X (x64)** | [![][osx-badge-master]][osx-version-master]<br>[Installer][osx-installer-master] ([Checksum][osx-installer-checksum-master])<br>[tar.gz][osx-targz-master] ([Checksum][osx-targz-checksum-master])<br>[NetHost (tar.gz)][osx-nethost-targz-master]<br>[Symbols (tar.gz)][osx-symbols-targz-master] | [![][osx-badge-3.0.X]][osx-version-3.0.X]<br>[Installer][osx-installer-3.0.X] ([Checksum][osx-installer-checksum-3.0.X])<br>[tar.gz][osx-targz-3.0.X] ([Checksum][osx-targz-checksum-3.0.X])<br>[NetHost (tar.gz)][osx-nethost-targz-3.0.X]<br>[Symbols (tar.gz)][osx-symbols-targz-3.0.X] | [![][osx-badge-2.2.X]][osx-version-2.2.X]<br>[Installer][osx-installer-2.2.X] ([Checksum][osx-installer-checksum-2.2.X])<br>[tar.gz][osx-targz-2.2.X] ([Checksum][osx-targz-checksum-2.2.X])<br>[Symbols (tar.gz)][osx-symbols-targz-2.2.X] | [![][osx-badge-2.1.X]][osx-version-2.1.X]<br>[Installer][osx-installer-2.1.X] ([Checksum][osx-installer-checksum-2.1.X])<br>[tar.gz][osx-targz-2.1.X] ([Checksum][osx-targz-checksum-2.1.X])<br>[Symbols (tar.gz)][osx-symbols-targz-2.1.X] |
| **Linux (x64)** (for glibc based OS) | [![][linux-x64-badge-master]][linux-x64-version-master]<br>[tar.gz][linux-x64-targz-master] ([Checksum][linux-x64-targz-checksum-master])<br>[NetHost (tar.gz)][linux-x64-nethost-targz-master]<br>[Symbols (tar.gz)][linux-x64-symbols-targz-master] | [![][linux-x64-badge-3.0.X]][linux-x64-version-3.0.X]<br>[tar.gz][linux-x64-targz-3.0.X] ([Checksum][linux-x64-targz-checksum-3.0.X])<br>[NetHost (tar.gz)][linux-x64-nethost-targz-3.0.X]<br>[Symbols (tar.gz)][linux-x64-symbols-targz-3.0.X] | [![][linux-x64-badge-2.2.X]][linux-x64-version-2.2.X]<br>[tar.gz][linux-x64-targz-2.2.X] ([Checksum][linux-x64-targz-checksum-2.2.X])<br>[Symbols (tar.gz)][linux-x64-symbols-targz-2.2.X] | [![][linux-x64-badge-2.1.X]][linux-x64-version-2.1.X]<br>[tar.gz][linux-x64-targz-2.1.X] ([Checksum][linux-x64-targz-checksum-2.1.X])<br>[Symbols (tar.gz)][linux-x64-symbols-targz-2.1.X] |
| **Linux (armhf)** (for glibc based OS) | [![][linux-arm-badge-master]][linux-arm-version-master]<br>[tar.gz][linux-arm-targz-master] ([Checksum][linux-arm-targz-checksum-master])<br>[NetHost (tar.gz)][linux-arm-nethost-targz-master]<br>[Symbols (tar.gz)][linux-arm-symbols-targz-master] | [![][linux-arm-badge-3.0.X]][linux-arm-version-3.0.X]<br>[tar.gz][linux-arm-targz-3.0.X] ([Checksum][linux-arm-targz-checksum-3.0.X])<br>[NetHost (tar.gz)][linux-arm-nethost-targz-3.0.X]<br>[Symbols (tar.gz)][linux-arm-symbols-targz-3.0.X] | [![][linux-arm-badge-2.2.X]][linux-arm-version-2.2.X]<br>[tar.gz][linux-arm-targz-2.2.X] ([Checksum][linux-arm-targz-checksum-2.2.X])<br>[Symbols (tar.gz)][linux-arm-symbols-targz-2.2.X] | [![][linux-arm-badge-2.1.X]][linux-arm-version-2.1.X]<br>[tar.gz][linux-arm-targz-2.1.X] ([Checksum][linux-arm-targz-checksum-2.1.X])<br>[Symbols (tar.gz)][linux-arm-symbols-targz-2.1.X] |
| **Linux (arm64)** (for glibc based OS) | [![][linux-arm64-badge-master]][linux-arm64-version-master]<br>[tar.gz][linux-arm64-targz-master] ([Checksum][linux-arm64-targz-checksum-master])<br>[NetHost (tar.gz)][linux-arm64-nethost-targz-master]<br>[Symbols (tar.gz)][linux-arm64-symbols-targz-master] | [![][linux-arm64-badge-3.0.X]][linux-arm64-version-3.0.X]<br>[tar.gz][linux-arm64-targz-3.0.X] ([Checksum][linux-arm64-targz-checksum-3.0.X])<br>[NetHost (tar.gz)][linux-arm64-nethost-targz-3.0.X]<br>[Symbols (tar.gz)][linux-arm64-symbols-targz-3.0.X] | [![][linux-arm64-badge-2.2.X]][linux-arm64-version-2.2.X]<br>[tar.gz][linux-arm64-targz-2.2.X] ([Checksum][linux-arm64-targz-checksum-2.2.X])<br>[Symbols (tar.gz)][linux-arm64-symbols-targz-2.2.X] | [![][linux-arm64-badge-2.1.X]][linux-arm64-version-2.1.X]<br>[tar.gz][linux-arm64-targz-2.1.X] ([Checksum][linux-arm64-targz-checksum-2.1.X])<br>[Symbols (tar.gz)][linux-arm64-symbols-targz-2.1.X] |
| **Ubuntu 14.04 (x64)** | [![][ubuntu-14.04-badge-master]][ubuntu-14.04-version-master]<br>[Runtime-Deps][ubuntu-14.04-runtime-deps-master] ([Checksum][ubuntu-14.04-runtime-deps-checksum-master])<br>[Host][ubuntu-14.04-host-master] ([Checksum][ubuntu-14.04-host-checksum-master])<br>[Host FX Resolver][ubuntu-14.04-hostfxr-master] ([Checksum][ubuntu-14.04-hostfxr-checksum-master])<br>[Shared Framework][ubuntu-14.04-sharedfx-master] ([Checksum][ubuntu-14.04-sharedfx-checksum-master]) | [![][ubuntu-14.04-badge-3.0.X]][ubuntu-14.04-version-3.0.X]<br>[Runtime-Deps][ubuntu-14.04-runtime-deps-3.0.X] ([Checksum][ubuntu-14.04-runtime-deps-checksum-3.0.X])<br>[Host][ubuntu-14.04-host-3.0.X] ([Checksum][ubuntu-14.04-host-checksum-3.0.X])<br>[Host FX Resolver][ubuntu-14.04-hostfxr-3.0.X] ([Checksum][ubuntu-14.04-hostfxr-checksum-3.0.X])<br>[Shared Framework][ubuntu-14.04-sharedfx-3.0.X] ([Checksum][ubuntu-14.04-sharedfx-checksum-3.0.X]) | [![][ubuntu-14.04-badge-2.2.X]][ubuntu-14.04-version-2.2.X]<br>[Host][ubuntu-14.04-host-2.2.X] ([Checksum][ubuntu-14.04-host-checksum-2.2.X])<br>[Host FX Resolver][ubuntu-14.04-hostfxr-2.2.X] ([Checksum][ubuntu-14.04-hostfxr-checksum-2.2.X])<br>[Shared Framework][ubuntu-14.04-sharedfx-2.2.X] ([Checksum][ubuntu-14.04-sharedfx-checksum-2.2.X]) | [![][ubuntu-14.04-badge-2.1.X]][ubuntu-14.04-version-2.1.X]<br>[Host][ubuntu-14.04-host-2.1.X] ([Checksum][ubuntu-14.04-host-checksum-2.1.X])<br>[Host FX Resolver][ubuntu-14.04-hostfxr-2.1.X] ([Checksum][ubuntu-14.04-hostfxr-checksum-2.1.X])<br>[Shared Framework][ubuntu-14.04-sharedfx-2.1.X] ([Checksum][ubuntu-14.04-sharedfx-checksum-2.1.X]) |
| **Ubuntu 16.04 (x64)** | [![][ubuntu-16.04-badge-master]][ubuntu-16.04-version-master]<br>[Runtime-Deps][ubuntu-16.04-runtime-deps-master] ([Checksum][ubuntu-16.04-runtime-deps-checksum-master])<br>[Host][ubuntu-16.04-host-master] ([Checksum][ubuntu-16.04-host-checksum-master])<br>[Host FX Resolver][ubuntu-16.04-hostfxr-master] ([Checksum][ubuntu-16.04-hostfxr-checksum-master])<br>[Shared Framework][ubuntu-16.04-sharedfx-master] ([Checksum][ubuntu-16.04-sharedfx-checksum-master]) | [![][ubuntu-16.04-badge-3.0.X]][ubuntu-16.04-version-3.0.X]<br>[Runtime-Deps][ubuntu-16.04-runtime-deps-3.0.X] ([Checksum][ubuntu-16.04-runtime-deps-checksum-3.0.X])<br>[Host][ubuntu-16.04-host-3.0.X] ([Checksum][ubuntu-16.04-host-checksum-3.0.X])<br>[Host FX Resolver][ubuntu-16.04-hostfxr-3.0.X] ([Checksum][ubuntu-16.04-hostfxr-checksum-3.0.X])<br>[Shared Framework][ubuntu-16.04-sharedfx-3.0.X] ([Checksum][ubuntu-16.04-sharedfx-checksum-3.0.X]) | [![][ubuntu-16.04-badge-2.2.X]][ubuntu-16.04-version-2.2.X]<br>[Host][ubuntu-16.04-host-2.2.X] ([Checksum][ubuntu-16.04-host-checksum-2.2.X])<br>[Host FX Resolver][ubuntu-16.04-hostfxr-2.2.X] ([Checksum][ubuntu-16.04-hostfxr-checksum-2.2.X])<br>[Shared Framework][ubuntu-16.04-sharedfx-2.2.X] ([Checksum][ubuntu-16.04-sharedfx-checksum-2.2.X]) | [![][ubuntu-16.04-badge-2.1.X]][ubuntu-16.04-version-2.1.X]<br>[Host][ubuntu-16.04-host-2.1.X] ([Checksum][ubuntu-16.04-host-checksum-2.1.X])<br>[Host FX Resolver][ubuntu-16.04-hostfxr-2.1.X] ([Checksum][ubuntu-16.04-hostfxr-checksum-2.1.X])<br>[Shared Framework][ubuntu-16.04-sharedfx-2.1.X] ([Checksum][ubuntu-16.04-sharedfx-checksum-2.1.X]) |
| **Ubuntu 18.04 (x64)** | [![][ubuntu-18.04-badge-master]][ubuntu-18.04-version-master]<br>[Runtime-Deps][ubuntu-18.04-runtime-deps-master] ([Checksum][ubuntu-18.04-runtime-deps-checksum-master])<br>[Host][ubuntu-18.04-host-master] ([Checksum][ubuntu-18.04-host-checksum-master])<br>[Host FX Resolver][ubuntu-18.04-hostfxr-master] ([Checksum][ubuntu-18.04-hostfxr-checksum-master])<br>[Shared Framework][ubuntu-18.04-sharedfx-master] ([Checksum][ubuntu-18.04-sharedfx-checksum-master]) | [![][ubuntu-18.04-badge-3.0.X]][ubuntu-18.04-version-3.0.X]<br>[Runtime-Deps][ubuntu-18.04-runtime-deps-3.0.X] ([Checksum][ubuntu-18.04-runtime-deps-checksum-3.0.X])<br>[Host][ubuntu-18.04-host-3.0.X] ([Checksum][ubuntu-18.04-host-checksum-3.0.X])<br>[Host FX Resolver][ubuntu-18.04-hostfxr-3.0.X] ([Checksum][ubuntu-18.04-hostfxr-checksum-3.0.X])<br>[Shared Framework][ubuntu-18.04-sharedfx-3.0.X] ([Checksum][ubuntu-18.04-sharedfx-checksum-3.0.X]) | [![][ubuntu-18.04-badge-2.2.X]][ubuntu-18.04-version-2.2.X]<br>[Runtime-Deps][ubuntu-18.04-runtime-deps-2.2.X] ([Checksum][ubuntu-18.04-runtime-deps-checksum-2.2.X])<br>[Host][ubuntu-18.04-host-2.2.X] ([Checksum][ubuntu-18.04-host-checksum-2.2.X])<br>[Host FX Resolver][ubuntu-18.04-hostfxr-2.2.X] ([Checksum][ubuntu-18.04-hostfxr-checksum-2.2.X])<br>[Shared Framework][ubuntu-18.04-sharedfx-2.2.X] ([Checksum][ubuntu-18.04-sharedfx-checksum-2.2.X]) | [![][ubuntu-18.04-badge-2.1.X]][ubuntu-18.04-version-2.1.X]<br>[Runtime-Deps][ubuntu-18.04-runtime-deps-2.1.X] ([Checksum][ubuntu-18.04-runtime-deps-checksum-2.1.X])<br>[Host][ubuntu-18.04-host-2.1.X] ([Checksum][ubuntu-18.04-host-checksum-2.1.X])<br>[Host FX Resolver][ubuntu-18.04-hostfxr-2.1.X] ([Checksum][ubuntu-18.04-hostfxr-checksum-2.1.X])<br>[Shared Framework][ubuntu-18.04-sharedfx-2.1.X] ([Checksum][ubuntu-18.04-sharedfx-checksum-2.1.X]) |
| **Ubuntu 19.04 (x64)** | [![][ubuntu-19.04-badge-master]][ubuntu-19.04-version-master]<br>[Runtime-Deps][ubuntu-19.04-runtime-deps-master] ([Checksum][ubuntu-19.04-runtime-deps-checksum-master])<br>[Host][ubuntu-19.04-host-master] ([Checksum][ubuntu-19.04-host-checksum-master])<br>[Host FX Resolver][ubuntu-19.04-hostfxr-master] ([Checksum][ubuntu-19.04-hostfxr-checksum-master])<br>[Shared Framework][ubuntu-19.04-sharedfx-master] ([Checksum][ubuntu-19.04-sharedfx-checksum-master]) | [![][ubuntu-19.04-badge-3.0.X]][ubuntu-19.04-version-3.0.X]<br>[Runtime-Deps][ubuntu-19.04-runtime-deps-3.0.X] ([Checksum][ubuntu-19.04-runtime-deps-checksum-3.0.X])<br>[Host][ubuntu-19.04-host-3.0.X] ([Checksum][ubuntu-19.04-host-checksum-3.0.X])<br>[Host FX Resolver][ubuntu-19.04-hostfxr-3.0.X] ([Checksum][ubuntu-19.04-hostfxr-checksum-3.0.X])<br>[Shared Framework][ubuntu-19.04-sharedfx-3.0.X] ([Checksum][ubuntu-19.04-sharedfx-checksum-3.0.X]) | [![][ubuntu-19.04-badge-2.2.X]][ubuntu-19.04-version-2.2.X]<br>[Runtime-Deps][ubuntu-19.04-runtime-deps-2.2.X] ([Checksum][ubuntu-19.04-runtime-deps-checksum-2.2.X])<br>[Host][ubuntu-19.04-host-2.2.X] ([Checksum][ubuntu-19.04-host-checksum-2.2.X])<br>[Host FX Resolver][ubuntu-19.04-hostfxr-2.2.X] ([Checksum][ubuntu-19.04-hostfxr-checksum-2.2.X])<br>[Shared Framework][ubuntu-19.04-sharedfx-2.2.X] ([Checksum][ubuntu-19.04-sharedfx-checksum-2.2.X]) | [![][ubuntu-19.04-badge-2.1.X]][ubuntu-19.04-version-2.1.X]<br>[Runtime-Deps][ubuntu-19.04-runtime-deps-2.1.X] ([Checksum][ubuntu-19.04-runtime-deps-checksum-2.1.X])<br>[Host][ubuntu-19.04-host-2.1.X] ([Checksum][ubuntu-19.04-host-checksum-2.1.X])<br>[Host FX Resolver][ubuntu-19.04-hostfxr-2.1.X] ([Checksum][ubuntu-19.04-hostfxr-checksum-2.1.X])<br>[Shared Framework][ubuntu-19.04-sharedfx-2.1.X] ([Checksum][ubuntu-19.04-sharedfx-checksum-2.1.X]) |
| **Debian 8.2 (x64)** | [![][debian-8.2-badge-master]][debian-8.2-version-master]<br>[Runtime-Deps][debian-8.2-runtime-deps-master] ([Checksum][debian-8.2-runtime-deps-checksum-master])<br>[Host][debian-8.2-host-master] ([Checksum][debian-8.2-host-checksum-master])<br>[Host FX Resolver][debian-8.2-hostfxr-master] ([Checksum][debian-8.2-hostfxr-checksum-master])<br>[Shared Framework][debian-8.2-sharedfx-master] ([Checksum][debian-8.2-sharedfx-checksum-master]) | [![][debian-8.2-badge-3.0.X]][debian-8.2-version-3.0.X]<br>[Runtime-Deps][debian-8.2-runtime-deps-3.0.X] ([Checksum][debian-8.2-runtime-deps-checksum-3.0.X])<br>[Host][debian-8.2-host-3.0.X] ([Checksum][debian-8.2-host-checksum-3.0.X])<br>[Host FX Resolver][debian-8.2-hostfxr-3.0.X] ([Checksum][debian-8.2-hostfxr-checksum-3.0.X])<br>[Shared Framework][debian-8.2-sharedfx-3.0.X] ([Checksum][debian-8.2-sharedfx-checksum-3.0.X]) | [![][debian-8.2-badge-2.2.X]][debian-8.2-version-2.2.X]<br>[Runtime-Deps][debian-8.2-runtime-deps-2.2.X] ([Checksum][debian-8.2-runtime-deps-checksum-2.2.X])<br>[Host][debian-8.2-host-2.2.X] ([Checksum][debian-8.2-host-checksum-2.2.X])<br>[Host FX Resolver][debian-8.2-hostfxr-2.2.X] ([Checksum][debian-8.2-hostfxr-checksum-2.2.X])<br>[Shared Framework][debian-8.2-sharedfx-2.2.X] ([Checksum][debian-8.2-sharedfx-checksum-2.2.X]) | [![][debian-8.2-badge-2.1.X]][debian-8.2-version-2.1.X]<br>[Runtime-Deps][debian-8.2-runtime-deps-2.1.X] ([Checksum][debian-8.2-runtime-deps-checksum-2.1.X])<br>[Host][debian-8.2-host-2.1.X] ([Checksum][debian-8.2-host-checksum-2.1.X])<br>[Host FX Resolver][debian-8.2-hostfxr-2.1.X] ([Checksum][debian-8.2-hostfxr-checksum-2.1.X])<br>[Shared Framework][debian-8.2-sharedfx-2.1.X] ([Checksum][debian-8.2-sharedfx-checksum-2.1.X]) |
| **Debian 9 (x64)** | [![][debian-9-badge-master]][debian-9-version-master]<br>[Runtime-Deps][debian-9-runtime-deps-master] ([Checksum][debian-9-runtime-deps-checksum-master])<br>[Host][debian-9-host-master] ([Checksum][debian-9-host-checksum-master])<br>[Host FX Resolver][debian-9-hostfxr-master] ([Checksum][debian-9-hostfxr-checksum-master])<br>[Shared Framework][debian-9-sharedfx-master] ([Checksum][debian-9-sharedfx-checksum-master]) | [![][debian-9-badge-3.0.X]][debian-9-version-3.0.X]<br>[Runtime-Deps][debian-9-runtime-deps-3.0.X] ([Checksum][debian-9-runtime-deps-checksum-3.0.X])<br>[Host][debian-9-host-3.0.X] ([Checksum][debian-9-host-checksum-3.0.X])<br>[Host FX Resolver][debian-9-hostfxr-3.0.X] ([Checksum][debian-9-hostfxr-checksum-3.0.X])<br>[Shared Framework][debian-9-sharedfx-3.0.X] ([Checksum][debian-9-sharedfx-checksum-3.0.X]) | [![][debian-9-badge-2.2.X]][debian-9-version-2.2.X]<br>[Runtime-Deps][debian-9-runtime-deps-2.2.X] ([Checksum][debian-9-runtime-deps-checksum-2.2.X])<br>[Host][debian-9-host-2.2.X] ([Checksum][debian-9-host-checksum-2.2.X])<br>[Host FX Resolver][debian-9-hostfxr-2.2.X] ([Checksum][debian-9-hostfxr-checksum-2.2.X])<br>[Shared Framework][debian-9-sharedfx-2.2.X] ([Checksum][debian-9-sharedfx-checksum-2.2.X]) | [![][debian-9-badge-2.1.X]][debian-9-version-2.1.X]<br>[Runtime-Deps][debian-9-runtime-deps-2.1.X] ([Checksum][debian-9-runtime-deps-checksum-2.1.X])<br>[Host][debian-9-host-2.1.X] ([Checksum][debian-9-host-checksum-2.1.X])<br>[Host FX Resolver][debian-9-hostfxr-2.1.X] ([Checksum][debian-9-hostfxr-checksum-2.1.X])<br>[Shared Framework][debian-9-sharedfx-2.1.X] ([Checksum][debian-9-sharedfx-checksum-2.1.X]) |
| **CentOS 7 (x64)** | [![][centos-7-badge-master]][centos-7-version-master]<br>[Runtime-Deps][centos-7-runtime-deps-master] ([Checksum][centos-7-runtime-deps-checksum-master])<br>[Host][centos-7-host-master] ([Checksum][centos-7-host-checksum-master])<br>[Host FX Resolver][centos-7-hostfxr-master] ([Checksum][centos-7-hostfxr-checksum-master])<br>[Shared Framework][centos-7-sharedfx-master] ([Checksum][centos-7-sharedfx-checksum-master]) | [![][centos-7-badge-3.0.X]][centos-7-version-3.0.X]<br>[Runtime-Deps][centos-7-runtime-deps-3.0.X] ([Checksum][centos-7-runtime-deps-checksum-3.0.X])<br>[Host][centos-7-host-3.0.X] ([Checksum][centos-7-host-checksum-3.0.X])<br>[Host FX Resolver][centos-7-hostfxr-3.0.X] ([Checksum][centos-7-hostfxr-checksum-3.0.X])<br>[Shared Framework][centos-7-sharedfx-3.0.X] ([Checksum][centos-7-sharedfx-checksum-3.0.X]) | [![][centos-7-badge-2.2.X]][centos-7-version-2.2.X]<br>[Runtime-Deps][centos-7-runtime-deps-2.2.X] ([Checksum][centos-7-runtime-deps-checksum-2.2.X])<br>[Host][centos-7-host-2.2.X] ([Checksum][centos-7-host-checksum-2.2.X])<br>[Host FX Resolver][centos-7-hostfxr-2.2.X] ([Checksum][centos-7-hostfxr-checksum-2.2.X])<br>[Shared Framework][centos-7-sharedfx-2.2.X] ([Checksum][centos-7-sharedfx-checksum-2.2.X]) | [![][centos-7-badge-2.1.X]][centos-7-version-2.1.X]<br>[Runtime-Deps][centos-7-runtime-deps-2.1.X] ([Checksum][centos-7-runtime-deps-checksum-2.1.X])<br>[Host][centos-7-host-2.1.X] ([Checksum][centos-7-host-checksum-2.1.X])<br>[Host FX Resolver][centos-7-hostfxr-2.1.X] ([Checksum][centos-7-hostfxr-checksum-2.1.X])<br>[Shared Framework][centos-7-sharedfx-2.1.X] ([Checksum][centos-7-sharedfx-checksum-2.1.X]) |
| **RHEL 6** | [![][rhel-6-badge-master]][rhel-6-version-master]<br>[tar.gz][rhel-6-targz-master] | [![][rhel-6-badge-3.0.X]][rhel-6-version-3.0.X]<br>[tar.gz][rhel-6-targz-3.0.X] | [![][rhel-6-badge-2.2.X]][rhel-6-version-2.2.X]<br>[tar.gz][rhel-6-targz-2.2.X] | [![][rhel-6-badge-2.1.X]][rhel-6-version-2.1.X]<br>[tar.gz][rhel-6-targz-2.1.X] |
| **RHEL 7.2 (x64)** | [![][rhel7-badge-master]][rhel7-version-master]<br>[Host][rhel7-host-master] ([Checksum][rhel7-host-checksum-master])<br>[Host FX Resolver][rhel7-hostfxr-master] ([Checksum][rhel7-hostfxr-checksum-master])<br>[Shared Framework][rhel7-sharedfx-master] ([Checksum][rhel7-sharedfx-checksum-master]) | [![][rhel7-badge-3.0.X]][rhel7-version-3.0.X]<br>[Host][rhel7-host-3.0.X] ([Checksum][rhel7-host-checksum-3.0.X])<br>[Host FX Resolver][rhel7-hostfxr-3.0.X] ([Checksum][rhel7-hostfxr-checksum-3.0.X])<br>[Shared Framework][rhel7-sharedfx-3.0.X] ([Checksum][rhel7-sharedfx-checksum-3.0.X]) | [![][rhel7-badge-2.2.X]][rhel7-version-2.2.X]<br>[Host][rhel7-host-2.2.X] ([Checksum][rhel7-host-checksum-2.2.X])<br>[Host FX Resolver][rhel7-hostfxr-2.2.X] ([Checksum][rhel7-hostfxr-checksum-2.2.X])<br>[Shared Framework][rhel7-sharedfx-2.2.X] ([Checksum][rhel7-sharedfx-checksum-2.2.X]) | [![][rhel7-badge-2.1.X]][rhel7-version-2.1.X]<br>[Host][rhel7-host-2.1.X] ([Checksum][rhel7-host-checksum-2.1.X])<br>[Host FX Resolver][rhel7-hostfxr-2.1.X] ([Checksum][rhel7-hostfxr-checksum-2.1.X])<br>[Shared Framework][rhel7-sharedfx-2.1.X] ([Checksum][rhel7-sharedfx-checksum-2.1.X]) |
| **Fedora 27 (x64)** | [![][fedora-27-badge-master]][fedora-27-version-master]<br>[Runtime-Deps][fedora-27-runtime-deps-master] ([Checksum][fedora-27-runtime-deps-checksum-master])<br>[Host][fedora-27-host-master] ([Checksum][fedora-27-host-checksum-master])<br>[Host FX Resolver][fedora-27-hostfxr-master] ([Checksum][fedora-27-hostfxr-checksum-master])<br>[Shared Framework][fedora-27-sharedfx-master] ([Checksum][fedora-27-sharedfx-checksum-master]) | [![][fedora-27-badge-3.0.X]][fedora-27-version-3.0.X]<br>[Runtime-Deps][fedora-27-runtime-deps-3.0.X] ([Checksum][fedora-27-runtime-deps-checksum-3.0.X])<br>[Host][fedora-27-host-3.0.X] ([Checksum][fedora-27-host-checksum-3.0.X])<br>[Host FX Resolver][fedora-27-hostfxr-3.0.X] ([Checksum][fedora-27-hostfxr-checksum-3.0.X])<br>[Shared Framework][fedora-27-sharedfx-3.0.X] ([Checksum][fedora-27-sharedfx-checksum-3.0.X]) | [![][fedora-27-badge-2.2.X]][fedora-27-version-2.2.X]<br>[Runtime-Deps][fedora-27-runtime-deps-2.2.X] ([Checksum][fedora-27-runtime-deps-checksum-2.2.X])<br>[Host][fedora-27-host-2.2.X] ([Checksum][fedora-27-host-checksum-2.2.X])<br>[Host FX Resolver][fedora-27-hostfxr-2.2.X] ([Checksum][fedora-27-hostfxr-checksum-2.2.X])<br>[Shared Framework][fedora-27-sharedfx-2.2.X] ([Checksum][fedora-27-sharedfx-checksum-2.2.X]) | [![][fedora-27-badge-2.1.X]][fedora-27-version-2.1.X]<br>[Runtime-Deps][fedora-27-runtime-deps-2.1.X] ([Checksum][fedora-27-runtime-deps-checksum-2.1.X])<br>[Host][fedora-27-host-2.1.X] ([Checksum][fedora-27-host-checksum-2.1.X])<br>[Host FX Resolver][fedora-27-hostfxr-2.1.X] ([Checksum][fedora-27-hostfxr-checksum-2.1.X])<br>[Shared Framework][fedora-27-sharedfx-2.1.X] ([Checksum][fedora-27-sharedfx-checksum-2.1.X]) |
| **SLES 12 (x64)** | [![][sles-12-badge-master]][sles-12-version-master]<br>[Runtime-Deps][sles-12-runtime-deps-master] ([Checksum][sles-12-runtime-deps-checksum-master])<br>[Host][sles-12-host-master] ([Checksum][sles-12-host-checksum-master])<br>[Host FX Resolver][sles-12-hostfxr-master] ([Checksum][sles-12-hostfxr-checksum-master])<br>[Shared Framework][sles-12-sharedfx-master] ([Checksum][sles-12-sharedfx-checksum-master]) | [![][sles-12-badge-3.0.X]][sles-12-version-3.0.X]<br>[Runtime-Deps][sles-12-runtime-deps-3.0.X] ([Checksum][sles-12-runtime-deps-checksum-3.0.X])<br>[Host][sles-12-host-3.0.X] ([Checksum][sles-12-host-checksum-3.0.X])<br>[Host FX Resolver][sles-12-hostfxr-3.0.X] ([Checksum][sles-12-hostfxr-checksum-3.0.X])<br>[Shared Framework][sles-12-sharedfx-3.0.X] ([Checksum][sles-12-sharedfx-checksum-3.0.X]) | [![][sles-12-badge-2.2.X]][sles-12-version-2.2.X]<br>[Runtime-Deps][sles-12-runtime-deps-2.2.X] ([Checksum][sles-12-runtime-deps-checksum-2.2.X])<br>[Host][sles-12-host-2.2.X] ([Checksum][sles-12-host-checksum-2.2.X])<br>[Host FX Resolver][sles-12-hostfxr-2.2.X] ([Checksum][sles-12-hostfxr-checksum-2.2.X])<br>[Shared Framework][sles-12-sharedfx-2.2.X] ([Checksum][sles-12-sharedfx-checksum-2.2.X]) | [![][sles-12-badge-2.1.X]][sles-12-version-2.1.X]<br>[Runtime-Deps][sles-12-runtime-deps-2.1.X] ([Checksum][sles-12-runtime-deps-checksum-2.1.X])<br>[Host][sles-12-host-2.1.X] ([Checksum][sles-12-host-checksum-2.1.X])<br>[Host FX Resolver][sles-12-hostfxr-2.1.X] ([Checksum][sles-12-hostfxr-checksum-2.1.X])<br>[Shared Framework][sles-12-sharedfx-2.1.X] ([Checksum][sles-12-sharedfx-checksum-2.1.X]) |
| **OpenSUSE 42 (x64)** | [![][OpenSUSE-42-badge-master]][OpenSUSE-42-version-master]<br>[Runtime-Deps][OpenSUSE-42-runtime-deps-master] ([Checksum][OpenSUSE-42-runtime-deps-checksum-master])<br>[Host][OpenSUSE-42-host-master] ([Checksum][OpenSUSE-42-host-checksum-master])<br>[Host FX Resolver][OpenSUSE-42-hostfxr-master] ([Checksum][OpenSUSE-42-hostfxr-checksum-master])<br>[Shared Framework][OpenSUSE-42-sharedfx-master] ([Checksum][OpenSUSE-42-sharedfx-checksum-master]) | [![][OpenSUSE-42-badge-3.0.X]][OpenSUSE-42-version-3.0.X]<br>[Runtime-Deps][OpenSUSE-42-runtime-deps-3.0.X] ([Checksum][OpenSUSE-42-runtime-deps-checksum-3.0.X])<br>[Host][OpenSUSE-42-host-3.0.X] ([Checksum][OpenSUSE-42-host-checksum-3.0.X])<br>[Host FX Resolver][OpenSUSE-42-hostfxr-3.0.X] ([Checksum][OpenSUSE-42-hostfxr-checksum-3.0.X])<br>[Shared Framework][OpenSUSE-42-sharedfx-3.0.X] ([Checksum][OpenSUSE-42-sharedfx-checksum-3.0.X]) | [![][OpenSUSE-42-badge-2.2.X]][OpenSUSE-42-version-2.2.X]<br>[Runtime-Deps][OpenSUSE-42-runtime-deps-2.2.X] ([Checksum][OpenSUSE-42-runtime-deps-checksum-2.2.X])<br>[Host][OpenSUSE-42-host-2.2.X] ([Checksum][OpenSUSE-42-host-checksum-2.2.X])<br>[Host FX Resolver][OpenSUSE-42-hostfxr-2.2.X] ([Checksum][OpenSUSE-42-hostfxr-checksum-2.2.X])<br>[Shared Framework][OpenSUSE-42-sharedfx-2.2.X] ([Checksum][OpenSUSE-42-sharedfx-checksum-2.2.X]) | [![][OpenSUSE-42-badge-2.1.X]][OpenSUSE-42-version-2.1.X]<br>[Runtime-Deps][OpenSUSE-42-runtime-deps-2.1.X] ([Checksum][OpenSUSE-42-runtime-deps-checksum-2.1.X])<br>[Host][OpenSUSE-42-host-2.1.X] ([Checksum][OpenSUSE-42-host-checksum-2.1.X])<br>[Host FX Resolver][OpenSUSE-42-hostfxr-2.1.X] ([Checksum][OpenSUSE-42-hostfxr-checksum-2.1.X])<br>[Shared Framework][OpenSUSE-42-sharedfx-2.1.X] ([Checksum][OpenSUSE-42-sharedfx-checksum-2.1.X]) |
| **Linux-musl (x64)** | [![][linux-musl-x64-badge-master]][linux-musl-x64-version-master]<br>[tar.gz][linux-musl-x64-targz-master] ([Checksum][linux-musl-x64-targz-checksum-master])<br>[NetHost (tar.gz)][linux-musl-x64-nethost-targz-master]<br>[Symbols (tar.gz)][linux-musl-x64-symbols-targz-master] | [![][linux-musl-x64-badge-3.0.X]][linux-musl-x64-version-3.0.X]<br>[tar.gz][linux-musl-x64-targz-3.0.X] ([Checksum][linux-musl-x64-targz-checksum-3.0.X])<br>[NetHost (tar.gz)][linux-musl-x64-nethost-targz-3.0.X]<br>[Symbols (tar.gz)][linux-musl-x64-symbols-targz-3.0.X] | [![][linux-musl-x64-badge-2.2.X]][linux-musl-x64-version-2.2.X]<br>[tar.gz][linux-musl-x64-targz-2.2.X] ([Checksum][linux-musl-x64-targz-checksum-2.2.X])<br>[Symbols (tar.gz)][linux-musl-x64-symbols-targz-2.2.X] | [![][linux-musl-x64-badge-2.1.X]][linux-musl-x64-version-2.1.X]<br>[tar.gz][linux-musl-x64-targz-2.1.X] ([Checksum][linux-musl-x64-targz-checksum-2.1.X])<br>[Symbols (tar.gz)][linux-musl-x64-symbols-targz-2.1.X] |
| **Linux-musl (arm64)** | [![][linux-musl-arm64-badge-master]][linux-musl-arm64-version-master]<br>[tar.gz][linux-musl-arm64-targz-master] ([Checksum][linux-musl-arm64-targz-checksum-master])<br>[NetHost (tar.gz)][linux-musl-arm64-nethost-targz-master]<br>[Symbols (tar.gz)][linux-musl-arm64-symbols-targz-master] | [![][linux-musl-arm64-badge-3.0.X]][linux-musl-arm64-version-3.0.X]<br>[tar.gz][linux-musl-arm64-targz-3.0.X] ([Checksum][linux-musl-arm64-targz-checksum-3.0.X])<br>[NetHost (tar.gz)][linux-musl-arm64-nethost-targz-3.0.X]<br>[Symbols (tar.gz)][linux-musl-arm64-symbols-targz-3.0.X] | N/A | N/A |

<!-- END generated table -->

*Note: Our Linux packages (.deb and .rpm) are put together slightly differently than the Windows and Mac specific installers. Instead of combining everything, we have separate component packages that depend on each other. If you're installing these directly from the installer files (via dpkg or similar), then you'll need to install them in the order presented above.*

<!-- BEGIN links to include in table -->

[win-x64-badge-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/sharedfx_win-x64_Release_version_badge.svg
[win-x64-version-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/latest.version
[win-x64-installer-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/dotnet-runtime-latest-win-x64.exe
[win-x64-installer-checksum-master]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/master/dotnet-runtime-latest-win-x64.exe.sha512
[win-x64-zip-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/dotnet-runtime-latest-win-x64.zip
[win-x64-zip-checksum-master]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/master/dotnet-runtime-latest-win-x64.zip.sha512
[win-x64-nethost-zip-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/dotnet-nethost-latest-win-x64.zip
[win-x64-symbols-zip-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/dotnet-runtime-symbols-latest-win-x64.zip

[win-x64-badge-3.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/3.0/sharedfx_win-x64_Release_version_badge.svg
[win-x64-version-3.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/3.0/latest.version
[win-x64-installer-3.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/3.0/dotnet-runtime-latest-win-x64.exe
[win-x64-installer-checksum-3.0.X]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/release/3.0/dotnet-runtime-latest-win-x64.exe.sha512
[win-x64-zip-3.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/3.0/dotnet-runtime-latest-win-x64.zip
[win-x64-zip-checksum-3.0.X]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/release/3.0/dotnet-runtime-latest-win-x64.zip.sha512
[win-x64-nethost-zip-3.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/3.0/dotnet-nethost-latest-win-x64.zip
[win-x64-symbols-zip-3.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/3.0/dotnet-runtime-symbols-latest-win-x64.zip

[win-x64-badge-2.2.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.2/sharedfx_win-x64_Release_version_badge.svg
[win-x64-version-2.2.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.2/latest.version
[win-x64-installer-2.2.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.2/dotnet-runtime-latest-win-x64.exe
[win-x64-installer-checksum-2.2.X]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/release/2.2/dotnet-runtime-latest-win-x64.exe.sha512
[win-x64-zip-2.2.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.2/dotnet-runtime-latest-win-x64.zip
[win-x64-zip-checksum-2.2.X]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/release/2.2/dotnet-runtime-latest-win-x64.zip.sha512
[win-x64-symbols-zip-2.2.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.2/dotnet-runtime-symbols-latest-win-x64.zip

[win-x64-badge-2.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.1/sharedfx_win-x64_Release_version_badge.svg
[win-x64-version-2.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.1/latest.version
[win-x64-installer-2.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.1/dotnet-runtime-latest-win-x64.exe
[win-x64-installer-checksum-2.1.X]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/release/2.1/dotnet-runtime-latest-win-x64.exe.sha512
[win-x64-zip-2.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.1/dotnet-runtime-latest-win-x64.zip
[win-x64-zip-checksum-2.1.X]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/release/2.1/dotnet-runtime-latest-win-x64.zip.sha512
[win-x64-symbols-zip-2.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.1/dotnet-runtime-symbols-latest-win-x64.zip


[win-x86-badge-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/sharedfx_win-x86_Release_version_badge.svg
[win-x86-version-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/latest.version
[win-x86-installer-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/dotnet-runtime-latest-win-x86.exe
[win-x86-installer-checksum-master]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/master/dotnet-runtime-latest-win-x86.exe.sha512
[win-x86-zip-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/dotnet-runtime-latest-win-x86.zip
[win-x86-zip-checksum-master]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/master/dotnet-runtime-latest-win-x86.zip.sha512
[win-x86-nethost-zip-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/dotnet-nethost-latest-win-x86.zip
[win-x86-symbols-zip-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/dotnet-runtime-symbols-latest-win-x86.zip

[win-x86-badge-3.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/3.0/sharedfx_win-x86_Release_version_badge.svg
[win-x86-version-3.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/3.0/latest.version
[win-x86-installer-3.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/3.0/dotnet-runtime-latest-win-x86.exe
[win-x86-installer-checksum-3.0.X]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/release/3.0/dotnet-runtime-latest-win-x86.exe.sha512
[win-x86-zip-3.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/3.0/dotnet-runtime-latest-win-x86.zip
[win-x86-zip-checksum-3.0.X]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/release/3.0/dotnet-runtime-latest-win-x86.zip.sha512
[win-x86-nethost-zip-3.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/3.0/dotnet-nethost-latest-win-x86.zip
[win-x86-symbols-zip-3.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/3.0/dotnet-runtime-symbols-latest-win-x86.zip

[win-x86-badge-2.2.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.2/sharedfx_win-x86_Release_version_badge.svg
[win-x86-version-2.2.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.2/latest.version
[win-x86-installer-2.2.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.2/dotnet-runtime-latest-win-x86.exe
[win-x86-installer-checksum-2.2.X]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/release/2.2/dotnet-runtime-latest-win-x86.exe.sha512
[win-x86-zip-2.2.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.2/dotnet-runtime-latest-win-x86.zip
[win-x86-zip-checksum-2.2.X]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/release/2.2/dotnet-runtime-latest-win-x86.zip.sha512
[win-x86-symbols-zip-2.2.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.2/dotnet-runtime-symbols-latest-win-x86.zip

[win-x86-badge-2.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.1/sharedfx_win-x86_Release_version_badge.svg
[win-x86-version-2.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.1/latest.version
[win-x86-installer-2.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.1/dotnet-runtime-latest-win-x86.exe
[win-x86-installer-checksum-2.1.X]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/release/2.1/dotnet-runtime-latest-win-x86.exe.sha512
[win-x86-zip-2.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.1/dotnet-runtime-latest-win-x86.zip
[win-x86-zip-checksum-2.1.X]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/release/2.1/dotnet-runtime-latest-win-x86.zip.sha512
[win-x86-symbols-zip-2.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.1/dotnet-runtime-symbols-latest-win-x86.zip


[win-arm-badge-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/sharedfx_win-arm_Release_version_badge.svg
[win-arm-version-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/latest.version
[win-arm-zip-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/dotnet-runtime-latest-win-arm.zip
[win-arm-zip-checksum-master]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/master/dotnet-runtime-latest-win-arm.zip.sha512
[win-arm-nethost-zip-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/dotnet-nethost-latest-win-arm.zip
[win-arm-symbols-zip-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/dotnet-runtime-symbols-latest-win-arm.zip

[win-arm-badge-3.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/3.0/sharedfx_win-arm_Release_version_badge.svg
[win-arm-version-3.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/3.0/latest.version
[win-arm-zip-3.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/3.0/dotnet-runtime-latest-win-arm.zip
[win-arm-zip-checksum-3.0.X]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/release/3.0/dotnet-runtime-latest-win-arm.zip.sha512
[win-arm-nethost-zip-3.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/3.0/dotnet-nethost-latest-win-arm.zip
[win-arm-symbols-zip-3.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/3.0/dotnet-runtime-symbols-latest-win-arm.zip

[win-arm-badge-2.2.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.2/sharedfx_win-arm_Release_version_badge.svg
[win-arm-version-2.2.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.2/latest.version
[win-arm-zip-2.2.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.2/dotnet-runtime-latest-win-arm.zip
[win-arm-zip-checksum-2.2.X]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/release/2.2/dotnet-runtime-latest-win-arm.zip.sha512
[win-arm-symbols-zip-2.2.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.2/dotnet-runtime-symbols-latest-win-arm.zip

[win-arm-badge-2.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.1/sharedfx_win-arm_Release_version_badge.svg
[win-arm-version-2.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.1/latest.version
[win-arm-zip-2.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.1/dotnet-runtime-latest-win-arm.zip
[win-arm-zip-checksum-2.1.X]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/release/2.1/dotnet-runtime-latest-win-arm.zip.sha512
[win-arm-symbols-zip-2.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.1/dotnet-runtime-symbols-latest-win-arm.zip


[win-arm64-badge-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/sharedfx_win-arm64_Release_version_badge.svg
[win-arm64-version-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/latest.version
[win-arm64-zip-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/dotnet-runtime-latest-win-arm64.zip
[win-arm64-zip-checksum-master]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/master/dotnet-runtime-latest-win-arm64.zip.sha512
[win-arm64-nethost-zip-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/dotnet-nethost-latest-win-arm64.zip
[win-arm64-symbols-zip-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/dotnet-runtime-symbols-latest-win-arm64.zip

[win-arm64-badge-3.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/3.0/sharedfx_win-arm64_Release_version_badge.svg
[win-arm64-version-3.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/3.0/latest.version
[win-arm64-zip-3.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/3.0/dotnet-runtime-latest-win-arm64.zip
[win-arm64-zip-checksum-3.0.X]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/release/3.0/dotnet-runtime-latest-win-arm64.zip.sha512
[win-arm64-nethost-zip-3.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/3.0/dotnet-nethost-latest-win-arm64.zip
[win-arm64-symbols-zip-3.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/3.0/dotnet-runtime-symbols-latest-win-arm64.zip

[win-arm64-badge-2.2.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.2/sharedfx_win-arm64_Release_version_badge.svg
[win-arm64-version-2.2.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.2/latest.version
[win-arm64-zip-2.2.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.2/dotnet-runtime-latest-win-arm64.zip
[win-arm64-zip-checksum-2.2.X]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/release/2.2/dotnet-runtime-latest-win-arm64.zip.sha512
[win-arm64-symbols-zip-2.2.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.2/dotnet-runtime-symbols-latest-win-arm64.zip

[win-arm64-badge-2.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.1/sharedfx_win-arm64_Release_version_badge.svg
[win-arm64-version-2.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.1/latest.version
[win-arm64-zip-2.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.1/dotnet-runtime-latest-win-arm64.zip
[win-arm64-zip-checksum-2.1.X]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/release/2.1/dotnet-runtime-latest-win-arm64.zip.sha512
[win-arm64-symbols-zip-2.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.1/dotnet-runtime-symbols-latest-win-arm64.zip


[osx-badge-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/sharedfx_osx-x64_Release_version_badge.svg
[osx-version-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/latest.version
[osx-installer-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/dotnet-runtime-latest-osx-x64.pkg
[osx-installer-checksum-master]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/master/dotnet-runtime-latest-osx-x64.pkg.sha512
[osx-targz-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/dotnet-runtime-latest-osx-x64.tar.gz
[osx-targz-checksum-master]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/master/dotnet-runtime-latest-osx-x64.tar.gz.sha512
[osx-nethost-targz-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/dotnet-nethost-latest-osx-x64.tar.gz
[osx-symbols-targz-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/dotnet-runtime-symbols-latest-osx-x64.tar.gz

[osx-badge-3.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/3.0/sharedfx_osx-x64_Release_version_badge.svg
[osx-version-3.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/3.0/latest.version
[osx-installer-3.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/3.0/dotnet-runtime-latest-osx-x64.pkg
[osx-installer-checksum-3.0.X]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/release/3.0/dotnet-runtime-latest-osx-x64.pkg.sha512
[osx-targz-3.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/3.0/dotnet-runtime-latest-osx-x64.tar.gz
[osx-targz-checksum-3.0.X]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/release/3.0/dotnet-runtime-latest-osx-x64.tar.gz.sha512
[osx-nethost-targz-3.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/3.0/dotnet-nethost-latest-osx-x64.tar.gz
[osx-symbols-targz-3.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/3.0/dotnet-runtime-symbols-latest-osx-x64.tar.gz

[osx-badge-2.2.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.2/sharedfx_osx-x64_Release_version_badge.svg
[osx-version-2.2.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.2/latest.version
[osx-installer-2.2.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.2/dotnet-runtime-latest-osx-x64.pkg
[osx-installer-checksum-2.2.X]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/release/2.2/dotnet-runtime-latest-osx-x64.pkg.sha512
[osx-targz-2.2.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.2/dotnet-runtime-latest-osx-x64.tar.gz
[osx-targz-checksum-2.2.X]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/release/2.2/dotnet-runtime-latest-osx-x64.tar.gz.sha512
[osx-symbols-targz-2.2.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.2/dotnet-runtime-symbols-latest-osx-x64.tar.gz

[osx-badge-2.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.1/sharedfx_osx-x64_Release_version_badge.svg
[osx-version-2.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.1/latest.version
[osx-installer-2.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.1/dotnet-runtime-latest-osx-x64.pkg
[osx-installer-checksum-2.1.X]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/release/2.1/dotnet-runtime-latest-osx-x64.pkg.sha512
[osx-targz-2.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.1/dotnet-runtime-latest-osx-x64.tar.gz
[osx-targz-checksum-2.1.X]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/release/2.1/dotnet-runtime-latest-osx-x64.tar.gz.sha512
[osx-symbols-targz-2.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.1/dotnet-runtime-symbols-latest-osx-x64.tar.gz


[linux-x64-badge-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/sharedfx_linux-x64_Release_version_badge.svg
[linux-x64-version-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/latest.version
[linux-x64-targz-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/dotnet-runtime-latest-linux-x64.tar.gz
[linux-x64-targz-checksum-master]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/master/dotnet-runtime-latest-linux-x64.tar.gz.sha512
[linux-x64-nethost-targz-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/dotnet-nethost-latest-linux-x64.tar.gz
[linux-x64-symbols-targz-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/dotnet-runtime-symbols-latest-linux-x64.tar.gz

[linux-x64-badge-3.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/3.0/sharedfx_linux-x64_Release_version_badge.svg
[linux-x64-version-3.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/3.0/latest.version
[linux-x64-targz-3.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/3.0/dotnet-runtime-latest-linux-x64.tar.gz
[linux-x64-targz-checksum-3.0.X]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/release/3.0/dotnet-runtime-latest-linux-x64.tar.gz.sha512
[linux-x64-nethost-targz-3.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/3.0/dotnet-nethost-latest-linux-x64.tar.gz
[linux-x64-symbols-targz-3.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/3.0/dotnet-runtime-symbols-latest-linux-x64.tar.gz

[linux-x64-badge-2.2.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.2/sharedfx_linux-x64_Release_version_badge.svg
[linux-x64-version-2.2.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.2/latest.version
[linux-x64-targz-2.2.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.2/dotnet-runtime-latest-linux-x64.tar.gz
[linux-x64-targz-checksum-2.2.X]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/release/2.2/dotnet-runtime-latest-linux-x64.tar.gz.sha512
[linux-x64-symbols-targz-2.2.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.2/dotnet-runtime-symbols-latest-linux-x64.tar.gz

[linux-x64-badge-2.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.1/sharedfx_linux-x64_Release_version_badge.svg
[linux-x64-version-2.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.1/latest.version
[linux-x64-targz-2.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.1/dotnet-runtime-latest-linux-x64.tar.gz
[linux-x64-targz-checksum-2.1.X]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/release/2.1/dotnet-runtime-latest-linux-x64.tar.gz.sha512
[linux-x64-symbols-targz-2.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.1/dotnet-runtime-symbols-latest-linux-x64.tar.gz


[linux-arm-badge-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/sharedfx_linux-arm_Release_version_badge.svg
[linux-arm-version-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/latest.version
[linux-arm-targz-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/dotnet-runtime-latest-linux-arm.tar.gz
[linux-arm-targz-checksum-master]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/master/dotnet-runtime-latest-linux-arm.tar.gz.sha512
[linux-arm-nethost-targz-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/dotnet-nethost-latest-linux-arm.tar.gz
[linux-arm-symbols-targz-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/dotnet-runtime-symbols-latest-linux-arm.tar.gz

[linux-arm-badge-3.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/3.0/sharedfx_linux-arm_Release_version_badge.svg
[linux-arm-version-3.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/3.0/latest.version
[linux-arm-targz-3.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/3.0/dotnet-runtime-latest-linux-arm.tar.gz
[linux-arm-targz-checksum-3.0.X]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/release/3.0/dotnet-runtime-latest-linux-arm.tar.gz.sha512
[linux-arm-nethost-targz-3.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/3.0/dotnet-nethost-latest-linux-arm.tar.gz
[linux-arm-symbols-targz-3.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/3.0/dotnet-runtime-symbols-latest-linux-arm.tar.gz

[linux-arm-badge-2.2.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.2/sharedfx_linux-arm_Release_version_badge.svg
[linux-arm-version-2.2.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.2/latest.version
[linux-arm-targz-2.2.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.2/dotnet-runtime-latest-linux-arm.tar.gz
[linux-arm-targz-checksum-2.2.X]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/release/2.2/dotnet-runtime-latest-linux-arm.tar.gz.sha512
[linux-arm-symbols-targz-2.2.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.2/dotnet-runtime-symbols-latest-linux-arm.tar.gz

[linux-arm-badge-2.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.1/sharedfx_linux-arm_Release_version_badge.svg
[linux-arm-version-2.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.1/latest.version
[linux-arm-targz-2.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.1/dotnet-runtime-latest-linux-arm.tar.gz
[linux-arm-targz-checksum-2.1.X]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/release/2.1/dotnet-runtime-latest-linux-arm.tar.gz.sha512
[linux-arm-symbols-targz-2.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.1/dotnet-runtime-symbols-latest-linux-arm.tar.gz


[linux-arm64-badge-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/sharedfx_linux-arm64_Release_version_badge.svg
[linux-arm64-version-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/latest.version
[linux-arm64-targz-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/dotnet-runtime-latest-linux-arm64.tar.gz
[linux-arm64-targz-checksum-master]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/master/dotnet-runtime-latest-linux-arm64.tar.gz.sha512
[linux-arm64-nethost-targz-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/dotnet-nethost-latest-linux-arm64.tar.gz
[linux-arm64-symbols-targz-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/dotnet-runtime-symbols-latest-linux-arm64.tar.gz

[linux-arm64-badge-3.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/3.0/sharedfx_linux-arm64_Release_version_badge.svg
[linux-arm64-version-3.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/3.0/latest.version
[linux-arm64-targz-3.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/3.0/dotnet-runtime-latest-linux-arm64.tar.gz
[linux-arm64-targz-checksum-3.0.X]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/release/3.0/dotnet-runtime-latest-linux-arm64.tar.gz.sha512
[linux-arm64-nethost-targz-3.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/3.0/dotnet-nethost-latest-linux-arm64.tar.gz
[linux-arm64-symbols-targz-3.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/3.0/dotnet-runtime-symbols-latest-linux-arm64.tar.gz

[linux-arm64-badge-2.2.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.2/sharedfx_linux-arm64_Release_version_badge.svg
[linux-arm64-version-2.2.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.2/latest.version
[linux-arm64-targz-2.2.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.2/dotnet-runtime-latest-linux-arm64.tar.gz
[linux-arm64-targz-checksum-2.2.X]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/release/2.2/dotnet-runtime-latest-linux-arm64.tar.gz.sha512
[linux-arm64-symbols-targz-2.2.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.2/dotnet-runtime-symbols-latest-linux-arm64.tar.gz

[linux-arm64-badge-2.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.1/sharedfx_linux-arm64_Release_version_badge.svg
[linux-arm64-version-2.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.1/latest.version
[linux-arm64-targz-2.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.1/dotnet-runtime-latest-linux-arm64.tar.gz
[linux-arm64-targz-checksum-2.1.X]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/release/2.1/dotnet-runtime-latest-linux-arm64.tar.gz.sha512
[linux-arm64-symbols-targz-2.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.1/dotnet-runtime-symbols-latest-linux-arm64.tar.gz


[ubuntu-14.04-badge-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/sharedfx_ubuntu.14.04-x64_Release_version_badge.svg
[ubuntu-14.04-version-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/latest.version
[ubuntu-14.04-runtime-deps-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/dotnet-runtime-deps-latest-x64.deb
[ubuntu-14.04-runtime-deps-checksum-master]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/master/dotnet-runtime-deps-latest-x64.deb.sha512
[ubuntu-14.04-host-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/dotnet-host-latest-x64.deb
[ubuntu-14.04-host-checksum-master]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/master/dotnet-host-latest-x64.deb.sha512
[ubuntu-14.04-hostfxr-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/dotnet-hostfxr-latest-x64.deb
[ubuntu-14.04-hostfxr-checksum-master]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/master/dotnet-hostfxr-latest-x64.deb.sha512
[ubuntu-14.04-sharedfx-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/dotnet-runtime-latest-x64.deb
[ubuntu-14.04-sharedfx-checksum-master]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/master/dotnet-runtime-latest-x64.deb.sha512

[ubuntu-14.04-badge-3.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/3.0/sharedfx_ubuntu.14.04-x64_Release_version_badge.svg
[ubuntu-14.04-version-3.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/3.0/latest.version
[ubuntu-14.04-runtime-deps-3.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/3.0/dotnet-runtime-deps-latest-x64.deb
[ubuntu-14.04-runtime-deps-checksum-3.0.X]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/release/3.0/dotnet-runtime-deps-latest-x64.deb.sha512
[ubuntu-14.04-host-3.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/3.0/dotnet-host-latest-x64.deb
[ubuntu-14.04-host-checksum-3.0.X]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/release/3.0/dotnet-host-latest-x64.deb.sha512
[ubuntu-14.04-hostfxr-3.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/3.0/dotnet-hostfxr-latest-x64.deb
[ubuntu-14.04-hostfxr-checksum-3.0.X]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/release/3.0/dotnet-hostfxr-latest-x64.deb.sha512
[ubuntu-14.04-sharedfx-3.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/3.0/dotnet-runtime-latest-x64.deb
[ubuntu-14.04-sharedfx-checksum-3.0.X]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/release/3.0/dotnet-runtime-latest-x64.deb.sha512

[ubuntu-14.04-badge-2.2.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.2/sharedfx_ubuntu.14.04-x64_Release_version_badge.svg
[ubuntu-14.04-version-2.2.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.2/latest.version
[ubuntu-14.04-host-2.2.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.2/dotnet-host-latest-x64.deb
[ubuntu-14.04-host-checksum-2.2.X]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/release/2.2/dotnet-host-latest-x64.deb.sha512
[ubuntu-14.04-hostfxr-2.2.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.2/dotnet-hostfxr-latest-x64.deb
[ubuntu-14.04-hostfxr-checksum-2.2.X]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/release/2.2/dotnet-hostfxr-latest-x64.deb.sha512
[ubuntu-14.04-sharedfx-2.2.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.2/dotnet-runtime-latest-x64.deb
[ubuntu-14.04-sharedfx-checksum-2.2.X]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/release/2.2/dotnet-runtime-latest-x64.deb.sha512

[ubuntu-14.04-badge-2.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.1/sharedfx_ubuntu.14.04-x64_Release_version_badge.svg
[ubuntu-14.04-version-2.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.1/latest.version
[ubuntu-14.04-host-2.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.1/dotnet-host-latest-x64.deb
[ubuntu-14.04-host-checksum-2.1.X]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/release/2.1/dotnet-host-latest-x64.deb.sha512
[ubuntu-14.04-hostfxr-2.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.1/dotnet-hostfxr-latest-x64.deb
[ubuntu-14.04-hostfxr-checksum-2.1.X]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/release/2.1/dotnet-hostfxr-latest-x64.deb.sha512
[ubuntu-14.04-sharedfx-2.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.1/dotnet-runtime-latest-x64.deb
[ubuntu-14.04-sharedfx-checksum-2.1.X]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/release/2.1/dotnet-runtime-latest-x64.deb.sha512


[ubuntu-16.04-badge-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/sharedfx_ubuntu.16.04-x64_Release_version_badge.svg
[ubuntu-16.04-version-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/latest.version
[ubuntu-16.04-host-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/dotnet-host-latest-x64.deb
[ubuntu-16.04-runtime-deps-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/dotnet-runtime-deps-latest-x64.deb
[ubuntu-16.04-runtime-deps-checksum-master]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/master/dotnet-runtime-deps-latest-x64.deb.sha512
[ubuntu-16.04-host-checksum-master]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/master/dotnet-host-latest-x64.deb.sha512
[ubuntu-16.04-hostfxr-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/dotnet-hostfxr-latest-x64.deb
[ubuntu-16.04-hostfxr-checksum-master]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/master/dotnet-hostfxr-latest-x64.deb.sha512
[ubuntu-16.04-sharedfx-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/dotnet-runtime-latest-x64.deb
[ubuntu-16.04-sharedfx-checksum-master]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/master/dotnet-runtime-latest-x64.deb.sha512

[ubuntu-16.04-badge-3.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/3.0/sharedfx_ubuntu.16.04-x64_Release_version_badge.svg
[ubuntu-16.04-version-3.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/3.0/latest.version
[ubuntu-16.04-host-3.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/3.0/dotnet-host-latest-x64.deb
[ubuntu-16.04-runtime-deps-3.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/3.0/dotnet-runtime-deps-latest-x64.deb
[ubuntu-16.04-runtime-deps-checksum-3.0.X]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/release/3.0/dotnet-runtime-deps-latest-x64.deb.sha512
[ubuntu-16.04-host-checksum-3.0.X]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/release/3.0/dotnet-host-latest-x64.deb.sha512
[ubuntu-16.04-hostfxr-3.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/3.0/dotnet-hostfxr-latest-x64.deb
[ubuntu-16.04-hostfxr-checksum-3.0.X]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/release/3.0/dotnet-hostfxr-latest-x64.deb.sha512
[ubuntu-16.04-sharedfx-3.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/3.0/dotnet-runtime-latest-x64.deb
[ubuntu-16.04-sharedfx-checksum-3.0.X]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/release/3.0/dotnet-runtime-latest-x64.deb.sha512

[ubuntu-16.04-badge-2.2.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.2/sharedfx_ubuntu.16.04-x64_Release_version_badge.svg
[ubuntu-16.04-version-2.2.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.2/latest.version
[ubuntu-16.04-host-2.2.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.2/dotnet-host-latest-x64.deb
[ubuntu-16.04-host-checksum-2.2.X]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/release/2.2/dotnet-host-latest-x64.deb.sha512
[ubuntu-16.04-hostfxr-2.2.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.2/dotnet-hostfxr-latest-x64.deb
[ubuntu-16.04-hostfxr-checksum-2.2.X]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/release/2.2/dotnet-hostfxr-latest-x64.deb.sha512
[ubuntu-16.04-sharedfx-2.2.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.2/dotnet-runtime-latest-x64.deb
[ubuntu-16.04-sharedfx-checksum-2.2.X]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/release/2.2/dotnet-runtime-latest-x64.deb.sha512

[ubuntu-16.04-badge-2.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.1/sharedfx_ubuntu.16.04-x64_Release_version_badge.svg
[ubuntu-16.04-version-2.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.1/latest.version
[ubuntu-16.04-host-2.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.1/dotnet-host-latest-x64.deb
[ubuntu-16.04-host-checksum-2.1.X]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/release/2.1/dotnet-host-latest-x64.deb.sha512
[ubuntu-16.04-hostfxr-2.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.1/dotnet-hostfxr-latest-x64.deb
[ubuntu-16.04-hostfxr-checksum-2.1.X]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/release/2.1/dotnet-hostfxr-latest-x64.deb.sha512
[ubuntu-16.04-sharedfx-2.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.1/dotnet-runtime-latest-x64.deb
[ubuntu-16.04-sharedfx-checksum-2.1.X]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/release/2.1/dotnet-runtime-latest-x64.deb.sha512


[ubuntu-18.04-badge-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/sharedfx_ubuntu.18.04-x64_Release_version_badge.svg
[ubuntu-18.04-version-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/latest.version
[ubuntu-18.04-host-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/dotnet-host-latest-x64.deb
[ubuntu-18.04-runtime-deps-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/dotnet-runtime-deps-latest-x64.deb
[ubuntu-18.04-runtime-deps-checksum-master]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/master/dotnet-runtime-deps-latest-x64.deb.sha512
[ubuntu-18.04-host-checksum-master]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/master/dotnet-host-latest-x64.deb.sha512
[ubuntu-18.04-hostfxr-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/dotnet-hostfxr-latest-x64.deb
[ubuntu-18.04-hostfxr-checksum-master]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/master/dotnet-hostfxr-latest-x64.deb.sha512
[ubuntu-18.04-sharedfx-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/dotnet-runtime-latest-x64.deb
[ubuntu-18.04-sharedfx-checksum-master]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/master/dotnet-runtime-latest-x64.deb.sha512

[ubuntu-18.04-badge-3.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/3.0/sharedfx_ubuntu.18.04-x64_Release_version_badge.svg
[ubuntu-18.04-version-3.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/3.0/latest.version
[ubuntu-18.04-host-3.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/3.0/dotnet-host-latest-x64.deb
[ubuntu-18.04-runtime-deps-3.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/3.0/dotnet-runtime-deps-latest-x64.deb
[ubuntu-18.04-runtime-deps-checksum-3.0.X]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/release/3.0/dotnet-runtime-deps-latest-x64.deb.sha512
[ubuntu-18.04-host-checksum-3.0.X]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/release/3.0/dotnet-host-latest-x64.deb.sha512
[ubuntu-18.04-hostfxr-3.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/3.0/dotnet-hostfxr-latest-x64.deb
[ubuntu-18.04-hostfxr-checksum-3.0.X]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/release/3.0/dotnet-hostfxr-latest-x64.deb.sha512
[ubuntu-18.04-sharedfx-3.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/3.0/dotnet-runtime-latest-x64.deb
[ubuntu-18.04-sharedfx-checksum-3.0.X]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/release/3.0/dotnet-runtime-latest-x64.deb.sha512

[ubuntu-18.04-badge-2.2.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.2/sharedfx_ubuntu.18.04-x64_Release_version_badge.svg
[ubuntu-18.04-version-2.2.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.2/latest.version
[ubuntu-18.04-host-2.2.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.2/dotnet-host-latest-x64.deb
[ubuntu-18.04-runtime-deps-2.2.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.2/dotnet-runtime-deps-latest-ubuntu.18.04-x64.deb
[ubuntu-18.04-runtime-deps-checksum-2.2.X]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/release/2.2/dotnet-runtime-deps-latest-ubuntu.18.04-x64.deb.sha512
[ubuntu-18.04-host-checksum-2.2.X]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/release/2.2/dotnet-host-latest-x64.deb.sha512
[ubuntu-18.04-hostfxr-2.2.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.2/dotnet-hostfxr-latest-x64.deb
[ubuntu-18.04-hostfxr-checksum-2.2.X]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/release/2.2/dotnet-hostfxr-latest-x64.deb.sha512
[ubuntu-18.04-sharedfx-2.2.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.2/dotnet-runtime-latest-x64.deb
[ubuntu-18.04-sharedfx-checksum-2.2.X]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/release/2.2/dotnet-runtime-latest-x64.deb.sha512

[ubuntu-18.04-badge-2.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.1/sharedfx_ubuntu.18.04-x64_Release_version_badge.svg
[ubuntu-18.04-version-2.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.1/latest.version
[ubuntu-18.04-host-2.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.1/dotnet-host-latest-x64.deb
[ubuntu-18.04-runtime-deps-2.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.1/dotnet-runtime-deps-latest-ubuntu.18.04-x64.deb
[ubuntu-18.04-runtime-deps-checksum-2.1.X]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/release/2.1/dotnet-runtime-deps-latest-ubuntu.18.04-x64.deb.sha512
[ubuntu-18.04-host-checksum-2.1.X]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/release/2.1/dotnet-host-latest-x64.deb.sha512
[ubuntu-18.04-hostfxr-2.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.1/dotnet-hostfxr-latest-x64.deb
[ubuntu-18.04-hostfxr-checksum-2.1.X]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/release/2.1/dotnet-hostfxr-latest-x64.deb.sha512
[ubuntu-18.04-sharedfx-2.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.1/dotnet-runtime-latest-x64.deb
[ubuntu-18.04-sharedfx-checksum-2.1.X]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/release/2.1/dotnet-runtime-latest-x64.deb.sha512


[ubuntu-19.04-badge-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/sharedfx_ubuntu.19.04-x64_Release_version_badge.svg
[ubuntu-19.04-version-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/latest.version
[ubuntu-19.04-host-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/dotnet-host-latest-x64.deb
[ubuntu-19.04-runtime-deps-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/dotnet-runtime-deps-latest-x64.deb
[ubuntu-19.04-runtime-deps-checksum-master]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/master/dotnet-runtime-deps-latest-x64.deb.sha512
[ubuntu-19.04-host-checksum-master]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/master/dotnet-host-latest-x64.deb.sha512
[ubuntu-19.04-hostfxr-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/dotnet-hostfxr-latest-x64.deb
[ubuntu-19.04-hostfxr-checksum-master]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/master/dotnet-hostfxr-latest-x64.deb.sha512
[ubuntu-19.04-sharedfx-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/dotnet-runtime-latest-x64.deb
[ubuntu-19.04-sharedfx-checksum-master]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/master/dotnet-runtime-latest-x64.deb.sha512

[ubuntu-19.04-badge-3.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/3.0/sharedfx_ubuntu.19.04-x64_Release_version_badge.svg
[ubuntu-19.04-version-3.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/3.0/latest.version
[ubuntu-19.04-host-3.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/3.0/dotnet-host-latest-x64.deb
[ubuntu-19.04-runtime-deps-3.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/3.0/dotnet-runtime-deps-latest-x64.deb
[ubuntu-19.04-runtime-deps-checksum-3.0.X]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/release/3.0/dotnet-runtime-deps-latest-x64.deb.sha512
[ubuntu-19.04-host-checksum-3.0.X]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/release/3.0/dotnet-host-latest-x64.deb.sha512
[ubuntu-19.04-hostfxr-3.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/3.0/dotnet-hostfxr-latest-x64.deb
[ubuntu-19.04-hostfxr-checksum-3.0.X]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/release/3.0/dotnet-hostfxr-latest-x64.deb.sha512
[ubuntu-19.04-sharedfx-3.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/3.0/dotnet-runtime-latest-x64.deb
[ubuntu-19.04-sharedfx-checksum-3.0.X]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/release/3.0/dotnet-runtime-latest-x64.deb.sha512

[ubuntu-19.04-badge-2.2.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.2/sharedfx_ubuntu.19.04-x64_Release_version_badge.svg
[ubuntu-19.04-version-2.2.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.2/latest.version
[ubuntu-19.04-host-2.2.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.2/dotnet-host-latest-x64.deb
[ubuntu-19.04-runtime-deps-2.2.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.2/dotnet-runtime-deps-latest-ubuntu.19.04-x64.deb
[ubuntu-19.04-runtime-deps-checksum-2.2.X]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/release/2.2/dotnet-runtime-deps-latest-ubuntu.19.04-x64.deb.sha512
[ubuntu-19.04-host-checksum-2.2.X]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/release/2.2/dotnet-host-latest-x64.deb.sha512
[ubuntu-19.04-hostfxr-2.2.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.2/dotnet-hostfxr-latest-x64.deb
[ubuntu-19.04-hostfxr-checksum-2.2.X]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/release/2.2/dotnet-hostfxr-latest-x64.deb.sha512
[ubuntu-19.04-sharedfx-2.2.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.2/dotnet-runtime-latest-x64.deb
[ubuntu-19.04-sharedfx-checksum-2.2.X]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/release/2.2/dotnet-runtime-latest-x64.deb.sha512

[ubuntu-19.04-badge-2.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.1/sharedfx_ubuntu.19.04-x64_Release_version_badge.svg
[ubuntu-19.04-version-2.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.1/latest.version
[ubuntu-19.04-host-2.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.1/dotnet-host-latest-x64.deb
[ubuntu-19.04-runtime-deps-2.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.1/dotnet-runtime-deps-latest-ubuntu.19.04-x64.deb
[ubuntu-19.04-runtime-deps-checksum-2.1.X]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/release/2.1/dotnet-runtime-deps-latest-ubuntu.19.04-x64.deb.sha512
[ubuntu-19.04-host-checksum-2.1.X]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/release/2.1/dotnet-host-latest-x64.deb.sha512
[ubuntu-19.04-hostfxr-2.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.1/dotnet-hostfxr-latest-x64.deb
[ubuntu-19.04-hostfxr-checksum-2.1.X]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/release/2.1/dotnet-hostfxr-latest-x64.deb.sha512
[ubuntu-19.04-sharedfx-2.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.1/dotnet-runtime-latest-x64.deb
[ubuntu-19.04-sharedfx-checksum-2.1.X]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/release/2.1/dotnet-runtime-latest-x64.deb.sha512


[debian-8.2-badge-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/sharedfx_debian.8-x64_Release_version_badge.svg
[debian-8.2-version-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/latest.version
[debian-8.2-runtime-deps-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/dotnet-runtime-deps-latest-x64.deb
[debian-8.2-runtime-deps-checksum-master]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/master/dotnet-runtime-deps-latest-x64.deb.sha512
[debian-8.2-host-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/dotnet-host-latest-x64.deb
[debian-8.2-host-checksum-master]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/master/dotnet-host-latest-x64.deb.sha512
[debian-8.2-hostfxr-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/dotnet-hostfxr-latest-x64.deb
[debian-8.2-hostfxr-checksum-master]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/master/dotnet-hostfxr-latest-x64.deb.sha512
[debian-8.2-sharedfx-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/dotnet-runtime-latest-x64.deb
[debian-8.2-sharedfx-checksum-master]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/master/dotnet-runtime-latest-x64.deb.sha512

[debian-8.2-badge-3.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/3.0/sharedfx_debian.8-x64_Release_version_badge.svg
[debian-8.2-version-3.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/3.0/latest.version
[debian-8.2-runtime-deps-3.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/3.0/dotnet-runtime-deps-latest-x64.deb
[debian-8.2-runtime-deps-checksum-3.0.X]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/release/3.0/dotnet-runtime-deps-latest-x64.deb.sha512
[debian-8.2-host-3.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/3.0/dotnet-host-latest-x64.deb
[debian-8.2-host-checksum-3.0.X]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/release/3.0/dotnet-host-latest-x64.deb.sha512
[debian-8.2-hostfxr-3.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/3.0/dotnet-hostfxr-latest-x64.deb
[debian-8.2-hostfxr-checksum-3.0.X]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/release/3.0/dotnet-hostfxr-latest-x64.deb.sha512
[debian-8.2-sharedfx-3.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/3.0/dotnet-runtime-latest-x64.deb
[debian-8.2-sharedfx-checksum-3.0.X]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/release/3.0/dotnet-runtime-latest-x64.deb.sha512

[debian-8.2-badge-2.2.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.2/sharedfx_debian.8-x64_Release_version_badge.svg
[debian-8.2-version-2.2.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.2/latest.version
[debian-8.2-runtime-deps-2.2.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.2/dotnet-runtime-deps-latest-debian.8-x64.deb
[debian-8.2-runtime-deps-checksum-2.2.X]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/release/2.2/dotnet-runtime-deps-latest-debian.8-x64.deb.sha512
[debian-8.2-host-2.2.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.2/dotnet-host-latest-x64.deb
[debian-8.2-host-checksum-2.2.X]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/release/2.2/dotnet-host-latest-x64.deb.sha512
[debian-8.2-hostfxr-2.2.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.2/dotnet-hostfxr-latest-x64.deb
[debian-8.2-hostfxr-checksum-2.2.X]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/release/2.2/dotnet-hostfxr-latest-x64.deb.sha512
[debian-8.2-sharedfx-2.2.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.2/dotnet-runtime-latest-x64.deb
[debian-8.2-sharedfx-checksum-2.2.X]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/release/2.2/dotnet-runtime-latest-x64.deb.sha512

[debian-8.2-badge-2.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.1/sharedfx_debian.8-x64_Release_version_badge.svg
[debian-8.2-version-2.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.1/latest.version
[debian-8.2-runtime-deps-2.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.1/dotnet-runtime-deps-latest-debian.8-x64.deb
[debian-8.2-runtime-deps-checksum-2.1.X]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/release/2.1/dotnet-runtime-deps-latest-debian.8-x64.deb.sha512
[debian-8.2-host-2.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.1/dotnet-host-latest-x64.deb
[debian-8.2-host-checksum-2.1.X]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/release/2.1/dotnet-host-latest-x64.deb.sha512
[debian-8.2-hostfxr-2.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.1/dotnet-hostfxr-latest-x64.deb
[debian-8.2-hostfxr-checksum-2.1.X]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/release/2.1/dotnet-hostfxr-latest-x64.deb.sha512
[debian-8.2-sharedfx-2.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.1/dotnet-runtime-latest-x64.deb
[debian-8.2-sharedfx-checksum-2.1.X]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/release/2.1/dotnet-runtime-latest-x64.deb.sha512


[debian-9-badge-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/sharedfx_debian.9-x64_Release_version_badge.svg
[debian-9-version-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/latest.version
[debian-9-runtime-deps-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/dotnet-runtime-deps-latest-x64.deb
[debian-9-runtime-deps-checksum-master]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/master/dotnet-runtime-deps-latest-x64.deb.sha512
[debian-9-host-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/dotnet-host-latest-x64.deb
[debian-9-host-checksum-master]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/master/dotnet-host-latest-x64.deb.sha512
[debian-9-hostfxr-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/dotnet-hostfxr-latest-x64.deb
[debian-9-hostfxr-checksum-master]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/master/dotnet-hostfxr-latest-x64.deb.sha512
[debian-9-sharedfx-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/dotnet-runtime-latest-x64.deb
[debian-9-sharedfx-checksum-master]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/master/dotnet-runtime-latest-x64.deb.sha512

[debian-9-badge-3.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/3.0/sharedfx_debian.9-x64_Release_version_badge.svg
[debian-9-version-3.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/3.0/latest.version
[debian-9-runtime-deps-3.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/3.0/dotnet-runtime-deps-latest-x64.deb
[debian-9-runtime-deps-checksum-3.0.X]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/release/3.0/dotnet-runtime-deps-latest-x64.deb.sha512
[debian-9-host-3.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/3.0/dotnet-host-latest-x64.deb
[debian-9-host-checksum-3.0.X]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/release/3.0/dotnet-host-latest-x64.deb.sha512
[debian-9-hostfxr-3.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/3.0/dotnet-hostfxr-latest-x64.deb
[debian-9-hostfxr-checksum-3.0.X]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/release/3.0/dotnet-hostfxr-latest-x64.deb.sha512
[debian-9-sharedfx-3.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/3.0/dotnet-runtime-latest-x64.deb
[debian-9-sharedfx-checksum-3.0.X]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/release/3.0/dotnet-runtime-latest-x64.deb.sha512

[debian-9-badge-2.2.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.2/sharedfx_debian.9-x64_Release_version_badge.svg
[debian-9-version-2.2.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.2/latest.version
[debian-9-runtime-deps-2.2.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.2/dotnet-runtime-deps-latest-debian.9-x64.deb
[debian-9-runtime-deps-checksum-2.2.X]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/release/2.2/dotnet-runtime-deps-latest-debian.9-x64.deb.sha512
[debian-9-host-2.2.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.2/dotnet-host-latest-x64.deb
[debian-9-host-checksum-2.2.X]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/release/2.2/dotnet-host-latest-x64.deb.sha512
[debian-9-hostfxr-2.2.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.2/dotnet-hostfxr-latest-x64.deb
[debian-9-hostfxr-checksum-2.2.X]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/release/2.2/dotnet-hostfxr-latest-x64.deb.sha512
[debian-9-sharedfx-2.2.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.2/dotnet-runtime-latest-x64.deb
[debian-9-sharedfx-checksum-2.2.X]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/release/2.2/dotnet-runtime-latest-x64.deb.sha512

[debian-9-badge-2.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.1/sharedfx_debian.9-x64_Release_version_badge.svg
[debian-9-version-2.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.1/latest.version
[debian-9-runtime-deps-2.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.1/dotnet-runtime-deps-latest-debian.9-x64.deb
[debian-9-runtime-deps-checksum-2.1.X]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/release/2.1/dotnet-runtime-deps-latest-debian.9-x64.deb.sha512
[debian-9-host-2.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.1/dotnet-host-latest-x64.deb
[debian-9-host-checksum-2.1.X]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/release/2.1/dotnet-host-latest-x64.deb.sha512
[debian-9-hostfxr-2.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.1/dotnet-hostfxr-latest-x64.deb
[debian-9-hostfxr-checksum-2.1.X]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/release/2.1/dotnet-hostfxr-latest-x64.deb.sha512
[debian-9-sharedfx-2.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.1/dotnet-runtime-latest-x64.deb
[debian-9-sharedfx-checksum-2.1.X]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/release/2.1/dotnet-runtime-latest-x64.deb.sha512


[centos-7-badge-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/sharedfx_centos.7-x64_Release_version_badge.svg
[centos-7-version-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/latest.version
[centos-7-host-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/dotnet-host-latest-x64.rpm
[centos-7-runtime-deps-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/dotnet-runtime-deps-latest-centos.7-x64.rpm
[centos-7-runtime-deps-checksum-master]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/master/dotnet-runtime-deps-latest-centos.7-x64.rpm.sha512
[centos-7-host-checksum-master]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/master/dotnet-host-latest-x64.rpm.sha512
[centos-7-hostfxr-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/dotnet-hostfxr-latest-x64.rpm
[centos-7-hostfxr-checksum-master]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/master/dotnet-hostfxr-latest-x64.rpm.sha512
[centos-7-sharedfx-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/dotnet-runtime-latest-x64.rpm
[centos-7-sharedfx-checksum-master]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/master/dotnet-runtime-latest-x64.rpm.sha512

[centos-7-badge-3.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/3.0/sharedfx_centos.7-x64_Release_version_badge.svg
[centos-7-version-3.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/3.0/latest.version
[centos-7-host-3.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/3.0/dotnet-host-latest-x64.rpm
[centos-7-runtime-deps-3.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/3.0/dotnet-runtime-deps-latest-centos.7-x64.rpm
[centos-7-runtime-deps-checksum-3.0.X]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/release/3.0/dotnet-runtime-deps-latest-centos.7-x64.rpm.sha512
[centos-7-host-checksum-3.0.X]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/release/3.0/dotnet-host-latest-x64.rpm.sha512
[centos-7-hostfxr-3.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/3.0/dotnet-hostfxr-latest-x64.rpm
[centos-7-hostfxr-checksum-3.0.X]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/release/3.0/dotnet-hostfxr-latest-x64.rpm.sha512
[centos-7-sharedfx-3.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/3.0/dotnet-runtime-latest-x64.rpm
[centos-7-sharedfx-checksum-3.0.X]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/release/3.0/dotnet-runtime-latest-x64.rpm.sha512

[centos-7-badge-2.2.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.2/sharedfx_centos.7-x64_Release_version_badge.svg
[centos-7-version-2.2.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.2/latest.version
[centos-7-host-2.2.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.2/dotnet-host-latest-x64.rpm
[centos-7-runtime-deps-2.2.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.2/dotnet-runtime-deps-latest-centos.7-x64.rpm
[centos-7-runtime-deps-checksum-2.2.X]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/release/2.2/dotnet-runtime-deps-latest-centos.7-x64.rpm.sha512
[centos-7-host-checksum-2.2.X]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/release/2.2/dotnet-host-latest-x64.rpm.sha512
[centos-7-hostfxr-2.2.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.2/dotnet-hostfxr-latest-x64.rpm
[centos-7-hostfxr-checksum-2.2.X]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/release/2.2/dotnet-hostfxr-latest-x64.rpm.sha512
[centos-7-sharedfx-2.2.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.2/dotnet-runtime-latest-x64.rpm
[centos-7-sharedfx-checksum-2.2.X]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/release/2.2/dotnet-runtime-latest-x64.rpm.sha512

[centos-7-badge-2.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.1/sharedfx_centos.7-x64_Release_version_badge.svg
[centos-7-version-2.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.1/latest.version
[centos-7-host-2.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.1/dotnet-host-latest-x64.rpm
[centos-7-runtime-deps-2.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.1/dotnet-runtime-deps-latest-centos.7-x64.rpm
[centos-7-runtime-deps-checksum-2.1.X]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/release/2.1/dotnet-runtime-deps-latest-centos.7-x64.rpm.sha512
[centos-7-host-checksum-2.1.X]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/release/2.1/dotnet-host-latest-x64.rpm.sha512
[centos-7-hostfxr-2.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.1/dotnet-hostfxr-latest-x64.rpm
[centos-7-hostfxr-checksum-2.1.X]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/release/2.1/dotnet-hostfxr-latest-x64.rpm.sha512
[centos-7-sharedfx-2.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.1/dotnet-runtime-latest-x64.rpm
[centos-7-sharedfx-checksum-2.1.X]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/release/2.1/dotnet-runtime-latest-x64.rpm.sha512


[rhel-6-badge-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/sharedfx_rhel.6-x64_Release_version_badge.svg
[rhel-6-version-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/latest.version
[rhel-6-targz-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/dotnet-runtime-latest-rhel.6-x64.tar.gz

[rhel-6-badge-3.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/3.0/sharedfx_rhel.6-x64_Release_version_badge.svg
[rhel-6-version-3.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/3.0/latest.version
[rhel-6-targz-3.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/3.0/dotnet-runtime-latest-rhel.6-x64.tar.gz

[rhel-6-badge-2.2.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.2/sharedfx_rhel.6-x64_Release_version_badge.svg
[rhel-6-version-2.2.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.2/latest.version
[rhel-6-targz-2.2.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.2/dotnet-runtime-latest-rhel.6-x64.tar.gz

[rhel-6-badge-2.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.1/sharedfx_rhel.6-x64_Release_version_badge.svg
[rhel-6-version-2.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.1/latest.version
[rhel-6-targz-2.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.1/dotnet-runtime-latest-rhel.6-x64.tar.gz

[rhel-6-badge-2.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.0.0/sharedfx_rhel.6-x64_Release_version_badge.svg
[rhel-6-version-2.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.0.0/latest.version
[rhel-6-targz-2.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.0.0/dotnet-runtime-latest-rhel.6-x64.tar.gz


[rhel7-badge-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/sharedfx_rhel.7-x64_Release_version_badge.svg
[rhel7-version-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/latest.version
[rhel7-host-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/dotnet-host-latest-rhel.7-x64.rpm
[rhel7-host-checksum-master]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/master/dotnet-host-latest-rhel.7-x64.rpm.sha512
[rhel7-hostfxr-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/dotnet-hostfxr-latest-rhel.7-x64.rpm
[rhel7-hostfxr-checksum-master]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/master/dotnet-hostfxr-latest-rhel.7-x64.rpm.sha512
[rhel7-sharedfx-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/dotnet-runtime-latest-rhel.7-x64.rpm
[rhel7-sharedfx-checksum-master]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/master/dotnet-runtime-latest-rhel.7-x64.rpm.sha512

[rhel7-badge-3.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/3.0/sharedfx_rhel.7-x64_Release_version_badge.svg
[rhel7-version-3.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/3.0/latest.version
[rhel7-host-3.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/3.0/dotnet-host-latest-rhel.7-x64.rpm
[rhel7-host-checksum-3.0.X]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/release/3.0/dotnet-host-latest-rhel.7-x64.rpm.sha512
[rhel7-hostfxr-3.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/3.0/dotnet-hostfxr-latest-rhel.7-x64.rpm
[rhel7-hostfxr-checksum-3.0.X]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/release/3.0/dotnet-hostfxr-latest-rhel.7-x64.rpm.sha512
[rhel7-sharedfx-3.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/3.0/dotnet-runtime-latest-rhel.7-x64.rpm
[rhel7-sharedfx-checksum-3.0.X]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/release/3.0/dotnet-runtime-latest-rhel.7-x64.rpm.sha512

[rhel7-badge-2.2.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.2/sharedfx_rhel.7-x64_Release_version_badge.svg
[rhel7-version-2.2.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.2/latest.version
[rhel7-host-2.2.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.2/dotnet-host-latest-rhel.7-x64.rpm
[rhel7-host-checksum-2.2.X]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/release/2.2/dotnet-host-latest-rhel.7-x64.rpm.sha512
[rhel7-hostfxr-2.2.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.2/dotnet-hostfxr-latest-rhel.7-x64.rpm
[rhel7-hostfxr-checksum-2.2.X]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/release/2.2/dotnet-hostfxr-latest-rhel.7-x64.rpm.sha512
[rhel7-sharedfx-2.2.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.2/dotnet-runtime-latest-rhel.7-x64.rpm
[rhel7-sharedfx-checksum-2.2.X]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/release/2.2/dotnet-runtime-latest-rhel.7-x64.rpm.sha512

[rhel7-badge-2.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.1/sharedfx_rhel.7-x64_Release_version_badge.svg
[rhel7-version-2.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.1/latest.version
[rhel7-host-2.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.1/dotnet-host-latest-rhel.7-x64.rpm
[rhel7-host-checksum-2.1.X]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/release/2.1/dotnet-host-latest-rhel.7-x64.rpm.sha512
[rhel7-hostfxr-2.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.1/dotnet-hostfxr-latest-rhel.7-x64.rpm
[rhel7-hostfxr-checksum-2.1.X]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/release/2.1/dotnet-hostfxr-latest-rhel.7-x64.rpm.sha512
[rhel7-sharedfx-2.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.1/dotnet-runtime-latest-rhel.7-x64.rpm
[rhel7-sharedfx-checksum-2.1.X]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/release/2.1/dotnet-runtime-latest-rhel.7-x64.rpm.sha512


[fedora-27-badge-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/sharedfx_fedora.27-x64_Release_version_badge.svg
[fedora-27-version-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/latest.version
[fedora-27-host-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/dotnet-host-latest-x64.rpm
[fedora-27-runtime-deps-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/dotnet-runtime-deps-latest-fedora.27-x64.rpm
[fedora-27-runtime-deps-checksum-master]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/master/dotnet-runtime-deps-latest-fedora.27-x64.rpm.sha512
[fedora-27-host-checksum-master]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/master/dotnet-host-latest-x64.rpm.sha512
[fedora-27-hostfxr-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/dotnet-hostfxr-latest-x64.rpm
[fedora-27-hostfxr-checksum-master]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/master/dotnet-hostfxr-latest-x64.rpm.sha512
[fedora-27-sharedfx-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/dotnet-runtime-latest-x64.rpm
[fedora-27-sharedfx-checksum-master]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/master/dotnet-runtime-latest-x64.rpm.sha512

[fedora-27-badge-3.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/3.0/sharedfx_fedora.27-x64_Release_version_badge.svg
[fedora-27-version-3.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/3.0/latest.version
[fedora-27-host-3.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/3.0/dotnet-host-latest-x64.rpm
[fedora-27-runtime-deps-3.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/3.0/dotnet-runtime-deps-latest-fedora.27-x64.rpm
[fedora-27-runtime-deps-checksum-3.0.X]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/release/3.0/dotnet-runtime-deps-latest-fedora.27-x64.rpm.sha512
[fedora-27-host-checksum-3.0.X]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/release/3.0/dotnet-host-latest-x64.rpm.sha512
[fedora-27-hostfxr-3.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/3.0/dotnet-hostfxr-latest-x64.rpm
[fedora-27-hostfxr-checksum-3.0.X]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/release/3.0/dotnet-hostfxr-latest-x64.rpm.sha512
[fedora-27-sharedfx-3.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/3.0/dotnet-runtime-latest-x64.rpm
[fedora-27-sharedfx-checksum-3.0.X]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/release/3.0/dotnet-runtime-latest-x64.rpm.sha512

[fedora-27-badge-2.2.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.2/sharedfx_fedora.27-x64_Release_version_badge.svg
[fedora-27-version-2.2.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.2/latest.version
[fedora-27-host-2.2.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.2/dotnet-host-latest-x64.rpm
[fedora-27-runtime-deps-2.2.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.2/dotnet-runtime-deps-latest-fedora.27-x64.rpm
[fedora-27-runtime-deps-checksum-2.2.X]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/release/2.2/dotnet-runtime-deps-latest-fedora.27-x64.rpm.sha512
[fedora-27-host-checksum-2.2.X]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/release/2.2/dotnet-host-latest-x64.rpm.sha512
[fedora-27-hostfxr-2.2.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.2/dotnet-hostfxr-latest-x64.rpm
[fedora-27-hostfxr-checksum-2.2.X]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/release/2.2/dotnet-hostfxr-latest-x64.rpm.sha512
[fedora-27-sharedfx-2.2.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.2/dotnet-runtime-latest-x64.rpm
[fedora-27-sharedfx-checksum-2.2.X]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/release/2.2/dotnet-runtime-latest-x64.rpm.sha512

[fedora-27-badge-2.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.1/sharedfx_fedora.27-x64_Release_version_badge.svg
[fedora-27-version-2.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.1/latest.version
[fedora-27-host-2.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.1/dotnet-host-latest-x64.rpm
[fedora-27-runtime-deps-2.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.1/dotnet-runtime-deps-latest-fedora.27-x64.rpm
[fedora-27-runtime-deps-checksum-2.1.X]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/release/2.1/dotnet-runtime-deps-latest-fedora.27-x64.rpm.sha512
[fedora-27-host-checksum-2.1.X]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/release/2.1/dotnet-host-latest-x64.rpm.sha512
[fedora-27-hostfxr-2.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.1/dotnet-hostfxr-latest-x64.rpm
[fedora-27-hostfxr-checksum-2.1.X]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/release/2.1/dotnet-hostfxr-latest-x64.rpm.sha512
[fedora-27-sharedfx-2.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.1/dotnet-runtime-latest-x64.rpm
[fedora-27-sharedfx-checksum-2.1.X]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/release/2.1/dotnet-runtime-latest-x64.rpm.sha512


[sles-12-badge-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/sharedfx_sles.12-x64_Release_version_badge.svg
[sles-12-version-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/latest.version
[sles-12-host-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/dotnet-host-latest-x64.rpm
[sles-12-runtime-deps-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/dotnet-runtime-deps-latest-sles.12-x64.rpm
[sles-12-runtime-deps-checksum-master]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/master/dotnet-runtime-deps-latest-sles.12-x64.rpm.sha512
[sles-12-host-checksum-master]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/master/dotnet-host-latest-x64.rpm.sha512
[sles-12-hostfxr-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/dotnet-hostfxr-latest-x64.rpm
[sles-12-hostfxr-checksum-master]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/master/dotnet-hostfxr-latest-x64.rpm.sha512
[sles-12-sharedfx-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/dotnet-runtime-latest-x64.rpm
[sles-12-sharedfx-checksum-master]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/master/dotnet-runtime-latest-x64.rpm.sha512

[sles-12-badge-3.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/3.0/sharedfx_sles.12-x64_Release_version_badge.svg
[sles-12-version-3.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/3.0/latest.version
[sles-12-host-3.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/3.0/dotnet-host-latest-x64.rpm
[sles-12-runtime-deps-3.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/3.0/dotnet-runtime-deps-latest-sles.12-x64.rpm
[sles-12-runtime-deps-checksum-3.0.X]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/release/3.0/dotnet-runtime-deps-latest-sles.12-x64.rpm.sha512
[sles-12-host-checksum-3.0.X]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/release/3.0/dotnet-host-latest-x64.rpm.sha512
[sles-12-hostfxr-3.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/3.0/dotnet-hostfxr-latest-x64.rpm
[sles-12-hostfxr-checksum-3.0.X]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/release/3.0/dotnet-hostfxr-latest-x64.rpm.sha512
[sles-12-sharedfx-3.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/3.0/dotnet-runtime-latest-x64.rpm
[sles-12-sharedfx-checksum-3.0.X]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/release/3.0/dotnet-runtime-latest-x64.rpm.sha512

[sles-12-badge-2.2.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.2/sharedfx_sles.12-x64_Release_version_badge.svg
[sles-12-version-2.2.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.2/latest.version
[sles-12-host-2.2.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.2/dotnet-host-latest-x64.rpm
[sles-12-runtime-deps-2.2.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.2/dotnet-runtime-deps-latest-sles.12-x64.rpm
[sles-12-runtime-deps-checksum-2.2.X]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/release/2.2/dotnet-runtime-deps-latest-sles.12-x64.rpm.sha512
[sles-12-host-checksum-2.2.X]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/release/2.2/dotnet-host-latest-x64.rpm.sha512
[sles-12-hostfxr-2.2.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.2/dotnet-hostfxr-latest-x64.rpm
[sles-12-hostfxr-checksum-2.2.X]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/release/2.2/dotnet-hostfxr-latest-x64.rpm.sha512
[sles-12-sharedfx-2.2.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.2/dotnet-runtime-latest-x64.rpm
[sles-12-sharedfx-checksum-2.2.X]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/release/2.2/dotnet-runtime-latest-x64.rpm.sha512

[sles-12-badge-2.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.1/sharedfx_sles.12-x64_Release_version_badge.svg
[sles-12-version-2.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.1/latest.version
[sles-12-host-2.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.1/dotnet-host-latest-x64.rpm
[sles-12-runtime-deps-2.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.1/dotnet-runtime-deps-latest-sles.12-x64.rpm
[sles-12-runtime-deps-checksum-2.1.X]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/release/2.1/dotnet-runtime-deps-latest-sles.12-x64.rpm.sha512
[sles-12-host-checksum-2.1.X]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/release/2.1/dotnet-host-latest-x64.rpm.sha512
[sles-12-hostfxr-2.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.1/dotnet-hostfxr-latest-x64.rpm
[sles-12-hostfxr-checksum-2.1.X]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/release/2.1/dotnet-hostfxr-latest-x64.rpm.sha512
[sles-12-sharedfx-2.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.1/dotnet-runtime-latest-x64.rpm
[sles-12-sharedfx-checksum-2.1.X]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/release/2.1/dotnet-runtime-latest-x64.rpm.sha512


[OpenSUSE-42-badge-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/sharedfx_opensuse.42-x64_Release_version_badge.svg
[OpenSUSE-42-version-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/latest.version
[OpenSUSE-42-runtime-deps-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/dotnet-runtime-deps-latest-opensuse.42-x64.rpm
[OpenSUSE-42-runtime-deps-checksum-master]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/master/dotnet-runtime-deps-latest-opensuse.42-x64.rpm.sha512
[OpenSUSE-42-host-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/dotnet-host-latest-opensuse.42-x64.rpm
[OpenSUSE-42-host-checksum-master]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/master/dotnet-host-latest-opensuse.42-x64.rpm.sha512
[OpenSUSE-42-hostfxr-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/dotnet-hostfxr-latest-opensuse.42-x64.rpm
[OpenSUSE-42-hostfxr-checksum-master]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/master/dotnet-hostfxr-latest-opensuse.42-x64.rpm.sha512
[OpenSUSE-42-sharedfx-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/dotnet-runtime-latest-opensuse.42-x64.rpm
[OpenSUSE-42-sharedfx-checksum-master]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/master/dotnet-runtime-latest-opensuse.42-x64.rpm.sha512

[OpenSUSE-42-badge-3.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/3.0/sharedfx_opensuse.42-x64_Release_version_badge.svg
[OpenSUSE-42-version-3.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/3.0/latest.version
[OpenSUSE-42-runtime-deps-3.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/3.0/dotnet-runtime-deps-latest-opensuse.42-x64.rpm
[OpenSUSE-42-runtime-deps-checksum-3.0.X]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/release/3.0/dotnet-runtime-deps-latest-opensuse.42-x64.rpm.sha512
[OpenSUSE-42-host-3.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/3.0/dotnet-host-latest-opensuse.42-x64.rpm
[OpenSUSE-42-host-checksum-3.0.X]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/release/3.0/dotnet-host-latest-opensuse.42-x64.rpm.sha512
[OpenSUSE-42-hostfxr-3.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/3.0/dotnet-hostfxr-latest-opensuse.42-x64.rpm
[OpenSUSE-42-hostfxr-checksum-3.0.X]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/release/3.0/dotnet-hostfxr-latest-opensuse.42-x64.rpm.sha512
[OpenSUSE-42-sharedfx-3.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/3.0/dotnet-runtime-latest-opensuse.42-x64.rpm
[OpenSUSE-42-sharedfx-checksum-3.0.X]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/release/3.0/dotnet-runtime-latest-opensuse.42-x64.rpm.sha512

[OpenSUSE-42-badge-2.2.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.2/sharedfx_opensuse.42-x64_Release_version_badge.svg
[OpenSUSE-42-version-2.2.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.2/latest.version
[OpenSUSE-42-runtime-deps-2.2.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.2/dotnet-runtime-deps-latest-opensuse.42-x64.rpm
[OpenSUSE-42-runtime-deps-checksum-2.2.X]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/release/2.2/dotnet-runtime-deps-latest-opensuse.42-x64.rpm.sha512
[OpenSUSE-42-host-2.2.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.2/dotnet-host-latest-opensuse.42-x64.rpm
[OpenSUSE-42-host-checksum-2.2.X]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/release/2.2/dotnet-host-latest-opensuse.42-x64.rpm.sha512
[OpenSUSE-42-hostfxr-2.2.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.2/dotnet-hostfxr-latest-opensuse.42-x64.rpm
[OpenSUSE-42-hostfxr-checksum-2.2.X]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/release/2.2/dotnet-hostfxr-latest-opensuse.42-x64.rpm.sha512
[OpenSUSE-42-sharedfx-2.2.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.2/dotnet-runtime-latest-opensuse.42-x64.rpm
[OpenSUSE-42-sharedfx-checksum-2.2.X]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/release/2.2/dotnet-runtime-latest-opensuse.42-x64.rpm.sha512

[OpenSUSE-42-badge-2.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.1/sharedfx_opensuse.42-x64_Release_version_badge.svg
[OpenSUSE-42-version-2.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.1/latest.version
[OpenSUSE-42-runtime-deps-2.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.1/dotnet-runtime-deps-latest-opensuse.42-x64.rpm
[OpenSUSE-42-runtime-deps-checksum-2.1.X]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/release/2.1/dotnet-runtime-deps-latest-opensuse.42-x64.rpm.sha512
[OpenSUSE-42-host-2.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.1/dotnet-host-latest-opensuse.42-x64.rpm
[OpenSUSE-42-host-checksum-2.1.X]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/release/2.1/dotnet-host-latest-opensuse.42-x64.rpm.sha512
[OpenSUSE-42-hostfxr-2.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.1/dotnet-hostfxr-latest-opensuse.42-x64.rpm
[OpenSUSE-42-hostfxr-checksum-2.1.X]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/release/2.1/dotnet-hostfxr-latest-opensuse.42-x64.rpm.sha512
[OpenSUSE-42-sharedfx-2.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.1/dotnet-runtime-latest-opensuse.42-x64.rpm
[OpenSUSE-42-sharedfx-checksum-2.1.X]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/release/2.1/dotnet-runtime-latest-opensuse.42-x64.rpm.sha512


[linux-musl-x64-badge-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/sharedfx_linux-musl-x64_Release_version_badge.svg
[linux-musl-x64-version-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/latest.version
[linux-musl-x64-targz-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/dotnet-runtime-latest-linux-musl-x64.tar.gz
[linux-musl-x64-targz-checksum-master]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/master/dotnet-runtime-latest-linux-musl-x64.tar.gz.sha512
[linux-musl-x64-nethost-targz-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/dotnet-nethost-latest-linux-musl-x64.tar.gz
[linux-musl-x64-symbols-targz-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/dotnet-runtime-symbols-latest-linux-musl-x64.tar.gz

[linux-musl-x64-badge-3.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/3.0/sharedfx_linux-musl-x64_Release_version_badge.svg
[linux-musl-x64-version-3.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/3.0/latest.version
[linux-musl-x64-targz-3.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/3.0/dotnet-runtime-latest-linux-musl-x64.tar.gz
[linux-musl-x64-targz-checksum-3.0.X]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/release/3.0/dotnet-runtime-latest-linux-musl-x64.tar.gz.sha512
[linux-musl-x64-nethost-targz-3.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/3.0/dotnet-nethost-latest-linux-musl-x64.tar.gz
[linux-musl-x64-symbols-targz-3.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/3.0/dotnet-runtime-symbols-latest-linux-musl-x64.tar.gz

[linux-musl-x64-badge-2.2.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.2/sharedfx_linux-musl-x64_Release_version_badge.svg
[linux-musl-x64-version-2.2.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.2/latest.version
[linux-musl-x64-targz-2.2.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.2/dotnet-runtime-latest-linux-musl-x64.tar.gz
[linux-musl-x64-targz-checksum-2.2.X]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/release/2.2/dotnet-runtime-latest-linux-musl-x64.tar.gz.sha512
[linux-musl-x64-symbols-targz-2.2.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.2/dotnet-runtime-symbols-latest-linux-musl-x64.tar.gz

[linux-musl-x64-badge-2.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.1/sharedfx_linux-musl-x64_Release_version_badge.svg
[linux-musl-x64-version-2.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.1/latest.version
[linux-musl-x64-targz-2.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.1/dotnet-runtime-latest-linux-musl-x64.tar.gz
[linux-musl-x64-targz-checksum-2.1.X]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/release/2.1/dotnet-runtime-latest-linux-musl-x64.tar.gz.sha512
[linux-musl-x64-symbols-targz-2.1.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.1/dotnet-runtime-symbols-latest-linux-musl-x64.tar.gz


[linux-musl-arm64-badge-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/sharedfx_linux-musl-arm64_Release_version_badge.svg
[linux-musl-arm64-version-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/latest.version
[linux-musl-arm64-targz-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/dotnet-runtime-latest-linux-musl-arm64.tar.gz
[linux-musl-arm64-targz-checksum-master]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/master/dotnet-runtime-latest-linux-musl-arm64.tar.gz.sha512
[linux-musl-arm64-nethost-targz-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/dotnet-nethost-latest-linux-musl-arm64.tar.gz
[linux-musl-arm64-symbols-targz-master]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/dotnet-runtime-symbols-latest-linux-musl-arm64.tar.gz

[linux-musl-arm64-badge-3.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/3.0/sharedfx_linux-musl-arm64_Release_version_badge.svg
[linux-musl-arm64-version-3.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/3.0/latest.version
[linux-musl-arm64-targz-3.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/3.0/dotnet-runtime-latest-linux-musl-arm64.tar.gz
[linux-musl-arm64-targz-checksum-3.0.X]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Runtime/release/3.0/dotnet-runtime-latest-linux-musl-arm64.tar.gz.sha512
[linux-musl-arm64-nethost-targz-3.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/3.0/dotnet-nethost-latest-linux-musl-arm64.tar.gz
[linux-musl-arm64-symbols-targz-3.0.X]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/3.0/dotnet-runtime-symbols-latest-linux-musl-arm64.tar.gz

<!-- END links to include in table -->

## .NET Foundation

.NET Core is a [.NET Foundation](https://www.dotnetfoundation.org/projects) project.

There are many .NET related projects on GitHub.

- [.NET home repo](https://github.com/Microsoft/dotnet) - links to 100s of .NET projects, from Microsoft and the community.
- [ASP.NET Core home](https://github.com/aspnet/home) - the best place to start learning about ASP.NET Core.

This project has adopted the code of conduct defined by the [Contributor Covenant](http://contributor-covenant.org/) to clarify expected behavior in our community. For more information, see the [.NET Foundation Code of Conduct](http://www.dotnetfoundation.org/code-of-conduct).

General .NET OSS discussions: [.NET Foundation forums](https://forums.dotnetfoundation.org)

## License

.NET (including the runtime repo) is licensed under the [MIT](LICENSE.TXT) license.
