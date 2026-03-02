# Batch PR Analysis Workflow

## Overview

dotnet/runtime typically has several hundred open PRs. The PowerShell script
`Get-PrTriageData.ps1` handles all batch analysis in a single run:

1. Fetches PR list via `gh pr list` (1 REST call)
2. Quick-screens out drafts, bots, stale, needs-author-action
3. Fetches reviews, threads, and Build Analysis via batched GraphQL (10 PRs per call)
4. Scores all 12 dimensions and determines next action + who
5. Outputs sorted JSON

**Performance**: ~2.5 seconds per 10 PRs + ~5s overhead. Full repo (300+ PRs) = 60-90 seconds.

## Default Exclusions

The script excludes by default (toggles available to include):

| Excluded | Why | Include flag |
|----------|-----|-------------|
| Drafts | Not ready for review | `-IncludeDrafts` |
| `dotnet-maestro` / `github-actions` bots | Automated dependency flow | (always excluded) |
| `needs-author-action` label | Ball is in author's court | `-IncludeNeedsAuthor` |
| `no-recent-activity` label | At risk of auto-close | `-IncludeStale` |

`copilot-swe-agent` PRs are **included by default** — they are maintainer-initiated.
Use `-ExcludeCopilot` to exclude them.

## GraphQL Batching

A single GraphQL query fetches reviews, review threads, and Build Analysis
(via `statusCheckRollup`) for up to 10 PRs at once:

```graphql
{
  repository(owner: "dotnet", name: "runtime") {
    pr0: pullRequest(number: 12345) {
      number
      comments { totalCount }
      reviews(last: 10) { nodes { author { login } state } }
      reviewThreads(first: 50) { nodes { isResolved comments(first: 5) { nodes { author { login } } } } }
      commits(last: 1) { nodes { commit { statusCheckRollup { contexts(first: 100) { nodes { ... on CheckRun { name conclusion status } } } } } } }
    }
    pr1: pullRequest(number: 12346) { ... }
  }
}
```

**Constraint**: `contexts(first: 100)` is the GraphQL max. PRs with >100 checks
may not include Build Analysis in the first page (rare).

## Caching in SQL

After a batch run, the LLM should load results into a SQL table so follow-up
questions don't require re-running the script. See SKILL.md Step 4 for the schema.

## API Budget

| Candidates | GraphQL calls | Total time |
|-----------|--------------|------------|
| 10 PRs | 1 | ~8s |
| 50 PRs | 5 | ~18s |
| 100 PRs | 10 | ~30s |
| 300 PRs | 30 | ~80s |

GitHub GraphQL limit: 5,000 points/hour. Even 300 PRs is well within limits.

## Anti-Patterns

> ❌ Don't try to analyze PRs one-by-one with separate `gh` calls — use the script
> ❌ Don't hold all PR data in LLM context — use SQL table for follow-ups
> ❌ Don't re-run the script for follow-up queries — query the SQL cache instead
