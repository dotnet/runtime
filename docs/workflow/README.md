# Workflow Guide

The repo can be built for the following platforms, using the provided setup and the following instructions. Before attempting to clone or build, please check these requirements.

## Build Requirements

| Chip  | Windows  | Linux    | macOS    | FreeBSD  |
| :---- | :------: | :------: | :------: | :------: |
| x64   | &#x2714; | &#x2714; | &#x2714; | &#x2714; |
| x86   | &#x2714; |          |          |          |
| ARM   | &#x2714; | &#x2714; |          |          |
| ARM64 | &#x2714; | &#x2714; |          |          |
|       | [Requirements](requirements/windows-requirements.md) | [Requirements](requirements/linux-requirements.md) | [Requirements](requirements/macos-requirements.md) | [Requirements](requirements/freebsd-requirements.md)

Before proceeding further, please click on the link above that matches your machine and ensure you have installed all the prerequisites for the build to work.

Additionally, keep in mind that cloning the full history of this repo takes roughly 400-500 MB of network transfer, inflating to a repository that can consume somewhere between 1 to 1.5 GB. A build of the repo can take somewhere between 10 and 20 GB of space for a single OS and Platform configuration depending on the portions of the product built. This might increase over time, so consider this to be a minimum bar for working with this codebase.

## Concepts

The runtime repo can be built from a regular, non-administrator command prompt, from the root of the repo, as follows:

For Linux and macOS
```bash
./build.sh
```

For Windows:
```cmd
build.cmd
```

This builds the product (in the default debug configuration), but not the tests.

For information about the different options available, supply the argument `--help|-h` when invoking the build script:
```
build -h
```

On Unix like systems, arguments can be passed in with a single `-` or double hyphen `--`.

The repository currently consists of different major parts: the runtimes, the libraries, and the installer.
To build just one part you use the root build script (build.cmd/sh), and you add the `-subset` flag.

## Editing and Debugging

For instructions on how to edit code and debug your changes, see [Editing and Debugging](editing-and-debugging.md).

## Configurations

You may need to build the tree in a combination of configurations. This section explains why.

A quick reminder of some concepts -- see the [glossary](../project/glossary.md) for more on these:

* **Debug Configuration** -- Non-optimized code.  Asserts are enabled.
* **Checked Configuration** -- Optimized code. Asserts are enabled.  Only relevant to CoreCLR runtime.
* **Release Configuration** -- Optimized code. Asserts are disabled. Runs at the best speed, and suitable for performance profiling. You will have limited debugging experience.

When we talk about mixing configurations, we're discussing the following sub-components:

* **Runtime** is the execution engine for managed code and there are two different implementations available. Both are written in C/C++, therefore, easier to debug when built in a Debug configuration.
    * CoreCLR is the comprehensive execution engine which if build in Debug Configuration it executes managed code very slowly. For example, it will take a long time to run the managed code unit tests. The code lives under [src/coreclr](../../src/coreclr).
    * Mono is portable and also slimmer runtime and it's not that sensitive to Debug Configuration for running managed code. You will still need to build it without optimizations to have good runtime debugging experience though. The code lives under [src/mono](../../src/mono).
* **CoreLib** (also known as System.Private.CoreLib) is the lowest level managed library. It has a special relationship to the runtimes and therefore it must be built in the matching configuration, e.g., if the runtime you are using was built in a Debug configuration, this must be in a Debug configuration. The runtime agnostic code for this library can be found at [src/libraries/System.Private.CoreLib/src](../../src/libraries/System.Private.CoreLib/src/README.md).
* **Libraries** is the bulk of the dlls that are oblivious to the configuration that runtimes and CoreLib were built in. They are most debuggable when built in a Debug configuration, and, happily, they still run sufficiently fast in that configuration that it's acceptable for development work. The code lives under [src/libraries](../../src/libraries).

### What does this mean for me?

At this point you probably know what you are planning to work on primarily: the runtimes or libraries.

* if you're working in runtimes, you may want to build everything in the Debug configuration, depending on how comfortable you are debugging optimized native code.
* if you're working in libraries, you will want to use debug libraries with a release version of runtime and CoreLib, because the tests will run faster.
* if you're working in CoreLib - you probably want to try to get the job done with release runtime and CoreLib, and fall back to debug if you need to. The [Building Libraries](building/libraries/README.md) document explains how you'll do this.

Now you know about configurations and how we use them, you will want to read how to build what you plan to work on. Pick one of these:

- [Building CoreCLR runtime](building/coreclr/README.md)
- [Building Mono runtime](building/mono/README.md)
- [Building Libraries](building/libraries/README.md)

After that, here's information about how to run tests:

- [Testing CoreCLR runtime](testing/coreclr/testing.md)
- [Testing Mono runtime](testing/mono/testing.md)
- [Testing Libraries](testing/libraries/testing.md)

And how to measure performance:

- [Benchmarking workflow for dotnet/runtime repository](https://github.com/dotnet/performance/blob/master/docs/benchmarking-workflow-dotnet-runtime.md)
- [Profiling workflow for dotnet/runtime repository](https://github.com/dotnet/performance/blob/master/docs/profiling-workflow-dotnet-runtime.md)
