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

# ###############################################################
# Override the COPILOT_GITHUB_TOKEN secret usage for the workflow
# with a randomly-selected token from a pool of secrets.
#
# As soon as organization-level billing is offered for Agentic
# Workflows, this stop-gap approach will be removed.
#
# See: /.github/actions/select-copilot-pat/README.md
# ###############################################################

  # Add the pre-activation step of selecting a random PAT from the supplied secrets
  steps:
    - uses: actions/checkout@de0fac2e4500dabe0009e67214ff5f5447ce83dd # v6.0.2
      name: Checkout the select-copilot-pat action folder
      with:
        persist-credentials: false
        sparse-checkout: .github/actions/select-copilot-pat
        sparse-checkout-cone-mode: true
        fetch-depth: 1

    - id: select-copilot-pat
      name: Select Copilot token from pool
      uses: ./.github/actions/select-copilot-pat
      env:
        SECRET_0: ${{ secrets.COPILOT_PAT_0 }}
        SECRET_1: ${{ secrets.COPILOT_PAT_1 }}
        SECRET_2: ${{ secrets.COPILOT_PAT_2 }}
        SECRET_3: ${{ secrets.COPILOT_PAT_3 }}
        SECRET_4: ${{ secrets.COPILOT_PAT_4 }}
        SECRET_5: ${{ secrets.COPILOT_PAT_5 }}
        SECRET_6: ${{ secrets.COPILOT_PAT_6 }}
        SECRET_7: ${{ secrets.COPILOT_PAT_7 }}
        SECRET_8: ${{ secrets.COPILOT_PAT_8 }}
        SECRET_9: ${{ secrets.COPILOT_PAT_9 }}

# Add the pre-activation output of the randomly selected PAT
jobs:
  pre-activation:
    outputs:
      copilot_pat_number: ${{ steps.select-copilot-pat.outputs.copilot_pat_number }}

# Override the COPILOT_GITHUB_TOKEN expression used in the activation job
# Consume the PAT number from the pre-activation step and select the corresponding secret
engine:
  id: copilot
  model: claude-sonnet-4.6
  env:
    # We cannot use line breaks in this expression as it leads to a syntax error in the compiled workflow
    # If none of the `COPILOT_PAT_#` secrets were selected, then the default COPILOT_GITHUB_TOKEN is used
    COPILOT_GITHUB_TOKEN: ${{ case(needs.pre_activation.outputs.copilot_pat_number == '0', secrets.COPILOT_PAT_0, needs.pre_activation.outputs.copilot_pat_number == '1', secrets.COPILOT_PAT_1, needs.pre_activation.outputs.copilot_pat_number == '2', secrets.COPILOT_PAT_2, needs.pre_activation.outputs.copilot_pat_number == '3', secrets.COPILOT_PAT_3, needs.pre_activation.outputs.copilot_pat_number == '4', secrets.COPILOT_PAT_4, needs.pre_activation.outputs.copilot_pat_number == '5', secrets.COPILOT_PAT_5, needs.pre_activation.outputs.copilot_pat_number == '6', secrets.COPILOT_PAT_6, needs.pre_activation.outputs.copilot_pat_number == '7', secrets.COPILOT_PAT_7, needs.pre_activation.outputs.copilot_pat_number == '8', secrets.COPILOT_PAT_8, needs.pre_activation.outputs.copilot_pat_number == '9', secrets.COPILOT_PAT_9, secrets.COPILOT_GITHUB_TOKEN) }}

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

For each actionable failure, in this order:

1. **Search for existing artifacts** before creating anything new:
   - `search_issues` for an open KBE: `is:issue is:open label:"Known Build Error" in:body "<error-signature>"`. Try variations on the signature (full `[FAIL]` line, assertion text, exception type + test name).
   - `search_pull_requests` for an open muting PR that already silences this test: `is:pr is:open in:title "<test-name>" "[ci-scan]"` and `is:pr is:open "<test-name>" ActiveIssue`.
   - `search_pull_requests` for an open small-fix PR: `is:pr is:open in:title "<short-failure-description>" "[ci-scan]"`.
   - If a KBE + muting PR already cover this failure, **skip** — record it in the coverage tally as `→ already-covered: KBE #<n> + PR #<n>` and move on. Do not duplicate.
