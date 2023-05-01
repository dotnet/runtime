# Build Project Guidelines
In order to work in the dotnet/runtime repo you must first run build.cmd/sh from the root of the repo at least once before you can iterate and work on a given library project.

## Behind the scenes with build.cmd/sh

- Restore tools
- Restore external dependencies
 - CoreCLR - Copy to `bin\runtime\$(BuildTargetFramework)-$(TargetOS)-$(Configuration)-$(TargetArchitecture)`
- Build shared framework projects
 - Build src\libraries\sfx.proj which builds all shared framework projects.
- Build out of band projects
 - Build src\libraries\oob.proj which builds all the out-of-band (OOB) projects.

For reference assembly project information see [ref](#ref)
For source library project information see [src](#src)

# Build Pivots
Below is a list of all the various options we pivot the project builds on:

- **Target Frameworks:** .NETFramework, .NETStandard, .NETCoreApp
- **Platform Runtimes:** .NETFramework (aka CLR/Desktop), CoreCLR, Mono
- **OS:** windows, linux, osx, freebsd, AnyOS
- **Flavor:** Debug, Release

## Individual build properties
The following are the properties associated with each build pivot

- `$(BuildTargetFramework) -> Any .NETCoreApp or .NETFramework TFM, e.g. net8.0`
- `$(TargetOS) -> windows | linux | osx | freebsd | ... | [defaults to running OS when empty]`
- `$(Configuration) -> Debug | Release | [defaults to Debug when empty]`
- `$(TargetArchitecture) - x86 | x64 | arm | arm64 | [defaults to x64 when empty]`

## Aggregate build properties
Each project will define a set of supported TargetFrameworks

```
<PropertyGroup>
  <TargetFrameworks>[TargetFramework];[TargetFramework];...</TargetFrameworks>
<PropertyGroup>
```

Example:
Non cross-targeting project that targets .NETStandard:
```
<PropertyGroup>
  <TargetFramework>netstandard2.0</TargetFramework>
<PropertyGroup>
```

A cross-targeting project which targets specific platform with `$(NetCoreAppCurrent)` and one .NETFramework tfm:
```
<PropertyGroup>
  <TargetFrameworks>$(NetCoreAppCurrent)-windows;$(NetCoreAppCurrent)-unix;$(NetFrameworkMinimum)</TargetFrameworks>
<PropertyGroup>
```

## Options for building

A full or individual project build is centered around BuildTargetFramework, TargetOS, Configuration and TargetArchitecture.

1. `$(BuildTargetFramework), $(TargetOS), $(Configuration), $(TargetArchitecture)` can individually be passed in to change the default values.
2. If nothing is passed to the build then we will default value of these properties from the environment. Example: `net8.0-[TargetOS Running On]-Debug-x64`.
3. When building an individual project (either from the CLI or an IDE), all target frameworks are built.

Any of the mentioned properties can be set via `/p:<Property>=<Value>` at the command line. When building using any of the wrapper scripts around it (i.e. build.cmd) a number of these properties have aliases which make them easier to pass (run build.cmd/sh -? for the aliases).

## Selecting the correct BuildSettings
When building an individual project the `BuildTargetFramework` and `TargetOS` will be used to select the compatible dependencies which are expressed as ProjectReference items.

## Supported full build settings
- .NET Core latest on current OS (default) -> `$(NetCoreAppCurrent)-[RunningOS]`
- .NET Framework latest -> `net48`

# Library project guidelines

## TargetFramework conditions
`TargetFramework` conditions should be avoided in the first PropertyGroup as that causes DesignTimeBuild issues: https://github.com/dotnet/project-system/issues/6143

1. Use TargetFrameworkIdentifier to condition on an entire framework to differentiate between .NETCoreApp, .NETStandard and .NETFramework.
Example:
```
<PropertyGroup>
  <TargetFrameworks>$(NetCoreAppCurrent);netstandard2.0;$(NetFrameworkMinimum)</TargetFrameworks>
</PropertyGroup>
<ItemGroup Condition="'$(TargetFrameworkIdentifier)' == '.NETCoreApp'">...</ItemGroup>
<ItemGroup Condition="'$(TargetFrameworkIdentifier)' == '.NETStandard'">...</ItemGroup>
<ItemGroup Condition="'$(TargetFrameworkIdentifier)' == '.NETFramework'">...</ItemGroup>
```
2. Use equality checks if you want to condition on specific runtime agnostic target frameworks (i.e. without the `-windows` suffix).
Example:
```
<PropertyGroup>
  <TargetFrameworks>$(NetCoreAppCurrent);netstandard2.0;$(NetFrameworkMinimum)</TargetFrameworks>
</PropertyGroup>
<ItemGroup Condition="'$(TargetFramework)' == '$(NetCoreAppCurrent)'">...</ItemGroup>
<ItemGroup Condition="'$(TargetFramework)' == 'netstandard2.0'">...</ItemGroup>
<ItemGroup Condition="'$(TargetFramework)' == '$(NetFrameworkMinimum)'">...</ItemGroup>
```
3. Use the `TargetPlatformIdentifier` property to condition on a .NETCoreApp platform specific target framework. Note that .NETStandard and .NETFramework target frameworks can't be platform specific.
Example:
```
<PropertyGroup>
  <TargetFrameworks>$(NetCoreAppCurrent)-windows;$(NetCoreAppCurrent)-osx;$(NetCoreAppCurrent)</TargetFrameworks>
</PropertyGroup>
<ItemGroup Condition="'$(TargetPlatformIdentifier)' == 'windows'">...</ItemGroup>
<ItemGroup Condition="'$(TargetPlatformIdentifier)' == 'osx'">...</ItemGroup>
```
Important: In contrast to the old `Targets*` checks, `TargetPlatformIdentifier` conditions apply to a single tfm only, inheritance between target frameworks can't be expressed. See the example below for Unix:
```
<PropertyGroup>
  <TargetFrameworks>$(NetCoreAppCurrent)-unix;$(NetCoreAppCurrent)-linux;$(NetCoreAppCurrent)-android;$(NetCoreAppCurrent)-windows</TargetFrameworks>
</PropertyGroup>
<ItemGroup Condition="'$(TargetPlatformIdentifier)' == 'unix' or '$(TargetPlatformIdentifier)' == 'linux' or '$(TargetPlatformIdentifier)' == 'android'">...</ItemGroup>
<!-- Negations make such conditions easier to write and read. -->
<ItemGroup Condition="'$(TargetPlatformIdentifier)' != 'windows'">...</ItemGroup>
```
4. Set the `TargetPlatformIdentifier` property in the project to be able to condition on it in properties in the project file.
That is necessary as the SDK sets the `TargetPlatformIdentifier` in a .targets file after the project is evaluated. Because of that, the property isn't available during the project's evaluation and must be set manually.
Example:
```
<PropertyGroup>
  <TargetFrameworks>$(NetCoreAppCurrent)-windows;$(NetCoreAppCurrent)-android</TargetFrameworks>
</PropertyGroup>
<!-- DesignTimeBuild requires all the TargetFramework Derived Properties to not be present in the first property group. -->
<PropertyGroup>
  <TargetPlatformIdentifier>$([MSBuild]::GetTargetPlatformIdentifier('$(TargetFramework)'))</TargetPlatformIdentifier>
  <DefineConstants Condition="'$(TargetPlatformIdentifier)' == 'android'">$(DefineConstants);ANDROID_USE_BUFFER</DefineConstants>
</PropertyGroup>
```
5. Use negations if that makes the conditions easier.
Example:
```
<PropertyGroup>
  <TargetFrameworks>$(NetCoreAppCurrent);netstandard2.0;net462</TargetFrameworks>
</PropertyGroup>
<ItemGroup Condition="'$(TargetFrameworkIdentifier)' != '.NETFramework'">...</ItemGroup>
<ItemGroup Condition="'$(TargetFramework)' != 'netstandard2.0'">...</ItemGroup>
```

## Directory layout

Library projects should use the following directory layout.

```
src\<Library Name>\gen - Contains source code for the assembly's source generator.
src\<Library Name>\ref - Contains any reference assembly projects for the library.
src\<Library Name>\src - Contains the source code for the library.
src\<Library Name>\tests - Contains the test code for a library.
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

Other target frameworks than .NETCoreApp latest (i.e. `netstandard2.0`, `net462`, `net6.0`) should use ProjectReference items to reference dependencies.

### src\ILLink
Contains the files used to direct the trimming tool. See [ILLink files](../workflow/trimming/ILLink-files.md).

### src output
All src outputs are under

`artifacts\bin\$(MSBuildProjectName)\$(TargetFramework)`

## tests
Similar to the src projects tests projects will define a `TargetFrameworks` property so they can list out the set of target frameworks they support.

Tests don't need to reference default references which are part of the targeting packs (i.e. `mscorlib` on .NETFramework or `System.Runtime` on .NETCoreApp). Everything on top of targeting packs should be referenced via ProjectReference items for live built assets.

### tests output
All test outputs should be under

`bin\$(MSBuildProjectName)\$(TargetFramework)`

## gen
In the gen directory any source generator related to the assembly should exist. This does not mean the source generator is only used for that assembly only that it is conceptually apart of that assembly. For example, the assembly may provide attributes or low-level types the source generator uses.
To consume a source generator, simply add a `<ProjectReference Include="..." ReferenceOutputAssembly="false" OutputItemType="Analyzer" />` item to the project, usually next to the `Reference` and `ProjectReference` items.

A source generator must target `netstandard2.0` as such assemblies are loaded into the compiler's process which might run on either .NET Framework or modern .NET depending on the tooling being used (CLI vs Visual Studio). While that's true, a source project can still multi-target and include `$(NetCoreAppToolCurrent)` (which is the latest non live-built .NETCoreApp tfm that is supported by the SDK) to benefit from the ehancanced nullable reference type warnings emitted by the compiler. For an example see [System.Text.Json's roslyn4.4 source generator](/src/libraries/System.Text.Json/gen/System.Text.Json.SourceGeneration.Roslyn4.4.csproj). While the repository's infrastructure makes sure that only the source generator's `netstandard2.0` build output is included in packages, to consume such a multi-targeting source generator via a `ProjectReference` (as described above), you need to add the `SetTargetFramework="TargetFramework=netstandard2.0"` metadata to the ProjectReference item to guarantee that the netstandard2.0 asset is chosen.

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
