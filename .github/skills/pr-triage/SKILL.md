---
name: pr-triage
description: >
  Triage open PRs for merge readiness in dotnet/runtime. Determine next action for each PR
  and rank PRs by how close they are to merging.
  USE FOR: "which PRs are ready to merge", "what's blocking PR #X", "triage open PRs",
  "next action for PR", "show merge-ready PRs", "triage area-X PRs", "show approved PRs",
  "triage community contributions", "PR merge readiness".
  DO NOT USE FOR: CI failure deep-dive (use ci-analysis skill), code review for correctness
  (use code-review skill), codeflow PR health (use vmr-codeflow-status skill),
  performance benchmarking (use performance-benchmark skill).
---

# PR Triage Skill

Identify which pull requests in dotnet/runtime are ready to merge and determine the next action for each PR. This is a **read-only analysis skill** — it never modifies PRs, issues, labels, or comments.

**Community contributions** are flagged so maintainers can make informed prioritization
decisions. Timely feedback — even a quick "not right now" — respects contributors' time.

## Architecture

All data fetching and scoring is done by a PowerShell script. Your job as the AI is:

1. **Parse** the user's request to determine filters (label, author, limit, etc.)
2. **Run** the script with the right flags
3. **Format** the JSON output as a readable table
4. **Annotate** with any additional context the user asked for

The script handles: batched GraphQL, Build Analysis extraction, review/thread parsing, 12-dimension scoring, next-action determination, and "Who" identification — all in ~15 seconds for 50 PRs.

## When to Use This Skill

- "Which PRs are ready to merge?" / "Triage area-X PRs" / "What's blocking PR #X?"
- "What PRs need my attention?" / "Show approved PRs" / "Triage community contributions"

## Batch Mode (Primary)

### Step 1 — Map User Request to Script Flags

| User says | Script flag |
|-----------|-------------|
| "triage area-CodeGen-coreclr" | `-Label "area-CodeGen-coreclr"` |
| "triage all open PRs" | (no -Label) `-Limit 300` |
| "top 20 PRs" | `-Top 20` |

### Step 2 — Estimate and Run the Script

> ⚠️ **IMPORTANT**: You MUST tell the user the estimated time BEFORE running the script.
> Print the estimate as your first output, then run the script. Never skip this step.

The script is dominated by GraphQL batching at ~2.5 seconds per 10 PRs, plus ~5s overhead.
Note that `-MyActions` and `-NextAction` are post-filters — the script still scans all
PRs matching the pre-fetch filters, so use the pre-fetch count for the estimate.

| Query type | Typical PRs scanned | Estimated time |
|-----------|-------------|---------------|
| Single PR (`-PrNumber`) | 1 | ~5 seconds |
| `-Label area-X` | 10-60 | 10-20 seconds |
| `-Label area-X` + `-MyActions` | 10-60 (same scan) | 10-20 seconds |
| Full repo (`-Limit 500`) | ~300 | 60-90 seconds |
| Full repo + any post-filter | ~300 (same scan) | 60-90 seconds |

Example: *"This will scan ~300 PRs and filter to your action items — should take about 60-90 seconds."*

```powershell
.\.github\skills\pr-triage\scripts\Get-PrTriageData.ps1 -Label "area-CodeGen-coreclr"
```

The script outputs JSON with this structure:

```json
{
  "timestamp": "...",
  "repo": "dotnet/runtime",
  "filters": { "label": "area-CodeGen-coreclr", "author": null, "top": 0, "..." : "..." },
  "scanned": 58,
  "analyzed": 46,
  "returned": 46,
  "screened_out": { "drafts_count": 12, "drafts": ["...first 10..."], "bots": [...], "needs_author_action": [...], "stale": [...] },
  "quick_actions": { "ready_to_merge": 1, "needs_maintainer_review": 10, "needs_author_action": 35, "blocked_conflicts": 9 },
  "prs": [
    {
      "number": 123546, "title": "...", "author": "jonathandavies-arm",
      "score": 8.2, "ci": "SUCCESS", "ci_detail": "91/2/0",
      "unresolved_threads": 0, "total_threads": 1, "total_comments": 3, "distinct_commenters": 2,
      "mergeable": "MERGEABLE",
      "approval_count": 1, "is_community": true,
      "age_days": 35, "days_since_update": 2, "changed_files": 3, "lines_changed": 45,
      "next_action": "Ready to merge",
      "who": "@EgorBo",
      "blockers": "—",
      "why": "CI:pass, OwnerApproved, Small"
    }
  ]
}
```