2. **No existing KBE → file one via safe-outputs `create_issue`**. The only labels permitted on KBE issues are `Known Build Error` and `blocking-clean-ci` (see "Outputs: title and labels" below). Title prefix: `[ci-scan] `. Body: the KBE format described in "Known Build Error issue" below. The safe-outputs handler will create the issue ~1 minute after the agent finishes; the issue number is not available to the agent during this run.
3. **Existing KBE found AND failure still occurring AND no muting PR exists yet → open the muting PR via safe-outputs `create_pull_request`** with the existing KBE issue number hardcoded in the diff: `[ActiveIssue("https://github.com/dotnet/runtime/issues/<existing-N>", ...)]` for unit tests, `<GCStressIncompatible>true</GCStressIncompatible>` (with an inline `<!-- https://github.com/dotnet/runtime/issues/<existing-N> -->` comment) for stress-incompatible JIT csproj families. PR title prefix `[ci-scan] `; **the PR body MUST include a top-level "Linked KBE" line of the form `Linked KBE: #<existing-N>` so the link is unambiguous and machine-readable**, in addition to the prose "Linked KBE" section. This PR must change **only test annotations / csproj test-config flags** — no product code, no diagnosis, no logic. Aim for ≤ 5 lines of diff.
4. **(Optional, alongside step 3) Open a small-fix PR via safe-outputs `create_pull_request`** if the failure satisfies the "small product fix opportunity" criteria above. Separate PR, separate branch, separate diff. PR body must (a) cite the failing test as evidence, (b) explain the root cause, (c) state explicitly why the fix is safe, (d) include `Linked KBE: #<existing-N>` as a top-level line, and (e) note "If this lands before #<muting-PR>, that PR can be closed". Do not bundle the fix into the muting PR — keep them separate so a maintainer can take one without the other.

Caps: safe-outputs `create_issue` max 5/run, `create_pull_request` max 10/run. When a cap is hit, fall back to "skipped: cap reached" rather than silently dropping signatures — subsequent runs will pick them up.

After every run, you should be able to answer YES to **whichever of these applies to each failure**:

- **First-encounter failure (no existing KBE):** "Did I file the KBE?" Muting/fix PRs are deferred to the next run — they cannot reference an issue number that doesn't exist yet at agent runtime.
- **Existing KBE, no muting PR yet:** "Did I open the muting PR (and, if criteria met, the small-fix PR)?"
- **Existing KBE + existing muting PR:** "Did I confirm both, skip silently, and record `→ already-covered: KBE #<n> + PR #<n>` in the coverage tally?"

If the answer is NO for any failure, you have not done the job.

### Per-failure-class rules

The two-pass flow above applies to all classes below. "KBE + muting PR" means: KBE in the run that first encounters the failure, muting PR in the next run that finds the KBE already exists.

- **Recurring failure with a stable error signature** (≥ 2 occurrences on `main` in the scanned window) → KBE (run N) + muting PR (run N+1) + fix PR (optional, run N+1, only if criteria met).
- **Per-test platform / configuration incompatibility** (e.g., test fails only under `jitstress=2`, `gcstress=0xC`, on a single mobile arch, on browser, on NativeAOT) → KBE (run N) + muting PR (run N+1). Allowed muting PR mechanisms:
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

Required structure (Build Analysis is strict — match the headings exactly, and use **exactly three backticks** for the JSON code fence; never four. The opening and closing fence must be the same length, otherwise the fence is broken and Build Analysis silently skips the issue):

