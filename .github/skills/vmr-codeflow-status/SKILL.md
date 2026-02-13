---
name: vmr-codeflow-status
description: >
  Analyze VMR codeflow PR status for dotnet repositories.
  USE FOR: investigating stale codeflow PRs, checking if fixes have flowed
  through the VMR pipeline, debugging dependency update issues in PRs authored
  by dotnet-maestro[bot], checking overall flow status for a repo, diagnosing
  why backflow PRs are missing or blocked, URLs containing dotnet-maestro or
  "Source code updates from dotnet/dotnet".
  DO NOT USE FOR: CI build failures (use ci-analysis skill), code review
  (use code-review skill), general PR investigation without codeflow context.
  INVOKES: Get-CodeflowStatus.ps1 script via powershell.
---

# VMR Codeflow Status

Analyze the health of VMR codeflow PRs in both directions:
- **Backflow**: `dotnet/dotnet` → product repos (e.g., `dotnet/sdk`)
- **Forward flow**: product repos → `dotnet/dotnet`

> 🚨 **NEVER** use `gh pr review --approve` or `--request-changes`. Only `--comment` is allowed. Approval and blocking are human-only actions.

**Workflow**: Run the script → read the human-readable output + `[CODEFLOW_SUMMARY]` JSON → synthesize recommendations yourself. The script collects data; you generate the advice.

> 📎 **Presenting PRs**: Script output references PRs by number (e.g., `#53001`). When presenting to the user, always render as markdown links to the correct repo: `[dotnet/sdk#53001](https://github.com/dotnet/sdk/pull/53001)`. The `-Repository` parameter and script context tell you which repo each PR belongs to — backflow PRs are in the product repo, forward flow PRs are in `dotnet/dotnet`.

## Prerequisites

- **GitHub CLI (`gh`)** — must be installed and authenticated (`gh auth login`)
- Run scripts **from the skill directory** or use the full path to the script

> 💡 **GitHub MCP tools** are available for simple queries (e.g., listing PRs, reading file contents). The script remains the primary tool for comprehensive codeflow analysis — it correlates multiple API calls, cross-references timestamps, and emits structured JSON that individual MCP calls can't replicate.

## When to Use This Skill

Use this skill when:
- A codeflow PR (from `dotnet-maestro[bot]`) has failing tests and you need to know if it's stale
- You need to check if a specific fix has flowed through the VMR pipeline to a codeflow PR
- A PR has a Maestro staleness warning ("codeflow cannot continue") or conflict
- You need to understand what manual commits would be lost if a codeflow PR is closed
- You want to check the overall state of flow for a repo (backflow and forward flow health)
- You need to know why backflow PRs are missing or when the last VMR build was published
- You're asked questions like "is this codeflow PR up to date", "has the runtime revert reached this PR", "why is the codeflow blocked", "what is the state of flow for the sdk", "what's the flow status for net11", "why are builds stale", "how far behind is the backflow"

## Two Modes

| Mode | Use When | Required Params |
|------|----------|-----------------|
| **PR analysis** | Investigating a specific codeflow PR | `-PRNumber -Repository` |
| **Flow health** (`-CheckMissing`) | Checking overall repo flow status | `-CheckMissing -Repository` (optional: `-Branch`) |

> ⚠️ **Branch names differ across repos.** The `-Branch` parameter is the **product repo** branch name, not the VMR branch. When the user says "net10", map to the correct branch per repo:
> - `dotnet/runtime`, `dotnet/aspnetcore`, `dotnet/efcore`, `dotnet/winforms`, `dotnet/wpf` → `release/10.0`
> - `dotnet/sdk`, `dotnet/msbuild` → `release/10.0.1xx` (or `10.0.2xx`, `10.0.3xx` for other bands)
> - `dotnet/roslyn` → `release/dev18.0` (Roslyn uses VS version numbering)
>
> **When the user asks about a major version** (e.g., "net10 flow", "net11 status"), **omit `-Branch`** and let the script discover all branches. Don't ask for clarification — check everything and present a consolidated view. Only narrow to a specific band if the user explicitly mentions one (e.g., "10.0.2xx").

