# Workflow Guide

The repo can be built for the following platforms, using the provided setup and the following instructions. Before attempting to clone or build, please check these requirements.

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

### Configurations

Sometimes you want to build this repo in a mixture of configurations. First, a quick reminder of some concepts -- see the [glossary](../project/glossary.md) for more on these:

* CoreCLR (often referred to as the runtime, most code under src/coreclr) -- this is the execution engine for managed code. It is written in C/C++. When built in a checked configuration, it is easier to debug into it, but it executes managed code more slowly - so slowly it will take a long time to run the managed code unit tests
 
* CoreLib (also known as System.Private.CoreLib - code under src/coreclr/System.Private.CoreLib) -- this is the lowest level managed library. It has a special relationship with the runtime -- it must be in the matching configuration. If the runtime you are using was built in a checked configuration, this must be in a checked configuration

* All other libraries (most code under src/libraries) -- the bulk of the libraries are oblivious to the configuration that CoreCLR/CoreLib were built in. Like most code they are most debuggable when built in a checked configuration, and, happily, they still run sufficiently fast in that configuration that it's acceptable for development work.

So if you're working in CoreCLR proper, you likely want to build everything in the checked configuration. If you're working in most libraries, you probably want to use checked libraries with release CoreCLR and CoreLib. And if you're working in CoreLib - you probably want to try to get the job done with release CoreCLR and CoreLib, and fall back to checked if you need to.

How do you achieve that mixed configuration state? You pass the `/p:CoreCLRConfiguration` flag to the build, like so for Linux:

```
src/coreclr/build.sh -release -skiptests
./libraries.sh /p:CoreCLRConfiguration=Release
```

Similarly on Windows you would do this:
```
src\coreclr\build.cmd -release -skiptests
libraries.cmd /p:CoreCLRConfiguration=Release
```

Detailed information about building and testing CoreCLR and the libraries is in the documents linked below.

## Workflows

For instructions on how to build, debug, test, etc. please visit the instructions in the workflow sub-folders.

- [Building coreclr](building/coreclr/README.md)
- [Building libraries](building/libraries/README.md)

- [Testing coreclr](testing/coreclr/testing.md)
- [Testing libraries](testing/libraries/testing.md)