Requirements to build and run dotnet/runtime on FreeBSD
=====================

This guide will walk you through the requirements needed to build and run dotnet/runtime on FreeBSD. We'll start by showing how to set up your environment from scratch.
Since there is no official build and FreeBSD package, native build on FreeBSD is not trivial. There are generally three options, sorted by ease of use:
- cross-compile on Linux using Docker
- cross-compile on Linux using Toolchain
- build on FreeBSD

Environment
===========

These instructions were validated for and on FreeBSD 12.2.

Build using Docker on Linux
---------------------------

This is similar to [Linux](linux-requirements.md) instructions. https://github.com/dotnet/dotnet-buildtools-prereqs-docker repro provides images
with all needed prerequisites to build. As the example bellow may become stale, https://github.com/dotnet/versions/blob/master/build-info/docker/image-info.dotnet-dotnet-buildtools-prereqs-docker-master.json offers list of latest Docker tags.

```sh
TAG=mcr.microsoft.com/dotnet-buildtools/prereqs:ubuntu-18.04-cross-freebsd-12-20210917001307-f13d79e
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
- icu
- libunwind
- lttng-ust
- krb5
- openssl (optional)
- ninja (optional, enables building native code with ninja instead of make)

The lines to install all the packages above using package manager.

```sh
sudo pkg install --yes libunwind icu libinotify lttng-ust krb5 cmake openssl ninja
```

Additionally, working dotnet cli with SDK is needed. On other platforms this would be downloaded automatically during build but it is not currently available for FreeBSD.
It needs to be built once on supported platform or obtained via community resources.

Running on FreeBSD
-------------------
Install the following packages:
- icu
- libunwind
- lttng-ust (optional, debug support)
- krb5
- openssl (optional, SSL support)
- libinotify
- terminfo-db (optional, terminal colors)

The lines to install all the packages above using package manager.

```sh
sudo pkg install --yes libunwind icu libinotify lttng-ust krb5 openssl terminfo-db
```

Extract the SDK:
The canonical location for the SDK is `/usr/share/dotnet`

"VERSION" is the SDK version being unpacked.

```sh
sudo mkdir /usr/share/dotnet
tar xf /tmp/dotnet-sdk-VERSION-freebsd-x64.tar.gz -C /usr/share/dotnet/
```

NuGet Packages:
The canonical location for the NuGet packages is `/var/cache/nuget`

"VERSION" is the same version as the SDK from above.

- Microsoft.NETCore.App.Host.freebsd-x64.VERSION.nupkg
- Microsoft.NETCore.App.Runtime.freebsd-x64.VERSION.nupkg
- Microsoft.AspNetCore.App.Runtime.freebsd-x64.VERSION.nupkg

Add the following line to any `NuGet.config` you are using under the `<packageSources>` section:

```xml
<add key="local" value="/var/cache/nuget" />
```

Finally, either add `/usr/share/dotnet` to your PATH or create a symbolic for `/usr/share/dotnet/dotnet`
