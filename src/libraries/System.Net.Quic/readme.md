# MsQuic

`System.Net.Quic` depends on [MsQuic](https://github.com/microsoft/msquic), Microsoft, cross-platform, native implementation of the [QUIC](https://datatracker.ietf.org/wg/quic/about/) protocol.
Currently, `System.Net.Quic` depends on [**MsQuic 2.1+**](https://github.com/microsoft/msquic/tree/release/2.1).

## Usage

MsQuic library is officially published by [MsQuic](https://github.com/microsoft/msquic) so there's no need to compile it yourself.

### Windows
Prerequisites:
- Windows 11 or Windows Server 2022 or Windows 10 Insiders Fast build. This is required for SChannel support for QUIC.
  - To confirm you have a new enough build, run winver on command line and confirm you version is greater than Version 2004 (OS Build 20145.1000).
- Turned on TLS 1.3
  - It is turned on by default, to confirm you can check the appropriate registry `Computer\HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\SecurityProviders\SCHANNEL\Protocols` (empty means default which means enabled).

During the build, the `msquic.dll` is automatically downloaded and placed in correct directories in order to be picked up by the runtime. It is also published as part of the .NET runtime for Windows.

### Linux

On Linux, `libmsquic` is published via official Microsoft Linux package repository `packages.microsoft.com`. In order to consume packages from it, you have to add it manually, see https://docs.microsoft.com/en-us/windows-server/administration/linux-package-repository-for-microsoft-software. After that, you should be able to install `libmsquic` via the package manager of your distro, e.g. for Ubuntu:
```
apt install libmsquic
```

### Build MsQuic

[MsQuic build docs](https://github.com/microsoft/msquic/blob/main/docs/BUILD.md)
You might want to test some `msquic` changes which hasn't propagated into the released package. For that, you need to build `msquic` yourself.

#### Linux
Prerequisites:
- build-essential
- cmake
- lttng-ust
- lttng-tools

`dotnet tool install --global`:
- microsoft.logging.clog
- microsoft.logging.clog2text.lttng

Run inside the msquic directory (for **Debug** build with logging on):
```bash
# build msquic in debug with logging
rm -rf build
mkdir build
cmake -B build -DCMAKE_BUILD_TYPE=Debug -DQUIC_ENABLE_LOGGING=on
cd build
cmake --build . --config Debug

# copy msquic into runtime
yes | cp -rf bin/Debug/libmsquic.* <path-to-runtime>/src/libraries/System.Net.Quic/src/
```

#### Windows
Prerequisites:
- Windows 11 or Windows Server 2022 or Windows 10 Insiders Fast build. This is required for SChannel support for QUIC.
  - To confirm you have a new enough build, run winver on command line and confirm you version is greater than Version 2004 (OS Build 20145.1000).

Follow the instructions from [msquic build documentation](https://github.com/microsoft/msquic/blob/main/docs/BUILD.md).

## Packaging

It differs greatly between Linux and Windows as we ship `libmsquic.dll` as part of .NET runtime on Windows. Whereas, `libmsquic` package must be manually installed on Linux distributions.

On top of that, we can consume officially released MsQuic builds, e.g. [MsQuic releases](https://github.com/microsoft/msquic/releases). As well as our own builds of the current MsQuic main branch via [dotnet/msquic](https://github.com/dotnet/msquic).

### Linux

For officially released Linux packages, we use [packages.microsoft.com](https://packages.microsoft.com/). MsQuic team themselves are publishing packages there, e.g. https://packages.microsoft.com/ubuntu/22.04/prod/pool/main/libm/libmsquic/.

Or, msquic can be compiled, either from a release branch or main. Then it must be copied inside `System.Net.Quic/src` directory, or installed into your system, see [fpm](https://github.com/jordansissel/fpm) to create your own package.

#### Testing

Testing on Linux is done with the help of docker images whose definition can be found in [dotnet/dotnet-buildtools-prereqs-docker](https://github.com/dotnet/dotnet-buildtools-prereqs-docker).

To consume a release version of the package, the docker image definition will contain:
```docker
RUN apt-get update \
    && apt-get upgrade -y \
    && apt-get install -y \
        apt-transport-https \
        curl \
        software-properties-common \
    && curl -sL https://packages.microsoft.com/config/debian/12/packages-microsoft-prod.deb -o packages-microsoft-prod.deb \
    && dpkg -i packages-microsoft-prod.deb \
    && rm packages-microsoft-prod.deb \
    && apt-get update \
    && apt-get install -y libmsquic
```
Source: https://github.com/dotnet/dotnet-buildtools-prereqs-docker/blob/7f86167248bcbda898a6b7f17ed01dce3adff2dd/src/debian/12/helix/amd64/Dockerfile#L22-L31

To consume the current main branch of msquic, we pull code from [dotnet/msquic](https://github.com/dotnet/msquic) and build it locally in our docker image:
```docker
WORKDIR /msquic
RUN apt-get update -y && \
    apt-get upgrade -y && \
    apt-get install -y cmake clang ruby-dev gem lttng-tools libssl-dev && \
    gem install fpm
RUN git clone --depth 1 --single-branch --branch main --recursive https://github.com/microsoft/msquic
RUN cd msquic/ && \
    mkdir build && \
    cmake -B build -DCMAKE_BUILD_TYPE=Debug -DQUIC_ENABLE_LOGGING=false -DQUIC_USE_SYSTEM_LIBCRYPTO=true -DQUIC_BUILD_TOOLS=off -DQUIC_BUILD_TEST=off -DQUIC_BUILD_PERF=off -DQUIC_TLS_LIB=quictls -DQUIC_ENABLE_SANITIZERS=on && \
    cd build && \
    cmake --build . --config Debug
RUN cd msquic/build/bin/Debug && \
    rm libmsquic.so && \
    fpm -f -s dir -t deb -n libmsquic -v $( find -type f | cut -d "." -f 4- ) \
    --license MIT --url https://github.com/microsoft/msquic --log error \
    $( ls ./* | cut -d "/" -f 2 | sed -r "s/(.*)/\1=\/usr\/lib\/\1/g" ) && \
    dpkg -i libmsquic_*.deb
```

Source:
https://github.com/dotnet/runtime/blob/c6566fb0bcc539c523be9796ba5af681bf65a904/src/libraries/System.Net.Http/tests/StressTests/HttpStress/Dockerfile#L4-L21

Note that to propagate newest sources / package to the docker image used for the test runs, it must be rebuilt by [dotnet-buildtools-prereqs-docker-all](https://dev.azure.com/dnceng/internal/_build?definitionId=1183&_a=summary) pipeline with `noCache = true` variable. And since [#76630](https://github.com/dotnet/runtime/pull/76630), the newest image will get automatically picked up by the dotnet/runtime infra.

### Windows

Officially released `msquic.dll` is published to NuGet.org, see [Microsoft.Native.Quic.MsQuic.Schannel](https://www.nuget.org/packages/Microsoft.Native.Quic.MsQuic.Schannel).