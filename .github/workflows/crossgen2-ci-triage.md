---
description: "Daily triage of crossgen2 CI pipeline failures - analyzes builds, creates issues, and assigns Copilot to fix or disable failing tests"

on:
  schedule: daily on weekdays
  workflow_dispatch:

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
  model: claude-opus-4.6
  env:
    # We cannot use line breaks in this expression as it leads to a syntax error in the compiled workflow
    # If none of the `COPILOT_PAT_#` secrets were selected, then the default COPILOT_GITHUB_TOKEN is used
    COPILOT_GITHUB_TOKEN: ${{ case(needs.pre_activation.outputs.copilot_pat_number == '0', secrets.COPILOT_PAT_0, needs.pre_activation.outputs.copilot_pat_number == '1', secrets.COPILOT_PAT_1, needs.pre_activation.outputs.copilot_pat_number == '2', secrets.COPILOT_PAT_2, needs.pre_activation.outputs.copilot_pat_number == '3', secrets.COPILOT_PAT_3, needs.pre_activation.outputs.copilot_pat_number == '4', secrets.COPILOT_PAT_4, needs.pre_activation.outputs.copilot_pat_number == '5', secrets.COPILOT_PAT_5, needs.pre_activation.outputs.copilot_pat_number == '6', secrets.COPILOT_PAT_6, needs.pre_activation.outputs.copilot_pat_number == '7', secrets.COPILOT_PAT_7, needs.pre_activation.outputs.copilot_pat_number == '8', secrets.COPILOT_PAT_8, needs.pre_activation.outputs.copilot_pat_number == '9', secrets.COPILOT_PAT_9, secrets.COPILOT_GITHUB_TOKEN) }}

timeout-minutes: 30

permissions:
  contents: read
  issues: read
  actions: read
  pull-requests: read

tools:
  github:
    toolsets: [default, actions, search]
    min-integrity: none
  web-fetch:
  cache-memory: true

network:
  allowed:
    - defaults
    - dev.azure.com
    - helix.dot.net
    - mihubot.xyz

safe-outputs:
  mentions: false
  allowed-github-references: []
  create-issue:
    max: 10
    assignees: [copilot]
    labels: [area-CodeGen-coreclr]
    title-prefix: "[Crossgen2 CI] "
    expires: 30
  noop:

tracker-id: crossgen2-ci-triage
---

# Crossgen2 CI Failure Triage

You are an automated CI triage agent for the dotnet/runtime repository. Your job is to analyze recent failures in crossgen2-related CI pipelines, identify new unknown test failures, and create actionable GitHub issues assigned to Copilot Coding Agent.

## Target Pipelines

Analyze failures from these Azure DevOps pipelines (org: `dnceng-public`, project: `public`):

1. `runtime-coreclr crossgen2`
2. `runtime-coreclr crossgen2-composite`
3. `runtime-coreclr crossgen2 outerloop`
4. `runtime-coreclr crossgen2-composite gcstress`

### Cross-Reference Pipeline

The following pipeline is used for cross-referencing failures (see Step 3):

- `runtime`

This pipeline is **not** a triage target — do not create issues for its failures. It is only queried when crossgen2 pipeline failures are found, to determine whether those failures also occur in the `runtime` pipeline (which indicates they are not crossgen2-specific).

### Branch Restriction

**All pipeline queries — both target pipelines and the cross-reference pipeline — MUST be filtered to the `main` branch only** (`branchName=refs/heads/main`). Do not analyze builds from pull request branches or any other branches. PR builds may have failures that will be fixed before merging to `main`.

## Step 1: Discover Failed Builds

Query Azure DevOps for builds completed in the last 48 hours (to cover weekends on Monday) that have failures. **Only query builds on the `main` branch — never analyze PR builds.**

For each target pipeline:

1. **Look up the pipeline definition ID**:
   ```
   curl -s "https://dev.azure.com/dnceng-public/public/_apis/build/definitions?name=<PIPELINE_NAME>&api-version=7.0"
   ```
   Extract the `id` field from the response.

2. **Query failed builds** using the definition ID:
   ```
   curl -s "https://dev.azure.com/dnceng-public/public/_apis/build/builds?definitions=<DEF_ID>&minTime=<ISO_DATETIME_48H_AGO>&resultFilter=failed&statusFilter=completed&branchName=refs/heads/main&api-version=7.0"
   ```
   Use the current UTC time minus 48 hours for `minTime` in ISO 8601 format. The `branchName=refs/heads/main` filter is **required** — do not omit it.

