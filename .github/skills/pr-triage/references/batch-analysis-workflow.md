# Batch PR Analysis Workflow

## Overview

dotnet/runtime has ~437 open PRs (252 human non-draft). Analyzing all is impractical. Use a **two-pass funnel**:

- **Pass 1**: Cheap filters via `gh pr list` (1 API call)
- **Pass 2**: Detailed analysis via parallel subagents (N calls)

## Available Filters

| Filter | Example trigger | Implementation |
|--------|----------------|---------------|
| Limit N (default 20) | "triage top 10 PRs" | `--limit N` |
| Area label | "triage area-System.Net PRs" | `--label area-System.Net` |
| Author | "triage PRs by stephentoub" | `--author stephentoub` |
| Exclude drafts (default on) | — | Filter `isDraft: false` |
| Exclude bots (default on) | — | Filter out `app/dotnet-maestro`, `app/copilot-swe-agent` |
| Exclude `needs-author-action` | "show PRs ready for maintainer" | Filter out label |
| Exclude `no-recent-activity` (default on) | — | Filter out label |
| Recently updated | "triage PRs updated this week" | `--sort updated` + date filter |
| Date range | "triage PRs opened in last 30 days" | Filter by `createdAt` or `updatedAt` |
| Community PRs | "triage community contributions" | `--label community-contribution` |
| Has maintainer approval | "show approved PRs" | Pass 2 filter: check reviews |
| Green Build Analysis | "show PRs with passing CI" | Pass 2 filter: check Build Analysis |
| Small/localized | "triage simple PRs" | Pass 2 filter: changedFiles, additions, deletions |

## Pass 1 — Quick-Screen

```bash
gh pr list --repo dotnet/runtime --state open --limit 100 \
  --json number,title,author,labels,isDraft,mergeable,createdAt,updatedAt,changedFiles,additions,deletions
```

Apply default exclusions:

1. Remove `isDraft: true`
2. Remove bot authors (`app/dotnet-maestro`) — but **include** `app/copilot-swe-agent` (invoked by maintainers, treat as normal PRs)
3. Remove PRs with `needs-author-action` label (unless user explicitly includes them)
4. Remove PRs with `no-recent-activity` label (unless user explicitly includes them)
5. Remove PRs with `CONFLICTING` mergeable state

Apply user filters (area, author, date range).

Store results in SQL:

```sql
CREATE TABLE pr_candidates (
  number INTEGER PRIMARY KEY,
  title TEXT,
  author TEXT,
  area_label TEXT,
  is_community INTEGER,
  mergeable TEXT,
  updated_at TEXT,
  changed_files INTEGER,
  additions INTEGER,
  deletions INTEGER,
  pass1_status TEXT DEFAULT 'pending',
  pass2_status TEXT DEFAULT 'pending',
  score REAL DEFAULT 0,
  next_action TEXT,
  blockers TEXT
);
```

Cap at user-specified limit (default 20) after filtering.

## Pass 2 — Detailed Analysis

### Step 2a: Batched GraphQL Fetch (ALL cheap dimensions in one call)

A single GraphQL query can fetch reviews, review threads, AND Build Analysis
(via `statusCheckRollup`) for up to 10 PRs at once. This replaces ~150
sequential REST calls with ~5 GraphQL calls for a 50-PR batch.

**Tested: 10 PRs in 2.5 seconds. 46 PRs in ~15 seconds total.**

```bash
gh api graphql -f query='
{
  repository(owner: "dotnet", name: "runtime") {
    pr0: pullRequest(number: 12345) {
      number
      reviews(last: 10) { nodes { author { login } state } }
      reviewThreads(first: 50) { nodes { isResolved } }
      commits(last: 1) {
        nodes {
          commit {
            statusCheckRollup {
              contexts(first: 100) {
                nodes {
                  ... on CheckRun { name conclusion status }
                }
              }
            }
          }
        }
      }
    }
    pr1: pullRequest(number: 12346) {
      # same fragment as above
    }
    # ... up to 10 aliases per call
  }
}
'
```

From the response, extract per-PR:
- **Build Analysis**: find `contexts.nodes` where `name == "Build Analysis"`,
  read `conclusion` (SUCCESS/FAILURE/null) and `status` (COMPLETED/IN_PROGRESS)
- **Reviews**: `reviews.nodes` → author login + state (APPROVED/CHANGES_REQUESTED/COMMENTED)
- **Unresolved threads**: count `reviewThreads.nodes` where `isResolved == false`
- **Check counts**: count nodes by conclusion/status for passed/failed/running

**Constraint**: `contexts(first: 100)` is the GraphQL max. PRs with >100 checks
may not include Build Analysis in the first page. If Build Analysis is not found,
fall back to a single `gh pr checks {N} --repo dotnet/runtime --json name,state`
call for that PR only.