> 📋 **Discovering repos to check.** When the user asks about flow status across "all repos" or a major version, use `darc` to get the authoritative list of repos with active backflow subscriptions:
> ```
> darc get-subscriptions --source-repo dotnet/dotnet --channel "11.0.1xx"
> ```
> This returns all repos subscribed to backflow from the VMR for that channel. Run `-CheckMissing` (omitting `-Branch`) against each target repo. If darc isn't installed or authenticated, fall back to the core product repos: `runtime`, `sdk`, `aspnetcore`, `roslyn`, `efcore`, `winforms`, `wpf`, `msbuild`, `fsharp`.

> ⚠️ **Common mistake**: Don't use `-PRNumber` and `-CheckMissing` together — they are separate modes. `-CheckMissing` scans branches discovered from open and recent backflow PRs (unless `-Branch` is provided), not a specific PR.

## Quick Start

```powershell
# Check codeflow PR status (most common)
./scripts/Get-CodeflowStatus.ps1 -PRNumber 52727 -Repository "dotnet/sdk"

# Trace a specific fix through the pipeline
./scripts/Get-CodeflowStatus.ps1 -PRNumber 52727 -Repository "dotnet/sdk" -TraceFix "dotnet/runtime#123974"

# Show individual VMR commits that are missing
./scripts/Get-CodeflowStatus.ps1 -PRNumber 52727 -Repository "dotnet/sdk" -ShowCommits

# Check overall flow health for a repo (backflow + forward flow)
./scripts/Get-CodeflowStatus.ps1 -Repository "dotnet/roslyn" -CheckMissing

# Check a specific branch only
./scripts/Get-CodeflowStatus.ps1 -Repository "dotnet/sdk" -CheckMissing -Branch "main"
```

## Key Parameters

| Parameter | Required | Default | Description |
|-----------|----------|---------|-------------|
| `-PRNumber` | Yes (unless `-CheckMissing`) | — | GitHub PR number to analyze |
| `-Repository` | Yes | — | Target repo in `owner/repo` format |
| `-TraceFix` | No | — | Trace a repo PR through the pipeline. Format: `owner/repo#number` (e.g., `dotnet/runtime#123974`) |
| `-ShowCommits` | No | `$false` | Show individual VMR commits between PR snapshot and branch HEAD |
| `-CheckMissing` | No | `$false` | Check overall flow health: missing backflow PRs, forward flow status, and official build freshness |
| `-Branch` | No | — | With `-CheckMissing`, only check a specific branch (e.g., `main`, `release/10.0`) |

## What the Script Does

### PR Analysis Mode (default)

> **Design principle**: Assess current state from primary signals first, then use Maestro comments as historical context — not the other way around. Comments tell you the history, not the present.

1. **PR Overview** — Basic PR info, flow direction (backflow vs forward flow)
2. **Current State** — Independent assessment from primary signals: empty diff, force pushes, merge status. Produces a one-line verdict (NO-OP / IN PROGRESS / STALE / ACTIVE / MERGED / CLOSED) before reading any comments
3. **Codeflow Metadata** — Extracts VMR commit, subscription ID, build info from PR body
4. **Snapshot Validation** — Cross-references PR body commit against Version.Details.xml and branch commits to detect stale metadata
5. **Source Freshness** — Compares PR's VMR snapshot against current VMR branch HEAD; shows pending forward flow PRs
6. **PR Branch Analysis** — Categorizes commits as auto-updates vs manual; detects codeflow-like manual commits
7. **Codeflow History** — Maestro comments as historical context (conflict/staleness warnings), cross-referenced against force push timestamps to determine if issues were already addressed
8. **Traces fixes** (with `-TraceFix`) — Checks if a specific fix has flowed through VMR → codeflow PR
9. **Emits structured summary** — `[CODEFLOW_SUMMARY]` JSON block with all key facts for the agent to reason over

