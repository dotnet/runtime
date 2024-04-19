# Contribution to .NET Runtime

You can contribute to .NET Runtime with issues and PRs. Simply filing issues for problems you encounter is a great way to contribute. Contributing implementations is greatly appreciated.

## Reporting Issues

We always welcome bug reports, API proposals and overall feedback. Here are a few tips on how you can make reporting your issue as effective as possible.

### Identify Where to Report

The .NET codebase is distributed across multiple repositories in the [dotnet organization](https://github.com/dotnet). Depending on the feedback you might want to file the issue on a different repo. Here are a few common repos:

* [dotnet/runtime](https://github.com/dotnet/runtime) .NET runtime, libraries and shared host installers.
* [dotnet/roslyn](https://github.com/dotnet/roslyn) C# and VB compiler.
* [dotnet/aspnetcore](https://github.com/dotnet/aspnetcore) ASP.NET Core.
* [dotnet/efcore](https://github.com/dotnet/efcore) Entity Framework Core.
* [dotnet/maui](https://github.com/dotnet/maui) .NET MAUI.
* [dotnet/core](https://github.com/dotnet/core) Can be used to submit feedback if not sure what repo to use.

### Finding Existing Issues

Before filing a new issue, please search our [open issues](https://github.com/dotnet/runtime/issues) to check if it already exists.

If you do find an existing issue, please include your own feedback in the discussion. Do consider upvoting (👍 reaction) the original post, as this helps us prioritize popular issues in our backlog.

### Writing a Good API Proposal

Please review our [API review process](https://github.com/dotnet/runtime/blob/main/docs/project/api-review-process.md) documents for guidelines on how to submit an API review. When ready to submit a proposal, please use the [API Suggestion issue template](https://github.com/dotnet/runtime/issues/new?assignees=&labels=api-suggestion&template=02_api_proposal.yml&title=%5BAPI+Proposal%5D%3A+).

### Writing a Good Bug Report

Good bug reports make it easier for maintainers to verify and root cause the underlying problem. The better a bug report, the faster the problem will be resolved. Ideally, a bug report should contain the following information:

* A high-level description of the problem.
* A _minimal reproduction_, i.e. the smallest size of code/configuration required to reproduce the wrong behavior.
* A description of the _expected behavior_, contrasted with the _actual behavior_ observed.
* Information on the environment: OS/distro, CPU arch, SDK version, etc.
* Additional information, e.g. is it a regression from previous versions? are there any known workarounds?

When ready to submit a bug report, please use the [Bug Report issue template](https://github.com/dotnet/runtime/issues/new?assignees=&labels=&template=01_bug_report.yml).

#### Why are Minimal Reproductions Important?

A reproduction lets maintainers verify the presence of a bug, and diagnose the issue using a debugger. A _minimal_ reproduction is the smallest possible console application demonstrating that bug. Minimal reproductions are generally preferable since they:

1. Focus debugging efforts on a simple code snippet,
2. Ensure that the problem is not caused by unrelated dependencies/configuration,
3. Avoid the need to share production codebases.

#### Are Minimal Reproductions Required?

In certain cases, creating a minimal reproduction might not be practical (e.g. due to nondeterministic factors, external dependencies). In such cases you would be asked to provide as much information as possible, for example by sharing a memory dump of the failing application. If maintainers are unable to root cause the problem, they might still close the issue as not actionable. While not required, minimal reproductions are strongly encouraged and will significantly improve the chances of your issue being prioritized and fixed by the maintainers.

#### How to Create a Minimal Reproduction

The best way to create a minimal reproduction is gradually removing code and dependencies from a reproducing app, until the problem no longer occurs. A good minimal reproduction:

* Excludes all unnecessary types, methods, code blocks, source files, nuget dependencies and project configurations.
* Contains documentation or code comments illustrating expected vs actual behavior.
* If possible, avoids performing any unneeded IO or system calls. For example, can the ASP.NET based reproduction be converted to a plain old console app?

## Contributing Changes

Project maintainers will merge changes that improve the product significantly.

The [Pull Request Guide](docs/workflow/ci/pr-guide.md) and [Copyright](docs/project/copyright.md) docs define additional guidance.

### DOs and DON'Ts

Please do:

* **DO** follow our [coding style](docs/coding-guidelines/coding-style.md) (C# code-specific).
* **DO** give priority to the current style of the project or file you're changing even if it diverges from the general guidelines.
* **DO** include tests when adding new features. When fixing bugs, start with
  adding a test that highlights how the current behavior is broken.
* **DO** keep the discussions focused. When a new or related topic comes up
  it's often better to create new issue than to side track the discussion.
* **DO** clearly state on an issue that you are going to take on implementing it.
* **DO** blog and tweet (or whatever) about your contributions, frequently!

Please do not:

* **DON'T** make PRs for style changes.
* **DON'T** surprise us with big pull requests. Instead, file an issue and start
  a discussion so we can agree on a direction before you invest a large amount
  of time.
* **DON'T** commit code that you didn't write. If you find code that you think is a good fit to add to the .NET runtime, file an issue and start a discussion before proceeding.
* **DON'T** submit PRs that alter licensing related files or headers. If you believe there's a problem with them, file an issue and we'll be happy to discuss it.
* **DON'T** add API additions without filing an issue and discussing with us first. See [API Review Process](docs/project/api-review-process.md).

### Breaking Changes

Contributions must maintain [API signature](docs/coding-guidelines/breaking-changes.md#bucket-1-public-contract) and behavioral compatibility. Contributions that include [breaking changes](docs/coding-guidelines/breaking-changes.md) will be rejected. Please file an issue to discuss your idea or change if you believe that it may affect managed code compatibility.

### Suggested Workflow

We use and recommend the following workflow:

1. Create an issue for your work.
    - You can skip this step for trivial changes.
    - Reuse an existing issue on the topic, if there is one.
    - Get agreement from the team and the community that your proposed change is a good one.
    - If your change adds a new API, follow the [API Review Process](docs/project/api-review-process.md).
    - Clearly state that you are going to take on implementing it, if that's the case. You can request that the issue be assigned to you. Note: The issue filer and the implementer don't have to be the same person.
2. Create a personal fork of the repository on GitHub (if you don't already have one).
3. In your fork, create a branch off of main (`git checkout -b mybranch`).
    - Name the branch so that it clearly communicates your intentions, such as issue-123 or githubhandle-issue.
    - Branches are useful since they isolate your changes from incoming changes from upstream. They also enable you to create multiple PRs from the same fork.
4. Make and commit your changes to your branch.
    - [Workflow Instructions](docs/workflow/README.md) explains how to build and test.
    - Please follow our [Commit Messages](#commit-messages) guidance.
5. Add new tests corresponding to your change, if applicable.
6. Build the repository with your changes.
    - Make sure that the builds are clean.
    - Make sure that the tests are all passing, including your new tests.
7. Create a pull request (PR) against the dotnet/runtime repository's **main** branch.
    - State in the description what issue or improvement your change is addressing.
    - Check if all the Continuous Integration checks are passing. Refer to [triaging failures in CI](docs/workflow/ci/failure-analysis.md) to check if any outstanding errors are known.
8. Wait for feedback or approval of your changes from the [area owners](docs/area-owners.md).
    - Details about the pull request [review procedure](docs/workflow/ci/pr-guide.md).
9. When area owners have signed off, and all checks are green, your PR will be merged.
    - The next official build will automatically include your change.
    - You can delete the branch you used for making the change.

### Help Wanted (Up for Grabs)

The team marks the most straightforward issues as [help wanted](https://github.com/dotnet/runtime/labels/help%20wanted). This set of issues is the place to start if you are interested in contributing but new to the codebase.

### Commit Messages

Please format commit messages as follows (based on [A Note About Git Commit Messages](http://tbaggery.com/2008/04/19/a-note-about-git-commit-messages.html)):

```
Summarize change in 50 characters or less

Provide more detail after the first line. Leave one blank line below the
summary and wrap all lines at 72 characters or less.

If the change fixes an issue, leave another blank line after the final
paragraph and indicate which issue is fixed in the specific format
below.

Fix #42
```

Also do your best to factor commits appropriately, not too large with unrelated things in the same commit, and not too small with the same small change applied N times in N different commits.

### Contributor License Agreement

You must sign a [.NET Foundation Contribution License Agreement (CLA)](https://cla.dotnetfoundation.org) before your PR will be merged. This is a one-time requirement for projects in the .NET Foundation. You can read more about [Contribution License Agreements (CLA)](http://en.wikipedia.org/wiki/Contributor_License_Agreement) on Wikipedia and the [Microsoft Open Source](https://opensource.microsoft.com/cla/) program.

You don't have to do this up-front. You can simply clone, fork, and submit your pull-request as usual. When your pull-request is created, it is classified by a CLA bot. If the change is trivial (for example, you just fixed a typo), then the PR is labelled with `cla-not-required`. Otherwise it's classified as `cla-required` and the CLA bot will present the agreement to you on the pull-request. The CLA bot will provide a prompt to sign the CLA through a comment on the pull-request. Once you sign a CLA, the current and all future pull-requests will be labelled as `cla-signed`.

### File Headers

The following file header is the used for files in this repo. Please use it for new files.

```
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
```

- See [class.cpp](./src/coreclr/vm/class.cpp) for an example of the header in a C++ file.
- See [List.cs](./src/libraries/System.Private.CoreLib/src/System/Collections/Generic/List.cs) for an example of the header in a C# file.

### PR - CI Process

The [dotnet continuous integration](https://dev.azure.com/dnceng-public/public/_build) (CI) system will automatically perform the required builds and run tests (including the ones you are expected to run) for PRs. Builds and test runs must be clean or have bugs properly filed against flaky/unexpected failures that are unrelated to your change.

If the CI build fails for any reason, the PR issue will link to the `Azure DevOps` build with further information on the failure.

### PR Feedback

Microsoft team and community members will provide feedback on your change. Community feedback is highly valued. You will often see the absence of team feedback if the community has already provided good review feedback.

One or more Microsoft team members will review every PR prior to merge. They will often reply with "LGTM, modulo comments". That means that the PR will be merged once the feedback is resolved. "LGTM" == "looks good to me".

There are lots of thoughts and [approaches](https://github.com/antlr/antlr4-cpp/blob/master/CONTRIBUTING.md#emoji) for how to efficiently discuss changes. It is best to be clear and explicit with your feedback. Please be patient with people who might not understand the finer details about your approach to feedback.

### Contributing Ports

We encourage ports of CoreCLR to other platforms. There are multiple ports ongoing at any one time. You may be interested in one of the following ports:

Chips:

- [ARM32](https://github.com/dotnet/runtime/labels/arch-arm32)
- [ARM64](https://github.com/dotnet/runtime/labels/arch-arm64)
- [X86](https://github.com/dotnet/runtime/labels/arch-x86)

Operating System:

- [Linux](https://github.com/dotnet/runtime/labels/os-linux)
- [macOS](https://github.com/dotnet/runtime/labels/os-mac-os-x)
- [Windows Subsystem for Linux](https://github.com/dotnet/runtime/labels/os-windows-wsl)
- [FreeBSD](https://github.com/dotnet/runtime/labels/os-freebsd)

Note: Add links to install instructions for each of these ports.

Ports have a weaker contribution bar, at least initially. A functionally correct implementation is considered an important first goal. Performance, reliability and compatibility are all important concerns after that.

#### Copying Files from Other Projects

The .NET runtime uses some files from other projects, typically where a binary distribution does not exist or would be inconvenient.

The following rules must be followed for PRs that include files from another project:

- The license of the file is [permissive](https://en.wikipedia.org/wiki/Permissive_free_software_licence).
- The license of the file is left in-tact.
- The contribution is correctly attributed in the [3rd party notices](./THIRD-PARTY-NOTICES.TXT) file in the repository, as needed.

See [IdnMapping.cs](./src/libraries/System.Private.CoreLib/src/System/Globalization/IdnMapping.cs) for an example of a file copied from another project and attributed in the [CoreCLR 3rd party notices](./THIRD-PARTY-NOTICES.TXT) file.

#### Porting Files from Other Projects

There are many good algorithms implemented in other languages that would benefit the .NET runtime. The rules for porting a Java file to C#, for example, are the same as would be used for copying the same file, as described above.

[Clean-room](https://en.wikipedia.org/wiki/Clean_room_design) implementations of existing algorithms that are not permissively licensed will generally not be accepted. If you want to create or nominate such an implementation, please create an issue to discuss the idea.
