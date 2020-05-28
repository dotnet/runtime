Requirements to build dotnet/runtime on Linux
======================

This guide will walk you through the requirements to build dotnet/runtime on Linux.  Before building there is environment setup that needs to happen to pull in all the dependencies required by the build.  There are two suggested ways to go about doing this. First you are able to use the Docker environments provided by https://github.com/dotnet/dotnet-buildtools-prereqs-docker, or you can set up the environment yourself. The documentation will go over both ways of building. Note that using docker only allows you to leverage our existing images which have a setup environment.

Docker
==================

Install Docker, see https://docs.docker.com/install/

Building using Docker will require that you choose the correct image for your environment. Note that the OS is strictly speaking not extremely important, for example if you are on Ubuntu 18.04 and build using the Ubuntu 16.04 x64 image there should be no issues. The target architecture is more important, as building arm32 using the x64 image will not work, there will be missing rootfs components required by the build. See [Docker Images](#Docker-Images) for more information on choosing an image to build with.

Please note that when choosing an image choosing the same image as the host os you are running on you will allow you to run the product/tests outside of the docker container you built in.

Once you have chosen an image the build is one command run from the root of the dotnet/runtime repository:

```sh
docker run --rm -v /home/dotnet-bot/runtime:/runtime -w /runtime mcr.microsoft.com/dotnet-buildtools/prereqs:ubuntu-16.04-c103199-20180628134544 ./build.sh
```

Dissecting the command:

`--rm: erase the created container after use`

`-v: mount the runtime repository under /dotnet/runtime`

`-w: set /runtime as working directory for the container`

`mcr.microsoft.com/dotnet-buildtools/prereqs:ubuntu-16.04-c103199-20180628134544: image name`

`./build.sh: command to be run in the container`

If you are attempting to cross build the CoreCLR runtime for arm/arm64 then use the crossrootfs location to set the ROOTFS_DIR. The command would add `-e ROOTFS_DIR=<crossrootfs location>`. See [Docker Images](#Docker-Images) for the crossrootfs location. In addition you will need to specify `cross`.

```sh
docker run --rm -v /home/dotnet-bot/runtime/src/coreclr:/coreclr -w /coreclr -e ROOTFS_DIR=/crossrootfs/arm64 mcr.microsoft.com/dotnet-buildtools/prereqs:ubuntu-16.04-cross-arm64-a3ae44b-20180315221921 ./build.sh arm64 cross
```

Note that instructions on building the crossrootfs location can be found at https://github.com/dotnet/runtime/blob/master/docs/workflow/building/coreclr/cross-building.md. These instructions are suggested only if there are plans to change the rootfs, or the Docker images for arm/arm64 are insufficient for your build.

Docker Images
=============

These instructions might fall stale often enough as we change our images as our requirements change. The table below is just a quick reference view of the images we use in different build scenarios. The ones that we use for our our official builds can be found in [the platform matrix](../../../eng/pipelines/common/platform-matrix.yml) of our Azure DevOps builds under the `container` key of the platform you plan to build.

| OS                             | Target Arch     | Image location                                                                                       | crossrootfs location |
| ------------------------------ | --------------- | ---------------------------------------------------------------------------------------------------- | -------------------- |
| Alpine                         | x64             | `mcr.microsoft.com/dotnet-buildtools/prereqs:alpine-3.9-WithNode-0fc54a3-20190918214015`             | -                    |
| CentOS 6 (build for RHEL 6)    | x64             | `mcr.microsoft.com/dotnet-buildtools/prereqs:centos-6-6aaa05d-20191106231336`                        | -                    |
| CentOS 7 (build for Linux x64) | x64             | `mcr.microsoft.com/dotnet-buildtools/prereqs:centos-7-6aaa05d-20191106231356`                        | -                    |
| Ubuntu 16.04                   | arm32(armhf)    | `mcr.microsoft.com/dotnet-buildtools/prereqs:ubuntu-18.04-cross-arm-16.04-09ec757-20200324125113`        | `/crossrootfs/arm`   |
| Ubuntu 16.04                   | arm64 (arm64v8) | `mcr.microsoft.com/dotnet-buildtools/prereqs:ubuntu-16.04-cross-arm64-cfdd435-20191023143847`        | `/crossrootfs/arm64` |
| Alpine                         | arm64 (arm64v8) | `mcr.microsoft.com/dotnet-buildtools/prereqs:ubuntu-16.04-cross-arm64-alpine-406629a-20191023143847` | `/crossrootfs/arm64` |

Environment
===========

These instructions are written assuming the Ubuntu 16.04/18.04 LTS, since that's the distro the team uses. Pull Requests are welcome to address other environments as long as they don't break the ability to use Ubuntu 16.04/18.04 LTS.

Minimum RAM required to build is 1GB. The build is known to fail on 512 MB VMs ([Issue 536](https://github.com/dotnet/coreclr/issues/536)).

Toolchain Setup
---------------

Building the repo requires CMake 3.14.2 or newer on Linux. Add Kitware's APT feed to your configuration for a newer version of CMake. See their instructions at <https://apt.kitware.com/>. Also, add LLVM/s APT feed to your configuration for a newer version of CMake. See their instructions as <http://apt.llvm.org/>.

Install the following packages for the toolchain:

- cmake
- llvm-9
- clang-9
- autoconf
- automake
- libtool
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

The following dependencies are needed if Mono Runtime is enabled (default behavior):

- autoconf
- automake
- libtool 

    ~$ sudo apt-get install cmake llvm-9 clang-9 autoconf automake libtool build-essential python curl git lldb-6.0 liblldb-6.0-dev libunwind8 libunwind8-dev gettext libicu-dev liblttng-ust-dev libssl-dev libnuma-dev libkrb5-dev zlib1g-dev autoconf automake libtool

You now have all the required components.