3. **Collect build IDs** for all failed builds across all four pipelines.

If no failed builds are found across any target pipeline, call the `noop` safe output with a message explaining that no crossgen2 pipeline failures were found in the last 48 hours.

## Step 2: Analyze Each Failed Build

For each failed build, use the CI Analysis skill script:

```bash
pwsh .github/skills/ci-analysis/scripts/Get-CIStatus.ps1 -BuildId <BUILD_ID> -ShowLogs -SearchMihuBot -ContinueOnError
```

From the output, extract and preserve:
- **Failed job names** and their error categories
- **Specific test names**: Fully qualified test class and method names (e.g., `System.Net.Security.Tests.SslStreamTest.ConnectAsync_InvalidCertificate_Throws`)
- **Error messages and stack traces**: Copy exact error text from the CI output — these go directly into issue bodies
- **Helix work item details**: Work item names, error snippets, and console log URLs
- **Known issue matches** from Build Analysis
- **The `[CI_ANALYSIS_SUMMARY]` JSON block** for structured analysis

**IMPORTANT**: Do not summarize or paraphrase error output. Copy the actual error messages, assertion failures, and stack traces verbatim from the CI analysis output. Issues must contain enough concrete detail for someone to understand the failure without re-running CI analysis.

### Filtering Known Issues

Skip failures that are already matched to known issues by Build Analysis. Focus only on **unknown/untracked failures** — these are the ones that need new issues.

### Check Cache Memory

Read from `cache-memory` a file named `triaged-builds.json` (if it exists). This contains build IDs and failure signatures that have already been triaged. Skip any failures that match entries in this file.

## Step 3: Cross-Reference Failures Against the `runtime` Pipeline

If Step 2 identified unknown, untracked crossgen2 failures, query the `runtime` pipeline to check whether those failures also occur there. **Skip this step entirely if there are no unknown crossgen2 failures to cross-reference.**

First, discover failed `runtime` pipeline builds using the same approach as Step 1 (same time window, same `branchName=refs/heads/main` filter):

1. Look up the pipeline definition ID for `runtime`.
2. Query failed builds on `main` in the last 48 hours.
3. Collect the `runtime` build IDs.

Then, for each failed `runtime` pipeline build, run the CI Analysis script:

```bash
pwsh .github/skills/ci-analysis/scripts/Get-CIStatus.ps1 -BuildId <BUILD_ID> -ShowLogs -SearchMihuBot -ContinueOnError
```

Extract the failing test names from the `runtime` pipeline builds. Build a set of **runtime pipeline failure signatures** (fully qualified test names).

Then compare the crossgen2 pipeline failures (from Step 2) against the runtime pipeline failures:

- If a test failure from a crossgen2 pipeline **also appears in the `runtime` pipeline** (matching by fully qualified test name, regardless of error category or platform), mark that failure as a **runtime-shared failure**.
- **Do NOT create issues for runtime-shared failures.** These failures are not specific to crossgen2 and do not warrant new crossgen2 issues.
- Instead, collect all runtime-shared failures to be reported in the `noop` safe output (see Step 5).

Only failures that are **unique to the crossgen2 pipelines** (i.e., not found in the `runtime` pipeline) should proceed to Step 4 and potentially have issues created.

## Step 4: Search for Existing Issues

For each unknown failure, search GitHub for existing issues that might already track it:

1. **Search by test name**: Use GitHub search to find open issues mentioning the failing test name in `dotnet/runtime`:
   - Search with the test class name and method name
   - Check issues with labels `area-CodeGen-coreclr` or `Known Build Error`

2. **Search by error signature**: If the test name search yields no results, search for distinctive parts of the error message.

3. **Check MihuBot results**: The CI analysis script with `-SearchMihuBot` may have already found related issues — use those results.

If an existing open issue already tracks the failure, skip creating a new one. Note the existing issue number in your analysis.

## Step 5: Create Issues for New Failures

For each genuinely new, untracked failure that is **unique to the crossgen2 pipelines** (not a runtime-shared failure from Step 3), create a GitHub issue using the `create-issue` safe output.

### Assess Fix Complexity

Before creating the issue, assess whether the failure looks **simply solvable**:

**Simply solvable** (instruct Copilot to fix the root cause):
- An assertion message clearly indicates what value was expected vs actual
- A null reference exception with an obvious missing null check
- A simple type mismatch or casting error
- A race condition with an obvious synchronization fix
- The error message directly points to the fix

