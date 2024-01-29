# Runtime IDs
The `Microsoft.NETCore.Platforms` transport package contains the portable and non-portable runtime identifier graph files for redistribution in the dotnet/sdk repository.

## What is a RID?
A RID is an opaque string that identifies a platform.  RIDs have relationships to other RIDs by "importing" the other RID.  In that way a RID is a directed graph of compatible RIDs.

## How does NuGet use RIDs?
When NuGet is deciding which assets to use from a package and which packages to include NuGet will consider a RID if the project.json lists a RID in its `runtimes` section.

- NuGet chooses the best RID-specific asset, where best is determined by a breadth first traversal of the RID graph.  Breadth ordering is document order.
- NuGet considers RID-specific assets for two asset types: lib and native.
- NuGet never considers RID-specific assets for compile.

### Best RID
Consider the partial RID-graph:
```
        "any": {},

        "win": {
            "#import": [ "any" ]
        },
        "win-x86": {
            "#import": [ "win" ]
        },
        "win-x64": {
            "#import": [ "win" ]
        },
        "win7": {
            "#import": [ "win" ]
        },
        "win7-x86": {
            "#import": [ "win7", "win-x86" ]
        },
        "win7-x64": {
            "#import": [ "win7", "win-x64" ]
        }
```

This can be visualized as a directed graph, as follows:
```
    win7-x64    win7-x86
       |   \   /    |
       |   win7     |
       |     |      |
    win-x64  |  win-x86
          \  |  /
            win
             |
            any
```
As such, best RID, when evaluating for win7-x64 would be:`win7-x64`, `win7`, `win-x64`, `win`, `any`
Similarly, when evaluating for `win-x64`: `win-x64`, `win`, `any`
Note that `win7` comes before `win-x64` due to the import for `win7` appearing before the import for `win-x64` in document order.

### RID-qualified assets are preferred
NuGet will always prefer a RID-qualified asset over a RID-less asset.  For example if a package contains
```
lib/netcoreapp1.0/foo.dll
runtimes/win/lib/netcoreapp1.0/foo.dll
```
When resolving for netstandard1.0/win7-x64 NuGet will choose `runtimes/win/lib/netcoreapp1.0/foo.dll`.

Additionally, NuGet will always prefer a RID-qualified asset over a RID-less asset, even if the framework is less specific for the RID-qualified asset.
```
lib/netstandard1.5/foo.dll
runtimes/win/lib/netstandard1.0/foo.dll
```
When resolving for netstandard1.5/win7-x64 NuGet will choose `runtimes/win/lib/netstandard1.0/foo.dll` over `lib/netstandard1.5/foo.dll` even though `netstandard1.5` is more specific than `netstandard1.0`.

### RID-qualified assets are never used for compile
NuGet will select different compile-assets than runtime-assets.  The compile assets can never be RID-qualified.  Consider the package:
```
lib/netstandard1.5/foo.dll
runtimes/win/lib/netstandard1.0/foo.dll
```
When resolving for netstandard1.5/win7-x64 will select `lib/netstandard1.5/foo.dll` for the compile asset and `runtimes/win/lib/netstandard1.0/foo.dll` for the runtime asset.

## Adding new RIDs
The RID graphs should be only updated with new base OSes, architectures, or C standard libraries. The RID graphs shouldn't be updated with new OS flavor- and version-specific RIDs anymore. Build from source automatically adds the non-portable distro RID encoded via the `OutputRID` property into the RID graph which allows build tools to target that RID.