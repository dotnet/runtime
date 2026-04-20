---
description: >
  Analyze pull request file changes and trigger the appropriate outerloop CI
  pipelines by posting /azp run comments.

concurrency:
  group: "outerloop-trigger-${{ github.event.pull_request.number }}"
  cancel-in-progress: true

permissions:
  contents: read
  issues: read
  pull-requests: read

network:
  allowed:
    - defaults

tools:
  github:
    mode: remote
    toolsets: [default]

safe-outputs:
  add-comment:
    max: 10
    target: "triggering"
    discussions: false
    issues: false
    hide-older-comments: true

on:
  pull_request_target:
    types: [opened, synchronize]

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
      if: contains(fromJSON('["OWNER", "MEMBER", "COLLABORATOR"]'), github.event.pull_request.author_association)
      with:
        persist-credentials: false
        sparse-checkout: .github/actions/select-copilot-pat
        sparse-checkout-cone-mode: true
        fetch-depth: 1

    - id: select-copilot-pat
      name: Select Copilot token from pool
      if: contains(fromJSON('["OWNER", "MEMBER", "COLLABORATOR"]'), github.event.pull_request.author_association)
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
  env:
    # We cannot use line breaks in this expression as it leads to a syntax error in the compiled workflow
    # If none of the `COPILOT_PAT_#` secrets were selected, then the default COPILOT_GITHUB_TOKEN is used
    COPILOT_GITHUB_TOKEN: ${{ case(needs.pre_activation.outputs.copilot_pat_number == '0', secrets.COPILOT_PAT_0, needs.pre_activation.outputs.copilot_pat_number == '1', secrets.COPILOT_PAT_1, needs.pre_activation.outputs.copilot_pat_number == '2', secrets.COPILOT_PAT_2, needs.pre_activation.outputs.copilot_pat_number == '3', secrets.COPILOT_PAT_3, needs.pre_activation.outputs.copilot_pat_number == '4', secrets.COPILOT_PAT_4, needs.pre_activation.outputs.copilot_pat_number == '5', secrets.COPILOT_PAT_5, needs.pre_activation.outputs.copilot_pat_number == '6', secrets.COPILOT_PAT_6, needs.pre_activation.outputs.copilot_pat_number == '7', secrets.COPILOT_PAT_7, needs.pre_activation.outputs.copilot_pat_number == '8', secrets.COPILOT_PAT_8, needs.pre_activation.outputs.copilot_pat_number == '9', secrets.COPILOT_PAT_9, secrets.COPILOT_GITHUB_TOKEN) }}
---

# Outerloop Pipeline Trigger

You are an automation agent for the dotnet/runtime repository. Your job is to
analyze the files changed in pull request #${{ github.event.pull_request.number }}
and determine which outerloop CI pipelines need to be triggered by posting
`/azp run <pipeline>` comments.

**IMPORTANT: Do NOT check out the pull request branch or use local filesystem
operations to inspect PR changes.** All file listings, diffs, patches, and file
contents must be obtained exclusively through the GitHub API (REST or GraphQL),
the GitHub MCP tools, or the `gh` CLI.

**CRITICAL SECURITY REQUIREMENT**: This workflow must NOT check out the PR branch.
If the compiled workflow (lock.yml) attempts to checkout the PR branch, this is a
compilation error that must be reported immediately. Approved data sources for
analyzing PR changes are:
- `GET /repos/{owner}/{repo}/pulls/{pull_number}/files` — Changed files with patches
- `GET /repos/{owner}/{repo}/pulls/{pull_number}` — PR metadata
- `GET /repos/{owner}/{repo}/contents/{path}` — File contents at specific SHAs
- GitHub GraphQL API — PRs, issues, commit history
- `gh` CLI with authenticated calls (no checkout needed)

## Step 1: Get Changed Files

Use the GitHub API (e.g. `GET /repos/{owner}/{repo}/pulls/{pull_number}/files`)
to list all files changed in PR #${{ github.event.pull_request.number }}.

