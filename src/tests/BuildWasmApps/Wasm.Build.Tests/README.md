# Wasm.Build.Tests

Contains tests for wasm project builds, eg. for aot, relinking, globalization
etc. The intent is to check if the build inputs result in a correct app bundle
being generated.

- When running locally, it tests against a local workload install (based on `artifacts`)
  - but this can be turned off with `/p:TestUsingWorkloads=false`
  - in which case, it will run against `emsdk` from `EMSDK_PATH`

- On CI, both workload, and emsdk cases are tested

- Running:

Linux/macOS: `$ make -C src/mono/wasm run-build-tests`
Windows: `.\dotnet.cmd build .\src\tests\BuildWasmApps\Wasm.Build.Tests\Wasm.Build.Tests.csproj -c Release -t:Test -p:TargetOS=Browser -p:TargetArchitecture=wasm`

- Specific tests can be run via `XUnitClassName`, and `XUnitMethodName`
  - eg. `XUnitClassName=Wasm.Build.Tests.BlazorWasmTests`
