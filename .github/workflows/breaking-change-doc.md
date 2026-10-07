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

checkout:
  repository: ${{ github.repository }}

tools:
  bash: ["pwsh", "gh", "jq", "mkdir"]

safe-outputs:
  report-failure-as-issue: false
  report-failed-jobs: false
  add-comment:
    target: "*"
  noop:
    report-as-issue: false  # Disable posting noop messages as issue comments
  steps:
    - name: Require successful documentation generation
      shell: bash
      env:
        AGENT_RESULT: ${{ needs.agent.result }}
      run: |
        if [ "$AGENT_RESULT" != "success" ]; then
          echo "::error::Documentation generation or outcome validation failed; refusing to publish."
          exit 1
        fi

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
  - name: Validate documentation outcome
    uses: actions/github-script@v9
    env:
      PR_NUMBER: ${{ github.event.pull_request.number || inputs.pr_number }}
      SUPPRESS_OUTPUT: ${{ github.event_name == 'workflow_dispatch' && inputs.suppress_output }}
      SAFE_OUTPUTS_PATH: ${{ steps.set-runtime-paths.outputs.GH_AW_SAFE_OUTPUTS }}
    with:
      script: |
        const fs = require('node:fs');
        function parsePrNumber(value) {
          const text = String(value);
          const number = Number(text);
          if ((typeof value !== 'number' && typeof value !== 'string') ||
              !/^\d+$/.test(text) || !Number.isSafeInteger(number) || number <= 0) {
            throw new Error('Invalid PR number: ' + text);
          }
          return number;
        }
        const output = JSON.parse(fs.readFileSync('/tmp/gh-aw/agent_output.json', 'utf8'));
        if (!Array.isArray(output.items) || output.errors?.length) {
          throw new Error('Documentation output is invalid: ' + JSON.stringify(output.errors));
        }
        const failures = output.items.filter(item =>
          ['missing_tool', 'missing_data', 'report_incomplete'].includes(item.type));
        if (failures.length) {
          throw new Error('Documentation could not be completed: ' + JSON.stringify(failures));
        }
        const comments = output.items.filter(item => item.type === 'add_comment');
        const noops = output.items.filter(item => item.type === 'noop');
        const dryRun = process.env.SUPPRESS_OUTPUT === 'true';
        if (dryRun && comments.length) {
          throw new Error('Dry-run documentation must not request publication.');
        }
        if (comments.length === 1 && noops.length === 0) {
          const comment = comments[0];
          const prNumber = parsePrNumber(process.env.PR_NUMBER);
          if (parsePrNumber(comment.item_number) !== prNumber) {
            throw new Error('Documentation comment targets the wrong PR.');
          }
          if (!comment.body?.trim()) {
            throw new Error('Documentation comment is empty.');
          }
          const rawComments = fs.readFileSync(process.env.SAFE_OUTPUTS_PATH, 'utf8')
            .split('\n')
            .filter(line => line.trim())
            .map(line => JSON.parse(line))
            .filter(item => item.type === 'add_comment');
          if (rawComments.length !== 1 || parsePrNumber(rawComments[0].item_number) !== prNumber) {
            throw new Error('Documentation requires exactly one raw comment intent for the source PR.');
          }
          const expectedBody = fs.readFileSync('artifacts/docs/breakingChanges/pr-comment.md', 'utf8');
          if (rawComments[0].body !== expectedBody) {
            throw new Error('Documentation comment does not match pr-comment.md before sanitization.');
          }
        } else if (comments.length !== 0 || noops.length !== 1 || !noops[0].message?.trim()) {
          throw new Error('Documentation must emit a comment or an explicit noop; drafts alone are not completion.');
        } else if (!dryRun && fs.existsSync('artifacts/docs/breakingChanges/pr-comment.md')) {
          throw new Error('Generated documentation drafts require publication or report_incomplete, not a noop.');
        }
        if (comments.length ||
            fs.existsSync('artifacts/docs/breakingChanges/issue-draft.md') ||
            fs.existsSync('artifacts/docs/breakingChanges/pr-comment.md')) {
          for (const name of ['issue-draft.md', 'pr-comment.md']) {
            if (!fs.readFileSync('artifacts/docs/breakingChanges/' + name, 'utf8').trim()) {
              throw new Error('Documentation draft is empty: ' + name);
            }
          }
        }

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

# Breaking Change Documentation

Create breaking change documentation for the pull request identified below.

## PR to document

- If triggered by a pull request event, the PR number is `${{ github.event.pull_request.number }}`.
- If triggered by `workflow_dispatch`, the PR number is `${{ github.event.inputs.pr_number }}`.

## Dry-run mode

Dry-run mode for this run: `${{ github.event.inputs.suppress_output || false }}`.

- If the value above is `true`,
  **do not** post a comment on the PR after producing the files. Write
  the markdown files and emit a `noop` explaining that dry-run drafts
  were generated without publication.
- If the value above is `false`, post the comment only when permitted by the
  skill's "Post the comment" rules, including its uncertainty gate.

## Instructions

Using the breaking-change-doc skill from
`.github/skills/breaking-change-doc/SKILL.md`, execute **all steps (0 through
6)** for the PR above.

Run `Get-VersionInfo.ps1` and `Build-IssueComment.ps1` as required by the
skill; do not infer the release version or recreate the comment by hand
instead of running the helpers. Use `pwsh --version` to check PowerShell,
not a compound shell command beginning with `command -v`.

In Step 6, if dry-run mode is active, skip publishing any output to the pull
request. The generated files in `artifacts/docs/breakingChanges/` are
automatically uploaded as a workflow artifact named **breaking-change-docs**
and can be downloaded from the workflow run summary page.

## Publication

For an authorized, non-dry-run outcome, read the complete
`artifacts/docs/breakingChanges/pr-comment.md` and submit it through the
`add_comment` safe output. This records the comment intent; the separate
safe-output job performs the GitHub write. Do not post with `gh pr comment`.

Use one shell command whose executable is `safeoutputs`, passing exactly
one JSON object through a single-quoted here-document:

```bash
safeoutputs add_comment . <<'EOF'
{"item_number": 123, "body": "Complete contents of pr-comment.md"}
EOF
```

Replace `123` with the source PR number and encode the complete file as a
valid JSON string, preserving its contents exactly. Do not chain commands
or pipe JSON from another command into `safeoutputs`. If the invocation
fails, correct it and retry this single-command form once. If publication
still fails, emit `report_incomplete` with the actual error; do not report
only `missing_tool`, silently finish with drafts, or substitute a `noop`
for a failed publication.

If the skill's uncertainty gate prevents publication, retain the drafts
and emit `report_incomplete` explaining what requires human review.

## When no action is needed

If no action is needed (PR has no area label, documentation already exists,
etc.), you MUST call the `noop` tool with a message explaining why. In a
non-dry-run, determine this before creating `pr-comment.md`; once the
comment is generated, submit it or report why publication is incomplete.

```json
{"noop": {"message": "No action needed: [brief explanation]"}}
```
