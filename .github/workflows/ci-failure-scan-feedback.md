---
name: "CI Outer-Loop Failure Scanner — Feedback"
description: "Periodic tick that reads the latest ci-failure-scan runs and maintainer feedback on the issues/PRs it produced, scores them against a quality rubric, and proposes targeted edits to ci-failure-scan.md as a single draft PR."

permissions:
  contents: read
  issues: read
  pull-requests: read
  actions: read

on:
  schedule: every 3d
  workflow_dispatch:
  roles: [admin, maintainer, write]
  permissions: {}

if: |
  github.event_name == 'workflow_dispatch' ||
  github.repository == 'dotnet/runtime'

# ###############################################################
# Override COPILOT_GITHUB_TOKEN with a random PAT from the pool.
# This stop-gap will be removed when org billing is available.
# See: .github/workflows/shared/pat_pool.README.md for more info.
# ###############################################################
imports:
  - shared/pat_pool.md

engine:
  id: copilot
  model: claude-opus-4.6
  env:
    COPILOT_GITHUB_TOKEN: ${{ case(needs.pat_pool.outputs.pat_number == '0', secrets.COPILOT_PAT_0, needs.pat_pool.outputs.pat_number == '1', secrets.COPILOT_PAT_1, needs.pat_pool.outputs.pat_number == '2', secrets.COPILOT_PAT_2, needs.pat_pool.outputs.pat_number == '3', secrets.COPILOT_PAT_3, needs.pat_pool.outputs.pat_number == '4', secrets.COPILOT_PAT_4, needs.pat_pool.outputs.pat_number == '5', secrets.COPILOT_PAT_5, needs.pat_pool.outputs.pat_number == '6', secrets.COPILOT_PAT_6, needs.pat_pool.outputs.pat_number == '7', secrets.COPILOT_PAT_7, needs.pat_pool.outputs.pat_number == '8', secrets.COPILOT_PAT_8, needs.pat_pool.outputs.pat_number == '9', secrets.COPILOT_PAT_9, secrets.COPILOT_GITHUB_TOKEN) }}

concurrency:
  group: "ci-failure-scan-feedback"
  cancel-in-progress: true

tools:
  github:
    toolsets: [pull_requests, repos, issues, search, actions]
    min-integrity: approved
  edit:
  bash: ["git", "find", "ls", "cat", "grep", "head", "tail", "wc", "curl", "jq", "tee", "sed", "awk", "tr", "cut", "sort", "uniq", "xargs", "echo", "date", "mkdir", "test", "env", "basename", "dirname", "gh"]

checkout:
  fetch-depth: 1

safe-outputs:
  create-pull-request:
    title-prefix: "[ci-scan-feedback] "
    draft: true
    max: 1
    allowed-files:
      - ".github/workflows/ci-failure-scan.md"
    labels: [agentic-workflows]
    allowed-labels: [agentic-workflows]
  push-to-pull-request-branch:
    max: 1
    allowed-files:
      - ".github/workflows/ci-failure-scan.md"
  update-pull-request:
    max: 1
  create-issue:
    max: 1
    labels: [agentic-workflows]
    allowed-labels: [agentic-workflows]
  update-issue:
    max: 1
  noop:
    report-as-issue: false

timeout-minutes: 60

network:
  allowed:
    - defaults
    - github
---

# CI Failure Scanner — Feedback

You evaluate the [`CI Outer-Loop Failure Scanner`](ci-failure-scan.md), maintain a single KPI tracker issue with a running window of metrics, and propose targeted edits to its prompt so the next run produces tighter, more actionable artifacts. You run read-only; the only write paths are against `.github/workflows/ci-failure-scan.md` and the tracker issue body.

Hard rules: no comments on issues/PRs, no edits outside `.github/workflows/ci-failure-scan.md`, max 1 PR + 1 tracker issue open at a time. All issue/PR reads go through tools with `min-integrity: approved`; `[Filtered]` results are skipped (record the count, do not chase them).

## Steps

