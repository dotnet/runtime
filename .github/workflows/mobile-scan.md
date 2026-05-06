---
name: "Mobile Platform Failure Scanner"
description: "Daily scan of the runtime-extra-platforms pipeline for Apple mobile and Android failures. Fixes per-test failures via PR; files an actionable tracking issue otherwise."

permissions:
  contents: read
  issues: read
  pull-requests: read

on:
  schedule: daily
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
  model: claude-sonnet-4.5
  env:
    # We cannot use line breaks in this expression as it leads to a syntax error in the compiled workflow
    # If none of the `COPILOT_PAT_#` secrets were selected, then the default COPILOT_GITHUB_TOKEN is used
    COPILOT_GITHUB_TOKEN: ${{ case(needs.pre_activation.outputs.copilot_pat_number == '0', secrets.COPILOT_PAT_0, needs.pre_activation.outputs.copilot_pat_number == '1', secrets.COPILOT_PAT_1, needs.pre_activation.outputs.copilot_pat_number == '2', secrets.COPILOT_PAT_2, needs.pre_activation.outputs.copilot_pat_number == '3', secrets.COPILOT_PAT_3, needs.pre_activation.outputs.copilot_pat_number == '4', secrets.COPILOT_PAT_4, needs.pre_activation.outputs.copilot_pat_number == '5', secrets.COPILOT_PAT_5, needs.pre_activation.outputs.copilot_pat_number == '6', secrets.COPILOT_PAT_6, needs.pre_activation.outputs.copilot_pat_number == '7', secrets.COPILOT_PAT_7, needs.pre_activation.outputs.copilot_pat_number == '8', secrets.COPILOT_PAT_8, needs.pre_activation.outputs.copilot_pat_number == '9', secrets.COPILOT_PAT_9, secrets.COPILOT_GITHUB_TOKEN) }}

concurrency:
  group: "mobile-scan"
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
    title-prefix: "[mobile] "
    draft: true
    max: 5
    protected-files: blocked
    allowed-files:
      - "src/libraries/**/tests/**"
      - "src/libraries/Common/tests/**"
    labels: [agentic-workflows]
  create-issue:
    max: 3
    labels: [agentic-workflows]

timeout-minutes: 60

network:
  allowed:
    - defaults
    - github
    - dev.azure.com
    - helix.dot.net
    - "*.blob.core.windows.net"
---

# Mobile Platform Failure Scanner

Scan the latest completed build of the `runtime-extra-platforms` pipeline (AzDO definition `154`, org `dnceng-public`, project `public`, branch `main`) for Apple mobile and Android failures. Every actionable failure becomes either a draft PR (per-test fix) or a tracking issue (everything else). Read `.github/skills/mobile-platforms/SKILL.md` first for the pipeline layout, platform helpers, and code-path map.

## Outcome

For each failed mobile work item in the latest completed build:

- **Per-test platform incompatibility** → open a draft PR. Use a per-test attribute change: `[SkipOnPlatform(...)]`, a narrowed `[ConditionalFact]` predicate built from existing `PlatformDetection.*` helpers, or `[ActiveIssue("https://github.com/dotnet/runtime/issues/<n>", TestPlatforms.<plat>)]` referencing an **existing** issue. Touch only files matching the `allowed-files` policy (`src/libraries/**/tests/**`, including test `.csproj`).
- **Anything else** — product regression, native crash, multi-assembly cluster, infrastructure (including queue exhaustion / dead-letter / device-lost) — file a tracking issue. The issue is the deliverable; do not paper over a product bug with `SkipOnPlatform`. Group all dead-letter / queue exhaustion / device-lost failures from one run into a single infrastructure issue. Before filing, `search_issues` for an open issue with the matching `area-Infrastructure` + `os-*` label and update its description in place rather than creating a duplicate.

Do not emit `noop`. Either a PR or an issue must come out of every actionable failure.

Cap: **5 PRs and 3 issues per run.** Group failures that share one fix into a single PR. Group failures with the same root cause into a single issue.

## Data sources

