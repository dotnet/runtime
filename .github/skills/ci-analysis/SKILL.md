---
name: ci-analysis
description: Analyze CI build and test status from Azure DevOps and Helix for dotnet repository PRs. Use when checking CI status, investigating failures, determining if a PR is ready to merge, or given URLs containing dev.azure.com or helix.dot.net. Also use when asked "why is CI red", "test failures", "retry CI", "rerun tests", "is CI green", "build failed", "checks failing", or "flaky tests".
---

# Azure DevOps and Helix CI Analysis

Analyze CI build status and test failures in Azure DevOps and Helix for dotnet repositories (runtime, sdk, aspnetcore, roslyn, and more).

> 🚨 **NEVER** use `gh pr review --approve` or `--request-changes`. Only `--comment` is allowed. Approval and blocking are human-only actions.

**Workflow**: Gather PR context (Step 0) → run the script → read the human-readable output + `[CI_ANALYSIS_SUMMARY]` JSON → synthesize recommendations yourself. The script collects data; you generate the advice.

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
| A GitHub PR number | `-PRNumber 12345` | Full analysis: all builds, failures, known issues, structured JSON summary |
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
9. **Emits structured summary** — `[CI_ANALYSIS_SUMMARY]` JSON block with all key facts for the agent to reason over

> **After the script runs**, you (the agent) generate recommendations. The script collects data; you synthesize the advice. See [Generating Recommendations](#generating-recommendations) below.

### Build ID Mode (`-BuildId`)
1. Fetches the build timeline directly (skips PR discovery)
2. Performs steps 3–7 from PR Analysis Mode, but does **not** fetch Build Analysis known issues or correlate failures with PR file changes (those require a PR number). Still emits `[CI_ANALYSIS_SUMMARY]` JSON.

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

> ⚠️ **Be cautious labeling failures as "infrastructure."** If Build Analysis didn't flag a failure as a known issue, treat it as potentially real — even if it looks like a device failure, Docker issue, or network timeout. Only conclude "infrastructure" when you have strong evidence (e.g., identical failure on the target branch, Build Analysis match, or confirmed outage). Dismissing failures as transient without evidence delays real bug discovery.

> ❌ **Don't confuse "environment-related" with "infrastructure."** A test that fails because a required framework isn't installed (e.g., .NET 2.2) is a **test defect** — the test has wrong assumptions about what's available. Infrastructure failures are *transient*: network timeouts, Docker pull failures, agent crashes, disk space. If the failure would reproduce 100% of the time on any machine with the same setup, it's a code/test issue, not infra. The word "environment" in the error doesn't make it an infrastructure problem.

> ❌ **Missing packages on flow PRs are NOT always infrastructure failures.** When a codeflow or dependency-update PR fails with "package not found" or "version not available", don't assume it's a feed propagation delay. Flow PRs bring in behavioral changes from upstream repos that can cause the build to request *different* packages than before. Example: an SDK flow changed runtime pack resolution logic, causing builds to look for `Microsoft.NETCore.App.Runtime.browser-wasm` (CoreCLR — doesn't exist) instead of `Microsoft.NETCore.App.Runtime.Mono.browser-wasm` (what had always been used). The fix was in the flowed code, not in feed infrastructure. Always check *which* package is missing and *why* it's being requested before diagnosing as infrastructure.

## Generating Recommendations

After the script outputs the `[CI_ANALYSIS_SUMMARY]` JSON block, **you** synthesize recommendations. Do not parrot the JSON — reason over it.

### Decision logic

Read `recommendationHint` as a starting point, then layer in context:

| Hint | Action |
|------|--------|
| `BUILD_SUCCESSFUL` | No failures. Confirm CI is green. |
| `KNOWN_ISSUES_DETECTED` | Known tracked issues found. Recommend retry if failures match known issues. Link the issues. |
| `LIKELY_PR_RELATED` | Failures correlate with PR changes. Lead with "fix these before retrying" and list `correlatedFiles`. |
| `POSSIBLY_TRANSIENT` | No correlation with PR changes, no known issues. Suggest checking the target branch, searching for issues, or retrying. |
| `REVIEW_REQUIRED` | Could not auto-determine cause. Review failures manually. |

Then layer in nuance the heuristic can't capture:

- **Mixed signals**: Some failures match known issues AND some correlate with PR changes → separate them. Known issues = safe to retry; correlated = fix first.
- **Canceled jobs with recoverable results**: If `canceledJobNames` is non-empty, mention that canceled jobs may have passing Helix results (see "Recovering Results from Canceled Jobs").
- **Build still in progress**: If `lastBuildJobSummary.pending > 0`, note that more failures may appear.
- **Multiple builds**: If `builds` has >1 entry, `lastBuildJobSummary` reflects only the last build — use `totalFailedJobs` for the aggregate count.
- **BuildId mode**: `knownIssues` will be empty and `prCorrelation` will show `hasCorrelation = false` with `changedFileCount = 0` (PR correlation is not available without a PR number). Don't say "no known issues" or "no correlation" — say "Build Analysis and PR correlation not available in BuildId mode."
- **Infrastructure vs code**: Don't label failures as "infrastructure" unless Build Analysis flagged them or the same test passes on the target branch. See the anti-patterns in "Interpreting Results" above.

### How to Retry

- **AzDO builds**: Comment `/azp run {pipeline-name}` on the PR (e.g., `/azp run dotnet-sdk-public`)
- **All pipelines**: Comment `/azp run` to retry all failing pipelines
- **Helix work items**: Cannot be individually retried — must re-run the entire AzDO build

### Tone

Be direct. Lead with the most important finding. Use 2-4 bullet points, not long paragraphs. Distinguish what's known vs. uncertain.

## Analysis Workflow

### Step 0: Gather Context (before running anything)

Before running the script, read the PR to understand what you're analyzing. Context changes how you interpret every failure.

1. **Read PR metadata** — title, description, author, labels, linked issues
2. **Classify the PR type** — this determines your interpretation framework:

| PR Type | How to detect | Interpretation shift |
|---------|--------------|---------------------|
| **Code PR** | Human author, code changes | Failures likely relate to the changes |
| **Flow/Codeflow PR** | Author is `dotnet-maestro[bot]`, title mentions "Update dependencies" | Missing packages may be behavioral, not infrastructure (see anti-pattern below) |
| **Backport** | Title mentions "backport", targets a release branch | Failures may be branch-specific; check if test exists on target branch |
| **Merge PR** | Merging between branches (e.g., release → main) | Conflicts and merge artifacts cause failures, not the individual changes |
| **Dependency update** | Bumps package versions, global.json changes | Build failures often trace to the dependency, not the PR's own code |

3. **Check existing comments** — has someone already diagnosed the failures? Is there a retry pending?
4. **Note the changed files** — you'll use these to evaluate correlation after the script runs

> ❌ **Don't skip Step 0.** Running the script without PR context leads to misdiagnosis — especially for flow PRs where "package not found" looks like infrastructure but is actually a code issue.

### Step 1: Run the script

Run with `-ShowLogs` for detailed failure info.

### Step 2: Analyze results

1. **Check Build Analysis** — Known issues are safe to retry
2. **Correlate with PR changes** — Same files failing = likely PR-related
3. **Compare with baseline** — If a test passes on the target branch but fails on the PR, compare Helix binlogs. See [references/binlog-comparison.md](references/binlog-comparison.md) — **delegate binlog download/extraction to subagents** to avoid burning context on mechanical work.
4. **Interpret patterns** (but don't jump to conclusions):
   - Same error across many jobs → Real code issue
   - Build Analysis flags a known issue → Safe to retry
   - Failure is **not** in Build Analysis → Investigate further before assuming transient
   - Device failures, Docker pulls, network timeouts → *Could* be infrastructure, but verify against the target branch first
   - Test timeout but tests passed → Executor issue, not test failure

### Step 3: Verify before claiming

Before stating a failure's cause, verify your claim:

- **"Infrastructure failure"** → Did Build Analysis flag it? Does the same test pass on the target branch? If neither, don't call it infrastructure.
- **"Transient/flaky"** → Has it failed before? Is there a known issue? A single non-reproducing failure isn't enough to call it flaky.
- **"PR-related"** → Do the changed files actually relate to the failing test? Correlation in the script output is heuristic, not proof.
- **"Safe to retry"** → Are ALL failures accounted for (known issues or infrastructure), or are you ignoring some?
- **"Not related to this PR"** → Have you checked if the test passes on the target branch? Don't assume — verify.

## Presenting Results

The script outputs both human-readable failure details and a `[CI_ANALYSIS_SUMMARY]` JSON block. Use both to produce a structured response.

### Output structure

Use this format — adapt sections based on what you find:

**1. Summary verdict** (1-2 sentences)
Lead with the most important finding. Is CI green? Are failures PR-related? Known issues?

**2. Failure details** (2-4 bullets)
For each distinct failure category, state: what failed, why (known/correlated/unknown), and evidence.

**3. Recommended actions** (numbered list)
Specific next steps: retry, fix specific files, investigate further. Include `/azp run` commands if retrying.

### How to synthesize

1. Read the JSON summary for structured facts (failed jobs, known issues, PR correlation, recommendation hint)
2. Read the human-readable output for failure details, console logs, and error messages
3. Layer in Step 0 context — PR type, author intent, changed files
4. Reason over all three to produce contextual recommendations — the `recommendationHint` is a starting point, not the final answer
5. Look for patterns the heuristic may have missed (e.g., same failure across multiple jobs, related failures in different builds)
6. Present findings with appropriate caveats — state what is known vs. uncertain

## References

- **Helix artifacts & binlogs**: See [references/helix-artifacts.md](references/helix-artifacts.md)
- **Binlog comparison (passing vs failing)**: See [references/binlog-comparison.md](references/binlog-comparison.md)
- **Subagent delegation patterns**: See [references/delegation-patterns.md](references/delegation-patterns.md)
- **Azure CLI deep investigation**: See [references/azure-cli.md](references/azure-cli.md)
- **Manual investigation steps**: See [references/manual-investigation.md](references/manual-investigation.md)
- **AzDO/Helix details**: See [references/azdo-helix-reference.md](references/azdo-helix-reference.md)

## Recovering Results from Canceled Jobs

Canceled jobs (typically from timeouts) often still have useful artifacts. The Helix work items may have completed successfully even though the AzDO job was killed while waiting to collect results.

**To investigate canceled jobs:**

1. **Download build artifacts**: Use `az pipelines runs artifact download` (see [references/azure-cli.md](references/azure-cli.md)) to get pipeline artifacts for the canceled job. These contain binlogs even for canceled jobs.
2. **Extract Helix job IDs**: Use the MSBuild MCP server to load the `SendToHelix.binlog` and search for `"Sent Helix Job"` messages. Each contains a Helix job ID. See [references/binlog-comparison.md](references/binlog-comparison.md) for the full "binlogs to find binlogs" workflow.
3. **Query Helix directly**: For each job ID, use the CI script: `./scripts/Get-CIStatus.ps1 -HelixJob "{GUID}" -FindBinlogs`

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

1. Check if same test fails on the target branch before assuming transient
2. Look for `[ActiveIssue]` attributes for known skipped tests
3. Use `-SearchMihuBot` for semantic search of related issues
4. Binlogs in artifacts help diagnose MSB4018 task failures
5. Use the MSBuild MCP server (`binlog.mcp`) to search binlogs for Helix job IDs, build errors, and properties
6. If checking CI status via `gh pr checks --json`, the valid fields are `bucket`, `completedAt`, `description`, `event`, `link`, `name`, `startedAt`, `state`, `workflow`. There is **no `conclusion` field** in current `gh` versions — `state` contains `SUCCESS`/`FAILURE` directly
7. When investigating internal AzDO pipelines, check `az account show` first to verify authentication before making REST API calls
