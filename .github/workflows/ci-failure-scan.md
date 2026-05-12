---
name: "CI Outer-Loop Failure Scanner"
description: "Periodic scan of runtime-extra-platforms and outer-loop CI pipelines (JIT/GC stress, PGO, libraries-jitstress, etc.). Files Known Build Errors so failures are immediately ignorable in PR CI; opens companion skip PRs to remove the failure permanently after human review."

permissions:
  contents: read
  issues: read
  pull-requests: read

on:
  schedule: every 12h
  workflow_dispatch:
  roles: [admin, maintainer, write]
  permissions: {}

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
  group: "ci-failure-scan"
  cancel-in-progress: true

tools:
  github:
    toolsets: [pull_requests, repos, issues, search]
    min-integrity: approved
  edit:
  bash: ["dotnet", "git", "find", "ls", "cat", "grep", "head", "tail", "wc", "curl", "jq", "tee", "sed", "awk", "tr", "cut", "sort", "uniq", "xargs", "echo", "date", "mkdir", "test", "env", "basename", "dirname", "bash", "sh", "chmod"]

checkout:
  fetch-depth: 50

safe-outputs:
  create-pull-request:
    title-prefix: "[ci-scan] "
    draft: true
    max: 10
    protected-files: blocked
    allowed-files:
      - "src/libraries/**"
      - "src/coreclr/**"
      - "src/mono/**"
      - "src/tests/**"
      - "src/native/**"
      - "eng/testing/**"
    labels: [agentic-workflows]
    allowed-labels: [agentic-workflows]
  create-issue:
    max: 5
    labels: [agentic-workflows]
    allowed-labels: ["Known Build Error", "blocking-clean-ci"]

timeout-minutes: 90

network:
  allowed:
    - defaults
    - github
    - dev.azure.com
    - helix.dot.net
    - "*.blob.core.windows.net"
---

# CI Outer-Loop Failure Scanner

Platform-agnostic scan of `dnceng-public/public` outer-loop CI pipelines on `main`. Every actionable failure becomes either a draft PR (per-test fix) or a tracking issue (everything else). The intent is to keep outer-loop pipelines green without waiting on humans to file issues.

## Pipelines to scan

Iterate over every pipeline in this list. For each, fetch builds on branch `main` filtered to `resultFilter=succeeded,failed,partiallySucceeded` (skip `canceled`). Pick the most recent such build as the "latest", then look back through ~10 prior completed builds to compute first-seen-in-window and occurrence counts.

| Pipeline | Definition ID | Notes |
|----------|---------------|-------|
| runtime-extra-platforms | 154 | Apple mobile, Android, browser, wasi, NativeAOT outer loop |
| runtime-coreclr outerloop | 108 | |
| runtime-coreclr jitstress | 109 | JIT stress modes |
| runtime-coreclr jitstressregs | 110 | |
| runtime-coreclr jitstress2-jitstressregs | 111 | |
| runtime-coreclr gcstress0x3-gcstress0xc | 112 | |
| runtime-coreclr gcstress-extra | 113 | |
| runtime-coreclr r2r-extra | 114 | |
| runtime-coreclr jitstress-isas-x86 | 115 | |
| runtime-coreclr jitstress-isas-arm | 116 | |
| runtime-coreclr jitstressregs-x86 | 117 | |
| runtime-coreclr libraries-jitstressregs | 118 | |
| runtime-coreclr libraries-jitstress2-jitstressregs | 119 | |
| runtime-coreclr r2r | 120 | |
| runtime-coreclr gc-simulator | 123 | |
| runtime-coreclr crossgen2 | 124 | |
| runtime-jit-experimental | 137 | OSR / partial compilation |
| runtime-coreclr libraries-jitstress | 138 | |
| runtime-coreclr ilasm | 140 | |
| runtime-coreclr pgo | 144 | |
| runtime-coreclr libraries-pgo | 145 | |
| gc-standalone | 146 | ADO name differs from display name |
| runtime-coreclr superpmi-replay | 150 | |
| runtime-coreclr superpmi-asmdiffs-checked-release | 153 | |
| runtime-coreclr jit-cfg | 155 | Control flow guard |
| runtime-coreclr jitstress-random | 159 | Stress mode value comes from logs |
| runtime-coreclr libraries-jitstress-random | 160 | Stress mode value comes from logs |
| runtime-coreclr pgostress | 230 | |
| runtime-coreclr jitstress-isas-avx512 | 235 | |
| runtime-nativeaot-outerloop | 265 | |
| runtime-diagnostics | 309 | |
| runtime-interpreter | 316 | ADO name differs from display name |
| runtime-libraries-interpreter | 330 | ADO name differs from display name |

If a pipeline has no completed build in the last 7 days, skip it silently.

## Skills to consult per failure

Read the relevant skill before classifying / fixing. Skills live under `.github/skills/`.

