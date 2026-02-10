---
name: vmr-codeflow-status
description: Analyze VMR codeflow PR status for dotnet repositories. Use when investigating stale codeflow PRs, checking if fixes have flowed through the VMR pipeline, debugging dependency update issues in PRs authored by dotnet-maestro[bot], checking overall flow status for a repo, or diagnosing why backflow PRs are missing or blocked.
---

# VMR Codeflow Status

Analyze the health of VMR codeflow PRs in both directions:
- **Backflow**: `dotnet/dotnet` → product repos (e.g., `dotnet/sdk`)
- **Forward flow**: product repos → `dotnet/dotnet`

> 🚨 **NEVER** use `gh pr review --approve` or `--request-changes`. Only `--comment` is allowed. Approval and blocking are human-only actions.

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
1. **Parses PR metadata** — Extracts VMR commit, subscription ID, build info from PR body
2. **Validates snapshot** — Cross-references PR body commit against branch commit messages to detect stale metadata
3. **Checks VMR freshness** — Compares PR's VMR snapshot against current VMR branch HEAD
4. **Shows pending forward flow** — For behind backflow PRs, finds open forward flow PRs that would close part of the gap
5. **Detects staleness & conflicts** — Finds Maestro "codeflow cannot continue" warnings and "Conflict detected" messages with file lists and resolve commands
6. **Analyzes PR commits** — Categorizes as auto-updates vs manual commits
7. **Traces fixes** (with `-TraceFix`) — Checks if a specific fix has flowed through VMR → codeflow PR
8. **Recommends actions** — Suggests force trigger, close/reopen, merge as-is, resolve conflicts, or wait

### Flow Health Mode (`-CheckMissing`)
1. **Checks official build freshness** — Queries `aka.ms` shortlinks for latest published VMR build dates per channel
2. **Scans backflow PRs** — Finds branches where a backflow PR should exist but doesn't, and checks health of open PRs (conflict/staleness/resolved status)
3. **Scans forward flow** — Checks open forward flow PRs into `dotnet/dotnet` for staleness and conflicts
4. **Produces summary** — Counts healthy/blocked/missing PRs across both directions

> ❌ **Never assume "Unknown" health means healthy.** When `gh` API calls fail (auth, rate limiting), the script returns "Unknown" status — this is explicitly excluded from healthy/covered counts.

> ⚠️ **aka.ms redirect behavior**: 301 is expected and treated as a valid product URL (→ ci.dot.net). Non-301 redirects (often 302, which goes to Bing) indicate an invalid URL. The script only accepts 301.

## Interpreting Results

### Freshness
- **✅ Up to date**: PR has the latest VMR snapshot
- **⚠️ VMR is N commits ahead**: The PR is missing updates. Check if the missing commits contain the fix you need.
- **📊 Forward flow coverage**: Shows how many missing repos have pending forward flow PRs that would close part of the gap once merged.

### Snapshot Validation
- **✅ Match**: PR body commit matches the branch's actual "Backflow from" commit
- **⚠️ Mismatch**: PR body is stale — the script automatically uses the branch-derived commit for freshness checks
- **ℹ️ Initial commit only**: PR body can't be verified yet (no "Backflow from" commit exists)

### Staleness & Conflicts
- **✅ No warnings**: Maestro can freely update the PR
- **⚠️ Staleness warning**: A forward flow merged while this backflow PR was open. Maestro blocked further updates.
- **🔴 Conflict detected**: Maestro found merge conflicts. Shows conflicting files and `darc vmr resolve-conflict` command.

### Manual Commits
Manual commits on the PR branch are at risk if the PR is closed or force-triggered. The script lists them so you can decide whether to preserve them.

### Fix Tracing
When using `-TraceFix`:
- **✅ Fix is in VMR manifest**: The fix has flowed to the VMR
- **✅ Fix is in PR snapshot**: The codeflow PR already includes this fix
- **❌ Fix is NOT in PR snapshot**: The PR needs a codeflow update to get this fix

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

## References

- **VMR codeflow concepts**: See [references/vmr-codeflow-reference.md](references/vmr-codeflow-reference.md)
- **VMR build topology & staleness diagnosis**: See [references/vmr-build-topology.md](references/vmr-build-topology.md) — explains how to diagnose widespread backflow staleness by checking VMR build health, the bootstrap chicken-and-egg problem, and the channel/subscription flow
- **Codeflow PR documentation**: [dotnet/dotnet Codeflow-PRs.md](https://github.com/dotnet/dotnet/blob/main/docs/Codeflow-PRs.md)
