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

# Set custom cache lifetime (default: 30 seconds)
.\.github\skills\azdo-helix-failures\Get-HelixFailures.ps1 -BuildId 1276327 -CacheTTLSeconds 60
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

## Build Analysis Integration

When analyzing a PR, the script automatically checks the "Build Analysis" PR check for known issues that have already been identified. This saves time by surfacing known transient failures immediately.

Example output:
```
Build Analysis found 1 known issue(s):
  - #117164: Unable to pull image from mcr.microsoft.com
    https://github.com/dotnet/runtime/issues/117164
```

The Build Analysis check is maintained by the dotnet/arcade infrastructure team and automatically matches failures against issues with the "Known Build Error" label.

## Multiple Build Analysis

PRs often trigger multiple pipelines that can fail independently:

| Pipeline | Description |
|----------|-------------|
| `runtime` | Main PR validation build |
| `runtime-dev-innerloop` | Fast innerloop validation |
| `dotnet-linker-tests` | ILLinker/trimming tests |
| `runtime-wasm-perf` | WASM performance tests |
| `runtime-libraries enterprise-linux` | Enterprise Linux compatibility |

The script analyzes **all failing builds** for a PR and provides:
- Per-build summaries with failed job counts
- Overall summary with totals across all builds
- Known issues from Build Analysis shown once at the start

Example with multiple builds:
```
Finding builds for PR #123909 in dotnet/runtime...
Found 3 failing builds:
  - Build 1276778 (runtime)
  - Build 1276779 (runtime-dev-innerloop)
  - Build 1276780 (dotnet-linker-tests)

=== Azure DevOps Build 1276778 ===
URL: https://dev.azure.com/dnceng-public/cbb18261-c48f-4abb-8651-8cdcb5474649/_build/results?buildId=1276778
Status: completed (failed)
...

=== Overall Summary ===
Analyzed 3 builds
Total failed jobs: 13
Total local test failures: 3

Known Issues (from Build Analysis):
  - #117164: Unable to pull image from mcr.microsoft.com
    https://github.com/dotnet/runtime/issues/117164
```

## PR Change Correlation

The script automatically correlates failures with files changed in the PR to help identify PR-related issues:

```
=== PR Change Correlation ===
⚠️  Test files changed by this PR are failing:
    src/libraries/System.Net.Http/tests/FunctionalTests/NtAuthTests.FakeServer.cs

These failures are likely PR-related.
```

This feature:
- Fetches the list of files changed in the PR
- Compares file names against failure messages and build errors
- Highlights test files and source files that appear in failures
- Skips correlation for large PRs (>100 files) to avoid performance issues

## Test Execution Types

The script detects and handles different test execution types:

### Helix Tests
Tests run on the Helix distributed test infrastructure. The script extracts Helix console log URLs and can fetch detailed failure information with `-ShowLogs`.

Example output for Helix tests:
```
Found 2 failed job(s):

--- browser-wasm linux Release WasmBuildTests ---
  Build: https://dev.azure.com/dnceng-public/cbb18261-c48f-4abb-8651-8cdcb5474649/_build/results?buildId=1276507&view=logs&j=1fa93050-f528-55d3-a351-f8bf9ce5adbf
  Fetching Helix task log...
  Failed tests:
    - System.Net.Http.Functional.Tests.NtAuthTests.Http2_FakeServer_SessionAuthChallenge

  Helix logs available (use -ShowLogs to fetch):
    https://helix.dot.net/api/2019-06-17/jobs/216ee994-0f0f-4568-949c-c0fa97892e89/workitems/Workloads-ST-Wasm.Build.Tests/console
```

### Local Tests (Non-Helix)
Some repositories (e.g., dotnet/sdk) run tests directly on the build agent. The script:
- Detects local test failures from Azure DevOps issues
- Extracts Azure DevOps Test Run URLs for detailed results
- Provides links to Test Management for viewing individual test failures

Example output for local tests:
```
=== Local Test Failures (non-Helix) ===
Build: https://dev.azure.com/dnceng-public/cbb18261-c48f-4abb-8651-8cdcb5474649/_build/results?buildId=1276327

--- Run TemplateEngine Tests ---
  Log: https://dev.azure.com/dnceng-public/cbb18261-c48f-4abb-8651-8cdcb5474649/_build/results?buildId=1276327&view=logs&j=...
  XUnit(5,2): error : Tests failed: dotnet-new.IntegrationTests_net10.0_x64.html

  Test Results:
    Run 35626548: https://dev.azure.com/dnceng-public/public/_TestManagement/Runs?runId=35626548
```

## Known Issue Search

The script automatically searches for known issues when failures are detected. It uses the `Known Build Error` label which is applied by Build Analysis across dotnet repositories.

### How It Works

1. When a test failure is detected, the script extracts the test name from the `[FAIL]` line in the log
2. It searches GitHub for open issues with the `Known Build Error` label matching the test name
3. If found, it displays links to the relevant issues

### Example Output

```
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

## MihuBot Semantic Search

The script can optionally use [MihuBot](https://github.com/MihaZupan/MihuBot)'s semantic search to find related issues and discussions in dotnet repositories. This provides broader context beyond just "Known Build Error" labeled issues.

### Enabling MihuBot Search

```powershell
.\.github\skills\azdo-helix-failures\Get-HelixFailures.ps1 -BuildId 1276327 -SearchMihuBot
```

### How It Works

1. When a test failure is detected, the script extracts test names and error patterns
2. It calls MihuBot's MCP endpoint to perform a semantic search across dotnet repositories
3. Results include open and closed issues/PRs that may be related to the failure
4. Results are deduplicated against the Known Build Error search to avoid duplicates

### Example Output

```
  Known Issues:
    #103584: Failing test due to no detected IO events in 'FileSystemWatcherTest.ExecuteAndVerifyEvents'
    https://github.com/dotnet/runtime/issues/103584

  Related Issues (MihuBot):
    #98234: FileSystemWatcher intermittent failures on Linux [closed]
    https://github.com/dotnet/runtime/issues/98234
    #101456: Improve FileSystemWatcher reliability [open]
    https://github.com/dotnet/runtime/issues/101456
```

### Benefits

- **Semantic search**: Finds conceptually related issues, not just exact text matches
- **Cross-repository**: Searches across all dotnet repositories
- **Historical context**: Includes closed issues/PRs to show how similar problems were resolved
- **Discussion context**: Can include issue and PR comments for deeper understanding

### Requirements

- Internet access to reach `https://mihubot.xyz/mcp`
- No authentication required

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

3. **Check Build Analysis results**
   - The script automatically fetches known issues from the Build Analysis PR check
   - Known issues indicate transient/infrastructure failures that are safe to retry

4. **Correlate failures with PR changes**
   - Are failures in areas the PR modifies? (e.g., new tests added by PR are failing)
   - Is this a codeflow/dependency update that could bring in breaking changes?
   - Check if the same failures appear on main branch

5. **Interpret failure patterns**
   - Same error across many jobs → Likely a real code issue
   - iOS/Android device failures → Often transient infrastructure issues
   - Docker image pull failures → Infrastructure, check known issues
   - New test files failing → PR-related, tests need fixing

6. **Determine actionability**
   - **Real bug**: Needs fix in PR (compilation errors, new test failures)
   - **Infrastructure**: May be transient, verify before retrying
   - **Pre-existing**: Unrelated to PR, may need separate issue
