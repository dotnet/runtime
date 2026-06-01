---
name: "CI Outer-Loop Failure Scanner — Feedback"
description: "Periodic tick that reads the latest ci-failure-scan runs and maintainer feedback on the issues/PRs it produced, scores them against a quality rubric, and proposes targeted edits to ci-failure-scan.md as a single draft PR."

permissions:
  contents: read
  issues: read
  pull-requests: read
  actions: read

on:
  schedule: daily
  workflow_dispatch:
  roles: [admin, maintainer, write]
  permissions: {}

if: |
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
    protected-files:
      policy: blocked
      exclude:
        - .github/
    labels: [agentic-workflows]
    allowed-labels: [agentic-workflows]
  push-to-pull-request-branch:
    max: 1
    allowed-files:
      - ".github/workflows/ci-failure-scan.md"
    protected-files:
      policy: blocked
      exclude:
        - .github/
  update-pull-request:
    max: 1
  create-issue:
    max: 1
    labels: [agentic-workflows]
    allowed-labels: [agentic-workflows]
  update-issue:
    target: "*"
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

Hard rules: no comments on issues/PRs, no edits outside `.github/workflows/ci-failure-scan.md`, max 1 PR + 1 tracker issue open at a time. Reading issue/PR bodies and comments (the user-supplied content the integrity gate exists to filter) MUST go through the `github` MCP tool with `min-integrity: approved`; `[Filtered]` results are skipped (record the count, do not chase them). `gh` calls are allowed for workflow-run metadata (`gh api .../actions/...`, `gh run view --log`) and for enumerating this workflow's own artifacts (finding the `[ci-scan-feedback]` PR/tracker by title or repository-owned label), but NOT for reading maintainer-supplied content — do not use `gh issue view`, `gh pr view`, or `gh api /repos/.../comments` to substitute for the integrity-gated reads.

## Steps

1. Fetch the latest 10 runs of `ci-failure-scan.lock.yml`:

   ```bash
   gh api "/repos/dotnet/runtime/actions/workflows/ci-failure-scan.lock.yml/runs?per_page=10" \
     | tee /tmp/gh-aw/agent/runs.json | jq -r '.workflow_runs[] | "\(.id) \(.conclusion) \(.head_branch) \(.event) \(.created_at) \(.html_url)"'
   ```

2. For the latest 2 runs, list the issues/PRs they produced and download the agent log to extract the final tally line emitted by Step 6 of `ci-failure-scan.md`. Extract ONLY the tally table block (header + body rows, terminated by the first non-pipe line) — do NOT pipe arbitrary trailing log content through, since the scanner agent's log may contain quoted maintainer-supplied content that bypasses the integrity gate:

   ```bash
   gh run view <run-id> --log \
     | awk '/^\| pipeline \|/{flag=1} flag && /^\|/{print; next} flag{exit}' \
     | tee /tmp/gh-aw/agent/tally_<run-id>.txt
   ```

3. Read in-scope feedback. Issues and PRs are in scope when EITHER the `agentic-workflows` label is present OR the title starts with `[ci-scan]`. For each in-scope item updated in the last 30 days, fetch body + all comments via the `github` MCP tool (NOT `gh`, per the hard rule above). Quote any maintainer comment matching: "too broad", "doesn't match", "duplicate", "wrong label", "JSON malformed", "fix-forward", "don't disable", "Known issue did not match", "should be supported", "wait for #".

   Discover candidates with the `github` tool's `search_issues` and `search_pull_requests` over both label and title scopes, applying the 30-day window via the search query itself:

   - `repo:dotnet/runtime is:issue label:agentic-workflows updated:>=<today-30d>`
   - `repo:dotnet/runtime is:issue in:title "[ci-scan]" updated:>=<today-30d>`
   - `repo:dotnet/runtime is:pr label:agentic-workflows updated:>=<today-30d>`
   - `repo:dotnet/runtime is:pr in:title "[ci-scan]" updated:>=<today-30d>`

   Both the closed and open state buckets are in scope (closed items often carry the most informative feedback about why the artifact was rejected). For each result, read the body and comments via the `github` tool. When listing comments, request only the most recent 100 (one page at the MCP default size) — the 30-day `updated:>=...` window is the primary filter, and threads with >100 comments are vanishingly rare for `[ci-scan]` artifacts. If a result has >100 comments, fetch the latest page only; do NOT paginate further (older comments are out-of-scope by construction). Record `integrity-filtered: N` for any `[Filtered]` results and continue.

