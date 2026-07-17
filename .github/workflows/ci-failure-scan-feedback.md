---
name: "CI Outer-Loop Failure Scanner — Feedback"
description: "Periodic tick that reads the latest ci-failure-scan and ci-failure-fix runs and maintainer feedback on the issues/PRs/comments they produce, scores them against separate scanner/fixer rubrics, and proposes targeted edits to ci-failure-scan.md, ci-failure-fix.md, and/or shared/create-kbe.instructions.md as a single draft PR. Maintains the KPI tracker with separate scanner and fixer metrics."

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

if: (!github.event.repository.fork)

# ###############################################################
# Select a PAT from the pool and override COPILOT_GITHUB_TOKEN.
# Run agentic jobs in an isolated `copilot-pat-pool` environment.
#
# When org-level billing is available, this will be removed.
# See `shared/pat_pool.README.md` for more information.
# ###############################################################
imports:
  - uses: shared/pat_pool.md
    with:
      environment: copilot-pat-pool

environment: copilot-pat-pool

engine:
  id: copilot
  model: claude-opus-4.8
  env:
    COPILOT_GITHUB_TOKEN: |
      ${{ case(
        needs.pat_pool.outputs.pat_number == '0', secrets.COPILOT_PAT_0,
        needs.pat_pool.outputs.pat_number == '1', secrets.COPILOT_PAT_1,
        needs.pat_pool.outputs.pat_number == '2', secrets.COPILOT_PAT_2,
        needs.pat_pool.outputs.pat_number == '3', secrets.COPILOT_PAT_3,
        needs.pat_pool.outputs.pat_number == '4', secrets.COPILOT_PAT_4,
        needs.pat_pool.outputs.pat_number == '5', secrets.COPILOT_PAT_5,
        needs.pat_pool.outputs.pat_number == '6', secrets.COPILOT_PAT_6,
        needs.pat_pool.outputs.pat_number == '7', secrets.COPILOT_PAT_7,
        needs.pat_pool.outputs.pat_number == '8', secrets.COPILOT_PAT_8,
        needs.pat_pool.outputs.pat_number == '9', secrets.COPILOT_PAT_9,
        'NO COPILOT PAT AVAILABLE')
      }}

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
      - ".github/workflows/ci-failure-fix.md"
      - ".github/workflows/shared/create-kbe.instructions.md"
    protected-files:
      policy: blocked
      exclude:
        - .github/
    labels: [agentic-workflows]
    allowed-labels: [agentic-workflows]
  push-to-pull-request-branch:
    target: "*"
    required-title-prefix: "[ci-scan-feedback] "
    max: 1
    allowed-files:
      - ".github/workflows/ci-failure-scan.md"
      - ".github/workflows/ci-failure-fix.md"
      - ".github/workflows/shared/create-kbe.instructions.md"
    protected-files:
      policy: blocked
      exclude:
        - .github/
  update-pull-request:
    target: "*"
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

You evaluate two workflows — the [`CI Outer-Loop Failure Scanner`](ci-failure-scan.md) (detection: files KBEs) and the [`CI Outer-Loop Failure Fixer`](ci-failure-fix.md) (mitigation: opens confident `[ci-fix]` fix PRs, opens help-wanted `[ci-fix]` PRs when a fix is attempted but unverified, or posts a loop-in comment on the KBE when no diff is producible) — maintain a single KPI tracker issue with a running window of metrics, and propose targeted edits to the scanner prompt, the fixer prompt, or the shared KBE authoring instructions so the next runs produce tighter, more actionable artifacts. You run read-only; the only write paths are against `.github/workflows/ci-failure-scan.md`, `.github/workflows/ci-failure-fix.md`, `.github/workflows/shared/create-kbe.instructions.md`, and the tracker issue body.

Hard rules: no comments on issues/PRs, no edits outside the three prompt/instruction files above, max 1 PR + 1 tracker issue open at a time. Reading issue/PR/comment bodies (the user-supplied content the integrity gate exists to filter) MUST go through the `github` MCP tool with `min-integrity: approved`; `[Filtered]` results are skipped (record the count, do not chase them). `gh` calls are allowed for workflow-run metadata (`gh api .../actions/...`, `gh run view --log`) and for enumerating this workflow's own artifacts (finding the `[ci-scan-feedback]` PR/tracker by title or repository-owned label), but NOT for reading maintainer-supplied content — do not use `gh issue view`, `gh pr view`, or `gh api /repos/.../comments` to substitute for the integrity-gated reads.

