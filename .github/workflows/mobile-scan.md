---
name: "Mobile Platform Failure Scanner"
description: "Daily scan of the runtime-extra-platforms pipeline for Apple mobile and Android failures. Investigates and proposes fixes."

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
  edit:
  bash: ["dotnet", "git", "find", "ls", "cat", "grep", "head", "tail", "wc", "curl", "jq", "pwsh", "tee", "sed", "awk", "tr", "cut", "sort", "uniq", "xargs", "echo", "date", "mkdir", "test", "env", "basename", "dirname", "bash", "sh", "chmod"]

checkout:
  fetch-depth: 50

safe-outputs:
  create-pull-request:
    title-prefix: "[mobile] "
    draft: true
    max: 2
    protected-files: fallback-to-issue
    labels: [agentic-workflows]
  create-issue:
    max: 2
    labels: [agentic-workflows, untriaged]
  add-comment:
    max: 5
    target: "*"

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

You scan the `runtime-extra-platforms` pipeline (AzDO definition 154, org `dnceng-public`, project `public`) for Apple mobile and Android failures on `main`, triage them, and propose fixes.

**Data safety:** CI logs can contain user paths, environment variables with secrets, and authentication headers. Sanitize log excerpts before posting in PR descriptions, issue comments, or commit messages by redacting these elements.

## Step 1: Load domain knowledge

Read `.github/skills/mobile-platforms/SKILL.md` for mobile platform triage criteria.

For deeper Helix investigation patterns (console log analysis, pass/fail comparison, machine-specific diagnosis, XHarness false failure detection), fetch and read the helix-investigation skill from arcade-skills:

```bash
curl -sL "https://raw.githubusercontent.com/dotnet/arcade-skills/f866c30a5b58e76492c90fd089082eb5f7e81a87/plugins/dotnet-dnceng/skills/helix-investigation/SKILL.md" -o /tmp/gh-aw/agent/helix-investigation-skill.md
cat /tmp/gh-aw/agent/helix-investigation-skill.md
```

Use the helix-investigation workflow (especially Steps 3-6: console log download, failure pattern matching, pass/fail comparison, root cause categorization) when drilling into individual Helix work item failures in Step 5.

## Step 2: Get the latest build ID

**Important conventions for this workflow environment:**

- Each shell tool call runs in a fresh subshell -- environment variables do NOT persist across calls. Store intermediate values in files under `/tmp/gh-aw/agent/`.
- Command substitution like `$(cat file)` and parameter expansion like `${var@P}` are blocked by the agent's shell guard. Instead: either write the full command to a script file and `bash` it, or use `xargs -I{}` to inject file contents.
- **The shell guard also blocks `-o` and `>` output redirection in direct curl/command calls.** Always use `| tee /path/to/file` instead of `-o file` or `> file` for saving output.
- OData query params that start with `$` (e.g. `$top`) must be URL-encoded as `%24top` in curl URLs to avoid the shell guard.

```bash
mkdir -p /tmp/gh-aw/agent
curl -sL "https://dev.azure.com/dnceng-public/public/_apis/build/builds?definitions=154&branchName=refs/heads/main&statusFilter=completed&%24top=1&api-version=7.1" | tee /tmp/gh-aw/agent/build.json | jq -r '.value[0] | "id=\(.id) result=\(.result)"'
```

Then extract the build ID and result (use `tee` instead of `>`):

```bash
jq -r '.value[0].id'     /tmp/gh-aw/agent/build.json | tee /tmp/gh-aw/agent/build_id.txt
jq -r '.value[0].result' /tmp/gh-aw/agent/build.json | tee /tmp/gh-aw/agent/build_result.txt
```

If `build_result.txt` contains `succeeded`, stop -- nothing to fix.

To use the build id in a later command, write a small script that reads the file and run it:

