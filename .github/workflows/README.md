# Workflows

General guidance:

Please make sure to include the @dotnet/runtime-infrastructure group as a reviewer of your PRs.

For workflows that are triggered by pull requests, refer to GitHub's documentation for the `pull_request` and `pull_request_target` events. The `pull_request_target` event is the more common use case in this repository as it runs the workflow in the context of the target branch instead of in the context of the pull request's fork or branch. However, workflows that need to consume the contents of the pull request need to use the `pull_request` event. There are security considerations with each of the events though.

Most workflows are intended to run only in the `dotnet/runtime` repository and not in forks. To force workflow jobs to be skipped in forks, each job should apply an `if` statement that checks the repository's fork property or owner. Either approach works, but checking only the repository owner allows the workflow to run in copies or forks within the dotnet org.

```yaml
jobs:
  job-1:
    # Do not run this job in forks
    if: ${{ !github.event.repository.fork }}

  job-2:
    # Do not run this job in forks outside the dotnet org
    if: github.repository_owner == 'dotnet'
```

Refer to GitHub's [Workflows in forked repositories](https://docs.github.com/en/actions/writing-workflows/choosing-when-your-workflow-runs/events-that-trigger-workflows#workflows-in-forked-repositories) and [pull_request_target](https://docs.github.com/en/actions/writing-workflows/choosing-when-your-workflow-runs/events-that-trigger-workflows#pull_request_target) documentation for more information.

Agentic workflow safe outputs sanitize posted bodies and [remove agent-provided HTML/XML comments](https://github.github.com/gh-aw/reference/safe-outputs/#text-sanitization-allowed-domains-allowed-github-references). Do not use HTML comments as machine-readable markers or persisted state. Prefer schema-validated `safe-outputs.data` when the output supports it and another fenced JSON block is compatible with downstream consumers; otherwise use stable visible fields. See [the repository agentic-workflow guidance](../agents/agentic-workflows.agent.md#repository-specific-requirements-safe-output-data) for authoring and migration requirements.

## Build-failure analysis regression checks

With Python 3, PyYAML, Bash, jq, and Node.js installed, run
`python -m unittest discover -s .github/workflows/tests -p "test_*.py"`.
The tests execute the workflow's own Bash, jq, and JavaScript with mocked API responses;
they do not download artifacts or post to GitHub. On Windows they use Git Bash
by default; `BFA_BASH` can select another Bash executable.

Artifacts are associated with failed/canceled timeline jobs using Azure DevOps'
`BuildArtifact.source` job ID, not normalized names or prefixes. Missing or
unknown source IDs leave those jobs to the mandatory hlx task-log analysis.

Command runs use the command-comment ID in their run name. A repeated request
is skipped only after that workflow's `safe_outputs` job and its processing step
have succeeded in a completed run attempt. A posted summary alone is not
completion evidence. API failures and histories exceeding GitHub's 1,000-run
search limit stop the check rather than authorizing duplicate publication.

Partial retries retain one summary per request/revision and suppress identical
inline findings using visible, workflow-generated fingerprints of the request,
revision, anchor, and body. Submitted review bodies are checked too, including
gh-aw's unanchored-comment fallback; pending reviews are not publication evidence.
Different requests, revisions, or finding bodies remain distinct. Regression
tests inject summary and review-submission failures independently and verify
that retries publish only the missing outputs; they do not inject live API faults.

Regenerate both locks with
`gh aw compile build-failure-analysis build-failure-analysis-command --strict --validate --schedule-seed dotnet/runtime`
before running the checks against a changed activation configuration.
