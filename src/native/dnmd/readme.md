# .NET MetaData &ndash; DNMD

## Requirements (minimum)

- [CMake](https://cmake.org/download/) 3.10

- C11 and C++14 compliant compilers

## Build

> `cmake -S . -B artifacts`

> `cmake --build artifacts --target install`

## Test

The `test/` directory contains all product tests. The native components for
DNMD should be built first. See the Build section.

The `DNMD.Tests.sln` file can be loaded in Visual Studio to run associated tests.
The managed tests will use the latest build of the DNMD libraries. Keep in mind
the native assets are built with a configuration independent of the tests.