### Step 3 — Format and Annotate

Format the JSON as a clean markdown table with a brief summary and observations.
This is a cheap LLM pass — the JSON is small and the output is a formatted table plus
a few sentences. Use `ci_detail` as `pass/fail/run` and map `ci` to emoji.

**Column order:** Score | PR | Title | Who | Next Action | CI | Disc | Age | Updated | Files | Author (last)

**PR column:** Always hyperlink: `[#12345](https://github.com/{repo}/pull/12345)`

**CI emoji:** SUCCESS→✅, FAILURE→❌, IN_PROGRESS→⏳, ABSENT→⚠️

**Discussion column:** Show as `{total_threads}t/{distinct_commenters}p` (threads/people).
Heavy discussion (>15t or >5p) is a signal the PR is expensive to push forward.

Example output:

```markdown
## Merge Readiness: area-CodeGen-coreclr
Scanned 58 → 46 analyzed (12 drafts excluded)

| Score | PR | Title | Who | Next Action | CI | Disc | Age | Updated | Files | Author |
|------:|---:|-------|-----|-------------|----|-----:|----:|--------:|------:|--------|
| 8.4 | [#123546](https://github.com/dotnet/runtime/pull/123546) | arm64: Remove widening casts before truncating | @EgorBo | Ready to merge ✅ | ✅ 91/2/0 | 1t/2p | 5d | 1d | 3 | @jonathandavies-arm |
| 7.9 | [#122485](https://github.com/dotnet/runtime/pull/122485) | [RISC-V] Enable instruction printing | @SkyShield | Author: merge main (stale 17d) | ✅ 91/2/0 | 1t/2p | 24d | 17d | 2 | @SkyShield |
| ... | | | | | | | | | | |

### Summary
- **Ready to merge**: 1 — #123546 (approved by @EgorBo, CI green, no threads)
- **Needs maintainer review**: 10 PRs — mostly awaiting @JulieLeeMSFT / @BruceForstall
- **Needs author action**: 35 PRs
- **Blocked by conflicts**: 9 PRs

### Observations
- #124846 has 38 review threads from 5 people — heavy discussion, will need significant effort to land
- 9 PRs have merge conflicts; most are older community contributions (>100 days)
- @amanasifkhalid has 6 open PRs, several with conflicts — may benefit from a triage pass
- #124953 by @akoeplinger is a simple test cleanup (0 threads, CI green) — quick win for a reviewer
```

**Key guidelines for observations:**
- Keep to 3-5 bullet points — highlight what's actionable
- Call out quick wins (high score, no threads, just needs a reviewer click)
- Call out expensive PRs (heavy discussion, many conflicts, large diffs)
- Note patterns (one author with many stale PRs, cluster of PRs needing same reviewer)
- **For community PRs**: note that they may need more shepherding and may not align with current investment. Frame feedback constructively — even a quick "not right now" respects the contributor's time
- Don't repeat what's already visible in the table

### Step 4 — Cache Results for Follow-up Queries

After a batch run, load the results into a SQL table so follow-up questions don't require
re-running the script. Create the table if it doesn't exist, then load from JSON:

```sql
CREATE TABLE IF NOT EXISTS pr_analysis (
  number INTEGER PRIMARY KEY, title TEXT, author TEXT, score REAL,
  ci TEXT, ci_detail TEXT, unresolved_threads INTEGER, total_threads INTEGER,
  distinct_commenters INTEGER, mergeable TEXT, approval_count INTEGER,
  is_community INTEGER, age_days INTEGER, days_since_update INTEGER,
  changed_files INTEGER, lines_changed INTEGER, next_action TEXT,
  who TEXT, blockers TEXT, why TEXT
);
```

