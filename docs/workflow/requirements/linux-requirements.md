# Requirements to build dotnet/runtime on Linux <!-- omit in toc -->

* [Docker](#docker)
* [Environment](#environment)
  * [Toolchain Setup](#toolchain-setup)
    * [Additional Requirements for Cross-Building](#additional-requirements-for-cross-building)
      * [Alpine ARM Cross-Building Requirements](#alpine-arm-cross-building-requirements)
  * [Unsupported OSes](#unsupported-oses)

This guide will walk you through the requirements to build _dotnet/runtime_ on Linux. Before building there is environment setup that needs to happen to pull in all the dependencies required by the build.

There are two suggested ways to go about doing this. The first one is you are able to use the [provided Docker environments](https://github.com/dotnet/dotnet-buildtools-prereqs-docker) used in the official builds, or you can set up the environment yourself. The documentation will go over both ways. Using Docker allows you to leverage our existing images which already have an environment set up, while using your own environment grants you better flexibility on having other tools at hand you might need.

## Docker

Install Docker. For further installation instructions, see [here](https://docs.docker.com/install/). You can find the official .NET images in [their Docker hub](https://hub.docker.com/_/microsoft-dotnet). You can also use the images used for the official builds. More details on these ones in the [Linux building instructions doc](/docs/workflow/building/coreclr/linux-instructions.md#docker-images).
All the required build tools are included in the Docker images used to do the build, so no additional setup is required.

## Environment

These instructions are written assuming the current Ubuntu LTS, since that's the officially used distribution. Pull Requests are welcome to address other environments as long as they don't break the ability to use Ubuntu LTS. As a general guideline, this guide was tested using Ubuntu 20.04 LTS (Focal Rossa).

Minimum RAM required to build is 1GB. The build is known to fail on 512 MB VMs ([dotnet/runtime#4069](https://github.com/dotnet/runtime/issues/4069)).

### Toolchain Setup

Building the repo requires CMake 3.14.5 or newer on Linux. Add Kitware's APT feed to your configuration for a newer version of CMake (optional). See their instructions at their [website](https://apt.kitware.com/). You may also need to add LLVM's APT feed to your configuration to obtain the required version of Clang/LLVM (usually not needed). See their instructions [over here](https://apt.llvm.org/).

Install the following packages for the toolchain:

* CMake
* llvm
* clang
* build-essential
* python-is-python3
* curl
* git
* lldb
* liblldb-dev
* libunwind8
* libunwind8-dev
* gettext
* libicu-dev
* liblttng-ust-dev
* libssl-dev
* libkrb5-dev
* libnuma-dev (optional, enables numa support)
* zlib1g-dev
* ninja-build (optional, enables building native code with ninja instead of make)

```bash
sudo apt-get install -y cmake llvm clang \
build-essential python-is-python3 curl git lldb liblldb-dev \
libunwind8 libunwind8-dev gettext libicu-dev liblttng-ust-dev \
libssl-dev libnuma-dev libkrb5-dev zlib1g-dev ninja-build
```

You now have all the required components.

#### Additional Requirements for Cross-Building

If you are planning to use your Linux environment to do cross-building for other architectures (e.g. Arm32, Arm64) and/or other operating systems (e.g. Alpine, FreeBSD), you need to install these additional dependencies:

* qemu
* qemu-user-static
* binfmt-support
* debootstrap
* binutils-arm-linux-gnueabihf _(for Arm32 cross-building)_
* binutils-aarch64-linux-gnu   _(for Arm64 cross-building)_
* binutils-arm-linux-gnueabi   _(for Armel cross-building)_

##### Alpine ARM Cross-Building Requirements

To cross-compile for _linux-musl_, most commonly for Alpine distros, you need to build the `binutils` from their repo, since unfortunately, they are not available as packages like the ones you installed in the encompassing section.

To build them, follow these steps:

* Clone the `musl-cross-make` repo (<https://github.com/richfelker/musl-cross-make>)
* Create a new `config.mak` file in the root of the repo, and add the following lines into it:

```makefile
# Note that these two 'TARGET' clauses are for demonstration purposes only.
# For an actual build, you have to only write one, since they are mutually exclusive.

TARGET = armv6-alpine-linux-musleabihf # if cross-compiling for Arm32
TARGET = aarch64-alpine-linux-musl     # if cross-compiling for Arm64
OUTPUT = /usr
BINUTILS_CONFIG=--enable-gold=yes
```

* Run `make` from the root of the repo.
* Run `sudo make install`, also from the root of the repo.

### Unsupported OSes

In case you have Gentoo you can run following command:

```bash
emerge --ask clang dev-util/lttng-ust app-crypt/mit-krb5
```
