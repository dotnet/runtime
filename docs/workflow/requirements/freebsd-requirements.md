# Requirements to build dotnet/runtime on FreeBSD

* [Docker](#docker)
* [Linux Environment](#linux-environment)
* [FreeBSD Environment](#freebsd-environment)
* [Old Documentation](#old-documentation)
  * [Toolchain Setup](#toolchain-setup)
  * [Running on FreeBSD](#running-on-freebsd)

This guide will walk you through the requirements needed to build and run _dotnet/runtime_ on FreeBSD. We'll start by showing how to set up your environment from scratch.

Since there is no official build and FreeBSD package, native build on FreeBSD is not trivial. There are generally three options, sorted by ease of use:

* Cross-compile using prebuilt Docker Images
* Cross-compile on Linux using your environment
* Build directly on FreeBSD

## Docker

Install Docker. For further instructions on installation, see [here](https://docs.docker.com/install/). You can find the official .NET images in [their Docker hub](https://hub.docker.com/_/microsoft-dotnet).

All the required build tools are included in the Docker images used to do the build, so no additional setup is required.

## Linux Environment

To cross-build FreeBSD on your Linux environment, first make sure you have all the [normal Linux prerequisites](/docs/workflow/requirements/linux-requirements.md) fulfilled. Then, the _crossrootfs_ for FreeBSD has to be constructed, and this requires a few more packages to be installed:

* libbz2-dev
* liblzma-dev
* libarchive-dev
* libbsd-dev

## FreeBSD Environment

These instructions assume you use FreeBSD's default binary package tool `pkg` (analog to `apt`, `apt-get`, or `yum` on Linux) to install the environment. Compiling the dependencies from source using the ports tree might work too, but is untested.

FreeBSD Prerequisites Coming Soon!

Meanwhile here are the old instructions.

## Old Documentation

These instructions were written quite a while ago, and they may or may not work today. Updated instructions coming soon.

### Toolchain Setup

Building the _dotnet/runtime_ repo requires several tools to be installed.

Install the following packages:

* Bash
* CMake
* icu
* libunwind
* krb5
* openssl (optional)
* python39
* libinotify
* ninja (optional, enables building native code with ninja instead of make)

```sh
sudo pkg install --yes libunwind icu libinotify lttng-ust krb5 cmake openssl ninja
```

### Running on FreeBSD

Install the following packages:

* icu
* libunwind
* lttng-ust (optional, debug support)
* krb5
* openssl (optional, SSL support)
* libinotify
* terminfo-db (optional, terminal colors)

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

* Microsoft.NETCore.App.Host.freebsd-x64.VERSION.nupkg
* Microsoft.NETCore.App.Runtime.freebsd-x64.VERSION.nupkg
* Microsoft.AspNetCore.App.Runtime.freebsd-x64.VERSION.nupkg

Add the following line to any `NuGet.config` you are using under the `<packageSources>` section:

```xml
<add key="local" value="/var/cache/nuget" />
```

Finally, either add `/usr/share/dotnet` to your PATH or create a symbolic for `/usr/share/dotnet/dotnet`
