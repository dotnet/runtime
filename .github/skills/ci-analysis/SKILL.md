---
name: ci-analysis
description: Analyze CI build and test status from Azure DevOps and Helix for dotnet repository PRs. Use when checking CI status, investigating failures, determining if a PR is ready to merge, or given URLs containing dev.azure.com or helix.dot.net. Also use when asked "why is CI red", "test failures", "retry CI", "rerun tests", or "is CI green".
---

# Azure DevOps and Helix CI Analysis

Analyze CI build status and test failures in Azure DevOps and Helix for dotnet repositories (runtime, sdk, aspnetcore, roslyn, and more).

> 🚨 **NEVER** use `gh pr review --approve` or `--request-changes`. Only `--comment` is allowed. Approval and blocking are human-only actions.

## When to Use This Skill

Use this skill when:
- Checking CI status on a PR ("is CI passing?", "what's the build status?", "why is CI red?")
- Investigating CI failures or checking why a PR's tests are failing
- Determining if a PR is ready to merge based on CI results
- Debugging Helix test issues or analyzing build errors
- Given URLs containing `dev.azure.com`, `helix.dot.net`, or GitHub PR links with failing checks
- Asked questions like "why is this PR failing", "analyze the CI", "is CI green", "retry CI", "rerun tests", or "test failures"
- Investigating canceled or timed-out jobs for recoverable results

## Script Limitations

The `Get-CIStatus.ps1` script targets **Azure DevOps + Helix** infrastructure specifically. It won't help with:
- **GitHub Actions** workflows (different API, different log format)
- Repos not using **Helix** for test distribution (no Helix work items to query)
- Pure **build performance** questions (use MSBuild binlog analysis instead)

However, the analysis patterns in this skill (interpreting failures, correlating with PR changes, distinguishing infrastructure vs. code issues) apply broadly even outside AzDO/Helix.

## Quick Start

```powershell
# Analyze PR failures (most common) - defaults to dotnet/runtime
./scripts/Get-CIStatus.ps1 -PRNumber 123445 -ShowLogs

# Analyze by build ID
./scripts/Get-CIStatus.ps1 -BuildId 1276327 -ShowLogs

# Query specific Helix work item
./scripts/Get-CIStatus.ps1 -HelixJob "4b24b2c2-..." -WorkItem "System.Net.Http.Tests"

# Other dotnet repositories
./scripts/Get-CIStatus.ps1 -PRNumber 12345 -Repository "dotnet/aspnetcore"
./scripts/Get-CIStatus.ps1 -PRNumber 67890 -Repository "dotnet/sdk"
./scripts/Get-CIStatus.ps1 -PRNumber 11111 -Repository "dotnet/roslyn"
```

## Key Parameters

| Parameter | Description |
|-----------|-------------|
| `-PRNumber` | GitHub PR number to analyze |
| `-BuildId` | Azure DevOps build ID |
| `-ShowLogs` | Fetch and display Helix console logs |
| `-Repository` | Target repo (default: dotnet/runtime) |
| `-MaxJobs` | Max failed jobs to show (default: 5) |
| `-SearchMihuBot` | Search MihuBot for related issues |

## Three Modes

The script operates in three distinct modes depending on what information you have:

| You have... | Use | What you get |
|-------------|-----|-------------|
| A GitHub PR number | `-PRNumber 12345` | Full analysis: all builds, failures, known issues, retry recommendation |
| An AzDO build ID | `-BuildId 1276327` | Single build analysis: timeline, failures, Helix results |
| A Helix job ID (optionally a specific work item) | `-HelixJob "..." [-WorkItem "..."]` | Deep dive: list work items for the job, or with `-WorkItem`, focus on a single work item's console logs, artifacts, and test results |

> ❌ **Don't guess the mode.** If the user gives a PR URL, use `-PRNumber`. If they paste an AzDO build link, extract the build ID. If they reference a specific Helix job, use `-HelixJob`.

## What the Script Does

### PR Analysis Mode (`-PRNumber`)
1. Discovers all AzDO builds associated with the PR
2. Fetches Build Analysis for known issues
3. Gets failed jobs from Azure DevOps timeline
4. **Separates canceled jobs from failed jobs** (canceled may be dependency-canceled or timeout-canceled)
5. Extracts Helix work item failures from each failed job
6. Fetches console logs (with `-ShowLogs`)
7. Searches for known issues with "Known Build Error" label
8. Correlates failures with PR file changes
9. **Provides smart retry recommendations**

