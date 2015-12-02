#Library Project guidelines
Library projects should use the fllowing directory layout.
```
src\<Library Name>\src - Contains the source code for the library. 
src\<Library Name>\ref - Contains any refernce assembly projects for the library
src\<Library Name>\tests - Contains the test code for a library
```
In the src directory for a library there should be only **one** `.csproj` file that contains any information necessary to build the library in various configurartions (see [Configurations](project-configuration-conventions)). The src directory should also contain exactly **one** `.builds` file which contains all the valid configurations that a library should be built for (see [.builds file](project-.builds-file)). In some cases there might be a Facade subdirectory under src with a .csproj (see [Facades Projects](facades-projects)). 


##Build Pivots
Below is a list of all the various options we pivot the project builds on.

- **Architecture:** x86, x64, ARM, ARM64
- **Flavor:** Debug, Release
- **OS:** Windows_NT, Linux, OSX, FreeBSD
- **Platform Runtimes:** NetFx (aka CLR/Desktop), CoreCLR, NetNative (aka MRT/AOT)
- **Target Frameworks:** NetFx (aka Desktop), .NETCore (aka Store/UWP), DNXCore (aka ASP.NET vNext), netstandard(aka dotnet/Portable)
- **Version:** Potentially multiple versions at the same time.

##Full Repo build pass
**Build Parameters:** *Flavor, Architecture, OS*<BR/>
For each combination of build parameters there should be a full build pass over the entire repo. For each OS the build pass should be performed on that OS.

##Project build pass
**Project Parameters:** *Platform Runtime, Target Framework, Version*<BR/>
These are optional for specific projects and don't require a full build pass but instead should be scoped to individual projects within the most appropriate full build pass.

#Project configuration conventions
For each unique configuration needed for a given library project a configuration property group should be added to the project so it can be selected and built in VS and also clearly identify the various configurations.<BR/>
`<PropertyGroup Condition="'$(Configuration)|$(Platform)' == '$(<OSGroup>_<TargetGroup>_<ConfigurationGroup>)|$(Platform)'">`

