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
Each eval attaches a read-only GitHub MCP server with the `pull_requests`,
`repos`, `issues`, and `search` toolsets. The `GITHUB_TOKEN` that the eval job
supplies to that server has only the job's read permissions, allowing the
scanner to use the `github` MCP server's `search_issues` tool.

These are format and behavior gates, not full ground-truth measurements. The
second stage, a collector that scrapes the real failures and KBEs that actually
exist and scores workflow output against them, is deferred.

- **`ci-failure-scan`** has the agent query the anonymous dnceng-public AzDO REST
  API for a currently-failing outer-loop build on `main`, extract a real error
  signature, check for an existing KBE, and emit the create-issue safe-output at
  `out/kbe.md`. Graders check the static Known Build Error format, meaning the
  title, exactly `Known Build Error` plus one blocking label, the three sections,
  collapsed authoring guidance, a single json signature, the collapsed
  workflow-owned positive match-count metadata, and no test-muting. They also check
  `tool-calls` evidence that it actually fetched a real build and searched existing
  KBEs.

- **`ci-failure-fix`** runs the workflow's deterministic scanner-author filter
  in trusted eval setup before the agent starts. The agent then reads a
  candidate's body and comments through GitHub MCP, reasons about the real open
  `[ci-scan]` Known Build Error, and emits one safe-output at
  `out/decision.md`. Graders check that it either created a fix PR, with a
  `[ci-fix]` title, a linked KBE, and a real diff that is never a test-disable,
  or engaged owners with a hand-off comment, and never both. A deterministic
  program grader validates the trusted filter output metadata and requires
  successful MCP body and comments reads for the same candidate referenced by
  `Linked KBE:` in the decision. The eval setup runs the same checked-in filter
  script before the agent and preserves the artifact for grading.
  An empty candidate list reports the live eval as unavailable (a grader error,
  not a pass); `noop` cannot pass. The production empty-list skip is covered by
  the deterministic intake tests instead.

  This eval connects directly to the GitHub MCP server, not through production's
  filtering gateway. It checks MCP usage and remediation behavior, but does not
  validate production filtering; gateway-parity coverage remains separate work.

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

The deterministic fixer tests need Python 3, Bash, jq, and Node with the eval
dependencies installed (`npm ci --prefix .github/workflows/evals`), but no
credentials or network access during testing. They exercise the shared intake
script and grader, including author filtering, pagination, empty results, API
failures, repository/job conditions, trusted candidate metadata, and
candidate/read/comments/decision identity:

```bash
python3 .github/workflows/evals/test_ci_failure_fix_candidates.py
```

You need Node 22.12 or newer, Docker, a Copilot token for the agent and judges,
and a GitHub token for the agent's `gh` calls and the GitHub MCP server.

```bash
npm ci --prefix .github/workflows/evals
export PATH="$PWD/.github/workflows/evals/node_modules/.bin:$PATH"
export COPILOT_GITHUB_TOKEN="$(gh auth token)"
export GH_TOKEN="$(gh auth token)"
export GITHUB_PERSONAL_ACCESS_TOKEN="$(gh auth token)"
vally lint --eval-spec .github/workflows/evals/ci-failure-scan.eval.yaml --strict
vally eval --eval-spec .github/workflows/evals/ci-failure-scan.eval.yaml \
  --skill-dir .github/workflows --workspace /tmp/ws --output-dir /tmp/out
```
