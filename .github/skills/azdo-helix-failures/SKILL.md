---
name: azdo-helix-failures
description: Retrieve and analyze test failures from Azure DevOps builds and Helix test runs for dotnet repositories.
---

# Azure DevOps and Helix Failure Analysis

Analyze CI test failures in Azure DevOps and Helix for dotnet repositories (runtime, sdk, aspnetcore, roslyn, and more).

## When to Use This Skill

Use this skill when:
- Investigating CI failures or checking why a PR's tests are failing
- Debugging Helix test issues or analyzing build errors
- Given URLs containing `dev.azure.com`, `helix.dot.net`, or GitHub PR links with failing checks
- Asked questions like "why is this PR failing", "analyze the CI failures", or "what's wrong with this build"

## Quick Start

**Note:** Examples use relative paths from the skill directory (`.github/skills/azdo-helix-failures/`).

```powershell
# Analyze PR failures (most common) - defaults to dotnet/runtime
./scripts/Get-HelixFailures.ps1 -PRNumber 123445 -ShowLogs

# Analyze by build ID
./scripts/Get-HelixFailures.ps1 -BuildId 1276327 -ShowLogs

# Query specific Helix work item
./scripts/Get-HelixFailures.ps1 -HelixJob "4b24b2c2-..." -WorkItem "System.Net.Http.Tests"

# Other dotnet repositories
./scripts/Get-HelixFailures.ps1 -PRNumber 12345 -Repository "dotnet/aspnetcore"
./scripts/Get-HelixFailures.ps1 -PRNumber 67890 -Repository "dotnet/sdk"
./scripts/Get-HelixFailures.ps1 -PRNumber 11111 -Repository "dotnet/roslyn"
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
3. **Separates canceled jobs from failed jobs** (canceled = dependency failures)
4. Extracts Helix work item failures
5. Fetches console logs (with `-ShowLogs`)
6. Searches for known issues with "Known Build Error" label
7. Correlates failures with PR changes
8. **Provides smart retry recommendations**

## Interpreting Results

**Known Issues section**: Failures matching existing GitHub issues - these are tracked and being investigated.

**Canceled jobs**: Jobs that were canceled (not failed) due to earlier stage failures or timeouts. These don't need separate investigation.

**PR Change Correlation**: Files changed by PR appearing in failures - likely PR-related.

**Build errors**: Compilation failures need code fixes.

**Helix failures**: Test failures on distributed infrastructure.

**Local test failures**: Some repos (e.g., dotnet/sdk) run tests directly on build agents. These can also match known issues - search for the test name with the "Known Build Error" label.

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
5. **Interpret patterns**:
   - Same error across many jobs → Real code issue
   - Device failures (iOS/Android/tvOS) → Often transient infrastructure
   - Docker/container image pull failures → Infrastructure issue
   - Network timeouts, "host not found" → Transient infrastructure
   - Test timeout but tests passed → Executor issue, not test failure

## References

- **Helix artifacts & binlogs**: See [references/helix-artifacts.md](references/helix-artifacts.md)
- **Manual investigation steps**: See [references/manual-investigation.md](references/manual-investigation.md)
- **AzDO/Helix details**: See [references/azdo-helix-reference.md](references/azdo-helix-reference.md)

## Tips

1. Read PR description and comments first for context
2. Check if same test fails on main branch before assuming transient
3. Look for `[ActiveIssue]` attributes for known skipped tests
4. Use `-SearchMihuBot` for semantic search of related issues
5. Binlogs in artifacts help diagnose MSB4018 task failures