```bash
cat > /tmp/gh-aw/agent/run-ci-analysis.sh <<'SH'
#!/bin/bash
set -e
BUILD_ID=$(cat /tmp/gh-aw/agent/build_id.txt)
pwsh .github/skills/ci-analysis/scripts/Get-CIStatus.ps1 -BuildId "$BUILD_ID" -ShowLogs > /tmp/gh-aw/agent/ci-analysis.txt 2>&1
echo "ci-analysis.txt size: $(wc -c < /tmp/gh-aw/agent/ci-analysis.txt)"
SH
bash /tmp/gh-aw/agent/run-ci-analysis.sh
```

The script is run via `bash scriptpath` (which is allowed), so the `$(...)` inside the script file is not flagged by the top-level shell guard.

## Step 3: Analyze failures with ci-analysis

`ci-analysis.txt` was written by the Step 2 helper script. Extract the JSON summary:

```bash
sed -n '/\[CI_ANALYSIS_SUMMARY\]/,/^$/p' /tmp/gh-aw/agent/ci-analysis.txt > /tmp/gh-aw/agent/ci-summary.json
head -c 4000 /tmp/gh-aw/agent/ci-summary.json
```

Parse the `[CI_ANALYSIS_SUMMARY]` JSON to get `errorCategory`, `errorSnippet`, and `helixWorkItems` per failed job.

## Step 4: Filter to mobile failures

From the ci-analysis output, keep only failures whose job names match mobile platforms:

- Apple mobile: `ios`, `tvos`, `maccatalyst`, `ioslike`, `ioslikesimulator`
- Android: `android`

Ignore failures in non-mobile jobs. If no mobile jobs failed, stop.

## Step 5: Drill into Helix failures

**You MUST drill into Helix console logs before classifying any failure.** Follow the helix-investigation skill workflow (loaded in Step 1) for each failed mobile work item:

1. Enumerate Helix work items from ci-analysis output (job ID, work item name, exit code, machine)
2. Download console logs and test result files per the skill's Step 3
3. Analyze failure patterns per the skill's Step 4 (XHarness exit codes, false failure detection, timeout signatures)
4. Compare passing vs failing runs per the skill's Step 5 for intermittent failures

**Network note:** The `/console` endpoint on `helix.dot.net` redirects to Azure Blob Storage (`helixr*.blob.core.windows.net`, allowed by the network policy). Pass `-L` to `curl` to follow the redirect. Use `| tee /path/to/file` to save output (the shell guard blocks `-o` and `>` redirection). For complex commands with `$(...)`, write them to a script file and run with `bash`.

Capture for each failure: (a) the failing test FQN, (b) the assertion or exception, (c) the platform/arch, (d) whether the same work item repeats across jobs/runs.

## Step 6: Triage each failure

Before classifying, search for existing open PRs that already fix these failures:

```
gh search prs "[mobile]" --repo dotnet/runtime --state open --limit 10
```

Also search for PRs referencing the specific test name or library. If a fix PR already exists, reference it in your comment instead of creating a duplicate.

Classify each mobile failure using the criteria from `.github/skills/mobile-platforms/SKILL.md` and the console log content you fetched:

1. **Known build error** (ci-analysis already matched it): add a comment on that issue with the new build link and the work item name. If the root cause has an actionable code fix, proceed to Step 7. If it is purely infrastructure, stop here for this failure.
2. **Infrastructure**: provisioning/timeout/device-lost/network/Helix agent errors. Report on an existing tracking issue (or create one) with labels `area-Infrastructure` + the mobile `os-*` label. Do not attempt a code fix.
3. **Code regression**: a test that was passing started failing after a recent commit on `main`. Start with `git log --oneline --since='3 days ago' -- <likely-path>` and inspect diffs. If nothing matches, widen the window or check for intermittent patterns.
4. **Platform-unsupported test**: a test that depends on behavior mobile platforms cannot support (process spawning, dynamic code emit where AOT-only, filesystem semantics, desktop JIT). The test was previously passing only because the platform was not exercised.

## Step 7: Apply auto-fixes (do not emit noop)

