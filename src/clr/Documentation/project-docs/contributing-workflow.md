Contribution Workflow
=====================

You can contribute to .NET Core with issues and PRs. Simply filing issues for problems you encounter is a great way to contribute. Contributing implementations is greatly appreciated.

## Suggested Workflow

We use and recommend the following workflow:

1. Create an issue for your work. 
    - You can skip this step for trivial changes.
    - Reuse an existing issue on the topic, if there is one.
    - Use [CODE_OWNERS.TXT](https://github.com/dotnet/coreclr/blob/master/CODE_OWNERS.TXT) to find relevant maintainers and @ mention them to ask for feedback on your issue.
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

## PR - CI Process

The [dotnet continuous integration](http://ci.dot.net/) (CI) system will automatically perform the required builds and run tests (including the ones you are expected to run) for PRs. Builds and test runs must be clean.

If the CI build fails for any reason, the PR issue will be updated with a link that can be used to determine the cause of the failure.

## PR Feedback

Microsoft team and community members will provide feedback on your change. Community feedback is highly valued. You will often see the absence of team feedback if the community has already provided good review feedback. 

1 or more Microsoft team members will review every PR prior to merge. They will often reply with "LGTM, modulo comments". That means that the PR will be merged once the feedback is resolved. "LGTM" == "looks good to me".

There are lots of thoughts and [approaches](https://github.com/antlr/antlr4-cpp/blob/master/CONTRIBUTING.md#emoji) for how to efficiently discuss changes. It is best to be clear and explicit with your feedback. Please be patient with people who might not understand the finer details about your approach to feedback.
