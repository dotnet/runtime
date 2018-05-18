Build CoreCLR on OS X
=====================

This guide will walk you through building CoreCLR on OS X. We'll start by showing how to set up your environment from scratch.

Environment
===========

These instructions were validated on macOS 10.12. Sierra. On older versions coreFX will fail to build properly because of SSL API changes.

If your machine has Command Line Tools for XCode 6.3 installed, you'll need to update them to the 6.3.1 version or higher in order to successfully build. There was an issue with the headers that shipped with version 6.3 that was subsequently fixed in 6.3.1.

Git Setup
---------

Clone the CoreCLR and CoreFX repositories (either upstream or a fork).

```sh
dotnet-mbp:git richlander$ git clone https://github.com/dotnet/coreclr
# Cloning into 'coreclr'...

dotnet-mbp:git richlander$ git clone https://github.com/dotnet/corefx
# Cloning into 'corefx'...
```

This guide assumes that you've cloned the coreclr and corefx repositories into `~/git/coreclr` and `~/git/corefx` on your OS X machine. If your setup is different, you'll need to pay careful attention to the commands you run. In this guide, I'll always show what directory I'm in.

CMake
-----

CoreCLR has a dependency on CMake for the build. You can download it from [CMake downloads](http://www.cmake.org/download/).

Alternatively, you can install CMake from [Homebrew](http://brew.sh/).

```sh
dotnet-mbp:~ richlander$ brew install cmake
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
The CoreFX cryptography libraries are built on OpenSSL. The version of OpenSSL included on OS X (0.9.8) has gone out of support, and a newer version is required. A supported version can be obtained via [Homebrew](http://brew.sh).

```sh
brew install openssl

# We need to make the runtime libraries discoverable, as well as make
# pkg-config be able to find the headers and current ABI version.
#
# Ensure the paths we will need exist
mkdir -p /usr/local/lib/pkgconfig

# The rest of these instructions assume a default Homebrew path of
# /usr/local/opt/<module>, with /usr/local being the answer to
# `brew --prefix`.
#
# Runtime dependencies
ln -s /usr/local/opt/openssl/lib/libcrypto.1.0.0.dylib /usr/local/lib/
ln -s /usr/local/opt/openssl/lib/libssl.1.0.0.dylib /usr/local/lib/

# Compile-time dependences (for pkg-config)
ln -s /usr/local/opt/openssl/lib/pkgconfig/libcrypto.pc /usr/local/lib/pkgconfig/
ln -s /usr/local/opt/openssl/lib/pkgconfig/libssl.pc /usr/local/lib/pkgconfig/
ln -s /usr/local/opt/openssl/lib/pkgconfig/openssl.pc /usr/local/lib/pkgconfig/

# Check pkg-config configuration
pkg-config --libs openssl

it should show something like '-L/usr/local/Cellar/openssl/1.0.2k/lib -lssl -lcrypto'
If pkg-config is not found, run 'brew install pkg-config'
```

If you miss a step or something changes, run clean.sh from top. cmake will cache certain parts and it may not pick up the updates. 


Build the Runtime and Microsoft Core Library
============================================

To Build CoreCLR, run build.sh from the root of the coreclr repo.

```sh
dotnet-mbp:~ richlander$ cd ~/git/coreclr
dotnet-mbp:coreclr richlander$ ./build.sh
```

After the build is completed, there should some files placed in `bin/Product/OSX.x64.Debug`. The ones we are interested in are:

- `corerun`: The command line host. This program loads and starts the CoreCLR runtime and passes the managed program you want to run to it.
- `libcoreclr.dylib`: The CoreCLR runtime itself.
- `System.Private.CoreLib.dll`: Microsoft Core Library.

Build the Framework
===================

```sh
dotnet-mbp:coreclr richlander$ cd ../corefx/
dotnet-mbp:corefx richlander$ ./build.sh
```

After the build is complete you will be able to find the output in the `bin` folder.
