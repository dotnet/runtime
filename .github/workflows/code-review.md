---
description: "Review a pull request's changes for correctness, performance, and consistency with project conventions. Dispatched per-PR by the code-review-orchestrator workflow. This is separate from the built-in Copilot Code Review agent; it posts customized review output as a comment on the PR. Follows the OrchestratorOps pattern from gh-aw."

permissions:
  contents: read
  issues: read
  pull-requests: read

network:
  allowed:
    - defaults

tools:
  github:
    toolsets: [default, search]
  web-fetch:

checkout:
  fetch-depth: 50

safe-outputs:
  # Threat detection screens the untrusted PR content this worker reviews. It runs in a separate
  # gh-aw job that authenticates via the coalescing token below.
  threat-detection:
    engine:
      id: copilot
      env:
        # Workaround for github/gh-aw#43917: the detection job's `needs` omit `pat_pool`, so the
        # main engine's case(needs.pat_pool...) token can't resolve here. Authenticate by
        # coalescing the pool PAT secrets directly -- the first non-empty one wins.
        COPILOT_GITHUB_TOKEN: ${{ secrets.COPILOT_PAT_0 || secrets.COPILOT_PAT_1 || secrets.COPILOT_PAT_2 || secrets.COPILOT_PAT_3 || secrets.COPILOT_PAT_4 || secrets.COPILOT_PAT_5 || secrets.COPILOT_PAT_6 || secrets.COPILOT_PAT_7 || secrets.COPILOT_PAT_8 || secrets.COPILOT_PAT_9 || 'NO COPILOT PAT AVAILABLE' }}
  add-comment:
    max: 1
    target: "*"
    hide-older-comments: true
    discussions: false
    issues: false

timeout-minutes: 30

concurrency:
  group: code-review-${{ github.event.inputs.pr_number }}
  cancel-in-progress: true
  # The per-PR job-discriminator gives each PR's compiler-generated jobs (agent, safe_outputs,
  # conclusion) a distinct concurrency slot, so concurrently dispatched workers for different PRs
  # run independently; cancel-in-progress above still cancels a stale review when the same PR is
  # pushed again.
  job-discriminator: ${{ github.event.inputs.pr_number }}

if: ${{ github.event_name == 'workflow_dispatch' || !github.event.repository.fork }}

on:
  workflow_dispatch:
    inputs:
      pr_number:
        description: 'Pull request number to review'
        required: true
        type: number
  # The orchestrator dispatches this worker with GITHUB_TOKEN, so the run's actor is
  # github-actions[bot]. Allowlist that bot so gh-aw's membership gate authorizes
  # orchestrator-dispatched runs; human manual dispatch still requires write access via the
  # default role check.
  bots: [github-actions]
  permissions: {}

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
  env:
    COPILOT_GITHUB_TOKEN: ${{ case(needs.pat_pool.outputs.pat_number == '0', secrets.COPILOT_PAT_0, needs.pat_pool.outputs.pat_number == '1', secrets.COPILOT_PAT_1, needs.pat_pool.outputs.pat_number == '2', secrets.COPILOT_PAT_2, needs.pat_pool.outputs.pat_number == '3', secrets.COPILOT_PAT_3, needs.pat_pool.outputs.pat_number == '4', secrets.COPILOT_PAT_4, needs.pat_pool.outputs.pat_number == '5', secrets.COPILOT_PAT_5, needs.pat_pool.outputs.pat_number == '6', secrets.COPILOT_PAT_6, needs.pat_pool.outputs.pat_number == '7', secrets.COPILOT_PAT_7, needs.pat_pool.outputs.pat_number == '8', secrets.COPILOT_PAT_8, needs.pat_pool.outputs.pat_number == '9', secrets.COPILOT_PAT_9, 'NO COPILOT PAT AVAILABLE') }}
---

# Code Review Worker

You are an expert code reviewer for the dotnet/runtime repository. Your job is to review pull request #${{ github.event.inputs.pr_number }} and post a thorough analysis as a comment.

This workflow is dispatched per-PR by the `code-review-orchestrator` workflow (or manually via `workflow_dispatch`) whenever a pull request is new or has had commits pushed.

## Step 0: Prepare Workspace

This workflow is triggered via `workflow_dispatch`, so the PR branch is **not** automatically checked out — the workspace contains the default branch. Before reviewing, you **must** fetch and check out the PR branch so the workspace reflects the PR's code:

```bash
git fetch origin pull/${{ github.event.inputs.pr_number }}/head:pr-branch
git checkout pr-branch
```

When posting the review via `add-comment`, include `item_number` set to `${{ github.event.inputs.pr_number }}` so the comment targets the correct PR.

## Step 1: Load Review Guidelines

Read the file `.github/skills/code-review/SKILL.md` from the repository. This contains the comprehensive code review process, analysis categories, output format, and verdict rules for dotnet/runtime.

## Step 2: Review and Post

Follow the instructions in `.github/skills/code-review/SKILL.md` to perform a thorough code review of PR #${{ github.event.inputs.pr_number }}.

**Important:** Before performing any analysis, check whether the PR has any actual code changes (lines added, removed, or modified). If the diff is empty (e.g., a merge commit with no effective changes), do **not** post a review comment. Simply stop without producing any output.

When completed, post the review output as a regular comment on the PR using the `add-comment` safe output.
