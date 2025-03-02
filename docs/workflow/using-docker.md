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
| Azure Linux (x64) | Alpine 3.17  | x64             | `mcr.microsoft.com/dotnet-buildtools/prereqs:azurelinux-3.0-net10.0-cross-amd64-musl` | `/crossrootfs/x64`   |
| Azure Linux (x64) | Ubuntu 18.04 | x64             | `mcr.microsoft.com/dotnet-buildtools/prereqs:azurelinux-3.0-net10.0-cross-amd64`        | `/crossrootfs/x64`   |
| Azure Linux (x64) | Alpine 3.17  | Arm32 (armhf)   | `mcr.microsoft.com/dotnet-buildtools/prereqs:azurelinux-3.0-net10.0-cross-arm-musl`   | `/crossrootfs/arm`   |
| Azure Linux (x64) | Ubuntu 22.04 | Arm32 (armhf)   | `mcr.microsoft.com/dotnet-buildtools/prereqs:azurelinux-3.0-net10.0-cross-arm`          | `/crossrootfs/arm`   |
| Azure Linux (x64) | Alpine 3.17  | Arm64 (arm64v8) | `mcr.microsoft.com/dotnet-buildtools/prereqs:azurelinux-3.0-net10.0-cross-arm64-musl` | `/crossrootfs/arm64` |
| Azure Linux (x64) | Ubuntu 18.04 | Arm64 (arm64v8) | `mcr.microsoft.com/dotnet-buildtools/prereqs:azurelinux-3.0-net10.0-cross-arm64`        | `/crossrootfs/arm64` |
| Azure Linux (x64) | Ubuntu 18.04 | x86             | `mcr.microsoft.com/dotnet-buildtools/prereqs:azurelinux-3.0-net10.0-cross-x86`          | `/crossrootfs/x86`   |

**Extended Docker Images**

| Host OS           | Target OS                  | Target Arch   | Image                                                                                   | crossrootfs dir        |
| ----------------- | -------------------------- | ------------- | --------------------------------------------------------------------------------------- | ---------------------- |
| Azure Linux (x64) | Android Bionic             | x64           | `mcr.microsoft.com/dotnet-buildtools/prereqs:azurelinux-3.0-net10.0-cross-android-amd64` |        *N/A*           |
| Azure Linux (x64) | Android Bionic (w/OpenSSL) | x64           | `mcr.microsoft.com/dotnet-buildtools/prereqs:azurelinux-3.0-net10.0-android-openssl`     |        *N/A*           |
| Azure Linux (x64) | Android Bionic (w/Docker)  | x64           | `mcr.microsoft.com/dotnet-buildtools/prereqs:azurelinux-3.0-net10.0-android-docker`      |        *N/A*           |
| Azure Linux (x64) | FreeBSD 14                 | x64           | `mcr.microsoft.com/dotnet-buildtools/prereqs:azurelinux-3.0-net10.0-cross-freebsd-14`    | `/crossrootfs/x64`     |
| Azure Linux (x64) | Ubuntu 18.04               | PPC64le       | `mcr.microsoft.com/dotnet-buildtools/prereqs:azurelinux-3.0-net10.0-cross-ppc64le`       | `/crossrootfs/ppc64le` |
| Azure Linux (x64) | Ubuntu 24.04               | RISC-V        | `mcr.microsoft.com/dotnet-buildtools/prereqs:azurelinux-3.0-net10.0-cross-riscv64`       | `/crossrootfs/riscv64` |
| Azure Linux (x64) | Debian sid                 | LoongArch     | `mcr.microsoft.com/dotnet-buildtools/prereqs:azurelinux-3.0-net10.0-cross-loongarch64`   | `/crossrootfs/loongarch64` |
| Azure Linux (x64) | Ubuntu 18.04               | S390x         | `mcr.microsoft.com/dotnet-buildtools/prereqs:azurelinux-3.0-net10.0-cross-s390x`         | `/crossrootfs/s390x`   |
| Azure Linux (x64) | Ubuntu 18.04 (Wasm)        | x64           | `mcr.microsoft.com/dotnet-buildtools/prereqs:azurelinux-3.0-net10.0-webassembly-amd64`   | `/crossrootfs/x64`     |
| Debian (x64)      | Debian 12                  | x64           | `mcr.microsoft.com/dotnet-buildtools/prereqs:debian-12-gcc14-amd64`                     |        *N/A*           |
| Ubuntu (x64)      | Tizen 9.0                  | Arm32 (armel) | `mcr.microsoft.com/dotnet-buildtools/prereqs:ubuntu-22.04-cross-armel-tizen`            | `/crossrootfs/armel`   |
| Ubuntu (x64)      | Ubuntu 20.04               | Arm32 (v6)    | `mcr.microsoft.com/dotnet-buildtools/prereqs:ubuntu-20.04-cross-armv6-raspbian-10`      | `/crossrootfs/armv6`   |

## Build the Repo

Once you've chosen the image that suits your needs, you can issue `docker run` with the necessary arguments to use your clone of the runtime repo, and call the build scripts as you need. Down below, we have a small command-line example, explaining each of the flags you might need to use:

```bash
docker run --rm \
  -v <RUNTIME_REPO_PATH>:/runtime \
  -w /runtime \
  -e ROOTFS_DIR=/crossrootfs/x64/ \
  mcr.microsoft.com/dotnet-buildtools/prereqs:azurelinux-3.0-net10.0-cross-amd64 \
  ./build.sh -s clr --cross -c Checked
```

Now, dissecting the command:

- `--rm`: Erase the created container after it finishes running.
- `-v <RUNTIME_REPO_PATH>:/runtime`: Mount the runtime repo clone located in `<RUNTIME_REPO_PATH>` to the container path `/runtime`.
- `-w /runtime`: Start the container in the `/runtime` directory.
- `-e ROOTFS_DIR=/crossrootfs/x64/` sets up the environment variable for crossbuilding.
- `mcr.microsoft.com/dotnet-buildtools/prereqs:azurelinux-3.0-net10.0-cross-amd64`: The fully qualified name of the Docker image to download. In this case, we want to use an *Azure Linux* image to target the *x64* architecture.
- `./build.sh -s clr --cross -c Checked`: The build command to run in the repo. In this case, we want to build the *Clr* subset in the *Checked* configuration with the cross compilation option.

You might also want to interact with the container directly for a myriad of reasons, like running multiple builds in different paths for example. In this case, instead of passing the build script command to the `docker` command-line, pass the flag `-it`. When you do this, you will get access to a small shell within the container, which allows you to explore it, run builds manually, and so on, like you would on a regular terminal in your machine. Note that the containers' shell's built-in tools are very limited in comparison to the ones you probably have on your machine, so don't expect to be able to do full work there.

