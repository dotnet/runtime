# IL Linker

The [IL Linker](src/linker/README.md) is a tool one can use to only ship the minimal possible IL code and metadata that a set of 
programs might require to run as opposed to the full libraries.

It is used by the various Xamarin products to extract only the bits of code that are needed to run
an application on Android, iOS and other platforms.

It can also be used in the form of [ILLink.Tasks](src/ILLink.Tasks/README.md) to reduce the size of .NET Core apps.

# Analyzer

The [analyzer](src/analyzer/README.md) is a tool to analyze dependencies which were recorded during linker processing and led linker to mark an item to keep it in the resulting linked assembly.

It can be used to better understand the dependencies between different metadata members to help further reduce the linked output.

## How to build the IL Linker

There is a shell script available in the root folder which can build the whole project and much more (build.cmd on Windows).

```sh
./build.sh
```

## Build & Test Status

**.NET Core / Mono**

[![Build Status](https://dev.azure.com/dnceng/public/_apis/build/status/mono/linker-ci?branchName=master)](https://dev.azure.com/dnceng/public/_build/latest?definitionId=364&branchName=master)
