# Workflow Guide

- [Build Requirements](#build-requirements)
- [Getting Yourself Started](#getting-yourself-started)
- [Configurations and Subsets](#configurations-and-subsets)
  - [What does this mean for me?](#what-does-this-mean-for-me)
- [Full Instructions on Building and Testing the Runtime Repo](#full-instructions-on-building-and-testing-the-runtime-repo)
- [Warnings as Errors](#warnings-as-errors)
- [Submitting a PR](#submitting-a-pr)
- [Triaging errors in CI](#triaging-errors-in-ci)

The repo can be built for the following platforms, using the provided setup and the following instructions. Before attempting to clone or build, please check the requirements that match your machine, and ensure you install and prepare all as necessary.

## Build Requirements

| Chip  | Windows  | Linux    | macOS    | FreeBSD  |
| :---- | :------: | :------: | :------: | :------: |
| x64   | &#x2714; | &#x2714; | &#x2714; | &#x2714; |
| x86   | &#x2714; |          |          |          |
| Arm32 |          | &#x2714; |          |          |
| Arm64 | &#x2714; | &#x2714; | &#x2714; |          |
|       | [Requirements](requirements/windows-requirements.md) | [Requirements](requirements/linux-requirements.md) | [Requirements](requirements/macos-requirements.md) | [Requirements](requirements/freebsd-requirements.md)

Additionally, keep in mind that cloning the full history of this repo takes roughly 400-500 MB of network transfer, inflating to a repository that can consume somewhere between 1 to 1.5 GB. A build of the repo can take somewhere between 10 and 20 GB of space for a single OS and Platform configuration depending on the portions of the product built. This might increase over time, so consider this to be a minimum bar for working with this codebase.

## Getting Yourself Started

The runtime repo can be built from a regular, non-administrator command prompt, from the root of the repo.

The repository currently consists of three different major parts:

* The Runtimes
* The Libraries
* The Installer

More info on this, as well as the different build configurations in the [Configurations and Subsets section](#configurations-and-subsets).

This was a concise introduction and now it's time to show the specifics of building specific subsets in any given supported platform, since most likely you will want to customize your builds according to what component(s) you're working on, as well as how you configured your build environment. We have links to instructions depending on your needs [in this section](#full-instructions-on-building-and-testing-the-runtime-repo).

* For instructions on how to edit code and make changes, see [Editing and Debugging](editing-and-debugging.md).
* For instructions on how to debug CoreCLR, see [Debugging CoreCLR](/docs/workflow/debugging/coreclr/debugging-runtime.md).
* For instructions on using GitHub Codespaces, see [Codespaces](/docs/workflow/Codespaces.md).

## Configurations and Subsets

You may need to build the tree in a combination of configurations. This section explains why.

<!-- LINK-UPDATES -->
A quick reminder of some concepts -- see the [glossary](/docs/project/glossary.md) for more on these:

* **Debug Configuration** -- Non-optimized code.  Asserts are enabled.
* **Checked Configuration** -- Optimized code. Asserts are enabled.  _Only relevant to CoreCLR runtime._
* **Release Configuration** -- Optimized code. Asserts are disabled. Runs at the best speed, and suitable for performance profiling. This will impact the debugging experience due to compiler optimizations that make understanding what the debugging is showing difficult to reason about, relative to the source code.

When we talk about mixing configurations, we're discussing the following sub-components:

<!-- LINK-UPDATES -->
* **Runtime** is the execution engine for managed code and there are two different implementations available. Both are written in C/C++, therefore, easier to debug when built in a Debug configuration.
  * CoreCLR is the comprehensive execution engine which, if built in Debug Configuration, executes managed code very slowly. For example, it will take a long time to run the managed code unit tests. The code lives under [src/coreclr](/src/coreclr).
  * Mono is a portable and also slimmer runtime and it's not that sensitive to Debug Configuration for running managed code. You will still need to build it without optimizations to have good runtime debugging experience though. The code lives under [src/mono](/src/mono).
* **CoreLib** (also known as System.Private.CoreLib) is the lowest level managed library. It has a special relationship to the runtimes and therefore it must be built in the matching configuration, e.g., if the runtime you are using was built in a Debug configuration, this must be in a Debug configuration. The runtime agnostic code for this library can be found at [src/libraries/System.Private.CoreLib/src](/src/libraries/System.Private.CoreLib/src/README.md).
* **Libraries** is the bulk of the dlls that are oblivious to the configuration that runtimes and CoreLib were built in. They are most debuggable when built in a Debug configuration, and happily, they still run sufficiently fast in that configuration that it's acceptable for development work. The code lives under [src/libraries](/src/libraries).

<!-- TODO: Provide a list of the possible subsets, since right now it's all up to one's own knowledge and guessing. -->
To build just one part of the repo, you add the `-subset` flag with the subset you wish to build to the root build script _(build.cmd/sh)_. You can specify more than one by linking them with the `+` operator (e.g. `-subset clr+libs` would build CoreCLR and the libraries). Note that if the subset is the first argument you pass to the script, you can omit the `--subset` flag altogether.

### What does this mean for me?

At this point you probably know what you are planning to work on primarily: the runtimes or libraries. As general suggestions on how to proceed, here are some ideas:

* If you're working in runtimes, you may want to build everything in the Debug configuration, depending on how comfortable you are debugging optimized native code.
* If you're working in libraries, you will want to use debug libraries with a release version of runtime and CoreLib, because the tests will run faster.
* If you're working in CoreLib - you probably want to try to get the job done with release runtime and CoreLib, and fall back to debug if you need to. The [Building Libraries](/docs/workflow/building/libraries/README.md) document explains how you'll do this.

## Full Instructions on Building and Testing the Runtime Repo

Now you know about configurations and how we use them, so now you will want to read how to build what you plan to work on. Each of these will have further specific instructions or links for whichever platform you are developing on.

* [Building CoreCLR runtime](/docs/workflow/building/coreclr/README.md)
* [Building Mono runtime](/docs/workflow/building/mono/README.md)
* [Building Libraries](/docs/workflow/building/libraries/README.md)

After that, here's information about how to run tests:

* [Testing CoreCLR runtime](/docs/workflow/testing/coreclr/testing.md)
* [Testing Mono runtime](/docs/workflow/testing/mono/testing.md)
* [Testing Libraries](/docs/workflow/testing/libraries/testing.md)

And how to measure performance:

* [Benchmarking workflow for dotnet/runtime repository](https://github.com/dotnet/performance/blob/master/docs/benchmarking-workflow-dotnet-runtime.md)
* [Profiling workflow for dotnet/runtime repository](https://github.com/dotnet/performance/blob/master/docs/profiling-workflow-dotnet-runtime.md)

## Warnings as Errors

The repo build treats warnings as errors. Dealing with warnings when you're in the middle of making changes can be annoying (e.g. unused variable that you plan to use later). To disable treating warnings as errors, set the `TreatWarningsAsErrors` environment variable to `false` before building. This variable will be respected by both the `build.sh`/`build.cmd` root build scripts and builds done with `dotnet build` or Visual Studio. Some people may prefer setting this environment variable globally in their machine settings.

## Submitting a PR

Before submitting a PR, make sure to review the [contribution guidelines](../../CONTRIBUTING.md). After you get familiarized with them, please read the [PR guide](ci/pr-guide.md) to find more information about tips and conventions around creating a PR, getting it reviewed, and understanding the CI results.

## Triaging errors in CI

Given the size of the runtime repository, flaky tests are expected to some degree. There are a few mechanisms we use to help with the discoverability of widely impacting issues. We also have a regular procedure that ensures issues get properly tracked and prioritized. You can find more information on [triaging failures in CI](ci/failure-analysis.md).
