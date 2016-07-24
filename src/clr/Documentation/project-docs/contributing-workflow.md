Contribution Workflow
=====================

You can contribute to .NET Core with issues and PRs. Simply filing issues for problems you encounter is a great way to contribute. Contributing implementations is greatly appreciated.

Getting Started
===============

If you are looking at getting your feet wet with some simple (but still beneficial) changes, check out _up for grabs_ issues on the [CoreCLR](https://github.com/dotnet/coreclr/labels/up-for-grabs) and [CoreFX](https://github.com/dotnet/corefx/labels/up%20for%20grabs) repos. 

For new ideas, please always start with an issue before starting development of an implementation. See [project priorities](project-priorities.md) to understand the Microsoft team's approach to engagement on general improvements to the product. Use [CODE_OWNERS.TXT](https://github.com/dotnet/coreclr/blob/master/CODE_OWNERS.TXT) to find relevant maintainers and @ mention them to ask for feedback on your issue.

You do not need to file an issue for trivial changes (e.g. typo fixes). Just create a PR for those changes.

Making a change
===============

Make a quality change. Consider and document (preferably with tests) as many usage scenarios as you can to ensure that your change will work correctly in the miriad of ways it might get used.

There are several issues to keep in mind when making a change.

Managed Code Compatibility
--------------------------
Please review [Breaking Changes](https://github.com/dotnet/corefx/blob/master/Documentation/coding-guidelines/breaking-changes.md) before making changes to managed code. Please pay the most attention to changes that affect the [Public Contract](https://github.com/dotnet/corefx/blob/master/Documentation/coding-guidelines/breaking-changes.md#bucket-1-public-contract).

Typos
-----
Typos are embarrassing! We will accept most PRs that fix typos. In order to make it easier to review your PR, please focus on a given component with your fixes or on one type of typo across the entire repository. If it's going to take >30 mins to review your PR, then we will probably ask you to chunk it up.

Coding Style Changes
--------------------

We intend to bring dotnet/corefx in to full conformance with the style guidelines described in [Coding Style](https://github.com/dotnet/corefx/blob/master/Documentation/coding-guidelines/coding-style.md). We plan to do that with tooling, in a holistic way. In the meantime, please:

* **DO NOT** send PRs for style changes. 
* **DO** give priority to the current style of the project or file you're changing even if it diverges from the general guidelines.

Commit Messages
---------------

Please format commit messages as follows (based on this [excellent post](http://tbaggery.com/2008/04/19/a-note-about-git-commit-messages.html)):

```
Summarize change in 50 characters or less

Provide more detail after the first line. Leave one blank line below the
summary and wrap all lines at 72 characters or less.

If the change fixes an issue, leave another blank line after the final
paragraph and indicate which issue is fixed in the specific format
below.

Fix #42
```

Also do your best to factor commits appropriately, i.e not too large with unrelated
things in the same commit, and not too small with the same small change applied N
times in N different commits. If there was some accidental reformatting or whitespace
changes during the course of your commits, please rebase them away before submitting
the PR.

PR - CI Process
===============

The [dotnet continuous integration](http://dotnet-ci.cloudapp.net/) (CI) system will automatically perform the required builds and run tests (including the ones you are expected to run) for PRs. Builds and test runs must be clean.

If the CI build fails for any reason, the PR issue will be updated with a link that can be used to determine the cause of the failure.

There is currently minimal test coverage for Linux and OS X builds that can be used by the dotnet CI. We are working to improve that so that more issues can be caught in CI, as is the case with Windows.

PR Feedback
===========

Microsoft team and community members will provide feedback on your change. Community feedback is highly valued. You will often see the absence of team feedback if the community has already provided good review feedback. 

1 or more Microsoft team members will review every PR prior to merge. They will often reply with "LGTM, modulo comments". That means that the PR will be merged once the feedback is resolved. "LGTM" == "looks good to me".

There are lots of thoughts and [approaches](https://github.com/antlr/antlr4-cpp/blob/master/CONTRIBUTING.md#emoji) for how to efficiently discuss changes. It is best to be clear and explicit with your feedback. Please be patient with people who might not understand the finer details about your approach to feedback.

Suggested Workflow
==================

We use and recommend the following workflow:

1. Create an issue for your work. 
  - You can skip this step for trivial changes.
  - Reuse an existing issue on the topic, if there is one.
  - Use [CODE_OWNERS.TXT](https://github.com/dotnet/coreclr/blob/CODE_OWNERS.TXT) to find relevant maintainers and @ mention them to ask for feedback on your issue.
  - Get agreement from the team and the community that your proposed change is a good one.
  - If your change adds a new API, follow the [API Review Process](https://github.com/dotnet/corefx/blob/master/Documentation/project-docs/api-review-process.md). 
  - Clearly state that you are going to take on implementing it, if that's the case. You can request that the issue be assigned to you. Note: The issue filer and the implementer don't have to be the same person.
2. Create a personal fork of the repository on GitHub (if you don't already have one).
3. Create a branch off of master (`git checkout -b mybranch`). 
  - Name the branch so that it clearly communicates your intentions, such as issue-123 or githubhandle-issue. 
  - Branches are useful since they isolate your changes from incoming changes from upstream. They also enable you to create multiple PRs from the same fork.
4. Make and commit your changes.
  - Please follow our [Commit Messages](https://github.com/dotnet/coreclr/blob/master/Documentation/project-docs/contributing-workflow.md#commit-messages) guidance.
5. Add new tests corresponding to your change, if applicable.
6. Build the repository with your changes.
  - Make sure that the builds are clean.
  - Make sure that the tests are all passing, including your new tests.
7. Create a pull request (PR) against the upstream repository's **master** branch.
  - Push your changes to your fork on GitHub (if you haven't already).

Note: It is OK for your PR to include a large number of commits. Once your change is accepted, you will be asked to squash your commits into one or some appropriately small number of commits before your PR is merged.

Note: It is OK to create your PR as "[WIP]" on the upstream repo before the implementation is done. This can be useful if you'd like to start the feedback process concurrent with your implementation. State that this is the case in the initial PR comment.
