Requirements to build dotnet/runtime on FreeBSD
=====================

This guide will walk you through the requirements needed to build dotnet/runtime on FreeBSD. We'll start by showing how to set up your environment from scratch.
Since there is no official build and FreeBSD package, native build on FreeBSD is not trivial. There are generally three options, sorted by ease of use:
- cross-compile on Linux using Docker
- cross-compile on Linux using Toolchain
- build on FreeBSD


Environment
===========

These instructions were validated for and on FreeBSD 11.3 and 12.1.

Build using Docker on Linux
---------------------------

This is similar to [Linux](linux-requirements.md) instructions. https://github.com/dotnet/dotnet-buildtools-prereqs-docker repro provides images
with all needed prerequisites to build. As the example bellow may become stale, https://github.com/dotnet/versions/blob/master/build-info/docker/image-info.dotnet-dotnet-buildtools-prereqs-docker-master.json offers list of latest Docker tags.

```sh
TAG=mcr.microsoft.com/dotnet-buildtools/prereqs:ubuntu-18.04-cross-freebsd-11-20200430154008-a84b0d2
docker run --rm --volume $(pwd):$(pwd) --workdir $(pwd) --env ROOTFS_DIR=/crossrootfs/x64 -ti  $TAG ./build.sh -cross -os FreeBSD
```

Build using Toolchain Setup
---------------------------
To build FreeBSD images, prerequisites described in [Linux](linux-requirements.md) are needed. Additionally, crossrootfs  for FreeBSD needs to be constructed.
In order to successfully build FreeBSD crossrootfs, few more packages needs to be installed. Following example is for Ubuntu 18:
```sh
apt-get install -y libbz2-dev libz-dev liblzma-dev libarchive-dev libbsd-dev
```
With prerequisites for crossrootfs one can run:
```sh
./eng/common/cross/build-rootfs.sh freebsd11 $(pwd)/rootfs/freebsd
```
After that, FreeBSD build can be started by running
```
ROOTFS_DIR=$(pwd)/rootfs/freebsd ./build.sh -cross -os FreeBSD
```


Building on FreeBSD
-------------------

Building dotnet/runtime depends on several tools to be installed.

Install the following packages:

- cmake
- autoconf
- automake
- libtool
- icu
- libunwind
- lttng-ust
- krb5
- openssl (optional)

The lines to install all the packages above using package manager.

```sh
sudo pkg install --yes libunwind icu libinotify lttng-ust krb5 cmake autoconf automake openssl
```

Additionally, working dotnet cli with SDK is needed. On other platforms this would be downloaded automatically during build but it is not currently available for FreeBSD.
It needs to be built once on supported platform or obtained via community resources.

