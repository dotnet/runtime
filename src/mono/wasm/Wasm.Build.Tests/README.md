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

Linux/macOS: `$ make -C src/mono/(browser|wasi) run-build-tests`
Windows: `.\dotnet.cmd build .\src\mono\wasm\Wasm.Build.Tests\Wasm.Build.Tests.csproj -c Release -t:Test -p:TargetOS=browser -p:TargetArchitecture=wasm`

- Specific tests can be run via `XUnitClassName`, and `XUnitMethodName`
  - e.g. `XUnitClassName=Wasm.Build.Tests.BlazorWasmTests` for running class of tests
  - e.g. `XUnitMethodName=Wasm.Build.Tests.Blazor.MiscTests3.WithDllImportInMainAssembly` for running a specific test.

## Running on helix

The wasm.build.tests are built, and sent as a payload to helix, alongwith
either sdk+no-workload-installed, or sdk+workload. And on helix the individual unit
tests generate test projects, and build those.

## About the tests

Most of the tests are structured on the idea that for a given case (or
combination of options), we want to:

1. build once
2. run the same build with different hosts, eg. Chrome, Firefox etc.

For this, the builds get cached using `ProjectInfo` as the key.

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

- Each test is saved in directory with randomly generated name and unique `ProjectInfo`. `ProjectInfo` can be used to find the binlogs, or cached builds.

## Useful environment variables

- `SHOW_BUILD_OUTPUT` - will show the build output to the console
- `SKIP_PROJECT_CLEANUP` - won't remove the temporary project directories generated for the tests

## How to add tests

Blazor specific tests should be located in `Blazor` directory. If you are adding a new class with tests, list it in `eng/testing/scenarios/BuildWasmAppsJobsList.txt`. If you are adding a new test to existing class, make sure it does not prolong the execution time significantly. Tests run on parallel on CI and having one class running much longer than the average prolongs the total execution time.

`Blazor.AssetCachingTests` exercises the browser HTTP cache using the hosted
`BlazorWebWasm` test asset. Its recovery cases seed cacheable invalid boot
responses, verify the cache modes and bounded download attempts, and check that
a repaired entry is reused on the next boot. These tests run against either
runtime using the locally constructed workload SDK, and assert that the client
uses the expected runtime pack rather than an SDK-default downloaded pack.
The resolved-pack loader bytes are compared with the extracted SDK-installed
pack when independently available, or with the local package if that runtime
pack is absent or the application resolves the installed pack itself.
The constructed SDK must be refreshed after rebuilding
the runtime; comparison with an installed pack checks SDK agreement, not
package freshness.
The cache-seeding page is a static HTML file in the hosted test asset. Native Wasm
recovery also runs with one download slot to exercise bootstrap dependencies.
Cache-reuse assertions use the small client assembly; browser cache retention
of large runtime binaries is discretionary.
Assembly selection follows `UseWebcil`: run the cache tests with
`-p:WasmEnableWebcil=false` to cover DLL resources as well as Webcil, and run
the class with `-p:WasmFingerprintAssets=false` to cover conditional revalidation
and bounded failed-download retries without cache reloads in the existing lane.
The tests follow the lane's fingerprinting configuration rather than overriding
it per theory row. Hosted builds and published apps share the Blazor server's
ephemeral-loopback URL default, while explicit per-test URLs are preserved.
The generated Mono client explicitly selects the current built runtime and
WebAssembly SDK pack, even when Blazor targets the previous framework.
It imports `WasmOverridePacks.targets` and sets `WBTMonoRuntimePackVersion`;
the test does not inject an MSBuild target. This opt-in selects current packs
before SDK resolution without enabling the legacy resolved-path overrides.
Existing imports without the opt-in retain their previous behavior, and
previous-runtime compatibility tests are not globally pinned to current packs.
The shared client project remains unchanged. After a framework retargeting,
rebuild the runtime packages and recreate the constructed workload SDK before
validating: an older manifest can map the previous framework to the current
package and hide a runtime-selection failure that a fresh CI SDK exposes.

`ModuleConfigTests.NativeSymbolsDownloadDoesNotHang` covers emitted fingerprinted
CoreCLR native symbols with HTTP and network failures, optional symbol loading,
and successful startup. On release/11.0 the test relinks to emit the symbol map,
which is not included in the prebuilt CoreCLR runtime pack.
The .NET 11 SDK also omits the newly linked map from its boot resources, so the
test copies the actual emitted map into the published framework directory under
its content hash and supplies that resource through the runtime configuration.
This tests symbol download/startup behavior, not SDK symbol staging.
Native symbols retain their no-retry behavior because
startup awaits them before queuing the other downloads; waiting for the retry
barrier at that point would deadlock. Optional HTTP failures permit startup,
while rejected fetches retain their existing fatal-error behavior. Native Wasm
cache recovery remains covered by `Blazor.AssetCachingTests`.

If you want to test templating mechanism, use `CreateWasmTemplateProject`. Otherwise, use `CopyTestAsset` with either `WasmBasicTestApp` or `BlazorBasicTestApp`, adding your custom `TestScenario` or using a generic `DotnetRun` scenario in case of WASM app and adding a page with test in case of Blazor app. Bigger snippets of code should be saved in `src/mono/wasm/testassets` and placed in the application using methods: `ReplaceFile` or `File.Move`. Replacing existing small parts of code with custom lines is done with `UpdateFile`.