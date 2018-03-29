# Additional Deps

## Summary
This document describes current and proposed behavior for dealing with "light-up" scenarios regarding additional deps functionality.

## Current behavior
The deps.json file format specifies assets including managed assemblies, resource assemblies and native libraries to load.

Every applicaton has its own `<app>.deps.json` file which is automatically processed. If an application needs additional deps files, typically for "lightup" scenaris, it can specify that by:
1) The `--additional-deps` command line option
2) If this is not set, use the `DOTNET_ADDITIONAL_DEPS` environment variable