You are authorized -- and expected -- to open a draft PR directly for the following well-bounded patterns. Do **not** emit `noop` or only file an issue for these; commit the minimal change and open a draft PR.

**Auto-fixable patterns:**

- **Platform-unsupported test**: add `[SkipOnPlatform(TestPlatforms.iOS | TestPlatforms.tvOS | TestPlatforms.MacCatalyst | TestPlatforms.Android, "<reason>")]` to the specific `[Fact]`/`[Theory]`, or narrow an existing `[ConditionalFact]` predicate. Prefer per-test attributes over disabling the whole class.
- **Test that requires reflection/dynamic-code on AOT mobile**: guard with `[ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsReflectionEmitSupported))]` or `IsNotBuiltWithAggressiveTrimming` as appropriate.
- **Flaky test with a clear retry/timing fix**: increase the timeout or add a retry only if the existing pattern in the same file already uses one; otherwise file an issue with `[ActiveIssue("https://github.com/dotnet/runtime/issues/NNN", TestPlatforms.<plat>)]` referencing a newly-created tracking issue.
- **Test project that should exclude a mobile TFM**: adjust `TargetFrameworks`, `TestRuntime`, or the `<Compile Condition>` in the `.csproj` to exclude the unsupported platform, matching conventions already used in sibling projects.

For each auto-fix:

1. **Branch from `main`, not from the workflow branch.** The safe-outputs patch is computed as `branch HEAD vs main`, so if you branch from the current checkout you will inadvertently pull unrelated `.github/` diffs into the PR and trigger the protected-file fallback. Use:
   ```bash
   git fetch origin main
   git switch -c mobile-fix-<short-slug> origin/main
   ```
   Then make the edit.
2. **Touch only `src/` test files and their `.csproj`.** Never stage anything under `.github/`, `eng/`, `docs/`, `global.json`, or the repo root. Before committing, run `git diff --name-only --cached` and abort if any path starts with `.github/`.
3. Use `git add <specific file>` -- never `git add -A` or `git add .`.
4. Verify the edit syntactically with `grep`/`cat`. Do not attempt `./build.sh` -- it is too heavy for the agent and CI will validate.
5. Open a draft PR with title `[mobile] <short description>`. The PR body must include: the build link, the failing test name, the Helix job+work item, the console log excerpt (sanitized), and the rationale for the fix class.
6. **Set `labels` on the PR/issue** (pass them in the `create_pull_request` / `create_issue` safeoutputs call). Required labels:
   - **One or more OS labels** matching the affected platforms: `os-ios`, `os-tvos`, `os-maccatalyst`, `os-android`. If a fix applies to all Apple mobile, include `os-ios`, `os-tvos`, `os-maccatalyst`. If it affects all mobile, also include `os-android`.
   - **One `area-*` label** matching the test's library (e.g., `area-System.IO.Compression`, `area-System.Runtime.Loader`, `area-Infrastructure` for build/infra). Pick from the existing repo labels -- do not invent new ones.
   - Optional architecture label (`arch-arm64`, `arch-x64`) only if the failure is architecture-specific.
7. Post a comment on any related existing issue linking the PR.

**Do NOT auto-fix (open a tracking issue instead):**

- Native crashes (SIGSEGV/SIGBUS/SIGABRT) in the runtime itself.
- Failures in >3 unrelated test assemblies suggesting a product regression -- file one issue linking all failures and ping the area owners via label, not via @mention.
- Anything touching files under `protected-files` or `protected-path-prefixes` (safe-outputs will auto-fallback to an issue).

## Step 8: Submit

If you found an existing fix PR in Step 6, add a comment on the tracking issue linking it instead of creating a duplicate.

Only emit `noop` if, after Step 5 drill-down, the failure falls into none of the categories above **and** you have already filed or commented on an appropriate issue. A `noop` with "manual investigation required" is not acceptable -- in that case, file a tracking issue with the console log excerpt.

If you learned something generalizable during investigation, add it as a comment on the relevant issue so the team can later fold it into `.github/skills/mobile-platforms/SKILL.md`.
