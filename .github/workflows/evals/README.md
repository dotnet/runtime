# CI workflow evals

Lightweight quality gates for the three agentic CI workflows
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
[Vally](https://microsoft.github.io/vally/)) against a small, self-contained
inline scenario, then grades the **safe-output** the agent emits. These are
format/behavior gates, not triage-correctness or ground-truth measurements, so
they need no recorded fixtures or network access.

- **`ci-failure-scan`** — the agent emits the create-issue safe-output it would
  file (`out/kbe.md`). Graders check it against the static Known Build Error
  format: `[ci-scan] ` title naming one failure shape, `Known Build Error` plus
  exactly one `blocking-clean-ci[-optional]` label, the three body sections, and
  exactly one fenced json block with the four `ErrorMessage`/`ErrorPattern`/
  `BuildRetry`/`ExcludeConsoleLog` keys (one signature non-empty, a specific
  substring), plus the `ci-scan-match-count` marker and no test-muting.

- **`ci-failure-fix`** — the agent acts on one open `[ci-scan]` KBE and a source
  snippet, then emits one safe-output (`out/decision.md`). Graders check it
  either **created a fix PR** (`[ci-fix]` title, linked KBE, a genuine product
  `diff` — never a test-disable) **or engaged owners** (a hand-off comment
  naming who to loop in).

- **`ci-failure-scan-feedback`** — the agent gets recorded run artifacts and a
  maintainer comment, then emits its feedback safe-output (`out/feedback.md`).
  An LLM judge checks it is **constructive**: grounded in the signals, names the
  concrete miss, and turns it into a specific, actionable next step (a prompt
  edit that quotes the triggering signal and targets an allowed workflow/
  instruction file, or an explicit justification that no edit is warranted).

Each spec mixes cheap deterministic `file-matches` graders (format) with a small
number of `prompt` LLM judges (conformance / behavior / constructiveness) and
passes at a 0.7 score threshold.

## Run locally

Node 22; a Copilot token for the agent + judges:

```bash
export COPILOT_GITHUB_TOKEN="$(gh auth token)"
vally lint --eval-spec .github/workflows/evals/ci-failure-scan.eval.yaml --strict
vally eval --eval-spec .github/workflows/evals/ci-failure-scan.eval.yaml \
  --skill-dir .github/workflows --workspace /tmp/ws --output-dir /tmp/out
```