1. Fetch the latest 10 runs of `ci-failure-scan.lock.yml`:

   ```bash
   gh api "/repos/dotnet/runtime/actions/workflows/ci-failure-scan.lock.yml/runs?per_page=10" \
     | tee /tmp/gh-aw/agent/runs.json | jq -r '.workflow_runs[] | "\(.id) \(.conclusion) \(.head_branch) \(.event) \(.created_at) \(.html_url)"'
   ```

2. For the latest 2 runs, list the issues/PRs they produced and download the agent log to extract the final tally line emitted by Step 6 of `ci-failure-scan.md`:

   ```bash
   gh run view <run-id> --log | grep -E '^\| pipeline ' -A 200 | tee /tmp/gh-aw/agent/tally_<run-id>.txt
   ```

3. Read in-scope feedback. Issues and PRs are in scope when EITHER the `agentic-workflows` label is present OR the title starts with `[ci-scan]`. For each in-scope item open in the last 30 days, fetch body + all comments. Quote any maintainer comment matching: "too broad", "doesn't match", "duplicate", "wrong label", "JSON malformed", "fix-forward", "don't disable", "Known issue did not match", "should be supported", "wait for #".

   ```bash
   gh issue list -R dotnet/runtime --label agentic-workflows --state open --limit 200 \
     --json number,title,labels,createdAt,updatedAt,url | tee /tmp/gh-aw/agent/bot_issues.json
   gh search issues 'repo:dotnet/runtime is:issue is:open in:title "[ci-scan]"' \
     --json number,title,url,createdAt,updatedAt | tee /tmp/gh-aw/agent/title_issues.json
   ```

4. Score each artifact against the rubric:

   - Title is scoped to a single failure shape (not a list of pipelines).
   - Classification matches the failure (KBE-eligible test/hang -> `Known Build Error`; build break -> KBE without test-disable; infra noise -> no issue at all).
   - JSON block is valid: exactly one fenced ```json block, all four keys present, exactly one of `ErrorMessage`/`ErrorPattern` non-empty.
   - Signature is specific: not a bare test/method name, not a generic exception/exit-code, not a phrase that also appears in `[PASS]`/`[SKIP]` lines of the same log.
   - For a sample of in-scope KBEs, cross-check the signature against the cited failing log (`gh run view <id> --log-failed | head -300` or the AzDO/Helix URL in the body) and flag PASS-line collisions or paraphrased signatures.

5. Translate each failure mode into a targeted edit to `.github/workflows/ci-failure-scan.md`. Prefer rule-shaped edits (tighten Step 4.2, extend Step 4.7's phrase list, add a Bad/Good row, narrow KBE check 7) over wholesale rewrites. Read the file first; reuse the existing voice and section structure.

6. Emit changes. Check for an existing open `[ci-scan-feedback]` PR first:

   ```bash
   gh pr list -R dotnet/runtime --state open --search 'in:title "[ci-scan-feedback]"' \
     --json number,headRefName,url | tee /tmp/gh-aw/agent/open_feedback_prs.json
   ```

   Branch on the result:

   - Existing PR found -> emit `push_to_pull_request_branch` to add the new edits as a commit on that PR's branch, then emit `update_pull_request` to append a new dated section to its body. Do NOT call `create_pull_request`.
   - No existing PR -> emit one `create_pull_request`. Title: `[ci-scan-feedback] <one-line summary>`.

   The PR body (or the appended section, when updating) MUST contain:
   - `## Triggering signals` — bullet list of `(issue/PR #, quoted maintainer comment or rubric finding, link)`.
   - `## Proposed edits` — bullet list of `(file:line-range, one-line rationale tied to a signal above)`.
   - `## Expected behavior change` — one paragraph naming the failure mode the next run will avoid.

   If no signal warrants an edit, skip this step (do NOT call `noop` — Step 7 still emits the tracker update).

