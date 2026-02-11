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

## Interpreting Results

**Known Issues section**: Failures matching existing GitHub issues - these are tracked and being investigated.

**Canceled jobs**: Jobs that were canceled (not failed) due to earlier stage failures or timeouts. Dependency-canceled jobs (canceled because an earlier stage failed) don't need investigation. Timeout-canceled jobs may still have recoverable Helix results — see "Recovering Results from Canceled Jobs" below.

> ❌ **Don't dismiss canceled jobs.** Timeout-canceled jobs may have passing Helix results that prove the "failure" was just an AzDO timeout wrapper issue.

**PR Change Correlation**: Files changed by PR appearing in failures - likely PR-related.

**Build errors**: Compilation failures need code fixes.

**Helix failures**: Test failures on distributed infrastructure.

**Local test failures**: Some repos (e.g., dotnet/sdk) run tests directly on build agents. These can also match known issues - search for the test name with the "Known Build Error" label.

> ⚠️ **Be cautious labeling failures as "infrastructure."** Only conclude infrastructure when you have strong evidence: Build Analysis match, identical failure on target branch, or confirmed outage. "Environment" in the error doesn't make it infrastructure — a test requiring an uninstalled framework is a test defect, not infra.

> ❌ **Missing packages on flow PRs ≠ infrastructure.** Flow PRs bring behavioral changes that can cause builds to request *different* packages. Always check *which* package is missing and *why* before assuming feed propagation delay.

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
| `MERGE_CONFLICTS` | PR has merge conflicts — CI won't run. Tell the user to resolve conflicts. Offer to analyze a previous build by ID. |
| `NO_BUILDS` | No AzDO builds found (CI not triggered). Offer to check if CI needs to be triggered or analyze a previous build. |

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

### Tone and output format

Be direct. Lead with the most important finding. Structure your response as:
1. **Summary verdict** (1-2 sentences) — Is CI green? Failures PR-related? Known issues?
2. **Failure details** (2-4 bullets) — what failed, why, evidence
3. **Recommended actions** (numbered) — retry, fix, investigate. Include `/azp run` commands.

Synthesize from: JSON summary (structured facts) + human-readable output (details/logs) + Step 0 context (PR type, author intent).

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
4. **Check build progression** — If the PR has multiple builds (multiple pushes), check whether earlier builds passed. A failure that appeared after a specific push narrows the investigation to those commits. See [references/build-progression-analysis.md](references/build-progression-analysis.md). Present findings as facts, not fix recommendations.
5. **Interpret patterns** (but don't jump to conclusions):
   - Same error across many jobs → Real code issue
   - Build Analysis flags a known issue → Safe to retry
   - Failure is **not** in Build Analysis → Investigate further before assuming transient
   - Device failures, Docker pulls, network timeouts → *Could* be infrastructure, but verify against the target branch first
   - Test timeout but tests passed → Executor issue, not test failure
6. **Check for mismatch with user's question** — The script only reports builds for the current head SHA. If the user asks about a job, error, or cancellation that doesn't appear in the results, **ask** if they're referring to a prior build. Common triggers:
   - User mentions a canceled job but `canceledJobNames` is empty
   - User says "CI is failing" but the latest build is green
   - User references a specific job name not in the current results
   Offer to re-run with `-BuildId` if the user can provide the earlier build ID from AzDO.

### Step 3: Verify before claiming

Before stating a failure's cause, verify your claim:

- **"Infrastructure failure"** → Did Build Analysis flag it? Does the same test pass on the target branch? If neither, don't call it infrastructure.
- **"Transient/flaky"** → Has it failed before? Is there a known issue? A single non-reproducing failure isn't enough to call it flaky.
- **"PR-related"** → Do the changed files actually relate to the failing test? Correlation in the script output is heuristic, not proof.
- **"Safe to retry"** → Are ALL failures accounted for (known issues or infrastructure), or are you ignoring some?
- **"Not related to this PR"** → Have you checked if the test passes on the target branch? Don't assume — verify.

## References

- **Helix artifacts & binlogs**: See [references/helix-artifacts.md](references/helix-artifacts.md)
- **Binlog comparison (passing vs failing)**: See [references/binlog-comparison.md](references/binlog-comparison.md)
- **Build progression (commit-to-build correlation)**: See [references/build-progression-analysis.md](references/build-progression-analysis.md)
- **Subagent delegation patterns**: See [references/delegation-patterns.md](references/delegation-patterns.md)
- **Azure CLI deep investigation**: See [references/azure-cli.md](references/azure-cli.md)
- **Manual investigation steps**: See [references/manual-investigation.md](references/manual-investigation.md)
- **AzDO/Helix details**: See [references/azdo-helix-reference.md](references/azdo-helix-reference.md)

## Tips

1. Check if same test fails on the target branch before assuming transient
2. Look for `[ActiveIssue]` attributes for known skipped tests
3. Use `-SearchMihuBot` for semantic search of related issues
4. Use the MSBuild MCP server (`binlog.mcp`) to search binlogs for Helix job IDs, build errors, and properties
5. `gh pr checks --json` valid fields: `bucket`, `completedAt`, `description`, `event`, `link`, `name`, `startedAt`, `state`, `workflow` — no `conclusion` field, `state` has `SUCCESS`/`FAILURE` directly
6. "Canceled" ≠ "Failed" — canceled jobs may have recoverable Helix results. Check artifacts before concluding results are lost.
