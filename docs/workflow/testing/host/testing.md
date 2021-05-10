# Building and running host tests

The [host tests](/src/installer/tests) use [xunit](http://xunit.github.io/) for their testing framework.

## Building tests

To build the host tests, first build the product:

1.  Build CoreCLR and libraries (`clr` and `libs` subsets):
    ```
    build.cmd/sh -subset clr+libs -c Release
    ```
    * [CoreCLR](../../building/coreclr/README.md) build instructions
    * [Libraries](../../building/libraries/README.md) build instructions

2.  Build the host and packs:
    ```
    build.cmd/sh -subset host+packs.product -runtimeConfiguration Release -librariesConfiguration Release
    ```
    If using a configuration other than Release for CoreCLR/libraries, specify the desired configuration in the `-runtimeConfiguration`/`-librariesConfiguration` arguments.

### Building all tests

The host tests are part of the `host` subset by default, so building the `host` subset also builds the host test. To build just the host tests:
```
build.cmd/sh -subset host.tests -runtimeConfiguration Release -librariesConfiguration Release
```

### Building specific tests

A specific test project can also be directly built. For example:
```
dotnet build src\installer\tests\HostActivation.Tests
```

## Test context

The host tests depend on:
  1. Product binaries in a directory layout matching that of a .NET install
  2. Restored [test projects](/src/installer/tests/Assets/TestProjects) which will be built and run by the tests
  3. TestContextVariables.txt file with property and value pairs which will be read by the tests

When [running all tests](#running-all-tests), the build is configured such that these are created/performed before the start of the test run.

In order to create (or update) these dependencies without running all tests, the build targets that create them - RefreshProjectTestAssets and SetupTestContextVariables - can be directly run for the desired test project. For example:
```
dotnet build src\installer\tests\HostActivation.Tests -t:RefreshProjectTestAssets;SetupTestContextVariables -p:RuntimeConfiguration=Release -p:LibrariesConfiguration=Release
```

## Running tests

### Running all tests

To run all host tests:
```
build.cmd/sh -subset host.tests -test
```

By default, the above command will also build the tests before running them. To run the tests without building them, specify `-testnobuild`.

### Running specific tests

If all tests have not been previously run, make sure the [test context](#test-context) is set up for the test library.

Tests from a specific test project can be run using [`dotnet test`](https://docs.microsoft.com/dotnet/core/tools/dotnet-test) targeting the built test binary. For example:
```
dotnet test artifacts/bin/HostActivation.Tests/Debug/net5.0/HostActivation.Tests.dll
```

To filter to specific tests within the test library, use the [filter options](https://docs.microsoft.com/dotnet/core/tools/dotnet-test#filter-option-details) available for `dotnet test`. For example:
```
dotnet test artifacts/bin/HostActivation.Tests/Debug/net5.0/HostActivation.Tests.dll --filter DependencyResolution
```

### Visual Studio

The [Microsoft.DotNet.CoreSetup.sln](/src/installer/Microsoft.DotNet.CoreSetup.sln) can be used to run and debug host tests through Visual Studio. When using the solution, the product should have already been [built](#building-tests) and the [test context](#test-context) set up.

### Preserving test artifacts

In order to test the hosting components, the tests launch a separate process (e.g. `dotnet`, apphost, native host) and validate the expected output (standard output and error) of the launched process. This usually involves copying or creating test artifacts in the form of an application to run or a .NET install to run against. The tests will delete these artifacts after the test finishes. To allow inspection or usage after the test finishes, set the environment variable `PRESERVE_TEST_RUNS=1` to avoid deleting the test artifacts.
