# Building a Vertical Implementation Details #

**Definitions**

*VerticalTargetGroup*

`VerticalTargetGroup` - We need a property to define the vertical target group, but we don't want to set "TargetGroup" explicitly or we won't be able to build the "" TargetGroups for projects.
  If `VerticalTargetGroup != ""`, we import buildvertical.targets which will contain our additional targets.

*SupportedGroups*

For each ref project and src project, we define `SupportedGroups`. `SupportedGroups` is a tuple for the supported `TargetGroups` and `OSGroups`.


ie

ref\System.Runtime.csproj
```MSBuild
<PropertyGroup>
  <SupportedGroups>
    netstandard1.7_Windows_NT;
    netstandard1.7_OSX;
    netstandard1.7_Linux;
    netcoreapp1.1_Windows_NT;
    netcoreapp1.1_OSX;
    netcoreapp1.1_Linux
  </SupportedGroups>
<PropertyGroup>
```

*Contract Layer*

We have a contract layer (msbuild task).

Inputs:

        SupportedGroups
        VerticalTargetGroup
        OSGroup
Output:

        VerticalTargets (ItemTask)
            metadata: TargetGroup
                      OSGroup

Given the supported target and OS groups, and the desired vertical target and OS groups, return the closest supported group or empty metadata items.
How should we handle determining the target / os groups, fallback groups, etc...?  The simplest solution is to use the NuGet api's for targets.  We can use platforms\runtime.json for os groups, or try to use the already existent os group filtering instead of adding it to the contract layer.

Options:

1. Use NuGet API's

2. Make use of inormation we already have and develop our own resolution algorithm.

The current plan is to use the NuGet API's.  We know that there is an intrinsic problem with the NuGet API's, in that we (CoreFx) define the targets (tfm's), but NuGet contains the data / logic, so anytime we want to create a new tfm, we have to go make an update to NuGet.  This is an existent problem.  For now, it is much simpler to utilize NuGet instead of deriving a second solution.  When we break free of the NuGet dependency and wholly define our tfm graph, then we should utilize that solution for this work.

**Building a vertical implementation steps**

1 - Include all projects, we don't need to build the .builds files for each library, because we only want to build each project at most once for a given vertical.

```MSBuild
<ItemGroup>
  <Project Include="**\ref\*proj" />
  <Project Include="**\src\*proj" />
</ItemGroup>
```

2 - Iterate all projects through the contract layer, removing (and logging) any projects which return null metadata (not supported).

3 - Build `OutputPath` is set to drop all binaries into a single folder

Current standard `OutputPath`

```MSBuild
<OutputPath Condition="'$(OutputPath)'==''">$(BaseOutputPath)$(OSPlatformConfig)/$(MSBuildProjectName)/$(TargetOutputRelPath)$(OutputPathSubfolder)</OutputPath>
```
Example: E:\gh\chcosta\corefx\bin/AnyOS.AnyCPU.Debug/System.Buffers/netcoreapp1.1/

Proposed vertical `OutputPath`

```MSBuild
<OutputPath Condition="'$(OutputPath)'==''">$(BinDir)/$(VerticalTargetGroup)/$(OSPlatformConfig)</OutputPath>
```
Example: E:\gh\chcosta\corefx\bin/netcoreapp1.7/AnyOS.AnyCPU.Debug

Traditionally, the output path contains the `TargetGroup` as a part of the path.  The flat structure means we don't have to play games with the `TargetPath` to figure out when, for example, "System.Buffers" ("netstandard1.1") is trying to find the "System.Runtime" reference ("netstandard1.7"), that there is no path for "System.Runtime.dll" containing the "netstandard1.1" target group.

4 - Build all reference assemblies.  The reference assembly projects, which were not trimmed in step 2, are all built.  TBD, should we again use the contract layer during the build to determine the targets for the project, or should we capture that as metadata for the project in step 2?

5 - Build all src assemblies into the "OutputPath". The src assembly projects, which were not trimmed in step 2. are all built.

6 - build packages, TBD

**Building a library**

In addition to the ability to build an entire vertical, we require the ability to build a single library.  This, single library build should utilize context to determine TargetGroup and OSGroup.  ie, If a vertical build completes, and you want to build an individual library, it should use the group values from the vertical build unless you specify otherwise.  If you specify otherwise, then those settings become the new settings.  If no context is available, then the library should be built with a set of commond default values.

When building an individual library, or project, its P2P references must be queried to determine supported configurations for building that refernce and then the best configuration must be chosen.

**Additional issues**

- building specific folders (filter by partition)?

- building / running tests for a vertical

  - building tests against packages

- Official builds?

- CI testing?

- Validation

  - Is it an error condition if any library does not contribute to the latest standard vertical?

  - Is it an error condition if a library does not contribute to any OS group? probably


