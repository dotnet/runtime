# Runtime Pipeline Architecture

This document describes the architecture of the `runtime.yml` pipeline — the primary CI pipeline for dotnet/runtime. It is intended as a reference for understanding how the pipeline is structured, how jobs are composed, and how to make changes.

## Overview

The runtime pipeline (`eng/pipelines/runtime.yml`) orchestrates builds and tests across 20+ platform configurations covering CoreCLR, Mono, Libraries, and Installers. It uses a layered template system where a single pipeline definition fans out into dozens of jobs through platform multiplexing.

```
runtime.yml
  │
  ├─ Stage: EvaluatePaths (PR only)
  │    └─ Determines which subsets changed → gates downstream jobs
  │
  └─ Stage: Build
       ├─ CoreCLR jobs (multiple platforms/configs)
       ├─ Libraries jobs
       ├─ Mono jobs
       ├─ WASM jobs
       ├─ Mobile jobs (Android, iOS)
       ├─ Installer jobs
       ├─ NativeAOT jobs
       ├─ Tool/CrossDac jobs
       └─ Diagnostics test jobs (non-blocking)
```

## Template Chain

The pipeline uses a four-layer template chain that adds infrastructure at each level:

```
runtime.yml                          ← triggers, parameters, variables, stages
  └─ pipeline-with-resources.yml     ← cross-compilation container definitions
      └─ templateDispatch.yml        ← routes official vs. public builds
          └─ template1es.yml         ← 1ES security/compliance (official)
             OR templatePublic.yml   ← simpler execution (PR/public)
```

### Layer 1: `runtime.yml` (entry point)

Defines:
- **Triggers**: `ci:` and `pr:` branch filters, path includes/excludes, and `schedules:`
- **Parameters**: Pipeline-level inputs (e.g., `diagnosticsBranch`)
- **Resources**: External repository references (e.g., `dotnet/diagnostics`)
- **Variables**: Imported from `variables.yml` and `helix-platforms.yml`
- **Stages**: `EvaluatePaths` and `Build`, passed down via `extends:`

### Layer 2: `pipeline-with-resources.yml`

Adds container definitions for ~20 cross-compilation targets (Linux ARM, ARM64, musl variants, Android, Browser WASM, etc.). These containers carry pre-built toolchains so jobs can cross-compile without installing tooling at runtime.

### Layer 3: `templateDispatch.yml`

Routes to either `template1es.yml` (official/internal builds) or `templatePublic.yml` (PR/public builds) based on the `isOfficialBuild` variable. This separation allows official builds to include security scanning (CodeQL, CredScan, PoliCheck) without burdening PR validation.

### Layer 4: `template1es.yml` / `templatePublic.yml`

The terminal templates that define the actual AzDO pipeline structure — pools, security checks, and stage execution. `templatePublic.yml` also instantiates the container resources defined in Layer 2.

## Stages

### EvaluatePaths (PR only)

Conditionally included when `Build.Reason == 'PullRequest'`. Uses `evaluate-default-paths.yml` to determine which components changed by comparing `HEAD^1` with the current commit.

**Outputs**: Variables of the form `SetPathVars_<subset>.containsChange` (e.g., `SetPathVars_coreclr.containsChange`, `SetPathVars_libraries.containsChange`).

**Subsets** include: `coreclr`, `libraries`, `mono_excluding_wasm`, `wasmbuildtests`, `tools_illink`, `tools_cdac`, `non_mono_and_wasm`, and others. Each subset has include/exclude path patterns defined in `evaluate-default-paths.yml`.

**Downstream usage**: Jobs reference these outputs in their `condition:` expressions:

```yaml
condition: >-
  or(
    eq(stageDependencies.EvaluatePaths.evaluate_paths.outputs['SetPathVars_coreclr.containsChange'], true),
    eq(variables['isRollingBuild'], true))
```

