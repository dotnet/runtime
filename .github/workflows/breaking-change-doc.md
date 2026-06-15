---
description: >
  Generate breaking change documentation for merged PRs labeled
  needs-breaking-change-doc-created. Produces two markdown files
  (issue-draft.md and pr-comment.md) and optionally comments on the PR.

concurrency:
  group: "breaking-change-doc-${{ github.event.pull_request.number || inputs.pr_number || github.run_id }}"
  cancel-in-progress: true

permissions:
  contents: read
  pull-requests: read
  issues: read

tools:
  bash: ["pwsh", "gh"]

safe-outputs:
  add-comment:
    target: "*"
  noop:
    report-as-issue: false  # Disable posting noop messages as issue comments

if: |
  github.event_name == 'workflow_dispatch' ||
  (
    !github.event.repository.fork &&
    github.event.pull_request.merged &&
    contains(github.event.pull_request.labels.*.name, 'needs-breaking-change-doc-created')
  )

post-steps:
  - name: Upload breaking change drafts
    if: always()
    uses: actions/upload-artifact@v4
    with:
      name: breaking-change-docs
      path: artifacts/docs/breakingChanges/
      retention-days: 30
      if-no-files-found: ignore

on:
  pull_request_target:
    types: [closed, labeled]
  workflow_dispatch:
    inputs:
      pr_number:
        description: "Pull Request Number"
        required: true
        type: string
      suppress_output:
        description: "Suppress workflow output (dry-run — only produce markdown workflow artifacts)"
        required: false
        type: boolean
        default: false

# ###############################################################
# Override COPILOT_GITHUB_TOKEN with a random PAT from the pool.
# This stop-gap will be removed when org billing is available.
# See: .github/workflows/shared/pat_pool.README.md for more info.
# ###############################################################
imports:
  - shared/pat_pool.md

engine:
  id: copilot
  env:
    COPILOT_GITHUB_TOKEN: ${{ case(needs.pat_pool.outputs.pat_number == '0', secrets.COPILOT_PAT_0, needs.pat_pool.outputs.pat_number == '1', secrets.COPILOT_PAT_1, needs.pat_pool.outputs.pat_number == '2', secrets.COPILOT_PAT_2, needs.pat_pool.outputs.pat_number == '3', secrets.COPILOT_PAT_3, needs.pat_pool.outputs.pat_number == '4', secrets.COPILOT_PAT_4, needs.pat_pool.outputs.pat_number == '5', secrets.COPILOT_PAT_5, needs.pat_pool.outputs.pat_number == '6', secrets.COPILOT_PAT_6, needs.pat_pool.outputs.pat_number == '7', secrets.COPILOT_PAT_7, needs.pat_pool.outputs.pat_number == '8', secrets.COPILOT_PAT_8, needs.pat_pool.outputs.pat_number == '9', secrets.COPILOT_PAT_9, secrets.COPILOT_GITHUB_TOKEN) }}
---

# Breaking Change Documentation

Create breaking change documentation for the pull request identified below.

## PR to document

- If triggered by a pull request event, the PR number is `${{ github.event.pull_request.number }}`.
- If triggered by `workflow_dispatch`, the PR number is `${{ github.event.inputs.pr_number }}`.

## Dry-run mode

- If triggered by `workflow_dispatch` with `suppress_output` = `true`,
  **do not** post a comment on the PR after producing the files. Just
  write the markdown files and stop.
- For pull_request triggers, always post the comment.

## Instructions

Using the breaking-change-doc skill from
`.github/skills/breaking-change-doc/SKILL.md`, execute **all steps (0 through
6)** for the PR above.

In Step 6, if dry-run mode is active, skip publishing any output to the pull
request. The generated files in `artifacts/docs/breakingChanges/` are
automatically uploaded as a workflow artifact named **breaking-change-docs**
and can be downloaded from the workflow run summary page.

## When no action is needed

If no action is needed (PR has no area label, documentation already exists,
etc.), you MUST call the `noop` tool with a message explaining why:

```json
{"noop": {"message": "No action needed: [brief explanation]"}}
```
