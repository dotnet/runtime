# Helix Work Item Artifacts

Guide to finding and analyzing artifacts from Helix test runs.

## Accessing Artifacts

### Via the Script

Query a specific work item to see its artifacts:

```powershell
./scripts/Get-HelixFailures.ps1 -HelixJob "4b24b2c2-..." -WorkItem "Microsoft.NET.Sdk.Tests.dll.1" -ShowLogs
```

### Via API

```bash
# Get work item details including Files array
curl -s "https://helix.dot.net/api/2019-06-17/jobs/{jobId}/workitems/{workItemName}"
```

The `Files` array contains artifacts with `FileName` and `Uri` properties.

## Artifact Availability Varies

**Not all test types produce the same artifacts.** What you see depends on the repo, test type, and configuration:

- **Build/publish tests** (SDK, WASM) → Multiple binlogs
- **AOT compilation tests** (iOS/Android) → `AOTBuild.binlog` plus device logs
- **Standard unit tests** → Console logs only, no binlogs
- **Crash failures** (exit code 134) → Core dumps may be present

Always query the specific work item to see what's available rather than assuming a fixed structure.

## Common Artifact Patterns

| File Pattern | Purpose | When Useful |
|--------------|---------|-------------|
| `*.binlog` | MSBuild binary logs | AOT/build failures, MSB4018 errors |
| `console.*.log` | Console output | Always available, general output |
| `run-*.log` | XHarness execution logs | Mobile test failures |
| `device-*.log` | Device-specific logs | iOS/Android device issues |
| `dotnetTestLog.*.log` | dotnet test output | Test framework issues |
| `vstest.*.log` | VSTest output | aspnetcore/SDK test issues |
| `core.*`, `*.dmp` | Core dumps | Crashes, hangs |
| `testResults.xml` | Test results | Detailed pass/fail info |

Artifacts may be at the root level or nested in subdirectories like `xharness-output/logs/`.

## Binlog Files

Binlogs are **only present for tests that invoke MSBuild** (build/publish tests, AOT compilation). Standard unit tests don't produce binlogs.

### Common Names

| File | Description |
|------|-------------|
| `build.msbuild.binlog` | Build phase |
| `publish.msbuild.binlog` | Publish phase |
| `AOTBuild.binlog` | AOT compilation |
| `msbuild.binlog` | General MSBuild operations |
| `msbuild0.binlog`, `msbuild1.binlog` | Per-test-run logs (numbered) |

### Analyzing Binlogs

**Online viewer (no download):**
1. Copy the binlog URI from the script output
2. Go to https://live.msbuildlog.com/
3. Paste the URL to load and analyze

**Download and view locally:**
```bash
curl -o build.binlog "https://helix.dot.net/api/jobs/{jobId}/workitems/{workItem}/files/build.msbuild.binlog?api-version=2019-06-17"
# Open with MSBuild Structured Log Viewer
```

**AI-assisted analysis:**
Use the MSBuild MCP server to analyze binlogs for errors and warnings.

## Core Dumps

Core dumps appear when tests crash (typically exit code 134 on Linux/macOS):

```
core.1000.34   # Format: core.{uid}.{pid}
```

## Mobile Test Artifacts (iOS/Android)

Mobile device tests typically include XHarness orchestration logs:

- `run-ios-device.log` / `run-android.log` - Execution log
- `device-{machine}-*.log` - Device output
- `list-ios-device-*.log` - Device discovery
- `AOTBuild.binlog` - AOT compilation (when applicable)
- `*.crash` - iOS crash reports

## Finding the Right Work Item

1. Run the script with `-ShowLogs` to see Helix job/work item info
2. Look for lines like:
   ```
   Helix Job: 4b24b2c2-ad5a-4c46-8a84-844be03b1d51
   Work Item: Microsoft.NET.Sdk.Tests.dll.1
   ```
3. Query that specific work item for full artifact list

## Artifact Retention

Helix artifacts are retained for a limited time (typically 30 days). Download important artifacts promptly if needed for long-term analysis.
