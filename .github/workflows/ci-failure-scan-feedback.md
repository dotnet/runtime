---
name: "CI Outer-Loop Failure Scanner — Feedback"
description: "Periodic tick that reads the latest ci-failure-scan runs and maintainer feedback on the issues/PRs it produced, scores them against a quality rubric, and proposes targeted edits to ci-failure-scan.md or shared/create-kbe.instructions.md as a single draft PR."

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
      - ".github/workflows/shared/create-kbe.instructions.md"
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
      - ".github/workflows/shared/create-kbe.instructions.md"
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

You evaluate the [`CI Outer-Loop Failure Scanner`](ci-failure-scan.md), maintain a single KPI tracker issue with a running window of metrics, and propose targeted edits to its prompt/instructions so the next run produces tighter, more actionable artifacts. You run read-only; the only write paths are against `.github/workflows/ci-failure-scan.md`, `.github/workflows/shared/create-kbe.instructions.md`, and the tracker issue body.

Hard rules: no comments on issues/PRs, no edits outside `.github/workflows/ci-failure-scan.md` and `.github/workflows/shared/create-kbe.instructions.md`, max 1 PR + 1 tracker issue open at a time. Reading issue/PR bodies and comments (the user-supplied content the integrity gate exists to filter) MUST go through the `github` MCP tool with `min-integrity: approved`; `[Filtered]` results are skipped (record the count, do not chase them). `gh` calls are allowed for workflow-run metadata (`gh api .../actions/...`, `gh run view --log`) and for enumerating this workflow's own artifacts (finding the `[ci-scan-feedback]` PR/tracker by title or repository-owned label), but NOT for reading maintainer-supplied content — do not use `gh issue view`, `gh pr view`, or `gh api /repos/.../comments` to substitute for the integrity-gated reads.

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

