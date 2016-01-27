Contributing to .NET Core
=========================

The .NET Core team maintains several guidelines for contributing to the .NET Core repos, which are provided below. Many of these are straightforward, while others may seem subjective. A .NET Core team member will be happy to explain why a guideline is defined as it is.

Contribution Guidelines
=======================

- [Licensing](#copyright) describes the licensing practices for the project.
- [General Contribution Guidance](#general-contribution-guidance) describes general contribution guidance, including more subjective stylistic guidelines.
- [Contribution Bar](#contribution-bar) describes the bar that the team uses to accept changes.
- [Contribution Workflow](contributing-workflow.md) describes the workflow that the team uses for considering and accepting changes.
- [Garbage Collection Guidelines](garbage-collector-guidelines.md) for changes that affect the GC.
- [Performance Guidelines](performance-guidelines.md) for changes in performance critical code or that otherwise affect performance.
- [Porting the JIT](https://github.com/dotnet/coreclr/pull/2214#issuecomment-161850464) to other chip architectures.

General Contribution Guidance
=============================

There are several issues to keep in mind when making a change.

Managed Code Compatibility
--------------------------
Please review [Breaking Changes](https://github.com/dotnet/corefx/blob/master/Documentation/coding-guidelines/breaking-changes.md) before making changes to managed code. Please pay the most attention to changes that affect the [Public Contract](https://github.com/dotnet/corefx/blob/master/Documentation/coding-guidelines/breaking-changes.md#bucket-1-public-contract).

Typos
-----
Typos are embarrassing! We will accept most PRs that fix typos. In order to make it easier to review your PR, please focus on a given component with your fixes or on one type of typo across the entire repository. If it's going to take >30 mins to review your PR, then we will probably ask you to chunk it up.

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

DOs and DON'Ts
--------------

* **DO** follow our [coding style](https://github.com/dotnet/corefx/blob/master/Documentation/coding-guidelines/coding-style.md) (C# code-specific)
* **DO** give priority to the current style of the project or file you're changing even if it diverges from the general guidelines.
* **DO** include tests when adding new features. When fixing bugs, start with
  adding a test that highlights how the current behavior is broken.
* **DO** keep the discussions focused. When a new or related topic comes up
  it's often better to create new issue than to side track the discussion.
* **DO** blog and tweet (or whatever) about your contributions, frequently!

* **DO NOT** send PRs for style changes. 
* **DON'T** surprise us with big pull requests. Instead, file an issue and start
  a discussion so we can agree on a direction before you invest a large amount
  of time.
* **DON'T** commit code that you didn't write. If you find code that you think is a good fit to add to .NET Core, file an issue and start a discussion before proceeding.
* **DON'T** submit PRs that alter licensing related files or headers. If you believe there's a problem with them, file an issue and we'll be happy to discuss it.
* **DON'T** add API additions without filing an issue and discussing with us first. See [API Review Process](https://github.com/dotnet/corefx/blob/master/Documentation/project-docs/api-review-process.md).

Contribution "Bar"
==================

Project maintainers will merge changes that align with [project priorities](project-priorities.md) and/or improve the product significantly for a broad set of apps. Proposals must also satisfy the published [guidelines for .NET Core](#contribution-guidelines).

Maintainers will not merge changes that have narrowly-defined benefits, due to compatibility risk. The CoreCLR codebase is used by several Microsoft products (e.g. Windows Phone, ASP.NET Core, .NET Framework 4.x) to enable execution of managed code. Changes to the open source codebase can become part of these products, but are first reviewed and tested to ensure they are correct for those products and will not inadvertently break applications. We may revert changes if they are found to be breaking.

Contributing Ports
------------------

We encourage ports of CoreCLR to other platforms. Linux and OS X ports are in progress and have a lot of momentum behind them. There is also interest in a [FreeBSD port](https://github.com/dotnet/coreclr/issues/455) (and OpenBSD and NetBSD).

Ports have a weaker contribution bar, since they do not contribute to compatibility risk with existing Microsoft products on Windows. For ports, we are primarily looking for functionaly correct implementations.

Contributing to mscorlib library
--------------------------------

Most managed code changes should be made in the [CoreFX](https://github.com/dotnet/corefx) repo. We have moved and are continuing to move many mscorlib types to CoreFX. Please use the following general rule-of-thumb for choosing the right repo to make your change (start by creating an issue):

- The type or concept doesn't yet exist in .NET Core -> choose CoreFX.
- The type exists in both CoreCLR and CoreFX repo -> choose CoreFX.
- The type exists in CoreCLR only -> choose CoreCLR.
- In doubt -> choose CoreFX.

Please see [Breaking Changes](https://github.com/dotnet/corefx/blob/master/Documentation/coding-guidelines/breaking-changes.md) to understand our requirements on changes that could impact compatibility. Please pay the most attention to changes that affect the [Public Contract](https://github.com/dotnet/corefx/blob/master/Documentation/coding-guidelines/breaking-changes.md#bucket-1-public-contract). We will not accept changes that break compatibility.

Licensing
=========

The .NET Core project sources are licensed as [MIT](../../LICENSE.TXT). The project contains source from other projects that may be licensed differently, which are called out in [3rd party notices](../../THIRD-PARTY-NOTICES).

.NET Core binaries are produced and licensed separately. Microsoft produces a distribution of .NET Core licensed under the [.NET Library License](https://www.microsoft.com/net/dotnet_library_license.htm). Other groups or companies may produce their own distributions of .NET Core.

Copyright
---------

The .NET Core project copyright is held by ".NET Foundation and Contributors" except where otherwise called out (see [3rd party notices](../../THIRD-PARTY-NOTICES)). Please read the [.NET Core license](../../LICENSE.TXT) to review the copyright.

File Headers
------------

The following file header is the used for .NET Core. Please use it for new files.

```
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
```

The addition of existing files from other projects is handled on a case by case basis. 

Contributor License Agreement
-----------------------------

You must sign a [.NET Foundation Contribution License Agreement (CLA)](http://cla2.dotnetfoundation.org) before your PR will be merged. This a one-time requirement for projects in the .NET Foundation. You can read more about [Contribution License Agreements (CLA)](http://en.wikipedia.org/wiki/Contributor_License_Agreement) on wikipedia.

Signing the CLA is super simple and can be done in less than a minute.

You don't have to do this up-front. You can simply clone, fork, and submit your pull-request as usual. When your pull-request is created, it is classified by a CLA bot. If the change is trivial (e.g. you just fixed a typo), then the PR is labelled with `cla-not-required`. Otherwise it's classified as `cla-required`. Once you signed a CLA, the current and all future pull-requests will be labelled as `cla-signed`.
