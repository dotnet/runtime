---
name: triaging-helix-failures
description: Analyze Helix test work item failures for dotnet/runtime PRs. Use this when given a PR number or Helix job/work item to investigate test failures and extract diagnostic information.
---

# Helix Failure Analysis for dotnet/runtime

This skill helps you investigate build and test failures in dotnet/runtime CI by querying Azure DevOps and Helix APIs to extract failure details, console logs, and artifacts.

## When to Use This Skill

Use this skill when asked to:
- Investigate build and test failures for a specific dotnet/runtime PR (if asked about either build failures or test failures, investigate both)
- Analyze a specific Helix work item failure given a job ID and work item name
- Extract console logs and artifacts from a failing Helix job
- Determine why a specific CI build leg failed

## Step 1: Find Azure DevOps Builds for a PR

Given a PR number, query the Azure DevOps API to find associated builds:

```bash
# Get builds for a specific PR (replace PR_NUMBER)
curl -s "https://dev.azure.com/dnceng-public/public/_apis/build/builds?definitions=129&branchName=refs/pull/PR_NUMBER/merge&api-version=7.0"
```

**Key build definition IDs for dotnet/runtime:**
- `129` - runtime (main PR validation)
- `133` - runtime-dev-innerloop
- `139` - dotnet-linker-tests

The response includes:
- `id` - Build ID for further queries
- `status` - "completed", "inProgress", etc.
- `result` - "succeeded", "failed", "partiallySucceeded"
- `_links.web.href` - Direct link to build results

## Step 2: Find Failed Jobs in a Build

Query the build timeline to identify failed jobs:

```bash
# Get timeline for a build (replace BUILD_ID)
curl -s "https://dev.azure.com/dnceng-public/cbb18261-c48f-4abb-8651-8cdcb5474649/_apis/build/builds/BUILD_ID/Timeline?api-version=7.0"
```

Parse the `records` array for entries where:
- `type` = "Job" and `result` = "failed" → Failed job names
- `type` = "Task" and `result` = "failed" → Failed tasks within jobs

For "Send to Helix" tasks that failed, the log URL is in `log.url`.

## Step 3: Extract Helix Job Information from Logs

Fetch the Azure DevOps task log and search for Helix URLs:

```bash
# Get task log (replace LOG_URL from timeline)
curl -s "LOG_URL" | grep -E "(HelixJobId|https://helix)"
```

This reveals:
- **Helix Job ID**: GUID like `4b24b2c2-ad5a-4c46-8a84-844be03b1d51`
- **Work items URL**: `https://helix.dot.net/api/jobs/JOB_ID/workitems`
- **Failure log URLs**: Direct links to failing work item console logs

## Step 4: Query Helix APIs

### Get Job Details

```bash
curl -s "https://helix.dot.net/api/2019-06-17/jobs/JOB_ID"
```

Returns job metadata including:
- `QueueId` - Helix queue (e.g., "osx.15.amd64.iphone.open")
- `Source` - Source branch/PR reference
- `Properties` - Build configuration details (architecture, configuration, etc.)

### List Work Items in a Job

```bash
curl -s "https://helix.dot.net/api/2019-06-17/jobs/JOB_ID/workitems"
```

### Get Specific Work Item Details

```bash
curl -s "https://helix.dot.net/api/2019-06-17/jobs/JOB_ID/workitems/WORK_ITEM_NAME"
```

Returns:
- `State` - "Passed", "Failed", "Error"
- `ExitCode` - Process exit code
- `MachineName` - Helix agent that ran the work item
- `Duration` - How long the work item ran
- `ConsoleOutputUri` - Direct link to console log blob
- `Files` - Array of uploaded artifacts with URIs
- `Logs` - Additional log files

### Get Work Item Console Log

```bash
curl -s "https://helix.dot.net/api/2019-06-17/jobs/JOB_ID/workitems/WORK_ITEM_NAME/console"
```