The two workflows are evaluated on separate axes; do NOT merge their quality numbers. The scanner is judged on KBE precision (right classification, specific signature, valid JSON). The fixer is judged on fix-PR usefulness, help-wanted-PR honesty (a real attempt + a clear ask, never a disguised mute), and loop-in-comment quality. A `[ci-scan]` KBE closed wrong and a `[ci-fix]` PR closed wrong are different failure modes with different fixes. The fixer always prefers a PR (confident or help-wanted) over a comment; a rising share of loop-in comments where a help-wanted PR was feasible is itself a fixer-quality signal.

## Steps

1. Fetch the latest 10 runs of BOTH `ci-failure-scan.lock.yml` and `ci-failure-fix.lock.yml`:

   ```bash
   for wf in ci-failure-scan ci-failure-fix; do
     gh api "/repos/dotnet/runtime/actions/workflows/${wf}.lock.yml/runs?per_page=10" \
       | tee /tmp/gh-aw/agent/runs_${wf}.json \
       | jq -r --arg wf "$wf" '.workflow_runs[] | "\($wf) \(.id) \(.conclusion) \(.head_branch) \(.event) \(.created_at) \(.html_url)"'
   done
   ```

   If `ci-failure-fix.lock.yml` has no runs yet (newly added workflow), record `ci-fix: no runs yet` and continue — the scanner half of this tick still runs.

2. For the latest 2 runs of EACH workflow, list the issues/PRs/comments they produced and download the agent log to extract the final tally table emitted by Step 6 of `ci-failure-scan.md` (`| pipeline | ...`) or Step 7 of `ci-failure-fix.md` (`| kbe | area | outcome | reason |`). Extract ONLY the tally table block (header + body rows, terminated by the first non-pipe line) — do NOT pipe arbitrary trailing log content through, since the agent logs may contain quoted maintainer-supplied content that bypasses the integrity gate:

   ```bash
   gh run view <run-id> --log \
     | awk '/^\| pipeline \|/{flag=1} /^\| kbe \|/{flag=1} flag && /^\|/{print; next} flag{exit}' \
     | tee /tmp/gh-aw/agent/tally_<run-id>.txt
   ```

3. Read in-scope feedback. Issues, PRs, and comments are in scope when EITHER the `agentic-workflows` label is present OR the title starts with `[ci-scan]` or `[ci-fix]`. For each in-scope item updated in the last 30 days, fetch body + all comments via the `github` MCP tool (NOT `gh`, per the hard rule above). Quote any maintainer comment matching: "too broad", "doesn't match", "duplicate", "wrong label", "JSON malformed", "fix-forward", "don't disable", "Known issue did not match", "should be supported", "wait for #", "wrong fix", "this isn't the cause", "don't @ me", "wrong owner", "not my area".

   Discover candidates with the `github` tool's `search_issues` and `search_pull_requests` over both label and title scopes, applying the 30-day window via the search query itself:

   - `repo:dotnet/runtime is:issue label:agentic-workflows updated:>=<today-30d>`
   - `repo:dotnet/runtime is:issue in:title "[ci-scan]" updated:>=<today-30d>`
   - `repo:dotnet/runtime is:pr in:title "[ci-scan]" updated:>=<today-30d>`
   - `repo:dotnet/runtime is:pr in:title "[ci-fix]" updated:>=<today-30d>`
   - `repo:dotnet/runtime is:pr label:agentic-workflows updated:>=<today-30d>`

   Both the closed and open state buckets are in scope (closed items often carry the most informative feedback about why the artifact was rejected). For each result, read the body and comments via the `github` tool. When listing comments, request only the most recent 100 (one page at the MCP default size) — the 30-day `updated:>=...` window is the primary filter, and threads with >100 comments are vanishingly rare for these artifacts. If a result has >100 comments, fetch the latest page only; do NOT paginate further (older comments are out-of-scope by construction). Record `integrity-filtered: N` for any `[Filtered]` results and continue.

   `[ci-fix]` loop-in comments live on `[ci-scan]` KBE issues (marker `<!-- ci-fix:handoff -->`); when you read a KBE, also read the replies to any loop-in comment — a maintainer reply that the @-mentioned owner was wrong, or that the analysis was off, is a primary feedback signal. Help-wanted `[ci-fix]` PRs (`Artifact kind: help`) carry the same signal in their review threads: a maintainer reply that the attempt was on the wrong track, or that a comment would have sufficed, feeds the fixer's confidence calibration.