### Build ID Mode (`-BuildId`)
1. Fetches the build timeline directly (skips PR discovery)
2. Performs steps 3–7 and 9 from PR Analysis Mode, but does **not** fetch Build Analysis known issues or correlate failures with PR file changes (those require a PR number)

### Helix Job Mode (`-HelixJob` [and optional `-WorkItem`])
1. With `-HelixJob` alone: enumerates work items for the job and summarizes their status
2. With `-HelixJob` and `-WorkItem`: queries the specific work item for status and artifacts
3. Fetches console logs and file listings, displays detailed failure information

> ⚠️ **Canceled ≠ Failed.** Canceled jobs often have completed Helix work items — the AzDO wrapper timed out but tests may have passed. See "Recovering Results from Canceled Jobs" below.

## Interpreting Results

**Known Issues section**: Failures matching existing GitHub issues - these are tracked and being investigated.

**Canceled jobs**: Jobs that were canceled (not failed) due to earlier stage failures or timeouts. Dependency-canceled jobs (canceled because an earlier stage failed) don't need investigation. Timeout-canceled jobs may still have recoverable Helix results — see "Recovering Results from Canceled Jobs" below.

> ❌ **Don't dismiss canceled jobs.** Timeout-canceled jobs may have passing Helix results that prove the "failure" was just an AzDO timeout wrapper issue.

**PR Change Correlation**: Files changed by PR appearing in failures - likely PR-related.

**Build errors**: Compilation failures need code fixes.

**Helix failures**: Test failures on distributed infrastructure.

**Local test failures**: Some repos (e.g., dotnet/sdk) run tests directly on build agents. These can also match known issues - search for the test name with the "Known Build Error" label.

> ⚠️ **Be cautious labeling failures as "infrastructure."** If Build Analysis didn't flag a failure as a known issue, treat it as potentially real — even if it looks like a device failure, Docker issue, or network timeout. Only conclude "infrastructure" when you have strong evidence (e.g., identical failure on main branch, Build Analysis match, or confirmed outage). Dismissing failures as transient without evidence delays real bug discovery.

> ❌ **Don't confuse "environment-related" with "infrastructure."** A test that fails because a required framework isn't installed (e.g., .NET 2.2) is a **test defect** — the test has wrong assumptions about what's available. Infrastructure failures are *transient*: network timeouts, Docker pull failures, agent crashes, disk space. If the failure would reproduce 100% of the time on any machine with the same setup, it's a code/test issue, not infra. The word "environment" in the error doesn't make it an infrastructure problem.

