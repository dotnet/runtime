Build CoreCLR on Linux
======================

This guide will walk you through building CoreCLR on Linux.  Before building there is environment setup that needs to happen to pull in all the dependencies required by the build.  There are two suggested ways to go about doing this. First you are able to use the Docker environments provided by https://github.com/dotnet/dotnet-buildtools-prereqs-docker, or you can set up the environment yourself. The documentation will go over both ways of building. Note that using docker only allows you to leverage our existing images which have a setup environment.

[Build using Docker](#Build-using-Docker)

[Build with own environment](#Environment)

Build using Docker
==================

Install Docker, see https://docs.docker.com/install/

Building using Docker will require that you choose the correct image for your environment. Note that the OS is strictly speaking not extremely important, for example if you are on Ubuntu 18.04 and build using the Ubuntu 16.04 x64 image there should be no issues. The target architecture is more important, as building arm32 using the x64 image will not work, there will be missing rootfs components required by the build. See [Docker Images](#Docker-Images) for more information on choosing an image to build with.

Please note that when choosing an image choosing the same image as the host os you are running on you will allow you to run the product/tests outside of the docker container you built in.

Once you have chosen an image the build is one command run from the root of the runtime repository:

```sh
docker run --rm -v <RUNTIME_REPO_PATH>:/runtime -w /runtime mcr.microsoft.com/dotnet-buildtools/prereqs:ubuntu-16.04-20200508132555-78cbb55 ./build.sh --subset clr -clang9
```

Dissecting the command:

`--rm`: erase the created container after use

`-v <RUNTIME_REPO_PATH>:/runtime`: mount the runtime repository under `/runtime`

`-w: /runtime`: set /runtime as working directory for the container

`mcr.microsoft.com/dotnet-buildtools/prereqs:ubuntu-16.04-20200508132555-78cbb55`: image name.

`./build.sh`: command to be run in the container, run the build to coreclr.

`--subset clr`: build the runtime subset (excluding libraries and installers)

`-clang9`: argument to use clang 9 for the build, only compiler in the build image.

If you are attempting to cross build for arm/arm64 then use the crossrootfs location to set the ROOTFS_DIR. The command would add `-e ROOTFS_DIR=<crossrootfs location>`. See [Docker Images](#Docker-Images) for the crossrootfs location. In addition you will need to specify `--cross`.

```sh
docker run --rm -v <RUNTIME_REPO_PATH>:/runtime -w /runtime -e ROOTFS_DIR=/crossrootfs/arm64 mcr.microsoft.com/dotnet-buildtools/prereqs:ubuntu-16.04-cross-arm64-20200508132638-b2c2436 ./build.sh --arch arm64 --cross --subset clr
```

Note that instructions on building the crossrootfs location can be found at [cross-building.md](cross-building.md). These instructions are suggested only if there are plans to change the rootfs, or the Docker images for arm/arm64 are insufficient for you build.

Docker Images
=============

These instructions might fall stale often enough as we change our images as our requirements change. The table below is just a quick reference view of the images we use in different build scenarios. The ones that we use for our our official builds can be found in [the platform matrix](../../../../eng/pipelines/common/platform-matrix.yml) of our Azure DevOps builds under the `container` key of the platform you plan to build.

| OS                          | Target Arch     | Image location                                                                                       | crossrootfs location | Clang Version |
| --------------------------- | --------------- | ---------------------------------------------------------------------------------------------------- | -------------------- | ------------- |
| Ubuntu 16.04                | x64             | `mcr.microsoft.com/dotnet-buildtools/prereqs:ubuntu-16.04-a50a721-20191120200116`                    | -                    | -clang9       |
| Alpine                      | x64             | `mcr.microsoft.com/dotnet-buildtools/prereqs:alpine-3.9-WithNode-0fc54a3-20190918214015`             | -                    | -clang9       |
| CentOS 6 (build for RHEL 6) | x64             | `mcr.microsoft.com/dotnet-buildtools/prereqs:centos-6-f39df28-20191023143802`                        | -                    | -clang9       |
| CentOS 7 (build for RHEL 7) | x64             | `mcr.microsoft.com/dotnet-buildtools/prereqs:centos-7-f39df28-20191023143754`                        | -                    | -clang9       |
| Ubuntu 16.04                | arm32(armhf)    | `mcr.microsoft.com/dotnet-buildtools/prereqs:ubuntu-16.04-cross-20200413125008-09ec757`              | `/crossrootfs/arm`   | -clang9       |
| Ubuntu 16.04                | arm64 (arm64v8) | `mcr.microsoft.com/dotnet-buildtools/prereqs:ubuntu-16.04-cross-arm64-20200413125008-cfdd435`        | `/crossrootfs/arm64` | -clang9       |
| Alpine                      | arm64 (arm64v8) | `mcr.microsoft.com/dotnet-buildtools/prereqs:ubuntu-16.04-cross-arm64-alpine-20200413125008-406629a` | `/crossrootfs/arm64` | -clang5.0     |

Environment
===========

These instructions are written assuming the Ubuntu 16.04/18.04 LTS and CentOS images, since those are the distros the team and the official builds use. Pull Requests are welcome to address other environments as long as they don't break the ability to use these.

Minimum RAM required to build is 1GB. The build is known to fail on 512 MB VMs ([Issue 536](https://github.com/dotnet/coreclr/issues/536)).

Toolchain Setup
---------------

Follow instructions and install dependencies listed [here](https://github.com/dotnet/runtime/blob/master/docs/workflow/requirements/linux-requirements.md#toolchain-setup).

Git Setup
---------

This guide assumes that you've cloned the runtime repository.

Set the maximum number of file-handles
--------------------------------------

To ensure that your system can allocate enough file-handles for the libraries build run `sysctl fs.file-max`. If it is less than 100000, add `fs.file-max = 100000` to `/etc/sysctl.conf`, and then run `sudo sysctl -p`.

Build the Runtime and System.Private.CoreLib
=============================================

To build the runtime on Linux, run build.sh to build the CoreCLR subset category of the runtime:

```
./build.sh -subset clr
```

After the build is completed, there should some files placed in `artifacts/bin/coreclr/Linux.x64.Debug`.  The ones we are most interested in are:

* `corerun`: The command line host.  This program loads and starts the CoreCLR runtime and passes the managed program you want to run to it.
* `libcoreclr.so`: The CoreCLR runtime itself.
* `System.Private.CoreLib.dll`: The core managed library, containing definitions of `Object` and base functionality.

Create the Core_Root
===================

The Core_Root folder will have the built binaries, from `build.sh` and it will also include the library packages required to run tests.

```
./src/coreclr/build-test.sh generatelayoutonly
```

After the build is complete you will be able to find the output in the `artifacts/tests/coreclr/Linux.x64.Debug/Tests/Core_Root` folder.

Running a single test
===================

After `src/coreclr/build-test.sh` is run, corerun from the Core_Root folder is ready to be run. This can be done by using the full absolute path to corerun, or by setting an environment variable to the Core_Root folder.

```sh
export CORE_ROOT=/runtime/artifacts/tests/coreclr/Linux.x64.Debug/Tests/Core_Root
$CORE_ROOT/corerun hello_world.dll
```