- **Mobile (`ios`, `tvos`, `maccatalyst`, `android`, `iossimulator`, `tvossimulator`)** → `mobile-platforms/SKILL.md`. Pipeline layout, platform helpers, code-path map.
- **JIT / GC / PGO stress** (definitions 109–160, 230, 235; `runtime-jit-experimental`) → `jit-regression-test/SKILL.md` for repro extraction; `ci-pipeline-monitor/SKILL.md` for triage and failure-shape recognition. JIT product fixes are out of scope for autofix — file an issue and `@`-mention the JIT area owners.
- **Browser/WASM, WASI** (extra-platforms) → consult `mobile-platforms/SKILL.md` (the WASM/WASI sections) for build-time conditional patterns; `extensions-review/SKILL.md` if the failure is in `Microsoft.Extensions.*` tests; `system-net-review/SKILL.md` if the failure is in `System.Net.*` tests.
- **NativeAOT outer loop** → check `eng/testing/tests.*aot*.targets` and the test `.csproj` for AOT-specific conditions before suggesting a fix.
- **Generic CI triage** → `ci-pipeline-monitor/SKILL.md` for known-failure-shape patterns and Build Analysis matching.

## Outcome (per actionable failure)

The primary purpose of this workflow is to keep PR CI green. **KBE** = Known Build Error: an issue tagged `Known Build Error` whose body contains a JSON `ErrorMessage`/`ErrorPattern` block that Arcade Build Analysis matches against future failure logs to mark them as already-tracked, so unrelated PRs aren't blocked. KBEs are immediately effective for PR CI; muting PRs are not effective until merged by a human (latency ≥ 12h, often days). The workflow runs every 12h and converges on **two artifacts per failure across two runs**: KBE in run N (immediate), muting PR in run N+1 (permanent after merge), with a small-fix PR added in run N+1 when scope allows.

### Per-failure deliverables

For each actionable failure, produce **up to three artifacts**:

1. **KBE** — immediate Build Analysis signal so PR CI is unblocked right away. Always produced (or reused if one already exists) for stable-signature failures.
2. **Muting PR** — small, clean, mergeable PR that just adds `[ActiveIssue(...)]` / `<GCStressIncompatible>` referencing the KBE. No diagnosis logic, no product code. Designed to be merge-without-thinking by any maintainer who agrees the failure should be silenced. Always produced when (1) is produced.
3. **Fix PR** — actual product/test code fix. Produced **only when** (a) the root cause is clear from the failure log, (b) the change fits the "small product fix opportunity" bounds (≤ 20 lines, single file, non-API, non-JIT-codegen, non-GC, non-threading, non-security), and (c) the failing test verifies the fix. Otherwise the deeper investigation is left to the area owner via the KBE — do NOT attempt a speculative fix PR.

The muting PR and the fix PR are independent: a maintainer can merge the muting PR immediately (CI goes green) and then iterate on the fix PR at human pace. If the fix PR lands first, the muting PR becomes a no-op and can be closed; if the muting PR lands first, the fix PR removes the `[ActiveIssue]` annotation.

### Two-pass KBE → PR flow (across runs)

Same-run KBE + PR is not possible: gh-aw strict mode forbids `issues: write` on the agent job, so the agent cannot create issues at runtime — it can only emit safe-outputs `create_issue` directives that are processed by a separate post-agent job after the agent finishes. Issue numbers are therefore never visible to the agent during execution. Patches cannot reference an issue number that doesn't exist yet.

The agent must accept this constraint and produce KBEs in run N, then companion PRs in run N+1. The 12-hour cadence makes this acceptable: the KBE alone unblocks PR CI immediately (the moment the safe-outputs job processes it, ~1 min after the agent finishes), and the muting PR follows within 12h.

For each actionable failure, walk through all six checks below before deciding the action — multiple can fire at once, and any one is reason to stop.

#### Step 1 — Look for an existing KBE.

Search `is:issue is:open label:"Known Build Error" in:body "<error-signature>"`. Try variations: the full `[FAIL]` line, the assertion text, the exception class plus the test name. On a hit, record the issue number as `existing-kbe` and continue — finding a KBE doesn't end the walk, it changes the final action.

#### Step 2 — Look for an area-team tracker without the KBE label.

Some teams track recurring failures in plain issues. Search `is:issue is:open in:title "<test-name>"` together with `in:body "<test-file-path>"`. On a hit, record it as `linked-tracker` — but **do not** treat the tracker as a substitute for a KBE. Build Analysis only matches against issues that carry the `Known Build Error` label and a valid JSON body, so a plain tracker won't unblock PR CI on its own. File a new KBE for Build Analysis to match against, and cross-link the tracker (`Tracking: dotnet/runtime#<n>`) inside the KBE body and the muting PR body.

#### Step 3 — Look for an existing muting PR.

Search `is:pr is:open in:title "<test-name>" "[ci-scan]"` and `is:pr is:open "<test-name>" ActiveIssue`. On a hit, record `→ existing-PR #<n>` (muting) and stop.

#### Step 4 — Look for an in-flight fix PR by anyone.

Search broadly — not only `[ci-scan]` PRs — by test name, file path, and assembly: `is:pr is:open "<test-name>"`, `is:pr is:open "<test-file-path>"`, `is:pr is:open "<assembly>" in:title`. For each candidate, fetch the PR body; if it claims to fix this failure (or links the same KBE), stop and record `→ existing-PR #<n>` (in-flight fix).

#### Step 5 — Verify every issue number you're about to write actually exists.

For every `<n>` you plan to embed in source (`[ActiveIssue("...issues/<n>")]`, `Linked KBE: #<n>`, the inline `<!-- ...issues/<n> -->` comment), call the github tool `issue_read` with method `get` (`{"method": "get", "owner": "dotnet", "repo": "runtime", "issue_number": <n>}`) and confirm it returns an open issue. If it doesn't, stop — a dead-link annotation in source requires a follow-up PR to remove.