This ensures jobs only run when relevant paths change on PRs, while always running on rolling (CI/scheduled) builds.

### Build

The main stage containing all build and test jobs. Jobs are organized by component and configuration, each using `platform-matrix.yml` to fan out across platforms.

## Platform Matrix System

The core mechanism for multi-platform job generation is the `platform-matrix.yml` template. It converts a single logical job definition into multiple platform-specific jobs.

### How it works

```yaml
- template: /eng/pipelines/common/platform-matrix.yml
  parameters:
    jobTemplate: /eng/pipelines/common/global-build-job.yml
    buildConfig: Release
    platforms:
    - linux_x64
    - windows_x64
    - osx_arm64
    jobParameters:
      nameSuffix: CoreCLR_Libraries
      buildArgs: -s clr+libs+libs.tests ...
```

This expands into three separate jobs, one per platform. For each platform, `platform-matrix.yml`:

1. Checks if the platform is in the `platforms` array
2. Calls `xplat-setup.yml` to configure platform-specific variables
3. Passes through to the specified `jobTemplate` with merged `jobParameters`

### Platform-specific variables (`xplat-setup.yml`)

Each platform gets standardized variables:

| Variable | Windows | Linux/macOS |
|----------|---------|-------------|
| `archiveExtension` | `.zip` | `.tar.gz` |
| `archiveType` | `zip` | `tar` |
| `scriptExt` | `.cmd` | `.sh` |
| `exeExt` | `.exe` | *(empty)* |
| `dir` | `\` | `/` |
| `osGroup` | `windows` | `linux`, `osx` |
| `archType` | `x64`, `x86`, `arm64` | `x64`, `arm64`, `arm` |

> **Important**: `archiveExtension` and `archiveType` are compile-time template expressions (`${{ if }}`). They resolve based on the *job's* platform, not any other job's platform. This matters when downloading artifacts produced by a different platform — see [Diagnostics Pipeline Consolidation](diagnostics-pipeline-consolidation.md) for details.

## Job Templates

### `global-build-job.yml` (primary)

Used by the majority of jobs. Provides a standardized build job structure:

**Job naming**: `build_{osGroup}{osSubgroup}_{archType}_{buildConfig}_{nameSuffix}`

Example: `build_windows_x64_Release_CoreCLR_Libraries`

**Key parameters**:
- `nameSuffix` — distinguishes jobs on the same platform (e.g., `CoreCLR_Libraries`, `NativeAOT`)
- `buildArgs` — arguments passed to `build.sh`/`build.cmd`
- `preBuildSteps` / `postBuildSteps` — custom steps before/after the build
- `dependsOnGlobalBuilds` — express dependencies using the naming convention

**Execution flow**:
1. Pre-build steps (optional)
2. Checkout and dependency restore
3. `build.sh -ci -arch $(archType) -os $(osGroup) {buildArgs}` (via `global-build-step.yml`)
4. CMake log collection
5. Post-build steps (optional)

### `runtime-diag-job.yml` (diagnostics)

Specialized template for SOS/DAC diagnostic tests. Checks out the `dotnet/diagnostics` repository and runs its test suite against a live runtime build.

**Key parameters**:
- `liveRuntimeDir` — path to shared framework binaries
- `useCdac` — `true` for cDAC tests, `false` for legacy DAC tests
- `shouldContinueOnError` — makes failures non-blocking

### Other job template families

| Template | Purpose |
|----------|---------|
| `coreclr/templates/build-test-job.yml` | Builds CoreCLR test assets |
| `coreclr/templates/run-test-job.yml` | Runs CoreCLR tests on Helix |
| `libraries/run-test-job.yml` | Runs library tests on Helix |
| `browser-wasm-coreclr-build-tests.yml` | WASM-specific build tests |

## Key Variables

Defined in `eng/pipelines/common/variables.yml`:

| Variable | Description |
|----------|-------------|
| `isOfficialBuild` | `true` only for internal project + official pipeline definition |
| `isRollingBuild` | `true` for any non-PR trigger (CI, scheduled, manual) |
| `debugOnPrReleaseOnRolling` | `Debug` on PRs, `Release` on rolling builds |
| `isDefaultPipeline` | Complex condition for non-WASM or rolling WASM builds |

`debugOnPrReleaseOnRolling` is particularly important because it determines build configurations and appears in job names referenced by `dependsOn` and artifact names:

```yaml
dependsOn:
- ${{ format('build_windows_x64_{0}_CoreCLR_Libraries', variables.debugOnPrReleaseOnRolling) }}
```

## Artifact Flow

Artifacts are the primary mechanism for passing build outputs between jobs.

### Upload (`upload-artifact-step.yml`)

Compresses a directory and publishes it as a pipeline artifact:

```yaml
- template: /eng/pipelines/common/upload-artifact-step.yml
  parameters:
    rootFolder: $(buildProductRootFolderPath)
    artifactName: CoreCLR_Product_$(osGroup)$(osSubgroup)_$(archType)_$(_BuildConfig)
    archiveType: $(archiveType)
    archiveExtension: $(archiveExtension)