### Step 2b: Fallback CI Check Retrieval

Only needed if Build Analysis was not found in the GraphQL response (rare):

```bash
gh pr checks {NUMBER} --repo dotnet/runtime --json name,state \
  | jq '[.[] | select(.name == "Build Analysis")] | .[0] // {"name":"Build Analysis","state":"ABSENT"}'
```

### Step 2c: Two-Tier Dimension Evaluation

**Tier A (cheap — evaluate for ALL candidates):** dimensions 1–7, 9–13.
These use data already fetched from `gh pr list`, batched GraphQL, and
`gh pr checks`.

**Tier B (medium cost — evaluate only for top ~15 after Tier A scoring):**
dimensions 8 (context drift) and 14 (author familiarity). Each requires
per-directory API calls:

```bash
# Context drift — only for top candidates
gh api "repos/dotnet/runtime/commits?path={dir}&since={updatedAt}&per_page=10"

# Author familiarity — only for top candidates
gh api "repos/dotnet/runtime/commits?author={author}&path={dir}&per_page=5"
```

Score all candidates on Tier A first, rank, then evaluate Tier B only on the
top N. This avoids ~2 API calls per PR per directory for the bottom half.

### Step 2d: Parallel Subagent Option

For large candidate sets (>15 PRs), the orchestrating agent can split work
across parallel subagents. Each subagent handles a batch of 8–10 PRs.

Use a **fast model** (e.g., `claude-haiku-4.5` or `gpt-5-mini`) for scoring
subagents — dimension scoring is mechanical (match data against thresholds)
and doesn't need heavy reasoning. Reserve the full model for the final
ranking summary and next-action recommendations.

```
Subagent 1 (haiku): PRs [#101, #102, ..., #108] — score dimensions, return JSON
Subagent 2 (haiku): PRs [#109, #110, ..., #116] — score dimensions, return JSON
Subagent 3 (haiku): PRs [#117, #118, ..., #124] — score dimensions, return JSON
```

Launch with `mode: "background"`, collect results, then rank and format.

### Step 2e: Update SQL

After analysis, update the SQL table:

```sql
UPDATE pr_candidates SET
  pass2_status = 'done',
  score = ?,
  next_action = ?,
  blockers = ?
WHERE number = ?;
```

## Scoring and Ranking

Query final results:

```sql
SELECT number, title, author, score, next_action, blockers
FROM pr_candidates
WHERE pass2_status = 'done'
ORDER BY score DESC
LIMIT ?;
```

## Output Format

Present as a ranked table:

```markdown
## Merge Readiness: {filter description} ({count} PRs analyzed)

| # | PR | Author | Score | Next Action | Key Blockers |
|---|----|--------|-------|-------------|-------------|
| 1 | [#12345](url) | @user | 14/16 | Ready to merge ✅ | — |
| 2 | [#12346](url) | @user2 | 12/16 | Maintainer review needed | No area-owner review |
| 3 | [#12347](url) | @user3 | 10/16 | Fix CI failures | Build Analysis red |
```

Then detail bullets for any PR the user wants to drill into.

## Performance Notes

- Pass 1 is a single API call regardless of PR count
- Batched GraphQL fetches review threads for 10 PRs per call (not 1 per PR)
- `gh pr checks` output filtered to Build Analysis via `jq` to reduce transfer
- Tier A dimensions (1–7, 9–13) scored first; Tier B (8, 14) only for top ~15
- For >15 candidates, split across parallel subagents (8–10 PRs each)
- SQL tracking prevents re-analyzing the same PR if the user refines filters

### API Budget Estimation

| Candidates | Calls without optimization | Calls with optimization |
|-----------|---------------------------|------------------------|
| 10 PRs | ~50 (10×5) | ~20 |
| 25 PRs | ~125 (25×5) | ~40 |
| 50 PRs | ~250 (50×5) | ~70 |
| 100 PRs | ~500 (100×5) | ~130 |

GitHub REST API limit: 5,000 requests/hour. GraphQL: 5,000 points/hour.
Even 100 PRs is well within limits for a single run.

### Rate Limiting Mitigation

- If approaching limits, reduce Tier B evaluation to top 10 instead of top 15
- Cache `docs/area-owners.md` content once per batch (not per PR)
- Reuse `gh pr list` JSON for dimensions that only need list-level data
- For repeated runs, check SQL table before re-fetching — skip PRs already scored

## Anti-Patterns

> ❌ Don't analyze all 437 PRs — always filter first
> ❌ Don't run expensive signals (context drift, author familiarity) on all candidates — only top N after initial ranking
> ❌ Don't hold all PR data in context — use SQL table
> ❌ Don't re-fetch data you already have — check SQL first