> ❌ **Missing packages on flow PRs are NOT always infrastructure failures.** When a codeflow or dependency-update PR fails with "package not found" or "version not available", don't assume it's a feed propagation delay. Flow PRs bring in behavioral changes from upstream repos that can cause the build to request *different* packages than before. Example: an SDK flow changed runtime pack resolution logic, causing builds to look for `Microsoft.NETCore.App.Runtime.browser-wasm` (CoreCLR — doesn't exist) instead of `Microsoft.NETCore.App.Runtime.Mono.browser-wasm` (what had always been used). The fix was in the flowed code, not in feed infrastructure. Always check *which* package is missing and *why* it's being requested before diagnosing as infrastructure.

## Retry Recommendations

The script provides a recommendation at the end:

| Recommendation | Meaning |
|----------------|---------|
| **KNOWN ISSUES DETECTED** | Tracked issues found that may correlate with failures. Review details. |
| **LIKELY PR-RELATED** | Failures correlate with PR changes. Fix issues first. |
| **POSSIBLY TRANSIENT** | No clear cause - check main branch, search for issues. |
| **REVIEW REQUIRED** | Could not auto-determine cause. Manual review needed. |

## Analysis Workflow

1. **Read PR context first** - Check title, description, comments
2. **Run the script** with `-ShowLogs` for detailed failure info
3. **Check Build Analysis** - Known issues are safe to retry
4. **Correlate with PR changes** - Same files failing = likely PR-related
5. **Interpret patterns** (but don't jump to conclusions):
   - Same error across many jobs → Real code issue
   - Build Analysis flags a known issue → Safe to retry
   - Failure is **not** in Build Analysis → Investigate further before assuming transient
   - Device failures, Docker pulls, network timeouts → *Could* be infrastructure, but verify against main branch first
   - Test timeout but tests passed → Executor issue, not test failure

## Presenting Results

The script provides a recommendation at the end, but this is based on heuristics and may be incomplete. Before presenting conclusions to the user:

> ❌ **Don't blindly trust the script's recommendation.** The heuristic can misclassify failures. If the recommendation says "POSSIBLY TRANSIENT" but you see the same test failing 5 times on the same code path the PR touched — it's PR-related.

1. Review the detailed failure information, not just the summary
2. Look for patterns the script may have missed (e.g., related failures across jobs)
3. Consider the PR context (what files changed, what the PR is trying to do)
4. Present findings with appropriate caveats - state what is known vs. uncertain
5. If the script's recommendation seems inconsistent with the details, trust the details

## References

- **Helix artifacts & binlogs**: See [references/helix-artifacts.md](references/helix-artifacts.md)
- **Manual investigation steps**: See [references/manual-investigation.md](references/manual-investigation.md)
- **AzDO/Helix details**: See [references/azdo-helix-reference.md](references/azdo-helix-reference.md)

## Recovering Results from Canceled Jobs

Canceled jobs (typically from timeouts) often still have useful artifacts. The Helix work items may have completed successfully even though the AzDO job was killed while waiting to collect results.

**To investigate canceled jobs:**

1. **Download build artifacts**: Use the AzDO artifacts API to get `Logs_Build_*` pipeline artifacts for the canceled job. These contain binlogs even for canceled jobs.
2. **Extract Helix job IDs**: Use the MSBuild MCP server to load the `SendToHelix.binlog` and search for `"Sent Helix Job"` messages. Each contains a Helix job ID.
3. **Query Helix directly**: For each job ID, query `https://helix.dot.net/api/2019-06-17/jobs/{jobId}/workitems` to get actual pass/fail results.

**Example**: A `browser-wasm windows WasmBuildTests` job was canceled after 3 hours. The binlog (truncated) still contained 12 Helix job IDs. Querying them revealed all 226 work items passed — the "failure" was purely a timeout in the AzDO wrapper.

**Key insight**: "Canceled" ≠ "Failed". Always check artifacts before concluding results are lost.

## Deep Investigation with Azure CLI

When the script and GitHub APIs aren't enough (e.g., investigating internal pipeline definitions or downloading build artifacts), you can use the Azure CLI with the `azure-devops` extension.

> 💡 **Prefer `az pipelines` / `az devops` commands over raw REST API calls.** The CLI handles authentication, pagination, and JSON output formatting. Only fall back to manual `Invoke-RestMethod` calls when the CLI doesn't expose the endpoint you need (e.g., artifact download URLs, specialized timeline queries). The CLI's `--query` (JMESPath) and `-o table` flags are powerful for filtering without extra scripting.

### Checking Azure CLI Authentication

Before making direct AzDO API calls, verify the CLI is installed and authenticated:

```powershell
# Ensure az is on PATH (Windows may need a refresh after install)
$env:Path = [System.Environment]::GetEnvironmentVariable("Path", "Machine") + ";" + [System.Environment]::GetEnvironmentVariable("Path", "User")

# Check if az CLI is available
az --version 2>$null | Select-Object -First 1

# Check if logged in and get current account
az account show --query "{name:name, user:user.name}" -o table 2>$null

# If not logged in, prompt the user to authenticate:
#   az login                              # Interactive browser login
#   az login --use-device-code            # Device code flow (for remote/headless)

# Get an AAD access token for AzDO REST API calls
$accessToken = (az account get-access-token --resource 499b84ac-1321-427f-aa17-267ca6975798 --query accessToken -o tsv)
$headers = @{ "Authorization" = "Bearer $accessToken" }
```

> ⚠️ If `az` is not installed, use `winget install -e --id Microsoft.AzureCLI` (Windows). The `azure-devops` extension is also required — install or verify it with `az extension add --name azure-devops` (safe to run if already installed). Ask the user to authenticate if needed.

> ⚠️ **Do NOT use `az devops configure --defaults`** — it writes to a global config file and will cause conflicts if multiple agents are running concurrently. Always pass `--org` and `--project` (or `-p`) explicitly on each command.

### Querying Pipeline Definitions and Builds

When investigating build failures, it's often useful to look at the pipeline definition itself to understand what stages, jobs, and templates are involved.

**Use `az` CLI commands first** — they're simpler and handle auth automatically. Set `$buildId` from a runs list or from the AzDO URL:

```powershell
$org = "https://dev.azure.com/dnceng"
$project = "internal"

# Find a pipeline definition by name
az pipelines list --name "dotnet-unified-build" --org $org -p $project --query "[].{id:id, name:name, path:path}" -o table

# Get pipeline definition details (shows YAML path, triggers, etc.)
az pipelines show --id 1330 --org $org -p $project --query "{id:id, name:name, yamlPath:process.yamlFilename, repo:repository.name}" -o table

# List recent builds for a pipeline (with filtering)
az pipelines runs list --pipeline-ids 1330 --branch "refs/heads/main" --top 5 --org $org -p $project --query "[].{id:id, result:result, finish:finishTime}" -o table

# Get a specific build's details
az pipelines runs show --id $buildId --org $org -p $project --query "{id:id, result:result, sourceBranch:sourceBranch}" -o table

# List build artifacts
az pipelines runs artifact list --run-id $buildId --org $org -p $project --query "[].{name:name, type:resource.type}" -o table
```

**Fall back to REST API** only when the CLI doesn't expose what you need (e.g., build timelines, artifact downloads):

```powershell
# Get build timeline (stages, jobs, tasks with results and durations) — no CLI equivalent
$accessToken = (az account get-access-token --resource 499b84ac-1321-427f-aa17-267ca6975798 --query accessToken -o tsv)
$headers = @{ "Authorization" = "Bearer $accessToken" }
$timelineUrl = "https://dev.azure.com/dnceng/internal/_apis/build/builds/$buildId/timeline?api-version=7.1"
$timeline = (Invoke-RestMethod -Uri $timelineUrl -Headers $headers)
$timeline.records | Where-Object { $_.result -eq "failed" -and $_.type -eq "Job" }

# Download a specific artifact (e.g., build logs with binlogs) — no CLI equivalent for zip download
$artifactName = "Windows_Workloads_x64_BuildPass2_BuildLogs_Attempt1"
$downloadUrl = "https://dev.azure.com/dnceng/internal/_apis/build/builds/$buildId/artifacts?artifactName=$artifactName&api-version=7.1&`$format=zip"
Invoke-WebRequest -Uri $downloadUrl -Headers $headers -OutFile "$env:TEMP\artifact.zip"
```

### Examining Pipeline YAML

All dotnet repos that use arcade put their pipeline definitions under `eng/pipelines/`. Use `az pipelines show` to find the YAML file path, then fetch it:

```powershell
# Find the YAML path for a pipeline
az pipelines show --id 1330 --org $org -p $project --query "{yamlPath:process.yamlFilename, repo:repository.name}" -o table

# Fetch the YAML from the repo (example: dotnet/runtime's runtime-official pipeline)
#   github-mcp-server-get_file_contents owner:dotnet repo:runtime path:eng/pipelines/runtime-official.yml

# For VMR unified builds, the YAML is in dotnet/dotnet:
#   github-mcp-server-get_file_contents owner:dotnet repo:dotnet path:eng/pipelines/unified-build.yml

# Templates are usually in eng/pipelines/common/ or eng/pipelines/templates/
```

This is especially useful when:
- A job name doesn't clearly indicate what it builds
- You need to understand stage dependencies (why a job was canceled)
- You want to find which template defines a specific step
- Investigating whether a pipeline change caused new failures

## Tips

1. Read PR description and comments first for context
2. Check if same test fails on main branch before assuming transient
3. Look for `[ActiveIssue]` attributes for known skipped tests
4. Use `-SearchMihuBot` for semantic search of related issues
5. Binlogs in artifacts help diagnose MSB4018 task failures
6. Use the MSBuild MCP server (`binlog.mcp`) to search binlogs for Helix job IDs, build errors, and properties
7. If checking CI status via `gh pr checks --json`, the valid fields are `bucket`, `completedAt`, `description`, `event`, `link`, `name`, `startedAt`, `state`, `workflow`. There is **no `conclusion` field** — `state` contains `SUCCESS`/`FAILURE` directly
8. When investigating internal AzDO pipelines, check `az account show` first to verify authentication before making REST API calls
