---
description: "Review pull request changes for correctness, performance, and consistency with project conventions"

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
    toolsets: [default, search]
  web-fetch:

checkout:
  fetch-depth: 50

safe-outputs:
  add-comment:
    max: 1
    target: "triggering"
    hide-older-comments: true
    discussions: false
    issues: false

timeout-minutes: 30

concurrency:
  group: code-review-${{ github.event.pull_request.number || github.event.inputs.pr_number }}
  cancel-in-progress: true

on:
  pull_request:
    types: [opened, synchronize]
  workflow_dispatch:
    inputs:
      pr_number:
        description: 'Pull request number to review'
        required: true
        type: number

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
  model: claude-opus-4.6
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
---

# Code Review

You are an expert code reviewer for the dotnet/runtime repository. Your job is to review pull request #${{ github.event.pull_request.number || github.event.inputs.pr_number }} and post a thorough analysis as a comment.

## Step 0: Prepare Workspace (workflow_dispatch only)

When this workflow is triggered via `workflow_dispatch`, the PR branch is **not** automatically checked out — the workspace contains the default branch. Before reviewing, you **must** fetch and check out the PR branch so the workspace reflects the PR's code:

```bash
git fetch origin pull/${{ github.event.pull_request.number || github.event.inputs.pr_number }}/head:pr-branch
git checkout pr-branch
```

Additionally, when posting the review via `add-comment`, include `item_number` set to `${{ github.event.pull_request.number || github.event.inputs.pr_number }}` so the comment targets the correct PR.

## Step 1: Load Review Guidelines

Read the file `.github/skills/code-review/SKILL.md` from the repository. This contains the comprehensive code review process, analysis categories, output format, and verdict rules for dotnet/runtime.

## Step 2: Review and Post

Follow the instructions in SKILL.md to perform a thorough code review of PR #${{ github.event.pull_request.number || github.event.inputs.pr_number }}.

**Important:** Before performing any analysis, check whether the PR has any actual code changes (lines added, removed, or modified). If the diff is empty (e.g., a merge commit with no effective changes), do **not** post a review comment. Simply stop without producing any output.

When completed, post the review output as a regular comment on the PR using the `add-comment` safe output.
