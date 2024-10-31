# .NET MetaData &ndash; DNMD

DNMD represents a suites of tools for manipulating [ECMA-335][ecma_335] defined metadata. It designed to be written in unmanaged code (that is, C/C++) in a modern style. This doesn't mean it is intended to rely on the latest features or libraries. Rather it is written to use canonical C and C++ in a manner that is clear.

DNMD provides the following tools:

- `dnmd` - A static library with no external dependencies that represents the lowest level of reading ECMA-335.
- `dnmd_interfaces` - A shared library (`.dll`|`.dylib`|`.so`) that consumes `dnmd` and provides higher level .NET APIs. At present the following interfaces are provided:
  - [`IMetaDataDispenser`][api_dispenser]
  - [`IMetaDataImport`][api_import] / [`IMetaDataImport2`][api_import2]
  - [`IMetaDataAssemblyImport`][api_assemblyimport]
- `dnmd_interfaces_static` - A static library version of `dnmd_interfaces`.
- `mddump` - Utility for dumping ECMA-335 tables.

The primary goal of DNMD is to explore the benefits of a rewrite of the metadata APIs found in [dotnet/runtime](https://github.com/dotnet/runtime). The rewrite has the following constraints:

- Must be sharable across any existing .NET runtime implementation.
- Must be cross-platform with minimal OS abstraction layering.
- Represent scenarios that are relevant to modern .NET (that is, .NET 6+).

## Requirements (minimum)

- [CMake](https://cmake.org/download/) 3.10

- C11 and C++14 compliant compilers

## Build

> `git submodule update --init --recursive`

> `cmake -S . -B artifacts`

> `cmake --build artifacts --target install`

## Test

The `test/` directory contains all product tests. The native components for
DNMD should be built first. See the Build section.

The `DNMD.Tests.sln` file can be loaded in Visual Studio to run associated tests.
The managed tests will use the latest build of the DNMD libraries. Keep in mind
the native assets are built with a configuration independent of the tests.

Testing correctness defers to the current implementation of the relevant interface
defined in the .NET runtime the test is running on (for example, `IMetaDataImport`).
The approach is to pass identical arguments to the current implementation and the
implementation in this repo. The return argument and all out arguments are then
compared for equality. In some cases pointers are returned so the pointer is dereferenced
and then hashed.

# Additional Resources

[ECMA-335 specification][ecma_335]

<!-- Links -->
[ecma_335]: https://www.ecma-international.org/publications-and-standards/standards/ecma-335/

[api_dispenser]: https://learn.microsoft.com/dotnet/framework/unmanaged-api/metadata/imetadatadispenser-interface
[api_import]: https://learn.microsoft.com/dotnet/framework/unmanaged-api/metadata/imetadataimport-interface
[api_import2]: https://learn.microsoft.com/dotnet/framework/unmanaged-api/metadata/imetadataimport2-interface
[api_assemblyimport]: https://learn.microsoft.com/dotnet/framework/unmanaged-api/metadata/imetadataassemblyimport-interface