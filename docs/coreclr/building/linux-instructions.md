Build CoreCLR on Linux
======================

This guide will walk you through building CoreCLR on Linux.  Before building there is environment setup that needs to happen to pull in all the dependencies required by the build.  There are two suggested ways to go about doing this. First you are able to use the Docker environments provided by https://github.com/dotnet/dotnet-buildtools-prereqs-docker, or you can set up the environment yourself. The documentation will go over both ways of building. Note that using docker only allows you to leverage our existing images which have a setup environment.

[Build using Docker](#Build-using-Docker)

[Build with own environment](#Environment)

Build using Docker
==================

Install Docker, see https://docs.docker.com/install/

Building using Docker will require that you choose the correct image for your environment. Note that the OS is strictly speaking not extremely important, for example if you are on Ubuntu 18.04 and build using the Ubuntu 16.04 x64 image there should be no issues. The target architecture is more important, as building arm32 using the x64 image will not work, there will be missing rootfs components required by the build. See [Docker Images](#Docker-Images) for more information on choosing an image to build with.

Please note that when choosing an image choosing the same image as the host os you are running on you willa allow you to run the product/tests outside of the docker container you built in.

Once you have chosen an image the build is one command run from the root of the coreclr repository:

```sh
docker run --rm -v /home/dotnet-bot/coreclr:/coreclr -w /coreclr mcr.microsoft.com/dotnet-buildtools/prereqs:ubuntu-16.04-c103199-20180628134544 ./build.sh
```

Dissecting the command:

`--rm: erase the created container after use`

`-v: mount the coreclr repository under /coreclr`

`-w: set /coreclr as working directory for the container`

`mcr.microsoft.com/dotnet-buildtools/prereqs:ubuntu-16.04-c103199-20180628134544: image name`

`./build.sh: command to be run in the container`

If you are attempting to cross build for arm/arm64 then use the crossrootfs location to set the ROOTFS_DIR. The command would add `-e ROOTFS_DIR=<crossrootfs location>`. See [Docker Images](#Docker-Images) for the crossrootfs location. In addition you will need to specify `cross`.

```sh
docker run --rm -v /home/dotnet-bot/coreclr:/coreclr -w /coreclr -e ROOTFS_DIR=/crossrootfs/arm64 mcr.microsoft.com/dotnet-buildtools/prereqs:ubuntu-16.04-cross-arm64-a3ae44b-20180315221921 ./build.sh arm64 cross
```

Note that instructions on building the crossrootfs location can be found at https://github.com/dotnet/coreclr/blob/master/Documentation/building/cross-building.md. These instructions are suggested only if there are plans to change the rootfs, or the Docker images for arm/arm64 are insufficient for you build.

Docker Images
=============

| OS                          | Target Arch     | Image location                                                                                      | crossrootfs location | Clang Version |
| --------------------------- | --------------- | --------------------------------------------------------------------------------------------------- | -------------------- | ------------- |
| Ubuntu 16.04                | x64             | `mcr.microsoft.com/dotnet-buildtools/prereqs:ubuntu-16.04-c103199-20180628134544`                   | -                    | -             |
| Alpine                      | x64             | `mcr.microsoft.com/dotnet-buildtools/prereqs:alpine-3.6-e2521f8-20180716231200`                     | -                    | -             |
| CentOS 6 (build for RHEL 6) | x64             | `mcr.microsoft.com/dotnet-buildtools/prereqs:centos-6-376e1a3-20174311014331`                       | -                    | -             |
| CentOS 7 (build for RHEL 7) | x64             | `mcr.microsoft.com/dotnet-buildtools/prereqs:centos-7-d485f41-20173404063424`                       | -                    | -             |
| Ubuntu 16.04                | arm32(armhf)    | `mcr.microsoft.com/dotnet-buildtools/prereqs:ubuntu-16.04-cross-14.04-23cacb0-20190528233931`             | `/crossrootfs/arm`   | -             |
| Ubuntu 16.04                | arm64 (arm64v8) | `mcr.microsoft.com/dotnet-buildtools/prereqs:ubuntu-16.04-cross-arm64-a3ae44b-20180315221921`       | `/crossrootfs/arm64` | -             |
| Alpine                      | arm64 (arm64v8) | `mcr.microsoft.com/dotnet-buildtools/prereqs:ubuntu-16.04-cross-arm64-alpine10fcdcf-20190208200917` | `/crossrootfs/arm64` | -clang5.0     |

Environment
===========

These instructions are written assuming the Ubuntu 16.04/18.04 LTS, since that's the distro the team uses. Pull Requests are welcome to address other environments as long as they don't break the ability to use Ubuntu 16.04/18.04 LTS.

Minimum RAM required to build is 1GB. The build is known to fail on 512 MB VMs ([Issue 536](https://github.com/dotnet/coreclr/issues/536)).

Toolchain Setup
---------------

Install the following packages for the toolchain: 

- cmake 
- llvm-3.9
- clang-3.9
- lldb-3.9
- liblldb-3.9-dev
- libunwind8 
- libunwind8-dev
- gettext
- libicu-dev
- liblttng-ust-dev
- libcurl4-openssl-dev
- libssl-dev
- libkrb5-dev
- libnuma-dev (optional, enables numa support)

Note: ARM clang has a known issue with CompareExchange
([#15074](https://github.com/dotnet/coreclr/issues/15074)), so for ARM you must
use clang-4.0 or higher.  Moreover, when building with clang-5.0, the
following errors occur:

```
coreclr/src/debug/inc/arm/primitives.h:66:1: error: __declspec attribute 'selectany' is
      not supported [-Werror,-Wignored-attributes]
```

This is fixed in clang-5.0.2, which can be installed from the apt
repository listed below.

For other version of Debian/Ubuntu, please visit http://apt.llvm.org/.

Then install the packages you need:

    ~$ sudo apt-get install cmake llvm-3.9 clang-3.9 lldb-3.9 liblldb-3.9-dev libunwind8 libunwind8-dev gettext libicu-dev liblttng-ust-dev libcurl4-openssl-dev libssl-dev libnuma-dev libkrb5-dev

You now have all the required components.

If you are using Fedora, then you will need to install the following packages:

    ~$ sudo dnf install llvm cmake clang lldb-devel libunwind-devel lttng-ust-devel libicu-devel numactl-devel

Git Setup
---------

This guide assumes that you've cloned the coreclr repository.

Set the maximum number of file-handles
--------------------------------------

To ensure that your system can allocate enough file-handles for the corefx build run `sysctl fs.file-max`. If it is less than 100000, add `fs.file-max = 100000` to `/etc/sysctl.conf`, and then run `sudo sysctl -p`.

On Fedora:

`$ sudo dnf install mono-devel`

Build the Runtime and Microsoft Core Library
=============================================

To build the runtime on Linux, run build.sh from the root of the coreclr repository:

```
./build.sh
```

After the build is completed, there should some files placed in `bin/Product/Linux.x64.Debug`.  The ones we are interested in are:

* `corerun`: The command line host.  This program loads and starts the CoreCLR runtime and passes the managed program you want to run to it.
* `libcoreclr.so`: The CoreCLR runtime itself.
* `System.Private.CoreLib.dll`: Microsoft Core Library.

Create the Core_Root
===================

The Core_Root folder will have the built binaries, from `build.sh` and it will also include the CoreFX packages required to run tests.

```
./build-test.sh generatelayoutonly
```

After the build is complete you will be able to find the output in the `bin/tests/Linux.x64.Debug/Tests/Core_Root` folder.

Running a single test
===================

After `build-test.sh` is run, corerun from the Core_Root folder is ready to be run. This can be done by using the full absolute path to corerun, or by setting an environment variable to the Core_Root folder.

```sh
export CORE_ROOT=/home/dotnet-bot/coreclr/bin/tests/Linux.x64.Debug/Tests/Core_Root
$CORE_ROOT/corerun hello_world.dll
```
