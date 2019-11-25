Requirements to build dotnet/runtime on macOS
=====================

This guide will walk you through the requirements needed to build dotnet/runtime on macOS. We'll start by showing how to set up your environment from scratch.

Environment
===========

These instructions were validated on macOS 10.13 High Sierra.

If your machine has Command Line Tools for XCode 6.3 installed, you'll need to update them to the 6.3.1 version or higher in order to successfully build. There was an issue with the headers that shipped with version 6.3 that was subsequently fixed in 6.3.1.

CMake
-----

dotnet/runtime has a dependency on CMake 3.15.5 for the build. You can download it from [CMake downloads](http://www.cmake.org/download/).

Alternatively, you can install CMake from [Homebrew](http://brew.sh/).

```sh
brew install cmake
```

ICU
---
ICU (International Components for Unicode) is also required to build and run. It can be obtained via [Homebrew](http://brew.sh/).

```sh
brew install icu4c
brew link --force icu4c
```
