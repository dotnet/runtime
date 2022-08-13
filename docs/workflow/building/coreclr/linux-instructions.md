Build CoreCLR on Linux
======================

This guide will walk you through building CoreCLR on Linux.

First, set up your environment to build using the instructions [here](../../requirements/linux-requirements.md).

Choose one of the following options for building.

[Build using Docker](#Build-using-Docker)

[Build with own environment](#Environment)

Build using Docker
==================

Building using Docker will require that you choose the correct image for your environment. Note that the OS is strictly speaking not extremely important, for example if you are on Ubuntu 18.04 and build using the Ubuntu 16.04 x64 image there should be no issues. The target architecture is more important, as building arm32 using the x64 image will not work: there will be missing rootfs components required by the build. See [Docker Images](#Docker-Images), below, for more information on choosing an image to build with.

Please note that when choosing an image choosing the same image as the host os you are running on you will allow you to run the product/tests outside of the docker container you built in.

Once you have chosen an image the build is one command run from the root of the runtime repository:

```sh
docker run --rm -v <RUNTIME_REPO_PATH>:/runtime -w /runtime mcr.microsoft.com/dotnet-buildtools/prereqs:ubuntu-16.04-20200508132555-78cbb55 ./build.sh -subset clr -clang9
```

Dissecting the command:

- `--rm`: erase the created container after use.
- `-v <RUNTIME_REPO_PATH>:/runtime`: mount the runtime repository under `/runtime`. Replace `<RUNTIME_REPO_PATH>` with the full path to your `runtime` repo clone, e.g., `-v /home/user/runtime:/runtime`.
- `-w: /runtime`: set /runtime as working directory for the container.
- `mcr.microsoft.com/dotnet-buildtools/prereqs:ubuntu-16.04-20200508132555-78cbb55`: Docker image name.
- `./build.sh`: command to be run in the container: run the root build command.
- `-subset clr`: build the clr subset (excluding libraries and installers).
- `-clang9`: argument to use clang 9 for the build (the only compiler in the build image).

If you are attempting to cross build for arm or arm64 then use the crossrootfs location to set the ROOTFS_DIR. The command would add `-e ROOTFS_DIR=<crossrootfs location>`. See [Docker Images](#Docker-Images) for the crossrootfs location. In addition you will need to specify `-cross`. For example:

```sh
docker run --rm -v <RUNTIME_REPO_PATH>:/runtime -w /runtime -e ROOTFS_DIR=/crossrootfs/arm64 mcr.microsoft.com/dotnet-buildtools/prereqs:ubuntu-16.04-cross-arm64-20200508132638-b2c2436 ./build.sh -arch arm64 -subset clr -cross -clang9
```

Note that instructions on building the crossrootfs location can be found at [cross-building.md](cross-building.md). These instructions are suggested only if there are plans to change the rootfs, or the Docker images for arm/arm64 are insufficient for you build.

Docker Images
=============

This table of images might often become stale as we change our images as our requirements change. The images used for our our official builds can be found in [the platform matrix](../../../../eng/pipelines/common/platform-matrix.yml) of our Azure DevOps builds under the `container` key of the platform you plan to build.

| OS                          | Target Arch     | Image location                                                                                       | crossrootfs location | Clang Version |
| --------------------------- | --------------- | ---------------------------------------------------------------------------------------------------- | -------------------- | ------------- |
| Alpine                      | x64             | `mcr.microsoft.com/dotnet-buildtools/prereqs:alpine-3.13-WithNode-20210910135845-c401c85`             | -                    | -clang5.0     |
| CentOS 7 (build for RHEL 7) | x64             | `mcr.microsoft.com/dotnet-buildtools/prereqs:centos-7-20210714125435-9b5bbc2`                        | -                    | -clang9       |
| Ubuntu 16.04 (x64, arm ROOTFS) | arm32 (armhf)   | `mcr.microsoft.com/dotnet-buildtools/prereqs:ubuntu-16.04-cross-20210719121212-8a8d3be`              | `/crossrootfs/arm`   | -clang9       |
| Ubuntu 16.04  (x64, arm64 ROOTFS) | arm64 (arm64v8) | `mcr.microsoft.com/dotnet-buildtools/prereqs:ubuntu-16.04-cross-arm64-20210719121212-8a8d3be`        | `/crossrootfs/arm64` | -clang9       |
| Alpine (x64, arm ROOTFS)    | arm             | `mcr.microsoft.com/dotnet-buildtools/prereqs:ubuntu-16.04-cross-arm-alpine-20210923140502-78f7860`   | `/crossrootfs/arm64` | -clang9       |
| Alpine (x64, arm64 ROOTFS)  | arm64 (arm64v8) | `mcr.microsoft.com/dotnet-buildtools/prereqs:ubuntu-16.04-cross-arm64-alpine-20210719121212-b2c2436` | `/crossrootfs/arm64` | -clang9       |

Environment
===========

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

The Core_Root folder will contain the built binaries, generated by `build.sh`, as well as the library packages required to run tests. It is required that you build
the libraries subset (`-subset libs`) before this command can be run. Note that this by default searches the libraries in Release mode, regardless of the runtime
configuration you specify. If you built your libs in another configuration, then you have to pass down the appropriate flag `/p:LibrariesConfiguration=<your_config>`.

```
./src/tests/build.sh generatelayoutonly
```

After the build is complete you will find the output in the `artifacts/tests/coreclr/Linux.x64.Debug/Tests/Core_Root` folder.

Running a single test
===================

After `src/tests/build.sh` is run, `corerun` from the Core_Root folder is ready to be run. This can be done by using the full absolute path to `corerun`, or by setting an environment variable to the Core_Root folder.

```sh
export CORE_ROOT=/runtime/artifacts/tests/coreclr/Linux.x64.Debug/Tests/Core_Root
$CORE_ROOT/corerun hello_world.dll
```
