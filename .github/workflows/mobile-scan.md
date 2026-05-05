---
name: "CI Outer-Loop Failure Scanner"
description: "Periodic platform-agnostic scan of runtime-extra-platforms and outer-loop CI pipelines (JIT/GC stress, PGO, libraries-jitstress, etc.). Fixes per-test failures via PR; files an actionable tracking issue otherwise."

permissions:
  contents: read
  issues: read
  pull-requests: read

on:
  schedule: every 6h
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
      - "src/libraries/**/tests/**"
      - "src/libraries/**/src/**"
      - "src/libraries/Common/tests/**"
      - "src/libraries/Common/src/**"
      - "src/coreclr/**/tests/**"
      - "src/mono/**/tests/**"
      - "src/tests/**"
      - "eng/testing/**"
    labels: [agentic-workflows]
  create-issue:
    max: 5
    labels: [agentic-workflows]

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
| runtime-coreclr jitstress | 109 | JIT stress modes |
| runtime-coreclr jitstressregs | 110 | |
| runtime-coreclr jitstress2-jitstressregs | 111 | |
| runtime-coreclr gcstress0x3-gcstress0xc | 112 | |
| runtime-coreclr gcstress-extra | 113 | |
| runtime-coreclr jitstress-isas-x86 | 115 | |
| runtime-coreclr jitstress-isas-arm | 116 | |
| runtime-coreclr jitstressregs-x86 | 117 | |
| runtime-coreclr libraries-jitstressregs | 118 | |
| runtime-coreclr libraries-jitstress2-jitstressregs | 119 | |
| runtime-jit-experimental | 137 | OSR / partial compilation |
| runtime-coreclr libraries-jitstress | 138 | |
| runtime-coreclr ilasm | 140 | |
| runtime-coreclr pgo | 144 | |
| runtime-coreclr libraries-pgo | 145 | |
| runtime-coreclr superpmi-replay | 150 | |
| runtime-coreclr jit-cfg | 155 | Control flow guard |
| runtime-coreclr jitstress-random | 159 | Stress mode value comes from logs |
| runtime-coreclr libraries-jitstress-random | 160 | Stress mode value comes from logs |
| runtime-coreclr pgostress | 230 | |
| runtime-coreclr jitstress-isas-avx512 | 235 | |

If a pipeline has no completed build in the last 7 days, skip it silently.

## Skills to consult per failure

Read the relevant skill before classifying / fixing. Skills live under `.github/skills/`.

- **Mobile (`ios`, `tvos`, `maccatalyst`, `android`, `iossimulator`, `tvossimulator`)** → `mobile-platforms/SKILL.md`. Pipeline layout, platform helpers, code-path map.
- **JIT / GC / PGO stress** (definitions 109–160, 230, 235; `runtime-jit-experimental`) → `jit-regression-test/SKILL.md` for repro extraction; `ci-pipeline-monitor/SKILL.md` for triage and failure-shape recognition. JIT product fixes are out of scope for autofix — file an issue and `@`-mention the JIT area owners.
- **Browser/WASM, WASI** (extra-platforms) → consult `mobile-platforms/SKILL.md` (the WASM/WASI sections) for build-time conditional patterns; `extensions-review/SKILL.md` if the failure is in `Microsoft.Extensions.*` tests; `system-net-review/SKILL.md` if the failure is in `System.Net.*` tests.
- **NativeAOT outer loop** → check `eng/testing/tests.*aot*.targets` and the test `.csproj` for AOT-specific conditions before suggesting a fix.
- **Generic CI triage** → `ci-pipeline-monitor/SKILL.md` for known-failure-shape patterns and Build Analysis matching.

## Outcome (per actionable failure)

- **Per-test platform / configuration incompatibility** (e.g., test fails only under `jitstress=2`, `gcstress=0xC`, on a single mobile arch, on browser, on NativeAOT) → open a draft PR with a per-test attribute change:
  - `[SkipOnPlatform(TestPlatforms.<plat>, "<reason>")]` for platform-specific failures.
  - `[ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.<helper>))]` narrowed via existing helpers.
  - `[ActiveIssue("https://github.com/dotnet/runtime/issues/<n>", TestPlatforms.<plat>)]` referencing an **existing** issue.
  - For JIT/GC stress: `[ActiveIssue("...", typeof(TestLibrary.PlatformDetection), nameof(TestLibrary.PlatformDetection.IsStressTest))]` or wrap with a guard helper that checks `DOTNET_JitStress`/`DOTNET_GCStress` env vars where the existing test infra supports it.