**IMPORTANT - Pagination**: This endpoint is paginated (max 100 items per page).
You MUST follow pagination completely:
- Request `per_page=100` for each page
- Check the response for a `Link` header containing `rel="next"`
- Continue fetching subsequent pages until no `Link: rel="next"` header is present
- If using the GitHub MCP tools or `gh` CLI, verify pagination is handled automatically
- Aggregate the complete file list from all pages before evaluating any rules

This returns file paths, status, and patch diffs. Collect the **full list** of file
paths and their patches from **all pages** before evaluating any rules.

## Step 2: Evaluate Trigger Rules

For each rule below, check whether **any** changed file matches. Collect the
set of pipelines that need to be triggered.

### Rule: `runtime-android`

Trigger `/azp run runtime-android` if **any** changed file matches at least one
of these conditions:

1. The file's **basename** (the last path segment) contains `.Android.` — for
   example `Foo.Android.cs` or `Bar.Android.Baz.csproj`.
2. The file is inside a directory whose name is exactly `Android` — that is,
   the path contains a `/Android/` segment (or starts with `Android/`).
3. The file is inside the `System.Security.Cryptography.Native.Android`
   directory — that is, the path contains
   `System.Security.Cryptography.Native.Android/`.

### Rule: `runtime-ioslike`

Trigger `/azp run runtime-ioslike` if **any** changed file matches at least one
of these conditions:

1. The file's **basename** contains `.iOS.` — for example
   `Interop.TimeZoneInfo.iOS.cs` or `Environment.iOS.cs`.
2. The file is inside a directory whose name is exactly `iOS` or `tvOS` — that
   is, the path contains a `/iOS/` or `/tvOS/` segment (or starts with one).
3. The file is inside the `System.Security.Cryptography.Native.Apple`
   directory — that is, the path contains
   `System.Security.Cryptography.Native.Apple/`.
4. The file's **basename** contains `.Apple.` — for example
   `AesGcm.Apple.cs` or `ChainPal.Apple.cs`.
5. The file is under `src/mono/msbuild/apple/` or `src/mono/sample/iOS/`.
6. The file is under `src/tasks/MobileBuildTasks/Apple/`.
7. The file is under `src/native/libs/System.Native/ios/`.
8. The file is under `src/tests/FunctionalTests/iOS/` or
   `src/tests/FunctionalTests/tvOS/`.

### Rule: `runtime-ioslikesimulator`

Trigger `/azp run runtime-ioslikesimulator` whenever `runtime-ioslike` is
triggered — the same matching conditions apply. Both pipelines should always
be triggered together.

### Rule: `runtime-maccatalyst`

Trigger `/azp run runtime-maccatalyst` if **any** changed file matches at least
one of these conditions:

1. The file's **basename** contains `.MacCatalyst.` — for example
   `Environment.OSVersion.MacCatalyst.cs`.
2. The file is inside a directory whose name is exactly `MacCatalyst`.
3. The file is inside the `System.Security.Cryptography.Native.Apple`
   directory (shared with iOS).
4. The file's **basename** contains `.Apple.` (shared with iOS).
5. The file is under `src/mono/msbuild/apple/` (shared Apple build
   infrastructure).
6. The file is under `src/tasks/MobileBuildTasks/Apple/`.

**Note:** Changes to shared Apple infrastructure (`.Apple.` files,
`System.Security.Cryptography.Native.Apple/`, `src/mono/msbuild/apple/`,
`src/tasks/MobileBuildTasks/Apple/`) should trigger **all three** Apple
pipelines: `runtime-ioslike`, `runtime-ioslikesimulator`, and
`runtime-maccatalyst`.

### Rule: `runtime-nativeaot-outerloop`

Trigger `/azp run runtime-nativeaot-outerloop` if **any** changed file is part
of the NativeAOT compiler (`ilc`) or any of its dependencies.

This includes files under any of the following directories inside
`src/coreclr/tools/aot/`:

