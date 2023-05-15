# Wasm.Build.Tests

Contains tests for wasm project builds, eg. for aot, relinking, globalization
etc. The intent is to check if the build inputs result in a correct app bundle
being generated.

- When running locally, it tests against a local workload install (based on `artifacts`)
  - but this can be turned off with `/p:TestUsingWorkloads=false`
  - in which case, it will run against a sdk that has updated workload
    manifests for `wasm-tools*`, but does not have the workload installed.
    Typically installed in `artifacts/bin/dotnet-none`.

- On CI, both workload, and no-workload cases are tested

- Running:

Linux/macOS: `$ make -C src/mono/wasm run-build-tests`
Windows: `.\dotnet.cmd build .\src\mono\wasm\Wasm.Build.Tests\Wasm.Build.Tests.csproj -c Release -t:Test -p:TargetOS=browser -p:TargetArchitecture=wasm`

- Specific tests can be run via `XUnitClassName`, and `XUnitMethodName`
  - eg. `XUnitClassName=Wasm.Build.Tests.BlazorWasmTests`

## Running on helix

The wasm.build.tests are built, and sent as a payload to helix, alongwith
either sdk+no-workload-installed, or sdk+workload. And on helix the individual unit
tests generate test projects, and build those.

## About the tests

Most of the tests are structured on the idea that for a given case (or
combination of options), we want to:

1. build once
2. run the same build with different hosts, eg. V8, Chrome, Firefox etc.

For this, the builds get cached using `BuildArgs` as the key.

## notes:

- when running locally, the default is to test with workloads. For this, sdk
  with `$(SdkVersionForWorkloadTesting)` is installed in
  `artifacts/bin/dotnet-latest`. And the workload packs are installed there
  using packages in `artifacts/packages/$(Configuration)/Shipping`.
    - If the packages get updated, then the workload will get installed again.

    - Keep in mind, that if you have any non-test changes, and don't regenerate
      the nuget packages, then you will still be running tests against the
      "old" sdk

- If you aren't explicitly testing workloads, then it can be useful to run the
  tests with `-p:TestUsingWorkloads=false`, and this will cause the tests to
  use the build bits from the usual locations in artifacts, without requiring
  regenerating the nugets, and workload re-install.

- Each test gets a randomly generated "id". This `id` can be used to find the
  binlogs, or the test directories.

## Useful environment variables

- `SHOW_BUILD_OUTPUT` - will show the build output to the console
- `SKIP_PROJECT_CLEANUP` - won't remove the temporary project directories generated for the tests