- **Recurring flaky failure with a stable error signature** (≥ 2 occurrences on `main` in the scanned window, no obvious product fix in flight, blocking unrelated PRs) → file a **Known Build Error** issue (see "Known Build Error issue" section below). This lets Arcade Build Analysis auto-match future hits and unblock PRs.
- **Build break on a single leg** (`Build product` or similar failed; `Send to Helix` skipped) → file a regular tracking issue (NOT a Known Build Error — Build Analysis explicitly forbids that for build breaks). Reference the failing source file or compile error from the log. Do not attempt an `allowed-files` PR for product code unless the fix is one-line and clearly limited to test infrastructure under `eng/testing/**`.
- **Anything else** — product regression, native crash, multi-assembly cluster, JIT/GC product bug, infrastructure (queue exhaustion / dead-letter / device-lost) — file a tracking issue. Group all infra failures from one run into a single issue. Before filing, `search_issues` for an open issue with the matching `area-*` + `os-*` label and update its description in place rather than duplicating.

For each failure compute a `(definition_id, work_item_or_phase, queue, stress_mode, [FAIL] or compile-error signature)` signature. Look back through ~10 completed builds in the same definition to build first-seen-in-window timestamp and occurrence count.

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
5. **Linked issue** (optional) — if an `ActiveIssue` reference is used, link the issue and quote the matching label set.

Branch from `origin/main`. Stage only the files you intend to change with `git add <specific path>`; never `git add -A`. Verify with `git diff --name-only --cached` before committing. Labels: at least one `os-*` (`os-android`, `os-ios`, `os-tvos`, `os-maccatalyst`, `os-browser`, `os-wasi`, `os-windows`, `os-linux`, `os-osx`) where applicable, plus the test's `area-*` label, plus `arch-*` for arch-specific failures, plus the relevant configuration label (`disabled-test`, `jit-stress`, `gc-stress`, `pgo`, `nativeaot`, …) when present.

## Issue body

Use this when a PR is not the right tool — product regression, native crash, multi-assembly cluster, infra requiring an owner, JIT/GC product bug. Same four sections as a PR (Reasoning, Impact on platforms, Errors log, First build it occurred), plus a fifth:

5. **Recommended action** — concrete next step: which area owner, which file likely needs the fix, or what investigation would localize the root cause. For JIT/GC issues include the exact stress mode env vars and the JIT method-name from the log. Reference any related PR or issue you found via `search_issues`. The issue must be actionable — a checkbox-ready task list, not just "FYI".

Same `os-*`, `area-*`, `arch-*` labels.

## Known Build Error issue

A Known Build Error is a tracking issue that Arcade Build Analysis (https://github.com/dotnet/arcade/blob/main/Documentation/Projects/Build%20Analysis/KnownIssueJsonStepByStep.md) automatically matches against future failures so PRs aren't blocked by an already-tracked flake.

File one when **all** of the following hold:
- The failure has occurred ≥ 2 times in the scanned window on `main`.
- The error has a stable substring or regex signature that uniquely identifies it.
- No fix PR is currently open (verify via `search_pull_requests`).
- The failure is **not** a build break — only test failures, hangs, or infra issues. Build breaks must use a regular issue.

Required structure (Build Analysis is strict — match the headings exactly):

````markdown
## Build Information
Build: <link to the dev.azure.com build that first hit this in window>
Build error leg or test failing: <AzDO leg name>-<assembly or test name>
Pull request: <link to the PR if the build was a PR build, otherwise omit this line>

## Error Message

<!-- Use ErrorMessage for String.Contains matches. Use ErrorPattern for regex (single line / no backtracking). Set BuildRetry to `true` only for clear infra flakes. ExcludeConsoleLog skips helix log scanning. -->

```json
{
  "ErrorMessage": "<the exact substring from the failure log; prefer the [FAIL] line>",
  "ErrorPattern": "",
  "BuildRetry": false,
  "ExcludeConsoleLog": false
}
```
````

Choose `ErrorMessage` (substring) by default. Use `ErrorPattern` only when a regex is genuinely needed and confirm it has no catastrophic backtracking. Set `BuildRetry: true` **only** for confirmed infra/queue-side flakes (dead-letter, device-lost, agent disconnect) where retrying is safe.

Title: `Test failure: <fully.qualified.TestName>` for test failures, or `Known Build Error: <short description>` for non-test build errors.

Labels: `Known Build Error`, `blocking-clean-ci`, plus the test's `area-*` label and any `os-*` / `arch-*` labels that apply.

Before filing, search for an existing Known Build Error issue with a matching `ErrorMessage` (`label:"Known Build Error" in:body "<signature>"`). If one exists and is open, do not duplicate — instead append the new build to the existing issue's body via an issue comment with the build link, leg, and timestamp.

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

## Submit

Search existing issues and PRs (`search_issues`, `search_pull_requests`) before creating anything new — never duplicate. Cross-check against issues filed by the existing JIT failure-tracking bot (e.g. open issues authored by `JulieLeeMSFT` for JIT pipelines) and reference rather than re-file them. When using `search_pull_requests`, filter to `is:merged OR review:approved` so the integrity filter does not silently drop low-trust results. If an issue already tracks the failure, **prefer opening a PR that references it via `[ActiveIssue("https://github.com/dotnet/runtime/issues/<n>")]`** rather than filing another issue. If `search_issues` returns no matches, proceed to file the issue.