#### Step 6 — Confirm muting is welcome on this issue.

Read the candidate KBE / tracker's body and its most recent area-owner comment. Skip muting (record `→ skipped: do-not-mute on issue #<n>` and stop) if any of the following holds:

- The body or a recent comment from an area owner explicitly says not to mute, disable, or skip — e.g. "please don't disable these tests", "do not mute", "keep failing", "investigation in progress".
- The issue is labeled with anything semantically equivalent (verify the label exists in `dotnet/runtime` before relying on it; do not invent labels).
- The most recent area-owner comment (within the last 14 days) actively opposes muting on procedural grounds — e.g. requesting a fix-forward, awaiting a JIT/GC repro.

When in doubt, skip muting and let the next run revisit; over-muting against an active investigation is the failure mode this step exists to prevent.

#### What action to take

- **Step 1 found nothing** → file a new KBE via safe-outputs `create_issue` with the body template below, title prefix `[ci-scan] `, and only the labels `Known Build Error` and `blocking-clean-ci`. If Step 2 found a tracker, cross-link it as `Tracking: dotnet/runtime#<tracker>` in the KBE body. The issue number isn't visible during this run, so the muting PR is deferred to the next run.
- **Step 1 found a valid KBE, AND steps 3–6 are clean** → open the muting PR via safe-outputs `create_pull_request`. Diff ≤ 5 lines, only test annotations or csproj flags. The body must include `Linked KBE: #<n>` as a top-level line plus the four-question verification block below. If Step 2 also found a tracker, cite it as `Tracking: dotnet/runtime#<tracker>` alongside `Linked KBE`.
- **Plus, if the failure satisfies the "small product fix opportunity" criteria above** → open a separate fix PR on its own branch. Body cites (a) the failing test as evidence, (b) the root cause, (c) why the fix is safe, (d) `Linked KBE: #<n>`, and (e) "If this lands before #<muting-PR>, that PR can be closed." Kept separate so a maintainer can take one without the other.

#### Before you link a KBE, verify it actually matches

Test-name overlap alone is not enough — common wrong-link patterns include reusing a KBE filed against a different architecture, or one about an exception class for a failure that's actually a work-item timeout. Answer all four before writing `Linked KBE: #<n>` or `[ActiveIssue("...issues/<n>")]`:

1. Does the candidate KBE describe the **same test (or test family)** as the current `[FAIL]` line?
2. Does its `ErrorMessage` / quoted exception text describe the **same failure signature** (exception class, assertion message)?
3. Is the failing **OS** in the set the KBE says it impacts?
4. Is the failing **architecture** in the set the KBE says it impacts?

If any answer is no, file a fresh KBE this run and defer the muting PR. Embed the four answers in the PR body's "Reasoning" section; PRs missing this, or with an unaddressed mismatch, will be closed.

Optional fifth check when the candidate KBE is older than ~14 days: confirm Build Analysis is still actually matching it. The hit count appears in the issue body and is rewritten by Build Analysis on every match — a stale, never-edited body is a hint the signature went bad. `gh api graphql` over `userContentEdits` on the issue gives the edit timeline.

#### Caps and end-of-run check

Per-run caps: `create_issue` max 5, `create_pull_request` max 10. On cap, record `→ skipped: cap reached` — the next run picks them up.

Before stopping, confirm each failure is handled:

- **No existing KBE** → KBE filed?
- **Existing KBE, no muting PR yet** → muting PR opened (and the optional fix PR if criteria are met)?
- **Existing KBE plus existing muting PR or in-flight fix** → `→ existing-PR #<n>` recorded?

If the answer is no for any failure, the run is incomplete.

### Per-failure-class rules

The two-pass flow above applies to all classes below. "KBE + muting PR" means: KBE in the run that first encounters the failure, muting PR in the next run that finds the KBE already exists.

- **Recurring failure with a stable error signature** (≥ 2 occurrences on `main` in the scanned window) → KBE (run N) + muting PR (run N+1) + fix PR (optional, run N+1, only if criteria met).
- **Per-test platform / configuration incompatibility** (e.g., test fails only under `jitstress=2`, `gcstress=0xC`, on a single mobile arch, on browser, on NativeAOT) → KBE (run N) + muting PR (run N+1). The muting PR's skip condition MUST be **as narrow as the observed failure scope** — only the OS / arch / config combinations that actually fail.

  | Observed failure scope | ❌ Bad (too broad) | ✅ Good (matches scope) |
  |---|---|---|
  | Only `linux-arm` fails | `[SkipOnPlatform(TestPlatforms.AnyUnix, ...)]` or muting on all NativeAOT | `<CLRTestTargetUnsupported Condition="'$(TargetOS)' == 'linux' and '$(TargetArchitecture)' == 'arm'">true</CLRTestTargetUnsupported>` |
  | Only NativeAOT on a single arch | `<NativeAotIncompatible>true</NativeAotIncompatible>` (all arches) | `<NativeAotIncompatible Condition="'$(TargetArchitecture)' == 'arm'">true</NativeAotIncompatible>` |
  | Only one stress mode | `<GCStressIncompatible>true</GCStressIncompatible>` (all stress modes) | Add stress-mode predicate, e.g. gate via the existing `GCStressIncompatible` only for the failing variant |

  In the PR's "Reasoning" section, list the exact set of failing legs (definition + queue + stress mode) that justifies the chosen condition, so a reviewer can verify scope matches evidence.
