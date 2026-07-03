# CI workflow evals

Quality gates for the three agentic CI workflows
[`ci-failure-scan`](../ci-failure-scan.md), [`ci-failure-fix`](../ci-failure-fix.md),
and [`ci-failure-scan-feedback`](../ci-failure-scan-feedback.md).

Maintainer-invoked from a PR comment; see [`../ci-eval.yml`](../ci-eval.yml):

| Command | Grades |
| --- | --- |
| `/ci-scan eval` | ci-failure-scan safe-output |
| `/ci-fix eval` | ci-failure-fix safe-output |
| `/ci-feedback eval` | ci-failure-scan-feedback safe-output |
| `/ci-eval` | all three |

## How it works

Each `*.eval.yaml` runs the **real workflow prompt** as the agent (via
[Vally](https://microsoft.github.io/vally/), `copilot-sdk` executor) against a
**live** task: the agent discovers real, current data itself and emits the
**safe-output** it would produce, which is then graded. Graders mix cheap
deterministic `file-matches` checks (format) with `tool-calls` checks (evidence
the agent really engaged live data) and a few `prompt` LLM judges
(conformance / behavior / constructiveness). Each spec passes at a 0.7 score
threshold, so a single grader can miss without failing the run.

These are format / behavior gates, not full ground-truth measurements. The
second stage — a collector that scrapes the real failures and KBEs that actually
exist and scores workflow output against them — is deferred.

- **`ci-failure-scan`** — the agent queries the dnceng-public AzDO REST API
  (anonymous) for a currently-failing outer-loop build on `main`, extracts a real
  error signature, checks for an existing KBE, and emits the create-issue
  safe-output (`out/kbe.md`). Graders check the static Known Build Error format
  (title, exactly `Known Build Error` + one blocking label, the three sections,
  a single json signature, the match-count marker, no test-muting) **and**
  `tool-calls` evidence that it actually fetched a real build and searched
  existing KBEs.

- **`ci-failure-fix`** — the agent finds a real open `[ci-scan]` / `Known Build
  Error` issue via `gh`, reasons about it, and emits one safe-output
  (`out/decision.md`). Graders check it **created a fix PR** (`[ci-fix]` title,
  linked KBE, a real diff — never a test-disable) **or engaged owners** (a
  hand-off comment), never both, plus `tool-calls` evidence it acted on a real
  issue.

- **`ci-failure-scan-feedback`** — the agent scans real recent `[ci-scan]` issues
  and `[ci-fix]` PRs via `gh`, then emits its feedback safe-output
  (`out/feedback.md`). `tool-calls` graders assert it actually scanned both the
  scanner issues and the fixer PRs; LLM judges check the feedback is
  **constructive** — grounded and quantified, naming the concrete miss and
  turning it into a specific, actionable next step (a prompt edit that quotes the
  triggering signal and targets an allowed workflow/instruction file, or an
  explicit justification that no edit is warranted).

Because the runs are live, they need network egress (AzDO / GitHub) and a
`GH_TOKEN` for the agent's `gh` calls; `ci-eval.yml` exports the workflow token
on the eval step. Live runs are non-deterministic and depend on what is failing
at eval time.

## Run locally

Node 22; a Copilot token for the agent + judges, and a GitHub token for the
agent's `gh` calls:

```bash
export COPILOT_GITHUB_TOKEN="$(gh auth token)"
export GH_TOKEN="$(gh auth token)"
vally lint --eval-spec .github/workflows/evals/ci-failure-scan.eval.yaml --strict
vally eval --eval-spec .github/workflows/evals/ci-failure-scan.eval.yaml \
  --skill-dir .github/workflows --workspace /tmp/ws --output-dir /tmp/out
```
