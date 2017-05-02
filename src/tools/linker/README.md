# IL linker

The linker is a tool one can use to only ship the minimal possible IL code and metadata that a set of 
programs might require to run as opposed to the full libraries.

It is used by the various Xamarin products to extract only the bits of code that are needed to run
an application on Android, iOS and other platforms.

### Build & Test Status

[![Build Status](https://jenkins.mono-project.com/buildStatus/icon?job=test-linker-mainline)](https://jenkins.mono-project.com/job/test-linker-mainline/)