- Allowed muting PR mechanisms:
  - `[SkipOnPlatform(TestPlatforms.<plat>, "<reason>")]` for platform-specific failures.
  - `[ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.<helper>))]` narrowed via existing helpers.
  - `[ActiveIssue("https://github.com/dotnet/runtime/issues/<N>", TestPlatforms.<plat>)]` referencing the KBE.
  - For JIT/GC stress: `[ActiveIssue("...", typeof(TestLibrary.PlatformDetection), nameof(TestLibrary.PlatformDetection.IsStressTest))]` or `<GCStressIncompatible>true</GCStressIncompatible>` at the csproj level. **Tradeoff**: stress-guarded skips remove the test signal from the stress pipelines, so the bug becomes invisible in those pipelines until the JIT fix lands. The KBE filed in run N is what keeps the JIT team aware; without that KBE, the muting PR alone would silently lose the signal.
- **Build break on a single leg** (`Build product` or similar failed; `Send to Helix` skipped) → if the compile error has a clear, mechanical root cause and the fix is **≤ 20 lines in a single file** (e.g., obvious typo, missing `#if`, wrong type cast, missing `using`), open a fix PR (no KBE — Build Analysis explicitly forbids KBEs for build breaks). If the fix is non-trivial, file a regular tracking issue and reference the failing source file and compile error.
- **Anything else** — multi-assembly cluster, infrastructure (queue exhaustion / dead-letter / device-lost) — file a tracking issue (not a KBE). Group all infra failures from one run into a single issue. Before filing, `search_issues` for an open issue whose title or body matches the same failure signature and skip silently if one already exists (do not duplicate, do not append a comment — the agent only has read permission on existing issues).

For each failure compute a `(definition_id, work_item_or_phase, queue, stress_mode, [FAIL] or compile-error signature)` signature. Look back through ~10 completed builds in the same definition to build first-seen-in-window timestamp and occurrence count.

**Convergence target**: across two consecutive runs, every actionable test/runtime failure ends up with both (a) a KBE filed (immediate effect on PR CI via Build Analysis) and (b) a clean muting PR open against that KBE (permanent effect after merge, low review cost). The fix PR is a bonus when the root cause is obviously small. A tracking-issue-only outcome is acceptable only for build breaks (which Build Analysis cannot match) and infra failures.

Do not emit `noop`. Either a PR or an issue must come out of every actionable failure.

Cap: **10 PRs and 5 issues per run.** Group failures that share one fix into a single PR. Group failures with the same root cause into a single issue.

## Data sources

- AzDO REST: `https://dev.azure.com/dnceng-public/public/_apis/build/...`. Anonymous access only — do **not** call `_apis/test/...` or `vstmr.dev.azure.com`; both redirect to sign-in. Stay on `builds`, `builds/{id}/timeline`, `builds/{id}/logs/{logId}`.
  - List builds: `?definitions={id}&branchName=refs/heads/main&statusFilter=completed&resultFilter=succeeded,failed,partiallySucceeded&%24top=20&api-version=7.1`.
  - Timeline: `/builds/{id}/timeline?api-version=7.1` returns a flat `records[]` array; reconstruct the tree via `parentId`.
  - Failed-leaf rule: a record with `result == "failed"` whose log id is non-null is a leaf to inspect; failed Stage/Phase records without a failed child Job indicate a build break — open the parent Phase log and the most recent non-succeeded Task log.
- Helix REST: `https://helix.dot.net/api/jobs/{jobId}/workitems?api-version=2019-06-17`. Helix job IDs come from the `Send to Helix` Task log, which is a child of the failed Job. Each work item has `Name`, `State`, `ExitCode`, `ConsoleOutputUri`. Failed: `ExitCode != 0` or `State == "Failed"`. Console URIs containing `helix-workitem-deadletter` are dead-lettered (queue had no agent) — group as infra.
- Build Analysis attachment (best-effort, may 404): `https://dev.azure.com/dnceng-public/public/_apis/build/builds/{id}/attachments/Build_Analysis_KnownIssues_v1?api-version=7.1`. Use to dedupe against already-known issues. A 404 means none were attached; do not fail.

## Failure classification

Classify every failed timeline record before deciding whether to PR or file an issue. The timeline graph is `Stage → Phase → Job → Task`. Walk it as follows:

1. List every record with `result == "failed"`. For each failed Job, list its child Tasks (records whose `parentId == job.id`).
2. **Build break (no test ever ran)**: among the Job's Tasks, the failed Task is `Build product`, `Build native components`, `Configure CMake`, or any pre-test compile step, **and** the `Send to Helix` Task is `skipped`. → tracking issue. Do **not** attempt a test-side fix.
3. **Phase/Stage-only failure with no failed Job underneath**: typical of compile-time breaks aggregated at the phase level (e.g. `windows-arm64 checked` on the JIT stress pipelines). Open the Phase log and the latest log of any non-succeeded child Task; classify as build break and file a tracking issue.
4. **Send to Helix succeeded but the Job still failed**: open the `Send to Helix` log, extract Helix job IDs (look for `Job <GUID> on <Queue>` or `JobId: <GUID>`; the Helix info-mart log entry that always appears is `Sent Helix Job: <GUID>`), then query Helix for failed work items. This is the test-failure path.
5. **Helix work item failure**: confirm via `ConsoleOutputUri`. `helix-workitem-deadletter` URIs → infra (group into one issue). Otherwise fetch the console log, find the `[FAIL]` line, and proceed to PR vs issue selection.
6. **Infra-shaped Job failure** without Helix workitems (e.g., `Initialize job` failed, agent disconnect, "Pool is offline") → file a single grouped infra issue, do not retry per-leg.