> **After the script runs**, you (the agent) generate recommendations. The script collects data; you synthesize the advice. See [Generating Recommendations](#generating-recommendations) below.

### Flow Health Mode (`-CheckMissing`)
1. **Checks official build freshness** — Queries `aka.ms` shortlinks for latest published VMR build dates per channel
2. **Scans backflow PRs** — Finds branches where a backflow PR should exist but doesn't, and checks health of open PRs (conflict/staleness/resolved status)
3. **Scans forward flow** — Checks open forward flow PRs into `dotnet/dotnet` for staleness and conflicts
4. **Produces summary** — Counts healthy/blocked/missing PRs across both directions
5. **Emits structured summary** — `[CODEFLOW_SUMMARY]` JSON block with backflow/forward flow counts and build freshness

> ❌ **Never assume "Unknown" health means healthy.** When `gh` API calls fail (auth, rate limiting), the script returns "Unknown" status — this is explicitly excluded from healthy/covered counts.

> ⚠️ **aka.ms redirect behavior**: 301 is expected and treated as a valid product URL (→ ci.dot.net). Non-301 redirects (often 302, which goes to Bing) indicate an invalid URL. The script only accepts 301.

## Interpreting Results

### Current State (assessed first, from primary signals)
- **✅ MERGED**: PR has been merged — no action needed
- **✖️ CLOSED**: PR was closed without merging — Maestro should create a replacement
- **📭 NO-OP**: Empty diff — PR likely already resolved, changes landed via other paths
- **🔄 IN PROGRESS**: Recent force push within 24h — someone is actively working on it
- **⏳ STALE**: No activity for >3 days — may need attention
- **✅ ACTIVE**: PR has content and recent activity

### Freshness
- **✅ Up to date**: PR has the latest VMR snapshot
- **⚠️ VMR is N commits ahead**: The PR is missing updates. Check if the missing commits contain the fix you need.
- **📊 Forward flow coverage**: Shows how many missing repos have pending forward flow PRs that would close part of the gap once merged.

### Snapshot Validation
- **✅ Match**: PR body commit matches the branch's actual "Backflow from" commit
- **⚠️ Mismatch**: PR body is stale — the script automatically uses the branch-derived commit for freshness checks
- **ℹ️ Initial commit only**: PR body can't be verified yet (no "Backflow from" commit exists)

### Codeflow History (Maestro comments as context)
- **✅ No warnings**: Maestro can freely update the PR
- **⚠️ Staleness warning**: A forward flow merged while this backflow PR was open. Maestro blocked further updates.
- **🔴 Conflict detected**: Maestro found merge conflicts. Shows conflicting files and `darc vmr resolve-conflict` command.
- **ℹ️ Force push after warning**: When a force push post-dates a conflict/staleness warning, the issue may already be resolved. The script cross-references timestamps automatically.

### Manual Commits
Manual commits on the PR branch are at risk if the PR is closed or force-triggered. The script lists them so you can decide whether to preserve them.

### Subscription Health (darc cross-reference)
When darc is available and a backflow PR is missing, the script cross-references subscription state against the latest build. The `subscriptionHealth` array in the JSON contains:
- `diagnostic`: `maestro-stuck` | `subscription-disabled` | `subscription-missing` | `channel-frozen` | `frequency-limited` | `healthy`
- `subscriptionId`: for use with `darc trigger-subscriptions --id <id>`
- `message`: human-readable summary with build numbers

Key diagnostics:
- **`maestro-stuck`**: Subscription enabled, but `Last Build` is older than latest published build — Maestro isn't processing new builds. Verify by comparing build numbers and dates. If confirmed, `darc trigger-subscriptions` can remediate (see Remediation Commands below).
- **`subscription-missing`**: No subscription exists for this repo/branch — expected for shipped previews, unexpected for active branches
- **`channel-frozen`**: Latest build is `Released` (preview shipped), subscription is up to date — no action needed
- **`subscription-disabled`**: Subscription exists but is turned off

### Fix Tracing
When using `-TraceFix`:
- **✅ Fix is in VMR manifest**: The fix has flowed to the VMR
- **✅ Fix is in PR snapshot**: The codeflow PR already includes this fix
- **❌ Fix is NOT in PR snapshot**: The PR needs a codeflow update to get this fix

## Generating Recommendations

After the script outputs the `[CODEFLOW_SUMMARY]` JSON block, **you** synthesize recommendations. Do not parrot the JSON — reason over it.

### Decision logic

Check `isCodeflowPR` first — if `false`, skip all codeflow-specific advice:
- **Not a codeflow PR** (`isCodeflowPR = false` or `flowDirection = "unknown"`): State this clearly. No darc commands, no codeflow recommendations. Treat as a normal PR.

Then read `currentState`:

| State | Action |
|-------|--------|
| `MERGED` | No action needed. Mention Maestro will create a new PR if VMR has newer content. |
| `CLOSED` | Suggest triggering a new PR if `subscriptionId` is available. |
| `NO-OP` | PR has no meaningful changes. Recommend closing or merging to clear state. If `subscriptionId` is available, offer force-trigger as a third option. |
| `IN_PROGRESS` | Someone is actively working. Recommend waiting, then checking back. |
| `STALE` | Needs attention — see warnings below for what's blocking. |
| `ACTIVE` | PR is healthy — check freshness and warnings for nuance. |

Then layer in context from `warnings`, `freshness`, and `commits`:

- **Unresolved conflict** (`warnings.conflictCount > 0`, `conflictMayBeResolved = false`): Lead with "resolve conflicts" using `darc vmr resolve-conflict --subscription <id>`. Offer "close & reopen" as alternative.
- **Conflict may be resolved** (`conflictMayBeResolved = true`): Note the force push post-dates the conflict warning. Suggest verifying, then merging.
- **Staleness warning active** (`stalenessCount > 0`, `stalenessMayBeResolved = false`): Codeflow is blocked. Options: merge as-is, force trigger, or close & reopen.
- **Manual commits present** (`commits.manual > 0`): Warn that force-trigger or close will lose them. If `commits.codeflowLikeManual > 0`, note the freshness gap may be partially covered.
- **Behind on freshness** (`freshness.aheadBy > 0`): Mention the PR is missing updates. If staleness is blocking, a force trigger is needed. Otherwise, Maestro should auto-update.
- **Subscription health** (`subscriptionHealth` array present): Use the `diagnostic` field to explain *why* a backflow PR is missing — don't just say "missing", say whether Maestro is stuck, the subscription is disabled, or the channel is frozen. For `channel-frozen` or `subscription-missing` on preview branches, this is expected — don't alarm the user.

### Darc commands to include

### Remediation Commands

When recommending actions, include the relevant `darc` command with the actual `subscriptionId` from the summary. Be precise about what each command does:

| Command | What it does | When to use |
|---------|-------------|-------------|
| `darc trigger-subscriptions --id <id>` | Normal trigger — only works if subscription isn't stale. Creates a new PR if none exists. | PR was closed, or no PR exists |
| `darc trigger-subscriptions --id <id> --force` | Force trigger — **overwrites the existing PR branch** with fresh VMR content. Does not create a new PR. | PR exists but is stale/no-op and you want to reuse it |
| `darc vmr resolve-conflict --subscription <id>` | Resolve conflicts locally and push to the PR branch | PR has merge conflicts |

> ⚠️ **Common mistake**: Don't say "close then force-trigger" — force-trigger pushes to the *existing* PR. If you close first, use a normal trigger instead (which creates a new PR). The two paths are: (A) force-trigger to refresh the existing PR, or (B) close + normal-trigger to get a new PR.

### Tone

Be direct. Lead with the most important action. Use 2-4 bullet points, not long paragraphs. Include the darc command inline so the user can copy-paste.

## Deep Analysis with SQL

For cross-repo or multi-run analysis, store each `[CODEFLOW_SUMMARY]` JSON in SQL after the script runs:

```sql
CREATE TABLE IF NOT EXISTS codeflow_status (
    id TEXT PRIMARY KEY,           -- e.g., 'dotnet/sdk#52727' or 'dotnet/sdk/main'
    mode TEXT,                     -- 'pr-analysis' or 'flow-health'
    repository TEXT,
    data TEXT,                     -- full JSON summary
    checked_at TEXT DEFAULT (datetime('now'))
);

-- After each script run, parse the JSON and insert:
INSERT OR REPLACE INTO codeflow_status (id, mode, repository, data)
VALUES ('dotnet/sdk/main', 'flow-health', 'dotnet/sdk', '<json>');
```

**When to use**: Checking flow health across multiple repos (run `-CheckMissing` for each, store results, then query). Example:

```sql
-- Find all repos with missing backflow
SELECT repository, json_extract(data, '$.backflow.missing') as missing,
       json_extract(data, '$.buildsAreStale') as builds_stale
FROM codeflow_status WHERE mode = 'flow-health' AND json_extract(data, '$.backflow.missing') > 0;
```

## References

- **VMR codeflow concepts & darc commands**: See [references/vmr-codeflow-reference.md](references/vmr-codeflow-reference.md)
- **VMR build topology & staleness diagnosis**: See [references/vmr-build-topology.md](references/vmr-build-topology.md) — explains how to diagnose widespread backflow staleness by checking VMR build health, the bootstrap chicken-and-egg problem, and the channel/subscription flow
- **Codeflow PR documentation**: [dotnet/dotnet Codeflow-PRs.md](https://github.com/dotnet/dotnet/blob/main/docs/Codeflow-PRs.md)
