Requirements to build dotnet/runtime on Linux
======================

This guide will walk you through the requirements to build dotnet/runtime on Linux. Before building there is environment setup that needs to happen to pull in all the dependencies required by the build. There are two suggested ways to go about doing this. First you are able to use the Docker environments provided by https://github.com/dotnet/dotnet-buildtools-prereqs-docker, or you can set up the environment yourself. The documentation will go over both ways of building. Using Docker allows you to leverage our existing images which already have an environment set up.

General instructions for building are [here](../README.md).
Instructions for building CoreCLR for Linux are [here](../building/coreclr/linux-instructions.md).


Docker
==================

Install Docker; see https://docs.docker.com/install/.

All the required build tools are included in the Docker images used to do the build, so no additional setup is required.


Environment
===========

These instructions are written assuming the Ubuntu 16.04/18.04 LTS, since that's the distro the team uses. Pull Requests are welcome to address other environments as long as they don't break the ability to use Ubuntu 16.04/18.04 LTS.

Minimum RAM required to build is 1GB. The build is known to fail on 512 MB VMs ([dotnet/runtime#4069](https://github.com/dotnet/runtime/issues/4069)).

Toolchain Setup
---------------

Building the repo requires CMake 3.14.5 or newer on Linux. Add Kitware's APT feed to your configuration for a newer version of CMake. See their instructions at <https://apt.kitware.com/>. You may need to add LLVM's APT feed to your configuration to obtain the required version of clang/LLVM. See their instructions at <https://apt.llvm.org/>.

Install the following packages for the toolchain:

- cmake
- llvm-9
- clang-9
- build-essential
- python
- curl
- git
- lldb-6.0
- liblldb-6.0-dev
- libunwind8
- libunwind8-dev
- gettext
- libicu-dev
- liblttng-ust-dev
- libssl-dev
- libkrb5-dev
- libnuma-dev (optional, enables numa support)
- zlib1g-dev
- ninja-build (optional, enables building native code with ninja instead of make)

```
sudo apt-get install -y cmake llvm-9 clang-9 \
build-essential python curl git lldb-6.0 liblldb-6.0-dev \
libunwind8 libunwind8-dev gettext libicu-dev liblttng-ust-dev \
libssl-dev libnuma-dev libkrb5-dev zlib1g-dev ninja-build
```

You now have all the required components.
*Unsupported OSes*:
In case you have Gentoo you can run following commands:

```
emerge --ask clang dev-util/lttng-ust app-crypt/mit-krb5
```