Then parse the JSON `prs` array and INSERT each row. Alternatively, run the script with
`-OutputCsv` to get tab-separated output and load that.

Once loaded, answer follow-up questions with SQL queries — no API calls needed:
- "Which community PRs need review?" → `WHERE is_community = 1 AND next_action LIKE '%review%'`
- "Who has the most open PRs?" → `GROUP BY author ORDER BY COUNT(*) DESC`
- "Show PRs with heavy discussion" → `WHERE total_threads > 15 ORDER BY total_threads DESC`
- "What needs @danmoseley's attention?" → `WHERE who LIKE '%danmoseley%'`

### Step 5 — Sentiment Check for Top PRs (Optional)

For the top 3-5 PRs in the results (highest score), fetch recent comments to assess
reviewer sentiment. This adds color the score can't capture — e.g., "LGTM ship it" vs
"approved but I have concerns about the approach".

```bash
gh pr view {number} --repo dotnet/runtime --json comments --jq '.comments[-5:][] | .author.login + ": " + .body[:200]'
```

Use this to add a brief **sentiment note** in the Observations section, such as:
- "#123546 — @EgorBo said 'looks good, ready to merge' — strong positive signal"
- "#124663 — ongoing design discussion between @tannergooding and author, approval may be conditional"

**Guidelines:**
- Only do this for the top 3-5 PRs — not the full list
- Focus on the most recent 3-5 comments per PR
- Look for: explicit merge-readiness signals, conditional approvals, soft blocks ("let's wait for design review"), or enthusiastic endorsement
- Don't attempt to score or quantify sentiment — just note it in observations
- Skip this step if the user asked for a quick summary or broad overview

### Step 6 — Answer Follow-ups

If the user asks about a specific PR from the results, query the SQL table first.
If they ask for CI failure details, delegate to the **ci-analysis** skill.

## Single-PR Mode

For "what's blocking PR #12345?", run the script and filter to that PR number,
then present the detailed breakdown using all available fields (score, ci, ci_detail,
unresolved_threads, mergeable, approval_count, blockers, why, who).

For deeper analysis (context drift, breaking change risk, perf-sensitive areas),
supplement with:

```bash
gh pr view {number} --repo dotnet/runtime --json files
```

And check the [merge-readiness rubric](references/merge-readiness-rubric.md) for
dimensions 8, 14-16 which require file-level analysis.

## Script Parameters

Map the user's request to these flags. Combine as needed.

### Pre-fetch filters (reduce API calls)

| Parameter | Default | Description | Example user request |
|-----------|---------|-------------|---------------------|
| `-Label` | (none) | Area label filter | "triage area-System.Net PRs" |
| `-Author` | (none) | Filter to specific PR author | "show PRs by @stephentoub" |
| `-Assignee` | (none) | Filter to specific assignee | "PRs assigned to me" |
| `-Limit` | 500 | Max PRs from `gh pr list` | "scan all PRs" → `-Limit 500` |
| `-Repo` | `dotnet/runtime` | Repository | (rarely changed) |

### Inclusion/exclusion toggles

| Parameter | Default | Description | Example user request |
|-----------|---------|-------------|---------------------|
| `-Community` | off | Only community-contribution PRs | "triage community contributions" |
| `-IncludeDrafts` | off | Include draft PRs | "show all PRs including drafts" |
| `-ExcludeCopilot` | off | Exclude copilot-swe-agent PRs | "skip bot PRs" |
| `-IncludeNeedsAuthor` | off | Include needs-author-action PRs | "show everything including needs-author" |
| `-IncludeStale` | off | Include no-recent-activity PRs | "show stale PRs too" |
| `-HasLabel` | (none) | Require a specific label | "show api-approved PRs" |
| `-ExcludeLabel` | (none) | Exclude a specific label | "skip blocked PRs" |

### Age and recency filters

| Parameter | Default | Description | Example user request |
|-----------|---------|-------------|---------------------|
| `-MinAge` | 0 | Minimum PR age in days | "PRs older than 30 days" → `-MinAge 30` |
| `-MaxAge` | 0 | Maximum PR age in days | "PRs less than a week old" → `-MaxAge 7` |
| `-UpdatedWithin` | 0 | Updated within N days | "recently active PRs" → `-UpdatedWithin 7` |

