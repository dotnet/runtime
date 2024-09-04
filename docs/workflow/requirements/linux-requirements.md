# Requirements to Set Up the Build Environment on Linux

- [Using your Linux Environment](#using-your-linux-environment)
  - [Debian/Ubuntu](#debian/ubuntu)
    - [CMake on Older Versions of Ubuntu and Debian](#cmake-on-older-versions-of-ubuntu-and-debian)
    - [Clang for WASM](#clang-for-wasm)
    - [Additional Tools for Cross Building](#additional-tools-for-cross-building)
  - [Fedora](#fedora)
  - [Gentoo](#gentoo)
- [Using Docker](#using-docker)

To build the runtime repo on *Linux*, there are two ways you can opt for: Set up your environment in your Linux machine, or use the Docker images that are used in the official builds. This guide will cover both of these approaches. Using Docker allows you to leverage our existing images which already have an environment set up, while using your own environment grants you better flexibility on having other tools at hand you might need.

**NOTE:** If you're using WSL, then follow the instructions for the distro you have installed there.

## Using your Linux Environment

The following sections describe the requirements for different kinds of Linux distros. Pull Requests are welcome to add documentation regarding environments and distros currently not described here.

The minimum required RAM is 1GB (builds are known to fail on 512MB VM's (https://github.com/dotnet/runtime/issues/4069), although more is recommended, as the builds can take a long time otherwise.

To get started, you can use this helper script to install dependencies on some platforms, or you can install them yourself following the instructions in the next sections. If you opt to try this script, make sure to run it as `sudo` if you don't have root privileges:

```bash
sudo eng/install-native-dependencies.sh
```

Note that it is always a good idea to manually double check that all the dependencies were installed correctly if you opt to use the script.

### Debian/Ubuntu

These instructions are written assuming the current *Ubuntu LTS*.

The packages you need to install are shown in the following list:

- `CMake` (version 3.20 or newer)
- `llvm`
- `lld`
- `Clang` (see the [Clang for WASM](#clang-for-wasm) section if you plan on doing work on *Web Assembly (Wasm)*)
- `build-essential`
- `python-is-python3`
- `curl`
- `git`
- `lldb`
- `libicu-dev`
- `liblttng-ust-dev`
- `libssl-dev`
- `libkrb5-dev`
- `ninja-build` (Optional. Enables building native code using `ninja` instead of `make`)

**NOTE:** If you are running on *Ubuntu* older than version *22.04 LTS*, or *Debian* older than version 12, then don't install `cmake` using `apt` directly. Follow the instructions in the [CMake on Older Versions of Ubuntu and Debian section](#cmake-on-older-versions-of-ubuntu-and-debian) later down in this doc.

```bash
sudo apt install -y cmake llvm lld clang build-essential \
  python-is-python3 curl git lldb libicu-dev liblttng-ust-dev \
  libssl-dev libkrb5-dev ninja-build
```

#### CMake on Older Versions of Ubuntu and Debian

As of now, Ubuntu's `apt` only has until *CMake* version 3.16.3 if you're using *Ubuntu 20.04 LTS* (less in older Ubuntu versions), and version 3.18.4 in *Debian 11* (less in older Debian versions). This is lower than the required 3.20, which in turn makes it incompatible with the runtime repo. To get around this, there are two options you can choose: Use the `snap` package manager, which has a more recent version of *CMake*, or directly use the *Kitware APT Feed*.

To use `snap`, run the following command:

```bash
sudo snap install cmake
```

To use the *Kitware APT feed*, follow their official instructions [in this link](https://apt.kitware.com/).

#### Clang for WASM

As of now, *WASM* builds have a minimum requirement of `Clang` version 16 or later (version 18 is the latest at the time of writing this doc). If you're using *Ubuntu 22.04 LTS* or older, then you will have to add an additional repository to `apt` to be able to get said version. Run the following commands on your terminal to do this:

```bash
sudo add-apt-repository -y "deb http://apt.llvm.org/$(lsb_release -s -c)/ llvm-toolchain-$(lsb_release -s -c)-18 main"
sudo apt update -y
sudo apt install -y clang-18
```

You can also take a look at the Linux-based *Dockerfile* [over here](/.devcontainer/Dockerfile) for another example.

#### Additional Tools for Cross Building

If you're planning to use your environment to do Linux cross-building to other architectures (e.g. Arm32, Arm64), and/or other operating systems (e.g. Alpine, FreeBSD), you'll need to install a few additional dependencies. It is worth mentioning these other packages are required to build the `crossrootfs`, which is used to effectively do the cross-compilation, not to build the runtime itself.

- `qemu`
- `qemu-user-static`
- `binfmt-support`
- `debootstrap`

### Fedora

These instructions are written assuming *Fedora 40*.

Install the following packages for the toolchain:

- `cmake`
- `llvm`
- `lld`
- `lldb`
- `clang`
- `python`
- `curl`
- `git`
- `libicu-devel`
- `openssl-devel`
- `krb5-devel`
- `lttng-ust-devel`
- `ninja-build` (Optional. Enables building native code using `ninja` instead of `make`)

```bash
sudo dnf install -y cmake llvm lld lldb clang python curl git \
  libicu-devel openssl-devel krb5-devel lttng-ust-devel ninja-build
```

### Gentoo

In case you have Gentoo you can run following command:

```bash
emerge --ask clang dev-util/lttng-ust app-crypt/mit-krb5
```

## Using Docker

As mentioned at the beginning of this dic, the other method to build the runtime repo for Linux is to use the prebuilt Docker images that our official builds use. In order to be able to run them, you first need to download and install the Docker Engine. The binaries needed and installation instructions can be found at the Docker official site [in this link](https://docs.docker.com/get-started/get-docker).
