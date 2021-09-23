# Runtime IDs
The package `Microsoft.NETCore.Platforms` defines the runtime identifiers (RIDs) used by .NET packages to represent runtime-specific assets in NuGet packages.

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

### Why do I need to add a new RID?
NuGet's extensibility mechanism for platform-specific assets requires a RID be defined for any platform that needs assets specific to that platform.  Unlike TFMs, which have a known relationship in NuGet (eg net4.5 is compatible with net4.0), RIDs are opaque strings which NuGet knows nothing about.  The definition and relationship of RIDs comes solely from the `runtime.json` files within the root of the packages referenced by the project.
As such, whenever we want to put a new RID in a project.json in order to get assets specific for that RID we have to define the rid in some package.  Typically that package is `Microsoft.NETCore.Platforms` if the RID is "official".  If you'd like to prototype you can put the RID in any other package and so long as that package is referenced you can use that RID.

### Do I really need to add a new RID?
If you're prototyping on a platform that is compatible with an existing platform then you can reuse the RID for that exsisting platform.  New RIDs are only needed when an asset needs to be different on a particular platform.

`Microsoft.NETCore.Platforms` attempts to define all RIDs that packages may need, and as such will define RIDs for platforms that we don't actually cross compile for.  This is to support higher-level packages, 3rd party packages, that may need to cross-compile for that RID.

### Adding a new OS
Add a new `RuntimeGroup` item in `runtimeGroups.props`.

For example:
```xml
    <RuntimeGroup Include="myLinuxDistro">
      <Parent>linux</Parent>
      <Architectures>x86;x64;arm</Architectures>
      <Versions>42.0;43.0</Versions>
    </RuntimeGroup>
```

This will create a new RID for `myLinuxDistro` where `myLinuxDistro` should be the string used for the `ID=` value in the `/etc/os-release` file.

Whenever modifying the `runtimeGroups.props` make sure to pack the project via the `dotnet pack` command and inspect if the generated package contains the desired changes.

RuntimeGroup items have the following format:
- `Identity`: the base string for the RID, without version architecture, or qualifiers.
- `Parent`: the base string for the parent of this RID.  This RID will be imported by the baseRID, architecture-specific, and qualifier-specific RIDs (with the latter two appending appropriate architecture and qualifiers).
- `Versions`: A list of strings delimited by semi-colons that represent the versions for this RID.
- `TreatVersionsAsCompatible`: Default is true.  When true, version-specific RIDs will import the previous version-specific RID in the Versions list, with the first version importing the version-less RID.  When false all version-specific RIDs will import the version-less RID (bypassing previous version-specific RIDs)
- `OmitVersionDelimiter`: Default is false.  When true no characters will separate the base RID and version (EG: win7). When false a '.' will separate the base RID and version (EG: osx.10.12).
- `ApplyVersionsToParent`: Default is false.  When true, version-specific RIDs will import version-specific Parent RIDs similar to is done for architecture and qualifier (see Parent above).
- `Architectures`: A list of strings delimited by semi-colons that represent the architectures for this RID.
- `AdditionalQualifiers`: A list of strings delimited by semi-colons that represent the additional qualifiers for this RID.  Additional qualifers do not stack, each only applies to the qualifier-less RIDs (so as not to cause combinatorial exponential growth of RIDs).

### Adding a new version to an existing OS
Find the existing `RuntimeGroup` in `runtimeGroups.props` and add the version to the list of `Versions`, separated by a semi-colon.

If the version you are adding needs to be treated as not-compatible with previous versions and the `RuntimeGroup` has not set `TreatVersionsAsCompatible`=`false` then you may create a new `RuntimeGroup` to represent the new compatibility band.

### Checking your work
After making a change to `runtimeGroups.props` you can examine the resulting changes in `runtime.json` and `runtime.compatibility.json`.

`runtime.json` is the graph representation of the RIDs and is what ships in the package.

`runtime.compatibility.json` is a flattened version of the graph that shows the RID precedence for each RID in the graph.

### Version compatibility
Version compatibility is represented through imports.  If a platform is considered compatible with another version of the same platform, or a specific version of another platform, then it can import that platform.  This permits packages to reuse assets that were built for the imported platform on the compatible platform.  Compatibility here is a bit nebulous because inevitably different platforms will have observable differences that can cause compatibility problems.  For the purposes of RIDs we'll try to represent compatibility as versions of a platform that are explicitly advertised as being compatible with a previous version and/or another platform and don't have any known broad breaking changes.  It is usually better to opt to treat platforms as compatible since that enables the scenario of building an asset for a particular version and using that in future versions, otherwise you force people to cross-compile for all future versions the moment they target a specific version.

## Appendix : details of RID graph generation

### Naming convention
We use the following convention in all newly-defined RIDs.  Some RIDs (win7-x64, win8-x64) predate this convention and don't follow it, but all new RIDs should follow it.
`[os name].[version]-[architecture]-[additional qualifiers]`, for example `osx.10.10-x64` or `ubuntu.14.04-x64`
- `[os name]` can contain any characters other than `.`
- `[version]` can contain any characters other than `-`.  Typically a numeric version like 14.04 or 10.0.
- `[architecture]` can contain any characters other than `-`. Typically: `x86`, `x64`, `arm`, `arm64`
- `[additional qualifiers]` can be things like `aot`.  Used to further differentiate different platforms.

For all of these we strive to make them something that can be uniquely discoverable at runtime, so that a RID may be computed from an executing application.  As such these properties should be derivable from `/etc/os-release` or similar platform APIs / data.

### Import convention
Imports should be used when the added RID is considered compatible with an existing RID.

1. Architecture-specific RIDs should first import the architecture-less RID.  EG: `osx.10.11-x64` should first import `osx.10.11`.
2. Architecture-specific RIDs that are compatible with a previous version RID for the same OS should then import the previous version, architecture specific RID.  EG: `osx.10.11-x64` should then import `osx.10.10-x64`.  If there is no earlier compatible/supported version, then a versionless RID should be imported.  EG: `osx.10.10-x64` should import `osx-x64`.
3. Architecture-less RIDs that are compatible with a previous version RID for the same OS should then import the previous version, architecture neutral RID.  EG: `osx.10.11` should import `osx.10.10`. If there is no earlier compatible/supported version, then a versionless RID should be imported.  EG: `osx.10.10` should import `osx`.
4. Version-less RIDs should import an OS category.  EG: `osx-x64` should import `unix-x64`, `osx` should import `unix`.

### Advanced RuntimeGroup metadata
The following options can be used under special circumstances but break the normal precedence rules we try to establish by generating the RID graph from common logic. These options make it possible to create a RID fallback chain that doesn't  match the rest of the RIDs and therefore is hard for developers/package authors to reason about.  Only use these options for cases where you know what you are doing and have carefully reviewed the resulting RID fallbacks using the CompatibliltyMap.

- `OmitRIDs`: A list of strings delimited by semi-colons that represent RIDs calculated from this RuntimeGroup that should be omitted from the RuntimeGraph.  These RIDs will not be referenced nor defined.
- `OmitRIDDefinitions`: A list of strings delimited by semi-colons that represent RIDs calculated from this RuntimeGroup that should be omitted from the RuntimeGraph.  These RIDs will not be defined by this RuntimeGroup, but will be referenced: useful in case some other RuntimeGroup (or runtime.json template) defines them.
- `OmitRIDReferences`: A list of strings delimited by semi-colons that represent RIDs calculated from this RuntimeGroup that should be omitted from the RuntimeGraph.  These RIDs will be defined but not referenced by this RuntimeGroup.
