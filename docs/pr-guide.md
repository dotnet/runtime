# Pull Request Guide

## Contributing Rules
All contributions to dotnet/runtime repo are made via pull requests (PRs) rather than through direct commits. The pull requests are reviewed and merged by the repository maintainers after a review and approval from at least one area maintainer.

To merge pull requests, you must have write permissions in the repository. If you are a member of the .NET team or have an official partnership you can request access [here](https://repos.opensource.microsoft.com/dotnet/teams/dotnet-corefx/join/).

## Quick Code Review Rules

* Do not mix unrelated changes in one pull request. For example, a code style change should never be mixed with a bug fix.
* All changes should follow the existing code style. You can read more about different code styles at [docs/coding-guidelines](coding-guidelines/).
* Use Draft pull requests for changes you are still working on but want early CI loop feedback. When you think your changes are ready for review, [change the status](https://help.github.com/en/github/collaborating-with-issues-and-pull-requests/changing-the-stage-of-a-pull-request) of your pull request.
* Avoid rebasing your changes. If you are asked to make changes during the review process do them as a new commit.

## Pull Request Ownership

Every pull request will have automatically a single `area-*` label assigned. The label not only indicates the code segment which the change touches but also the owner. We maintain a list of [areas owners](area-owners.md) for all dotnet/runtime labels. They are responsible for landing pull requests in their area in a timely manner and for helping contributors with their submitted pull request. You can ask them for assistance if you need help with landing your changes.

If during the code review process a merge conflict occurs the area owner is responsible for its resolution. Pull requests should not be on hold due to the author's unwillingness to resolve code conflicts. GitHub makes this easier by allowing simple conflict resolution using the [conflict-editor](https://help.github.com/en/github/collaborating-with-issues-and-pull-requests/resolving-a-merge-conflict-on-github).

## Merging Pull Requests

Anyone with write access can merge a pull request manually or by setting the [auto-merge](/labels/auto-merge) label when it satisfies all of the following conditions:

* The PR has been approved by at least one reviewer and any other objections are addressed.
    * You can request another review from the original reviewer.
* The PR successfully builds and passes all tests in the Continuous Integration (CI) system.
    * Depending on your change, you may need to re-run validation. See [rerunning validation](#rerunning-validation) below.

Typically, PRs are merged as one commit. It creates a simpler history than a Merge Commit. "Special circumstances" are rare, and typically mean that there are a series of cleanly separated changes that will be too hard to understand if squashed together, or for some reason we want to preserve the ability to bisect them.

## Rerunning Validation

Validation may fail for several reasons:

### Option 1: You have a defect in your PR

* Simply push the fix to your PR branch, and validation will start over.

### Option 2: There is a flaky test that is not related to your PR

* Your assumption should be that a failed test indicates a problem in your PR. (If we don't operate this way, chaos ensues.) If the test fails when run again, it is almost surely a failure caused by your PR. However, there are occasions where unrelated failures occur. Here's some ways to know:
  * Perhaps you see the same failure in CI results for unrelated active PR's.
  * It's a known issue listed in our [big tracking issue](https://github.com/dotnet/runtime/issues/702) or tagged `blocking-clean-ci` [(query here)](https://github.com/dotnet/runtime/issues?utf8=%E2%9C%93&q=is%3Aissue+is%3Aopen+label%3Ablocking-clean-ci+)
  * Its otherwise beyond any reasonable doubt that your code changes could not have caused this.
  * If the tests pass on rerun, that may suggest it's not related.
* In this situation, you want to re-run but not necessarily rebase on main.
  * To rerun just the failed leg(s):
    * Click on any leg. Navigate through the Azure DevOps UI, find the "..." button and choose "Retry failed legs"
    * Or, on the GitHub Checks tab choose "re-run failed checks". This will not rebase your change.
  * To rerun all validation:
    * Add a comment `/azp run runtime`
    * Or, click on "re-run all checks" in the GitHub Checks tab
    * Or, simply close and reopen the PR.
* If you have established that it is an unrelated failure, please ensure we have an active issue for it. See the [unrelated failure](#what-to-do-if-you-determine-the-failure-is-unrelated) section below.
* Whoever merges the PR should be satisfied that the failure is unrelated, is not introduced by the change, and that we are appropriately tracking it.

### Option 3: The state of the main branch HEAD is bad.

* This is the very rare case where there was a build break in main, and you got unlucky. Hopefully the break has been fixed, and you want CI to rebase your change and rerun validation.
* To rebase and rerun all validation:
  * Add a comment `/azp run runtime`
  * Or, click on "re-run all checks" in the GitHub Checks tab
  * Or, simply close and reopen the PR.
  * Or, ammend your commit with `--amend --no-edit` and force push to your branch.

### Additional information:
  * You can list the available pipelines by adding a comment like `/azp list` or get the available commands by adding a comment like `azp help`.
  * Reach out to the infrastructure team for assistance on [Teams channel](https://teams.microsoft.com/l/channel/19%3ab27b36ecd10a46398da76b02f0411de7%40thread.skype/Infrastructure?groupId=014ca51d-be57-47fa-9628-a15efcc3c376&tenantId=72f988bf-86f1-41af-91ab-2d7cd011db47) (for corpnet users) or on [Gitter](https://gitter.im/dotnet/community) in other cases.

## What to do if you determine the failure is unrelated

If you have determined the failure is definitely not caused by changes in your PR, please do this:

* Search for an [existing issue](https://github.com/dotnet/runtime/issues). Usually the test method name or (if a crash/hang) the test assembly name are good search parameters.
  * If there's an existing issue, add a comment with
    * a) the link to the build
    * b) the affected configuration (ie `net6.0-windows-Release-x64-Windows.81.Amd64.Open`)
    * c) all console output including the error message and stack trace from the Azure DevOps tab (This is necessary as retention policies are in place that recycle old builds.)
    * d) if there's a dump file (see Attachments tab in Azure DevOps) include that
    * If the issue is already closed, reopen it and update the labels to reflect the current failure state.
  * If there's no existing issue, create an issue with the same information listed above.
  * Update the original pull request with a comment linking to the new or existing issue.
* In a follow-up Pull Request, disable the failing test(s) with the corresponding issue link tracking the disable.
  * Update the tracking issue with the label `disabled-test`.
  * For libraries tests add a [`[ActiveIssue(link)]`](https://github.com/dotnet/arcade/blob/master/src/Microsoft.DotNet.XUnitExtensions/src/Attributes/ActiveIssueAttribute.cs) attribute on the test method. You can narrow the disabling down to runtime variant, flavor, and platform. For an example see [File_AppendAllLinesAsync_Encoded](https://github.com/dotnet/runtime/blob/a259ec2e967d502f82163beba6b84da5319c5e08/src/libraries/System.IO.FileSystem/tests/File/AppendAsync.cs#L899)
  * For runtime tests found under `src/tests`, please edit [`issues.targets`](https://github.com/dotnet/runtime/blob/main/src/tests/issues.targets). There are several groups for different types of disable (mono vs. coreclr, different platforms, different scenarios). Add the folder containing the test and issue mimicking any of the samples in the file.

There are plenty of possible bugs, e.g. race conditions, where a failure might highlight a real problem and it won't manifest again on a retry. Therefore these steps should be followed for every iteration of the PR build, e.g. before retrying/rebuilding.

## Blocking Pull Request Merging

If for whatever reason you would like to move your pull request back to an in-progress status to avoid merging it in the current form, you can do that by adding [WIP] prefix to the pull request title.

## Old Pull Request Policy

From time to time we will review older PR's (> 1 month) and check them for relevance. If we find the PR is inactive or no longer applies, we will close it. As the PR owner, you can simply reopen it if you feel your closed PR needs our attention.
