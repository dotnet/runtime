# Merge-Readiness Rubric

Reference for evaluating PRs across 16 merge-readiness dimensions and computing
composite scores for ranking.

Each dimension yields a score:

| Symbol | Meaning | Points |
|--------|---------|--------|
| ✅ | Ready | 1 |
| ⚠️ | Needs attention | 0.5 |
| ❌ | Blocking | 0 |

---

## Per-Dimension Evaluation

### 1. CI Status

**How to check**

```bash
gh pr checks {PR} --repo dotnet/runtime --json name,state
```

Find the **Build Analysis** entry.

**Scoring**

| Score | Condition |
|-------|-----------|
| ✅ 1 | Build Analysis green (all failures matched known issues) |
| ⚠️ 0.5 | Build Analysis pending or no builds yet |
| ❌ 0 | Build Analysis red (unmatched failures) |

**Edge cases**

- No "Build Analysis" check at all → build may not have run → ⚠️

---

### 2. Build Staleness

**How to check**

Compare `headRefOid` from `gh pr view` against build info from `gh pr checks`
timestamps.

**Scoring**

| Score | Condition |
|-------|-----------|
| ✅ 1 | Build ran on current HEAD and within last 3 days |
| ⚠️ 0.5 | Build is 3–14 days old |
| ❌ 0 | Build is >14 days old or ran on a different SHA than current HEAD |

**Next-action guidance**: Do NOT recommend "merge main / rebase" solely because
a build is a few days old. Running CI takes 2–3 hours, so a rebase is costly.
Only recommend rebase when:
- Build is >14 days old, OR
- Context drift (dimension 8) shows significant changes to touched directories, OR
- Build ran on a different SHA than current HEAD

A 7-day-old green build with no context drift is fine — the next action should
be whatever the other blocking dimensions indicate.

**Edge cases**

- PR just pushed, build queued → ⚠️

---

### 3. Maintainer Review

**How to check**

```bash
gh pr view {PR} --repo dotnet/runtime --json reviews
```

