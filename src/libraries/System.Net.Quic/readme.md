# MsQuic

`System.Net.Quic` depends on [MsQuic](https://github.com/microsoft/msquic), Microsoft, cross-platform, native implementation of the [QUIC](https://datatracker.ietf.org/wg/quic/about/) protocol.
Currently, `System.Net.Quic` depends on [**msquic@7b31e149a9d1ed7a6850e8253ba3d0af707150e5**](https://github.com/microsoft/msquic/commit/7b31e149a9d1ed7a6850e8253ba3d0af707150e5) revision.

## Usage

### Build MsQuic

[MsQuic build docs](https://github.com/microsoft/msquic/blob/main/docs/BUILD.md)

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
# build msquic in debug with logging and stub tls
rm -rf build
mkdir build
cmake -B build -DCMAKE_BUILD_TYPE=Debug -DQUIC_ENABLE_LOGGING=on -DQUIC_TLS=stub
cd build
cmake --build . --config Debug

# copy msquic into runtime
yes | cp -rf bin/Debug/libmsquic.* <path-to-runtime>/src/libraries/System.Net.Quic/src/
```

#### Windows
Prerequisites:
- Latest [Windows Insider Builds](https://insider.windows.com/en-us/), Insiders Fast build. This is required for SChannel support for QUIC.
  - To confirm you have a new enough build, run winver on command line and confirm you version is greater than Version 2004 (OS Build 20145.1000).

TODO

