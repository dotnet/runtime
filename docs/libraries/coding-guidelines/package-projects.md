# Package projects
Package projects bring together all the assemblies that make up a library on different platforms into a set of NuGet packages.

## Package hierarchy
All libraries should have at least one package if they represent public surface area.  This is called the *reference package* and is named the same as the library's assembly, EG: System.Collections.Immutable.

Packages may have platform specific implementation packages.  These are referred to as *runtime packages* and follow the naming convention of runtime.{rid}.{assemblyName}, EG: runtime.unix.System.IO.FileSystem.

In either case the file name of the `.pkgproj` is just {assemblyName}.pkgproj and package names are derived from the contents of the project.

## Package samples
### Simple portable library
This is the simplest case.  The package project need only reference the single project that implements the portable libary.

Sample `System.Text.Encodings.Web.pkgproj`
```
<?xml version="1.0" encoding="utf-8"?>
<Project ToolsVersion="12.0" DefaultTargets="Build" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
  <Import Project="$([MSBuild]::GetDirectoryNameOfFileAbove($(MSBuildThisFileDirectory), dir.props))\dir.props" />

  <ItemGroup>
    <ProjectReference Include="..\src\System.Text.Encodings.Web.csproj">
      <SupportedFramework>net45;netcore45;wp8;wpa81;netcoreapp1.0</SupportedFramework>
    </ProjectReference>
  </ItemGroup>

  <Import Project="$([MSBuild]::GetDirectoryNameOfFileAbove($(MSBuildThisFileDirectory), dir.targets))\dir.targets" />
</Project>
```

### Portable library, inbox on some platforms
These packages need to include placeholders for inbox platforms.  They should also include reference assemblies for representing the fixed API that is inbox in old platforms.

Sample `System.Collections.Concurrent.pkgproj`
```
<?xml version="1.0" encoding="utf-8"?>
<Project ToolsVersion="12.0" DefaultTargets="Build" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
  <Import Project="$([MSBuild]::GetDirectoryNameOfFileAbove($(MSBuildThisFileDirectory), dir.props))\dir.props" />
  <ItemGroup>
    <ProjectReference Include="..\ref\4.0.0\System.Collections.Concurrent.depproj">
      <SupportedFramework>net45;netcore45;wpa81</SupportedFramework>
    </ProjectReference>
    <ProjectReference Include="..\ref\System.Collections.Concurrent.csproj">
      <SupportedFramework>net46;netcore50;netcoreapp1.0</SupportedFramework>
    </ProjectReference>
    <ProjectReference Include="..\src\System.Collections.Concurrent.csproj"/>

    <InboxOnTargetFramework Include="MonoAndroid10" />
    <InboxOnTargetFramework Include="MonoTouch10" />
    <InboxOnTargetFramework Include="net45" />
    <InboxOnTargetFramework Include="win8" />
    <InboxOnTargetFramework Include="wpa81" />
    <InboxOnTargetFramework Include="xamarinios10" />
    <InboxOnTargetFramework Include="xamarinmac20" />
  </ItemGroup>
  <Import Project="$([MSBuild]::GetDirectoryNameOfFileAbove($(MSBuildThisFileDirectory), dir.targets))\dir.targets" />
</Project>
```

### Framework-specific library
Framework specific libraries are effectively the same as the previous example.  The difference is that the src project reference **must** refer to the `.builds` file which will provide multiple assets from multiple projects.

