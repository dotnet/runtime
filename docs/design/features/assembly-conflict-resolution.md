
# Assembly Conflict Resolution

## Summary
This document describes current behavior for dealing with references to assemblies that exist physically in more than one location including the "app" location and "framework" location(s).

## Current behavior

When a CoreCLR app is being launched, it builds a list of "probing" locations and then iterates through each <*layer*'s'>.app.deps.json file. For each assembly entry in that file, it uses the probing locations in order to determine the physical location of the assembly. Once all deps.json files are parsed, that information (which consists of a list of full paths to assemblies) is passed to the CLR which it uses to load each assembly when it is accessed.

A *layer* consists of the app at the highest layer, then a chain of one or more framework layers ending with Microsoft.NETCore.App as the lowest layer. For 2.0 and earlier, there were only the app and Microsoft.NETCore.App layers. For 2.1+, those two layers exist plus additional, optional frameworks in-between those two.

Since each layer (and other probing locations) can contain the same assembly by name, the "host policy" determines the semantics for conflict cases where the same assembly exists more than once. The "host policy" logic lives in hostpolicy.dll located in the Microsoft.NETCore.App's folder.

#### Probe Ordering
The probing locations consists of:
1.  Servicing Location
1.  App directory
1.  Framework directory(s) from higher to lower
1.  Shared Store
1.  Additional locations specified in `runtimeconfig.dev.json` or via `--additionalprobingpath` file

Detailed description of the exact probe locations is described in [host-probing](host-probing.md).

Once a deps.json entry has been found in a probe location, no further probes are performed for that entry.

#### Deps.json Ordering
The order in which each layer's deps.json is processed is:
*   The application
*   Deps file specified by optional `--additional-deps` argument
*   The framework(s) from higher to lower

#### Algorithm
1. Determine the probing paths
1. For each entry in the app's `.deps.json`
1. ->If there's already a resolved entry with the same asset name, then check the assembly and file version of the new entry against the already resolved one. If the new entry is equal or lower, skip it. Otherwise remove the existing one and go to the "else" branch below
1. ->Else (new asset, or replaced with higher version) probe for the actual asset file
1. -->For each probing path except frameworks that are higher-level
1. --->If the probing path is a framework, then check its `.deps.json` to see if it contains the exact package (by name and version). If it does, then use that location and end probing for this entry.
1. --->If the probing path is not a framework, then use that location and end probing for this entry
1. Read the additional deps from `--additional-deps` and repeat steps 3-7
1. Read each framework's deps.json (higher to lower) and repeat steps 3-7
1. Pass the set of assemblies and their paths to the CLR

Note that for an app both its `.deps.json` as well as probing path comes before the framework's `.deps.json`, so the app will usually win. Also because the app will likely reference an OOB package that the framework doesn't (because a framework, at least Microsoft.NETCore.App, has its own metapackage and does not reference OOB packages), the framework probing path never matches up in step 6 for the app's `.deps.json` package\assembly entry, so the "app wins".

The reason for only probing paths from equal or lower level framework in step 5 is that a given framework should never have a dependency on a higher-level framework, and is expected to find deps assets in its layer or lower. This is also required so that the given framework can find its asset, and replace the higher-level asset (see next paragraph).

In order to compare Assembly Version and File Version, additional metadata will need to be written to each deps.json file. If this metadata is not present (as in the case of applications published prior to 2.1) then the assembly will be considered older and will not replace any locations that the assembly was previously found at.