4. Score each artifact against the rubric.

   **Scanner artifacts (`[ci-scan]` KBE issues):**

   - Title is scoped to a single failure shape (not a list of pipelines).
   - Classification matches the failure (KBE-eligible test/hang -> `Known Build Error`; build break -> KBE; infra noise -> no issue at all).
   - JSON block is valid: exactly one fenced `` ```json `` block, all four keys present, exactly one of `ErrorMessage`/`ErrorPattern` non-empty.
   - Signature is specific: not a bare test/method name, not a generic exception/exit-code, not a phrase that also appears in `[PASS]`/`[SKIP]` lines of the same log.
   - For a sample of in-scope KBEs, cross-check the signature against the cited failing log (`gh run view <id> --log-failed | head -300` or the AzDO/Helix URL in the body) and flag PASS-line collisions or paraphrased signatures.
   - Skip-reason vocabulary stability: any scanner tally row using a `skipped:` reason NOT in the Step 6 'Recognized values' list in `ci-failure-scan.md` is flagged as `unknown-skip-reason: <verbatim string>`.

   **Fixer artifacts (`[ci-fix]` confident PRs, help-wanted PRs, and loop-in comments):**

   - Carries the artifact marker block (`Workflow artifact: ci-fix`, `Artifact kind: fix|help|handoff`, `Linked KBE: #<n>`). Flag any missing marker.
   - Confident fix PR (`kind: fix`): diff is small (<= 20 lines, single file), is a genuine fix (NOT a test-disable / `[ActiveIssue]` / `Skip` / `<*Incompatible>` — flag any muting as a hard violation), and the body states an explicit validation command + result.
   - Help-wanted PR (`kind: help`): carries a real best-effort diff that is NOT a mute (flag any muting as a hard violation), an explicit "what is unverified / where I need help" section, and a loop-in of at most one likely author + 1–2 individual owners. A help-wanted PR closed without merge is NOT a quality miss by itself (it explicitly asked for help) — only flag it when a maintainer reply says the attempt was wrong-headed, a mute, or that no PR should have been opened. Flag a `kind: help` PR whose diff is actually validated and in bounds (it should have been `kind: fix`), and flag a loop-in comment that could clearly have been a help-wanted PR.
   - PR linkage: `Linked KBE: #<n>` present and the KBE is real and open.
   - Loop-in comment (`kind: handoff`): at most one per KBE (flag duplicates), contains a concrete root cause, mentions at most one likely author + 1–2 individual owners, and is justified (no producible diff). Flag live `@dotnet/<team>` team mentions (should be inline code) and flag mis-attributed authors called out by maintainer replies.
   - Fixer skip-reason vocabulary: any fixer tally row using a `skipped:` reason NOT in the Step 7 'Recognized skip reasons' list in `ci-failure-fix.md` is flagged as `unknown-skip-reason: <verbatim string>`.

5. Translate each failure mode into a targeted edit to whichever file owns the rule: scanner findings -> `.github/workflows/ci-failure-scan.md` or `.github/workflows/shared/create-kbe.instructions.md`; fixer findings -> `.github/workflows/ci-failure-fix.md`. Prefer rule-shaped edits (tighten a step, extend a keyword/phrase list, add a Bad/Good row, narrow a gate) over wholesale rewrites. Read the target file first; reuse the existing voice and section structure. A single PR may edit any combination of those files when the signals warrant it.

