# MsQuic

`System.Net.Quic` depends on [MsQuic](https://github.com/microsoft/msquic), Microsoft, cross-platform, native implementation of the [QUIC](https://datatracker.ietf.org/wg/quic/about/) protocol.
Currently, `System.Net.Quic` depends on [**msquic@3e40721bff04845208bc07eb4ee0c5e421e6388d**](https://github.com/microsoft/msquic/commit/3e40721bff04845208bc07eb4ee0c5e421e6388d) revision.

## Usage

MsQuic library in now being published so there's no need to compile it yourself.

For a reference, the packaging repository is in https://github.com/dotnet/msquic.

### Windows
Prerequisites:
- Latest [Windows Insider Builds](https://insider.windows.com/en-us/), Insiders Fast build. This is required for SChannel support for QUIC.
  - To confirm you have a new enough build, run winver on command line and confirm you version is greater than Version 2004 (OS Build 20145.1000).
- Turned on TLS 1.3
  - It is turned on by default, to confirm you can check the appropriate registry `Computer\HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\SecurityProviders\SCHANNEL\Protocols` (empty means default which means enabled).

During the build, the `msquic.dll` is automatically downloaded and placed in correct directories in order to be picked up by the runtime. It is also published as part of the runtime for Windows.

### Linux

On Linux, `libmsquic` is published via Microsoft official Linux package repository `packages.microsoft.com`. In order to consume it, you have to add it manually, see https://docs.microsoft.com/en-us/windows-server/administration/linux-package-repository-for-microsoft-software. After that, you should be able to install it via the package manager of your distro, e.g. for Ubuntu:
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
- Latest [Windows Insider Builds](https://insider.windows.com/en-us/), Insiders Fast build. This is required for SChannel support for QUIC.
  - To confirm you have a new enough build, run `winver` on command line and confirm you version is greater than Version 2004 (OS Build 20145.1000).

Follow the instructions from msquic build documentation.