**Not simply solvable** (instruct Copilot to disable the test):
- Complex logic failures requiring deep domain knowledge
- Intermittent/flaky failures without clear reproduction pattern
- Failures related to infrastructure or environment issues
- Crashes or timeouts without clear root cause
- Failures that require understanding complex crossgen2 internals

### Issue Format

Create issues with the following structure:

**Title**: A concise description of the failing test (the `[Crossgen2 CI]` prefix is added automatically)

**Body**:

```markdown
### Failure Details

- **Pipeline**: <pipeline name>
- **Build**: [<build_id>](https://dev.azure.com/dnceng-public/public/_build/results?buildId=<build_id>)
- **Failed Tests**: List each failing test with its fully qualified name
- **Configuration**: <OS/arch/config if available>
- **Error Category**: <test-failure|build-error|crash|timeout>

### Failing Tests

List each failing test individually with its fully qualified name:

| Test Name | Platform | Error Type |
|-----------|----------|------------|
| `Namespace.Class.Method` | linux-x64 | AssertionError / Timeout / Crash / etc. |

### Error Output

Include the **actual error messages and stack traces** from the CI analysis output.
Do NOT write "Helix console logs are not accessible" — instead include whatever error
text the CI analysis script DID return (assertion messages, exit codes, error lines).

<details>
<summary><b>Error details</b></summary>

\`\`\`
<paste the exact error output from Get-CIStatus.ps1, including:
  - Assertion failure messages with expected vs actual values
  - Exception types and messages
  - Relevant stack trace lines
  - Exit codes
  - Build error messages
Truncate only if over 2000 characters, keeping the most diagnostic lines.>
\`\`\`

</details>

### Helix Details

- **Job**: <helix job ID if available>
- **Work Item**: <work item name if available>
- **Console Log**: <link to Helix console log if available>

### Recommended Action

<ONE OF THE FOLLOWING>

**Option A (simple fix):**
The failure appears to be straightforward to fix. Please investigate and fix the root cause:
- <specific guidance about what the fix should address>
- <relevant code location hints>

**Option B (disable test):**
This failure requires deeper investigation. Please disable the failing test by adding an `[ActiveIssue]` attribute referencing this issue:
- Locate the test method or test class
- Add `[ActiveIssue("https://github.com/dotnet/runtime/issues/ISSUE_NUMBER")]` attribute
- If the test is in a `.csproj` with crossgen2-specific conditions, the disable may need to target specific configurations
```

For Option B (disabling tests), provide specific guidance:
- If you can identify the test source file path, mention it
- Suggest the correct `[ActiveIssue]` attribute syntax
- Note which configurations to disable for (e.g., only crossgen2, only specific OS)

## Step 6: Update Cache Memory

After processing all builds, write the updated `triaged-builds.json` to `cache-memory` with:
- Build IDs that were analyzed from the crossgen2 target pipelines
- Build IDs from the `runtime` cross-reference pipeline that were checked (to avoid re-analyzing them)
- Failure signatures (test name + error category) that were triaged
- Timestamp of this triage run

Use filesystem-safe timestamp format `YYYY-MM-DD-HH-MM-SS` (no colons).

## Important Guidelines

- **Only analyze `main` branch builds.** Never analyze builds from pull request branches or feature branches. PR builds may contain failures that will be fixed before merging.
- **Do not create issues for failures that also occur in the `runtime` pipeline.** These are not crossgen2-specific. Instead, note them in the `noop` safe output.
- **Do not create duplicate issues.** Always search thoroughly before creating.
- **Do not create issues for known/tracked failures.** If Build Analysis has already matched a failure to a known issue, skip it.
- **Be conservative with "simple fix" assessments.** When in doubt, instruct Copilot to disable the test rather than attempt a fix.
- **Include specific test names and real error output in every issue.** Each issue MUST contain:
  - Fully qualified test names (not just work item names like "GC" — drill into the specific test methods)
  - Actual error messages, assertion text, or stack traces copied from the CI analysis output
  - Do NOT say "Helix console logs are not accessible without authentication" as a substitute for error details. The CI analysis script already extracts error information — use it.
- **Include enough context in issues** for Copilot Coding Agent to act without further investigation.
- **Group related failures.** If the same test fails across multiple pipelines or configurations, create a single issue covering all occurrences.
- When calling the `noop` safe output, include:
  - A summary of what was analyzed (which pipelines, how many builds)
  - Any failures that were skipped because they also appear in the `runtime` pipeline (list the test names and note they are shared with `runtime`)
  - Any failures that were skipped because they match known issues or cached entries