Match reviewer logins against the **Approval Authority Levels** defined in [runtime-signals.md](runtime-signals.md#approval-authority-levels):

1. **Area owner/lead** (level 1) — parse `docs/area-owners.md`, match reviewer login against Owners column for the PR's area label
2. **Community triager** (level 2) — check if reviewer is in the Community Triagers list in `docs/area-owners.md`
3. **Frequent contributor** (level 3) — `gh api "repos/dotnet/runtime/commits?author={login}&path={dir}&per_page=5"` — 3+ hits

**Scoring**

| Score | Condition |
|-------|-----------|
| ✅ 1 | Approved by at least one area owner/lead (level 1) |
| ⚠️ 0.75 | Approved by community triager (level 2) — strong signal but not merge authority |
| ⚠️ 0.5 | Approved by frequent contributor (level 3) or commented-only by any reviewer |
| ❌ 0 | No reviews, or only `CHANGES_REQUESTED` from owner |

**Edge cases**

- PR has multiple area labels → need approval from at least one area's owner.
- Community triager approval (level 2) is a strong positive signal even though it doesn't fully satisfy the maintainer review requirement — note this in the output.

---

### 4. Feedback Addressed

**How to check**

```bash
gh pr view {PR} --repo dotnet/runtime --json labels
```

Also query GraphQL `reviewThreads` for unresolved threads:

```graphql
{
  repository(owner: "dotnet", name: "runtime") {
    pullRequest(number: PR) {
      reviewThreads(first: 100) {
        nodes { isResolved }
      }
    }
  }
}
```

**Scoring**

| Score | Condition |
|-------|-----------|
| ✅ 1 | No `needs-author-action` label AND zero unresolved review threads |
| ⚠️ 0.5 | Has unresolved threads but no `needs-author-action` label (label may not have been applied yet) |
| ❌ 0 | `needs-author-action` label present |

---

### 5. Merge Conflicts

**How to check**

```bash
gh pr view {PR} --repo dotnet/runtime --json mergeable
```

**Scoring**

| Score | Condition |
|-------|-----------|
| ✅ 1 | `MERGEABLE` |
| ⚠️ 0.5 | `UNKNOWN` (GitHub hasn't computed yet) |
| ❌ 0 | `CONFLICTING` |

---

### 6. Alignment

**How to check**

```bash
gh pr view {PR} --repo dotnet/runtime --json labels
```

Check for `area-*` label and absence of `untriaged`.

**Scoring (batch mode)**

| Score | Condition |
|-------|-----------|
| ✅ 1 | Has area label, not untriaged |
| ❌ 0 | `untriaged` label or no area label |

**Single-PR mode**: Can additionally check linked issues via GraphQL
`closingIssuesReferences` for a richer signal.

---

### 7. Freshness

**How to check**

Check labels for `no-recent-activity` and the `updatedAt` field.

```bash
gh pr view {PR} --repo dotnet/runtime --json labels,updatedAt
```

**Scoring**

| Score | Condition |
|-------|-----------|
| ✅ 1 | Updated within 14 days, no stale label |
| ⚠️ 0.5 | Updated 14–30 days ago |
| ❌ 0 | `no-recent-activity` label or >30 days since update |

---

### 8. Context Drift (medium cost)

**How to check**

Get PR's changed file paths, then for each directory touched, check recent
commits on main since the PR was last updated:

```bash
gh pr view {PR} --repo dotnet/runtime --json files
gh api "repos/dotnet/runtime/commits?path={dir}&since={PR_updatedAt}&per_page=10"
```

**Scoring**

| Score | Condition |
|-------|-----------|
| ✅ 1 | <5 commits to touched directories on main since PR last updated |
| ⚠️ 0.5 | 5–20 commits (moderate churn) |
| ❌ 0 | >20 commits (significant context shift, even if it merges clean) |

---

### 9. PR Size / Complexity

**How to check**

```bash
gh pr view {PR} --repo dotnet/runtime --json changedFiles,additions,deletions,files
```

Also derive directory spread: count unique parent directories from
`files[].path`.

**Scoring**

| Score | Condition |
|-------|-----------|
| ✅ 1 | ≤5 files, ≤200 lines changed, ≤2 directories |
| ⚠️ 0.5 | 6–20 files, or 200–500 lines, or 3–5 directories |
| ❌ 0 | >20 files, or >500 lines, or >5 directories |

---

### 10. Community Contribution Flag

**How to check**

Check for `community-contribution` label; match author login against known team
members.

```bash
gh pr view {PR} --repo dotnet/runtime --json labels,author
```

**Scoring**

| Score | Condition |
|-------|-----------|
| ✅ 1 | Team member author (no special handling needed) |
| ⚠️ 0.5 | Community contributor — flagged for visibility so maintainers can prioritize |

**Score note**: The lower score reflects that community PRs are typically more
expensive to drive (alignment, mentoring, iteration) — not a quality judgment.
The `is_community` flag in output lets maintainers filter and prioritize as they see fit.

---

### 11. Linked Issue Priority

**How to check**

Query GraphQL `closingIssuesReferences` to find linked issues, then inspect
each:

```bash
gh issue view {ISSUE} --repo dotnet/runtime --json labels,milestone
```

Look for labels containing `Priority:` or a milestone.

**Scoring**

| Score | Condition |
|-------|-----------|
| ✅ 1 | Linked to `Priority:1` issue or current milestone |
| ⚠️ 0.5 | Linked to lower-priority issue |
| ❌ 0 | No linked issue (neutral, doesn't block) |

---

### 12. Approval Strength

**How to check**

```bash
gh pr view {PR} --repo dotnet/runtime --json reviews
```

Count `APPROVED` reviews and classify each reviewer by approval authority level (see [runtime-signals.md](runtime-signals.md#approval-authority-levels)):
- **Level 1** (area owner/lead): has merge authority
- **Level 2** (community triager): strong signal, weight 1.5×
- **Level 3** (frequent contributor): domain expertise, weight 1×
- **Level 4** (new contributor): valued feedback, weight 0.5×

**Scoring**

| Score | Condition |
|-------|-----------|
| ✅ 1 | 2+ approvals with at least one from area owner (tier 1) |
| ⚠️ 0.75 | 1 area owner approval, or approval from community triager (tier 2) + another reviewer |
| ⚠️ 0.5 | 1 approval from community triager alone, or multiple tier 3/4 approvals |
| ❌ 0 | No approvals |

---

### 13. Review Velocity

**How to check**

Compare `createdAt`, `updatedAt`, and the latest review timestamp.

```bash
gh pr view {PR} --repo dotnet/runtime --json createdAt,updatedAt,reviews
```

**Scoring**

| Score | Condition |
|-------|-----------|
| ✅ 1 | PR <7 days old with review activity, or PR with review in last 3 days |
| ⚠️ 0.5 | PR 7–14 days old with some review activity |
| ❌ 0 | PR >14 days old with no reviews (stalling — may need proactive outreach) |

---

### 14. Author Familiarity (medium cost)

**How to check**

For each directory the PR touches, query recent commits by the same author:

```bash
gh api "repos/dotnet/runtime/commits?author={author}&path={dir}&per_page=5"
```

**Scoring**

| Score | Condition |
|-------|-----------|
| ✅ 1 | Author has 3+ recent commits in the same area |
| ⚠️ 0.5 | Author has 1–2 commits in the area |
| ❌ 0 | No prior commits from this author in the touched area (first-time contributor to this area) |

---

### 15. Perf-Sensitive Area (expensive, single-PR only)

**How to check**

Match changed file paths against known hot paths:

| Path pattern | Reason |
|-------------|--------|
| `src/libraries/System.Private.CoreLib/` | Perf-critical core library |
| `src/coreclr/jit/` | JIT compiler |
| `src/coreclr/gc/` | Garbage collector |
| `src/libraries/System.Runtime/` | Core runtime |
| Any path containing `Span`, `Memory`, or `Buffer` | Hot-path types |

Also check if `performance-benchmark` was already run (look for @EgorBot
comments in the PR timeline).

**Scoring**

| Score | Condition |
|-------|-----------|
| ✅ 1 | Not perf-sensitive, or benchmark already run |
| ⚠️ 0.5 | Perf-sensitive area, benchmark not yet run |

This dimension is never blocking (❌), only advisory.

---

### 16. Breaking Change Risk (expensive, single-PR only)

**How to check**

Look for `ref/` files in the changed files list (API surface changes). Check
for `api-ready-for-review` or `api-approved` labels.

```bash
gh pr view {PR} --repo dotnet/runtime --json files,labels
```

**Scoring**

| Score | Condition |
|-------|-----------|
| ✅ 1 | No `ref/` changes, or `api-approved` label present |
| ⚠️ 0.5 | `ref/` changes with `api-ready-for-review` (review pending) |
| ❌ 0 | `ref/` changes without any API review label |

---

## Composite Scoring

For batch-mode ranking, compute a weighted score per PR. Multiply each
dimension's point value (0, 0.5, or 1) by its weight and sum.

### Weight Table (as implemented in the script)

The script computes 12 dimensions. Dimensions 8 (Context Drift), 11 (Linked Issue
Priority), 14 (Author Familiarity), 15 (Perf-Sensitive Area), and 16 (Breaking
Change Risk) are documented above for single-PR deep dives but are **not included**
in the batch composite score.

| # | Dimension | Weight | Rationale |
|---|-----------|--------|-----------|
| 1 | CI Status | 3 | Can't merge without green CI |
| 5 | Merge Conflicts | 3 | Can't merge with conflicts |
| 3 | Maintainer Review | 3 | Required for merge |
| 4 | Feedback Addressed | 2 | Blocks merge if outstanding |
| 12 | Approval Strength | 2 | Stronger approvals = closer to merge |
| 2 | Staleness | 1.5 | Days since last update |
| — | Discussion Complexity | 1.5 | Thread count and distinct commenters |
| 6 | Alignment | 1 | Organizational signal |
| 7 | Freshness | 1 | Stale PRs less likely to merge soon |
| 9 | PR Size / Complexity | 1 | Ease of review |
| 10 | Community Contribution Flag | 0.5 | Flags community PRs — typically more expensive to drive |
| 13 | Review Velocity | 0.5 | Momentum signal |

**Max possible raw score**: 20.5. Normalized to a 0–10 scale: `(rawScore / 20.5) × 10`.

### Formula

```
composite = Σ (dimension_score × weight)
```

Rank PRs by composite score descending. Ties are broken by:

1. Higher CI Status score
2. Higher Maintainer Review score
3. Older `createdAt` (longer-waiting PRs first)

---

## Edge Cases

### Draft PRs

Skip entirely in batch mode. In single-PR mode, note: *"This is a draft PR —
most dimensions don't apply until it's marked ready for review."*

### Bot PRs

`dotnet-maestro[bot]` (codeflow) PRs are excluded from batch by default.
`copilot-swe-agent` PRs are **included by default** — they are maintainer-initiated
and follow normal review workflows. Use `-ExcludeCopilot` to exclude them.

### Flow PRs

Missing packages ≠ infrastructure failure. Context drift is expected for
dependency-flow PRs. Deprioritize in ranking.

### Backport PRs

May target release branches. Check if the target branch has the test
infrastructure. Build Analysis may behave differently on non-main branches.
