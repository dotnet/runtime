
# Assembly Conflict Resolution

## Summary
This document describes current and proposed behavior for dealing with references to assemblies that exist physically in more than one location including the "app" location and "framework" location(s). It proposes moving from "app wins" to "framework wins" during a [minor] or [major] roll-forward when the framework has a newer version of the given assembly.

The corresponding issue is https://github.com/dotnet/core-setup/issues/3546.

## Current behavior

When a CoreCLR app is being launched, it builds a list of "probing" locations and then iterates through each <*layer*'s'>.app.deps.json file. For each assembly entry in that file, it uses the probing locations in order to determine the physical location of the assembly. Once all deps.json files are parsed, that information (which consists of a list of full paths to assemblies) is passed to the CLR which it uses to load each assembly when it is accessed.

A *layer* consists of the app at the highest layer, then then a chain of one or more framework layers ending with Microsoft.NETCore.App as the lowest layer. For 2.0 and earlier, there were only the app and Microsoft.NETCore.App layers. For 2.1+, those two layers exist plus additional, optional frameworks in-between those two.

Since each layer (and other probing locations) can contain the same assembly by name, the "host policy" determines the semantics for conflict cases where the same assembly exists more than once. The "host policy" logic lives in hostpolicy.dll located in the Microsoft.NETCore.App's folder.

#### Probe Ordering
The probing locations consists of:
1.  Serving Location
1.  Shared Store
1.  Framework directory(s) from higher to lower
1.  App directory
1.  Additional locations specified in runtimeconfig.dev.json file

For example, here's the probing order on Windows when running a non-published folder (in order to get additional locations from the app.runtimeconfig.dev.json file)
* `C:\\Program Files (x86)\\coreservicing\\x64`
*	`C:\\Program Files (x86)\\coreservicing\\pkgs`
*	`C:\\Program files\\dotnet\\shared\\Microsoft.NETCore.App\\2.0.0`
*	(app location)
*	`C:\\Users\\<user>\\.nuget\\packages`
	`C:\\Program Files (x86)\\Microsoft SDKs\\NuGetPackagesFallback`
*	`C:\\Program Files\\dotnet\\sdk\\NuGetFallbackFolder`

Once a deps.json entry has been found in a probe location, no further probes are performed for that entry.

#### Deps.json Ordering
The order in which each layer's deps.json is processed is:
*   The application
*   Deps file specified by optional `--additional-deps` argument
*   The framework(s) from higher to lower

#### Algorithm
1. Determine the probing paths
1. For each entry in the app's deps.json
1. ->For each probing path
1. -->If the probing path is a framework, then check its deps.json to see if it contains the exact package (by name and version). If so, then use the framework's location and end probing for this entry
1. -->If the probing path is not a framework, then use that location and end probing for this entry
1. Read the additional deps from `--additional-deps` and repeat steps 3-5
1. Read each framework's deps.json (higher to lower) and repeat steps 3-5
1. Pass the set of assemblies and their paths to the CLR

Note that for an app, its probing path comes *after* the framework's, so intuitively it would appear that "framework wins" in collisions. However, because the app's deps.json is parsed *before* the framework's deps.json and because the app will likely reference an OOB package that the framework doesn't (because a framework, at least Microsoft.NETCore.App, has its own metapackage and does not reference OOB packages), the framework probing path never matches up in step 4 for the app's deps.json package\assembly entry, so it goes to the next probing path which is the app's and because the package matches the "app wins".

## Changes for 2.1+
Probe the app location before the framework's. This means flip (3) and (4) under **Probe Ordering** above and treat the app as the highest-level framework. The reason is that there may be frameworks that use OOB packages like apps, and we want to have "app wins" in non roll-forward cases.

Replace step 3 under **Algorithm** above with:
* For each probing path except frameworks that are higher-level

The reason for this change is that a given framework should never have a dependency on a higher-level framework, and is expected to find deps assets in its layer or lower. This is also required so that the given framework can find its asset, and replace the higher-level asset (see next paragraph).

Replace step 4 under **Algorithm** above with:
* If the probing path is a framework, then check its deps.json to see if it contains the exact package (by name and version). If so check if a [minor] or [major] roll-forward occurred for this framework. If true (roll-forward), then check its deps.json to see if it contains a newer version of the assembly (by Assembly Version and then File Version if necessary) compared to a previously found deps entry and use that and end probing for this entry. If false (no roll-forward), then use that location and end probing for this entry.

In order to compare Assembly Version and File Version, additional metadata will need to be written to each deps.json file. If this metadata is not present (as in the case of applications published prior to 2.1) then the assembly will be considered older and will not replace any locations that the assembly was previously found at.
