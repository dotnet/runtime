# Deep Investigation: Build Progression Analysis

When the current build is failing, the PR's build history can reveal whether the failure existed from the start or appeared after specific changes. This is a fact-gathering technique — like target-branch comparison — that provides context for understanding the current failure.

## When to Use This Pattern

- Standard analysis (script + logs) hasn't identified the root cause of the current failure
- The PR has multiple pushes and you want to know whether earlier builds passed or failed
- You need to understand whether a failure is inherent to the PR's approach or was introduced by a later change

## The Pattern

### Step 1: List all builds for the PR

`gh pr checks` only shows checks for the current HEAD SHA. To see the full build history, query AzDO:

```powershell
$org = "https://dev.azure.com/dnceng-public"
$project = "public"
az pipelines runs list --branch "refs/pull/{PR}/merge" --top 20 --org $org -p $project `
    --query "[].{id:id, result:result, sourceVersion:sourceVersion, finishTime:finishTime}" -o table
```

### Step 2: Map builds to commits

For each build, identify the source commit and use `git log` to see what was included:

```powershell
# Get source version for a build
az pipelines runs show --id $buildId --org $org -p $project `
    --query "{id:id, result:result, sourceVersion:sourceVersion}" -o json

# See what commits are between two source versions
# git log --oneline $passingCommit..$failingCommit
```

### Step 3: Build a progression table

Present the facts as a table:

| Build | Source commit | Result | What changed since previous build |
|-------|-------------|--------|----------------------------------|
| 1284433 | abc123 | ✅ 9/9 | Initial PR commits |
| 1286087 | def456 | ❌ 7/9 | Added commit C |
| 1286967 | ghi789 | ❌ 7/9 | Modified commit C |

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

Use target-branch comparison first to confirm the failure is PR-related. Use build progression to narrow down *which part* of the PR introduced it.

## Anti-Patterns

> ❌ **Don't treat build history as a substitute for analyzing the current build.** The current build determines CI status. Build history is context for understanding and investigating the current failure.

> ❌ **Don't make fix recommendations from progression alone.** "Build N passed and build N+1 failed after adding commit C" is a fact worth reporting. "Therefore revert commit C" is a judgment that requires more context than the agent has — the commit may be addressing a critical review concern, fixing a different bug, or partially correct.

> ❌ **Don't assume earlier passing builds prove the original approach was complete.** A build may pass because it didn't change enough to trigger the failing test scenario. The reviewer who requested additional changes may have identified a real gap.
