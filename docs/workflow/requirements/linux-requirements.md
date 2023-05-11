# Requirements to build dotnet/runtime on Linux

* [Docker](#docker)
* [Environment](#environment)
  * [Toolchain Setup](#toolchain-setup)
    * [Additional Requirements for Cross-Building](#additional-requirements-for-cross-building)
  * [Gentoo Special Case](#gentoo-special-case)

This guide will walk you through the requirements to build _dotnet/runtime_ on Linux. Before building there is environment setup that needs to happen to pull in all the dependencies required by the build.

There are two suggested ways to go about doing this. You can use the Docker images used in the official builds, or you can set up the environment yourself. The documentation will go over both ways. Using Docker allows you to leverage our existing images which already have an environment set up, while using your own environment grants you better flexibility on having other tools at hand you might need.

## Docker

Install Docker. For further installation instructions, see [here](https://docs.docker.com/install/). Details on the images used by the official builds can be found in the [Linux building instructions doc](/docs/workflow/building/coreclr/linux-instructions.md#docker-images). All the required build tools are included in the Docker images used to do the build, so no additional setup is required.

## Environment

These instructions are written assuming the current Ubuntu LTS. Pull Requests are welcome to address other environments.

Minimum RAM required to build is 1GB. The build is known to fail on 512 MB VMs ([dotnet/runtime#4069](https://github.com/dotnet/runtime/issues/4069)).

### Toolchain Setup

Install the following packages for the toolchain:

* CMake 3.14.5 or newer
* llvm
* lld
* clang
* build-essential
* python-is-python3
* curl
* git
* lldb
* libicu-dev
* liblttng-ust-dev
* libssl-dev
* libkrb5-dev
* zlib1g-dev
* ninja-build (optional, enables building native code with ninja instead of make)

```bash
sudo apt install -y cmake llvm lld clang build-essential \
python-is-python3 curl git lldb libicu-dev liblttng-ust-dev \
libssl-dev libkrb5-dev zlib1g-dev ninja-build
```

You now have all the required components.

#### Additional Requirements for Cross-Building

If you are planning to use your Linux environment to do cross-building for other architectures (e.g. Arm32, Arm64) and/or other operating systems (e.g. Alpine, FreeBSD), you need to install these additional dependencies:

* qemu
* qemu-user-static
* binfmt-support
* debootstrap

**NOTE**: These dependencies are used to build the `crossrootfs`, not the runtime itself.

### Gentoo Special Case

In case you have Gentoo you can run following command:

```bash
emerge --ask clang dev-util/lttng-ust app-crypt/mit-krb5
```
