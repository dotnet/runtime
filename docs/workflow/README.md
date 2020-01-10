# Workflow Guide

The repo can be built for the following platforms, using the provided setup and the following instructions. Before attempting to clone or build, please check these requirements.

## Requirements (what you need for building to work)

| Chip  | Windows  | Linux    | macOS    | FreeBSD  |
| :---- | :------: | :------: | :------: | :------: |
| x64   | &#x2714; | &#x2714; | &#x2714; | &#x2714; |
| x86   | &#x2714; |          |          |          |
| ARM   | &#x2714; | &#x2714; |          |          |
| ARM64 | &#x2714; | &#x2714; |          |          |
|       | [Requirements](requirements/windows-requirements.md) | [Requirements](requirements/linux-requirements.md) | [Requirements](requirements/macos-requirements.md) |

## Concepts

The runtime repo can be built from a regular, non-admin command prompt. The repository currently consists of three different partitions: the runtime (coreclr), libraries and the installer. For every partition there's a helper script available in the root (e.g. libraries.cmd/sh). The root build script (build.cmd/sh) should be used to build the entire repository.

For information about the different options available, supply the argument `-help|-h` when invoking the build script:
```
build -h
```
On Unix, arguments can be passed in with a single `-` or double hyphen `--`.

## Configurations

You may need to build the tree in a combination of configurations. This section explains why. 

A quick reminder of some concepts -- see the [glossary](../project/glossary.md) for more on these:

* **Debug configuration** -- Non-optimized code.  Asserts are enabled.
  
* **Checked configuration** -- Optimized code. Asserts are enabled.  Only relevant to CoreCLR.
  
* **Release configuration** -- Optimized code. Asserts are disabled. Runs at full speed, and suitable for performance profiling. Somewhat poorer debugging experience.
  
* **CoreCLR** (often referred to as the runtime, most code under src/coreclr) -- this is the execution engine for managed code. It is written in C/C++. When built in a debug configuration, it is easier to debug into it, but it executes managed code more slowly - so slowly it will take a long time to run the managed code unit tests
 
* **CoreLib** (also known as System.Private.CoreLib - code under src/coreclr/System.Private.CoreLib) -- this is the lowest level managed library. It has a special relationship with the runtime -- it must be in the matching configuration, e.g., if the runtime you are using was built in a debug configuration, this must be in a debug configuration

* **All other libraries** (most code under src/libraries) -- the bulk of the libraries are oblivious to the configuration that CoreCLR/CoreLib were built in. Like most code they are most debuggable when built in a debug configuration, and, happily, they still run sufficiently fast in that configuration that it's acceptable for development work.

### What does this mean for me?

* if you're working in CoreCLR proper, you may want to build everything in the debug configuration, depending on how comfortable you are debugging optimized native code
* if you're working in most libraries, you will want to use debug libraries with release CoreCLR and CoreLib, because the tests will run faster.
* if you're working in CoreLib - you probably want to try to get the job done with release CoreCLR and CoreLib, and fall back to debug if you need to. The [Building libraries](building/libraries/README.md) document explains how you'll do this.

Now you know about configurations and how we use them, you will want to read how to build what you plan to work on. Pick one of these:

- [Building coreclr](building/coreclr/README.md)
- [Building libraries](building/libraries/README.md)

After that, here's information about how to run tests:

- [Testing coreclr](testing/coreclr/testing.md)
- [Testing libraries](testing/libraries/testing.md)