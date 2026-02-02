---
name: azdo-helix-failures
description: Retrieve and analyze test failures from Azure DevOps builds and Helix test runs. Use this when investigating CI failures, checking why a PR's tests are failing, or debugging Helix test issues.
---

# Azure DevOps and Helix Failure Analysis

When you need to investigate CI test failures in Azure DevOps or Helix, use this skill to retrieve and analyze the failure information.

## Overview

The dotnet/runtime repository uses Azure DevOps for CI builds and Helix for distributed test execution. When tests fail:

1. **Azure DevOps** orchestrates the build and sends test workloads to Helix
2. **Helix** runs the tests on various platforms and reports results back
3. Failure information is spread across AzDO build logs and Helix console logs

This skill provides tools to quickly retrieve this information.

## Using the Script

### Get Failures by Build ID

If you know the Azure DevOps build ID:

```powershell
.\.github\skills\azdo-helix-failures\Get-HelixFailures.ps1 -BuildId 1276327
```

### Get Failures by PR Number

To find and analyze failures for a specific PR:

```powershell
.\.github\skills\azdo-helix-failures\Get-HelixFailures.ps1 -PRNumber 123445
```

### Show Detailed Helix Logs

To fetch and display the actual Helix console logs with test failure details:

```powershell
.\.github\skills\azdo-helix-failures\Get-HelixFailures.ps1 -BuildId 1276327 -ShowLogs
```

### Query a Specific Helix Work Item

To directly analyze a specific Helix job and work item:

```powershell
.\.github\skills\azdo-helix-failures\Get-HelixFailures.ps1 -HelixJob "4b24b2c2-ad5a-4c46-8a84-844be03b1d51" -WorkItem "iOS.Device.Aot.Test"
```

### Use with Other Repositories

The script works with any GitHub repository that uses Azure DevOps/Helix:

```powershell
.\.github\skills\azdo-helix-failures\Get-HelixFailures.ps1 -PRNumber 12345 -Repository "dotnet/aspnetcore"
```

### Control Output Volume

```powershell
# See more failed jobs (default: 5)
.\.github\skills\azdo-helix-failures\Get-HelixFailures.ps1 -BuildId 1276327 -MaxJobs 10

# Show more lines of stack trace per failure (default: 50)
.\.github\skills\azdo-helix-failures\Get-HelixFailures.ps1 -BuildId 1276327 -ShowLogs -MaxFailureLines 100

# Show context lines before errors
.\.github\skills\azdo-helix-failures\Get-HelixFailures.ps1 -BuildId 1276327 -ContextLines 3

# Increase API timeout for slow connections (default: 30 seconds)
.\.github\skills\azdo-helix-failures\Get-HelixFailures.ps1 -BuildId 1276327 -TimeoutSec 60

# Enable verbose output for debugging
.\.github\skills\azdo-helix-failures\Get-HelixFailures.ps1 -BuildId 1276327 -Verbose
```

### Prerequisites

- **PowerShell 5.1+** or **PowerShell Core 7+**
- **GitHub CLI (`gh`)**: Required only for `-PRNumber` parameter. Install from https://cli.github.com/

## Failure Classification

The script automatically classifies failures and suggests actions:

| Pattern | Type | Transient? | Suggested Action |
|---------|------|------------|------------------|
| `.pcm: No such file` | Infrastructure | No | Apply StripSymbols=false workaround |
| `Size of the executable` | Size Regression | No | Investigate size increase |
| `Unable to find package` | Infrastructure | Yes | Wait for upstream build |
| `DEVICE_NOT_FOUND` | Infrastructure | Yes | Retry - device issue |
| `timed out` | Infrastructure | Yes | Retry or increase timeout |
| `error CS####` | Build | No | Fix compilation error |
| `error MSB####` | Build | No | Check build configuration |
| `OutOfMemoryException` | Infrastructure | Yes | Retry - memory pressure |
| `Assert.Equal() Failure` | Test | No | Fix test or code |
| `Unable to pull image` | Infrastructure | Yes | Retry - container registry issue |
| `Connection refused` | Infrastructure | Yes | Retry - network issue |

## Build Definition IDs

Key Azure DevOps build definitions for dotnet/runtime:

| Definition ID | Name | Description |
|---------------|------|-------------|
| `129` | runtime | Main PR validation build |
| `133` | runtime-dev-innerloop | Fast innerloop validation |
| `139` | dotnet-linker-tests | ILLinker/trimming tests |

## Azure DevOps Organizations

