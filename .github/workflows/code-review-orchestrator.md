---
description: "Polls open pull requests every 10 minutes and dispatches the code-review workflow for each PR that is new or has had commits pushed. A deterministic step computes the work list and the agent fans out via the dispatch-workflow safe output. This is separate from the built-in Copilot Code Review agent; the dispatched code-review workflow posts customized review output as a comment on the PR. Follows the OrchestratorOps, DeterministicOps, and WorkQueueOps patterns from gh-aw."

on:
  schedule: every 10m
  workflow_dispatch:
  permissions: {}

if: ${{ github.event_name == 'workflow_dispatch' || !github.event.repository.fork }}

permissions:
  contents: read
  pull-requests: read

concurrency:
  # Serialize orchestrator runs; queue overlapping ticks rather than cancelling so the
  # reviewed-SHA cache is never written by two runs at once.
  group: code-review-orchestrator
  cancel-in-progress: false

timeout-minutes: 15

network:
  allowed:
    - defaults

tools:
  # The deterministic step below queries PRs via `gh` and writes the dispatch list; the
  # agent only reads that list (cat, jq) and relays it to the dispatch-workflow safe output.
  bash: [cat, jq]

# The agent dispatches the worker via this safe output (workflow_dispatch), up to `max` per run.
# The compiler validates that the code-review workflow exists and declares workflow_dispatch.
safe-outputs:
  # Threat detection is disabled: the orchestrator only relays a deterministic list of PR
  # numbers to a trusted same-repo worker, so there is no untrusted content to screen.
  threat-detection: false
  dispatch-workflow:
    workflows: [code-review]
    max: 20

# The durable per-PR reviewed-SHA map is persisted in an actions/cache: the restore step reads
# the previous run's state before the compute step, and the save step persists the updated state
# after it, so the deterministic compute decides what to (re)dispatch from prior state.
steps:
  - name: Restore reviewed-SHA state
    uses: actions/cache/restore@v4
    with:
      path: .review-state
      key: code-review-state-${{ github.run_id }}
      restore-keys: |
        code-review-state-

  - name: Build dispatch list and advance reviewed-SHA state
    shell: bash
    env:
      GH_TOKEN: ${{ github.token }}
      MAX_DISPATCH: '20'
    run: |
      set -euo pipefail
      mkdir -p .review-state /tmp/gh-aw/agent
      STATE=".review-state/reviewed-shas.json"
      [ -f "$STATE" ] || echo '{}' > "$STATE"

      # POLL: open, non-draft PRs with their current head SHA.
      #
      # SECURITY NOTE (fork PRs): dispatching the worker runs it in base-repo context on the
      # default branch with secrets (the Copilot PAT pool), then the worker checks out the PR
      # head to review it. The worker is read-only, egress-firewalled to `defaults`, posts a
      # single sanitized comment, and the PATs are Copilot-Requests-only, so exposure is bounded.
      # Fork PRs are included so every PR is reviewed; to review only same-repo branches, add
      # `and .isCrossRepository == false` to the select below.
      gh pr list --repo "$GITHUB_REPOSITORY" --state open --limit 1000 \
        --json number,headRefOid,isDraft,updatedAt,isCrossRepository \
        --jq '[.[] | select(.isDraft == false)]' > /tmp/open_prs.json
      echo "Open non-draft PRs: $(jq 'length' /tmp/open_prs.json) (forks included)"

      # QUEUE: select PRs whose current head SHA differs from the last-dispatched SHA (new PRs
      # have no recorded SHA, so they always match; this covers any update path: push,
      # force-push, rebase, reopen, base merge). Order oldest-updated first for fairness, then
      # throttle to MAX_DISPATCH.
      SEEN="$(cat "$STATE")"
      jq -n \
        --slurpfile prs /tmp/open_prs.json \
        --argjson seen "$SEEN" \
        --argjson max "$MAX_DISPATCH" '
          $prs[0]
          | [ .[] | select( ($seen[(.number | tostring)] // "") != .headRefOid ) ]
          | sort_by(.updatedAt)
          | .[:$max]
          | map({pr_number: .number, head_sha: .headRefOid})
        ' > /tmp/gh-aw/agent/dispatch_list.json
      echo "Queued this run: $(jq 'length' /tmp/gh-aw/agent/dispatch_list.json)"
      cat /tmp/gh-aw/agent/dispatch_list.json

      # Advance state for queued PRs and prune closed PRs so it stays bounded. State records
      # dispatch intent and is advanced before the agent fans out, so the next push to a PR (a
      # new head SHA) re-queues it; a maintainer can also dispatch the code-review workflow
      # manually for a specific PR.
      tmp="$(mktemp)"
      jq \
        --slurpfile queued /tmp/gh-aw/agent/dispatch_list.json \
        --slurpfile prs /tmp/open_prs.json '
          ($prs[0] | map(.number | tostring)) as $open
          | reduce $queued[0][] as $q (.; .[($q.pr_number | tostring)] = $q.head_sha)
          | with_entries(select(.key | IN($open[])))
        ' "$STATE" > "$tmp" && mv "$tmp" "$STATE"

  - name: Save reviewed-SHA state
    if: always()
    uses: actions/cache/save@v4
    with:
      path: .review-state
      key: code-review-state-${{ github.run_id }}

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
  # The agent only relays a small precomputed list, so the subscription default model is
  # sufficient; no model is pinned.
  env:
    COPILOT_GITHUB_TOKEN: ${{ case(needs.pat_pool.outputs.pat_number == '0', secrets.COPILOT_PAT_0, needs.pat_pool.outputs.pat_number == '1', secrets.COPILOT_PAT_1, needs.pat_pool.outputs.pat_number == '2', secrets.COPILOT_PAT_2, needs.pat_pool.outputs.pat_number == '3', secrets.COPILOT_PAT_3, needs.pat_pool.outputs.pat_number == '4', secrets.COPILOT_PAT_4, needs.pat_pool.outputs.pat_number == '5', secrets.COPILOT_PAT_5, needs.pat_pool.outputs.pat_number == '6', secrets.COPILOT_PAT_6, needs.pat_pool.outputs.pat_number == '7', secrets.COPILOT_PAT_7, needs.pat_pool.outputs.pat_number == '8', secrets.COPILOT_PAT_8, needs.pat_pool.outputs.pat_number == '9', secrets.COPILOT_PAT_9, 'NO COPILOT PAT AVAILABLE') }}
---

# Code Review Orchestrator

You dispatch code-review workers. A deterministic step has already computed the work for this run.

Read the JSON array at `/tmp/gh-aw/agent/dispatch_list.json`. Each element is `{ "pr_number": <number>, "head_sha": "<sha>" }` for a pull request that is new or has had commits pushed and therefore needs a (re)review.

1. If the array is empty, do nothing and stop.
2. Otherwise, for **every** element in the array, call the **`code_review`** tool (the dispatch-workflow tool for the `code-review` workflow), passing `pr_number` set to that element's `pr_number`. Call the tool once per element. Do **not** hand-write or `echo` any JSON yourself and do **not** use any shell command to emit output -- only invoke the `code_review` tool, which records the dispatch for you.
3. You **must** dispatch **every** element in the list -- the reviewed-SHA state has already been advanced for all of them, so any element you skip will not be re-queued until its head changes again. Do **not** dispatch any pull request that is not in the list, and do **not** review pull requests yourself -- the worker performs the actual review.

After calling the tool for every element, briefly confirm how many workers you dispatched.
