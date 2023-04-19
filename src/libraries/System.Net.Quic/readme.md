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
RUN curl -LO https://packages.microsoft.com/keys/microsoft.asc && \
    echo 2cfd20a306b2fa5e25522d78f2ef50a1f429d35fd30bd983e2ebffc2b80944fa microsoft.asc | sha256sum --check - && \
    apt-key add microsoft.asc && \
    rm microsoft.asc && \
    apt-add-repository https://packages.microsoft.com/debian/11/prod && \
    apt-get update && \
    apt-get install -y libmsquic
```
Source: https://github.com/dotnet/dotnet-buildtools-prereqs-docker/blob/efbcd1079edef4698ada1676a5e33c4c9672f85a/src/debian/11/helix/amd64/Dockerfile#L44-L52

To consume the current main branch of msquic, we pull code from [dotnet/msquic](https://github.com/dotnet/msquic) and build it locally in our docker image:
```docker
WORKDIR /msquic
RUN apt-get update -y && \
    apt-get upgrade -y && \
    apt-get install -y cmake clang ruby-dev gem lttng-tools libssl-dev && \
    gem install fpm
RUN git clone --recursive https://github.com/dotnet/msquic
RUN cd msquic/src/msquic && \
    mkdir build && \
    cmake -B build -DCMAKE_BUILD_TYPE=Release -DQUIC_ENABLE_LOGGING=false -DQUIC_USE_SYSTEM_LIBCRYPTO=true -DQUIC_BUILD_TOOLS=off -DQUIC_BUILD_TEST=off -DQUIC_BUILD_PERF=off && \
    cd build && \
    cmake --build . --config Release
RUN cd msquic/src/msquic/build/bin/Release && \
    rm libmsquic.so && \
    fpm -f -s dir -t deb -n libmsquic -v $( find -type f | cut -d "." -f 4- ) \
    --license MIT --url https://github.com/microsoft/msquic --log error \
    $( ls ./* | cut -d "/" -f 2 | sed -r "s/(.*)/\1=\/usr\/lib\/\1/g" ) && \
    dpkg -i libmsquic_*.deb
```

Source:
https://github.com/dotnet/runtime/blob/bd540938a4830ee91dec5ee2d39545b2f69a19d5/src/libraries/System.Net.Http/tests/StressTests/HttpStress/Dockerfile#L4-L21

Note that to propagate newest sources / package to the docker image used for the test runs, it must be rebuilt by [dotnet-buildtools-prereqs-docker-all](https://dev.azure.com/dnceng/internal/_build?definitionId=1183&_a=summary) pipeline with `noCache = true` variable. And since [#76630](https://github.com/dotnet/runtime/pull/76630), the newest image will get automatically picked up by the dotnet/runtime infra.

### Windows

Officially released `msquic.dll` is published to NuGet.org, see [Microsoft.Native.Quic.MsQuic.Schannel](https://www.nuget.org/packages/Microsoft.Native.Quic.MsQuic.Schannel).

To consume MsQuic from the current main branch, we use [dotnet/msquic](https://github.com/dotnet/msquic) repository which will build and publish `msquic.dll` to the transport feed, e.g. [dotnet8-transport](https://dev.azure.com/dnceng/public/_artifacts/feed/dotnet8-transport). And from there, it'll get flown into this repository via [Darc subscription](https://github.com/dotnet/arcade/blob/main/Documentation/Darc.md). See https://github.com/dotnet/runtime/blob/bd540938a4830ee91dec5ee2d39545b2f69a19d5/eng/Version.Details.xml#L7-L10 and maestro-bot PR: https://github.com/dotnet/runtime/pull/71900.


System.Net.Quic [project file](https://github.com/dotnet/runtime/blob/0304f1f5157a8280fa093bdfc7cfb8d9f62e016f/src/libraries/System.Net.Quic/src/System.Net.Quic.csproj) allows switching between those two options with [`UseQuicTransportPackage` property](https://github.com/dotnet/runtime/blob/0304f1f5157a8280fa093bdfc7cfb8d9f62e016f/src/libraries/System.Net.Quic/src/System.Net.Quic.csproj#L15).