Drill into one representative console log per signature to confirm the shape before classifying.

## PR body

Five H2 sections, in this exact order:

1. **Reasoning** — why the test fails on the affected platform/configuration; why the chosen attribute is the right fix; why this is a test-side fix and not a product bug.
2. **Impact on platforms** — bullet list of `(pipeline + platform/arch + Helix queue + stress mode + exit code)` per affected occurrence.
3. **Errors log** — sanitized excerpt from the Helix console log (the `[FAIL]` line, the assertion or exception, and the `Failed tests:` summary). Strip JWTs, bearer tokens, `ApplicationGatewayAffinity*=`, and per-user paths.
4. **First build it occurred** — first build in the scanned window where this signature appeared: build link, finish time, commit SHA, occurrences-in-window count. State explicitly that this is computed within the scanned window and may not be the true origin.
5. **Linked issue** (optional) — if an `ActiveIssue` reference is used, link the issue.

Branch from `origin/main`. Stage only the files you intend to change with `git add <specific path>`; never `git add -A`. Verify with `git diff --name-only --cached` before committing. Do not include any labels in the PR (see "Outputs: title and labels" below).

## Issue body

Use this when a PR is not the right tool — product regression, native crash, multi-assembly cluster, infra requiring an owner, JIT/GC product bug. Same four sections as a PR (Reasoning, Impact on platforms, Errors log, First build it occurred), plus a fifth:

5. **Recommended action** — concrete next step: which area owner, which file likely needs the fix, or what investigation would localize the root cause. For JIT/GC issues include the exact stress mode env vars and the JIT method-name from the log. Reference any related PR or issue you found via `search_issues`. The issue must be actionable — a checkbox-ready task list, not just "FYI".

Do not include any labels in the issue creation request (see "Outputs: title and labels" below).

### JIT pipeline issue template (definitions 109–160, 230, 235, 108, 137, 144–145, 150, 153)

