# Pull Request Guide

## Contributing Rules
All contributions to dotnet/runtime repo are made via pull requests (PRs) rather than through direct commits. The pull requests are reviewed and merged by the repository maintainers after a review and approval from at least one area maintainer.

To merge pull requests, you must have write permissions in the repository. If you are a member of the .NET team or have an official partnership you can request access [here](https://repos.opensource.microsoft.com/dotnet/teams/dotnet-corefx/join/).

## Quick Code Review Rules

* Do not mix unrelated changes in one pull request. For example, a code style change should never be mixed with a bug fix.
* All changes should follow the existing code style. You can read more about different code styles at [docs/coding-guidelines](coding-guidelines/).
* Use Draft pull requests for changes you are still working on but want early CI loop feedback. When you think your changes are ready for review, [change the status](https://help.github.com/en/github/collaborating-with-issues-and-pull-requests/changing-the-stage-of-a-pull-request) of your pull request.
* Avoid rebasing your changes. If you are asked to make changes during the review process do them as a new commit.
* To resolve merge conflicts, use "merge" instead of "rebase".

## Pull Request Ownership

Every pull request will have automatically a single `area-*` label assigned. The label not only indicates the code segment which the change touches but also the owner. We maintain a list of [areas owners](area-owners.md) for all dotnet/runtime labels. They are responsible for landing pull requests in their area in a timely manner and for helping contributors with their submitted pull request. You can ask them for assistance if you need help with landing your changes.

If during the code review process a merge conflict occurs the area owner is responsible for its resolution. Pull requests should not be on hold due to the author's unwillingness to resolve code conflicts. GitHub makes this easier by allowing simple conflict resolution using the [conflict-editor](https://help.github.com/en/github/collaborating-with-issues-and-pull-requests/resolving-a-merge-conflict-on-github).

## Merging Pull Requests

Anyone with write access can merge a pull request manually or by setting the [auto-merge](https://docs.github.com/en/github/collaborating-with-issues-and-pull-requests/automatically-merging-a-pull-request) label when it satisfies all of the following conditions:

* The PR has been approved by at least one reviewer and any other objections are addressed.
    * You can request another review from the original reviewer.
* The PR successfully builds and passes all tests in the Continuous Integration (CI) system. For more information please read to our [PR Builds](pr-builds.md) doc.

Typically, PRs are merged as one commit. It creates a simpler history than a Merge Commit. "Special circumstances" are rare, and typically mean that there are a series of cleanly separated changes that will be too hard to understand if squashed together, or for some reason we want to preserve the ability to bisect them.

## Blocking Pull Request Merging

If for whatever reason you would like to move your pull request back to an in-progress status to avoid merging it in the current form, you can do that by adding [WIP] prefix to the pull request title.

## Old Pull Request Policy

From time to time we will review older PR's (> 1 month) and check them for relevance. If we find the PR is inactive or no longer applies, we will close it. As the PR owner, you can simply reopen it if you feel your closed PR needs our attention.