- AzDO REST: `https://dev.azure.com/dnceng-public/public/_apis/build/...` — list completed builds (definition 154, branch main), get a build's timeline, download per-job AzDO logs. Mobile job names match the regex `(ios|tvos|maccatalyst|android)` (case-insensitive).
- Helix REST: `https://helix.dot.net/api/jobs/{jobId}/workitems?api-version=2019-06-17` — Helix job IDs appear in AzDO logs as `Job <GUID> on <Queue>`. Each work item has `Name`, `State`, `ExitCode`, `ConsoleOutputUri`. Failed: `ExitCode != 0` or `State == "Failed"`. Console URIs containing `helix-workitem-deadletter` are dead-lettered (queue had no agent) and are pure infra — drop them.

Look back through roughly the last 20 completed builds to compute a "first seen in scanned window" timestamp and occurrence count per `(work_item, queue)` signature.

Drill into one representative console log per signature to confirm the failure shape (`[FAIL]` markers, assertion text) before classifying.

## PR body

Four H2 sections, in this exact order:

1. **Reasoning** — why the test fails on the affected mobile platforms; why the chosen attribute is the right fix.
2. **Impact on platforms** — bullet list of `(platform/arch + Helix queue + exit code)` per affected occurrence.
3. **Errors log** — sanitized excerpt from the Helix console log (the `[FAIL]` line, the assertion or exception, and the `Failed tests:` summary). Strip JWTs, bearer tokens, `ApplicationGatewayAffinity*=`, and per-user paths.
4. **First build it occurred** — first build (in the scanned window) where this signature appeared: build link, finish time, commit SHA, occurrences-in-window count. State explicitly that this is computed within the scanned window and may not be the true origin.

Branch from `origin/main`. Stage only the files you intend to change with `git add <specific path>`; never `git add -A`. Verify with `git diff --name-only --cached` before committing. Labels: one or more `os-*` (`os-android`, `os-ios`, `os-tvos`, `os-maccatalyst`) plus the test's `area-*` label.

## Issue body

Use this when a PR is not the right tool — product regression, native crash, multi-assembly cluster, infra requiring an owner. Same four sections as a PR (Reasoning, Impact on platforms, Errors log, First build it occurred), plus a fifth:

5. **Recommended action** — concrete next step: which area owner, which file likely needs the fix, or what investigation would localize the root cause. Reference any related PR or issue you found via `search_issues`. The issue must be actionable — a checkbox-ready task list, not just "FYI".

Same `os-*` and `area-*` labels.

## Hard environment constraints

These look like permission errors but are physical:

- `curl` URLs containing `?` or `&` MUST be **single-quoted**. Double-quoted URLs trigger `Permission denied and could not request permission from user`.
- `>` and `-o` redirection at the agent's command line is blocked. Use `| tee /path/to/file`.
- `$(...)` and `${var@P}` are blocked at the command line. Compose values via `xargs -I{}` or by reading files inline.
- OData `$top` must be encoded as `%24top` in URLs.
- Bash allowlist: `dotnet`, `git`, `find`, `ls`, `cat`, `grep`, `head`, `tail`, `wc`, `curl`, `jq`, `tee`, `sed`, `awk`, `tr`, `cut`, `sort`, `uniq`, `xargs`, `echo`, `date`, `mkdir`, `test`, `env`, `basename`, `dirname`, `bash`, `sh`, `chmod`. No `gh`, no `pwsh`, no `python`. Each call runs in a fresh subshell — persist intermediate state to files under `/tmp/gh-aw/agent/` (just files; you do not need to author a helper script).

## Submit

Search existing issues and PRs (`search_issues`, `search_pull_requests`) before creating anything new — never duplicate. When using `search_pull_requests`, filter to `is:merged OR review:approved` so the integrity filter does not silently drop low-trust results. If an issue already tracks the failure, **prefer opening a PR that references it via `[ActiveIssue("https://github.com/dotnet/runtime/issues/<n>")]`** rather than filing another issue. If `search_issues` returns no matches, proceed to file the issue.