Sample System.Net.Security.pkgproj
```
<?xml version="1.0" encoding="utf-8"?>
<Project ToolsVersion="14.0" DefaultTargets="Build" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
  <Import Project="$([MSBuild]::GetDirectoryNameOfFileAbove($(MSBuildThisFileDirectory), dir.props))\dir.props" />
  <ItemGroup>
    <ProjectReference Include="..\ref\System.Net.Security.builds">
      <SupportedFramework>net463;netcoreapp1.1;$(AllXamarinFrameworks)</SupportedFramework>
    </ProjectReference>
    <ProjectReference Include="..\src\System.Net.Security.builds" />
  </ItemGroup>
  <ItemGroup>
    <InboxOnTargetFramework Include="MonoAndroid10" />
    <InboxOnTargetFramework Include="MonoTouch10" />
    <InboxOnTargetFramework Include="xamarinios10" />
    <InboxOnTargetFramework Include="xamarinmac20" />
    <InboxOnTargetFramework Include="xamarintvos10" />
    <InboxOnTargetFramework Include="xamarinwatchos10" />

    <NotSupportedOnTargetFramework Include="netcore50">
      <PackageTargetRuntime>win7</PackageTargetRuntime>
    </NotSupportedOnTargetFramework>
  </ItemGroup>
  <ItemGroup>
  </ItemGroup>
  <Import Project="$([MSBuild]::GetDirectoryNameOfFileAbove($(MSBuildThisFileDirectory), dir.targets))\dir.targets" />
</Project>
```

Sample \ref .builds file defining a constant used to filter API that were added on top of the netstandard1.7 ones and are available only in netcoreapp1.1: 

```
<?xml version="1.0" encoding="utf-8"?>
<Project ToolsVersion="14.0" DefaultTargets="Build" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
  <Import Project="$([MSBuild]::GetDirectoryNameOfFileAbove($(MSBuildThisFileDirectory), dir.props))\dir.props" />
  <PropertyGroup>
    <OutputType>Library</OutputType>
    <NuGetTargetMoniker>.NETStandard,Version=v1.7</NuGetTargetMoniker>
    <DefineConstants Condition="'$(TargetGroup)' == 'netcoreapp1.1'">$(DefineConstants);netcoreapp11</DefineConstants>
  </PropertyGroup>
  <ItemGroup>
    <Compile Include="System.Net.Security.cs" />
    <Compile Include="System.Net.Security.Manual.cs" />
  </ItemGroup>
  <ItemGroup>
    <None Include="project.json" />
  </ItemGroup>
  <Import Project="$([MSBuild]::GetDirectoryNameOfFileAbove($(MSBuildThisFileDirectory), dir.targets))\dir.targets" />
</Project>
```

Conditional compilation using the above-mentioned constant (from `ref\System.Net.Security.cs`):

```
#if netcoreapp11
        public virtual void AuthenticateAsClient(string targetHost, System.Security.Cryptography.X509Certificates.X509CertificateCollection clientCertificates, bool checkCertificateRevocation) { }
#endif
```

Sample \src .builds file (in this case the implementation is the same in both netcoreapp1.1 and netstandard1.7):

```
<?xml version="1.0" encoding="utf-8"?>
<Project ToolsVersion="14.0" DefaultTargets="Build" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
  <Import Project="$([MSBuild]::GetDirectoryNameOfFileAbove($(MSBuildThisFileDirectory), dir.props))\dir.props" />
  <ItemGroup>
    <Project Include="System.Net.Security.csproj">
      <OSGroup>Unix</OSGroup>
    </Project>
    <Project Include="System.Net.Security.csproj">
      <OSGroup>Windows_NT</OSGroup>
    </Project>
    <Project Include="System.Net.Security.csproj">
      <TargetGroup>net463</TargetGroup>
    </Project>
  </ItemGroup>
  <Import Project="$([MSBuild]::GetDirectoryNameOfFileAbove($(MSBuildThisFileDirectory), dir.traversal.targets))\dir.traversal.targets" />
</Project>
```

Tests can be similarly filtered grouping the compilation directives under:
```
  <ItemGroup Condition="'$(TargetGroup)'=='netcoreapp1.1'">
```
(from `\tests\FunctionalTests\System.Net.Security.Tests.csproj`)

### Platform-specific library
These packages need to provide a different platform specific implementation on each platform.  They do this by splitting the implementations into seperate packages and associating those platform specific packages with the primary reference package.  Each platform specific package sets `PackageTargetRuntime` to the specific platform RID that it applies.