```

### Download (`download-artifact-step.yml`)

Downloads and extracts an artifact from the *current* pipeline run:

```yaml
- template: /eng/pipelines/common/download-artifact-step.yml
  parameters:
    artifactName: CoreCLR_Product_windows_x64_Release
    artifactFileName: CoreCLR_Product_windows_x64_Release.zip
    unpackFolder: $(Build.SourcesDirectory)/artifacts/product
```

### Cross-build download (`download-specific-artifact-step.yml`)

Downloads artifacts from a *different* pipeline run, using `buildId`, `pipeline`, and `branchName` to locate the source build. Used for scenarios requiring artifacts from another pipeline.

### Archive format considerations

Since `archiveType` and `archiveExtension` resolve per-platform, downloading cross-platform artifacts requires hardcoding the correct format. For example, downloading a Windows-produced artifact on Linux must use `.zip`, not the Linux default `.tar.gz`. When cross-platform artifact sharing is needed, standardizing on `.tar.gz` (which works on all platforms) avoids this issue.

## Helix Test Infrastructure

Test jobs distribute work to Helix, Microsoft's distributed test execution system. Queue assignments are defined in `libraries/helix-queues-setup.yml`, which maps platform configurations to machine pools:

- **Innerloop** (PR): Runs on a focused set of queues for fast feedback
- **Outerloop** (rolling): Runs on broader queue sets for coverage

Queue names reference container images (e.g., `(Alpine.323.Amd64.Open)AzureLinux.3.Amd64.Open@mcr.microsoft.com/...`) or bare machine pools (e.g., `Windows.Amd64.Server2022.Open`).

Helix queue variables are defined in `helix-platforms.yml` and referenced as `$(helix_windows_x64_latest)`, `$(helix_linux_arm64_oldest)`, etc.

## Job Categories in runtime.yml

The Build stage contains approximately 55 `platform-matrix.yml` invocations organized into these categories:

| Category | Suffix | Platforms | Purpose |
|----------|--------|-----------|---------|
| CoreCLR Verticals | `AllSubsets_CoreCLR` | ARM, ARM64, musl | Full CoreCLR + libs + host + packs |
| CoreCLR_Libraries | `CoreCLR_Libraries` | x64, ARM64, musl, OSX | Core library tests with CoreCLR |
| Libraries_CheckedCoreCLR | `Libraries_CheckedCoreCLR` | x64, ARM64 | Library tests with Checked CoreCLR |
| CoreCLR_ReleaseLibraries | `CoreCLR_ReleaseLibraries` | ARM, x86, ARM64 | Release CoreCLR + library build |
| NativeAOT | `NativeAOT` | x64 | AOT compilation smoke tests |
| NativeAOT_Libraries | `NativeAOT_Libraries` | x64 | Library tests with NativeAOT |
| Mono | `AllSubsets_Mono` | Various | Mono runtime + libs |
| WASM | Various | browser_wasm | WebAssembly builds and tests |
| Mobile | Various | Android, iOS | Mobile platform builds |
| Installer | `Installer_Build_And_Test` | Various | Host + packs |
| GNU Compiler | `Native_GCC` | gcc_linux_x64 | GCC native build validation |
| Crossgen | `CoreCLR` | linux_x86 | Crossgen on System.Private.CoreLib |
| CrossDac | `CrossDac` | Various | Cross-platform DAC builds |
| CLR Tools | `CLR_Tools_Tests` | x64 | ILLink, cDAC tools |
| Diagnostics | `cDAC`, `DAC` | windows_x64 | SOS diagnostic tests (non-blocking) |

## Adding a New Job

To add a new job to the pipeline:

1. **Choose a job template** — usually `global-build-job.yml` for builds, or a specialized template
2. **Add a `platform-matrix.yml` invocation** in the Build stage:
   ```yaml
   - template: /eng/pipelines/common/platform-matrix.yml
     parameters:
       jobTemplate: /eng/pipelines/common/global-build-job.yml
       buildConfig: ${{ variables.debugOnPrReleaseOnRolling }}
       platforms:
       - windows_x64
       jobParameters:
         nameSuffix: MyNewJob
         buildArgs: -s clr+libs ...
   ```
3. **Add a path condition** to skip the job on unrelated PRs:
   ```yaml
       condition: >-
         or(
           eq(stageDependencies.EvaluatePaths.evaluate_paths.outputs['SetPathVars_coreclr.containsChange'], true),
           eq(variables['isRollingBuild'], true))
   ```
4. **Express dependencies** if your job needs artifacts from another job:
   ```yaml
       jobParameters:
         dependsOn:
         - ${{ format('build_windows_x64_{0}_CoreCLR_Libraries', variables.debugOnPrReleaseOnRolling) }}
         preBuildSteps:
         - template: /eng/pipelines/common/download-artifact-step.yml
           parameters:
             artifactName: ...
   ```
5. **Make it non-blocking** (optional) with `shouldContinueOnError: true`

## File Reference

| File | Purpose |
|------|---------|
| `eng/pipelines/runtime.yml` | Pipeline entry point |
| `eng/pipelines/common/variables.yml` | Pipeline-wide variables |
| `eng/pipelines/common/platform-matrix.yml` | Platform multiplexer |
| `eng/pipelines/common/xplat-setup.yml` | Per-platform variable setup |
| `eng/pipelines/common/global-build-job.yml` | Standard build job template |
| `eng/pipelines/common/global-build-step.yml` | Build invocation step |
| `eng/pipelines/common/upload-artifact-step.yml` | Artifact upload |
| `eng/pipelines/common/download-artifact-step.yml` | Artifact download (same pipeline) |
| `eng/pipelines/common/download-specific-artifact-step.yml` | Artifact download (cross-pipeline) |
| `eng/pipelines/common/evaluate-default-paths.yml` | PR path filtering definitions |
| `eng/pipelines/common/evaluate-paths-job.yml` | Path evaluation job |
| `eng/pipelines/common/templates/pipeline-with-resources.yml` | Container resources |
| `eng/pipelines/common/templates/templateDispatch.yml` | Official vs. public routing |
| `eng/pipelines/common/templates/templatePublic.yml` | Public build execution |
| `eng/pipelines/common/templates/template1es.yml` | Official build execution |
| `eng/pipelines/helix-platforms.yml` | Helix queue variable definitions |
| `eng/pipelines/libraries/helix-queues-setup.yml` | Platform-to-queue mapping |
| `eng/pipelines/diagnostics/runtime-diag-job.yml` | Diagnostics test job template |