- `ILCompiler/` — the `ilc` entry-point application
- `ILCompiler.Compiler/` — the core NativeAOT compiler library
- `ILCompiler.RyuJit/` — the RyuJit code-generation backend
- `ILCompiler.MetadataTransform/` — metadata transformation library

It also includes files under these **shared** directories that are dependencies
of `ilc` (even though crossgen2 also uses them):

- `ILCompiler.DependencyAnalysisFramework/`
- `ILCompiler.TypeSystem/`
- `ILCompiler.Diagnostics/`

Finally, it includes shared source files linked into the above projects from
`src/coreclr/tools/Common/` (e.g. `Compiler/`, `TypeSystem/`,
`Internal/Runtime/`, `JitInterface/`). If a changed file is under
`src/coreclr/tools/Common/`, consider it a match for this rule.

### Rule: `runtime-coreclr crossgen2`

Trigger `/azp run runtime-coreclr crossgen2` if **any** changed file is part of
the `crossgen2` (ReadyToRun) compiler or any of its dependencies.

This includes files under any of the following directories inside
`src/coreclr/tools/aot/`:

- `crossgen2/` — the `crossgen2` entry-point application
- `ILCompiler.ReadyToRun/` — the ReadyToRun compiler library
- `ILCompiler.Reflection.ReadyToRun/` — ReadyToRun reflection library

It also includes files under these **shared** directories that are dependencies
of `crossgen2` (even though ilc also uses them):

- `ILCompiler.DependencyAnalysisFramework/`
- `ILCompiler.TypeSystem/`
- `ILCompiler.Diagnostics/`

And shared source files linked from `src/coreclr/tools/Common/` — if a changed
file is under `src/coreclr/tools/Common/`, consider it a match for this rule.

**Note:** A change to a shared directory (e.g. `ILCompiler.TypeSystem/` or
`src/coreclr/tools/Common/`) should trigger **both** `runtime-nativeaot-outerloop`
and `runtime-coreclr crossgen2`.

### Rule: `runtime-libraries-coreclr outerloop`

Trigger `/azp run runtime-libraries-coreclr outerloop` if **any** changed file
is a test file under `src/libraries/` that uses the `[OuterLoop]` attribute, or
if the change **introduces** the `[OuterLoop]` attribute to a test.

To evaluate this rule, inspect the **patch/diff** of each changed file under
`src/libraries/` (use the GitHub API to get file patches). A file matches if:

1. For each file, attempt to read its `patch` field from the PR files API
2. **If `patch` is present**: Search for `[OuterLoop` (in any form: `[OuterLoop]`,
   `[OuterLoop("reason")]`, etc.)
   - Match if the file already contains `[OuterLoop]` and was modified, **OR**
   - Match if the diff's added lines (`+` lines) introduce `[OuterLoop`
3. **If `patch` is missing or truncated** (which can happen for large diffs):
   - Fetch the file contents at the PR head SHA using
     `GET /repos/{owner}/{repo}/contents/{path}?ref={head_sha}`
   - Search the full file content for the `[OuterLoop` attribute
   - To determine if `[OuterLoop` was **introduced** by the change, also fetch
     the file at the PR base SHA and compare — if it has `[OuterLoop`, then
     it wasn't introduced; if it doesn't, then it was

Only consider files that are plausibly test files (e.g. under a `tests/`
subdirectory within the library, or with `Test` / `Tests` in the path or
filename).

### Rule: `runtime-coreclr outerloop`

Trigger `/azp run runtime-coreclr outerloop` if **any** changed file under
`src/tests/` matches either of these conditions:

1. **OuterLoop attribute** — The changed file uses the `[OuterLoop]` attribute.
   To detect this:
   - Attempt to read the file's `patch` field from the PR files API
   - **If `patch` is present**: Search for `[OuterLoop` and match if found in
     the file or introduced in added lines
   - **If `patch` is missing or truncated**: Fetch the file contents at the PR
     head SHA using `GET /repos/{owner}/{repo}/contents/{path}?ref={head_sha}`
     and search for `[OuterLoop`. To determine if it was introduced, also fetch
     the file at the base SHA and compare.