Sample `System.IO.FileSystem.pkgproj`
```
<?xml version="1.0" encoding="utf-8"?>
<Project ToolsVersion="12.0" DefaultTargets="Build" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
  <Import Project="$([MSBuild]::GetDirectoryNameOfFileAbove($(MSBuildThisFileDirectory), dir.props))\dir.props" />
  <ItemGroup>
    <ProjectReference Include="..\ref\System.IO.FileSystem.csproj">
      <SupportedFramework>net46;netcore50;netcoreapp1.0</SupportedFramework>
    </ProjectReference>
    <ProjectReference Include="..\src\Facade\System.IO.FileSystem.csproj" />
    <ProjectReference Include="win\System.IO.FileSystem.pkgproj" />
    <ProjectReference Include="unix\System.IO.FileSystem.pkgproj" />

    <InboxOnTargetFramework Include="MonoAndroid10" />
    <InboxOnTargetFramework Include="MonoTouch10" />
    <InboxOnTargetFramework Include="xamarinios10" />
    <InboxOnTargetFramework Include="xamarinmac20" />
  </ItemGroup>
  <Import Project="$([MSBuild]::GetDirectoryNameOfFileAbove($(MSBuildThisFileDirectory), dir.targets))\dir.targets" />
</Project>
```

`win/System.IO.FileSystem.pkgproj`
```
<?xml version="1.0" encoding="utf-8"?>
<Project ToolsVersion="12.0" DefaultTargets="Build" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
  <Import Project="$([MSBuild]::GetDirectoryNameOfFileAbove($(MSBuildThisFileDirectory), dir.props))\dir.props" />

  <PropertyGroup>
    <PackageTargetRuntime>win7</PackageTargetRuntime>
    <PreventImplementationReference>true</PreventImplementationReference>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\System.IO.FileSystem.builds">
      <AdditionalProperties>OSGroup=Windows_NT</AdditionalProperties>
    </ProjectReference>

    <!-- No implementation on platforms where our P-Invokes are not allowed -->
    <NotSupportedOnTargetFramework Include="win8" />
    <NotSupportedOnTargetFramework Include="wp8" />
    <NotSupportedOnTargetFramework Include="wpa81" />

    <!-- don't use the dotnet implementation for any version of desktop, it's implementation comes from the reference package -->
    <ExternalOnTargetFramework Include="net" />
  </ItemGroup>


  <Import Project="$([MSBuild]::GetDirectoryNameOfFileAbove($(MSBuildThisFileDirectory), dir.targets))\dir.targets" />
</Project>
```
`unix/System.IO.FileSystem.pkgproj`
```
<?xml version="1.0" encoding="utf-8"?>
<Project ToolsVersion="12.0" DefaultTargets="Build" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
  <Import Project="$([MSBuild]::GetDirectoryNameOfFileAbove($(MSBuildThisFileDirectory), dir.props))\dir.props" />

  <PropertyGroup>
    <PackageTargetRuntime>unix</PackageTargetRuntime>
    <PreventImplementationReference>true</PreventImplementationReference>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\System.IO.FileSystem.builds">
      <AdditionalProperties>OSGroup=Linux</AdditionalProperties>
    </ProjectReference>
  </ItemGroup>

  <Import Project="$([MSBuild]::GetDirectoryNameOfFileAbove($(MSBuildThisFileDirectory), dir.targets))\dir.targets" />
</Project>
```

## Asset selection
The makeup of a package folder is primarily a grouping of project references to the projects that compose that package.  Settings within each referenced project determines where that asset will be placed in the package.  For example, reference assembly projects will be placed under the `ref/{targetMoniker}` folder in the package and implementations will be under either `lib/{targetMoniker}` or `runtimes/{rid}/lib/{targetMoniker}`.  Whenever NuGet evaulates a package in the context of a referencing project it will choose the best compile time asset (preferring `ref`, then falling back to `lib`) and runtime asset (preffering `runtimes/{rid}/lib` and falling back to `lib`) for every package that is referenced.  For more information see http://docs.nuget.org/.