6. Emit changes. Check for an existing open `[ci-scan-feedback]` PR first:

   ```bash
   gh pr list -R dotnet/runtime --state open --search 'in:title "[ci-scan-feedback]"' \
     --json number,headRefName,url | tee /tmp/gh-aw/agent/open_feedback_prs.json
   ```

   Branch on the result:

   - Existing PR found -> emit `push_to_pull_request_branch` (with the existing PR's `pull_request_number` from the search above, since this is a scheduled run with no triggering PR) to add the new edits as a commit on that PR's branch, then emit `update_pull_request` (same `pull_request_number`) to append a new dated section to its body. Do NOT call `create_pull_request`.
   - No existing PR -> emit one `create_pull_request`. Title: `[ci-scan-feedback] <one-line summary>`.

   **Emission order.** Emit the Step 7 tracker `update_issue` / `create_issue` BEFORE these PR safe-outputs. The safe-outputs processor runs messages in emission order and cancels every later message once one fails, so a PR-push or patch error here must never cancel the daily tracker snapshot.

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

   Collect the full universe of `[ci-scan]` and `[ci-fix]` issues and PRs (open + closed) since `window_start` via `gh search issues` / `gh search prs` with `created:>=<window_start>`. This produces a list of issue/PR numbers and metadata; do NOT read bodies with `gh`. Cache the list to `/tmp/gh-aw/agent/artifacts.json` so later sections do not re-query. Tag each cached row with its workflow: title starting `[ci-scan]` -> scanner; title starting `[ci-fix]` -> fixer.

   `gh search issues --json` and `gh search prs --json` already return `state`, `stateReason`, `labels`, `mergedAt`, `closedAt`, and `author` for each result; that metadata is sufficient for the activity and quality counts below. Only fetch through the integrity-gated `github` MCP when you actually need body or comment text.

   For metrics that need MEMBER/OWNER comment content (rejection-keyword detection, re-file outage signal, hand-off engagement), fetch through the `github` MCP `issue_read get`, `pull_request_read get`, and the corresponding comments tools, one per item, respecting `min-integrity: approved`. Skip `[Filtered]` items.

   Loop-in comments are `[ci-fix]` comments on `[ci-scan]` KBE issues carrying the marker `<!-- ci-fix:handoff -->`. Count them by reading KBE comments via the `github` MCP. A loop-in has "maintainer engagement" when a MEMBER/OWNER replied after it.

   Compute these KPIs. The shape below is deliberately small: raw counts, two quality ratios (scanner and fixer, kept separate), a fixed set of outage signals. Do not re-introduce Wilson scoring, time-to-KBE, coverage ratios, or tally-extraction; they were dropped because they came back `n/a` most ticks and added noise.

   ### A) Activity (last 7d)

   Count, per artifact stream:

   - Scanner `[ci-scan]` KBE issues: `opened`, `closed_good` (closed `completed`), `closed_wrong` (closed `not_planned`/`duplicate`).
   - Fixer `[ci-fix]` PRs: `opened`, `merged`, `closed_unmerged`. Sub-split by marker `Artifact kind`: `confident` (`kind: fix`) vs `help_wanted` (`kind: help`).
   - Fixer loop-in comments: `posted` (count of new `<!-- ci-fix:handoff -->` comments), `engaged` (subset with a MEMBER/OWNER reply).

   ### B) Scanner quality (closure cohort, last 30d)

   Closure-based, not creation-based: a wrong closure of an old KBE still counts against this period. Scope = `[ci-scan]` KBE issues only.

   - `i_good_30d` — KBEs closed `completed` in the last 30d.
   - `i_wrong_30d` — KBEs closed `not_planned`/`duplicate` in the last 30d.
   - `scan_closed_30d = i_good_30d + i_wrong_30d`.
   - `scan_wrong_rate_30d = i_wrong_30d / scan_closed_30d`. Emit as `n/a` when `scan_closed_30d < 10`.
   - `complaints_30d` — count of MEMBER/OWNER comments on in-scope `[ci-scan]` artifacts (open or closed) matching (case-insensitive): `don't disable`, `do not disable`, `do not mute`, `please don't`, `false positive`, `fix forward`, `fix-forward`, `investigation in progress`, `will investigate`, `stop filing`, `noise`, `not a real failure`, `flaky test`.
   - `duplicates_30d` — KBEs closed in the last 30d with `state_reason: duplicate` OR carrying a `duplicate` label.

   ### C) Fixer quality (closure cohort, last 30d)

   Scope = `[ci-fix]` PRs (both `kind: fix` and `kind: help`) and loop-in comments. Do NOT fold these into the scanner rate. A `[ci-fix]` PR closed without merge is NOT automatically "wrong" — a maintainer may have taken over or landed an equivalent fix, and a help-wanted PR explicitly invites a maintainer to close it — so it is reported but only the rejection-keyword subset counts as a quality miss.

   - `fix_merged_30d` — `[ci-fix]` PRs merged in the last 30d.
   - `fix_unmerged_30d` — `[ci-fix]` PRs closed without merge in the last 30d.
   - `fix_rejected_30d` — subset of `fix_unmerged_30d` whose close carries a MEMBER/OWNER comment matching the section B keyword set OR `wrong fix`, `this isn't the cause`, `wrong owner`, `not my area`, `don't @ me`. A help-wanted PR closed with only neutral/thankful maintainer replies is NOT counted here.
   - `fix_closed_30d = fix_merged_30d + fix_unmerged_30d`.
   - `fix_reject_rate_30d = fix_rejected_30d / fix_closed_30d`. Emit `n/a` when `fix_closed_30d < 5`.
   - `handoff_posted_30d`, `handoff_engaged_30d` — loop-in comments posted and those with a MEMBER/OWNER reply.
   - `misattributions_30d` — loop-in comments or help-wanted PRs with a MEMBER/OWNER reply matching `wrong owner`, `not my area`, `don't @ me`, `wasn't me`, `not the cause`.

   ### D) Outage signals

   These reflect the health of the **CI being monitored**, not the scanner/fixer workflows. Each signal has a fixed threshold and renders as 🔴 when tripped, otherwise 🟢.

   Source data comes from the cached artifact list; `[ci-scan]` KBE issues are proxies for distinct stable failure signatures in CI. For each KBE issue, fetch the body via the `github` MCP and grep for the line `Build error leg or test failing: <value>` (this is the only place the KBE template carries leg information). The value is `<AzDO leg name>-<assembly or test name>` where the separator is the LAST `-` in the value: split on the last hyphen and treat the left side as the leg. Cache `(issue_number, leg)` rows to `/tmp/gh-aw/agent/artifact_legs.tsv`. The pipeline-outage signal counts distinct legs because the KBE body does not capture the AzDO pipeline definition id reliably; treat distinct legs as a conservative proxy for distinct pipelines.

   | signal | source | threshold |
   |---|---|---|
   | New-KBE burst | count of `[ci-scan]` KBE issues created per day in the last 7d | any day > 2x trailing 30d daily median (min absolute count 3 to avoid noise) |
   | Build-break spike | count of `[ci-scan] Build break:` issues created per 24h | >= 2 in any 24h window in the last 7d |
   | Multi-pipeline outage (distinct legs proxy) | distinct leg names (parsed per above) across KBEs created in the last 24h | >= 3 distinct legs |
   | KBE re-filed after maintainer close | for each `[ci-scan]` KBE issue opened in the last 7d, search `is:issue is:closed -is:open in:title "<test-name-stem>" closed:>=<14d-ago>` and check whether any closed predecessor exists with a MEMBER/OWNER comment matching the keyword set in section B | any in the last 7d |
   | Scanner wrong-closure rate | section B's `scan_wrong_rate_30d` | >= 30% with `scan_closed_30d >= 10` |
   | Fixer rejection rate | section C's `fix_reject_rate_30d` | >= 30% with `fix_closed_30d >= 5` |

   Emit a body with this exact shape (regenerate every tick):

   ````markdown
   <!-- ci-scan-feedback:kpi-tracker -->
   <!-- ci-scan-feedback:window-start=<window_start> -->
   Tracking quality of `[ci-scan]` (detection) and `[ci-fix]` (mitigation) issues, PRs, and loop-in comments since <window_start>. Updated every tick of [ci-failure-scan-feedback.lock.yml](https://github.com/dotnet/runtime/blob/main/.github/workflows/ci-failure-scan-feedback.lock.yml). To raise a concern, comment here or on any `[ci-scan]`/`[ci-fix]` issue/PR; the next tick reads in-scope feedback and either opens a `[ci-scan-feedback]` PR with prompt edits or pushes to the existing one.

   ## Snapshot — <UTC timestamp>

   ### Activity (last 7d)

   | stream | opened | good | wrong/unmerged |
   |---|---|---|---|
   | Scanner KBE issues | <i_op_7> | <i_good_7> | <i_wrong_7> |
   | Fixer `[ci-fix]` PRs (confident) | <p_fix_op_7> | <p_fix_merged_7> | <p_fix_unmerged_7> |
   | Fixer `[ci-fix]` PRs (help-wanted) | <p_help_op_7> | <p_help_merged_7> | <p_help_unmerged_7> |
   | Fixer loop-in comments | <handoff_posted_7> | <handoff_engaged_7> engaged | — |

   "good" = KBEs closed `completed`, PRs merged, loop-ins with a maintainer reply. "wrong/unmerged" = KBEs closed `not_planned`/`duplicate`, PRs closed without merge (note: a help-wanted PR closed unmerged is expected, not necessarily a miss).

   ### Scanner quality (last 30d)

   | metric | count | rate |
   |---|---|---|
   | KBE closures | <scan_closed_30d> | — |
   | Wrong KBE closures | <i_wrong_30d> | <scan_wrong_rate_30d_pct or `n/a (<scan_closed_30d><10)`> |
   | Maintainer rejection comments | <complaints_30d> | — |
   | Duplicate KBEs | <duplicates_30d> | — |

   ### Fixer quality (last 30d)

   | metric | count | rate |
   |---|---|---|
   | Fix PR closures | <fix_closed_30d> | — |
   | Fix PRs merged | <fix_merged_30d> | — |
   | Fix PRs rejected (maintainer pushback) | <fix_rejected_30d> | <fix_reject_rate_30d_pct or `n/a (<fix_closed_30d><5)`> |
   | Loop-in comments (posted / engaged) | <handoff_posted_30d> / <handoff_engaged_30d> | — |
   | Loop-in / help-wanted mis-attributions | <misattributions_30d> | — |

   If `ci-failure-fix.lock.yml` has no runs yet, render this table's counts as `0` and add the note `_Fixer not yet active._` below it.

   ### Outage signals (analyzed CI)

   | signal | threshold | 24h | 7d | status |
   |---|---|---|---|---|
   | New-KBE burst | day > 2x trailing 30d median (min 3) | <new_kbe_24h> / median <median_daily_kbe_30d> | peak day <peak_kbe_7d> | <🔴 or 🟢> |
   | Build-break spike | >= 2 in any 24h | <bb_24h> | <bb_7d> | <icon> |
   | Multi-pipeline outage (distinct legs proxy) | >= 3 distinct legs in 24h | <legs_24h> distinct | <legs_peak_7d> distinct (peak day) | <icon> |
   | KBE re-filed after maintainer close | any in 7d | <refile_24h> | <refile_7d> | <icon> |
   | Scanner wrong-closure rate (30d) | >= 30% with `scan_closed_30d >= 10` | — | <scan_wrong_rate_30d_pct> | <icon> |
   | Fixer rejection rate (30d) | >= 30% with `fix_closed_30d >= 5` | — | <fix_reject_rate_30d_pct> | <icon> |

   For each signal at 🔴, emit one `details:` line **after** the Outage signals table (not inside it; markdown tables cannot carry sub-rows). Prefix each line with the signal name so it is unambiguous which row it explains. Example:

   ```
   details: Multi-pipeline outage — runtime-coreclr outerloop linux x64 checked, runtime-extra-platforms windows x86 release, runtime-interpreter linux x64 checked all red on 2026-06-01
   details: KBE re-filed after maintainer close — #128793 re-filed #128737
   ```

   Omit the details block entirely when no signal is 🔴.
   ````

   Suppression rules:

   - If `scan_closed_30d < 10`, the Scanner quality table's `rate` reads `n/a (<n><10)` for the wrong-closure row; other rows still render raw counts.
   - If `fix_closed_30d < 5`, the Fixer quality table's `rate` reads `n/a (<n><5)` for the rejection row.
   - Outage signals always render. An explicit 🟢 with no data still carries information.
   - Do NOT emit charts (mermaid or otherwise).
   - Do NOT emit historical weekly buckets. The body is a current snapshot.

   If the tracker exists -> emit one `update_issue` with the new body. If not -> emit one `create_issue` titled `[ci-scan-feedback] KPI Tracker`. Either way, this step ALWAYS fires (never call `noop` for the tracker — a daily snapshot is the point). Emit this tracker output BEFORE the Step 6 PR safe-outputs (see the Step 6 "Emission order" note) so a PR-push failure cannot cancel the snapshot.

## Output to agent log

Print the rubric scorecards to the agent log so the next tick can grep them. Scanner artifacts:

```
| run-id | artifact | title-scoped | classification | json-valid | signature-specific | log-cross-check | maintainer-feedback |
```

Fixer artifacts:

```
| run-id | artifact | kind | marker-present | small-validated-fix | no-muting | kbe-linked | mention-discipline | maintainer-feedback |
```

One row per artifact scored. Skip rows where every column is `pass`. Append a final line `[Filtered] count: <n>` so out-of-integrity items are visible without being followed.
