# CI workflow evals

Quality gates for the three agentic CI workflows
[`ci-failure-scan`](../ci-failure-scan.md), [`ci-failure-fix`](../ci-failure-fix.md),
and [`ci-failure-scan-feedback`](../ci-failure-scan-feedback.md).

Maintainers invoke them from a PR comment. See [`../ci-eval.yml`](../ci-eval.yml).

| Command | Grades |
| --- | --- |
| `/ci-scan eval` | ci-failure-scan safe-output |
| `/ci-fix eval` | ci-failure-fix safe-output |
| `/ci-feedback eval` | ci-failure-scan-feedback safe-output |
| `/ci-eval` | all three |

## How it works

Each `*.eval.yaml` runs the real workflow prompt as the agent, using
[Vally](https://microsoft.github.io/vally/) with the `copilot-sdk` executor,
against a live task. The agent discovers real, current data itself and emits the
safe-output it would produce, which is then graded. Graders mix cheap
deterministic `file-matches` checks for format with `tool-calls` checks for
evidence that the agent really engaged live data, and a few `prompt` LLM judges
for conformance, behavior, and constructiveness. Every grader must pass.

The workflow preserves the eval specs and installs Vally from the trusted base
branch before it checks out the PR head. This lets it evaluate PR changes to the
workflow prompts without allowing the PR to weaken its graders or toolchain.

These are format and behavior gates, not full ground-truth measurements. The
second stage, a collector that scrapes the real failures and KBEs that actually
exist and scores workflow output against them, is deferred.

- **`ci-failure-scan`** has the agent query the anonymous dnceng-public AzDO REST
  API for a currently-failing outer-loop build on `main`, extract a real error
  signature, check for an existing KBE, and emit the create-issue safe-output at
  `out/kbe.md`. Graders check the static Known Build Error format, meaning the
  title, exactly `Known Build Error` plus one blocking label, the three sections,
  a single json signature, the match-count marker, and no test-muting. They also
  check `tool-calls` evidence that it actually fetched a real build and searched
  existing KBEs.

- **`ci-failure-fix`** has the agent find a real open `[ci-scan]` Known Build
  Error issue via `gh`, reason about it, and emit one safe-output at
  `out/decision.md`. Graders check that it either created a fix PR, with a
  `[ci-fix]` title, a linked KBE, and a real diff that is never a test-disable,
  or engaged owners with a hand-off comment, and never both, plus `tool-calls`
  evidence that it acted on a real issue.

- **`ci-failure-scan-feedback`** has the agent scan real recent `[ci-scan]`
  issues and `[ci-fix]` PRs via `gh`, then emit its feedback safe-output at
  `out/feedback.md`. `tool-calls` graders assert it actually scanned both the
  scanner issues and the fixer PRs. LLM judges check the feedback is
  constructive, meaning it is grounded and quantified, names the concrete miss,
  and turns it into a specific, actionable next step. That next step is either a
  prompt edit that quotes the triggering signal and targets an allowed workflow
  or instruction file, or an explicit justification that no edit is warranted.

Because the runs are live, they need network egress to AzDO and GitHub and a
`GH_TOKEN` for the agent's `gh` calls, which `ci-eval.yml` exports the workflow
token for on the eval step. Live runs are non-deterministic and depend on what
is failing at eval time.

## Run locally

You need Node 22, a Copilot token for the agent and judges, and a GitHub token
for the agent's `gh` calls.

```bash
export COPILOT_GITHUB_TOKEN="$(gh auth token)"
export GH_TOKEN="$(gh auth token)"
vally lint --eval-spec .github/workflows/evals/ci-failure-scan.eval.yaml --strict
vally eval --eval-spec .github/workflows/evals/ci-failure-scan.eval.yaml \
  --skill-dir .github/workflows --workspace /tmp/ws --output-dir /tmp/out
```