For tracking issues filed against a JIT, GC, PGO, or stress pipeline, use this body layout instead of the generic "five sections" above (matches the in-repo convention; see #125685 for the canonical example):

```
**Summary:**
  <one-line description of the failure shape>

**Failed in (<N>):**
- [<pipeline name> <build number>](<build url>)
- [<pipeline name> <build number>](<build url>)
- ...

**Console Log:** [Console Log](<one representative helix console log url>)

**Failed tests:**
(use a fenced code block; per-pipeline, list the failing legs and tests)
<pipeline-name-1>
- <leg name e.g. net11.0-windows-Release-x64-jitstress2_jitstressregs8-Windows.10.Amd64.Open>
  - <test assembly or test name>
  - <test assembly or test name>
<pipeline-name-2>
- <leg name>
  - <test assembly>

**Error Message:**
(fenced code block with the canonical error line)

**Stack Trace:**
(fenced code block with the relevant stack trace; trim noise but keep the failing frame)
```

This format makes the issue immediately actionable for JIT/GC owners (@JulieLeeMSFT, @BruceForstall, @jakobbotsch, @dotnet/jit-contrib) without further drilldown. Area triage (`area-CodeGen-coreclr` / `area-GC-coreclr` / `area-PGO-coreclr` / `area-Tools-ILVerification`) is added later by a human reviewer — do not propose any `area-*` label yourself.

## Outputs: title and labels

- **All issues and PRs MUST have title prefix `[ci-scan] `**, including tracking issues, Known Build Error issues, and muting PRs. Examples:
  - `[ci-scan] Test failure: <fully.qualified.TestName> on <pipeline>`
  - `[ci-scan] Known Build Error: <short description>`
  - `[ci-scan] Skip <test-or-family> under <stress-or-platform> (refs #<n>)`
- **Do not use the word "Mute" or "Muting"** in titles. Use "Skip", "Disable", "Suppress", or "Exclude" depending on the mechanism. Examples: "Skip … under GCStress", "Disable … on tvOS", "Suppress … in MiniFull AOT mode".
- **Labels (hard restriction).** You **MUST NOT** propose any labels in your output. The workflow auto-applies `agentic-workflows` to every issue and PR, and additionally permits **only** `Known Build Error` and `blocking-clean-ci` on Known Build Error issues. Any other label — `os-*`, `area-*`, `arch-*`, `disabled-test`, `jit-stress`, `gc-stress`, `pgo`, `nativeaot`, `untriaged`, etc. — is rejected by `safe-outputs.allowed-labels` and **will be dropped**. Do not invent new labels under any name. Area, OS, and arch triage is performed by a human reviewer after the issue/PR is filed; do not attempt to pre-apply or guess them.

## Known Build Error issue

A Known Build Error is a tracking issue that Arcade Build Analysis (https://github.com/dotnet/arcade/blob/main/Documentation/Projects/Build%20Analysis/KnownIssueJsonStepByStep.md) automatically matches against future failures so PRs aren't blocked by an already-tracked flake.

File one when **all** of the following hold:
- The failure has occurred ≥ 2 times in the scanned window on `main`.
- The error has a stable substring or regex signature that uniquely identifies it.
- No fix PR is currently open (verify via `search_pull_requests`).
- The failure is **not** a build break or an infrastructure failure — only test failures or hangs are eligible for a KBE. Build breaks and infra failures (for example dead-letter, device-lost, or agent-disconnect issues) must use a regular tracking issue.

Required structure: match the headings exactly. The literal body MUST look like one of the two templates below — pick exactly one (literal substring is the default; regex only if no single literal line is specific enough). Do not emit both blocks. The outer fence in this prompt uses `~~~` (tildes) only so the inner ` ``` ` fences stay literal; in the issue you emit, do **not** use tildes anywhere — emit only the inner content between (but not including) the `~~~` lines for the template you chose. Walk the "Verify the body before submitting" checks below before committing to the issue body.

**Template A — literal substring match (default).** Pick this when the failure log contains a stable, specific assertion or exception message line.

~~~
## Build Information
Build: <link to the dev.azure.com build that first hit this in window>
Build error leg or test failing: <AzDO leg name>-<assembly or test name>
Pull request: <link to the PR if the build was a PR build, otherwise omit this line>

## Error Message

<!-- ErrorMessage is a literal String.Contains substring (case-sensitive, ordinal). Set BuildRetry to `true` only for clear infra flakes. ExcludeConsoleLog skips helix log scanning. -->

```json
{
  "ErrorMessage": "<exact substring from the failure log; the assertion or exception message text — never a bare test name>",
  "BuildRetry": false,
  "ExcludeConsoleLog": false
}
```
~~~

**Template B — regex match.** Pick this only when no single literal line is specific enough. Anchored, prefer `[^\\n]*` over `.*`, no catastrophic backtracking. The JSON value itself must be a single-line string with no real newlines, but you can match across log lines via the regex `\n` escape inside that string or via the array form.

~~~
## Build Information
Build: <link to the dev.azure.com build that first hit this in window>
Build error leg or test failing: <AzDO leg name>-<assembly or test name>
Pull request: <link to the PR if the build was a PR build, otherwise omit this line>

## Error Message

<!-- ErrorPattern is a regex with .NET options Singleline | IgnoreCase | NonBacktracking and a 50ms-per-line timeout. Set BuildRetry to `true` only for clear infra flakes. ExcludeConsoleLog skips helix log scanning. -->

```json
{
  "ErrorPattern": "<single-line anchored regex; use `[^\\n]*` instead of `.*`>",
  "BuildRetry": false,
  "ExcludeConsoleLog": false
}
```
~~~

#### Verify the body before submitting

Build Analysis is strict: a malformed JSON block or an over-broad signature means the issue is silently skipped or matches every passing run. Walk these checks; fix and re-check on any failure. Canonical upstream reference (worth reading in full before filing your first KBE): [`dotnet/arcade-skills/.../kbe-issue-creation.md`](https://github.com/dotnet/arcade-skills/blob/main/plugins/dotnet-dnceng/skills/ci-analysis/references/kbe-issue-creation.md).

1. **The body contains a fenced JSON block.** Without it Build Analysis has nothing to parse. Prose `**Error Message:**` / `**Stack Trace:**` sections don't count.
2. **Exactly one fenced JSON block.** Multiple skeletons yield zero matches.
3. **The opening fence is exactly three backticks followed by `json`**, lowercase, with nothing else on the line. Four backticks, missing lang tag, or trailing whitespace causes the parser to skip the issue.
4. **The closing fence is exactly three backticks**, same length as the open.
5. **Exactly one of `ErrorMessage` or `ErrorPattern` is present and non-empty.** Populating both is undefined behavior — Build Analysis may apply only one and you don't control which. Do not leave the unused field as `""` either; delete it. Empty signatures match nothing.
6. **The signature is not a bare identifier.** A fully-qualified test name, a stack-frame line, or a bare exception type all appear in `[PASS]` and `[SKIP]` lines for the same test, so the signature would match every passing run going forward. This applies to BOTH `ErrorMessage` and `ErrorPattern` — a regex like `TestMethodName` or `Some\\.Class\\.TestMethod` is just as broken as the literal.
7. **Negative-match before submitting.** If you have the failing log on disk (Helix work-item console, AzDO step log), run a smoke test against it — eyeballing the signature catches roughly nothing. Build Analysis's `ErrorMessage` matcher is `String.Contains` ordinal case-sensitive, which `grep -F` reproduces exactly:

   ```bash
   grep -Fc "<your ErrorMessage value>" failure.log                                # > 0 = matches the failure
   grep -F  "<your ErrorMessage value>" failure.log | grep -E '^\[(PASS|SKIP)\]'   # MUST be empty
   ```

   For `ErrorPattern`, use `grep -E` — different regex flavor than .NET's `NonBacktracking`, but close enough to flag over-broad patterns:

   ```bash
   grep -Ec '<your ErrorPattern>' failure.log
   grep -E  '<your ErrorPattern>' failure.log | grep -E '^\[(PASS|SKIP)\]'         # MUST be empty
   ```

   If the second command in either pair prints anything, the signature also matches `[PASS]` / `[SKIP]` lines for this test and will mute future passing runs. Narrow it. Also mentally check whether the signature would match (a) other tests in the same assembly, or (b) build-time output (Crossgen2, ilasm, MSBuild). The canonical validator is [`Test-KnownIssuePattern.ps1`](https://github.com/dotnet/arcade-skills/blob/main/plugins/dotnet-dnceng/skills/ci-analysis/scripts/Test-KnownIssuePattern.ps1) (uses the exact regex flavor, emits a validated JSON block); pwsh isn't in this workflow's tool allowlist today, so the `grep -F` / `grep -E` smoke test above is the in-band substitute.
8. **Single-line, no escapes.** Build Analysis runs `String.Contains` (case-sensitive, ordinal) for `ErrorMessage` and `Regex` with `Singleline | IgnoreCase | NonBacktracking` and a 50ms-per-line timeout for `ErrorPattern`. Newlines, ANSI escapes (`\u001b[`), and time-prefixes (`[12:34:56.789]`) are not stripped from log lines before matching. Use the array form (below) for multi-line; use `[^\\n]*` instead of `.*` in regexes.
9. **JSON escaping is correct.** Inside the JSON string value: `"` → `\"`, `\` → `\\`, real newlines → `\n`. For regex patterns this means **double escape**: a literal dot is `\\.` in JSON (the JSON parser consumes one backslash, leaving `\.` for the regex engine). A `\d` you actually want regex to see has to be written `\\d` in JSON. GitHub's issue Preview tab will flag invalid JSON — use it.

##### Multi-line signatures (array form)

Both `ErrorMessage` and `ErrorPattern` accept an **array of strings**: each element matches a separate log line, in order, and lines may appear between matched elements. Use this when no single line on its own is unique enough — e.g., the test name on one line and the assertion text two lines down.

```json
{
  "ErrorMessage": [
    "System.Net.Http.Tests.HttpClientHandlerTest.GetAsync_UnknownHost_Throws",
    "System.Net.Http.HttpRequestException : Name or service not known"
  ]
}
```

Rules: each element matches one line (the elements are NOT concatenated and matched as a single multi-line string). All elements must match in order. Don't mix `ErrorMessage` and `ErrorPattern` in the same array. Don't pad the array with generic tokens like `exitcode: 139` or `Crash` — they add no specificity and risk false negatives if the log format changes.

#### Signature examples — Bad → Good

`ErrorMessage` is matched as an exact literal substring — `...` in the value is matched as three literal dots, not "anything". Use the array form (above) when you need to span variable text between two anchors. The "Good" column below shows the form to use; values shown as plain strings go in `ErrorMessage`, values prefixed `ErrorPattern:` go in `ErrorPattern`, and values shown as a JSON array go in `ErrorMessage` array form.

| ❌ Bad | Why bad | ✅ Good |
|---|---|---|
| `"Some.Test.Class.TestMethodName"` | bare test name; matches `[PASS]` lines for the same test | array: `["Some.Test.Class.TestMethodName", "System.Net.Sockets.SocketException : Try again"]` |
| `"SomeTests.Prefix_"` (trailing `_`) | truncated prefix; trailing `_`/`*`/`.` is literal not glob | `ErrorPattern: "^SomeTests\\.Prefix_[A-Za-z]+\\b[^\\n]*Xunit\\.Sdk\\."` |
| `"Some.Type.Method"` (bare type/method) | matches stack scans of unrelated tests | `ErrorPattern: "^System\\.NullReferenceException\\b[^\\n]*\\n\\s+at Some\\.Type\\.Method\\b"` |
| `"BadImageFormatException"` | bare exception type; matches infra hiccups too | `"System.BadImageFormatException: Could not load file or assembly 'System.Private.CoreLib'"` |
| `"Operation timed out"` | matches transient network failures everywhere | array: `["xharness exec android test", "Operation timed out after 3600s"]` paired with `BuildRetry: false` |

Choose `ErrorMessage` (literal substring) by default. Use `ErrorPattern` only when no single literal line is specific enough — and confirm the regex is anchored and has no catastrophic backtracking. **Populate exactly one of the two fields per JSON block; never both.** Pattern length doesn't matter; specificity does — don't shorten a unique multi-line signature into a pithy one-liner. Set `BuildRetry: true` **only** for confirmed infra/queue-side flakes (dead-letter, device-lost, agent disconnect) where retrying is safe.

### Signature specificity (mandatory)

The `ErrorMessage` / `ErrorPattern` MUST uniquely identify **this specific failure mode**, not an entire category of crashes or build errors. A signature that would match unrelated future regressions is wrong and will mute legitimate failures.

**Reject** signatures that consist only of:

- A bare exit code or signal: `exitcode: 139`, `exit code 1`, `Segmentation fault`, `Aborted`, `SIGSEGV`, `SIGABRT`.
- A generic tool name + failure verb: `Crossgen2 failed`, `ilasm failed`, `dotnet build failed`, `xharness exited`.
- A bare exception type with no message: `BadImageFormatException`, `NullReferenceException`, `Fatal error. Invalid Program`, `Assertion failed`.
- A bare `[FAIL]` line with only the test class name and no exception/assertion text.
- A bare fully-qualified test name (e.g. `"ErrorMessage": "Namespace.Class.TestName"`) without the assertion/exception text that follows it on the next line of the log. The test name alone matches every future regression of that test, including unrelated ones, and Build Analysis will mute legitimate new failures.
- A truncated test-name prefix ending in an underscore, dot, or wildcard glyph (e.g. `"SomeClass.SomeMethod_"`, `"Foo.Bar."`, `"Connect_*"`). `ErrorMessage` is a literal `String.Contains` match, not a glob — a trailing `_` or `*` is treated as a literal character and either over-matches every test whose name contains the prefix or never matches at all. If you need to cover multiple related test methods, instead set `ErrorPattern` to a properly anchored regex (e.g. `"SomeClass\\.SomeMethod_[A-Za-z]+ "`), or pick the exception/assertion message that is common to all of them.
- Common infra strings: `Connection reset`, `Operation timed out`, `Resource temporarily unavailable`, `No space left on device`.

**Prefer** signatures built from the most specific stable token in the log. In order of preference:

1. The exact assertion text or exception **message** (not just the type), e.g. `Assertion failed 'comp->compHndBBtabCount == 0' in 'X' during 'Y'`.
2. The fully-qualified failing test name combined with a specific exception message, e.g. `System.Text.Json.Tests.Utf8JsonReaderTests.TestFoo … System.InvalidOperationException: Cannot read value of type X`.
3. A unique native stack frame or symbol from the crash dump excerpt, e.g. `coreclr!Compiler::fgMorphCall + 0x`.
4. A specific JIT method-being-compiled marker plus the specific stress mode, when the crash is JIT/GC stress only.

**Combining signature parts** — a JSON array in `ErrorMessage` is AND-matched (all substrings must be present in the failure log). Do not pad an array with generic tokens like `exitcode: 139` or `Crash` alongside the specific message — those tokens add no specificity and only risk false negatives if the log format changes. Include at most one supplementary token, and only when it is itself non-generic (e.g. a specific assembly name or test name).

If you cannot produce a signature that meets the bar above, **do not file a Known Build Error**. File a regular tracking issue instead and call out in "Recommended action" that the failure needs a stable signature before it can be muted.

Title: `[ci-scan] Test failure: <fully.qualified.TestName>` for test failures, or `[ci-scan] Known Build Error: <short description>` for non-test build errors. The `[ci-scan] ` prefix is mandatory on every issue and PR this workflow files (see "Outputs: title and labels" above).

Labels: only `Known Build Error` and `blocking-clean-ci` are permitted on Known Build Error issues. Do not include any other label (no `area-*`, `os-*`, `arch-*`, etc.) — they will be rejected by `safe-outputs.allowed-labels`. Area and platform triage is added later by a human reviewer.

Before filing, search for an existing Known Build Error issue with a matching `ErrorMessage` (`label:"Known Build Error" in:body "<signature>"`). If one exists and is open, **skip silently — do not duplicate, do not append a comment**. Build Analysis already counts the new occurrence in its hit-count summary on the issue body; piling on issue comments per occurrence creates noise on already-noisy KBEs (some have tens of hits per run). If `search_issues` returns no matches, proceed to file the new KBE.

## Hard environment constraints

These look like permission errors but are physical:

- **Pre-bind every URL to a shell variable on a line of its own, then `curl -s "$url"`.** Inline URLs with `?` or `&` are rejected as "Permission denied and could not request permission from user" even when single-quoted, because the Copilot CLI tool-approver treats query strings as interactive prompts. The only working pattern is:
  ```bash
  url='https://dev.azure.com/dnceng-public/public/_apis/build/builds?definitions=154&branchName=refs/heads/main&statusFilter=completed&resultFilter=succeeded,failed,partiallySucceeded&%24top=25&api-version=7.1'
  curl -s "$url" | jq '.' | tee /tmp/gh-aw/agent/builds.json | jq -r '.value[0] | "\(.id) \(.result)"'
  ```
  Do **not** retry an inline URL hoping the rejection will clear — it won't. Switch to the variable pattern immediately.
- `>` and `-o` redirection at the agent's command line is blocked. Use `| tee /path/to/file`.
- `$(...)` and `${var@P}` are blocked at the command line. Compose values via `xargs -I{}` or by reading files inline.
- OData `$top` must be encoded as `%24top` in URLs.
- Bash allowlist: `dotnet`, `git`, `find`, `ls`, `cat`, `grep`, `head`, `tail`, `wc`, `curl`, `jq`, `tee`, `sed`, `awk`, `tr`, `cut`, `sort`, `uniq`, `xargs`, `echo`, `date`, `mkdir`, `test`, `env`, `basename`, `dirname`, `bash`, `sh`, `chmod`. No `gh`, no `pwsh`, no `python`. Each call runs in a fresh subshell — persist intermediate state to files under `/tmp/gh-aw/agent/`.

## Coverage discipline (avoid arbitrary selection)

Process every failed signature in every pipeline — do not cherry-pick the obvious ones and skip the rest. Walk pipelines in the order listed in the "Pipelines to scan" table; finish all classifications for pipeline N before moving to pipeline N+1.

For each pipeline:

1. List every failed signature in the latest scanned build, sorted by occurrence count in the window (descending).
2. For each signature, run the six-step walk in "Two-pass KBE → PR flow" and record the outcome (`→ filed-issue #aw_<id>`, `→ filed-PR #aw_<id>`, `→ existing-issue #<n>`, `→ existing-PR #<n>`, or `→ skipped: <reason>`). A skipped signature MUST have a reason (e.g., "build canceled, not a test failure", "less than 2 occurrences and not blocking", "owned by area-Infrastructure rota and already triaged").
3. Keep a per-pipeline tally on disk under `/tmp/gh-aw/agent/coverage/<pipeline>.txt`. At the end of the run, print a summary table to the agent log: `pipeline | total-signatures | issues-filed | prs-filed | reused-existing | skipped-with-reason`.