- `$(Platform) -> AnyCPU* | x86 | x64 | ARM | ARM64`
- `$(Configuration) -> $(OSGroup)_$(TargetGroup)_$(ConfigurationGroup)`
- `$(OSGroup) -> [Empty]* | Windows | Linux | OSX | FreeBSD`
- `$(TargetGroup) -> [Empty]* | <TargetFrameworkMoniker> | <RuntimeIdentifier> | <Version> | <TFM><RID>`
 - `$(PackageTargetFramework) -> netstandard | netcore50 | net46 | dnxcore50`
 - `$(PackageTargetRuntime) -> aot`
 - For more information on various targets see also [.NET Platform Standard](https://github.com/dotnet/corefx/blob/master/Documentation/project-docs/standard-platform.md)
- `$(ConfigurationGroup) -> Debug* | Release`
<BR/>`*` -> *default values*

####*Examples*
Project configurations for a pure IL library project which targets the defaults.
```
  <PropertyGroup Condition="'$(Configuration)|$(Platform)' == 'Debug|AnyCPU'" />
  <PropertyGroup Condition="'$(Configuration)|$(Platform)' == 'Release|AnyCPU'" />
```
Project configurations with a unique implementation for each OS
```
  <PropertyGroup Condition="'$(Configuration)|$(Platform)' == 'FreeBSD_Debug|AnyCPU " />
  <PropertyGroup Condition="'$(Configuration)|$(Platform)' == 'FreeBSD_Release|AnyCPU'" />
  <PropertyGroup Condition="'$(Configuration)|$(Platform)' == 'Linux_Debug|AnyCPU'" />
  <PropertyGroup Condition="'$(Configuration)|$(Platform)' == 'Linux_Release|AnyCPU'" />
  <PropertyGroup Condition="'$(Configuration)|$(Platform)' == 'OSX_Debug|AnyCPU'" />
  <PropertyGroup Condition="'$(Configuration)|$(Platform)' == 'OSX_Release|AnyCPU'" />
  <PropertyGroup Condition="'$(Configuration)|$(Platform)' == 'Windows_Debug|AnyCPU'" />
  <PropertyGroup Condition="'$(Configuration)|$(Platform)' == 'Windows_Release|AnyCPU'" />
```
Project configurations that are unique for a few different target frameworks and runtimes
```
  <PropertyGroup Condition="'$(Configuration)|$(Platform)' == 'Debug|AnyCPU'" />
  <PropertyGroup Condition="'$(Configuration)|$(Platform)' == 'Release|AnyCPU'" />
  <PropertyGroup Condition="'$(Configuration)|$(Platform)' == 'netcore50aot_Debug|AnyCPU'" />
  <PropertyGroup Condition="'$(Configuration)|$(Platform)' == 'netcore50aot_Release|AnyCPU'" />
  <PropertyGroup Condition="'$(Configuration)|$(Platform)' == 'netcore50_Debug|AnyCPU'" />
  <PropertyGroup Condition="'$(Configuration)|$(Platform)' == 'netcore50_Release|AnyCPU'" />
```
## Project .builds file
To drive the Project build pass we have a *.builds project that will multiplex the various optional build parameters we have for all the various configurtions within a given build pass.

Below is an example for System.Linq.Expressions where it only builds for the windows full build passes and needs a unique build for various target frameworks.
```
<?xml version="1.0" encoding="utf-8"?>
<Project ToolsVersion="12.0" DefaultTargets="Build" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
  <Import Project="$([MSBuild]::GetDirectoryNameOfFileAbove($(MSBuildThisFileDirectory), dir.props))\dir.props" />
  <ItemGroup Condition="'$(TargetsWindows)' == 'true'">
    <Project Include="System.Linq.Expressions.csproj">
      <AdditionalProperties>TargetGroup=</AdditionalProperties> 
    </Project>
    <Project Include="System.Linq.Expressions.csproj">
      <AdditionalProperties>TargetGroup=netcore50aot</AdditionalProperties>
    </Project>
    <Project Include="System.Linq.Expressions.csproj">
      <AdditionalProperties>TargetGroup=netcore50</AdditionalProperties> 
    </Project>
    <Project Include="Facade\System.Linq.Expressions.csproj">
      <AdditionalProperties>TargetGroup=net46</AdditionalProperties>
    </Project>
  </ItemGroup>
  <ItemGroup>
  <Import Project="$([MSBuild]::GetDirectoryNameOfFileAbove($(MSBuildThisFileDirectory), dir.traversal.targets))\dir.traversal.targets" />
</Project>
```
##Facades Projects
Facade projects are unique in that they don't have any code and instead are generated by finding a contract reference assembly with the matching identity and generating type forwards for all the types to where they live in the implementation assemblies (aka facade seeds). There are also partial facades which contain some type forwards as well as some code definitions. Ideally all the various build configurations would be contained in the one csproj file per library but given the unique nature of facades and the fact they are usually a complete fork of everything in the project file it is recommended to create a matching csproj under a Facade directory (<library>\src\Facade\<library>.csproj) and reference that from your .builds file, as in the System.Linq.Expressions example.

TODO: Fill in more information about the required properties for creatng a facade project.

##Defines for code #ifdefs
Configuration base defines should match the name of the configurtion that is needs the fork in code. So either $(Platform), $(OSGroup), $(ConfigurationGroup), $(TargetGroup). These should only be used as a last resort and we should prefer using partial classes or configuration specific cs files for any necessary forking (see [File naming convention](file-naming-convention)).

Feature based defines should be named like `FEATURE_[FeatureName]`. These can unique to a given library project or potentially shared (via name) across multiple projects.


##File naming convention

`<class name>.<group>.cs`

Where `<group>` is:
- Windows/Linux/Unix/Osx - For files specific to a given OS
- WinRT - For files that are have unique WinRT dependencies
- Target Runtime
 - NetFx - Files specific for the full .NET Framework/CLR runtime
 - CoreCLR - Files specific to the CoreCLR runtime
 - NETNative - Files specific to the .NET Native runtime (MRT) or toolchain.
