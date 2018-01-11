# Build Project Guidelines
In order to work in corefx repo you must first run build.cmd/sh from the root of the repo at least
once before you can iterate and work on a given library project.

## Behind the scenes with build.cmd/sh

- Setup tools (currently done in init-tools but will later be a boot-strap script in run.cmd/sh)
- Restore external dependencies
 - CoreCLR - Copy to `bin\runtime\$(BuildConfiguration)`
 - Netstandard Library - Copy to `bin\ref\netstandard`
 - UAP - Copy to `bin\runtime\$(BuildConfiguration)`
 - NetFx targeting pack - Copy to `bin\ref\netfx`
- Build targeting pack
 - Build src\ref.builds which builds all references assembly projects. For reference assembly project information see [ref](#ref)
- Build product
 - Build src\src.builds which builds all the source library projects. For source library project information see [src](#src).
- Sign product
 - Build src\sign.builds
//**CONSIDER**: We should make this as part of the src.builds file instead of a separate .builds file.

## Behind the scenes with build-test.cmd/sh
- build-test.cmd cannot be ran successfully until build.cmd has been ran at least once for a `BuildConfiguration`.
- Build src\tests.builds which builds all applicable test projects. For test project information see [tests](#tests).
- The build pass will happen twice. Once for the specific `$(BuildConfiguration)` and once for netstandard. That way we run both sets of applicable tests against for the given `$(BuildConfiguration)`.
- TODO: Currently as part of src/post.builds we call CloudBuild.targets which sets up our test runs. This needs to be moved to be part of build-test.cmd now.

## Behind the scenes with build-packages.cmd/sh
- build-packages.cmd cannot be run successfully until build.cmd has been ran at least once for a BuildConfiguration.
- Build src\packages.builds which will build only the packages it has the context to build which will generally be only the ones for the given `BuildConfiguration`. If a package requires assets from multiple `BuildConfigurations` it will require that all `BuildConfigurations` are built first.

# Build Pivots
Below is a list of all the various options we pivot the project builds on:

- **Target Frameworks:** NetFx (aka Desktop), netstandard (aka dotnet/Portable), NETCoreApp (aka .NET Core), UAP (aka UWP/Store/netcore50)
- **Platform Runtimes:** NetFx (aka CLR/Desktop), CoreCLR, CoreRT (aka NetNative/AOT/MRT)
- **OS:** Windows_NT, Linux, OSX, FreeBSD, AnyOS
- **Flavor:** Debug, Release
- **Architecture:** x86, x64, arm, arm64, AnyCPU

## Individual build properties
The following are the properties associated with each build pivot

- `$(TargetGroup) -> netstandard | netcoreapp | netcoreappcorert | netfx | uap | uapaot`
//**CONSIDER**: naming netcoreappcorert something shorter maybe just corert.
- `$(OSGroup) -> Windows | Linux | OSX | FreeBSD | [defaults to running OS when empty]`
- `$(ConfigurationGroup) -> Release | [defaults to Debug when empty]`
- `$(ArchGroup) - x86 | x64 | arm | arm64 | [defaults to x64 when empty]`
- `$(RuntimeOS) - win7 | osx10.10 | ubuntu.14.04 | [any other RID OS+version] | [defaults to runnning OS when empty]` See [RIDs](https://github.com/dotnet/corefx/tree/master/pkg/Microsoft.NETCore.Platforms) for more info.

For more information on various targets see also [.NET Standard](https://github.com/dotnet/standard/blob/master/docs/versions.md)

## Aggregate build properties
Each project will define a set of supported build configurations

```
<PropertyGroup>
  <BuildConfigurations>
    [BuildConfiguration1];
    [BuildConfiguration2];
    ...
  </BuildConfigurations>
<PropertyGroup>
```

- `$(BuildConfiguration) -> $(TargetGroup)[-$(OSGroup)][-$(ConfigurationGroup)][-$(ArchGroup)]`
 - Note this property should be file path safe and thus can be used in file names or directories that need to a unique path for a project configuration.
 - The only required configuration value is the `$(TargetGroup)` the others are optional.

Example:
Pure netstandard configuration:
```
<PropertyGroup>
  <BuildConfigurations>
    netstandard;
  </BuildConfigurations>
<PropertyGroup>
```

All supported targets with unique windows/unix build for netcoreapp:
```
<PropertyGroup>
  <BuildConfigurations>
    netcoreapp-Windows_NT;
    netcoreapp-Unix;
    netfx-Windows_NT;
    uap-Windows_NT;
  </BuildConfigurations>
<PropertyGroup>
```

### Placeholder build configurations
Placeholder build configurations can be added to the `<BuildConfigurations>` property to indicate the build system that the specific project is inbox in that framework and that build configuration needs to be ignored.

Placeholder build configurations start with _ prefix.

Example:
When we have a project that has a `netstandard` build configuration that means that this project is compatible with any build configuration. So if we do a vertical build for `netfx` this project will be built as part of the vertical because `netfx` is compatible with `netstandard`. This means that in the runtime and testhost binaries the netstandard implementation will be included, and we will test against those assets instead of testing against the framework inbox asset. In order to tell the build system to not include this project as part of the `netfx` vertical we need to add a placeholder configuration:
```
<PropertyGroup>
  <BuildConfigurations>
    netstandard;
    _netfx;
  </BuildConfigurations>
</PropertyGroup>
```

## Options for building

A full or individual project build is centered around BuildConfiguration and will be setup in one of the following ways:

1. `$(BuildConfiguration)` can directly be passed to the build.
2. `$(Configuration)` can be passed to the build and `$(BuildConfiguration)` will be set to `$(Configuration)-$(ArchGroup)`. This is a convenience mechanism primarily to help with VS support because VS uses the `Configuration` property for switching between various configurations in the UI. NOTE: this only works well for individual projects and not the root builds.
3. `$(TargetGroup), $(OSGroup), $(ConfigurationGroup), $(ArchGroup)` can individually be passed in to change the default value for just part of the `BuildConfiguration`.
4. If nothing is passed to the build then we will default `BuildConfiguration` from the environment. Example: `netcoreapp-[OSGroup Running On]-Debug-x64`.

On top of the `BuildConfiguration` we also have `RuntimeOS` which can be passed to customize the specific OS and version needed for native package builds as well as package restoration. If not passed it will default based on the OS you are running on.

Any of the mentioned properties can be set via `/p:<Property>=<Value>` at the command line. When building using our run tool or any of the wrapper scripts around it (i.e. build.cmd) a number of these properties have aliases which make them easier to pass (run build.cmd/sh -? for the aliases).

## Selecting the correct build configuration
When building an individual project the `BuildConfiguation` will be used to select the closest matching configuration listed in the projects `BuildConfigurations` property. The rules used to select the configuration will consider compatible target frameworks and OS fallbacks.

TODO: Link to the target framework and OS fallbacks when they are available.
Temporary versions are at https://github.com/dotnet/corefx/blob/dev/eng/src/Tools/GenerateProps/osgroups.props and https://github.com/dotnet/corefx/blob/dev/eng/src/Tools/GenerateProps/targetgroups.props

## Supported full build configurations
- .NET Core latest on current OS (default) -> `netcoreapp-[RunningOS]`
- .NET Core CoreRT -> `netcoreappcorert-[RunningOS]`
- .NET Framework latest -> `netfx-Windows_NT`
- UWP -> `uapaot-Windows_NT`
- UAP F5 -> `uap-Windows_NT`

## Project configurations for VS
For each unique configuration needed for a given library project a configuration property group should be added to the project so it can be selected and built in VS and also clearly identify the various configurations.<BR/>

`<PropertyGroup Condition="'$(Configuration)|$(Platform)' == '$(OSGroup)-$(TargetGroup)-$(ConfigurationGroup)|$(Platform)'">`

- Note that the majority of managed projects, currently all in corefx, $(Platform) is overridden to be AnyCPU.

####*Examples*
Project configurations for a pure IL library project which targets the defaults.
```xml
  <PropertyGroup Condition="'$(Configuration)|$(Platform)' == 'Debug|AnyCPU'" />
  <PropertyGroup Condition="'$(Configuration)|$(Platform)' == 'Release|AnyCPU'" />
```
Project configurations with a unique implementation on Unix and Windows
```xml
  <PropertyGroup Condition="'$(Configuration)|$(Platform)' == 'Unix-Debug|AnyCPU'" />
  <PropertyGroup Condition="'$(Configuration)|$(Platform)' == 'Unix-Release|AnyCPU'" />
  <PropertyGroup Condition="'$(Configuration)|$(Platform)' == 'Windows_NT-Debug|AnyCPU'" />
  <PropertyGroup Condition="'$(Configuration)|$(Platform)' == 'Windows_NT-Release|AnyCPU'" />
```
Project configurations that are unique for a few different target frameworks and runtimes
```xml
  <PropertyGroup Condition="'$(Configuration)|$(Platform)' == 'Debug|AnyCPU'" />
  <PropertyGroup Condition="'$(Configuration)|$(Platform)' == 'Release|AnyCPU'" />
  <PropertyGroup Condition="'$(Configuration)|$(Platform)' == 'uap101aot-Debug|AnyCPU'" />
  <PropertyGroup Condition="'$(Configuration)|$(Platform)' == 'uap101aot-Release|AnyCPU'" />
  <PropertyGroup Condition="'$(Configuration)|$(Platform)' == 'uap101-Debug|AnyCPU'" />
  <PropertyGroup Condition="'$(Configuration)|$(Platform)' == 'uap101-Release|AnyCPU'" />
```

## Updating Configurations

We have a build task that you can run to automatically update all the projects with the above boilerplate as well as updating all the solution files for the libraries. Whenever you change the list of configurations for a project you can regenerate all these for the entire repo by running:

```
msbuild build.proj /t:UpdateVSConfigurations
```

If you want to scope the geneneration you can either undo changes that you don't need or you can temporally limit the set of projects or directories by updating the item set in the UpdateVSConfigurations target in https://github.com/dotnet/corefx/blob/master/build.proj

# Library project guidelines
Library projects should use the following directory layout.

```
src\<Library Name>\src - Contains the source code for the library.
src\<Library Name>\ref - Contains any reference assembly projects for the library
src\<Library Name>\pkg - Contains package projects for the library.
src\<Library Name>\tests - Contains the test code for a library
```

## ref
Reference assemblies are required for any library that has more than one implementation or uses a facade. A reference assembly is a surface-area-only assembly that represents the public API of the library. To generate a reference assembly source file you can use the [GenAPI tool](https://www.nuget.org/packages/Microsoft.DotNet.BuildTools.GenAPI). If a library is a pure portable library with a single implementation it need not use a reference assembly at all.

In the ref directory for the library there should be at most **one** `.csproj` that contains the latest API for the reference assembly for the library. That project can contain multiple entries in its `BuildConfigurations` property.

There are two types of reference assembly projects:

1. Libraries that are contain APIs in netstandard
 - `BuildConfigurations` should contain non-netstandard configurations for the platforms they support.
 - Should use a relative path `<ProjectReference>` to the dependencies it has. Those dependencies should only be libraries with similar build configurations and be part of netstandard.
<BR/>//**CONSIDER**: just using Reference with a custom task to pull from TP or turn to ProjectReference
2. Libraries that are built on top of netstandard
 - `BuildConfigurations` should contain only netstandard configurations.
 - Should contain `<Reference Include='netstandard'>`
 - Anything outside of netstandard should use a relative path `<ProjectReference>` to its dependencies it has. Those dependencies should only be libraries that are built against netstandard as well.

### ref output
The output for the ref project build will be a flat targeting pack folder in the following directory:

`bin\ref\$(TargetGroup)`

<BR/>//**CONSIDER**: Do we need a specific BuildConfiguration version of TargetGroup for this output path to ensure all projects output to same targeting path?

## src
In the src directory for a library there should be only **one** `.csproj` file that contains any information necessary to build the library in various configurations. All supported configurations should be listed in the `BuildConfigurations` property.

All libraries should use `<Reference Include="..." />` for all their project references. That will cause them to be resolved against a targeting pack (i.e. `bin\ref\netcoreapp` or `\bin\ref\netstanard`) based on the project configuration. There should not be any direct project references to other libraries. The only exception to that rule right now is for partial facades which directly reference System.Private.CoreLib and thus need to directly reference other partial facades to avoid type conflicts.
<BR>//**CONSIDER**: just using Reference and use a reference to System.Private.CoreLib as a trigger to turn the other References into a ProjectReference automatically. That will allow us to have consistency where all projects just use Reference.

### src output
The output for the src product build will be a flat runtime folder into the following directory:

`bin\runtime\$(BuildConfiguration)`

Note: The `BuildConfiguration` is the global property and not the project configuration because we need all projects to output to the same runtime directory no matter which compatible configuration we select and build the project with.

## pkg
In the pkg directory for the library there should be only **one** `.pkgproj` for the primary package for the library. If the library has platform-specific implementations those should be split into platform specific projects in a subfolder for each platform. (see [Package projects](./package-projects.md))

TODO: Outline changes needed for pkgprojs

## tests
Similar to the src projects tests projects will define a `BuildConfigurations` property so they can list out the set of build configurations they support.

Tests should not have any `<Reference>` or `<ProjectReference>` items in their project because they will automatically reference everything in the targeting pack based on the configuration they are building in. The only exception to this is a `<ProjectReference>` can be used to reference other test helper libraries or assets.

In order to build and run a test project in a given configuration a root level build.cmd/sh must have been completed for that configuration first. Tests will run on the live built runtime at `bin\runtime\$(BuildConfiguration)`.
TODO: We need update our test host so that it can run from the shared runtime directory as well as resolve assemblies from the test output directory.

### tests output
All test outputs should be under

`bin\tests\$(MSBuildProjectName)\$(BuildConfiguration)` or
`bin\tests\$(MSBuildProjectName)\netstandard`

## Facades
Facade are unique in that they don't have any code and instead are generated by finding a contract reference assembly with the matching identity and generating type forwards for all the types to where they live in the implementation assemblies (aka facade seeds). There are also partial facades which contain some type forwards as well as some code definitions. All the various build configurations should be contained in the one csproj file per library.

TODO: Fill in more information about the required properties for creatng a facade project.

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
- Classes that are forked based on configuration should have file names `<class>.<configuration>.cs`.
 - Where `<configuration>` is one of `$(OSGroup)`, `$(TargetGroup)`, `$(ConfigurationGroup)`, or `$(Platform)`, matching exactly by case to ensure consistency.
- Classes that are forked based on a feature set should have file names `<class>.<feature>.cs`.
 - Where `<feature>` is the name of something that causes a fork in code that isn't a single configuration. Examples:
  - `.CoreCLR.cs` - implementation specific to CoreCLR runtime
  - `.CoreRT.cs` - implementation specific to CoreRT runtime
  - `.Win32.cs` - implementation based on [Win32](https://en.wikipedia.org/wiki/Windows_API)
  - `.WinRT.cs` - implementation based on [WinRT](https://en.wikipedia.org/wiki/Windows_Runtime)
  - `.Uap.cs` - implementation specific to UAP, also known as [UWP](https://en.wikipedia.org/wiki/Universal_Windows_Platform)

## Define naming convention
As mentioned in [Conventions for forked code](conventions-for-forked-code) `#ifdef`ing the code is the last resort as it makes code harder to maintain overtime. If we do need to use `#ifdef`'s we should use the following conventions:
- Defines based on conventions should be one of `$(OSGroup)`, `$(TargetGroup)`, `$(ConfigurationGroup)`, or `$(Platform)`, matching exactly by case to ensure consistency.
 - Examples: `<DefineConstants>$(DefineConstants);net46</DefineConstants>`
- Defines based on convention should match the pattern `FEATURE_<feature name>`. These can unique to a given library project or potentially shared (via name) across multiple projects.
