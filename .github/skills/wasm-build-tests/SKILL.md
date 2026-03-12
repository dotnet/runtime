---
name: wasm-build-tests
description: Build runtime, construct .NET SDK with wasm-tools workload, and run Wasm.Build.Tests. Use when asked to "build wasm", "run wasm tests", "wasm build tests", "test wasm", "build browser wasm", "install wasm workload", or work with src/mono/wasm/Wasm.Build.Tests.
---

# Wasm.Build.Tests — Build & Test Workflow

Pre-flight check + three-phase workflow: validate artifacts → build runtime → construct SDK with workload → run tests. All commands run from the repo root.

## Pre-flight: Validate RuntimeFlavor Artifacts

Ask the user for **RuntimeFlavor** and **Configuration** if not specified.

Before any build phase, check whether existing artifacts match the requested RuntimeFlavor. The runtime build produces flavor-specific artifacts at:

| RuntimeFlavor | Artifact marker path |
|---|---|
| **Mono** | `artifacts\obj\mono\browser.wasm.{Config}\out\lib\` |
| **CoreCLR** | `artifacts\obj\coreclr\browser.wasm.{Config}\libs-native\` |

**Mismatch detection**: If the *other* flavor's marker path exists but the requested flavor's does not, the artifacts were built for a different RuntimeFlavor. In that case, **remove the entire `artifacts` folder** before proceeding:

```powershell
Remove-Item .\artifacts -Recurse -Force
```

This avoids subtle errors from mixing Mono and CoreCLR outputs (stale packages, wrong native libraries, incorrect workload packs).

**Example check** (requesting Mono/Debug):

```powershell
$monoExists = Test-Path ".\artifacts\obj\mono\browser.wasm.Debug\out\lib"
$coreclrExists = Test-Path ".\artifacts\obj\coreclr\browser.wasm.Debug\libs-native"
if ($coreclrExists -and -not $monoExists) {
    Write-Host "CoreCLR artifacts detected but Mono requested — removing artifacts folder"
    Remove-Item .\artifacts -Recurse -Force
}
```

Reverse the check when requesting CoreCLR.

## Phase 1: Build Runtime

| RuntimeFlavor | Command |
|---|---|
| **Mono** (default) | `./build.cmd -bl -os browser -subset mono+libs+packs -c {Config}` |
| **CoreCLR** | `./build.cmd -bl -os browser -subset clr+libs+packs -c {Config}` |

`{Config}` is `Debug` or `Release`.

**When rebuilding after runtime source changes** (same flavor), delete stale packages first:

```
Remove-Item .\artifacts\packages\{Config}\Shipping\* -Recurse -Force
```

Then re-run the build command above.

⏱️ This build takes 15–40 minutes.

## Phase 2: Construct SDK with Workload

Installs a `wasm-tools` workload into a constructed .NET SDK under `artifacts\bin\dotnet-latest`, with a matching base SDK in `artifacts\bin\dotnet-none`.

```
.\dotnet.cmd build -bl -p:TargetOS=browser -p:TargetArchitecture=wasm -p:RuntimeFlavor={Flavor} -c {Config} .\src\mono\wasm\Wasm.Build.Tests -t:InstallWorkloadUsingArtifacts
```

**When new packages were built in Phase 1**, delete the old constructed SDK first:

```
Remove-Item .\artifacts\bin\dotnet-latest -Recurse -Force
```

The `dotnet-none` SDK version matches the `SdkVersionForWorkloadTesting` MSBuild property.

⏱️ Takes 2–5 minutes.

## Phase 3: Run Tests

```
.\dotnet.cmd build -bl -p:TargetOS=browser -p:TargetArchitecture=wasm -p:RuntimeFlavor={Flavor} -c {Config} -t:Test .\src\mono\wasm\Wasm.Build.Tests\ -p:InstallWorkloadForTesting=false {Filters} {ExtraProps}
```

`-p:InstallWorkloadForTesting=false` skips workload re-install (already done in Phase 2).

### Using Constructed SDKs When System SDK Is Too Old

The `.\dotnet.cmd` wrapper resolves the SDK via `global.json` (which has `"rollForward": "major"`), so any reasonably recent SDK on PATH usually works. However, if the system SDK is too old and Phase 2 has already completed, use the constructed SDK directly:

```powershell
# Use dotnet-latest (has wasm-tools workload installed)
.\artifacts\bin\dotnet-latest\dotnet.exe build -bl -p:TargetOS=browser -p:TargetArchitecture=wasm -p:RuntimeFlavor={Flavor} -c {Config} -t:Test .\src\mono\wasm\Wasm.Build.Tests\ -p:InstallWorkloadForTesting=false {Filters}

