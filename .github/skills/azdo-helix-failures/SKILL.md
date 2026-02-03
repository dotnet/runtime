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
3. **Local tests** run directly on the build agent (some repos like dotnet/sdk)
4. Failure information is spread across AzDO build logs, Helix console logs, and Test Management

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
.\.github\skills\azdo-helix-failures\Get-HelixFailures.ps1 -BuildId 1276276 -Repository "dotnet/sdk"
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

### Caching

The script caches API responses to speed up repeated analysis. Cache files are stored in the system temp directory.

```powershell
# Force fresh data (bypass cache)
.\.github\skills\azdo-helix-failures\Get-HelixFailures.ps1 -BuildId 1276327 -NoCache

# Clear all cached files
.\.github\skills\azdo-helix-failures\Get-HelixFailures.ps1 -ClearCache

# Set custom cache lifetime (default: 60 minutes)
.\.github\skills\azdo-helix-failures\Get-HelixFailures.ps1 -BuildId 1276327 -CacheTTLMinutes 30
```

**Note:** In-progress build status and timelines are not cached, ensuring you always see current failure state.

### Error Handling

```powershell
# Continue processing if some API calls fail (show partial results)
.\.github\skills\azdo-helix-failures\Get-HelixFailures.ps1 -BuildId 1276327 -ContinueOnError
```

### Prerequisites

- **PowerShell 5.1+** or **PowerShell Core 7+**
- **GitHub CLI (`gh`)**: Required only for `-PRNumber` parameter. Install from https://cli.github.com/

## Test Execution Types

The script detects and handles different test execution types:

### Helix Tests
Tests run on the Helix distributed test infrastructure. The script extracts Helix console log URLs and can fetch detailed failure information with `-ShowLogs`.

### Local Tests (Non-Helix)
Some repositories (e.g., dotnet/sdk) run tests directly on the build agent. The script:
- Detects local test failures from Azure DevOps issues
- Extracts Azure DevOps Test Run URLs for detailed results
- Provides links to Test Management for viewing individual test failures

Example output for local tests:
```
=== Local Test Failures (non-Helix) ===

--- Run TemplateEngine Tests ---
  XUnit(0,0): error : Tests failed: dotnet-new.IntegrationTests_net10.0_x64.html

  Test Results:
    Run 35626548: https://dev.azure.com/dnceng-public/public/_TestManagement/Runs?runId=35626548
    Run 35626550: https://dev.azure.com/dnceng-public/public/_TestManagement/Runs?runId=35626550

  Classification: [Test] Local xUnit test failure
  Suggested action: Check test run URL for specific failed test details
```

## Failure Classification

The script automatically classifies failures and suggests actions:

| Pattern | Type | May Be Transient | Suggested Action |
|---------|------|------------------|------------------|
| `.pcm: No such file` | Infrastructure | No | Apply StripSymbols=false workaround |
| `Size of the executable` | Size Regression | No | Investigate size increase |
| `Unable to find package` | Infrastructure | Yes | Check if package exists in feeds |
| `DEVICE_NOT_FOUND` | Infrastructure | Yes | Check if leg passes on main branch |
| `timed out` | Infrastructure | Yes | Check if test is slow or hanging |
| `error CS####` | Build | No | Fix compilation error |
| `error MSB####` | Build | No | Check build configuration |
| `OutOfMemoryException` | Infrastructure | Yes | Check for memory leaks in test |
| `Assert.Equal() Failure` | Test | No | Fix test or code |
| `Unable to pull image` | Infrastructure | Yes | Check container registry availability |
| `Connection refused` | Infrastructure | Yes | Check if passes on main branch |
| `XUnit...Tests failed` | Test | No | Check test run URL for details |
| `[FAIL]` | Test | No | Helix test failure - check console log |

**Note:** "May Be Transient" means this failure pattern *can* be caused by transient infrastructure issues, but is not guaranteed to be. Always verify by checking if the same test passes on the main branch or in recent CI runs before assuming a retry will help.

## Known Issue Search

The script automatically searches for known issues when failures are detected. It uses the `Known Build Error` label which is applied by Build Analysis across dotnet repositories.

### How It Works

1. When a test failure is detected, the script extracts the test name from the `[FAIL]` line in the log
2. It searches GitHub for open issues with the `Known Build Error` label matching the test name
3. If found, it displays links to the relevant issues

### Example Output

```
  Classification: [Test] Helix test failure
  Suggested action: Check console log for failure details
  Known Issues:
    #103584: Failing test due to no detected IO events in 'FileSystemWatcherTest.ExecuteAndVerifyEvents'
    https://github.com/dotnet/runtime/issues/103584
```

### Requirements

- **GitHub CLI (`gh`)**: Required for searching known issues. Install from https://cli.github.com/

### Manual Search

If you need to search manually, use GitHub's search:
```
repo:dotnet/runtime is:issue is:open label:"Known Build Error" FileSystemWatcher
```

Or use the GitHub CLI:
```bash
gh issue list --repo dotnet/runtime --label "Known Build Error" --state open --search "FileSystemWatcher"
```

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

1. **Read the PR description and comments first**: The PR may be a validation build for another repo, a codeflow PR, or have known issues discussed in comments. This context is essential for accurate analysis.
2. **Check if it's a known flaky test**: Search for existing issues with the test name
3. **Compare with main branch**: Check if the same test is failing on main before assuming it's transient
4. **Look at the specific platform**: Failures may be platform-specific (Mono, NativeAOT, WASM, etc.)
5. **Check for `[ActiveIssue]` attributes**: Tests may need to be skipped for known issues
6. **Don't assume transient**: Even failures marked "may be transient" should be verified - check recent CI history
7. **Check Build Analysis**: Look for issues with the "Known Build Error" label before retrying

## Analysis Workflow

When analyzing CI failures, follow this workflow for best results:

1. **Get PR context first**
   - Read the PR title and description
   - Check if it's a validation build for another PR (common in dotnet/dotnet VMR)
   - Read any comments discussing build issues

2. **Run the failure analysis script**
   ```powershell
   .\.github\skills\azdo-helix-failures\Get-HelixFailures.ps1 -PRNumber 12345 -Repository owner/repo
   ```

3. **Interpret results with context**
   - Are failures in areas the PR modifies?
   - Is this a codeflow/dependency update that could bring in breaking changes?
   - Check if the same failures appear on main branch

4. **Check for known issues**
   - The script searches for issues with "Known Build Error" label
   - Also search manually if the test name is generic

5. **Determine actionability**
   - Real bug: Needs fix in PR
   - Infrastructure: May be transient, verify before retrying
   - Pre-existing: Unrelated to PR, may need separate issue