Asset projects (`.csproj`, `.vbproj`, or `.depproj`) can control their `{targetMoniker}` using the `PackageTargetFramework` property in the project file.  Similarly `{rid}` is controlled using the `PackageTargetRuntime` property.  In the corefx repo we automatically select default values for these properties based on the [Build pivots](#build-pivots).  These can be overridden in the project reference using metadata of the same name, but this is rarely needed.

The primary thing that the library author needs to do in order to ensure the correct asset selection is:

1. Configure the correct projects in your library's `.builds` file.
2. Reference the `.builds` file from the package project.
3. Provide a default PackageTargetFramework for empty-TargetGroup builds in the library's `.csproj` or `.vbproj`.
    ```
    <PackageTargetFramework Condition="'$(PackageTargetFramework)' == ''">dotnet5.4</PackageTargetFramework>
    ```
### Which version of dotnet/netstandard should I select?
TL;DR - choose the lowest version that doesn't result in build errors for both the library projects and package project.

NETStandard/DotNet are *open* ended portable identifiers.  They allow a package to place an asset in a folder and that asset can be reused on any framework that supports that version of NETStandard/DotNet.  This is in contrast to the previous *closed* set portable-a+b+c identifiers which only applied to the frameworks listed in the set.  For more information see [.NET Platform Standard](https://github.com/dotnet/corefx/blob/master/Documentation/architecture/net-platform-standard.md).

Libraries should select a version of DotNet/NETStandard that supports the most frameworks.  This means the library should choose the lowest version that provides all the API needed to implement their functionality.  Eventually this will be the same moniker used for package resolution in the library project, AKA in `frameworks` section for the libraries project.json.

In CoreFx we don't always use the package resolution for dependencies, sometimes we must use project references.  Additionally we aren't building all projects with the NETStandard/DotNet identifier.  This issue is tracked with https://github.com/dotnet/corefx/issues/2427.  As a result we calculate the version as an added safegaurd based on seeds.  These seeds are listed in [Generations.json](https://github.com/dotnet/buildtools/blob/master/src/Microsoft.DotNet.Build.Tasks.Packaging/src/PackageFiles/Generations.json) and rarely change.  They are a record of what libraries shipped in-box and are unchangeable for a particular framework supporting a generation.  Occasionally an API change can be made even to these in-box libraries and shipped out-of-band, for example by adding a new type and putting that type in a hybrid facade.  This is the only case when it is permitted to update Generations.json. 

In addition to the minimum API version required by implementation, reference assemblies should only claim the NETStandard/DotNet version of the minimum implementation assembly.  Just because a reference assembly only depends on API in NETStandard1.0, if its implementations only apply to frameworks supporting NETStandard1.4, it should use NETStandard1.4.

### .NET Framework facades
.NET Framework facades must be part of the reference package.  This is because if we were to use the reference assembly on desktop it would have type collisions with whatever types already exist in the desktop reference assemblies.  Since we include the desktop reference facade in the reference package we also include the runtime facade in the same package for compression savings.

## Applicability validation
Part of package build is to ensure that a package is applicable on all platforms it supports and not applicable on platforms it does not support.  We do this validation for a set of targets established in the packaging tools (see [DefaultValidateFramework](https://github.com/dotnet/buildtools/blob/9f4ddda1cb021c9bd25f606bc4e74b92e4b82869/src/Microsoft.DotNet.Build.Tasks.Packaging/src/PackageFiles/Packaging.targets#L709)).  Package projects identify the targets supported in one of two ways.

1. **Preferred:** Through `SupportedFramework` metadata on the project reference.  The metadata will associate the API version of that project reference with the frameworks listed.
    ```
    <ProjectReference Include="..\ref\4.0.0\System.Collections.Concurrent.depproj">
        <SupportedFramework>net45;netcore45;wpa81</SupportedFramework>
    </ProjectReference>
    <ProjectReference Include="..\ref\System.Collections.Concurrent.csproj">
        <SupportedFramework>net46;netcore50;netcoreapp1.0</SupportedFramework>
    </ProjectReference>
    ```

2. Through SupportedFramework items with Version metdata.
    ```
    <!-- no version indicates latest is supported -->
    <SupportedFramework Include="net46;netcore50;netcoreapp1.0" />
    <!-- specific version indicates that version is supported -->
    <SupportedFramework Include="net45;netcore45;wpa81">
        <Version>4.0.0.0</Version>
    </SupportedFramework>
    ```

###Inbox assets
Some libraries are supported inbox on particular frameworks.  For these frameworks the package should not present any assets for (ref or lib) for that framework, but instead permit installation and provide no assets.  We do this in the package by using placeholders ref and lib folders for that framework.  In the package project one can use `InboxOnTargetFramework` items.  The following is an example from the System.Linq.Expressions package.
```
<InboxOnTargetFramework Include="net45" />
<InboxOnTargetFramework Include="win8" />
<InboxOnTargetFramework Include="wp80" />
<InboxOnTargetFramework Include="wpa81" />
```

If the library is also a "classic" reference assembly, not referenced by default, then adding the `AsFrameworkReference` metadata will instruct that the package include a `frameworkReference` element in the nuspec.  The following is the an example from the Microsoft.CSharp package.
```
<InboxOnTargetFramework Include="net45">
    <AsFrameworkReference>true</AsFrameworkReference>
</InboxOnTargetFramework>
<InboxOnTargetFramework Include="win8" />
<InboxOnTargetFramework Include="wp80" />
<InboxOnTargetFramework Include="wpa81" />
```

Package validation will catch a case where we know a library is supported inbox but a package is using an asset from the package.  This data is driven by framework lists from previously-shipped targeting packs.  The error will appear as: *Framework net45 should support Microsoft.CSharp inbox but {explanation of problem}.  You may need to add <InboxOnTargetFramework Include="net45" /> to your project.*

###External assets
Runtime specific packages are used to break apart implementations into seperate packages and enable "pay-for-play".  For example: don't download the Windows implementation if we're only building/deploying for linux.  In most cases we can completely seperate implementations into seperate packages such that they easily translate.  For example:
```
runtimes/win/lib/dotnet5.4/System.Banana.dll
runtimes/unix/lib/dotnet5.4/System.Banana.dll
```
This can easily be split into a `win` and `unix` package.  If someone happens to install both packages into a project they'll still get a single implementation.

Consider the following:
```
runtimes/win/lib/dotnet5.4/System.Banana.dll
runtimes/win/lib/net46/System.Banana.dll
```
Suppose we wanted to split the desktop (`net46`) implementation into a seperate package than the portable implementation.  Doing so would cause both the `dotnet5.4` asset and the `net46` asset to be applicable and result in a bin-clash.  This is because in a single package the `net46` asset is preferred over the `dotnet5.4` asset, but in seperate packages both are in view.  The packaging validation will catch this problem and display an error such as

*System.Banana includes both package1/runtimes/win/lib/net46/System.Banana.dll and package2/runtimes/win/lib/dotnet5.4/System.Banana.dll an on net46 which have the same name and will clash when both packages are used.*


The fix for the error is to put a placeholder in the package that contains the asset we want to prevent applying.  This can be done with the following syntax.
```
<ExternalOnTargetFramework Include="net46" />
```

###Not supported
In rare cases a particular library might represent itself as targeting a specific portable moniker (eg: `dotnet5.4`) but it cannot be supported on a particular target framework that is included in that portable moniker for other reasons.  One example of this is System.Diagnostics.Process.  The surface area of this API is portable to dotnet5.4 and could technically run in UWP based on its managed dependencies.  The native API, however, is not supported in app container.  To prevent this package and packages which depend on from installing in UWP projects, only to fail at runtime, we can block the package from being installed.

To do this we create a placeholder in the lib folder with the following syntax.  The resulting combination will be an applicable ref asset with no applicable lib and NuGet's compat check will fail.
```
<NotSupportedOnTargetFramework Include="net46" />
```
The packaging validation will catch this problem and display an error such as
*System.Diagnostics.Process should not be supported on netcore50 but has both compile and runtime assets.*
