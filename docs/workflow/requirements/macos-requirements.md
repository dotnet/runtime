Requirements to build dotnet/runtime on macOS
=====================

This guide will walk you through the requirements needed to build dotnet/runtime on macOS. We'll start by showing how to set up your environment from scratch.

Environment
===========

These instructions were validated on macOS 10.13 High Sierra.

Xcode
-----

Install Apple Xcode developer tools from the Mac App Store ([link](https://apps.apple.com/us/app/xcode/id497799835)).

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
