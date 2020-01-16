Requirements to build dotnet/runtime on macOS
=====================

This guide will walk you through the requirements needed to build dotnet/runtime on macOS. We'll start by showing how to set up your environment from scratch.

Environment
===========

These instructions were validated on macOS 10.13 High Sierra.

Xcode
-----

Install Apple Xcode developer tools from the Mac App Store ([link](https://apps.apple.com/us/app/xcode/id497799835)).

Toolchain Setup
---------------

Building dotnet/runtime requires CMake 3.15.5 or newer.  You can download it from [CMake downloads](http://www.cmake.org/download/).

For ease of use, you should setup the entire toolchain using [Homebrew](http://brew.sh).

Install the following packages:

- cmake
- autoconf
- automake
- libtool
- pkg-config
- python3
- icu4c

The lines to install all the packages above:

```
brew install cmake autoconf automake libtool pkg-config python3 icu4c
brew link --force icu4c
```
