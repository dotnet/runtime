# Workflows

General guidance:

Please make sure to include the @dotnet/runtime-infrastructure group as a reviewer of your PRs.

For workflows that are triggered by pull requests, refer to GitHub's documentation for the `pull_request` and `pull_request_target` events. The `pull_request_target` event is the more common use case in this repository as it runs the workflow in the context of the target branch instead of in the context of the pull request's fork or branch. However, workflows that need to consume the contents of the pull request need to use the `pull_request` event. There are security considerations with each of the events though.

Most workflows are intended to run only in the `dotnet/runtime` repository and not in forks. To force workflow jobs to be skipped in forks, each job should apply an `if` statement that checks the repository name or owner. Either approach works, but checking only the repository owner allows the workflow to run in copies or forks within the dotnet org.

```yaml
jobs:
  job-1:
    # Do not run this job in forks
    if: github.repository == 'dotnet/runtime'

  job-2:
    # Do not run this job in forks outside the dotnet org
    if: github.repository_owner == 'dotnet'
```

Refer to GitHub's [Workflows in forked repositories](https://docs.github.com/en/actions/writing-workflows/choosing-when-your-workflow-runs/events-that-trigger-workflows#workflows-in-forked-repositories) and [pull_request_target](https://docs.github.com/en/actions/writing-workflows/choosing-when-your-workflow-runs/events-that-trigger-workflows#pull_request_target) documentation for more information.

## Markdown Link Checking

The repository uses the [becheran/mlc](https://github.com/becheran/mlc) action to check for broken links in markdown files. This helps catch broken documentation links early in the development process.

### Configuration

- **Workflow**: `.github/workflows/markdown-link-check.yml`
- **Configuration**: `.mlc_config` (TOML format)
- **Problem Matcher**: `.github/workflows/mlc-problem-matcher.json`

### When it runs

- On pull requests that modify markdown files or the link checker configuration
- Weekly on Mondays at 9:00 AM UTC
- Can be triggered manually via workflow_dispatch

### Ignored links

The link checker is configured to ignore certain types of links that are commonly problematic:
- Local development servers (localhost, 127.0.0.1)
- Example domains (example.com)
- GitHub PR/issue links that might not exist or be private
- Internal Microsoft links that may not be publicly accessible
- Email addresses (mailto: links)