The script defaults to the public Azure DevOps organization:
- **Organization**: `dnceng-public`
- **Project**: `cbb18261-c48f-4abb-8651-8cdcb5474649` (public)

For internal/private builds:
- **Organization**: `dnceng` (internal)
- **Project GUID**: Varies by pipeline

Override with:
```powershell
.\.github\skills\azdo-helix-failures\Get-HelixFailures.ps1 -BuildId 1276327 -Organization "dnceng" -Project "internal-project-guid"
```

## Manual Investigation

If the script doesn't provide enough information, you can manually investigate:

### Step 1: Get the Build Timeline

```powershell
$buildId = 1276327
$response = Invoke-RestMethod -Uri "https://dev.azure.com/dnceng-public/cbb18261-c48f-4abb-8651-8cdcb5474649/_apis/build/builds/$buildId/timeline?api-version=7.0"
$failedJobs = $response.records | Where-Object { $_.type -eq "Job" -and $_.result -eq "failed" }
$failedJobs | Select-Object id, name, result | Format-Table
```

### Step 2: Find Helix Tasks in Failed Jobs

```powershell
$jobId = "90274d9a-fbd8-54f8-6a7d-8dfc4e2f6f3f"  # From step 1
$helixTasks = $response.records | Where-Object { $_.parentId -eq $jobId -and $_.name -like "*Helix*" }
$helixTasks | Select-Object id, name, result, log | Format-Table
```

### Step 3: Get the Build Log

```powershell
$logId = 565  # From task.log.id
$logContent = Invoke-RestMethod -Uri "https://dev.azure.com/dnceng-public/cbb18261-c48f-4abb-8651-8cdcb5474649/_apis/build/builds/$buildId/logs/${logId}?api-version=7.0"
$logContent | Select-String -Pattern "error|FAIL" -Context 2,5
```

### Step 4: Query Helix APIs Directly

```bash
# Get job details
curl -s "https://helix.dot.net/api/2019-06-17/jobs/JOB_ID"

# List work items in a job
curl -s "https://helix.dot.net/api/2019-06-17/jobs/JOB_ID/workitems"

# Get specific work item details
curl -s "https://helix.dot.net/api/2019-06-17/jobs/JOB_ID/workitems/WORK_ITEM_NAME"

# Get console log
curl -s "https://helix.dot.net/api/2019-06-17/jobs/JOB_ID/workitems/WORK_ITEM_NAME/console"
```

### Step 5: Download Artifacts

Work item artifacts are available in the `Files` array from the work item details:

```powershell
$workItem = Invoke-RestMethod -Uri "https://helix.dot.net/api/2019-06-17/jobs/$jobId/workitems/$workItemName"
$workItem.Files | ForEach-Object { Write-Host "$($_.Name): $($_.Uri)" }
```

Common artifacts include:
- `console.*.log` - Console output
- `*.binlog` - MSBuild binary logs (for AOT/build failures)
- `run-*.log` - XHarness/test runner logs
- Core dumps and crash reports (when available)

### Extracting Environment Variables

The console log shows all `DOTNET_*` variables that affect test behavior:

```bash
curl -s "https://helix.dot.net/api/2019-06-17/jobs/JOB_ID/workitems/WORK_ITEM_NAME/console" | grep "DOTNET_"
```

Example output:
```
DOTNET_JitStress=1
DOTNET_TieredCompilation=0
DOTNET_GCStress=0xC
```

These are critical for reproducing the failure locally.

## Useful Links

- [Azure DevOps Build](https://dev.azure.com/dnceng-public/public/_build?definitionId=129): Main runtime build definition
- [Helix Portal](https://helix.dot.net/): View Helix jobs and work items
- [Helix API Documentation](https://helix.dot.net/swagger/): Swagger docs for Helix REST API
- [Build Analysis](https://github.com/dotnet/arcade/blob/main/Documentation/Projects/Build%20Analysis/LandingPage.md): Known issues tracking
- [Triaging Failures Guide](https://github.com/dotnet/runtime/blob/main/docs/workflow/ci/triaging-failures.md): Official triage documentation
- [Area Owners](https://github.com/dotnet/runtime/blob/main/docs/area-owners.md): Find the right person to ask

## Tips

1. **Check if it's a known flaky test**: Search for existing issues with the test name
2. **Compare with main branch**: Check if the same test is failing on main
3. **Look at the specific platform**: Failures may be platform-specific (Mono, NativeAOT, WASM, etc.)
4. **Check for `[ActiveIssue]` attributes**: Tests may need to be skipped for known issues
5. **Look for transient flags**: The script marks failures that are likely to pass on retry
