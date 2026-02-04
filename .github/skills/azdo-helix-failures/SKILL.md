---
name: azdo-helix-failures
description: Retrieve and analyze test failures from Azure DevOps builds and Helix test runs. Use this when investigating CI failures, checking why a PR's tests are failing, debugging Helix test issues, analyzing build errors, or triaging dotnet/runtime test failures. Triggers include questions like "why is this PR failing", "analyze the CI failures", "what's wrong with this build", or any URL containing dev.azure.com, helix.dot.net, or GitHub PR links with failing checks.
---

# Azure DevOps and Helix Failure Analysis

Analyze CI test failures in Azure DevOps and Helix for dotnet repositories.

## Quick Start

```powershell
# Analyze PR failures (most common)
scripts/Get-HelixFailures.ps1 -PRNumber 123445 -ShowLogs

# Analyze by build ID
scripts/Get-HelixFailures.ps1 -BuildId 1276327 -ShowLogs

# Query specific Helix work item
scripts/Get-HelixFailures.ps1 -HelixJob "4b24b2c2-..." -WorkItem "iOS.Device.Aot.Test"

# Other repositories
scripts/Get-HelixFailures.ps1 -PRNumber 12345 -Repository "dotnet/aspnetcore"
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

## What the Script Does

1. Fetches Build Analysis for known issues
2. Gets failed jobs from Azure DevOps timeline
3. Extracts Helix work item failures
4. Fetches console logs (with `-ShowLogs`)
5. Searches for known issues with "Known Build Error" label
6. Correlates failures with PR changes

## Interpreting Results

**Known Issues section**: Failures matching existing GitHub issues - likely transient/infrastructure.

**PR Change Correlation**: Files changed by PR appearing in failures - likely PR-related.

**Build errors**: Compilation failures need code fixes.

**Helix failures**: Test failures on distributed infrastructure.

## Analysis Workflow

1. **Read PR context first** - Check title, description, comments
2. **Run the script** with `-ShowLogs` for detailed failure info
3. **Check Build Analysis** - Known issues are safe to retry
4. **Correlate with PR changes** - Same files failing = likely PR-related
5. **Interpret patterns**:
   - Same error across many jobs → Real code issue
   - iOS/Android device failures → Often transient infrastructure
   - Docker image pull failures → Infrastructure, check known issues

## References

- **Manual investigation steps**: See [references/manual-investigation.md](references/manual-investigation.md)
- **AzDO/Helix details**: See [references/azdo-helix-reference.md](references/azdo-helix-reference.md)

## Tips

1. Read PR description and comments first for context
2. Check if same test fails on main branch before assuming transient
3. Look for `[ActiveIssue]` attributes for known skipped tests
4. Use `-SearchMihuBot` for semantic search of related issues
5. Binlogs in artifacts help diagnose MSB4018 task failures
