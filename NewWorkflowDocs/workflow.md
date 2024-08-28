# Workflow Guide

- [Introduction](#introduction)
- [Important Concepts to Understand](#important-concepts-to-understand)
  - [Build Configurations](#build-configurations)
- [Building the Repo](#building-the-repo)
  - [General Overview](#general-overview)
  - [Get Started on your Platform and Components](#get-started-on-your-platform-and-components)
- [Warnings as Errors](#warnings-as-errors)
- [Submitting a PR](#submitting-a-pr)
- [Triaging Errors in CI](#triaging-errors-in-ci)

## Introduction

The runtime repo can be worked with on Windows, Linux, macOS, and FreeBSD. Each platform has its own specific requirements to work properly, and not all architectures are supported for dev work. The following table shows the matrix of compatibility, as well as links to each OS's requirements doc.

| Chip  | Windows  | Linux    | macOS    | FreeBSD  |
| :---: | :------: | :------: | :------: | :------: |
| x64   | &#x2714; | &#x2714; | &#x2714; | &#x2714; |
| x86   | &#x2714; |          |          |          |
| Arm32 |          | &#x2714; |          |          |
| Arm64 | &#x2714; | &#x2714; | &#x2714; |          |
|       | [Requirements](requirements/windows-requirements.md) | [Requirements](requirements/linux-requirements.md) | [Requirements](requirements/macos-requirements.md) | [Requirements](requirements/freebsd-requirements.md)

Additionally, keep in mind that cloning the full history of this repo takes roughly 400-500 MB of network transfer, inflating to a repository that can consume somewhere between 1 to 1.5 GB. A build of the repo can take somewhere between 10 and 20 GB of space for a single OS and Platform configuration depending on the portions of the product built. This might increase over time, so consider this to be a minimum bar for working with this codebase.

The runtime repo consists of three major components:

- The Runtimes (CoreCLR and Mono)
- The Libraries
- The Installer

You can run your builds from a regular terminal, from the root of the repository. Sudo and administrator privileges are not needed for this.

## Important Concepts to Understand

The following sections describe some important terminology to keep in mind while working with runtime repo builds. For more information, and a complete list of acronyms and their meanings, check out the glossary [over here](/docs/project/glossary.md).

### Build Configurations

To work with the runtime repo, there are three supported configurations (one is *CoreCLR* exclusive) that define how your build will behave:

- **Debug**: Non-optimized code. Asserts are enabled. This configuration runs the slowest. As its name suggests, it provides the best experience for debugging the product.
- **Checked** *(CoreCLR runtime exclusive)*: Optimized code. Asserts are enabled.
- **Release**: Optimized code. Asserts are disabled. Runs at the best speed, and is most suitable for performance profiling. This will impact the debugging experience however, due to compiler optimizations that make understanding what the debugger shows difficult, relative to the source code.

### Build Components

- **Runtime**: The execution engine for managed code. There are two different flavor implementations, both written in C/C++:
  - *CoreCLR*: The comprehensive execution engine originally born from .NET Framework. Its source code lives in under the [src/coreclr](/src/coreclr) subtree.
  - *Mono*: A slimmer runtime than CoreCLR, originally born open-source to bring .NET and C# support to non-Windows platforms. Due to its lightweight nature, it is less affected in terms of speed when working with the *Debug* configuration. Its source code lives in under the [src/mono](/src/mono) subtree.

- **CoreLib** *(also known as System.Private.CoreLib)*: The lowest level managed library. It is directly related to the runtime, which means it must be built in the matching configuration (e.g. Building a *Debug* runtime means *CoreLib* must also be in *Debug*). You usually don't have to worry about that, since the `clr` subset includes it, but there are some special cases where you might need to build it separately. The runtime agnostic code for this library can be found at [src/libraries/System.Private.CoreLib/src](/src/libraries/System.Private.CoreLib/src/README.md).

- **Libraries**: The bulk of dll's providing the rest of the functionality to the runtime. The libraries can be built in their own configuration, regardless of which one the runtime is using. Their source code lives in under the [src/libraries](/src/libraries) subtree.

## Building the Repo

The main script that will be in charge of most of the building you might want to do is the `build.sh`, or `build.cmd` on Windows, located at the root of the repo. This script receives as arguments the subset(s) you might want to build, as well as multiple parameters to configure your build, such as the build configuration, target operating system, target architecture, and so on.

### General Overview

Running the script as is will build all the components in *Debug* configuration. But you probably will be working with only one or two at a time, so it is more efficient to just build those. This is done by means of the `-subset` flag. For example, for CoreCLR, it would be:

```bash
./build.sh -subset clr
```

<!--
    We might need to point to a doc or briefly explain here what the packs subset
    actually means. Also, might be good to point out some subsets have dependencies
    on others.
-->

The main subset values you can use are:

- `Clr`: The full CoreCLR runtime
- `Libs`: All the libraries components, excluding their tests. This includes the libraries' native parts, refs, source assemblies, and their packages and test infrastructure.
- `Packs`: The shared framework packs, archives, bundles, installers, and the framework pack tests.
- `Host`: The .NET hosts, packages, hosting libraries, and their tests.
- `Mono`: The Mono runtime and its CoreLib.

Some subsets are subsequently divided into smaller pieces, giving you more flexibility as to what to build/rebuild depending on what you're working on. For a full list of all the supported subsets, run the build script, passing `help` as the argument to the `subset` flag.

It is also possible to build more than one subset under the same command-line. In order to do this, you have to link them together with a `+` sign in the value you're passing to `-subset`. For example, to build both, CoreCLR and Libraries in Release configuration, the command-line would look like this:

```bash
./build.sh -subset clr+libs -configuration Release
```

As a final note here, if your first argument to the build script are the subsets, you can omit the `-subset` flag altogether. Additionally, several of the supported flags also include a shorthand version (e.g. `-c` for `-configuration`). Run the script with `-h` or `-help` to get an extensive overview on all the supported flags to customize your build, including their shorthand forms, as well as a wider variety of examples.

**NOTE:** On non-Windows systems, the longhand versions of the flags can be passed with either single `-` or double `--` dashes.

<!--
    TODO: Fill the sections under construction, and add links to the editing,
    debugging, and Codespaces docs.
-->

### Get Started on your Platform and Components

Now that you've got the general idea on how to get started, it is important to mention that, while the procedure is very similar among platforms and subsets, each component has its own technicalities and details, as explained in their own specific docs:

**Component Specifics:**

- _[CoreCLR](/docs/workflow/building/building-coreclr.md)_
- _Libraries_
- _Mono_

### General Recommendations

General Recommendations Under Construction!

## Testing the Repo

Testing the Repo Under Construction!

## Performance Analysis

Performance Analysis Under Construction!

## Warnings as Errors

The repo build treats warnings as errors. Dealing with warnings when you're in the middle of making changes can be annoying (e.g. unused variable that you plan to use later). To disable treating warnings as errors, set the `TreatWarningsAsErrors` environment variable to `false` before building. This variable will be respected by both the `build.sh`/`build.cmd` root build scripts and builds done with `dotnet build` or Visual Studio. Some people may prefer setting this environment variable globally in their machine settings.

## Submitting a PR

Before submitting a PR, make sure to review the [contribution guidelines](/CONTRIBUTING.md). After you get familiarized with them, please read the [PR guide](ci/pr-guide.md) to find more information about tips and conventions around creating a PR, getting it reviewed, and understanding the CI results.

## Triaging Errors in CI

Given the size of the runtime repository, flaky tests are expected to some degree. There are a few mechanisms we use to help with the discoverability of widely impacting issues. We also have a regular procedure that ensures issues get properly tracked and prioritized. You can find more information on [triaging failures in CI](ci/failure-analysis.md).
