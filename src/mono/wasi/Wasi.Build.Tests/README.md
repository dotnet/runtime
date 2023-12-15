# Wasi.Build.Tests

Contains tests for wasi project builds, eg. for aot, relinking, globalization
etc. The intent is to check if the build inputs result in a correct app bundle
being generated.

- When running locally, it tests against a local workload install (based on `artifacts`)
  - but this can be turned off with `/p:TestUsingWorkloads=false`
  - in which case, it will run against a sdk with updated workload manifests, but no workload installed

- On CI, both workload, and no-workload cases are tested

- Running:

Linux/macOS: `$ make -C src/mono/wasi run-build-tests`
Windows: `.\dotnet.cmd build .\src\mono\wasi\Wasi.Build.Tests\Wasi.Build.Tests.csproj -c Release -t:Test -p:TargetOS=wasi -p:TargetArchitecture=wasm`

- Specific tests can be run via `XUnitClassName`, and `XUnitMethodName`
  - eg. `XUnitClassName=Wasm.Build.Tests.BlazorWasmTests`
