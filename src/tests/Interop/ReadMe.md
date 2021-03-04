# Interop Testing

Testing Interop in the CoreCLR repo follows other tests in the repo and utilizes a series of small EXE projects that exercise a specific feature.

See `Documentation/building/test-configuration.md` for details on how to create new tests.

## Assets

There should be no more than **1** project type per folder (i.e. a folder can contain a managed and native but no more than **1** of each).

Ancillary source assets for all tests should be located in `Interop/common` and can be easily added to all managed tests via the `Interop.settings.targets` file or native tests via `Interop.cmake`.

A common pattern for testing is using the `Assert` utilities. This class is part of the `CoreCLRTestLibrary` which is included in all test projects by the `Interop.settings.targets` import. In order to use, add the following `using TestLibrary;` in the relevant test file.

### Managed

Managed tests should be designed to use the [SDK style project](https://docs.microsoft.com/en-us/dotnet/core/tools/csproj) system provided by [`dotnet-cli`](https://github.com/dotnet/cli).

### Native

Native test assets use [CMake](https://cmake.org/) and can leverage any of the product build assets. In addition to the use of CMake projects, all native projects should include the following:

`include ("${CLR_INTEROP_TEST_ROOT}/Interop.cmake")`

The above import allows all native projects to be maintained in a unified way.

Native assets should be written in a manner that is as portable as possible across platforms (i.e. Windows, MacOS, Linux).

**Note** Native assets are hard to get right and in many instances scenarios they test may not apply to all platforms. See details in `Documentation/building/test-configuration.md` about how to disable a tests for a specific platform.

## Testing Areas

Interop testing is divided into several areas.

### P/Invoke

The P/Invoke bucket represents tests that involve a [Platform Invoke](https://docs.microsoft.com/en-us/dotnet/standard/native-interop) scenario.

Testing P/Invoke has two aspects:

1) Marshaling types
    * Primitives
    * `Array`
    * Structure
    * Union
    * `String`
    * `Delegate`
1) `DllImportAttribute`
    * Attribute values
    * Search paths

### Marshal API

The Marshal API surface area testing is traditionally done via unit testing and far better suited in the [library test folder](https://github.com/dotnet/runtime/tree/main/src/libraries/System.Runtime.InteropServices/tests). Cases where testing the API surface area requires native tests assets will be performed in the [coreclr test folder](https://github.com/dotnet/runtime/tree/main/src/tests/Interop) repo.

### NativeLibrary

This series has unit tests corresponding to `System.Runtime.NativeLibrary` APIs and related events in `System.Runtime.Loader.AssemblyLoadContext`.

## Common Task steps

### Adding new native project
1) Update `src/tests/Interop/CMakeLists.txt` to include new test asset directory.
1) Verify project builds by running `build-tests.cmd`/`build-tests.sh` from repo root.

### Adding new managed project
1) The build system automatically discovers managed test projects.
1) Verify project builds by running `build-tests.cmd`/`build-tests.sh` from repo root.
