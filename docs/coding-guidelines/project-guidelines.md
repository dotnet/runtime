# Build Project Guidelines
In order to work in the dotnet/runtime repo you must first run build.cmd/sh from the root of the repo at least once before you can iterate and work on a given library project.

## Behind the scenes with build.cmd/sh

- Restore tools
- Restore external dependencies
 - CoreCLR - Copy to `bin\runtime\$(BuildTargetFramework)-$(TargetOS)-$(Configuration)-$(TargetArchitecture)`
- Build targeting pack
 - Build src\libraries\ref.proj which builds all references assembly projects. For reference assembly project information see [ref](#ref)
- Build product
 - Build src\libraries\src.proj which builds all the source library projects. For source library project information see [src](#src).

# Build Pivots
Below is a list of all the various options we pivot the project builds on:

- **Target Frameworks:** .NETFramework, .NETStandard, .NETCoreApp
- **Platform Runtimes:** .NETFramework (aka CLR/Desktop), CoreCLR, Mono
- **OS:** windows, Linux, OSX, FreeBSD, AnyOS
- **Flavor:** Debug, Release

## Individual build properties
The following are the properties associated with each build pivot

- `$(BuildTargetFramework) -> Any .NETCoreApp or .NETFramework TFM, e.g. net5.0`
- `$(TargetOS) -> Windows | Linux | OSX | FreeBSD | [defaults to running OS when empty]`
- `$(Configuration) -> Release | [defaults to Debug when empty]`
- `$(TargetArchitecture) - x86 | x64 | arm | arm64 | [defaults to x64 when empty]`
- `$(RuntimeOS) - win7 | osx10.10 | ubuntu.14.04 | [any other RID OS+version] | [defaults to running OS when empty]` See [RIDs](https://github.com/dotnet/runtime/tree/main/src/libraries/Microsoft.NETCore.Platforms) for more info.

## Aggregate build properties
Each project will define a set of supported TargetFrameworks

```
<PropertyGroup>
  <TargetFrameworks>[TargetFramework];[TargetFramework];...</TargetFrameworks>
<PropertyGroup>
```

- `$(BuildSettings) -> $(BuildTargetFramework)[-$(TargetOS)][-$(Configuration)][-$(TargetArchitecture)]`
 - Note this property should be file path safe and thus can be used in file names or directories that need to a unique path for a project configuration.
 - The only required Build Settings value is the `$(BuildTargetFramework)` the others are optional.

Example:
Pure netstandard configuration:
```
<PropertyGroup>
  <TargetFrameworks>netstandard2.0</TargetFrameworks>
<PropertyGroup>
```

All supported targets with unique windows/unix build for netcoreapp:
```
<PropertyGroup>
  <TargetFrameworks>$(NetCoreAppCurrent)-windows;$(NetCoreAppCurrent)-Unix;net461-windows</TargetFrameworks>
<PropertyGroup>
```

## Options for building

A full or individual project build is centered around BuildTargetFramework, TargetOS, Configuration and TargetArchitecture.

1. `$(BuildTargetFramework), $(TargetOS), $(Configuration), $(TargetArchitecture)` can individually be passed in to change the default values.
2. If nothing is passed to the build then we will default value of these properties from the environment. Example: `net5.0-[TargetOS Running On]-Debug-x64`.
3. While Building an individual project from the VS, we build the project for all latest netcoreapp target frameworks.

We also have `RuntimeOS` which can be passed to customize the specific OS and version needed for native package builds as well as package restoration. If not passed it will default based on the OS you are running on.

Any of the mentioned properties can be set via `/p:<Property>=<Value>` at the command line. When building using our run tool or any of the wrapper scripts around it (i.e. build.cmd) a number of these properties have aliases which make them easier to pass (run build.cmd/sh -? for the aliases).

## Selecting the correct BuildSettings
When building an individual project the `BuildTargetFramework` and `TargetOS` will be used to select the closest matching TargetFramework listed in the projects `TargetFrameworks` property. The rules used to select the targetFramework will consider compatible target frameworks and OS fallbacks.

## Supported full build settings
- .NET Core latest on current OS (default) -> `$(NetCoreAppCurrent)-[RunningOS]`
- .NET Framework latest -> `net48-windows`

# Library project guidelines

## TargetFramework conditions
`TargetFramework` conditions should be avoided in the first PropertyGroup as that causes DesignTimeBuild issues: https://github.com/dotnet/project-system/issues/6143

1. Use an equality check if the TargetFramework isn't overloaded with the OS portion.
Example:
```
<PropertyGroup>
  <TargetFrameworks>netstandard2.0;netstandard2.1</TargetFrameworks>
</PropertyGroup>
<ItemGroup Condition="'$(TargetFramework)' == 'netstandard2.0'">...</ItemGroup>
```
2. Use a StartsWith when you want to test for multiple .NETStandard or .NETFramework versions.
Example:
```
<PropertyGroup>
  <TargetFrameworks>netstandard2.0;netstandard2.1</TargetFrameworks>
</PropertyGroup>
<ItemGroup Condition="$(TargetFramework.StartsWith('netstandard'))>...</ItemGroup>
```
3. Use a StartsWith if the TargetFramework is overloaded with the OS portion.
Example:
```
<PropertyGroup>
  <TargetFrameworks>netstandard2.0-windows;netstandard2.0-Unix</TargetFrameworks>
</PropertyGroup>
<ItemGroup Condition="$(TargetFramework.StartsWith('netstandard2.0'))>...</ItemGroup>
```
4. Use negations if that makes the conditions easier.
Example:
```
<PropertyGroup>
  <TargetFrameworks>netstandard2.0;net461;net472;net5.0</TargetFrameworks>
</PropertyGroup>
<ItemGroup Condition="!$(TargetFramework.StartsWith('net4'))>...</ItemGroup>
<ItemGroup Condition="'$(TargetFramework)' != 'netstandard2.0'">...</ItemGroup>
```

## Directory layout

Library projects should use the following directory layout.

```
src\<Library Name>\src - Contains the source code for the library.
src\<Library Name>\ref - Contains any reference assembly projects for the library.
src\<Library Name>\pkg - Contains package projects for the library.
src\<Library Name>\tests - Contains the test code for a library.
src\<Library Name>\gen - Contains source code for the assembly's source generator.
```

## ref
Reference assemblies are required for any library that has more than one implementation or uses a facade. A reference assembly is a surface-area-only assembly that represents the public API of the library. To generate a reference assembly source file you can use the [GenAPI tool](https://www.nuget.org/packages/Microsoft.DotNet.BuildTools.GenAPI). If a library is a pure portable library with a single implementation it need not use a reference assembly at all. Instructions on updating reference sources can be found [here](https://github.com/dotnet/runtime/blob/main/docs/coding-guidelines/updating-ref-source.md).

In the ref directory for the library there should be at most **one** `.csproj` that contains the latest API for the reference assembly for the library. That project can contain multiple entries in its `TargetFrameworks` property. Ref projects should use `<ProjectReference>` for its dependencies.

### ref output
All ref outputs should be under

`bin\$(MSBuildProjectName)\ref\$(TargetFramework)`

## src
In the src directory for a library there should be only **one** `.csproj` file that contains any information necessary to build the library in various target frameworks. All supported target frameworks should be listed in the `TargetFrameworks` property.

All libraries should use `<Reference Include="..." />` for all their references to libraries that compose the shared framework of the current .NETCoreApp. That will cause them to be resolved against the locally built targeting pack which is located at `artifacts\bin\microsoft.netcore.app.ref`. The only exception to that rule right now is for partial facades which directly reference System.Private.CoreLib and thus need to directly reference other partial facades to avoid type conflicts.

Other target frameworks than .NETCoreApp latest (i.e. `netstandard2.0`, `net461`, `netcoreapp3.0`) should use ProjectReference items to reference dependencies.

### src\ILLink
Contains the files used to direct the trimming tool. See [ILLink files](../workflow/trimming/ILLink-files.md).

### src output
All src outputs are under

`bin\$(MSBuildProjectName)\$(TargetFramework)`

## pkg
In the pkg directory for the library there should be only **one** `.pkgproj` for the primary package for the library. If the library has platform-specific implementations those should be split into platform specific projects in a subfolder for each platform. (see [Package projects](./package-projects.md))

## tests
Similar to the src projects tests projects will define a `TargetFrameworks` property so they can list out the set of target frameworks they support.

Tests don't need to reference default references which are part of the targeting packs (i.e. `mscorlib` on .NETFramework or `System.Runtime` on .NETCoreApp). Everything on top of targeting packs should be referenced via ProjectReference items for live built assets.

### tests output
All test outputs should be under

`bin\$(MSBuildProjectName)\$(TargetFramework)`

## gen
In the gen directory any source generator related to the assembly should exist. This does not mean the source generator is only used for that assembly only that it is conceptually apart of that assembly. For example, the assembly may provide attributes or low-level types the source generator uses.

## Facades
Facade are unique in that they don't have any code and instead are generated by finding a contract reference assembly with the matching identity and generating type forwards for all the types to where they live in the implementation assemblies (aka facade seeds). There are also partial facades which contain some type forwards as well as some code definitions. All the various build configurations should be contained in the one csproj file per library.

TODO: Fill in more information about the required properties for creating a facade project.

# Conventions for forked code
While our goal is to have the exact code for every configuration there is always reasons why that is not realistic so we need to have a set of conventions for dealing with places where we fork code. In order of preference, here are the strategies we employ:

1. Using different code files with partial classes to implement individual methods different on different configurations
2. Using entirely different code files for cases were the entire class (or perhaps static class) needs to be unique in a given configuration.
3. Using `#ifdef`'s directly in a shared code file.

In general we prefer different code files over `#ifdef`'s because it forces us to better factor the code which leads to easier maintenance over time.

## Code file naming conventions
Each source file should use the following guidelines
- The source code file should contain only one class. The only exception is small supporting structs, enums, nested classes, or delegates that only apply to the class can also be contained in the source file.
- The source code file should be named `<class>.cs` and should be placed in a directory structure that matches its namespace relative to its project directory. Ex. `System\IO\Stream.cs`
- Larger nested classes should be factored out into their own source files using a partial class and the file name should be `<class>.<nested class>.cs`.
- Classes that are forked based on BuildSettings should have file names `<class>.<BuildSettings>.cs`.
 - Where `<BuildSettings>` is one of `$(TargetOS)`, `$(TargetFramework)`, `$(Configuration)`, or `$(Platform)`, matching exactly by case to ensure consistency.
- Classes that are forked based on a feature set should have file names `<class>.<feature>.cs`.
 - Where `<feature>` is the name of something that causes a fork in code that isn't a single configuration. Examples:
  - `.CoreCLR.cs` - implementation specific to CoreCLR runtime
  - `.Win32.cs` - implementation based on [Win32](https://en.wikipedia.org/wiki/Windows_API)

## Define naming convention

As mentioned in [Conventions for forked code](#conventions-for-forked-code) `#ifdef`ing the code is the last resort as it makes code harder to maintain overtime. If we do need to use `#ifdef`'s we should use the following conventions:
- Defines based on conventions should be one of `$(TargetOS)`, `$(TargetFramework)`, `$(Configuration)`, or `$(Platform)`, matching exactly by case to ensure consistency.
 - Examples: `<DefineConstants>$(DefineConstants);net46</DefineConstants>`
- Defines based on convention should match the pattern `FEATURE_<feature name>`. These can unique to a given library project or potentially shared (via name) across multiple projects.
