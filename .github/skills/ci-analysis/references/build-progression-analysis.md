# Deep Investigation: Build Progression Analysis

When the current build is failing, the PR's build history can reveal whether the failure existed from the start or appeared after specific changes. This is a fact-gathering technique — like target-branch comparison — that provides context for understanding the current failure.

## When to Use This Pattern

- Standard analysis (script + logs) hasn't identified the root cause of the current failure
- The PR has multiple pushes and you want to know whether earlier builds passed or failed
- You need to understand whether a failure is inherent to the PR's approach or was introduced by a later change

## The Pattern

### Step 0: Start with the recent builds

Don't try to analyze the full build history upfront — especially on large PRs with many pushes. Start with the most recent N builds (5-8), present the progression table, and let the user decide whether to dig deeper into earlier builds.

On large PRs, the user is usually iterating toward a solution. The recent builds are the most relevant. Offer: "Here are the last N builds — the pass→fail transition was between X and Y. Want me to look at earlier builds?"

### Step 1: List builds for the PR

`gh pr checks` only shows checks for the current HEAD SHA. To see the full build history, use AzDO MCP or CLI:

**With AzDO MCP (preferred):**
```
azure-devops-pipelines_get_builds with:
  project: "public"
  branchName: "refs/pull/{PR}/merge"
  top: 20
  queryOrder: "QueueTimeDescending"
```

The response includes `triggerInfo` with `pr.sourceSha` — the PR's HEAD commit for each build.

**Without MCP (fallback):**
```powershell
$org = "https://dev.azure.com/dnceng-public"
$project = "public"
az pipelines runs list --branch "refs/pull/{PR}/merge" --top 20 --org $org -p $project -o json
```

### Step 2: Map builds to the PR's head commit

Each build's `triggerInfo` contains `pr.sourceSha` — the PR's HEAD commit when the build was triggered. Extract it from the `get_builds` response or the `az` JSON output.

> ⚠️ **`sourceVersion` is the merge commit**, not the PR's head commit. Use `triggerInfo.'pr.sourceSha'` instead.

> ⚠️ **Target branch moves between builds.** Each build merges `pr.sourceSha` into the target branch HEAD *at the time the build starts*. If `main` received new commits between build N and N+1, the two builds merged against different baselines — even if `pr.sourceSha` is the same. Always extract the target branch HEAD to detect baseline shifts.

### Step 2b: Extract the target branch HEAD from checkout logs

The AzDO build API doesn't expose the target branch SHA. Extract it from the checkout task log.

**With AzDO MCP (preferred):**
```
azure-devops-pipelines_get_build_log_by_id with:
  project: "public"
  buildId: {BUILD_ID}
  logId: 5
  startLine: 500
```

Search the output for the merge line:
```
HEAD is now at {mergeCommit} Merge {prSourceSha} into {targetBranchHead}
```

**Without MCP (fallback):**
```powershell
$token = az account get-access-token --resource "499b84ac-1321-427f-aa17-267ca6975798" --query accessToken -o tsv
$headers = @{ Authorization = "Bearer $token" }
$logUrl = "https://dev.azure.com/{org}/{project}/_apis/build/builds/{BUILD_ID}/logs/5"
$log = Invoke-RestMethod -Uri $logUrl -Headers $headers
```

> Note: log ID 5 is the first checkout task in most pipelines. The merge line is typically around line 500-650. If log 5 doesn't contain it, check the build timeline for "Checkout" tasks.

Note: a PR may have more unique `pr.sourceSha` values than commits visible on GitHub, because force-pushes replace the commit history. Each force-push triggers a new build with a new merge commit and a new `pr.sourceSha`.

### Step 3: Build a progression table

Include the target branch HEAD to catch baseline shifts:

| PR HEAD | Target HEAD | Builds | Result | Notes |
|---------|-------------|--------|--------|-------|
| 7af79ad | 2d638dc | 1283986 | ❌ | Initial commits |
| 28ec8a0 | 0b691ba | 1284169 | ❌ | Iteration 2 |
| 39dc0a6 | 18a3069 | 1284433 | ✅ | Iteration 3 |
| f186b93 | 5709f35 | 1286087 | ❌ | Added commit C; target moved ~35 commits |
| 2e74845 | 482d8f9 | 1286967 | ❌ | Modified commit C |

When both `pr.sourceSha` AND `Target HEAD` change between a pass→fail transition, either could be the cause. Analyze the failure content to determine which. If only the target moved (same `pr.sourceSha`), the failure came from the new baseline.

### Step 4: Present findings, not conclusions

Report what the progression shows:
- Which builds passed and which failed
- What commits were added between the last passing and first failing build
- Whether the failing commits were added in response to review feedback (check review threads)

**Do not** make fix recommendations based solely on build progression. The progression narrows the investigation — it doesn't determine the right fix. The human may have context about why changes were made, what constraints exist, or what the reviewer intended.

## Checking review context

When the progression shows that a failure appeared after new commits, check whether those commits were review-requested:

```powershell
# Get review comments with timestamps
gh api "repos/{OWNER}/{REPO}/pulls/{PR}/comments" `
    --jq '.[] | {author: .user.login, body: .body, created: .created_at}'
```

Present this as additional context: "Commit C was pushed after reviewer X commented requesting Y." Let the author decide how to proceed.

## Combining with Binlog Comparison

Build progression identifies **which change** correlates with the current failure. Binlog comparison (see [binlog-comparison.md](binlog-comparison.md)) shows **what's different** in the build between a passing and failing state. Together they provide a complete picture:

1. Progression → "The current failure first appeared in build N+1, which added commit C"
2. Binlog comparison → "In the current (failing) build, task X receives parameter Y=Z, whereas in the passing build it received Y=W"

## Relationship to Target-Branch Comparison

Both techniques compare a failing build against a passing one:

| Technique | Passing build from | Answers |
|-----------|-------------------|---------|
| **Target-branch comparison** | Recent build on the base branch (e.g., main) | "Does this test pass without the PR's changes at all?" |
| **Build progression** | Earlier build on the same PR | "Did this test pass with the PR's *earlier* changes?" |

Use target-branch comparison first to confirm the failure is PR-related. Use build progression to narrow down *which part* of the PR introduced it. If build progression shows a pass→fail transition with the same `pr.sourceSha`, the target branch is the more likely culprit — use target-branch comparison to confirm.

## Anti-Patterns

> ❌ **Don't treat build history as a substitute for analyzing the current build.** The current build determines CI status. Build history is context for understanding and investigating the current failure.

> ❌ **Don't make fix recommendations from progression alone.** "Build N passed and build N+1 failed after adding commit C" is a fact worth reporting. "Therefore revert commit C" is a judgment that requires more context than the agent has — the commit may be addressing a critical review concern, fixing a different bug, or partially correct.

> ❌ **Don't assume earlier passing builds prove the original approach was complete.** A build may pass because it didn't change enough to trigger the failing test scenario. The reviewer who requested additional changes may have identified a real gap.
