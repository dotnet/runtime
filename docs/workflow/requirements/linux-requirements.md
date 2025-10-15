# Requirements to Set Up the Build Environment on Linux

- [Using your Linux Environment](#using-your-linux-environment)
  - [Debian and Ubuntu](#debian-and-ubuntu)
    - [CMake on Older Versions of Ubuntu and Debian](#cmake-on-older-versions-of-ubuntu-and-debian)
    - [Clang for WASM](#clang-for-wasm)
    - [Additional Tools for Cross Building](#additional-tools-for-cross-building)
  - [Fedora](#fedora)
  - [Gentoo](#gentoo)
- [Using Docker](#using-docker)

There are two ways to build the runtime repo on *Linux*: Set up your environment in your Linux machine, or use the Docker images that are used in the official builds. This guide will cover both of these approaches. Using Docker allows you to leverage our existing images which already have an environment set up, while using your own environment grants you better flexibility on having other tools at hand you might need.

**NOTE:** If you're using WSL, then follow the instructions for the distro you have installed there.

## Using your Linux Environment

The following sections describe the requirements for different kinds of Linux distros. Pull Requests are welcome to add documentation regarding environments and distros currently not described here.

The minimum required RAM is 1GB (builds are known to fail on 512MB VM's (https://github.com/dotnet/runtime/issues/4069), although more is recommended, as the builds can take a long time otherwise.

To get started, you can use this helper script to install dependencies on some platforms, or you can install them yourself following the instructions in the next sections. If you opt to try this script, make sure to run it as `sudo` if you don't have root privileges:

```bash
# requires sudo for non-root user
eng/common/native/install-dependencies.sh
```

Note that it is always a good idea to manually double check that all the dependencies were installed correctly if you opt to use the script.

### Debian and Ubuntu

These instructions are written assuming the current *Ubuntu LTS*.

The packages you need to install are shown in the following list:

- `build-essential`
- `clang` (see the [Clang for WASM](#clang-for-wasm) section if you plan on doing work on *Web Assembly (Wasm)*)
- `cmake` (version 3.20 or newer)
- `cpio`
- `curl`
- `git`
- `libicu-dev`
- `libkrb5-dev`
- `liblttng-ust-dev`
- `libssl-dev`
- `lld`
- `lldb`
- `llvm`
- `ninja-build` (Optional. Enables building native code using `ninja` instead of `make`)
- `pigz` (Optional. Enables parallel gzip compression for tarball creation in `packs` subset)
- `python-is-python3`

**NOTE:** If you are running on *Ubuntu* older than version *22.04 LTS*, or *Debian* older than version 12, then don't install `cmake` using `apt` directly. Follow the instructions in the [CMake on Older Versions of Ubuntu and Debian section](#cmake-on-older-versions-of-ubuntu-and-debian) later down in this doc.

```bash
# requires sudo for non-root user
apt install -y cmake llvm lld clang build-essential \
  python-is-python3 curl git lldb libicu-dev liblttng-ust-dev \
  libssl-dev libkrb5-dev ninja-build pigz cpio
```

#### CMake on Older Versions of Ubuntu and Debian

As of now, Ubuntu's `apt` only has until *CMake* version 3.16.3 if you're using *Ubuntu 20.04 LTS* (less in older Ubuntu versions), and version 3.18.4 in *Debian 11* (less in older Debian versions). This is lower than the required 3.20, which in turn makes it incompatible with the runtime repo. To get around this, there are two options you can choose: Use the `snap` package manager, which has a more recent version of *CMake*, or directly use the *Kitware APT Feed*.

To use `snap`, run the following command:

```bash
# requires sudo for non-root user
snap install cmake
```

To use the *Kitware APT feed*, follow their official instructions [in this link](https://apt.kitware.com/).

#### Clang for WASM

As of now, *WASM* builds have a minimum requirement of `Clang` version 16 or later (version 18 is the latest at the time of writing this doc). If you're using *Ubuntu 22.04 LTS* or older, then you will have to add an additional repository to `apt` to be able to get said version. Run the following commands on your terminal to do this:

```bash
# requires sudo for non-root user
add-apt-repository -y "deb http://apt.llvm.org/$(lsb_release -s -c)/ llvm-toolchain-$(lsb_release -s -c)-18 main"
apt update -y
apt install -y clang-18
```

You can also take a look at the Linux-based *Dockerfile* [over here](/.devcontainer/Dockerfile) for another example.

#### Additional Tools for Cross Building

If you're planning to use your environment to do Linux cross-building to other architectures (e.g. Arm32, Arm64), and/or other operating systems (e.g. Alpine, FreeBSD), you'll need to install a few additional dependencies. It is worth mentioning these other packages are required to build the `crossrootfs`, which is used to effectively do the cross-compilation, not to build the runtime itself.

- `binfmt-support`
- `debootstrap`
- `qemu`
- `qemu-user-static`

```bash
apt install binfmt-support debootstrap qemu qemu-user-static
```

### Fedora

These instructions are written assuming *Fedora 40*.

Install the following packages for the toolchain:

- `clang`
- `cmake`
- `cpio`
- `curl`
- `git`
- `krb5-devel`
- `libicu-devel`
- `lld`
- `lldb`
- `llvm`
- `lttng-ust-devel`
- `ninja-build` (Optional. Enables building native code using `ninja` instead of `make`)
- `openssl-devel`
- `pigz` (Optional. Enables parallel gzip compression for tarball creation in `packs` subset)
- `python`

```bash
# requires sudo for non-root user
dnf install -y cmake llvm lld lldb clang python curl git \
  libicu-devel openssl-devel krb5-devel lttng-ust-devel ninja-build pigz cpio
```

### Gentoo

In case you have Gentoo you can run following command:

```bash
emerge --ask clang dev-util/lttng-ust app-crypt/mit-krb5
```

## Using Docker

As mentioned at the beginning of this doc, the other method to build the runtime repo for Linux is to use the prebuilt Docker images that our official builds use. In order to be able to run them, you first need to download and install the Docker Engine. The binaries needed and installation instructions can be found at the Docker official site [in this link](https://docs.docker.com/get-started/get-docker).

Once you have the Docker Engine up and running, you can follow our docker building instructions [over here](/docs/workflow/using-docker.md).
