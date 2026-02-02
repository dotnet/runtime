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

### Control Output Volume

To see more or fewer failed jobs:

```powershell
.\.github\skills\azdo-helix-failures\Get-HelixFailures.ps1 -BuildId 1276327 -MaxJobs 10
```

### Additional Options

```powershell
# Show more lines of stack trace per failure (default: 50)
.\.github\skills\azdo-helix-failures\Get-HelixFailures.ps1 -BuildId 1276327 -ShowLogs -MaxFailureLines 100

# Increase API timeout for slow connections (default: 30 seconds)
.\.github\skills\azdo-helix-failures\Get-HelixFailures.ps1 -BuildId 1276327 -TimeoutSec 60

# Enable verbose output for debugging
.\.github\skills\azdo-helix-failures\Get-HelixFailures.ps1 -BuildId 1276327 -Verbose
```

### Prerequisites

- **PowerShell 5.1+** or **PowerShell Core 7+**
- **GitHub CLI (`gh`)**: Required only for `-PRNumber` parameter. Install from https://cli.github.com/

## Azure DevOps Organizations

The script defaults to the public Azure DevOps organization:
- **Organization**: `dnceng-public`
- **Project**: `cbb18261-c48f-4abb-8651-8cdcb5474649` (public)

For internal/private builds, you may need different values:
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
$logContent = Invoke-RestMethod -Uri "https://dev.azure.com/dnceng-public/cbb18261-c48f-4abb-8651-8cdcb5474649/_apis/build/builds/$buildId/logs/$logId?api-version=7.0"
$logContent | Select-String -Pattern "error|FAIL" -Context 2,5
```

### Step 4: Get Helix Console Log

Extract the Helix URL from the build log and fetch it:

```powershell
$helixUrl = "https://helix.dot.net/api/2019-06-17/jobs/046b3afd-b2d7-4297-8f79-618b9c038ee1/workitems/System.Reflection.Tests/console"
$helixLog = Invoke-RestMethod -Uri $helixUrl
$helixLog | Select-String -Pattern "FAIL|Exception|Error" -Context 3,5
```

## Common Failure Patterns

### Test Assertion Failure

Look for patterns like:
```
[FAIL]
Assert.Equal() Failure: Values differ
Expected: ...
Actual:   ...
Stack Trace:
```

### Build/Compilation Error

Look for MSBuild errors:
```
error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
error MSB3073: The command ... exited with code 1
```

### Helix Infrastructure Issues

Look for:
```
Helix work item timed out
Failed to download correlation payload
```

## Useful Links

- [Azure DevOps Build](https://dev.azure.com/dnceng-public/public/_build?definitionId=129): Main runtime build definition
- [Helix Portal](https://helix.dot.net/): View Helix jobs and work items
- [Build Analysis](https://github.com/dotnet/arcade/blob/main/Documentation/Projects/Build%20Analysis/LandingPage.md): Known issues tracking

## Tips

1. **Check if it's a known flaky test**: Search for existing issues with the test name
2. **Compare with main branch**: Check if the same test is failing on main
3. **Look at the specific platform**: Failures may be platform-specific (Mono, NativeAOT, WASM, etc.)
4. **Check for `[ActiveIssue]` attributes**: Tests may need to be skipped for known issues
