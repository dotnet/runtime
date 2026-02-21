# Consolidating Diagnostics Tests into the Runtime Pipeline

## Summary

This document describes the consolidation of the `runtime-diagnostics` pipeline into the main `runtime` pipeline (`eng/pipelines/runtime.yml`). The goal is to eliminate redundant CoreCLR builds by reusing artifacts already produced by the runtime pipeline, and to simplify CI coordination for diagnostics test validation.

## Background

The `runtime-diagnostics` pipeline (`eng/pipelines/runtime-diagnostics.yml`) runs SOS/DAC diagnostic tests from the [dotnet/diagnostics](https://github.com/dotnet/diagnostics) repository against CoreCLR builds from dotnet/runtime. It currently:

1. **Rebuilds CoreCLR and libraries from scratch** — the `AllSubsets_CoreCLR` job in `runtime-diagnostics.yml` runs `./build.sh clr+libs`, duplicating the same build that the runtime pipeline already performs.
2. **Uploads a shared framework artifact** — extracts the shared framework binaries (coreclr, cDAC reader, System.Private.CoreLib, etc.) from the testhost output.
3. **Runs two test jobs** — one with the cDAC enabled (`useCdac: true`) and one with the legacy DAC (`useCdac: false`), both using the `runtime-diag-job.yml` template.

This architecture means every PR that touches CoreCLR paths triggers two independent full builds of the same configuration, wasting CI resources and wall-clock time.

## Problem

### Redundant builds

The runtime pipeline already builds CoreCLR + libraries for multiple platforms. The `runtime-diagnostics` pipeline rebuilds the same `windows_x64` configuration independently. This doubles the build cost for that configuration on every qualifying PR.

### Cross-pipeline artifact coordination is fragile

We evaluated several approaches to share artifacts between the two pipelines instead of rebuilding:

| Approach | Mechanism | Drawback |
|----------|-----------|----------|
| `resources: pipelines:` | AzDO first-class trigger — downstream pipeline runs when upstream completes | Creates a separate pipeline run, not a PR check. Cannot gate the PR on the downstream result. |
| `DownloadBuildArtifacts` with polling | Query AzDO REST API for the upstream build by `sourceBranch` or `sourceVersion`, wait for completion, download artifacts | Fragile — requires polling, build ID correlation, and careful filtering to avoid picking up artifacts from a different PR. |
| `download-specific-artifact-step.yml` | Existing template in `eng/pipelines/common/` for cross-build downloads | Still requires a known build ID, which means polling or trigger-based coordination. |

All cross-pipeline approaches add complexity without reliability guarantees. The polling approaches are particularly brittle because:

- The AzDO Builds API filters by pipeline run, not individual jobs — you cannot wait for a specific job to complete.
- The Timeline API can query individual job status, but depends on internal job naming conventions that may change.
- Race conditions arise when multiple PRs are active simultaneously.

### Cross-platform artifact format mismatch

A related issue discovered during CI triage of [PR #124564](https://github.com/dotnet/runtime/pull/124564): the `$(archiveExtension)` variable resolves based on the *downloading* platform, not the *uploading* platform. When downloading Windows-produced `.zip` artifacts on Linux (or vice versa), the `ExtractFiles` task fails because the glob matches nothing. Additionally, Linux AzDO agents do not have `unzip` installed, so even with corrected filenames, `.zip` extraction fails on Linux.

## Design

### Approach: single-pipeline consolidation

Instead of coordinating across pipelines, add the diagnostics test jobs directly to the runtime pipeline as additional jobs in the Build stage. They depend on the existing `CoreCLR_Libraries` build job and download only the shared framework artifact — no duplicate build required.

```
┌─────────────────────────────────────────────────┐
│                 runtime.yml                      │
│                                                  │
│  ┌──────────────────────────────────────┐        │
│  │  CoreCLR_Libraries (windows_x64)     │        │
│  │  - Builds clr+libs                   │        │
│  │  - Uploads build artifacts           │        │
│  │  - Uploads DiagnosticsRuntime_*  ◄── NEW      │
│  └──────────┬───────────────────────────┘        │
│             │ dependsOn                          │
│    ┌────────┴────────┐                           │
│    ▼                 ▼                           │
│  ┌──────────┐  ┌──────────┐                      │
│  │ cDAC     │  │ DAC      │  ◄── NEW             │
│  │ SOS tests│  │ SOS tests│                      │
│  └──────────┘  └──────────┘                      │
│  (non-blocking: shouldContinueOnError: true)     │
└─────────────────────────────────────────────────┘
```

### Changes to `runtime.yml`

#### 1. Diagnostics repository resource

```yaml
parameters:
- name: diagnosticsBranch
  displayName: Diagnostics Branch
  type: string
  default: main

resources:
  repositories:
    - repository: diagnostics
      type: github
      endpoint: public
      name: dotnet/diagnostics
      ref: ${{ parameters.diagnosticsBranch }}
```

The `diagnosticsBranch` parameter allows testing against a specific diagnostics branch when needed (e.g., for coordinated changes across both repos).

#### 2. Shared framework artifact upload

A new step in the `CoreCLR_Libraries` job's `postBuildSteps` uploads just the shared framework directory:

```yaml
- powershell: |
    $versionDir = Get-ChildItem -Directory -Path "$(Build.SourcesDirectory)/artifacts/bin/testhost/net*/shared/Microsoft.NETCore.App" | Select-Object -First 1 -ExpandProperty FullName
    Write-Host "##vso[task.setvariable variable=versionDir]$versionDir"
  displayName: 'Set Path to Shared Framework Artifacts'
- template: /eng/pipelines/common/upload-artifact-step.yml
  parameters:
    rootFolder: $(versionDir)
    includeRootFolder: false
    artifactName: DiagnosticsRuntime_$(osGroup)$(osSubgroup)_$(archType)_$(_BuildConfig)
    displayName: Diagnostics Runtime
```

This artifact contains: `coreclr.dll`, `cdacreader.dll`, `System.Private.CoreLib.dll`, and other shared framework binaries needed by the diagnostics test runner.

#### 3. Diagnostics test jobs

Two jobs are added at the end of the Build stage, one for cDAC and one for DAC:

- **Template**: `eng/pipelines/diagnostics/runtime-diag-job.yml` (unchanged)
- **Dependency**: `build_windows_x64_{config}_CoreCLR_Libraries`
- **Non-blocking**: `shouldContinueOnError: true` — failures show as warnings but do not fail the pipeline check
- **Path conditions**: Same as `CoreCLR_Libraries` — only runs when CoreCLR or library paths change

The `runtime-diag-job.yml` template handles checking out the diagnostics repo, building its test infrastructure, and running the SOS test suite with `-liveRuntimeDir` pointing to the downloaded shared framework.

### Non-blocking behavior

The diagnostics jobs use `shouldContinueOnError: true`, which the `runtime-diag-job.yml` template propagates to the underlying 1ES job. This means:

- **On failure**: The job shows as orange/warning in the pipeline UI. The overall pipeline check on the PR remains green (assuming other jobs pass).
- **On success**: The job shows as green, same as any other passing job.

This is appropriate because the diagnostics tests validate SOS tooling compatibility, not the correctness of the runtime itself. A diagnostics test failure should be investigated but should not block runtime PRs.

### What about `runtime-diagnostics.yml`?

Once this consolidation is validated, the standalone `runtime-diagnostics.yml` pipeline can be retired. Until then, both can coexist — they test the same thing but the consolidated version avoids the redundant build.

## Future considerations

### Additional platforms

The current implementation only runs diagnostics tests on `windows_x64`. The `runtime-diag-job.yml` template supports Linux as well. Adding `linux_x64` would require:

- Ensuring the `CoreCLR_Libraries` linux_x64 job also uploads a `DiagnosticsRuntime_*` artifact
- Adding a second platform entry to the diagnostics jobs
- Using `.tar.gz` for cross-platform artifact downloads (Linux agents lack `unzip`)

### cDAC dump tests

[PR #124564](https://github.com/dotnet/runtime/pull/124564) introduces a separate "golden dump" testing strategy where debuggee programs are crash-dumped and then validated with CLRMD-based cDAC contract tests. These dump tests are complementary to the SOS tests described here and could follow the same consolidation pattern — running as additional non-blocking jobs in the runtime pipeline rather than in a separate pipeline.

### Making diagnostics tests blocking

If diagnostics test stability improves to the point where failures reliably indicate real regressions, `shouldContinueOnError` can be set back to `false` to make them blocking. This is a policy decision rather than a technical one.