# Or use dotnet-none (SDK with no workloads — for TestUsingWorkloads=false scenarios)
.\artifacts\bin\dotnet-none\dotnet.exe build -bl -p:TargetOS=browser -p:TargetArchitecture=wasm -p:RuntimeFlavor={Flavor} -c {Config} -t:Test .\src\mono\wasm\Wasm.Build.Tests\ -p:InstallWorkloadForTesting=false -p:TestUsingWorkloads=false {Filters}
```

The SDK version in these directories matches `SdkVersionForWorkloadTesting` (computed from `global.json` and workload manifest versions). Phase 2 downloads this SDK automatically via the `dotnet-install` script.

**When to use which:**

| SDK | Path | Use when |
|---|---|---|
| `dotnet-latest` | `artifacts\bin\dotnet-latest\dotnet.exe` | Running tests with workloads (default for Mono) |
| `dotnet-none` | `artifacts\bin\dotnet-none\dotnet.exe` | Running tests without workloads (`TestUsingWorkloads=false`, default for CoreCLR) |

> ⚠️ These SDKs only exist after Phase 2 completes. Phase 1 still requires `.\dotnet.cmd` (or a compatible system SDK).

### Test Filtering

| Property | Scope | Example |
|---|---|---|
| `-p:XUnitClassName=` | Run all tests in a class | `Wasm.Build.Tests.MainWithArgsTests` |
| `-p:XUnitMethodName=` | Run a single test method | `Wasm.Build.Tests.MainWithArgsTests.AsyncMainWithArgs` |
| `-p:XUnitNamespace=` | Run all tests in a namespace | `Wasm.Build.Tests` |

Use fully qualified names (FQN).

### Additional Properties

| Property | Default | Effect |
|---|---|---|
| `TestUsingWorkloads` | `true` (Mono), `false` (CoreCLR) | Test with/without workloads installed |
| `WasmFingerprintAssets` | — | When `false`, runs tests with `no-fingerprinting` trait |
| `WasmBundlerFriendlyBootConfig` | — | When `true`, runs tests with `bundler-friendly` trait |
| `InstallChromeForTests` | auto in CI | Install Chrome for browser tests |
| `InstallFirefoxForTests` | auto in CI | Install Firefox for browser tests |
| `InstallV8ForTests` | auto in CI | Install V8 for JS engine tests |

## Quick Reference — Full Workflow Example

Mono / Debug / single test:

```powershell
# Pre-flight — check for flavor mismatch
$monoExists = Test-Path ".\artifacts\obj\mono\browser.wasm.Debug\out\lib"
$coreclrExists = Test-Path ".\artifacts\obj\coreclr\browser.wasm.Debug\libs-native"
if ($coreclrExists -and -not $monoExists) { Remove-Item .\artifacts -Recurse -Force }

# Phase 1 — build runtime
./build.cmd -bl -os browser -subset mono+libs+packs -c Debug

# Phase 2 — construct SDK
.\dotnet.cmd build -bl -p:TargetOS=browser -p:TargetArchitecture=wasm -p:RuntimeFlavor=Mono -c Debug .\src\mono\wasm\Wasm.Build.Tests -t:InstallWorkloadUsingArtifacts

# Phase 3 — run one test (if system SDK is too old, use .\artifacts\bin\dotnet-latest\dotnet.exe instead of .\dotnet.cmd)
.\dotnet.cmd build -bl -p:TargetOS=browser -p:TargetArchitecture=wasm -p:RuntimeFlavor=Mono -c Debug -t:Test .\src\mono\wasm\Wasm.Build.Tests\ -p:InstallWorkloadForTesting=false -p:XUnitMethodName=Wasm.Build.Tests.MainWithArgsTests.AsyncMainWithArgs
```

## Rebuild Cheat Sheet

| What changed | Actions needed |
|---|---|
| **Switching RuntimeFlavor** (e.g. Mono→CoreCLR) | Pre-flight detects mismatch → removes `artifacts\` → Phase 1 → Phase 2 → Phase 3 |
| Runtime sources (coreclr/mono/libs) | Delete `artifacts\packages\{Config}\Shipping\*` → Phase 1 → Delete `artifacts\bin\dotnet-latest` → Phase 2 → Phase 3 |
| Test sources only | Phase 3 only |
| Workload infrastructure / packaging | Delete `artifacts\bin\dotnet-latest` → Phase 2 → Phase 3 |