7. KPI tracker. Maintain a single pinned `[ci-scan-feedback] KPI Tracker` issue whose body is rewritten every tick with a running window of metrics measured since the scanner was established. The body must be regenerated in full, not appended to — there is only one current snapshot.

   Find or bootstrap the tracker:

   ```bash
   gh search issues 'repo:dotnet/runtime is:issue is:open in:title "[ci-scan-feedback] KPI Tracker"' \
     --json number,url | tee /tmp/gh-aw/agent/tracker.json
   ```

   Compute the window. The window starts at the workflow's first run; cache the date on the tracker body and reuse it:

   ```bash
   gh api "/repos/dotnet/runtime/actions/workflows/ci-failure-scan.lock.yml" \
     | jq -r '.created_at' | tee /tmp/gh-aw/agent/window_start.txt
   ```

   Collect the full universe of `[ci-scan]` issues and PRs (open + closed) since `window_start` via `gh search issues` / `gh search prs` with `created:>=<window_start>`. Bucket by ISO week.

   Compute these KPIs:

   - `issues_total`, `issues_open`, `issues_closed`
   - `closed_last_7d`, `closed_last_30d`
   - `median_time_to_close_30d` (hours; from `createdAt` to `closedAt` for issues closed in last 30d)
   - `p90_time_to_close_30d`
   - `pct_closed_as_not_planned` (state_reason `not_planned` / total closed) — proxy for false positives flagged by maintainers
   - `prs_total`, `prs_merged`, `prs_closed_unmerged`
   - `top_pipelines` (top 3 `definition_id` mentions in issue bodies)

   Emit a body with this exact shape (regenerate every tick):

   ````markdown
   <!-- ci-scan-feedback:kpi-tracker -->
   Tracking quality of `[ci-scan]` issues and PRs since <window_start>. Updated every tick of [ci-failure-scan-feedback.lock.yml]. To raise a concern, comment here or on any `[ci-scan]` issue/PR; the next tick reads in-scope feedback and either opens a `[ci-scan-feedback]` PR with prompt edits or pushes to the existing one.

   ## Snapshot — <UTC timestamp>

   | metric | value |
   |---|---|
   | Window | <window_start> -> now (<N> days) |
   | Issues filed | <issues_total> (open <issues_open>, closed <issues_closed>) |
   | Closed last 7d / 30d | <closed_last_7d> / <closed_last_30d> |
   | Median time-to-close (30d) | <h>h |
   | P90 time-to-close (30d) | <h>h |
   | Closed as `not planned` | <pct>% (proxy for false-positive rate) |
   | PRs filed | <prs_total> (merged <prs_merged>, closed-unmerged <prs_closed_unmerged>) |
   | Top pipelines | <name1> (<n1>), <name2> (<n2>), <name3> (<n3>) |

   ## Weekly volume (last 12 weeks)

   ```mermaid
   xychart-beta
       title "[ci-scan] issues filed vs closed per ISO week"
       x-axis [<w1>, <w2>, ..., <w12>]
       y-axis "count" 0 --> <max>
       bar [<filed_w1>, ..., <filed_w12>]
       line [<closed_w1>, ..., <closed_w12>]
   ```

   ## Median time-to-close (last 12 weeks)

   ```mermaid
   xychart-beta
       title "median hours from created to closed, per ISO week"
       x-axis [<w1>, ..., <w12>]
       y-axis "hours" 0 --> <max>
       line [<med_w1>, ..., <med_w12>]
   ```

   <details>
   <summary>Raw weekly buckets</summary>

   | iso-week | filed | closed | median-hrs |
   |---|---|---|---|
   | <w1> | <n> | <n> | <h> |
   ...
   </details>
   ````

   If the tracker exists -> emit one `update_issue` with the new body. If not -> emit one `create_issue` titled `[ci-scan-feedback] KPI Tracker`. Either way, this step ALWAYS fires (never call `noop` for the tracker — a snapshot every 3 days is the point).

## Output to agent log

Print the rubric scorecard table to the agent log so the next tick can grep it:

```
| run-id | artifact | title-scoped | classification | json-valid | signature-specific | log-cross-check | maintainer-feedback |
```

One row per artifact scored. Skip rows where every column is `pass`. Append a final line `[Filtered] count: <n>` so out-of-integrity items are visible without being followed.
