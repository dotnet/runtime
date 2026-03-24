---
description: >
  Generate breaking change documentation for merged PRs labeled
  needs-breaking-change-doc-created. Produces two markdown files
  (issue-draft.md and pr-comment.md) and optionally comments on the PR.

on:
  pull_request:
    types: [closed, labeled]
    names: [needs-breaking-change-doc-created]
  workflow_dispatch:
    inputs:
      pr_number:
        description: "Pull Request Number"
        required: true
        type: string
      suppress_comment:
        description: "Suppress PR comment (dry-run — only produce markdown files)"
        required: false
        type: boolean
        default: false
  steps:
    - name: Check merge status
      id: merge_check
      env:
        PR_MERGED: ${{ github.event.pull_request.merged }}
        EVENT_NAME: ${{ github.event_name }}
      run: |
        if [ "$EVENT_NAME" = "workflow_dispatch" ]; then
          echo "Manual dispatch — skipping merge check"
          exit 0
        fi
        if [ "$PR_MERGED" != "true" ]; then
          echo "PR is not merged — skipping"
          exit 1
        fi

if: needs.pre_activation.outputs.merge_check_result == 'success'

permissions:
  contents: read

tools:
  bash: ["pwsh", "gh"]

safe-outputs:
  add-comment:
    target: "*"
---

# Breaking Change Documentation

Create breaking change documentation for the pull request identified below.

## PR to document

- If triggered by a pull request event, the PR number is `${{ github.event.pull_request.number }}`.
- If triggered by `workflow_dispatch`, the PR number is `${{ github.event.inputs.pr_number }}`.

## Dry-run mode

- If triggered by `workflow_dispatch` with `suppress_comment` = `${{ github.event.inputs.suppress_comment }}`, **do not** post a comment on the PR after producing the files. Just write the markdown files and stop.
- For pull_request triggers, always post the comment.

## Instructions

Using the breaking-change-doc skill from
`.github/skills/breaking-change-doc/SKILL.md`, execute **all steps (0 through
6)** for the PR above.

In Step 6, if dry-run mode is active, skip posting the comment — the generated
files under `artifacts/docs/breakingChanges/` are the workflow's outputs and
can be inspected in the runner workspace.

## When no action is needed

If no action is needed (PR has no area label, documentation already exists,
etc.), you MUST call the `noop` tool with a message explaining why:

```json
{"noop": {"message": "No action needed: [brief explanation]"}}
```
