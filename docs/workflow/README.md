# Workflow Guide

- [Introduction](#introduction)
- [Important Concepts to Understand](#important-concepts-to-understand)
  - [Build Configurations](#build-configurations)
- [Building the Repo](#building-the-repo)
  - [General Overview](#general-overview)
  - [Get Started on your Platform and Components](#get-started-on-your-platform-and-components)
  - [General Recommendations](#general-recommendations)
- [Testing the Repo](#testing-the-repo)
  - [Performance Analysis](#performance-analysis)
- [Warnings as Errors](#warnings-as-errors)
- [Submitting a PR](#submitting-a-pr)
- [Triaging Errors in CI](#triaging-errors-in-ci)

## Introduction

The runtime repo can be worked with on Windows, Linux, macOS, and FreeBSD. Each platform has its own specific requirements to work properly, and not all architectures are supported for dev work. The following table shows the matrix of compatibility, as well as links to each OS's requirements doc. If you are using WSL directly (i.e. not Docker), then follow the Linux requirements doc.

| Chip  | Windows  | Linux    | macOS    | FreeBSD  |
| :---: | :------: | :------: | :------: | :------: |
| x64   | &#x2714; | &#x2714; | &#x2714; | &#x2714; |
| x86   | &#x2714; | &#x2718; | &#x2718; | &#x2718; |
| Arm32 | &#x2718; | &#x2714; | &#x2718; | &#x2718; |
| Arm64 | &#x2714; | &#x2714; | &#x2714; | &#x2718; |
|       | [Requirements](requirements/windows-requirements.md) | [Requirements](requirements/linux-requirements.md) | [Requirements](requirements/macos-requirements.md) | [Requirements](requirements/freebsd-requirements.md)

Additionally, keep in mind that cloning the full history of this repo takes roughly 400-500 MB of network transfer, inflating to a repository that can consume somewhere between 1 to 1.5 GB. A build of the repo can take somewhere between 10 and 20 GB of space for a single OS and Platform configuration depending on the portions of the product built. This might increase over time, so consider this to be a minimum bar for working with this codebase.

The runtime repo consists of three major components:

- The Runtimes (CoreCLR and Mono)
- The Libraries
- The Installer

You can run your builds from a regular terminal, from the root of the repository. Sudo and administrator privileges are not needed for this.

- For instructions on how to edit code and make changes, see [Editing and Debugging](/docs/workflow/editing-and-debugging.md).
- For instructions on how to debug CoreCLR, see [Debugging CoreCLR](/docs/workflow/debugging/coreclr/debugging-runtime.md).
- For instructions on using GitHub Codespaces, see [Codespaces](/docs/workflow/Codespaces.md).

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

The main script that will be in charge of most of the building you might want to do is the `build.sh`, or `build.cmd` on Windows, located at the root of the repo. This script receives as arguments the subset(s) you might want to build, as well as multiple parameters to configure your build, such as the configuration, target operating system, target architecture, and so on.

**NOTE:** If you plan on using Docker to work on the runtime repo, read [this doc](/docs/workflow/using-docker.md) first, as it explains how to set it up, as well as the images and the containers, so that you are ready to start following the building and testing instructions in the next sections and their linked docs.

### General Overview

Running the script as is with no arguments whatsoever, will build the whole repo in *Debug* configuration, for the OS and architecture of your machine. But you probably will be working with only one or two components at a time, so it is more efficient to just build those. This is done by means of the `-subset` flag. For example, for CoreCLR, it would be:

```bash
./build.sh -subset clr
```

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

If you require to use different configurations for different subsets, there are some specific flags you can use:

- `-runtimeConfiguration (-rc)`: The CoreCLR build configuration
- `-librariesConfiguration (-lc)`: The Libraries build configuration
- `-hostConfiguration (-hc)`: The Host build configuration

The behavior of the script is that the general configuration flag `-c` affects all subsets that have not been qualified with a more specific flag, as well as the subsets that don't have a specific flag supported, like `packs`. For example, the following command-line would build the libraries in *Release* mode and the runtime in *Debug* mode:

```bash
./build.sh -subset clr+libs -configuration Release -runtimeConfiguration Debug
```

In this example, the `-lc` flag was not specified, so `-c` qualifies `libs`. And in the first example, only `-c` was passed, so it qualifies both, `clr` and `libs`.

As an extra note here, if your first argument to the build script are the subsets, you can omit the `-subset` flag altogether. Additionally, several of the supported flags also include a shorthand version (e.g. `-c` for `-configuration`). Run the script with `-h` or `-help` to get an extensive overview on all the supported flags to customize your build, including their shorthand forms, as well as a wider variety of examples.

**NOTE:** On non-Windows systems, the longhand versions of the flags can be passed with either single `-` or double `--` dashes.

### Get Started on your Platform and Components

Now that you've got the general idea on how to get started, it is important to mention that, while the procedure is very similar among platforms and subsets, each component has its own technicalities and details, as explained in their own specific docs:

**Component Specifics:**

- [CoreCLR](/docs/workflow/building/coreclr/README.md)
- [Libraries](/docs/workflow/building/libraries/README.md)
- [Mono](/docs/workflow/building/mono/README.md)

**NOTE:** *NativeAOT* is part of CoreCLR, but it has its own specifics when it comes to building. We have a separate doc dedicated to it [over here](/docs/workflow/building/coreclr/nativeaot.md).

### General Recommendations

- If you're working with the runtimes, then the usual recommendation is to build everything in *Debug* mode. That said, if you know you won't be debugging the libraries source code but will need them (e.g. for a *Core_Root* build), then building the libraries on *Release* instead will provide a more productive experience.
- The counterpart to the previous point: When you are working in libraries. In this case, it is recommended to build the runtime on *Release* and the libraries on *Debug*.
- If you're working on *CoreLib*, then you probably want to try to get the job done with a *Release* runtime, and fall back to *Debug* if you need to.

## Testing the Repo

Building the components of the repo is just part of the experience. The runtime repo also includes vast test suites you can run to ensure your changes work properly as expected and don't inadvertently break something else. Each component has its own methodologies to run their tests, which are explained in their own specific docs:

- [CoreCLR](/docs/workflow/testing/coreclr/testing.md)
  - [NativeAOT](/docs/workflow/building/coreclr/nativeaot.md#running-tests)
- [Libraries](/docs/workflow/testing/libraries/testing.md)
- [Mono](/docs/workflow/testing/mono/testing.md)

### Performance Analysis

Fixing bugs and adding new features aren't the only things to work on in the runtime repo. We also have to ensure performance is kept as optimal as can be, and that is done through benchmarking and profiling. If you're interested in conducting these kinds of analysis, the following links will show you the usual workflow you can follow:

* [Benchmarking Workflow for dotnet/runtime repository](https://github.com/dotnet/performance/blob/master/docs/benchmarking-workflow-dotnet-runtime.md)
* [Profiling Workflow for dotnet/runtime repository](https://github.com/dotnet/performance/blob/master/docs/profiling-workflow-dotnet-runtime.md)

## Warnings as Errors

The repo build treats warnings as errors. Dealing with warnings when you're in the middle of making changes can be annoying (e.g. unused variable that you plan to use later). To disable treating warnings as errors, set the `TreatWarningsAsErrors` environment variable to `false` before building. This variable will be respected by both the `build.sh`/`build.cmd` root build scripts and builds done with `dotnet build` or Visual Studio. Some people may prefer setting this environment variable globally in their machine settings.

## Submitting a PR

Before submitting a PR, make sure to review the [contribution guidelines](/CONTRIBUTING.md). After you get familiarized with them, please read the [PR guide](/docs/workflow/ci/pr-guide.md) to find more information about tips and conventions around creating a PR, getting it reviewed, and understanding the CI results.

## Triaging Errors in CI

Given the size of the runtime repository, flaky tests are expected to some degree. There are a few mechanisms we use to help with the discoverability of widely impacting issues. We also have a regular procedure that ensures issues get properly tracked and prioritized. You can find more information on [triaging failures in CI](/docs/workflow/ci/failure-analysis.md).
