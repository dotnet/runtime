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

2.  Build the host:
    ```
    build.cmd/sh -subset host -runtimeConfiguration Release -librariesConfiguration Release
    ```
    If using a configuration other than Release for CoreCLR/libraries, specify the desired configuration in the `-runtimeConfiguration`/`-librariesConfiguration` arguments.

### Building all tests

The host tests are part of the `host` subset by default, so building the `host` subset also builds the host tests. To build just the host tests:
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
  1. Pre-built [test project](/src/installer/tests/Assets/Projects) output which will be copied and run by the tests. The `host.pretest` subset builds these projects.
  2. Product binaries in a directory layout matching that of a .NET install. The `host.pretest` subset creates this layout.
  3. TestContextVariables.txt files with property and value pairs which will be read by the tests. The `host.tests` subset creates these files as part of building the tests.

When [running all tests](#running-all-tests), the build is configured such that these are created/performed before the start of the test run.

In order to create (or update) these dependencies without running all tests:
  1. Build the `host.pretest` subset. By default, this is included in the `host` subset. This corresponds to (1) and (2) above.
  2. Build the desired test project. This corresponds to (3) above. Building the test itself will run the `SetupTestContextVariables` target, but it can also be run independently - for example:
  ```
  dotnet build src\installer\tests\HostActivation.Tests -t:SetupTestContextVariables -p:RuntimeConfiguration=Release -p:LibrariesConfiguration=Release
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

Tests from a specific test project can be run using [`dotnet test`](https://learn.microsoft.com/dotnet/core/tools/dotnet-test) targeting the built test binary. For example:
```
dotnet test artifacts/bin/HostActivation.Tests/Debug/net6.0/HostActivation.Tests.dll --filter category!=failing
```

To filter to specific tests within the test library, use the [filter options](https://learn.microsoft.com/dotnet/core/tools/dotnet-test#filter-option-details) available for `dotnet test`. For example:
```
dotnet test artifacts/bin/HostActivation.Tests/Debug/net6.0/HostActivation.Tests.dll --filter "DependencyResolution&category!=failing"
```

The `category!=failing` is to respect the [filtering traits](../libraries/filtering-tests.md) used by the runtime repo.

### Visual Studio

The [Microsoft.DotNet.CoreSetup.sln](/src/installer/Microsoft.DotNet.CoreSetup.sln) can be used to run and debug host tests through Visual Studio. When using the solution, the product should have already been [built](#building-tests) and the [test context](#test-context) set up.

If you built the runtime or libraries with a different configuration from the host, you have to specify this when starting visual studio:

```console
build.cmd -vs Microsoft.DotNet.CoreSetup -rc Release -lc Release
```

## Investigating failures

When [running all tests](#running-all-tests), reports with results will be generated under `<repo_root>\artifacts\TestResults`. When [running individual tests](#running-specific-tests), results will be output to the console by default and can be configured via [`dotnet test` options](https://learn.microsoft.com/dotnet/core/tools/dotnet-test#options).

In order to test the hosting components, the tests launch a separate process (e.g. `dotnet`, apphost, native host) and validate the expected output (standard output and error) of the launched process. This usually involves copying or creating test artifacts in the form of an application to run or a .NET install to run against.

On failure, tests will report the file, arguments, and environment for the launched process that failed validation. With [preserved test artifacts](#preserving-test-artifacts), this information can be used to directly debug the specific scenario that the test was running.

### Preserving test artifacts

The tests will delete any generated test artifacts after the test finishes. To allow inspection or usage after the test finishes, set the environment variable `PRESERVE_TEST_RUNS=1` to avoid deleting the test artifacts.