### Post-scoring filters (applied after scoring)

| Parameter | Default | Description | Example user request |
|-----------|---------|-------------|---------------------|
| `-MinApprovals` | 0 | Minimum approval count | "show approved PRs" → `-MinApprovals 1` |
| `-MinScore` | 0 | Minimum score (0-10) | "show PRs scoring 7+" → `-MinScore 7` |
| `-NextAction` | (none) | Filter by action type: `ready`, `review`, `author`, `conflicts`, `ci` | "which are ready to merge?" → `-NextAction ready` |
| `-MyActions` | (none) | Show PRs where this person owns next step | "what needs my attention?" → `-MyActions @danmoseley` |
| `-PrNumber` | (none) | Single PR mode | "what's blocking #12345?" → `-PrNumber 12345` |
| `-Top` | 0 (all) | Return only top N results | "show top 10" → `-Top 10` |
| `-OutputCsv` | off | Tab-separated output for SQL import | (used internally for caching) |

### Example invocations

```powershell
# Triage an area
.\Get-PrTriageData.ps1 -Label "area-CodeGen-coreclr"

# What's ready to merge across the whole repo?
.\Get-PrTriageData.ps1 -Limit 300 -NextAction ready

# Community PRs needing maintainer review
.\Get-PrTriageData.ps1 -Community -NextAction review

# My action items
.\Get-PrTriageData.ps1 -Limit 300 -MyActions "@danmoseley"

# High-scoring PRs updated recently
.\Get-PrTriageData.ps1 -Limit 300 -MinScore 7 -UpdatedWithin 7

# Single PR deep-dive
.\Get-PrTriageData.ps1 -PrNumber 123546

# Old community PRs that are almost ready
.\Get-PrTriageData.ps1 -Community -MinAge 30 -MinScore 5

# Top 15 PRs with at least one approval
.\Get-PrTriageData.ps1 -Limit 300 -MinApprovals 1 -Top 15
```

## Score Dimensions (0-10 scale)

The script scores 12 dimensions with weighted composite (see [rubric](references/merge-readiness-rubric.md)):

| Weight | Dimension | What it measures |
|--------|-----------|-----------------|
| 3.0 | CI (Build Analysis) | Hard blocker — can't merge if BA is red |
| 3.0 | Conflicts | Hard blocker — unmergeable |
| 3.0 | Maintainer Review | Hard blocker — runtime requires owner/triager approval |
| 2.0 | Feedback | Unresolved review threads |
| 2.0 | Approval Strength | Who approved: area owner > triager > community |
| 1.5 | Staleness | Days since last update |
| 1.5 | Discussion Complexity | Thread count and distinct commenters |
| 1.0 | Alignment | Has area label, not untriaged |
| 1.0 | Freshness | Recent activity |
| 1.0 | Size | Smaller = easier to review |
| 0.5 | Community | Flags community PRs for visibility — they need different handling |
| 0.5 | Velocity | Review momentum |

## "My Actions" Filter

When the user asks "what PRs need my attention?", run the script then filter results
where `who` contains their username. The script already identifies the responsible
person(s) for each PR using area ownership, review threads, and approval state.

## Anti-Patterns

> 🚨 **Never approve or request changes on PRs.** Read-only analysis only.

> ❌ **Don't deep-dive CI failures** — delegate to the **ci-analysis** skill.

> ❌ **Don't review code for correctness** — delegate to the **code-review** skill.

> ❌ **Don't modify PRs, issues, labels, or comments.**

> ⚠️ **Do NOT recommend rebase/merge-main just because a build is a few days old.** CI takes 2–3 hours. Only recommend it when build is >14 days old.

## References

- **Scoring details**: [references/merge-readiness-rubric.md](references/merge-readiness-rubric.md)
- **Batch workflow**: [references/batch-analysis-workflow.md](references/batch-analysis-workflow.md)
- **Runtime signals**: [references/runtime-signals.md](references/runtime-signals.md)
