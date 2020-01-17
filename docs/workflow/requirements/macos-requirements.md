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

OpenSSL
-------

To build the libraries on macOS, you must install and configure links for OpenSSL 1.1, and install `pkg-config` to use it. Using [Homebrew](http://brew.sh/):

```sh
brew install pkg-config openssl

# You might need to "link" pkg-config:
brew link pkg-config

# We need to make the runtime libraries discoverable, as well as make
# pkg-config be able to find the headers and current ABI version.
#
# Ensure the paths we will need exist
mkdir -p /usr/local/lib/pkgconfig

# The rest of these instructions assume a default Homebrew path of
# `/usr/local/opt/<module>`, with `brew --prefix` returning `/usr/local`
# and `brew --prefix openssl` returning `/usr/local/opt/openssl@1.1`.
# In this case, `brew info openssl` shows:
# `openssl@1.1: stable 1.1.1d (bottled) [keg-only]`.

# Runtime dependencies
ln -s /usr/local/opt/openssl\@1.1/lib/libcrypto.1.1.dylib /usr/local/lib/
ln -s /usr/local/opt/openssl\@1.1/lib/libssl.1.1.dylib /usr/local/lib/

# Compile-time dependencies (for pkg-config)
ln -s /usr/local/opt/openssl\@1.1/lib/pkgconfig/libcrypto.pc /usr/local/lib/pkgconfig/
ln -s /usr/local/opt/openssl\@1.1/lib/pkgconfig/libssl.pc /usr/local/lib/pkgconfig/
ln -s /usr/local/opt/openssl\@1.1/lib/pkgconfig/openssl.pc /usr/local/lib/pkgconfig/
```
