# Build CoreCLR on Linux

* [Build using Docker](#build-using-docker)
  * [Docker Images](#docker-images)
* [Build using your own Environment](#build-using-your-own-environment)
  * [Set the maximum number of file-handles](#set-the-maximum-number-of-file-handles)
* [Build the Runtime](#build-the-runtime)
  * [Cross-Compilation](#cross-compilation)
* [Create the Core_Root](#create-the-core_root)

This guide will walk you through building CoreCLR on Linux.

As mentioned in the [Linux requirements doc](/docs/workflow/requirements/linux-requirements.md), there are two options to build CoreCLR on Linux:

* Build using Docker.
* Build using your own environment.

## Build using Docker

Building using Docker will require that you choose the correct image for your environment.

Note that the OS is strictly speaking not important. For example if you are on Ubuntu 20.04 and build using the Ubuntu 18.04 x64 image there should be no issues. You can even use Linux images on a Windows OS if you have [WSL](https://docs.microsoft.com/windows/wsl/about) enabled. However, note that you can't run multiple OS's on the same _Docker Daemon_, as it takes resources from the underlying kernel as needed. In other words, you can run either Linux on WSL, or Windows containers. You have to switch between them if you need both, and restart Docker.

The target architecture is more important, as building arm32 using the x64 image will not work. There will be missing _rootfs_ components required by the build. See [Docker Images](#docker-images) below, for more information on choosing an image to build with.

**NOTE**: The image's architecture has to match your machine's supported platforms. For example, you can't run arm32 images on an x64 machine. But you could run x64 and arm64 images on an M1 Mac, for example. This is thanks to the _Rosetta_ emulator that Apple Silicon provides. Same case applies to running x86 on an x64 Windows machine thanks to Windows' _SYSWOW64_. Likewise, you can run Linux arm32 images on a Linux arm64 host.

Please note that choosing the same image as the host OS you are running on will allow you to run the product/tests outside of the docker container you built in.

Once you have chosen an image, the build is one command run from the root of the runtime repository:

```bash
docker run --rm -v <RUNTIME_REPO_PATH>:/runtime -w /runtime mcr.microsoft.com/dotnet-buildtools/prereqs:mcr.microsoft.com/dotnet-buildtools/prereqs:centos-7-20210714125435-9b5bbc2 ./build.sh --subset clr
```

Dissecting the command:

* `--rm`: Erase the created container after use.
* `-v <RUNTIME_REPO_PATH>:/runtime`: Mount the runtime repository under `/runtime`. Replace `<RUNTIME_REPO_PATH>` with the full path to your `runtime` repo clone, e.g., `-v /home/user/runtime:/runtime`.
* `-w: /runtime`: Set /runtime as working directory for the container.
* `mcr.microsoft.com/dotnet-buildtools/prereqs:centos-7-20210714125435-9b5bbc2`: Docker image name.
* `./build.sh`: Command to be run in the container: run the root build command.
* `-subset clr`: Build the clr subset (excluding libraries and installers).

To do cross-building for ARM32 or ARM64 using Docker, you need to use either specific images designated for this purpose, or configure your own. Detailed information on this can be found in the [cross-building doc](/docs/workflow/building/coreclr/cross-building.md#cross-building-using-docker).

### Docker Images

This table of images might often become stale as we change our images as our requirements change. The images used for our official builds can be found in [the pipeline resources](/eng/pipelines/common/templates/pipeline-with-resources.yml) of our Azure DevOps builds under the `container` key of the platform you plan to build.

| OS                                | Target Arch     | Image location                                                                                       | crossrootfs location | Clang Version |
| --------------------------------- | --------------- | ---------------------------------------------------------------------------------------------------- | -------------------- | ------------- |
| Alpine                            | x64             | `mcr.microsoft.com/dotnet-buildtools/prereqs:alpine-3.13-WithNode-20210910135845-c401c85`            | -                    | -clang5.0     |
| CentOS 7 (build for RHEL 7)       | x64             | `mcr.microsoft.com/dotnet-buildtools/prereqs:centos-7-20210714125435-9b5bbc2`                        | -                    | -clang9       |
| Ubuntu 18.04 (x64, arm ROOTFS)    | arm32 (armhf)   | `mcr.microsoft.com/dotnet-buildtools/prereqs:ubuntu-18.04-cross-20220426130400-6e40d49`              | `/crossrootfs/arm`   | -clang9       |
| Ubuntu 18.04  (x64, arm64 ROOTFS) | arm64 (arm64v8) | `mcr.microsoft.com/dotnet-buildtools/prereqs:ubuntu-18.04-cross-arm64-20220427171722-6e40d49`        | `/crossrootfs/arm64` | -clang9       |
| Alpine (x64, arm ROOTFS)          | arm             | `mcr.microsoft.com/dotnet-buildtools/prereqs:ubuntu-16.04-cross-arm-alpine-20210923140502-78f7860`   | `/crossrootfs/arm64` | -clang9       |
| Alpine (x64, arm64 ROOTFS)        | arm64 (arm64v8) | `mcr.microsoft.com/dotnet-buildtools/prereqs:ubuntu-16.04-cross-arm64-alpine-20210923140502-78f7860` | `/crossrootfs/arm64` | -clang9       |
| FreeBSD (x64 ROOTFS)              | x64             | `mcr.microsoft.com/dotnet-buildtools/prereqs:ubuntu-18.04-cross-freebsd-12-20210917001307-f13d79e`   | `/crossrootfs/x64`   | (default)     |

These Docker images are built using the Dockerfiles maintained in the [dotnet-buildtools-prereqs-docker repo](https://github.com/dotnet/dotnet-buildtools-prereqs-docker).

## Build using your own Environment

Ensure you have all of the prerequisites installed from the [Linux Requirements](/docs/workflow/requirements/linux-requirements.md).

### Set the maximum number of file-handles

To ensure that your system can allocate enough file-handles for the libraries build, run the command in your terminal `sysctl fs.file-max`. If it is less than 100000, add `fs.file-max = 100000` to `/etc/sysctl.conf`, and then run `sudo sysctl -p`.

## Build the Runtime

To build CoreCLR on Linux, run `build.sh` while specifying the `clr` subset:

```bash
./build.sh --subset clr <other args>
```

After the build is completed, there should be some files placed in `artifacts/bin/coreclr/Linux.<arch>.<configuration>` (for example `artifacts/bin/coreclr/Linux.x64.Release`). The most important binaries are the following:

* `corerun`: The command line host.  This program loads and starts the CoreCLR runtime and passes the managed program (e.g. `program.dll`) you want to run with it.
* `libcoreclr.so`: The CoreCLR runtime itself.
* `System.Private.CoreLib.dll`: The core managed library, containing definitions of `Object` and base functionality.

### Cross-Compilation

Just like you can use specialized Docker images, you can also do any of the supported cross-builds for ARM32 or ARM64 on your own Linux environment. Detailed instructions are found in the [cross-building doc](/docs/workflow/building/coreclr/cross-building.md#linux-cross-building).

## Create the Core_Root

The Core_Root provides one of the main ways to test your build. Full instructions on how to build it in the [CoreCLR testing doc](/docs/workflow/testing/coreclr/testing.md), and we also have a detailed guide on how to use it for your own testing in [its own dedicated doc](/docs/workflow/testing/using-corerun-and-coreroot.md).