This returns the full console output from the test execution. Look for:
- Error messages and stack traces
- `DOTNET_*` environment variables that were set
- Exit codes and failure reasons

## Step 5: Analyze the Failure

### Common Failure Patterns

1. **Infrastructure failures**: Network issues, device connectivity problems
   - Look for: "Can't assign requested address", timeout errors, device not found

2. **Test failures**: Actual test assertions failed
   - Look for: "Assert.Equal failed", stack traces pointing to test code

3. **Build/compilation failures**: Code didn't compile
   - Look for: MSBuild errors, compiler errors in the log

4. **JIT/Runtime crashes**: Runtime crashed during execution
   - Look for: "Assert failure" with `src/coreclr/` paths, SIGSEGV, access violations

### Extracting Environment Variables

The console log shows all `DOTNET_*` variables that affect test behavior:

```
+ printenv | grep DOTNET
DOTNET_JitStress=1
DOTNET_TieredCompilation=0
DOTNET_GCStress=0xC
```

These are critical for reproducing the failure locally.

## Example: Full Workflow

Given: "Investigate failures on PR 123824"

1. **Find builds:**
   ```bash
   curl -s "https://dev.azure.com/dnceng-public/public/_apis/build/builds?definitions=129&branchName=refs/pull/123824/merge&api-version=7.0"
   ```
   → Build ID: 1274344, result: failed

2. **Find failed jobs:**
   ```bash
   curl -s "https://dev.azure.com/dnceng-public/cbb18261-c48f-4abb-8651-8cdcb5474649/_apis/build/builds/1274344/Timeline?api-version=7.0" | python3 -c "import json,sys; [print(r['name']) for r in json.load(sys.stdin)['records'] if r.get('result')=='failed' and r.get('type')=='Job']"
   ```
   → Failed job: "ios-arm64 Release AllSubsets_NativeAOT_Smoke"

3. **Get Helix job from task log:**
   ```bash
   curl -s "https://dev.azure.com/dnceng-public/cbb18261-c48f-4abb-8651-8cdcb5474649/_apis/build/builds/1274344/logs/894" | grep "helix.dot.net"
   ```
   → Helix Job: 4b24b2c2-ad5a-4c46-8a84-844be03b1d51, Work Item: iOS.Device.ExportManagedSymbols.Test

4. **Get work item details:**
   ```bash
   curl -s "https://helix.dot.net/api/2019-06-17/jobs/4b24b2c2-ad5a-4c46-8a84-844be03b1d51/workitems/iOS.Device.ExportManagedSymbols.Test"
   ```
   → State: Failed, ExitCode: 78

5. **Get console log:**
   ```bash
   curl -s "https://helix.dot.net/api/2019-06-17/jobs/4b24b2c2-ad5a-4c46-8a84-844be03b1d51/workitems/iOS.Device.ExportManagedSymbols.Test/console"
   ```
   → Error: "Can't assign requested address (NSPOSIXErrorDomain error 49)"

6. **Assessment:** This is an infrastructure failure - the iOS device connection failed, not a code issue.

## Downloading Artifacts

Work item artifacts can be downloaded directly from the URIs in the `Files` array:

```bash
# Get work item details to find artifact URIs
curl -s "https://helix.dot.net/api/2019-06-17/jobs/JOB_ID/workitems/WORK_ITEM_NAME" | jq '.Files[].Uri'
```

Common artifacts include:
- `console.*.log` - Console output
- `*.binlog` - MSBuild binary logs (for AOT/build failures)
- `run-*.log` - XHarness/test runner logs
- Core dumps and crash reports (when available)

## Additional Resources

- [Triaging Failures Guide](https://github.com/dotnet/runtime/blob/main/docs/workflow/ci/triaging-failures.md)
- [Area Owners](https://github.com/dotnet/runtime/blob/main/docs/area-owners.md)
- [Helix API Documentation](https://helix.dot.net/swagger/)
