---
name: vmr-codeflow-status
description: Analyze VMR codeflow PR status for dotnet repositories. Use when investigating stale codeflow PRs, checking if fixes have flowed through the VMR pipeline, or debugging dependency update issues in PRs authored by dotnet-maestro[bot].
---

# VMR Codeflow Status

Analyze the health of VMR codeflow PRs (backflow from `dotnet/dotnet` to product repositories like `dotnet/sdk`).

## When to Use This Skill

Use this skill when:
- A codeflow PR (from `dotnet-maestro[bot]`) has failing tests and you need to know if it's stale
- You need to check if a specific fix has flowed through the VMR pipeline to a codeflow PR
- A PR has a Maestro staleness warning ("codeflow cannot continue")
- You need to understand what manual commits would be lost if a codeflow PR is closed
- Asked questions like "is this codeflow PR up to date", "has the runtime revert reached this PR", "why is the codeflow blocked"

## Quick Start

```powershell
# Check codeflow PR status (most common)
./scripts/Get-CodeflowStatus.ps1 -PRNumber 52727 -Repository "dotnet/sdk"

# Trace a specific fix through the pipeline
./scripts/Get-CodeflowStatus.ps1 -PRNumber 52727 -Repository "dotnet/sdk" -TraceFix "dotnet/runtime#123974"

# Show individual VMR commits that are missing
./scripts/Get-CodeflowStatus.ps1 -PRNumber 52727 -Repository "dotnet/sdk" -ShowCommits

# Check if any backflow PRs are missing for a repo
./scripts/Get-CodeflowStatus.ps1 -Repository "dotnet/roslyn" -CheckMissing

# Check a specific branch only
./scripts/Get-CodeflowStatus.ps1 -Repository "dotnet/sdk" -CheckMissing -Branch "main"
```

## Key Parameters

| Parameter | Description |
|-----------|-------------|
| `-PRNumber` | GitHub PR number to analyze (required unless `-CheckMissing`) |
| `-Repository` | Target repo in `owner/repo` format (default: `dotnet/sdk`) |
| `-TraceFix` | Trace a repo PR through the pipeline (e.g., `dotnet/runtime#123974`) |
| `-ShowCommits` | Show individual VMR commits between PR snapshot and branch HEAD |
| `-CheckMissing` | Check if backflow PRs are expected but missing for a repository |
| `-Branch` | With `-CheckMissing`, only check a specific branch |

## What the Script Does

1. **Parses PR metadata** — Extracts VMR commit, subscription ID, build info from PR body
2. **Checks VMR freshness** — Compares PR's VMR snapshot against current VMR branch HEAD
3. **Detects staleness** — Finds Maestro "codeflow cannot continue" warnings
4. **Analyzes PR commits** — Categorizes as auto-updates vs manual commits
5. **Traces fixes** (optional) — Checks if a specific fix has flowed through VMR → codeflow PR
6. **Recommends actions** — Suggests force trigger, close/reopen, merge as-is, or wait
7. **Checks for missing backflow** (optional) — Finds branches where a backflow PR should exist but doesn't

## Interpreting Results

### Freshness
- **✅ Up to date**: PR has the latest VMR snapshot
- **⚠️ VMR is N commits ahead**: The PR is missing updates. Check if the missing commits contain the fix you need.

### Staleness
- **✅ No staleness warnings**: Maestro can freely update the PR
- **⚠️ Staleness warning detected**: A forward flow merged while this backflow PR was open. Maestro blocked further updates.

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
darc vmr resolve --subscription <subscription-id> --build <bar-build-id>
```

Install darc via `eng\common\darc-init.ps1` in any arcade-enabled repository.

## References

- **VMR codeflow concepts**: See [references/vmr-codeflow-reference.md](references/vmr-codeflow-reference.md)
- **Codeflow PR documentation**: [dotnet/dotnet Codeflow-PRs.md](https://github.com/dotnet/dotnet/blob/main/docs/Codeflow-PRs.md)
