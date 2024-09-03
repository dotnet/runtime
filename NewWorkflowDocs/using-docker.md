# Using Docker for your Workflow

- [Docker Basics](#docker-basics)
- [The Official Runtime Docker Images](#the-official-runtime-docker-images)
- [Build the Repo](#build-the-repo)

This doc will cover the usage of Docker images and containers for your builds.

## Docker Basics

First, you have to enable and install the Docker Engine. Follow the instructions in their official site in [this link](https://docs.docker.com/get-started/get-docker) if you haven't done so.

When using Docker, your machine's OS is strictly speaking not terribly important. For example, if you are on *Ubuntu 22.04*, you can use the *Ubuntu 18.04* image without any issues whatsoever. Likewise, you can run Linux images on Windows if you have WSL enabled. If you followed the instructions from the Docker official website when installing the engine, you most likely have it already up and running. If not, you can follow the instructions in [this link](https://learn.microsoft.com/windows/wsl/install) to enable it. However, note that you can't run multiple OS's on the same *Docker Daemon*, as it takes resources from the underlying kernel as needed. In other words, you can run either Linux on WSL, or Windows containers. You have to switch between them if you need both, and restart Docker.

The target architecture is more important to consider when using Docker containers. The image's architecture has to match your machine's supported platforms. For instance, you can run both, x64 and Arm64 images on an *Apple Silicon Mac*, thanks to the *Rosetta* x64 emulator it provides. Likewise, you can run Linux Arm32 images on a Linux Arm64 host.

Note that while Docker uses WSL to run the Linux containers on Windows, you don't have to boot up a WSL terminal to run them. Any `cmd` or `powershell` terminal with the `docker` command available will suffice to run all the commands. Docker takes care of the rest.

## The Official Runtime Docker Images

In the following tables, you will find the full names with tags of the images used for the official builds.

**Main Docker Images**

The main Docker images are the most commonly used ones, and the ones you will probably need for your builds. If you are working with more specific scenarios (e.g. Android, Risc-V), then you will find the images you need in the *Extended Docker Images* table right below this one.

| Host OS           | Target OS    | Target Arch     | Image                                                                                  | crossrootfs dir      |
| ----------------- | ------------ | --------------- | -------------------------------------------------------------------------------------- | -------------------- |
| Azure Linux (x64) | Alpine 3.13  | x64             | `mcr.microsoft.com/dotnet-buildtools/prereqs:azurelinux-3.0-net9.0-cross-amd64-alpine` | `/crossrootfs/x64`   |
| Azure Linux (x64) | Ubuntu 16.04 | x64             | `mcr.microsoft.com/dotnet-buildtools/prereqs:azurelinux-3.0-net9.0-cross-amd64`        | `/crossrootfs/x64`   |
| Azure Linux (x64) | Alpine 3.13  | Arm32 (armhf)   | `mcr.microsoft.com/dotnet-buildtools/prereqs:azurelinux-3.0-net9.0-cross-arm-alpine`   | `/crossrootfs/arm`   |
| Azure Linux (x64) | Ubuntu 22.04 | Arm32 (armhf)   | `mcr.microsoft.com/dotnet-buildtools/prereqs:azurelinux-3.0-net9.0-cross-arm`          | `/crossrootfs/arm`   |
| Azure Linux (x64) | Alpine 3.13  | Arm64 (arm64v8) | `mcr.microsoft.com/dotnet-buildtools/prereqs:azurelinux-3.0-net9.0-cross-arm64-alpine` | `/crossrootfs/arm64` |
| Azure Linux (x64) | Ubuntu 16.04 | Arm64 (arm64v8) | `mcr.microsoft.com/dotnet-buildtools/prereqs:azurelinux-3.0-net9.0-cross-arm64`        | `/crossrootfs/arm64` |
| Azure Linux (x64) | Ubuntu 16.04 | x86             | `mcr.microsoft.com/dotnet-buildtools/prereqs:azurelinux-3.0-net9.0-cross-x86`          | `/crossrootfs/x86`   |

**Extended Docker Images**

<!-- TODO: Check the Debian 12 image to see if it really has a Crossrootfs and/or whether it really targets x64. -->
| Host OS           | Target OS                  | Target Arch   | Image                                                                                  | crossrootfs dir        |
| ----------------- | -------------------------- | ------------- | -------------------------------------------------------------------------------------- | ---------------------- |
| Azure Linux (x64) | Android Bionic             | x64           | `mcr.microsoft.com/dotnet-buildtools/prereqs:azurelinux-3.0-net9.0-cross-android-amd64`|  N/A                   |
| Azure Linux (x64) | Android Bionic (w/OpenSSL) | x64           | `mcr.microsoft.com/dotnet-buildtools/prereqs:azurelinux-3.0-net9.0-android-openssl`    |  N/A                   |
| Azure Linux (x64) | Android Bionic (w/Docker)  | x64           | `mcr.microsoft.com/dotnet-buildtools/prereqs:azurelinux-3.0-net9.0-android-docker`     |  N/A                   |
| Azure Linux (x64) | Azure Linux 3.0            | x64           | `mcr.microsoft.com/dotnet-buildtools/prereqs:azurelinux-3.0-net9.0-fpm`                |  N/A                   |
| Azure Linux (x64) | FreeBSD 13                 | x64           | `mcr.microsoft.com/dotnet-buildtools/prereqs:azurelinux-3.0-net9.0-cross-freebsd-13`   | `/crossrootfs/x64`     |
| Azure Linux (x64) | Ubuntu 18.04               | PPC64le       | `mcr.microsoft.com/dotnet-buildtools/prereqs:azurelinux-3.0-net9.0-cross-ppc64le`      | `/crossrootfs/ppc64le` |
| Azure Linux (x64) | Ubuntu 24.04               | RISC-V        | `mcr.microsoft.com/dotnet-buildtools/prereqs:azurelinux-3.0-net9.0-cross-riscv64`      | `/crossrootfs/riscv64` |
| Azure Linux (x64) | Ubuntu 18.04               | S390x         | `mcr.microsoft.com/dotnet-buildtools/prereqs:azurelinux-3.0-net9.0-cross-s390x`        | `/crossrootfs/s390x`   |
| Azure Linux (x64) | Ubuntu 16.04 (Wasm)        | x64           | `mcr.microsoft.com/dotnet-buildtools/prereqs:azurelinux-3.0-net9.0-webassembly-amd64`  | `/crossrootfs/x64`     |
| Debian (x64)      | Debian 12                  | x64           | `mcr.microsoft.com/dotnet-buildtools/prereqs:debian-12-gcc14-amd64`                    | `/crossrootfs/armv6`   |
| Ubuntu (x64)      | Ubuntu 22.04               | x64           | `mcr.microsoft.com/dotnet-buildtools/prereqs:ubuntu-22.04-debpkg`                      |  N/A                   |
| Ubuntu (x64)      | Tizen 9.0                  | Arm32 (armel) | `mcr.microsoft.com/dotnet-buildtools/prereqs:ubuntu-22.04-cross-armel-tizen`           | `/crossrootfs/armel`   |
| Ubuntu (x64)      | Ubuntu 20.04               | Arm32 (v6)    | `mcr.microsoft.com/dotnet-buildtools/prereqs:ubuntu-20.04-cross-armv6-raspbian-10`     | `/crossrootfs/armv6`   |

## Build the Repo

Build the Repo Under Construction!