2. **CLRTestPriority 1** — The changed file is a `.csproj` that sets
   `<CLRTestPriority>1</CLRTestPriority>`, or the diff introduces that
   property. Also match if a changed `.cs` test file belongs to a project
   whose `.csproj` (in the same directory or a parent directory) already sets
   `<CLRTestPriority>1</CLRTestPriority>` — use the GitHub API
   (e.g. `GET /repos/{owner}/{repo}/contents/{path}`) to read the `.csproj`
   from the repository's default branch to check.

### Rule: Linked issue pipeline triggers

If the PR description contains "Closes #NNN", "Fixes #NNN", or any other
GitHub closing keyword linking to an issue, fetch each linked issue and inspect
its title and body. If the issue describes a test failure associated with a
specific CI pipeline name (e.g. `runtime-coreclr gcstress0x3-gcstress0xc`,
`runtime-coreclr jitstress`, `runtime-nativeaot-outerloop`, etc.), trigger that
pipeline.

To evaluate this rule:

1. Parse the PR description for GitHub issue-closing keywords (`closes`,
   `fixes`, `resolves` — with optional `#` or full URL) to collect linked
   issue numbers.
2. For each linked issue, use the GitHub API to read the issue title and body.
3. Look for Azure DevOps pipeline names in the issue. Pipeline names in
   dotnet/runtime follow the pattern `runtime-*` (e.g.
   `runtime-coreclr gcstress0x3-gcstress0xc`,
   `runtime-coreclr jitstress-isas-avx2`,
   `runtime-libraries-coreclr outerloop`). They typically appear in the issue
   title, in CI failure links, or in `/azp run` references within the body.
4. Add each discovered pipeline to the set of pipelines to trigger.

This rule may produce pipelines that overlap with other rules — that is fine,
duplicates are naturally deduplicated into a single set before posting.

### Rule: Re-trigger contributor-requested pipelines

If a repository contributor (someone with write/maintain/admin permission) has
previously posted a `/azp run` comment on this PR requesting specific outerloop
pipelines, those same pipelines should be re-triggered on every subsequent push.

To evaluate this rule:

1. List all comments on the PR using the GitHub API.
2. For each comment that contains `/azp run`, check whether the comment author
   has write access to the repository. Use the GitHub API to check the
   author's permission level (look for `permission` of `write`, `maintain`, or
   `admin`). **Ignore** comments from bots (e.g. `github-actions[bot]`,
   `azure-pipelines[bot]`) — only consider human contributors.
3. Parse the `/azp run` line(s) to extract pipeline names. The format is
   `/azp run <name1>, <name2>, ...` (comma-separated) or one pipeline per
   `/azp run` line.
4. Collect all pipeline names from all qualifying contributor comments and add
   them to the set of pipelines to trigger.

This ensures that when a contributor manually requests a specialized pipeline
(e.g. `runtime-coreclr jitstress`), it continues to run on future pushes
without the contributor having to re-comment each time.

## Step 3: Post Trigger Comments

Group the pipelines into batches of **up to 5 pipelines per comment**. Azure
Pipelines supports triggering multiple pipelines from a single `/azp run`
command using a comma-separated list.

For each batch, post **one** comment on the PR with the following exact format:

```
/azp run <pipeline-1>, <pipeline-2>, <pipeline-3>
```

Replace each `<pipeline-N>` with the pipeline name (e.g. `runtime-android`).

Use the `add-comment` safe output to post each batch comment.

## Step 4: Report

- **If any pipelines were posted as comments**: Do NOT call the `noop` tool.
  The `/azp run` comments already serve as the report.
- **If all candidate pipelines were skipped as duplicates** (because they are
  already running on this PR): Do NOT call the `noop` tool. The duplicate
  detection itself is sufficient.
- **Only if NO pipelines are triggered AND no duplicates were found**: Call the
  `noop` tool with a short message explaining why no outerloop pipelines are
  needed for this change set (e.g., "No outerloop pipelines triggered: changes
  are isolated to non-test code and do not affect CI-relevant directories").
