---
name: vmr-codeflow-status
description: Analyze VMR codeflow PR status for dotnet repositories. Use when investigating stale codeflow PRs, checking if fixes have flowed through the VMR pipeline, debugging dependency update issues in PRs authored by dotnet-maestro[bot], checking overall flow status for a repo, or diagnosing why backflow PRs are missing or blocked.
---

# VMR Codeflow Status

Analyze the health of VMR codeflow PRs in both directions:
- **Backflow**: `dotnet/dotnet` → product repos (e.g., `dotnet/sdk`)
- **Forward flow**: product repos → `dotnet/dotnet`

> 🚨 **NEVER** use `gh pr review --approve` or `--request-changes`. Only `--comment` is allowed. Approval and blocking are human-only actions.

**Workflow**: Run the script → read the human-readable output + `[CODEFLOW_SUMMARY]` JSON → synthesize recommendations yourself. The script collects data; you generate the advice.

## Prerequisites

- **GitHub CLI (`gh`)** — must be installed and authenticated (`gh auth login`)
- Run scripts **from the skill directory** or use the full path to the script

## When to Use This Skill

Use this skill when:
- A codeflow PR (from `dotnet-maestro[bot]`) has failing tests and you need to know if it's stale
- You need to check if a specific fix has flowed through the VMR pipeline to a codeflow PR
- A PR has a Maestro staleness warning ("codeflow cannot continue") or conflict
- You need to understand what manual commits would be lost if a codeflow PR is closed
- You want to check the overall state of flow for a repo (backflow and forward flow health)
- You need to know why backflow PRs are missing or when the last VMR build was published
- You're asked questions like "is this codeflow PR up to date", "has the runtime revert reached this PR", "why is the codeflow blocked", "what is the state of flow for the sdk", "what's the flow status for net11"

## Two Modes

| Mode | Use When | Required Params |
|------|----------|-----------------|
| **PR analysis** | Investigating a specific codeflow PR | `-PRNumber` (and optionally `-Repository`) |
| **Flow health** (`-CheckMissing`) | Checking overall repo flow status | `-CheckMissing` (optional: `-Repository`, `-Branch`) |

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
| `-Repository` | No | `dotnet/sdk` | Target repo in `owner/repo` format |
| `-TraceFix` | No | — | Trace a repo PR through the pipeline. Format: `owner/repo#number` (e.g., `dotnet/runtime#123974`) |
| `-ShowCommits` | No | `$false` | Show individual VMR commits between PR snapshot and branch HEAD |
| `-CheckMissing` | No | `$false` | Check overall flow health: missing backflow PRs, forward flow status, and official build freshness |
| `-Branch` | No | — | With `-CheckMissing`, only check a specific branch (e.g., `main`, `release/10.0`) |

## What the Script Does

### PR Analysis Mode (default)

> **Design principle**: Assess current state from primary signals first, then use Maestro comments as historical context — not the other way around. Comments tell you the history, not the present.

1. **PR Overview** — Basic PR info, flow direction (backflow vs forward flow)
2. **Current State** — Independent assessment from primary signals: empty diff, force pushes, merge status. Produces a one-line verdict (NO-OP / IN PROGRESS / STALE / ACTIVE) before reading any comments
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

### Fix Tracing
When using `-TraceFix`:
- **✅ Fix is in VMR manifest**: The fix has flowed to the VMR
- **✅ Fix is in PR snapshot**: The codeflow PR already includes this fix
- **❌ Fix is NOT in PR snapshot**: The PR needs a codeflow update to get this fix

## Generating Recommendations

After the script outputs the `[CODEFLOW_SUMMARY]` JSON block, **you** synthesize recommendations. Do not parrot the JSON — reason over it.

### Decision logic

Read `currentState` first:

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

### Darc commands to include

When recommending actions, include the relevant `darc` command with the actual `subscriptionId` from the summary:

```
darc trigger-subscriptions --id <subscriptionId>           # normal trigger
darc trigger-subscriptions --id <subscriptionId> --force   # force trigger (overwrites PR)
darc vmr resolve-conflict --subscription <subscriptionId>  # resolve conflicts locally
```

### Tone

Be direct. Lead with the most important action. Use 2-4 bullet points, not long paragraphs. Include the darc command inline so the user can copy-paste.

## Darc Commands for Remediation

After analyzing the codeflow status, common next steps involve `darc` commands:

```bash
# Force trigger the subscription to get a fresh codeflow update
darc trigger-subscriptions --id <subscription-id> --force

# Normal trigger (only works if not stale)
darc trigger-subscriptions --id <subscription-id>

# Check subscription details
darc get-subscriptions --target-repo dotnet/sdk --source-repo dotnet/dotnet

# Get BAR build details
darc get-build --id <bar-build-id>

# Resolve codeflow conflicts locally
darc vmr resolve-conflict --subscription <subscription-id>
```

Install darc via `eng\common\darc-init.ps1` in any arcade-enabled repository.

### When the script reports "Maestro may be stuck"

When the script shows a missing backflow PR with "Maestro may be stuck" (builds are fresh but no PR was created), follow these diagnostic steps:

1. **Check the subscription** to find when it last consumed a build:
   ```bash
   darc get-subscriptions --target-repo <repo> --source-repo dotnet/dotnet
   ```
   Look at the `Last Build` field — if it's weeks old while the channel has newer builds, the subscription is stuck.

2. **Compare against the latest channel build** to confirm the gap:
   ```bash
   darc get-latest-build --repo dotnet/dotnet --channel "<channel-name>"
   ```
   Channel names follow patterns like `.NET 11.0.1xx SDK`, `.NET 10.0.1xx SDK`, `.NET 11.0.1xx SDK Preview 1`.

3. **Trigger the subscription manually** to unstick it:
   ```bash
   darc trigger-subscriptions --id <subscription-id>
   ```

4. **If triggering doesn't produce a PR within a few minutes**, the issue may be deeper — check Maestro health or open an issue on `dotnet/arcade`.

## References

- **VMR codeflow concepts**: See [references/vmr-codeflow-reference.md](references/vmr-codeflow-reference.md)
- **VMR build topology & staleness diagnosis**: See [references/vmr-build-topology.md](references/vmr-build-topology.md) — explains how to diagnose widespread backflow staleness by checking VMR build health, the bootstrap chicken-and-egg problem, and the channel/subscription flow
- **Codeflow PR documentation**: [dotnet/dotnet Codeflow-PRs.md](https://github.com/dotnet/dotnet/blob/main/docs/Codeflow-PRs.md)