4. Score each artifact against the rubric:

   - Title is scoped to a single failure shape (not a list of pipelines).
   - Classification matches the failure (KBE-eligible test/hang -> `Known Build Error`; build break -> KBE without test-disable; infra noise -> no issue at all).
   - JSON block is valid: exactly one fenced `` ```json `` block, all four keys present, exactly one of `ErrorMessage`/`ErrorPattern` non-empty.
   - Signature is specific: not a bare test/method name, not a generic exception/exit-code, not a phrase that also appears in `[PASS]`/`[SKIP]` lines of the same log.
   - For a sample of in-scope KBEs, cross-check the signature against the cited failing log (`gh run view <id> --log-failed | head -300` or the AzDO/Helix URL in the body) and flag PASS-line collisions or paraphrased signatures.
   - Skip-reason vocabulary stability: any tally row using a `skipped:` reason NOT in the Step 6 'Recognized values' list in `ci-failure-scan.md` is flagged as `unknown-skip-reason: <verbatim string>`. The recognized values list is the source of truth; the feedback PR should propose adding new reasons there before they start appearing in tallies.

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

7. KPI tracker. Maintain a single `[ci-scan-feedback] KPI Tracker` issue whose body is rewritten every tick with a running window of metrics measured since the scanner was established. The body must be regenerated in full, not appended to — there is only one current snapshot. The workflow cannot pin issues; maintainers may pin the tracker manually if desired.

   Find or bootstrap the tracker:

   ```bash
   gh search issues 'repo:dotnet/runtime is:issue is:open in:title "[ci-scan-feedback] KPI Tracker"' \
     --json number,url | tee /tmp/gh-aw/agent/tracker.json
   ```

   Compute the window. The window starts at the timestamp of the FIRST recorded run of `ci-failure-scan.lock.yml` (NOT the workflow file's creation time, which can predate any run by days when the file is added but its lock isn't yet checked in). On the first tick, derive it from the runs list and persist the resulting ISO-8601 timestamp inside the tracker body as `<!-- ci-scan-feedback:window-start=<ts> -->`. On every subsequent tick, prefer the cached value parsed from the existing tracker body (read via the `github` MCP `issue_read get` tool) over re-deriving — this keeps the window stable even if old runs are deleted.

   ```bash
   gh api --paginate "/repos/dotnet/runtime/actions/workflows/ci-failure-scan.lock.yml/runs?per_page=100" \
     | jq -s '[.[].workflow_runs[]] | sort_by(.created_at) | .[0].created_at // empty' -r \
     | tee /tmp/gh-aw/agent/window_start_first_run.txt
   ```

   The `/runs` endpoint does NOT accept `order=asc`; you MUST paginate and pick `min(created_at)`. Fall back to the workflow's `.created_at` only if no runs exist yet (first-ever invocation). Once the tracker exists, the cached marker is authoritative.

   Collect the full universe of `[ci-scan]` issues and PRs (open + closed) since `window_start` via `gh search issues` / `gh search prs` with `created:>=<window_start>`. This produces a list of issue/PR numbers and metadata; do NOT read bodies with `gh`.

   For metrics that require body or comment content (rejection-keyword detection, maintainer-touch signal), fetch through the `github` MCP `issue_read get`, `pull_request_read get`, and the corresponding comments tools, one per item, respecting `min-integrity: approved`. Skip `[Filtered]` items.

   Compute these KPIs.

   ### A) Acceptance classification (primary)

   Each artifact (issue or PR) is classified into exactly one of three buckets. Evaluate the rules in order; the first matching rule wins.

   - **Accepted** — PR merged; OR issue closed with `state_reason: completed`; OR issue still open and >= 7 days old AND touched by a MEMBER/OWNER (commented, labeled, assigned, milestoned, or linked from another issue/PR).
   - **Rejected** — PR closed unmerged; OR issue closed with `state_reason` in `{not_planned, duplicate}`; OR ANY MEMBER/OWNER comment on the artifact matches (case-insensitive) one of: `don't disable`, `do not disable`, `do not mute`, `please don't`, `false positive`, `fix forward`, `fix-forward`, `investigation in progress`, `will investigate`, `stop filing`, `noise`, `not a real failure`, `flaky test`.
   - **Pending** — every other artifact (no Accepted or Rejected rule has matched yet, e.g. a young untouched issue or an older open issue with only non-rejection maintainer activity). Excluded from the rate.

   Compute the classification over both the 30-day and 90-day rolling windows from `now`. For each window emit:

   - `accepted_n`, `rejected_n`, `pending_n`.
   - `acceptance_rate = accepted_n / (accepted_n + rejected_n)`. Emit as `n/a` when `accepted_n + rejected_n < 10`.
   - `acceptance_lower_bound` — Wilson score 95% lower bound on the same numerator/denominator (only when N >= 10). Use this for any single-number summary so 5/5 (N=5) does not read as 100% better than 50/55 (N=55).

   ### B) Environment / scanner activity (volume)

   Source CI failure counts from the scanner's own agent logs over the window. Step 1 (latest 10 runs) and Step 2 (tallies for the latest 2) are sized for the rubric-scoring path and do NOT cover 30 days, the prior-7d comparison period, or anything beyond the most recent two tallies — this section MUST enumerate scanner runs independently. Paginate `ci-failure-scan.lock.yml` runs created in the last 30 days and download the tally for each (reuse the Step 2 `awk` extractor; skip downloads when the tally file already exists from Step 2). The signature tuple is `(pipeline_definition_id, job_or_test_name, normalized_error_fingerprint)` as the scanner already produces it for filing.

   ```bash
   SINCE_30D=$(date -u -d '30 days ago' +%Y-%m-%dT%H:%M:%SZ 2>/dev/null || date -u -v-30d +%Y-%m-%dT%H:%M:%SZ)
   gh api --paginate \
     "/repos/dotnet/runtime/actions/workflows/ci-failure-scan.lock.yml/runs?per_page=100&created=>=${SINCE_30D}" \
     | jq -r '.workflow_runs[] | "\(.id) \(.created_at)"' \
     | tee /tmp/gh-aw/agent/runs_30d.txt
   while read -r RUN_ID RUN_CREATED; do
     OUT="/tmp/gh-aw/agent/tally_${RUN_ID}.txt"
     test -s "$OUT" && continue
     gh run view "$RUN_ID" --log \
       | awk '/^\| pipeline \|/{flag=1} flag && /^\|/{print; next} flag{exit}' \
       > "$OUT"
   done < /tmp/gh-aw/agent/runs_30d.txt
   ```

   Bucket each tally row by its source run's `created_at` into `last 7d`, `prior 7d` (the 7-day window ending 7 days ago), and `last 30d`. Sum failure counts within each bucket; de-duplicate signature tuples within each bucket before counting distinct signatures.

   - `ci_failures_7d`, `ci_failures_30d` — total failure observations across all pipelines the scanner monitors.
   - `ci_failures_delta_pct` — `(ci_failures_7d / ci_failures_prev_7d) - 1`, formatted as a signed percentage. Use the 7-day window ending 7 days ago as the prior period. Emit as `—` if either window has < 20 failures (too few to call a spike).
   - `distinct_signatures_7d`, `distinct_signatures_30d` — count of distinct signature tuples observed in the window.
   - `top_failing_pipelines_7d` — top 3 `pipeline_definition_id` values by failure count in the last 7d, with their counts.
   - `kbes_created_7d`, `kbes_closed_7d`, `kbes_created_30d`, `kbes_closed_30d` — issues created/closed by the scanner in each window (titles prefixed `[ci-scan]`).
   - `prs_created_7d`, `prs_merged_7d`, `prs_closed_unmerged_7d`, and the matching 30d trio — PRs created by the scanner in each window.

   ### C) Coverage and latency

   - `coverage_30d = kbes_created_30d / distinct_signatures_30d`. Emit as `n/a` when `distinct_signatures_30d < 10`. A drop here means the scanner is missing repeated failures.
   - `time_to_kbe_median_30d`, `time_to_kbe_p90_30d` — for each signature first observed in the window that did get a KBE filed, hours between first observation and the KBE's `createdAt`. Skip signatures with no KBE.

   ### D) Diagnostics

   Drill-down counts (last 30d) — surface them so rubric findings have quantitative grounding, do not headline them:

   - `complaint_count` — MEMBER/OWNER comments on scanner artifacts matching the rejection-keyword set in section A.
   - `duplicate_count` — issues closed with `state_reason: duplicate` or labeled `duplicate`.
   - `scanner_pr_ci_fail_count` — PRs whose most recent commit had any required-status check report `failure`/`error` before maintainer review (proxy for "should the pre-emit checklist have caught it").

   Emit a body with this exact shape (regenerate every tick):

   ````markdown
   <!-- ci-scan-feedback:kpi-tracker -->
   Tracking quality of `[ci-scan]` issues and PRs since <window_start>. Updated every tick of [ci-failure-scan-feedback.lock.yml](https://github.com/dotnet/runtime/blob/main/.github/workflows/ci-failure-scan-feedback.lock.yml). To raise a concern, comment here or on any `[ci-scan]` issue/PR; the next tick reads in-scope feedback and either opens a `[ci-scan-feedback]` PR with prompt edits or pushes to the existing one.

   ## Snapshot — <UTC timestamp>

   Primary metric: **scanner artifact acceptance rate** = accepted / (accepted + rejected), measured over a rolling window. Pending artifacts (no accepted/rejected rule has matched yet) are excluded from the denominator.

   | window | accepted | rejected | pending | acceptance | Wilson 95% lower |
   |---|---|---|---|---|---|
   | last 30d | <a30> | <r30> | <p30> | <pct30 or `n/a (<n><10)`> | <wilson30 or `—`> |
   | last 90d | <a90> | <r90> | <p90> | <pct90 or `n/a (<n><10)`> | <wilson90 or `—`> |

   ### CI environment

   | metric | 7d | 30d |
   |---|---|---|
   | Failures observed | <ci_failures_7d> (Δ <ci_failures_delta_pct> vs prior 7d) | <ci_failures_30d> |
   | Distinct signatures | <distinct_signatures_7d> | <distinct_signatures_30d> |
   | Top failing pipelines (7d) | <name1> (<n1>), <name2> (<n2>), <name3> (<n3>) | — |

   ### Scanner activity

   | artifact | 7d created | 7d resolved | 30d created | 30d resolved |
   |---|---|---|---|---|
   | KBE issues | <kbes_created_7d> | <kbes_closed_7d> | <kbes_created_30d> | <kbes_closed_30d> |
   | Test-disable PRs | <prs_created_7d> | merged <prs_merged_7d>, closed <prs_closed_unmerged_7d> | <prs_created_30d> | merged <prs_merged_30d>, closed <prs_closed_unmerged_30d> |

   ### Coverage and latency (30d)

   | metric | value |
   |---|---|
   | Coverage (KBEs filed / distinct signatures) | <coverage_30d or `n/a (<n><10)`> |
   | Time to KBE (median / P90) | <ttk_med>h / <ttk_p90>h |

   ### Diagnostics (30d)

   | signal | count |
   |---|---|
   | Maintainer complaint comments | <complaint_count> |
   | Duplicate KBEs | <duplicate_count> |
   | Scanner PRs failing CI before review | <scanner_pr_ci_fail_count> |
   ````

   Suppression rules:

   - If both windows have `accepted + rejected < 10`, replace the acceptance-rate table with the line `Insufficient data: <n> graded artifacts in the last 90 days; need at least 10 before reporting a rate.` Still print the other tables.
   - If `distinct_signatures_30d < 10`, omit the coverage row (do not emit a misleading ratio).
   - Do NOT emit charts (mermaid or otherwise). Rates over small N look like signal when they are noise.
   - Do NOT emit historical weekly buckets. The body is a current snapshot.

   If the tracker exists -> emit one `update_issue` with the new body. If not -> emit one `create_issue` titled `[ci-scan-feedback] KPI Tracker`. Either way, this step ALWAYS fires (never call `noop` for the tracker — a daily snapshot is the point).

## Output to agent log

Print the rubric scorecard table to the agent log so the next tick can grep it:

```
| run-id | artifact | title-scoped | classification | json-valid | signature-specific | log-cross-check | maintainer-feedback |
```

One row per artifact scored. Skip rows where every column is `pass`. Append a final line `[Filtered] count: <n>` so out-of-integrity items are visible without being followed.
