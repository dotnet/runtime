---
permissions:
  contents: read

network:
  allowed:
    - defaults

safe-outputs:
  noop:
    report-as-issue: false

on:
  workflow_dispatch:
    inputs:
      message:
        description: 'Message to use for the echo test'
        required: true
        type: string
  permissions: {}

# ###############################################################
# Override COPILOT_GITHUB_TOKEN with a random PAT from the pool.
# This stop-gap will be removed when org billing is available.
# See: .github/workflows/shared/pat_pool.README.md for more info.
# ###############################################################
imports:
  - shared/pat_pool.md

engine:
  id: copilot
  env:
    COPILOT_GITHUB_TOKEN: ${{ case(needs.pat_pool.outputs.pat_number == '0', secrets.COPILOT_PAT_0, needs.pat_pool.outputs.pat_number == '1', secrets.COPILOT_PAT_1, needs.pat_pool.outputs.pat_number == '2', secrets.COPILOT_PAT_2, needs.pat_pool.outputs.pat_number == '3', secrets.COPILOT_PAT_3, needs.pat_pool.outputs.pat_number == '4', secrets.COPILOT_PAT_4, needs.pat_pool.outputs.pat_number == '5', secrets.COPILOT_PAT_5, needs.pat_pool.outputs.pat_number == '6', secrets.COPILOT_PAT_6, needs.pat_pool.outputs.pat_number == '7', secrets.COPILOT_PAT_7, needs.pat_pool.outputs.pat_number == '8', secrets.COPILOT_PAT_8, needs.pat_pool.outputs.pat_number == '9', secrets.COPILOT_PAT_9, secrets.COPILOT_GITHUB_TOKEN) }}
---

## Copilot Echo
You are a test harness that validates the repository's configuration for running agentic workflows with Copilot.

### Your Task

1. **Read the input**: The user has provided a message via the workflow input: `${{ github.event.inputs.message }}`. Use this to guide your response.

2. **Produce a response**: Generate a single sentence response that is a polite and appropriate reply to the user's input.

3. **Report your output**: Call the `noop` tool with a message that shows the user's input message and your produced response, well-formatted with markdown so it renders nicely in the GitHub Actions step summary.
