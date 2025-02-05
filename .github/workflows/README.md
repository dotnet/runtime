# Workflows

General guidance:

- Please make sure to include the @dotnet/runtime-infrastructure group as a reviewer of your PRs.
- Do not use the `pull_request` event. Use `pull_request_target` instead, as documented in [Workflows in forked repositories](https://docs.github.com/en/actions/writing-workflows/choosing-when-your-workflow-runs/events-that-trigger-workflows#workflows-in-forked-repositories) and [pull_request_target](https://docs.github.com/en/actions/writing-workflows/choosing-when-your-workflow-runs/events-that-trigger-workflows#pull_request_target).