```
## Build Information
Build: <link to the dev.azure.com build that first hit this in window>
Build error leg or test failing: <AzDO leg name>-<assembly or test name>
Pull request: <link to the PR if the build was a PR build, otherwise omit this line>

## Error Message

<!-- Use ErrorMessage for String.Contains matches. Use ErrorPattern for regex (single line / no backtracking). Set BuildRetry to `true` only for clear infra flakes. ExcludeConsoleLog skips helix log scanning. -->

(open three backticks, then `json` on the same line)
{
  "ErrorMessage": "<the exact substring from the failure log; prefer the [FAIL] line>",
  "ErrorPattern": "",
  "BuildRetry": false,
  "ExcludeConsoleLog": false
}
(close three backticks)
```

The pseudo-instructions `(open three backticks, then ...)` and `(close three backticks)` above are **placeholders** in this prompt because nesting fenced code blocks in the prompt itself is fragile; in the actual issue body emit literal ```` ``` `` (three backticks) on each side of the JSON object. Verify the open and close fences both consist of exactly three backticks before submitting. If you are uncertain, count them.

Choose `ErrorMessage` (substring) by default. Use `ErrorPattern` only when a regex is genuinely needed and confirm it has no catastrophic backtracking. Set `BuildRetry: true` **only** for confirmed infra/queue-side flakes (dead-letter, device-lost, agent disconnect) where retrying is safe.

### Signature specificity (mandatory)

The `ErrorMessage` / `ErrorPattern` MUST uniquely identify **this specific failure mode**, not an entire category of crashes or build errors. A signature that would match unrelated future regressions is wrong and will mute legitimate failures.

**Reject** signatures that consist only of:

- A bare exit code or signal: `exitcode: 139`, `exit code 1`, `Segmentation fault`, `Aborted`, `SIGSEGV`, `SIGABRT`.
- A generic tool name + failure verb: `Crossgen2 failed`, `ilasm failed`, `dotnet build failed`, `xharness exited`.
- A bare exception type with no message: `BadImageFormatException`, `NullReferenceException`, `Fatal error. Invalid Program`, `Assertion failed`.
- A bare `[FAIL]` line with only the test class name and no exception/assertion text.
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

Failure selection must be **systematic, not opportunistic**. Process pipelines in the order listed in the "Pipelines to scan" table. For each pipeline:

1. List every failed signature in the latest scanned build (sorted by occurrence count in the window, descending).
2. For each signature, decide and record one of: `→ filed-issue #aw_<id>`, `→ filed-PR #aw_<id>`, `→ existing-issue #<n>`, `→ existing-PR #<n>`, `→ skipped: <reason>`. A skipped signature MUST have a reason (e.g., "build canceled, not a test failure", "less than 2 occurrences and not blocking", "owned by area-Infrastructure rota and already triaged").
3. Keep a per-pipeline tally on disk under `/tmp/gh-aw/agent/coverage/<pipeline>.txt`. At the end, print a summary table to the agent log: `pipeline | total-signatures | issues-filed | prs-filed | reused-existing | skipped-with-reason`.

Caps still apply (10 PRs / 5 issues / run); when the cap is hit, fall back to "skipped: cap reached" rather than dropping signatures silently. Subsequent runs will pick them up.

Do not jump between pipelines mid-investigation. Finish all classifications for pipeline N before moving to pipeline N+1.

## Submit

Search existing issues and PRs (`search_issues`, `search_pull_requests`) before creating anything new — never duplicate. Cross-check against issues filed by the existing JIT failure-tracking bot (e.g. open issues authored by `JulieLeeMSFT` for JIT pipelines) and reference rather than re-file them. When using `search_pull_requests`, filter to `is:merged OR review:approved` so the integrity filter does not silently drop low-trust results. If an issue already tracks the failure, **prefer opening a PR that references it via `[ActiveIssue("https://github.com/dotnet/runtime/issues/<n>")]`** rather than filing another issue. If `search_issues` returns no matches, proceed to file the issue.
