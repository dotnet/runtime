# [IL Linker](linker/README.md)

The IL Linker is a tool one can use to only ship the minimal possible IL code and metadata that a set of 
programs might require to run as opposed to the full libraries.

It is used by the various Xamarin products to extract only the bits of code that are needed to run
an application on Android, iOS and other platforms.

It can also be used in the form of [ILLink.Tasks](corebuild/README.md) to reduce the size of .NET Core apps.

# [Analyzer](analyzer/README.md)

The analyzer is a tool to analyze dependencies which were recorded during linker processing and led linker to mark an item to keep it in the resulting linked assembly.

It can be used to better understand dependencies between different metadata members to help further reduce the linked output.

## How to build the IL Linker

TODO

## Build & Test Status

[![Build Status](https://jenkins.mono-project.com/buildStatus/icon?job=test-linker-mainline)](https://jenkins.mono-project.com/job/test-linker-mainline/)
