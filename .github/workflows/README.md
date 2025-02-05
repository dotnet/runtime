# Workflows

General guidance:

Please make sure to include the @dotnet/runtime-infrastructure group as a reviewer of your PRs.

For workflows that are triggered by pull requests, refer to GitHub's documentation for the pull_request and pull_request_target events. The pull_request_target event is the more common use case in this repository as it runs the workflow in the context of the target branch instead of in the context of the pull request's fork or branch. However, workflows that need to consume the contents of the pull request need to use the pull_request event. There are security considerations with each of the events though.

Refer to GitHub's Workflows in forked repositories and pull_request_target documentation for more information.
