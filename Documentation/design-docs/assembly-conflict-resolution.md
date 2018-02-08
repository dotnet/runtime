# Assembly Conflict Resolution

## Summary
This document describes current and proposed behavior for dealing with references to assemblies that exist physically in more than one location including the "app" location and "framework" location(s). It proposes moving from "app wins" to "framework wins" during a [minor] or [major] roll-forward when the framework has a newer version of the given assembly.

## Current behavior

When a CoreCLR app is being launched, it builds a list of "probing" locations and then iterates through each <*layer*'s'>.app.deps.json file. For each assembly entry in that file, it uses the probing locations in order to determine the physical location of the assembly. Once all deps.json files are parsed, that information (which consists of a list of full paths to assemblies) is passed to the CLR which it uses to load each assembly when it is accessed.

A *layer* consists of the app at the highest layer, then then a chain of one or more framework layers ending with Microsoft.NETCore.App as the lowest layer. For 2.0 and earlier, there were only the app and Microsoft.NETCore.App layers. For 2.1+, those two layers exist plus additional, optional frameworks in-between those two.

Since each layer (and other probing locations) can contain the same assembly by name, the "host policy" determines the semantics for conflict cases where the same assembly exists more than once. The "host policy" logic lives in hostpolicy.dll located in the Microsoft.NETCore.App's folder.

The probing locations consists of:
a) Serving Location
b) Shared Store
c) Framework directory(s) from higher to lower
d) App directory
e) Additional locations specified in runtimeconfig.dev.json file

For example, here's the probing order on Windows when running a non-published folder (in order to get additional locations from the app.runtimeconfig.dev.json file)
+		C:\\Program Files (x86)\\coreservicing\\x64
+		C:\\Program Files (x86)\\coreservicing\\pkgs
+		C:\\Program files\\dotnet\\shared\\Microsoft.NETCore.App\\2.0.0
+		(app location)
+		C:\\Users\\<user>\\.nuget\\packages
+		C:\\Program Files (x86)\\Microsoft SDKs\\NuGetPackagesFallback
+		C:\\Program Files\\dotnet\\sdk\\NuGetFallbackFolder

The order in which each layer's deps.json is processed is:
+   The application
+   Deps file specified by optional --additional-deps
+   The framework(s) from higher to lower

So the algorithm:
1) Determine the probing paths
2) Read the app's deps.json
3)   For each package entry, loop through the assemblies
4)     For each assembly, loop through the probing paths
5)       If the probing path is a framework, then check its deps.json to see if it contains the exact package (by name and version). If so, then use the framework's location
6)       If the probing path is not a framework, then use that location
7) Read the additional deps from --additional-deps and repeat steps 3-6
7) Read each framework's deps.json and repeat steps 3-6
8) Pass the set of assemblies and their paths to the CLR

Note that for an app, its probing path comes *after* the framework, so intuitively it would appear that "framework wins" in collisions. However, because the app's deps.json is parsed *before* the framework and because the app will likely reference an OOB package that the framework doesn't (because a framework, at least Microsoft.NETCore.App, has its own metapackage and does not reference OOB packages), the framework probing path never matches up in step 5 for the app's deps.json package\assembly entry, so it goes onto the next probing path which is the app's. Thus "app wins".

## Proposed changes
Probe the app location before the framework's. This means flip (c) and (d) above and treat the app as the highest-level framework. The reason is that there may be frameworks that use OOB packages like apps, and we want to have "app wins" in such cases.

Replace step 5 above with:
5a)       If the probing path is a framework, and no [minor] or [major] roll-forward occurred on that framework, then check its deps.json to see if it contains the exact package (by name and version). If so, then use the framework's location
5b)       If the probing path is a framework, and a [minor] or [major] roll-forward occurred on that framework, then check its deps.json to see if it contains a newer version of the assembly (by File Version and \ or Assembly Version). If so, then use the framework's location
