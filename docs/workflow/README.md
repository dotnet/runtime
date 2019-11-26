# Workflow Guide

The repo can be built for the following platforms, using the provided setup and the following instructions.

| Chip  | Windows  | Linux    | macOS    | FreeBSD  |
| :---- | :------: | :------: | :------: | :------: |
| x64   | &#x2714; | &#x2714; | &#x2714; | &#x2714; |
| x86   | &#x2714; |          |          |          |
| ARM   | &#x2714; | &#x2714; |          |          |
| ARM64 | &#x2714; | &#x2714; |          |          |
|       | [Requirements](windows-requirements.md) | [Requirements](linux-requirements.md) | [Requirements](macos-instructions.md) |

## Building the repository

The runtime repo can be built from a regular, non-admin command prompt. The repository currently consists of three different partitions: the runtime (coreclr), libraries and the installer. For every partition there's a helper script available in the root (e.g. libraries.cmd/sh). The root build script (build.cmd/sh) should be used to build the entire repository.

For information about the different options available, suplly the argument `-help|-h` when invoking the build script:
```
libraries -h
```
On Unix, arguments can be passed in with a single `-` or double hyphen `--`.

## Workflows

For instructions on how to build, debug, test, etc. please visit the instructions in the workflow sub-folders.