5. Translate each failure mode into a targeted edit to whichever file owns the rule: `.github/workflows/ci-failure-scan.md` for scanner behavior or `.github/workflows/shared/create-kbe.instructions.md` for KBE authoring guidance. Prefer rule-shaped edits (tighten Step 4.2, extend Step 4.7's phrase list, add a Bad/Good row, narrow KBE check 7) over wholesale rewrites. Read the target file first; reuse the existing voice and section structure.

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

   Collect the full universe of `[ci-scan]` issues and PRs (open + closed) since `window_start` via `gh search issues` / `gh search prs` with `created:>=<window_start>`. This produces a list of issue/PR numbers and metadata; do NOT read bodies with `gh`. Cache the list to `/tmp/gh-aw/agent/artifacts.json` so later sections do not re-query.

   `gh search issues --json` and `gh search prs --json` already return `state`, `stateReason`, `labels`, `mergedAt`, `closedAt`, and `author` for each result; that metadata is sufficient for the activity and quality counts below. Only fetch through the integrity-gated `github` MCP when you actually need body or comment text.

   For metrics that need MEMBER/OWNER comment content (rejection-keyword detection, re-file outage signal), fetch through the `github` MCP `issue_read get`, `pull_request_read get`, and the corresponding comments tools, one per item, respecting `min-integrity: approved`. Skip `[Filtered]` items.

   Compute these KPIs. The shape below is deliberately small: raw counts, one quality ratio, a fixed set of outage signals. Do not re-introduce Wilson scoring, time-to-KBE, coverage ratios, or tally-extraction; they were dropped because they came back `n/a` most ticks and added noise.

   ### A) Activity (last 7d)

   For each artifact type (issues, PRs), count:

   - `opened` — artifacts created in the last 7d.
   - `closed_good` — issues closed in the last 7d with `state_reason: completed`; PRs merged in the last 7d.
   - `closed_wrong` — issues closed in the last 7d with `state_reason` in `{not_planned, duplicate}`; PRs closed without merge in the last 7d.

   ### B) Quality (closure cohort, last 30d)

   The quality rate is closure-based, not creation-based: a wrong closure of an old item still counts against this period's quality. This avoids the cross-cohort denominator bug where rate could exceed 100% if computed against opens.

   - `closed_good_30d = i_good_30d + p_merged_30d`.
   - `closed_wrong_30d = i_wrong_30d + p_unmerged_30d`.
   - `closed_30d = closed_good_30d + closed_wrong_30d`.
   - `wrong_rate_30d = closed_wrong_30d / closed_30d`. Emit as `n/a` when `closed_30d < 10`.
   - `complaints_30d` — count of MEMBER/OWNER comments on in-scope artifacts (open or closed) matching (case-insensitive): `don't disable`, `do not disable`, `do not mute`, `please don't`, `false positive`, `fix forward`, `fix-forward`, `investigation in progress`, `will investigate`, `stop filing`, `noise`, `not a real failure`, `flaky test`.
   - `duplicates_30d` — issues closed in the last 30d with `state_reason: duplicate` OR carrying a `duplicate` label.

   ### C) Outage signals

   These reflect the health of the **CI being monitored**, not the scanner workflow itself. Each signal has a fixed threshold and renders as 🔴 when tripped, otherwise 🟢.

   Source data comes from the cached artifact list; `[ci-scan]` KBE issues are proxies for distinct stable failure signatures in CI. For each KBE issue, fetch the body via the `github` MCP and grep for the line `Build error leg or test failing: <value>` (this is the only place the KBE template carries leg information; older `Impact on platforms` lines are not present in templates emitted after #128440). The value is `<AzDO leg name>-<assembly or test name>` where the separator is the LAST `-` in the value: split on the last hyphen and treat the left side as the leg. Cache `(issue_number, leg)` rows to `/tmp/gh-aw/agent/artifact_legs.tsv`. The pipeline-outage signal counts distinct legs because the KBE body does not capture the AzDO pipeline definition id reliably; treat distinct legs as a conservative proxy for distinct pipelines.

   | signal | source | threshold |
   |---|---|---|
   | New-KBE burst | count of `[ci-scan]` KBE issues created per day in the last 7d | any day > 2x trailing 30d daily median (min absolute count 3 to avoid noise) |
   | Build-break spike | count of `[ci-scan] Build break:` issues created per 24h | >= 2 in any 24h window in the last 7d |
   | Multi-pipeline outage (distinct legs proxy) | distinct leg names (parsed per above) across KBEs created in the last 24h | >= 3 distinct legs |
   | KBE re-filed after maintainer close | for each `[ci-scan]` KBE issue opened in the last 7d, search `is:issue is:closed -is:open in:title "<test-name-stem>" closed:>=<14d-ago>` and check whether any closed predecessor exists with a MEMBER/OWNER comment matching the keyword set in section B | any in the last 7d |
   | Wrong-closure rate | section B's `wrong_rate_30d` | >= 30% with `closed_30d >= 10` |

   Emit a body with this exact shape (regenerate every tick):

   ````markdown
   <!-- ci-scan-feedback:kpi-tracker -->
   <!-- ci-scan-feedback:window-start=<window_start> -->
   Tracking quality of `[ci-scan]` issues and PRs since <window_start>. Updated every tick of [ci-failure-scan-feedback.lock.yml](https://github.com/dotnet/runtime/blob/main/.github/workflows/ci-failure-scan-feedback.lock.yml). To raise a concern, comment here or on any `[ci-scan]` issue/PR; the next tick reads in-scope feedback and either opens a `[ci-scan-feedback]` PR with prompt edits or pushes to the existing one.

   ## Snapshot — <UTC timestamp>

   ### Activity (last 7d)

   | artifact | opened | closed (good) | closed (wrong) |
   |---|---|---|---|
   | Issues | <i_op_7> | <i_good_7> | <i_wrong_7> |
   | PRs | <p_op_7> | <p_merged_7> | <p_unmerged_7> |

   "closed (good)" = issues closed `completed`, PRs merged. "closed (wrong)" = issues closed `not_planned`/`duplicate`, PRs closed without merge.

   ### Quality (last 30d)

   | metric | count | rate |
   |---|---|---|
   | Total closures | <closed_30d> | — |
   | Wrong closures | <closed_wrong_30d> | <wrong_rate_30d_pct or `n/a (<closed_30d><10)`> |
   | Maintainer rejection comments | <complaints_30d> | — |
   | Duplicate KBEs | <duplicates_30d> | — |

   ### Outage signals (analyzed CI)

   | signal | threshold | 24h | 7d | status |
   |---|---|---|---|---|
   | New-KBE burst | day > 2x trailing 30d median (min 3) | <new_kbe_24h> / median <median_daily_kbe_30d> | peak day <peak_kbe_7d> | <🔴 or 🟢> |
   | Build-break spike | >= 2 in any 24h | <bb_24h> | <bb_7d> | <icon> |
   | Multi-pipeline outage (distinct legs proxy) | >= 3 distinct legs in 24h | <legs_24h> distinct | <legs_peak_7d> distinct (peak day) | <icon> |
   | KBE re-filed after maintainer close | any in 7d | <refile_24h> | <refile_7d> | <icon> |
   | Wrong-closure rate (30d) | >= 30% with `closed_30d >= 10` | — | <wrong_rate_30d_pct> | <icon> |

   For each signal at 🔴, emit one `details:` line **after** the Outage signals table (not inside it; markdown tables cannot carry sub-rows). Prefix each line with the signal name so it is unambiguous which row it explains. Example:

   ```
   details: Multi-pipeline outage — runtime-coreclr outerloop linux x64 checked, runtime-extra-platforms windows x86 release, runtime-interpreter linux x64 checked all red on 2026-06-01
   details: KBE re-filed after maintainer close — #128793 re-filed #128737
   ```

   Omit the details block entirely when no signal is 🔴.
   ````

   Suppression rules:

   - If `closed_30d < 10`, the Quality table's `rate` column reads `n/a (<n><10)` for `wrong_rate_30d`; all other rows still render with raw counts.
   - Outage signals always render. An explicit 🟢 with no data still carries information.
   - Do NOT emit charts (mermaid or otherwise).
   - Do NOT emit historical weekly buckets. The body is a current snapshot.

   If the tracker exists -> emit one `update_issue` with the new body. If not -> emit one `create_issue` titled `[ci-scan-feedback] KPI Tracker`. Either way, this step ALWAYS fires (never call `noop` for the tracker — a daily snapshot is the point).

## Output to agent log

Print the rubric scorecard table to the agent log so the next tick can grep it:

```
| run-id | artifact | title-scoped | classification | json-valid | signature-specific | log-cross-check | maintainer-feedback |
```

One row per artifact scored. Skip rows where every column is `pass`. Append a final line `[Filtered] count: <n>` so out-of-integrity items are visible without being followed.
