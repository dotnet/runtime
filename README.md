# About This Project

This project contains the xUnit.net assertion library source code, intended to be used as a Git submodule. Code here is built with a target-framework of `netstandard1.1`, and must support both `net452` and `netcoreapp1.0`. The code must be buildable by a minimum of C# 6.0. These constraints are supported by the [suggested contribution workflow](#suggested-contribution-workflow), which makes it trivial to know when you've used unavailable features.

> _**Note:** If your PR requires a newer target framework or a newer C# language to build, please start a discussion in the related issue(s) before starting any work. PRs that arbitrarily use newer target frameworks and/or newer C# language features will need to be fixed; you may be asked to fix them, or we may fix them for you, or we may decline the PR (at our discretion)._

To open an issue for this project, please visit the [core xUnit.net project issue tracker](https://github.com/xunit/xunit/issues).

## Annotations

Whether you are using this repository via Git submodule or via the [source-based NuGet package](https://www.nuget.org/packages/xunit.assert.source), the following pre-processor directives can be used to influence the code contained in this repository:

### `XUNIT_IMMUTABLE_COLLECTIONS` (min: C# 6.0, xUnit.net v2)

There are assertions that target immutable collections. If you are using a target framework that is compatible with [`System.Collections.Immutable`](https://www.nuget.org/packages/System.Collections.Immutable), you should define `XUNIT_IMMUTABLE_COLLECTIONS` to enable the additional versions of those assertions that will consume immutable collections.

### `XUNIT_NULLABLE` (min: C# 9.0, xUnit.net v2)

Projects that consume this repository as source, which wish to use nullable reference type annotations should define the `XUNIT_NULLABLE` compilation symbol to opt-in to the relevant nullability analysis annotations on method signatures.

### `XUNIT_SKIP` (min: C# 10.0, xUnit.net v3)

The Skip family of assertions (like `Assert.Skip`) require xUnit.net v3. Define this to enable the Skip assertions.

> _**Note**: If you enable try to use it from xUnit.net v2, the test will show up as failed rather than skipped. Runtime support in the core library is required to make this feature work properly, which is why it's not supported for v2._

### `XUNIT_SPAN` (min: C# 6.0, xUnit.net v2)

There are optimized versions of `Assert.Equal` for arrays which use `Span<T>`- and/or `Memory<T>`-based comparison options. If you are using a target framework that supports `Span<T>` and `Memory<T>`, you should define `XUNIT_SPAN` to enable these new assertions.

### `XUNIT_VALUETASK` (min: C# 6.0, xUnit.net v2)

Any asynchronous assertion API (like `Assert.ThrowsAsync`) is available with versions that consume `Task` or `Task<T>`. If you are using a target framework and compiler that support `ValueTask<T>`, you should define `XUNIT_VALUETASK` to enable additional versions of those assertions that will consume `ValueTask` and/or `ValueTask<T>`.

### `XUNIT_VISIBILITY_INTERNAL`

By default, the `Assert` class has `public` visibility. This is appropriate for the default usage (as a shipped library). If your consumption of `Assert` via source is intended to be local to a single library, you should define `XUNIT_VISIBILITY_INTERNAL` to move the visibility of the `Assert` class to `internal`.

## Suggested Contribution Workflow

The pull request workflow for the assertion library is more complex than a typical single-repository project. The source code for the assertions live in this repository, and the source code for the unit tests live in the main repository: [`xunit/xunit`](https://github.com/xunit/xunit).

This workflow makes it easier to work in your branches as well as ensuring that your PR build has a higher chance of succeeding.

You will need a fork of both `xunit/assert.xunit` (this repository) and `xunit/xunit` (the main repository for xUnit.net). You will also need a local clone of `xunit/xunit`, which is where you will be doing all your work. _You do not need a clone of your `xunit/assert.xunit` fork, because we use Git submodules to bring both repositories together into a single folder._

### Before you start working

1. In a command prompt, from the root of the repository, run:

   * `git submodule update --init` to ensure the Git submodule in `/src/xunit.v3.assert/Asserts` is initialized.
   * `git switch main`
   * `git pull origin --ff-only` to ensure that `main` is up to date.
   * `git remote add fork https://github.com/yourusername/assert.xunit` to point to your fork (update the URL as appropriate).
   * `git fetch fork` to ensure that your `fork` remote is working.
   * `git switch -c my-branch-name` to create a new branch for `xunit/xunit`.

   _Replace `my-branch-name` with whatever branch name you want. We suggest you put the general feature and the `xunit/xunit` issue number into the name, to help you track the work if you're planning to help with multiple issues. An example branch name might be something like `add-support-for-IAsyncEnumerable-2367`._

1. In a command prompt, from `/src/xunit.v3.assert/Asserts`, run:

   * `git switch main`
   * `git pull origin --ff-only` to ensure that `main` is up to date.
   * `git remote add fork https://github.com/yourusername/assert.xunit` to point to your fork (update the URL as appropriate).
   * `git fetch fork` to ensure that your `fork` remote is working.
   * `git switch -c my-branch-name` to create a new branch for `xunit/assert.xunit`.

   _You may use the same branch name that you used above, as these branches are in two different repositories; identical names won't conflict, and may help you keep your work straight if you are working on multiple issues._

### Create the code and test

Open the solution in Visual Studio (or your preferred editor/IDE), and create your changes. The assertion changes will live in `/src/xunit.v3.assert/Asserts` and the tests will live in `/src/xunit.v3.assert.tests/Asserts`. In Visual Studio, the two projects you'll be working in are named `xunit.v3.assert` and `xunit.v3.assert.tests`. (You will see several `xunit.v3.assert.*` projects which ensure that the code you're writing correctly compiles in all the supported scenarios.)

When the changes are complete, you can run `./build` from the root of the repository to run the full test suite that would normally be run by a PR.

### When you're ready to submit the pull requests

1. In a command prompt, from `/src/xunit.v3.assert/Asserts`, run:

   * `git add -A`
   * `git commit`
   * `git push fork my-branch-name`

   _This pushes the branch up to your fork for you to create the PR for `xunit/assert.xunit`. The push message will give you a link (something like `https://github.com/yourusername/assert.xunit/pull/new/my-new-branch`) to start the PR process. You may do that now. We do this folder first, because we need for the source to be pushed to get a commit reference for the next step._

1. In a command prompt, from the root of the repository, run the same three commands:

   * `git add -A`
   * `git commit`
   * `git push fork my-branch-name`

   _Just like the previous steps did, this pushes up your branch for the PR for `xunit/xunit`. Only do this after you have pushed your PR-ready changes for `xunit/assert.xunit`. You may now start the PR process for `xunit/xunit` as well, and it will include the reference to the new assertion code that you've already pushed._

A maintainer will review and merge your PRs, and automatically create equivalent updates to the `v2` branch so that your assertion changes will be made available for any potential future xUnit.net v2.x releases.

_Please remember that all PRs require associated unit tests. You may be asked to write the tests if you create a PR without them. If you're not sure how to test the code in question, please feel free to open the PR and then mention that in the PR description, and someone will help you with this._

# About xUnit.net

[<img align="right" width="100px" src="https://raw.githubusercontent.com/xunit/media/main/dotnet-foundation.svg" />](https://dotnetfoundation.org/projects/project-detail/xunit)

xUnit.net is a free, open source, community-focused unit testing tool for the .NET Framework. Written by the original inventor of NUnit v2, xUnit.net is the latest technology for unit testing C#, F#, VB.NET and other .NET languages. xUnit.net works with ReSharper, CodeRush, TestDriven.NET and Xamarin. It is part of the [.NET Foundation](https://www.dotnetfoundation.org/), and operates under their [code of conduct](http://www.dotnetfoundation.org/code-of-conduct). It is licensed under [Apache 2](https://opensource.org/licenses/Apache-2.0) (an OSI approved license).

For project documentation, please visit the [xUnit.net project home](https://xunit.net/).
