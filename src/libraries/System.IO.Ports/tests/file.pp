<!--
============================================================================================================================================
C:\Users\calope\source\repos\runtime8\src\libraries\System.IO.Ports\tests\System.IO.Ports.Tests.csproj
============================================================================================================================================
-->
<Project DefaultTargets="Build" InitialTargets="ValidateTargetOSLowercase">
  <!--
============================================================================================================================================
  <Import Project="Sdk.props" Sdk="Microsoft.NET.Sdk">
  This import was added implicitly because the Project element's Sdk attribute specified "Microsoft.NET.Sdk".

C:\Program Files\dotnet\sdk\8.0.110\Sdks\Microsoft.NET.Sdk\Sdk\Sdk.props
============================================================================================================================================
-->
  <!--
***********************************************************************************************
Sdk.props

WARNING:  DO NOT MODIFY this file unless you are knowledgeable about MSBuild and have
          created a backup copy.  Incorrect changes to this file will make it
          impossible to load or build your projects from the command-line or the IDE.

Copyright (c) .NET Foundation. All rights reserved.
***********************************************************************************************
-->
  <PropertyGroup xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <!--
      Indicate to other targets that Microsoft.NET.Sdk is being used.

      This must be set here (as early as possible, before Microsoft.Common.props)
      so that everything that follows can depend on it.

      In particular, Directory.Build.props and nuget package props need to be able
      to use this flag and they are imported by Microsoft.Common.props.
    -->
    <UsingMicrosoftNETSdk>true</UsingMicrosoftNETSdk>
    <!--
      Indicate whether the set of SDK defaults that makes SDK style project concise are being used.
      For example: globbing, importing msbuild common targets.

      Similar to the property above, it must be set here.
    -->
    <UsingNETSdkDefaults>true</UsingNETSdkDefaults>
  </PropertyGroup>
  <!-- We need to put the UseArtifactsOutput logic after the import of Directory.Build.props, but before the MSBuild Project Extensions .props import.
       However, both of these things happen in Microsoft.Common.props with no opportunity to insert logic in between them.

       So what we do here is duplicate the Directory.Build.props import logic from Microsoft.Common.props, and then set ImportDirectoryBuildProps to
       false so that it doesn't get imported twice.

       Alternatively, we could add a hook in MSBuild to define a file to import at the right location, to avoid duplicating the logic here.
       -->
  <PropertyGroup xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <ImportDirectoryBuildProps Condition="'$(ImportDirectoryBuildProps)' == ''">true</ImportDirectoryBuildProps>
  </PropertyGroup>
  <PropertyGroup Condition="'$(ImportDirectoryBuildProps)' == 'true' and '$(DirectoryBuildPropsPath)' == ''" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <_DirectoryBuildPropsFile Condition="'$(_DirectoryBuildPropsFile)' == ''">Directory.Build.props</_DirectoryBuildPropsFile>
    <_DirectoryBuildPropsBasePath Condition="'$(_DirectoryBuildPropsBasePath)' == ''">$([MSBuild]::GetDirectoryNameOfFileAbove($(MSBuildProjectDirectory), '$(_DirectoryBuildPropsFile)'))</_DirectoryBuildPropsBasePath>
    <DirectoryBuildPropsPath Condition="'$(_DirectoryBuildPropsBasePath)' != '' and '$(_DirectoryBuildPropsFile)' != ''">$([System.IO.Path]::Combine('$(_DirectoryBuildPropsBasePath)', '$(_DirectoryBuildPropsFile)'))</DirectoryBuildPropsPath>
  </PropertyGroup>
  <!--
============================================================================================================================================
  <Import Project="$(DirectoryBuildPropsPath)" Condition="'$(ImportDirectoryBuildProps)' == 'true' and exists('$(DirectoryBuildPropsPath)')">

C:\Users\calope\source\repos\runtime8\src\libraries\System.IO.Ports\Directory.Build.props
============================================================================================================================================
-->
  <!--
============================================================================================================================================
  <Import Project="..\Directory.Build.props">

C:\Users\calope\source\repos\runtime8\src\libraries\Directory.Build.props
============================================================================================================================================
-->
  <PropertyGroup>
    <DisableArcadeTestFramework>true</DisableArcadeTestFramework>
    <!-- Enabling this rule will cause build failures on undocumented public APIs.
         We cannot add it in eng/Versions.props because src/coreclr does not have access to UseCompilerGeneratedDocXmlFile, which ensures
         we only enable it in specific projects. so to avoid duplicating this property in coreclr, we can first scope it to src/libraries.
         This property needs to be declared before the ..\..\Directory.Build.props import. -->
    <SkipArcadeNoWarnCS1591>true</SkipArcadeNoWarnCS1591>
  </PropertyGroup>
  <!--
============================================================================================================================================
  <Import Project="..\..\Directory.Build.props">

C:\Users\calope\source\repos\runtime8\Directory.Build.props
============================================================================================================================================
-->
  <PropertyGroup>
    <!-- For non-SDK projects that import this file and then import Microsoft.Common.props,
         tell Microsoft.Common.props not to import Directory.Build.props again. -->
    <ImportDirectoryBuildProps>false</ImportDirectoryBuildProps>
    <!-- Used to determine if we should build some packages only once across multiple official build legs.
         For offline builds we still set OfficialBuildId but we need to build all the packages for a single
         leg only, so we also take DotNetBuildFromSource  into account. -->
    <BuildingAnOfficialBuildLeg Condition="'$(BuildingAnOfficialBuildLeg)' == '' and '$(OfficialBuildId)' != '' and '$(DotNetBuildFromSource)' != 'true'">true</BuildingAnOfficialBuildLeg>
    <!-- When doing a source build, we want to build the various text-only manifests in
         all cases, rather than ordinarily where we build them during mobile or wasm
         build legs. This makes the manifests available on source-only builds. -->
    <ForceBuildMobileManifests Condition="'$(DotNetBuildFromSource)' == 'true'">true</ForceBuildMobileManifests>
  </PropertyGroup>
  <PropertyGroup Label="CalculateTargetOS">
    <_hostOS>linux</_hostOS>
    <_hostOS Condition="$([MSBuild]::IsOSPlatform('OSX'))">osx</_hostOS>
    <_hostOS Condition="$([MSBuild]::IsOSPlatform('FREEBSD'))">freebsd</_hostOS>
    <_hostOS Condition="$([MSBuild]::IsOSPlatform('NETBSD'))">netbsd</_hostOS>
    <_hostOS Condition="$([MSBuild]::IsOSPlatform('ILLUMOS'))">illumos</_hostOS>
    <_hostOS Condition="$([MSBuild]::IsOSPlatform('SOLARIS'))">solaris</_hostOS>
    <_hostOS Condition="$([MSBuild]::IsOSPlatform('HAIKU'))">haiku</_hostOS>
    <_hostOS Condition="$([MSBuild]::IsOSPlatform('WINDOWS'))">windows</_hostOS>
    <HostOS>$(_hostOS)</HostOS>
    <TargetOS Condition="'$(TargetOS)' == '' and '$(RuntimeIdentifier)' == 'browser-wasm'">browser</TargetOS>
    <TargetOS Condition="'$(TargetOS)' == ''">$(_hostOS)</TargetOS>
    <TargetsMobile Condition="'$(TargetOS)' == 'ios' or '$(TargetOS)' == 'iossimulator' or '$(TargetOS)' == 'maccatalyst' or '$(TargetOS)' == 'tvos' or '$(TargetOS)' == 'tvossimulator' or '$(TargetOS)' == 'android' or '$(TargetOS)' == 'browser' or '$(TargetOS)' == 'wasi'">true</TargetsMobile>
    <TargetsAppleMobile Condition="'$(TargetOS)' == 'ios' or '$(TargetOS)' == 'iossimulator' or '$(TargetOS)' == 'maccatalyst' or '$(TargetOS)' == 'tvos' or '$(TargetOS)' == 'tvossimulator'">true</TargetsAppleMobile>
  </PropertyGroup>
  <!-- Platform property is required by RepoLayout.props in Arcade SDK. -->
  <PropertyGroup Label="CalculateArch">
    <_hostArch>$([System.Runtime.InteropServices.RuntimeInformation]::ProcessArchitecture.ToString().ToLowerInvariant)</_hostArch>
    <BuildArchitecture Condition="'$(BuildArchitecture)' == ''">$(_hostArch)</BuildArchitecture>
    <TargetArchitecture Condition="'$(TargetArchitecture)' == '' and ('$(TargetOS)' == 'browser' or '$(RuntimeIdentifier)' == 'browser-wasm')">wasm</TargetArchitecture>
    <TargetArchitecture Condition="'$(TargetArchitecture)' == '' and ('$(TargetOS)' == 'wasi' or '$(RuntimeIdentifier)' == 'wasi-wasm')">wasm</TargetArchitecture>
    <TargetArchitecture Condition="'$(TargetArchitecture)' == '' and '$(_hostArch)' == 'arm'">arm</TargetArchitecture>
    <TargetArchitecture Condition="'$(TargetArchitecture)' == '' and '$(_hostArch)' == 'armv6'">armv6</TargetArchitecture>
    <TargetArchitecture Condition="'$(TargetArchitecture)' == '' and '$(_hostArch)' == 'armel'">armel</TargetArchitecture>
    <TargetArchitecture Condition="'$(TargetArchitecture)' == '' and '$(_hostArch)' == 'arm64'">arm64</TargetArchitecture>
    <TargetArchitecture Condition="'$(TargetArchitecture)' == '' and '$(_hostArch)' == 'loongarch64'">loongarch64</TargetArchitecture>
    <TargetArchitecture Condition="'$(TargetArchitecture)' == '' and '$(_hostArch)' == 's390x'">s390x</TargetArchitecture>
    <TargetArchitecture Condition="'$(TargetArchitecture)' == '' and '$(_hostArch)' == 'ppc64le'">ppc64le</TargetArchitecture>
    <TargetArchitecture Condition="'$(TargetArchitecture)' == '' and '$(TargetsMobile)' == 'true'">x64</TargetArchitecture>
    <TargetArchitecture Condition="'$(TargetArchitecture)' == ''">x64</TargetArchitecture>
    <Platform Condition="'$(Platform)' == '' and '$(InferPlatformFromTargetArchitecture)' == 'true'">$(TargetArchitecture)</Platform>
  </PropertyGroup>
  <PropertyGroup Label="SetOSTargetMinVersions">
    <!--
      Minimum target OS versions, keep in sync with:
        - eng/native/configurecompiler.cmake
        - eng/native/build-commons.sh
        - src/native/libs/build-native.sh
        - src/coreclr/tools/aot/ILCompiler.Compiler/Compiler/DependencyAnalysis/ObjectWriter.cs
        - src/coreclr/nativeaot/BuildIntegration/Microsoft.NETCore.Native.Unix.targets
        - src/installer/pkg/sfx/bundle/shared-framework-distribution-template-x64.xml
        - src/installer/pkg/sfx/bundle/shared-framework-distribution-template-arm64.xml
        - src/tasks/MobileBuildTasks/Apple/AppleProject.cs
     -->
    <AndroidApiLevelMin>21</AndroidApiLevelMin>
    <iOSVersionMin>11.0</iOSVersionMin>
    <tvOSVersionMin>11.0</tvOSVersionMin>
    <watchOSVersionMin>2.0</watchOSVersionMin>
    <watchOS64_32VersionMin>5.1</watchOS64_32VersionMin>
    <macOSVersionMin>10.15</macOSVersionMin>
    <macOSVersionMin Condition="('$(TargetOS)' == 'osx' or '$(TargetOS)' == 'maccatalyst') and '$(TargetArchitecture)' == 'arm64'">11.0</macOSVersionMin>
  </PropertyGroup>
  <PropertyGroup>
    <!-- Set OutDirName (Arcade specific property that must be set before the Arcade SDK is imported) to change the BaseOutputPath and
         BaseIntermediateOutputPath properties to include the ref subfolder. -->
    <IsReferenceAssemblyProject Condition="$([System.IO.Path]::GetFileName('$(MSBuildProjectDirectory)')) == 'ref'">true</IsReferenceAssemblyProject>
    <OutDirName Condition="'$(IsReferenceAssemblyProject)' == 'true'">$(MSBuildProjectName)$([System.IO.Path]::DirectorySeparatorChar)ref</OutDirName>
  </PropertyGroup>
  <!-- Import the Arcade SDK -->
  <!--
============================================================================================================================================
  <Import Project="Sdk.props" Sdk="Microsoft.DotNet.Arcade.Sdk">

C:\.tools\.nuget\packages\microsoft.dotnet.arcade.sdk\8.0.0-beta.24426.2\Sdk\Sdk.props
============================================================================================================================================
-->
  <!-- Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. -->
  <PropertyGroup>
    <!-- 
      When the bootstrapper script initializes a repo it restores an empty project that imports the toolset SDK.
      It invokes WriteToolsetLocation target with __ToolsetLocationOutputFile set to the path where the location of 
      SDK Build.proj entry point is to be stored. Suppress all other imports for that project.      
    -->
    <_SuppressSdkImports>false</_SuppressSdkImports>
    <_SuppressSdkImports Condition="'$(__ToolsetLocationOutputFile)' != ''">true</_SuppressSdkImports>
  </PropertyGroup>
  <!--
============================================================================================================================================
  <Import Project="..\tools\Settings.props" Condition="!$(_SuppressSdkImports)">

C:\.tools\.nuget\packages\microsoft.dotnet.arcade.sdk\8.0.0-beta.24426.2\tools\Settings.props
============================================================================================================================================
-->
  <!-- Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. -->
  <PropertyGroup>
    <_ArcadeOverriddenCustomBeforeMicrosoftCommonTargets>$(CustomBeforeMicrosoftCommonTargets)</_ArcadeOverriddenCustomBeforeMicrosoftCommonTargets>
    <_ArcadeOverriddenCustomBeforeMicrosoftCommonCrossTargetingTargets>$(CustomBeforeMicrosoftCommonCrossTargetingTargets)</_ArcadeOverriddenCustomBeforeMicrosoftCommonCrossTargetingTargets>
    <CustomBeforeMicrosoftCommonTargets>$(MSBuildThisFileDirectory)BeforeCommonTargets.targets</CustomBeforeMicrosoftCommonTargets>
    <CustomBeforeMicrosoftCommonCrossTargetingTargets>$(MSBuildThisFileDirectory)BeforeCommonTargets.CrossTargeting.targets</CustomBeforeMicrosoftCommonCrossTargetingTargets>
    <!-- MSBuild has "global" variables (ie command-line or MSBuild task properties) override local declarations.  That's generally not the behavior that we want in Arcade.
         We want to be able to have Arcade MSBuild a project / target with the property set as a default, but let the project override that value.  To work around MSBuild,
         we pass in `_blah` and set it to a local property (`blah`) which is not global. -->
    <NETCORE_ENGINEERING_TELEMETRY Condition="'$(NETCORE_ENGINEERING_TELEMETRY)' == ''">$(_NETCORE_ENGINEERING_TELEMETRY)</NETCORE_ENGINEERING_TELEMETRY>
  </PropertyGroup>
  <!--
============================================================================================================================================
  <Import Project="BuildTasks.props">

C:\.tools\.nuget\packages\microsoft.dotnet.arcade.sdk\8.0.0-beta.24426.2\tools\BuildTasks.props
============================================================================================================================================
-->
  <!-- Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. -->
  <PropertyGroup>
    <ArcadeSdkBuildTasksAssembly Condition="'$(MSBuildRuntimeType)' != 'Core'">$(MSBuildThisFileDirectory)net472\Microsoft.DotNet.Arcade.Sdk.dll</ArcadeSdkBuildTasksAssembly>
    <ArcadeSdkBuildTasksAssembly Condition="'$(MSBuildRuntimeType)' == 'Core'">$(MSBuildThisFileDirectory)net8.0\Microsoft.DotNet.Arcade.Sdk.dll</ArcadeSdkBuildTasksAssembly>
  </PropertyGroup>
  <!--
============================================================================================================================================
  </Import>

C:\.tools\.nuget\packages\microsoft.dotnet.arcade.sdk\8.0.0-beta.24426.2\tools\Settings.props
============================================================================================================================================
-->
  <!--
============================================================================================================================================
  <Import Project="ProjectLayout.props">

C:\.tools\.nuget\packages\microsoft.dotnet.arcade.sdk\8.0.0-beta.24426.2\tools\ProjectLayout.props
============================================================================================================================================
-->
  <!-- Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. -->
  <!--
     Properties describing the layout of the repo specific to the current project.
  -->
  <!--
============================================================================================================================================
  <Import Project="RepoLayout.props">

C:\.tools\.nuget\packages\microsoft.dotnet.arcade.sdk\8.0.0-beta.24426.2\tools\RepoLayout.props
============================================================================================================================================
-->
  <!-- Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. -->
  <!--
     Properties describing the layout of the repo.
  -->
  <PropertyGroup>
    <Configuration Condition="'$(Configuration)' == ''">Debug</Configuration>
    <Platform Condition="'$(Platform)' == ''">AnyCPU</Platform>
    <PlatformName Condition="'$(PlatformName)' == ''">$(Platform)</PlatformName>
  </PropertyGroup>
  <PropertyGroup>
    <!-- Respect environment variable for the NuGet Packages Root if set; otherwise, use the current default location -->
    <NuGetPackageRoot Condition="'$(NuGetPackageRoot)' != ''">$([MSBuild]::NormalizeDirectory('$(NuGetPackageRoot)'))</NuGetPackageRoot>
    <NuGetPackageRoot Condition="'$(NuGetPackageRoot)' == '' and '$(NUGET_PACKAGES)' != ''">$([MSBuild]::NormalizeDirectory('$(NUGET_PACKAGES)'))</NuGetPackageRoot>
    <NuGetPackageRoot Condition="'$(NuGetPackageRoot)' == '' and '$(OS)' == 'Windows_NT'">$([MSBuild]::NormalizeDirectory('$(UserProfile)', '.nuget', 'packages'))</NuGetPackageRoot>
    <NuGetPackageRoot Condition="'$(NuGetPackageRoot)' == '' and '$(OS)' != 'Windows_NT'">$([MSBuild]::NormalizeDirectory('$(HOME)', '.nuget', 'packages'))</NuGetPackageRoot>
  </PropertyGroup>
  <PropertyGroup>
    <RepoRoot Condition="'$(RepoRoot)' == ''">$([MSBuild]::NormalizeDirectory('$([MSBuild]::GetDirectoryNameOfFileAbove($(MSBuildProjectDirectory), 'global.json'))'))</RepoRoot>
  </PropertyGroup>
  <PropertyGroup Condition="'$(DotNetTool)' == ''">
    <!-- Respect environment variable for the .NET install directory if set; otherwise, use the repo default location -->
    <DotNetRoot Condition="'$(DOTNET_INSTALL_DIR)' != ''">$(DOTNET_INSTALL_DIR)</DotNetRoot>
    <DotNetRoot Condition="'$(DotNetRoot)' != ''">$([MSBuild]::NormalizeDirectory('$(DotNetRoot)'))</DotNetRoot>
    <DotNetRoot Condition="'$(DotNetRoot)' == ''">$([MSBuild]::NormalizeDirectory('$(RepoRoot)', '.dotnet'))</DotNetRoot>
    <!-- Let the exec task find dotnet on PATH -->
    <DotNetRoot Condition="!Exists($(DotNetRoot))" />
    <DotNetTool Condition="'$(OS)' == 'Windows_NT'">$(DotNetRoot)dotnet.exe</DotNetTool>
    <DotNetTool Condition="'$(OS)' != 'Windows_NT'">$(DotNetRoot)dotnet</DotNetTool>
  </PropertyGroup>
  <PropertyGroup Condition="'$(MonoTool)' == ''">
    <MonoTool>mono</MonoTool>
  </PropertyGroup>
  <PropertyGroup>
    <RepositoryEngineeringDir>$([MSBuild]::NormalizeDirectory('$(RepoRoot)', 'eng'))</RepositoryEngineeringDir>
    <RepositoryToolsDir>$([MSBuild]::NormalizeDirectory('$(RepoRoot)', '.tools'))</RepositoryToolsDir>
    <VersionsPropsPath>$(RepositoryEngineeringDir)Versions.props</VersionsPropsPath>
    <ArtifactsDir Condition="'$(ArtifactsDir)' == ''">$([MSBuild]::NormalizeDirectory('$(RepoRoot)', 'artifacts'))</ArtifactsDir>
    <ArtifactsToolsetDir>$([MSBuild]::NormalizeDirectory('$(ArtifactsDir)', 'toolset'))</ArtifactsToolsetDir>
    <ArtifactsObjDir>$([MSBuild]::NormalizeDirectory('$(ArtifactsDir)', 'obj'))</ArtifactsObjDir>
    <ArtifactsBinDir>$([MSBuild]::NormalizeDirectory('$(ArtifactsDir)', 'bin'))</ArtifactsBinDir>
    <ArtifactsLogDir>$([MSBuild]::NormalizeDirectory('$(ArtifactsDir)', 'log', '$(Configuration)'))</ArtifactsLogDir>
    <ArtifactsLogNgenDir>$([MSBuild]::NormalizeDirectory('$(ArtifactsLogDir)', 'ngen'))</ArtifactsLogNgenDir>
    <ArtifactsTmpDir>$([MSBuild]::NormalizeDirectory('$(ArtifactsDir)', 'tmp', '$(Configuration)'))</ArtifactsTmpDir>
    <ArtifactsTestResultsDir>$([MSBuild]::NormalizeDirectory('$(ArtifactsDir)', 'TestResults', '$(Configuration)'))</ArtifactsTestResultsDir>
    <ArtifactsSymStoreDirectory>$([MSBuild]::NormalizeDirectory('$(ArtifactsDir)', 'SymStore', '$(Configuration)'))</ArtifactsSymStoreDirectory>
    <ArtifactsPackagesDir>$([MSBuild]::NormalizeDirectory('$(ArtifactsDir)', 'packages', '$(Configuration)'))</ArtifactsPackagesDir>
    <ArtifactsShippingPackagesDir>$([MSBuild]::NormalizeDirectory('$(ArtifactsPackagesDir)', 'Shipping'))</ArtifactsShippingPackagesDir>
    <ArtifactsNonShippingPackagesDir>$([MSBuild]::NormalizeDirectory('$(ArtifactsPackagesDir)', 'NonShipping'))</ArtifactsNonShippingPackagesDir>
    <VisualStudioSetupOutputPath>$([MSBuild]::NormalizeDirectory('$(ArtifactsDir)', 'VSSetup', '$(Configuration)'))</VisualStudioSetupOutputPath>
    <VisualStudioSetupInsertionPath>$([MSBuild]::NormalizeDirectory('$(VisualStudioSetupOutputPath)', 'Insertion'))</VisualStudioSetupInsertionPath>
    <VisualStudioSetupIntermediateOutputPath>$([MSBuild]::NormalizeDirectory('$(ArtifactsDir)', 'VSSetup.obj', '$(Configuration)'))</VisualStudioSetupIntermediateOutputPath>
    <VisualStudioBuildPackagesDir>$([MSBuild]::NormalizeDirectory('$(VisualStudioSetupOutputPath)', 'DevDivPackages'))</VisualStudioBuildPackagesDir>
  </PropertyGroup>
  <!--
============================================================================================================================================
  </Import>

C:\.tools\.nuget\packages\microsoft.dotnet.arcade.sdk\8.0.0-beta.24426.2\tools\ProjectLayout.props
============================================================================================================================================
-->
  <PropertyGroup>
    <OutDirName Condition="'$(OutDirName)' == ''">$(MSBuildProjectName)</OutDirName>
    <BaseOutputPath Condition="'$(BaseOutputPath)' == ''">$([System.IO.Path]::GetFullPath('$(ArtifactsBinDir)$(OutDirName)\'))</BaseOutputPath>
    <OutputPath Condition="'$(PlatformName)' == 'AnyCPU'">$(BaseOutputPath)$(Configuration)\</OutputPath>
    <OutputPath Condition="'$(PlatformName)' != 'AnyCPU'">$(BaseOutputPath)$(PlatformName)\$(Configuration)\</OutputPath>
    <BaseIntermediateOutputPath Condition="'$(BaseIntermediateOutputPath)' == ''">$([System.IO.Path]::GetFullPath('$(ArtifactsObjDir)$(OutDirName)\'))</BaseIntermediateOutputPath>
    <IntermediateOutputPath Condition="'$(PlatformName)' == 'AnyCPU'">$(BaseIntermediateOutputPath)$(Configuration)\</IntermediateOutputPath>
    <IntermediateOutputPath Condition="'$(PlatformName)' != 'AnyCPU'">$(BaseIntermediateOutputPath)$(PlatformName)\$(Configuration)\</IntermediateOutputPath>
  </PropertyGroup>
  <!--
============================================================================================================================================
  </Import>

C:\.tools\.nuget\packages\microsoft.dotnet.arcade.sdk\8.0.0-beta.24426.2\tools\Settings.props
============================================================================================================================================
-->
  <!--
============================================================================================================================================
  <Import Project="DefaultVersions.props">

C:\.tools\.nuget\packages\microsoft.dotnet.arcade.sdk\8.0.0-beta.24426.2\tools\DefaultVersions.props
============================================================================================================================================
-->
  <!-- Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. -->
  <!--
    Sets versions of packages used by the toolset unless they were already specified by the repo.
    Version.props shall be imported prior this file.
  -->
  <PropertyGroup>
    <OfficialBuild>false</OfficialBuild>
    <OfficialBuild Condition="'$(OfficialBuildId)' != ''">true</OfficialBuild>
  </PropertyGroup>
  <!--
============================================================================================================================================
  <Import Project="DefaultVersions.Generated.props">

C:\.tools\.nuget\packages\microsoft.dotnet.arcade.sdk\8.0.0-beta.24426.2\tools\DefaultVersions.Generated.props
============================================================================================================================================
-->
  <!-- Generated by Microsoft.DotNet.Arcade.Sdk.csproj -->
  <PropertyGroup>
    <ArcadeSdkVersion>8.0.0-beta.24426.2</ArcadeSdkVersion>
    <MicrosoftNetCompilersToolsetVersion>4.6.0</MicrosoftNetCompilersToolsetVersion>
    <MicrosoftNetILLinkTasksVersion>6.0.100-1.22103.2</MicrosoftNetILLinkTasksVersion>
    <MicrosoftDiaSymReaderPdb2PdbVersion>1.1.0-beta2-19575-01</MicrosoftDiaSymReaderPdb2PdbVersion>
    <MicrosoftDotNetXliffTasksVersion>1.0.0-beta.23475.1</MicrosoftDotNetXliffTasksVersion>
    <MicrosoftDotNetMaestroTasksVersion>1.1.0-beta.24321.1</MicrosoftDotNetMaestroTasksVersion>
    <MicrosoftSymbolUploaderBuildTaskVersion>2.0.0-preview.1.23470.14</MicrosoftSymbolUploaderBuildTaskVersion>
    <MicrosoftTemplateEngineAuthoringTasksVersion>8.0.100-rtm.23479.1</MicrosoftTemplateEngineAuthoringTasksVersion>
    <MicrosoftDotNetXUnitAssertVersion>8.0.0-beta.24426.2</MicrosoftDotNetXUnitAssertVersion>
  </PropertyGroup>
  <!--
============================================================================================================================================
  </Import>

C:\.tools\.nuget\packages\microsoft.dotnet.arcade.sdk\8.0.0-beta.24426.2\tools\DefaultVersions.props
============================================================================================================================================
-->
  <!--
============================================================================================================================================
  <Import Project="$(VersionsPropsPath)" Condition="Exists('$(VersionsPropsPath)')">

C:\Users\calope\source\repos\runtime8\eng\Versions.props
============================================================================================================================================
-->
  <PropertyGroup>
    <!-- The .NET product branding version -->
    <ProductVersion>8.0.10</ProductVersion>
    <!-- File version numbers -->
    <MajorVersion>8</MajorVersion>
    <MinorVersion>0</MinorVersion>
    <PatchVersion>10</PatchVersion>
    <SdkBandVersion>8.0.100</SdkBandVersion>
    <PackageVersionNet7>7.0.20</PackageVersionNet7>
    <PackageVersionNet6>6.0.$([MSBuild]::Add($(PatchVersion),25))</PackageVersionNet6>
    <PreReleaseVersionLabel>servicing</PreReleaseVersionLabel>
    <PreReleaseVersionIteration />
    <!-- Enable to remove prerelease label. -->
    <StabilizePackageVersion Condition="'$(StabilizePackageVersion)' == ''">true</StabilizePackageVersion>
    <DotNetFinalVersionKind Condition="'$(StabilizePackageVersion)' == 'true'">release</DotNetFinalVersionKind>
    <WorkloadVersionSuffix Condition="'$(DotNetFinalVersionKind)' != 'release' and '$(PreReleaseVersionIteration)' == ''">-$(PreReleaseVersionLabel)</WorkloadVersionSuffix>
    <WorkloadVersionSuffix Condition="'$(WorkloadVersionSuffix)' == '' and '$(DotNetFinalVersionKind)' != 'release'">-$(PreReleaseVersionLabel).$(PreReleaseVersionIteration)</WorkloadVersionSuffix>
    <SdkBandVersionForWorkload_FromRuntimeVersions>$(SdkBandVersion)$(WorkloadVersionSuffix)</SdkBandVersionForWorkload_FromRuntimeVersions>
    <!-- set to false for release branches -->
    <WorkloadsTestPreviousVersions Condition="'$(WorkloadsTestPreviousVersions)' == ''">false</WorkloadsTestPreviousVersions>
    <!-- Set assembly version to align with major and minor version,
         as for the patches and revisions should be manually updated per assembly if it is serviced. -->
    <AssemblyVersion>$(MajorVersion).$(MinorVersion).0.0</AssemblyVersion>
    <!-- Opt-in/out repo features -->
    <UsingToolMicrosoftNetILLinkTasks Condition="'$(UsingToolMicrosoftNetILLinkTasks)' == ''">true</UsingToolMicrosoftNetILLinkTasks>
    <UsingToolIbcOptimization>false</UsingToolIbcOptimization>
    <UsingToolXliff>false</UsingToolXliff>
    <LastReleasedStableAssemblyVersion>$(AssemblyVersion)</LastReleasedStableAssemblyVersion>
    <!-- Use SDK compilers in full source-build. -->
    <UsingToolMicrosoftNetCompilers Condition="'$(DotNetBuildFromSourceFlavor)' != 'Product'">true</UsingToolMicrosoftNetCompilers>
  </PropertyGroup>
  <ItemGroup>
    <WorkloadSdkBandVersions Include="$(SdkBandVersion)" SupportsMachineArch="true" />
  </ItemGroup>
  <PropertyGroup>
    <!-- dotnet/roslyn-analyzers dependencies -->
    <MicrosoftCodeAnalysisAnalyzersVersion>3.11.0-beta1.23614.1</MicrosoftCodeAnalysisAnalyzersVersion>
    <MicrosoftCodeAnalysisNetAnalyzersVersion>8.0.0-preview.23614.1</MicrosoftCodeAnalysisNetAnalyzersVersion>
    <!-- dotnet/roslyn dependencies -->
    <!--
      These versions should not be used by any project that contributes to the design-time experience in VS, such as an analyzer, code-fix, or generator assembly.
      Any tools that contribute to the design-time experience should use the MicrosoftCodeAnalysisVersion_LatestVS property above to ensure
      they do not break the local dev experience.
    -->
    <MicrosoftCodeAnalysisCSharpVersion>4.8.0-7.23566.2</MicrosoftCodeAnalysisCSharpVersion>
    <MicrosoftCodeAnalysisVersion>4.8.0-7.23566.2</MicrosoftCodeAnalysisVersion>
    <MicrosoftNetCompilersToolsetVersion>4.8.0-7.23566.2</MicrosoftNetCompilersToolsetVersion>
  </PropertyGroup>
  <!--
    For source generator support we need to target multiple versions of Roslyn in order to be able to run on older versions of Roslyn.
    We pin these versions as we need to match them exactly for any scenarios that run Roslyn on .NET Framework, like Visual Studio.
  -->
  <PropertyGroup>
    <!-- Compatibility with VS 16.11/.NET SDK 5.0.4xx -->
    <MicrosoftCodeAnalysisVersion_3_11>3.11.0</MicrosoftCodeAnalysisVersion_3_11>
    <!-- Compatibility with VS 17.0/.NET SDK 6.0.1xx  -->
    <MicrosoftCodeAnalysisVersion_4_0>4.0.1</MicrosoftCodeAnalysisVersion_4_0>
    <!-- Compatibility with VS 17.4/.NET SDK 7.0.1xx -->
    <!--
      The exact version is a moving target until we ship.
      It should never go ahead of the Roslyn version included in the SDK version in dotnet/arcade's global.json to avoid causing breaks in product construction.
    -->
    <MicrosoftCodeAnalysisVersion_4_4>4.4.0</MicrosoftCodeAnalysisVersion_4_4>
    <!-- Compatibility with the latest Visual Studio Preview release -->
    <!--
      The exact version is always a moving target. This version should never go ahead of the version of Roslyn that is included in the most recent
      public Visual Studio preview version. If it were to go ahead, then any components depending on this version would not work in Visual Studio
      and would cause a major regression for any local development that depends on those components contributing to the build.
      This version must also not go ahead of the most recently release .NET SDK version, as that would break the source-build build.
      Source-build builds the product with the most recent previously source-built release. Thankfully, these two requirements line up nicely
      such that any version that satisfies the VS version requirement will also satisfy the .NET SDK version requirement because of how we ship.
    -->
    <MicrosoftCodeAnalysisVersion_LatestVS>4.5.0</MicrosoftCodeAnalysisVersion_LatestVS>
    <!-- Some of the analyzer dependencies used by ILLink project -->
    <MicrosoftCodeAnalysisBannedApiAnalyzersVersion>3.3.5-beta1.23270.2</MicrosoftCodeAnalysisBannedApiAnalyzersVersion>
  </PropertyGroup>
  <!--
    These packages affect the design-time experience in VS, so we update them at the same cadance as the MicrosoftCodeAnalysisVersion_LatestVS version.
  -->
  <PropertyGroup>
    <MicrosoftCodeAnalysisCSharpCodeStyleVersion>$(MicrosoftCodeAnalysisVersion_LatestVS)</MicrosoftCodeAnalysisCSharpCodeStyleVersion>
  </PropertyGroup>
  <PropertyGroup>
    <StaticCsVersion>0.2.0</StaticCsVersion>
    <!-- SDK dependencies -->
    <MicrosoftDotNetApiCompatTaskVersion>8.0.100</MicrosoftDotNetApiCompatTaskVersion>
    <!-- Arcade dependencies -->
    <MicrosoftDotNetBuildTasksFeedVersion>8.0.0-beta.24426.2</MicrosoftDotNetBuildTasksFeedVersion>
    <MicrosoftDotNetCodeAnalysisVersion>8.0.0-beta.24426.2</MicrosoftDotNetCodeAnalysisVersion>
    <MicrosoftDotNetGenAPIVersion>8.0.0-beta.24426.2</MicrosoftDotNetGenAPIVersion>
    <MicrosoftDotNetGenFacadesVersion>8.0.0-beta.24426.2</MicrosoftDotNetGenFacadesVersion>
    <MicrosoftDotNetXUnitExtensionsVersion>8.0.0-beta.24426.2</MicrosoftDotNetXUnitExtensionsVersion>
    <MicrosoftDotNetXUnitConsoleRunnerVersion>2.5.1-beta.24426.2</MicrosoftDotNetXUnitConsoleRunnerVersion>
    <MicrosoftDotNetBuildTasksArchivesVersion>8.0.0-beta.24426.2</MicrosoftDotNetBuildTasksArchivesVersion>
    <MicrosoftDotNetBuildTasksInstallersVersion>8.0.0-beta.24426.2</MicrosoftDotNetBuildTasksInstallersVersion>
    <MicrosoftDotNetBuildTasksPackagingVersion>8.0.0-beta.24426.2</MicrosoftDotNetBuildTasksPackagingVersion>
    <MicrosoftDotNetBuildTasksTargetFrameworkVersion>8.0.0-beta.24426.2</MicrosoftDotNetBuildTasksTargetFrameworkVersion>
    <MicrosoftDotNetBuildTasksTemplatingVersion>8.0.0-beta.24426.2</MicrosoftDotNetBuildTasksTemplatingVersion>
    <MicrosoftDotNetBuildTasksWorkloadsPackageVersion>8.0.0-beta.24426.2</MicrosoftDotNetBuildTasksWorkloadsPackageVersion>
    <MicrosoftDotNetRemoteExecutorVersion>8.0.0-beta.24426.2</MicrosoftDotNetRemoteExecutorVersion>
    <MicrosoftDotNetVersionToolsTasksVersion>8.0.0-beta.24426.2</MicrosoftDotNetVersionToolsTasksVersion>
    <MicrosoftDotNetPackageTestingVersion>8.0.0-beta.24426.2</MicrosoftDotNetPackageTestingVersion>
    <!-- NuGet dependencies -->
    <NuGetBuildTasksPackVersion>6.0.0-preview.1.102</NuGetBuildTasksPackVersion>
    <!-- Installer dependencies -->
    <MicrosoftNETCoreAppRuntimewinx64Version>8.0.0-rc.1.23406.6</MicrosoftNETCoreAppRuntimewinx64Version>
    <MicrosoftExtensionsDependencyModelVersion>6.0.0</MicrosoftExtensionsDependencyModelVersion>
    <!-- CoreClr dependencies -->
    <MicrosoftNETCoreILAsmVersion>8.0.0-rc.1.23406.6</MicrosoftNETCoreILAsmVersion>
    <MicrosoftNETCoreILAsmVersion>8.0.0-preview.7.23325.2</MicrosoftNETCoreILAsmVersion>
    <runtimelinuxarm64MicrosoftNETCoreRuntimeObjWriterVersion>16.0.5-alpha.1.23566.1</runtimelinuxarm64MicrosoftNETCoreRuntimeObjWriterVersion>
    <runtimelinuxx64MicrosoftNETCoreRuntimeObjWriterVersion>16.0.5-alpha.1.23566.1</runtimelinuxx64MicrosoftNETCoreRuntimeObjWriterVersion>
    <runtimelinuxmuslarm64MicrosoftNETCoreRuntimeObjWriterVersion>16.0.5-alpha.1.23566.1</runtimelinuxmuslarm64MicrosoftNETCoreRuntimeObjWriterVersion>
    <runtimelinuxmuslx64MicrosoftNETCoreRuntimeObjWriterVersion>16.0.5-alpha.1.23566.1</runtimelinuxmuslx64MicrosoftNETCoreRuntimeObjWriterVersion>
    <runtimewinarm64MicrosoftNETCoreRuntimeObjWriterVersion>16.0.5-alpha.1.23566.1</runtimewinarm64MicrosoftNETCoreRuntimeObjWriterVersion>
    <runtimewinx64MicrosoftNETCoreRuntimeObjWriterVersion>16.0.5-alpha.1.23566.1</runtimewinx64MicrosoftNETCoreRuntimeObjWriterVersion>
    <runtimeosxarm64MicrosoftNETCoreRuntimeObjWriterVersion>16.0.5-alpha.1.23566.1</runtimeosxarm64MicrosoftNETCoreRuntimeObjWriterVersion>
    <runtimeosxx64MicrosoftNETCoreRuntimeObjWriterVersion>16.0.5-alpha.1.23566.1</runtimeosxx64MicrosoftNETCoreRuntimeObjWriterVersion>
    <!-- Libraries dependencies -->
    <MicrosoftBclAsyncInterfacesVersion>6.0.0</MicrosoftBclAsyncInterfacesVersion>
    <MicrosoftBclHashCodeVersion>1.1.1</MicrosoftBclHashCodeVersion>
    <MicrosoftWin32RegistryVersion>5.0.0</MicrosoftWin32RegistryVersion>
    <StyleCopAnalyzersVersion>1.2.0-beta.406</StyleCopAnalyzersVersion>
    <SystemBuffersVersion>4.5.1</SystemBuffersVersion>
    <SystemCollectionsImmutableVersion>7.0.0</SystemCollectionsImmutableVersion>
    <SystemComponentModelAnnotationsVersion>5.0.0</SystemComponentModelAnnotationsVersion>
    <SystemDataSqlClientVersion>4.8.6</SystemDataSqlClientVersion>
    <SystemDrawingCommonVersion>8.0.0</SystemDrawingCommonVersion>
    <SystemIOFileSystemAccessControlVersion>5.0.0</SystemIOFileSystemAccessControlVersion>
    <SystemMemoryVersion>4.5.5</SystemMemoryVersion>
    <SystemReflectionMetadataVersion>7.0.0</SystemReflectionMetadataVersion>
    <SystemSecurityAccessControlVersion>6.0.0</SystemSecurityAccessControlVersion>
    <SystemSecurityCryptographyCngVersion>5.0.0</SystemSecurityCryptographyCngVersion>
    <SystemSecurityCryptographyOpenSslVersion>5.0.0</SystemSecurityCryptographyOpenSslVersion>
    <SystemSecurityPrincipalWindowsVersion>5.0.0</SystemSecurityPrincipalWindowsVersion>
    <SystemSecurityPermissionsVersion>7.0.0</SystemSecurityPermissionsVersion>
    <!-- The JSON version that's present in minimum MSBuild / VS version that this release is supported on -->
    <SystemTextJsonToolsetVersion>7.0.3</SystemTextJsonToolsetVersion>
    <SystemTextJsonVersion>8.0.0-rc.1.23406.6</SystemTextJsonVersion>
    <SystemRuntimeCompilerServicesUnsafeVersion>6.0.0</SystemRuntimeCompilerServicesUnsafeVersion>
    <SystemThreadingAccessControlVersion>7.0.0</SystemThreadingAccessControlVersion>
    <SystemThreadingTasksExtensionsVersion>4.5.4</SystemThreadingTasksExtensionsVersion>
    <SystemValueTupleVersion>4.5.0</SystemValueTupleVersion>
    <runtimenativeSystemIOPortsVersion>8.0.0-rc.1.23406.6</runtimenativeSystemIOPortsVersion>
    <!-- Runtime-Assets dependencies -->
    <SystemRuntimeNumericsTestDataVersion>8.0.0-beta.24362.2</SystemRuntimeNumericsTestDataVersion>
    <SystemComponentModelTypeConverterTestDataVersion>8.0.0-beta.24362.2</SystemComponentModelTypeConverterTestDataVersion>
    <SystemDataCommonTestDataVersion>8.0.0-beta.24362.2</SystemDataCommonTestDataVersion>
    <SystemDrawingCommonTestDataVersion>8.0.0-beta.24362.2</SystemDrawingCommonTestDataVersion>
    <SystemFormatsTarTestDataVersion>8.0.0-beta.24362.2</SystemFormatsTarTestDataVersion>
    <SystemIOCompressionTestDataVersion>8.0.0-beta.24362.2</SystemIOCompressionTestDataVersion>
    <SystemIOPackagingTestDataVersion>8.0.0-beta.24362.2</SystemIOPackagingTestDataVersion>
    <SystemNetTestDataVersion>8.0.0-beta.24362.2</SystemNetTestDataVersion>
    <SystemPrivateRuntimeUnicodeDataVersion>8.0.0-beta.24362.2</SystemPrivateRuntimeUnicodeDataVersion>
    <SystemRuntimeTimeZoneDataVersion>8.0.0-beta.24362.2</SystemRuntimeTimeZoneDataVersion>
    <SystemSecurityCryptographyX509CertificatesTestDataVersion>8.0.0-beta.24362.2</SystemSecurityCryptographyX509CertificatesTestDataVersion>
    <SystemTextRegularExpressionsTestDataVersion>8.0.0-beta.24362.2</SystemTextRegularExpressionsTestDataVersion>
    <SystemWindowsExtensionsTestDataVersion>8.0.0-beta.24362.2</SystemWindowsExtensionsTestDataVersion>
    <MicrosoftDotNetCilStripSourcesVersion>8.0.0-beta.24362.2</MicrosoftDotNetCilStripSourcesVersion>
    <!-- dotnet-optimization dependencies -->
    <optimizationwindows_ntx64MIBCRuntimeVersion>1.0.0-prerelease.23566.3</optimizationwindows_ntx64MIBCRuntimeVersion>
    <optimizationwindows_ntx86MIBCRuntimeVersion>1.0.0-prerelease.23566.3</optimizationwindows_ntx86MIBCRuntimeVersion>
    <optimizationwindows_ntarm64MIBCRuntimeVersion>1.0.0-prerelease.23566.3</optimizationwindows_ntarm64MIBCRuntimeVersion>
    <optimizationlinuxx64MIBCRuntimeVersion>1.0.0-prerelease.23566.3</optimizationlinuxx64MIBCRuntimeVersion>
    <optimizationlinuxarm64MIBCRuntimeVersion>1.0.0-prerelease.23566.3</optimizationlinuxarm64MIBCRuntimeVersion>
    <optimizationPGOCoreCLRVersion>1.0.0-prerelease.23566.3</optimizationPGOCoreCLRVersion>
    <!-- Not auto-updated. -->
    <MicrosoftDiaSymReaderNativeVersion>17.10.0-beta1.24272.1</MicrosoftDiaSymReaderNativeVersion>
    <SystemCommandLineVersion>2.0.0-beta4.23307.1</SystemCommandLineVersion>
    <TraceEventVersion>3.0.3</TraceEventVersion>
    <NETStandardLibraryRefVersion>2.1.0</NETStandardLibraryRefVersion>
    <NetStandardLibraryVersion>2.0.3</NetStandardLibraryVersion>
    <MicrosoftDiagnosticsToolsRuntimeClientVersion>1.0.4-preview6.19326.1</MicrosoftDiagnosticsToolsRuntimeClientVersion>
    <DNNEVersion>2.0.5</DNNEVersion>
    <MicrosoftBuildVersion>17.8.3</MicrosoftBuildVersion>
    <MicrosoftBuildTasksCoreVersion>$(MicrosoftBuildVersion)</MicrosoftBuildTasksCoreVersion>
    <MicrosoftBuildFrameworkVersion>$(MicrosoftBuildVersion)</MicrosoftBuildFrameworkVersion>
    <MicrosoftBuildUtilitiesCoreVersion>$(MicrosoftBuildVersion)</MicrosoftBuildUtilitiesCoreVersion>
    <NugetProjectModelVersion>6.2.4</NugetProjectModelVersion>
    <NugetPackagingVersion>6.2.4</NugetPackagingVersion>
    <DotnetSosVersion>7.0.412701</DotnetSosVersion>
    <DotnetSosTargetFrameworkVersion>6.0</DotnetSosTargetFrameworkVersion>
    <!-- Testing -->
    <MicrosoftNETCoreCoreDisToolsVersion>1.1.0</MicrosoftNETCoreCoreDisToolsVersion>
    <MicrosoftNETTestSdkVersion>17.4.0-preview-20220707-01</MicrosoftNETTestSdkVersion>
    <MicrosoftDotNetXHarnessTestRunnersCommonVersion>8.0.0-prerelease.24229.2</MicrosoftDotNetXHarnessTestRunnersCommonVersion>
    <MicrosoftDotNetXHarnessTestRunnersXunitVersion>8.0.0-prerelease.24229.2</MicrosoftDotNetXHarnessTestRunnersXunitVersion>
    <MicrosoftDotNetXHarnessCLIVersion>8.0.0-prerelease.24229.2</MicrosoftDotNetXHarnessCLIVersion>
    <MicrosoftDotNetHotReloadUtilsGeneratorBuildToolVersion>8.0.0-alpha.0.24453.2</MicrosoftDotNetHotReloadUtilsGeneratorBuildToolVersion>
    <XUnitVersion>2.4.2</XUnitVersion>
    <XUnitAnalyzersVersion>1.0.0</XUnitAnalyzersVersion>
    <XUnitRunnerVisualStudioVersion>2.4.5</XUnitRunnerVisualStudioVersion>
    <NUnitVersion>3.12.0</NUnitVersion>
    <NUnitTestAdapterVersion>4.1.0</NUnitTestAdapterVersion>
    <CoverletCollectorVersion>6.0.0</CoverletCollectorVersion>
    <NewtonsoftJsonVersion>13.0.3</NewtonsoftJsonVersion>
    <NewtonsoftJsonBsonVersion>1.0.2</NewtonsoftJsonBsonVersion>
    <SQLitePCLRawbundle_greenVersion>2.0.4</SQLitePCLRawbundle_greenVersion>
    <MoqVersion>4.18.4</MoqVersion>
    <FluentAssertionsVersion>6.7.0</FluentAssertionsVersion>
    <FsCheckVersion>2.14.3</FsCheckVersion>
    <!-- Android gRPC client tests -->
    <GoogleProtobufVersion>3.19.4</GoogleProtobufVersion>
    <GrpcAspNetCoreVersion>2.46.0</GrpcAspNetCoreVersion>
    <GrpcAspNetCoreWebVersion>2.46.0</GrpcAspNetCoreWebVersion>
    <GrpcAuthVersion>2.46.3</GrpcAuthVersion>
    <GrpcCoreVersion>2.46.3</GrpcCoreVersion>
    <GrpcDotnetClientVersion>2.45.0</GrpcDotnetClientVersion>
    <GrpcToolsVersion>2.45.0</GrpcToolsVersion>
    <CompilerPlatformTestingVersion>1.1.2-beta1.23323.1</CompilerPlatformTestingVersion>
    <!-- Docs -->
    <MicrosoftPrivateIntellisenseVersion>8.0.0-preview-20230918.1</MicrosoftPrivateIntellisenseVersion>
    <!-- ILLink -->
    <MicrosoftNETILLinkTasksVersion>8.0.0-rc.1.23406.6</MicrosoftNETILLinkTasksVersion>
    <!-- Mono Cecil -->
    <MicrosoftDotNetCecilVersion>0.11.4-alpha.23509.2</MicrosoftDotNetCecilVersion>
    <!-- ILCompiler -->
    <MicrosoftDotNetILCompilerVersion>8.0.0-rc.1.23406.6</MicrosoftDotNetILCompilerVersion>
    <!-- ICU -->
    <MicrosoftNETCoreRuntimeICUTransportVersion>8.0.0-rtm.23523.2</MicrosoftNETCoreRuntimeICUTransportVersion>
    <!-- MsQuic -->
    <MicrosoftNativeQuicMsQuicSchannelVersion>2.3.5</MicrosoftNativeQuicMsQuicSchannelVersion>
    <SystemNetMsQuicTransportVersion>8.0.0-alpha.1.23527.1</SystemNetMsQuicTransportVersion>
    <!-- Mono LLVM -->
    <runtimelinuxarm64MicrosoftNETCoreRuntimeMonoLLVMSdkVersion>16.0.5-alpha.1.23566.1</runtimelinuxarm64MicrosoftNETCoreRuntimeMonoLLVMSdkVersion>
    <runtimelinuxarm64MicrosoftNETCoreRuntimeMonoLLVMToolsVersion>16.0.5-alpha.1.23566.1</runtimelinuxarm64MicrosoftNETCoreRuntimeMonoLLVMToolsVersion>
    <runtimelinuxmuslarm64MicrosoftNETCoreRuntimeMonoLLVMSdkVersion>16.0.5-alpha.1.23566.1</runtimelinuxmuslarm64MicrosoftNETCoreRuntimeMonoLLVMSdkVersion>
    <runtimelinuxmuslarm64MicrosoftNETCoreRuntimeMonoLLVMToolsVersion>16.0.5-alpha.1.23566.1</runtimelinuxmuslarm64MicrosoftNETCoreRuntimeMonoLLVMToolsVersion>
    <runtimelinuxx64MicrosoftNETCoreRuntimeMonoLLVMSdkVersion>16.0.5-alpha.1.23566.1</runtimelinuxx64MicrosoftNETCoreRuntimeMonoLLVMSdkVersion>
    <runtimelinuxx64MicrosoftNETCoreRuntimeMonoLLVMToolsVersion>16.0.5-alpha.1.23566.1</runtimelinuxx64MicrosoftNETCoreRuntimeMonoLLVMToolsVersion>
    <runtimelinuxmuslx64MicrosoftNETCoreRuntimeMonoLLVMSdkVersion>16.0.5-alpha.1.23566.1</runtimelinuxmuslx64MicrosoftNETCoreRuntimeMonoLLVMSdkVersion>
    <runtimelinuxmuslx64MicrosoftNETCoreRuntimeMonoLLVMToolsVersion>16.0.5-alpha.1.23566.1</runtimelinuxmuslx64MicrosoftNETCoreRuntimeMonoLLVMToolsVersion>
    <runtimewinx64MicrosoftNETCoreRuntimeMonoLLVMSdkVersion>16.0.5-alpha.1.23566.1</runtimewinx64MicrosoftNETCoreRuntimeMonoLLVMSdkVersion>
    <runtimewinx64MicrosoftNETCoreRuntimeMonoLLVMToolsVersion>16.0.5-alpha.1.23566.1</runtimewinx64MicrosoftNETCoreRuntimeMonoLLVMToolsVersion>
    <runtimeosxarm64MicrosoftNETCoreRuntimeMonoLLVMSdkVersion>16.0.5-alpha.1.23566.1</runtimeosxarm64MicrosoftNETCoreRuntimeMonoLLVMSdkVersion>
    <runtimeosxarm64MicrosoftNETCoreRuntimeMonoLLVMToolsVersion>16.0.5-alpha.1.23566.1</runtimeosxarm64MicrosoftNETCoreRuntimeMonoLLVMToolsVersion>
    <runtimeosxx64MicrosoftNETCoreRuntimeMonoLLVMSdkVersion>16.0.5-alpha.1.23566.1</runtimeosxx64MicrosoftNETCoreRuntimeMonoLLVMSdkVersion>
    <runtimeosxx64MicrosoftNETCoreRuntimeMonoLLVMToolsVersion>16.0.5-alpha.1.23566.1</runtimeosxx64MicrosoftNETCoreRuntimeMonoLLVMToolsVersion>
    <!-- emscripten / Node
         Note: when the name is updated, make sure to update dependency name in eng/pipelines/common/xplat-setup.yml
               like - DarcDependenciesChanged.Microsoft_NET_Workload_Emscripten_Current_Manifest-8_0_100_Transport
    -->
    <MicrosoftNETWorkloadEmscriptenCurrentManifest80100Version>8.0.10</MicrosoftNETWorkloadEmscriptenCurrentManifest80100Version>
    <MicrosoftNETRuntimeEmscriptenVersion>$(MicrosoftNETWorkloadEmscriptenCurrentManifest80100Version)</MicrosoftNETRuntimeEmscriptenVersion>
    <!-- workloads -->
    <SwixPackageVersion>1.1.87-gba258badda</SwixPackageVersion>
    <!-- JIT Tools -->
    <runtimelinuxarm64MicrosoftNETCoreRuntimeJITToolsVersion>16.0.5-alpha.1.23566.1</runtimelinuxarm64MicrosoftNETCoreRuntimeJITToolsVersion>
    <runtimelinuxx64MicrosoftNETCoreRuntimeJITToolsVersion>16.0.5-alpha.1.23566.1</runtimelinuxx64MicrosoftNETCoreRuntimeJITToolsVersion>
    <runtimelinuxmuslarm64MicrosoftNETCoreRuntimeJITToolsVersion>16.0.5-alpha.1.23566.1</runtimelinuxmuslarm64MicrosoftNETCoreRuntimeJITToolsVersion>
    <runtimelinuxmuslx64MicrosoftNETCoreRuntimeJITToolsVersion>16.0.5-alpha.1.23566.1</runtimelinuxmuslx64MicrosoftNETCoreRuntimeJITToolsVersion>
    <runtimewinarm64MicrosoftNETCoreRuntimeJITToolsVersion>16.0.5-alpha.1.23566.1</runtimewinarm64MicrosoftNETCoreRuntimeJITToolsVersion>
    <runtimewinx64MicrosoftNETCoreRuntimeJITToolsVersion>16.0.5-alpha.1.23566.1</runtimewinx64MicrosoftNETCoreRuntimeJITToolsVersion>
    <runtimeosxarm64MicrosoftNETCoreRuntimeJITToolsVersion>16.0.5-alpha.1.23566.1</runtimeosxarm64MicrosoftNETCoreRuntimeJITToolsVersion>
    <runtimeosxx64MicrosoftNETCoreRuntimeJITToolsVersion>16.0.5-alpha.1.23566.1</runtimeosxx64MicrosoftNETCoreRuntimeJITToolsVersion>
    <!-- BrowserDebugProxy libs -->
    <MicrosoftExtensionsLoggingVersion>3.1.7</MicrosoftExtensionsLoggingVersion>
    <MicrosoftSymbolStoreVersion>1.0.406601</MicrosoftSymbolStoreVersion>
    <!-- installer version, for testing workloads must be greater than or equal to global.json sdk version -->
    <MicrosoftDotnetSdkInternalVersion>8.0.108</MicrosoftDotnetSdkInternalVersion>
    <SdkVersionForWorkloadTesting>$(MicrosoftDotnetSdkInternalVersion)</SdkVersionForWorkloadTesting>
  </PropertyGroup>
  <PropertyGroup>
    <!--
      Targeting pack package for NETStandard 2.1 gets rebuilt on demand.

      Set to rebuild with 8.0.8 release, and automatically disabled after that.
    -->
    <BuildNETStandard21TargetingPack Condition="'$(PatchVersion)' == '8'">true</BuildNETStandard21TargetingPack>
  </PropertyGroup>
  <!--
============================================================================================================================================
  </Import>

C:\.tools\.nuget\packages\microsoft.dotnet.arcade.sdk\8.0.0-beta.24426.2\tools\DefaultVersions.props
============================================================================================================================================
-->
  <!-- TODO: remove once all repos remove RestoreSources from their Version.props files -->
  <PropertyGroup>
    <RestoreSources />
  </PropertyGroup>
  <!--
    Prevent NuGet from using cached packages
    Workaround for https://github.com/NuGet/Home/issues/3116
  -->
  <PropertyGroup>
    <RestoreNoCache Condition="'$(ContinuousIntegrationBuild)' == 'true'">true</RestoreNoCache>
  </PropertyGroup>
  <!--
    Arcade SDK features.
  -->
  <PropertyGroup>
    <!-- Opt-out features -->
    <UsingToolXliff Condition="'$(UsingToolXliff)' == ''">true</UsingToolXliff>
    <UsingToolPdbConverter Condition="'$(UsingToolPdbConverter)' == ''">true</UsingToolPdbConverter>
    <!-- Opt-in features -->
    <UsingToolVSSDK Condition="'$(UsingToolVSSDK)' == ''">false</UsingToolVSSDK>
    <UsingToolIbcOptimization Condition="'$(UsingToolIbcOptimization)' == ''">false</UsingToolIbcOptimization>
    <UsingToolVisualStudioIbcTraining Condition="'$(UsingToolVisualStudioIbcTraining)' == ''">false</UsingToolVisualStudioIbcTraining>
    <UsingToolNuGetRepack Condition="'$(UsingToolNuGetRepack)' == ''">false</UsingToolNuGetRepack>
    <UsingToolSymbolUploader Condition="'$(UsingToolSymbolUploader)' == ''">false</UsingToolSymbolUploader>
    <!-- Deprecated features -->
    <!--
      The UsingToolXUnit property is deprecated,
      use the TestRunnerName property to choose which test runner to use.
    -->
    <UsingToolXUnit Condition="'$(UsingToolXUnit)' == ''">true</UsingToolXUnit>
    <!--
      Use compilers from the Microsoft.Net.Compilers/Microsoft.NETCore.Compilers package.
      Repo can set this property to true if it needs to use a different version of the compiler than the one in the dotnet SDK.
    -->
    <UsingToolMicrosoftNetCompilers Condition="'$(UsingToolMicrosoftNetCompilers)' == ''">false</UsingToolMicrosoftNetCompilers>
    <!--
      Use IL linker from the Microsoft.NET.ILLink.Tasks package.
      Repo can set this property to true if it needs to use a different version of the IL linker than the one in the dotnet SDK.
    -->
    <UsingToolMicrosoftNetILLinkTasks Condition="'$(UsingToolMicrosoftNetILLinkTasks)' == ''">false</UsingToolMicrosoftNetILLinkTasks>
  </PropertyGroup>
  <!--
    Disable features when building from source.
  -->
  <PropertyGroup Condition="'$(ArcadeBuildFromSource)' == 'true'">
    <UsingToolPdbConverter>false</UsingToolPdbConverter>
    <UsingToolVSSDK>false</UsingToolVSSDK>
    <UsingToolIbcOptimization>false</UsingToolIbcOptimization>
    <UsingToolVisualStudioIbcTraining>false</UsingToolVisualStudioIbcTraining>
    <UsingToolSymbolUploader>false</UsingToolSymbolUploader>
  </PropertyGroup>
  <!--
    Default versions
  -->
  <PropertyGroup>
    <DropAppVersion Condition="'$(DropAppVersion)' == ''">18.165.29912-buildid11693003</DropAppVersion>
    <MicroBuildPluginsSwixBuildVersion Condition="'$(MicroBuildPluginsSwixBuildVersion)' == ''">1.0.422</MicroBuildPluginsSwixBuildVersion>
    <MicroBuildCoreVersion Condition="'$(MicroBuildCoreVersion)' == ''">0.2.0</MicroBuildCoreVersion>
    <MicrosoftDotNetIBCMergeVersion Condition="'$(MicrosoftDotNetIBCMergeVersion)' == ''">5.1.0-beta.21356.1</MicrosoftDotNetIBCMergeVersion>
    <MicrosoftNETTestSdkVersion Condition="'$(MicrosoftNETTestSdkVersion)' == ''">17.5.0</MicrosoftNETTestSdkVersion>
    <MicrosoftVSSDKBuildToolsVersion Condition="'$(MicrosoftVSSDKBuildToolsVersion)' == ''">16.9.1050</MicrosoftVSSDKBuildToolsVersion>
    <MicrosoftDotnetNuGetRepackTasksVersion Condition="'$(MicrosoftDotnetNuGetRepackTasksVersion)' == ''">$(ArcadeSdkVersion)</MicrosoftDotnetNuGetRepackTasksVersion>
    <MicrosoftDotNetSignToolVersion Condition="'$(MicrosoftDotNetSignToolVersion)' == ''">$(ArcadeSdkVersion)</MicrosoftDotNetSignToolVersion>
    <MicrosoftDotNetTarVersion Condition="'$(MicrosoftDotNetTarVersion)' == ''">$(ArcadeSdkVersion)</MicrosoftDotNetTarVersion>
    <MicrosoftTestPlatformVersion Condition="'$(MicrosoftTestPlatformVersion)' == ''">16.5.0</MicrosoftTestPlatformVersion>
    <XUnitVersion Condition="'$(XUnitVersion)' == ''">2.4.2</XUnitVersion>
    <XUnitAnalyzersVersion Condition="'$(XUnitAnalyzersVersion)' == ''">1.1.0</XUnitAnalyzersVersion>
    <XUnitRunnerConsoleVersion Condition="'$(XUnitRunnerConsoleVersion)' == ''">$(XUnitVersion)</XUnitRunnerConsoleVersion>
    <!-- Version 2.4.3 of xunit.runner.visualstudio was released to fix testing of net5 projects without updating any other xunit packages -->
    <XUnitRunnerVisualStudioVersion Condition="'$(XUnitRunnerVisualStudioVersion)' == ''">2.4.3</XUnitRunnerVisualStudioVersion>
    <MSTestVersion Condition="'$(MSTestVersion)' == ''">2.0.0</MSTestVersion>
    <MSTestTestAdapterVersion Condition="'$(MSTestTestAdapterVersion)' == ''">$(MSTestVersion)</MSTestTestAdapterVersion>
    <MSTestTestFrameworkVersion Condition="'$(MSTestTestFrameworkVersion)' == ''">$(MSTestVersion)</MSTestTestFrameworkVersion>
    <MicrosoftDotNetBuildTasksFeedVersion Condition="'$(MicrosoftDotNetBuildTasksFeedVersion)' == ''">$(ArcadeSdkVersion)</MicrosoftDotNetBuildTasksFeedVersion>
    <MicrosoftDotNetBuildTasksInstallersVersion Condition="'$(MicrosoftDotNetBuildTasksInstallersVersion)' == ''">$(ArcadeSdkVersion)</MicrosoftDotNetBuildTasksInstallersVersion>
    <NUnitVersion Condition="'$(NUnitVersion)' == ''">3.12.0</NUnitVersion>
    <NUnit3TestAdapterVersion Condition="'$(NUnit3TestAdapterVersion)' == ''">3.15.1</NUnit3TestAdapterVersion>
    <VSWhereVersion Condition="'$(VSWhereVersion)' == ''">2.6.7</VSWhereVersion>
    <SNVersion Condition="'$(SNVersion)' == ''">1.0.0</SNVersion>
    <MicrosoftDotNetBuildTasksVisualStudioVersion Condition="'$(MicrosoftDotNetBuildTasksVisualStudioVersion)' == ''">$(ArcadeSdkVersion)</MicrosoftDotNetBuildTasksVisualStudioVersion>
    <MicrosoftDotNetSourceBuildTasksVersion Condition="'$(MicrosoftDotNetSourceBuildTasksVersion)' == ''">$(ArcadeSdkVersion)</MicrosoftDotNetSourceBuildTasksVersion>
    <RichCodeNavPackageVersion Condition="'$(RichCodeNavPackageVersion)' == ''">0.1.1832-alpha</RichCodeNavPackageVersion>
    <MicrosoftVisualStudioEngMicroBuildCoreVersion Condition="'$(MicrosoftVisualStudioEngMicroBuildCoreVersion)' == ''">1.0.0</MicrosoftVisualStudioEngMicroBuildCoreVersion>
    <MicrosoftManifestToolCrossPlatformVersion Condition="'$(MicrosoftManifestToolCrossPlatformVersion)' == ''">2.1.3</MicrosoftManifestToolCrossPlatformVersion>
    <MicrosoftVisualStudioEngMicroBuildPluginsSwixBuildVersion Condition="'$(MicrosoftVisualStudioEngMicroBuildPluginsSwixBuildVersion)' == ''">1.1.286</MicrosoftVisualStudioEngMicroBuildPluginsSwixBuildVersion>
    <MicrosoftSignedWixVersion Condition="'$(MicrosoftSignedWixVersion)' == ''">3.14.1-8722.20240403.1</MicrosoftSignedWixVersion>
  </PropertyGroup>
  <!-- RestoreSources overrides - defines DotNetRestoreSources variable if available -->
  <!--<Import Project="$(DotNetPackageVersionPropsPath)" Condition="'$(DotNetPackageVersionPropsPath)' != ''" />-->
  <!--
    Defaults for properties that need to be available to all CI build steps and are dependent on settings specified in eng/Versions.props.
  -->
  <PropertyGroup>
    <IbcOptimizationDataDir Condition="'$(UsingToolVisualStudioIbcTraining)' == 'true'">$(ArtifactsDir)ibc\</IbcOptimizationDataDir>
  </PropertyGroup>
  <!--
============================================================================================================================================
  </Import>

C:\.tools\.nuget\packages\microsoft.dotnet.arcade.sdk\8.0.0-beta.24426.2\tools\Settings.props
============================================================================================================================================
-->
  <!--
============================================================================================================================================
  <Import Project="StrongName.props">

C:\.tools\.nuget\packages\microsoft.dotnet.arcade.sdk\8.0.0-beta.24426.2\tools\StrongName.props
============================================================================================================================================
-->
  <!-- Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. -->
  <PropertyGroup>
    <MicrosoftSharedPublicKey>0024000004800000940000000602000000240000525341310004000001000100b5fc90e7027f67871e773a8fde8938c81dd402ba65b9201d60593e96c492651e889cc13f1415ebb53fac1131ae0bd333c5ee6021672d9718ea31a8aebd0da0072f25d87dba6fc90ffd598ed4da35e44c398c454307e8e33b8426143daec9f596836f97c8f74750e5975c64e2189f45def46b2a2b1247adc3652bf5c308055da9</MicrosoftSharedPublicKey>
    <MicrosoftPublicKey>002400000480000094000000060200000024000052534131000400000100010007d1fa57c4aed9f0a32e84aa0faefd0de9e8fd6aec8f87fb03766c834c99921eb23be79ad9d5dcc1dd9ad236132102900b723cf980957fc4e177108fc607774f29e8320e92ea05ece4e821c0a5efe8f1645c4c0c93c1ab99285d622caa652c1dfad63d745d6f2de5f17e5eaf0fc4963d261c8a12436518206dc093344d5ad293</MicrosoftPublicKey>
    <MicrosoftAspNetCorePublicKey>0024000004800000940000000602000000240000525341310004000001000100f33a29044fa9d740c9b3213a93e57c84b472c84e0b8a0e1ae48e67a9f8f6de9d5f7f3d52ac23e48ac51801f1dc950abe901da34d2a9e3baadb141a17c77ef3c565dd5ee5054b91cf63bb3c6ab83f72ab3aafe93d0fc3c2348b764fafb0b1c0733de51459aeab46580384bf9d74c4e28164b7cde247f891ba07891c9d872ad2bb</MicrosoftAspNetCorePublicKey>
    <ECMAPublicKey>00000000000000000400000000000000</ECMAPublicKey>
    <OpenPublicKey>00240000048000009400000006020000002400005253413100040000010001004b86c4cb78549b34bab61a3b1800e23bfeb5b3ec390074041536a7e3cbd97f5f04cf0f857155a8928eaa29ebfd11cfbbad3ba70efea7bda3226c6a8d370a4cd303f714486b6ebc225985a638471e6ef571cc92a4613c00b8fa65d61ccee0cbe5f36330c9a01f4183559f1bef24cc2917c6d913e3a541333a1d05d9bed22b38cb</OpenPublicKey>
    <SilverlightPlatformPublicKey>00240000048000009400000006020000002400005253413100040000010001008d56c76f9e8649383049f383c44be0ec204181822a6c31cf5eb7ef486944d032188ea1d3920763712ccb12d75fb77e9811149e6148e5d32fbaab37611c1878ddc19e20ef135d0cb2cff2bfec3d115810c3d9069638fe4be215dbf795861920e5ab6f7db2e2ceef136ac23d5dd2bf031700aec232f6c6b1c785b4305c123b37ab</SilverlightPlatformPublicKey>
  </PropertyGroup>
  <!--
============================================================================================================================================
  </Import>

C:\.tools\.nuget\packages\microsoft.dotnet.arcade.sdk\8.0.0-beta.24426.2\tools\Settings.props
============================================================================================================================================
-->
  <!--
============================================================================================================================================
  <Import Project="ProjectDefaults.props">

C:\.tools\.nuget\packages\microsoft.dotnet.arcade.sdk\8.0.0-beta.24426.2\tools\ProjectDefaults.props
============================================================================================================================================
-->
  <!-- Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. -->
  <PropertyGroup>
    <SignAssembly>true</SignAssembly>
    <StrongNameKeyId>MicrosoftShared</StrongNameKeyId>
    <ChecksumAlgorithm>SHA256</ChecksumAlgorithm>
    <HighEntropyVA>true</HighEntropyVA>
    <NeutralLanguage>en-US</NeutralLanguage>
    <Company>Microsoft Corporation</Company>
    <CopyrightMicrosoft>© Microsoft Corporation. All rights reserved.</CopyrightMicrosoft>
    <CopyrightNetFoundation>© .NET Foundation and Contributors</CopyrightNetFoundation>
    <Authors>Microsoft</Authors>
    <Serviceable>true</Serviceable>
    <DevelopmentDependency>false</DevelopmentDependency>
    <PackageRequireLicenseAcceptance>true</PackageRequireLicenseAcceptance>
    <PublishRepositoryUrl>true</PublishRepositoryUrl>
    <EmbedUntrackedSources>true</EmbedUntrackedSources>
    <PackageIcon>Icon.png</PackageIcon>
    <PackageIconFullPath>$(MSBuildThisFileDirectory)Assets\DotNetPackageIcon.png</PackageIconFullPath>
    <!-- Disable the message indicating we are using a preview SDK. That is understood and by design -->
    <SuppressNETCoreSdkPreviewMessage>true</SuppressNETCoreSdkPreviewMessage>
    <!-- By default do not build NuGet package for a non pkgproj project. Project may override. -->
    <IsPackable Condition="'$(MSBuildProjectExtension)' != '.pkgproj'">false</IsPackable>
    <!--
      Official build:
       - Build standalone Portable PDBs to reduce the size of the binaries.
       - Convert Portable PDBs to Windows PDBs and publish the converted PDBs to Symbol Store to allow WinDBG, 
         Watson and other tools to find symbol format they understand.

      PR validation build:
       - Embed PDBs to make it easier to debug crash dumps captured on the CI machine.

      Developer build:
       - Embed PDBs to be consistent with PR validation build.    
    -->
    <DebugType>portable</DebugType>
    <DebugType Condition="'$(OfficialBuild)' != 'true'">embedded</DebugType>
    <!-- 
      This controls the places MSBuild will consult to resolve assembly references.  This is 
      kept as minimal as possible to make our build reliable from machine to machine.  Global
      locations such as GAC, AssemblyFoldersEx, etc ... are deliberately removed from this 
      list as they will not be the same from machine to machine.
    -->
    <AssemblySearchPaths>
      {HintPathFromItem};
      {TargetFrameworkDirectory};
      {RawFileName};
    </AssemblySearchPaths>
  </PropertyGroup>
  <PropertyGroup>
    <Language Condition="'$(MSBuildProjectExtension)' == '.csproj'">C#</Language>
    <Language Condition="'$(MSBuildProjectExtension)' == '.vbproj'">VB</Language>
    <Language Condition="'$(MSBuildProjectExtension)' == '.fsproj'">F#</Language>
    <Language Condition="'$(MSBuildProjectExtension)' == '.vcxproj'">C++</Language>
  </PropertyGroup>
  <!--
    When building WPF / VSIX projects MSBuild will create a temporary project with an extension of
    tmp_proj.  In that case the SDK is unable to determine the target language and cannot pick
    the correct import. Need to set it explicitly here.
    See https://github.com/dotnet/project-system/issues/1467
  -->
  <Choose>
    <When Condition="'$(MSBuildProjectExtension)' != '.csproj' and '$(MSBuildProjectExtension)' != '.vbproj' and '$(MSBuildProjectExtension)' != '.shproj'">
      <Choose>
        <When Condition="'$(Language)' == 'C#' or Exists('$(MSBuildProjectDirectory)\$(AssemblyName).csproj')">
          <PropertyGroup>
            <Language>C#</Language>
            <LanguageTargets>$(MSBuildToolsPath)\Microsoft.CSharp.targets</LanguageTargets>
          </PropertyGroup>
        </When>
        <When Condition="'$(Language)' == 'VB' or Exists('$(MSBuildProjectDirectory)\$(AssemblyName).vbproj')">
          <PropertyGroup>
            <Language>VB</Language>
            <LanguageTargets>$(MSBuildToolsPath)\Microsoft.VisualBasic.targets</LanguageTargets>
          </PropertyGroup>
        </When>
      </Choose>
    </When>
  </Choose>
  <Choose>
    <!-- C# specific settings -->
    <When Condition="'$(Language)' == 'C#'">
      <PropertyGroup>
        <NoWarn>$(NoWarn);1701;1702;1705</NoWarn>
        <NoWarn Condition="'$(SkipArcadeNoWarnCS1591)' != 'true'">$(NoWarn);1591</NoWarn>
      </PropertyGroup>
    </When>
    <!-- VB specific settings -->
    <When Condition="'$(Language)' == 'VB'">
      <PropertyGroup>
        <MyType>Empty</MyType>
        <OptionCompare>Binary</OptionCompare>
        <OptionStrict>On</OptionStrict>
        <RemoveIntegerChecks>true</RemoveIntegerChecks>
      </PropertyGroup>
    </When>
    <!-- F# specific settings -->
    <When Condition="'$(Language)' == 'F#'">
      <PropertyGroup>
        <!-- F# compiler doesn't support PathMap (see https://github.com/Microsoft/visualfsharp/issues/3812) -->
        <DeterministicSourcePaths>false</DeterministicSourcePaths>
      </PropertyGroup>
    </When>
    <!-- C++ specific settings -->
    <When Condition="'$(Language)' == 'C++'">
      <PropertyGroup>
        <OutDir>$(OutputPath)</OutDir>
        <!-- 
          Disable NuGet package resolution during build - PackageReferences are not fully supported 
          Props and target files are still going to be imported from referenced packages.
        -->
        <ResolveNuGetPackages>false</ResolveNuGetPackages>
      </PropertyGroup>
    </When>
  </Choose>
  <ItemGroup Condition="'$(EnableRichCodeNavigation)' == 'true'">
    <PackageReference Include="RichCodeNav.EnvVarDump" Version="$(RichCodeNavPackageVersion)" IsImplicitlyDefined="true" PrivateAssets="all" />
  </ItemGroup>
  <!--
============================================================================================================================================
  </Import>

C:\.tools\.nuget\packages\microsoft.dotnet.arcade.sdk\8.0.0-beta.24426.2\tools\Settings.props
============================================================================================================================================
-->
  <!--<Import Project="Tests.props" Condition="'$(DisableArcadeTestFramework)' != 'true'" />-->
  <!--
============================================================================================================================================
  <Import Project="Workarounds.props">

C:\.tools\.nuget\packages\microsoft.dotnet.arcade.sdk\8.0.0-beta.24426.2\tools\Workarounds.props
============================================================================================================================================
-->
  <!-- Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. -->
  <!--
    Determine whether the project is WPF temp project.
    Since .NET Framework 4.7.2 WPF temp project name ends with _wpftmp suffix and keeps the language specific extension (e.g. csproj). 
  -->
  <PropertyGroup Condition="'$(IsWpfTempProject)' == ''">
    <IsWpfTempProject>false</IsWpfTempProject>
    <IsWpfTempProject Condition="$(MSBuildProjectName.EndsWith('_wpftmp'))">true</IsWpfTempProject>
  </PropertyGroup>
  <!--
    WPF temp-projects do not import .props and .targets files from NuGet packages.
    (see https://github.com/dotnet/sourcelink/issues/91).
    
    Property _TargetAssemblyProjectName is set by GenerateTemporaryTargetAssembly task.

    Disable Source Link and Xliff in WPF temp projects to avoid generating non-deterministic file names to obj dir.
    The project name is non-deterministic and is included in the Source Link json file name and xlf directory names.
    It's also not necessary to generate these assets.
  -->
  <PropertyGroup Condition="'$(IsWpfTempProject)' == 'true'">
    <EnableSourceLink>false</EnableSourceLink>
    <EmbedUntrackedSources>false</EmbedUntrackedSources>
    <DeterministicSourcePaths>false</DeterministicSourcePaths>
    <EnableXlfLocalization>false</EnableXlfLocalization>
  </PropertyGroup>
  <!--
============================================================================================================================================
  </Import>

C:\.tools\.nuget\packages\microsoft.dotnet.arcade.sdk\8.0.0-beta.24426.2\tools\Settings.props
============================================================================================================================================
-->
  <!--
============================================================================================================================================
  <Import Project="TargetFrameworkDefaults.props">

C:\.tools\.nuget\packages\microsoft.dotnet.arcade.sdk\8.0.0-beta.24426.2\tools\TargetFrameworkDefaults.props
============================================================================================================================================
-->
  <!-- Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. -->
  <!-- Repositories using the arcade SDK can stay up to date with their target framework more easily using the properties in this file.
       - NetCurrent - The TFM of the major release of .NET that the Arcade SDK aligns with.
       - NetPrevious - The previously released version of .NET (e.g. this would be net7 if NetCurrent is net8)
       - NetMinimum - Lowest supported version of .NET the time of the release of NetCurrent. E.g. if NetCurrent is net8, then NetMinimum is net6
       - NetFrameworkMinimum - Lowest supported version of .NET Framework the time of the release of NetCurrent. E.g. if NetCurrent is net8, then NetFrameworkMinimum is net462
       - NetFrameworkToolCurrent - The version of .NET Framework that tools (msbuild tasks) should target.
       - NetFrameworkCurrent - The TFM of the latest version of .NET Framework.

       Examples:

       <TargetFrameworks>$(NetCurrent)</TargetFrameworks>
       <TargetFrameworks>$(NetCurrent);net472</TargetFrameworks>
       <TargetFrameworks>$(NetCurrent);$(NetPrevious);$(NetFrameworkMinimum);net472</TargetFrameworks>
  -->
  <PropertyGroup>
    <!-- .NET -->
    <NetCurrent>net8.0</NetCurrent>
    <NetPrevious>net7.0</NetPrevious>
    <NetMinimum>net6.0</NetMinimum>
    <!-- .NET Framework -->
    <NetFrameworkMinimum>net462</NetFrameworkMinimum>
    <NetFrameworkToolCurrent>net472</NetFrameworkToolCurrent>
    <NetFrameworkCurrent>net481</NetFrameworkCurrent>
  </PropertyGroup>
  <!--
============================================================================================================================================
  </Import>

C:\.tools\.nuget\packages\microsoft.dotnet.arcade.sdk\8.0.0-beta.24426.2\tools\Settings.props
============================================================================================================================================
-->
  <!--
============================================================================================================================================
  <Import Project="Compiler.props" Condition="'$(UsingToolMicrosoftNetCompilers)' == 'true'">

C:\.tools\.nuget\packages\microsoft.dotnet.arcade.sdk\8.0.0-beta.24426.2\tools\Compiler.props
============================================================================================================================================
-->
  <!-- Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. -->
  <ItemGroup>
    <PackageReference Include="Microsoft.Net.Compilers.Toolset" Version="$(MicrosoftNetCompilersToolsetVersion)" PrivateAssets="all" IsImplicitlyDefined="true" />
  </ItemGroup>
  <!--
============================================================================================================================================
  </Import>

C:\.tools\.nuget\packages\microsoft.dotnet.arcade.sdk\8.0.0-beta.24426.2\tools\Settings.props
============================================================================================================================================
-->
  <!--
============================================================================================================================================
  <Import Project="Linker.props" Condition="'$(UsingToolMicrosoftNetILLinkTasks)' == 'true'">

C:\.tools\.nuget\packages\microsoft.dotnet.arcade.sdk\8.0.0-beta.24426.2\tools\Linker.props
============================================================================================================================================
-->
  <!-- Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. -->
  <ItemGroup>
    <PackageReference Include="Microsoft.NET.ILLink.Tasks" Version="$(MicrosoftNetILLinkTasksVersion)" PrivateAssets="all" IsImplicitlyDefined="true" />
  </ItemGroup>
  <!--
============================================================================================================================================
  </Import>

C:\.tools\.nuget\packages\microsoft.dotnet.arcade.sdk\8.0.0-beta.24426.2\tools\Settings.props
============================================================================================================================================
-->
  <!--<Import Project="VisualStudio.props" Condition="'$(UsingToolVSSDK)' == 'true' and '$(MSBuildRuntimeType)' != 'Core'" />-->
  <!--
============================================================================================================================================
  </Import>

C:\.tools\.nuget\packages\microsoft.dotnet.arcade.sdk\8.0.0-beta.24426.2\Sdk\Sdk.props
============================================================================================================================================
-->
  <!--
============================================================================================================================================
  </Import>

C:\Users\calope\source\repos\runtime8\Directory.Build.props
============================================================================================================================================
-->
  <!-- The TFMs to build and test against. -->
  <PropertyGroup>
    <NetCoreAppCurrentVersion>8.0</NetCoreAppCurrentVersion>
    <NetCoreAppCurrentIdentifier>.NETCoreApp</NetCoreAppCurrentIdentifier>
    <NetCoreAppCurrentTargetFrameworkMoniker>$(NetCoreAppCurrentIdentifier),Version=v$(NetCoreAppCurrentVersion)</NetCoreAppCurrentTargetFrameworkMoniker>
    <MicrosoftNetCoreAppFrameworkName>Microsoft.NETCore.App</MicrosoftNetCoreAppFrameworkName>
    <NetCoreAppCurrentBrandName>.NET $(NetCoreAppCurrentVersion)</NetCoreAppCurrentBrandName>
    <NetCoreAppCurrent>net$(NetCoreAppCurrentVersion)</NetCoreAppCurrent>
    <!-- The previous supported .NET version. -->
    <NetCoreAppPreviousVersion>7.0</NetCoreAppPreviousVersion>
    <NetCoreAppPrevious>net$(NetCoreAppPreviousVersion)</NetCoreAppPrevious>
    <NetCoreAppPrevious Condition="'$(DotNetBuildFromSource)' == 'true'">$(NetCoreAppCurrent)</NetCoreAppPrevious>
    <!-- The minimum supported .NET version. -->
    <NetCoreAppMinimum>net6.0</NetCoreAppMinimum>
    <NetCoreAppMinimum Condition="'$(DotNetBuildFromSource)' == 'true'">$(NetCoreAppCurrent)</NetCoreAppMinimum>
    <!-- when this is updated, make sure to keep $(_TargetFrameworkForNETCoreTasks)
         in src/mono/wasm/build/WasmApp.LocalBuild.props in sync -->
    <NetCoreAppToolCurrentVersion>8.0</NetCoreAppToolCurrentVersion>
    <NetCoreAppToolCurrent>net$(NetCoreAppToolCurrentVersion)</NetCoreAppToolCurrent>
    <NetCoreAppCurrentToolTargetFrameworkMoniker>$(NetCoreAppCurrentIdentifier),Version=v$(NetCoreAppToolCurrentVersion)</NetCoreAppCurrentToolTargetFrameworkMoniker>
    <AspNetCoreAppCurrentVersion>8.0</AspNetCoreAppCurrentVersion>
    <AspNetCoreAppCurrent>net$(AspNetCoreAppCurrentVersion)</AspNetCoreAppCurrent>
    <NetFrameworkMinimum>net462</NetFrameworkMinimum>
    <NetFrameworkCurrent>net48</NetFrameworkCurrent>
    <NetFrameworkToolCurrent>net472</NetFrameworkToolCurrent>
    <!-- Don't build for NETFramework during source-build. -->
    <NetFrameworkMinimum Condition="'$(DotNetBuildFromSource)' == 'true'" />
    <NetFrameworkToolCurrent Condition="'$(DotNetBuildFromSource)' == 'true'" />
    <NetFrameworkCurrent Condition="'$(DotNetBuildFromSource)' == 'true'" />
    <!-- Important: Set this to the GA version (or a close approximation) during servicing and adjust the TFM property below. -->
    <ApiCompatNetCoreAppBaselineVersion>8.0.0</ApiCompatNetCoreAppBaselineVersion>
    <ApiCompatNetCoreAppBaselineTFM>net8.0</ApiCompatNetCoreAppBaselineTFM>
    <TargetFrameworkForNETFrameworkTasks>$(NetFrameworkToolCurrent)</TargetFrameworkForNETFrameworkTasks>
    <!-- Don't build for NETFramework during source-build. -->
    <TargetFrameworkForNETFrameworkTasks Condition="'$(DotNetBuildFromSource)' == 'true'" />
    <TargetFrameworkForNETCoreTasks>$(NetCoreAppToolCurrent)</TargetFrameworkForNETCoreTasks>
  </PropertyGroup>
  <PropertyGroup Label="CalculateConfiguration">
    <!-- The RuntimeConfiguration property allows to pass in/specify a configuration that applies to both CoreCLR and Mono. -->
    <RuntimeConfiguration Condition="'$(RuntimeConfiguration)' == ''">$(Configuration)</RuntimeConfiguration>
    <RuntimeConfiguration Condition="'$(RuntimeConfiguration)' == '' and ('$(Configuration)' == 'Debug' or '$(Configuration)' == 'Release')">$(Configuration)</RuntimeConfiguration>
    <RuntimeConfiguration Condition="'$(RuntimeConfiguration)' == ''">Debug</RuntimeConfiguration>
    <CoreCLRConfiguration Condition="'$(CoreCLRConfiguration)' == ''">$(RuntimeConfiguration)</CoreCLRConfiguration>
    <MonoConfiguration Condition="'$(MonoConfiguration)' == '' and '$(RuntimeConfiguration.ToLower())' != 'checked'">$(RuntimeConfiguration)</MonoConfiguration>
    <!-- There's no checked configuration on Mono. -->
    <MonoConfiguration Condition="'$(MonoConfiguration)' == '' and '$(RuntimeConfiguration.ToLower())' == 'checked'">Debug</MonoConfiguration>
    <LibrariesConfiguration Condition="'$(LibrariesConfiguration)' == ''">$(Configuration)</LibrariesConfiguration>
    <HostConfiguration Condition="'$(HostConfiguration)' == ''">$(Configuration)</HostConfiguration>
    <TasksConfiguration Condition="'$(TasksConfiguration)' == ''">$(Configuration)</TasksConfiguration>
  </PropertyGroup>
  <PropertyGroup>
    <LibrariesProjectRoot>$([MSBuild]::NormalizeDirectory('$(MSBuildThisFileDirectory)', 'src', 'libraries'))</LibrariesProjectRoot>
    <CoreClrProjectRoot>$([MSBuild]::NormalizeDirectory('$(MSBuildThisFileDirectory)', 'src', 'coreclr'))</CoreClrProjectRoot>
    <MonoProjectRoot>$([MSBuild]::NormalizeDirectory('$(MSBuildThisFileDirectory)', 'src', 'mono'))</MonoProjectRoot>
    <InstallerProjectRoot>$([MSBuild]::NormalizeDirectory('$(MSBuildThisFileDirectory)', 'src', 'installer'))</InstallerProjectRoot>
    <WorkloadsProjectRoot>$([MSBuild]::NormalizeDirectory('$(MSBuildThisFileDirectory)', 'src', 'workloads'))</WorkloadsProjectRoot>
    <ToolsProjectRoot>$([MSBuild]::NormalizeDirectory('$(MSBuildThisFileDirectory)', 'src', 'tools'))</ToolsProjectRoot>
    <SharedNativeRoot>$([MSBuild]::NormalizeDirectory('$(RepoRoot)', 'src', 'native'))</SharedNativeRoot>
    <RepoTasksDir>$([MSBuild]::NormalizeDirectory('$(MSBuildThisFileDirectory)', 'src', 'tasks'))</RepoTasksDir>
    <IbcOptimizationDataDir>$([MSBuild]::NormalizeDirectory('$(ArtifactsDir)', 'ibc'))</IbcOptimizationDataDir>
    <MibcOptimizationDataDir>$([MSBuild]::NormalizeDirectory('$(ArtifactsDir)', 'mibc'))</MibcOptimizationDataDir>
    <DocsDir>$([MSBuild]::NormalizeDirectory('$(MSBuildThisFileDirectory)', 'docs'))</DocsDir>
    <AppleAppBuilderDir>$([MSBuild]::NormalizeDirectory('$(ArtifactsBinDir)', 'AppleAppBuilder', '$(TasksConfiguration)', '$(NetCoreAppToolCurrent)'))</AppleAppBuilderDir>
    <AndroidAppBuilderDir>$([MSBuild]::NormalizeDirectory('$(ArtifactsBinDir)', 'AndroidAppBuilder', '$(TasksConfiguration)', '$(NetCoreAppToolCurrent)', 'publish'))</AndroidAppBuilderDir>
    <MobileBuildTasksDir>$([MSBuild]::NormalizeDirectory('$(ArtifactsBinDir)', 'MobileBuildTasks', '$(TasksConfiguration)', '$(NetCoreAppToolCurrent)'))</MobileBuildTasksDir>
    <WasmAppBuilderDir>$([MSBuild]::NormalizeDirectory('$(ArtifactsBinDir)', 'WasmAppBuilder', '$(TasksConfiguration)', '$(NetCoreAppToolCurrent)'))</WasmAppBuilderDir>
    <WasmBuildTasksDir>$([MSBuild]::NormalizeDirectory('$(ArtifactsBinDir)', 'WasmBuildTasks', '$(TasksConfiguration)', '$(NetCoreAppToolCurrent)', 'publish'))</WasmBuildTasksDir>
    <WorkloadBuildTasksDir>$([MSBuild]::NormalizeDirectory('$(ArtifactsBinDir)', 'WorkloadBuildTasks', '$(TasksConfiguration)', '$(NetCoreAppToolCurrent)'))</WorkloadBuildTasksDir>
    <LibraryBuilderDir>$([MSBuild]::NormalizeDirectory('$(ArtifactsBinDir)', 'LibraryBuilder', '$(TasksConfiguration)', '$(NetCoreAppToolCurrent)'))</LibraryBuilderDir>
    <MonoAOTCompilerDir>$([MSBuild]::NormalizeDirectory('$(ArtifactsBinDir)', 'MonoAOTCompiler', '$(TasksConfiguration)', '$(NetCoreAppToolCurrent)'))</MonoAOTCompilerDir>
    <MonoTargetsTasksDir>$([MSBuild]::NormalizeDirectory('$(ArtifactsBinDir)', 'MonoTargetsTasks', '$(TasksConfiguration)', '$(NetCoreAppToolCurrent)'))</MonoTargetsTasksDir>
    <TestExclusionListTasksDir>$([MSBuild]::NormalizeDirectory('$(ArtifactsBinDir)', 'TestExclusionListTasks', '$(TasksConfiguration)', '$(NetCoreAppToolCurrent)'))</TestExclusionListTasksDir>
    <InstallerTasksAssemblyPath Condition="'$(MSBuildRuntimeType)' == 'Core'">$([MSBuild]::NormalizePath('$(ArtifactsBinDir)', 'installer.tasks', '$(TasksConfiguration)', '$(NetCoreAppToolCurrent)', 'installer.tasks.dll'))</InstallerTasksAssemblyPath>
    <InstallerTasksAssemblyPath Condition="'$(MSBuildRuntimeType)' != 'Core'">$([MSBuild]::NormalizePath('$(ArtifactsBinDir)', 'installer.tasks', '$(TasksConfiguration)', '$(NetFrameworkToolCurrent)', 'installer.tasks.dll'))</InstallerTasksAssemblyPath>
    <Crossgen2SdkOverridePropsPath Condition="'$(MSBuildRuntimeType)' == 'Core'">$([MSBuild]::NormalizePath('$(ArtifactsBinDir)', 'Crossgen2Tasks', '$(TasksConfiguration)', '$(NetCoreAppToolCurrent)', 'Microsoft.NET.CrossGen.props'))</Crossgen2SdkOverridePropsPath>
    <Crossgen2SdkOverrideTargetsPath Condition="'$(MSBuildRuntimeType)' == 'Core'">$([MSBuild]::NormalizePath('$(ArtifactsBinDir)', 'Crossgen2Tasks', '$(TasksConfiguration)', '$(NetCoreAppToolCurrent)', 'Microsoft.NET.CrossGen.targets'))</Crossgen2SdkOverrideTargetsPath>
    <AppleAppBuilderTasksAssemblyPath>$([MSBuild]::NormalizePath('$(AppleAppBuilderDir)', 'AppleAppBuilder.dll'))</AppleAppBuilderTasksAssemblyPath>
    <AndroidAppBuilderTasksAssemblyPath>$([MSBuild]::NormalizePath('$(AndroidAppBuilderDir)', 'AndroidAppBuilder.dll'))</AndroidAppBuilderTasksAssemblyPath>
    <MobileBuildTasksAssemblyPath>$([MSBuild]::NormalizePath('$(MobileBuildTasksDir)', 'MobileBuildTasks.dll'))</MobileBuildTasksAssemblyPath>
    <WasmAppBuilderTasksAssemblyPath>$([MSBuild]::NormalizePath('$(WasmAppBuilderDir)', 'WasmAppBuilder.dll'))</WasmAppBuilderTasksAssemblyPath>
    <WasmBuildTasksAssemblyPath>$([MSBuild]::NormalizePath('$(WasmBuildTasksDir)', 'WasmBuildTasks.dll'))</WasmBuildTasksAssemblyPath>
    <WasmAppHostDir>$([MSBuild]::NormalizeDirectory('$(ArtifactsBinDir)', 'WasmAppHost', 'wasm', '$(Configuration)'))</WasmAppHostDir>
    <WorkloadBuildTasksAssemblyPath>$([MSBuild]::NormalizePath('$(WorkloadBuildTasksDir)', 'WorkloadBuildTasks.dll'))</WorkloadBuildTasksAssemblyPath>
    <LibraryBuilderTasksAssemblyPath>$([MSBuild]::NormalizePath('$(LibraryBuilderDir)', 'LibraryBuilder.dll'))</LibraryBuilderTasksAssemblyPath>
    <MonoAOTCompilerTasksAssemblyPath>$([MSBuild]::NormalizePath('$(MonoAOTCompilerDir)', 'MonoAOTCompiler.dll'))</MonoAOTCompilerTasksAssemblyPath>
    <MonoTargetsTasksAssemblyPath>$([MSBuild]::NormalizePath('$(MonoTargetsTasksDir)', 'MonoTargetsTasks.dll'))</MonoTargetsTasksAssemblyPath>
    <TestExclusionListTasksAssemblyPath>$([MSBuild]::NormalizePath('$(TestExclusionListTasksDir)', 'TestExclusionListTasks.dll'))</TestExclusionListTasksAssemblyPath>
    <CoreCLRToolPath>$([MSBuild]::NormalizeDirectory('$(ArtifactsBinDir)', 'coreclr', '$(TargetOS).$(TargetArchitecture).$(RuntimeConfiguration)'))</CoreCLRToolPath>
    <ILAsmToolPath Condition="'$(DotNetBuildFromSource)' == 'true' or '$(BuildArchitecture)' == 's390x' or '$(BuildArchitecture)' == 'ppc64le'">$(CoreCLRToolPath)</ILAsmToolPath>
    <WasmtimeDir Condition="'$(WasmtimeDir)' == '' and '$(WASMTIME_PATH)' != '' and Exists($(WASMTIME_PATH))">$(WASMTIME_PATH)</WasmtimeDir>
    <WasmtimeDir Condition="'$(WasmtimeDir)' == ''">$([MSBuild]::NormalizeDirectory($(ArtifactsObjDir), 'wasmtime'))</WasmtimeDir>
    <InstallWasmtimeForTests Condition="'$(InstallWasmtimeForTests)' == '' and !Exists($(WasmtimeDir))">true</InstallWasmtimeForTests>
  </PropertyGroup>
  <PropertyGroup Label="CalculatePortableBuild">
    <PortableBuild Condition="'$(PortableBuild)' == '' and '$(DotNetBuildFromSource)' == 'true'">false</PortableBuild>
    <PortableBuild Condition="'$(PortableBuild)' == ''">true</PortableBuild>
  </PropertyGroup>
  <!-- _portableOS is the portable rid-OS corresponding to the target platform. -->
  <PropertyGroup Label="CalculatePortableOS">
    <!-- To determine _portableOS we use TargetOS.
         TargetOS is not a rid-OS. For example: for Windows it is 'windows' instead of 'win'.
         And, for flavors of Linux, like 'linux-musl' and 'linux-bionic', TargetOS is 'linux'. -->
    <_portableOS>$(TargetOS.ToLowerInvariant())</_portableOS>
    <_portableOS Condition="'$(_portableOS)' == 'windows'">win</_portableOS>
    <!-- TargetOS=AnyOS is a sentinel value used by tests, ignore it.  -->
    <_portableOS Condition="'$(_portableOS)' == 'anyos'">$(__PortableTargetOS)</_portableOS>
    <!-- Detect linux flavors using __PortableTargetOS from the native script. -->
    <_portableOS Condition="'$(_portableOS)' == 'linux' and '$(__PortableTargetOS)' == 'linux-musl'">linux-musl</_portableOS>
    <_portableOS Condition="'$(_portableOS)' == 'linux' and '$(__PortableTargetOS)' == 'linux-bionic'">linux-bionic</_portableOS>
    <!-- On Windows, we can build for Windows and Mobile.
         For other TargetOSes, create a "win" build, built from TargetOS sources and "win" pre-built packages. -->
    <_portableOS Condition="'$(HostOS)' == 'win' and '$(TargetsMobile)' != 'true'">win</_portableOS>
  </PropertyGroup>
  <!-- PackageRID is used for packages needed for the target. -->
  <PropertyGroup Label="CalculatePackageRID">
    <_packageOS>$(_portableOS)</_packageOS>
    <_packageOS Condition="'$(CrossBuild)' == 'true' and '$(_portableOS)' != 'linux-musl' and '$(_portableOS)' != 'linux-bionic'">$(_hostOS)</_packageOS>
    <!-- source-build sets PackageOS to build with non-portable rid packages that were source-built previously. -->
    <PackageRID Condition="'$(PackageOS)' != ''">$(PackageOS)-$(TargetArchitecture)</PackageRID>
    <PackageRID Condition="'$(PackageRID)' == ''">$(_packageOS)-$(TargetArchitecture)</PackageRID>
  </PropertyGroup>
  <!-- ToolsRID is used for packages needed on the build host. -->
  <PropertyGroup Label="CalculateToolsRID">
    <!-- _portableHostOS is the portable rid-OS corresponding to the build host platform.

         To determine _portableHostOS we use _hostOS, similar to how _portableOS is calculated from TargetOS.

         When we're not cross-building we can detect linux flavors by looking at _portableOS
         because the target platform and the build host platform are the same.
         For cross-builds, we're currently unable to detect the flavors. -->
    <_portableHostOS>$(_hostOS)</_portableHostOS>
    <_portableHostOS Condition="'$(_portableHostOS)' == 'windows'">win</_portableHostOS>
    <_portableHostOS Condition="'$(CrossBuild)' != 'true' and '$(_portableOS)' == 'linux-musl'">linux-musl</_portableHostOS>
    <!-- source-build sets ToolsOS to build with non-portable rid packages that were source-built previously. -->
    <ToolsRID Condition="'$(ToolsOS)' != ''">$(ToolsOS)-$(_hostArch)</ToolsRID>
    <ToolsRID Condition="'$(ToolsRID)' == ''">$(_portableHostOS)-$(_hostArch)</ToolsRID>
    <!-- Microsoft.NET.Sdk.IL SDK defaults to the portable host rid. Match it to ToolsRID (for source-build). -->
    <MicrosoftNetCoreIlasmPackageRuntimeId>$(ToolsRID)</MicrosoftNetCoreIlasmPackageRuntimeId>
  </PropertyGroup>
  <!-- OutputRID is used to name the target platform.
       For portable builds, OutputRID matches _portableOS.
       For non-portable builds, it uses __DistroRid (from the native build script), or falls back to RuntimeInformation.RuntimeIdentifier.
       Source-build sets OutputRID directly. -->
  <PropertyGroup Label="CalculateOutputRID">
    <_hostRid Condition="'$(MSBuildRuntimeType)' == 'core'">$([System.Runtime.InteropServices.RuntimeInformation]::RuntimeIdentifier)</_hostRid>
    <_hostRid Condition="'$(MSBuildRuntimeType)' != 'core'">win-$([System.Runtime.InteropServices.RuntimeInformation]::OSArchitecture.ToString().ToLowerInvariant)</_hostRid>
    <_parseDistroRid>$(__DistroRid)</_parseDistroRid>
    <_parseDistroRid Condition="'$(_parseDistroRid)' == ''">$(_hostRid)</_parseDistroRid>
    <_distroRidIndex>$(_parseDistroRid.LastIndexOf('-'))</_distroRidIndex>
    <_outputOS>$(_parseDistroRid.SubString(0, $(_distroRidIndex)))</_outputOS>
    <_outputOS Condition="'$(PortableBuild)' == 'true'">$(_portableOS)</_outputOS>
    <OutputRID Condition="'$(OutputRID)' == ''">$(_outputOS)-$(TargetArchitecture)</OutputRID>
  </PropertyGroup>
  <PropertyGroup Label="CalculateTargetOSName">
    <TargetsFreeBSD Condition="'$(TargetOS)' == 'freebsd'">true</TargetsFreeBSD>
    <Targetsillumos Condition="'$(TargetOS)' == 'illumos'">true</Targetsillumos>
    <TargetsSolaris Condition="'$(TargetOS)' == 'solaris'">true</TargetsSolaris>
    <TargetsHaiku Condition="'$(TargetOS)' == 'haiku'">true</TargetsHaiku>
    <TargetsLinux Condition="'$(TargetOS)' == 'linux' or '$(TargetOS)' == 'android'">true</TargetsLinux>
    <TargetsLinuxBionic Condition="'$(_portableOS)' == 'linux-bionic'">true</TargetsLinuxBionic>
    <TargetsLinuxMusl Condition="'$(_portableOS)' == 'linux-musl'">true</TargetsLinuxMusl>
    <TargetsNetBSD Condition="'$(TargetOS)' == 'netbsd'">true</TargetsNetBSD>
    <TargetsOSX Condition="'$(TargetOS)' == 'osx'">true</TargetsOSX>
    <TargetsMacCatalyst Condition="'$(TargetOS)' == 'maccatalyst'">true</TargetsMacCatalyst>
    <TargetsiOS Condition="'$(TargetOS)' == 'ios' or '$(TargetOS)' == 'iossimulator'">true</TargetsiOS>
    <TargetstvOS Condition="'$(TargetOS)' == 'tvos' or '$(TargetOS)' == 'tvossimulator'">true</TargetstvOS>
    <TargetsiOSSimulator Condition="'$(TargetOS)' == 'iossimulator'">true</TargetsiOSSimulator>
    <TargetstvOSSimulator Condition="'$(TargetOS)' == 'tvossimulator'">true</TargetstvOSSimulator>
    <TargetsAndroid Condition="'$(TargetOS)' == 'android'">true</TargetsAndroid>
    <TargetsBrowser Condition="'$(TargetOS)' == 'browser'">true</TargetsBrowser>
    <TargetsWasi Condition="'$(TargetOS)' == 'wasi'">true</TargetsWasi>
    <TargetsWindows Condition="'$(TargetOS)' == 'windows'">true</TargetsWindows>
    <TargetsUnix Condition="'$(TargetsFreeBSD)' == 'true' or '$(Targetsillumos)' == 'true' or '$(TargetsSolaris)' == 'true' or '$(TargetsHaiku)' == 'true' or '$(TargetsLinux)' == 'true' or '$(TargetsNetBSD)' == 'true' or '$(TargetsOSX)' == 'true' or '$(TargetsMacCatalyst)' == 'true' or '$(TargetstvOS)' == 'true' or '$(TargetsiOS)' == 'true' or '$(TargetsAndroid)' == 'true'">true</TargetsUnix>
  </PropertyGroup>
  <PropertyGroup>
    <MicrosoftNetCoreAppRefPackDir>$([MSBuild]::NormalizeDirectory('$(ArtifactsBinDir)', 'microsoft.netcore.app.ref'))</MicrosoftNetCoreAppRefPackDir>
    <MicrosoftNetCoreAppRefPackRefDir>$([MSBuild]::NormalizeDirectory('$(MicrosoftNetCoreAppRefPackDir)', 'ref', '$(NetCoreAppCurrent)'))</MicrosoftNetCoreAppRefPackRefDir>
    <MicrosoftNetCoreAppRefPackDataDir>$([MSBuild]::NormalizeDirectory('$(MicrosoftNetCoreAppRefPackDir)', 'data'))</MicrosoftNetCoreAppRefPackDataDir>
    <MicrosoftNetCoreAppRuntimePackDir>$([MSBuild]::NormalizeDirectory('$(ArtifactsBinDir)', 'microsoft.netcore.app.runtime.$(OutputRID)', '$(LibrariesConfiguration)'))</MicrosoftNetCoreAppRuntimePackDir>
    <MicrosoftNetCoreAppRuntimePackRidDir>$([MSBuild]::NormalizeDirectory('$(MicrosoftNetCoreAppRuntimePackDir)', 'runtimes', '$(OutputRID)'))</MicrosoftNetCoreAppRuntimePackRidDir>
    <MicrosoftNetCoreAppRuntimePackRidLibTfmDir>$([MSBuild]::NormalizeDirectory('$(MicrosoftNetCoreAppRuntimePackRidDir)', 'lib', '$(NetCoreAppCurrent)'))</MicrosoftNetCoreAppRuntimePackRidLibTfmDir>
    <MicrosoftNetCoreAppRuntimePackNativeDir>$([MSBuild]::NormalizeDirectory('$(MicrosoftNetCoreAppRuntimePackRidDir)', 'native'))</MicrosoftNetCoreAppRuntimePackNativeDir>
  </PropertyGroup>
  <PropertyGroup>
    <DotNetHostBinDir>$([MSBuild]::NormalizeDirectory('$(ArtifactsBinDir)', '$(OutputRID).$(HostConfiguration)', 'corehost'))</DotNetHostBinDir>
  </PropertyGroup>
  <!--Feature switches -->
  <PropertyGroup>
    <NoPgoOptimize Condition="'$(NoPgoOptimize)' == '' and '$(DotNetBuildFromSource)' == 'true'">true</NoPgoOptimize>
    <EnableNgenOptimization Condition="'$(EnableNgenOptimization)' == '' and '$(DotNetBuildFromSource)' == 'true'">false</EnableNgenOptimization>
    <EnableNgenOptimization Condition="'$(EnableNgenOptimization)' == '' and ('$(Configuration)' == 'Release' or '$(Configuration)' == 'Checked')">true</EnableNgenOptimization>
    <!-- Enable NuGet static graph evaluation to optimize incremental restore -->
    <RestoreUseStaticGraphEvaluation>true</RestoreUseStaticGraphEvaluation>
    <!-- Turn off end of life target framework checks as we intentionally build older .NETCoreApp configurations. -->
    <CheckEolTargetFramework>false</CheckEolTargetFramework>
    <!-- Turn off workload support until we support them. -->
    <MSBuildEnableWorkloadResolver>false</MSBuildEnableWorkloadResolver>
    <!-- Disable source link when building locally. -->
    <DisableSourceLink Condition="'$(DisableSourceLink)' == '' and&#xD;&#xA;                                  '$(ContinuousIntegrationBuild)' != 'true' and&#xD;&#xA;                                  '$(OfficialBuildId)' == ''">true</DisableSourceLink>
    <!-- Runtime doesn't support Arcade-driven target framework filtering. -->
    <NoTargetFrameworkFiltering>true</NoTargetFrameworkFiltering>
  </PropertyGroup>
  <!-- RepositoryEngineeringDir isn't set when Installer tests import this file. -->
  <!--
============================================================================================================================================
  <Import Project="$(RepositoryEngineeringDir)native\naming.props">

C:\Users\calope\source\repos\runtime8\eng\native\naming.props
============================================================================================================================================
-->
  <PropertyGroup>
    <StaticLibPrefix>lib</StaticLibPrefix>
    <ExeSuffix Condition="'$(HostOS)' == 'windows'">.exe</ExeSuffix>
  </PropertyGroup>
  <!-- Add path globs specific to native binaries to exclude unnecessary files from packages. -->
  <Choose>
    <When Condition="$(PackageRID.StartsWith('win'))">
      <PropertyGroup>
        <LibSuffix>.dll</LibSuffix>
        <StaticLibSuffix>.lib</StaticLibSuffix>
        <SymbolsSuffix>.pdb</SymbolsSuffix>
      </PropertyGroup>
    </When>
    <When Condition="$(PackageRID.StartsWith('osx')) or $(PackageRID.StartsWith('maccatalyst')) or $(PackageRID.StartsWith('ios')) or $(PackageRID.StartsWith('tvos'))">
      <PropertyGroup>
        <LibPrefix>lib</LibPrefix>
        <LibSuffix>.dylib</LibSuffix>
        <StaticLibSuffix>.a</StaticLibSuffix>
        <SymbolsSuffix>.dwarf</SymbolsSuffix>
      </PropertyGroup>
    </When>
    <Otherwise>
      <PropertyGroup>
        <LibPrefix>lib</LibPrefix>
        <LibSuffix>.so</LibSuffix>
        <StaticLibSuffix>.a</StaticLibSuffix>
        <SymbolsSuffix>.dbg</SymbolsSuffix>
      </PropertyGroup>
    </Otherwise>
  </Choose>
  <ItemGroup>
    <AdditionalLibPackageExcludes Condition="'$(SymbolsSuffix)' != ''" Include="%2A%2A\%2A$(SymbolsSuffix)" />
    <AdditionalSymbolPackageExcludes Condition="'$(LibSuffix)' != ''" Include="%2A%2A\%2A.a;%2A%2A\%2A$(LibSuffix)" />
  </ItemGroup>
  <!--
============================================================================================================================================
  </Import>

C:\Users\calope\source\repos\runtime8\Directory.Build.props
============================================================================================================================================
-->
  <!--
============================================================================================================================================
  <Import Project="$(RepositoryEngineeringDir)Subsets.props">

C:\Users\calope\source\repos\runtime8\eng\Subsets.props
============================================================================================================================================
-->
  <!--
    This file defines the list of projects to build and divides them into subsets. In ordinary
    situations, you should perform a full build by running 'build.cmd' or './build.sh'. This ensures
    the projects are sequenced correctly so the outputs and test results are what you would expect.

    Examples:

      ./build.sh host.native

        This builds only the .NET host.

      ./build.sh libs+host.native

        This builds the .NET host and also the manged libraries portion.
        A space ' ' or '+' are the delimiters between multiple subsets to build.

      ./build.sh -test host.tests

        This builds and executes the installer test projects. (The '-test' argument is an Arcade SDK argument
        that indicates tests should be run. Otherwise, they'll only be built.)
  -->
  <!-- Determine the primary runtime flavor. This is usually CoreCLR, except on
       platforms (like s390x) where only Mono is supported. The primary runtime
       flavor is used to decide when to build the hosts and installers. -->
  <PropertyGroup>
    <DefaultPrimaryRuntimeFlavor>CoreCLR</DefaultPrimaryRuntimeFlavor>
    <DefaultPrimaryRuntimeFlavor Condition="'$(TargetArchitecture)' == 'armv6'">Mono</DefaultPrimaryRuntimeFlavor>
    <DefaultPrimaryRuntimeFlavor Condition="'$(TargetArchitecture)' == 'ppc64le'">Mono</DefaultPrimaryRuntimeFlavor>
    <DefaultPrimaryRuntimeFlavor Condition="'$(TargetArchitecture)' == 's390x'">Mono</DefaultPrimaryRuntimeFlavor>
    <DefaultPrimaryRuntimeFlavor Condition="'$(TargetsLinuxBionic)' == 'true'">Mono</DefaultPrimaryRuntimeFlavor>
    <PrimaryRuntimeFlavor Condition="'$(PrimaryRuntimeFlavor)' == ''">$(DefaultPrimaryRuntimeFlavor)</PrimaryRuntimeFlavor>
  </PropertyGroup>
  <PropertyGroup>
    <DefaultSubsets>clr+mono+libs+tools+host+packs</DefaultSubsets>
    <DefaultSubsets Condition="'$(TargetsMobile)' == 'true'">mono+libs+packs</DefaultSubsets>
    <DefaultSubsets Condition="'$(TargetsLinuxBionic)' == 'true'">mono+libs+host+packs</DefaultSubsets>
    <!-- In source build, mono is only supported as primary runtime flavor. On Windows mono is supported for x86/x64 only. -->
    <DefaultSubsets Condition="('$(DotNetBuildFromSource)' == 'true' and '$(PrimaryRuntimeFlavor)' != 'Mono') or ('$(TargetOS)' == 'windows' and '$(TargetArchitecture)' != 'x86' and '$(TargetArchitecture)' != 'x64')">clr+libs+tools+host+packs</DefaultSubsets>
  </PropertyGroup>
  <!-- Init _subset here to allow RuntimeFlavor to be set as early as possible -->
  <PropertyGroup>
    <_subset Condition="'$(Subset)' != ''">+$(Subset.ToLowerInvariant())+</_subset>
    <_subset Condition="'$(Subset)' == ''">+$(DefaultSubsets)+</_subset>
  </PropertyGroup>
  <PropertyGroup>
    <RuntimeFlavor Condition="'$(TargetsMobile)' == 'true' and !$(_subset.Contains('+clr.nativeaotlibs+'))">Mono</RuntimeFlavor>
    <RuntimeFlavor Condition="('$(TargetsMobile)' == 'true' or '$(TargetsLinuxBionic)' == 'true') and $(_subset.Contains('+clr.nativeaotlibs+'))">CoreCLR</RuntimeFlavor>
    <RuntimeFlavor Condition="'$(RuntimeFlavor)' == '' and ($(_subset.Contains('+mono+')) or $(_subset.Contains('+mono.runtime+'))) and (!$(_subset.Contains('+clr+')) and !$(_subset.Contains('+clr.runtime+')))">Mono</RuntimeFlavor>
    <RuntimeFlavor Condition="'$(RuntimeFlavor)' == ''">$(PrimaryRuntimeFlavor)</RuntimeFlavor>
  </PropertyGroup>
  <PropertyGroup>
    <DefaultCoreClrSubsets>clr.native+linuxdac+clr.corelib+clr.tools+clr.nativecorelib+clr.packages+clr.nativeaotlibs+clr.crossarchtools+host.native</DefaultCoreClrSubsets>
    <DefaultCoreClrSubsets Condition="'$(PackageRID)' == 'linux-armel'">clr.native+linuxdac+clr.corelib+clr.tools+clr.nativecorelib+clr.packages+clr.nativeaotlibs+clr.crossarchtools</DefaultCoreClrSubsets>
    <!-- Even on platforms that do not support the CoreCLR runtime, we still want to build ilasm/ildasm. -->
    <DefaultCoreClrSubsets Condition="'$(PrimaryRuntimeFlavor)' != 'CoreCLR'">clr.iltools+clr.packages</DefaultCoreClrSubsets>
    <DefaultNativeAotSubsets>clr.alljits+clr.tools+clr.nativeaotlibs+clr.nativeaotruntime</DefaultNativeAotSubsets>
    <DefaultMonoSubsets Condition="'$(MonoEnableLLVM)' == 'true' and '$(MonoLLVMDir)' == ''">mono.llvm+</DefaultMonoSubsets>
    <DefaultMonoSubsets Condition="'$(MonoAOTEnableLLVM)' == 'true' and '$(MonoLLVMDir)' == ''">mono.llvm+</DefaultMonoSubsets>
    <DefaultMonoSubsets Condition="'$(TargetOS)' == 'browser'">$(DefaultMonoSubsets)mono.wasmruntime+</DefaultMonoSubsets>
    <DefaultMonoSubsets Condition="'$(TargetOS)' == 'wasi'">$(DefaultMonoSubsets)mono.wasiruntime+</DefaultMonoSubsets>
    <DefaultMonoSubsets Condition="'$(MonoCrossAOTTargetOS)' != ''">$(DefaultMonoSubsets)mono.aotcross+</DefaultMonoSubsets>
    <DefaultMonoSubsets>$(DefaultMonoSubsets)mono.runtime+mono.corelib+mono.packages+</DefaultMonoSubsets>
    <DefaultMonoSubsets Condition="'$(TargetsMobile)' == 'true' or '$(ForceBuildMobileManifests)' == 'true'">$(DefaultMonoSubsets)mono.manifests+</DefaultMonoSubsets>
    <DefaultMonoSubsets Condition="$(_subset.Contains('+mono.wasmworkload+'))">$(DefaultMonoSubsets)mono.manifests+</DefaultMonoSubsets>
    <DefaultMonoSubsets Condition="'$(PrimaryRuntimeFlavor)' != 'Mono'">$(DefaultMonoSubsets)mono.tools+</DefaultMonoSubsets>
    <DefaultMonoSubsets Condition="'$(TargetsMobile)' != 'true'">$(DefaultMonoSubsets)host.native+</DefaultMonoSubsets>
    <DefaultLibrariesSubsets Condition="'$(BuildTargetFramework)' == '$(NetCoreAppCurrent)' or&#xD;&#xA;                                        '$(BuildTargetFramework)' == '' or&#xD;&#xA;                                        '$(BuildAllConfigurations)' == 'true'">libs.native+</DefaultLibrariesSubsets>
    <DefaultLibrariesSubsets>$(DefaultLibrariesSubsets)libs.sfx+libs.oob+libs.pretest</DefaultLibrariesSubsets>
    <DefaultToolsSubsets>tools.illink</DefaultToolsSubsets>
    <DefaultHostSubsets>host.native+host.tools+host.pkg</DefaultHostSubsets>
    <DefaultHostSubsets Condition="'$(DotNetBuildFromSource)' != 'true'">$(DefaultHostSubsets)+host.tests</DefaultHostSubsets>
    <DefaultHostSubsets Condition="'$(RuntimeFlavor)' != '$(PrimaryRuntimeFlavor)'" />
    <DefaultHostSubsets Condition="'$(RuntimeFlavor)' != '$(PrimaryRuntimeFlavor)' and '$(TargetsMobile)' != 'true'">host.native</DefaultHostSubsets>
    <DefaultPacksSubsets>packs.product</DefaultPacksSubsets>
    <DefaultPacksSubsets Condition="'$(BuildMonoAOTCrossCompilerOnly)' != 'true' and '$(DotNetBuildFromSource)' != 'true'">$(DefaultPacksSubsets)+packs.tests</DefaultPacksSubsets>
    <DefaultPacksSubsets Condition="'$(DotNetBuildFromSource)' == 'true'">$(DefaultPacksSubsets)+packs.installers</DefaultPacksSubsets>
    <DefaultPacksSubsets Condition="'$(RuntimeFlavor)' != 'Mono' and '$(ForceBuildMobileManifests)' == 'true'">$(DefaultPacksSubsets)+mono.manifests</DefaultPacksSubsets>
  </PropertyGroup>
  <PropertyGroup>
    <_subset>$(_subset.Replace('+clr.paltests+', '+clr.paltests+clr.paltestlist+'))</_subset>
    <_subset>$(_subset.Replace('+clr+', '+$(DefaultCoreClrSubsets)+'))</_subset>
    <_subset>$(_subset.Replace('+clr.aot+', '+$(DefaultNativeAotSubsets)+'))</_subset>
    <_subset>$(_subset.Replace('+mono+', '+$(DefaultMonoSubsets)+'))</_subset>
    <_subset>$(_subset.Replace('+libs+', '+$(DefaultLibrariesSubsets)+'))</_subset>
    <_subset>$(_subset.Replace('+tools+', '+$(DefaultToolsSubsets)+'))</_subset>
    <_subset>$(_subset.Replace('+host+', '+$(DefaultHostSubsets)+'))</_subset>
    <_subset>$(_subset.Replace('+packs+', '+$(DefaultPacksSubsets)+'))</_subset>
    <!-- Surround _subset in dashes to simplify checks below -->
    <_subset>+$(_subset.Trim('+'))+</_subset>
    <ClrRuntimeBuildSubsets />
    <ClrDefaultRuntimeBuildSubsets>ClrRuntimeSubset=true;ClrJitSubset=true;ClrILToolsSubset=true</ClrDefaultRuntimeBuildSubsets>
  </PropertyGroup>
  <PropertyGroup>
    <!-- CLR NativeAot only builds in a subset of the matrix -->
    <NativeAotSupported Condition="('$(TargetOS)' == 'windows' or '$(TargetOS)' == 'linux' or '$(TargetOS)' == 'osx' or '$(TargetOS)' == 'maccatalyst' or '$(TargetOS)' == 'iossimulator' or '$(TargetOS)' == 'ios' or '$(TargetOS)' == 'tvossimulator' or '$(TargetOS)' == 'tvos' or '$(TargetOS)' == 'freebsd') and ('$(TargetArchitecture)' == 'x64' or '$(TargetArchitecture)' == 'arm64')">true</NativeAotSupported>
    <!-- If we're building clr.nativeaotlibs and not building the CLR runtime, compile libraries against NativeAOT CoreLib -->
    <UseNativeAotCoreLib Condition="'$(TestNativeAot)' == 'true' or ($(_subset.Contains('+clr.nativeaotlibs+')) and !$(_subset.Contains('+clr.native+')) and !$(_subset.Contains('+clr.runtime+')))">true</UseNativeAotCoreLib>
  </PropertyGroup>
  <ItemGroup>
    <!-- CoreClr -->
    <SubsetName Include="Clr" Description="The full CoreCLR runtime. Equivalent to: $(DefaultCoreClrSubsets)" />
    <SubsetName Include="Clr.NativePrereqs" Description="Managed tools that support building the native components of the runtime (such as DacTableGen)." />
    <SubsetName Include="Clr.ILTools" Description="The CoreCLR IL tools (ilasm/ildasm)." />
    <SubsetName Include="Clr.Runtime" Description="The CoreCLR .NET runtime. Includes clr.jit, clr.iltools, clr.hosts." />
    <SubsetName Include="Clr.Native" Description="All CoreCLR native non-test components, including the runtime, jits, and other native tools. Includes clr.hosts, clr.runtime, clr.jit, clr.alljits, clr.paltests, clr.iltools, clr.nativeaotruntime, clr.spmi." />
    <SubsetName Include="Clr.Aot" Description="Everything needed for Native AOT workloads, including clr.alljits, clr.tools, clr.nativeaotlibs, and clr.nativeaotruntime" />
    <SubsetName Include="Clr.NativeAotLibs" Description="The CoreCLR native AOT CoreLib and other low level class libraries." />
    <SubsetName Include="Clr.NativeAotRuntime" Description="The stripped-down CoreCLR native AOT runtime." />
    <SubsetName Include="Clr.CrossArchTools" Description="The cross-targeted CoreCLR tools." />
    <SubsetName Include="Clr.PalTests" OnDemand="true" Description="The CoreCLR PAL tests." />
    <SubsetName Include="Clr.PalTestList" OnDemand="true" Description="Generate the list of the CoreCLR PAL tests. When using the command line, use Clr.PalTests instead." />
    <SubsetName Include="Clr.Hosts" Description="The CoreCLR corerun test host." />
    <SubsetName Include="Clr.Jit" Description="The JIT for the CoreCLR .NET runtime." />
    <SubsetName Include="Clr.AllJits" Description="All of the cross-targeting JIT compilers for the CoreCLR .NET runtime." />
    <SubsetName Include="Clr.Spmi" Description="SuperPMI, a tool for CoreCLR JIT testing." />
    <SubsetName Include="Clr.CoreLib" Description="The managed System.Private.CoreLib library for CoreCLR." />
    <SubsetName Include="Clr.NativeCoreLib" Description="Run crossgen on System.Private.CoreLib library for CoreCLR." />
    <SubsetName Include="Clr.Tools" Description="Managed tools that support CoreCLR development and testing." />
    <SubsetName Include="Clr.ToolsTests" OnDemand="true" Description="Unit tests for the clr.tools subset." />
    <SubsetName Include="Clr.Packages" Description="The projects that produce NuGet packages for the CoreCLR runtime, crossgen, and IL tools." />
    <SubsetName Include="LinuxDac" Condition="$([MSBuild]::IsOsPlatform(Windows))" Description="The cross-OS Windows-&gt;libc-based Linux DAC. Skipped on x86." />
    <SubsetName Include="AlpineDac" Condition="$([MSBuild]::IsOsPlatform(Windows))" OnDemand="true" Description="The cross-OS Windows-&gt;musl-libc-based Linux DAC. Skipped on x86" />
    <SubsetName Include="CrossDacPack" OnDemand="true" Description="Packaging of cross OS DAC. Requires all assets needed to be present at a folder specified by $(CrossDacArtifactsDir). See 'Microsoft.CrossOsDiag.Private.CoreCLR.proj' for details." />
    <!-- Mono -->
    <SubsetName Include="Mono" Description="The Mono runtime and CoreLib. Equivalent to: $(DefaultMonoSubsets)" />
    <SubsetName Include="Mono.Runtime" Description="The Mono .NET runtime." />
    <SubsetName Include="Mono.AotCross" Description="The cross-compiler runtime for Mono AOT." />
    <SubsetName Include="Mono.CoreLib" Description="The managed System.Private.CoreLib library for Mono." />
    <SubsetName Include="Mono.Manifests" Description="The NuGet packages with manifests defining the mobile and Blazor workloads." />
    <SubsetName Include="Mono.Packages" Description="The projects that produce NuGet packages for the Mono runtime." />
    <SubsetName Include="Mono.Tools" Description="Tooling that helps support Mono development and testing." />
    <SubsetName Include="Mono.WasmRuntime" Description="The Emscripten runtime." />
    <SubsetName Include="Mono.WasiRuntime" Description="The WASI runtime." />
    <SubsetName Include="Mono.WasmWorkload" Description="*Helper* subset for building some pre-requisites for wasm workload testing, useful on CI." />
    <SubsetName Include="Mono.MsCorDbi" Description="The implementation of ICorDebug interface." />
    <SubsetName Include="Mono.Workloads" OnDemand="true" Description="Builds the installers and the insertion metadata for Blazor workloads." />
    <!-- Tools -->
    <SubsetName Include="Tools" Description="Additional runtime tools projects. Equivalent to: $(DefaultToolsSubsets)" />
    <SubsetName Include="Tools.ILLink" Description="The projects that produce illink and analyzer tools for trimming." />
    <SubsetName Include="Tools.ILLinkTests" OnDemand="true" Description="Unit tests for the tools.illink subset." />
    <!-- Host -->
    <SubsetName Include="Host" Description="The .NET hosts, packages, hosting libraries, and tests. Equivalent to: $(DefaultHostSubsets)" />
    <SubsetName Include="Host.Native" Description="The .NET hosts." />
    <SubsetName Include="Host.Pkg" Description="The .NET host packages." />
    <SubsetName Include="Host.Tools" Description="The .NET hosting libraries." />
    <SubsetName Include="Host.Tests" Description="The .NET hosting tests." />
    <!-- Libs -->
    <SubsetName Include="Libs" Description="The libraries native part, refs and source assemblies, test infra and packages, but NOT the tests (use Libs.Tests to request those explicitly). Equivalent to: $(DefaultLibrariesSubsets)" />
    <SubsetName Include="Libs.Native" Description="The native libraries used in the shared framework." />
    <SubsetName Include="Libs.Sfx" Description="The managed shared framework libraries." />
    <SubsetName Include="Libs.Oob" Description="The managed out-of-band libraries." />
    <SubsetName Include="Libs.Ref" OnDemand="true" Description="The managed reference libraries." />
    <SubsetName Include="Libs.Src" OnDemand="true" Description="The managed implementation libraries." />
    <SubsetName Include="Libs.PreTest" Description="Test assets which are necessary to run tests." />
    <SubsetName Include="Libs.Tests" OnDemand="true" Description="The test projects. Note that building this doesn't execute tests: you must also pass the '-test' argument." />
    <!-- Packs -->
    <SubsetName Include="Packs" Description="Builds the shared framework packs, archives, bundles, installers, and the framework pack tests. Equivalent to: $(DefaultPacksSubsets)" />
    <SubsetName Include="Packs.Product" Description="Builds the shared framework packs, archives, bundles, and installers." />
    <SubsetName Include="Packs.Installers" Description="Builds the shared framework bundles and installers." />
    <SubsetName Include="Packs.Tests" Description="The framework pack tests." />
    <!-- Utility -->
    <SubsetName Include="publish" OnDemand="true" Description="Generate asset manifests and prepare to publish to BAR." />
    <SubsetName Include="RegenerateDownloadTable" OnDemand="true" Description="Regenerates the nightly build download table" />
    <SubsetName Include="RegenerateThirdPartyNotices" OnDemand="true" Description="Regenerates the THIRD-PARTY-NOTICES.TXT file based on other repos' TPN files." />
    <SubsetName Include="tasks" OnDemand="true" Description="Build the repo local task projects." />
  </ItemGroup>
  <!-- Default targets, parallelization and configurations. -->
  <ItemDefinitionGroup>
    <ProjectToBuild>
      <Test>false</Test>
      <Pack>false</Pack>
      <Publish>false</Publish>
      <BuildInParallel>false</BuildInParallel>
    </ProjectToBuild>
  </ItemDefinitionGroup>
  <!-- CoreClr sets -->
  <ItemGroup Condition="$(_subset.Contains('+clr.corelib+'))">
    <ProjectToBuild Include="$(CoreClrProjectRoot)System.Private.CoreLib\System.Private.CoreLib.csproj" Category="clr" />
  </ItemGroup>
  <PropertyGroup Condition="$(_subset.Contains('+clr.hosts+'))">
    <ClrRuntimeBuildSubsets>$(ClrRuntimeBuildSubsets);ClrHostsSubset=true</ClrRuntimeBuildSubsets>
  </PropertyGroup>
  <PropertyGroup Condition="$(_subset.Contains('+clr.runtime+'))">
    <ClrRuntimeBuildSubsets>$(ClrRuntimeBuildSubsets);ClrRuntimeSubset=true</ClrRuntimeBuildSubsets>
  </PropertyGroup>
  <PropertyGroup Condition="$(_subset.Contains('+clr.native+'))">
    <ClrRuntimeBuildSubsets>$(ClrRuntimeBuildSubsets);ClrFullNativeBuild=true</ClrRuntimeBuildSubsets>
  </PropertyGroup>
  <PropertyGroup Condition="$(_subset.Contains('+clr.jit+'))">
    <ClrRuntimeBuildSubsets>$(ClrRuntimeBuildSubsets);ClrJitSubset=true</ClrRuntimeBuildSubsets>
  </PropertyGroup>
  <PropertyGroup Condition="$(_subset.Contains('+clr.paltests+'))">
    <ClrRuntimeBuildSubsets>$(ClrRuntimeBuildSubsets);ClrPalTestsSubset=true</ClrRuntimeBuildSubsets>
  </PropertyGroup>
  <PropertyGroup Condition="$(_subset.Contains('+clr.alljits+'))">
    <ClrRuntimeBuildSubsets>$(ClrRuntimeBuildSubsets);ClrAllJitsSubset=true</ClrRuntimeBuildSubsets>
  </PropertyGroup>
  <PropertyGroup Condition="$(_subset.Contains('+clr.iltools+'))">
    <ClrRuntimeBuildSubsets>$(ClrRuntimeBuildSubsets);ClrILToolsSubset=true</ClrRuntimeBuildSubsets>
  </PropertyGroup>
  <PropertyGroup Condition="$(_subset.Contains('+clr.nativeaotruntime+')) and '$(NativeAotSupported)' == 'true'">
    <ClrRuntimeBuildSubsets>$(ClrRuntimeBuildSubsets);ClrNativeAotSubset=true</ClrRuntimeBuildSubsets>
  </PropertyGroup>
  <PropertyGroup Condition="$(_subset.Contains('+clr.spmi+'))">
    <ClrRuntimeBuildSubsets>$(ClrRuntimeBuildSubsets);ClrSpmiSubset=true</ClrRuntimeBuildSubsets>
  </PropertyGroup>
  <ItemGroup Condition="'$(ClrRuntimeBuildSubsets)' != '' or $(_subset.Contains('+clr.nativeprereqs+'))">
    <ProjectToBuild Include="$(CoreClrProjectRoot)runtime-prereqs.proj" Category="clr" />
  </ItemGroup>
  <ItemGroup Condition="'$(ClrRuntimeBuildSubsets)' != ''">
    <ProjectToBuild Include="$(CoreClrProjectRoot)runtime.proj" AdditionalProperties="%(AdditionalProperties);$(ClrRuntimeBuildSubsets)" Category="clr" />
  </ItemGroup>
  <!--
    Build the CoreCLR cross tools when we're doing a cross build and either we're building any CoreCLR native tools for platforms CoreCLR fully supports or when someone explicitly requests them.
    The cross tools are used as part of the build process with the downloaded build tools, so we need to build them for the host architecture and build them as unsanitized binaries.
    -->
  <ItemGroup Condition="(('$(ClrRuntimeBuildSubsets)' != '' and '$(PrimaryRuntimeFlavor)' == 'CoreCLR') or $(_subset.Contains('+clr.crossarchtools+'))) and ('$(CrossBuild)' == 'true' or '$(BuildArchitecture)' != '$(TargetArchitecture)' or '$(EnableNativeSanitizers)' != '')">
    <ProjectToBuild Include="$(CoreClrProjectRoot)runtime.proj" AdditionalProperties="%(AdditionalProperties);&#xD;&#xA;                            ClrCrossComponentsSubset=true;&#xD;&#xA;                            HostArchitecture=$(BuildArchitecture);&#xD;&#xA;                            HostCrossOS=$(HostOS);&#xD;&#xA;                            PgoInstrument=false;&#xD;&#xA;                            NoPgoOptimize=true;&#xD;&#xA;                            CrossBuild=false;&#xD;&#xA;                            CMakeArgs=$(CMakeArgs) -DCLR_CROSS_COMPONENTS_BUILD=1" UndefineProperties="EnableNativeSanitizers" Category="clr" />
  </ItemGroup>
  <!--
    Build the debugging components of CoreCLR for the same target architecture as an unsanitized build whenever we build a sanitized coreclr build.
    These components are loaded into a debugger process, which generally is not a sanitized executable.
  -->
  <ItemGroup Condition="'$(ClrRuntimeBuildSubsets)' != '' and '$(EnableNativeSanitizers)' != ''">
    <ProjectToBuild Include="$(CoreClrProjectRoot)runtime.proj" AdditionalProperties="%(AdditionalProperties);&#xD;&#xA;                            ClrDebugSubset=true;&#xD;&#xA;                            PgoInstrument=false;&#xD;&#xA;                            NoPgoOptimize=true;&#xD;&#xA;                            CrossBuild=$(CrossBuild);&#xD;&#xA;                            BuildSubdirectory=unsanitized" UndefineProperties="EnableNativeSanitizers" Category="clr" />
  </ItemGroup>
  <ItemGroup Condition="$(_subset.Contains('+clr.paltestlist+'))">
    <ProjectToBuild Include="$(CoreClrProjectRoot)pal/tests/palsuite/producepaltestlist.proj" />
  </ItemGroup>
  <PropertyGroup>
    <CrossDacHostArch>x64</CrossDacHostArch>
    <CrossDacHostArch Condition="'$(TargetArchitecture)' == 'arm'">x86</CrossDacHostArch>
  </PropertyGroup>
  <ItemGroup Condition="$(_subset.Contains('+linuxdac+')) and $([MSBuild]::IsOsPlatform(Windows))  and ('$(BuildArchitecture)' == 'x64' or '$(BuildArchitecture)' == 'x86') and '$(TargetArchitecture)' != 'x86'">
    <ProjectToBuild Include="$(CoreClrProjectRoot)runtime.proj" AdditionalProperties="%(AdditionalProperties);&#xD;&#xA;                            ClrCrossComponentsSubset=true;&#xD;&#xA;                            HostArchitecture=$(CrossDacHostArch);&#xD;&#xA;                            PgoInstrument=false;&#xD;&#xA;                            NoPgoOptimize=true;&#xD;&#xA;                            TargetOS=linux;&#xD;&#xA;                            CMakeArgs=$(CMakeArgs) -DCLR_CROSS_COMPONENTS_BUILD=1" Category="clr" />
  </ItemGroup>
  <ItemGroup Condition="$(_subset.Contains('+alpinedac+')) and $([MSBuild]::IsOsPlatform(Windows)) and ('$(BuildArchitecture)' == 'x64' or '$(BuildArchitecture)' == 'x86') and '$(TargetArchitecture)' != 'x86'">
    <ProjectToBuild Include="$(CoreClrProjectRoot)runtime.proj" AdditionalProperties="%(AdditionalProperties);&#xD;&#xA;                            ClrCrossComponentsSubset=true;&#xD;&#xA;                            HostArchitecture=$(CrossDacHostArch);&#xD;&#xA;                            PgoInstrument=false;&#xD;&#xA;                            NoPgoOptimize=true;&#xD;&#xA;                            TargetOS=alpine;&#xD;&#xA;                            CMakeArgs=$(CMakeArgs) -DCLR_CROSS_COMPONENTS_BUILD=1" Category="clr" />
  </ItemGroup>
  <ItemGroup Condition="$(_subset.Contains('+crossdacpack+'))">
    <ProjectToBuild Include="$(CoreClrProjectRoot).nuget\Microsoft.CrossOsDiag.Private.CoreCLR\Microsoft.CrossOsDiag.Private.CoreCLR.proj" Category="clr" />
  </ItemGroup>
  <ItemGroup Condition="$(_subset.Contains('+clr.tools+'))">
    <ProjectToBuild Include="$(CoreClrProjectRoot)tools\runincontext\runincontext.csproj;&#xD;&#xA;                             $(CoreClrProjectRoot)tools\tieringtest\tieringtest.csproj;&#xD;&#xA;                             $(CoreClrProjectRoot)tools\r2rdump\R2RDump.csproj;&#xD;&#xA;                             $(CoreClrProjectRoot)tools\dotnet-pgo\dotnet-pgo.csproj;&#xD;&#xA;                             $(CoreClrProjectRoot)tools\aot\ILCompiler\repro\repro.csproj;&#xD;&#xA;                             $(CoreClrProjectRoot)tools\r2rtest\R2RTest.csproj" Category="clr" Condition="'$(DotNetBuildFromSource)' != 'true'" />
    <ProjectToBuild Include="$(CoreClrProjectRoot)tools\aot\crossgen2\crossgen2.csproj" Category="clr" />
    <ProjectToBuild Include="$(CoreClrProjectRoot)tools\aot\ILCompiler.Build.Tasks\ILCompiler.Build.Tasks.csproj" Category="clr" Condition="'$(NativeAotSupported)' == 'true'" />
    <ProjectToBuild Include="$(CoreClrProjectRoot)tools\aot\ILCompiler\ILCompiler.csproj" Category="clr" Condition="'$(NativeAotSupported)' == 'true'" />
    <ProjectToBuild Include="$(CoreClrProjectRoot)nativeaot\BuildIntegration\BuildIntegration.proj" Category="clr" Condition="'$(NativeAotSupported)' == 'true'" />
    <ProjectToBuild Condition="'$(NativeAotSupported)' == 'true' and ('$(CrossBuild)' == 'true' or '$(BuildArchitecture)' != '$(TargetArchitecture)' or '$(HostOS)' != '$(TargetOS)' or '$(EnableNativeSanitizers)' != '')" Include="$(CoreClrProjectRoot)tools\aot\ILCompiler\ILCompiler_crossarch.csproj" Category="clr" />
    <ProjectToBuild Include="$(CoreClrProjectRoot)tools\aot\crossgen2\crossgen2_inbuild.csproj" Category="clr" />
    <ProjectToBuild Condition="'$(TargetOS)' == 'windows' or ('$(TargetOS)' == 'linux' and ('$(TargetArchitecture)' == 'x64' or '$(TargetArchitecture)' == 'arm64')) or '$(TargetOS)' == 'osx'" Include="$(CoreClrProjectRoot)tools\SuperFileCheck\SuperFileCheck.csproj" Category="clr" />
  </ItemGroup>
  <ItemGroup Condition="$(_subset.Contains('+clr.toolstests+'))">
    <ProjectToBuild Include="$(CoreClrProjectRoot)tools\aot\ILCompiler.TypeSystem.Tests\ILCompiler.TypeSystem.Tests.csproj" Test="true" Category="clr" Condition="'$(DotNetBuildFromSource)' != 'true'" />
    <ProjectToBuild Include="$(CoreClrProjectRoot)tools\aot\ILCompiler.Compiler.Tests\ILCompiler.Compiler.Tests.csproj" Test="true" Category="clr" Condition="'$(DotNetBuildFromSource)' != 'true' and '$(NativeAotSupported)' == 'true'" />
    <ProjectToBuild Include="$(CoreClrProjectRoot)tools\aot\Mono.Linker.Tests\Mono.Linker.Tests.csproj" Test="true" Category="clr" Condition="'$(DotNetBuildFromSource)' != 'true' and '$(NativeAotSupported)' == 'true'" />
  </ItemGroup>
  <ItemGroup Condition="$(_subset.Contains('+tools.illink+'))">
    <ProjectToBuild Include="$(ToolsProjectRoot)illink\src\linker\Mono.Linker.csproj" Category="tools" />
    <ProjectToBuild Include="$(ToolsProjectRoot)illink\src\ILLink.Tasks\ILLink.Tasks.csproj" Category="tools" />
    <ProjectToBuild Include="$(ToolsProjectRoot)illink\src\analyzer\analyzer.csproj" Category="tools" />
    <ProjectToBuild Include="$(ToolsProjectRoot)illink\src\ILLink.RoslynAnalyzer\ILLink.RoslynAnalyzer.csproj" Category="tools" />
    <ProjectToBuild Include="$(ToolsProjectRoot)illink\src\linker\ref\Mono.Linker.csproj" Category="tools" />
    <ProjectToBuild Include="$(ToolsProjectRoot)illink\src\tlens\tlens.csproj" Category="tools" />
    <ProjectToBuild Include="$(ToolsProjectRoot)illink\src\ILLink.CodeFix\ILLink.CodeFixProvider.csproj" Category="tools" />
  </ItemGroup>
  <ItemGroup Condition="$(_subset.Contains('+tools.illinktests+'))">
    <ProjectToBuild Include="$(ToolsProjectRoot)illink\test\Mono.Linker.Tests\Mono.Linker.Tests.csproj" Test="true" Category="tools" />
    <ProjectToBuild Include="$(ToolsProjectRoot)illink\test\Mono.Linker.Tests.Cases\Mono.Linker.Tests.Cases.csproj" Test="true" Category="tools" />
    <ProjectToBuild Include="$(ToolsProjectRoot)illink\test\Mono.Linker.Tests.Cases.Expectations\Mono.Linker.Tests.Cases.Expectations.csproj" Test="true" Category="tools" />
    <ProjectToBuild Include="$(ToolsProjectRoot)illink\test\ILLink.Tasks.Tests\ILLink.Tasks.Tests.csproj" Test="true" Category="tools" />
    <ProjectToBuild Include="$(ToolsProjectRoot)illink\test\ILLink.RoslynAnalyzer.Tests\ILLink.RoslynAnalyzer.Tests.csproj" Test="true" Category="tools" />
    <ProjectToBuild Include="$(ToolsProjectRoot)illink\test\ILLink.RoslynAnalyzer.Tests.Generator\ILLink.RoslynAnalyzer.Tests.Generator.csproj" Test="true" Category="tools" />
  </ItemGroup>
  <ItemGroup Condition="$(_subset.Contains('+clr.nativecorelib+'))">
    <ProjectToBuild Include="$(CoreClrProjectRoot)crossgen-corelib.proj" Category="clr" />
  </ItemGroup>
  <ItemGroup Condition="$(_subset.Contains('+clr.packages+')) and '$(PgoInstrument)' != 'true'">
    <ProjectToBuild Include="$(CoreClrProjectRoot).nuget\coreclr-packages.proj" Pack="true" Category="clr" />
    <ProjectToBuild Include="$(CoreClrProjectRoot)tools\dotnet-pgo\dotnet-pgo-pack.proj" Pack="true" Category="clr" Condition="'$(DotNetBuildFromSource)' != 'true' and '$(RuntimeFlavor)' != 'Mono'" />
  </ItemGroup>
  <ItemGroup Condition="$(_subset.Contains('+clr.nativeaotlibs+')) and '$(NativeAotSupported)' == 'true'">
    <ProjectToBuild Include="$(CoreClrProjectRoot)nativeaot\**\src\*.csproj" Category="clr" />
  </ItemGroup>
  <!-- Mono sets -->
  <ItemGroup Condition="$(_subset.Contains('+mono.llvm+')) or $(_subset.Contains('+mono.aotcross+')) or '$(TargetOS)' == 'ios' or '$(TargetOS)' == 'iossimulator' or '$(TargetOS)' == 'tvos' or '$(TargetOS)' == 'tvossimulator' or '$(TargetOS)' == 'maccatalyst' or '$(TargetOS)' == 'android' or '$(TargetOS)' == 'browser' or '$(TargetOS)' == 'wasi' or '$(TargetsLinuxBionic)' == 'true'">
    <ProjectToBuild Include="$(MonoProjectRoot)llvm\llvm-init.proj" Category="mono" />
  </ItemGroup>
  <ItemGroup Condition="$(_subset.Contains('+mono.manifests+'))">
    <ProjectToBuild Include="$(MonoProjectRoot)nuget\manifest-packages.proj" Category="mono" Pack="true" />
  </ItemGroup>
  <ItemGroup Condition="$(_subset.Contains('+mono.packages+'))">
    <ProjectToBuild Include="$(MonoProjectRoot)nuget\mono-packages.proj" Category="mono" Pack="true" />
  </ItemGroup>
  <ItemGroup Condition="$(_subset.Contains('+mono.runtime+'))">
    <ProjectToBuild Include="$(MonoProjectRoot)mono.proj" AdditionalProperties="%(AdditionalProperties);MonoMsCorDbi=$(_subset.Contains('+mono.mscordbi+'))" Category="mono" />
  </ItemGroup>
  <ItemGroup Condition="$(_subset.Contains('+mono.aotcross+'))">
    <ProjectToBuild Include="$(MonoProjectRoot)monoaotcross.proj" Category="mono" />
  </ItemGroup>
  <ItemGroup Condition="$(_subset.Contains('+mono.corelib+'))">
    <ProjectToBuild Include="$(MonoProjectRoot)System.Private.CoreLib\System.Private.CoreLib.csproj" Category="mono" />
  </ItemGroup>
  <ItemGroup Condition="$(_subset.Contains('+mono.tools+'))">
    <ProjectToBuild Include="$(CoreClrProjectRoot)tools\dotnet-pgo\dotnet-pgo.csproj;" Category="mono" />
  </ItemGroup>
  <ItemGroup Condition="$(_subset.Contains('+mono.workloads+'))">
    <ProjectToBuild Include="$(WorkloadsProjectRoot)\workloads.csproj" Category="mono" />
  </ItemGroup>
  <ItemGroup Condition="$(_subset.Contains('+mono.wasmworkload+'))">
    <ProjectToBuild Include="$(MonoProjectRoot)\wasm\workloads.proj" Category="mono" />
  </ItemGroup>
  <!-- Host sets -->
  <ItemGroup Condition="$(_subset.Contains('+host.native+'))">
    <CorehostProjectToBuild Include="$(SharedNativeRoot)corehost\corehost.proj" SignPhase="Binaries" />
    <ProjectToBuild Include="@(CorehostProjectToBuild)" Pack="true" Category="host" />
  </ItemGroup>
  <ItemGroup Condition="$(_subset.Contains('+host.tools+'))">
    <ManagedProjectToBuild Include="$(InstallerProjectRoot)managed\**\*.csproj" SignPhase="Binaries" />
    <ProjectToBuild Include="@(ManagedProjectToBuild)" BuildInParallel="true" Pack="true" Category="host" />
  </ItemGroup>
  <ItemGroup Condition="$(_subset.Contains('+host.pkg+')) and '$(PgoInstrument)' != 'true'">
    <PkgprojProjectToBuild Include="$(InstallerProjectRoot)pkg\projects\host-packages.proj" SignPhase="MsiFiles" />
    <ProjectToBuild Include="@(PkgprojProjectToBuild)" Pack="true" Category="host" />
  </ItemGroup>
  <!-- Libraries sets -->
  <ItemGroup Condition="$(_subset.Contains('+libs.native+'))">
    <ProjectToBuild Include="$(SharedNativeRoot)libs\build-native.proj" Category="libs" />
  </ItemGroup>
  <ItemGroup Condition="$(_subset.Contains('+libs.ref+')) or $(_subset.Contains('+libs.src+')) or $(_subset.Contains('+libs.sfx+'))">
    <ProjectToBuild Include="$(LibrariesProjectRoot)sfx.proj" Category="libs" Condition="'$(BuildTargetFramework)' == '$(NetCoreAppCurrent)' or&#xD;&#xA;                               '$(BuildTargetFramework)' == '' or&#xD;&#xA;                               '$(BuildAllConfigurations)' == 'true'">
      <AdditionalProperties Condition="$(_subset.Contains('+libs.ref+'))">%(AdditionalProperties);RefOnly=true</AdditionalProperties>
    </ProjectToBuild>
  </ItemGroup>
  <ItemGroup Condition="$(_subset.Contains('+libs.ref+')) or $(_subset.Contains('+libs.src+')) or $(_subset.Contains('+libs.oob+'))">
    <ProjectToBuild Include="$(LibrariesProjectRoot)oob.proj" Category="libs">
      <AdditionalProperties Condition="$(_subset.Contains('+libs.ref+'))">%(AdditionalProperties);RefOnly=true</AdditionalProperties>
    </ProjectToBuild>
  </ItemGroup>
  <ItemGroup Condition="$(_subset.Contains('+mono.wasmruntime+'))">
    <ProjectToBuild Include="$(LibrariesProjectRoot)\System.Runtime.InteropServices.JavaScript\src\System.Runtime.InteropServices.JavaScript.csproj" Category="mono" />
    <ProjectToBuild Include="$(MonoProjectRoot)wasm\wasm.proj" Category="mono" />
  </ItemGroup>
  <ItemGroup Condition="$(_subset.Contains('+mono.wasiruntime+'))">
    <ProjectToBuild Include="$(MonoProjectRoot)wasi\wasi.proj" Category="mono" />
  </ItemGroup>
  <ItemGroup Condition="$(_subset.Contains('+libs.pretest+'))">
    <ProjectToBuild Include="$(LibrariesProjectRoot)pretest.proj" Category="libs" />
  </ItemGroup>
  <ItemGroup Condition="$(_subset.Contains('+libs.tests+'))">
    <ProjectToBuild Include="$(LibrariesProjectRoot)tests.proj" Category="libs" Test="true" />
  </ItemGroup>
  <!-- Host.tests subset (consumes live built libraries assets so needs to come after libraries) -->
  <ItemGroup Condition="$(_subset.Contains('+host.tests+'))">
    <TestProjectToBuild Include="$(InstallerProjectRoot)tests\Microsoft.NET.HostModel.Tests\AppHost.Bundle.Tests\AppHost.Bundle.Tests.csproj" />
    <TestProjectToBuild Include="$(InstallerProjectRoot)tests\Microsoft.NET.HostModel.Tests\Microsoft.NET.HostModel.AppHost.Tests\Microsoft.NET.HostModel.AppHost.Tests.csproj" />
    <TestProjectToBuild Include="$(InstallerProjectRoot)tests\Microsoft.NET.HostModel.Tests\Microsoft.NET.HostModel.Bundle.Tests\Microsoft.NET.HostModel.Bundle.Tests.csproj" />
    <TestProjectToBuild Include="$(InstallerProjectRoot)tests\Microsoft.NET.HostModel.Tests\Microsoft.NET.HostModel.ComHost.Tests\Microsoft.NET.HostModel.ComHost.Tests.csproj" />
    <TestProjectToBuild Include="$(InstallerProjectRoot)tests\HostActivation.Tests\HostActivation.Tests.csproj" />
    <ProjectToBuild Include="@(TestProjectToBuild)" BuildInParallel="true" Test="true" Category="host" />
  </ItemGroup>
  <!-- Packs sets -->
  <Choose>
    <When Condition="$(_subset.Contains('+packs.product+'))">
      <ItemGroup Condition="'$(PgoInstrument)' != 'true'">
        <SharedFrameworkProjectToBuild Condition="'$(BuildMonoAOTCrossCompilerOnly)' != 'true'" Include="$(InstallerProjectRoot)pkg\sfx\Microsoft.NETCore.App\Microsoft.NETCore.App.Ref.sfxproj" />
      </ItemGroup>
      <ItemGroup Condition="'$(BuildNativeAOTRuntimePack)' != 'true' and '$(PgoInstrument)' != 'true'">
        <SharedFrameworkProjectToBuild Condition="'$(RuntimeFlavor)' == '$(PrimaryRuntimeFlavor)'" Include="$(InstallerProjectRoot)pkg\sfx\Microsoft.NETCore.App\Microsoft.NETCore.App.Host.sfxproj" />
        <SharedFrameworkProjectToBuild Condition="'$(RuntimeFlavor)' != 'Mono' and '$(PgoInstrument)' != 'true'" Include="$(InstallerProjectRoot)pkg\sfx\Microsoft.NETCore.App\Microsoft.NETCore.App.Crossgen2.sfxproj" />
        <SharedFrameworkProjectToBuild Condition="'$(RuntimeFlavor)' == '$(PrimaryRuntimeFlavor)'" Include="$(InstallerProjectRoot)pkg\sfx\installers\dotnet-host.proj" />
        <SharedFrameworkProjectToBuild Condition="'$(RuntimeFlavor)' == '$(PrimaryRuntimeFlavor)'" Include="$(InstallerProjectRoot)pkg\sfx\installers\dotnet-hostfxr.proj" />
        <SharedFrameworkProjectToBuild Condition="'$(BuildNETStandard21TargetingPack)' == 'true' and '$(RuntimeFlavor)' == '$(PrimaryRuntimeFlavor)'" Include="$(InstallerProjectRoot)pkg\sfx\installers\netstandard2.1.proj" />
        <SharedFrameworkProjectToBuild Condition="'$(RuntimeFlavor)' == '$(PrimaryRuntimeFlavor)'" Include="$(InstallerProjectRoot)pkg\sfx\installers\dotnet-runtime-deps\*.proj" />
        <SharedFrameworkProjectToBuild Condition="'$(RuntimeFlavor)' == '$(PrimaryRuntimeFlavor)'" Include="$(InstallerProjectRoot)pkg\archives\dotnet-nethost.proj" />
        <SharedFrameworkProjectToBuild Condition="'$(MonoCrossAOTTargetOS)' != ''" Include="$(InstallerProjectRoot)pkg\sfx\Microsoft.NETCore.App\monocrossaot.sfxproj" Pack="true" />
      </ItemGroup>
      <ItemGroup>
        <ProjectToBuild Condition="'$(NativeAotSupported)' == 'true' and '$(RuntimeFlavor)' != 'Mono' and '$(PgoInstrument)' != 'true'" Include="$(InstallerProjectRoot)\pkg\projects\nativeaot-packages.proj" Category="packs" />
      </ItemGroup>
      <ItemGroup>
        <SharedFrameworkProjectToBuild Condition="'$(BuildMonoAOTCrossCompilerOnly)' != 'true'" Include="$(InstallerProjectRoot)pkg\sfx\Microsoft.NETCore.App\Microsoft.NETCore.App.Runtime.sfxproj" />
        <SharedFrameworkProjectToBuild Condition="'$(BuildNativeAOTRuntimePack)' != 'true' and '$(RuntimeFlavor)' == '$(PrimaryRuntimeFlavor)'" Include="$(InstallerProjectRoot)pkg\sfx\bundle\Microsoft.NETCore.App.Bundle.bundleproj" />
        <ProjectToBuild Include="@(SharedFrameworkProjectToBuild)" Category="packs" />
      </ItemGroup>
    </When>
  </Choose>
  <ItemGroup Condition="$(_subset.Contains('+packs.installers+')) AND '$(PgoInstrument)' != 'true'">
    <InstallerProjectToBuild Include="$(InstallerProjectRoot)pkg\sfx\installers.proj" />
    <ProjectToBuild Include="@(InstallerProjectToBuild)" Category="packs" />
  </ItemGroup>
  <ItemGroup Condition="$(_subset.Contains('+packs.tests+')) AND '$(PgoInstrument)' != 'true'">
    <TestProjectToBuild Include="$(InstallerProjectRoot)tests\Microsoft.DotNet.CoreSetup.Packaging.Tests\Microsoft.DotNet.CoreSetup.Packaging.Tests.csproj" />
    <ProjectToBuild Include="@(TestProjectToBuild)" BuildInParallel="true" Test="true" Category="packs" />
  </ItemGroup>
  <ItemGroup Condition="$(_subset.Contains('+publish+'))">
    <ProjectToBuild Include="$(InstallerProjectRoot)prepare-artifacts.proj" Pack="true" Category="publish" />
  </ItemGroup>
  <!-- Utility -->
  <ItemGroup Condition="$(_subset.Contains('+regeneratedownloadtable+'))">
    <ProjectToBuild Include="$(RepositoryEngineeringDir)regenerate-download-table.proj" Pack="true" />
  </ItemGroup>
  <ItemGroup Condition="$(_subset.Contains('+regeneratethirdpartynotices+'))">
    <ProjectToBuild Include="$(RepositoryEngineeringDir)regenerate-third-party-notices.proj" Pack="false" BuildInParallel="false" />
  </ItemGroup>
  <!-- Tasks-->
  <ItemGroup Condition="$(_subset.Contains('+tasks+'))">
    <ProjectToBuild Include="$(RepoTasksDir)tasks.proj" Pack="false" Category="tasks" />
  </ItemGroup>
  <!-- Set default configurations. -->
  <ItemGroup>
    <ProjectToBuild Update="@(ProjectToBuild)">
      <AdditionalProperties Condition="'%(ProjectToBuild.Category)' == 'clr'">%(AdditionalProperties);Configuration=$(CoreCLRConfiguration)</AdditionalProperties>
      <AdditionalProperties Condition="'%(ProjectToBuild.Category)' == 'mono'">%(AdditionalProperties);Configuration=$(MonoConfiguration)</AdditionalProperties>
      <AdditionalProperties Condition="'%(ProjectToBuild.Category)' == 'libs'">%(AdditionalProperties);Configuration=$(LibrariesConfiguration)</AdditionalProperties>
      <AdditionalProperties Condition="'%(ProjectToBuild.Category)' == 'host'">%(AdditionalProperties);Configuration=$(HostConfiguration)</AdditionalProperties>
      <AdditionalProperties Condition="'%(ProjectToBuild.Category)' == 'tasks'">%(AdditionalProperties);Configuration=$(TasksConfiguration)</AdditionalProperties>
      <!-- Propagate configurations for cross-subset builds -->
      <AdditionalProperties>%(AdditionalProperties);LibrariesConfiguration=$(LibrariesConfiguration)</AdditionalProperties>
      <AdditionalProperties>%(AdditionalProperties);HostConfiguration=$(HostConfiguration)</AdditionalProperties>
      <AdditionalProperties>%(AdditionalProperties);TasksConfiguration=$(TasksConfiguration)</AdditionalProperties>
    </ProjectToBuild>
  </ItemGroup>
  <!--
============================================================================================================================================
  </Import>

C:\Users\calope\source\repos\runtime8\Directory.Build.props
============================================================================================================================================
-->
  <PropertyGroup>
    <CoreLibSharedDir>$([MSBuild]::NormalizeDirectory('$(LibrariesProjectRoot)', 'System.Private.CoreLib', 'src'))</CoreLibSharedDir>
    <CoreLibRefDir>$([MSBuild]::NormalizeDirectory('$(LibrariesProjectRoot)', 'System.Private.CoreLib', 'ref'))</CoreLibRefDir>
    <CoreLibProject Condition="'$(RuntimeFlavor)' == 'CoreCLR'">$([MSBuild]::NormalizePath('$(CoreClrProjectRoot)', 'System.Private.CoreLib', 'System.Private.CoreLib.csproj'))</CoreLibProject>
    <CoreLibProject Condition="'$(RuntimeFlavor)' == 'Mono'">$([MSBuild]::NormalizePath('$(MonoProjectRoot)', 'System.Private.CoreLib', 'System.Private.CoreLib.csproj'))</CoreLibProject>
    <CoreLibProject Condition="'$(UseNativeAotCoreLib)' == 'true'">$([MSBuild]::NormalizePath('$(CoreClrProjectRoot)', 'nativeaot', 'System.Private.CoreLib', 'src', 'System.Private.CoreLib.csproj'))</CoreLibProject>
    <UriProject>$([MSBuild]::NormalizePath('$(LibrariesProjectRoot)', 'System.Private.Uri', 'src', 'System.Private.Uri.csproj'))</UriProject>
    <!-- this property is used by the SDK to pull in mono-based runtime packs -->
    <UseMonoRuntime Condition="'$(UseMonoRuntime)' == '' and '$(RuntimeFlavor)' == 'Mono'">true</UseMonoRuntime>
  </PropertyGroup>
  <!-- Packaging -->
  <PropertyGroup>
    <GitHubRepositoryName>runtime</GitHubRepositoryName>
    <RepositoryUrl>https://github.com/dotnet/$(GitHubRepositoryName)</RepositoryUrl>
    <PackageProjectUrl>https://dot.net</PackageProjectUrl>
    <Owners>microsoft,dotnetframework</Owners>
    <IncludeSymbols>true</IncludeSymbols>
    <LicenseFile>$(MSBuildThisFileDirectory)LICENSE.TXT</LicenseFile>
    <PackageLicenseExpression>MIT</PackageLicenseExpression>
    <PackageRequireLicenseAcceptance>false</PackageRequireLicenseAcceptance>
    <Copyright>$(CopyrightNetFoundation)</Copyright>
    <PackageThirdPartyNoticesFile>$(MSBuildThisFileDirectory)THIRD-PARTY-NOTICES.TXT</PackageThirdPartyNoticesFile>
    <PackageReleaseNotes>https://go.microsoft.com/fwlink/?LinkID=799421</PackageReleaseNotes>
    <IsPrivateAssembly>$(MSBuildProjectName.Contains('Private'))</IsPrivateAssembly>
    <!-- Private packages should not be stable -->
    <SuppressFinalPackageVersion Condition="'$(SuppressFinalPackageVersion)' == '' and $(IsPrivateAssembly)">true</SuppressFinalPackageVersion>
    <!-- We don't want Private packages to be shipped to NuGet.org -->
    <IsShippingPackage Condition="$(IsPrivateAssembly)">false</IsShippingPackage>
    <PlaceholderFile>$(RepositoryEngineeringDir)_._</PlaceholderFile>
  </PropertyGroup>
  <!-- Flow these properties to consuming projects. -->
  <ItemDefinitionGroup>
    <TargetPathWithTargetPlatformMoniker>
      <IsPrivateAssembly>$(IsPrivateAssembly.ToLowerInvariant())</IsPrivateAssembly>
    </TargetPathWithTargetPlatformMoniker>
  </ItemDefinitionGroup>
  <!-- Language configuration -->
  <PropertyGroup>
    <!-- default to allowing all language features -->
    <LangVersion>preview</LangVersion>
    <!-- default to max warnlevel -->
    <AnalysisLevel Condition="'$(MSBuildProjectExtension)' == '.csproj'">preview</AnalysisLevel>
    <LangVersion Condition="'$(MSBuildProjectExtension)' == '.vbproj'">latest</LangVersion>
    <!-- Enables Strict mode for Roslyn compiler -->
    <Features>strict;nullablePublicOnly</Features>
    <TreatWarningsAsErrors Condition="'$(TreatWarningsAsErrors)' == ''">true</TreatWarningsAsErrors>
    <!-- Warnings to always disable -->
    <NoWarn>$(NoWarn),CS8969</NoWarn>
    <!-- Always pass portable to override arcade sdk which uses embedded for local builds -->
    <DebugType>portable</DebugType>
    <DebugSymbols>true</DebugSymbols>
    <KeepNativeSymbols Condition="'$(KeepNativeSymbols)' == '' and '$(DotNetBuildFromSource)' == 'true'">true</KeepNativeSymbols>
    <KeepNativeSymbols Condition="'$(KeepNativeSymbols)' == ''">false</KeepNativeSymbols>
    <!-- Used for launchSettings.json and runtime config files. -->
    <AppDesignerFolder>Properties</AppDesignerFolder>
    <!-- By default the SDK produces ref assembly for 5.0 or later -->
    <ProduceReferenceAssembly>false</ProduceReferenceAssembly>
  </PropertyGroup>
  <!-- Define test projects and companions -->
  <PropertyGroup Condition="$(MSBuildProjectFullPath.Contains('$([System.IO.Path]::DirectorySeparatorChar)tests$([System.IO.Path]::DirectorySeparatorChar)'))">
    <IsTestProject Condition="$(MSBuildProjectName.EndsWith('.UnitTests')) or $(MSBuildProjectName.EndsWith('.Tests'))">true</IsTestProject>
    <IsTrimmingTestProject Condition="$(MSBuildProjectName.EndsWith('.TrimmingTests'))">true</IsTrimmingTestProject>
    <IsNativeAotTestProject Condition="$(MSBuildProjectName.EndsWith('.NativeAotTests'))">true</IsNativeAotTestProject>
    <IsPublishedAppTestProject Condition="'$(IsTrimmingTestProject)' == 'true' or '$(IsNativeAotTestProject)' == 'true'">true</IsPublishedAppTestProject>
    <IsTestSupportProject Condition="'$(IsTestProject)' != 'true' and '$(IsPublishedAppTestProject)' != 'true'">true</IsTestSupportProject>
    <!-- Treat test assemblies as non-shipping (do not publish or sign them). -->
    <IsShipping Condition="'$(IsTestProject)' == 'true' or '$(IsTestSupportProject)' == 'true' or '$(IsPublishedAppTestProject)' == 'true'">false</IsShipping>
  </PropertyGroup>
  <PropertyGroup>
    <!-- Treat as a generator project if either the parent or the parent parent directory is named gen. -->
    <IsGeneratorProject Condition="$([System.IO.Path]::GetFileName('$(MSBuildProjectDirectory)')) == 'gen' or&#xD;&#xA;                                   $([System.IO.Path]::GetFileName('$([System.IO.Path]::GetFullPath('$(MSBuildProjectDirectory)\..'))')) == 'gen'">true</IsGeneratorProject>
    <IsSourceProject Condition="'$(IsSourceProject)' == '' and&#xD;&#xA;                                '$(IsReferenceAssemblyProject)' != 'true' and&#xD;&#xA;                                '$(IsGeneratorProject)' != 'true' and&#xD;&#xA;                                '$(IsTestProject)' != 'true' and&#xD;&#xA;                                '$(IsPublishedAppTestProject)' != 'true' and&#xD;&#xA;                                '$(IsTestSupportProject)' != 'true' and&#xD;&#xA;                                '$(UsingMicrosoftDotNetSharedFrameworkSdk)' != 'true' and&#xD;&#xA;                                '$(MSBuildProjectExtension)' != '.pkgproj' and&#xD;&#xA;                                '$(UsingMicrosoftNoTargetsSdk)' != 'true' and&#xD;&#xA;                                '$(UsingMicrosoftTraversalSdk)' != 'true'">true</IsSourceProject>
  </PropertyGroup>
  <PropertyGroup Condition="'$(IsReferenceAssemblyProject)' == 'true'">
    <!-- Reference assemblies are special and don't initialize fields or have empty finalizers, etc. -->
    <RunAnalyzers>false</RunAnalyzers>
    <!-- disable warnings about unused fields -->
    <NoWarn>$(NoWarn);CS0169;CS0649;CS8618</NoWarn>
    <!-- disable CS8597 because we throw null on reference assemblies. -->
    <NoWarn>$(NoWarn);CS8597</NoWarn>
    <!-- We base calls from constructors with null literals. -->
    <NoWarn>$(NoWarn);CS8625</NoWarn>
    <!-- We dont need to add null annotation within the ref for explicit interface methods. -->
    <NoWarn>$(NoWarn);CS8617</NoWarn>
    <!-- No symbols are produced for ref assemblies, but some parts of the SDK still expect pdbs, so we explicitly tell it there are none. -->
    <!-- Must be set after importing Arcade to override its defaults. -->
    <DebugType>none</DebugType>
    <!-- Don't try to publish PDBs for ref assemblies that have none. -->
    <PublishWindowsPdb>false</PublishWindowsPdb>
  </PropertyGroup>
  <!-- All reference assemblies should have a ReferenceAssemblyAttribute and the 0x70 flag which prevents them from loading. -->
  <ItemGroup Condition="'$(IsReferenceAssemblyProject)' == 'true'">
    <AssemblyAttribute Include="System.Runtime.CompilerServices.ReferenceAssemblyAttribute" />
    <AssemblyAttribute Include="System.Reflection.AssemblyFlags">
      <_Parameter1>(System.Reflection.AssemblyNameFlags)0x70</_Parameter1>
      <_Parameter1_IsLiteral>true</_Parameter1_IsLiteral>
    </AssemblyAttribute>
  </ItemGroup>
  <PropertyGroup Condition="'$(IsSourceProject)' == 'true'">
    <!-- Must be defined in a props file as imports in Microsoft.DotNet.ApiCompat.Task.targets depend on it. -->
    <ApiCompatValidateAssemblies>true</ApiCompatValidateAssemblies>
  </PropertyGroup>
  <PropertyGroup Condition="'$(IsGeneratorProject)' == 'true'">
    <!-- Unique assembly versions increases(3x) the compiler throughput during reference package updates. -->
    <AutoGenerateAssemblyVersion>true</AutoGenerateAssemblyVersion>
    <!-- To suppress warnings about resetting the assembly version.-->
    <AssemblyVersion />
    <!-- Enforce extended rules around API usages in analyzers and generators to ensure our generators follow best practices. -->
    <EnforceExtendedAnalyzerRules>true</EnforceExtendedAnalyzerRules>
  </PropertyGroup>
  <!-- Warnings that should be disabled in our test projects. -->
  <PropertyGroup Condition="'$(IsTestProject)' == 'true' or '$(IsTestSupportProject)' == 'true' or '$(IsPublishedAppTestProject)' == 'true'">
    <!-- we need to re-enable BinaryFormatter within test projects since some tests exercise these code paths to ensure compat -->
    <EnableUnsafeBinaryFormatterSerialization>true</EnableUnsafeBinaryFormatterSerialization>
    <!-- don't warn on usage of BinaryFormatter or legacy serialization infrastructure from test projects -->
    <NoWarn>$(NoWarn);SYSLIB0011;SYSLIB0050;SYSLIB0051</NoWarn>
    <!-- don't warn about unnecessary trim warning suppressions. can be removed with preview 6. -->
    <NoWarn>$(NoWarn);IL2121</NoWarn>
    <!-- allow nullable annotated files to be incorporated into tests without warning -->
    <Nullable Condition="'$(Nullable)' == '' and '$(Language)' == 'C#'">annotations</Nullable>
  </PropertyGroup>
  <PropertyGroup>
    <CustomBeforeNoTargets>$(RepositoryEngineeringDir)NoTargetsSdk.BeforeTargets.targets</CustomBeforeNoTargets>
    <CustomAfterTraversalTargets>$(RepositoryEngineeringDir)TraversalSdk.AfterTargets.targets</CustomAfterTraversalTargets>
  </PropertyGroup>
  <!--
============================================================================================================================================
  </Import>

C:\Users\calope\source\repos\runtime8\src\libraries\Directory.Build.props
============================================================================================================================================
-->
  <PropertyGroup>
    <BeforeTargetFrameworkInferenceTargets>$(RepositoryEngineeringDir)BeforeTargetFrameworkInference.targets</BeforeTargetFrameworkInferenceTargets>
    <ShouldUnsetParentConfigurationAndPlatform>false</ShouldUnsetParentConfigurationAndPlatform>
    <GeneratePlatformNotSupportedAssemblyHeaderFile>$(RepositoryEngineeringDir)LicenseHeader.txt</GeneratePlatformNotSupportedAssemblyHeaderFile>
  </PropertyGroup>
  <ItemGroup>
    <!-- Projects which are manually built. -->
    <ProjectExclusions Include="$(CommonTestPath)System\Net\Prerequisites\**\*.csproj" />
  </ItemGroup>
  <!--
============================================================================================================================================
  <Import Project="NetCoreAppLibrary.props">

C:\Users\calope\source\repos\runtime8\src\libraries\NetCoreAppLibrary.props
============================================================================================================================================
-->
  <PropertyGroup>
    <!-- The trailing semicolon is important for each entry! -->
    <NetFxReference>
      mscorlib;
      Microsoft.VisualBasic;
      System;
      System.ComponentModel.DataAnnotations;
      System.Configuration;
      System.Core;
      System.Data;
      System.Drawing;
      System.IO.Compression.FileSystem;
      System.Net;
      System.Numerics;
      System.Runtime.Serialization;
      System.Security;
      System.ServiceProcess;
      System.ServiceModel.Web;
      System.Transactions;
      System.Web;
      System.Windows;
      System.Xml;
      System.Xml.Serialization;
      System.Xml.Linq;
      WindowsBase;
    </NetFxReference>
    <NetCoreAppLibrary>
      $(NetFxReference)
      netstandard;
      Microsoft.CSharp;
      Microsoft.VisualBasic.Core;
      Microsoft.Win32.Primitives;
      Microsoft.Win32.Registry;
      System.AppContext;
      System.Buffers;
      System.Collections;
      System.Collections.Concurrent;
      System.Collections.Immutable;
      System.Collections.NonGeneric;
      System.Collections.Specialized;
      System.ComponentModel;
      System.ComponentModel.Annotations;
      System.ComponentModel.EventBasedAsync;
      System.ComponentModel.Primitives;
      System.ComponentModel.TypeConverter;
      System.Console;
      System.Data.Common;
      System.Data.DataSetExtensions;
      System.Diagnostics.Contracts;
      System.Diagnostics.Debug;
      System.Diagnostics.DiagnosticSource;
      System.Diagnostics.FileVersionInfo;
      System.Diagnostics.Process;
      System.Diagnostics.StackTrace;
      System.Diagnostics.TextWriterTraceListener;
      System.Diagnostics.Tools;
      System.Diagnostics.TraceSource;
      System.Diagnostics.Tracing;
      System.Drawing.Primitives;
      System.Dynamic.Runtime;
      System.Formats.Asn1;
      System.Formats.Tar;
      System.Globalization;
      System.Globalization.Calendars;
      System.Globalization.Extensions;
      System.IO;
      System.IO.Compression;
      System.IO.Compression.Brotli;
      System.IO.Compression.ZipFile;
      System.IO.FileSystem;
      System.IO.FileSystem.AccessControl;
      System.IO.FileSystem.DriveInfo;
      System.IO.FileSystem.Primitives;
      System.IO.FileSystem.Watcher;
      System.IO.IsolatedStorage;
      System.IO.MemoryMappedFiles;
      System.IO.Pipes;
      System.IO.Pipes.AccessControl;
      System.IO.UnmanagedMemoryStream;
      System.Linq;
      System.Linq.Expressions;
      System.Linq.Parallel;
      System.Linq.Queryable;
      System.Memory;
      System.Net.Http;
      System.Net.Http.Json;
      System.Net.HttpListener;
      System.Net.Mail;
      System.Net.NameResolution;
      System.Net.NetworkInformation;
      System.Net.Ping;
      System.Net.Primitives;
      System.Net.Quic;
      System.Net.Requests;
      System.Net.Security;
      System.Net.ServicePoint;
      System.Net.Sockets;
      System.Net.WebClient;
      System.Net.WebHeaderCollection;
      System.Net.WebProxy;
      System.Net.WebSockets;
      System.Net.WebSockets.Client;
      System.Numerics.Vectors;
      System.ObjectModel;
      System.Private.CoreLib;
      System.Private.DataContractSerialization;
      System.Private.Uri;
      System.Private.Xml;
      System.Private.Xml.Linq;
      System.Reflection;
      System.Reflection.DispatchProxy;
      System.Reflection.Emit;
      System.Reflection.Emit.ILGeneration;
      System.Reflection.Emit.Lightweight;
      System.Reflection.Extensions;
      System.Reflection.Metadata;
      System.Reflection.Primitives;
      System.Reflection.TypeExtensions;
      System.Resources.Reader;
      System.Resources.ResourceManager;
      System.Resources.Writer;
      System.Runtime;
      System.Runtime.CompilerServices.Unsafe;
      System.Runtime.CompilerServices.VisualC;
      System.Runtime.Extensions;
      System.Runtime.Handles;
      System.Runtime.InteropServices;
      System.Runtime.InteropServices.JavaScript;
      System.Runtime.InteropServices.RuntimeInformation;
      System.Runtime.Intrinsics;
      System.Runtime.Loader;
      System.Runtime.Numerics;
      System.Runtime.Serialization.Formatters;
      System.Runtime.Serialization.Json;
      System.Runtime.Serialization.Primitives;
      System.Runtime.Serialization.Xml;
      System.Security.AccessControl;
      System.Security.Claims;
      System.Security.Cryptography;
      System.Security.Cryptography.Algorithms;
      System.Security.Cryptography.Cng;
      System.Security.Cryptography.Csp;
      System.Security.Cryptography.Encoding;
      System.Security.Cryptography.OpenSsl;
      System.Security.Cryptography.Primitives;
      System.Security.Cryptography.X509Certificates;
      System.Security.Principal;
      System.Security.Principal.Windows;
      System.Security.SecureString;
      System.Text.Encoding;
      System.Text.Encoding.CodePages;
      System.Text.Encoding.Extensions;
      System.Text.Encodings.Web;
      System.Text.Json;
      System.Text.RegularExpressions;
      System.Threading;
      System.Threading.Channels;
      System.Threading.Overlapped;
      System.Threading.Tasks;
      System.Threading.Tasks.Dataflow;
      System.Threading.Tasks.Extensions;
      System.Threading.Tasks.Parallel;
      System.Threading.Thread;
      System.Threading.ThreadPool;
      System.Threading.Timer;
      System.Transactions.Local;
      System.ValueTuple;
      System.Web.HttpUtility;
      System.Xml.ReaderWriter;
      System.Xml.XDocument;
      System.Xml.XmlDocument;
      System.Xml.XmlSerializer;
      System.Xml.XPath;
      System.Xml.XPath.XDocument;
    </NetCoreAppLibrary>
    <!-- List .NETCoreApp shared framework generator project names below. -->
    <NetCoreAppLibraryGenerator>
      ComInterfaceGenerator;
      LibraryImportGenerator;
      JSImportGenerator;
      Microsoft.Interop.SourceGeneration;
      System.Text.Json.SourceGeneration.Roslyn4.4;
      System.Text.RegularExpressions.Generator;
    </NetCoreAppLibraryGenerator>
    <AspNetCoreAppLibrary>
      Microsoft.Extensions.Caching.Abstractions;
      Microsoft.Extensions.Caching.Memory;
      Microsoft.Extensions.Configuration;
      Microsoft.Extensions.Configuration.Abstractions;
      Microsoft.Extensions.Configuration.Binder;
      Microsoft.Extensions.Configuration.CommandLine;
      Microsoft.Extensions.Configuration.EnvironmentVariables;
      Microsoft.Extensions.Configuration.FileExtensions;
      Microsoft.Extensions.Configuration.Ini;
      Microsoft.Extensions.Configuration.Json;
      Microsoft.Extensions.Configuration.UserSecrets;
      Microsoft.Extensions.Configuration.Xml;
      Microsoft.Extensions.DependencyInjection;
      Microsoft.Extensions.DependencyInjection.Abstractions;
      Microsoft.Extensions.Diagnostics;
      Microsoft.Extensions.Diagnostics.Abstractions;
      Microsoft.Extensions.FileProviders.Abstractions;
      Microsoft.Extensions.FileProviders.Composite;
      Microsoft.Extensions.FileProviders.Physical;
      Microsoft.Extensions.FileSystemGlobbing;
      Microsoft.Extensions.Hosting;
      Microsoft.Extensions.Hosting.Abstractions;
      Microsoft.Extensions.Http;
      Microsoft.Extensions.Logging;
      Microsoft.Extensions.Logging.Abstractions;
      Microsoft.Extensions.Logging.Configuration;
      Microsoft.Extensions.Logging.Console;
      Microsoft.Extensions.Logging.Debug;
      Microsoft.Extensions.Logging.EventLog;
      Microsoft.Extensions.Logging.EventSource;
      Microsoft.Extensions.Logging.TraceSource;
      Microsoft.Extensions.Options;
      Microsoft.Extensions.Options.ConfigurationExtensions;
      Microsoft.Extensions.Options.DataAnnotations;
      Microsoft.Extensions.Primitives;
      System.Diagnostics.EventLog;
      System.IO.Pipelines;
      System.Security.Cryptography.Xml;
      System.Threading.RateLimiting;
    </AspNetCoreAppLibrary>
    <WindowsDesktopCoreAppLibrary>
      Microsoft.Win32.Registry.AccessControl;
      Microsoft.Win32.SystemEvents;
      System.CodeDom;
      System.Configuration.ConfigurationManager;
      System.Diagnostics.EventLog;
      System.Diagnostics.PerformanceCounter;
      System.DirectoryServices;
      System.IO.Packaging;
      System.Resources.Extensions;
      System.Security.Cryptography.Pkcs;
      System.Security.Cryptography.ProtectedData;
      System.Security.Cryptography.Xml;
      System.Security.Permissions;
      System.Threading.AccessControl;
      System.Windows.Extensions;
    </WindowsDesktopCoreAppLibrary>
  </PropertyGroup>
  <!-- Make available as an item. -->
  <ItemGroup>
    <NetFxReference Include="$(NetFxReference)" />
    <NetCoreAppLibrary Include="$(NetCoreAppLibrary)" />
    <NetCoreAppLibraryGenerator Include="$(NetCoreAppLibraryGenerator)" />
    <AspNetCoreAppLibrary Include="$(AspNetCoreAppLibrary)" />
    <WindowsDesktopCoreAppLibrary Include="$(WindowsDesktopCoreAppLibrary)" />
  </ItemGroup>
  <!--
============================================================================================================================================
  </Import>

C:\Users\calope\source\repos\runtime8\src\libraries\Directory.Build.props
============================================================================================================================================
-->
  <!--<Import Project="$(RepositoryEngineeringDir)testing\linker\trimmingTests.props" Condition="'$(IsPublishedAppTestProject)' == 'true'" />-->
  <PropertyGroup>
    <!-- Default any assembly not specifying a key to use the Open Key -->
    <StrongNameKeyId>Open</StrongNameKeyId>
    <!-- Microsoft.Extensions projects have a separate StrongNameKeyId -->
    <StrongNameKeyId Condition="$(MSBuildProjectName.StartsWith('Microsoft.Extensions.'))">MicrosoftAspNetCore</StrongNameKeyId>
    <!-- We can't generate an apphost without restoring the targeting pack. -->
    <UseAppHost>false</UseAppHost>
    <EnableDefaultItems>false</EnableDefaultItems>
    <!-- Libraries packages use the incremental servicing infrastructure. -->
    <PackageUseIncrementalServicingVersion>true</PackageUseIncrementalServicingVersion>
  </PropertyGroup>
  <!-- Language configuration -->
  <PropertyGroup>
    <GenFacadesIgnoreBuildAndRevisionMismatch>true</GenFacadesIgnoreBuildAndRevisionMismatch>
    <!-- Disable analyzers for tests and unsupported projects -->
    <RunAnalyzers Condition="'$(IsTestProject)' != 'true' and '$(IsSourceProject)' != 'true' and '$(IsGeneratorProject)' != 'true'">false</RunAnalyzers>
    <!-- Enable documentation file generation by the compiler for all libraries except for vbproj. -->
    <GenerateDocumentationFile Condition="'$(IsSourceProject)' == 'true' and '$(MSBuildProjectExtension)' != '.vbproj'">true</GenerateDocumentationFile>
    <CLSCompliant Condition="'$(CLSCompliant)' == '' and '$(IsTestProject)' != 'true' and '$(IsTestSupportProject)' != 'true'">true</CLSCompliant>
    <!-- Nullability is enabled by default except for test projects, which instead default to annotations. -->
    <Nullable Condition="'$(Nullable)' == '' and '$(IsTestProject)' != 'true'">enable</Nullable>
    <Nullable Condition="'$(Nullable)' == '' and '$(IsTestProject)' == 'true'">annotations</Nullable>
    <!-- EnableAOTAnalyzer is enabled by default for src projects. -->
    <EnableAOTAnalyzer Condition="'$(EnableAOTAnalyzer)' == '' and '$(IsSourceProject)' == 'true'">true</EnableAOTAnalyzer>
  </PropertyGroup>
  <!-- Set up common paths -->
  <PropertyGroup>
    <!-- Helix properties -->
    <OSPlatformConfig>$(TargetOS).$(Platform).$(Configuration)</OSPlatformConfig>
    <TestArchiveRoot>$(ArtifactsDir)helix/</TestArchiveRoot>
    <TestArchiveTestsRoot Condition="$(IsFunctionalTest) != true">$(TestArchiveRoot)tests/</TestArchiveTestsRoot>
    <TestArchiveTestsRoot Condition="$(IsFunctionalTest) == true">$(TestArchiveRoot)runonly/</TestArchiveTestsRoot>
    <TestArchiveTestsRoot Condition="'$(Scenario)' == 'BuildWasmApps'">$(TestArchiveRoot)buildwasmapps/</TestArchiveTestsRoot>
    <TestArchiveTestsDir>$(TestArchiveTestsRoot)$(OSPlatformConfig)/</TestArchiveTestsDir>
    <TestArchiveRuntimeRoot>$(TestArchiveRoot)runtime/</TestArchiveRuntimeRoot>
    <UseAppBundleRootForBuildingTests Condition="'$(ArchiveTests)' == 'true' and '$(BuildTestsOnHelix)' != 'true' and '$(TargetsAppleMobile)' == 'true'">true</UseAppBundleRootForBuildingTests>
    <AppBundleRoot Condition="'$(UseAppBundleRootForBuildingTests)' == 'true'">$(ArtifactsDir)bundles\</AppBundleRoot>
    <CommonPathRoot>$([MSBuild]::NormalizeDirectory('$(LibrariesProjectRoot)', 'Common'))</CommonPathRoot>
    <CommonPath>$([MSBuild]::NormalizeDirectory('$(CommonPathRoot)', 'src'))</CommonPath>
    <CommonTestPath>$([MSBuild]::NormalizeDirectory('$(CommonPathRoot)', 'tests'))</CommonTestPath>
  </PropertyGroup>
  <ItemGroup Condition="'$(IsTestProject)' == 'true' and '$(SkipTestUtilitiesReference)' != 'true'">
    <ProjectReference Include="$(CommonTestPath)TestUtilities\TestUtilities.csproj" />
  </ItemGroup>
  <PropertyGroup Condition="'$(IsTestProject)' == 'true'">
    <EnableTestSupport>true</EnableTestSupport>
    <!-- TODO: Remove these conditions when VSTest is used in CI. -->
    <EnableRunSettingsSupport Condition="'$(ContinuousIntegrationBuild)' != 'true'">true</EnableRunSettingsSupport>
    <EnableCoverageSupport Condition="'$(ContinuousIntegrationBuild)' != 'true'">true</EnableCoverageSupport>
  </PropertyGroup>
  <!-- To enable the interpreter for mono desktop, we need to pass an env switch -->
  <PropertyGroup>
    <MonoEnvOptions Condition="'$(MonoEnvOptions)' == '' and '$(TargetsMobile)' != 'true' and '$(MonoForceInterpreter)' == 'true'">--interpreter</MonoEnvOptions>
  </PropertyGroup>
  <PropertyGroup Condition="'$(TargetsMobile)' == 'true'">
    <SdkWithNoWorkloadForTestingPath>$(ArtifactsBinDir)dotnet-none\</SdkWithNoWorkloadForTestingPath>
    <SdkWithNoWorkloadForTestingPath>$([MSBuild]::NormalizeDirectory($(SdkWithNoWorkloadForTestingPath)))</SdkWithNoWorkloadForTestingPath>
    <SdkWithNoWorkloadStampPath>$(SdkWithNoWorkloadForTestingPath)version-$(SdkVersionForWorkloadTesting).stamp</SdkWithNoWorkloadStampPath>
    <SdkWithNoWorkload_WorkloadStampPath>$(SdkWithNoWorkloadForTestingPath)workload.stamp</SdkWithNoWorkload_WorkloadStampPath>
    <SdkWithWorkloadForTestingPath Condition="'$(TargetOS)' == 'browser'">$(ArtifactsBinDir)dotnet-latest\</SdkWithWorkloadForTestingPath>
    <SdkWithWorkloadForTestingPath Condition="'$(TargetOS)' == 'wasi'">$(ArtifactsBinDir)dotnet-latest\</SdkWithWorkloadForTestingPath>
    <SdkWithWorkloadForTestingPath Condition="'$(SdkWithWorkloadForTestingPath)' != ''">$([MSBuild]::NormalizeDirectory($(SdkWithWorkloadForTestingPath)))</SdkWithWorkloadForTestingPath>
    <SdkWithWorkloadStampPath>$(SdkWithWorkloadForTestingPath)version-$(SdkVersionForWorkloadTesting).stamp</SdkWithWorkloadStampPath>
    <SdkWithWorkload_WorkloadStampPath>$(SdkWithWorkloadForTestingPath)workload.stamp</SdkWithWorkload_WorkloadStampPath>
  </PropertyGroup>
  <!--
============================================================================================================================================
  <Import Project="$(RepositoryEngineeringDir)testing\tests.props" Condition="'$(EnableTestSupport)' == 'true'">

C:\Users\calope\source\repos\runtime8\eng\testing\tests.props
============================================================================================================================================
-->
  <PropertyGroup>
    <TestProjectName Condition="'$(TestProjectName)' == ''">$(MSBuildProjectName)</TestProjectName>
    <TestFramework Condition="'$(TestFramework)' == ''">xunit</TestFramework>
    <!-- Implicit test build support when invoking the Test target on the project directly. -->
    <TestDependsOn Condition="'$(TestNoBuild)' != 'true' and '$(BuildAllProjects)' != 'true'">Build</TestDependsOn>
    <TestDependsOn>$(TestDependsOn);GenerateRunScript;RunTests</TestDependsOn>
    <VSTestNoLogo>true</VSTestNoLogo>
    <ILLinkDescriptorsPath>$(MSBuildThisFileDirectory)ILLinkDescriptors\</ILLinkDescriptorsPath>
    <TestSingleFile Condition="'$(TestNativeAot)' == 'true'">true</TestSingleFile>
    <TestSingleFile Condition="'$(TestReadyToRun)' == 'true'">true</TestSingleFile>
  </PropertyGroup>
  <PropertyGroup Condition="'$(TargetsMobile)' == 'true'">
    <!-- Test runners are built as part of libs.pretest so we need to use libraries configuration -->
    <AppleTestRunnerDir>$([MSBuild]::NormalizeDirectory('$(ArtifactsBinDir)', 'AppleTestRunner', '$(Configuration)', '$(NetCoreAppCurrent)'))</AppleTestRunnerDir>
    <AndroidTestRunnerDir>$([MSBuild]::NormalizeDirectory('$(ArtifactsBinDir)', 'AndroidTestRunner', '$(Configuration)', '$(NetCoreAppCurrent)'))</AndroidTestRunnerDir>
    <WasmTestRunnerDir>$([MSBuild]::NormalizeDirectory('$(ArtifactsBinDir)', 'WasmTestRunner', '$(Configuration)', '$(NetCoreAppCurrent)'))</WasmTestRunnerDir>
    <RuntimeIdentifier>$(OutputRID)</RuntimeIdentifier>
    <SelfContained>true</SelfContained>
  </PropertyGroup>
  <!-- Provide runtime options to Mono (interpreter, aot, debugging, etc) -->
  <ItemGroup Condition="'$(MonoEnvOptions)' != '' and '$(TargetsMobile)' != 'true'">
    <RunScriptCommands Condition="'$(TargetsWindows)' == 'true' or '$(TargetPlatformIdentifier)' == 'windows'" Include="set MONO_ENV_OPTIONS='$(MonoEnvOptions)'" />
    <RunScriptCommands Condition="'$(TargetsWindows)' != 'true' and '$(TargetPlatformIdentifier)' != 'windows'" Include="export MONO_ENV_OPTIONS='$(MonoEnvOptions)'" />
  </ItemGroup>
  <ItemGroup Condition="'$(TestRunRequiresLiveRefPack)' == 'true'">
    <None Include="$(MicrosoftNetCoreAppRefPackRefDir)**/*.dll" CopyToOutputDirectory="PreserveNewest" CopyToPublishDirectory="PreserveNewest" LinkBase="live-ref-pack/" Visible="false" />
  </ItemGroup>
  <!--
    Unit/Functional/Integration test support.
    Supported runners: xunit.
  -->
  <!--
============================================================================================================================================
  <Import Project="$(MSBuildThisFileDirectory)xunit\xunit.props" Condition="'$(TestFramework)' == 'xunit'">

C:\Users\calope\source\repos\runtime8\eng\testing\xunit\xunit.props
============================================================================================================================================
-->
  <PropertyGroup>
    <!-- Microsoft.Net.Test.Sdk brings a lot of satellite assemblies in. -->
    <SatelliteResourceLanguages>en</SatelliteResourceLanguages>
    <TestRunnerConfigPath>$(MSBuildThisFileDirectory)xunit.runner.json</TestRunnerConfigPath>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Condition="'$(IncludeRemoteExecutor)' == 'true'" Include="Microsoft.DotNet.RemoteExecutor" Version="$(MicrosoftDotNetRemoteExecutorVersion)" />
    <!-- Excluding xunit.core/build as it enables deps file generation. -->
    <PackageReference Include="xunit" Version="$(XUnitVersion)" ExcludeAssets="build" />
    <PackageReference Include="xunit.analyzers" Version="$(XUnitAnalyzersVersion)" ExcludeAssets="build" />
    <PackageReference Include="Microsoft.DotNet.XUnitExtensions" Version="$(MicrosoftDotNetXUnitExtensionsVersion)" />
    <!--
      Microsoft.Net.Test.Sdk has a dependency on Newtonsoft.Json v9.0.1. We upgrade the dependency version
      with the one used in libraries to have a consistent set of dependency versions. Additionally this works
      around a duplicate type between System.Runtime.Serialization.Formatters and Newtonsoft.Json.
    -->
    <PackageReference Include="Newtonsoft.Json" Version="$(NewtonsoftJsonVersion)" />
  </ItemGroup>
  <ItemGroup Condition="'$(ArchiveTests)' != 'true' and '$(PublishingTestsRun)' != 'true' and '$(TestSingleFile)' != 'true'">
    <!-- Microsoft.Net.Test.Sdk brings a lot of assemblies with it. To reduce helix payload submission size we disable it on CI. -->
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="$(MicrosoftNETTestSdkVersion)" />
    <PackageReference Include="xunit.runner.visualstudio" Version="$(XUnitRunnerVisualStudioVersion)" GeneratePathProperty="true" />
    <PackageReference Include="coverlet.collector" Version="$(CoverletCollectorVersion)" />
  </ItemGroup>
  <ItemGroup>
    <None Include="$(TestRunnerConfigPath)" CopyToOutputDirectory="PreserveNewest" Visible="false" />
  </ItemGroup>
  <!--
============================================================================================================================================
  </Import>

C:\Users\calope\source\repos\runtime8\eng\testing\tests.props
============================================================================================================================================
-->
  <!--
============================================================================================================================================
  </Import>

C:\Users\calope\source\repos\runtime8\src\libraries\Directory.Build.props
============================================================================================================================================
-->
  <!-- Use msbuild path functions as that property is used in bash scripts. -->
  <ItemGroup>
    <CoverageExcludeByFile Include="$([MSBuild]::NormalizePath('$(LibrariesProjectRoot)', 'Common', 'src', 'System', 'SR.*'))" />
    <CoverageExcludeByFile Include="$([MSBuild]::NormalizePath('$(LibrariesProjectRoot)', 'Common', 'src', 'System', 'NotImplemented.cs'))" />
    <!-- Link to the testhost folder to probe additional assemblies. -->
    <CoverageIncludeDirectory Include="shared\Microsoft.NETCore.App\$(ProductVersion)" />
  </ItemGroup>
  <!--
============================================================================================================================================
  </Import>

C:\Users\calope\source\repos\runtime8\src\libraries\System.IO.Ports\Directory.Build.props
============================================================================================================================================
-->
  <PropertyGroup>
    <IncludePlatformAttributes>true</IncludePlatformAttributes>
    <UnsupportedOSPlatforms>browser;android;ios;tvos</UnsupportedOSPlatforms>
  </PropertyGroup>
  <!--
============================================================================================================================================
  </Import>

C:\Program Files\dotnet\sdk\8.0.110\Sdks\Microsoft.NET.Sdk\Sdk\Sdk.props
============================================================================================================================================
-->
  <PropertyGroup xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <ImportDirectoryBuildProps>false</ImportDirectoryBuildProps>
  </PropertyGroup>
  <!-- If ArtifactsPath or UseArtifactsOutput are set, then import .props to set ArtifactsPath here, so that BaseIntermediateOutputPath can be
       set in the ArtifactsPath.
       If the .props file is not imported here, it will be imported from Microsoft.NET.DefaultOutputPaths.targets, so that artifacts output
       properties can be set directly in the project file too (only in that case they won't affect the intermediate output). -->
  <!--<Import Project="$(MSBuildThisFileDirectory)..\targets\Microsoft.NET.DefaultArtifactsPath.props" Condition="'$(UseArtifactsOutput)' == 'true' Or '$(ArtifactsPath)' != ''" />-->
  <PropertyGroup Condition="'$(UseArtifactsOutput)' == 'true'" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <UseArtifactsIntermediateOutput Condition="'$(UseArtifactsIntermediateOutput)' == ''">true</UseArtifactsIntermediateOutput>
    <ArtifactsProjectName Condition="'$(ArtifactsProjectName)' == ''">$(MSBuildProjectName)</ArtifactsProjectName>
  </PropertyGroup>
  <PropertyGroup Condition="'$(UseArtifactsOutput)' == 'true' And '$(BaseIntermediateOutputPath)' == '' And '$(UseArtifactsIntermediateOutput)' == 'true'" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <BaseIntermediateOutputPath Condition="'$(IncludeProjectNameInArtifactsPaths)' == 'true'">$(ArtifactsPath)\obj\$(ArtifactsProjectName)\</BaseIntermediateOutputPath>
    <BaseIntermediateOutputPath Condition="'$(BaseIntermediateOutputPath)' == ''">$(ArtifactsPath)\obj\</BaseIntermediateOutputPath>
  </PropertyGroup>
  <!-- Record whether ArtifactsPath / UseArtifactsOutput was set at this point in evaluation.  We will generate an error if these properties are set
       after this point (ie in the project file). -->
  <PropertyGroup Condition="'$(UseArtifactsOutput)' == 'true'" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <_ArtifactsPathSetEarly>true</_ArtifactsPathSetEarly>
  </PropertyGroup>
  <PropertyGroup Condition="'$(MSBuildProjectFullPath)' == '$(ProjectToOverrideProjectExtensionsPath)'" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <MSBuildProjectExtensionsPath>$(ProjectExtensionsPathForSpecifiedProject)</MSBuildProjectExtensionsPath>
  </PropertyGroup>
  <!--<Import Project="$(AlternateCommonProps)" Condition="'$(AlternateCommonProps)' != ''" />-->
  <!--
============================================================================================================================================
  <Import Project="$(MSBuildExtensionsPath)\$(MSBuildToolsVersion)\Microsoft.Common.props" Condition="'$(AlternateCommonProps)' == ''">

C:\Program Files\dotnet\sdk\8.0.110\Current\Microsoft.Common.props
============================================================================================================================================
-->
  <!--
***********************************************************************************************
Microsoft.Common.props

WARNING:  DO NOT MODIFY this file unless you are knowledgeable about MSBuild and have
          created a backup copy.  Incorrect changes to this file will make it
          impossible to load or build your projects from the command-line or the IDE.

Copyright (C) Microsoft Corporation. All rights reserved.
***********************************************************************************************
-->
  <PropertyGroup>
    <ImportByWildcardBeforeMicrosoftCommonProps Condition="'$(ImportByWildcardBeforeMicrosoftCommonProps)' == ''">true</ImportByWildcardBeforeMicrosoftCommonProps>
    <ImportByWildcardAfterMicrosoftCommonProps Condition="'$(ImportByWildcardAfterMicrosoftCommonProps)' == ''">true</ImportByWildcardAfterMicrosoftCommonProps>
    <ImportUserLocationsByWildcardBeforeMicrosoftCommonProps Condition="'$(ImportUserLocationsByWildcardBeforeMicrosoftCommonProps)' == ''">true</ImportUserLocationsByWildcardBeforeMicrosoftCommonProps>
    <ImportUserLocationsByWildcardAfterMicrosoftCommonProps Condition="'$(ImportUserLocationsByWildcardAfterMicrosoftCommonProps)' == ''">true</ImportUserLocationsByWildcardAfterMicrosoftCommonProps>
    <ImportDirectoryBuildProps Condition="'$(ImportDirectoryBuildProps)' == ''">true</ImportDirectoryBuildProps>
  </PropertyGroup>
  <!--
      Determine the path to the directory build props file if the user did not disable $(ImportDirectoryBuildProps) and
      they did not already specify an absolute path to use via $(DirectoryBuildPropsPath)
  -->
  <PropertyGroup Condition="'$(ImportDirectoryBuildProps)' == 'true' and '$(DirectoryBuildPropsPath)' == ''">
    <_DirectoryBuildPropsFile Condition="'$(_DirectoryBuildPropsFile)' == ''">Directory.Build.props</_DirectoryBuildPropsFile>
    <_DirectoryBuildPropsBasePath Condition="'$(_DirectoryBuildPropsBasePath)' == ''">$([MSBuild]::GetDirectoryNameOfFileAbove($(MSBuildProjectDirectory), '$(_DirectoryBuildPropsFile)'))</_DirectoryBuildPropsBasePath>
    <DirectoryBuildPropsPath Condition="'$(_DirectoryBuildPropsBasePath)' != '' and '$(_DirectoryBuildPropsFile)' != ''">$([System.IO.Path]::Combine('$(_DirectoryBuildPropsBasePath)', '$(_DirectoryBuildPropsFile)'))</DirectoryBuildPropsPath>
  </PropertyGroup>
  <!--<Import Project="$(CustomBeforeDirectoryBuildProps)" Condition="'$(CustomBeforeDirectoryBuildProps)' != ''" />-->
  <!--<Import Project="$(DirectoryBuildPropsPath)" Condition="'$(ImportDirectoryBuildProps)' == 'true' and exists('$(DirectoryBuildPropsPath)')" />-->
  <!--<Import Project="$(CustomAfterDirectoryBuildProps)" Condition="'$(CustomAfterDirectoryBuildProps)' != ''" />-->
  <!--
      Prepare to import project extensions which usually come from packages.  Package management systems will create a file at:
        $(MSBuildProjectExtensionsPath)\$(MSBuildProjectFile).<SomethingUnique>.props

      Each package management system should use a unique moniker to avoid collisions.  It is a wild-card import so the package
      management system can write out multiple files but the order of the import is alphabetic because MSBuild sorts the list.
  -->
  <PropertyGroup>
    <!--
        The declaration of $(BaseIntermediateOutputPath) had to be moved up from Microsoft.Common.CurrentVersion.targets
        in order for the $(MSBuildProjectExtensionsPath) to use it as a default.
    -->
    <BaseIntermediateOutputPath Condition="'$(BaseIntermediateOutputPath)'=='' ">obj\</BaseIntermediateOutputPath>
    <BaseIntermediateOutputPath Condition="!HasTrailingSlash('$(BaseIntermediateOutputPath)')">$(BaseIntermediateOutputPath)\</BaseIntermediateOutputPath>
    <_InitialBaseIntermediateOutputPath>$(BaseIntermediateOutputPath)</_InitialBaseIntermediateOutputPath>
    <MSBuildProjectExtensionsPath Condition="'$(MSBuildProjectExtensionsPath)' == '' ">$(BaseIntermediateOutputPath)</MSBuildProjectExtensionsPath>
    <!--
        Import paths that are relative default to be relative to the importing file.  However, since MSBuildExtensionsPath
        defaults to BaseIntermediateOutputPath we expect it to be relative to the project directory.  So if the path is relative
        it needs to be made absolute based on the project directory.
    -->
    <MSBuildProjectExtensionsPath Condition="'$([System.IO.Path]::IsPathRooted($(MSBuildProjectExtensionsPath)))' == 'false'">$([System.IO.Path]::Combine('$(MSBuildProjectDirectory)', '$(MSBuildProjectExtensionsPath)'))</MSBuildProjectExtensionsPath>
    <MSBuildProjectExtensionsPath Condition="!HasTrailingSlash('$(MSBuildProjectExtensionsPath)')">$(MSBuildProjectExtensionsPath)\</MSBuildProjectExtensionsPath>
    <ImportProjectExtensionProps Condition="'$(ImportProjectExtensionProps)' == ''">true</ImportProjectExtensionProps>
    <_InitialMSBuildProjectExtensionsPath Condition=" '$(ImportProjectExtensionProps)' == 'true' ">$(MSBuildProjectExtensionsPath)</_InitialMSBuildProjectExtensionsPath>
  </PropertyGroup>
  <!--
============================================================================================================================================
  <Import Project="$(MSBuildProjectExtensionsPath)$(MSBuildProjectFile).*.props" Condition="'$(ImportProjectExtensionProps)' == 'true' and exists('$(MSBuildProjectExtensionsPath)')">

C:\Users\calope\source\repos\runtime8\artifacts\obj\System.IO.Ports.Tests\System.IO.Ports.Tests.csproj.nuget.g.props
============================================================================================================================================
-->
  <PropertyGroup Condition=" '$(ExcludeRestorePackageImports)' != 'true' " xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <RestoreSuccess Condition=" '$(RestoreSuccess)' == '' ">True</RestoreSuccess>
    <RestoreTool Condition=" '$(RestoreTool)' == '' ">NuGet</RestoreTool>
    <ProjectAssetsFile Condition=" '$(ProjectAssetsFile)' == '' ">$(MSBuildThisFileDirectory)project.assets.json</ProjectAssetsFile>
    <NuGetPackageRoot Condition=" '$(NuGetPackageRoot)' == '' ">C:\.tools\.nuget\packages\</NuGetPackageRoot>
    <NuGetPackageFolders Condition=" '$(NuGetPackageFolders)' == '' ">C:\.tools\.nuget\packages\;C:\Program Files\dotnet\sdk\NuGetFallbackFolder</NuGetPackageFolders>
    <NuGetProjectStyle Condition=" '$(NuGetProjectStyle)' == '' ">PackageReference</NuGetProjectStyle>
    <NuGetToolVersion Condition=" '$(NuGetToolVersion)' == '' ">6.8.1</NuGetToolVersion>
  </PropertyGroup>
  <ItemGroup Condition=" '$(ExcludeRestorePackageImports)' != 'true' " xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <SourceRoot Include="C:\.tools\.nuget\packages\" />
    <SourceRoot Include="C:\Program Files\dotnet\sdk\NuGetFallbackFolder\" />
  </ItemGroup>
  <!--<ImportGroup Condition=" '$(TargetFramework)' == '' AND '$(ExcludeRestorePackageImports)' != 'true' ">-->
  <!--
============================================================================================================================================
  <Import Project="$(NuGetPackageRoot)microsoft.net.compilers.toolset\4.8.0-7.23566.2\buildMultiTargeting\Microsoft.Net.Compilers.Toolset.props" Condition="Exists('$(NuGetPackageRoot)microsoft.net.compilers.toolset\4.8.0-7.23566.2\buildMultiTargeting\Microsoft.Net.Compilers.Toolset.props')">

C:\.tools\.nuget\packages\microsoft.net.compilers.toolset\4.8.0-7.23566.2\buildMultiTargeting\Microsoft.Net.Compilers.Toolset.props
============================================================================================================================================
-->
  <!-- Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. -->
  <!--
============================================================================================================================================
  <Import Project="$(MSBuildThisFileDirectory)..\build\$(MSBuildThisFile)">

C:\.tools\.nuget\packages\microsoft.net.compilers.toolset\4.8.0-7.23566.2\build\Microsoft.Net.Compilers.Toolset.props
============================================================================================================================================
-->
  <!-- Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE file in the project root for more information. -->
  <PropertyGroup>
    <_RoslynTargetDirectoryName Condition="'$(MSBuildRuntimeType)' == 'Core'">netcore</_RoslynTargetDirectoryName>
    <_RoslynTargetDirectoryName Condition="'$(MSBuildRuntimeType)' != 'Core'">net472</_RoslynTargetDirectoryName>
    <_RoslynTasksDirectory>$(MSBuildThisFileDirectory)..\tasks\$(_RoslynTargetDirectoryName)\</_RoslynTasksDirectory>
    <RoslynTasksAssembly>$(_RoslynTasksDirectory)Microsoft.Build.Tasks.CodeAnalysis.dll</RoslynTasksAssembly>
    <UseSharedCompilation Condition="'$(UseSharedCompilation)' == ''">true</UseSharedCompilation>
    <CSharpCoreTargetsPath>$(_RoslynTasksDirectory)Microsoft.CSharp.Core.targets</CSharpCoreTargetsPath>
    <VisualBasicCoreTargetsPath>$(_RoslynTasksDirectory)Microsoft.VisualBasic.Core.targets</VisualBasicCoreTargetsPath>
  </PropertyGroup>
  <UsingTask TaskName="Microsoft.CodeAnalysis.BuildTasks.Csc" AssemblyFile="$(RoslynTasksAssembly)" />
  <UsingTask TaskName="Microsoft.CodeAnalysis.BuildTasks.Vbc" AssemblyFile="$(RoslynTasksAssembly)" />
  <UsingTask TaskName="Microsoft.CodeAnalysis.BuildTasks.CopyRefAssembly" AssemblyFile="$(RoslynTasksAssembly)" />
  <!--
============================================================================================================================================
  </Import>

C:\.tools\.nuget\packages\microsoft.net.compilers.toolset\4.8.0-7.23566.2\buildMultiTargeting\Microsoft.Net.Compilers.Toolset.props
============================================================================================================================================
-->
  <!--
============================================================================================================================================
  </Import>

C:\Users\calope\source\repos\runtime8\artifacts\obj\System.IO.Ports.Tests\System.IO.Ports.Tests.csproj.nuget.g.props
============================================================================================================================================
-->
  <!--
============================================================================================================================================
  <Import Project="$(NuGetPackageRoot)microsoft.dotnet.build.tasks.targetframework\8.0.0-beta.24426.2\buildMultiTargeting\Microsoft.DotNet.Build.Tasks.TargetFramework.props" Condition="Exists('$(NuGetPackageRoot)microsoft.dotnet.build.tasks.targetframework\8.0.0-beta.24426.2\buildMultiTargeting\Microsoft.DotNet.Build.Tasks.TargetFramework.props')">

C:\.tools\.nuget\packages\microsoft.dotnet.build.tasks.targetframework\8.0.0-beta.24426.2\buildMultiTargeting\Microsoft.DotNet.Build.Tasks.TargetFramework.props
============================================================================================================================================
-->
  <!-- Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. -->
  <!--
============================================================================================================================================
  <Import Project="..\build\$(MSBuildThisFile)">

C:\.tools\.nuget\packages\microsoft.dotnet.build.tasks.targetframework\8.0.0-beta.24426.2\build\Microsoft.DotNet.Build.Tasks.TargetFramework.props
============================================================================================================================================
-->
  <!-- Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. -->
  <PropertyGroup>
    <DotNetBuildTasksTargetFrameworkAssembly Condition="'$(MSBuildRuntimeType)' == 'core'">..\tools\net8.0\Microsoft.DotNet.Build.Tasks.TargetFramework.dll</DotNetBuildTasksTargetFrameworkAssembly>
    <DotNetBuildTasksTargetFrameworkAssembly Condition="'$(MSBuildRuntimeType)' != 'core'">..\tools\net472\Microsoft.DotNet.Build.Tasks.TargetFramework.dll</DotNetBuildTasksTargetFrameworkAssembly>
  </PropertyGroup>
  <!--
============================================================================================================================================
  </Import>

C:\.tools\.nuget\packages\microsoft.dotnet.build.tasks.targetframework\8.0.0-beta.24426.2\buildMultiTargeting\Microsoft.DotNet.Build.Tasks.TargetFramework.props
============================================================================================================================================
-->
  <!--
============================================================================================================================================
  </Import>

C:\Users\calope\source\repos\runtime8\artifacts\obj\System.IO.Ports.Tests\System.IO.Ports.Tests.csproj.nuget.g.props
============================================================================================================================================
-->
  <!--
============================================================================================================================================
  <Import Project="$(NuGetPackageRoot)microsoft.net.test.sdk\17.4.0-preview-20220707-01\buildMultiTargeting\Microsoft.NET.Test.Sdk.props" Condition="Exists('$(NuGetPackageRoot)microsoft.net.test.sdk\17.4.0-preview-20220707-01\buildMultiTargeting\Microsoft.NET.Test.Sdk.props')">

C:\.tools\.nuget\packages\microsoft.net.test.sdk\17.4.0-preview-20220707-01\buildMultiTargeting\Microsoft.NET.Test.Sdk.props
============================================================================================================================================
-->
  <!--
 ***********************************************************************************************
 Microsoft.NET.Test.Sdk.props
 
 WARNING:  DO NOT MODIFY this file unless you are knowledgeable about MSBuild and have
           created a backup copy.  Incorrect changes to this file will make it
           impossible to load or build your projects from the command-line or the IDE.
 
 Copyright (c) .NET Foundation. All rights reserved. 
 ***********************************************************************************************
-->
  <PropertyGroup xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <TestProject>true</TestProject>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>
  <ItemGroup xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <ProjectCapability Include="TestContainer" />
  </ItemGroup>
  <!--
============================================================================================================================================
  </Import>

C:\Users\calope\source\repos\runtime8\artifacts\obj\System.IO.Ports.Tests\System.IO.Ports.Tests.csproj.nuget.g.props
============================================================================================================================================
-->
  <!--</ImportGroup>-->
  <!--<ImportGroup Condition=" '$(TargetFramework)' == 'net462' AND '$(ExcludeRestorePackageImports)' != 'true' ">-->
  <!--<Import Project="$(NuGetPackageRoot)xunit.runner.visualstudio\2.4.5\build\net462\xunit.runner.visualstudio.props" Condition="Exists('$(NuGetPackageRoot)xunit.runner.visualstudio\2.4.5\build\net462\xunit.runner.visualstudio.props')" />-->
  <!--<Import Project="$(NuGetPackageRoot)xunit.runner.console\2.4.2\build\xunit.runner.console.props" Condition="Exists('$(NuGetPackageRoot)xunit.runner.console\2.4.2\build\xunit.runner.console.props')" />-->
  <!--<Import Project="$(NuGetPackageRoot)microsoft.net.illink.tasks\8.0.0-rc.1.23406.6\build\Microsoft.NET.ILLink.Tasks.props" Condition="Exists('$(NuGetPackageRoot)microsoft.net.illink.tasks\8.0.0-rc.1.23406.6\build\Microsoft.NET.ILLink.Tasks.props')" />-->
  <!--<Import Project="$(NuGetPackageRoot)microsoft.net.compilers.toolset\4.8.0-7.23566.2\build\Microsoft.Net.Compilers.Toolset.props" Condition="Exists('$(NuGetPackageRoot)microsoft.net.compilers.toolset\4.8.0-7.23566.2\build\Microsoft.Net.Compilers.Toolset.props')" />-->
  <!--<Import Project="$(NuGetPackageRoot)microsoft.dotnet.build.tasks.targetframework\8.0.0-beta.24426.2\build\Microsoft.DotNet.Build.Tasks.TargetFramework.props" Condition="Exists('$(NuGetPackageRoot)microsoft.dotnet.build.tasks.targetframework\8.0.0-beta.24426.2\build\Microsoft.DotNet.Build.Tasks.TargetFramework.props')" />-->
  <!--<Import Project="$(NuGetPackageRoot)microsoft.codeanalysis.netanalyzers\8.0.0-preview.23614.1\buildTransitive\Microsoft.CodeAnalysis.NetAnalyzers.props" Condition="Exists('$(NuGetPackageRoot)microsoft.codeanalysis.netanalyzers\8.0.0-preview.23614.1\buildTransitive\Microsoft.CodeAnalysis.NetAnalyzers.props')" />-->
  <!--<Import Project="$(NuGetPackageRoot)microsoft.codeanalysis.csharp.codestyle\4.5.0\build\Microsoft.CodeAnalysis.CSharp.CodeStyle.props" Condition="Exists('$(NuGetPackageRoot)microsoft.codeanalysis.csharp.codestyle\4.5.0\build\Microsoft.CodeAnalysis.CSharp.CodeStyle.props')" />-->
  <!--<Import Project="$(NuGetPackageRoot)microsoft.codecoverage\17.4.0-preview-20220707-01\build\netstandard1.0\Microsoft.CodeCoverage.props" Condition="Exists('$(NuGetPackageRoot)microsoft.codecoverage\17.4.0-preview-20220707-01\build\netstandard1.0\Microsoft.CodeCoverage.props')" />-->
  <!--<Import Project="$(NuGetPackageRoot)microsoft.net.test.sdk\17.4.0-preview-20220707-01\build\net45\Microsoft.NET.Test.Sdk.props" Condition="Exists('$(NuGetPackageRoot)microsoft.net.test.sdk\17.4.0-preview-20220707-01\build\net45\Microsoft.NET.Test.Sdk.props')" />-->
  <!--</ImportGroup>-->
  <!--<ImportGroup Condition=" '$(TargetFramework)' == 'net8.0-freebsd' AND '$(ExcludeRestorePackageImports)' != 'true' ">-->
  <!--<Import Project="$(NuGetPackageRoot)xunit.runner.visualstudio\2.4.5\build\netcoreapp3.1\xunit.runner.visualstudio.props" Condition="Exists('$(NuGetPackageRoot)xunit.runner.visualstudio\2.4.5\build\netcoreapp3.1\xunit.runner.visualstudio.props')" />-->
  <!--<Import Project="$(NuGetPackageRoot)microsoft.net.illink.tasks\8.0.0-rc.1.23406.6\build\Microsoft.NET.ILLink.Tasks.props" Condition="Exists('$(NuGetPackageRoot)microsoft.net.illink.tasks\8.0.0-rc.1.23406.6\build\Microsoft.NET.ILLink.Tasks.props')" />-->
  <!--<Import Project="$(NuGetPackageRoot)microsoft.net.compilers.toolset\4.8.0-7.23566.2\build\Microsoft.Net.Compilers.Toolset.props" Condition="Exists('$(NuGetPackageRoot)microsoft.net.compilers.toolset\4.8.0-7.23566.2\build\Microsoft.Net.Compilers.Toolset.props')" />-->
  <!--<Import Project="$(NuGetPackageRoot)microsoft.dotnet.build.tasks.targetframework\8.0.0-beta.24426.2\build\Microsoft.DotNet.Build.Tasks.TargetFramework.props" Condition="Exists('$(NuGetPackageRoot)microsoft.dotnet.build.tasks.targetframework\8.0.0-beta.24426.2\build\Microsoft.DotNet.Build.Tasks.TargetFramework.props')" />-->
  <!--<Import Project="$(NuGetPackageRoot)microsoft.codeanalysis.netanalyzers\8.0.0-preview.23614.1\buildTransitive\Microsoft.CodeAnalysis.NetAnalyzers.props" Condition="Exists('$(NuGetPackageRoot)microsoft.codeanalysis.netanalyzers\8.0.0-preview.23614.1\buildTransitive\Microsoft.CodeAnalysis.NetAnalyzers.props')" />-->
  <!--<Import Project="$(NuGetPackageRoot)microsoft.codeanalysis.csharp.codestyle\4.5.0\build\Microsoft.CodeAnalysis.CSharp.CodeStyle.props" Condition="Exists('$(NuGetPackageRoot)microsoft.codeanalysis.csharp.codestyle\4.5.0\build\Microsoft.CodeAnalysis.CSharp.CodeStyle.props')" />-->
  <!--<Import Project="$(NuGetPackageRoot)microsoft.testplatform.testhost\17.4.0-preview-20220707-01\build\netcoreapp2.1\Microsoft.TestPlatform.TestHost.props" Condition="Exists('$(NuGetPackageRoot)microsoft.testplatform.testhost\17.4.0-preview-20220707-01\build\netcoreapp2.1\Microsoft.TestPlatform.TestHost.props')" />-->
  <!--<Import Project="$(NuGetPackageRoot)microsoft.codecoverage\17.4.0-preview-20220707-01\build\netstandard1.0\Microsoft.CodeCoverage.props" Condition="Exists('$(NuGetPackageRoot)microsoft.codecoverage\17.4.0-preview-20220707-01\build\netstandard1.0\Microsoft.CodeCoverage.props')" />-->
  <!--<Import Project="$(NuGetPackageRoot)microsoft.net.test.sdk\17.4.0-preview-20220707-01\build\netcoreapp2.1\Microsoft.NET.Test.Sdk.props" Condition="Exists('$(NuGetPackageRoot)microsoft.net.test.sdk\17.4.0-preview-20220707-01\build\netcoreapp2.1\Microsoft.NET.Test.Sdk.props')" />-->
  <!--<Import Project="$(NuGetPackageRoot)microsoft.dotnet.xunitconsolerunner\2.5.1-beta.24426.2\build\Microsoft.DotNet.XUnitConsoleRunner.props" Condition="Exists('$(NuGetPackageRoot)microsoft.dotnet.xunitconsolerunner\2.5.1-beta.24426.2\build\Microsoft.DotNet.XUnitConsoleRunner.props')" />-->
  <!--</ImportGroup>-->
  <!--<ImportGroup Condition=" '$(TargetFramework)' == 'net8.0-linux' AND '$(ExcludeRestorePackageImports)' != 'true' ">-->
  <!--<Import Project="$(NuGetPackageRoot)xunit.runner.visualstudio\2.4.5\build\netcoreapp3.1\xunit.runner.visualstudio.props" Condition="Exists('$(NuGetPackageRoot)xunit.runner.visualstudio\2.4.5\build\netcoreapp3.1\xunit.runner.visualstudio.props')" />-->
  <!--<Import Project="$(NuGetPackageRoot)microsoft.net.illink.tasks\8.0.0-rc.1.23406.6\build\Microsoft.NET.ILLink.Tasks.props" Condition="Exists('$(NuGetPackageRoot)microsoft.net.illink.tasks\8.0.0-rc.1.23406.6\build\Microsoft.NET.ILLink.Tasks.props')" />-->
  <!--<Import Project="$(NuGetPackageRoot)microsoft.net.compilers.toolset\4.8.0-7.23566.2\build\Microsoft.Net.Compilers.Toolset.props" Condition="Exists('$(NuGetPackageRoot)microsoft.net.compilers.toolset\4.8.0-7.23566.2\build\Microsoft.Net.Compilers.Toolset.props')" />-->
  <!--<Import Project="$(NuGetPackageRoot)microsoft.dotnet.build.tasks.targetframework\8.0.0-beta.24426.2\build\Microsoft.DotNet.Build.Tasks.TargetFramework.props" Condition="Exists('$(NuGetPackageRoot)microsoft.dotnet.build.tasks.targetframework\8.0.0-beta.24426.2\build\Microsoft.DotNet.Build.Tasks.TargetFramework.props')" />-->
  <!--<Import Project="$(NuGetPackageRoot)microsoft.codeanalysis.netanalyzers\8.0.0-preview.23614.1\buildTransitive\Microsoft.CodeAnalysis.NetAnalyzers.props" Condition="Exists('$(NuGetPackageRoot)microsoft.codeanalysis.netanalyzers\8.0.0-preview.23614.1\buildTransitive\Microsoft.CodeAnalysis.NetAnalyzers.props')" />-->
  <!--<Import Project="$(NuGetPackageRoot)microsoft.codeanalysis.csharp.codestyle\4.5.0\build\Microsoft.CodeAnalysis.CSharp.CodeStyle.props" Condition="Exists('$(NuGetPackageRoot)microsoft.codeanalysis.csharp.codestyle\4.5.0\build\Microsoft.CodeAnalysis.CSharp.CodeStyle.props')" />-->
  <!--<Import Project="$(NuGetPackageRoot)microsoft.testplatform.testhost\17.4.0-preview-20220707-01\build\netcoreapp2.1\Microsoft.TestPlatform.TestHost.props" Condition="Exists('$(NuGetPackageRoot)microsoft.testplatform.testhost\17.4.0-preview-20220707-01\build\netcoreapp2.1\Microsoft.TestPlatform.TestHost.props')" />-->
  <!--<Import Project="$(NuGetPackageRoot)microsoft.codecoverage\17.4.0-preview-20220707-01\build\netstandard1.0\Microsoft.CodeCoverage.props" Condition="Exists('$(NuGetPackageRoot)microsoft.codecoverage\17.4.0-preview-20220707-01\build\netstandard1.0\Microsoft.CodeCoverage.props')" />-->
  <!--<Import Project="$(NuGetPackageRoot)microsoft.net.test.sdk\17.4.0-preview-20220707-01\build\netcoreapp2.1\Microsoft.NET.Test.Sdk.props" Condition="Exists('$(NuGetPackageRoot)microsoft.net.test.sdk\17.4.0-preview-20220707-01\build\netcoreapp2.1\Microsoft.NET.Test.Sdk.props')" />-->
  <!--<Import Project="$(NuGetPackageRoot)microsoft.dotnet.xunitconsolerunner\2.5.1-beta.24426.2\build\Microsoft.DotNet.XUnitConsoleRunner.props" Condition="Exists('$(NuGetPackageRoot)microsoft.dotnet.xunitconsolerunner\2.5.1-beta.24426.2\build\Microsoft.DotNet.XUnitConsoleRunner.props')" />-->
  <!--</ImportGroup>-->
  <!--<ImportGroup Condition=" '$(TargetFramework)' == 'net8.0-osx' AND '$(ExcludeRestorePackageImports)' != 'true' ">-->
  <!--<Import Project="$(NuGetPackageRoot)xunit.runner.visualstudio\2.4.5\build\netcoreapp3.1\xunit.runner.visualstudio.props" Condition="Exists('$(NuGetPackageRoot)xunit.runner.visualstudio\2.4.5\build\netcoreapp3.1\xunit.runner.visualstudio.props')" />-->
  <!--<Import Project="$(NuGetPackageRoot)microsoft.net.illink.tasks\8.0.0-rc.1.23406.6\build\Microsoft.NET.ILLink.Tasks.props" Condition="Exists('$(NuGetPackageRoot)microsoft.net.illink.tasks\8.0.0-rc.1.23406.6\build\Microsoft.NET.ILLink.Tasks.props')" />-->
  <!--<Import Project="$(NuGetPackageRoot)microsoft.net.compilers.toolset\4.8.0-7.23566.2\build\Microsoft.Net.Compilers.Toolset.props" Condition="Exists('$(NuGetPackageRoot)microsoft.net.compilers.toolset\4.8.0-7.23566.2\build\Microsoft.Net.Compilers.Toolset.props')" />-->
  <!--<Import Project="$(NuGetPackageRoot)microsoft.dotnet.build.tasks.targetframework\8.0.0-beta.24426.2\build\Microsoft.DotNet.Build.Tasks.TargetFramework.props" Condition="Exists('$(NuGetPackageRoot)microsoft.dotnet.build.tasks.targetframework\8.0.0-beta.24426.2\build\Microsoft.DotNet.Build.Tasks.TargetFramework.props')" />-->
  <!--<Import Project="$(NuGetPackageRoot)microsoft.codeanalysis.netanalyzers\8.0.0-preview.23614.1\buildTransitive\Microsoft.CodeAnalysis.NetAnalyzers.props" Condition="Exists('$(NuGetPackageRoot)microsoft.codeanalysis.netanalyzers\8.0.0-preview.23614.1\buildTransitive\Microsoft.CodeAnalysis.NetAnalyzers.props')" />-->
  <!--<Import Project="$(NuGetPackageRoot)microsoft.codeanalysis.csharp.codestyle\4.5.0\build\Microsoft.CodeAnalysis.CSharp.CodeStyle.props" Condition="Exists('$(NuGetPackageRoot)microsoft.codeanalysis.csharp.codestyle\4.5.0\build\Microsoft.CodeAnalysis.CSharp.CodeStyle.props')" />-->
  <!--<Import Project="$(NuGetPackageRoot)microsoft.testplatform.testhost\17.4.0-preview-20220707-01\build\netcoreapp2.1\Microsoft.TestPlatform.TestHost.props" Condition="Exists('$(NuGetPackageRoot)microsoft.testplatform.testhost\17.4.0-preview-20220707-01\build\netcoreapp2.1\Microsoft.TestPlatform.TestHost.props')" />-->
  <!--<Import Project="$(NuGetPackageRoot)microsoft.codecoverage\17.4.0-preview-20220707-01\build\netstandard1.0\Microsoft.CodeCoverage.props" Condition="Exists('$(NuGetPackageRoot)microsoft.codecoverage\17.4.0-preview-20220707-01\build\netstandard1.0\Microsoft.CodeCoverage.props')" />-->
  <!--<Import Project="$(NuGetPackageRoot)microsoft.net.test.sdk\17.4.0-preview-20220707-01\build\netcoreapp2.1\Microsoft.NET.Test.Sdk.props" Condition="Exists('$(NuGetPackageRoot)microsoft.net.test.sdk\17.4.0-preview-20220707-01\build\netcoreapp2.1\Microsoft.NET.Test.Sdk.props')" />-->
  <!--<Import Project="$(NuGetPackageRoot)microsoft.dotnet.xunitconsolerunner\2.5.1-beta.24426.2\build\Microsoft.DotNet.XUnitConsoleRunner.props" Condition="Exists('$(NuGetPackageRoot)microsoft.dotnet.xunitconsolerunner\2.5.1-beta.24426.2\build\Microsoft.DotNet.XUnitConsoleRunner.props')" />-->
  <!--</ImportGroup>-->
  <!--<ImportGroup Condition=" '$(TargetFramework)' == 'net8.0-windows' AND '$(ExcludeRestorePackageImports)' != 'true' ">-->
  <!--<Import Project="$(NuGetPackageRoot)xunit.runner.visualstudio\2.4.5\build\netcoreapp3.1\xunit.runner.visualstudio.props" Condition="Exists('$(NuGetPackageRoot)xunit.runner.visualstudio\2.4.5\build\netcoreapp3.1\xunit.runner.visualstudio.props')" />-->
  <!--<Import Project="$(NuGetPackageRoot)microsoft.net.illink.tasks\8.0.0-rc.1.23406.6\build\Microsoft.NET.ILLink.Tasks.props" Condition="Exists('$(NuGetPackageRoot)microsoft.net.illink.tasks\8.0.0-rc.1.23406.6\build\Microsoft.NET.ILLink.Tasks.props')" />-->
  <!--<Import Project="$(NuGetPackageRoot)microsoft.net.compilers.toolset\4.8.0-7.23566.2\build\Microsoft.Net.Compilers.Toolset.props" Condition="Exists('$(NuGetPackageRoot)microsoft.net.compilers.toolset\4.8.0-7.23566.2\build\Microsoft.Net.Compilers.Toolset.props')" />-->
  <!--<Import Project="$(NuGetPackageRoot)microsoft.dotnet.build.tasks.targetframework\8.0.0-beta.24426.2\build\Microsoft.DotNet.Build.Tasks.TargetFramework.props" Condition="Exists('$(NuGetPackageRoot)microsoft.dotnet.build.tasks.targetframework\8.0.0-beta.24426.2\build\Microsoft.DotNet.Build.Tasks.TargetFramework.props')" />-->
  <!--<Import Project="$(NuGetPackageRoot)microsoft.codeanalysis.netanalyzers\8.0.0-preview.23614.1\buildTransitive\Microsoft.CodeAnalysis.NetAnalyzers.props" Condition="Exists('$(NuGetPackageRoot)microsoft.codeanalysis.netanalyzers\8.0.0-preview.23614.1\buildTransitive\Microsoft.CodeAnalysis.NetAnalyzers.props')" />-->
  <!--<Import Project="$(NuGetPackageRoot)microsoft.codeanalysis.csharp.codestyle\4.5.0\build\Microsoft.CodeAnalysis.CSharp.CodeStyle.props" Condition="Exists('$(NuGetPackageRoot)microsoft.codeanalysis.csharp.codestyle\4.5.0\build\Microsoft.CodeAnalysis.CSharp.CodeStyle.props')" />-->
  <!--<Import Project="$(NuGetPackageRoot)microsoft.testplatform.testhost\17.4.0-preview-20220707-01\build\netcoreapp2.1\Microsoft.TestPlatform.TestHost.props" Condition="Exists('$(NuGetPackageRoot)microsoft.testplatform.testhost\17.4.0-preview-20220707-01\build\netcoreapp2.1\Microsoft.TestPlatform.TestHost.props')" />-->
  <!--<Import Project="$(NuGetPackageRoot)microsoft.codecoverage\17.4.0-preview-20220707-01\build\netstandard1.0\Microsoft.CodeCoverage.props" Condition="Exists('$(NuGetPackageRoot)microsoft.codecoverage\17.4.0-preview-20220707-01\build\netstandard1.0\Microsoft.CodeCoverage.props')" />-->
  <!--<Import Project="$(NuGetPackageRoot)microsoft.net.test.sdk\17.4.0-preview-20220707-01\build\netcoreapp2.1\Microsoft.NET.Test.Sdk.props" Condition="Exists('$(NuGetPackageRoot)microsoft.net.test.sdk\17.4.0-preview-20220707-01\build\netcoreapp2.1\Microsoft.NET.Test.Sdk.props')" />-->
  <!--<Import Project="$(NuGetPackageRoot)microsoft.dotnet.xunitconsolerunner\2.5.1-beta.24426.2\build\Microsoft.DotNet.XUnitConsoleRunner.props" Condition="Exists('$(NuGetPackageRoot)microsoft.dotnet.xunitconsolerunner\2.5.1-beta.24426.2\build\Microsoft.DotNet.XUnitConsoleRunner.props')" />-->
  <!--</ImportGroup>-->
  <PropertyGroup Condition=" '$(TargetFramework)' == 'net462' AND '$(ExcludeRestorePackageImports)' != 'true' " xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <Pkgxunit_runner_visualstudio Condition=" '$(Pkgxunit_runner_visualstudio)' == '' ">C:\.tools\.nuget\packages\xunit.runner.visualstudio\2.4.5</Pkgxunit_runner_visualstudio>
    <Pkgxunit_runner_console Condition=" '$(Pkgxunit_runner_console)' == '' ">C:\.tools\.nuget\packages\xunit.runner.console\2.4.2</Pkgxunit_runner_console>
    <Pkgxunit_analyzers Condition=" '$(Pkgxunit_analyzers)' == '' ">C:\.tools\.nuget\packages\xunit.analyzers\1.0.0</Pkgxunit_analyzers>
    <PkgMicrosoft_NET_ILLink_Tasks Condition=" '$(PkgMicrosoft_NET_ILLink_Tasks)' == '' ">C:\.tools\.nuget\packages\microsoft.net.illink.tasks\8.0.0-rc.1.23406.6</PkgMicrosoft_NET_ILLink_Tasks>
    <PkgMicrosoft_DotNet_Build_Tasks_TargetFramework Condition=" '$(PkgMicrosoft_DotNet_Build_Tasks_TargetFramework)' == '' ">C:\.tools\.nuget\packages\microsoft.dotnet.build.tasks.targetframework\8.0.0-beta.24426.2</PkgMicrosoft_DotNet_Build_Tasks_TargetFramework>
    <PkgStyleCop_Analyzers_Unstable Condition=" '$(PkgStyleCop_Analyzers_Unstable)' == '' ">C:\.tools\.nuget\packages\stylecop.analyzers.unstable\1.2.0.406</PkgStyleCop_Analyzers_Unstable>
    <PkgMicrosoft_CodeAnalysis_NetAnalyzers Condition=" '$(PkgMicrosoft_CodeAnalysis_NetAnalyzers)' == '' ">C:\.tools\.nuget\packages\microsoft.codeanalysis.netanalyzers\8.0.0-preview.23614.1</PkgMicrosoft_CodeAnalysis_NetAnalyzers>
    <PkgMicrosoft_CodeAnalysis_CSharp_CodeStyle Condition=" '$(PkgMicrosoft_CodeAnalysis_CSharp_CodeStyle)' == '' ">C:\.tools\.nuget\packages\microsoft.codeanalysis.csharp.codestyle\4.5.0</PkgMicrosoft_CodeAnalysis_CSharp_CodeStyle>
  </PropertyGroup>
  <PropertyGroup Condition=" '$(TargetFramework)' == 'net8.0-freebsd' AND '$(ExcludeRestorePackageImports)' != 'true' " xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <Pkgxunit_runner_visualstudio Condition=" '$(Pkgxunit_runner_visualstudio)' == '' ">C:\.tools\.nuget\packages\xunit.runner.visualstudio\2.4.5</Pkgxunit_runner_visualstudio>
    <Pkgxunit_analyzers Condition=" '$(Pkgxunit_analyzers)' == '' ">C:\.tools\.nuget\packages\xunit.analyzers\1.0.0</Pkgxunit_analyzers>
    <PkgMicrosoft_NET_ILLink_Tasks Condition=" '$(PkgMicrosoft_NET_ILLink_Tasks)' == '' ">C:\.tools\.nuget\packages\microsoft.net.illink.tasks\8.0.0-rc.1.23406.6</PkgMicrosoft_NET_ILLink_Tasks>
    <PkgMicrosoft_DotNet_Build_Tasks_TargetFramework Condition=" '$(PkgMicrosoft_DotNet_Build_Tasks_TargetFramework)' == '' ">C:\.tools\.nuget\packages\microsoft.dotnet.build.tasks.targetframework\8.0.0-beta.24426.2</PkgMicrosoft_DotNet_Build_Tasks_TargetFramework>
    <PkgStyleCop_Analyzers_Unstable Condition=" '$(PkgStyleCop_Analyzers_Unstable)' == '' ">C:\.tools\.nuget\packages\stylecop.analyzers.unstable\1.2.0.406</PkgStyleCop_Analyzers_Unstable>
    <PkgMicrosoft_CodeAnalysis_NetAnalyzers Condition=" '$(PkgMicrosoft_CodeAnalysis_NetAnalyzers)' == '' ">C:\.tools\.nuget\packages\microsoft.codeanalysis.netanalyzers\8.0.0-preview.23614.1</PkgMicrosoft_CodeAnalysis_NetAnalyzers>
    <PkgMicrosoft_CodeAnalysis_CSharp_CodeStyle Condition=" '$(PkgMicrosoft_CodeAnalysis_CSharp_CodeStyle)' == '' ">C:\.tools\.nuget\packages\microsoft.codeanalysis.csharp.codestyle\4.5.0</PkgMicrosoft_CodeAnalysis_CSharp_CodeStyle>
    <PkgMicrosoft_DotNet_XUnitConsoleRunner Condition=" '$(PkgMicrosoft_DotNet_XUnitConsoleRunner)' == '' ">C:\.tools\.nuget\packages\microsoft.dotnet.xunitconsolerunner\2.5.1-beta.24426.2</PkgMicrosoft_DotNet_XUnitConsoleRunner>
  </PropertyGroup>
  <PropertyGroup Condition=" '$(TargetFramework)' == 'net8.0-linux' AND '$(ExcludeRestorePackageImports)' != 'true' " xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <Pkgxunit_runner_visualstudio Condition=" '$(Pkgxunit_runner_visualstudio)' == '' ">C:\.tools\.nuget\packages\xunit.runner.visualstudio\2.4.5</Pkgxunit_runner_visualstudio>
    <Pkgxunit_analyzers Condition=" '$(Pkgxunit_analyzers)' == '' ">C:\.tools\.nuget\packages\xunit.analyzers\1.0.0</Pkgxunit_analyzers>
    <PkgMicrosoft_NET_ILLink_Tasks Condition=" '$(PkgMicrosoft_NET_ILLink_Tasks)' == '' ">C:\.tools\.nuget\packages\microsoft.net.illink.tasks\8.0.0-rc.1.23406.6</PkgMicrosoft_NET_ILLink_Tasks>
    <PkgMicrosoft_DotNet_Build_Tasks_TargetFramework Condition=" '$(PkgMicrosoft_DotNet_Build_Tasks_TargetFramework)' == '' ">C:\.tools\.nuget\packages\microsoft.dotnet.build.tasks.targetframework\8.0.0-beta.24426.2</PkgMicrosoft_DotNet_Build_Tasks_TargetFramework>
    <PkgStyleCop_Analyzers_Unstable Condition=" '$(PkgStyleCop_Analyzers_Unstable)' == '' ">C:\.tools\.nuget\packages\stylecop.analyzers.unstable\1.2.0.406</PkgStyleCop_Analyzers_Unstable>
    <PkgMicrosoft_CodeAnalysis_NetAnalyzers Condition=" '$(PkgMicrosoft_CodeAnalysis_NetAnalyzers)' == '' ">C:\.tools\.nuget\packages\microsoft.codeanalysis.netanalyzers\8.0.0-preview.23614.1</PkgMicrosoft_CodeAnalysis_NetAnalyzers>
    <PkgMicrosoft_CodeAnalysis_CSharp_CodeStyle Condition=" '$(PkgMicrosoft_CodeAnalysis_CSharp_CodeStyle)' == '' ">C:\.tools\.nuget\packages\microsoft.codeanalysis.csharp.codestyle\4.5.0</PkgMicrosoft_CodeAnalysis_CSharp_CodeStyle>
    <PkgMicrosoft_DotNet_XUnitConsoleRunner Condition=" '$(PkgMicrosoft_DotNet_XUnitConsoleRunner)' == '' ">C:\.tools\.nuget\packages\microsoft.dotnet.xunitconsolerunner\2.5.1-beta.24426.2</PkgMicrosoft_DotNet_XUnitConsoleRunner>
  </PropertyGroup>
  <PropertyGroup Condition=" '$(TargetFramework)' == 'net8.0-osx' AND '$(ExcludeRestorePackageImports)' != 'true' " xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <Pkgxunit_runner_visualstudio Condition=" '$(Pkgxunit_runner_visualstudio)' == '' ">C:\.tools\.nuget\packages\xunit.runner.visualstudio\2.4.5</Pkgxunit_runner_visualstudio>
    <Pkgxunit_analyzers Condition=" '$(Pkgxunit_analyzers)' == '' ">C:\.tools\.nuget\packages\xunit.analyzers\1.0.0</Pkgxunit_analyzers>
    <PkgMicrosoft_NET_ILLink_Tasks Condition=" '$(PkgMicrosoft_NET_ILLink_Tasks)' == '' ">C:\.tools\.nuget\packages\microsoft.net.illink.tasks\8.0.0-rc.1.23406.6</PkgMicrosoft_NET_ILLink_Tasks>
    <PkgMicrosoft_DotNet_Build_Tasks_TargetFramework Condition=" '$(PkgMicrosoft_DotNet_Build_Tasks_TargetFramework)' == '' ">C:\.tools\.nuget\packages\microsoft.dotnet.build.tasks.targetframework\8.0.0-beta.24426.2</PkgMicrosoft_DotNet_Build_Tasks_TargetFramework>
    <PkgStyleCop_Analyzers_Unstable Condition=" '$(PkgStyleCop_Analyzers_Unstable)' == '' ">C:\.tools\.nuget\packages\stylecop.analyzers.unstable\1.2.0.406</PkgStyleCop_Analyzers_Unstable>
    <PkgMicrosoft_CodeAnalysis_NetAnalyzers Condition=" '$(PkgMicrosoft_CodeAnalysis_NetAnalyzers)' == '' ">C:\.tools\.nuget\packages\microsoft.codeanalysis.netanalyzers\8.0.0-preview.23614.1</PkgMicrosoft_CodeAnalysis_NetAnalyzers>
    <PkgMicrosoft_CodeAnalysis_CSharp_CodeStyle Condition=" '$(PkgMicrosoft_CodeAnalysis_CSharp_CodeStyle)' == '' ">C:\.tools\.nuget\packages\microsoft.codeanalysis.csharp.codestyle\4.5.0</PkgMicrosoft_CodeAnalysis_CSharp_CodeStyle>
    <PkgMicrosoft_DotNet_XUnitConsoleRunner Condition=" '$(PkgMicrosoft_DotNet_XUnitConsoleRunner)' == '' ">C:\.tools\.nuget\packages\microsoft.dotnet.xunitconsolerunner\2.5.1-beta.24426.2</PkgMicrosoft_DotNet_XUnitConsoleRunner>
  </PropertyGroup>
  <PropertyGroup Condition=" '$(TargetFramework)' == 'net8.0-windows' AND '$(ExcludeRestorePackageImports)' != 'true' " xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <Pkgxunit_runner_visualstudio Condition=" '$(Pkgxunit_runner_visualstudio)' == '' ">C:\.tools\.nuget\packages\xunit.runner.visualstudio\2.4.5</Pkgxunit_runner_visualstudio>
    <Pkgxunit_analyzers Condition=" '$(Pkgxunit_analyzers)' == '' ">C:\.tools\.nuget\packages\xunit.analyzers\1.0.0</Pkgxunit_analyzers>
    <PkgMicrosoft_NET_ILLink_Tasks Condition=" '$(PkgMicrosoft_NET_ILLink_Tasks)' == '' ">C:\.tools\.nuget\packages\microsoft.net.illink.tasks\8.0.0-rc.1.23406.6</PkgMicrosoft_NET_ILLink_Tasks>
    <PkgMicrosoft_DotNet_Build_Tasks_TargetFramework Condition=" '$(PkgMicrosoft_DotNet_Build_Tasks_TargetFramework)' == '' ">C:\.tools\.nuget\packages\microsoft.dotnet.build.tasks.targetframework\8.0.0-beta.24426.2</PkgMicrosoft_DotNet_Build_Tasks_TargetFramework>
    <PkgStyleCop_Analyzers_Unstable Condition=" '$(PkgStyleCop_Analyzers_Unstable)' == '' ">C:\.tools\.nuget\packages\stylecop.analyzers.unstable\1.2.0.406</PkgStyleCop_Analyzers_Unstable>
    <PkgMicrosoft_CodeAnalysis_NetAnalyzers Condition=" '$(PkgMicrosoft_CodeAnalysis_NetAnalyzers)' == '' ">C:\.tools\.nuget\packages\microsoft.codeanalysis.netanalyzers\8.0.0-preview.23614.1</PkgMicrosoft_CodeAnalysis_NetAnalyzers>
    <PkgMicrosoft_CodeAnalysis_CSharp_CodeStyle Condition=" '$(PkgMicrosoft_CodeAnalysis_CSharp_CodeStyle)' == '' ">C:\.tools\.nuget\packages\microsoft.codeanalysis.csharp.codestyle\4.5.0</PkgMicrosoft_CodeAnalysis_CSharp_CodeStyle>
    <PkgMicrosoft_DotNet_XUnitConsoleRunner Condition=" '$(PkgMicrosoft_DotNet_XUnitConsoleRunner)' == '' ">C:\.tools\.nuget\packages\microsoft.dotnet.xunitconsolerunner\2.5.1-beta.24426.2</PkgMicrosoft_DotNet_XUnitConsoleRunner>
  </PropertyGroup>
  <!--
============================================================================================================================================
  </Import>

C:\Program Files\dotnet\sdk\8.0.110\Current\Microsoft.Common.props
============================================================================================================================================
-->
  <!--
      Wildcard imports come from $(MSBuildExtensionsPath)\$(MSBuildToolsVersion)\Imports\Microsoft.Common.props\ directory.
      This is very similar to the same extension point used in Microsoft.Common.targets, which is located in
      the $(MSBuildExtensionsPath)\$(MSBuildToolsVersion)\Microsoft.Common.targets\ directory.

      Unfortunately, there is already a file named "Microsoft.Common.props" in this directory,
      so we have to have a slightly different directory name to hold extensions.
  -->
  <!--<Import Project="$(MSBuildUserExtensionsPath)\$(MSBuildToolsVersion)\Imports\Microsoft.Common.props\ImportBefore\*" Condition="'$(ImportUserLocationsByWildcardBeforeMicrosoftCommonProps)' == 'true' and exists('$(MSBuildUserExtensionsPath)\$(MSBuildToolsVersion)\Imports\Microsoft.Common.props\ImportBefore')" />-->
  <!--<Import Project="$(MSBuildExtensionsPath)\$(MSBuildToolsVersion)\Imports\Microsoft.Common.props\ImportBefore\*" Condition="'$(ImportByWildcardBeforeMicrosoftCommonProps)' == 'true' and exists('$(MSBuildExtensionsPath)\$(MSBuildToolsVersion)\Imports\Microsoft.Common.props\ImportBefore')" />-->
  <PropertyGroup>
    <CustomBeforeMicrosoftCommonProps Condition="'$(CustomBeforeMicrosoftCommonProps)'==''">$(MSBuildExtensionsPath)\v$(MSBuildToolsVersion)\Custom.Before.$(MSBuildThisFile)</CustomBeforeMicrosoftCommonProps>
    <CustomAfterMicrosoftCommonProps Condition="'$(CustomAfterMicrosoftCommonProps)'==''">$(MSBuildExtensionsPath)\v$(MSBuildToolsVersion)\Custom.After.$(MSBuildThisFile)</CustomAfterMicrosoftCommonProps>
  </PropertyGroup>
  <!--<Import Project="$(CustomBeforeMicrosoftCommonProps)" Condition="'$(CustomBeforeMicrosoftCommonProps)' != '' and Exists('$(CustomBeforeMicrosoftCommonProps)')" />-->
  <!-- This is used to determine whether Microsoft.Common.targets needs to import
      Microsoft.Common.props itself, or whether it has been imported previously,
      e.g. by the project itself. -->
  <PropertyGroup>
    <MicrosoftCommonPropsHasBeenImported>true</MicrosoftCommonPropsHasBeenImported>
  </PropertyGroup>
  <PropertyGroup>
    <Configuration Condition=" '$(Configuration)' == '' and '$(DefaultProjectConfiguration)' != '' ">$(DefaultProjectConfiguration)</Configuration>
    <Platform Condition=" '$(Platform)' == '' and '$(DefaultProjectPlatform)' != '' ">$(DefaultProjectPlatform)</Platform>
  </PropertyGroup>
  <PropertyGroup>
    <WMSJSProject Condition="'$(WMSJSProject)' == ''">WJProject</WMSJSProject>
    <WMSJSProjectDirectory Condition="'$(WMSJSProjectDirectory)' == ''">JavaScript</WMSJSProjectDirectory>
  </PropertyGroup>
  <!--<Import Project="$(MSBuildExtensionsPath)\$(MSBuildToolsVersion)\Microsoft.VisualStudioVersion.v*.Common.props" Condition="'$(VisualStudioVersion)' == ''" />-->
  <!--<Import Project="$(CustomAfterMicrosoftCommonProps)" Condition="'$(CustomAfterMicrosoftCommonProps)' != '' and Exists('$(CustomAfterMicrosoftCommonProps)')" />-->
  <!--<Import Project="$(MSBuildUserExtensionsPath)\$(MSBuildToolsVersion)\Imports\Microsoft.Common.props\ImportAfter\*" Condition="'$(ImportUserLocationsByWildcardAfterMicrosoftCommonProps)' == 'true' and exists('$(MSBuildUserExtensionsPath)\$(MSBuildToolsVersion)\Imports\Microsoft.Common.props\ImportAfter')" />-->
  <!--<Import Project="$(MSBuildExtensionsPath)\$(MSBuildToolsVersion)\Imports\Microsoft.Common.props\ImportAfter\*" Condition="'$(ImportByWildcardAfterMicrosoftCommonProps)' == 'true' and exists('$(MSBuildExtensionsPath)\$(MSBuildToolsVersion)\Imports\Microsoft.Common.props\ImportAfter')" />-->
  <!--
      Import NuGet.props file.
  -->
  <PropertyGroup>
    <MSBuildUseVisualStudioDirectoryLayout Condition="'$(MSBuildUseVisualStudioDirectoryLayout)'==''">$([MSBuild]::IsRunningFromVisualStudio())</MSBuildUseVisualStudioDirectoryLayout>
    <NuGetPropsFile Condition="'$(NuGetPropsFile)'=='' and '$(MSBuildUseVisualStudioDirectoryLayout)'=='true'">$([MSBuild]::GetToolsDirectory32())\..\..\..\Common7\IDE\CommonExtensions\Microsoft\NuGet\NuGet.props</NuGetPropsFile>
    <NuGetPropsFile Condition="'$(NuGetPropsFile)'==''">$(MSBuildToolsPath)\NuGet.props</NuGetPropsFile>
  </PropertyGroup>
  <!--
============================================================================================================================================
  <Import Project="$(NuGetPropsFile)" Condition="Exists('$(NuGetPropsFile)')">

C:\Program Files\dotnet\sdk\8.0.110\NuGet.props
============================================================================================================================================
-->
  <!--
***********************************************************************************************
NuGet.props

WARNING:  DO NOT MODIFY this file unless you are knowledgeable about MSBuild and have
          created a backup copy.  Incorrect changes to this file will make it
          impossible to load or build your projects from the command-line or the IDE.

Copyright (c) .NET Foundation. All rights reserved.
***********************************************************************************************
-->
  <!--
      Import 'Directory.Packages.props' which will contain centralized packages for all the projects and solutions under
      the directory in which the file is present. This is similar to 'Directory.Build.props/targets' logic which is present
      in the common props/targets which serve a similar purpose.
  -->
  <PropertyGroup>
    <ImportDirectoryPackagesProps Condition="'$(ImportDirectoryPackagesProps)' == ''">true</ImportDirectoryPackagesProps>
  </PropertyGroup>
  <!--
      Determine the path to the 'Directory.Packages.props' file, if the user did not disable $(ImportDirectoryPackagesProps) and
      they did not already specify an absolute path to use via $(DirectoryPackagesPropsPath)
  -->
  <PropertyGroup Condition="'$(ImportDirectoryPackagesProps)' == 'true' and '$(DirectoryPackagesPropsPath)' == ''">
    <_DirectoryPackagesPropsFile Condition="'$(_DirectoryPackagesPropsFile)' == ''">Directory.Packages.props</_DirectoryPackagesPropsFile>
    <_DirectoryPackagesPropsBasePath Condition="'$(_DirectoryPackagesPropsBasePath)' == ''">$([MSBuild]::GetDirectoryNameOfFileAbove('$(MSBuildProjectDirectory)', '$(_DirectoryPackagesPropsFile)'))</_DirectoryPackagesPropsBasePath>
    <DirectoryPackagesPropsPath Condition="'$(_DirectoryPackagesPropsBasePath)' != '' and '$(_DirectoryPackagesPropsFile)' != ''">$([MSBuild]::NormalizePath('$(_DirectoryPackagesPropsBasePath)', '$(_DirectoryPackagesPropsFile)'))</DirectoryPackagesPropsPath>
  </PropertyGroup>
  <!--<Import Project="$(DirectoryPackagesPropsPath)" Condition="'$(ImportDirectoryPackagesProps)' == 'true' and '$(DirectoryPackagesPropsPath)' != '' and Exists('$(DirectoryPackagesPropsPath)')" />-->
  <PropertyGroup Condition="'$(ImportDirectoryPackagesProps)' == 'true' and '$(DirectoryPackagesPropsPath)' != '' and Exists('$(DirectoryPackagesPropsPath)')">
    <CentralPackageVersionsFileImported>true</CentralPackageVersionsFileImported>
  </PropertyGroup>
  <!--
============================================================================================================================================
  </Import>

C:\Program Files\dotnet\sdk\8.0.110\Current\Microsoft.Common.props
============================================================================================================================================
-->
  <PropertyGroup Condition=" '$(MSBuildLogVerboseTaskParameters)' != 'true' ">
    <DisableLogTaskParameter_ConvertToAbsolutePath_Path>true</DisableLogTaskParameter_ConvertToAbsolutePath_Path>
    <DisableLogTaskParameter_FindUnderPath_OutOfPath>true</DisableLogTaskParameter_FindUnderPath_OutOfPath>
    <DisableLogTaskParameter_RemoveDuplicates_Inputs>true</DisableLogTaskParameter_RemoveDuplicates_Inputs>
    <DisableLogTaskParameterItemMetadata_ConvertToAbsolutePath_AbsolutePaths>true</DisableLogTaskParameterItemMetadata_ConvertToAbsolutePath_AbsolutePaths>
    <DisableLogTaskParameterItemMetadata_Copy_CopiedFiles>true</DisableLogTaskParameterItemMetadata_Copy_CopiedFiles>
    <DisableLogTaskParameterItemMetadata_Copy_DestinationFiles>true</DisableLogTaskParameterItemMetadata_Copy_DestinationFiles>
    <DisableLogTaskParameterItemMetadata_Copy_SourceFiles>true</DisableLogTaskParameterItemMetadata_Copy_SourceFiles>
    <DisableLogTaskParameterItemMetadata_FindUnderPath_Files>true</DisableLogTaskParameterItemMetadata_FindUnderPath_Files>
    <DisableLogTaskParameterItemMetadata_FindUnderPath_InPath>true</DisableLogTaskParameterItemMetadata_FindUnderPath_InPath>
    <DisableLogTaskParameterItemMetadata_GenerateResource_FilesWritten>true</DisableLogTaskParameterItemMetadata_GenerateResource_FilesWritten>
    <DisableLogTaskParameterItemMetadata_Hash_ItemsToHash>true</DisableLogTaskParameterItemMetadata_Hash_ItemsToHash>
    <DisableLogTaskParameterItemMetadata_RemoveDuplicates_Filtered>true</DisableLogTaskParameterItemMetadata_RemoveDuplicates_Filtered>
    <DisableLogTaskParameterItemMetadata_WriteLinesToFile_Lines>true</DisableLogTaskParameterItemMetadata_WriteLinesToFile_Lines>
  </PropertyGroup>
  <!--
============================================================================================================================================
  </Import>

C:\Program Files\dotnet\sdk\8.0.110\Sdks\Microsoft.NET.Sdk\Sdk\Sdk.props
============================================================================================================================================
-->
  <!--
============================================================================================================================================
  <Import Project="$(MSBuildThisFileDirectory)..\targets\Microsoft.NET.Sdk.props">

C:\Program Files\dotnet\sdk\8.0.110\Sdks\Microsoft.NET.Sdk\targets\Microsoft.NET.Sdk.props
============================================================================================================================================
-->
  <!--
***********************************************************************************************
Microsoft.NET.Sdk.props

WARNING:  DO NOT MODIFY this file unless you are knowledgeable about MSBuild and have
          created a backup copy.  Incorrect changes to this file will make it
          impossible to load or build your projects from the command-line or the IDE.

Copyright (c) .NET Foundation. All rights reserved.
***********************************************************************************************
-->
  <PropertyGroup xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <!-- This property disables the conflict resolution logic from the Microsoft.Packaging.Tools package, which is superceded by the logic here in the SDK -->
    <DisableHandlePackageFileConflicts>true</DisableHandlePackageFileConflicts>
  </PropertyGroup>
  <!-- Default configuration and platform to Debug|AnyCPU-->
  <PropertyGroup xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <Configurations Condition=" '$(Configurations)' == '' ">Debug;Release</Configurations>
    <Platforms Condition=" '$(Platforms)' == '' ">AnyCPU</Platforms>
    <Configuration Condition=" '$(Configuration)' == '' ">Debug</Configuration>
    <Platform Condition=" '$(Platform)' == '' ">AnyCPU</Platform>
  </PropertyGroup>
  <!-- Default the value of PackRelease for every project. Note that this is pre-evaluated by the CLI in an evaluation before evaluation (see ReleasePropertyProjectLocator.cs).
  Multitargeting pack splits a single 2 TFM project into 9 separate evaluations, with different target imports. Microsoft.NET.SDK.DefaultItems.Targets ...
  is where PublishRelease is defaulted because it depends on _TargetFrameworkVersionWithoutV. Some targets do not run in some instances of dotnet pack.
  So, we must set PackRelease here. Even though this occurs before project import, the project import can override PackRelease later, before the pre-evaluation, so this is fine.-->
  <PropertyGroup Condition="'$(PackRelease)' == ''" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <!-- Do not depend on this value being correct until after project evaluation.-->
    <PackRelease>true</PackRelease>
  </PropertyGroup>
  <!-- User-facing configuration-agnostic defaults -->
  <PropertyGroup xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <OutputType Condition=" '$(OutputType)' == '' ">Library</OutputType>
    <FileAlignment Condition=" '$(FileAlignment)' == '' ">512</FileAlignment>
    <ErrorReport Condition=" '$(ErrorReport)' == '' ">prompt</ErrorReport>
    <AssemblyName Condition=" '$(AssemblyName)' == '' ">$(MSBuildProjectName)</AssemblyName>
    <RootNamespace Condition=" '$(RootNamespace)' == '' ">$(MSBuildProjectName.Replace(" ", "_"))</RootNamespace>
    <Deterministic Condition=" '$(Deterministic)' == '' ">true</Deterministic>
  </PropertyGroup>
  <!-- User-facing configuration-specific defaults -->
  <PropertyGroup Condition=" '$(Configuration)' == 'Debug' " xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <DebugSymbols Condition=" '$(DebugSymbols)' == '' ">true</DebugSymbols>
    <Optimize Condition=" '$(Optimize)' == '' ">false</Optimize>
  </PropertyGroup>
  <PropertyGroup Condition=" '$(Configuration)' == 'Release' " xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <Optimize Condition=" '$(Optimize)' == '' ">true</Optimize>
  </PropertyGroup>
  <!-- User-facing platform-specific defaults -->
  <!--
    NOTE:

     * We cannot compare against $(Platform) directly as that will give VS cause to instantiate extra
       configurations, for each combination, which leads to performance problems and clutter in the sln
       in the common AnyCPU-only case.

     * We cannot just set $(PlatformTarget) to $(Platform) here because $(Platform) can be set to anything
       at the solution level, but there are a fixed set valid $(PlatformTarget) values that can be passed
       to the compiler. It is up to the user to explicitly set PlatformTarget to non-AnyCPU (if desired)
       outside the 1:1 defaults below.
  -->
  <PropertyGroup xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <_PlatformWithoutConfigurationInference>$(Platform)</_PlatformWithoutConfigurationInference>
  </PropertyGroup>
  <PropertyGroup Condition=" '$(_PlatformWithoutConfigurationInference)' == 'x64' " xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <PlatformTarget Condition=" '$(PlatformTarget)' == '' ">x64</PlatformTarget>
  </PropertyGroup>
  <PropertyGroup Condition=" '$(_PlatformWithoutConfigurationInference)' == 'x86' " xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <PlatformTarget Condition=" '$(PlatformTarget)' == '' ">x86</PlatformTarget>
  </PropertyGroup>
  <PropertyGroup Condition=" '$(_PlatformWithoutConfigurationInference)' == 'ARM' " xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <PlatformTarget Condition=" '$(PlatformTarget)' == '' ">ARM</PlatformTarget>
  </PropertyGroup>
  <PropertyGroup Condition=" '$(_PlatformWithoutConfigurationInference)' == 'arm64' " xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <PlatformTarget Condition=" '$(PlatformTarget)' == '' ">arm64</PlatformTarget>
  </PropertyGroup>
  <!-- Default settings for all projects built with this Sdk package -->
  <PropertyGroup Condition=" '$(AssemblySearchPaths)' == '' " xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <!-- By default exclude GAC, registry, output directory from search paths. -->
    <AssemblySearchPaths Condition="'$(AssemblySearchPath_UseCandidateAssemblyFiles)' != 'false'">{CandidateAssemblyFiles}</AssemblySearchPaths>
    <AssemblySearchPaths Condition="'$(AssemblySearchPath_UseHintPathFromItem)' != 'false'">$(AssemblySearchPaths);{HintPathFromItem}</AssemblySearchPaths>
    <AssemblySearchPaths Condition="'$(AssemblySearchPath_UseTargetFrameworkDirectory)' != 'false'">$(AssemblySearchPaths);{TargetFrameworkDirectory}</AssemblySearchPaths>
    <AssemblySearchPaths Condition="'$(AssemblySearchPath_UseRawFileName)' != 'false'">$(AssemblySearchPaths);{RawFileName}</AssemblySearchPaths>
  </PropertyGroup>
  <PropertyGroup xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <DebugType Condition=" '$(DebugType)' == '' ">portable</DebugType>
    <!-- This will turn off the base UWP-specific 'ResolveNuGetPackages' target -->
    <ResolveNuGetPackages>false</ResolveNuGetPackages>
    <!-- Skip import of Microsoft.NuGet.props and Microsoft.NuGet.targets -->
    <SkipImportNuGetProps>true</SkipImportNuGetProps>
    <SkipImportNuGetBuildTargets>true</SkipImportNuGetBuildTargets>
    <!-- NuGet should always restore .NET SDK projects with "PackageReference" style restore.  Setting this property will
         cause the right thing to happen even if there aren't any PackageReference items in the project, such as when
         a project targets .NET Framework and doesn't have any direct package dependencies. -->
    <RestoreProjectStyle>PackageReference</RestoreProjectStyle>
    <DesignTimeAssemblySearchPaths Condition=" '$(DesignTimeAssemblySearchPaths)' == '' ">$(AssemblySearchPaths)</DesignTimeAssemblySearchPaths>
    <AllowUnsafeBlocks Condition="'$(AllowUnsafeBlocks)'==''">false</AllowUnsafeBlocks>
    <TreatWarningsAsErrors Condition="'$(TreatWarningsAsErrors)'==''">false</TreatWarningsAsErrors>
    <Prefer32Bit Condition="'$(Prefer32Bit)'==''">false</Prefer32Bit>
    <SignAssembly Condition="'$(SignAssembly)'==''">false</SignAssembly>
    <DelaySign Condition="'$(DelaySign)'==''">false</DelaySign>
    <GeneratePackageOnBuild Condition="'$(GeneratePackageOnBuild)'==''">false</GeneratePackageOnBuild>
    <PackageRequireLicenseAcceptance Condition="'$(PackageRequireLicenseAcceptance)'==''">false</PackageRequireLicenseAcceptance>
    <DebugSymbols Condition="'$(DebugSymbols)'==''">false</DebugSymbols>
    <CheckForOverflowUnderflow Condition="'$(CheckForOverflowUnderflow)'==''">false</CheckForOverflowUnderflow>
    <AutomaticallyUseReferenceAssemblyPackages Condition="'$(AutomaticallyUseReferenceAssemblyPackages)'==''">true</AutomaticallyUseReferenceAssemblyPackages>
    <MicrosoftNETFrameworkReferenceAssembliesLatestPackageVersion>1.0.3</MicrosoftNETFrameworkReferenceAssembliesLatestPackageVersion>
    <CopyConflictingTransitiveContent>false</CopyConflictingTransitiveContent>
    <MSBuildCopyContentTransitively Condition="'$(MSBuildCopyContentTransitively)' == ''">true</MSBuildCopyContentTransitively>
    <ResolveAssemblyReferenceOutputUnresolvedAssemblyConflicts Condition="'$(ResolveAssemblyReferenceOutputUnresolvedAssemblyConflicts)' == ''">true</ResolveAssemblyReferenceOutputUnresolvedAssemblyConflicts>
    <!-- Uncomment this once https://github.com/Microsoft/visualfsharp/issues/3207 gets fixed -->
    <!-- <WarningsAsErrors>$(WarningsAsErrors);NU1605</WarningsAsErrors> -->
  </PropertyGroup>
  <PropertyGroup xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <!-- Path to project that the .NET CLI will build in order to generate deps.json files for .NET CLI tools -->
    <ToolDepsJsonGeneratorProject>$(MSBuildThisFileDirectory)GenerateDeps\GenerateDeps.proj</ToolDepsJsonGeneratorProject>
  </PropertyGroup>
  <!-- Default item includes (globs and implicit references) -->
  <!--
============================================================================================================================================
  <Import Project="Microsoft.NET.Sdk.DefaultItems.props">

C:\Program Files\dotnet\sdk\8.0.110\Sdks\Microsoft.NET.Sdk\targets\Microsoft.NET.Sdk.DefaultItems.props
============================================================================================================================================
-->
  <!--
***********************************************************************************************
Microsoft.NET.Sdk.DefaultItems.props

WARNING:  DO NOT MODIFY this file unless you are knowledgeable about MSBuild and have
          created a backup copy.  Incorrect changes to this file will make it
          impossible to load or build your projects from the command-line or the IDE.

Copyright (c) .NET Foundation. All rights reserved.
***********************************************************************************************
-->
  <PropertyGroup Condition="'$(NETCoreSdkBundledVersionsProps)' == ''" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <NETCoreSdkBundledVersionsProps>$(MSBuildThisFileDirectory)..\..\..\Microsoft.NETCoreSdk.BundledVersions.props</NETCoreSdkBundledVersionsProps>
  </PropertyGroup>
  <!--
============================================================================================================================================
  <Import Project="$(NETCoreSdkBundledVersionsProps)" Condition="Exists('$(NETCoreSdkBundledVersionsProps)')">

C:\Program Files\dotnet\sdk\8.0.110\Microsoft.NETCoreSdk.BundledVersions.props
============================================================================================================================================
-->
  <!--
***********************************************************************************************
Microsoft.NETCoreSdk.BundledVersions.props

WARNING:  DO NOT MODIFY this file unless you are knowledgeable about MSBuild and have
          created a backup copy.  Incorrect changes to this file will make it
          impossible to load or build your projects from the command-line or the IDE.

Copyright (c) .NET Foundation. All rights reserved.
***********************************************************************************************
-->
  <PropertyGroup>
    <NetCoreRoot Condition="'$(NetCoreRoot)' == ''">$([MSBuild]::NormalizePath('$(MSBuildThisFileDirectory)..\..\'))</NetCoreRoot>
    <NetCoreTargetingPackRoot Condition="'$(NetCoreTargetingPackRoot)' == ''">$([MSBuild]::EnsureTrailingSlash('$(NetCoreRoot)'))packs</NetCoreTargetingPackRoot>
    <_NetFrameworkHostedCompilersVersion>4.8.0-7.24468.9</_NetFrameworkHostedCompilersVersion>
    <NETCoreAppMaximumVersion>8.0</NETCoreAppMaximumVersion>
    <BundledNETCoreAppTargetFrameworkVersion>8.0</BundledNETCoreAppTargetFrameworkVersion>
    <BundledNETCoreAppPackageVersion>8.0.10</BundledNETCoreAppPackageVersion>
    <BundledNETStandardTargetFrameworkVersion>2.1</BundledNETStandardTargetFrameworkVersion>
    <BundledNETStandardPackageVersion>2.1.0</BundledNETStandardPackageVersion>
    <BundledNETCorePlatformsPackageVersion>8.0.10-servicing.24466.10</BundledNETCorePlatformsPackageVersion>
    <BundledRuntimeIdentifierGraphFile>$(MSBuildThisFileDirectory)RuntimeIdentifierGraph.json</BundledRuntimeIdentifierGraphFile>
    <NETCoreSdkVersion>8.0.110</NETCoreSdkVersion>
    <NETCoreSdkRuntimeIdentifier>win-x64</NETCoreSdkRuntimeIdentifier>
    <NETCoreSdkPortableRuntimeIdentifier>win-x64</NETCoreSdkPortableRuntimeIdentifier>
    <_NETCoreSdkIsPreview>false</_NETCoreSdkIsPreview>
  </PropertyGroup>
  <ItemGroup>
    <ImplicitPackageReferenceVersion Include="Microsoft.NETCore.App" TargetFrameworkVersion="1.0" DefaultVersion="1.0.5" LatestVersion="1.0.16" />
    <ImplicitPackageReferenceVersion Include="Microsoft.NETCore.App" TargetFrameworkVersion="1.1" DefaultVersion="1.1.2" LatestVersion="1.1.13" />
    <ImplicitPackageReferenceVersion Include="Microsoft.NETCore.App" TargetFrameworkVersion="2.0" DefaultVersion="2.0.0" LatestVersion="2.0.9" />
    <ImplicitPackageReferenceVersion Include="Microsoft.NETCore.App" TargetFrameworkVersion="2.1" DefaultVersion="2.1.0" LatestVersion="2.1.30" />
    <ImplicitPackageReferenceVersion Include="Microsoft.NETCore.App" TargetFrameworkVersion="2.2" DefaultVersion="2.2.0" LatestVersion="2.2.8" />
    <ImplicitPackageReferenceVersion Include="Microsoft.AspNetCore.App" TargetFrameworkVersion="2.1" DefaultVersion="2.1.1" LatestVersion="2.1.30" />
    <ImplicitPackageReferenceVersion Include="Microsoft.AspNetCore.All" TargetFrameworkVersion="2.1" DefaultVersion="2.1.1" LatestVersion="2.1.30" />
    <ImplicitPackageReferenceVersion Include="Microsoft.AspNetCore.App" TargetFrameworkVersion="2.2" DefaultVersion="2.2.0" LatestVersion="2.2.8" />
    <ImplicitPackageReferenceVersion Include="Microsoft.AspNetCore.All" TargetFrameworkVersion="2.2" DefaultVersion="2.2.0" LatestVersion="2.2.8" />
    <!-- .NET 8.0 -->
    <KnownFrameworkReference Include="Microsoft.NETCore.App" TargetFramework="net8.0" RuntimeFrameworkName="Microsoft.NETCore.App" DefaultRuntimeFrameworkVersion="8.0.0" LatestRuntimeFrameworkVersion="8.0.10" TargetingPackName="Microsoft.NETCore.App.Ref" TargetingPackVersion="8.0.10" RuntimePackNamePatterns="Microsoft.NETCore.App.Runtime.**RID**" RuntimePackRuntimeIdentifiers="linux-arm;linux-arm64;linux-musl-arm64;linux-musl-x64;linux-x64;osx-x64;rhel.6-x64;tizen.4.0.0-armel;tizen.5.0.0-armel;win-arm64;win-x64;win-x86;linux-musl-arm;osx-arm64;linux-s390x;linux-bionic-arm;linux-bionic-arm64;linux-bionic-x64;linux-bionic-x86;linux-ppc64le" />
    <KnownAppHostPack Include="Microsoft.NETCore.App" TargetFramework="net8.0" AppHostPackNamePattern="Microsoft.NETCore.App.Host.**RID**" AppHostPackVersion="8.0.10" AppHostRuntimeIdentifiers="linux-arm;linux-arm64;linux-musl-arm64;linux-musl-x64;linux-x64;osx-x64;rhel.6-x64;tizen.4.0.0-armel;tizen.5.0.0-armel;win-arm64;win-x64;win-x86;linux-musl-arm;osx-arm64;linux-s390x;linux-bionic-arm;linux-bionic-arm64;linux-bionic-x64;linux-bionic-x86;linux-ppc64le" ExcludedRuntimeIdentifiers="android" />
    <KnownCrossgen2Pack Include="Microsoft.NETCore.App.Crossgen2" TargetFramework="net8.0" Crossgen2PackNamePattern="Microsoft.NETCore.App.Crossgen2.**RID**" Crossgen2PackVersion="8.0.10" Crossgen2RuntimeIdentifiers="linux-musl-x64;linux-x64;win-x64;linux-arm;linux-arm64;linux-musl-arm;linux-musl-arm64;osx-arm64;osx-x64;win-arm64;win-x86" />
    <KnownILCompilerPack Include="Microsoft.DotNet.ILCompiler" TargetFramework="net8.0" ILCompilerPackNamePattern="runtime.**RID**.Microsoft.DotNet.ILCompiler" ILCompilerPackVersion="8.0.10" ILCompilerRuntimeIdentifiers="linux-arm64;linux-musl-arm64;linux-musl-x64;linux-x64;win-arm64;win-x64;osx-x64;osx-arm64" />
    <KnownRuntimePack Include="Microsoft.NETCore.App" TargetFramework="net8.0" RuntimeFrameworkName="Microsoft.NETCore.App" LatestRuntimeFrameworkVersion="8.0.10" RuntimePackNamePatterns="Microsoft.NETCore.App.Runtime.NativeAOT.**RID**" RuntimePackRuntimeIdentifiers="ios-arm64;iossimulator-arm64;iossimulator-x64;tvos-arm64;tvossimulator-arm64;tvossimulator-x64;maccatalyst-arm64;maccatalyst-x64;linux-bionic-arm64;linux-bionic-x64;osx-arm64;osx-x64" RuntimePackLabels="NativeAOT" />
    <KnownILLinkPack Include="Microsoft.NET.ILLink.Tasks" TargetFramework="net8.0" ILLinkPackVersion="8.0.10" />
    <KnownWebAssemblySdkPack Include="Microsoft.NET.Sdk.WebAssembly.Pack" TargetFramework="net8.0" WebAssemblySdkPackVersion="8.0.10" />
    <KnownRuntimePack Include="Microsoft.NETCore.App" TargetFramework="net8.0" RuntimeFrameworkName="Microsoft.NETCore.App" LatestRuntimeFrameworkVersion="8.0.10" RuntimePackNamePatterns="Microsoft.NETCore.App.Runtime.Mono.**RID**" RuntimePackRuntimeIdentifiers="linux-arm;linux-arm64;linux-musl-arm64;linux-musl-x64;linux-x64;osx-x64;rhel.6-x64;win-arm64;win-x64;win-x86;linux-musl-arm;osx-arm64;linux-s390x;browser-wasm;ios-arm64;ios-arm;iossimulator-arm64;iossimulator-x64;iossimulator-x86;tvos-arm64;tvossimulator-arm64;tvossimulator-x64;maccatalyst-x64;maccatalyst-arm64;android-arm64;android-arm;android-x64;android-x86" RuntimePackLabels="Mono" />
    <KnownFrameworkReference Include="Microsoft.WindowsDesktop.App" TargetFramework="net8.0" RuntimeFrameworkName="Microsoft.WindowsDesktop.App" DefaultRuntimeFrameworkVersion="8.0.0" LatestRuntimeFrameworkVersion="8.0.10" TargetingPackName="Microsoft.WindowsDesktop.App.Ref" TargetingPackVersion="8.0.10" RuntimePackNamePatterns="Microsoft.WindowsDesktop.App.Runtime.**RID**" RuntimePackRuntimeIdentifiers="win-x64;win-x86;win-arm64" IsWindowsOnly="true" />
    <KnownFrameworkReference Include="Microsoft.WindowsDesktop.App.WPF" TargetFramework="net8.0" RuntimeFrameworkName="Microsoft.WindowsDesktop.App" DefaultRuntimeFrameworkVersion="8.0.0" LatestRuntimeFrameworkVersion="8.0.10" TargetingPackName="Microsoft.WindowsDesktop.App.Ref" TargetingPackVersion="8.0.10" RuntimePackNamePatterns="Microsoft.WindowsDesktop.App.Runtime.**RID**" RuntimePackRuntimeIdentifiers="win-x64;win-x86;win-arm64" IsWindowsOnly="true" Profile="WPF" />
    <KnownFrameworkReference Include="Microsoft.WindowsDesktop.App.WindowsForms" TargetFramework="net8.0" RuntimeFrameworkName="Microsoft.WindowsDesktop.App" DefaultRuntimeFrameworkVersion="8.0.0" LatestRuntimeFrameworkVersion="8.0.10" TargetingPackName="Microsoft.WindowsDesktop.App.Ref" TargetingPackVersion="8.0.10" RuntimePackNamePatterns="Microsoft.WindowsDesktop.App.Runtime.**RID**" RuntimePackRuntimeIdentifiers="win-x64;win-x86;win-arm64" IsWindowsOnly="true" Profile="WindowsForms" />
    <KnownFrameworkReference Include="Microsoft.AspNetCore.App" TargetFramework="net8.0" RuntimeFrameworkName="Microsoft.AspNetCore.App" DefaultRuntimeFrameworkVersion="8.0.0" LatestRuntimeFrameworkVersion="8.0.10" TargetingPackName="Microsoft.AspNetCore.App.Ref" TargetingPackVersion="8.0.10" RuntimePackNamePatterns="Microsoft.AspNetCore.App.Runtime.**RID**" RuntimePackRuntimeIdentifiers="win-x64;win-x86;win-arm;osx-x64;linux-musl-x64;linux-musl-arm64;linux-x64;linux-arm;linux-arm64;linux-musl-arm;win-arm64;osx-arm64;linux-s390x;linux-ppc64le" RuntimePackExcludedRuntimeIdentifiers="android;linux-bionic" />
    <KnownFrameworkReference Include="Microsoft.Windows.SDK.NET.Ref" TargetFramework="net8.0-windows10.0.17763.0" RuntimeFrameworkName="Microsoft.Windows.SDK.NET.Ref" DefaultRuntimeFrameworkVersion="10.0.17763.44" LatestRuntimeFrameworkVersion="10.0.17763.44" TargetingPackName="Microsoft.Windows.SDK.NET.Ref" TargetingPackVersion="10.0.17763.44" RuntimePackAlwaysCopyLocal="true" RuntimePackNamePatterns="Microsoft.Windows.SDK.NET.Ref" RuntimePackRuntimeIdentifiers="any" IsWindowsOnly="true" />
    <KnownFrameworkReference Include="Microsoft.Windows.SDK.NET.Ref" TargetFramework="net8.0-windows10.0.18362.0" RuntimeFrameworkName="Microsoft.Windows.SDK.NET.Ref" DefaultRuntimeFrameworkVersion="10.0.18362.44" LatestRuntimeFrameworkVersion="10.0.18362.44" TargetingPackName="Microsoft.Windows.SDK.NET.Ref" TargetingPackVersion="10.0.18362.44" RuntimePackAlwaysCopyLocal="true" RuntimePackNamePatterns="Microsoft.Windows.SDK.NET.Ref" RuntimePackRuntimeIdentifiers="any" IsWindowsOnly="true" />
    <KnownFrameworkReference Include="Microsoft.Windows.SDK.NET.Ref" TargetFramework="net8.0-windows10.0.19041.0" RuntimeFrameworkName="Microsoft.Windows.SDK.NET.Ref" DefaultRuntimeFrameworkVersion="10.0.19041.44" LatestRuntimeFrameworkVersion="10.0.19041.44" TargetingPackName="Microsoft.Windows.SDK.NET.Ref" TargetingPackVersion="10.0.19041.44" RuntimePackAlwaysCopyLocal="true" RuntimePackNamePatterns="Microsoft.Windows.SDK.NET.Ref" RuntimePackRuntimeIdentifiers="any" IsWindowsOnly="true" />
    <!-- .NET 7.0 -->
    <KnownFrameworkReference Include="Microsoft.NETCore.App" TargetFramework="net7.0" RuntimeFrameworkName="Microsoft.NETCore.App" DefaultRuntimeFrameworkVersion="7.0.0" LatestRuntimeFrameworkVersion="7.0.20" TargetingPackName="Microsoft.NETCore.App.Ref" TargetingPackVersion="7.0.20" RuntimePackNamePatterns="Microsoft.NETCore.App.Runtime.**RID**" RuntimePackRuntimeIdentifiers="linux-arm;linux-arm64;linux-musl-arm64;linux-musl-x64;linux-x64;osx-x64;rhel.6-x64;tizen.4.0.0-armel;tizen.5.0.0-armel;win-arm;win-arm64;win-x64;win-x86;linux-musl-arm;osx-arm64;linux-s390x;linux-bionic-arm;linux-bionic-arm64;linux-bionic-x64;linux-bionic-x86;linux-ppc64le" />
    <KnownAppHostPack Include="Microsoft.NETCore.App" TargetFramework="net7.0" AppHostPackNamePattern="Microsoft.NETCore.App.Host.**RID**" AppHostPackVersion="7.0.20" AppHostRuntimeIdentifiers="linux-arm;linux-arm64;linux-musl-arm64;linux-musl-x64;linux-x64;osx-x64;rhel.6-x64;tizen.4.0.0-armel;tizen.5.0.0-armel;win-arm;win-arm64;win-x64;win-x86;linux-musl-arm;osx-arm64;linux-s390x;linux-bionic-arm;linux-bionic-arm64;linux-bionic-x64;linux-bionic-x86;linux-ppc64le" ExcludedRuntimeIdentifiers="android" />
    <KnownCrossgen2Pack Include="Microsoft.NETCore.App.Crossgen2" TargetFramework="net7.0" Crossgen2PackNamePattern="Microsoft.NETCore.App.Crossgen2.**RID**" Crossgen2PackVersion="7.0.20" Crossgen2RuntimeIdentifiers="linux-musl-x64;linux-x64;win-x64;linux-arm;linux-arm64;linux-musl-arm;linux-musl-arm64;osx-arm64;osx-x64;win-arm;win-arm64;win-x86" />
    <KnownILCompilerPack Include="Microsoft.DotNet.ILCompiler" TargetFramework="net7.0" ILCompilerPackNamePattern="runtime.**RID**.Microsoft.DotNet.ILCompiler" ILCompilerPackVersion="7.0.20" ILCompilerRuntimeIdentifiers="linux-arm64;linux-musl-arm64;linux-musl-x64;linux-x64;win-arm64;win-x64" />
    <KnownILLinkPack Include="Microsoft.NET.ILLink.Tasks" TargetFramework="net7.0" ILLinkPackVersion="7.0.100-1.23211.1" />
    <KnownWebAssemblySdkPack Include="Microsoft.NET.Sdk.WebAssembly.Pack" TargetFramework="net7.0" WebAssemblySdkPackVersion="8.0.10" />
    <KnownRuntimePack Include="Microsoft.NETCore.App" TargetFramework="net7.0" RuntimeFrameworkName="Microsoft.NETCore.App" LatestRuntimeFrameworkVersion="7.0.20" RuntimePackNamePatterns="Microsoft.NETCore.App.Runtime.Mono.**RID**" RuntimePackRuntimeIdentifiers="linux-arm;linux-arm64;linux-musl-arm64;linux-musl-x64;linux-x64;osx-x64;rhel.6-x64;win-arm;win-arm64;win-x64;win-x86;linux-musl-arm;osx-arm64;linux-s390x;browser-wasm;ios-arm64;ios-arm;iossimulator-arm64;iossimulator-x64;iossimulator-x86;tvos-arm64;tvossimulator-arm64;tvossimulator-x64;maccatalyst-x64;maccatalyst-arm64;android-arm64;android-arm;android-x64;android-x86" RuntimePackLabels="Mono" />
    <KnownFrameworkReference Include="Microsoft.WindowsDesktop.App" TargetFramework="net7.0" RuntimeFrameworkName="Microsoft.WindowsDesktop.App" DefaultRuntimeFrameworkVersion="7.0.0" LatestRuntimeFrameworkVersion="7.0.20" TargetingPackName="Microsoft.WindowsDesktop.App.Ref" TargetingPackVersion="7.0.20" RuntimePackNamePatterns="Microsoft.WindowsDesktop.App.Runtime.**RID**" RuntimePackRuntimeIdentifiers="win-x64;win-x86;win-arm64" IsWindowsOnly="true" />
    <KnownFrameworkReference Include="Microsoft.WindowsDesktop.App.WPF" TargetFramework="net7.0" RuntimeFrameworkName="Microsoft.WindowsDesktop.App" DefaultRuntimeFrameworkVersion="7.0.0" LatestRuntimeFrameworkVersion="7.0.20" TargetingPackName="Microsoft.WindowsDesktop.App.Ref" TargetingPackVersion="7.0.20" RuntimePackNamePatterns="Microsoft.WindowsDesktop.App.Runtime.**RID**" RuntimePackRuntimeIdentifiers="win-x64;win-x86;win-arm64" IsWindowsOnly="true" Profile="WPF" />
    <KnownFrameworkReference Include="Microsoft.WindowsDesktop.App.WindowsForms" TargetFramework="net7.0" RuntimeFrameworkName="Microsoft.WindowsDesktop.App" DefaultRuntimeFrameworkVersion="7.0.0" LatestRuntimeFrameworkVersion="7.0.20" TargetingPackName="Microsoft.WindowsDesktop.App.Ref" TargetingPackVersion="7.0.20" RuntimePackNamePatterns="Microsoft.WindowsDesktop.App.Runtime.**RID**" RuntimePackRuntimeIdentifiers="win-x64;win-x86;win-arm64" IsWindowsOnly="true" Profile="WindowsForms" />
    <KnownFrameworkReference Include="Microsoft.AspNetCore.App" TargetFramework="net7.0" RuntimeFrameworkName="Microsoft.AspNetCore.App" DefaultRuntimeFrameworkVersion="7.0.0" LatestRuntimeFrameworkVersion="7.0.20" TargetingPackName="Microsoft.AspNetCore.App.Ref" TargetingPackVersion="7.0.20" RuntimePackNamePatterns="Microsoft.AspNetCore.App.Runtime.**RID**" RuntimePackRuntimeIdentifiers="win-x64;win-x86;win-arm;osx-x64;linux-musl-x64;linux-musl-arm64;linux-x64;linux-arm;linux-arm64;linux-musl-arm;win-arm64;osx-arm64;linux-s390x;linux-ppc64le" RuntimePackExcludedRuntimeIdentifiers="android" />
    <KnownFrameworkReference Include="Microsoft.Windows.SDK.NET.Ref" TargetFramework="net7.0-windows10.0.17763.0" RuntimeFrameworkName="Microsoft.Windows.SDK.NET.Ref" DefaultRuntimeFrameworkVersion="10.0.17763.44" LatestRuntimeFrameworkVersion="10.0.17763.44" TargetingPackName="Microsoft.Windows.SDK.NET.Ref" TargetingPackVersion="10.0.17763.44" RuntimePackAlwaysCopyLocal="true" RuntimePackNamePatterns="Microsoft.Windows.SDK.NET.Ref" RuntimePackRuntimeIdentifiers="any" IsWindowsOnly="true" />
    <KnownFrameworkReference Include="Microsoft.Windows.SDK.NET.Ref" TargetFramework="net7.0-windows10.0.18362.0" RuntimeFrameworkName="Microsoft.Windows.SDK.NET.Ref" DefaultRuntimeFrameworkVersion="10.0.18362.44" LatestRuntimeFrameworkVersion="10.0.18362.44" TargetingPackName="Microsoft.Windows.SDK.NET.Ref" TargetingPackVersion="10.0.18362.44" RuntimePackAlwaysCopyLocal="true" RuntimePackNamePatterns="Microsoft.Windows.SDK.NET.Ref" RuntimePackRuntimeIdentifiers="any" IsWindowsOnly="true" />
    <KnownFrameworkReference Include="Microsoft.Windows.SDK.NET.Ref" TargetFramework="net7.0-windows10.0.19041.0" RuntimeFrameworkName="Microsoft.Windows.SDK.NET.Ref" DefaultRuntimeFrameworkVersion="10.0.19041.44" LatestRuntimeFrameworkVersion="10.0.19041.44" TargetingPackName="Microsoft.Windows.SDK.NET.Ref" TargetingPackVersion="10.0.19041.44" RuntimePackAlwaysCopyLocal="true" RuntimePackNamePatterns="Microsoft.Windows.SDK.NET.Ref" RuntimePackRuntimeIdentifiers="any" IsWindowsOnly="true" />
    <!-- .NET 6.0 -->
    <KnownFrameworkReference Include="Microsoft.NETCore.App" TargetFramework="net6.0" RuntimeFrameworkName="Microsoft.NETCore.App" DefaultRuntimeFrameworkVersion="6.0.0" LatestRuntimeFrameworkVersion="6.0.35" TargetingPackName="Microsoft.NETCore.App.Ref" TargetingPackVersion="6.0.35" RuntimePackNamePatterns="Microsoft.NETCore.App.Runtime.**RID**" RuntimePackRuntimeIdentifiers="linux-arm;linux-arm64;linux-musl-arm64;linux-musl-x64;linux-x64;osx-x64;rhel.6-x64;tizen.4.0.0-armel;tizen.5.0.0-armel;win-arm;win-arm64;win-x64;win-x86;linux-musl-arm;osx-arm64;linux-s390x" />
    <KnownAppHostPack Include="Microsoft.NETCore.App" TargetFramework="net6.0" AppHostPackNamePattern="Microsoft.NETCore.App.Host.**RID**" AppHostPackVersion="6.0.35" AppHostRuntimeIdentifiers="linux-arm;linux-arm64;linux-musl-arm64;linux-musl-x64;linux-x64;osx-x64;rhel.6-x64;tizen.4.0.0-armel;tizen.5.0.0-armel;win-arm;win-arm64;win-x64;win-x86;linux-musl-arm;osx-arm64;linux-s390x" ExcludedRuntimeIdentifiers="android" />
    <KnownCrossgen2Pack Include="Microsoft.NETCore.App.Crossgen2" TargetFramework="net6.0" Crossgen2PackNamePattern="Microsoft.NETCore.App.Crossgen2.**RID**" Crossgen2PackVersion="6.0.35" Crossgen2RuntimeIdentifiers="linux-musl-x64;linux-x64;win-x64;linux-arm;linux-arm64;linux-musl-arm;linux-musl-arm64;osx-arm64;osx-x64;win-arm;win-arm64;win-x86" />
    <KnownILLinkPack Include="Microsoft.NET.ILLink.Tasks" TargetFramework="net6.0" ILLinkPackVersion="7.0.100-1.23211.1" />
    <KnownWebAssemblySdkPack Include="Microsoft.NET.Sdk.WebAssembly.Pack" TargetFramework="net6.0" WebAssemblySdkPackVersion="8.0.10" />
    <KnownRuntimePack Include="Microsoft.NETCore.App" TargetFramework="net6.0" RuntimeFrameworkName="Microsoft.NETCore.App" LatestRuntimeFrameworkVersion="6.0.35" RuntimePackNamePatterns="Microsoft.NETCore.App.Runtime.Mono.**RID**" RuntimePackRuntimeIdentifiers="linux-arm;linux-arm64;linux-musl-arm64;linux-musl-x64;linux-x64;osx-x64;rhel.6-x64;win-arm;win-arm64;win-x64;win-x86;linux-musl-arm;osx-arm64;linux-s390x;browser-wasm;ios-arm64;ios-arm;iossimulator-arm64;iossimulator-x64;iossimulator-x86;tvos-arm64;tvossimulator-arm64;tvossimulator-x64;maccatalyst-x64;maccatalyst-arm64;android-arm64;android-arm;android-x64;android-x86" RuntimePackLabels="Mono" />
    <KnownFrameworkReference Include="Microsoft.WindowsDesktop.App" TargetFramework="net6.0" RuntimeFrameworkName="Microsoft.WindowsDesktop.App" DefaultRuntimeFrameworkVersion="6.0.0" LatestRuntimeFrameworkVersion="6.0.35" TargetingPackName="Microsoft.WindowsDesktop.App.Ref" TargetingPackVersion="6.0.35" RuntimePackNamePatterns="Microsoft.WindowsDesktop.App.Runtime.**RID**" RuntimePackRuntimeIdentifiers="win-x64;win-x86;win-arm64" IsWindowsOnly="true" />
    <KnownFrameworkReference Include="Microsoft.WindowsDesktop.App.WPF" TargetFramework="net6.0" RuntimeFrameworkName="Microsoft.WindowsDesktop.App" DefaultRuntimeFrameworkVersion="6.0.0" LatestRuntimeFrameworkVersion="6.0.35" TargetingPackName="Microsoft.WindowsDesktop.App.Ref" TargetingPackVersion="6.0.35" RuntimePackNamePatterns="Microsoft.WindowsDesktop.App.Runtime.**RID**" RuntimePackRuntimeIdentifiers="win-x64;win-x86;win-arm64" IsWindowsOnly="true" Profile="WPF" />
    <KnownFrameworkReference Include="Microsoft.WindowsDesktop.App.WindowsForms" TargetFramework="net6.0" RuntimeFrameworkName="Microsoft.WindowsDesktop.App" DefaultRuntimeFrameworkVersion="6.0.0" LatestRuntimeFrameworkVersion="6.0.35" TargetingPackName="Microsoft.WindowsDesktop.App.Ref" TargetingPackVersion="6.0.35" RuntimePackNamePatterns="Microsoft.WindowsDesktop.App.Runtime.**RID**" RuntimePackRuntimeIdentifiers="win-x64;win-x86;win-arm64" IsWindowsOnly="true" Profile="WindowsForms" />
    <KnownFrameworkReference Include="Microsoft.AspNetCore.App" TargetFramework="net6.0" RuntimeFrameworkName="Microsoft.AspNetCore.App" DefaultRuntimeFrameworkVersion="6.0.0" LatestRuntimeFrameworkVersion="6.0.35" TargetingPackName="Microsoft.AspNetCore.App.Ref" TargetingPackVersion="6.0.35" RuntimePackNamePatterns="Microsoft.AspNetCore.App.Runtime.**RID**" RuntimePackRuntimeIdentifiers="win-x64;win-x86;win-arm;osx-x64;linux-musl-x64;linux-musl-arm64;linux-x64;linux-arm;linux-arm64;linux-musl-arm;win-arm64;osx-arm64;linux-s390x" />
    <KnownFrameworkReference Include="Microsoft.Windows.SDK.NET.Ref" TargetFramework="net6.0-windows10.0.17763.0" RuntimeFrameworkName="Microsoft.Windows.SDK.NET.Ref" DefaultRuntimeFrameworkVersion="10.0.17763.44" LatestRuntimeFrameworkVersion="10.0.17763.44" TargetingPackName="Microsoft.Windows.SDK.NET.Ref" TargetingPackVersion="10.0.17763.44" RuntimePackAlwaysCopyLocal="true" RuntimePackNamePatterns="Microsoft.Windows.SDK.NET.Ref" RuntimePackRuntimeIdentifiers="any" IsWindowsOnly="true" />
    <KnownFrameworkReference Include="Microsoft.Windows.SDK.NET.Ref" TargetFramework="net6.0-windows10.0.18362.0" RuntimeFrameworkName="Microsoft.Windows.SDK.NET.Ref" DefaultRuntimeFrameworkVersion="10.0.18362.44" LatestRuntimeFrameworkVersion="10.0.18362.44" TargetingPackName="Microsoft.Windows.SDK.NET.Ref" TargetingPackVersion="10.0.18362.44" RuntimePackAlwaysCopyLocal="true" RuntimePackNamePatterns="Microsoft.Windows.SDK.NET.Ref" RuntimePackRuntimeIdentifiers="any" IsWindowsOnly="true" />
    <KnownFrameworkReference Include="Microsoft.Windows.SDK.NET.Ref" TargetFramework="net6.0-windows10.0.19041.0" RuntimeFrameworkName="Microsoft.Windows.SDK.NET.Ref" DefaultRuntimeFrameworkVersion="10.0.19041.44" LatestRuntimeFrameworkVersion="10.0.19041.44" TargetingPackName="Microsoft.Windows.SDK.NET.Ref" TargetingPackVersion="10.0.19041.44" RuntimePackAlwaysCopyLocal="true" RuntimePackNamePatterns="Microsoft.Windows.SDK.NET.Ref" RuntimePackRuntimeIdentifiers="any" IsWindowsOnly="true" />
    <!-- .NET 5.0 -->
    <KnownFrameworkReference Include="Microsoft.NETCore.App" TargetFramework="net5.0" RuntimeFrameworkName="Microsoft.NETCore.App" DefaultRuntimeFrameworkVersion="5.0.0" LatestRuntimeFrameworkVersion="5.0.17" TargetingPackName="Microsoft.NETCore.App.Ref" TargetingPackVersion="5.0.0" RuntimePackNamePatterns="Microsoft.NETCore.App.Runtime.**RID**" RuntimePackRuntimeIdentifiers="linux-arm;linux-arm64;linux-musl-arm64;linux-musl-x64;linux-x64;osx-x64;rhel.6-x64;tizen.4.0.0-armel;tizen.5.0.0-armel;win-arm;win-arm64;win-x64;win-x86;linux-musl-arm;browser-wasm" IsTrimmable="true" />
    <KnownAppHostPack Include="Microsoft.NETCore.App" TargetFramework="net5.0" AppHostPackNamePattern="Microsoft.NETCore.App.Host.**RID**" AppHostPackVersion="5.0.17" AppHostRuntimeIdentifiers="linux-arm;linux-arm64;linux-musl-arm64;linux-musl-x64;linux-x64;osx-x64;rhel.6-x64;tizen.4.0.0-armel;tizen.5.0.0-armel;win-arm;win-arm64;win-x64;win-x86;linux-musl-arm" />
    <KnownCrossgen2Pack Include="Microsoft.NETCore.App.Crossgen2" TargetFramework="net5.0" Crossgen2PackNamePattern="Microsoft.NETCore.App.Crossgen2.**RID**" Crossgen2PackVersion="5.0.17" Crossgen2RuntimeIdentifiers="linux-musl-x64;linux-x64;win-x64" />
    <KnownILLinkPack Include="Microsoft.NET.ILLink.Tasks" TargetFramework="net5.0" ILLinkPackVersion="7.0.100-1.23211.1" />
    <KnownFrameworkReference Include="Microsoft.WindowsDesktop.App" TargetFramework="net5.0" RuntimeFrameworkName="Microsoft.WindowsDesktop.App" DefaultRuntimeFrameworkVersion="5.0.0" LatestRuntimeFrameworkVersion="5.0.17" TargetingPackName="Microsoft.WindowsDesktop.App.Ref" TargetingPackVersion="5.0.0" RuntimePackNamePatterns="Microsoft.WindowsDesktop.App.Runtime.**RID**" RuntimePackRuntimeIdentifiers="win-x64;win-x86;win-arm64" IsWindowsOnly="true" />
    <KnownFrameworkReference Include="Microsoft.WindowsDesktop.App.WPF" TargetFramework="net5.0" RuntimeFrameworkName="Microsoft.WindowsDesktop.App" DefaultRuntimeFrameworkVersion="5.0.0" LatestRuntimeFrameworkVersion="5.0.17" TargetingPackName="Microsoft.WindowsDesktop.App.Ref" TargetingPackVersion="5.0.0" RuntimePackNamePatterns="Microsoft.WindowsDesktop.App.Runtime.**RID**" RuntimePackRuntimeIdentifiers="win-x64;win-x86;win-arm64" IsWindowsOnly="true" Profile="WPF" />
    <KnownFrameworkReference Include="Microsoft.WindowsDesktop.App.WindowsForms" TargetFramework="net5.0" RuntimeFrameworkName="Microsoft.WindowsDesktop.App" DefaultRuntimeFrameworkVersion="5.0.0" LatestRuntimeFrameworkVersion="5.0.17" TargetingPackName="Microsoft.WindowsDesktop.App.Ref" TargetingPackVersion="5.0.0" RuntimePackNamePatterns="Microsoft.WindowsDesktop.App.Runtime.**RID**" RuntimePackRuntimeIdentifiers="win-x64;win-x86;win-arm64" IsWindowsOnly="true" Profile="WindowsForms" />
    <KnownFrameworkReference Include="Microsoft.AspNetCore.App" TargetFramework="net5.0" RuntimeFrameworkName="Microsoft.AspNetCore.App" DefaultRuntimeFrameworkVersion="5.0.0" LatestRuntimeFrameworkVersion="5.0.17" TargetingPackName="Microsoft.AspNetCore.App.Ref" TargetingPackVersion="5.0.0" RuntimePackNamePatterns="Microsoft.AspNetCore.App.Runtime.**RID**" RuntimePackRuntimeIdentifiers="win-x64;win-x86;win-arm;osx-x64;linux-musl-x64;linux-musl-arm64;linux-x64;linux-arm;linux-arm64;linux-musl-arm;win-arm64" />
    <KnownFrameworkReference Include="Microsoft.Windows.SDK.NET.Ref" TargetFramework="net5.0-windows10.0.17763.0" RuntimeFrameworkName="Microsoft.Windows.SDK.NET.Ref" DefaultRuntimeFrameworkVersion="10.0.17763.44" LatestRuntimeFrameworkVersion="10.0.17763.44" TargetingPackName="Microsoft.Windows.SDK.NET.Ref" TargetingPackVersion="10.0.17763.44" RuntimePackAlwaysCopyLocal="true" RuntimePackNamePatterns="Microsoft.Windows.SDK.NET.Ref" RuntimePackRuntimeIdentifiers="any" IsWindowsOnly="true" />
    <KnownFrameworkReference Include="Microsoft.Windows.SDK.NET.Ref" TargetFramework="net5.0-windows10.0.18362.0" RuntimeFrameworkName="Microsoft.Windows.SDK.NET.Ref" DefaultRuntimeFrameworkVersion="10.0.18362.44" LatestRuntimeFrameworkVersion="10.0.18362.44" TargetingPackName="Microsoft.Windows.SDK.NET.Ref" TargetingPackVersion="10.0.18362.44" RuntimePackAlwaysCopyLocal="true" RuntimePackNamePatterns="Microsoft.Windows.SDK.NET.Ref" RuntimePackRuntimeIdentifiers="any" IsWindowsOnly="true" />
    <KnownFrameworkReference Include="Microsoft.Windows.SDK.NET.Ref" TargetFramework="net5.0-windows10.0.19041.0" RuntimeFrameworkName="Microsoft.Windows.SDK.NET.Ref" DefaultRuntimeFrameworkVersion="10.0.19041.44" LatestRuntimeFrameworkVersion="10.0.19041.44" TargetingPackName="Microsoft.Windows.SDK.NET.Ref" TargetingPackVersion="10.0.19041.44" RuntimePackAlwaysCopyLocal="true" RuntimePackNamePatterns="Microsoft.Windows.SDK.NET.Ref" RuntimePackRuntimeIdentifiers="any" IsWindowsOnly="true" />
    <!-- .NET Core 3.1  -->
    <KnownFrameworkReference Include="Microsoft.NETCore.App" TargetFramework="netcoreapp3.1" RuntimeFrameworkName="Microsoft.NETCore.App" DefaultRuntimeFrameworkVersion="3.1.0" LatestRuntimeFrameworkVersion="3.1.32" TargetingPackName="Microsoft.NETCore.App.Ref" TargetingPackVersion="3.1.0" RuntimePackNamePatterns="Microsoft.NETCore.App.Runtime.**RID**" RuntimePackRuntimeIdentifiers="linux-arm;linux-arm64;linux-musl-arm64;linux-musl-x64;linux-x64;osx-x64;rhel.6-x64;tizen.4.0.0-armel;tizen.5.0.0-armel;win-arm;win-arm64;win-x64;win-x86" IsTrimmable="true" />
    <KnownAppHostPack Include="Microsoft.NETCore.App" TargetFramework="netcoreapp3.1" AppHostPackNamePattern="Microsoft.NETCore.App.Host.**RID**" AppHostPackVersion="3.1.32" AppHostRuntimeIdentifiers="linux-arm;linux-arm64;linux-musl-arm64;linux-musl-x64;linux-x64;osx-x64;rhel.6-x64;tizen.4.0.0-armel;tizen.5.0.0-armel;win-arm;win-arm64;win-x64;win-x86" />
    <KnownILLinkPack Include="Microsoft.NET.ILLink.Tasks" TargetFramework="netcoreapp3.1" ILLinkPackVersion="7.0.100-1.23211.1" />
    <KnownFrameworkReference Include="Microsoft.WindowsDesktop.App" TargetFramework="netcoreapp3.1" RuntimeFrameworkName="Microsoft.WindowsDesktop.App" DefaultRuntimeFrameworkVersion="3.1.0" LatestRuntimeFrameworkVersion="3.1.32" TargetingPackName="Microsoft.WindowsDesktop.App.Ref" TargetingPackVersion="3.1.0" RuntimePackNamePatterns="Microsoft.WindowsDesktop.App.Runtime.**RID**" RuntimePackRuntimeIdentifiers="win-x64;win-x86" IsWindowsOnly="true" />
    <KnownFrameworkReference Include="Microsoft.WindowsDesktop.App.WPF" TargetFramework="netcoreapp3.1" RuntimeFrameworkName="Microsoft.WindowsDesktop.App" DefaultRuntimeFrameworkVersion="3.1.0" LatestRuntimeFrameworkVersion="3.1.32" TargetingPackName="Microsoft.WindowsDesktop.App.Ref" TargetingPackVersion="3.1.0" RuntimePackNamePatterns="Microsoft.WindowsDesktop.App.Runtime.**RID**" RuntimePackRuntimeIdentifiers="win-x64;win-x86" IsWindowsOnly="true" Profile="WPF" />
    <KnownFrameworkReference Include="Microsoft.WindowsDesktop.App.WindowsForms" TargetFramework="netcoreapp3.1" RuntimeFrameworkName="Microsoft.WindowsDesktop.App" DefaultRuntimeFrameworkVersion="3.1.0" LatestRuntimeFrameworkVersion="3.1.32" TargetingPackName="Microsoft.WindowsDesktop.App.Ref" TargetingPackVersion="3.1.0" RuntimePackNamePatterns="Microsoft.WindowsDesktop.App.Runtime.**RID**" RuntimePackRuntimeIdentifiers="win-x64;win-x86" IsWindowsOnly="true" Profile="WindowsForms" />
    <KnownFrameworkReference Include="Microsoft.AspNetCore.App" TargetFramework="netcoreapp3.1" RuntimeFrameworkName="Microsoft.AspNetCore.App" DefaultRuntimeFrameworkVersion="3.1.0" LatestRuntimeFrameworkVersion="3.1.32" TargetingPackName="Microsoft.AspNetCore.App.Ref" TargetingPackVersion="3.1.10" RuntimePackNamePatterns="Microsoft.AspNetCore.App.Runtime.**RID**" RuntimePackRuntimeIdentifiers="win-x64;win-x86;win-arm;osx-x64;linux-musl-x64;linux-musl-arm64;linux-x64;linux-arm;linux-arm64" />
    <!-- .NET Core 3.0 -->
    <KnownFrameworkReference Include="Microsoft.NETCore.App" TargetFramework="netcoreapp3.0" RuntimeFrameworkName="Microsoft.NETCore.App" DefaultRuntimeFrameworkVersion="3.0.0" LatestRuntimeFrameworkVersion="3.0.3" TargetingPackName="Microsoft.NETCore.App.Ref" TargetingPackVersion="3.0.0" RuntimePackNamePatterns="Microsoft.NETCore.App.Runtime.**RID**" RuntimePackRuntimeIdentifiers="linux-arm;linux-arm64;linux-musl-arm64;linux-musl-x64;linux-x64;osx-x64;rhel.6-x64;tizen.4.0.0-armel;tizen.5.0.0-armel;win-arm;win-arm64;win-x64;win-x86" IsTrimmable="true" />
    <KnownAppHostPack Include="Microsoft.NETCore.App" TargetFramework="netcoreapp3.0" AppHostPackNamePattern="Microsoft.NETCore.App.Host.**RID**" AppHostPackVersion="3.0.3" AppHostRuntimeIdentifiers="linux-arm;linux-arm64;linux-musl-arm64;linux-musl-x64;linux-x64;osx-x64;rhel.6-x64;tizen.4.0.0-armel;tizen.5.0.0-armel;win-arm;win-arm64;win-x64;win-x86" />
    <KnownILLinkPack Include="Microsoft.NET.ILLink.Tasks" TargetFramework="netcoreapp3.0" ILLinkPackVersion="7.0.100-1.23211.1" />
    <KnownFrameworkReference Include="Microsoft.WindowsDesktop.App" TargetFramework="netcoreapp3.0" RuntimeFrameworkName="Microsoft.WindowsDesktop.App" DefaultRuntimeFrameworkVersion="3.0.0" LatestRuntimeFrameworkVersion="3.0.3" TargetingPackName="Microsoft.WindowsDesktop.App.Ref" TargetingPackVersion="3.0.0" RuntimePackNamePatterns="Microsoft.WindowsDesktop.App.Runtime.**RID**" RuntimePackRuntimeIdentifiers="win-x64;win-x86" IsWindowsOnly="true" />
    <KnownFrameworkReference Include="Microsoft.WindowsDesktop.App.WPF" TargetFramework="netcoreapp3.0" RuntimeFrameworkName="Microsoft.WindowsDesktop.App" DefaultRuntimeFrameworkVersion="3.0.0" LatestRuntimeFrameworkVersion="3.0.3" TargetingPackName="Microsoft.WindowsDesktop.App.Ref" TargetingPackVersion="3.0.0" RuntimePackNamePatterns="Microsoft.WindowsDesktop.App.Runtime.**RID**" RuntimePackRuntimeIdentifiers="win-x64;win-x86" IsWindowsOnly="true" Profile="WPF" />
    <KnownFrameworkReference Include="Microsoft.WindowsDesktop.App.WindowsForms" TargetFramework="netcoreapp3.0" RuntimeFrameworkName="Microsoft.WindowsDesktop.App" DefaultRuntimeFrameworkVersion="3.0.0" LatestRuntimeFrameworkVersion="3.0.3" TargetingPackName="Microsoft.WindowsDesktop.App.Ref" TargetingPackVersion="3.0.0" RuntimePackNamePatterns="Microsoft.WindowsDesktop.App.Runtime.**RID**" RuntimePackRuntimeIdentifiers="win-x64;win-x86" IsWindowsOnly="true" Profile="WindowsForms" />
    <KnownFrameworkReference Include="Microsoft.AspNetCore.App" TargetFramework="netcoreapp3.0" RuntimeFrameworkName="Microsoft.AspNetCore.App" DefaultRuntimeFrameworkVersion="3.0.0" LatestRuntimeFrameworkVersion="3.0.3" TargetingPackName="Microsoft.AspNetCore.App.Ref" TargetingPackVersion="3.0.1" RuntimePackNamePatterns="Microsoft.AspNetCore.App.Runtime.**RID**" RuntimePackRuntimeIdentifiers="win-x64;win-x86;win-arm;osx-x64;linux-musl-x64;linux-musl-arm64;linux-x64;linux-arm;linux-arm64" />
    <KnownFrameworkReference Include="NETStandard.Library" TargetFramework="netstandard2.1" TargetingPackName="NETStandard.Library.Ref" TargetingPackVersion="2.1.0" />
    <!-- Supported Windows versions -->
    <WindowsSdkSupportedTargetPlatformVersion Include="10.0.26100.0" WindowsSdkPackageVersion="10.0.26100.44" MinimumNETVersion="8.0" />
    <WindowsSdkSupportedTargetPlatformVersion Include="10.0.22621.0" WindowsSdkPackageVersion="10.0.22621.44" MinimumNETVersion="8.0" />
    <WindowsSdkSupportedTargetPlatformVersion Include="10.0.22000.0" WindowsSdkPackageVersion="10.0.22000.44" MinimumNETVersion="8.0" />
    <WindowsSdkSupportedTargetPlatformVersion Include="10.0.20348.0" WindowsSdkPackageVersion="10.0.20348.44" MinimumNETVersion="8.0" />
    <WindowsSdkSupportedTargetPlatformVersion Include="10.0.19041.0" WindowsSdkPackageVersion="10.0.19041.44" MinimumNETVersion="8.0" />
    <WindowsSdkSupportedTargetPlatformVersion Include="10.0.18362.0" WindowsSdkPackageVersion="10.0.18362.44" MinimumNETVersion="8.0" />
    <WindowsSdkSupportedTargetPlatformVersion Include="10.0.17763.0" WindowsSdkPackageVersion="10.0.17763.44" MinimumNETVersion="8.0" />
    <WindowsSdkSupportedTargetPlatformVersion Include="10.0.26100.0" WindowsSdkPackageVersion="10.0.26100.43" MinimumNETVersion="6.0" />
    <WindowsSdkSupportedTargetPlatformVersion Include="10.0.22621.0" WindowsSdkPackageVersion="10.0.22621.43" MinimumNETVersion="6.0" />
    <WindowsSdkSupportedTargetPlatformVersion Include="10.0.22000.0" WindowsSdkPackageVersion="10.0.22000.43" MinimumNETVersion="6.0" />
    <WindowsSdkSupportedTargetPlatformVersion Include="10.0.20348.0" WindowsSdkPackageVersion="10.0.20348.43" MinimumNETVersion="6.0" />
    <WindowsSdkSupportedTargetPlatformVersion Include="10.0.19041.0" WindowsSdkPackageVersion="10.0.19041.43" MinimumNETVersion="6.0" />
    <WindowsSdkSupportedTargetPlatformVersion Include="10.0.18362.0" WindowsSdkPackageVersion="10.0.18362.43" MinimumNETVersion="6.0" />
    <WindowsSdkSupportedTargetPlatformVersion Include="10.0.17763.0" WindowsSdkPackageVersion="10.0.17763.43" MinimumNETVersion="6.0" />
    <WindowsSdkSupportedTargetPlatformVersion Include="10.0.22000.0" WindowsSdkPackageVersion="10.0.22000.26" MinimumNETVersion="5.0" />
    <WindowsSdkSupportedTargetPlatformVersion Include="10.0.20348.0" WindowsSdkPackageVersion="10.0.20348.26" MinimumNETVersion="5.0" />
    <WindowsSdkSupportedTargetPlatformVersion Include="10.0.19041.0" WindowsSdkPackageVersion="10.0.19041.26" MinimumNETVersion="5.0" />
    <WindowsSdkSupportedTargetPlatformVersion Include="10.0.18362.0" WindowsSdkPackageVersion="10.0.18362.26" MinimumNETVersion="5.0" />
    <WindowsSdkSupportedTargetPlatformVersion Include="10.0.17763.0" WindowsSdkPackageVersion="10.0.17763.26" MinimumNETVersion="5.0" />
    <WindowsSdkSupportedTargetPlatformVersion Include="8.0" />
    <WindowsSdkSupportedTargetPlatformVersion Include="7.0" />
    <_KnownRuntimeIdentiferPlatforms Include="any;aot;freebsd;illumos;solaris;unix;any;aot;freebsd;illumos;solaris;unix;any;aot;freebsd;illumos;solaris;unix;any;aot;freebsd;illumos;solaris;unix;any;aot;freebsd;illumos;solaris;unix;any;aot;freebsd;illumos;solaris;unix;any;aot;freebsd;illumos;solaris;unix" />
    <_ExcludedKnownRuntimeIdentiferPlatforms Include="rhel.6;tizen.4.0.0;tizen.5.0.0;rhel.6;tizen.4.0.0;tizen.5.0.0;rhel.6;tizen.4.0.0;tizen.5.0.0;rhel.6;tizen.4.0.0;tizen.5.0.0;rhel.6;tizen.4.0.0;tizen.5.0.0;rhel.6;tizen.4.0.0;tizen.5.0.0;rhel.6;tizen.4.0.0;tizen.5.0.0" />
  </ItemGroup>
  <!--
============================================================================================================================================
  </Import>

C:\Program Files\dotnet\sdk\8.0.110\Sdks\Microsoft.NET.Sdk\targets\Microsoft.NET.Sdk.DefaultItems.props
============================================================================================================================================
-->
  <PropertyGroup xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <!-- Disable web SDK implicit package versions for ASP.NET packages, since the .NET SDK now handles that -->
    <EnableWebSdkImplicitPackageVersions>false</EnableWebSdkImplicitPackageVersions>
  </PropertyGroup>
  <PropertyGroup xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <__WindowsAppSdkDefaultImageIncludes>**/*.png;**/*.bmp;**/*.jpg;**/*.dds;**/*.tif;**/*.tga;**/*.gif</__WindowsAppSdkDefaultImageIncludes>
  </PropertyGroup>
  <ItemGroup Condition=" '$(EnableDefaultItems)' == 'true' " xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <Compile Include="**/*$(DefaultLanguageSourceExtension)" Exclude="$(DefaultItemExcludes);$(DefaultExcludesInProjectFolder)" Condition=" '$(EnableDefaultCompileItems)' == 'true' " />
    <EmbeddedResource Include="**/*.resx" Exclude="$(DefaultItemExcludes);$(DefaultExcludesInProjectFolder)" Condition=" '$(EnableDefaultEmbeddedResourceItems)' == 'true' " />
    <!-- Microsoft.WindowsAppSDK is a NuGet delivered SDK. EnableDefaultWindowsAppSdkContentItems and EnableDefaultWindowsAppSdkPRIResourceItems are overridable properties that the SDK will set to true by default. -->
    <Content Include="$(__WindowsAppSdkDefaultImageIncludes)" Exclude="$(DefaultItemExcludes);$(DefaultExcludesInProjectFolder)" Condition=" '$(EnableDefaultContentItems)' != 'false' And '$(EnableDefaultWindowsAppSdkContentItems)' == 'true' " />
    <PRIResource Include="**/*.resw" Exclude="$(DefaultItemExcludes);$(DefaultExcludesInProjectFolder)" Condition=" '$(EnableDefaultPRIResourceItems)' != 'false' And '$(EnableDefaultWindowsAppSdkPRIResourceItems)' == 'true' " />
  </ItemGroup>
  <ItemGroup Condition=" '$(EnableDefaultItems)' == 'true' And '$(EnableDefaultNoneItems)' == 'true' " xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <None Include="**/*" Exclude="$(DefaultItemExcludes);$(DefaultExcludesInProjectFolder)" />
    <None Remove="**/*$(DefaultLanguageSourceExtension)" />
    <None Remove="**/*.resx" />
    <!-- Microsoft.WindowsAppSDK is a NuGet delivered SDK. EnableDefaultWindowsAppSdkContentItems and EnableDefaultWindowsAppSdkPRIResourceItems are overridable properties that the SDK will set to true by default. -->
    <None Remove="$(__WindowsAppSdkDefaultImageIncludes)" Condition=" '$(EnableDefaultWindowsAppSdkContentItems)' == 'true' " />
    <None Remove="**/*.resw" Condition=" '$(EnableDefaultWindowsAppSdkPRIResourceItems)' == 'true' " />
  </ItemGroup>
  <!-- Automatically reference NETStandard.Library or Microsoft.NETCore.App package if targeting the corresponding target framework.
      We can refer here in the .props file to properties set in the .targets files because items and their conditions are
      evaluated in the second pass of evaluation, after all properties have been evaluated. -->
  <ItemGroup Condition=" '$(DisableImplicitFrameworkReferences)' != 'true' and '$(TargetFrameworkIdentifier)' == '.NETStandard' And '$(_TargetFrameworkVersionWithoutV)' &lt; '2.1'" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <PackageReference Include="NETStandard.Library" Version="$(NETStandardImplicitPackageVersion)" IsImplicitlyDefined="true" />
    <!-- If targeting .NET Standard 2.0 or higher, then don't include a dependency on NETStandard.Library in the package produced by pack -->
    <PackageReference Update="NETStandard.Library" Condition=" ('$(_TargetFrameworkVersionWithoutV)' != '') And ('$(_TargetFrameworkVersionWithoutV)' &gt;= '2.0') " PrivateAssets="All" Publish="true" />
  </ItemGroup>
  <ItemGroup Condition=" '$(DisableImplicitFrameworkReferences)' != 'true' and '$(TargetFrameworkIdentifier)' == '.NETStandard' And '$(_TargetFrameworkVersionWithoutV)' &gt;= '2.1'" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <FrameworkReference Include="NETStandard.Library" IsImplicitlyDefined="true" Pack="false" PrivateAssets="All" />
  </ItemGroup>
  <ItemGroup Condition=" '$(DisableImplicitFrameworkReferences)' != 'true' and '$(TargetFrameworkIdentifier)' == '.NETCoreApp'" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <!-- Use implicit PackageReference for Microsoft.NETCore.App on versions prior to 3.0.  For 3.0 or higher, use
         an implicit FrameworkReference -->
    <PackageReference Include="Microsoft.NETCore.App" IsImplicitlyDefined="true" Condition="('$(_TargetFrameworkVersionWithoutV)' != '') And ('$(_TargetFrameworkVersionWithoutV)' &lt; '3.0')" />
    <!-- For targeting .NET Core 2.0 or higher, don't include a dependency on Microsoft.NETCore.App in the package produced by pack.
         Packing an DotnetCliTool should include the Microsoft.NETCore.App package dependency. -->
    <PackageReference Update="Microsoft.NETCore.App" Condition="('$(_TargetFrameworkVersionWithoutV)' != '') And ('$(_TargetFrameworkVersionWithoutV)' &gt;= '2.0') And ('$(PackageType)' != 'DotnetCliTool')" PrivateAssets="All" Publish="true" />
    <FrameworkReference Include="Microsoft.NETCore.App" IsImplicitlyDefined="true" Pack="false" PrivateAssets="All" Condition="('$(_TargetFrameworkVersionWithoutV)' != '') And ('$(_TargetFrameworkVersionWithoutV)' &gt;= '3.0')" />
    <!-- Allow opt-in to Mono runtime pack for .NET 6.0 or higher -->
    <FrameworkReference Update="Microsoft.NETCore.App" RuntimePackLabels="Mono" Condition="'$(UseMonoRuntime)' == 'true' And ('$(_TargetFrameworkVersionWithoutV)' != '') And ('$(_TargetFrameworkVersionWithoutV)' &gt;= '6.0')" />
    <!-- Allow opt-in to NativeAOT runtime pack for .NET 8.0 or higher -->
    <FrameworkReference Update="Microsoft.NETCore.App" RuntimePackLabels="NativeAOT" Condition="'$(_IsPublishing)' == 'true' and '$(PublishAotUsingRuntimePack)' == 'true' And ('$(_TargetFrameworkVersionWithoutV)' != '') And ('$(_TargetFrameworkVersionWithoutV)' &gt;= '8.0')" />
  </ItemGroup>
  <!--
============================================================================================================================================
  </Import>

C:\Program Files\dotnet\sdk\8.0.110\Sdks\Microsoft.NET.Sdk\targets\Microsoft.NET.Sdk.props
============================================================================================================================================
-->
  <!-- Put workload resolution behind a feature flag.  It can be enabled either by setting the MSBuildEnableWorkloadResolver environment variable to true, or by
       putting an EnableWorkloadResolver.sentinel file beside the MSBuild SDK resolver DLL -->
  <PropertyGroup Condition="'$(MSBuildEnableWorkloadResolver)' == ''" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <__DisableWorkloadResolverSentinelPath Condition="'$(MSBuildRuntimeType)' == 'Core'">$(MSBuildBinPath)\DisableWorkloadResolver.sentinel</__DisableWorkloadResolverSentinelPath>
    <__DisableWorkloadResolverSentinelPath Condition="'$(MSBuildRuntimeType)' != 'Core'">$(MSBuildBinPath)\SdkResolvers\Microsoft.DotNet.MSBuildSdkResolver\DisableWorkloadResolver.sentinel</__DisableWorkloadResolverSentinelPath>
    <MSBuildEnableWorkloadResolver Condition="!Exists('$(__DisableWorkloadResolverSentinelPath)')">true</MSBuildEnableWorkloadResolver>
  </PropertyGroup>
  <!-- Import workload props -->
  <!--<Import Project="Microsoft.NET.Sdk.ImportWorkloads.props" Condition="'$(MSBuildEnableWorkloadResolver)' == 'true'" />-->
  <!-- List of supported .NET Core and .NET Standard TFMs -->
  <!--
============================================================================================================================================
  <Import Project="Microsoft.NET.SupportedTargetFrameworks.props">

C:\Program Files\dotnet\sdk\8.0.110\Sdks\Microsoft.NET.Sdk\targets\Microsoft.NET.SupportedTargetFrameworks.props
============================================================================================================================================
-->
  <!--
***********************************************************************************************
Microsoft.NET.SupportedTargetFrameworks.targets

WARNING:  DO NOT MODIFY this file unless you are knowledgeable about MSBuild and have
          created a backup copy.  Incorrect changes to this file will make it
          impossible to load or build your projects from the command-line or the IDE.

Copyright (c) .NET Foundation. All rights reserved.
***********************************************************************************************
-->
  <!-- This file contains a list of the TFMs that are supported by this SDK for .NET Core, .NET Standard, and .NET Framework.
     This is used by VS to show the list of frameworks to which projects can be retargeted. -->
  <!-- .NET Core App -->
  <ItemGroup>
    <SupportedNETCoreAppTargetFramework Include=".NETCoreApp,Version=v1.0" DisplayName=".NET Core 1.0" Alias="netcoreapp1.0" />
    <SupportedNETCoreAppTargetFramework Include=".NETCoreApp,Version=v1.1" DisplayName=".NET Core 1.1" Alias="netcoreapp1.1" />
    <SupportedNETCoreAppTargetFramework Include=".NETCoreApp,Version=v2.0" DisplayName=".NET Core 2.0" Alias="netcoreapp2.0" />
    <SupportedNETCoreAppTargetFramework Include=".NETCoreApp,Version=v2.1" DisplayName=".NET Core 2.1" Alias="netcoreapp2.1" />
    <SupportedNETCoreAppTargetFramework Include=".NETCoreApp,Version=v2.2" DisplayName=".NET Core 2.2" Alias="netcoreapp2.2" />
    <SupportedNETCoreAppTargetFramework Include=".NETCoreApp,Version=v3.0" DisplayName=".NET Core 3.0" Alias="netcoreapp3.0" />
    <SupportedNETCoreAppTargetFramework Include=".NETCoreApp,Version=v3.1" DisplayName=".NET Core 3.1" Alias="netcoreapp3.1" />
    <SupportedNETCoreAppTargetFramework Include=".NETCoreApp,Version=v5.0" DisplayName=".NET 5.0" Alias="net5.0" />
    <SupportedNETCoreAppTargetFramework Include=".NETCoreApp,Version=v6.0" DisplayName=".NET 6.0" Alias="net6.0" />
    <SupportedNETCoreAppTargetFramework Include=".NETCoreApp,Version=v7.0" DisplayName=".NET 7.0" Alias="net7.0" />
    <SupportedNETCoreAppTargetFramework Include=".NETCoreApp,Version=v8.0" DisplayName=".NET 8.0" Alias="net8.0" Condition="$([MSBuild]::VersionGreaterThanOrEquals($(MSBuildVersion), '17.8.0'))" />
  </ItemGroup>
  <PropertyGroup>
    <UnsupportedTargetFrameworkVersion>9.0</UnsupportedTargetFrameworkVersion>
    <MinimumVisualStudioVersionForUnsupportedTargetFrameworkVersion>17.8</MinimumVisualStudioVersionForUnsupportedTargetFrameworkVersion>
  </PropertyGroup>
  <!-- .NET Standard -->
  <ItemGroup>
    <SupportedNETStandardTargetFramework Include=".NETStandard,Version=v1.0" DisplayName=".NET Standard 1.0" Alias="netstandard1.0" />
    <SupportedNETStandardTargetFramework Include=".NETStandard,Version=v1.1" DisplayName=".NET Standard 1.1" Alias="netstandard1.1" />
    <SupportedNETStandardTargetFramework Include=".NETStandard,Version=v1.2" DisplayName=".NET Standard 1.2" Alias="netstandard1.2" />
    <SupportedNETStandardTargetFramework Include=".NETStandard,Version=v1.3" DisplayName=".NET Standard 1.3" Alias="netstandard1.3" />
    <SupportedNETStandardTargetFramework Include=".NETStandard,Version=v1.4" DisplayName=".NET Standard 1.4" Alias="netstandard1.4" />
    <SupportedNETStandardTargetFramework Include=".NETStandard,Version=v1.5" DisplayName=".NET Standard 1.5" Alias="netstandard1.5" />
    <SupportedNETStandardTargetFramework Include=".NETStandard,Version=v1.6" DisplayName=".NET Standard 1.6" Alias="netstandard1.6" />
    <SupportedNETStandardTargetFramework Include=".NETStandard,Version=v2.0" DisplayName=".NET Standard 2.0" Alias="netstandard2.0" />
    <SupportedNETStandardTargetFramework Include=".NETStandard,Version=v2.1" DisplayName=".NET Standard 2.1" Alias="netstandard2.1" />
  </ItemGroup>
  <!-- .NET Framework -->
  <ItemGroup>
    <SupportedNETFrameworkTargetFramework Include=".NETFramework,Version=v2.0" DisplayName=".NET Framework 2.0" Alias="net20" />
    <SupportedNETFrameworkTargetFramework Include=".NETFramework,Version=v3.0" DisplayName=".NET Framework 3.0" Alias="net30" />
    <SupportedNETFrameworkTargetFramework Include=".NETFramework,Version=v3.5" DisplayName=".NET Framework 3.5" Alias="net35" />
    <SupportedNETFrameworkTargetFramework Include=".NETFramework,Version=v4.0" DisplayName=".NET Framework 4.0" Alias="net40" />
    <SupportedNETFrameworkTargetFramework Include=".NETFramework,Version=v4.5" DisplayName=".NET Framework 4.5" Alias="net45" />
    <SupportedNETFrameworkTargetFramework Include=".NETFramework,Version=v4.5.1" DisplayName=".NET Framework 4.5.1" Alias="net451" />
    <SupportedNETFrameworkTargetFramework Include=".NETFramework,Version=v4.5.2" DisplayName=".NET Framework 4.5.2" Alias="net452" />
    <SupportedNETFrameworkTargetFramework Include=".NETFramework,Version=v4.6" DisplayName=".NET Framework 4.6" Alias="net46" />
    <SupportedNETFrameworkTargetFramework Include=".NETFramework,Version=v4.6.1" DisplayName=".NET Framework 4.6.1" Alias="net461" />
    <SupportedNETFrameworkTargetFramework Include=".NETFramework,Version=v4.6.2" DisplayName=".NET Framework 4.6.2" Alias="net462" />
    <SupportedNETFrameworkTargetFramework Include=".NETFramework,Version=v4.7" DisplayName=".NET Framework 4.7" Alias="net47" />
    <SupportedNETFrameworkTargetFramework Include=".NETFramework,Version=v4.7.1" DisplayName=".NET Framework 4.7.1" Alias="net471" />
    <SupportedNETFrameworkTargetFramework Include=".NETFramework,Version=v4.7.2" DisplayName=".NET Framework 4.7.2" Alias="net472" />
    <SupportedNETFrameworkTargetFramework Include=".NETFramework,Version=v4.8" DisplayName=".NET Framework 4.8" Alias="net48" />
    <SupportedNETFrameworkTargetFramework Include=".NETFramework,Version=v4.8.1" DisplayName=".NET Framework 4.8.1" Alias="net481" />
  </ItemGroup>
  <!-- All supported target frameworks -->
  <ItemGroup>
    <SupportedTargetFramework Include="@(SupportedNETCoreAppTargetFramework);@(SupportedNETStandardTargetFramework);@(SupportedNETFrameworkTargetFramework)" />
  </ItemGroup>
  <!--
============================================================================================================================================
  </Import>

C:\Program Files\dotnet\sdk\8.0.110\Sdks\Microsoft.NET.Sdk\targets\Microsoft.NET.Sdk.props
============================================================================================================================================
-->
  <!-- List of supported target platforms -->
  <!--
============================================================================================================================================
  <Import Project="Microsoft.NET.SupportedPlatforms.props">

C:\Program Files\dotnet\sdk\8.0.110\Sdks\Microsoft.NET.Sdk\targets\Microsoft.NET.SupportedPlatforms.props
============================================================================================================================================
-->
  <!--
***********************************************************************************************
Microsoft.NET.SupportedPlatforms.props

WARNING:  DO NOT MODIFY this file unless you are knowledgeable about MSBuild and have
          created a backup copy.  Incorrect changes to this file will make it
          impossible to load or build your projects from the command-line or the IDE.

Copyright (c) .NET Foundation. All rights reserved.
***********************************************************************************************
-->
  <ItemGroup>
    <!-- Platforms supported by this SDK for analyzer warnings. Spec: https://github.com/dotnet/designs/blob/main/accepted/2020/platform-exclusion/platform-exclusion.md  -->
    <SupportedPlatform Include="Linux" />
    <SupportedPlatform Include="macOS" />
    <SupportedPlatform Include="Windows" />
  </ItemGroup>
  <!--
============================================================================================================================================
  </Import>

C:\Program Files\dotnet\sdk\8.0.110\Sdks\Microsoft.NET.Sdk\targets\Microsoft.NET.Sdk.props
============================================================================================================================================
-->
  <!-- List of supported .NET windows target platform versions -->
  <!--
============================================================================================================================================
  <Import Project="Microsoft.NET.WindowsSdkSupportedTargetPlatforms.props">

C:\Program Files\dotnet\sdk\8.0.110\Sdks\Microsoft.NET.Sdk\targets\Microsoft.NET.WindowsSdkSupportedTargetPlatforms.props
============================================================================================================================================
-->
  <!--
***********************************************************************************************
Microsoft.NET.WindowsSdkSupportedTargetPlatforms.props

WARNING:  DO NOT MODIFY this file unless you are knowledgeable about MSBuild and have
          created a backup copy.  Incorrect changes to this file will make it
          impossible to load or build your projects from the command-line or the IDE.

Copyright (c) .NET Foundation. All rights reserved.
***********************************************************************************************
-->
  <!-- This file contains a list of the windows target platform versions that are supported by this SDK for .NET. Supported versions are processed in _NormalizeTargetPlatformVersion -->
  <!-- These will be added to the BundledVersions.props that's generated in dotnet/installer.  So only add them here if we don't have that change yet -->
  <ItemGroup Condition="'@(WindowsSdkSupportedTargetPlatformVersion)' == ''">
    <WindowsSdkSupportedTargetPlatformVersion Include="10.0.19041.0" WindowsSdkPackageVersion="10.0.19041.16" MinimumNETVersion="5.0" />
    <WindowsSdkSupportedTargetPlatformVersion Include="10.0.18362.0" WindowsSdkPackageVersion="10.0.18362.16" MinimumNETVersion="5.0" />
    <WindowsSdkSupportedTargetPlatformVersion Include="10.0.17763.0" WindowsSdkPackageVersion="10.0.17763.16" MinimumNETVersion="5.0" />
    <WindowsSdkSupportedTargetPlatformVersion Include="8.0" />
    <WindowsSdkSupportedTargetPlatformVersion Include="7.0" />
  </ItemGroup>
  <ItemGroup>
    <SdkSupportedTargetPlatformVersion Condition="'$(TargetPlatformIdentifier)' == 'Windows'" Include="@(WindowsSdkSupportedTargetPlatformVersion)" />
  </ItemGroup>
  <!--
============================================================================================================================================
  </Import>

C:\Program Files\dotnet\sdk\8.0.110\Sdks\Microsoft.NET.Sdk\targets\Microsoft.NET.Sdk.props
============================================================================================================================================
-->
  <!--
============================================================================================================================================
  <Import Project="$(MSBuildThisFileDirectory)Microsoft.NET.Sdk.SourceLink.props">

C:\Program Files\dotnet\sdk\8.0.110\Sdks\Microsoft.NET.Sdk\targets\Microsoft.NET.Sdk.SourceLink.props
============================================================================================================================================
-->
  <!--
***********************************************************************************************
Microsoft.NET.Sdk.SourceLink.props

WARNING:  DO NOT MODIFY this file unless you are knowledgeable about MSBuild and have
          created a backup copy.  Incorrect changes to this file will make it
          impossible to load or build your projects from the command-line or the IDE.

Copyright (c) .NET Foundation. All rights reserved.
***********************************************************************************************
-->
  <PropertyGroup xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <!-- Suppress implicit SourceLink inclusion if any Microsoft.SourceLink package is referenced. -->
    <SuppressImplicitGitSourceLink Condition="'$(PkgMicrosoft_SourceLink_Common)' != ''">true</SuppressImplicitGitSourceLink>
    <_SourceLinkPropsImported>true</_SourceLinkPropsImported>
  </PropertyGroup>
  <!--<ImportGroup Condition="'$(SuppressImplicitGitSourceLink)' != 'true'">-->
  <!--
============================================================================================================================================
  <Import Project="$(MSBuildThisFileDirectory)..\..\Microsoft.Build.Tasks.Git\build\Microsoft.Build.Tasks.Git.props">

C:\Program Files\dotnet\sdk\8.0.110\Sdks\Microsoft.Build.Tasks.Git\build\Microsoft.Build.Tasks.Git.props
============================================================================================================================================
-->
  <!-- Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the License.txt file in the project root for more information. -->
  <PropertyGroup>
    <MicrosoftBuildTasksGitAssemblyFile Condition="'$(MSBuildRuntimeType)' != 'Core'">$(MSBuildThisFileDirectory)..\tools\net472\Microsoft.Build.Tasks.Git.dll</MicrosoftBuildTasksGitAssemblyFile>
    <MicrosoftBuildTasksGitAssemblyFile Condition="'$(MSBuildRuntimeType)' == 'Core'">$(MSBuildThisFileDirectory)..\tools\core\Microsoft.Build.Tasks.Git.dll</MicrosoftBuildTasksGitAssemblyFile>
  </PropertyGroup>
  <!--
============================================================================================================================================
  </Import>

C:\Program Files\dotnet\sdk\8.0.110\Sdks\Microsoft.NET.Sdk\targets\Microsoft.NET.Sdk.SourceLink.props
============================================================================================================================================
-->
  <!--
============================================================================================================================================
  <Import Project="$(MSBuildThisFileDirectory)..\..\Microsoft.SourceLink.Common\build\Microsoft.SourceLink.Common.props">

C:\Program Files\dotnet\sdk\8.0.110\Sdks\Microsoft.SourceLink.Common\build\Microsoft.SourceLink.Common.props
============================================================================================================================================
-->
  <!-- Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the License.txt file in the project root for more information. -->
  <PropertyGroup>
    <_MicrosoftSourceLinkCommonAssemblyFile Condition="'$(MSBuildRuntimeType)' != 'Core'">$(MSBuildThisFileDirectory)..\tools\net472\Microsoft.SourceLink.Common.dll</_MicrosoftSourceLinkCommonAssemblyFile>
    <_MicrosoftSourceLinkCommonAssemblyFile Condition="'$(MSBuildRuntimeType)' == 'Core'">$(MSBuildThisFileDirectory)..\tools\core\Microsoft.SourceLink.Common.dll</_MicrosoftSourceLinkCommonAssemblyFile>
  </PropertyGroup>
  <PropertyGroup>
    <!--
      Used to suppress querying source control and features that use the information (e.g. git commit SHA).
    -->
    <EnableSourceControlManagerQueries Condition="'$(EnableSourceControlManagerQueries)' == ''">true</EnableSourceControlManagerQueries>
    <!--
      Do not generate SourceLink when building in the IDE or for Live Unit Testing.
    -->
    <EnableSourceLink Condition="'$(EnableSourceLink)' == '' and '$(DesignTimeBuild)' != 'true' and '$(BuildingForLiveUnitTesting)' != 'true'">true</EnableSourceLink>
  </PropertyGroup>
  <!--
============================================================================================================================================
  </Import>

C:\Program Files\dotnet\sdk\8.0.110\Sdks\Microsoft.NET.Sdk\targets\Microsoft.NET.Sdk.SourceLink.props
============================================================================================================================================
-->
  <!--
============================================================================================================================================
  <Import Project="$(MSBuildThisFileDirectory)..\..\Microsoft.SourceLink.GitHub\build\Microsoft.SourceLink.GitHub.props">

C:\Program Files\dotnet\sdk\8.0.110\Sdks\Microsoft.SourceLink.GitHub\build\Microsoft.SourceLink.GitHub.props
============================================================================================================================================
-->
  <!-- Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the License.txt file in the project root for more information. -->
  <ItemGroup>
    <SourceLinkGitHubHost Include="github.com" ContentUrl="https://raw.githubusercontent.com" />
  </ItemGroup>
  <!--
============================================================================================================================================
  </Import>

C:\Program Files\dotnet\sdk\8.0.110\Sdks\Microsoft.NET.Sdk\targets\Microsoft.NET.Sdk.SourceLink.props
============================================================================================================================================
-->
  <!--
============================================================================================================================================
  <Import Project="$(MSBuildThisFileDirectory)..\..\Microsoft.SourceLink.GitLab\build\Microsoft.SourceLink.GitLab.props">

C:\Program Files\dotnet\sdk\8.0.110\Sdks\Microsoft.SourceLink.GitLab\build\Microsoft.SourceLink.GitLab.props
============================================================================================================================================
-->
  <!-- Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the License.txt file in the project root for more information. -->
  <ItemGroup>
    <SourceLinkGitLabHost Include="gitlab.com" />
  </ItemGroup>
  <!--
============================================================================================================================================
  </Import>

C:\Program Files\dotnet\sdk\8.0.110\Sdks\Microsoft.NET.Sdk\targets\Microsoft.NET.Sdk.SourceLink.props
============================================================================================================================================
-->
  <!--
============================================================================================================================================
  <Import Project="$(MSBuildThisFileDirectory)..\..\Microsoft.SourceLink.AzureRepos.Git\build\Microsoft.SourceLink.AzureRepos.Git.props">

C:\Program Files\dotnet\sdk\8.0.110\Sdks\Microsoft.SourceLink.AzureRepos.Git\build\Microsoft.SourceLink.AzureRepos.Git.props
============================================================================================================================================
-->
  <!-- Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the License.txt file in the project root for more information. -->
  <ItemGroup>
    <SourceLinkAzureReposGitHost Include="visualstudio.com" />
    <SourceLinkAzureReposGitHost Include="vsts.me" />
    <SourceLinkAzureReposGitHost Include="dev.azure.com" />
  </ItemGroup>
  <!--
============================================================================================================================================
  </Import>

C:\Program Files\dotnet\sdk\8.0.110\Sdks\Microsoft.NET.Sdk\targets\Microsoft.NET.Sdk.SourceLink.props
============================================================================================================================================
-->
  <!--
============================================================================================================================================
  <Import Project="$(MSBuildThisFileDirectory)..\..\Microsoft.SourceLink.Bitbucket.Git\build\Microsoft.SourceLink.Bitbucket.Git.props">

C:\Program Files\dotnet\sdk\8.0.110\Sdks\Microsoft.SourceLink.Bitbucket.Git\build\Microsoft.SourceLink.Bitbucket.Git.props
============================================================================================================================================
-->
  <!-- Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the License.txt file in the project root for more information. -->
  <ItemGroup>
    <SourceLinkBitbucketGitHost Include="bitbucket.org" EnterpriseEdition="false" />
  </ItemGroup>
  <!--
============================================================================================================================================
  </Import>

C:\Program Files\dotnet\sdk\8.0.110\Sdks\Microsoft.NET.Sdk\targets\Microsoft.NET.Sdk.SourceLink.props
============================================================================================================================================
-->
  <!--</ImportGroup>-->
  <!--
============================================================================================================================================
  </Import>

C:\Program Files\dotnet\sdk\8.0.110\Sdks\Microsoft.NET.Sdk\targets\Microsoft.NET.Sdk.props
============================================================================================================================================
-->
  <!--
============================================================================================================================================
  <Import Project="$(MSBuildThisFileDirectory)Microsoft.NET.Sdk.CSharp.props" Condition="'$(MSBuildProjectExtension)' == '.csproj'">

C:\Program Files\dotnet\sdk\8.0.110\Sdks\Microsoft.NET.Sdk\targets\Microsoft.NET.Sdk.CSharp.props
============================================================================================================================================
-->
  <!--
***********************************************************************************************
Microsoft.NET.Sdk.CSharp.props

WARNING:  DO NOT MODIFY this file unless you are knowledgeable about MSBuild and have
          created a backup copy.  Incorrect changes to this file will make it
          impossible to load or build your projects from the command-line or the IDE.

Copyright (c) .NET Foundation. All rights reserved.
***********************************************************************************************
-->
  <PropertyGroup xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <NoWarn Condition=" '$(NoWarn)' == '' ">1701;1702</NoWarn>
    <!-- Remove the line below once https://github.com/Microsoft/visualfsharp/issues/3207 gets fixed -->
    <WarningsAsErrors>$(WarningsAsErrors);NU1605</WarningsAsErrors>
  </PropertyGroup>
  <PropertyGroup xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <DefineConstants Condition=" '$(DefineConstants)' != '' ">$(DefineConstants);</DefineConstants>
    <DefineConstants>$(DefineConstants)TRACE</DefineConstants>
  </PropertyGroup>
  <!-- Implicit imports -->
  <ItemGroup Condition="'$(ImplicitUsings)' == 'true' Or '$(ImplicitUsings)' == 'enable'" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <Using Include="System" />
    <Using Include="System.Collections.Generic" />
    <Using Include="System.IO" />
    <Using Include="System.Linq" />
    <Using Include="System.Net.Http" Condition="'$(TargetFrameworkIdentifier)' != '.NETFramework'" />
    <Using Include="System.Threading" />
    <Using Include="System.Threading.Tasks" />
  </ItemGroup>
  <!--
============================================================================================================================================
  </Import>

C:\Program Files\dotnet\sdk\8.0.110\Sdks\Microsoft.NET.Sdk\targets\Microsoft.NET.Sdk.props
============================================================================================================================================
-->
  <!--<Import Project="$(MSBuildThisFileDirectory)Microsoft.NET.Sdk.VisualBasic.props" Condition="'$(MSBuildProjectExtension)' == '.vbproj'" />-->
  <!--<Import Project="$(MSBuildThisFileDirectory)Microsoft.NET.Sdk.FSharp.props" Condition="'$(MSBuildProjectExtension)' == '.fsproj'" />-->
  <!--
============================================================================================================================================
  <Import Project="$(MSBuildThisFileDirectory)Microsoft.NET.PackTool.props">

C:\Program Files\dotnet\sdk\8.0.110\Sdks\Microsoft.NET.Sdk\targets\Microsoft.NET.PackTool.props
============================================================================================================================================
-->
  <!--
***********************************************************************************************
Microsoft.NET.PackTool.props

WARNING:  DO NOT MODIFY this file unless you are knowledgeable about MSBuild and have
          created a backup copy.  Incorrect changes to this file will make it
          impossible to load or build your projects from the command-line or the IDE.

Copyright (c) .NET Foundation. All rights reserved.
***********************************************************************************************
-->
  <PropertyGroup xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <TargetsForTfmSpecificContentInPackage>$(TargetsForTfmSpecificContentInPackage);PackTool</TargetsForTfmSpecificContentInPackage>
  </PropertyGroup>
  <!--
============================================================================================================================================
  </Import>

C:\Program Files\dotnet\sdk\8.0.110\Sdks\Microsoft.NET.Sdk\targets\Microsoft.NET.Sdk.props
============================================================================================================================================
-->
  <!--
============================================================================================================================================
  <Import Project="$(MSBuildThisFileDirectory)Microsoft.NET.PackProjectTool.props">

C:\Program Files\dotnet\sdk\8.0.110\Sdks\Microsoft.NET.Sdk\targets\Microsoft.NET.PackProjectTool.props
============================================================================================================================================
-->
  <!--
***********************************************************************************************
Microsoft.NET.PackProjectTool.props

WARNING:  DO NOT MODIFY this file unless you are knowledgeable about MSBuild and have
          created a backup copy.  Incorrect changes to this file will make it
          impossible to load or build your projects from the command-line or the IDE.

Copyright (c) .NET Foundation. All rights reserved.
***********************************************************************************************
-->
  <PropertyGroup xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <TargetsForTfmSpecificContentInPackage>$(TargetsForTfmSpecificContentInPackage);_PackProjectToolValidation</TargetsForTfmSpecificContentInPackage>
  </PropertyGroup>
  <!--
============================================================================================================================================
  </Import>

C:\Program Files\dotnet\sdk\8.0.110\Sdks\Microsoft.NET.Sdk\targets\Microsoft.NET.Sdk.props
============================================================================================================================================
-->
  <!--
============================================================================================================================================
  <Import Project="$(MSBuildThisFileDirectory)../../Microsoft.NET.Sdk.WindowsDesktop/targets/Microsoft.NET.Sdk.WindowsDesktop.props" Condition="Exists('$(MSBuildThisFileDirectory)../../Microsoft.NET.Sdk.WindowsDesktop/targets/Microsoft.NET.Sdk.WindowsDesktop.props')">

C:\Program Files\dotnet\sdk\8.0.110\Sdks\Microsoft.NET.Sdk.WindowsDesktop\targets\Microsoft.NET.Sdk.WindowsDesktop.props
============================================================================================================================================
-->
  <ItemDefinitionGroup Condition=" '$(_EnableWindowsDesktopGlobbing)' == 'true' ">
    <ApplicationDefinition>
      <Generator>MSBuild:Compile</Generator>
      <XamlRuntime>$(DefaultXamlRuntime)</XamlRuntime>
      <SubType>Designer</SubType>
    </ApplicationDefinition>
    <Page>
      <Generator>MSBuild:Compile</Generator>
      <XamlRuntime>$(DefaultXamlRuntime)</XamlRuntime>
      <SubType>Designer</SubType>
    </Page>
  </ItemDefinitionGroup>
  <ItemGroup Condition=" '$(_EnableWindowsDesktopGlobbing)' == 'true' ">
    <ApplicationDefinition Include="App.xaml" Condition="'$(EnableDefaultApplicationDefinition)' != 'false' And Exists('$(MSBuildProjectDirectory)/App.xaml') And '$(MSBuildProjectExtension)' == '.csproj'" />
    <ApplicationDefinition Include="Application.xaml" Condition="'$(EnableDefaultApplicationDefinition)' != 'false' And Exists('$(MSBuildProjectDirectory)/Application.xaml') And '$(MSBuildProjectExtension)' == '.vbproj'" />
    <Page Include="**/*.xaml" Exclude="$(DefaultItemExcludes);$(DefaultExcludesInProjectFolder);@(ApplicationDefinition)" Condition="'$(EnableDefaultPageItems)' != 'false'" />
    <!--
      See https://github.com/dotnet/wpf/issues/685
      Visual Studio would prefer that we remove **/*.xaml instead of
      being more precise.

      <None Remove="@(Page)"
              Condition="'$(EnableDefaultPageItems)' != 'false'" />
      <None Remove="@(ApplicationDefinition)"
            Condition="'$(EnableDefaultApplicationDefinition)' != 'false'" />
    -->
    <None Remove="**/*.xaml" Condition="'$(EnableDefaultApplicationDefinition)' != 'false' And '$(EnableDefaultPageItems)' != 'false'" />
  </ItemGroup>
  <ItemGroup Condition=" '$(_EnableWindowsDesktopNetCoreFrameworkReferences)' == 'true' ">
    <FrameworkReference Include="Microsoft.WindowsDesktop.App" IsImplicitlyDefined="true" Condition="('$(UseWPF)' == 'true') And ('$(UseWindowsForms)' == 'true')" />
    <FrameworkReference Include="Microsoft.WindowsDesktop.App.WPF" IsImplicitlyDefined="true" Condition="('$(UseWPF)' == 'true') And ('$(UseWindowsForms)' != 'true')" />
    <FrameworkReference Include="Microsoft.WindowsDesktop.App.WindowsForms" IsImplicitlyDefined="true" Condition="('$(UseWPF)' != 'true') And ('$(UseWindowsForms)' == 'true')" />
  </ItemGroup>
  <!--
    Traditionally, Visual Studio has supplied these references for .NET Framework based
    WPF Projects:

    .NET 3.x:   PresentationCore, PresentationFramework, WindowsBase
    .NET 4.x:   PresentationCore, PresentationFramework, WindowsBase, System.Xaml


    Microsoft.NET.WindowsDesktop.SDK will supply the following references to .NET Framework based
    WPF Projects:

    .NET 3.x:   PresentationCore, PresentationFramework, WindowsBase

    .NET 4.0:   PresentationCore, PresentationFramework, WindowsBase, System.Xaml,
                UIAutomationClient, UIAutomationClientSideProviders, UIAutomationProvider, UIAutomationTypes

    .NET 4.5+:  PresentationCore, PresentationFramework, WindowsBase, System.Xaml,
                UIAutomationClient, UIAutomationClientSideProviders, UIAutomationProvider, UIAutomationTypes
                System.Windows.Controls.Ribbon
  -->
  <ItemGroup Condition=" '$(_EnableWindowsDesktopNETFrameworkImplicitReference)' == 'true' ">
    <!--
      The following 3 _WpfCommonNetFxReference items normally require Condition="'$(_TargetFrameworkVersionValue)' >= '3.0'", since
      they are supported on .NET Framework 3.0 and above.

      This condition is implicitly satisfied by '$(_TargetFrameworkVersionValue)' >= '$(_WindowsDesktopSdkTargetFrameworkVersionFloor)'
      in the outer ItemGroup
    -->
    <_WpfCommonNetFxReference Include="WindowsBase" />
    <_WpfCommonNetFxReference Include="PresentationCore" />
    <_WpfCommonNetFxReference Include="PresentationFramework" />
    <_WpfCommonNetFxReference Include="System.Xaml" Condition="'$(_TargetFrameworkVersionValue)' != '' And '$(_TargetFrameworkVersionValue)' &gt;= '4.0'">
      <RequiredTargetFramework>4.0</RequiredTargetFramework>
    </_WpfCommonNetFxReference>
    <_WpfCommonNetFxReference Include="UIAutomationClient" Condition="'$(_TargetFrameworkVersionValue)' != '' And '$(_TargetFrameworkVersionValue)' &gt;= '4.0'" />
    <_WpfCommonNetFxReference Include="UIAutomationClientSideProviders" Condition="'$(_TargetFrameworkVersionValue)' != '' And '$(_TargetFrameworkVersionValue)' &gt;= '4.0'" />
    <_WpfCommonNetFxReference Include="UIAutomationProvider" Condition="'$(_TargetFrameworkVersionValue)' != '' And '$(_TargetFrameworkVersionValue)' &gt;= '4.0'" />
    <_WpfCommonNetFxReference Include="UIAutomationTypes" Condition="'$(_TargetFrameworkVersionValue)' != '' And '$(_TargetFrameworkVersionValue)' &gt;= '4.0'" />
    <_WpfCommonNetFxReference Include="System.Windows.Controls.Ribbon" Condition="'$(_TargetFrameworkVersionValue)' != '' And '$(_TargetFrameworkVersionValue)' &gt;= '4.5'" />
  </ItemGroup>
  <ItemGroup Condition=" '$(_EnableWindowsDesktopNETFrameworkImplicitReference)' == 'true' ">
    <_SDKImplicitReference Include="@(_WpfCommonNetFxReference)" Condition="'$(UseWPF)' == 'true'" />
    <_SDKImplicitReference Include="System.Windows.Forms" Condition="('$(UseWindowsForms)' == 'true') " />
    <_SDKImplicitReference Include="WindowsFormsIntegration" Condition=" ('$(UseWindowsForms)' == 'true') And ('$(UseWPF)' == 'true') " />
  </ItemGroup>
  <!--
        Supported (and unsupported) TargetFrameworks

        Visual Studio Project System determines the list of valid TargetFrameworks to show
        in the Project properties by querying SupportedTargetFramework values.

        The Project System does not refer to this list at this time for .NET Framework TFM's.
  -->
  <!--
    When WindowsDesktop SDK is used without setting UseWPF or UseWindowsForms, it shows a (suppressible) warning and functions much
    like Microsoft.NET.Sdk

    Likewise, when WindowsDesktop SDK is used with a netcore TFM that is less than 3.0, it will simply act as if it were an
    Microsoft.NET.Sdk project (and show a suppressible build-time warning).

    Detect these situations and skip updates to @(SupportedTargetFramework) etc.
  -->
  <ItemGroup Condition=" '$(_RemoveUnsupportedTargetFrameworksForWindowsDesktop)' == 'true' ">
    <!--
        Windows Forms and WPF are supported only on .NET Core 3.0+
    -->
    <_UnsupportedNETCoreAppTargetFramework Include=".NETCoreApp,Version=v1.0" />
    <_UnsupportedNETCoreAppTargetFramework Include=".NETCoreApp,Version=v1.1" />
    <_UnsupportedNETCoreAppTargetFramework Include=".NETCoreApp,Version=v2.0" />
    <_UnsupportedNETCoreAppTargetFramework Include=".NETCoreApp,Version=v2.1" />
    <_UnsupportedNETCoreAppTargetFramework Include=".NETCoreApp,Version=v2.2" />
    <!--
        Windows Forms and WPF are not supported an any .NET Standard targets
    -->
    <_UnsupportedNETStandardTargetFramework Include="@(SupportedNETStandardTargetFramework)" />
    <!--
        Windows Forms was supported since .NET Framework 1.0, and is currently supported on
        .NET Framework 2.0+.

        WPF is supported on .NET Framework and WPF are supported on .NET Framework 3.0+

        In practice, the WindowsDesktop SDK is only supported on .NET Framework 3.0+ - this is controlled
        by $(_WindowsDesktopSdkTargetFrameworkVersionFloor), defined as 3.0, which applies to both .NETFramework
        and .NETCore.

        Here, we will encode .NET Framework 3.0 as the lowest supported version for both Windows Forms and WPF.

        The SDK does not define versions < 2.0 in @(SupportedNETFrameworkTargetFramework) list, so none of those
        need to be excluded here - removing 2.0 would suffice.
    -->
    <_UnsupportedNETFrameworkTargetFramework Include=".NETFramework,Version=v2.0" />
    <SupportedNETCoreAppTargetFramework Remove="@(_UnsupportedNETCoreAppTargetFramework)" />
    <SupportedNETStandardTargetFramework Remove="@(_UnsupportedNETStandardTargetFramework)" />
    <SupportedNETFrameworkTargetFramework Remove="@(_UnsupportedNETFrameworkTargetFramework)" />
    <SupportedTargetFramework Remove="@(_UnsupportedNETCoreAppTargetFramework);@(_UnsupportedNETStandardTargetFramework);@(_UnsupportedNETFrameworkTargetFramework)" />
  </ItemGroup>
  <!--
    Import Windows Forms props.
    These come via the Windows Forms transport package, that can be found under
    https://github.com/dotnet/winforms/tree/main/pkg/Microsoft.Private.Winforms/sdk
  -->
  <!--
============================================================================================================================================
  <Import Project="Microsoft.NET.Sdk.WindowsDesktop.WindowsForms.props">

C:\Program Files\dotnet\sdk\8.0.110\Sdks\Microsoft.NET.Sdk.WindowsDesktop\targets\Microsoft.NET.Sdk.WindowsDesktop.WindowsForms.props
============================================================================================================================================
-->
  <!--
    This props file comes from dotnet/winforms. It gets ingested by dotnet/wpf and processed by
    packaging/Microsoft.NET.Sdk.WindowsDesktop project.
    
    It is referenced via Microsoft.NET.Sdk.WindowsDesktop/targets/Microsoft.NET.Sdk.WindowsDesktop.props.
   -->
  <!--
    ============================================================
                      GenerateImplicitNamespaceImports
    Generates implicit namespace imports source to intermediate directory for Windows Forms projects
    ============================================================
    -->
  <ItemGroup Condition="'$(UseWindowsForms)' == 'true' and ('$(ImplicitUsings)' == 'true' or '$(ImplicitUsings)' == 'enable')">
    <!--
      SDK defines the following global usings:
      * System
      * System.Collections.Generic
      * System.Linq
      * System.Threading.Tasks
    -->
    <Using Include="System.Drawing" />
    <Using Include="System.Windows.Forms" />
  </ItemGroup>
  <!-- Windows Forms source generator and analyzers -->
  <!--
============================================================================================================================================
  <Import Project="System.Windows.Forms.Analyzers.props">

C:\Program Files\dotnet\sdk\8.0.110\Sdks\Microsoft.NET.Sdk.WindowsDesktop\targets\System.Windows.Forms.Analyzers.props
============================================================================================================================================
-->
  <!--
    This props file comes from dotnet/winforms. It gets ingested by dotnet/wpf and processed by
    packaging/Microsoft.NET.Sdk.WindowsDesktop project.
    
    It is referenced via Microsoft.NET.Sdk.WindowsDesktop.WindowsForms.props.
   -->
  <!-- Import for Windows Forms applications or when developing/testing locally -->
  <ItemGroup Condition="('$(UseWindowsForms)' == 'true') or ('$(ResolveWinFormsAnalyzersFromSdkRefPack)' == 'false')">
    <!-- Known Application properties -->
    <CompilerVisibleProperty Include="ApplicationManifest" />
    <CompilerVisibleProperty Include="StartupObject" />
    <!-- 
      Custom Application properties
      See for more details: https://github.com/dotnet/designs/blob/main/accepted/2021/winforms/streamline-application-bootstrap.md#msbuild-properties
      -->
    <CompilerVisibleProperty Include="ApplicationDefaultFont" />
    <CompilerVisibleProperty Include="ApplicationHighDpiMode" />
    <CompilerVisibleProperty Include="ApplicationUseCompatibleTextRendering" />
    <CompilerVisibleProperty Include="ApplicationVisualStyles" />
    <!-- If there is an app.manifest - let the generator explore it -->
    <AdditionalFiles Include="$(ApplicationManifest)" Condition="'$(ApplicationManifest)' != ''" />
  </ItemGroup>
  <!--
============================================================================================================================================
  </Import>

C:\Program Files\dotnet\sdk\8.0.110\Sdks\Microsoft.NET.Sdk.WindowsDesktop\targets\Microsoft.NET.Sdk.WindowsDesktop.WindowsForms.props
============================================================================================================================================
-->
  <!--
============================================================================================================================================
  </Import>

C:\Program Files\dotnet\sdk\8.0.110\Sdks\Microsoft.NET.Sdk.WindowsDesktop\targets\Microsoft.NET.Sdk.WindowsDesktop.props
============================================================================================================================================
-->
  <!--
============================================================================================================================================
  <Import Project="Microsoft.NET.Sdk.WindowsDesktop.WPF.props">

C:\Program Files\dotnet\sdk\8.0.110\Sdks\Microsoft.NET.Sdk.WindowsDesktop\targets\Microsoft.NET.Sdk.WindowsDesktop.WPF.props
============================================================================================================================================
-->
  <!--
    Generates implicit global namespace imports file <projectname>.ImplicitGlobalNamespaceImports.cs.
  -->
  <ItemGroup Condition="'$(UseWPF)' == 'true' and ('$(ImplicitUsings)' == 'true' or '$(ImplicitUsings)' == 'enable')">
    <Using Remove="System.IO" />
    <Using Remove="System.Net.Http" />
  </ItemGroup>
  <!--
============================================================================================================================================
  </Import>

C:\Program Files\dotnet\sdk\8.0.110\Sdks\Microsoft.NET.Sdk.WindowsDesktop\targets\Microsoft.NET.Sdk.WindowsDesktop.props
============================================================================================================================================
-->
  <!--
============================================================================================================================================
  </Import>

C:\Program Files\dotnet\sdk\8.0.110\Sdks\Microsoft.NET.Sdk\targets\Microsoft.NET.Sdk.props
============================================================================================================================================
-->
  <!--
============================================================================================================================================
  <Import Project="$(MSBuildThisFileDirectory)Microsoft.NET.Windows.props">

C:\Program Files\dotnet\sdk\8.0.110\Sdks\Microsoft.NET.Sdk\targets\Microsoft.NET.Windows.props
============================================================================================================================================
-->
  <!--
***********************************************************************************************
Microsoft.NET.Windows.props

WARNING:  DO NOT MODIFY this file unless you are knowledgeable about MSBuild and have
          created a backup copy.  Incorrect changes to this file will make it
          impossible to load or build your projects from the command-line or the IDE.

Copyright (c) .NET Foundation. All rights reserved.
***********************************************************************************************
-->
  <ItemGroup Condition=" '$(IncludeWindowsSDKRefFrameworkReferences)' == 'true' " xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <FrameworkReference Include="Microsoft.Windows.SDK.NET.Ref" IsImplicitlyDefined="true" Pack="false" PrivateAssets="All" />
  </ItemGroup>
  <PropertyGroup xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <MicrosoftNETWindowsWorkloadInstalled>true</MicrosoftNETWindowsWorkloadInstalled>
    <!--https://github.com/dotnet/sdk/issues/12403-->
    <_TargetFrameworkVersionValue>0.0</_TargetFrameworkVersionValue>
    <_WindowsDesktopSdkTargetFrameworkVersionFloor>3.0</_WindowsDesktopSdkTargetFrameworkVersionFloor>
  </PropertyGroup>
  <!--
============================================================================================================================================
  </Import>

C:\Program Files\dotnet\sdk\8.0.110\Sdks\Microsoft.NET.Sdk\targets\Microsoft.NET.Sdk.props
============================================================================================================================================
-->
  <!--
============================================================================================================================================
  </Import>

C:\Program Files\dotnet\sdk\8.0.110\Sdks\Microsoft.NET.Sdk\Sdk\Sdk.props
============================================================================================================================================
-->
  <!--
============================================================================================================================================
  </Import>

C:\Users\calope\source\repos\runtime8\src\libraries\System.IO.Ports\tests\System.IO.Ports.Tests.csproj
============================================================================================================================================
-->
  <PropertyGroup>
    <TargetFrameworks>$(NetCoreAppCurrent)-windows;$(NetCoreAppCurrent)-linux;$(NetCoreAppCurrent)-osx;$(NetCoreAppCurrent)-freebsd;$(NetFrameworkMinimum)</TargetFrameworks>
  </PropertyGroup>
  <ItemGroup>
    <Compile Include="AssemblyInfo.cs" />
    <Compile Include="SerialPort\BaseStream.cs" />
    <Compile Include="SerialPort\BaudRate.cs" />
    <Compile Include="SerialPort\BreakState.cs" />
    <Compile Include="SerialPort\BytesToRead.cs" />
    <Compile Include="SerialPort\BytesToSend.cs" />
    <Compile Include="SerialPort\CDHolding.cs" />
    <Compile Include="SerialPort\Close.cs" />
    <Compile Include="SerialPort\ctor.cs" />
    <Compile Include="SerialPort\ctor_IContainer.cs" />
    <Compile Include="SerialPort\ctor_str.cs" />
    <Compile Include="SerialPort\ctor_str_int.cs" />
    <Compile Include="SerialPort\ctor_str_int_parity.cs" />
    <Compile Include="SerialPort\ctor_str_int_parity_int.cs" />
    <Compile Include="SerialPort\ctor_str_int_parity_int_stopbits.cs" />
    <Compile Include="SerialPort\CtsHolding.cs" />
    <Compile Include="SerialPort\DataBits.cs" />
    <Compile Include="SerialPort\DiscardInBuffer.cs" />
    <Compile Include="SerialPort\DiscardInBuffer_Generic.cs" />
    <Compile Include="SerialPort\DiscardNull.cs" />
    <Compile Include="SerialPort\DiscardOutBuffer.cs" />
    <Compile Include="SerialPort\DiscardOutBuffer_Generic.cs" />
    <Compile Include="SerialPort\DsrHolding.cs" />
    <Compile Include="SerialPort\DtrEnable.cs" />
    <Compile Include="SerialPort\Encoding.cs" />
    <Compile Include="SerialPort\ErrorEvent.cs" />
    <Compile Include="SerialPort\Event_Close_Stress.cs" />
    <Compile Include="SerialPort\Event_Generic.cs" />
    <Compile Include="SerialPort\GetPortNames.cs" />
    <Compile Include="SerialPort\Handshake.cs" />
    <Compile Include="SerialPort\IsOpen.cs" />
    <Compile Include="SerialPort\NewLine.cs" />
    <Compile Include="SerialPort\AbortOnError.cs" />
    <Compile Include="SerialPort\Open.cs" />
    <Compile Include="SerialPort\Open_Stress.cs" />
    <Compile Include="SerialPort\Parity.cs" />
    <Compile Include="SerialPort\ParityReplace.cs" />
    <Compile Include="SerialPort\PinChangedEvent.cs" />
    <Compile Include="SerialPort\ReadBufferSize.cs" />
    <Compile Include="SerialPort\ReadByte.cs" />
    <Compile Include="SerialPort\ReadByte_Generic.cs" />
    <Compile Include="SerialPort\ReadChar.cs" />
    <Compile Include="SerialPort\ReadChar_Generic.cs" />
    <Compile Include="SerialPort\ReadExisting.cs" />
    <Compile Include="SerialPort\ReadExisting_Generic.cs" />
    <Compile Include="SerialPort\ReadLine.cs" />
    <Compile Include="SerialPort\ReadLine_Generic.cs" />
    <Compile Include="SerialPort\ReadTimeout.cs" />
    <Compile Include="SerialPort\ReadTo.cs" />
    <Compile Include="SerialPort\ReadTo_Generic.cs" />
    <Compile Include="SerialPort\Read_byte_int_int.cs" />
    <Compile Include="SerialPort\Read_byte_int_int_Generic.cs" />
    <Compile Include="SerialPort\Read_char_int_int.cs" />
    <Compile Include="SerialPort\Read_char_int_int_Generic.cs" />
    <Compile Include="SerialPort\ReceivedBytesThreshold.cs" />
    <Compile Include="SerialPort\ReceivedEvent.cs" />
    <Compile Include="SerialPort\RtsEnable.cs" />
    <Compile Include="SerialPort\SerialPortRegressions.cs" />
    <Compile Include="SerialPort\StopBits.cs" />
    <Compile Include="SerialPort\Stress01.cs" />
    <Compile Include="SerialPort\WriteBufferSize.cs" />
    <Compile Include="SerialPort\WriteLine.cs" />
    <Compile Include="SerialPort\WriteLine_Generic.cs" />
    <Compile Include="SerialPort\WriteTimeout.cs" />
    <Compile Include="SerialPort\Write_byte_int_int.cs" />
    <Compile Include="SerialPort\Write_byte_int_int_Generic.cs" />
    <Compile Include="SerialPort\Write_char_int_int.cs" />
    <Compile Include="SerialPort\Write_char_int_int_Generic.cs" />
    <Compile Include="SerialPort\Write_str.cs" />
    <Compile Include="SerialPort\Write_str_Generic.cs" />
    <Compile Include="SerialStream\BeginRead.cs" />
    <Compile Include="SerialStream\BeginRead_Generic.cs" />
    <Compile Include="SerialStream\BeginWrite.cs" />
    <Compile Include="SerialStream\BeginWrite_Generic.cs" />
    <Compile Include="SerialStream\CanRead.cs" />
    <Compile Include="SerialStream\CanSeek.cs" />
    <Compile Include="SerialStream\CanTimeout.cs" />
    <Compile Include="SerialStream\CanWrite.cs" />
    <Compile Include="SerialStream\Close.cs" />
    <Compile Include="SerialStream\EndRead.cs" />
    <Compile Include="SerialStream\EndWrite.cs" />
    <Compile Include="SerialStream\Flush.cs" />
    <Compile Include="SerialStream\Length.cs" />
    <Compile Include="SerialStream\Position.cs" />
    <Compile Include="SerialStream\ReadByte.cs" />
    <Compile Include="SerialStream\ReadByte_Generic.cs" />
    <Compile Include="SerialStream\ReadTimeout.cs" />
    <Compile Include="SerialStream\Read_byte_int_int.cs" />
    <Compile Include="SerialStream\Read_byte_int_int_Generic.cs" />
    <Compile Include="SerialStream\Seek.cs" />
    <Compile Include="SerialStream\SetLength.cs" />
    <Compile Include="SerialStream\WriteByte.cs" />
    <Compile Include="SerialStream\WriteByte_Generic.cs" />
    <Compile Include="SerialStream\WriteTimeout.cs" />
    <Compile Include="SerialStream\Write_byte_int_int.cs" />
    <Compile Include="SerialStream\Write_byte_int_int_Generic.cs" />
    <Compile Include="Support\FlowControlCapabilities.cs" />
    <Compile Include="Support\LocalMachineSerialInfo.cs" />
    <Compile Include="Support\PortHelper.cs" />
    <Compile Include="Support\SerialPortConnection.cs" />
    <Compile Include="Support\SerialPortProperties.cs" />
    <Compile Include="Support\KnownFailureAttribute.cs" />
    <Compile Include="Support\TCSupport.cs" />
    <Compile Include="Support\PortsTest.cs" />
    <Compile Include="Support\TestEventHandler.cs" />
  </ItemGroup>
  <ItemGroup Condition="'$(TargetPlatformIdentifier)' == 'windows' or '$(TargetFrameworkIdentifier)' == '.NETFramework'">
    <Compile Include="SerialPort\DosDevices.cs" />
    <Compile Include="SerialPort\PortName.cs" />
    <Compile Include="SerialPort\OpenDevices.cs" />
  </ItemGroup>
  <ItemGroup>
    <Folder Include="Legacy\Manual\" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\src\System.IO.Ports.csproj" />
  </ItemGroup>
  <ItemGroup Condition="'$(TargetFrameworkIdentifier)' == '.NETFramework'">
    <ProjectReference Include="$(LibrariesProjectRoot)System.Text.Encoding.CodePages\src\System.Text.Encoding.CodePages.csproj" />
  </ItemGroup>
  <!--
============================================================================================================================================
  <Import Project="Sdk.targets" Sdk="Microsoft.NET.Sdk">
  This import was added implicitly because the Project element's Sdk attribute specified "Microsoft.NET.Sdk".

C:\Program Files\dotnet\sdk\8.0.110\Sdks\Microsoft.NET.Sdk\Sdk\Sdk.targets
============================================================================================================================================
-->
  <!--
***********************************************************************************************
Sdk.targets

WARNING:  DO NOT MODIFY this file unless you are knowledgeable about MSBuild and have
          created a backup copy.  Incorrect changes to this file will make it
          impossible to load or build your projects from the command-line or the IDE.

Copyright (c) .NET Foundation. All rights reserved.
***********************************************************************************************
-->
  <!-- Using the same property as Microsoft.CSharp.targets and presumably Microsoft.VisualBasic.targets here -->
  <PropertyGroup Condition="'$(TargetFrameworks)' != '' and '$(TargetFramework)' == ''" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <IsCrossTargetingBuild>true</IsCrossTargetingBuild>
  </PropertyGroup>
  <!--
============================================================================================================================================
  <Import Project="$(MSBuildThisFileDirectory)..\targets\Microsoft.NET.Sdk.BeforeCommonCrossTargeting.targets" Condition="'$(IsCrossTargetingBuild)' == 'true'">

C:\Program Files\dotnet\sdk\8.0.110\Sdks\Microsoft.NET.Sdk\targets\Microsoft.NET.Sdk.BeforeCommonCrossTargeting.targets
============================================================================================================================================
-->
  <!--
***********************************************************************************************
Microsoft.NET.Sdk.BeforeCommonCrossTargeting.targets

WARNING:  DO NOT MODIFY this file unless you are knowledgeable about MSBuild and have
          created a backup copy.  Incorrect changes to this file will make it
          impossible to load or build your projects from the command-line or the IDE.

Copyright (c) .NET Foundation. All rights reserved.
***********************************************************************************************
-->
  <!--
    Apply these defaults from Microsoft.Common.CurrentVersion.targets now since we're running before them,
    but need to adjust them and/or make decisions in terms of them.
   -->
  <PropertyGroup xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <Configuration Condition="'$(Configuration)'==''">Debug</Configuration>
    <Platform Condition="'$(Platform)'==''">AnyCPU</Platform>
    <PlatformName Condition="'$(PlatformName)' == ''">$(Platform)</PlatformName>
  </PropertyGroup>
  <!--
============================================================================================================================================
  <Import Project="$(MSBuildThisFileDirectory)Microsoft.NET.DefaultAssemblyInfo.targets">

C:\Program Files\dotnet\sdk\8.0.110\Sdks\Microsoft.NET.Sdk\targets\Microsoft.NET.DefaultAssemblyInfo.targets
============================================================================================================================================
-->
  <!--
***********************************************************************************************
Microsoft.NET.DefaultAssemblyInfo.targets

WARNING:  DO NOT MODIFY this file unless you are knowledgeable about MSBuild and have
          created a backup copy.  Incorrect changes to this file will make it
          impossible to load or build your projects from the command-line or the IDE.

Copyright (c) .NET Foundation. All rights reserved.
***********************************************************************************************
-->
  <PropertyGroup Condition=" '$(Version)' == '' " xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <VersionPrefix Condition=" '$(VersionPrefix)' == '' ">1.0.0</VersionPrefix>
    <Version Condition=" '$(VersionSuffix)' != '' ">$(VersionPrefix)-$(VersionSuffix)</Version>
    <Version Condition=" '$(Version)' == '' ">$(VersionPrefix)</Version>
  </PropertyGroup>
  <PropertyGroup xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <Authors Condition=" '$(Authors)'=='' ">$(AssemblyName)</Authors>
    <Company Condition=" '$(Company)'=='' ">$(Authors)</Company>
    <AssemblyTitle Condition=" '$(AssemblyTitle)' == '' ">$(AssemblyName)</AssemblyTitle>
    <Product Condition=" '$(Product)' == ''">$(AssemblyName)</Product>
  </PropertyGroup>
  <!--
============================================================================================================================================
  </Import>

C:\Program Files\dotnet\sdk\8.0.110\Sdks\Microsoft.NET.Sdk\targets\Microsoft.NET.Sdk.BeforeCommonCrossTargeting.targets
============================================================================================================================================
-->
  <!--
============================================================================================================================================
  <Import Project="$(MSBuildThisFileDirectory)Microsoft.NET.DefaultOutputPaths.targets">

C:\Program Files\dotnet\sdk\8.0.110\Sdks\Microsoft.NET.Sdk\targets\Microsoft.NET.DefaultOutputPaths.targets
============================================================================================================================================
-->
  <!--
***********************************************************************************************
Microsoft.NET.DefaultOutputPaths.targets

WARNING:  DO NOT MODIFY this file unless you are knowledgeable about MSBuild and have
          created a backup copy.  Incorrect changes to this file will make it
          impossible to load or build your projects from the command-line or the IDE.

Copyright (c) .NET Foundation. All rights reserved.
***********************************************************************************************
-->
  <!--
    Note that common targets only set a default OutputPath if neither configuration nor
    platform were set by the user. This was used to validate that a valid configuration is passed,
    assuming the convention maintained by VS that every Configuration|Platform combination had
    an explicit OutputPath. Since we now want to support leaner project files with less
    duplication and more automatic defaults, we always set a default OutputPath.
   -->
  <!-- Projects which don't use Microsoft.NET.Sdk will typically define the OutputPath directly (usually in a
       Configuration-specific PropertyGroup), so in that case we won't append to it by default. -->
  <PropertyGroup Condition="'$(UsingNETSdkDefaults)' == 'true'" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <AppendTargetFrameworkToOutputPath Condition="'$(AppendTargetFrameworkToOutputPath)' == ''">true</AppendTargetFrameworkToOutputPath>
    <AppendPlatformToOutputPath Condition="'$(AppendPlatformToOutputPath)' == '' and '$(PlatformName)' == 'AnyCPU'">false</AppendPlatformToOutputPath>
    <AppendPlatformToOutputPath Condition="'$(AppendPlatformToOutputPath)' == '' and '$(PlatformName)' != 'AnyCPU'">true</AppendPlatformToOutputPath>
    <_PlatformToAppendToOutputPath Condition="'$(AppendPlatformToOutputPath)' == 'true'">$(PlatformName)\</_PlatformToAppendToOutputPath>
  </PropertyGroup>
  <!-- NOTE: If we want to default UseArtifactsOutput to true when targeting a given version of .NET or higher, this is where we would do it.

    It would look something like this:

  <PropertyGroup Condition="'$(UseArtifactsOutput)' == '' and
                            '$(TargetFrameworks)' == '' and
                            '$(TargetFrameworkIdentifier)' == '.NETCoreApp' and
                            $([MSBuild]::VersionGreaterThanOrEquals($(TargetFrameworkVersion), 8.0))">
    <UseArtifactsOutput>true</UseArtifactsOutput>
  </PropertyGroup>
  -->
  <!-- Import .props file to set ArtifactsPath if it wasn't already imported from Sdk.props (this is for the case when artifacts
       properties are set in the project file instead of Directory.Build.props -->
  <!--
============================================================================================================================================
  <Import Project="$(MSBuildThisFileDirectory)Microsoft.NET.DefaultArtifactsPath.props" Condition="'$(_DefaultArtifactsPathPropsImported)' != 'true'">

C:\Program Files\dotnet\sdk\8.0.110\Sdks\Microsoft.NET.Sdk\targets\Microsoft.NET.DefaultArtifactsPath.props
============================================================================================================================================
-->
  <!--
***********************************************************************************************
Microsoft.NET.DefaultArtifactsPath.props

WARNING:  DO NOT MODIFY this file unless you are knowledgeable about MSBuild and have
          created a backup copy.  Incorrect changes to this file will make it
          impossible to load or build your projects from the command-line or the IDE.

Copyright (c) .NET Foundation. All rights reserved.
***********************************************************************************************
-->
  <!-- This .props file may be imported either from Sdk.props or from Microsoft.NET.DefaultOutputPaths.targets, depending
       on whether artifacts output properties were set in Directory.Build.props or not.

       Set a property to indicate it was imported, so we can avoid a duplicate import. -->
  <PropertyGroup>
    <_DefaultArtifactsPathPropsImported>true</_DefaultArtifactsPathPropsImported>
  </PropertyGroup>
  <!-- Setting ArtifactsPath automatically opts in to the artifacts output format -->
  <PropertyGroup Condition="'$(ArtifactsPath)' != '' And '$(UsingMicrosoftArtifactsSdk)' != 'true'">
    <UseArtifactsOutput Condition="'$(UseArtifactsOutput)' == ''">true</UseArtifactsOutput>
    <IncludeProjectNameInArtifactsPaths Condition="'$(IncludeProjectNameInArtifactsPaths)' == ''">true</IncludeProjectNameInArtifactsPaths>
    <_ArtifactsPathLocationType>ExplicitlySpecified</_ArtifactsPathLocationType>
  </PropertyGroup>
  <!-- Set up base output folders if UseArtifactsOutput is set -->
  <PropertyGroup Condition="'$(UseArtifactsOutput)' == 'true' And '$(ArtifactsPath)' == '' And '$(_DirectoryBuildPropsBasePath)' != ''">
    <!-- Default ArtifactsPath to be in the directory where Directory.Build.props is found
         Note that we do not append a backslash to the ArtifactsPath as we do with most paths, because it may be a global property passed in on the command-line which we can't easily change -->
    <ArtifactsPath>$(_DirectoryBuildPropsBasePath)\artifacts</ArtifactsPath>
    <IncludeProjectNameInArtifactsPaths Condition="'$(IncludeProjectNameInArtifactsPaths)' == ''">true</IncludeProjectNameInArtifactsPaths>
    <_ArtifactsPathLocationType>DirectoryBuildPropsFolder</_ArtifactsPathLocationType>
  </PropertyGroup>
  <PropertyGroup Condition="'$(UseArtifactsOutput)' == 'true' And '$(ArtifactsPath)' == ''">
    <!-- If there was no Directory.Build.props file, then put the artifacts path in the project folder -->
    <ArtifactsPath>$(MSBuildProjectDirectory)\artifacts</ArtifactsPath>
    <_ArtifactsPathLocationType>ProjectFolder</_ArtifactsPathLocationType>
  </PropertyGroup>
  <!--
============================================================================================================================================
  </Import>

C:\Program Files\dotnet\sdk\8.0.110\Sdks\Microsoft.NET.Sdk\targets\Microsoft.NET.DefaultOutputPaths.targets
============================================================================================================================================
-->
  <PropertyGroup Condition="'$(UseArtifactsOutput)' == 'true'" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <ArtifactsProjectName Condition="'$(ArtifactsProjectName)' == ''">$(MSBuildProjectName)</ArtifactsProjectName>
    <ArtifactsBinOutputName Condition="'$(ArtifactsBinOutputName)' == ''">bin</ArtifactsBinOutputName>
    <ArtifactsPublishOutputName Condition="'$(ArtifactsPublishOutputName)' == ''">publish</ArtifactsPublishOutputName>
    <ArtifactsPackageOutputName Condition="'$(ArtifactsPackageOutputName)' == ''">package</ArtifactsPackageOutputName>
  </PropertyGroup>
  <PropertyGroup Condition="'$(UseArtifactsOutput)' == 'true' And '$(ArtifactsPivots)' == ''" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <ArtifactsPivots>$(Configuration.ToLowerInvariant())</ArtifactsPivots>
    <!-- Include the TargetFramework in the pivots if the project is multi-targeted (ie TargetFrameworks) is defined -->
    <ArtifactsPivots Condition="'$(TargetFrameworks)' != '' And '$(TargetFramework)' != ''">$(ArtifactsPivots)_$(TargetFramework.ToLowerInvariant())</ArtifactsPivots>
    <!-- This targets file is evaluated before RuntimeIdentifierInference.targets, so this will only include the
         RuntimeIdentifier in the path if it was explicitly specified, not if it was inferred.  This is the
         behavior we want.

         The BlazorWebAssembly .props file sets the RuntimeIdentifier to browser-wasm, so treat that as a special case.
         -->
    <ArtifactsPivots Condition="'$(RuntimeIdentifier)' != '' And !('$(RuntimeIdentifier)' == 'browser-wasm' And '$(AppendRuntimeIdentifierToOutputPath)' == 'false')">$(ArtifactsPivots)_$(RuntimeIdentifier.ToLowerInvariant())</ArtifactsPivots>
  </PropertyGroup>
  <PropertyGroup Condition="'$(UseArtifactsOutput)' == 'true' And '$(IncludeProjectNameInArtifactsPaths)' == 'true'" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <!-- Set artifacts paths when project name should be included in the path -->
    <BaseOutputPath Condition="'$(BaseOutputPath)' == ''">$(ArtifactsPath)\$(ArtifactsBinOutputName)\$(ArtifactsProjectName)\</BaseOutputPath>
    <BaseIntermediateOutputPath Condition="'$(BaseIntermediateOutputPath)' == '' And '$(UseArtifactsIntermediateOutput)' == 'true'">$(ArtifactsPath)\obj\$(ArtifactsProjectName)\</BaseIntermediateOutputPath>
    <PublishDir Condition="'$(PublishDir)' == ''">$(ArtifactsPath)\$(ArtifactsPublishOutputName)\$(ArtifactsProjectName)\$(ArtifactsPivots)\</PublishDir>
  </PropertyGroup>
  <PropertyGroup Condition="'$(UseArtifactsOutput)' == 'true' And '$(IncludeProjectNameInArtifactsPaths)' != 'true'" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <!-- Set artifacts paths when project name should not be included in the path -->
    <BaseOutputPath Condition="'$(BaseOutputPath)' == ''">$(ArtifactsPath)\$(ArtifactsBinOutputName)\</BaseOutputPath>
    <BaseIntermediateOutputPath Condition="'$(BaseIntermediateOutputPath)' == '' And '$(UseArtifactsIntermediateOutput)' == 'true'">$(ArtifactsPath)\obj\</BaseIntermediateOutputPath>
    <PublishDir Condition="'$(PublishDir)' == ''">$(ArtifactsPath)\$(ArtifactsPublishOutputName)\$(ArtifactsPivots)\</PublishDir>
  </PropertyGroup>
  <PropertyGroup Condition="'$(UseArtifactsOutput)' == 'true'" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <OutputPath Condition="'$(OutputPath)' == ''">$(BaseOutputPath)$(ArtifactsPivots)\</OutputPath>
    <IntermediateOutputPath Condition=" $(IntermediateOutputPath) == '' And '$(UseArtifactsIntermediateOutput)' == 'true'">$(BaseIntermediateOutputPath)$(ArtifactsPivots)\</IntermediateOutputPath>
    <!-- The package output path does not include the project name, and only includes the Configuration as a pivot -->
    <PackageOutputPath Condition="'$(PackageOutputPath)' == ''">$(ArtifactsPath)\$(ArtifactsPackageOutputName)\$(Configuration.ToLowerInvariant())\</PackageOutputPath>
  </PropertyGroup>
  <PropertyGroup Condition="'$(UseArtifactsOutput)' != 'true'" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <BaseOutputPath Condition="'$(BaseOutputPath)' == ''">bin\</BaseOutputPath>
    <BaseOutputPath Condition="!HasTrailingSlash('$(BaseOutputPath)')">$(BaseOutputPath)\</BaseOutputPath>
    <OutputPath Condition="'$(OutputPath)' == ''">$(BaseOutputPath)$(_PlatformToAppendToOutputPath)$(Configuration)\</OutputPath>
    <OutputPath Condition="!HasTrailingSlash('$(OutputPath)')">$(OutputPath)\</OutputPath>
  </PropertyGroup>
  <!-- If "UseArtifactsOutput" wasn't set when the MSBuild project extensions .props files were imported, then use "obj" in the project folder for the intermediate output path
         instead a folder under ArtifactsPath.  To have the intermediate output path in the artifacts folder, "UseArtifactsOutput" should be set in Directory.Build.props-->
  <PropertyGroup Condition="'$(UseArtifactsIntermediateOutput)' != 'true'" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <BaseIntermediateOutputPath Condition="'$(BaseIntermediateOutputPath)' == ''">obj\</BaseIntermediateOutputPath>
    <BaseIntermediateOutputPath Condition="!HasTrailingSlash('$(BaseIntermediateOutputPath)')">$(BaseIntermediateOutputPath)\</BaseIntermediateOutputPath>
    <IntermediateOutputPath Condition=" $(IntermediateOutputPath) == '' ">$(BaseIntermediateOutputPath)$(_PlatformToAppendToOutputPath)$(Configuration)\</IntermediateOutputPath>
    <IntermediateOutputPath Condition="!HasTrailingSlash('$(IntermediateOutputPath)')">$(IntermediateOutputPath)\</IntermediateOutputPath>
  </PropertyGroup>
  <!-- Set the package output path (for nuget pack target) now, before the TargetFramework is appended -->
  <PropertyGroup Condition="'$(PackageOutputPath)' == ''" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <PackageOutputPath Condition="'$(UseArtifactsOutput)' != 'true'">$(OutputPath)</PackageOutputPath>
  </PropertyGroup>
  <!-- Exclude files from OutputPath and IntermediateOutputPath from default item globs.  Use the value
       of these properties before the TargetFramework is appended, so that if these values are specified
       in the project file, the specified value will be used for the exclude. -->
  <PropertyGroup Condition="'$(UseArtifactsOutput)' != 'true'" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <DefaultItemExcludes>$(DefaultItemExcludes);$(OutputPath)/**</DefaultItemExcludes>
    <DefaultItemExcludes>$(DefaultItemExcludes);$(IntermediateOutputPath)/**</DefaultItemExcludes>
  </PropertyGroup>
  <PropertyGroup Condition="'$(UseArtifactsOutput)' == 'true'" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <DefaultItemExcludes>$(DefaultItemExcludes);$(ArtifactsPath)/**</DefaultItemExcludes>
    <!-- Exclude bin and obj folders to avoid issues with projects that switch to using artifacts output format -->
    <DefaultItemExcludes>$(DefaultItemExcludes);bin/**;obj/**</DefaultItemExcludes>
  </PropertyGroup>
  <!--
    Append $(TargetFramework) directory to output and intermediate paths to prevent bin clashes between
    targets.
   -->
  <PropertyGroup Condition="'$(UseArtifactsOutput)' != 'true' and&#xD;&#xA;                            '$(AppendTargetFrameworkToOutputPath)' == 'true' and '$(TargetFramework)' != '' and '$(_UnsupportedTargetFrameworkError)' != 'true'" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <OutputPath>$(OutputPath)$(TargetFramework.ToLowerInvariant())\</OutputPath>
  </PropertyGroup>
  <PropertyGroup Condition="'$(UseArtifactsOutput)' != 'true' and&#xD;&#xA;                            '$(AppendTargetFrameworkToOutputPath)' == 'true' and '$(TargetFramework)' != '' and '$(_UnsupportedTargetFrameworkError)' != 'true'" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <IntermediateOutputPath>$(IntermediateOutputPath)$(TargetFramework.ToLowerInvariant())\</IntermediateOutputPath>
  </PropertyGroup>
  <Target Name="_CheckForUnsupportedArtifactsPath" BeforeTargets="_CheckForInvalidConfigurationAndPlatform" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <!-- Generate an error if ArtifactsPath or UseArtifactsOutput are set in the project file.

         We generate an error because if they are set in the project file, it is too late to change the intermediate output path,
         and because it would be confusing to set the property in the project file and have the artifacts path depend on whether
         there happened to be a Directory.Build.props file defined.
    -->
    <NetSdkError Condition="'$(UseArtifactsOutput)' == 'true' and '$(_ArtifactsPathSetEarly)' != 'true'" ResourceName="ArtifactsPathCannotBeSetInProject" />
    <NetSdkError Condition="'$(_ArtifactsPathLocationType)' == 'ProjectFolder'" ResourceName="UseArtifactsOutputRequiresDirectoryBuildProps" />
  </Target>
  <!--
============================================================================================================================================
  </Import>

C:\Program Files\dotnet\sdk\8.0.110\Sdks\Microsoft.NET.Sdk\targets\Microsoft.NET.Sdk.BeforeCommonCrossTargeting.targets
============================================================================================================================================
-->
  <!--
============================================================================================================================================
  <Import Project="$(MSBuildThisFileDirectory)Microsoft.NET.Sdk.Workloads.CrossTargeting.targets">

C:\Program Files\dotnet\sdk\8.0.110\Sdks\Microsoft.NET.Sdk\targets\Microsoft.NET.Sdk.Workloads.CrossTargeting.targets
============================================================================================================================================
-->
  <!--
***********************************************************************************************
Microsoft.NET.Sdk.Workloads.CrossTargeting.targets

WARNING:  DO NOT MODIFY this file unless you are knowledgeable about MSBuild and have
          created a backup copy.  Incorrect changes to this file will make it
          impossible to load or build your projects from the command-line or the IDE.

Copyright (c) .NET Foundation. All rights reserved.
***********************************************************************************************
-->
  <!--
============================================================
                                     _GetRequiredWorkloads

 Collecting all Workload requirement for each inner build.
============================================================
-->
  <Target Name="_GetRequiredWorkloads" DependsOnTargets="_ComputeTargetFrameworkItems" Returns="@(_ResolvedSuggestedWorkload)">
    <MSBuild Projects="@(_InnerBuildProjects)" Condition="'@(_InnerBuildProjects)' != '' " Targets="_GetRequiredWorkloads" BuildInParallel="$(BuildInParallel)">
      <Output ItemName="_ResolvedSuggestedWorkload" TaskParameter="TargetOutputs" />
    </MSBuild>
  </Target>
  <!--
============================================================================================================================================
  </Import>

C:\Program Files\dotnet\sdk\8.0.110\Sdks\Microsoft.NET.Sdk\targets\Microsoft.NET.Sdk.BeforeCommonCrossTargeting.targets
============================================================================================================================================
-->
  <!--
============================================================================================================================================
  </Import>

C:\Program Files\dotnet\sdk\8.0.110\Sdks\Microsoft.NET.Sdk\Sdk\Sdk.targets
============================================================================================================================================
-->
  <!--<Import Project="$(MSBuildThisFileDirectory)..\targets\Microsoft.NET.Sdk.BeforeCommon.targets" Condition="'$(IsCrossTargetingBuild)' != 'true'" />-->
  <PropertyGroup Condition="'$(LanguageTargets)' == ''" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <LanguageTargets Condition="'$(MSBuildProjectExtension)' == '.csproj'">$(MSBuildToolsPath)\Microsoft.CSharp.targets</LanguageTargets>
    <LanguageTargets Condition="'$(MSBuildProjectExtension)' == '.vbproj'">$(MSBuildToolsPath)\Microsoft.VisualBasic.targets</LanguageTargets>
    <LanguageTargets Condition="'$(MSBuildProjectExtension)' == '.fsproj'">$(MSBuildThisFileDirectory)..\targets\Microsoft.NET.Sdk.FSharpTargetsShim.targets</LanguageTargets>
    <!-- If LanguageTargets isn't otherwise set, then just import the common targets.  This should allow the restore target to run,
         which could bring in NuGet packages that set the LanguageTargets to something else.  This means support for different
         languages could either be supplied via an SDK or via a NuGet package. -->
    <LanguageTargets Condition="'$(LanguageTargets)' == ''">$(MSBuildToolsPath)\Microsoft.Common.targets</LanguageTargets>
  </PropertyGroup>
  <!-- REMARK: Dont remove/rename, the LanguageTargets property is used by F# to hook inside the project's sdk
               using Sdk attribute (from .NET Core Sdk 1.0.0-preview4) -->
  <!--
============================================================================================================================================
  <Import Project="$(LanguageTargets)">

C:\Program Files\dotnet\sdk\8.0.110\Microsoft.CSharp.targets
============================================================================================================================================
-->
  <!--
***********************************************************************************************
Microsoft.CSharp.targets

WARNING:  DO NOT MODIFY this file unless you are knowledgeable about MSBuild and have
          created a backup copy.  Incorrect changes to this file will make it
          impossible to load or build your projects from the command-line or the IDE.

This file defines the steps in the standard build process specific for C# .NET projects.
For example, it contains the step that actually calls the C# compiler.  The remainder
of the build process is defined in Microsoft.Common.targets, which is imported by
this file.

Copyright (C) Microsoft Corporation. All rights reserved.
***********************************************************************************************
-->
  <Choose>
    <When Condition="'$(IsCrossTargetingBuild)' == 'true'">
      <PropertyGroup>
        <CSharpTargetsPath>$(MSBuildToolsPath)\Microsoft.CSharp.CrossTargeting.targets</CSharpTargetsPath>
      </PropertyGroup>
    </When>
    <Otherwise>
      <PropertyGroup>
        <CSharpTargetsPath>$(MSBuildToolsPath)\Microsoft.CSharp.CurrentVersion.targets</CSharpTargetsPath>
      </PropertyGroup>
    </Otherwise>
  </Choose>
  <!--
============================================================================================================================================
  <Import Project="$(MSBuildToolsPath)\Microsoft.Managed.Before.targets">

C:\Program Files\dotnet\sdk\8.0.110\Microsoft.Managed.Before.targets
============================================================================================================================================
-->
  <!--
***********************************************************************************************
WARNING:  DO NOT MODIFY this file unless you are knowledgeable about MSBuild and have
          created a backup copy.  Incorrect changes to this file will make it
          impossible to load or build your projects from the command-line or the IDE.

This file defines common build logic for all managed languaged: C#, VisualBasic, F#
It is imported before the common targets have been imported.

Copyright (C) Microsoft Corporation. All rights reserved.
***********************************************************************************************
-->
  <!--
      We are doing a cross-targeting build if there is a non-empty list of target frameworks specified
      and there is no current target framework being built individually. In that case, a multitargeting
      project file like Microsoft.<language>.CrossTargeting.targets gets imported.
  -->
  <PropertyGroup Condition="'$(TargetFrameworks)' != '' and '$(TargetFramework)' == ''">
    <IsCrossTargetingBuild>true</IsCrossTargetingBuild>
  </PropertyGroup>
  <!--
============================================================================================================================================
  </Import>

C:\Program Files\dotnet\sdk\8.0.110\Microsoft.CSharp.targets
============================================================================================================================================
-->
  <!--
============================================================================================================================================
  <Import Project="$(CSharpTargetsPath)">

C:\Program Files\dotnet\sdk\8.0.110\Microsoft.CSharp.CrossTargeting.targets
============================================================================================================================================
-->
  <!--
***********************************************************************************************
Microsoft.CSharp.CrossTargeting.targets

WARNING:  DO NOT MODIFY this file unless you are knowledgeable about MSBuild and have
          created a backup copy.  Incorrect changes to this file will make it
          impossible to load or build your projects from the command-line or the IDE.

Copyright (C) Microsoft Corporation. All rights reserved.
***********************************************************************************************
-->
  <!-- Import design time targets for Roslyn Project System. These are only available if Visual Studio is installed. -->
  <!-- Import design time targets before the common crosstargeting targets, which import targets from Nuget. -->
  <PropertyGroup>
    <CSharpDesignTimeTargetsPath Condition="'$(CSharpDesignTimeTargetsPath)'==''">$(MSBuildExtensionsPath)\Microsoft\VisualStudio\Managed\Microsoft.CSharp.DesignTime.targets</CSharpDesignTimeTargetsPath>
  </PropertyGroup>
  <!--<Import Project="$(CSharpDesignTimeTargetsPath)" Condition="'$(CSharpDesignTimeTargetsPath)' != '' and Exists('$(CSharpDesignTimeTargetsPath)')" />-->
  <!--
============================================================================================================================================
  <Import Project="Microsoft.Common.CrossTargeting.targets">

C:\Program Files\dotnet\sdk\8.0.110\Microsoft.Common.CrossTargeting.targets
============================================================================================================================================
-->
  <!--
***********************************************************************************************
Microsoft.Common.CrossTargeting.targets

WARNING:  DO NOT MODIFY this file unless you are knowledgeable about MSBuild and have
          created a backup copy.  Incorrect changes to this file will make it
          impossible to load or build your projects from the command-line or the IDE.

Copyright (C) Microsoft Corporation. All rights reserved.
***********************************************************************************************
-->
  <PropertyGroup>
    <BuildInParallel Condition="'$(BuildInParallel)' == ''">true</BuildInParallel>
    <ImportByWildcardBeforeMicrosoftCommonCrossTargetingTargets Condition="'$(ImportByWildcardBeforeMicrosoftCommonCrossTargetingTargets)' == ''">true</ImportByWildcardBeforeMicrosoftCommonCrossTargetingTargets>
  </PropertyGroup>
  <!--<Import Project="$(MSBuildExtensionsPath)\$(MSBuildToolsVersion)\Microsoft.Common.CrossTargeting.targets\ImportBefore\*.targets" Condition="'$(ImportByWildcardBeforeMicrosoftCommonCrossTargetingTargets)' == 'true' and exists('$(MSBuildExtensionsPath)\$(MSBuildToolsVersion)\Microsoft.Common.CrossTargeting.targets\ImportBefore')" />-->
  <!--
============================================================================================================================================
  <Import Project="$(CustomBeforeMicrosoftCommonCrossTargetingTargets)" Condition="'$(CustomBeforeMicrosoftCommonCrossTargetingTargets)' != '' and Exists('$(CustomBeforeMicrosoftCommonCrossTargetingTargets)')">

C:\.tools\.nuget\packages\microsoft.dotnet.arcade.sdk\8.0.0-beta.24426.2\tools\BeforeCommonTargets.CrossTargeting.targets
============================================================================================================================================
-->
  <!-- Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. -->
  <!--<Import Project="$(_ArcadeOverriddenCustomBeforeMicrosoftCommonCrossTargetingTargets)" Condition="Exists('$(_ArcadeOverriddenCustomBeforeMicrosoftCommonCrossTargetingTargets)')" />-->
  <PropertyGroup>
    <_ArcadeBeforeCommonTargetsImported>true</_ArcadeBeforeCommonTargetsImported>
  </PropertyGroup>
  <!--
============================================================================================================================================
  <Import Project="Version.BeforeCommonTargets.targets">

C:\.tools\.nuget\packages\microsoft.dotnet.arcade.sdk\8.0.0-beta.24426.2\tools\Version.BeforeCommonTargets.targets
============================================================================================================================================
-->
  <!-- Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. -->
  <!--
    Compute the IsShipping* properties

    Unless specified otherwise project is assumed to produce artifacts (assembly, package, vsix, etc.) that ship.
      Test projects automatically set IsShipping to false.

      Some projects may produce packages that contain shipping assemblies but the packages themselves do not ship.
      Thes projects shall specify IsShippingPackage=false and leave IsShipping unset (will default to true).

      Targets that need to determine whether an artifact is shipping shall use the artifact specific IsShippingXxx property,
      if available for the kind of artifact they operate on.
  -->
  <PropertyGroup>
    <IsShipping Condition="'$(IsShipping)' == ''">true</IsShipping>
    <IsShippingAssembly Condition="'$(IsShippingAssembly)' == ''">$(IsShipping)</IsShippingAssembly>
    <IsShippingPackage Condition="'$(IsVisualStudioBuildPackage)' == 'true'">false</IsShippingPackage>
    <IsShippingPackage Condition="'$(IsShippingPackage)' == ''">$(IsShipping)</IsShippingPackage>
    <IsShippingVsix Condition="'$(IsShippingVsix)' == ''">$(IsShipping)</IsShippingVsix>
  </PropertyGroup>
  <!--
    Specification: https://github.com/dotnet/arcade/blob/master/Documentation/CorePackages/Versioning.md

    Workaround for https://github.com/dotnet/sdk/issues/3173:
    The following must be evaluated after the project file is imported but before Microsoft.NET.DefaultAssemblyInfo.targets from .NET Core SDK is imported.
    The project may set VersionPrefix, MajorVersion, MinorVersion, or AutoGenerateAssemblyVersion properties, which are consumed below.
    Microsoft.NET.DefaultAssemblyInfo.targets consumes VersionPrefix property, which may be set below.
  -->
  <!--
    Version numbers calculated here are date-based. In official builds this is given by OfficialBuildId parameter,
    but other builds do not have such input and would therefore be non-deterministic.
  -->
  <PropertyGroup Condition="'$(OfficialBuild)' == 'true' or '$(DotNetUseShippingVersions)' == 'true'">
    <!--
      Building MSIs from dev build requires file versions to be increasing.
      Use the current date in non-official builds. Note that this reduces the deterministic properties of the build
      and should only be enabled when it's necessary to test-install the MSIs produced by the build.
    -->
    <_BuildNumber>$(OfficialBuildId)</_BuildNumber>
    <_BuildNumber Condition="'$(OfficialBuildId)' == ''">$([System.DateTime]::Now.ToString(yyyyMMdd)).1</_BuildNumber>
    <!--
      OfficialBuildId is assumed to have format "20yymmdd.r" (the assumption is checked later in a target).
    -->
    <_BuildNumberYY>$(_BuildNumber.Substring(2, 2))</_BuildNumberYY>
    <_BuildNumberMM>$(_BuildNumber.Substring(4, 2))</_BuildNumberMM>
    <_BuildNumberDD>$(_BuildNumber.Substring(6, 2))</_BuildNumberDD>
    <_BuildNumberR>$(_BuildNumber.Substring(9))</_BuildNumberR>
    <!-- SHORT_DATE := yy * 1000 + mm * 50 + dd -->
    <VersionSuffixDateStamp>$([MSBuild]::Add($([MSBuild]::Add($([MSBuild]::Multiply($(_BuildNumberYY), 1000)), $([MSBuild]::Multiply($(_BuildNumberMM), 50)))), $(_BuildNumberDD)))</VersionSuffixDateStamp>
    <!-- REVISION := r -->
    <VersionSuffixBuildOfTheDay>$(_BuildNumberR)</VersionSuffixBuildOfTheDay>
    <VersionSuffixBuildOfTheDayPadded>$(VersionSuffixBuildOfTheDay.PadLeft(2, $([System.Convert]::ToChar(`0`))))</VersionSuffixBuildOfTheDayPadded>
    <!-- PATCH_NUMBER := (SHORT_DATE - VersionBaseShortDate) * 100 + r -->
    <_PatchNumber>$([MSBuild]::Add($([MSBuild]::Multiply($([MSBuild]::Subtract($(VersionSuffixDateStamp), $([MSBuild]::ValueOrDefault($(VersionBaseShortDate), 19000)))), 100)), $(_BuildNumberR)))</_PatchNumber>
  </PropertyGroup>
  <!--
    Calculate VersionPrefix.
  -->
  <!--
    The project can specify version either directly using the .NET SDK recognized property VersionPrefix, or using MajorVersion and MinorVersion properties.
    Note that .NET Core SDK sets VersionPrefix to 1.0.0 if not set by the project. Override it here if the project sets MajorVersion, MinorVersion, and optionally a PatchVersion.
  -->
  <PropertyGroup>
    <VersionPrefix Condition="'$(MajorVersion)' != '' and '$(MinorVersion)' != ''">$(MajorVersion).$(MinorVersion).$([MSBuild]::ValueOrDefault('$(PatchVersion)', '0'))</VersionPrefix>
    <_OriginalVersionPrefix>$(VersionPrefix)</_OriginalVersionPrefix>
  </PropertyGroup>
  <!--
    If a package is designated to be a release-only package (PreReleaseVersionLabel is empty) its package version does
    not include any pre-release labels in official build. The 3rd component of the version prefix is overwritten either
    by PATCH_NUMBER or '0' in non-official builds.
  -->
  <PropertyGroup Condition="'$(PreReleaseVersionLabel)' == ''">
    <_VersionPrefixMajor>$(VersionPrefix.Split('.')[0])</_VersionPrefixMajor>
    <_VersionPrefixMinor>$(VersionPrefix.Split('.')[1])</_VersionPrefixMinor>
    <VersionPrefix>$(_VersionPrefixMajor).$(_VersionPrefixMinor).$([MSBuild]::ValueOrDefault($(_PatchNumber), '0'))</VersionPrefix>
    <VersionSuffix />
  </PropertyGroup>
  <!--
    Calculate VersionSuffix.
  -->
  <PropertyGroup Condition="'$(PreReleaseVersionLabel)' != '' or '$(VersionSuffixDateStamp)' == ''">
    <!--
      Traditionally, .NET Core has used prerelease labels like:
      - preview1
      - beta1,
      - preview9

      For previews, this presents a problem if we decide to release more than 9 previews, as preview10 sorts
      after preview9. This could be dealt with by using preview01, preview02, etc. but this is harder to read.
      Instead, repos should use preview.1, preview.2, etc. if using SemVer2. NuGet will properly preference preview.10
      over preview.9.

      If PreReleaseVersionIteration is set and SemanticVersioningV1 is not set to true, then the prerelease version
      number is appended with a '.' to PreReleaseVersionLabel to create the final prerelease label.
    -->
    <_PreReleaseLabel>$(PreReleaseVersionLabel)</_PreReleaseLabel>
    <_PreReleaseLabel Condition="'$(SemanticVersioningV1)' != 'true' and '$(PreReleaseVersionIteration)' != ''">$(PreReleaseVersionLabel).$(PreReleaseVersionIteration)</_PreReleaseLabel>
    <_PreReleaseLabel Condition="'$(SemanticVersioningV1)' == 'true'">$(PreReleaseVersionLabel)$(PreReleaseVersionIteration)</_PreReleaseLabel>
    <_PreReleaseLabel Condition="'$(ContinuousIntegrationBuild)' == 'true' and '$(OfficialBuild)' != 'true'">ci</_PreReleaseLabel>
    <_PreReleaseLabel Condition="'$(ContinuousIntegrationBuild)' != 'true'">dev</_PreReleaseLabel>
    <_BuildNumberLabels Condition="'$(VersionSuffixDateStamp)' != '' and '$(SemanticVersioningV1)' != 'true'">.$(VersionSuffixDateStamp).$(VersionSuffixBuildOfTheDay)</_BuildNumberLabels>
    <_BuildNumberLabels Condition="'$(VersionSuffixDateStamp)' != '' and '$(SemanticVersioningV1)' == 'true'">-$(VersionSuffixDateStamp)-$(VersionSuffixBuildOfTheDayPadded)</_BuildNumberLabels>
    <!--
      If DotNetFinalVersionKind is specified, overrides the package version produced by the build like so:
        ""           1.2.3-beta.12345.67
        "prerelease" 1.2.3-beta
        "release"    1.2.3
    -->
    <VersionSuffix Condition="'$(DotNetFinalVersionKind)' == 'release'" />
    <VersionSuffix Condition="'$(DotNetFinalVersionKind)' == 'prerelease' and '$(SemanticVersioningV1)' != 'true'">$(_PreReleaseLabel).final</VersionSuffix>
    <VersionSuffix Condition="'$(DotNetFinalVersionKind)' == 'prerelease' and '$(SemanticVersioningV1)' == 'true'">$(_PreReleaseLabel)-final</VersionSuffix>
    <VersionSuffix Condition="'$(DotNetFinalVersionKind)' == ''">$(_PreReleaseLabel)$(_BuildNumberLabels)</VersionSuffix>
    <!--
      Some projects want to remain producing prerelease packages even if we are doing a final stable build because
      they don't ship or aren't ready to ship stable. Those projects can set SuppressFinalPackageVersion property to true.

      TODO: BlockStable is obsolete. Remove once repos update. https://github.com/dotnet/arcade/issues/1213
    -->
    <VersionSuffix Condition="'$(BlockStable)' == 'true' or '$(SuppressFinalPackageVersion)' == 'true'">$(_PreReleaseLabel)$(_BuildNumberLabels)</VersionSuffix>
    <!--
      If a project produces non-shipping packages, these packages should always include the build number label
    -->
    <VersionSuffix Condition="'$(IsShippingPackage)' != 'true'">$(_PreReleaseLabel)$(_BuildNumberLabels)</VersionSuffix>
    <!--
      Disable NuGet Pack warning that the version is SemVer 2.0.
      SemVer 2.0 is supported by NuGet since 3.0.0 (July 2015) in some capacity, and fully since 3.5.0 (October 2016).
    -->
    <NoWarn Condition="'$(SemanticVersioningV1)' != 'true'">$(NoWarn);NU5105</NoWarn>
  </PropertyGroup>
  <PropertyGroup Condition="'$(VersionSuffixDateStamp)' == ''">
    <!--
      Don't include a commit SHA to AssemblyInformationalVersion.
      It would reduce the possibility of sharing otherwise unchanged build artifacts across deterministic builds.
    -->
    <IncludeSourceRevisionInInformationalVersion>false</IncludeSourceRevisionInInformationalVersion>
  </PropertyGroup>
  <!--
    Workaround for https://github.com/dotnet/sdk/issues/3173.
    Overwrite the value of Version set in Microsoft.NET.DefaultAssemblyInfo.targets.
  -->
  <PropertyGroup>
    <VersionPrefix Condition="'$(VersionPrefix)' == ''">1.0.0</VersionPrefix>
    <Version>$(VersionPrefix)</Version>
    <Version Condition="'$(VersionSuffix)' != ''">$(Version)-$(VersionSuffix)</Version>
  </PropertyGroup>
  <!--
============================================================================================================================================
  </Import>

C:\.tools\.nuget\packages\microsoft.dotnet.arcade.sdk\8.0.0-beta.24426.2\tools\BeforeCommonTargets.CrossTargeting.targets
============================================================================================================================================
-->
  <!--
============================================================================================================================================
  <Import Project="TargetFrameworkFilters.BeforeCommonTargets.targets">

C:\.tools\.nuget\packages\microsoft.dotnet.arcade.sdk\8.0.0-beta.24426.2\tools\TargetFrameworkFilters.BeforeCommonTargets.targets
============================================================================================================================================
-->
  <!-- Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. -->
  <PropertyGroup>
    <_EnableTargetFrameworkFiltering>false</_EnableTargetFrameworkFiltering>
    <_EnableTargetFrameworkFiltering Condition="'$(NoTargetFrameworkFiltering)' != 'true' and '$(DotNetTargetFrameworkFilter)' != ''">true</_EnableTargetFrameworkFiltering>
  </PropertyGroup>
  <PropertyGroup Condition="$(_EnableTargetFrameworkFiltering)">
    <_OriginalTargetFrameworks Condition="'$(TargetFrameworks)' != ''">$(TargetFrameworks)</_OriginalTargetFrameworks>
    <_OriginalTargetFrameworks Condition="'$(TargetFramework)' != ''">$(TargetFramework)</_OriginalTargetFrameworks>
    <_FilteredTargetFrameworks>$([MSBuild]::Unescape($([MSBuild]::FilterTargetFrameworks('$(_OriginalTargetFrameworks)', '$(DotNetTargetFrameworkFilter)'))))</_FilteredTargetFrameworks>
    <!-- Maintain usage of the original property -->
    <TargetFrameworks Condition="'$(TargetFrameworks)' != ''">$(_FilteredTargetFrameworks)</TargetFrameworks>
    <TargetFramework Condition="'$(TargetFramework)' != ''">$(_FilteredTargetFrameworks)</TargetFramework>
    <!-- If nothing left to build, exclude it! -->
    <ExcludeFromBuild Condition="'$(_FilteredTargetFrameworks)' == ''">true</ExcludeFromBuild>
  </PropertyGroup>
  <!-- Opt-in target to verify that a project doesn't bring in the .NET Standard 1.x dependency graph
       (usually transitively) via old dependencies. -->
  <Target Name="FlagNetStandard1xDependencies" Condition="'$(FlagNetStandard1XDependencies)' == 'true'" AfterTargets="ResolvePackageAssets">
    <ItemGroup>
      <NetStandard1xPackage Include="&#xD;&#xA;        Microsoft.Win32.Primitives;&#xD;&#xA;        System.AppContext;&#xD;&#xA;        System.Collections;&#xD;&#xA;        System.Collections.Concurrent;&#xD;&#xA;        System.Console;&#xD;&#xA;        System.Diagnostics.Debug;&#xD;&#xA;        System.Diagnostics.Tools;&#xD;&#xA;        System.Diagnostics.Tracing;&#xD;&#xA;        System.Globalization;&#xD;&#xA;        System.Globalization.Calendars;&#xD;&#xA;        System.IO;&#xD;&#xA;        System.IO.Compression;&#xD;&#xA;        System.IO.Compression.ZipFile;&#xD;&#xA;        System.IO.FileSystem;&#xD;&#xA;        System.IO.FileSystem.Primitives;&#xD;&#xA;        System.Linq;&#xD;&#xA;        System.Linq.Expressions;&#xD;&#xA;        System.Net.Http;&#xD;&#xA;        System.Net.Primitives;&#xD;&#xA;        System.Net.Sockets;&#xD;&#xA;        System.ObjectModel;&#xD;&#xA;        System.Reflection;&#xD;&#xA;        System.Reflection.Extensions;&#xD;&#xA;        System.Reflection.Primitives;&#xD;&#xA;        System.Resources.ResourceManager;&#xD;&#xA;        System.Runtime;&#xD;&#xA;        System.Runtime.Extensions;&#xD;&#xA;        System.Runtime.Handles;&#xD;&#xA;        System.Runtime.InteropServices;&#xD;&#xA;        System.Runtime.InteropServices.RuntimeInformation;&#xD;&#xA;        System.Runtime.Numerics;&#xD;&#xA;        System.Security.Cryptography.Algorithms;&#xD;&#xA;        System.Security.Cryptography.Encoding;&#xD;&#xA;        System.Security.Cryptography.Primitives;&#xD;&#xA;        System.Security.Cryptography.X509Certificates;&#xD;&#xA;        System.Text.Encoding;&#xD;&#xA;        System.Text.Encoding.Extensions;&#xD;&#xA;        System.Text.RegularExpressions;&#xD;&#xA;        System.Threading;&#xD;&#xA;        System.Threading.Tasks;&#xD;&#xA;        System.Threading.Timer;&#xD;&#xA;        System.Xml.ReaderWriter;&#xD;&#xA;        System.Xml.XDocument" />
    </ItemGroup>
    <ItemGroup>
      <NonFoundNetStandard1xPackage Include="@(PackageDependencies)" Exclude="@(NetStandard1xPackage)" />
      <FoundNetStandard1xPackage Include="@(PackageDependencies)" Exclude="@(NonFoundNetStandard1xPackage)" />
    </ItemGroup>
    <Error Text="The following .NET Standard 1.x packages are referenced and must be removed: %0D%0A- @(FoundNetStandard1xPackage, '%0D%0A- ')%0D%0AConsult the project.assets.json files to find the parent dependencies." Condition="'@(FoundNetStandard1xPackage)' != ''" />
  </Target>
  <!--
============================================================================================================================================
  </Import>

C:\.tools\.nuget\packages\microsoft.dotnet.arcade.sdk\8.0.0-beta.24426.2\tools\BeforeCommonTargets.CrossTargeting.targets
============================================================================================================================================
-->
  <!-- Import the logic to determine whether a project should not build.
       This is done at this point because we need to exclude the import of some standard Microsoft restore targets (NuGet)
       that are loaded before the import of Arcade's Sdk.targets. See info in ExcludeFromBuild -->
  <!--
============================================================================================================================================
  <Import Project="ExcludeFromBuild.BeforeCommonTargets.targets">

C:\.tools\.nuget\packages\microsoft.dotnet.arcade.sdk\8.0.0-beta.24426.2\tools\ExcludeFromBuild.BeforeCommonTargets.targets
============================================================================================================================================
-->
  <!-- Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. -->
  <!--  This file sets properties that enable skipping of a project build if desired.
        Cases where we might skip include:
        - Source-build mode and "ExcludeFromSourceBuild" is set.
        - Non source-build mode and "ExcludeFromBuild" is set
        - Target filtering is used and the target filters set "ExcludeFromBuild".

        To exclude a project from building, Arcade must do two things:
        - Create/override the standard targets (Build, Restore, Sign, etc.) with empty ones.
        - Keep the .NET SDK from importing the standard NuGet restore targets - NuGet uses the
          '_IsProjectRestoreSupported target' to determine whether a project can be restored. If the project shouldn't be built,
          it shouldn't be restored either. This could be done two ways:
            - Override the _IsProjectRestoreSupported target to an empty target, or one that returns false.
            - Avoid import of the _IsProjectRestoreSupported target altogether.
          The first approach is more consistent with the rest of Arcade's approach to skipping a build.
          However is does **not** work with msbuild static graph. Static graph uses the *existence* of the
          target to determine whether a project should be restored, so overriding with an empty target will
          only avoid building a project, but it will still get restored. This could cause issues with target
          framework filtering, or introduce unexpected prebuilts.

          So to achieve the desired affect, Arcade must reset NuGetRestoreTargets to an empty file. Because
          this import is done early, the BeforeCommonTargets hook must be used. There is a case
          where the BeforeCommonTargets hook is not used (see https://github.com/dotnet/arcade/issues/2676).
          In that case, Sdk.targets imports it explicitly. -->
  <!--
    If a project specifies ExcludeFromSourceBuild=true during a source build suppress all targets and emulate a no-op
    (empty common targets like Restore, Build, Pack, etc.).

    It's also possible to set ExcludeFromBuild prior to importing the Sdk.targets
    to skip building as desired in non-source build scenarios. This might be done to
    avoid building tests in certain product build scenarios.
  -->
  <PropertyGroup>
    <_SuppressAllTargets>false</_SuppressAllTargets>
    <_SuppressAllTargets Condition="'$(DotNetBuildFromSource)' == 'true' and '$(ExcludeFromSourceBuild)' == 'true'">true</_SuppressAllTargets>
    <_SuppressAllTargets Condition="'$(ExcludeFromBuild)' == 'true'">true</_SuppressAllTargets>
    <!-- If excluding, then disable a restore warning, which will fire on newer SDKs, as well as set the NuGetRestoreTargets property to empty,
         which will avoid importing the restore targets inside the .NET SDK. If the restore targets exist, then static graph restore will attempt tpo
         execute. -->
    <DisableWarnForInvalidRestoreProjects Condition="'$(_SuppressAllTargets)' == 'true'">true</DisableWarnForInvalidRestoreProjects>
    <NuGetRestoreTargets Condition="'$(_SuppressAllTargets)' == 'true'">$(MSBuildThisFileDirectory)NoRestore.targets</NuGetRestoreTargets>
    <!-- When a project is using the .NET SDK, but with the "UseWpf" property, there will be an attempt to import the windows desktop SDK targets.
         These are not available in certain circumstances, like linux source build. -->
    <ImportWindowsDesktopTargets Condition="'$(_SuppressAllTargets)' == 'true'">false</ImportWindowsDesktopTargets>
  </PropertyGroup>
  <!--
============================================================================================================================================
  </Import>

C:\.tools\.nuget\packages\microsoft.dotnet.arcade.sdk\8.0.0-beta.24426.2\tools\BeforeCommonTargets.CrossTargeting.targets
============================================================================================================================================
-->
  <!--
============================================================================================================================================
  </Import>

C:\Program Files\dotnet\sdk\8.0.110\Microsoft.Common.CrossTargeting.targets
============================================================================================================================================
-->
  <Target Name="GetTargetFrameworks" DependsOnTargets="GetTargetFrameworksWithPlatformFromInnerBuilds" Returns="@(_ThisProjectBuildMetadata)">
    <Error Condition="'$(IsCrossTargetingBuild)' != 'true'" Text="Internal MSBuild error: CrossTargeting GetTargetFrameworks target should only be used in cross targeting (outer) build" />
    <CombineXmlElements RootElementName="AdditionalProjectProperties" XmlElements="@(_TargetFrameworkInfo->'%(AdditionalPropertiesFromProject)')">
      <Output TaskParameter="Result" PropertyName="_AdditionalPropertiesFromProject" />
    </CombineXmlElements>
    <ItemGroup>
      <_ThisProjectBuildMetadata Include="$(MSBuildProjectFullPath)">
        <TargetFrameworks>@(_TargetFrameworkInfo)</TargetFrameworks>
        <TargetFrameworkMonikers>@(_TargetFrameworkInfo->'%(TargetFrameworkMonikers)')</TargetFrameworkMonikers>
        <TargetPlatformMonikers>@(_TargetFrameworkInfo->'%(TargetPlatformMonikers)')</TargetPlatformMonikers>
        <AdditionalPropertiesFromProject>$(_AdditionalPropertiesFromProject)</AdditionalPropertiesFromProject>
        <HasSingleTargetFramework>false</HasSingleTargetFramework>
        <IsRidAgnostic>@(_TargetFrameworkInfo->'%(IsRidAgnostic)')</IsRidAgnostic>
        <!-- Extract necessary information for SetPlatform negotiation -->
        <!-- This target does not run for cpp projects. -->
        <IsVcxOrNativeProj>false</IsVcxOrNativeProj>
        <Platform Condition="$([MSBuild]::AreFeaturesEnabled('17.4'))">$(Platform)</Platform>
        <Platforms>$(Platforms)</Platforms>
      </_ThisProjectBuildMetadata>
    </ItemGroup>
  </Target>
  <Target Name="_ComputeTargetFrameworkItems" Returns="@(InnerOutput)">
    <ItemGroup>
      <_TargetFramework Include="$(TargetFrameworks)" />
      <!-- Make normalization explicit: Trim; Deduplicate by keeping first occurrence, case insensitive -->
      <_TargetFrameworkNormalized Include="@(_TargetFramework-&gt;Trim()-&gt;Distinct())" />
      <_InnerBuildProjects Include="$(MSBuildProjectFile)">
        <AdditionalProperties>TargetFramework=%(_TargetFrameworkNormalized.Identity)</AdditionalProperties>
      </_InnerBuildProjects>
    </ItemGroup>
  </Target>
  <Target Name="GetTargetFrameworksWithPlatformFromInnerBuilds" DependsOnTargets="_ComputeTargetFrameworkItems">
    <MSBuild Projects="@(_InnerBuildProjects)" Condition="'@(_InnerBuildProjects)' != '' " Targets="GetTargetFrameworksWithPlatformForSingleTargetFramework" BuildInParallel="$(BuildInParallel)">
      <Output ItemName="_TargetFrameworkInfo" TaskParameter="TargetOutputs" />
    </MSBuild>
  </Target>
  <!--
    Target that allows targets consuming source control confirmation to establish a dependency on targets producing this information.

    Any target that reads SourceRevisionId, PrivateRepositoryUrl, SourceRoot, and other source control properties and items
    should depend on this target and be conditioned on '$(SourceControlInformationFeatureSupported)' == 'true'.

    SourceRevisionId property uniquely identifies the source control revision of the repository the project belongs to.
    For Git repositories this id is a commit hash, for TFVC repositories it's the changeset number, etc.

    PrivateRepositoryUrl property stores the URL of the repository supplied by the CI server or retrieved from source control manager.
    Targets consuming this property shall not publish its value implicitly as it might inadvertently reveal an internal URL.
    Instead, they shall only do so if the project sets PublishRepositoryUrl property to true. For example, the NuGet Pack target
    may include the repository URL in the nuspec file generated for NuGet package produced by the project if PublishRepositoryUrl is true.

    SourceRoot item group lists all source roots that the project source files reside under and their mapping to source control server URLs,
    if available. This includes both source files under source control as well as source files in source packages. SourceRoot items are
    used by compilers to determine path map in deterministic build and by SourceLink provider, which maps local paths to URLs of source files
    stored on the source control server.

    Source control information provider that sets these properties and items shall execute before this target (by including
    InitializeSourceControlInformation in its BeforeTargets) and set source control properties and items that haven't been initialized yet.
  -->
  <Target Name="InitializeSourceControlInformation" />
  <PropertyGroup>
    <SourceControlInformationFeatureSupported>true</SourceControlInformationFeatureSupported>
  </PropertyGroup>
  <!--
  ============================================================
                                       DispatchToInnerBuilds

     Builds this project with /t:$(InnerTarget) /p:TargetFramework=X for each
     value X in $(TargetFrameworks)

     [IN]
     $(TargetFrameworks) - Semicolon delimited list of target frameworks.
     $(InnerTargets) - The targets to build for each target framework

     [OUT]
     @(InnerOutput) - The combined output items of the inner targets across
                      all target frameworks..
  ============================================================
  -->
  <Target Name="DispatchToInnerBuilds" DependsOnTargets="_ComputeTargetFrameworkItems" Returns="@(InnerOutput)">
    <!-- If this logic is changed, also update Clean -->
    <MSBuild Projects="@(_InnerBuildProjects)" Condition="'@(_InnerBuildProjects)' != '' " Targets="$(InnerTargets)" BuildInParallel="$(BuildInParallel)">
      <Output ItemName="InnerOutput" TaskParameter="TargetOutputs" />
    </MSBuild>
  </Target>
  <!--
  ============================================================
                                       Build

   Cross-targeting version of Build.

   [IN]
   $(TargetFrameworks) - Semicolon delimited list of target frameworks.

   $(InnerTargets)     - The targets to build for each target framework. Defaults
                         to 'Build' if unset, but allows override to support
                         `msbuild /p:InnerTargets=X;Y;Z` which will build X, Y,
                         and Z targets for each target framework.

   [OUT]
   @(InnerOutput) - The combined output items of the inner targets across
                    all builds.
  ============================================================
  -->
  <Target Name="Build" DependsOnTargets="_SetBuildInnerTarget;DispatchToInnerBuilds" />
  <Target Name="_SetBuildInnerTarget" Returns="@(InnerOutput)">
    <PropertyGroup Condition="'$(InnerTargets)' == ''">
      <InnerTargets>Build</InnerTargets>
    </PropertyGroup>
  </Target>
  <!--
  ============================================================
                                       Clean

   Cross-targeting version of clean.

   Inner-build dispatch is a clone of DispatchToInnerBuilds;
   the only reason it's replicated is that it must be a different
   target to be run in the same build (e.g. by Rebuild or by
   a /t:Clean;Build invocation.
  ============================================================
  -->
  <Target Name="Clean" DependsOnTargets="_ComputeTargetFrameworkItems">
    <!-- If this logic is changed, also update DispatchToInnerBuilds -->
    <MSBuild Projects="@(_InnerBuildProjects)" Condition="'@(_InnerBuildProjects)' != '' " Targets="Clean" BuildInParallel="$(BuildInParallel)" />
  </Target>
  <!--
  ============================================================
                                       Rebuild

   Cross-targeting version of rebuild.
  ============================================================
  -->
  <Target Name="Rebuild" DependsOnTargets="Clean;Build" />
  <!--
    This will import NuGet restore targets. We need restore to work before any package assets are available.
  -->
  <PropertyGroup>
    <MSBuildUseVisualStudioDirectoryLayout Condition="'$(MSBuildUseVisualStudioDirectoryLayout)'==''">$([MSBuild]::IsRunningFromVisualStudio())</MSBuildUseVisualStudioDirectoryLayout>
    <NuGetRestoreTargets Condition="'$(NuGetRestoreTargets)'=='' and '$(MSBuildUseVisualStudioDirectoryLayout)'=='true'">$([MSBuild]::GetToolsDirectory32())\..\..\..\Common7\IDE\CommonExtensions\Microsoft\NuGet\NuGet.targets</NuGetRestoreTargets>
    <NuGetRestoreTargets Condition="'$(NuGetRestoreTargets)'==''">$(MSBuildToolsPath)\NuGet.targets</NuGetRestoreTargets>
  </PropertyGroup>
  <!--
============================================================================================================================================
  <Import Project="$(NuGetRestoreTargets)">

C:\Program Files\dotnet\sdk\8.0.110\NuGet.targets
============================================================================================================================================
-->
  <!--
***********************************************************************************************
NuGet.targets

WARNING:  DO NOT MODIFY this file unless you are knowledgeable about MSBuild and have
          created a backup copy.  Incorrect changes to this file will make it
          impossible to load or build your projects from the command-line or the IDE.

Copyright (c) .NET Foundation. All rights reserved.
***********************************************************************************************

  This target file contains the NuGet Restore target for walking the project and reference graph
  and restoring dependencies from the graph.

  Ways to use this targets file:
  1. Invoke it directly and provide project file paths using $(RestoreGraphProjectInput).
  2. With a solution this may be used as a target in the metaproj.
  3. Import the targets file from a project.

  Restore flow summary:
  1. Top level projects (entry points) are determined.
  2. Each project and all of its project references are walked recursively.
  3. The project is evaluated for each $(TargetFramework). Items are created
     for project properties and dependencies. Each item is marked
     with the project it came from so that it can be matched up later.
  4. All restore items generated by the walk are grouped together by
     project and convert into a project spec.

  The result file contains:
  1. A list of projects to restore.
  2. The complete closure of all projects referenced (Includes project references that are not being restored directly).
  3. Package and project dependencies for each project.
  4. DotnetCliTool references
  -->
  <PropertyGroup xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <!-- Mark that this target file has been loaded.  -->
    <IsRestoreTargetsFileLoaded>true</IsRestoreTargetsFileLoaded>
    <!-- Load NuGet.Build.Tasks.dll, this can be overridden to use a different version with $(RestoreTaskAssemblyFile) -->
    <RestoreTaskAssemblyFile Condition=" '$(RestoreTaskAssemblyFile)' == '' ">NuGet.Build.Tasks.dll</RestoreTaskAssemblyFile>
    <!-- Do not hide errors and warnings by default -->
    <HideWarningsAndErrors Condition=" '$(HideWarningsAndErrors)' == '' ">false</HideWarningsAndErrors>
    <!-- Recurse by default -->
    <RestoreRecursive Condition=" '$(RestoreRecursive)' == '' ">true</RestoreRecursive>
    <RestoreUseSkipNonexistentTargets Condition=" '$(RestoreUseSkipNonexistentTargets)' == '' ">true</RestoreUseSkipNonexistentTargets>
    <!-- RuntimeIdentifier compatibility check -->
    <ValidateRuntimeIdentifierCompatibility Condition=" '$(ValidateRuntimeIdentifierCompatibility)' == '' ">false</ValidateRuntimeIdentifierCompatibility>
    <!-- Error handling while walking projects -->
    <RestoreContinueOnError Condition=" '$(RestoreContinueOnError)' == '' ">WarnAndContinue</RestoreContinueOnError>
    <!-- Build in parallel -->
    <RestoreBuildInParallel Condition=" '$(BuildInParallel)' != '' ">$(BuildInParallel)</RestoreBuildInParallel>
    <RestoreBuildInParallel Condition=" '$(RestoreBuildInParallel)' == '' ">true</RestoreBuildInParallel>
    <!-- Check if the restore target was executed on a sln file -->
    <_RestoreSolutionFileUsed Condition=" '$(_RestoreSolutionFileUsed)' == '' AND '$(SolutionDir)' != '' AND $(MSBuildProjectFullPath.EndsWith('.metaproj')) == 'true' ">true</_RestoreSolutionFileUsed>
    <!-- We default to MSBuildInteractive. -->
    <NuGetInteractive Condition=" '$(NuGetInteractive)' == '' ">$(MSBuildInteractive)</NuGetInteractive>
    <!-- Mark that this targets file supports package download. -->
    <PackageDownloadSupported>true</PackageDownloadSupported>
    <!-- Mark that this targets file GetReferenceNearestTargetFrameworkTask task supports the TargetPlatformMoniker -->
    <GetReferenceNearestTargetFrameworkTaskSupportsTargetPlatformParameter>true</GetReferenceNearestTargetFrameworkTaskSupportsTargetPlatformParameter>
    <!-- Flag if the Central package file is enabled -->
    <_CentralPackageVersionsEnabled Condition="'$(ManagePackageVersionsCentrally)' == 'true' AND '$(CentralPackageVersionsFileImported)' == 'true'">true</_CentralPackageVersionsEnabled>
  </PropertyGroup>
  <PropertyGroup xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <!-- Exclude packages from changing restore inputs.  -->
    <_GenerateRestoreGraphProjectEntryInputProperties>ExcludeRestorePackageImports=true</_GenerateRestoreGraphProjectEntryInputProperties>
    <!-- Standalone mode
         This is used by NuGet.exe to inject targets into the project that will be
         walked next. In normal /t:Restore mode this causes a duplicate import
         since NuGet.targets it loaded as part of MSBuild, there is should be
         skipped. -->
    <_GenerateRestoreGraphProjectEntryInputProperties Condition=" '$(RestoreUseCustomAfterTargets)' == 'true' ">
      $(_GenerateRestoreGraphProjectEntryInputProperties);
      NuGetRestoreTargets=$(MSBuildThisFileFullPath);
      RestoreUseCustomAfterTargets=$(RestoreUseCustomAfterTargets);
      CustomAfterMicrosoftCommonCrossTargetingTargets=$(MSBuildThisFileFullPath);
      CustomAfterMicrosoftCommonTargets=$(MSBuildThisFileFullPath);
    </_GenerateRestoreGraphProjectEntryInputProperties>
    <!-- Include SolutionDir and SolutionName for solution restores and persist these properties during the walk. -->
    <_GenerateRestoreGraphProjectEntryInputProperties Condition=" '$(_RestoreSolutionFileUsed)' == 'true' ">
      $(_GenerateRestoreGraphProjectEntryInputProperties);
      _RestoreSolutionFileUsed=true;
      SolutionDir=$(SolutionDir);
      SolutionName=$(SolutionName);
      SolutionFileName=$(SolutionFileName);
      SolutionPath=$(SolutionPath);
      SolutionExt=$(SolutionExt);
    </_GenerateRestoreGraphProjectEntryInputProperties>
  </PropertyGroup>
  <ItemGroup Condition="'$(ManagePackageVersionsCentrally)' == 'true' And '$(RestoreEnableGlobalPackageReference)' != 'false'" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <!--
        Add GlobalPackageReference items to the PackageReference item group with no version.

        Global package references only include the same assets as a development dependency (runtime; build; native; contentfiles; analyzers)
        because those kind of packages are the best candidate for a global package reference.  They are generally packages that
        extend the build.

        Global package references have all assets private because central package references are generally packages that provide
        versioning, signing, etc and should not flow to downstream dependencies.  Also, central package references are already
        referenced by every project in the tree so they don't need to be transitive.
      -->
    <PackageReference Include="@(GlobalPackageReference)" Version="" IncludeAssets="Runtime;Build;Native;contentFiles;Analyzers" PrivateAssets="All" />
    <!--
        Add GlobalPackageReference items to the PackageVersion item group with the version.
      -->
    <PackageVersion Include="@(GlobalPackageReference)" Version="%(Version)" />
  </ItemGroup>
  <!-- Tasks -->
  <UsingTask TaskName="NuGet.Build.Tasks.RestoreTask" AssemblyFile="$(RestoreTaskAssemblyFile)" xmlns="http://schemas.microsoft.com/developer/msbuild/2003" />
  <UsingTask TaskName="NuGet.Build.Tasks.WriteRestoreGraphTask" AssemblyFile="$(RestoreTaskAssemblyFile)" xmlns="http://schemas.microsoft.com/developer/msbuild/2003" />
  <UsingTask TaskName="NuGet.Build.Tasks.GetRestoreProjectJsonPathTask" AssemblyFile="$(RestoreTaskAssemblyFile)" xmlns="http://schemas.microsoft.com/developer/msbuild/2003" />
  <UsingTask TaskName="NuGet.Build.Tasks.GetRestoreProjectReferencesTask" AssemblyFile="$(RestoreTaskAssemblyFile)" xmlns="http://schemas.microsoft.com/developer/msbuild/2003" />
  <UsingTask TaskName="NuGet.Build.Tasks.GetRestorePackageReferencesTask" AssemblyFile="$(RestoreTaskAssemblyFile)" xmlns="http://schemas.microsoft.com/developer/msbuild/2003" />
  <UsingTask TaskName="NuGet.Build.Tasks.GetCentralPackageVersionsTask" AssemblyFile="$(RestoreTaskAssemblyFile)" xmlns="http://schemas.microsoft.com/developer/msbuild/2003" />
  <UsingTask TaskName="NuGet.Build.Tasks.GetRestorePackageDownloadsTask" AssemblyFile="$(RestoreTaskAssemblyFile)" xmlns="http://schemas.microsoft.com/developer/msbuild/2003" />
  <UsingTask TaskName="NuGet.Build.Tasks.GetRestoreFrameworkReferencesTask" AssemblyFile="$(RestoreTaskAssemblyFile)" xmlns="http://schemas.microsoft.com/developer/msbuild/2003" />
  <UsingTask TaskName="NuGet.Build.Tasks.GetRestoreDotnetCliToolsTask" AssemblyFile="$(RestoreTaskAssemblyFile)" xmlns="http://schemas.microsoft.com/developer/msbuild/2003" />
  <UsingTask TaskName="NuGet.Build.Tasks.GetProjectTargetFrameworksTask" AssemblyFile="$(RestoreTaskAssemblyFile)" xmlns="http://schemas.microsoft.com/developer/msbuild/2003" />
  <UsingTask TaskName="NuGet.Build.Tasks.GetRestoreSolutionProjectsTask" AssemblyFile="$(RestoreTaskAssemblyFile)" xmlns="http://schemas.microsoft.com/developer/msbuild/2003" />
  <UsingTask TaskName="NuGet.Build.Tasks.GetRestoreSettingsTask" AssemblyFile="$(RestoreTaskAssemblyFile)" xmlns="http://schemas.microsoft.com/developer/msbuild/2003" />
  <UsingTask TaskName="NuGet.Build.Tasks.WarnForInvalidProjectsTask" AssemblyFile="$(RestoreTaskAssemblyFile)" xmlns="http://schemas.microsoft.com/developer/msbuild/2003" />
  <UsingTask TaskName="NuGet.Build.Tasks.GetReferenceNearestTargetFrameworkTask" AssemblyFile="$(RestoreTaskAssemblyFile)" xmlns="http://schemas.microsoft.com/developer/msbuild/2003" />
  <UsingTask TaskName="NuGet.Build.Tasks.GetRestoreProjectStyleTask" AssemblyFile="$(RestoreTaskAssemblyFile)" xmlns="http://schemas.microsoft.com/developer/msbuild/2003" />
  <UsingTask TaskName="NuGet.Build.Tasks.NuGetMessageTask" AssemblyFile="$(RestoreTaskAssemblyFile)" xmlns="http://schemas.microsoft.com/developer/msbuild/2003" />
  <UsingTask TaskName="NuGet.Build.Tasks.CheckForDuplicateNuGetItemsTask" AssemblyFile="$(RestoreTaskAssemblyFile)" xmlns="http://schemas.microsoft.com/developer/msbuild/2003" />
  <UsingTask TaskName="NuGet.Build.Tasks.GetGlobalPropertyValueTask" AssemblyFile="$(RestoreTaskAssemblyFile)" xmlns="http://schemas.microsoft.com/developer/msbuild/2003" />
  <!--
    ============================================================
    Restore
    Main entry point for restoring packages
    ============================================================
  -->
  <Target Name="Restore" DependsOnTargets="_GenerateRestoreGraph" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <!-- Drop any duplicate items -->
    <RemoveDuplicates Inputs="@(_RestoreGraphEntry)">
      <Output TaskParameter="Filtered" ItemName="_RestoreGraphEntryFiltered" />
    </RemoveDuplicates>
    <!-- Call restore -->
    <RestoreTask RestoreGraphItems="@(_RestoreGraphEntryFiltered)" RestoreDisableParallel="$(RestoreDisableParallel)" RestoreNoCache="$(RestoreNoCache)" RestoreIgnoreFailedSources="$(RestoreIgnoreFailedSources)" RestoreRecursive="$(RestoreRecursive)" RestoreForce="$(RestoreForce)" HideWarningsAndErrors="$(HideWarningsAndErrors)" Interactive="$(NuGetInteractive)" RestoreForceEvaluate="$(RestoreForceEvaluate)" RestorePackagesConfig="$(RestorePackagesConfig)">
      <Output TaskParameter="EmbedInBinlog" ItemName="EmbedInBinlog" />
    </RestoreTask>
  </Target>
  <!--
    ============================================================
    GenerateRestoreGraphFile
    Writes the output of _GenerateRestoreGraph to disk
    ============================================================
  -->
  <Target Name="GenerateRestoreGraphFile" DependsOnTargets="_GenerateRestoreGraph" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <!-- Validate  -->
    <Error Condition="$(RestoreGraphOutputPath) == ''" Text="Missing RestoreGraphOutputPath property!" />
    <!-- Drop any duplicate items -->
    <RemoveDuplicates Inputs="@(_RestoreGraphEntry)">
      <Output TaskParameter="Filtered" ItemName="_RestoreGraphEntryFiltered" />
    </RemoveDuplicates>
    <!-- Write file -->
    <WriteRestoreGraphTask RestoreGraphItems="@(_RestoreGraphEntryFiltered)" RestoreGraphOutputPath="$(RestoreGraphOutputPath)" RestoreRecursive="$(RestoreRecursive)" />
  </Target>
  <!--
    ============================================================
    CollectPackageReferences
    Gathers all PackageReference items from the project.
    This target may be used as an extension point to modify
    package references before NuGet reads them.
    ============================================================
  -->
  <Target Name="CollectPackageReferences" Returns="@(PackageReference)" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <!-- NOTE for design-time builds we need to ensure that we continue on error. -->
    <PropertyGroup>
      <CollectPackageReferencesContinueOnError>$(ContinueOnError)</CollectPackageReferencesContinueOnError>
      <CollectPackageReferencesContinueOnError Condition="'$(ContinueOnError)' == '' ">false</CollectPackageReferencesContinueOnError>
    </PropertyGroup>
    <CheckForDuplicateNuGetItemsTask Condition="'$(DisableCheckingDuplicateNuGetItems)' != 'true' " Items="@(PackageReference)" ItemName="PackageReference" LogCode="NU1504" MSBuildProjectFullPath="$(MSBuildProjectFullPath)" TreatWarningsAsErrors="$(TreatWarningsAsErrors)" WarningsAsErrors="$(WarningsAsErrors)" WarningsNotAsErrors="$(WarningsNotAsErrors)" NoWarn="$(NoWarn)" ContinueOnError="$(CollectPackageReferencesContinueOnError)">
      <Output TaskParameter="DeduplicatedItems" ItemName="DeduplicatedPackageReferences" />
    </CheckForDuplicateNuGetItemsTask>
    <ItemGroup Condition="'@(DeduplicatedPackageReferences)' != ''">
      <PackageReference Remove="@(PackageReference)" />
      <PackageReference Include="@(DeduplicatedPackageReferences)" />
    </ItemGroup>
  </Target>
  <!--
    ============================================================
    CollectCentralPackageVersions
    Gathers all PackageVersion items from the central package versions file.
    ============================================================
  -->
  <Target Name="CollectCentralPackageVersions" Returns="@(PackageVersion)" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <!-- NOTE for design-time builds we need to ensure that we continue on error. -->
    <PropertyGroup>
      <CollectCentralPackageVersionsContinueOnError>$(ContinueOnError)</CollectCentralPackageVersionsContinueOnError>
      <CollectCentralPackageVersionsContinueOnError Condition="'$(ContinueOnError)' == '' ">false</CollectCentralPackageVersionsContinueOnError>
    </PropertyGroup>
    <CheckForDuplicateNuGetItemsTask Condition="'$(DisableCheckingDuplicateNuGetItems)' != 'true' " Items="@(PackageVersion)" ItemName="PackageVersion" LogCode="NU1506" MSBuildProjectFullPath="$(MSBuildProjectFullPath)" TreatWarningsAsErrors="$(TreatWarningsAsErrors)" WarningsAsErrors="$(WarningsAsErrors)" WarningsNotAsErrors="$(WarningsNotAsErrors)" NoWarn="$(NoWarn)" ContinueOnError="$(CollectCentralPackageVersionsContinueOnError)">
      <Output TaskParameter="DeduplicatedItems" ItemName="DeduplicatedPackageVersions" />
    </CheckForDuplicateNuGetItemsTask>
    <ItemGroup Condition="'@(DeduplicatedPackageVersions)' != ''">
      <PackageVersion Remove="@(PackageVersion)" />
      <PackageVersion Include="@(DeduplicatedPackageVersions)" />
    </ItemGroup>
  </Target>
  <!--
    ============================================================
    CollectPackageDownloads
    Gathers all PackageDownload items from the project.
    This target may be used as an extension point to modify
    package downloads before NuGet reads them.
    ============================================================
  -->
  <Target Name="CollectPackageDownloads" Returns="@(PackageDownload)" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <!-- NOTE for design-time builds we need to ensure that we continue on error. -->
    <PropertyGroup>
      <CollectPackageDownloadsContinueOnError>$(ContinueOnError)</CollectPackageDownloadsContinueOnError>
      <CollectPackageDownloadsContinueOnError Condition="'$(ContinueOnError)' == '' ">false</CollectPackageDownloadsContinueOnError>
    </PropertyGroup>
    <CheckForDuplicateNuGetItemsTask Condition="'$(DisableCheckingDuplicateNuGetItems)' != 'true' " Items="@(PackageDownload)" ItemName="PackageDownload" LogCode="NU1505" MSBuildProjectFullPath="$(MSBuildProjectFullPath)" TreatWarningsAsErrors="$(TreatWarningsAsErrors)" WarningsAsErrors="$(WarningsAsErrors)" WarningsNotAsErrors="$(WarningsNotAsErrors)" NoWarn="$(NoWarn)" ContinueOnError="$(CollectPackageDownloadsContinueOnError)">
      <Output TaskParameter="DeduplicatedItems" ItemName="DeduplicatedPackageDownloads" />
    </CheckForDuplicateNuGetItemsTask>
    <ItemGroup Condition="'@(DeduplicatedPackageDownloads)' != ''">
      <PackageDownload Remove="@(PackageDownload)" />
      <PackageDownload Include="@(DeduplicatedPackageDownloads)" />
    </ItemGroup>
  </Target>
  <!--
    ============================================================
    CollectFrameworkReferences
    ============================================================
  -->
  <Target Name="CollectFrameworkReferences" Returns="@(_FrameworkReferenceForRestore)" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <ItemGroup>
      <_FrameworkReferenceForRestore Include="@(FrameworkReference)" Condition="'%(FrameworkReference.IsTransitiveFrameworkReference)' != 'true'" />
    </ItemGroup>
  </Target>
  <!--
    ============================================================
    _LoadRestoreGraphEntryPoints
    Find project entry points and load them into items.
    ============================================================
  -->
  <Target Name="_LoadRestoreGraphEntryPoints" Returns="@(RestoreGraphProjectInputItems)" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <!-- Allow overriding items with RestoreGraphProjectInput -->
    <ItemGroup Condition=" @(RestoreGraphProjectInputItems) == '' ">
      <RestoreGraphProjectInputItems Include="$(RestoreGraphProjectInput)" />
    </ItemGroup>
    <!-- Project case -->
    <ItemGroup Condition=" $(MSBuildProjectFullPath.EndsWith('.metaproj')) != 'true' AND @(RestoreGraphProjectInputItems) == '' ">
      <RestoreGraphProjectInputItems Include="$(MSBuildProjectFullPath)" />
    </ItemGroup>
    <!-- Solution case -->
    <GetRestoreSolutionProjectsTask Condition=" $(MSBuildProjectFullPath.EndsWith('.metaproj')) == 'true' AND @(RestoreGraphProjectInputItems) == '' " ProjectReferences="@(ProjectReference)" SolutionFilePath="$(MSBuildProjectFullPath)">
      <Output TaskParameter="OutputProjectReferences" ItemName="RestoreGraphProjectInputItems" />
    </GetRestoreSolutionProjectsTask>
  </Target>
  <!--
    ============================================================
    _FilterRestoreGraphProjectInputItems
    Filter out unsupported project entry points.
    ============================================================
  -->
  <Target Name="_FilterRestoreGraphProjectInputItems" DependsOnTargets="_LoadRestoreGraphEntryPoints" Returns="@(FilteredRestoreGraphProjectInputItems)" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <PropertyGroup>
      <RestoreProjectFilterMode Condition=" '$(RestoreProjectFilterMode)' == '' ">exclusionlist</RestoreProjectFilterMode>
    </PropertyGroup>
    <!-- Filter to a list of known supported types -->
    <ItemGroup Condition=" '$(RestoreProjectFilterMode)' == 'inclusionlist' ">
      <_FilteredRestoreGraphProjectInputItemsTmp Include="@(RestoreGraphProjectInputItems)" Condition=" '%(RestoreGraphProjectInputItems.Extension)' == '.csproj' Or&#xD;&#xA;                   '%(RestoreGraphProjectInputItems.Extension)' == '.vbproj' Or&#xD;&#xA;                   '%(RestoreGraphProjectInputItems.Extension)' == '.fsproj' Or&#xD;&#xA;                   '%(RestoreGraphProjectInputItems.Extension)' == '.nuproj' Or&#xD;&#xA;                   '%(RestoreGraphProjectInputItems.Extension)' == '.proj' Or&#xD;&#xA;                   '%(RestoreGraphProjectInputItems.Extension)' == '.msbuildproj' Or&#xD;&#xA;                   '%(RestoreGraphProjectInputItems.Extension)' == '.vcxproj' " />
    </ItemGroup>
    <!-- Filter out disallowed types -->
    <ItemGroup Condition=" '$(RestoreProjectFilterMode)' == 'exclusionlist' ">
      <_FilteredRestoreGraphProjectInputItemsTmp Include="@(RestoreGraphProjectInputItems)" Condition=" '%(RestoreGraphProjectInputItems.Extension)' != '.metaproj'&#xD;&#xA;                   AND '%(RestoreGraphProjectInputItems.Extension)' != '.shproj'&#xD;&#xA;                   AND '%(RestoreGraphProjectInputItems.Extension)' != '.vcxitems'&#xD;&#xA;                   AND '%(RestoreGraphProjectInputItems.Extension)' != '.vdproj'&#xD;&#xA;                   AND '%(RestoreGraphProjectInputItems.Extension)' != '' " />
    </ItemGroup>
    <!-- No filtering -->
    <ItemGroup Condition=" '$(RestoreProjectFilterMode)' != 'exclusionlist' AND '$(RestoreProjectFilterMode)' != 'inclusionlist' ">
      <_FilteredRestoreGraphProjectInputItemsTmp Include="@(RestoreGraphProjectInputItems)" />
    </ItemGroup>
    <!-- Remove duplicates -->
    <RemoveDuplicates Inputs="@(_FilteredRestoreGraphProjectInputItemsTmp)">
      <Output TaskParameter="Filtered" ItemName="FilteredRestoreGraphProjectInputItemsWithoutDuplicates" />
    </RemoveDuplicates>
    <!-- Remove projects that do not support restore. -->
    <!-- With SkipNonexistentTargets support -->
    <MSBuild Condition=" '$(RestoreUseSkipNonexistentTargets)' == 'true' " BuildInParallel="$(RestoreBuildInParallel)" Projects="@(FilteredRestoreGraphProjectInputItemsWithoutDuplicates)" Targets="_IsProjectRestoreSupported" SkipNonexistentTargets="true" Properties="$(_GenerateRestoreGraphProjectEntryInputProperties)">
      <Output TaskParameter="TargetOutputs" ItemName="FilteredRestoreGraphProjectInputItems" />
    </MSBuild>
    <!-- Without SkipNonexistentTargets support -->
    <MSBuild Condition=" '$(RestoreUseSkipNonexistentTargets)' != 'true' " Projects="@(FilteredRestoreGraphProjectInputItemsWithoutDuplicates)" Targets="_IsProjectRestoreSupported" ContinueOnError="$(RestoreContinueOnError)" Properties="$(_GenerateRestoreGraphProjectEntryInputProperties)">
      <Output TaskParameter="TargetOutputs" ItemName="FilteredRestoreGraphProjectInputItems" />
    </MSBuild>
    <!-- Warn for projects that do not support restore. -->
    <WarnForInvalidProjectsTask Condition=" '$(DisableWarnForInvalidRestoreProjects)' != 'true' AND '$(HideWarningsAndErrors)' != 'true' " AllProjects="@(FilteredRestoreGraphProjectInputItemsWithoutDuplicates)" ValidProjects="@(FilteredRestoreGraphProjectInputItems)" />
  </Target>
  <!--
    ============================================================
    _GenerateRestoreGraph
    Entry point for creating the project to project restore graph.
    ============================================================
  -->
  <Target Name="_GenerateRestoreGraph" DependsOnTargets="_FilterRestoreGraphProjectInputItems;_GetAllRestoreProjectPathItems" Returns="@(_RestoreGraphEntry)" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <Message Text="Generating dg file" Importance="low" />
    <Message Text="%(_RestoreProjectPathItems.Identity)" Importance="low" />
    <!-- Use all projects if RestoreRecursive is true. Otherwise use only the top level projects. -->
    <ItemGroup>
      <_GenerateRestoreGraphProjectEntryInput Include="@(FilteredRestoreGraphProjectInputItems)" Condition=" '$(RestoreRecursive)' != 'true' " />
      <_GenerateRestoreGraphProjectEntryInput Include="@(_RestoreProjectPathItems)" Condition=" '$(RestoreRecursive)' == 'true' " />
    </ItemGroup>
    <!-- Add top level entries to the direct restore list. These projects will also restore tools. -->
    <MSBuild BuildInParallel="$(RestoreBuildInParallel)" Projects="@(_GenerateRestoreGraphProjectEntryInput)" Targets="_GenerateRestoreGraphProjectEntry" Properties="$(_GenerateRestoreGraphProjectEntryInputProperties)">
      <Output TaskParameter="TargetOutputs" ItemName="_RestoreGraphEntry" />
    </MSBuild>
    <!-- Generate a spec for every project including dependencies. -->
    <MSBuild BuildInParallel="$(RestoreBuildInParallel)" Projects="@(_RestoreProjectPathItems)" Targets="_GenerateProjectRestoreGraph" Properties="$(_GenerateRestoreGraphProjectEntryInputProperties)">
      <Output TaskParameter="TargetOutputs" ItemName="_RestoreGraphEntry" />
    </MSBuild>
  </Target>
  <!--
    ============================================================
    _GenerateRestoreGraphProjectEntry
    Top level entry point within a project.
    ============================================================
  -->
  <Target Name="_GenerateRestoreGraphProjectEntry" DependsOnTargets="_GenerateRestoreSpecs;_GenerateDotnetCliToolReferenceSpecs" Returns="@(_RestoreGraphEntry)" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <!-- Returns restore graph entries for the project and all dependencies -->
  </Target>
  <!--
    ============================================================
    _GenerateRestoreSpecs
    Mark entry points for restore.
    ============================================================
  -->
  <Target Name="_GenerateRestoreSpecs" DependsOnTargets="_GetRestoreProjectStyle" Returns="@(_RestoreGraphEntry)" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <Message Text="Restore entry point $(MSBuildProjectFullPath)" Importance="low" />
    <!-- Mark entry point -->
    <ItemGroup Condition=" '$(RestoreProjects)' == '' OR '$(RestoreProjects)' == 'true' ">
      <_RestoreGraphEntry Include="$([System.Guid]::NewGuid())" Condition=" '$(RestoreProjectStyle)' != 'Unknown' ">
        <Type>RestoreSpec</Type>
        <ProjectUniqueName>$(MSBuildProjectFullPath)</ProjectUniqueName>
      </_RestoreGraphEntry>
    </ItemGroup>
  </Target>
  <!--
    ============================================================
    _GenerateDotnetCliToolReferenceSpecs
    Collect DotnetCliToolReferences
    ============================================================
  -->
  <Target Name="_GenerateDotnetCliToolReferenceSpecs" DependsOnTargets="_GetRestoreSettings" Returns="@(_RestoreGraphEntry)" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <PropertyGroup>
      <DotnetCliToolTargetFramework Condition=" '$(DotnetCliToolTargetFramework)' == '' ">netcoreapp1.0</DotnetCliToolTargetFramework>
    </PropertyGroup>
    <!-- Write out tool references -->
    <GetRestoreDotnetCliToolsTask Condition=" '$(RestoreDotnetCliToolReferences)' == '' OR '$(RestoreDotnetCliToolReferences)' == 'true' " ProjectPath="$(MSBuildProjectFullPath)" ToolFramework="$(DotnetCliToolTargetFramework)" RestorePackagesPath="$(_OutputPackagesPath)" RestoreFallbackFolders="$(_OutputFallbackFolders)" RestoreSources="$(_OutputSources)" RestoreConfigFilePaths="$(_OutputConfigFilePaths)" DotnetCliToolReferences="@(DotnetCliToolReference)">
      <Output TaskParameter="RestoreGraphItems" ItemName="_RestoreGraphEntry" />
    </GetRestoreDotnetCliToolsTask>
  </Target>
  <!--
    ============================================================
    _GetProjectJsonPath
    Discover the project.json path if one exists for the project.
    ============================================================
  -->
  <Target Name="_GetProjectJsonPath" Returns="$(_CurrentProjectJsonPath)" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <!-- Get project.json path -->
    <!-- Skip this if the project style is already set. -->
    <GetRestoreProjectJsonPathTask ProjectPath="$(MSBuildProjectFullPath)" Condition=" '$(RestoreProjectStyle)' == 'ProjectJson' OR '$(RestoreProjectStyle)' == '' ">
      <Output TaskParameter="ProjectJsonPath" PropertyName="_CurrentProjectJsonPath" />
    </GetRestoreProjectJsonPathTask>
  </Target>
  <!--
    ============================================================
    _GetRestoreProjectStyle
    Determine the project restore type.
    ============================================================
  -->
  <Target Name="_GetRestoreProjectStyle" DependsOnTargets="_GetProjectJsonPath;CollectPackageReferences" Returns="$(RestoreProjectStyle);$(PackageReferenceCompatibleProjectStyle)" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <!--
      Older versions of MSBuild do not support the Count() item function which is an optimization.  Expanding the
      entire item list into a semicolon delimited string is slower but older versions of MSBuild don't support it so
      use the older logic if necessary
    -->
    <PropertyGroup Condition="'$(MSBuildAssemblyVersion)' &lt; '15.0'">
      <_HasPackageReferenceItems Condition="'@(PackageReference)' != ''">true</_HasPackageReferenceItems>
    </PropertyGroup>
    <PropertyGroup Condition="'$(MSBuildAssemblyVersion)' &gt;= '15.0'">
      <_HasPackageReferenceItems Condition="@(PackageReference-&gt;Count()) &gt; 0">true</_HasPackageReferenceItems>
    </PropertyGroup>
    <GetRestoreProjectStyleTask HasPackageReferenceItems="$(_HasPackageReferenceItems)" MSBuildProjectDirectory="$(MSBuildProjectDirectory)" MSBuildProjectName="$(MSBuildProjectName)" ProjectJsonPath="$(_CurrentProjectJsonPath)" RestoreProjectStyle="$(RestoreProjectStyle)">
      <Output TaskParameter="ProjectStyle" PropertyName="RestoreProjectStyle" />
      <Output TaskParameter="IsPackageReferenceCompatibleProjectStyle" PropertyName="PackageReferenceCompatibleProjectStyle" />
    </GetRestoreProjectStyleTask>
    <PropertyGroup>
      <_HasPackageReferenceItems />
    </PropertyGroup>
  </Target>
  <!--
    ============================================================
    EnableIntermediateOutputPathMismatchWarning
    If using PackageReference, enable an MSBuild warning if BaseIntermediateOutputPath is set to something different
    than MSBuildProjectExtensionsPath, because it may be unexpected that the assets and related files wouldn't be written
    to the BaseIntermediateOutputPath.
    ============================================================
  -->
  <Target Name="EnableIntermediateOutputPathMismatchWarning" DependsOnTargets="_GetRestoreProjectStyle" BeforeTargets="_CheckForInvalidConfigurationAndPlatform" Condition="'$(RestoreProjectStyle)' == 'PackageReference'" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <PropertyGroup Condition="'$(EnableBaseIntermediateOutputPathMismatchWarning)' == ''">
      <EnableBaseIntermediateOutputPathMismatchWarning>true</EnableBaseIntermediateOutputPathMismatchWarning>
    </PropertyGroup>
  </Target>
  <!--
    ============================================================
    _GetRestoreTargetFrameworksOutput
    Read target frameworks from the project.
    Non-NETCore project frameworks will be returned.
    ============================================================
  -->
  <Target Name="_GetRestoreTargetFrameworksOutput" DependsOnTargets="_GetRestoreProjectStyle;_GetRestoreTargetFrameworkOverride" Returns="@(_RestoreTargetFrameworksOutputFiltered)" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <PropertyGroup>
      <_RestoreProjectFramework />
      <_TargetFrameworkToBeUsed Condition=" '$(_TargetFrameworkOverride)' == '' ">$(TargetFrameworks)</_TargetFrameworkToBeUsed>
    </PropertyGroup>
    <!-- For project.json projects target frameworks will be read from project.json. -->
    <GetProjectTargetFrameworksTask Condition=" '$(RestoreProjectStyle)' != 'ProjectJson'" ProjectPath="$(MSBuildProjectFullPath)" TargetFrameworks="$(_TargetFrameworkToBeUsed)" TargetFramework="$(TargetFramework)" TargetFrameworkMoniker="$(TargetFrameworkMoniker)" TargetPlatformIdentifier="$(TargetPlatformIdentifier)" TargetPlatformVersion="$(TargetPlatformVersion)" TargetPlatformMinVersion="$(TargetPlatformMinVersion)">
      <Output TaskParameter="ProjectTargetFrameworks" PropertyName="_RestoreProjectFramework" />
    </GetProjectTargetFrameworksTask>
    <ItemGroup Condition=" '$(_RestoreProjectFramework)' != '' ">
      <_RestoreTargetFrameworksOutputFiltered Include="$(_RestoreProjectFramework.Split(';'))" />
    </ItemGroup>
  </Target>
  <!--
    ============================================================
    _GetRestoreTargetFrameworksAsItems
    Read $(TargetFrameworks) from the project as items.
    Projects that do not have $(TargetFrameworks) will noop.
    If $(TargetFramework) is specified globally, it'll be preferred over $(TargetFrameworks)
    ============================================================
  -->
  <Target Name="_GetRestoreTargetFrameworksAsItems" DependsOnTargets="_GetRestoreProjectStyle;_GetRestoreTargetFrameworkOverride" Returns="@(_RestoreTargetFrameworkItems)" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <ItemGroup Condition=" '$(TargetFrameworks)' != '' AND '$(_TargetFrameworkOverride)' == '' ">
      <_RestoreTargetFrameworkItems Include="$(TargetFrameworks.Split(';'))" />
    </ItemGroup>
    <ItemGroup Condition=" '$(TargetFrameworks)' != '' AND '$(_TargetFrameworkOverride)' != '' ">
      <_RestoreTargetFrameworkItems Include="$(_TargetFrameworkOverride)" />
    </ItemGroup>
  </Target>
  <!--
    ============================================================
    _GetRestoreSettings
    ============================================================
  -->
  <Target Name="_GetRestoreSettings" Condition=" '$(RestoreProjectStyle)' == 'PackageReference' OR '$(RestoreProjectStyle)' == 'ProjectJson' OR '$(RestoreProjectStyle)' == 'DotnetToolReference' OR '$(RestoreProjectStyle)' == 'PackagesConfig'" DependsOnTargets="_GetRestoreSettingsOverrides;_GetRestoreSettingsCurrentProject;_GetRestoreSettingsAllFrameworks" Returns="$(_OutputSources);$(_OutputPackagesPath);$(_OutputRepositoryPath);$(_OutputFallbackFolders);$(_OutputConfigFilePaths)" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <PropertyGroup Condition=" '$(RestoreSolutionDirectory)' == '' AND '$(RestoreProjectStyle)' == 'PackagesConfig' AND '$(SolutionDir)' != '*Undefined*'">
      <RestoreSolutionDirectory>$(SolutionDir)</RestoreSolutionDirectory>
    </PropertyGroup>
    <!-- For transitive project styles, we rely on evaluating all the settings and including them in the dg spec to faciliate no-op restore-->
    <GetRestoreSettingsTask ProjectUniqueName="$(MSBuildProjectFullPath)" RestoreSources="$(RestoreSources)" RestorePackagesPath="$(RestorePackagesPath)" RestoreRepositoryPath="$(RestoreRepositoryPath)" RestoreFallbackFolders="$(RestoreFallbackFolders)" RestoreConfigFile="$(RestoreConfigFile)" RestoreRootConfigDirectory="$(RestoreRootConfigDirectory)" RestoreSolutionDirectory="$(RestoreSolutionDirectory)" RestoreSettingsPerFramework="@(_RestoreSettingsPerFramework)" RestorePackagesPathOverride="$(_RestorePackagesPathOverride)" RestoreRepositoryPathOverride="$(_RestoreRepositoryPathOverride)" RestoreSourcesOverride="$(_RestoreSourcesOverride)" RestoreFallbackFoldersOverride="$(_RestoreFallbackFoldersOverride)" RestoreProjectStyle="$(RestoreProjectStyle)" MSBuildStartupDirectory="$(MSBuildStartupDirectory)">
      <Output TaskParameter="OutputSources" PropertyName="_OutputSources" />
      <Output TaskParameter="OutputPackagesPath" PropertyName="_OutputPackagesPath" />
      <Output TaskParameter="OutputRepositoryPath" PropertyName="_OutputRepositoryPath" />
      <Output TaskParameter="OutputFallbackFolders" PropertyName="_OutputFallbackFolders" />
      <Output TaskParameter="OutputConfigFilePaths" PropertyName="_OutputConfigFilePaths" />
    </GetRestoreSettingsTask>
  </Target>
  <!--
    ============================================================
    _GetRestoreSettingsCurrentProject
    Generate items for a single framework.
    ============================================================
  -->
  <Target Name="_GetRestoreSettingsCurrentProject" Condition=" '$(TargetFrameworks)' == '' AND '$(PackageReferenceCompatibleProjectStyle)' == 'true' " DependsOnTargets="_GetRestoreSettingsPerFramework" Returns="@(_RestoreSettingsPerFramework)" xmlns="http://schemas.microsoft.com/developer/msbuild/2003" />
  <!--
    ============================================================
    _GetRestoreSettingsAllFrameworks
    Generate items for all frameworks.
    ============================================================
  -->
  <Target Name="_GetRestoreSettingsAllFrameworks" Condition=" '$(TargetFrameworks)' != '' AND '$(PackageReferenceCompatibleProjectStyle)' == 'true' " DependsOnTargets="_GetRestoreTargetFrameworksAsItems;_GetRestoreProjectStyle" Returns="@(_RestoreSettingsPerFramework)" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <!-- Read additional sources and fallback folders for each framework  -->
    <MSBuild BuildInParallel="$(RestoreBuildInParallel)" Projects="$(MSBuildProjectFullPath)" Targets="_GetRestoreSettingsPerFramework" Properties="TargetFramework=%(_RestoreTargetFrameworkItems.Identity);&#xD;&#xA;                  $(_GenerateRestoreGraphProjectEntryInputProperties)">
      <Output TaskParameter="TargetOutputs" ItemName="_RestoreSettingsPerFramework" />
    </MSBuild>
  </Target>
  <!--
    ============================================================
    _GetRestoreSettingsPerFramework
    Generate items with framework specific settings.
    ============================================================
  -->
  <Target Name="_GetRestoreSettingsPerFramework" Returns="@(_RestoreSettingsPerFramework)" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <ItemGroup>
      <_RestoreSettingsPerFramework Include="$([System.Guid]::NewGuid())">
        <RestoreAdditionalProjectSources>$(RestoreAdditionalProjectSources)</RestoreAdditionalProjectSources>
        <RestoreAdditionalProjectFallbackFolders>$(RestoreAdditionalProjectFallbackFolders)</RestoreAdditionalProjectFallbackFolders>
        <RestoreAdditionalProjectFallbackFoldersExcludes>$(RestoreAdditionalProjectFallbackFoldersExcludes)</RestoreAdditionalProjectFallbackFoldersExcludes>
      </_RestoreSettingsPerFramework>
    </ItemGroup>
  </Target>
  <!--
    ============================================================
    _GenerateRestoreProjectSpec
    Generate a restore project spec for the current project.
    ============================================================
  -->
  <Target Name="_GenerateRestoreProjectSpec" DependsOnTargets="_GetRestoreProjectStyle;_GetRestoreTargetFrameworksOutput;_GetRestoreSettings" Returns="@(_RestoreGraphEntry)" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <!-- Determine the restore output path -->
    <PropertyGroup Condition=" '$(PackageReferenceCompatibleProjectStyle)' == 'true' OR '$(RestoreProjectStyle)' == 'ProjectJson' ">
      <RestoreOutputPath Condition=" '$(RestoreOutputPath)' == '' ">$(MSBuildProjectExtensionsPath)</RestoreOutputPath>
    </PropertyGroup>
    <ConvertToAbsolutePath Paths="$(RestoreOutputPath)" Condition=" '$(PackageReferenceCompatibleProjectStyle)' == 'true' OR '$(RestoreProjectStyle)' == 'ProjectJson'">
      <Output TaskParameter="AbsolutePaths" PropertyName="RestoreOutputAbsolutePath" />
    </ConvertToAbsolutePath>
    <!--
      Determine project name for the assets file.
      Highest priority: PackageId
      If PackageId does not exist use: AssemblyName
      If AssemblyName does not exist fallback to the project file name without the extension: $(MSBuildProjectName)

      For non-PackageReference projects use only: $(MSBuildProjectName)
    -->
    <PropertyGroup>
      <_RestoreProjectName>$(MSBuildProjectName)</_RestoreProjectName>
      <_RestoreProjectName Condition=" '$(PackageReferenceCompatibleProjectStyle)' == 'true' AND '$(AssemblyName)' != '' ">$(AssemblyName)</_RestoreProjectName>
      <_RestoreProjectName Condition=" '$(PackageReferenceCompatibleProjectStyle)' == 'true' AND '$(PackageId)' != '' ">$(PackageId)</_RestoreProjectName>
    </PropertyGroup>
    <!--
      Determine project version for .NETCore projects
      Default to 1.0.0
      Use Version if it exists
      Override with PackageVersion if it exists (same as pack)
    -->
    <PropertyGroup Condition=" '$(PackageReferenceCompatibleProjectStyle)' == 'true' ">
      <_RestoreProjectVersion>1.0.0</_RestoreProjectVersion>
      <_RestoreProjectVersion Condition=" '$(Version)' != '' ">$(Version)</_RestoreProjectVersion>
      <_RestoreProjectVersion Condition=" '$(PackageVersion)' != '' ">$(PackageVersion)</_RestoreProjectVersion>
    </PropertyGroup>
    <!-- Determine if this will use cross targeting -->
    <PropertyGroup Condition=" '$(PackageReferenceCompatibleProjectStyle)' == 'true' AND '$(TargetFrameworks)' != '' ">
      <_RestoreCrossTargeting>true</_RestoreCrossTargeting>
    </PropertyGroup>
    <!-- Determine if ContentFiles should be written by NuGet -->
    <PropertyGroup Condition=" '$(PackageReferenceCompatibleProjectStyle)' == 'true' AND '$(_RestoreSkipContentFileWrite)' == '' ">
      <_RestoreSkipContentFileWrite Condition=" '$(TargetFrameworks)' == '' AND '$(TargetFramework)' == '' ">true</_RestoreSkipContentFileWrite>
    </PropertyGroup>
    <!-- Write properties for the top level entry point -->
    <ItemGroup Condition=" '$(PackageReferenceCompatibleProjectStyle)' == 'true' ">
      <_RestoreGraphEntry Include="$([System.Guid]::NewGuid())">
        <Type>ProjectSpec</Type>
        <Version>$(_RestoreProjectVersion)</Version>
        <ProjectUniqueName>$(MSBuildProjectFullPath)</ProjectUniqueName>
        <ProjectPath>$(MSBuildProjectFullPath)</ProjectPath>
        <ProjectName>$(_RestoreProjectName)</ProjectName>
        <Sources>$(_OutputSources)</Sources>
        <FallbackFolders>$(_OutputFallbackFolders)</FallbackFolders>
        <PackagesPath>$(_OutputPackagesPath)</PackagesPath>
        <ProjectStyle>$(RestoreProjectStyle)</ProjectStyle>
        <OutputPath>$(RestoreOutputAbsolutePath)</OutputPath>
        <RuntimeIdentifiers>$(RuntimeIdentifiers);$(RuntimeIdentifier)</RuntimeIdentifiers>
        <RuntimeSupports>$(RuntimeSupports)</RuntimeSupports>
        <CrossTargeting>$(_RestoreCrossTargeting)</CrossTargeting>
        <RestoreLegacyPackagesDirectory>$(RestoreLegacyPackagesDirectory)</RestoreLegacyPackagesDirectory>
        <ValidateRuntimeAssets>$(ValidateRuntimeIdentifierCompatibility)</ValidateRuntimeAssets>
        <SkipContentFileWrite>$(_RestoreSkipContentFileWrite)</SkipContentFileWrite>
        <ConfigFilePaths>$(_OutputConfigFilePaths)</ConfigFilePaths>
        <TreatWarningsAsErrors>$(TreatWarningsAsErrors)</TreatWarningsAsErrors>
        <WarningsAsErrors>$(WarningsAsErrors)</WarningsAsErrors>
        <WarningsNotAsErrors>$(WarningsNotAsErrors)</WarningsNotAsErrors>
        <NoWarn>$(NoWarn)</NoWarn>
        <RestorePackagesWithLockFile>$(RestorePackagesWithLockFile)</RestorePackagesWithLockFile>
        <NuGetLockFilePath>$(NuGetLockFilePath)</NuGetLockFilePath>
        <RestoreLockedMode>$(RestoreLockedMode)</RestoreLockedMode>
        <_CentralPackageVersionsEnabled>$(_CentralPackageVersionsEnabled)</_CentralPackageVersionsEnabled>
        <CentralPackageVersionOverrideEnabled>$(CentralPackageVersionOverrideEnabled)</CentralPackageVersionOverrideEnabled>
        <CentralPackageTransitivePinningEnabled>$(CentralPackageTransitivePinningEnabled)</CentralPackageTransitivePinningEnabled>
        <NuGetAudit>$(NuGetAudit)</NuGetAudit>
        <NuGetAuditLevel>$(NuGetAuditLevel)</NuGetAuditLevel>
        <NuGetAuditMode>$(NuGetAuditMode)</NuGetAuditMode>
      </_RestoreGraphEntry>
    </ItemGroup>
    <!-- Use project.json -->
    <ItemGroup Condition=" '$(RestoreProjectStyle)' == 'ProjectJson' ">
      <_RestoreGraphEntry Include="$([System.Guid]::NewGuid())">
        <Type>ProjectSpec</Type>
        <ProjectUniqueName>$(MSBuildProjectFullPath)</ProjectUniqueName>
        <ProjectPath>$(MSBuildProjectFullPath)</ProjectPath>
        <ProjectName>$(_RestoreProjectName)</ProjectName>
        <Sources>$(_OutputSources)</Sources>
        <OutputPath>$(RestoreOutputAbsolutePath)</OutputPath>
        <FallbackFolders>$(_OutputFallbackFolders)</FallbackFolders>
        <PackagesPath>$(_OutputPackagesPath)</PackagesPath>
        <ProjectJsonPath>$(_CurrentProjectJsonPath)</ProjectJsonPath>
        <ProjectStyle>$(RestoreProjectStyle)</ProjectStyle>
        <ConfigFilePaths>$(_OutputConfigFilePaths)</ConfigFilePaths>
      </_RestoreGraphEntry>
    </ItemGroup>
    <!-- Use packages.config -->
    <ItemGroup Condition=" '$(RestoreProjectStyle)' == 'PackagesConfig' ">
      <_RestoreGraphEntry Include="$([System.Guid]::NewGuid())">
        <Type>ProjectSpec</Type>
        <ProjectUniqueName>$(MSBuildProjectFullPath)</ProjectUniqueName>
        <ProjectPath>$(MSBuildProjectFullPath)</ProjectPath>
        <ProjectName>$(_RestoreProjectName)</ProjectName>
        <ProjectStyle>$(RestoreProjectStyle)</ProjectStyle>
        <PackagesConfigPath Condition="Exists('$(MSBuildProjectDirectory)\packages.$(MSBuildProjectName).config')">$(MSBuildProjectDirectory)\packages.$(MSBuildProjectName).config</PackagesConfigPath>
        <PackagesConfigPath Condition="Exists('$(MSBuildProjectDirectory)\packages.config')">$(MSBuildProjectDirectory)\packages.config</PackagesConfigPath>
        <RestorePackagesWithLockFile>$(RestorePackagesWithLockFile)</RestorePackagesWithLockFile>
        <NuGetLockFilePath>$(NuGetLockFilePath)</NuGetLockFilePath>
        <RestoreLockedMode>$(RestoreLockedMode)</RestoreLockedMode>
        <Sources>$(_OutputSources)</Sources>
        <SolutionDir>$(SolutionDir)</SolutionDir>
        <RepositoryPath>$(_OutputRepositoryPath)</RepositoryPath>
        <ConfigFilePaths>$(_OutputConfigFilePaths)</ConfigFilePaths>
        <PackagesPath>$(_OutputPackagesPath)</PackagesPath>
        <TargetFrameworks>@(_RestoreTargetFrameworksOutputFiltered)</TargetFrameworks>
      </_RestoreGraphEntry>
    </ItemGroup>
    <!-- Non-NuGet type -->
    <ItemGroup Condition=" '$(RestoreProjectStyle)' == 'Unknown' ">
      <_RestoreGraphEntry Include="$([System.Guid]::NewGuid())">
        <Type>ProjectSpec</Type>
        <ProjectUniqueName>$(MSBuildProjectFullPath)</ProjectUniqueName>
        <ProjectPath>$(MSBuildProjectFullPath)</ProjectPath>
        <ProjectName>$(_RestoreProjectName)</ProjectName>
        <ProjectStyle>$(RestoreProjectStyle)</ProjectStyle>
        <TargetFrameworks>@(_RestoreTargetFrameworksOutputFiltered)</TargetFrameworks>
      </_RestoreGraphEntry>
    </ItemGroup>
  </Target>
  <!--
    ============================================================
    _GenerateProjectRestoreGraph
    Recursively walk project to project references.
    ============================================================
  -->
  <Target Name="_GenerateProjectRestoreGraph" DependsOnTargets="&#xD;&#xA;      _GetRestoreProjectStyle;&#xD;&#xA;      _GenerateRestoreProjectSpec;&#xD;&#xA;      _GenerateRestoreDependencies" Returns="@(_RestoreGraphEntry)" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <!-- Output from dependency targets -->
  </Target>
  <!--
    ============================================================
    _GenerateRestoreDependencies
    Generate items for package and project references.
    ============================================================
  -->
  <Target Name="_GenerateRestoreDependencies" DependsOnTargets="_GenerateProjectRestoreGraphAllFrameworks;_GenerateProjectRestoreGraphCurrentProject" Returns="@(_RestoreGraphEntry)" xmlns="http://schemas.microsoft.com/developer/msbuild/2003" />
  <!--
    ============================================================
    _GenerateProjectRestoreGraphAllFrameworks
    Walk dependencies for all frameworks.
    ============================================================
  -->
  <Target Name="_GenerateProjectRestoreGraphAllFrameworks" Condition=" '$(TargetFrameworks)' != '' " DependsOnTargets="_GetRestoreTargetFrameworksAsItems" Returns="@(_RestoreGraphEntry)" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <!-- Get project and package references  -->
    <!-- Evaluate for each framework -->
    <MSBuild BuildInParallel="$(RestoreBuildInParallel)" Projects="$(MSBuildProjectFullPath)" Targets="_GenerateProjectRestoreGraphPerFramework" Properties="TargetFramework=%(_RestoreTargetFrameworkItems.Identity);&#xD;&#xA;                  $(_GenerateRestoreGraphProjectEntryInputProperties)">
      <Output TaskParameter="TargetOutputs" ItemName="_RestoreGraphEntry" />
    </MSBuild>
  </Target>
  <!--
    ============================================================
    _GenerateProjectRestoreGraphCurrentProject
    Walk dependencies with the current framework.
    ============================================================
  -->
  <Target Name="_GenerateProjectRestoreGraphCurrentProject" Condition=" '$(TargetFrameworks)' == '' " DependsOnTargets="_GenerateProjectRestoreGraphPerFramework" Returns="@(_RestoreGraphEntry)" xmlns="http://schemas.microsoft.com/developer/msbuild/2003" />
  <!--
    ============================================================
    _GenerateProjectRestoreGraphPerFramework
    Walk dependencies using $(TargetFramework)
    ============================================================
  -->
  <Target Name="_GenerateProjectRestoreGraphPerFramework" DependsOnTargets="_GetRestoreProjectStyle;CollectPackageReferences;CollectPackageDownloads;CollectFrameworkReferences;CollectCentralPackageVersions" Returns="@(_RestoreGraphEntry)" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <!-- Write out project references -->
    <GetRestoreProjectReferencesTask ProjectUniqueName="$(MSBuildProjectFullPath)" ProjectReferences="@(ProjectReference)" TargetFrameworks="$(TargetFramework)" ParentProjectPath="$(MSBuildProjectFullPath)">
      <Output TaskParameter="RestoreGraphItems" ItemName="_RestoreGraphEntry" />
    </GetRestoreProjectReferencesTask>
    <!-- Write out package references-->
    <GetRestorePackageReferencesTask Condition=" '$(PackageReferenceCompatibleProjectStyle)' == 'true' " ProjectUniqueName="$(MSBuildProjectFullPath)" PackageReferences="@(PackageReference)" TargetFrameworks="$(TargetFramework)">
      <Output TaskParameter="RestoreGraphItems" ItemName="_RestoreGraphEntry" />
    </GetRestorePackageReferencesTask>
    <!-- Write out central package versions -->
    <GetCentralPackageVersionsTask Condition=" '$(PackageReferenceCompatibleProjectStyle)' == 'true' AND '$(_CentralPackageVersionsEnabled)' == 'true' " ProjectUniqueName="$(MSBuildProjectFullPath)" CentralPackageVersions="@(PackageVersion)" TargetFrameworks="$(TargetFramework)">
      <Output TaskParameter="RestoreGraphItems" ItemName="_RestoreGraphEntry" />
    </GetCentralPackageVersionsTask>
    <!-- Write out package downloads -->
    <GetRestorePackageDownloadsTask Condition=" '$(PackageReferenceCompatibleProjectStyle)' == 'true' " ProjectUniqueName="$(MSBuildProjectFullPath)" PackageDownloads="@(PackageDownload)" TargetFrameworks="$(TargetFramework)">
      <Output TaskParameter="RestoreGraphItems" ItemName="_RestoreGraphEntry" />
    </GetRestorePackageDownloadsTask>
    <!-- Write out Framework References-->
    <GetRestoreFrameworkReferencesTask Condition=" '$(PackageReferenceCompatibleProjectStyle)' == 'true' " ProjectUniqueName="$(MSBuildProjectFullPath)" FrameworkReferences="@(FrameworkReference)" TargetFrameworks="$(TargetFramework)">
      <Output TaskParameter="RestoreGraphItems" ItemName="_RestoreGraphEntry" />
    </GetRestoreFrameworkReferencesTask>
    <!-- Write out target framework information -->
    <ItemGroup Condition="  '$(PackageReferenceCompatibleProjectStyle)' == 'true'">
      <_RestoreGraphEntry Include="$([System.Guid]::NewGuid())">
        <Type>TargetFrameworkInformation</Type>
        <ProjectUniqueName>$(MSBuildProjectFullPath)</ProjectUniqueName>
        <PackageTargetFallback>$(PackageTargetFallback)</PackageTargetFallback>
        <AssetTargetFallback>$(AssetTargetFallback)</AssetTargetFallback>
        <TargetFramework>$(TargetFramework)</TargetFramework>
        <TargetFrameworkIdentifier>$(TargetFrameworkIdentifier)</TargetFrameworkIdentifier>
        <TargetFrameworkVersion>$(TargetFrameworkVersion)</TargetFrameworkVersion>
        <TargetFrameworkMoniker>$(TargetFrameworkMoniker)</TargetFrameworkMoniker>
        <TargetFrameworkProfile>$(TargetFrameworkProfile)</TargetFrameworkProfile>
        <TargetPlatformMoniker>$(TargetPlatformMoniker)</TargetPlatformMoniker>
        <TargetPlatformIdentifier>$(TargetPlatformIdentifier)</TargetPlatformIdentifier>
        <TargetPlatformVersion>$(TargetPlatformVersion)</TargetPlatformVersion>
        <TargetPlatformMinVersion>$(TargetPlatformMinVersion)</TargetPlatformMinVersion>
        <CLRSupport>$(CLRSupport)</CLRSupport>
        <RuntimeIdentifierGraphPath>$(RuntimeIdentifierGraphPath)</RuntimeIdentifierGraphPath>
        <WindowsTargetPlatformMinVersion>$(WindowsTargetPlatformMinVersion)</WindowsTargetPlatformMinVersion>
      </_RestoreGraphEntry>
    </ItemGroup>
  </Target>
  <!--
    ============================================================
    _GenerateRestoreProjectPathItemsCurrentProject
    Get absolute paths for all project references.
    ============================================================
  -->
  <Target Name="_GenerateRestoreProjectPathItemsCurrentProject" Condition=" '$(TargetFrameworks)' == '' " DependsOnTargets="_GenerateRestoreProjectPathItemsPerFramework" Returns="@(_RestoreProjectPathItems)" xmlns="http://schemas.microsoft.com/developer/msbuild/2003" />
  <!--
    ============================================================
    _GenerateRestoreProjectPathItemsPerFramework
    Get absolute paths for all project references.
    ============================================================
  -->
  <Target Name="_GenerateRestoreProjectPathItemsPerFramework" Returns="@(_RestoreProjectPathItems)" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <!-- Get the absolute paths to all projects -->
    <ConvertToAbsolutePath Paths="@(ProjectReference)">
      <Output TaskParameter="AbsolutePaths" PropertyName="_RestoreGraphAbsoluteProjectPaths" />
    </ConvertToAbsolutePath>
    <ItemGroup>
      <_RestoreProjectPathItems Include="$(_RestoreGraphAbsoluteProjectPaths)" />
    </ItemGroup>
  </Target>
  <!--
    ============================================================
    _GenerateRestoreProjectPathItems
    Get all project references regardless of framework
    ============================================================
  -->
  <Target Name="_GenerateRestoreProjectPathItems" DependsOnTargets="_GenerateRestoreProjectPathItemsAllFrameworks;_GenerateRestoreProjectPathItemsCurrentProject" Returns="@(_CurrentRestoreProjectPathItems)" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <!-- Drop any duplicate items -->
    <RemoveDuplicates Inputs="@(_RestoreProjectPathItems)">
      <Output TaskParameter="Filtered" ItemName="_CurrentRestoreProjectPathItems" />
    </RemoveDuplicates>
  </Target>
  <!--
    ============================================================
    _GenerateRestoreProjectPathItemsAllFrameworks
    Get all project references regardless of framework
    ============================================================
  -->
  <Target Name="_GenerateRestoreProjectPathItemsAllFrameworks" Condition=" '$(TargetFrameworks)' != '' " DependsOnTargets="_GetRestoreTargetFrameworksAsItems" Returns="@(_RestoreProjectPathItems)" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <!-- Get all project references for the current project  -->
    <!-- With SkipNonexistentTargets support -->
    <MSBuild Condition=" '$(RestoreUseSkipNonexistentTargets)' == 'true' " BuildInParallel="$(RestoreBuildInParallel)" Projects="$(MSBuildProjectFullPath)" Targets="_GenerateRestoreProjectPathItemsPerFramework" SkipNonexistentTargets="true" SkipNonexistentProjects="true" Properties="TargetFramework=%(_RestoreTargetFrameworkItems.Identity);&#xD;&#xA;                  $(_GenerateRestoreGraphProjectEntryInputProperties)">
      <Output TaskParameter="TargetOutputs" ItemName="_RestoreProjectPathItems" />
    </MSBuild>
    <!-- Without SkipNonexistentTargets support -->
    <MSBuild Condition=" '$(RestoreUseSkipNonexistentTargets)' != 'true' " Projects="$(MSBuildProjectFullPath)" Targets="_GenerateRestoreProjectPathItemsPerFramework" ContinueOnError="$(RestoreContinueOnError)" Properties="TargetFramework=%(_RestoreTargetFrameworkItems.Identity);&#xD;&#xA;                  $(_GenerateRestoreGraphProjectEntryInputProperties)">
      <Output TaskParameter="TargetOutputs" ItemName="_RestoreProjectPathItems" />
    </MSBuild>
  </Target>
  <!--
    ============================================================
    _GenerateRestoreProjectPathWalk
    Recursively walk projects
    ============================================================
  -->
  <Target Name="_GenerateRestoreProjectPathWalk" DependsOnTargets="_GenerateRestoreProjectPathItems" Returns="@(_RestoreProjectPathItems)" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <!-- Walk project references  -->
    <!-- With SkipNonexistentTargets -->
    <MSBuild Condition=" '$(RestoreUseSkipNonexistentTargets)' == 'true' " BuildInParallel="$(RestoreBuildInParallel)" Projects="@(_CurrentRestoreProjectPathItems)" Targets="_GenerateRestoreProjectPathWalk" SkipNonexistentTargets="true" SkipNonexistentProjects="true" Properties="$(_GenerateRestoreGraphProjectEntryInputProperties)">
      <Output TaskParameter="TargetOutputs" ItemName="_GenerateRestoreProjectPathWalkOutputs" />
    </MSBuild>
    <!-- Without SkipNonexistentTargets -->
    <MSBuild Condition=" '$(RestoreUseSkipNonexistentTargets)' != 'true' " Projects="@(_CurrentRestoreProjectPathItems)" Targets="_GenerateRestoreProjectPathWalk" ContinueOnError="$(RestoreContinueOnError)" Properties="$(_GenerateRestoreGraphProjectEntryInputProperties)">
      <Output TaskParameter="TargetOutputs" ItemName="_GenerateRestoreProjectPathWalkOutputs" />
    </MSBuild>
    <!-- Include the current project in the result -->
    <ItemGroup>
      <_GenerateRestoreProjectPathWalkOutputs Include="$(MSBuildProjectFullPath)" />
    </ItemGroup>
    <!-- Remove duplicates -->
    <RemoveDuplicates Inputs="@(_GenerateRestoreProjectPathWalkOutputs)">
      <Output TaskParameter="Filtered" ItemName="_RestoreProjectPathItems" />
    </RemoveDuplicates>
  </Target>
  <!--
    ============================================================
    _GetAllRestoreProjectPathItems
    Get the full list of known projects.
    This includes all child projects from all target frameworks.
    ============================================================
  -->
  <Target Name="_GetAllRestoreProjectPathItems" DependsOnTargets="_FilterRestoreGraphProjectInputItems" Returns="@(_RestoreProjectPathItems)" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <NuGetMessageTask Name="DeterminingProjectsToRestore" Importance="High" />
    <!-- Walk projects -->
    <!-- With SkipNonexistentTargets -->
    <MSBuild Condition=" '$(RestoreUseSkipNonexistentTargets)' == 'true' " BuildInParallel="$(RestoreBuildInParallel)" Projects="@(FilteredRestoreGraphProjectInputItems)" Targets="_GenerateRestoreProjectPathWalk" SkipNonexistentTargets="true" SkipNonexistentProjects="true" Properties="$(_GenerateRestoreGraphProjectEntryInputProperties)">
      <Output TaskParameter="TargetOutputs" ItemName="_RestoreProjectPathItemsOutputs" />
    </MSBuild>
    <!-- Without SkipNonexistentTargets -->
    <MSBuild Condition=" '$(RestoreUseSkipNonexistentTargets)' != 'true' " Projects="@(FilteredRestoreGraphProjectInputItems)" Targets="_GenerateRestoreProjectPathWalk" ContinueOnError="$(RestoreContinueOnError)" Properties="$(_GenerateRestoreGraphProjectEntryInputProperties)">
      <Output TaskParameter="TargetOutputs" ItemName="_RestoreProjectPathItemsOutputs" />
    </MSBuild>
    <!-- Remove duplicates -->
    <RemoveDuplicates Inputs="@(_RestoreProjectPathItemsOutputs)">
      <Output TaskParameter="Filtered" ItemName="_RestoreProjectPathItemsWithoutDupes" />
    </RemoveDuplicates>
    <!-- Remove projects that do not support restore. -->
    <!-- With SkipNonexistentTargets -->
    <MSBuild Condition=" '$(RestoreUseSkipNonexistentTargets)' == 'true' " BuildInParallel="$(RestoreBuildInParallel)" Projects="@(_RestoreProjectPathItemsWithoutDupes)" Targets="_IsProjectRestoreSupported" SkipNonexistentTargets="true" SkipNonexistentProjects="true" Properties="$(_GenerateRestoreGraphProjectEntryInputProperties)">
      <Output TaskParameter="TargetOutputs" ItemName="_RestoreProjectPathItems" />
    </MSBuild>
    <!-- Without SkipNonexistentTargets -->
    <MSBuild Condition=" '$(RestoreUseSkipNonexistentTargets)' != 'true' " Projects="@(_RestoreProjectPathItemsWithoutDupes)" Targets="_IsProjectRestoreSupported" ContinueOnError="$(RestoreContinueOnError)" Properties="$(_GenerateRestoreGraphProjectEntryInputProperties)">
      <Output TaskParameter="TargetOutputs" ItemName="_RestoreProjectPathItems" />
    </MSBuild>
  </Target>
  <!--
    ============================================================
    _GetRestoreSettingsOverrides
    Get global property overrides that should be resolved
    against the current working directory instead of the project.
    This is done by calling into NuGet.targets in a new scope,
    project properties will not be returned by the calls below.
    ============================================================
  -->
  <Target Name="_GetRestoreSettingsOverrides" Returns="$(_RestorePackagesPathOverride);$(_RestoreRepositoryPathOverride);$(_RestoreSourcesOverride);$(_RestoreFallbackFoldersOverride)" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <!-- RestorePackagesPathOverride -->
    <MSBuild BuildInParallel="$(RestoreBuildInParallel)" Condition=" '$(RestorePackagesPath)' != '' " Projects="$(MSBuildThisFileFullPath)" Targets="_GetRestorePackagesPathOverride">
      <Output TaskParameter="TargetOutputs" PropertyName="_RestorePackagesPathOverride" />
    </MSBuild>
    <!-- RestoreRepositoryPathOverride -->
    <MSBuild BuildInParallel="$(RestoreBuildInParallel)" Condition=" '$(RestoreRepositoryPathOverride)' != '' " Projects="$(MSBuildThisFileFullPath)" Targets="_GetRestoreRepositoryPathOverride">
      <Output TaskParameter="TargetOutputs" PropertyName="_RestoreRepositoryPathOverride" />
    </MSBuild>
    <!-- RestoreSourcesOverride -->
    <MSBuild BuildInParallel="$(RestoreBuildInParallel)" Condition=" '$(RestoreSources)' != '' " Projects="$(MSBuildThisFileFullPath)" Targets="_GetRestoreSourcesOverride">
      <Output TaskParameter="TargetOutputs" PropertyName="_RestoreSourcesOverride" />
    </MSBuild>
    <!-- RestoreFallbackFoldersOverride -->
    <MSBuild BuildInParallel="$(RestoreBuildInParallel)" Condition=" '$(RestoreFallbackFolders)' != '' " Projects="$(MSBuildThisFileFullPath)" Targets="_GetRestoreFallbackFoldersOverride">
      <Output TaskParameter="TargetOutputs" PropertyName="_RestoreFallbackFoldersOverride" />
    </MSBuild>
  </Target>
  <!--
    ============================================================
    _GetRestorePackagesPathOverride
    ============================================================
  -->
  <Target Name="_GetRestorePackagesPathOverride" Returns="$(_RestorePackagesPathOverride)" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <PropertyGroup>
      <_RestorePackagesPathOverride>$(RestorePackagesPath)</_RestorePackagesPathOverride>
    </PropertyGroup>
  </Target>
  <!--
    ============================================================
    _GetRestoreRepositoryPathOverride
    ============================================================
  -->
  <Target Name="_GetRestoreRepositoryPathOverride" Returns="$(_RestoreRepositoryPathOverride)" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <PropertyGroup>
      <_RestorePackagesPathOverride>$(RestoreRepositoryPath)</_RestorePackagesPathOverride>
    </PropertyGroup>
  </Target>
  <!--
    ============================================================
    _GetRestoreSourcesOverride
    ============================================================
  -->
  <Target Name="_GetRestoreSourcesOverride" Returns="$(_RestoreSourcesOverride)" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <PropertyGroup>
      <_RestoreSourcesOverride>$(RestoreSources)</_RestoreSourcesOverride>
    </PropertyGroup>
  </Target>
  <!--
    ============================================================
    _GetRestoreFallbackFoldersOverride
    ============================================================
  -->
  <Target Name="_GetRestoreFallbackFoldersOverride" Returns="$(_RestoreFallbackFoldersOverride)" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <PropertyGroup>
      <_RestoreFallbackFoldersOverride>$(RestoreFallbackFolders)</_RestoreFallbackFoldersOverride>
    </PropertyGroup>
  </Target>
  <!--
    ============================================================
    _GetRestoreTargetFrameworkOverride
    ============================================================
  -->
  <Target Name="_GetRestoreTargetFrameworkOverride" Condition=" '$(_DisableNuGetRestoreTargetFrameworksOverride)' != 'true' " Returns="$(_TargetFrameworkOverride)" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <GetGlobalPropertyValueTask PropertyName="TargetFramework" Condition=" '$(TargetFrameworks)' != '' ">
      <Output TaskParameter="GlobalPropertyValue" PropertyName="_TargetFrameworkOverride" />
    </GetGlobalPropertyValueTask>
    <!-- Only set the override if TargetFrameworks has not been overriden as well. In that case, prefer it. -->
  </Target>
  <!--
    ============================================================
    _GetTargetFrameworkOverrides
    ============================================================
  -->
  <Target Name="_GetTargetFrameworkOverrides" Returns="$(_TargetFrameworkOverride)" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <PropertyGroup>
      <_TargetFrameworkOverride Condition=" '$(TargetFrameworks)' == '' ">$(TargetFramework)</_TargetFrameworkOverride>
    </PropertyGroup>
  </Target>
  <!--
    ============================================================
    _IsProjectRestoreSupported
    Verify restore targets exist in the project.
    ============================================================
  -->
  <Target Name="_IsProjectRestoreSupported" Returns="@(_ValidProjectsForRestore)" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <ItemGroup>
      <_ValidProjectsForRestore Include="$(MSBuildProjectFullPath)" />
    </ItemGroup>
  </Target>
  <!--
    ============================================================
    Import NuGet.RestoreEx.targets if the MSBuild property 'RestoreEnableStaticGraph'
    is 'true'.  This file overrides the Restore target to use MSBuild Static Graph
    to load and evaluate projects which is much faster.

    This feature is not supported for NuGet.exe scenarios and NuGet.RestoreEx.targets
    won't exist in that case.
    ============================================================
  -->
  <!--
============================================================================================================================================
  <Import Project="NuGet.RestoreEx.targets" Condition="'$(RestoreUseStaticGraphEvaluation)' == 'true' And Exists('NuGet.RestoreEx.targets')">

C:\Program Files\dotnet\sdk\8.0.110\NuGet.RestoreEx.targets
============================================================================================================================================
-->
  <!--
***********************************************************************************************
NuGet.RestoreEx.targets

WARNING:  DO NOT MODIFY this file unless you are knowledgeable about MSBuild and have
          created a backup copy.  Incorrect changes to this file will make it
          impossible to load or build your projects from the command-line or the IDE.

Copyright (c) .NET Foundation. All rights reserved.
***********************************************************************************************
-->
  <UsingTask TaskName="NuGet.Build.Tasks.RestoreTaskEx" AssemblyFile="$(RestoreTaskAssemblyFile)" xmlns="http://schemas.microsoft.com/developer/msbuild/2003" />
  <UsingTask TaskName="NuGet.Build.Tasks.GenerateRestoreGraphFileTask" AssemblyFile="$(RestoreTaskAssemblyFile)" xmlns="http://schemas.microsoft.com/developer/msbuild/2003" />
  <Target Name="Restore" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <!-- Restore using MSBuild's Static Graph Evaluation -->
    <RestoreTaskEx CleanupAssetsForUnsupportedProjects="$([MSBuild]::ValueOrDefault('$(RestoreCleanupAssetsForUnsupportedProjects)', 'true'))" DisableParallel="$(RestoreDisableParallel)" Force="$(RestoreForce)" ForceEvaluate="$(RestoreForceEvaluate)" HideWarningsAndErrors="$(HideWarningsAndErrors)" IgnoreFailedSources="$(RestoreIgnoreFailedSources)" Interactive="$([MSBuild]::ValueOrDefault('$(NuGetInteractive)', '$(MSBuildInteractive)'))" MSBuildBinPath="$(MSBuildBinPath)" NoCache="$(RestoreNoCache)" ProjectFullPath="$(MSBuildProjectFullPath)" Recursive="$([MSBuild]::ValueOrDefault('$(RestoreRecursive)', 'true'))" RestorePackagesConfig="$(RestorePackagesConfig)" SolutionPath="$(SolutionPath)" ProcessFileName="$(NuGetConsoleProcessFileName)" MSBuildStartupDirectory="$(MSBuildStartupDirectory)">
      <Output TaskParameter="EmbedInBinlog" ItemName="EmbedInBinlog" />
    </RestoreTaskEx>
  </Target>
  <!--
    ============================================================
    GenerateRestoreGraphFile
    Writes the output the dg spec generation by static graph restore.
    ============================================================
  -->
  <Target Name="GenerateRestoreGraphFile" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <Error Condition="$(RestoreGraphOutputPath) == ''" Text="Missing RestoreGraphOutputPath property!" />
    <GenerateRestoreGraphFileTask MSBuildBinPath="$(MSBuildBinPath)" ProjectFullPath="$(MSBuildProjectFullPath)" Recursive="$([MSBuild]::ValueOrDefault('$(RestoreRecursive)', 'true'))" SolutionPath="$(SolutionPath)" RestoreGraphOutputPath="$(RestoreGraphOutputPath)" ProcessFileName="$(NuGetConsoleProcessFileName)" MSBuildStartupDirectory="$(MSBuildStartupDirectory)" />
  </Target>
  <!--
============================================================================================================================================
  </Import>

C:\Program Files\dotnet\sdk\8.0.110\NuGet.targets
============================================================================================================================================
-->
  <!--
============================================================================================================================================
  </Import>

C:\Program Files\dotnet\sdk\8.0.110\Microsoft.Common.CrossTargeting.targets
============================================================================================================================================
-->
  <!--<Import Project="$(CustomAfterMicrosoftCommonCrossTargetingTargets)" Condition="'$(CustomAfterMicrosoftCommonCrossTargetingTargets)' != '' and Exists('$(CustomAfterMicrosoftCommonCrossTargetingTargets)')" />-->
  <!--
    Allow extensions like NuGet restore to work before any package assets are available.
  -->
  <PropertyGroup>
    <ImportByWildcardAfterMicrosoftCommonCrossTargetingTargets Condition="'$(ImportByWildcardAfterMicrosoftCommonCrossTargetingTargets)' == ''">true</ImportByWildcardAfterMicrosoftCommonCrossTargetingTargets>
  </PropertyGroup>
  <!--
============================================================================================================================================
  <Import Project="$(MSBuildExtensionsPath)\$(MSBuildToolsVersion)\Microsoft.Common.CrossTargeting.targets\ImportAfter\*.targets" Condition="'$(ImportByWildcardAfterMicrosoftCommonCrossTargetingTargets)' == 'true' and exists('$(MSBuildExtensionsPath)\$(MSBuildToolsVersion)\Microsoft.Common.CrossTargeting.targets\ImportAfter')">

C:\Program Files\dotnet\sdk\8.0.110\Current\Microsoft.Common.CrossTargeting.targets\ImportAfter\Microsoft.TestPlatform.CrossTargeting.targets
============================================================================================================================================
-->
  <!--
***********************************************************************************************
Microsoft.TestPlatform.CrossTargeting.targets

WARNING:  DO NOT MODIFY this file unless you are knowledgeable about MSBuild and have
          created a backup copy.  Incorrect changes to this file will make it
          impossible to load or build your projects from the command-line or the IDE.

Copyright (c) .NET Foundation. All rights reserved.
***********************************************************************************************
-->
  <PropertyGroup xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <VSTestTaskAssemblyFile Condition="$(VSTestTaskAssemblyFile) == ''">$(MSBuildExtensionsPath)\Microsoft.TestPlatform.Build.dll</VSTestTaskAssemblyFile>
    <MSBuildAllProjects>$(MSBuildAllProjects);$(MSBuildThisFileFullPath)</MSBuildAllProjects>
  </PropertyGroup>
  <UsingTask TaskName="Microsoft.TestPlatform.Build.Tasks.VSTestLogsTask" AssemblyFile="$(VSTestTaskAssemblyFile)" xmlns="http://schemas.microsoft.com/developer/msbuild/2003" />
  <!--
  ===================================================================================
               DispatchToInnerBuildsWithVSTestTarget

     Builds this project with /t:$(InnerVSTestTargets) /p:TargetFramework=X for each
     value X in $(TargetFrameworks)

     [IN]
     $(TargetFrameworks) - Semicolon delimited list of target frameworks.
     $(InnerVSTestTargets) - The targets to build for each target framework

     [OUT]
     @(InnerOutput) - The combined output items of inner targets across
                      all target frameworks..

  ===================================================================================
  -->
  <Target Name="DispatchToInnerBuildsWithVSTestTarget" Returns="@(InnerOutput)" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <ItemGroup>
      <_TargetFramework Include="$(TargetFrameworks)" />
    </ItemGroup>
    <MSBuild Projects="$(MSBuildProjectFile)" Condition="'$(TargetFrameworks)' != '' " Targets="$(InnerVSTestTargets)" Properties="TargetFramework=%(_TargetFramework.Identity);VSTestNoBuild=true" ContinueOnError="ErrorAndContinue">
      <Output ItemName="InnerOutput" TaskParameter="TargetOutputs" />
    </MSBuild>
  </Target>
  <!--
  ==================================================================================
                                   VSTest

   Cross-targeting version of VSTest.

   [IN]
   $(TargetFrameworks) - Semicolon delimited list of target frameworks.
   $(InnerVSTestTargets) - The targets to build for each target framework. Defaults
                         to 'VSTest' if unset, but allows override to support
                         `msbuild /p:InnerTargets=X;Y;Z` which will build X, Y,
                         and Z targets for each target framework.

   [OUT]
   @(InnerOutput) - The combined output items of the inner targets across
                    all builds.
  =================================================================================
  -->
  <Target Name="VSTest" DependsOnTargets="_ComputeTargetFrameworkItems" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <CallTarget Condition="'$(VSTestNoBuild)' != 'true'" Targets="BuildProject" />
    <CallTarget Targets="SetVSTestInnerTarget" />
    <CallTarget Targets="DispatchToInnerBuildsWithVSTestTarget" />
  </Target>
  <Target Name="BuildProject" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <Microsoft.TestPlatform.Build.Tasks.VSTestLogsTask LogType="BuildStarted" />
    <CallTarget Targets="Build" />
    <Microsoft.TestPlatform.Build.Tasks.VSTestLogsTask LogType="BuildCompleted" />
  </Target>
  <Target Name="SetVSTestInnerTarget" Returns="@(InnerOutput)" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <PropertyGroup Condition="'$(InnerVSTestTargets)' == ''">
      <InnerVSTestTargets>VSTest</InnerVSTestTargets>
    </PropertyGroup>
  </Target>
  <!--
============================================================================================================================================
  </Import>

C:\Program Files\dotnet\sdk\8.0.110\Microsoft.Common.CrossTargeting.targets
============================================================================================================================================
-->
  <!--
    Import project extensions which usually come from packages.  Package management systems will create a file at:
      $(MSBuildProjectExtensionsPath)\$(MSBuildProjectFile).<SomethingUnique>.targets

    Each package management system should use a unique moniker to avoid collisions.  It is a wild-card iport so the package
    management system can write out multiple files but the order of the import is alphabetic because MSBuild sorts the list.

    This is the same import that would happen in an inner (non-cross targeting) build. Package management systems are responsible for generating
    appropriate conditions based on $(IsCrossTargetingBuild) to pull in only those package targets that are meant to participate in a cross-targeting
    build.
  -->
  <PropertyGroup>
    <ImportProjectExtensionTargets Condition="'$(ImportProjectExtensionTargets)' == ''">true</ImportProjectExtensionTargets>
  </PropertyGroup>
  <!--
============================================================================================================================================
  <Import Project="$(MSBuildProjectExtensionsPath)$(MSBuildProjectFile).*.targets" Condition="'$(ImportProjectExtensionTargets)' == 'true' and exists('$(MSBuildProjectExtensionsPath)')">

C:\Users\calope\source\repos\runtime8\artifacts\obj\System.IO.Ports.Tests\System.IO.Ports.Tests.csproj.nuget.g.targets
============================================================================================================================================
-->
  <!--<ImportGroup Condition=" '$(TargetFramework)' == '' AND '$(ExcludeRestorePackageImports)' != 'true' ">-->
  <!--
============================================================================================================================================
  <Import Project="$(NuGetPackageRoot)microsoft.dotnet.build.tasks.targetframework\8.0.0-beta.24426.2\buildMultiTargeting\Microsoft.DotNet.Build.Tasks.TargetFramework.targets" Condition="Exists('$(NuGetPackageRoot)microsoft.dotnet.build.tasks.targetframework\8.0.0-beta.24426.2\buildMultiTargeting\Microsoft.DotNet.Build.Tasks.TargetFramework.targets')">

C:\.tools\.nuget\packages\microsoft.dotnet.build.tasks.targetframework\8.0.0-beta.24426.2\buildMultiTargeting\Microsoft.DotNet.Build.Tasks.TargetFramework.targets
============================================================================================================================================
-->
  <!-- Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. -->
  <UsingTask TaskName="ChooseBestTargetFrameworksTask" AssemblyFile="$(DotNetBuildTasksTargetFrameworkAssembly)" />
  <!-- We filter _InnerBuildProjects items during DispatchToInnerBuilds and Clean to only run for best target frameworks. -->
  <Target Name="RunOnlyBestTargetFrameworks" Condition="'$(BuildTargetFramework)' != ''" BeforeTargets="DispatchToInnerBuilds;Clean" DependsOnTargets="GetProjectWithBestTargetFrameworks">
    <ItemGroup>
      <_OriginalInnerBuildProjects Include="@(_InnerBuildProjects)" />
      <_InnerBuildProjects Remove="@(_InnerBuildProjects)" />
      <_InnerBuildProjects Include="@(InnerBuildProjectsWithBestTargetFramework)" />
    </ItemGroup>
  </Target>
  <!-- As _InnerBuildProjects items are used in the GetTargetFrameworks path as well (>= .NET 5), we restore the item state. -->
  <Target Name="RestoreInnerBuildProjects" BeforeTargets="GetTargetFrameworksWithPlatformFromInnerBuilds" AfterTargets="DispatchToInnerBuilds;Clean" Condition="'@(_OriginalInnerBuildProjects)' != ''">
    <ItemGroup>
      <_InnerBuildProjects Remove="@(_InnerBuildProjects)" />
      <_InnerBuildProjects Include="@(_OriginalInnerBuildProjects)" />
    </ItemGroup>
  </Target>
  <Target Name="GetProjectWithBestTargetFrameworks">
    <ItemGroup>
      <_BuildTargetFramework Include="$(BuildTargetFramework)" Condition="'$(BuildTargetFramework)' != ''" />
      <_BuildTargetFramework Include="$([MSBuild]::Unescape($([System.Text.RegularExpressions.Regex]::Replace('$(TargetFrameworks)', '(-[^;]+)', ''))))" Condition="'$(BuildTargetFramework)' == ''" />
    </ItemGroup>
    <ItemGroup>
      <_BuildTargetFrameworkWithTargetOS Include="@(_BuildTargetFramework-&gt;Distinct()-&gt;'%(Identity)-$(TargetOS)')" Condition="!$([System.String]::Copy('%(Identity)').StartsWith('net4'))" />
      <_BuildTargetFrameworkWithTargetOS Include="@(_BuildTargetFramework-&gt;Distinct())" Condition="$([System.String]::Copy('%(Identity)').StartsWith('net4'))" />
    </ItemGroup>
    <ChooseBestTargetFrameworksTask BuildTargetFrameworks="@(_BuildTargetFrameworkWithTargetOS);$(AdditionalBuildTargetFrameworks)" SupportedTargetFrameworks="$(TargetFrameworks)" RuntimeGraph="$([MSBuild]::ValueOrDefault('$(RuntimeIdentifierGraphPath)', '$(BundledRuntimeIdentifierGraphFile)'))" Distinct="true">
      <Output TaskParameter="BestTargetFrameworks" ItemName="_BestTargetFramework" />
    </ChooseBestTargetFrameworksTask>
    <!-- Create inner build project nodes with the TargetFramework supplied via AdditionalProperties. -->
    <ItemGroup>
      <_BestTargetFramework Project="$(MSBuildProjectFile)" />
      <InnerBuildProjectsWithBestTargetFramework Include="%(_BestTargetFramework.Project)" AdditionalProperties="TargetFramework=%(Identity)" />
    </ItemGroup>
  </Target>
  <!--
============================================================================================================================================
  </Import>

C:\Users\calope\source\repos\runtime8\artifacts\obj\System.IO.Ports.Tests\System.IO.Ports.Tests.csproj.nuget.g.targets
============================================================================================================================================
-->
  <!--</ImportGroup>-->
  <!--<ImportGroup Condition=" '$(TargetFramework)' == 'net462' AND '$(ExcludeRestorePackageImports)' != 'true' ">-->
  <!--<Import Project="$(NuGetPackageRoot)microsoft.dotnet.build.tasks.targetframework\8.0.0-beta.24426.2\build\Microsoft.DotNet.Build.Tasks.TargetFramework.targets" Condition="Exists('$(NuGetPackageRoot)microsoft.dotnet.build.tasks.targetframework\8.0.0-beta.24426.2\build\Microsoft.DotNet.Build.Tasks.TargetFramework.targets')" />-->
  <!--<Import Project="$(NuGetPackageRoot)microsoft.dotnet.codeanalysis\8.0.0-beta.24426.2\build\Microsoft.DotNet.CodeAnalysis.targets" Condition="Exists('$(NuGetPackageRoot)microsoft.dotnet.codeanalysis\8.0.0-beta.24426.2\build\Microsoft.DotNet.CodeAnalysis.targets')" />-->
  <!--<Import Project="$(NuGetPackageRoot)microsoft.codeanalysis.netanalyzers\8.0.0-preview.23614.1\buildTransitive\Microsoft.CodeAnalysis.NetAnalyzers.targets" Condition="Exists('$(NuGetPackageRoot)microsoft.codeanalysis.netanalyzers\8.0.0-preview.23614.1\buildTransitive\Microsoft.CodeAnalysis.NetAnalyzers.targets')" />-->
  <!--<Import Project="$(NuGetPackageRoot)microsoft.codeanalysis.csharp.codestyle\4.5.0\build\Microsoft.CodeAnalysis.CSharp.CodeStyle.targets" Condition="Exists('$(NuGetPackageRoot)microsoft.codeanalysis.csharp.codestyle\4.5.0\build\Microsoft.CodeAnalysis.CSharp.CodeStyle.targets')" />-->
  <!--<Import Project="$(NuGetPackageRoot)microsoft.codecoverage\17.4.0-preview-20220707-01\build\netstandard1.0\Microsoft.CodeCoverage.targets" Condition="Exists('$(NuGetPackageRoot)microsoft.codecoverage\17.4.0-preview-20220707-01\build\netstandard1.0\Microsoft.CodeCoverage.targets')" />-->
  <!--<Import Project="$(NuGetPackageRoot)microsoft.net.test.sdk\17.4.0-preview-20220707-01\build\net45\Microsoft.NET.Test.Sdk.targets" Condition="Exists('$(NuGetPackageRoot)microsoft.net.test.sdk\17.4.0-preview-20220707-01\build\net45\Microsoft.NET.Test.Sdk.targets')" />-->
  <!--<Import Project="$(NuGetPackageRoot)coverlet.collector\6.0.0\build\netstandard1.0\coverlet.collector.targets" Condition="Exists('$(NuGetPackageRoot)coverlet.collector\6.0.0\build\netstandard1.0\coverlet.collector.targets')" />-->
  <!--</ImportGroup>-->
  <!--<ImportGroup Condition=" '$(TargetFramework)' == 'net8.0-freebsd' AND '$(ExcludeRestorePackageImports)' != 'true' ">-->
  <!--<Import Project="$(NuGetPackageRoot)netstandard.library\2.0.3\build\netstandard2.0\NETStandard.Library.targets" Condition="Exists('$(NuGetPackageRoot)netstandard.library\2.0.3\build\netstandard2.0\NETStandard.Library.targets')" />-->
  <!--<Import Project="$(NuGetPackageRoot)microsoft.dotnet.build.tasks.targetframework\8.0.0-beta.24426.2\build\Microsoft.DotNet.Build.Tasks.TargetFramework.targets" Condition="Exists('$(NuGetPackageRoot)microsoft.dotnet.build.tasks.targetframework\8.0.0-beta.24426.2\build\Microsoft.DotNet.Build.Tasks.TargetFramework.targets')" />-->
  <!--<Import Project="$(NuGetPackageRoot)microsoft.dotnet.codeanalysis\8.0.0-beta.24426.2\build\Microsoft.DotNet.CodeAnalysis.targets" Condition="Exists('$(NuGetPackageRoot)microsoft.dotnet.codeanalysis\8.0.0-beta.24426.2\build\Microsoft.DotNet.CodeAnalysis.targets')" />-->
  <!--<Import Project="$(NuGetPackageRoot)microsoft.codeanalysis.netanalyzers\8.0.0-preview.23614.1\buildTransitive\Microsoft.CodeAnalysis.NetAnalyzers.targets" Condition="Exists('$(NuGetPackageRoot)microsoft.codeanalysis.netanalyzers\8.0.0-preview.23614.1\buildTransitive\Microsoft.CodeAnalysis.NetAnalyzers.targets')" />-->
  <!--<Import Project="$(NuGetPackageRoot)microsoft.codeanalysis.csharp.codestyle\4.5.0\build\Microsoft.CodeAnalysis.CSharp.CodeStyle.targets" Condition="Exists('$(NuGetPackageRoot)microsoft.codeanalysis.csharp.codestyle\4.5.0\build\Microsoft.CodeAnalysis.CSharp.CodeStyle.targets')" />-->
  <!--<Import Project="$(NuGetPackageRoot)microsoft.codecoverage\17.4.0-preview-20220707-01\build\netstandard1.0\Microsoft.CodeCoverage.targets" Condition="Exists('$(NuGetPackageRoot)microsoft.codecoverage\17.4.0-preview-20220707-01\build\netstandard1.0\Microsoft.CodeCoverage.targets')" />-->
  <!--<Import Project="$(NuGetPackageRoot)microsoft.net.test.sdk\17.4.0-preview-20220707-01\build\netcoreapp2.1\Microsoft.NET.Test.Sdk.targets" Condition="Exists('$(NuGetPackageRoot)microsoft.net.test.sdk\17.4.0-preview-20220707-01\build\netcoreapp2.1\Microsoft.NET.Test.Sdk.targets')" />-->
  <!--<Import Project="$(NuGetPackageRoot)coverlet.collector\6.0.0\build\netstandard1.0\coverlet.collector.targets" Condition="Exists('$(NuGetPackageRoot)coverlet.collector\6.0.0\build\netstandard1.0\coverlet.collector.targets')" />-->
  <!--</ImportGroup>-->
  <!--<ImportGroup Condition=" '$(TargetFramework)' == 'net8.0-linux' AND '$(ExcludeRestorePackageImports)' != 'true' ">-->
  <!--<Import Project="$(NuGetPackageRoot)netstandard.library\2.0.3\build\netstandard2.0\NETStandard.Library.targets" Condition="Exists('$(NuGetPackageRoot)netstandard.library\2.0.3\build\netstandard2.0\NETStandard.Library.targets')" />-->
  <!--<Import Project="$(NuGetPackageRoot)microsoft.dotnet.build.tasks.targetframework\8.0.0-beta.24426.2\build\Microsoft.DotNet.Build.Tasks.TargetFramework.targets" Condition="Exists('$(NuGetPackageRoot)microsoft.dotnet.build.tasks.targetframework\8.0.0-beta.24426.2\build\Microsoft.DotNet.Build.Tasks.TargetFramework.targets')" />-->
  <!--<Import Project="$(NuGetPackageRoot)microsoft.dotnet.codeanalysis\8.0.0-beta.24426.2\build\Microsoft.DotNet.CodeAnalysis.targets" Condition="Exists('$(NuGetPackageRoot)microsoft.dotnet.codeanalysis\8.0.0-beta.24426.2\build\Microsoft.DotNet.CodeAnalysis.targets')" />-->
  <!--<Import Project="$(NuGetPackageRoot)microsoft.codeanalysis.netanalyzers\8.0.0-preview.23614.1\buildTransitive\Microsoft.CodeAnalysis.NetAnalyzers.targets" Condition="Exists('$(NuGetPackageRoot)microsoft.codeanalysis.netanalyzers\8.0.0-preview.23614.1\buildTransitive\Microsoft.CodeAnalysis.NetAnalyzers.targets')" />-->
  <!--<Import Project="$(NuGetPackageRoot)microsoft.codeanalysis.csharp.codestyle\4.5.0\build\Microsoft.CodeAnalysis.CSharp.CodeStyle.targets" Condition="Exists('$(NuGetPackageRoot)microsoft.codeanalysis.csharp.codestyle\4.5.0\build\Microsoft.CodeAnalysis.CSharp.CodeStyle.targets')" />-->
  <!--<Import Project="$(NuGetPackageRoot)microsoft.codecoverage\17.4.0-preview-20220707-01\build\netstandard1.0\Microsoft.CodeCoverage.targets" Condition="Exists('$(NuGetPackageRoot)microsoft.codecoverage\17.4.0-preview-20220707-01\build\netstandard1.0\Microsoft.CodeCoverage.targets')" />-->
  <!--<Import Project="$(NuGetPackageRoot)microsoft.net.test.sdk\17.4.0-preview-20220707-01\build\netcoreapp2.1\Microsoft.NET.Test.Sdk.targets" Condition="Exists('$(NuGetPackageRoot)microsoft.net.test.sdk\17.4.0-preview-20220707-01\build\netcoreapp2.1\Microsoft.NET.Test.Sdk.targets')" />-->
  <!--<Import Project="$(NuGetPackageRoot)coverlet.collector\6.0.0\build\netstandard1.0\coverlet.collector.targets" Condition="Exists('$(NuGetPackageRoot)coverlet.collector\6.0.0\build\netstandard1.0\coverlet.collector.targets')" />-->
  <!--</ImportGroup>-->
  <!--<ImportGroup Condition=" '$(TargetFramework)' == 'net8.0-osx' AND '$(ExcludeRestorePackageImports)' != 'true' ">-->
  <!--<Import Project="$(NuGetPackageRoot)netstandard.library\2.0.3\build\netstandard2.0\NETStandard.Library.targets" Condition="Exists('$(NuGetPackageRoot)netstandard.library\2.0.3\build\netstandard2.0\NETStandard.Library.targets')" />-->
  <!--<Import Project="$(NuGetPackageRoot)microsoft.dotnet.build.tasks.targetframework\8.0.0-beta.24426.2\build\Microsoft.DotNet.Build.Tasks.TargetFramework.targets" Condition="Exists('$(NuGetPackageRoot)microsoft.dotnet.build.tasks.targetframework\8.0.0-beta.24426.2\build\Microsoft.DotNet.Build.Tasks.TargetFramework.targets')" />-->
  <!--<Import Project="$(NuGetPackageRoot)microsoft.dotnet.codeanalysis\8.0.0-beta.24426.2\build\Microsoft.DotNet.CodeAnalysis.targets" Condition="Exists('$(NuGetPackageRoot)microsoft.dotnet.codeanalysis\8.0.0-beta.24426.2\build\Microsoft.DotNet.CodeAnalysis.targets')" />-->
  <!--<Import Project="$(NuGetPackageRoot)microsoft.codeanalysis.netanalyzers\8.0.0-preview.23614.1\buildTransitive\Microsoft.CodeAnalysis.NetAnalyzers.targets" Condition="Exists('$(NuGetPackageRoot)microsoft.codeanalysis.netanalyzers\8.0.0-preview.23614.1\buildTransitive\Microsoft.CodeAnalysis.NetAnalyzers.targets')" />-->
  <!--<Import Project="$(NuGetPackageRoot)microsoft.codeanalysis.csharp.codestyle\4.5.0\build\Microsoft.CodeAnalysis.CSharp.CodeStyle.targets" Condition="Exists('$(NuGetPackageRoot)microsoft.codeanalysis.csharp.codestyle\4.5.0\build\Microsoft.CodeAnalysis.CSharp.CodeStyle.targets')" />-->
  <!--<Import Project="$(NuGetPackageRoot)microsoft.codecoverage\17.4.0-preview-20220707-01\build\netstandard1.0\Microsoft.CodeCoverage.targets" Condition="Exists('$(NuGetPackageRoot)microsoft.codecoverage\17.4.0-preview-20220707-01\build\netstandard1.0\Microsoft.CodeCoverage.targets')" />-->
  <!--<Import Project="$(NuGetPackageRoot)microsoft.net.test.sdk\17.4.0-preview-20220707-01\build\netcoreapp2.1\Microsoft.NET.Test.Sdk.targets" Condition="Exists('$(NuGetPackageRoot)microsoft.net.test.sdk\17.4.0-preview-20220707-01\build\netcoreapp2.1\Microsoft.NET.Test.Sdk.targets')" />-->
  <!--<Import Project="$(NuGetPackageRoot)coverlet.collector\6.0.0\build\netstandard1.0\coverlet.collector.targets" Condition="Exists('$(NuGetPackageRoot)coverlet.collector\6.0.0\build\netstandard1.0\coverlet.collector.targets')" />-->
  <!--</ImportGroup>-->
  <!--<ImportGroup Condition=" '$(TargetFramework)' == 'net8.0-windows' AND '$(ExcludeRestorePackageImports)' != 'true' ">-->
  <!--<Import Project="$(NuGetPackageRoot)netstandard.library\2.0.3\build\netstandard2.0\NETStandard.Library.targets" Condition="Exists('$(NuGetPackageRoot)netstandard.library\2.0.3\build\netstandard2.0\NETStandard.Library.targets')" />-->
  <!--<Import Project="$(NuGetPackageRoot)microsoft.dotnet.build.tasks.targetframework\8.0.0-beta.24426.2\build\Microsoft.DotNet.Build.Tasks.TargetFramework.targets" Condition="Exists('$(NuGetPackageRoot)microsoft.dotnet.build.tasks.targetframework\8.0.0-beta.24426.2\build\Microsoft.DotNet.Build.Tasks.TargetFramework.targets')" />-->
  <!--<Import Project="$(NuGetPackageRoot)microsoft.dotnet.codeanalysis\8.0.0-beta.24426.2\build\Microsoft.DotNet.CodeAnalysis.targets" Condition="Exists('$(NuGetPackageRoot)microsoft.dotnet.codeanalysis\8.0.0-beta.24426.2\build\Microsoft.DotNet.CodeAnalysis.targets')" />-->
  <!--<Import Project="$(NuGetPackageRoot)microsoft.codeanalysis.netanalyzers\8.0.0-preview.23614.1\buildTransitive\Microsoft.CodeAnalysis.NetAnalyzers.targets" Condition="Exists('$(NuGetPackageRoot)microsoft.codeanalysis.netanalyzers\8.0.0-preview.23614.1\buildTransitive\Microsoft.CodeAnalysis.NetAnalyzers.targets')" />-->
  <!--<Import Project="$(NuGetPackageRoot)microsoft.codeanalysis.csharp.codestyle\4.5.0\build\Microsoft.CodeAnalysis.CSharp.CodeStyle.targets" Condition="Exists('$(NuGetPackageRoot)microsoft.codeanalysis.csharp.codestyle\4.5.0\build\Microsoft.CodeAnalysis.CSharp.CodeStyle.targets')" />-->
  <!--<Import Project="$(NuGetPackageRoot)microsoft.codecoverage\17.4.0-preview-20220707-01\build\netstandard1.0\Microsoft.CodeCoverage.targets" Condition="Exists('$(NuGetPackageRoot)microsoft.codecoverage\17.4.0-preview-20220707-01\build\netstandard1.0\Microsoft.CodeCoverage.targets')" />-->
  <!--<Import Project="$(NuGetPackageRoot)microsoft.net.test.sdk\17.4.0-preview-20220707-01\build\netcoreapp2.1\Microsoft.NET.Test.Sdk.targets" Condition="Exists('$(NuGetPackageRoot)microsoft.net.test.sdk\17.4.0-preview-20220707-01\build\netcoreapp2.1\Microsoft.NET.Test.Sdk.targets')" />-->
  <!--<Import Project="$(NuGetPackageRoot)coverlet.collector\6.0.0\build\netstandard1.0\coverlet.collector.targets" Condition="Exists('$(NuGetPackageRoot)coverlet.collector\6.0.0\build\netstandard1.0\coverlet.collector.targets')" />-->
  <!--</ImportGroup>-->
  <!--
============================================================================================================================================
  </Import>

C:\Program Files\dotnet\sdk\8.0.110\Microsoft.Common.CrossTargeting.targets
============================================================================================================================================
-->
  <PropertyGroup>
    <ImportDirectoryBuildTargets Condition="'$(ImportDirectoryBuildTargets)' == ''">true</ImportDirectoryBuildTargets>
  </PropertyGroup>
  <!--
        Determine the path to the directory build targets file if the user did not disable $(ImportDirectoryBuildTargets) and
        they did not already specify an absolute path to use via $(DirectoryBuildTargetsPath)
    -->
  <PropertyGroup Condition="'$(ImportDirectoryBuildTargets)' == 'true' and '$(DirectoryBuildTargetsPath)' == ''">
    <_DirectoryBuildTargetsFile Condition="'$(_DirectoryBuildTargetsFile)' == ''">Directory.Build.targets</_DirectoryBuildTargetsFile>
    <_DirectoryBuildTargetsBasePath Condition="'$(_DirectoryBuildTargetsBasePath)' == ''">$([MSBuild]::GetDirectoryNameOfFileAbove($(MSBuildProjectDirectory), '$(_DirectoryBuildTargetsFile)'))</_DirectoryBuildTargetsBasePath>
    <DirectoryBuildTargetsPath Condition="'$(_DirectoryBuildTargetsBasePath)' != '' and '$(_DirectoryBuildTargetsFile)' != ''">$([System.IO.Path]::Combine('$(_DirectoryBuildTargetsBasePath)', '$(_DirectoryBuildTargetsFile)'))</DirectoryBuildTargetsPath>
  </PropertyGroup>
  <!--
============================================================================================================================================
  <Import Project="$(DirectoryBuildTargetsPath)" Condition="'$(ImportDirectoryBuildTargets)' == 'true' and exists('$(DirectoryBuildTargetsPath)')">

C:\Users\calope\source\repos\runtime8\src\libraries\Directory.Build.targets
============================================================================================================================================
-->
  <PropertyGroup>
    <!-- Override strong name key to default to Open for test projects,
         Tests which wish to control this should set TestStrongNameKeyId. -->
    <TestStrongNameKeyId Condition="'$(TestStrongNameKeyId)' == '' and $(MSBuildProjectName.StartsWith('Microsoft.Extensions.'))">MicrosoftAspNetCore</TestStrongNameKeyId>
    <TestStrongNameKeyId Condition="'$(TestStrongNameKeyId)' == ''">Open</TestStrongNameKeyId>
    <StrongNameKeyId Condition="'$(IsTestProject)' == 'true' or '$(IsTestSupportProject)' == 'true'">$(TestStrongNameKeyId)</StrongNameKeyId>
  </PropertyGroup>
  <!-- Need to be defined before packaging.targets is imported. -->
  <PropertyGroup>
    <!-- The source of truth for these IsNETCoreApp* properties is NetCoreAppLibrary.props. -->
    <IsNETCoreAppSrc Condition="'$(IsSourceProject)' == 'true' and&#xD;&#xA;                                $(NetCoreAppLibrary.Contains('$(AssemblyName);'))">true</IsNETCoreAppSrc>
    <IsNETCoreAppRef Condition="('$(IsReferenceAssemblyProject)' == 'true' or '$(IsRuntimeAndReferenceAssembly)' == 'true') and&#xD;&#xA;                                $(NetCoreAppLibrary.Contains('$(AssemblyName);')) and&#xD;&#xA;                                '$(IsPrivateAssembly)' != 'true'">true</IsNETCoreAppRef>
    <IsNETCoreAppAnalyzer Condition="'$(IsGeneratorProject)' == 'true' and&#xD;&#xA;                                     $(NetCoreAppLibraryGenerator.Contains('$(MSBuildProjectName);'))">true</IsNETCoreAppAnalyzer>
  </PropertyGroup>
  <!-- resources.targets need to be imported before the Arcade SDK. -->
  <!--
============================================================================================================================================
  <Import Project="$(RepositoryEngineeringDir)resources.targets">

C:\Users\calope\source\repos\runtime8\eng\resources.targets
============================================================================================================================================
-->
  <PropertyGroup>
    <StringResourcesPath Condition="'$(StringResourcesPath)' == '' and Exists('$(MSBuildProjectDirectory)\Resources\Strings.resx')">$(MSBuildProjectDirectory)\Resources\Strings.resx</StringResourcesPath>
    <StringResourcesNamespace Condition="'$(StringResourcesNamespace)' == ''">System</StringResourcesNamespace>
    <StringResourcesClassName Condition="'$(StringResourcesClassName)' == ''">SR</StringResourcesClassName>
    <StringResourcesName Condition="'$(StringResourcesName)' == ''">FxResources.$(AssemblyName).$(StringResourcesClassName)</StringResourcesName>
    <!-- We define our own implementation of GetResourceString -->
    <GenerateResxSourceOmitGetResourceString>true</GenerateResxSourceOmitGetResourceString>
    <!-- For debug builds we include the full value string so that we get actual resources, even in the case the toolchain strips the resources file -->
    <GenerateResxSourceIncludeDefaultValues Condition="'$(Configuration)' == 'Debug'">true</GenerateResxSourceIncludeDefaultValues>
  </PropertyGroup>
  <!-- Include files under StringResourcesPath by convention unless OmitResources is set. -->
  <ItemGroup Condition="'$(StringResourcesPath)' != '' and '$(OmitResources)' != 'true'">
    <!-- Delete the embedded resource item pointing to StringResourcesPath in case the
         EnableDefaultEmbeddedResourceItems glob didn't include it and include it again. -->
    <EmbeddedResource Remove="$(StringResourcesPath)" Condition="'$(EnableDefaultEmbeddedResourceItems)' == 'true'" />
    <EmbeddedResource Include="$(StringResourcesPath)" Visible="true" ManifestResourceName="$(StringResourcesName)" GenerateSource="true" ClassName="$(StringResourcesNamespace).$(StringResourcesClassName)" />
    <!-- Include common SR helper when resources are included. -->
    <Compile Include="$(CommonPath)/System/SR$(DefaultLanguageSourceExtension)" Visible="true" Link="Resources/Common/SR$(DefaultLanguageSourceExtension)" />
  </ItemGroup>
  <!--
============================================================================================================================================
  </Import>

C:\Users\calope\source\repos\runtime8\src\libraries\Directory.Build.targets
============================================================================================================================================
-->
  <!--
============================================================================================================================================
  <Import Project="..\..\Directory.Build.targets">

C:\Users\calope\source\repos\runtime8\Directory.Build.targets
============================================================================================================================================
-->
  <PropertyGroup>
    <!--
    For non-SDK projects that import this file and then import Microsoft.Common.targets,
    tell Microsoft.Common.targets not to import Directory.Build.targets again
    -->
    <ImportDirectoryBuildTargets>false</ImportDirectoryBuildTargets>
  </PropertyGroup>
  <!--
============================================================================================================================================
  <Import Project="$(RepositoryEngineeringDir)Analyzers.targets">

C:\Users\calope\source\repos\runtime8\eng\Analyzers.targets
============================================================================================================================================
-->
  <PropertyGroup Condition="'$(UsingMicrosoftNoTargetsSdk)' == 'true' or&#xD;&#xA;                            '$(UsingMicrosoftDotNetSharedFrameworkSdk)' == 'true' or&#xD;&#xA;                            '$(MSBuildProjectExtension)' == '.pkgproj' or&#xD;&#xA;                            '$(UsingMicrosoftTraversalSdk)' == 'true'">
    <!-- Explicitly disable running analyzers to avoid trying to discover the correct ILLink tool pack for a project that has no sources. -->
    <RunAnalyzers>false</RunAnalyzers>
  </PropertyGroup>
  <PropertyGroup>
    <!-- Disable analyzers in sourcebuild -->
    <RunAnalyzers Condition="'$(DotNetBuildFromSource)' == 'true'">false</RunAnalyzers>
    <EnableNETAnalyzers Condition="'$(EnableNETAnalyzers)' == ''">$(RunAnalyzers)</EnableNETAnalyzers>
  </PropertyGroup>
  <PropertyGroup Condition="'$(RunAnalyzers)' != 'false'">
    <EnableSingleFileAnalyzer Condition="&#xD;&#xA;      '$(EnableSingleFileAnalyzer)' == '' And&#xD;&#xA;      '$(TargetFrameworkIdentifier)' == '.NETCoreApp'">true</EnableSingleFileAnalyzer>
  </PropertyGroup>
  <ItemGroup Condition="'$(RunAnalyzers)' != 'false'">
    <EditorConfigFiles Include="$(MSBuildThisFileDirectory)CodeAnalysis.src.globalconfig" />
    <PackageReference Include="Microsoft.DotNet.CodeAnalysis" Version="$(MicrosoftDotNetCodeAnalysisVersion)" PrivateAssets="all" IsImplicitlyDefined="true" />
    <PackageReference Include="Microsoft.CodeAnalysis.NetAnalyzers" Version="$(MicrosoftCodeAnalysisNetAnalyzersVersion)" PrivateAssets="all" />
    <PackageReference Include="Microsoft.CodeAnalysis.CSharp.CodeStyle" Version="$(MicrosoftCodeAnalysisCSharpCodeStyleVersion)" PrivateAssets="all" />
    <PackageReference Include="StyleCop.Analyzers" Version="$(StyleCopAnalyzersVersion)" PrivateAssets="all" />
  </ItemGroup>
  <ItemGroup Condition="'$(IsTestProject)' == 'true'">
    <EditorConfigFiles Remove="$(RepositoryEngineeringDir)CodeAnalysis.src.globalconfig" />
    <EditorConfigFiles Include="$(RepositoryEngineeringDir)CodeAnalysis.test.globalconfig" />
  </ItemGroup>
  <!--
============================================================================================================================================
  </Import>

C:\Users\calope\source\repos\runtime8\Directory.Build.targets
============================================================================================================================================
-->
  <!--
============================================================================================================================================
  <Import Project="Sdk.targets" Sdk="Microsoft.DotNet.Arcade.Sdk">

C:\.tools\.nuget\packages\microsoft.dotnet.arcade.sdk\8.0.0-beta.24426.2\Sdk\Sdk.targets
============================================================================================================================================
-->
  <!-- Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. -->
  <!--
    Some projects do not import Common targets, so BeforeCommonTargets.targets doesn't get imported. 
    (https://github.com/dotnet/arcade/issues/2676).
  -->
  <PropertyGroup>
    <_BeforeCommonTargetsHookUsed>true</_BeforeCommonTargetsHookUsed>
    <_BeforeCommonTargetsHookUsed Condition="'$(_ArcadeBeforeCommonTargetsImported)' != 'true'">false</_BeforeCommonTargetsHookUsed>
  </PropertyGroup>
  <!--<Import Project="..\tools\BeforeCommonTargets.targets" Condition="!$(_SuppressSdkImports) and '$(_ArcadeBeforeCommonTargetsImported)' != 'true' and '$(IsCrossTargetingBuild)' != 'true'" />-->
  <!--<Import Project="..\tools\BeforeCommonTargets.CrossTargeting.targets" Condition="!$(_SuppressSdkImports) and '$(_ArcadeBeforeCommonTargetsImported)' != 'true' and '$(IsCrossTargetingBuild)' == 'true'" />-->
  <!-- 
    Output the location of the Build.proj so that the build driver can find where it was restored.
    Ideally we would have msbuild API to do that for an SDK: https://github.com/Microsoft/msbuild/issues/2992
  -->
  <Target Name="__WriteToolsetLocation" Outputs="$(__ToolsetLocationOutputFile)" Condition="'$(__ToolsetLocationOutputFile)' != ''">
    <WriteLinesToFile File="$(__ToolsetLocationOutputFile)" Lines="$([System.IO.Path]::GetFullPath('$(MSBuildThisFileDirectory)..\tools\Build.proj'))" Overwrite="true" />
    <ItemGroup>
      <FileWrites Include="$(__ToolsetLocationOutputFile)" />
    </ItemGroup>
  </Target>
  <!--
============================================================================================================================================
  <Import Project="..\tools\Imports.targets" Condition="!$(_SuppressSdkImports) and !$(_SuppressAllTargets)">

C:\.tools\.nuget\packages\microsoft.dotnet.arcade.sdk\8.0.0-beta.24426.2\tools\Imports.targets
============================================================================================================================================
-->
  <!-- Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. -->
  <!--
============================================================================================================================================
  <Import Project="ProjectDefaults.targets">

C:\.tools\.nuget\packages\microsoft.dotnet.arcade.sdk\8.0.0-beta.24426.2\tools\ProjectDefaults.targets
============================================================================================================================================
-->
  <!-- Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. -->
  <PropertyGroup>
    <DeployProjectOutput Condition="'$(DeployProjectOutput)' == ''">$(__DeployProjectOutput)</DeployProjectOutput>
    <!-- Run Deploy step by default when the solution is build directly via msbuild (from command line or VS). -->
    <DeployProjectOutput Condition="'$(DeployProjectOutput)' == ''">true</DeployProjectOutput>
  </PropertyGroup>
  <!-- Default empty deploy target. -->
  <Target Name="Deploy" AfterTargets="Build" Condition="'$(DeployProjectOutput)' == 'true'" />
  <PropertyGroup>
    <!--
      Set PackageOutputPath based on the IsShippingPackage flag set by projects.
      This distinction allows publishing tools to determine which assets to publish to official channels.

      Visual Studio Build (aka CoreXT) packages are non-shipping packages that are used to insert binaries into an internal 
      Visual Studio repository that builds the product from components. These packages are not standard NuGet packages.
    -->
    <PackageOutputPath Condition="'$(IsShippingPackage)' == 'true'">$(ArtifactsShippingPackagesDir)</PackageOutputPath>
    <PackageOutputPath Condition="'$(IsShippingPackage)' != 'true'">$(ArtifactsNonShippingPackagesDir)</PackageOutputPath>
    <PackageOutputPath Condition="'$(IsVisualStudioBuildPackage)' == 'true'">$(VisualStudioBuildPackagesDir)</PackageOutputPath>
    <IsSwixProject>false</IsSwixProject>
    <IsSwixProject Condition="'$(VisualStudioInsertionComponent)' != '' and '$(IsVsixProject)' != 'true'">true</IsSwixProject>
  </PropertyGroup>
  <!--
    Closed source license must be added to the package. 
    NuGet.org accepts only OSI or FSF approved licenses when using license type expression. 
  -->
  <PropertyGroup Condition="'$(PackageLicenseExpressionInternal)' != '' and '$(IsPackable)' == 'true' and '$(PackageLicenseFile)' == ''">
    <PackageLicenseFile>License.txt</PackageLicenseFile>
  </PropertyGroup>
  <PropertyGroup Condition="'$(PackageLicenseExpressionInternal)' != '' and '$(IsPackable)' == 'true' and '$(PackageLicenseFullPath)' == ''">
    <PackageLicenseFullPath>$(MSBuildThisFileDirectory)Licenses\$(PackageLicenseExpressionInternal).txt</PackageLicenseFullPath>
  </PropertyGroup>
  <ItemGroup Condition="'$(PackageLicenseExpressionInternal)' != '' and '$(IsPackable)' == 'true'">
    <None Include="$(PackageLicenseFullPath)" Pack="true" PackagePath="$(PackageLicenseFile)" Visible="false" />
  </ItemGroup>
  <!--
    Include package icon in the package.
  -->
  <ItemGroup Condition="'$(PackageIcon)' != '' and '$(IsPackable)' == 'true'">
    <None Include="$(PackageIconFullPath)" Pack="true" PackagePath="$(PackageIcon)" Visible="false" />
  </ItemGroup>
  <!--
    Copyright used for binary assets (assemblies and packages) built by Microsoft must be Microsoft copyright.
    Override any other value the project may set.
  -->
  <PropertyGroup>
    <Copyright>$(CopyrightMicrosoft)</Copyright>
  </PropertyGroup>
  <!--
============================================================================================================================================
  </Import>

C:\.tools\.nuget\packages\microsoft.dotnet.arcade.sdk\8.0.0-beta.24426.2\tools\Imports.targets
============================================================================================================================================
-->
  <!--
============================================================================================================================================
  <Import Project="StrongName.targets">

C:\.tools\.nuget\packages\microsoft.dotnet.arcade.sdk\8.0.0-beta.24426.2\tools\StrongName.targets
============================================================================================================================================
-->
  <!-- Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. -->
  <!--
    Reads variables:
      SignAssembly    "true" to sign the output assembly of the current project
      FullAssemblySigningSupported    "false" to use public signing even when full signing is possible. This is useful
                                      in environments where full signing is non-functional or not desired. For example,
                                      in some Linux distributions RSA+SHA1 (required for full signing) is not
                                      functional/available, and trying to use full signing results in the runtime
                                      throwing an exception. For more details and an example, see
                                      https://github.com/dotnet/runtime/issues/65874.
      StrongNameKeyId The id of the key used for strong name generation

    Writes variables:
      DelaySign
      PublicSign
      PublicKey
      PublicKeyToken
      AssemblyOriginatorKeyFile
  -->
  <PropertyGroup Condition="'$(SignAssembly)' != 'false'">
    <DelaySign>false</DelaySign>
    <PublicSign>true</PublicSign>
  </PropertyGroup>
  <!-- Binaries are delay or public-signed with one of these keys; later, the signing system will finish the strong-name signing. -->
  <Choose>
    <When Condition="'$(SignAssembly)' == 'false'" />
    <When Condition="'$(StrongNameKeyId)' == 'Microsoft'">
      <PropertyGroup>
        <AssemblyOriginatorKeyFile>$(MSBuildThisFileDirectory)snk/MSFT.snk</AssemblyOriginatorKeyFile>
        <PublicKey>$(MicrosoftPublicKey)</PublicKey>
        <PublicKeyToken>b03f5f7f11d50a3a</PublicKeyToken>
      </PropertyGroup>
    </When>
    <When Condition="'$(StrongNameKeyId)' == 'MicrosoftShared'">
      <PropertyGroup>
        <AssemblyOriginatorKeyFile>$(MSBuildThisFileDirectory)snk/35MSSharedLib1024.snk</AssemblyOriginatorKeyFile>
        <PublicKey>$(MicrosoftSharedPublicKey)</PublicKey>
        <PublicKeyToken>31BF3856AD364E35</PublicKeyToken>
      </PropertyGroup>
    </When>
    <When Condition="'$(StrongNameKeyId)' == 'MicrosoftAspNetCore'">
      <PropertyGroup>
        <AssemblyOriginatorKeyFile>$(MSBuildThisFileDirectory)snk/AspNetCore.snk</AssemblyOriginatorKeyFile>
        <PublicKey>$(MicrosoftAspNetCorePublicKey)</PublicKey>
        <PublicKeyToken>adb9793829ddae60</PublicKeyToken>
        <PublicSign Condition="'$(FullAssemblySigningSupported)' != 'false'">false</PublicSign>
        <!-- The MicrosoftAspNetCore strong name key is a full key -->
      </PropertyGroup>
    </When>
    <When Condition="'$(StrongNameKeyId)' == 'ECMA'">
      <PropertyGroup>
        <AssemblyOriginatorKeyFile>$(MSBuildThisFileDirectory)snk/ECMA.snk</AssemblyOriginatorKeyFile>
        <PublicKey>$(ECMAPublicKey)</PublicKey>
        <PublicKeyToken>b77a5c561934e089</PublicKeyToken>
      </PropertyGroup>
    </When>
    <!--
      The Open key can be used by any library that needs strong name signing that doesn't
      have to be protected by the closed MS based keys. The idea is to have a key for identity but
      not for any security purposes.
    -->
    <When Condition="'$(StrongNameKeyId)' == 'Open'">
      <PropertyGroup>
        <AssemblyOriginatorKeyFile>$(MSBuildThisFileDirectory)snk/Open.snk</AssemblyOriginatorKeyFile>
        <PublicKey>$(OpenPublicKey)</PublicKey>
        <PublicKeyToken>cc7b13ffcd2ddd51</PublicKeyToken>
        <DelaySign>false</DelaySign>
        <PublicSign Condition="'$(FullAssemblySigningSupported)' != 'false'">false</PublicSign>
        <!-- The Open strong name key is a full key -->
      </PropertyGroup>
    </When>
    <When Condition="'$(StrongNameKeyId)' == 'SilverlightPlatform'">
      <PropertyGroup>
        <AssemblyOriginatorKeyFile>$(MSBuildThisFileDirectory)snk/SilverlightPlatformPublicKey.snk</AssemblyOriginatorKeyFile>
        <PublicKey>$(SilverlightPlatformPublicKey)</PublicKey>
        <PublicKeyToken>7cec85d7bea7798e</PublicKeyToken>
      </PropertyGroup>
    </When>
  </Choose>
  <!-- Build Flag Verification -->
  <PropertyGroup>
    <PrepareForBuildDependsOn>$(PrepareForBuildDependsOn);VerifyBuildFlags</PrepareForBuildDependsOn>
  </PropertyGroup>
  <Target Name="VerifyBuildFlags">
    <Error Condition="'$(SignAssembly)' != 'false' and&#xD;&#xA;                      ('$(PublicKey)' == '' or '$(PublicKeyToken)' == '' or '$(AssemblyOriginatorKeyFile)' == '')" Text="PublicKey, PublicKeyToken and AssemblyOriginatorKeyFile must be specified" />
  </Target>
  <!--
============================================================================================================================================
  </Import>

C:\.tools\.nuget\packages\microsoft.dotnet.arcade.sdk\8.0.0-beta.24426.2\tools\Imports.targets
============================================================================================================================================
-->
  <!--
============================================================================================================================================
  <Import Project="GenerateChecksums.targets">

C:\.tools\.nuget\packages\microsoft.dotnet.arcade.sdk\8.0.0-beta.24426.2\tools\GenerateChecksums.targets
============================================================================================================================================
-->
  <!-- Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. -->
  <UsingTask TaskName="Microsoft.DotNet.Arcade.Sdk.GenerateChecksums" AssemblyFile="$(ArcadeSdkBuildTasksAssembly)" />
  <!--
    Generate Checksums for the specified assets. Runs after the build of a project.
  -->
  <Target Name="GenerateChecksums" Condition="'@(GenerateChecksumItems)' != ''" AfterTargets="Build">
    <Error Condition="'%(GenerateChecksumItems.DestinationPath)' == ''" Text="Item &quot;%(GenerateChecksumItems.Identity)&quot; does not define required metadata &quot;DestinationPath&quot;" />
    <GenerateChecksums Items="@(GenerateChecksumItems)" />
    <!-- Automatically include generated checksums in the asset manifest -->
    <ItemGroup>
      <ItemsToPushToBlobFeed Include="@(GenerateChecksumItems -> '%(DestinationPath)')" />
    </ItemGroup>
  </Target>
  <!--
============================================================================================================================================
  </Import>

C:\.tools\.nuget\packages\microsoft.dotnet.arcade.sdk\8.0.0-beta.24426.2\tools\Imports.targets
============================================================================================================================================
-->
  <!--
============================================================================================================================================
  <Import Project="GenerateInternalsVisibleTo.targets">

C:\.tools\.nuget\packages\microsoft.dotnet.arcade.sdk\8.0.0-beta.24426.2\tools\GenerateInternalsVisibleTo.targets
============================================================================================================================================
-->
  <!-- Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. -->
  <PropertyGroup>
    <GeneratedInternalsVisibleToFile>$(IntermediateOutputPath)$(MSBuildProjectName).InternalsVisibleTo$(DefaultLanguageSourceExtension)</GeneratedInternalsVisibleToFile>
  </PropertyGroup>
  <ItemDefinitionGroup>
    <InternalsVisibleTo>
      <Visible>false</Visible>
    </InternalsVisibleTo>
  </ItemDefinitionGroup>
  <Target Name="PrepareGenerateInternalsVisibleToFile" Condition="'@(InternalsVisibleTo)' != ''">
    <ItemGroup>
      <_InternalsVisibleToAttribute Include="System.Runtime.CompilerServices.InternalsVisibleToAttribute">
        <_Parameter1 Condition="'%(InternalsVisibleTo.Key)' != ''">%(InternalsVisibleTo.Identity), PublicKey=%(InternalsVisibleTo.Key)</_Parameter1>
        <_Parameter1 Condition="'%(InternalsVisibleTo.Key)' == '' and '$(PublicKey)' != ''">%(InternalsVisibleTo.Identity), PublicKey=$(PublicKey)</_Parameter1>
        <_Parameter1 Condition="'%(InternalsVisibleTo.Key)' == '' and '$(PublicKey)' == ''">%(InternalsVisibleTo.Identity)</_Parameter1>
      </_InternalsVisibleToAttribute>
    </ItemGroup>
  </Target>
  <!--
    Dependency on PrepareForBuild is necessary so that we don't accidentally get ordered before it.
    We rely on PrepareForBuild to create the IntermediateOutputDirectory if it doesn't exist.

    Must run before BeforeCompile, as it's the MSBuild's convention for code generators.
  -->
  <Target Name="GenerateInternalsVisibleToFile" Inputs="$(MSBuildThisFileFullPath);$(MSBuildProjectFile)" Outputs="$(GeneratedInternalsVisibleToFile)" DependsOnTargets="PrepareGenerateInternalsVisibleToFile;PrepareForBuild" Condition="'@(InternalsVisibleTo)' != ''" BeforeTargets="BeforeCompile;CoreCompile">
    <WriteCodeFragment AssemblyAttributes="@(_InternalsVisibleToAttribute)" Language="$(Language)" OutputFile="$(GeneratedInternalsVisibleToFile)">
      <Output TaskParameter="OutputFile" ItemName="CompileBefore" Condition="'$(Language)' == 'F#'" />
      <Output TaskParameter="OutputFile" ItemName="Compile" Condition="'$(Language)' != 'F#'" />
      <Output TaskParameter="OutputFile" ItemName="FileWrites" />
    </WriteCodeFragment>
  </Target>
  <!--
============================================================================================================================================
  </Import>

C:\.tools\.nuget\packages\microsoft.dotnet.arcade.sdk\8.0.0-beta.24426.2\tools\Imports.targets
============================================================================================================================================
-->
  <!--
============================================================================================================================================
  <Import Project="GenerateResxSource.targets">

C:\.tools\.nuget\packages\microsoft.dotnet.arcade.sdk\8.0.0-beta.24426.2\tools\GenerateResxSource.targets
============================================================================================================================================
-->
  <!-- Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. -->
  <!--
    Generates a class source for EmbeddedResource whose GenerateSource property is set to true.
    The source file is generated to intermediate output dir to avoid polluting the source tree.
    See https://github.com/dotnet/sdk/issues/94 that tracks productization of this code.
  -->
  <UsingTask TaskName="Microsoft.DotNet.Arcade.Sdk.GenerateResxSource" AssemblyFile="$(ArcadeSdkBuildTasksAssembly)" />
  <!-- Set the Generator metadata so that VS triggers design-time build whenever the .resx file is saved -->
  <ItemGroup>
    <EmbeddedResource Update="@(EmbeddedResource)">
      <GenerateSource Condition="'%(Extension)' == '.resx' and '%(GenerateSource)' == '' and '$(GenerateResxSource)' == 'true'">true</GenerateSource>
      <Generator Condition="'%(Extension)' == '.resx' and '%(Generator)' == '' and ('%(GenerateSource)' == 'true' or ('%(GenerateSource)' == '' and '$(GenerateResxSource)' == 'true') )">MSBuild:_GenerateResxSource</Generator>
    </EmbeddedResource>
  </ItemGroup>
  <!--
    Note: Targets that generate Compile items are expected to run before BeforeCompile targets (common targets convention).
  -->
  <Target Name="_GenerateResxSource" BeforeTargets="BeforeCompile;CoreCompile" DependsOnTargets="PrepareResourceNames;&#xD;&#xA;                            _GetEmbeddedResourcesWithSourceGeneration;&#xD;&#xA;                            _BatchGenerateResxSource">
    <ItemGroup>
      <GeneratedResxSource Include="@(EmbeddedResourceSGResx->'%(SourceOutputPath)')" />
      <FileWrites Include="@(GeneratedResxSource)" />
      <Compile Include="@(GeneratedResxSource)" />
    </ItemGroup>
  </Target>
  <Target Name="_CustomizeResourceNames" BeforeTargets="PrepareResourceNames">
    <ItemGroup>
      <EmbeddedResource>
        <ManifestResourceName Condition="'%(EmbeddedResource.Namespace)' != ''">%(EmbeddedResource.Namespace).%(EmbeddedResource.Filename)</ManifestResourceName>
      </EmbeddedResource>
    </ItemGroup>
  </Target>
  <Target Name="_BatchGenerateResxSource" Inputs="@(EmbeddedResourceSGResx)" Outputs="%(EmbeddedResourceSGResx.SourceOutputPath)">
    <Microsoft.DotNet.Arcade.Sdk.GenerateResxSource Language="$(Language)" ResourceFile="%(EmbeddedResourceSGResx.FullPath)" ResourceName="%(EmbeddedResourceSGResx.ManifestResourceName)" ResourceClassName="%(EmbeddedResourceSGResx.ClassName)" AsConstants="%(EmbeddedResourceSGResx.GenerateResourcesCodeAsConstants)" OmitGetResourceString="$(GenerateResxSourceOmitGetResourceString)" IncludeDefaultValues="$(GenerateResxSourceIncludeDefaultValues)" EmitFormatMethods="$(GenerateResxSourceEmitFormatMethods)" OutputPath="%(EmbeddedResourceSGResx.SourceOutputPath)" />
  </Target>
  <Target Name="_GetEmbeddedResourcesWithSourceGeneration" Condition="'@(EmbeddedResource)' != ''">
    <PropertyGroup>
      <_EmbeddedResourceSourceExtension Condition="'$(Language)' == 'C#'">cs</_EmbeddedResourceSourceExtension>
      <_EmbeddedResourceSourceExtension Condition="'$(Language)' == 'VB'">vb</_EmbeddedResourceSourceExtension>
      <_EmbeddedResourceSourceExtension Condition="'$(Language)' == 'F#'">fs</_EmbeddedResourceSourceExtension>
    </PropertyGroup>
    <Error Text="GenerateResxSource doesn't support language: '$(Language)'" Condition="'$(_EmbeddedResourceSourceExtension)' == ''" />
    <ItemGroup>
      <EmbeddedResourceSG Include="@(EmbeddedResource)" Condition="'%(EmbeddedResource.GenerateSource)' == 'true' and '%(EmbeddedResource.XlfLanguage)' == ''" />
      <EmbeddedResourceSG Condition="'$(Language)' != 'F#'">
        <SourceOutputPath Condition="'%(EmbeddedResourceSG.SourceOutputPath)' == '' AND '%(EmbeddedResourceSG.ClassName)' != ''">$(IntermediateOutputPath)%(EmbeddedResourceSG.ClassName).$(_EmbeddedResourceSourceExtension)</SourceOutputPath>
        <SourceOutputPath Condition="'%(EmbeddedResourceSG.SourceOutputPath)' == '' AND '%(EmbeddedResourceSG.ClassName)' == ''">$(IntermediateOutputPath)%(EmbeddedResourceSG.ManifestResourceName).$(_EmbeddedResourceSourceExtension)</SourceOutputPath>
      </EmbeddedResourceSG>
      <!-- Other source generators might exist, so create a separate group for the items that are set to use the generator from this targets file. -->
      <EmbeddedResourceSGResx Include="@(EmbeddedResourceSG-&gt;WithMetadataValue('Generator', 'MSBuild:_GenerateResxSource'))" />
    </ItemGroup>
  </Target>
  <!--
============================================================================================================================================
  </Import>

C:\.tools\.nuget\packages\microsoft.dotnet.arcade.sdk\8.0.0-beta.24426.2\tools\Imports.targets
============================================================================================================================================
-->
  <!--
============================================================================================================================================
  <Import Project="Workarounds.targets">

C:\.tools\.nuget\packages\microsoft.dotnet.arcade.sdk\8.0.0-beta.24426.2\tools\Workarounds.targets
============================================================================================================================================
-->
  <!-- Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. -->
  <!-- Workaround for https://github.com/Microsoft/msbuild/issues/1310 -->
  <Target Name="ForceGenerationOfBindingRedirects" AfterTargets="ResolveAssemblyReferences" BeforeTargets="GenerateBindingRedirects" Condition="'$(AutoGenerateBindingRedirects)' == 'true'">
    <PropertyGroup>
      <!-- Needs to be set in a target because it has to be set after the initial evaluation in the common targets -->
      <GenerateBindingRedirectsOutputType>true</GenerateBindingRedirectsOutputType>
    </PropertyGroup>
  </Target>
  <!--
    Workaround to fix IntelliSense file generation for XAML projects
    https://github.com/dotnet/project-system/issues/2488
  -->
  <Target Name="WorkaroundForXamlIntelliSenseBuildIssue" AfterTargets="_CheckCompileDesignTimePrerequisite">
    <PropertyGroup>
      <BuildingProject>false</BuildingProject>
    </PropertyGroup>
  </Target>
  <!--
    WPF temp project sets OutDir, which makes the SDK create an empty directory for it,
    polluting the output dir. Avoid creating these directories.
    https://github.com/dotnet/sdk/issues/1367
  -->
  <PropertyGroup Condition="'$(IsWpfTempProject)' == 'true'">
    <OutDir />
  </PropertyGroup>
  <!--
    Workaround to fix that Visual Studio sometimes uses a special MSBuild evaluation
    mode where all common conditions (e.g., inside ItemGroup) are ignored.
  -->
  <Choose>
    <When Condition=" '$(IsWpfTempProject)' == 'true' and '$(TargetFrameworkIdentifier)' == '.NETFramework' ">
      <ItemGroup>
        <Reference Include="mscorlib" Pack="false" />
      </ItemGroup>
    </When>
  </Choose>
  <!--
    Workaround for a race condition https://github.com/Microsoft/msbuild/issues/1479.
  -->
  <PropertyGroup>
    <TargetFrameworkMonikerAssemblyAttributesPath>$(IntermediateOutputPath)$(TargetFrameworkMoniker).AssemblyAttributes$(DefaultLanguageSourceExtension)</TargetFrameworkMonikerAssemblyAttributesPath>
    <TargetFrameworkMonikerAssemblyAttributesFileClean>true</TargetFrameworkMonikerAssemblyAttributesFileClean>
  </PropertyGroup>
  <!--
     Portable PDBs are not included in .nupkg by default. Include them unless the project produces symbol packages.
     Remove this once we migrate to .snupkg. See https://github.com/dotnet/arcade/issues/1959.
   -->
  <PropertyGroup Condition="'$(DebugType)' == 'portable' and '$(IncludeSymbols)' != 'true'">
    <AllowedOutputExtensionsInPackageBuildOutputFolder>$(AllowedOutputExtensionsInPackageBuildOutputFolder);.pdb</AllowedOutputExtensionsInPackageBuildOutputFolder>
  </PropertyGroup>
  <!--
    Workarounds for insufficient support for tools packages by NuGet Pack: https://github.com/NuGet/Home/issues/6321.

    Project that produce tools packages use .nuspec file in order to include all the needed dependencies.
    This target translates common msbuild properties to NuSpec properties.
  -->
  <Target Name="InitializeStandardNuspecProperties" BeforeTargets="GenerateNuspec" DependsOnTargets="_InitializeNuspecRepositoryInformationPropertiesWorkaround" Condition="'$(IsPackable)' == 'true'">
    <PropertyGroup>
      <PackageId Condition="'$(NuspecPackageId)' != ''">$(NuspecPackageId)</PackageId>
      <PackageProjectUrl Condition="'$(PackageProjectUrl)' == ''">$(RepositoryUrl)</PackageProjectUrl>
    </PropertyGroup>
    <Error Text="PackageDescription must be specified" Condition="'$(PackageDescription)' == ''" />
    <Error Text="PackageProjectUrl must be specified" Condition="'$(PackageProjectUrl)' == ''" />
    <Error Text="RepositoryUrl must be specified" Condition="'$(RepositoryUrl)' == ''" />
    <Error Text="RepositoryCommit must be specified" Condition="'$(RepositoryCommit)' == ''" />
    <Error Text="RepositoryType must be specified" Condition="'$(RepositoryType)' == ''" />
    <Error Text="Either PackageLicenseExpression or PackageLicenseFile must be specified" Condition="'$(PackageLicenseExpression)' == '' and '$(PackageLicenseFile)' == ''" />
    <PropertyGroup Condition="'$(NuspecFile)' != '' and '$(NuspecProperties)' == ''">
      <_LicenseElement />
      <_LicenseElement Condition="'$(PackageLicenseExpression)' != ''">
        <license type="expression">$(PackageLicenseExpression)</license>
      </_LicenseElement>
      <_LicenseElement Condition="'$(PackageLicenseFile)' != ''">
        <license type="file">$(PackageLicenseFile)</license>
      </_LicenseElement>
      <_LicenseFileElement />
      <_LicenseFileElement Condition="'$(PackageLicenseFile)' != ''">
        <file src="$(PackageLicenseFullPath)" target="$(PackageLicenseFile)" />
      </_LicenseFileElement>
      <_TagsElement />
      <_TagsElement Condition="'$(PackageTags)' != ''">
        <tags>$(PackageTags.Replace(';', ' '))</tags>
      </_TagsElement>
      <_IconUrlElement />
      <_IconUrlElement Condition="'$(PackageIcon)' == '' and '$(PackageIconUrl)' != ''">
        <iconUrl>$(PackageIconUrl)</iconUrl>
      </_IconUrlElement>
      <_IconElement />
      <_IconElement Condition="'$(PackageIcon)' != ''">
        <icon>$(PackageIcon)</icon>
      </_IconElement>
      <_IconFileElement />
      <_IconFileElement Condition="'$(PackageIcon)' != ''">
        <file src="$(PackageIconFullPath)" target="$(PackageIcon)" />
      </_IconFileElement>
      <_ReleaseNotesElement />
      <_ReleaseNotesElement Condition="'$(PackageReleaseNotes)' != ''">
        <releaseNotes>$(PackageReleaseNotes)</releaseNotes>
      </_ReleaseNotesElement>
      <_CommonMetadataElements>
        <id>$(PackageId)</id>
        <description>$(PackageDescription)</description>
        <version>$(PackageVersion)</version>
        <authors>$(Authors)</authors>
        <requireLicenseAcceptance>$(PackageRequireLicenseAcceptance)</requireLicenseAcceptance>
        $(_TagsElement)
        $(_LicenseElement)
        $(_IconElement)
        $(_IconUrlElement)
        $(_ReleaseNotesElement)
        <projectUrl>$(PackageProjectUrl)</projectUrl><copyright>$(Copyright)</copyright><developmentDependency>$(DevelopmentDependency)</developmentDependency><serviceable>$(Serviceable)</serviceable><repository type="$(RepositoryType)" url="$(RepositoryUrl)" commit="$(RepositoryCommit)" /></_CommonMetadataElements>
      <_CommonFileElements>
        $(_IconFileElement)
        $(_LicenseFileElement)
      </_CommonFileElements>
    </PropertyGroup>
    <ItemGroup Condition="'$(NuspecFile)' != '' and '$(NuspecProperties)' == ''">
      <NuspecProperty Include="CommonMetadataElements=$(_CommonMetadataElements)" />
      <NuspecProperty Include="CommonFileElements=$(_CommonFileElements)" />
      <NuspecProperty Include="PackageId=$(PackageId)" />
      <NuspecProperty Include="Version=$(PackageVersion)" />
      <NuspecProperty Include="ProjectDirectory=$(MSBuildProjectDirectory)" />
    </ItemGroup>
    <PropertyGroup Condition="'$(NuspecFile)' != '' and '$(NuspecProperties)' == ''">
      <NuspecProperties>@(NuspecProperty, ';')</NuspecProperties>
    </PropertyGroup>
  </Target>
  <!--
    Initialize Repository* properties from properties set by a source control package, if available in the project.
  -->
  <Target Name="_InitializeNuspecRepositoryInformationPropertiesWorkaround" DependsOnTargets="InitializeSourceControlInformation" Condition="'$(SourceControlInformationFeatureSupported)' == 'true'">
    <PropertyGroup>
      <!-- The project must specify PublishRepositoryUrl=true in order to publish the URL, in order to prevent inadvertent leak of internal URL. -->
      <RepositoryUrl Condition="'$(RepositoryUrl)' == '' and '$(PublishRepositoryUrl)' == 'true'">$(PrivateRepositoryUrl)</RepositoryUrl>
      <RepositoryCommit Condition="'$(RepositoryCommit)' == ''">$(SourceRevisionId)</RepositoryCommit>
    </PropertyGroup>
  </Target>
  <!--
    NuGet Restore uses PackageId and project name in the same namespace, so that project reference can be interchanged with a package reference.
    This causes issues however for leaf packages that are not to be referenced (such as analyzer or tools packages) when we want to name the package
    the same as an existing project in the solution. In that case we set PackageId to an invalid but unique value for Restore and override it for Pack
    with the desired name stored in $(NuspecPackageId).
  -->
  <PropertyGroup Condition="'$(NuspecPackageId)' != ''">
    <PackageId>*$(MSBuildProjectName)*</PackageId>
  </PropertyGroup>
  <!--
    Source packaging helpers.
  -->
  <PropertyGroup Condition="'$(IsPackable)' == 'true' and '$(IsSourcePackage)' == 'true'">
    <TargetsForTfmSpecificContentInPackage Condition="'$(NuspecFile)' == ''">$(TargetsForTfmSpecificContentInPackage);_AddSourceFilesToSourcePackage</TargetsForTfmSpecificContentInPackage>
  </PropertyGroup>
  <Target Name="_AddSourceFilesToSourcePackage">
    <PropertyGroup>
      <!-- TODO: language to dir name mapping (https://github.com/Microsoft/msbuild/issues/2101) -->
      <_LanguageDirName>$(DefaultLanguageSourceExtension.TrimStart('.'))</_LanguageDirName>
    </PropertyGroup>
    <ItemGroup>
      <_File Remove="@(_File)" />
      <_File Include="$(MSBuildProjectDirectory)\**\*$(DefaultLanguageSourceExtension)" TargetDir="contentFiles/$(_LanguageDirName)/$(TargetFramework)" BuildAction="Compile" />
      <TfmSpecificPackageFile Include="@(_File)" PackagePath="%(_File.TargetDir)/%(_File.RecursiveDir)%(_File.FileName)%(_File.Extension)" />
    </ItemGroup>
  </Target>
  <!--
============================================================================================================================================
  </Import>

C:\.tools\.nuget\packages\microsoft.dotnet.arcade.sdk\8.0.0-beta.24426.2\tools\Imports.targets
============================================================================================================================================
-->
  <!--
============================================================================================================================================
  <Import Project="RepositoryInfo.targets">

C:\.tools\.nuget\packages\microsoft.dotnet.arcade.sdk\8.0.0-beta.24426.2\tools\RepositoryInfo.targets
============================================================================================================================================
-->
  <!-- Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. -->
  <!-- Opt-in switch to disable source link (i.e. for local builds). -->
  <PropertyGroup Condition="'$(DisableSourceLink)' == 'true'">
    <EnableSourceLink>false</EnableSourceLink>
    <EnableSourceControlManagerQueries>false</EnableSourceControlManagerQueries>
    <DeterministicSourcePaths>false</DeterministicSourcePaths>
  </PropertyGroup>
  <!--
    Set the SourceRoot to repo root to facilitate deterministic source paths when SCM queries are disabled, unless during design-time build.
    Set the RepositoryUrl to the Build.Repository.Uri Azure DevOps build variable if on CI, otherwise to local repo path.
    Do not set these properties during design-time build to avoid differences between design-time and reuglar builds.
  -->
  <ItemGroup Condition="'$(EnableSourceControlManagerQueries)' != 'true' and '$(DesignTimeBuild)' != 'true'">
    <SourceRoot Include="$(RepoRoot)" />
  </ItemGroup>
  <PropertyGroup Condition="'$(EnableSourceControlManagerQueries)' != 'true' and '$(DesignTimeBuild)' != 'true' and '$(RepositoryUrl)' == ''">
    <RepositoryUrl Condition="'$(BUILD_REPOSITORY_URI)' != '' and '$(DisableSourceLinkUrlTranslation)' != 'true'">$([System.Text.RegularExpressions.Regex]::Replace($(BUILD_REPOSITORY_URI), $(_TranslateUrlPattern), $(_TranslateUrlReplacement)))</RepositoryUrl>
    <RepositoryUrl Condition="'$(BUILD_REPOSITORY_URI)' != '' and '$(DisableSourceLinkUrlTranslation)' == 'true'">$(BUILD_REPOSITORY_URI)</RepositoryUrl>
    <RepositoryUrl Condition="'$(BUILD_REPOSITORY_URI)' == ''">file://$(RepoRoot)</RepositoryUrl>
  </PropertyGroup>
  <PropertyGroup Condition="'$(EnableSourceControlManagerQueries)' != 'true' and '$(DesignTimeBuild)' != 'true' and '$(RepositoryCommit)' == ''">
    <RepositoryCommit Condition="'$(BUILD_SOURCEVERSION)' != ''">$(BUILD_SOURCEVERSION)</RepositoryCommit>
    <RepositoryCommit Condition="'$(BUILD_SOURCEVERSION)' == ''">0000000000000000000000000000000000000000</RepositoryCommit>
  </PropertyGroup>
  <PropertyGroup Condition="'$(EnableSourceControlManagerQueries)' != 'true' and '$(DesignTimeBuild)' != 'true' and '$(RepositoryType)' == ''">
    <RepositoryType>git</RepositoryType>
  </PropertyGroup>
  <!-- 
    The convention for names of Azure DevOps repositories mirrored from GitHub is "{GitHub org name}-{GitHub repository name}"
    In the legacy devdiv.visualstudio instance, it is instead "{GitHub org name}-{GitHub repository name}-Trusted" with no guarantees for casing.
  -->
  <PropertyGroup>
    <!-- There are quite a few git repo forms:
      https://dnceng@dev.azure.com/dnceng/internal/_git/dotnet-arcade-services
      https://dev.azure.com/dnceng/internal/_git/dotnet-arcade-services
      https://dnceng.visualstudio.com/internal/_git/dotnet-arcade-services
      https://devdiv.visualstudio.com/DevDiv/_git/DotNet-msbuild-Trusted
      dnceng@vs-ssh.visualstudio.com:v3/dnceng/internal/dotnet-arcade-services
      git@ssh.dev.azure.com:v3/dnceng/internal/dotnet-arcade-services
    -->
    <!-- Set DisableSourceLinkUrlTranslation to true when building a tool for internal use where sources only come from internal URIs -->
    <DisableSourceLinkUrlTranslation Condition="'$(DisableSourceLinkUrlTranslation)' == ''">false</DisableSourceLinkUrlTranslation>
    <_TranslateUrlPattern>(https://dnceng%40dev\.azure\.com/dnceng/internal/_git|https://dev\.azure\.com/dnceng/internal/_git|https://dnceng\.visualstudio\.com/internal/_git|dnceng%40vs-ssh\.visualstudio\.com:v3/dnceng/internal|git%40ssh\.dev\.azure\.com:v3/dnceng/internal|https://devdiv\.visualstudio\.com/devdiv/_git)/([^/-]+)-(.+)</_TranslateUrlPattern>
    <_TranslateUrlReplacement>https://github.com/$2/$3</_TranslateUrlReplacement>
  </PropertyGroup>
  <Target Name="_TranslateAzureDevOpsUrlToGitHubUrl" Condition="'$(DisableSourceLinkUrlTranslation)' == 'false'" DependsOnTargets="$(SourceControlManagerUrlTranslationTargets)" BeforeTargets="SourceControlManagerPublishTranslatedUrls">
    <PropertyGroup>
      <!-- Repositories mirrored on devdiv.visualstudio will have '-Trusted' added to their name and this needs to be stripped off before translation
           Eventually, all repos should move to dnceng/internal when possible. -->
      <ScmRepositoryUrl Condition=" '$([MSBuild]::ValueOrDefault(`%(SourceRoot.ScmRepositoryUrl)`, ``).Contains(`devdiv.visualstudio`))' == 'true' ">$([MSBuild]::ValueOrDefault(`%(SourceRoot.ScmRepositoryUrl)`, ``).ToLower().Replace(`-trusted`,``))</ScmRepositoryUrl>
      <ScmRepositoryUrl>$([System.Text.RegularExpressions.Regex]::Replace($(ScmRepositoryUrl), $(_TranslateUrlPattern), $(_TranslateUrlReplacement)))</ScmRepositoryUrl>
    </PropertyGroup>
    <ItemGroup>
      <SourceRoot Update="@(SourceRoot)">
        <ScmRepositoryUrl Condition="$([MSBuild]::ValueOrDefault(`%(SourceRoot.ScmRepositoryUrl)`, ``).Contains(`devdiv.visualstudio`))">$([MSBuild]::ValueOrDefault(`%(SourceRoot.ScmRepositoryUrl)`, ``).ToLower().Replace(`-trusted`,``))</ScmRepositoryUrl>
      </SourceRoot>
      <SourceRoot Update="@(SourceRoot)">
        <ScmRepositoryUrl>$([System.Text.RegularExpressions.Regex]::Replace(%(SourceRoot.ScmRepositoryUrl), $(_TranslateUrlPattern), $(_TranslateUrlReplacement)))</ScmRepositoryUrl>
      </SourceRoot>
    </ItemGroup>
  </Target>
  <!--
    Generates and adds {PackageId}.SourceLink.targets file to the build directory of the source package.
  -->
  <UsingTask TaskName="Microsoft.DotNet.Arcade.Sdk.GenerateSourcePackageSourceLinkTargetsFile" AssemblyFile="$(ArcadeSdkBuildTasksAssembly)" />
  <PropertyGroup Condition="'$(IsPackable)' == 'true' and '$(IsSourcePackage)' == 'true' and '$(EnableSourceLink)' == 'true'">
    <BeforePack>$(BeforePack);_AddSourcePackageSourceLinkFile</BeforePack>
  </PropertyGroup>
  <Target Name="_AddSourcePackageSourceLinkFile" DependsOnTargets="_GenerateSourcePackageSourceLinkFile">
    <ItemGroup>
      <!-- Add a packable item if the project builds the package with auto-generated .nuspec file -->
      <None Include="$(_SourcePackageSourceLinkTargetsFilePath)" PackagePath="build" Pack="true" Condition="'$(NuspecFile)' == ''" />
      <!-- Include path in the nuspec properties if the project builds package using custom .nuspec -->
      <NuspecProperty Include="SourceLinkTargetsFilePath=$(_SourcePackageSourceLinkTargetsFilePath)" Condition="'$(NuspecFile)' != ''" />
    </ItemGroup>
  </Target>
  <Target Name="_CalculateGenerateSourcePackageSourceLinkFileOutputs">
    <PropertyGroup>
      <_SourcePackageSourceLinkTargetsFileName>$([MSBuild]::ValueOrDefault($(SourcePackageSourceLinkTargetsFileName), '$(PackageId).targets'))</_SourcePackageSourceLinkTargetsFileName>
      <_SourcePackageSourceLinkTargetsFilePath>$(IntermediateOutputPath)$(_SourcePackageSourceLinkTargetsFileName)</_SourcePackageSourceLinkTargetsFilePath>
    </PropertyGroup>
  </Target>
  <Target Name="_GenerateSourcePackageSourceLinkFile" DependsOnTargets="InitializeSourceControlInformation;$(SourceLinkUrlInitializerTargets);_CalculateGenerateSourcePackageSourceLinkFileOutputs" Inputs="$(MSBuildAllProjects)" Outputs="$(_SourcePackageSourceLinkTargetsFilePath)">
    <Microsoft.DotNet.Arcade.Sdk.GenerateSourcePackageSourceLinkTargetsFile ProjectDirectory="$(MSBuildProjectDirectory)" PackageId="$(PackageId)" SourceRoots="@(SourceRoot)" OutputPath="$(_SourcePackageSourceLinkTargetsFilePath)" />
    <ItemGroup>
      <FileWrites Include="$(_SourcePackageSourceLinkTargetsFilePath)" />
    </ItemGroup>
  </Target>
  <!--
    Validates repository-wide requirements.
    MSBuild only evaluates the target project once per each set of values of global properties and caches the results.
  -->
  <Target Name="_RepositoryValidation" BeforeTargets="Build" Condition="'$(ContinuousIntegrationBuild)' == 'true'">
    <MSBuild Projects="$(MSBuildThisFileDirectory)RepositoryValidation.proj" Targets="Validate" RemoveProperties="TargetFramework;Platform" Properties="RepoRoot=$(RepoRoot);PackageLicenseExpression=$(PackageLicenseExpression);PackageLicenseExpressionInternal=$(PackageLicenseExpressionInternal);SuppressLicenseValidation=$(SuppressLicenseValidation)" UseResultsCache="true" />
  </Target>
  <!--
============================================================================================================================================
  </Import>

C:\.tools\.nuget\packages\microsoft.dotnet.arcade.sdk\8.0.0-beta.24426.2\tools\Imports.targets
============================================================================================================================================
-->
  <!--
============================================================================================================================================
  <Import Project="Version.targets">

C:\.tools\.nuget\packages\microsoft.dotnet.arcade.sdk\8.0.0-beta.24426.2\tools\Version.targets
============================================================================================================================================
-->
  <!-- Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. -->
  <!--
    Specification: https://github.com/dotnet/arcade/blob/master/Documentation/CorePackages/Versioning.md

    Properties:
      SemanticVersioningV1        "true" if the Version needs to respect SemVer 1.0. Default is false, which means format following SemVer 2.0.
  -->
  <UsingTask TaskName="Microsoft.DotNet.Arcade.Sdk.CalculateAssemblyAndFileVersions" AssemblyFile="$(ArcadeSdkBuildTasksAssembly)" />
  <Target Name="_InitializeAssemblyVersion" BeforeTargets="GetAssemblyVersion">
    <Warning Text="AssemblyVersion '$(AssemblyVersion)' overridden by auto-generated version" Condition="'$(AssemblyVersion)' != '' and '$(AutoGenerateAssemblyVersion)' == 'true'" />
    <Microsoft.DotNet.Arcade.Sdk.CalculateAssemblyAndFileVersions VersionPrefix="$(_OriginalVersionPrefix)" BuildNumber="$(_BuildNumber)" PatchNumber="$(_PatchNumber)" AutoGenerateAssemblyVersion="$(AutoGenerateAssemblyVersion)" Condition="'$(VersionSuffixDateStamp)' != ''">
      <Output TaskParameter="AssemblyVersion" PropertyName="AssemblyVersion" Condition="'$(AssemblyVersion)' == '' or '$(AutoGenerateAssemblyVersion)' == 'true'" />
      <Output TaskParameter="FileVersion" PropertyName="FileVersion" />
    </Microsoft.DotNet.Arcade.Sdk.CalculateAssemblyAndFileVersions>
    <PropertyGroup Condition="'$(VersionSuffixDateStamp)' == ''">
      <!--
        Set FileVersion to a distinct version that's greater than any shipping version.
        This makes it possible to install binaries produced by a dev build over product binaries,
        provided that the installer only requires higher version.
      -->
      <FileVersion>42.42.42.42424</FileVersion>
      <!--
        Respect version explicitly set by the project.
        The default .NET Core SDK implementation sets AssemblyVersion from NuGet package version,
        which we want to override in dev builds.
      -->
      <AssemblyVersion Condition="'$(AssemblyVersion)' == ''">42.42.42.42</AssemblyVersion>
    </PropertyGroup>
  </Target>
  <PropertyGroup>
    <GenerateNativeVersionFileDependsOn>_InitializeAssemblyVersion</GenerateNativeVersionFileDependsOn>
    <GenerateNativeVersionFileDependsOn Condition="'$(DisableSourceLink)' != 'true'">$(GenerateNativeVersionFileDependsOn);
                                                                                     InitializeSourceControlInformationFromSourceControlManager</GenerateNativeVersionFileDependsOn>
  </PropertyGroup>
  <!--
    GenerateNativeVersionFile target is a standalone target intended to be pulled into a build once as
    a pre-step before kicking off a native build. It will generate a _version.h or _version.c depending
    on the OS it is targeting.
  -->
  <Target Name="GenerateNativeVersionFile" DependsOnTargets="$(GenerateNativeVersionFileDependsOn)">
    <!-- To support builds without a source control provider available, allow this property to be unset. -->
    <PropertyGroup Condition="'$(SourceRevisionId)' != ''">
      <_SourceBuildInfo> %40Commit: $(SourceRevisionId)</_SourceBuildInfo>
    </PropertyGroup>
    <PropertyGroup Condition="'$(OS)' == 'Windows_NT'">
      <NativeVersionFile Condition="'$(NativeVersionFile)' == ''">$(IntermediateOutputPath)_version.h</NativeVersionFile>
      <_WindowsFileVersion>$(FileVersion.Replace('.', ','))</_WindowsFileVersion>
      <_Windows_VER_DEBUG>0</_Windows_VER_DEBUG>
      <_Windows_VER_DEBUG Condition="'$(Configuration)'=='Debug'">VS_FF_DEBUG</_Windows_VER_DEBUG>
      <_NativeVersionFileContents><![CDATA[
#ifndef VER_COMPANYNAME_STR
#define VER_COMPANYNAME_STR         "Microsoft Corporation"
#endif
#ifndef VER_FILEDESCRIPTION_STR
#define VER_FILEDESCRIPTION_STR     "$(AssemblyName)"
#endif
#ifndef VER_INTERNALNAME_STR
#define VER_INTERNALNAME_STR        VER_FILEDESCRIPTION_STR
#endif
#ifndef VER_ORIGINALFILENAME_STR
#define VER_ORIGINALFILENAME_STR    VER_FILEDESCRIPTION_STR
#endif
#ifndef VER_PRODUCTNAME_STR
#define VER_PRODUCTNAME_STR         ".NET"
#endif
#undef VER_PRODUCTVERSION
#define VER_PRODUCTVERSION          $(_WindowsFileVersion)
#undef VER_PRODUCTVERSION_STR
#define VER_PRODUCTVERSION_STR      "$(Version)$(_SourceBuildInfo)"
#undef VER_FILEVERSION
#define VER_FILEVERSION             $(_WindowsFileVersion)
#undef VER_FILEVERSION_STR
#define VER_FILEVERSION_STR         "$(_WindowsFileVersion)$(_SourceBuildInfo)"
#ifndef VER_LEGALCOPYRIGHT_STR
#define VER_LEGALCOPYRIGHT_STR      "\xa9 Microsoft Corporation. All rights reserved."
#endif
#ifndef VER_DEBUG
#define VER_DEBUG                   $(_Windows_VER_DEBUG)
#endif
]]></_NativeVersionFileContents>
    </PropertyGroup>
    <!--
      Copy the NativeVersion.rc file next to the version header so that it can be picked
      up and used in the native build along with the version.h file.
    -->
    <Copy SourceFiles="$(MSBuildThisFileDirectory)NativeVersion.rc" DestinationFolder="$([System.IO.Path]::GetDirectoryName($(NativeVersionFile)))" Condition="'$(OS)' == 'Windows_NT'" />
    <PropertyGroup Condition="'$(OS)' != 'Windows_NT'">
      <NativeVersionFile Condition="'$(NativeVersionFile)' == ''">$(ArtifactsObjDir)_version.c</NativeVersionFile>
      <!--
        There isn't a defacto standard for including version information in a native binary on unix so we defined a static
        variable which contains the version information we want which can be retrieved by using What(1) or strings+grep.
        See https://github.com/dotnet/coreclr/issues/3133 for further discussion on this approach.
      -->
      <_NativeVersionFileContents><![CDATA[
static char sccsid[] __attribute__((used)) = "@(#)Version $(FileVersion)$(_SourceBuildInfo)";
 ]]></_NativeVersionFileContents>
    </PropertyGroup>
    <MakeDir Directories="$([System.IO.Path]::GetDirectoryName($(NativeVersionFile)))" />
    <WriteLinesToFile File="$(NativeVersionFile)" Lines="$(_NativeVersionFileContents.Replace(';', '%3B'))" Overwrite="true" WriteOnlyWhenDifferent="true" />
    <ItemGroup>
      <FileWrites Include="$(NativeVersionFile)" />
    </ItemGroup>
  </Target>
  <!--
============================================================================================================================================
  </Import>

C:\.tools\.nuget\packages\microsoft.dotnet.arcade.sdk\8.0.0-beta.24426.2\tools\Imports.targets
============================================================================================================================================
-->
  <!--<Import Project="Tests.targets" Condition="'$(DisableArcadeTestFramework)' != 'true'" />-->
  <!--<Import Project="Pack.targets" Condition="'$(__ImportPackTargets)' == 'true'" />-->
  <!--<Import Project="Performance.targets" Condition="'$(DisableArcadeTestFramework)' != 'true'" />-->
  <!--
============================================================================================================================================
  <Import Project="Localization.targets">

C:\.tools\.nuget\packages\microsoft.dotnet.arcade.sdk\8.0.0-beta.24426.2\tools\Localization.targets
============================================================================================================================================
-->
  <!-- Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. -->
  <!--
    XliffTasks for localizing .resx files and generating satellite assemblies.
    When not building in CI, automatically sync .xlf files to .resx files on build.
    Otherwise, let the build fail to catch .xlf files that are not up-to-date.
  -->
  <PropertyGroup Condition="'$(UsingToolXliff)' == 'true'">
    <!-- 
      It is only intended to automatically run update during dev cycle. However, it will fail the build on CI if the XLF file is not updated.
      XLF file should be checked in and loc team will update the XLF it with translated version.
    -->
    <UpdateXlfOnBuild Condition="'$(ContinuousIntegrationBuild)' != 'true'">true</UpdateXlfOnBuild>
    <!--
      Use Satellite assembly generation task from Microsoft.NET.Sdk even when building with
      full Framework MSBuild. This will support public signing, is deterministic, and always
      generates them as AnyCPU. 
    -->
    <GenerateSatelliteAssembliesForCore Condition="'$(GenerateSatelliteAssembliesForCore)' == ''">true</GenerateSatelliteAssembliesForCore>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.DotNet.XliffTasks" Version="$(MicrosoftDotNetXliffTasksVersion)" PrivateAssets="all" IsImplicitlyDefined="true" Condition="'$(UsingToolXliff)' == 'true' and '$(IsShippingAssembly)' == 'true'" />
  </ItemGroup>
  <!-- TemplateLocalizer for localizing 'dotnet new' templates -->
  <PropertyGroup Condition="'$(UsingToolTemplateLocalizer)' == 'true' AND '$(DotNetBuildFromSource)' != 'true'">
    <!-- Run localizer when building on dev machine. -->
    <LocalizeTemplates Condition="'$(ContinuousIntegrationBuild)' != 'true'">true</LocalizeTemplates>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.TemplateEngine.Authoring.Tasks" Version="$(MicrosoftTemplateEngineAuthoringTasksVersion)" PrivateAssets="all" IsImplicitlyDefined="true" Condition="'$(UsingToolTemplateLocalizer)' == 'true' AND '$(DotNetBuildFromSource)' != 'true'" />
  </ItemGroup>
  <!--
============================================================================================================================================
  </Import>

C:\.tools\.nuget\packages\microsoft.dotnet.arcade.sdk\8.0.0-beta.24426.2\tools\Imports.targets
============================================================================================================================================
-->
  <!--<Import Project="VisualStudio.targets" Condition="'$(UsingToolVSSDK)' == 'true' and ('$(IsVsixProject)' == 'true' or '$(IsSwixProject)' == 'true' or '$(GeneratePkgDefFile)' == 'true') and '$(MSBuildRuntimeType)' != 'Core'" />-->
  <!--<Import Project="OptimizationData.targets" Condition="'$(UsingToolIbcOptimization)' == 'true'" />-->
  <!--<Import Project="SymStore.targets" Condition="'$(ContinuousIntegrationBuild)' == 'true' and '$(OS)' == 'Windows_NT'" />-->
  <!--
============================================================================================================================================
  </Import>

C:\.tools\.nuget\packages\microsoft.dotnet.arcade.sdk\8.0.0-beta.24426.2\Sdk\Sdk.targets
============================================================================================================================================
-->
  <!--<Import Project="..\tools\Empty.targets" Condition="!$(_SuppressSdkImports) and $(_SuppressAllTargets)" />-->
  <!--
============================================================================================================================================
  </Import>

C:\Users\calope\source\repos\runtime8\Directory.Build.targets
============================================================================================================================================
-->
  <!--
============================================================================================================================================
  <Import Project="$(RepositoryEngineeringDir)liveBuilds.targets">

C:\Users\calope\source\repos\runtime8\eng\liveBuilds.targets
============================================================================================================================================
-->
  <!-- Accept override paths for live artifacts. -->
  <PropertyGroup>
    <CoreCLRArtifactsPath Condition="'$(CoreCLROverridePath)' != ''">$([MSBuild]::NormalizeDirectory('$(CoreCLROverridePath)'))</CoreCLRArtifactsPath>
    <MonoArtifactsPath Condition="'$(MonoOverridePath)' != ''">$([MSBuild]::NormalizeDirectory('$(MonoOverridePath)'))</MonoArtifactsPath>
    <LibrariesArtifactsPath Condition="'$(LibrariesOverridePath)' != ''">$([MSBuild]::NormalizeDirectory('$(LibrariesOverridePath)'))</LibrariesArtifactsPath>
    <LibrariesAllConfigurationsArtifactsPath Condition="'$(LibrariesAllConfigurationsOverridePath)' != ''">$([MSBuild]::NormalizeDirectory('$(LibrariesAllConfigurationsOverridePath)'))</LibrariesAllConfigurationsArtifactsPath>
    <!-- Honor the RuntimeArtifactsPath property. -->
    <CoreCLRArtifactsPath Condition="'$(CoreCLRArtifactsPath)' == '' and '$(RuntimeArtifactsPath)' != ''">$([MSBuild]::NormalizeDirectory('$(RuntimeArtifactsPath)'))</CoreCLRArtifactsPath>
    <MonoArtifactsPath Condition="'$(MonoArtifactsPath)' == '' and '$(RuntimeArtifactsPath)' != ''">$([MSBuild]::NormalizeDirectory('$(RuntimeArtifactsPath)'))</MonoArtifactsPath>
    <LibrariesTargetOSConfigurationArchitecture Condition="'$(LibrariesTargetOSConfigurationArchitecture)' == ''">$(TargetOS)-$(LibrariesConfiguration)-$(TargetArchitecture)</LibrariesTargetOSConfigurationArchitecture>
  </PropertyGroup>
  <!-- Set up default live asset paths if no overrides provided. -->
  <PropertyGroup>
    <CoreCLRArtifactsPath Condition="'$(CoreCLRArtifactsPath)' == ''">$([MSBuild]::NormalizeDirectory('$(RepoRoot)', 'artifacts', 'bin', 'coreclr', '$(TargetOS).$(TargetArchitecture).$(CoreCLRConfiguration)'))</CoreCLRArtifactsPath>
    <MonoArtifactsPath Condition="'$(MonoArtifactsPath)' == ''">$([MSBuild]::NormalizeDirectory('$(RepoRoot)', 'artifacts', 'bin', 'mono', '$(TargetOS).$(TargetArchitecture).$(MonoConfiguration)'))</MonoArtifactsPath>
    <LibrariesArtifactsPath Condition="'$(LibrariesArtifactsPath)' == ''">$([MSBuild]::NormalizeDirectory('$(RepoRoot)', 'artifacts'))</LibrariesArtifactsPath>
    <LibrariesAllConfigurationsArtifactsPath Condition="'$(LibrariesAllConfigurationsArtifactsPath)' == ''">$([MSBuild]::NormalizeDirectory('$(RepoRoot)', 'artifacts'))</LibrariesAllConfigurationsArtifactsPath>
  </PropertyGroup>
  <!-- Set up artifact subpaths. -->
  <PropertyGroup>
    <CoreCLRSharedFrameworkDir>$([MSBuild]::NormalizeDirectory('$(CoreCLRArtifactsPath)', 'sharedFramework'))</CoreCLRSharedFrameworkDir>
    <CoreCLRCrossgen2Dir>$([MSBuild]::NormalizeDirectory('$(CoreCLRArtifactsPath)', 'crossgen2'))</CoreCLRCrossgen2Dir>
    <CoreCLRILCompilerDir>$([MSBuild]::NormalizeDirectory('$(CoreCLRArtifactsPath)', 'ilc-published'))</CoreCLRILCompilerDir>
    <CoreCLRCrossILCompilerDir Condition="'$(CrossBuild)' == 'true' or '$(BuildArchitecture)' != '$(TargetArchitecture)' or '$(HostOS)' != '$(TargetOS)' or '$(EnableNativeSanitizers)' != ''">$([MSBuild]::NormalizeDirectory('$(CoreCLRArtifactsPath)', '$(BuildArchitecture)', 'ilc'))</CoreCLRCrossILCompilerDir>
    <CoreCLRAotSdkDir>$([MSBuild]::NormalizeDirectory('$(CoreCLRArtifactsPath)', 'aotsdk'))</CoreCLRAotSdkDir>
    <CoreCLRBuildIntegrationDir>$([MSBuild]::NormalizeDirectory('$(CoreCLRArtifactsPath)', 'build'))</CoreCLRBuildIntegrationDir>
    <ToolsILLinkDir>$([MSBuild]::NormalizeDirectory('$(ArtifactsBinDir)', 'ILLink.Tasks', '$(Configuration)'))</ToolsILLinkDir>
    <MonoAotCrossDir>$([MSBuild]::NormalizeDirectory('$(MonoArtifactsPath)', 'cross', $(TargetOS)-$(TargetArchitecture.ToLowerInvariant())))</MonoAotCrossDir>
    <GrpcServerDockerImageDir>$([MSBuild]::NormalizeDirectory('$(LibrariesArtifactsPath)', 'obj', 'grpcserver', 'docker'))</GrpcServerDockerImageDir>
    <LibrariesPackagesDir>$([MSBuild]::NormalizeDirectory('$(LibrariesArtifactsPath)', 'packages', '$(LibrariesConfiguration)'))</LibrariesPackagesDir>
    <LibrariesShippingPackagesDir>$([MSBuild]::NormalizeDirectory('$(LibrariesPackagesDir)', 'Shipping'))</LibrariesShippingPackagesDir>
    <LibrariesNonShippingPackagesDir>$([MSBuild]::NormalizeDirectory('$(LibrariesPackagesDir)', 'NonShipping'))</LibrariesNonShippingPackagesDir>
    <LibrariesAllConfigPackagesDir>$([MSBuild]::NormalizeDirectory('$(LibrariesAllConfigurationsArtifactsPath)', 'packages', '$(LibrariesConfiguration)'))</LibrariesAllConfigPackagesDir>
    <LibrariesAllConfigShippingPackagesDir>$([MSBuild]::NormalizeDirectory('$(LibrariesAllConfigPackagesDir)', 'Shipping'))</LibrariesAllConfigShippingPackagesDir>
    <LibrariesAllConfigNonShippingPackagesDir>$([MSBuild]::NormalizeDirectory('$(LibrariesAllConfigPackagesDir)', 'NonShipping'))</LibrariesAllConfigNonShippingPackagesDir>
    <LibrariesSharedFrameworkRefArtifactsPath Condition="'$(LibrariesSharedFrameworkRefArtifactsPath)' == ''">$(MicrosoftNetCoreAppRefPackRefDir)</LibrariesSharedFrameworkRefArtifactsPath>
    <LibrariesAllRefArtifactsPath Condition="'$(LibrariesAllRefArtifactsPath)' == ''">$([MSBuild]::NormalizeDirectory('$(LibrariesArtifactsPath)', 'bin', 'ref', '$(NetCoreAppCurrent)'))</LibrariesAllRefArtifactsPath>
    <LibrariesSharedFrameworkBinArtifactsPath Condition="'$(LibrariesSharedFrameworkBinArtifactsPath)' == ''">$(MicrosoftNetCoreAppRuntimePackRidLibTfmDir)</LibrariesSharedFrameworkBinArtifactsPath>
    <LibrariesAllBinArtifactsPath Condition="'$(LibrariesAllBinArtifactsPath)' == ''">$([MSBuild]::NormalizeDirectory('$(LibrariesArtifactsPath)', 'bin', 'runtime', '$(NetCoreAppCurrent)-$(LibrariesTargetOSConfigurationArchitecture)'))</LibrariesAllBinArtifactsPath>
    <LibrariesNativeArtifactsPath Condition="'$(LibrariesNativeArtifactsPath)' == ''">$([MSBuild]::NormalizeDirectory('$(LibrariesArtifactsPath)', 'bin', 'native', '$(NetCoreAppCurrent)-$(LibrariesTargetOSConfigurationArchitecture)'))</LibrariesNativeArtifactsPath>
    <CoreCLRCrossTargetComponentDirName Condition="'$(TargetArchitecture)' == 'arm64' and '$(BuildArchitecture)' != 'arm64'">x64</CoreCLRCrossTargetComponentDirName>
    <CoreCLRCrossTargetComponentDirName Condition="'$(TargetArchitecture)' == 'arm' and '$(BuildArchitecture)' != 'arm' and '$(TargetsWindows)' == 'true'">x86</CoreCLRCrossTargetComponentDirName>
    <CoreCLRCrossTargetComponentDirName Condition="'$(TargetArchitecture)' == 'arm' and '$(BuildArchitecture)' != 'arm' and '$(TargetsLinux)' == 'true'">x64</CoreCLRCrossTargetComponentDirName>
    <CoreCLRCrossTargetComponentDirName Condition="'$(TargetArchitecture)' == 'armel' and '$(BuildArchitecture)' != 'armel' and '$(TargetsLinux)' == 'true'">x64</CoreCLRCrossTargetComponentDirName>
    <AppHostSourcePath Condition="'$(UseLocalAppHostPack)' == 'true'">$([MSBuild]::NormalizePath('$(DotNetHostBinDir)', 'apphost$(ExeSuffix)'))</AppHostSourcePath>
    <SingleFileHostSourcePath>$([MSBuild]::NormalizePath('$(CoreCLRArtifactsPath)', 'corehost', 'singlefilehost$(ExeSuffix)'))</SingleFileHostSourcePath>
  </PropertyGroup>
  <Target Name="ResolveRuntimeFilesFromLocalBuild">
    <Error Condition="!Exists('$(CoreCLRArtifactsPath)') and '$(RuntimeFlavor)' == 'CoreCLR'" Text="The CoreCLR artifacts path does not exist '$(CoreCLRArtifactsPath)'. The 'clr' subset must be built before building this project. Configuration: '$(CoreCLRConfiguration)'. To use a different configuration, specify the 'RuntimeConfiguration' property." />
    <Error Condition="!Exists('$(MonoArtifactsPath)') and '$(RuntimeFlavor)' == 'Mono'" Text="The Mono artifacts path does not exist '$(MonoArtifactsPath)'. The 'mono' subset must be built before building this project. Configuration: '$(MonoConfiguration)'. To use a different configuration, specify the 'RuntimeConfiguration' property." />
    <PropertyGroup Condition="'$(RuntimeFlavor)' == 'CoreCLR'">
      <CoreCLRArtifactsPath>$([MSBuild]::NormalizeDirectory('$(CoreCLRArtifactsPath)'))</CoreCLRArtifactsPath>
      <CoreCLRArtifactsPdbDir>$([MSBuild]::NormalizeDirectory('$(CoreCLRArtifactsPath)','PDB'))</CoreCLRArtifactsPdbDir>
      <!--
        Even though CoreCLRSharedFrameworkDir is statically initialized, set it again in case the
        value is different after CoreCLRArtifactsPath is normalized.
      -->
      <CoreCLRSharedFrameworkDir>$([MSBuild]::NormalizeDirectory('$(CoreCLRArtifactsPath)','sharedFramework'))</CoreCLRSharedFrameworkDir>
      <CoreCLRSharedFrameworkPdbDir>$([MSBuild]::NormalizeDirectory('$(CoreCLRSharedFrameworkDir)','PDB'))</CoreCLRSharedFrameworkPdbDir>
      <CoreCLRCrossTargetComponentDir Condition="'$(CoreCLRCrossTargetComponentDirName)' != ''">$([MSBuild]::NormalizeDirectory('$(CoreCLRArtifactsPath)','$(CoreCLRCrossTargetComponentDirName)','sharedFramework'))</CoreCLRCrossTargetComponentDir>
    </PropertyGroup>
    <PropertyGroup Condition="'$(RuntimeFlavor)' == 'Mono'">
      <MonoArtifactsPath>$([MSBuild]::NormalizeDirectory('$(MonoArtifactsPath)'))</MonoArtifactsPath>
    </PropertyGroup>
    <ItemGroup Condition="'$(BuildNativeAOTRuntimePack)' == 'true'">
      <_nativeAotRuntimeFiles Include="$(CoreCLRAotSdkDir)*" />
      <RuntimeFiles Include="@(_nativeAotRuntimeFiles)" Condition="'%(Extension)' != '.xml'">
        <IsNative>true</IsNative>
      </RuntimeFiles>
    </ItemGroup>
    <ItemGroup Condition="'$(RuntimeFlavor)' == 'CoreCLR' and '$(BuildNativeAOTRuntimePack)' != 'true'">
      <RuntimeFiles Include="$(CoreCLRSharedFrameworkDir)*.*" />
      <RuntimeFiles Condition="'$(PgoInstrument)' == 'true'" Include="$(CoreCLRSharedFrameworkDir)PGD/*" />
      <CoreCLRCrossTargetFiles Condition="'$(CoreCLRCrossTargetComponentDir)' != ''" Include="$(CoreCLRCrossTargetComponentDir)*.*" IsNative="true" />
      <RuntimeFiles>
        <IsNative>true</IsNative>
      </RuntimeFiles>
      <_systemPrivateCoreLib Include="$(CoreCLRArtifactsPath)System.Private.CoreLib.dll" Condition="Exists('$(CoreCLRArtifactsPath)System.Private.CoreLib.dll')" />
      <_systemPrivateCoreLib Include="$(CoreCLRArtifactsPath)IL/System.Private.CoreLib.dll" Condition="Exists('$(CoreCLRArtifactsPath)IL/System.Private.CoreLib.dll') and '@(_systemPrivateCoreLib)' == ''" />
      <RuntimeFiles Include="@(_systemPrivateCoreLib)" />
      <RuntimeFiles Include="&#xD;&#xA;          $(CoreCLRSharedFrameworkPdbDir)*.pdb;&#xD;&#xA;          $(CoreCLRSharedFrameworkPdbDir)*.dbg;&#xD;&#xA;          $(CoreCLRSharedFrameworkPdbDir)*.dwarf" IsNative="true" />
      <RuntimeFiles Condition="Exists('$(CoreCLRArtifactsPdbDir)System.Private.CoreLib.pdb')" Include="$(CoreCLRArtifactsPdbDir)System.Private.CoreLib.pdb" />
      <RuntimeFiles Condition="Exists('$(CoreCLRArtifactsPdbDir)System.Private.CoreLib.ni.pdb')" Include="$(CoreCLRArtifactsPdbDir)System.Private.CoreLib.ni.pdb" />
      <CoreCLRCrossTargetFiles Condition="'$(CoreCLRCrossTargetComponentDir)' != ''" Include="&#xD;&#xA;          $(CoreCLRSharedFrameworkPdbDir)*.pdb;&#xD;&#xA;          $(CoreCLRSharedFrameworkPdbDir)*.dbg;&#xD;&#xA;          $(CoreCLRSharedFrameworkPdbDir)*.dwarf" />
      <CoreCLRCrossTargetFiles>
        <TargetPath>runtime/$(CoreCLRCrossTargetComponentDirName)_$(TargetArchitecture)/native</TargetPath>
        <IsNative>true</IsNative>
      </CoreCLRCrossTargetFiles>
    </ItemGroup>
    <ItemGroup Condition="'$(RuntimeFlavor)' == 'Mono'">
      <RuntimeFiles Include="$(MonoArtifactsPath)\*.*" />
      <RuntimeFiles>
        <IsNative>true</IsNative>
      </RuntimeFiles>
      <MonoFrameworkReleaseFiles Condition="'$(TargetsMacCatalyst)' == 'true' or '$(TargetsiOS)' == 'true' or '$(TargetstvOS)' == 'true'" Include="$(MonoArtifactsPath)\Mono.release.framework\*.*" />
      <MonoFrameworkDebugFiles Condition="'$(TargetsMacCatalyst)' == 'true' or '$(TargetsiOS)' == 'true' or '$(TargetstvOS)' == 'true'" Include="$(MonoArtifactsPath)\Mono.debug.framework\*.*" />
      <MonoIncludeFiles Include="$(MonoArtifactsPath)\include\**\*.*" />
      <MonoBuildFiles Include="$(MonoArtifactsPath)\build\**\*.*" />
    </ItemGroup>
    <Error Condition="'@(RuntimeFiles)' == ''" Text="The '$(RuntimeFlavor)' subset must be built before building this project." />
  </Target>
  <Target Name="EnsureLocalArtifactsExist">
    <Error Condition="!Exists('$(LibrariesSharedFrameworkRefArtifactsPath)')" Text="The 'libs' subset must be built before building this project. Missing artifacts: $(LibrariesSharedFrameworkRefArtifactsPath). Configuration: '$(LibrariesConfiguration)'. To use a different configuration, specify the 'LibrariesConfiguration' property." />
    <Error Condition="'$(IncludeOOBLibraries)' == 'true' and !Exists('$(LibrariesAllRefArtifactsPath)')" Text="The 'libs' subset must be built before building this project. Missing artifacts: $(LibrariesAllRefArtifactsPath). Configuration: '$(LibrariesConfiguration)'. To use a different configuration, specify the 'LibrariesConfiguration' property." />
  </Target>
  <!--
    Ensure artifacts exist for the more advanced paths. If the configuration is '*', don't emit
    these errors: it isn't a local dev scenario.
  -->
  <Target Name="EnsureLocalOSGroupConfigurationArchitectureSpecificArtifactsExist" Condition="'$(LibrariesTargetOSConfigurationArchitecture)' != '*'">
    <Error Condition="!Exists('$(LibrariesSharedFrameworkBinArtifactsPath)')" Text="The 'libs' subset must be built before building this project. Missing artifacts: $(LibrariesSharedFrameworkBinArtifactsPath). Configuration: '$(LibrariesConfiguration)'. To use a different configuration, specify the 'LibrariesConfiguration' property." />
    <Error Condition="'$(IncludeOOBLibraries)' == 'true' and !Exists('$(LibrariesAllBinArtifactsPath)')" Text="The 'libs' subset must be built before building this project. Missing artifacts: $(LibrariesAllBinArtifactsPath). Configuration: '$(LibrariesConfiguration)'. To use a different configuration, specify the 'LibrariesConfiguration' property." />
    <Error Condition="!Exists('$(LibrariesNativeArtifactsPath)')" Text="The 'libs' subset must be built before building this project. Missing artifacts: $(LibrariesNativeArtifactsPath). Configuration: '$(LibrariesConfiguration)'. To use a different configuration, specify the 'LibrariesConfiguration' property." />
  </Target>
  <Target Name="ResolveLibrariesRefAssembliesFromLocalBuild" DependsOnTargets="EnsureLocalArtifactsExist">
    <ItemGroup>
      <LibrariesRefAssemblies Condition="'$(IncludeOOBLibraries)' != 'true'" Include="$(LibrariesSharedFrameworkRefArtifactsPath)*.dll;$(LibrariesSharedFrameworkRefArtifactsPath)*.pdb" />
      <LibrariesRefAssemblies Condition="'$(IncludeOOBLibraries)' == 'true'" Include="$(LibrariesAllRefArtifactsPath)*.dll;$(LibrariesAllRefArtifactsPath)*.pdb" />
    </ItemGroup>
    <Error Condition="'@(LibrariesRefAssemblies)' == ''" Text="The 'libs' subset must be built before building this project." />
  </Target>
  <Target Name="ResolveLibrariesRuntimeFilesFromLocalBuild" DependsOnTargets="&#xD;&#xA;            EnsureLocalArtifactsExist;&#xD;&#xA;            EnsureLocalOSGroupConfigurationArchitectureSpecificArtifactsExist">
    <ItemGroup>
      <LibrariesRuntimeFiles Condition="'$(IncludeOOBLibraries)' != 'true'" Include="&#xD;&#xA;        $(LibrariesSharedFrameworkBinArtifactsPath)*.dll;&#xD;&#xA;        $(LibrariesSharedFrameworkBinArtifactsPath)*.pdb" IsNative="" />
      <LibrariesRuntimeFiles Condition="'$(IncludeOOBLibraries)' == 'true'" Include="&#xD;&#xA;        $(LibrariesAllBinArtifactsPath)*.dll;&#xD;&#xA;        $(LibrariesAllBinArtifactsPath)*.pdb" IsNative="" />
      <ExcludeNativeLibrariesRuntimeFiles Condition="'$(IncludeOOBLibraries)' != 'true'" Include="$(LibrariesNativeArtifactsPath)libSystem.IO.Ports.Native.*" />
      <LibrariesRuntimeFiles Include="&#xD;&#xA;        $(LibrariesNativeArtifactsPath)*.dat;&#xD;&#xA;        $(LibrariesNativeArtifactsPath)*.dll;&#xD;&#xA;        $(LibrariesNativeArtifactsPath)*.dylib;&#xD;&#xA;        $(LibrariesNativeArtifactsPath)*.a;&#xD;&#xA;        $(LibrariesNativeArtifactsPath)*.so;&#xD;&#xA;        $(LibrariesNativeArtifactsPath)*.dbg;&#xD;&#xA;        $(LibrariesNativeArtifactsPath)*.dwarf;&#xD;&#xA;        $(LibrariesNativeArtifactsPath)*.pdb" IsNative="true" Exclude="@(ExcludeNativeLibrariesRuntimeFiles)" />
      <LibrariesRuntimeFiles Condition="'$(TargetOS)' == 'android'" Include="&#xD;&#xA;        $(LibrariesNativeArtifactsPath)*.dex;&#xD;&#xA;        $(LibrariesNativeArtifactsPath)*.jar;" IsNative="true" />
      <LibrariesRuntimeFiles Condition="'$(TargetOS)' == 'browser'" Include="&#xD;&#xA;        $(LibrariesNativeArtifactsPath)dotnet.js;&#xD;&#xA;        $(LibrariesNativeArtifactsPath)dotnet.js.map;&#xD;&#xA;        $(LibrariesNativeArtifactsPath)dotnet.native.js;&#xD;&#xA;        $(LibrariesNativeArtifactsPath)dotnet.runtime.js;&#xD;&#xA;        $(LibrariesNativeArtifactsPath)dotnet.runtime.js.map;&#xD;&#xA;        $(LibrariesNativeArtifactsPath)dotnet.d.ts;&#xD;&#xA;        $(LibrariesNativeArtifactsPath)dotnet-legacy.d.ts;&#xD;&#xA;        $(LibrariesNativeArtifactsPath)package.json;&#xD;&#xA;        $(LibrariesNativeArtifactsPath)dotnet.native.wasm;&#xD;&#xA;        $(LibrariesNativeArtifactsPath)dotnet.native.js.symbols;&#xD;&#xA;        $(LibrariesNativeArtifactsPath)*.dat;" IsNative="true" />
      <!-- for threaded wasm -->
      <LibrariesRuntimeFiles Condition="'$(TargetOS)' == 'browser' and Exists('$(LibrariesNativeArtifactsPath)dotnet.native.worker.js')" Include="&#xD;&#xA;        $(LibrariesNativeArtifactsPath)dotnet.native.worker.js" IsNative="true" />
      <LibrariesRuntimeFiles Condition="'$(TargetOS)' == 'browser'" Include="&#xD;&#xA;        $(LibrariesNativeArtifactsPath)src\*.c;&#xD;&#xA;        $(LibrariesNativeArtifactsPath)src\*.js;&#xD;&#xA;        $(LibrariesNativeArtifactsPath)src\emcc-default.rsp;&#xD;&#xA;        $(LibrariesNativeArtifactsPath)src\emcc-link.rsp;&#xD;&#xA;        $(LibrariesNativeArtifactsPath)src\emcc-props.json;" NativeSubDirectory="src" IsNative="true" />
      <LibrariesRuntimeFiles Condition="'$(TargetOS)' == 'browser'" Include="$(LibrariesNativeArtifactsPath)src\es6\*.js" NativeSubDirectory="src\es6" IsNative="true" />
      <LibrariesRuntimeFiles Condition="'$(TargetOS)' == 'browser'" Include="&#xD;&#xA;        $(LibrariesNativeArtifactsPath)include\wasm\*.h;" NativeSubDirectory="include\wasm" IsNative="true" />
    </ItemGroup>
    <ItemGroup Label="Wasi" Condition="'$(TargetOS)' == 'wasi'">
      <LibrariesRuntimeFiles Include="&#xD;&#xA;        $(LibrariesNativeArtifactsPath)dotnet.wasm;&#xD;&#xA;        $(LibrariesNativeArtifactsPath)*.dat;" IsNative="true" />
      <LibrariesRuntimeFiles Include="&#xD;&#xA;        $(LibrariesNativeArtifactsPath)src\*.c" NativeSubDirectory="src" IsNative="true" />
      <LibrariesRuntimeFiles Include="&#xD;&#xA;        $(LibrariesNativeArtifactsPath)include\wasm\*.h;" NativeSubDirectory="include\wasm" IsNative="true" />
    </ItemGroup>
    <Error Condition="'@(LibrariesRuntimeFiles)' == ''" Text="The 'libs' subset must be built before building this project." />
  </Target>
  <Target Name="ResolveLibrariesFromLocalBuild" DependsOnTargets="&#xD;&#xA;            ResolveLibrariesRefAssembliesFromLocalBuild;&#xD;&#xA;            ResolveLibrariesRuntimeFilesFromLocalBuild" />
  <!--
============================================================================================================================================
  </Import>

C:\Users\calope\source\repos\runtime8\Directory.Build.targets
============================================================================================================================================
-->
  <!--
============================================================================================================================================
  <Import Project="$(RepositoryEngineeringDir)generators.targets">

C:\Users\calope\source\repos\runtime8\eng\generators.targets
============================================================================================================================================
-->
  <PropertyGroup>
    <EnableLibraryImportGenerator Condition="'$(EnableLibraryImportGenerator)' == '' and&#xD;&#xA;                                             '$(MSBuildProjectName)' == 'System.Private.CoreLib'">true</EnableLibraryImportGenerator>
  </PropertyGroup>
  <ItemGroup>
    <EnabledGenerators Include="LibraryImportGenerator" Condition="'$(EnableLibraryImportGenerator)' == 'true'" />
    <!-- If the current project is not System.Private.CoreLib, we enable the LibraryImportGenerator source generator
         when the project is a C# source project that:
         - doesn't target the a TFM that includes LibraryImportGenerator or
         - doesn't reference the live targeting pack (i.e. when inbox) and
           - references System.Private.CoreLib, or
           - references System.Runtime.InteropServices -->
    <EnabledGenerators Include="LibraryImportGenerator" Condition="'$(EnableLibraryImportGenerator)' == '' and&#xD;&#xA;                                  '$(IsSourceProject)' == 'true' and&#xD;&#xA;                                  '$(MSBuildProjectExtension)' == '.csproj' and&#xD;&#xA;                                  (&#xD;&#xA;                                    !$([MSBuild]::IsTargetFrameworkCompatible('$(TargetFramework)', 'net7.0')) or&#xD;&#xA;                                    (&#xD;&#xA;                                      '$(DisableImplicitFrameworkReferences)' == 'true' and&#xD;&#xA;                                      (&#xD;&#xA;                                        '@(Reference-&gt;AnyHaveMetadataValue('Identity', 'System.Runtime.InteropServices'))' == 'true' or&#xD;&#xA;                                        '@(ProjectReference-&gt;AnyHaveMetadataValue('Identity', '$(CoreLibProject)'))' == 'true'&#xD;&#xA;                                      )&#xD;&#xA;                                    )&#xD;&#xA;                                  )" />
    <!-- We enable the ComInterfaceGenerator source generator
         when the project is a C# source project that:
         - references System.Runtime.InteropServices directly and not through the live targeting pack (i.e. when inbox) -->
    <EnabledGenerators Include="ComInterfaceGenerator" Condition="'$(IsSourceProject)' == 'true' and&#xD;&#xA;                                  '$(MSBuildProjectExtension)' == '.csproj' and&#xD;&#xA;                                  (&#xD;&#xA;                                      '$(DisableImplicitFrameworkReferences)' == 'true' and&#xD;&#xA;                                      '@(Reference-&gt;AnyHaveMetadataValue('Identity', 'System.Runtime.InteropServices'))' == 'true'&#xD;&#xA;                                  )" />
  </ItemGroup>
  <ItemGroup Condition="'@(EnabledGenerators)' != '' and&#xD;&#xA;                        @(EnabledGenerators-&gt;AnyHaveMetadataValue('Identity', 'LibraryImportGenerator')) and&#xD;&#xA;                        !$([MSBuild]::IsTargetFrameworkCompatible('$(TargetFramework)', 'net7.0'))">
    <Compile Include="$(CoreLibSharedDir)System\Runtime\InteropServices\LibraryImportAttribute.cs" />
    <Compile Include="$(CoreLibSharedDir)System\Runtime\InteropServices\StringMarshalling.cs" />
  </ItemGroup>
  <!-- Use this complex item list based filtering to add the ProjectReference to make sure dotnet/runtime stays compatible with NuGet Static Graph Restore.
       That is required as the EnabledGenerators condition checks on the Reference and ProjectReference items and hence can't be a property condition. -->
  <ItemGroup Condition="'@(EnabledGenerators)' != ''">
    <ProjectReference Include="$(LibrariesProjectRoot)System.Runtime.InteropServices\gen\Microsoft.Interop.SourceGeneration\Microsoft.Interop.SourceGeneration.csproj" ReferenceOutputAssembly="false" OutputItemType="Analyzer" SetConfiguration="Configuration=$(LibrariesConfiguration)" />
    <ProjectReference Include="$(LibrariesProjectRoot)System.Runtime.InteropServices\gen\LibraryImportGenerator\LibraryImportGenerator.csproj" ReferenceOutputAssembly="false" OutputItemType="Analyzer" SetConfiguration="Configuration=$(LibrariesConfiguration)" Condition="@(EnabledGenerators-&gt;AnyHaveMetadataValue('Identity', 'LibraryImportGenerator'))" />
    <ProjectReference Include="$(LibrariesProjectRoot)System.Runtime.InteropServices\gen\ComInterfaceGenerator\ComInterfaceGenerator.csproj" ReferenceOutputAssembly="false" OutputItemType="Analyzer" SetConfiguration="Configuration=$(LibrariesConfiguration)" Condition="@(EnabledGenerators-&gt;AnyHaveMetadataValue('Identity', 'ComInterfaceGenerator'))" />
  </ItemGroup>
  <Target Name="ConfigureGenerators" DependsOnTargets="ConfigureLibraryImportGenerator" BeforeTargets="CoreCompile" />
  <!-- Microsoft.Interop.LibraryImportGenerator -->
  <Target Name="ConfigureLibraryImportGenerator" Condition="'@(EnabledGenerators)' != '' and @(EnabledGenerators-&gt;AnyHaveMetadataValue('Identity', 'LibraryImportGenerator'))" DependsOnTargets="ResolveProjectReferences" BeforeTargets="GenerateMSBuildEditorConfigFileShouldRun">
    <PropertyGroup>
      <LibraryImportGenerator_UseMarshalType>true</LibraryImportGenerator_UseMarshalType>
    </PropertyGroup>
  </Target>
  <!--
============================================================================================================================================
  <Import Project="$(LibrariesProjectRoot)System.Runtime.InteropServices\gen\LibraryImportGenerator\Microsoft.Interop.LibraryImportGenerator.props">

C:\Users\calope\source\repos\runtime8\src\libraries\System.Runtime.InteropServices\gen\LibraryImportGenerator\Microsoft.Interop.LibraryImportGenerator.props
============================================================================================================================================
-->
  <!--
    Define all of the configuration options supported for the LibraryImportGenerator.
    To use, set an MSBuild property with the name of the option to `true`.
    See OptionsHelper.cs for more information on usage.
  -->
  <ItemGroup>
    <!--
        Use the System.Runtime.InteropServices.Marshal type instead of
        the System.Runtime.InteropServices.MarshalEx type when emitting code.
      -->
    <CompilerVisibleProperty Include="LibraryImportGenerator_UseMarshalType" />
    <!--
        Generate a stub that forwards to a runtime implemented P/Invoke stub instead
        of generating a stub that handles all of the marshalling.
      -->
    <CompilerVisibleProperty Include="LibraryImportGenerator_GenerateForwarders" />
    <!-- These properies are defined by the MSBuild SDK but are used in the interop source generators' TFM calculations -->
    <CompilerVisibleProperty Include="TargetFrameworkIdentifier" />
    <CompilerVisibleProperty Include="TargetFrameworkVersion" />
  </ItemGroup>
  <!--
============================================================================================================================================
  </Import>

C:\Users\calope\source\repos\runtime8\eng\generators.targets
============================================================================================================================================
-->
  <!--
============================================================================================================================================
  </Import>

C:\Users\calope\source\repos\runtime8\Directory.Build.targets
============================================================================================================================================
-->
  <!--
============================================================================================================================================
  <Import Project="$(RepositoryEngineeringDir)python.targets">

C:\Users\calope\source\repos\runtime8\eng\python.targets
============================================================================================================================================
-->
  <Target Name="_FindPythonWindows" Condition="$([MSBuild]::IsOSPlatform(Windows)) and '$(PYTHON)' == ''" Returns="$(PYTHON)">
    <PropertyGroup>
      <_PythonLocationScript>-c "import sys; sys.stdout.write(sys.executable)"</_PythonLocationScript>
    </PropertyGroup>
    <Exec Command="py -3 $(_PythonLocationScript) 2&gt; nul || python3 $(_PythonLocationScript) 2&gt; nul || python $(_PythonLocationScript) 2&gt; nul" StandardOutputImportance="Low" EchoOff="true" ContinueOnError="ErrorAndContinue" ConsoleToMsBuild="true">
      <Output TaskParameter="ConsoleOutput" PropertyName="PYTHON" />
    </Exec>
  </Target>
  <Target Name="_FindPythonUnix" Condition="!$([MSBuild]::IsOSPlatform(Windows)) and '$(PYTHON)' == ''" Returns="$(PYTHON)">
    <Exec Command="command -v python3 || command -v python || command -v py" StandardOutputImportance="Low" EchoOff="true" ContinueOnError="ErrorAndContinue" ConsoleToMsBuild="true">
      <Output TaskParameter="ConsoleOutput" PropertyName="PYTHON" />
    </Exec>
  </Target>
  <Target Name="FindPython" DependsOnTargets="_FindPythonWindows;_FindPythonUnix">
    <Error Condition="'$(PYTHON)' == ''" Text="Python not found. Please add Python 3 to your path and try again." />
  </Target>
  <!--
============================================================================================================================================
  </Import>

C:\Users\calope\source\repos\runtime8\Directory.Build.targets
============================================================================================================================================
-->
  <!--<Import Project="$(RepositoryEngineeringDir)generatorProjects.targets" Condition="'$(IsGeneratorProject)' == 'true'" />-->
  <!--<Import Project="$(RepositoryEngineeringDir)resolveContract.targets" Condition="'$(IsSourceProject)' == 'true'" />-->
  <!--<Import Project="$(RepositoryEngineeringDir)packaging.targets" Condition="'$(IsPackable)' == 'true' and '$(MSBuildProjectExtension)' != '.pkgproj'" />-->
  <!--
  When .NET gets built from source, make the SDK aware there are bootstrap packages
  for Microsoft.NETCore.App.Runtime.<rid> and Microsoft.NETCore.App.Crossgen2.<rid>.
  -->
  <ItemGroup Condition="'$(DotNetBuildFromSource)' == 'true'">
    <KnownFrameworkReference Update="@(KnownFrameworkReference-&gt;WithMetadataValue('Identity', 'Microsoft.NETCore.App')-&gt;WithMetadataValue('TargetFramework', '$(NetCoreAppCurrent)'))">
      <RuntimePackRuntimeIdentifiers>$(PackageRID)</RuntimePackRuntimeIdentifiers>
    </KnownFrameworkReference>
    <KnownCrossgen2Pack Update="@(KnownCrossgen2Pack-&gt;WithMetadataValue('Identity', 'Microsoft.NETCore.App.Crossgen2')-&gt;WithMetadataValue('TargetFramework', '$(NetCoreAppCurrent)'))">
      <Crossgen2RuntimeIdentifiers>$(PackageRID)</Crossgen2RuntimeIdentifiers>
    </KnownCrossgen2Pack>
    <!-- Avoid references to Microsoft.AspNetCore.App.Runtime.<rid> -->
    <KnownFrameworkReference Remove="Microsoft.AspNetCore.App" />
  </ItemGroup>
  <PropertyGroup>
    <!--
      Define this here (not just in Versions.props) because the SDK resets it
      unconditionally in Microsoft.NETCoreSdk.BundledVersions.props.
    -->
    <NETCoreAppMaximumVersion>$(MajorVersion).$(MinorVersion)</NETCoreAppMaximumVersion>
    <!-- SDK sets product to assembly but we want it to be our product name -->
    <Product>Microsoft%AE .NET</Product>
    <!-- Use the .NET product branding version for informational version description -->
    <InformationalVersion Condition="'$(InformationalVersion)' == '' and '$(VersionSuffix)' == ''">$(ProductVersion)</InformationalVersion>
    <InformationalVersion Condition="'$(InformationalVersion)' == '' and '$(PreReleaseVersionLabel)' == 'servicing'">$(ProductVersion)</InformationalVersion>
    <InformationalVersion Condition="'$(InformationalVersion)' == '' and '$(VersionSuffix)' != ''">$(ProductVersion)-$(VersionSuffix)</InformationalVersion>
  </PropertyGroup>
  <ItemGroup>
    <SupportedNETCoreAppTargetFramework Include=".NETCoreApp,Version=v$(NETCoreAppMaximumVersion)" DisplayName=".NET $(NETCoreAppMaximumVersion)" Alias="net$(NETCoreAppMaximumVersion)" />
  </ItemGroup>
  <!-- The Default behavior in VS is to show files for the first target framework in TargetFrameworks property.
       This is required to show all the files corresponding to all target frameworks in VS. -->
  <ItemGroup Condition="'$(DefaultLanguageSourceExtension)' != '' and&#xD;&#xA;                        ('$(BuildingInsideVisualStudio)' == 'true' or '$(DesignTimeBuild)' == 'true')">
    <None Include="$(MSBuildProjectDirectory)\**\*$(DefaultLanguageSourceExtension)" Exclude="$(DefaultItemExcludes);$(DefaultExcludesInProjectFolder);@(Compile)" />
  </ItemGroup>
  <!-- Packaging -->
  <ItemGroup Condition="'$(IsPackable)' == 'true'">
    <!-- The sfxproj files add the license themselves. -->
    <None Include="$(LicenseFile)" PackagePath="$([System.IO.Path]::GetFileName('$(LicenseFile)'))" Pack="true" Condition="'$(MSBuildProjectExtension)' != '.sfxproj' and '$(MSBuildProjectFile)' != 'msi.csproj'" />
    <None Include="$(PackageThirdPartyNoticesFile)" PackagePath="$([System.IO.Path]::GetFileName('$(PackageThirdPartyNoticesFile)'))" Pack="true" />
  </ItemGroup>
  <PropertyGroup>
    <PackageDescription Condition="'$(PackageDescription)' == '' and '$(Description)' != ''">$(Description)</PackageDescription>
    <RuntimePackageDisclaimer>Internal implementation package not meant for direct consumption. Please do not reference directly.</RuntimePackageDisclaimer>
    <UseRuntimePackageDisclaimer Condition="'$(UseRuntimePackageDisclaimer)' == '' and&#xD;&#xA;                                            ($(MSBuildProjectName.StartsWith('runtime.native')) or '$(PackageTargetRuntime)' != '')">true</UseRuntimePackageDisclaimer>
    <PackageDescription Condition="'$(PackageDescription)' != '' and '$(UseRuntimePackageDisclaimer)' == 'true'">$(RuntimePackageDisclaimer) %0A$(PackageDescription)</PackageDescription>
    <PackageDescription Condition="'$(PackageDescription)' == '' and '$(UseRuntimePackageDisclaimer)' == 'true'">$(RuntimePackageDisclaimer)</PackageDescription>
    <!-- Keep in sync as required by the Packaging SDK in Arcade. -->
    <Description>$(PackageDescription)</Description>
  </PropertyGroup>
  <Target Name="ValidateTargetOSLowercase" Condition="!$(TargetOS.Equals($(TargetOS.ToLower()), StringComparison.InvariantCulture))">
    <Error Text="The passed-in TargetOS property value '$(TargetOS)' must be lowercase." />
  </Target>
  <ItemDefinitionGroup>
    <TargetPathWithTargetPlatformMoniker>
      <IsReferenceAssemblyProject>$(IsReferenceAssemblyProject)</IsReferenceAssemblyProject>
    </TargetPathWithTargetPlatformMoniker>
  </ItemDefinitionGroup>
  <Target Name="ValidateReferenceAssemblyProjectReferencesAndTargetFramework" AfterTargets="ResolveReferences" Condition="'$(IsReferenceAssemblyProject)' == 'true' and&#xD;&#xA;                     '$(SkipValidateReferenceAssemblyProjectReferences)' != 'true'">
    <Error Text="Reference assemblies must only reference other reference assemblies and '%(ReferencePath.ProjectReferenceOriginalItemSpec)' is not a reference assembly project and does not set 'ProduceReferenceAssembly'." Condition="'%(ReferencePath.ReferenceSourceTarget)' == 'ProjectReference' and '%(ReferencePath.IsReferenceAssemblyProject)' != 'true' and '%(ReferencePath.ReferenceAssembly)' == ''" />
    <Error Text="Reference assemblies must be TargetPlatform agnostic. $(MSBuildProjectName) incorrectly targets $(TargetFramework), platform: $(TargetPlatformIdentifier)." Condition="'$(TargetPlatformIdentifier)' != ''" />
  </Target>
  <!-- For experimental ref assemblies (which typically have the same name as a regular ref
       assembly), bump their minor file version by 100 to make it distinguishable from the regular
       ref assembly. -->
  <Target Name="UpdateExperimentalRefAssemblyFileVersion" AfterTargets="_InitializeAssemblyVersion" Condition="'$(IsReferenceAssemblyProject)' == 'true' and '$(IsExperimentalRefAssembly)' == 'true'">
    <PropertyGroup>
      <_FileVersionMaj>$(FileVersion.Split('.')[0])</_FileVersionMaj>
      <_FileVersionMin>$(FileVersion.Split('.')[1])</_FileVersionMin>
      <_FileVersionBld>$(FileVersion.Split('.')[2])</_FileVersionBld>
      <_FileVersionRev>$(FileVersion.Split('.')[3])</_FileVersionRev>
      <FileVersion>$(_FileVersionMaj).$([MSBuild]::Add($(_FileVersionMin), 100)).$(_FileVersionBld).$(_FileVersionRev)</FileVersion>
    </PropertyGroup>
  </Target>
  <!-- Allows building against source assemblies when the 'SkipUseReferenceAssembly' attribute is present on ProjectReference items. -->
  <Target Name="HandleReferenceAssemblyAttributeForProjectReferences" AfterTargets="ResolveProjectReferences" BeforeTargets="FindReferenceAssembliesForReferences" Condition="'@(ProjectReference)' != '' and '@(_ResolvedProjectReferencePaths)' != ''">
    <!-- If we have a ProjectReference to CoreLib, we need to compile against implementation assemblies, 
         and ignore architecture mismatches in those implementation assemblies. -->
    <PropertyGroup Condition="@(_ResolvedProjectReferencePaths-&gt;AnyHaveMetadataValue('MSBuildSourceProjectFile', '$(CoreLibProject)'))">
      <CompileUsingReferenceAssemblies Condition="'$(CompileUsingReferenceAssemblies)' == ''">false</CompileUsingReferenceAssemblies>
      <ResolveAssemblyWarnOrErrorOnTargetArchitectureMismatch>None</ResolveAssemblyWarnOrErrorOnTargetArchitectureMismatch>
    </PropertyGroup>
    <!-- Clear the ReferenceAssembly attribute on resolved P2Ps that set SkipUseReferenceAssembly to true. -->
    <ItemGroup>
      <_ResolvedProjectReferencePaths Condition="'%(_ResolvedProjectReferencePaths.SkipUseReferenceAssembly)' == 'true'" ReferenceAssembly="" />
    </ItemGroup>
  </Target>
  <!-- Filter out transitive P2Ps which should be excluded. -->
  <Target Name="FilterTransitiveProjectReferences" AfterTargets="IncludeTransitiveProjectReferences" Condition="'$(DisableTransitiveProjectReferences)' != 'true' and&#xD;&#xA;                     '@(DefaultReferenceExclusion)' != ''">
    <ItemGroup>
      <_transitiveProjectReferenceWithProjectName Include="@(ProjectReference-&gt;Metadata('NuGetPackageId'))" OriginalIdentity="%(Identity)" />
      <_transitiveIncludedProjectReferenceWithProjectName Include="@(_transitiveProjectReferenceWithProjectName)" Exclude="@(DefaultReferenceExclusion)" />
      <_transitiveExcludedProjectReferenceWithProjectName Include="@(_transitiveProjectReferenceWithProjectName)" Exclude="@(_transitiveIncludedProjectReferenceWithProjectName)" />
      <ProjectReference Remove="@(_transitiveExcludedProjectReferenceWithProjectName-&gt;Metadata('OriginalIdentity'))" />
    </ItemGroup>
  </Target>
  <!-- Filter out conflicting implicit assembly references. -->
  <Target Name="FilterImplicitAssemblyReferences" Condition="'$(DisableImplicitFrameworkReferences)' != 'true'" DependsOnTargets="ResolveProjectReferences" AfterTargets="ResolveTargetingPackAssets">
    <ItemGroup>
      <_targetingPackReferenceExclusion Include="$(TargetName)" />
      <_targetingPackReferenceExclusion Include="@(_ResolvedProjectReferencePaths-&gt;Metadata('Filename'))" />
      <_targetingPackReferenceExclusion Include="@(DefaultReferenceExclusion)" />
    </ItemGroup>
    <!-- Filter out shims from the targeting pack references as an opt-in. -->
    <ItemGroup Condition="'$(TargetFrameworkIdentifier)' == '.NETCoreApp' and&#xD;&#xA;                          '$(SkipTargetingPackShimReferences)' == 'true'">
      <_targetingPackReferenceExclusion Include="@(NetFxReference)" />
      <_targetingPackReferenceExclusion Include="netstandard" />
    </ItemGroup>
    <ItemGroup>
      <_targetingPackReferenceWithProjectName Include="@(Reference-&gt;WithMetadataValue('ExternallyResolved', 'true')-&gt;Metadata('Filename'))" OriginalIdentity="%(Identity)" />
      <_targetingPackIncludedReferenceWithProjectName Include="@(_targetingPackReferenceWithProjectName)" Exclude="@(_targetingPackReferenceExclusion)" />
      <_targetingPackExcludedReferenceWithProjectName Include="@(_targetingPackReferenceWithProjectName)" Exclude="@(_targetingPackIncludedReferenceWithProjectName)" />
      <Reference Remove="@(_targetingPackExcludedReferenceWithProjectName-&gt;Metadata('OriginalIdentity'))" />
    </ItemGroup>
    <ItemGroup>
      <_targetingPackAnalyzerReferenceWithProjectName Include="@(Analyzer-&gt;WithMetadataValue('ExternallyResolved', 'true')-&gt;Metadata('Filename'))" OriginalIdentity="%(Identity)" />
      <_targetingPackIncludedAnalyzerReferenceWithProjectName Include="@(_targetingPackAnalyzerReferenceWithProjectName)" Exclude="@(_targetingPackReferenceExclusion)" />
      <_targetingPackExcludedAnalyzerReferenceWithProjectName Include="@(_targetingPackAnalyzerReferenceWithProjectName)" Exclude="@(_targetingPackIncludedAnalyzerReferenceWithProjectName)" />
      <Analyzer Remove="@(_targetingPackExcludedAnalyzerReferenceWithProjectName-&gt;Metadata('OriginalIdentity'))" />
    </ItemGroup>
  </Target>
  <!--
    Arcade SDK versioning is defined by static properties in a targets file: work around this by
    moving properties based on versioning into a target.
  -->
  <Target Name="GetProductVersions">
    <PropertyGroup>
      <IncludePreReleaseLabelInPackageVersion Condition="'$(DotNetFinalVersionKind)' != 'release'">true</IncludePreReleaseLabelInPackageVersion>
      <IncludePreReleaseLabelInPackageVersion Condition="'$(SuppressFinalPackageVersion)' == 'true'">true</IncludePreReleaseLabelInPackageVersion>
      <IncludePreReleaseLabelInPackageVersion Condition="'$(IsShipping)' != 'true'">true</IncludePreReleaseLabelInPackageVersion>
      <IncludeBuildNumberInPackageVersion Condition="'$(StabilizePackageVersion)' != 'true'">true</IncludeBuildNumberInPackageVersion>
      <IncludeBuildNumberInPackageVersion Condition="'$(SuppressFinalPackageVersion)' == 'true'">true</IncludeBuildNumberInPackageVersion>
      <IncludeBuildNumberInPackageVersion Condition="'$(IsShipping)' != 'true'">true</IncludeBuildNumberInPackageVersion>
      <ProductVersionSuffix Condition="'$(IncludePreReleaseLabelInPackageVersion)' == 'true'">-$(VersionSuffix)</ProductVersionSuffix>
      <ProductBandVersion Condition="'$(ProductBandVersion)' == ''">$(MajorVersion).$(MinorVersion)</ProductBandVersion>
      <ProductionVersion Condition="'$(ProductionVersion)' == ''">$(ProductBandVersion).$(PatchVersion)</ProductionVersion>
      <ProductVersion>$(ProductionVersion)$(ProductVersionSuffix)</ProductVersion>
      <SharedFrameworkNugetVersion>$(ProductVersion)</SharedFrameworkNugetVersion>
      <NuGetVersion>$(SharedFrameworkNugetVersion)</NuGetVersion>
      <InstallersRelativePath>Runtime/$(SharedFrameworkNugetVersion)/</InstallersRelativePath>
      <!--
        By default, we are always building the nuget packages for HostPolicy, HostFXR and
        Dotnet/AppHost. Thus, the properties (below) are always set to $(ProductVersion).
        However, there are scenarios when only some of these components will change (e.g. during
        servicing, we may only change HostPolicy but not HostFXR and Dotnet/AppHost). In such cases,
        pass the appropriate version value(s) as argument to the build command in order to override;
        e.g. 'build -p:HostPolicyVersion=x.y.z ...'
      -->
      <HostVersion Condition="'$(HostVersion)' == ''">$(ProductVersion)</HostVersion>
      <AppHostVersion Condition="'$(AppHostVersion)' == ''">$(ProductVersion)</AppHostVersion>
      <HostResolverVersion Condition="'$(HostResolverVersion)' == ''">$(ProductVersion)</HostResolverVersion>
      <HostPolicyVersion Condition="'$(HostPolicyVersion)' == ''">$(ProductVersion)</HostPolicyVersion>
    </PropertyGroup>
  </Target>
  <!--
============================================================================================================================================
  </Import>

C:\Users\calope\source\repos\runtime8\src\libraries\Directory.Build.targets
============================================================================================================================================
-->
  <PropertyGroup>
    <UseDefaultTestHost Condition="'$(UseDefaultTestHost)' == ''">false</UseDefaultTestHost>
    <NetCoreAppCurrentBuildSettings>$(NetCoreAppCurrent)-$(TargetOS)-$(Configuration)-$(TargetArchitecture)</NetCoreAppCurrentBuildSettings>
    <NativeBinDir>$([MSBuild]::NormalizeDirectory('$(ArtifactsBinDir)', 'native', '$(NetCoreAppCurrentBuildSettings)'))</NativeBinDir>
    <NetCoreAppCurrentTestHostPath Condition="'$(UseDefaultTestHost)' != 'true'">$([MSBuild]::NormalizeDirectory('$(ArtifactsBinDir)', 'testhost', '$(NetCoreAppCurrentBuildSettings)'))</NetCoreAppCurrentTestHostPath>
    <NetCoreAppCurrentTestHostSharedFrameworkPath Condition="'$(UseDefaultTestHost)' != 'true'">$([MSBuild]::NormalizeDirectory('$(NetCoreAppCurrentTestHostPath)', 'shared', '$(MicrosoftNetCoreAppFrameworkName)', '$(ProductVersion)'))</NetCoreAppCurrentTestHostSharedFrameworkPath>
    <NETStandard21RefPath>$([MSBuild]::NormalizeDirectory('$(NuGetPackageRoot)', 'netstandard.library.ref', '$(NETStandardLibraryRefVersion)', 'ref', 'netstandard2.1'))</NETStandard21RefPath>
    <NoWarn Condition="'$(TargetFrameworkIdentifier)' != '.NETCoreApp'">$(NoWarn);nullable</NoWarn>
    <NoWarn Condition="'$(GeneratePlatformNotSupportedAssembly)' == 'true' or '$(GeneratePlatformNotSupportedAssemblyMessage)' != ''">$(NoWarn);nullable;CA1052</NoWarn>
    <!-- Ignore Obsolete errors within the generated shims that type-forward types.
         SYSLIB0003: Code Access Security (CAS).
         SYSLIB0004: Constrained Execution Region (CER).
         SYSLIB0017: Strong name signing.
         SYSLIB0021: Derived cryptographic types.
         SYSLIB0022: Rijndael types.
         SYSLIB0023: RNGCryptoServiceProvider.
         SYSLIB0025: SuppressIldasmAttribute.
         SYSLIB0032: HandleProcessCorruptedStateExceptionsAttribute.
         SYSLIB0036: Regex.CompileToAssembly
    -->
    <NoWarn Condition="'$(IsPartialFacadeAssembly)' == 'true'">$(NoWarn);SYSLIB0003;SYSLIB0004;SYSLIB0015;SYSLIB0017;SYSLIB0021;SYSLIB0022;SYSLIB0023;SYSLIB0025;SYSLIB0032;SYSLIB0036</NoWarn>
    <!-- Ignore analyzers that recommend APIs introduced in .NET Core when targeting frameworks that lack those APIs
         to avoid issues with multitargeting.
    -->
    <NoWarn Condition="$(TargetFrameworks.Contains('net4')) or $(TargetFrameworks.Contains('netstandard'))">$(NoWarn);CA1510;CA1511;CA1512;CA1513;CA1845;CA1846;CA1847</NoWarn>
    <!-- Microsoft.NET.Sdk enables some warnings as errors out of the box.
         We want to remove some items from this list so they don't fail the build.
         Can't use 'WarningsNotAsErrors' element because vbproj doesn't honor it.
         Items to remove:
         NU1605: Package downgrade detected
    -->
    <WarningsAsErrors>$(WarningsAsErrors.Replace('NU1605', ''))</WarningsAsErrors>
    <!-- Inbox analyzers shouldn't use the live targeting / runtime pack. They better depend on an LKG to avoid layering concerns. -->
    <UseLocalTargetingRuntimePack Condition="'$(IsNETCoreAppAnalyzer)' == 'true'">false</UseLocalTargetingRuntimePack>
    <!-- By default, disable implicit framework references for NetCoreAppCurrent libraries. -->
    <DisableImplicitFrameworkReferences Condition="'$(TargetFrameworkIdentifier)' == '.NETCoreApp' and&#xD;&#xA;                                                   $([MSBuild]::VersionGreaterThanOrEquals($(TargetFrameworkVersion), '$(NETCoreAppCurrentVersion)')) and&#xD;&#xA;                                                   ('$(IsNETCoreAppRef)' == 'true' or '$(IsNETCoreAppSrc)' == 'true')">true</DisableImplicitFrameworkReferences>
    <!-- Enable trimming for any source project that's part of the shared framework.
         Don't attempt to trim PNSE assemblies which are generated from the reference source. -->
    <ILLinkTrimAssembly Condition="'$(ILLinkTrimAssembly)' == '' and&#xD;&#xA;                                   '$(TargetFrameworkIdentifier)' == '.NETCoreApp' and&#xD;&#xA;                                   '$(IsNETCoreAppSrc)' == 'true' and&#xD;&#xA;                                   '$(GeneratePlatformNotSupportedAssembly)' != 'true' and&#xD;&#xA;                                   '$(GeneratePlatformNotSupportedAssemblyMessage)' == ''">true</ILLinkTrimAssembly>
  </PropertyGroup>
  <!--
============================================================================================================================================
  <Import Project="$(RepositoryEngineeringDir)versioning.targets">

C:\Users\calope\source\repos\runtime8\eng\versioning.targets
============================================================================================================================================
-->
  <PropertyGroup>
    <!-- Libraries have never generated these attributes so don't let the SDK generate them. -->
    <GenerateAssemblyConfigurationAttribute>false</GenerateAssemblyConfigurationAttribute>
    <GenerateNeutralResourcesLanguageAttribute>false</GenerateNeutralResourcesLanguageAttribute>
    <!-- Libraries have always added a description set to assembly name so include that here. -->
    <Description Condition="'$(Description)' == ''">$(AssemblyName)</Description>
  </PropertyGroup>
  <ItemGroup Condition="'$(IsTestProject)' != 'true' and '$(IsTestSupportProject)' != 'true'">
    <AssemblyAttribute Include="System.Reflection.AssemblyMetadata">
      <_Parameter1>Serviceable</_Parameter1>
      <_Parameter2>True</_Parameter2>
    </AssemblyAttribute>
    <AssemblyAttribute Include="System.Reflection.AssemblyMetadata">
      <_Parameter1>PreferInbox</_Parameter1>
      <_Parameter2>True</_Parameter2>
    </AssemblyAttribute>
  </ItemGroup>
  <ItemGroup>
    <AssemblyAttribute Include="System.Reflection.AssemblyDefaultAliasAttribute">
      <_Parameter1>$(AssemblyName)</_Parameter1>
    </AssemblyAttribute>
    <AssemblyAttribute Include="System.Resources.NeutralResourcesLanguage" Condition="'@(EmbeddedResource)' != '' and '$(IsSourceProject)' == 'true'">
      <_Parameter1>en-US</_Parameter1>
    </AssemblyAttribute>
    <AssemblyAttribute Include="CLSCompliantAttribute" Condition="'$(CLSCompliant)' == 'true'">
      <_Parameter1>true</_Parameter1>
    </AssemblyAttribute>
  </ItemGroup>
  <Target Name="CalculateIncludeDllSafeSearchPathAttribute" DependsOnTargets="ResolveReferences" BeforeTargets="GetAssemblyAttributes" Condition="'$(IsSourceProject)' == 'true'">
    <!-- We want to apply the IncludeDllSafeSearchPathAttribute on all source assemblies that may contain DllImport -->
    <PropertyGroup Condition="'$(IncludeDllSafeSearchPathAttribute)' == '' and '@(ReferencePath)' != ''">
      <IncludeDllSafeSearchPathAttribute Condition="@(ReferencePath-&gt;AnyHaveMetadataValue('Filename', 'System.Runtime.InteropServices'))">true</IncludeDllSafeSearchPathAttribute>
      <IncludeDllSafeSearchPathAttribute Condition="@(ReferencePath-&gt;AnyHaveMetadataValue('Filename', 'System.Private.CoreLib'))">true</IncludeDllSafeSearchPathAttribute>
      <IncludeDllSafeSearchPathAttribute Condition="'$(TargetFrameworkIdentifier)' == '.NETStandard' and @(ReferencePath-&gt;AnyHaveMetadataValue('Filename', 'netstandard'))">true</IncludeDllSafeSearchPathAttribute>
      <IncludeDllSafeSearchPathAttribute Condition="'$(TargetFrameworkIdentifier)' == '.NETFramework' and @(ReferencePath-&gt;AnyHaveMetadataValue('Filename', 'mscorlib'))">true</IncludeDllSafeSearchPathAttribute>
    </PropertyGroup>
    <PropertyGroup>
      <AssemblyAttributeOrOperator Condition="'$(MSBuildProjectExtension)' == '.csproj'">|</AssemblyAttributeOrOperator>
      <AssemblyAttributeOrOperator Condition="'$(MSBuildProjectExtension)' == '.vbproj'">Or</AssemblyAttributeOrOperator>
    </PropertyGroup>
    <ItemGroup Condition="'$(IncludeDllSafeSearchPathAttribute)' == 'true'">
      <AssemblyAttribute Include="System.Runtime.InteropServices.DefaultDllImportSearchPathsAttribute">
        <_Parameter1>System.Runtime.InteropServices.DllImportSearchPath.AssemblyDirectory $(AssemblyAttributeOrOperator) System.Runtime.InteropServices.DllImportSearchPath.System32</_Parameter1>
        <_Parameter1_IsLiteral>true</_Parameter1_IsLiteral>
      </AssemblyAttribute>
    </ItemGroup>
  </Target>
  <PropertyGroup Condition="'$(TargetPlatformIdentifier)' == '' and !$(TargetFrameworks.Contains('$(TargetFramework)-browser'))">
    <CrossPlatformAndHasNoBrowserTarget>true</CrossPlatformAndHasNoBrowserTarget>
  </PropertyGroup>
  <PropertyGroup Condition="'$(TargetPlatformIdentifier)' == '' and !$(TargetFrameworks.Contains('$(TargetFramework)-wasi'))">
    <CrossPlatformAndHasNoWasiTarget>true</CrossPlatformAndHasNoWasiTarget>
  </PropertyGroup>
  <!-- Enables warnings for Android, iOS, tvOS and macCatalyst for all builds -->
  <ItemGroup>
    <SupportedPlatform Include="Android" />
    <SupportedPlatform Include="iOS" />
    <SupportedPlatform Include="tvOS" />
    <SupportedPlatform Include="macCatalyst" />
  </ItemGroup>
  <!-- Enables browser warnings for cross platform or Browser targeted builds -->
  <ItemGroup Condition="('$(TargetPlatformIdentifier)' == 'browser' or '$(CrossPlatformAndHasNoBrowserTarget)' == 'true') and '$(IsTestProject)' != 'true'">
    <SupportedPlatform Include="browser" />
  </ItemGroup>
  <ItemGroup Condition="('$(TargetPlatformIdentifier)' == 'wasi' or '$(CrossPlatformAndHasNoWasiTarget)' == 'true') and '$(IsTestProject)' != 'true'">
    <SupportedPlatform Include="wasi" />
  </ItemGroup>
  <!-- Add target platforms into MSBuild SupportedPlatform list -->
  <ItemGroup Condition="'$(IsTestProject)' != 'true'">
    <SupportedPlatform Condition="'$(TargetPlatformIdentifier)' == 'illumos'" Include="illumos" />
    <SupportedPlatform Condition="'$(TargetPlatformIdentifier)' == 'solaris'" Include="Solaris" />
    <SupportedPlatform Condition="'$(TargetPlatformIdentifier)' == 'haiku'" Include="Haiku" />
    <SupportedPlatform Condition="'$(TargetPlatformIdentifier)' != '' and&#xD;&#xA;                                  '$(TargetPlatformIdentifier)' != 'browser' and&#xD;&#xA;                                  '$(TargetPlatformIdentifier)' != 'wasi' and&#xD;&#xA;                                  '$(TargetPlatformIdentifier)' != 'windows'" Include="Unix" />
  </ItemGroup>
  <!-- Add PlatformNeutralAssembly property for targeted builds of cross platform assemblies -->
  <ItemGroup Condition="'$(TargetPlatformIdentifier)' != '' and '$(IsTestProject)' != 'true' and '@(SupportedOSPlatforms)' == ''">
    <CompilerVisibleProperty Include="PlatformNeutralAssembly" />
  </ItemGroup>
  <!-- Adds UnsupportedOSPlatform and SupportedOSPlatform attributes to the assembly when:
      * This isn't a test project
      * This is a cross-platform target
      * The build isn't targeting .NET Framework
    -->
  <Target Name="AddOSPlatformAttributes" BeforeTargets="GenerateAssemblyInfo" AfterTargets="PrepareForBuild" Condition="'$(TargetPlatformIdentifier)' == '' and&#xD;&#xA;                     '$(IsTestProject)' != 'true' and&#xD;&#xA;                     '$(TargetFrameworkIdentifier)' != '.NETFramework'&#xD;&#xA;                     and '$(AddOSPlatformAttributes)' != 'false'">
    <!-- Defensively de-dupe the values -->
    <ItemGroup>
      <_unsupportedOSPlatforms Include="$(UnsupportedOSPlatforms)" />
      <_supportedOSPlatforms Include="$(SupportedOSPlatforms)" />
    </ItemGroup>
    <ItemGroup Condition="'@(_unsupportedOSPlatforms)' != ''">
      <AssemblyAttribute Include="System.Runtime.Versioning.UnsupportedOSPlatform">
        <_Parameter1>%(_unsupportedOSPlatforms.Identity)</_Parameter1>
      </AssemblyAttribute>
      <!-- Ensure this platform is included in the platforms enabled for the CA1416 analyzer -->
      <SupportedPlatform Include="@(_unsupportedOSPlatforms)" />
    </ItemGroup>
    <ItemGroup Condition="'@(_supportedOSPlatforms)' != ''">
      <AssemblyAttribute Include="System.Runtime.Versioning.SupportedOSPlatform">
        <_Parameter1>%(_supportedOSPlatforms.Identity)</_Parameter1>
      </AssemblyAttribute>
      <!-- Ensure this platform is included in the platforms enabled for the CA1416 analyzer -->
      <SupportedPlatform Include="@(_supportedOSPlatforms)" />
    </ItemGroup>
  </Target>
  <!-- Remove assembly level attributes from certain projects.
       Use a target for that until https://github.com/dotnet/sdk/issues/14836 is implemented. -->
  <Target Name="RemoveSupportedOSTargetPlatformAttributeFromProjects" AfterTargets="GetAssemblyAttributes" BeforeTargets="CreateGeneratedAssemblyInfoInputsCacheFile">
    <ItemGroup>
      <AssemblyAttribute Remove="System.Runtime.Versioning.SupportedOSPlatformAttribute" Condition="'$(IsTestProject)' == 'true' or '$(AddOSPlatformAttributes)' == 'false'" />
      <!-- Don't include target platform attributes, since we use the target platform to represent RIDs instead. -->
      <AssemblyAttribute Remove="System.Runtime.Versioning.TargetPlatformAttribute" />
    </ItemGroup>
  </Target>
  <Target Name="GenerateRuntimeVersionFile" DependsOnTargets="GenerateNativeVersionFile">
    <PropertyGroup>
      <RuntimeVersionFile Condition="'$(RuntimeVersionFile)' == ''">$(ArtifactsObjDir)runtime_version.h</RuntimeVersionFile>
      <_RuntimeVersionFileContents><![CDATA[
#define RuntimeAssemblyMajorVersion $(MajorVersion)
#define RuntimeAssemblyMinorVersion $(MinorVersion)

#define RuntimeFileMajorVersion $(FileVersion.Split('.')[0])
#define RuntimeFileMinorVersion $(FileVersion.Split('.')[1])
#define RuntimeFileBuildVersion $(FileVersion.Split('.')[2])
#define RuntimeFileRevisionVersion $(FileVersion.Split('.')[3])

#define RuntimeProductMajorVersion $(Version.Split(".-")[0])
#define RuntimeProductMinorVersion $(Version.Split(".-")[1])
#define RuntimeProductPatchVersion $(Version.Split(".-")[2])

#define RuntimeProductVersion $(Version)
 ]]></_RuntimeVersionFileContents>
    </PropertyGroup>
    <MakeDir Directories="$([System.IO.Path]::GetDirectoryName($(RuntimeVersionFile)))" />
    <WriteLinesToFile File="$(RuntimeVersionFile)" Lines="$(_RuntimeVersionFileContents)" Overwrite="true" WriteOnlyWhenDifferent="true" />
  </Target>
  <Target Name="GenerateNativeSourcelinkFile" Condition="'$(EnableSourceLink)' == 'true'" DependsOnTargets="_CreateIntermediateOutputPath;_CopyGeneratedSourcelinkFile;_VerifyNativeSourceLinkFileExists" />
  <Target Name="_CreateIntermediateOutputPath">
    <MakeDir Directories="$(IntermediateOutputPath)" />
  </Target>
  <Target Name="_CopyGeneratedSourcelinkFile" DependsOnTargets="GenerateSourceLinkFile" Inputs="$(SourceLink)" Outputs="$(NativeSourceLinkFile)">
    <Error Condition="'$(NativeSourceLinkFile)' == ''" Text="Please set NativeSourceLinkFile to forward appropriate information for sourcelink." />
    <Copy SourceFiles="$(SourceLink)" DestinationFiles="$(NativeSourceLinkFile)" />
  </Target>
  <Target Name="_VerifyNativeSourceLinkFileExists" Condition="'$(VerifySourceLinkFileExists)' == true">
    <Error Condition="!Exists('$(NativeSourceLinkFile)')" Text="Native SourceLink file could not be made available to the native build. Ensure that $(MSBuildProjectName) ran the sourcelink targets." />
  </Target>
  <!--
============================================================================================================================================
  </Import>

C:\Users\calope\source\repos\runtime8\src\libraries\Directory.Build.targets
============================================================================================================================================
-->
  <!--<Import Project="$(RepositoryEngineeringDir)intellisense.targets" Condition="'$(IsSourceProject)' == 'true'" />-->
  <!-- Libraries-specific binplacing properties -->
  <PropertyGroup>
    <BinPlaceRef Condition="'$(BinPlaceRef)' == '' and ('$(IsReferenceAssemblyProject)' == 'true' or '$(IsRuntimeAndReferenceAssembly)' == 'true')">true</BinPlaceRef>
    <BinPlaceRuntime Condition="'$(BinPlaceRuntime)' == '' and '$(IsSourceProject)' == 'true'">true</BinPlaceRuntime>
    <BinPlaceForTargetVertical Condition="'$(BinPlaceForTargetVertical)' == ''">true</BinPlaceForTargetVertical>
  </PropertyGroup>
  <ItemGroup>
    <!-- Used by the runtime tests to prepare the CORE_ROOT layout. Don't use in libraries. -->
    <BinPlaceTargetFrameworks Include="$(NetCoreAppCurrent)-$(TargetOS)" Condition="'$(BinPlaceForTargetVertical)' == 'true'">
      <NativePath>$(LibrariesAllBinArtifactsPath)</NativePath>
      <RefPath>$(LibrariesAllRefArtifactsPath)</RefPath>
      <RuntimePath>$(LibrariesAllBinArtifactsPath)</RuntimePath>
    </BinPlaceTargetFrameworks>
    <!-- Source generator projects might multi-target. Make sure that only the netstandard2.0 compiled assets get binplaced. -->
    <BinPlaceDir Include="$(MicrosoftNetCoreAppRefPackDir)$(GeneratorProjectBaseTargetPath)" Condition="'$(IsNETCoreAppAnalyzer)' == 'true' and&#xD;&#xA;                            '$(TargetFramework)' == 'netstandard2.0'" />
    <!-- Setup the shared framework directory for testing -->
    <BinPlaceTargetFrameworks Include="$(NetCoreAppCurrent)-$(TargetOS)">
      <NativePath>$(NetCoreAppCurrentTestHostSharedFrameworkPath)</NativePath>
      <RuntimePath Condition="'$(IsNETCoreAppSrc)' == 'true'">$(NetCoreAppCurrentTestHostSharedFrameworkPath)</RuntimePath>
    </BinPlaceTargetFrameworks>
    <!-- Microsoft.NetCore.App.Ref and Microsoft.NetCore.App.Runtime targeting packs -->
    <BinPlaceTargetFrameworks Include="$(NetCoreAppCurrent)-$(TargetOS)">
      <NativePath>$(MicrosoftNetCoreAppRuntimePackNativeDir)</NativePath>
      <RefPath Condition="'$(IsNETCoreAppRef)' == 'true'">$(MicrosoftNetCoreAppRefPackRefDir)</RefPath>
      <RuntimePath Condition="'$(IsNETCoreAppSrc)' == 'true'">$(MicrosoftNetCoreAppRuntimePackRidLibTfmDir)</RuntimePath>
    </BinPlaceTargetFrameworks>
  </ItemGroup>
  <!--
============================================================================================================================================
  <Import Project="$(RepositoryEngineeringDir)targetingpacks.targets">

C:\Users\calope\source\repos\runtime8\eng\targetingpacks.targets
============================================================================================================================================
-->
  <!--
  The following properties need to be set for this logic to work correctly:
  - ProductVersion
  - NetCoreAppCurrent
  - NetCoreAppCurrentVersion
  - MicrosoftNetCoreAppFrameworkName
  - MicrosoftNetCoreAppRefPackDir
  - optional: MicrosoftNetCoreAppRuntimePackDir
  - optional: AppHostSourcePath & SingleFileHostSourcePath
-->
  <PropertyGroup>
    <LocalFrameworkOverrideName>$(MicrosoftNetCoreAppFrameworkName)</LocalFrameworkOverrideName>
    <TargetingpacksTargetsImported>true</TargetingpacksTargetsImported>
  </PropertyGroup>
  <PropertyGroup Condition="'$(DisableImplicitFrameworkReferences)' != 'true' and&#xD;&#xA;                            '$(TargetFrameworkIdentifier)' == '.NETCoreApp' and&#xD;&#xA;                            '$(TargetFrameworkVersion)' == 'v$(NetCoreAppCurrentVersion)'">
    <UseLocalTargetingRuntimePack Condition="'$(UseLocalTargetingRuntimePack)' == ''">true</UseLocalTargetingRuntimePack>
    <UseLocalAppHostPack Condition="'$(UseLocalAppHostPack)' == ''">$(UseLocalTargetingRuntimePack)</UseLocalAppHostPack>
    <EnableTargetingPackDownload>false</EnableTargetingPackDownload>
    <!-- TODO: Enable when a 8.0.100 SDK is consumed
    <EnableRuntimePackDownload>false</EnableRuntimePackDownload>
    <EnableAppHostPackDownload>false</EnableAppHostPackDownload>
    -->
    <GenerateErrorForMissingTargetingPacks>false</GenerateErrorForMissingTargetingPacks>
  </PropertyGroup>
  <!-- Add Known* items if the SDK doesn't support the TargetFramework yet. -->
  <ItemGroup Condition="'$(UseLocalTargetingRuntimePack)' == 'true'">
    <KnownFrameworkReference Include="$(LocalFrameworkOverrideName)" DefaultRuntimeFrameworkVersion="$(ProductVersion)" LatestRuntimeFrameworkVersion="$(ProductVersion)" RuntimeFrameworkName="$(LocalFrameworkOverrideName)" RuntimePackNamePatterns="$(LocalFrameworkOverrideName).Runtime.**RID**" RuntimePackRuntimeIdentifiers="linux-arm;linux-arm64;linux-musl-arm64;linux-musl-x64;linux-x64;osx-x64;rhel.6-x64;tizen.4.0.0-armel;tizen.5.0.0-armel;win-arm64;win-x64;win-x86;linux-musl-arm;osx-arm64;maccatalyst-x64;maccatalyst-arm64;linux-s390x;linux-bionic-arm;linux-bionic-arm64;linux-bionic-x64;linux-bionic-x86;freebsd-x64;freebsd-arm64" TargetFramework="$(NetCoreAppCurrent)" TargetingPackName="$(LocalFrameworkOverrideName).Ref" TargetingPackVersion="$(ProductVersion)" Condition="'@(KnownFrameworkReference)' == '' or !@(KnownFrameworkReference-&gt;AnyHaveMetadataValue('TargetFramework', '$(NetCoreAppCurrent)'))" />
    <KnownRuntimePack Include="$(LocalFrameworkOverrideName)" TargetFramework="$(NetCoreAppCurrent)" RuntimeFrameworkName="$(LocalFrameworkOverrideName)" LatestRuntimeFrameworkVersion="$(ProductVersion)" RuntimePackNamePatterns="$(LocalFrameworkOverrideName).Runtime.Mono.**RID**" RuntimePackRuntimeIdentifiers="linux-arm;linux-arm64;linux-musl-arm64;linux-musl-x64;linux-x64;osx-x64;rhel.6-x64;win-arm64;win-x64;win-x86;linux-musl-arm;osx-arm64;maccatalyst-x64;maccatalyst-arm64;linux-s390x;linux-bionic-arm;linux-bionic-arm64;linux-bionic-x64;linux-bionic-x86;browser-wasm;wasi-wasm;ios-arm64;iossimulator-arm64;iossimulator-x64;tvos-arm64;tvossimulator-arm64;tvossimulator-x64;android-arm64;android-arm;android-x64;android-x86" RuntimePackLabels="Mono" Condition="'@(KnownRuntimePack)' == '' or !@(KnownRuntimePack-&gt;AnyHaveMetadataValue('TargetFramework', '$(NetCoreAppCurrent)'))" />
    <KnownCrossgen2Pack Include="$(LocalFrameworkOverrideName).Crossgen2" TargetFramework="$(NetCoreAppCurrent)" Crossgen2PackNamePattern="$(LocalFrameworkOverrideName).Crossgen2.**RID**" Crossgen2PackVersion="$(ProductVersion)" Crossgen2RuntimeIdentifiers="linux-musl-x64;linux-x64;win-x64;linux-arm;linux-arm64;linux-musl-arm;linux-musl-arm64;osx-arm64;osx-x64;win-arm64;win-x86" Condition="'@(KnownCrossgen2Pack)' == '' or !@(KnownCrossgen2Pack-&gt;AnyHaveMetadataValue('TargetFramework', '$(NetCoreAppCurrent)'))" />
    <KnownILCompilerPack Include="Microsoft.DotNet.ILCompiler" ILCompilerPackNamePattern="runtime.**RID**.Microsoft.DotNet.ILCompiler" TargetFramework="$(NetCoreAppCurrent)" ILCompilerPackVersion="$(ProductVersion)" ILCompilerRuntimeIdentifiers="linux-musl-x64;linux-x64;win-x64;linux-arm;linux-arm64;linux-musl-arm;linux-musl-arm64;osx-arm64;osx-x64;win-arm64;win-x86" Condition="'@(KnownILCompilerPack)' == '' or !@(KnownILCompilerPack-&gt;AnyHaveMetadataValue('TargetFramework', '$(NetCoreAppCurrent)'))" />
  </ItemGroup>
  <ItemGroup Condition="'$(UseLocalAppHostPack)' == 'true'">
    <KnownAppHostPack Include="$(LocalFrameworkOverrideName)" ExcludedRuntimeIdentifiers="android" AppHostPackNamePattern="$(LocalFrameworkOverrideName).Host.**RID**" AppHostPackVersion="$(ProductVersion)" AppHostRuntimeIdentifiers="linux-arm;linux-arm64;linux-musl-arm64;linux-musl-x64;linux-x64;osx-x64;rhel.6-x64;tizen.4.0.0-armel;tizen.5.0.0-armel;win-arm64;win-x64;win-x86;linux-musl-arm;osx-arm64;linux-s390x;linux-bionic-arm;linux-bionic-arm64;linux-bionic-x64;linux-bionic-x86" TargetFramework="$(NetCoreAppCurrent)" Condition="'@(KnownAppHostPack)' == '' or !@(KnownAppHostPack-&gt;AnyHaveMetadataValue('TargetFramework', '$(NetCoreAppCurrent)'))" />
  </ItemGroup>
  <!-- Simple name references will be resolved from the targeting pack folders and should never be copied to the output. -->
  <ItemGroup>
    <Reference Update="@(Reference)">
      <Private Condition="'%(Reference.Extension)' != '.dll'">false</Private>
    </Reference>
  </ItemGroup>
  <!-- Add the resolved targeting pack to the assembly search path. -->
  <Target Name="UseTargetingPackForAssemblySearchPaths" BeforeTargets="ResolveAssemblyReferences;&#xD;&#xA;                         DesignTimeResolveAssemblyReferences" Condition="'$(TargetFrameworkIdentifier)' == '.NETCoreApp' and&#xD;&#xA;                     '$(TargetFrameworkVersion)' == 'v$(NetCoreAppCurrentVersion)' and&#xD;&#xA;                     '$(DisableImplicitFrameworkReferences)' == 'true'">
    <PropertyGroup>
      <AssemblySearchPaths>$(AssemblySearchPaths);$(MicrosoftNetCoreAppRefPackRefDir.TrimEnd('/\'))</AssemblySearchPaths>
      <DesignTimeAssemblySearchPaths>$(DesignTimeAssemblySearchPaths);$(MicrosoftNetCoreAppRefPackRefDir.TrimEnd('/\'))</DesignTimeAssemblySearchPaths>
    </PropertyGroup>
  </Target>
  <!-- 
    SDK tries to download runtime packs when RuntimeIdentifier is set, remove them from PackageDownload item.
    TODO: Remove this target when an 8.0.100 SDK is consumed that respects EnableRuntimePackDownload.
   -->
  <Target Name="RemoveRuntimePackFromDownloadItem" Condition="'$(UseLocalTargetingRuntimePack)' == 'true'" AfterTargets="ProcessFrameworkReferences">
    <ItemGroup>
      <PackageDownload Remove="@(PackageDownload)" Condition="'$(UsePackageDownload)' == 'true' and $([System.String]::Copy('%(Identity)').StartsWith('$(LocalFrameworkOverrideName).Runtime'))" />
      <PackageReference Remove="@(PackageReference)" Condition="'$(UsePackageDownload)' != 'true' and $([System.String]::Copy('%(Identity)').StartsWith('$(LocalFrameworkOverrideName).Runtime'))" />
      <PackageDownload Remove="@(PackageDownload)" Condition="'$(UsePackageDownload)' == 'true' and $([System.String]::Copy('%(Identity)').StartsWith('$(LocalFrameworkOverrideName).Crossgen2'))" />
      <PackageReference Remove="@(PackageReference)" Condition="'$(UsePackageDownload)' != 'true' and $([System.String]::Copy('%(Identity)').StartsWith('$(LocalFrameworkOverrideName).Crossgen2'))" />
      <PackageDownload Remove="@(PackageDownload)" Condition="'$(UsePackageDownload)' == 'true' and $([System.String]::Copy('%(Identity)').EndsWith('Microsoft.DotNet.ILCompiler'))" />
      <PackageReference Remove="@(PackageReference)" Condition="'$(UsePackageDownload)' != 'true' and $([System.String]::Copy('%(Identity)').EndsWith('Microsoft.DotNet.ILCompiler'))" />
    </ItemGroup>
  </Target>
  <!-- 
    TODO: Remove this target when an 8.0.100 SDK is consumed that respects EnableAppHostPackDownload.
   -->
  <Target Name="RemoveAppHostPackFromDownloadItem" Condition="'$(UseLocalAppHostPack)' == 'true'" AfterTargets="ProcessFrameworkReferences">
    <ItemGroup>
      <PackageDownload Remove="@(PackageDownload)" Condition="'$(UsePackageDownload)' == 'true' and $([System.String]::Copy('%(Identity)').StartsWith('$(LocalFrameworkOverrideName).Host'))" />
      <PackageReference Remove="@(PackageReference)" Condition="'$(UsePackageDownload)' != 'true' and $([System.String]::Copy('%(Identity)').StartsWith('$(LocalFrameworkOverrideName).Host'))" />
    </ItemGroup>
  </Target>
  <!-- Use local targeting/runtime pack for NetCoreAppCurrent. -->
  <Target Name="UpdateLocalTargetingAndRuntimePack" Condition="'$(UseLocalTargetingRuntimePack)' == 'true'" AfterTargets="ResolveFrameworkReferences">
    <Error Text="The shared framework must be built before the local targeting pack can be consumed." Condition="!Exists('$(MicrosoftNetCoreAppRefPackDir)data\FrameworkList.xml')" />
    <ItemGroup>
      <ResolvedTargetingPack Path="$(MicrosoftNetCoreAppRefPackDir.TrimEnd('/\'))" NuGetPackageVersion="$(ProductVersion)" PackageDirectory="$(MicrosoftNetCoreAppRefPackDir.TrimEnd('/\'))" Condition="'%(ResolvedTargetingPack.RuntimeFrameworkName)' == '$(LocalFrameworkOverrideName)'" />
      <ResolvedRuntimePack PackageDirectory="$(MicrosoftNetCoreAppRuntimePackDir)" Condition="'$(MicrosoftNetCoreAppRuntimePackDir)' != '' and&#xD;&#xA;                                      '%(ResolvedRuntimePack.FrameworkName)' == '$(LocalFrameworkOverrideName)'" />
      <ResolvedFrameworkReference TargetingPackPath="$(MicrosoftNetCoreAppRefPackDir.TrimEnd('/\'))" TargetingPackVersion="$(ProductVersion)" Condition="'%(Identity)' == '$(LocalFrameworkOverrideName)'">
        <RuntimePackPath Condition="'$(MicrosoftNetCoreAppRuntimePackDir)' != ''">$(MicrosoftNetCoreAppRuntimePackDir)</RuntimePackPath>
      </ResolvedFrameworkReference>
    </ItemGroup>
  </Target>
  <!-- Use local app host pack for NetCoreAppCurrent. -->
  <Target Name="UpdateLocalAppHostPack" Condition="'$(UseLocalAppHostPack)' == 'true'" AfterTargets="ResolveFrameworkReferences">
    <ItemGroup>
      <ResolvedAppHostPack Path="$(AppHostSourcePath)" PackageDirectory="$([System.IO.Path]::GetDirectoryName('$(AppHostSourcePath)'))" PathInPackage="$([System.IO.Path]::GetFileName('$(AppHostSourcePath)'))" Condition="'$(AppHostSourcePath)' != ''" />
      <ResolvedSingleFileHostPack Path="$(SingleFileHostSourcePath)" PackageDirectory="$([System.IO.Path]::GetDirectoryName('$(SingleFileHostSourcePath)'))" PathInPackage="$([System.IO.Path]::GetFileName('$(SingleFileHostSourcePath)'))" Condition="'$(SingleFileHostSourcePath)' != ''" />
    </ItemGroup>
  </Target>
  <!-- Update the local targeting pack's version as it's written into the runtimeconfig.json file to select the right framework. -->
  <Target Name="UpdateRuntimeFrameworkVersion" Condition="'$(UseLocalTargetingRuntimePack)' == 'true'" AfterTargets="ResolveTargetingPackAssets">
    <ItemGroup>
      <RuntimeFramework Version="$(ProductVersion)" Condition="'%(RuntimeFramework.FrameworkName)' == '$(LocalFrameworkOverrideName)'" />
    </ItemGroup>
  </Target>
  <!--
============================================================================================================================================
  </Import>

C:\Users\calope\source\repos\runtime8\src\libraries\Directory.Build.targets
============================================================================================================================================
-->
  <PropertyGroup>
    <!-- Libraries ref and source projects which don't bring in dependencies from outside the repository shouldn't reference compat shims. -->
    <SkipTargetingPackShimReferences Condition="'$(UseLocalTargetingRuntimePack)' == 'true' and&#xD;&#xA;                                                '$(IsTestProject)' != 'true' and&#xD;&#xA;                                                '$(IsTestSupportProject)' != 'true' and&#xD;&#xA;                                                '$(IsGeneratorProject)' != 'true'">true</SkipTargetingPackShimReferences>
  </PropertyGroup>
  <!--
============================================================================================================================================
  <Import Project="$(RepositoryEngineeringDir)codeOptimization.targets">

C:\Users\calope\source\repos\runtime8\eng\codeOptimization.targets
============================================================================================================================================
-->
  <PropertyGroup Condition="'$(IsEligibleForNgenOptimization)' == ''">
    <IsEligibleForNgenOptimization>true</IsEligibleForNgenOptimization>
    <IsEligibleForNgenOptimization Condition="'$(IsReferenceAssemblyProject)' == 'true'">false</IsEligibleForNgenOptimization>
    <IsEligibleForNgenOptimization Condition="'$(GeneratePlatformNotSupportedAssembly)' == 'true' or '$(GeneratePlatformNotSupportedAssemblyMessage)' != ''">false</IsEligibleForNgenOptimization>
    <!-- There's an issue causing IBCMerge failures because of mismatched MVIDs
           across many of our assemblies on Mac, so disable
           IBCMerge optimizations on Mac for now to unblock the official build.
           See issue https://github.com/dotnet/runtime/issues/33303
      -->
    <IsEligibleForNgenOptimization Condition="'$(TargetOS)' == 'osx' or '$(TargetsMobile)' == 'true'">false</IsEligibleForNgenOptimization>
  </PropertyGroup>
  <Target Name="SetApplyNgenOptimization" Condition="'$(IsEligibleForNgenOptimization)' == 'true'" BeforeTargets="CoreCompile">
    <PropertyGroup>
      <IbcOptimizationDataDir Condition="'$(TargetOS)' == 'unix' or '$(TargetOS)' == 'linux'">$(IbcOptimizationDataDir)linux\</IbcOptimizationDataDir>
      <IbcOptimizationDataDir Condition="'$(TargetOS)' == 'windows'">$(IbcOptimizationDataDir)windows\</IbcOptimizationDataDir>
    </PropertyGroup>
    <ItemGroup>
      <_optimizationDataAssembly Include="$(IbcOptimizationDataDir)**\$(TargetFileName)" />
    </ItemGroup>
    <PropertyGroup>
      <ApplyNgenOptimization Condition="'@(_optimizationDataAssembly)' != ''">full</ApplyNgenOptimization>
    </PropertyGroup>
  </Target>
  <!--
============================================================================================================================================
  </Import>

C:\Users\calope\source\repos\runtime8\src\libraries\Directory.Build.targets
============================================================================================================================================
-->
  <!--
============================================================================================================================================
  <Import Project="$(RepositoryEngineeringDir)references.targets">

C:\Users\calope\source\repos\runtime8\eng\references.targets
============================================================================================================================================
-->
  <PropertyGroup Condition="'$(TargetFrameworkIdentifier)' == '.NETCoreApp'">
    <!--
      Disable RAR from transitively discovering dependencies for references. This is required as we don't copy
      dependencies over into the output directory which means RAR can't resolve them.
    -->
    <_FindDependencies>false</_FindDependencies>
  </PropertyGroup>
  <!-- Project references shouldn't be copied to the output for reference, source, or generator projects. -->
  <ItemDefinitionGroup Condition="'$(IsSourceProject)' == 'true' or '$(IsReferenceAssemblyProject)' == 'true' or '$(IsGeneratorProject)' == 'true'">
    <ProjectReference>
      <Private>false</Private>
    </ProjectReference>
  </ItemDefinitionGroup>
  <ItemGroup Condition="'@(ProjectReference)' != ''">
    <_coreLibProjectReference Include="@(ProjectReference-&gt;WithMetadataValue('Identity', '$(CoreLibProject)'))" />
    <ProjectReference Update="@(_coreLibProjectReference)" Private="false">
      <SetConfiguration Condition="'$(RuntimeFlavor)' == 'CoreCLR' and&#xD;&#xA;                                   '$(Configuration)' != '$(CoreCLRConfiguration)'">Configuration=$(CoreCLRConfiguration)</SetConfiguration>
      <SetConfiguration Condition="'$(RuntimeFlavor)' == 'Mono' and&#xD;&#xA;                                   '$(Configuration)' != '$(MonoConfiguration)'">Configuration=$(MonoConfiguration)</SetConfiguration>
    </ProjectReference>
    <!-- If a CoreLib ProjectReference is present, make all P2P assets non transitive. -->
    <ProjectReference Update="@(ProjectReference-&gt;WithMetadataValue('PrivateAssets', ''))" PrivateAssets="all" Condition="'$(IsSourceProject)' == 'true' and '@(_coreLibProjectReference)' != ''" />
  </ItemGroup>
  <!-- Make shared framework assemblies not app-local (non private). -->
  <Target Name="UpdateProjectReferencesWithPrivateAttribute" AfterTargets="AssignProjectConfiguration" BeforeTargets="PrepareProjectReferences" Condition="'$(TargetFrameworkIdentifier)' == '.NETCoreApp' and&#xD;&#xA;                     ('$(IsTestProject)' == 'true' or '$(IsTestSupportProject)' == 'true') and&#xD;&#xA;                     '@(ProjectReferenceWithConfiguration)' != ''">
    <ItemGroup>
      <ProjectReferenceWithConfiguration PrivateAssets="all" Private="false" Condition="$(NetCoreAppLibrary.Contains('%(Filename);'))" />
    </ItemGroup>
  </Target>
  <Target Name="ReplaceCoreLibSrcWithRefAssemblyForCompilation" AfterTargets="FindReferenceAssembliesForReferences" Condition="'$(CompileUsingReferenceAssemblies)' != 'true' and '@(_coreLibProjectReference)' != ''">
    <ItemGroup>
      <_resolvedCoreLibProjectReference Include="@(_ResolvedProjectReferencePaths-&gt;WithMetadataValue('MSBuildSourceProjectFile','$(CoreLibProject)'))" />
      <ReferencePathWithRefAssemblies Remove="@(_resolvedCoreLibProjectReference)" />
      <ReferencePathWithRefAssemblies Include="@(_resolvedCoreLibProjectReference-&gt;Metadata('ReferenceAssembly'))" />
    </ItemGroup>
  </Target>
  <!--
============================================================================================================================================
  </Import>

C:\Users\calope\source\repos\runtime8\src\libraries\Directory.Build.targets
============================================================================================================================================
-->
  <!--
============================================================================================================================================
  <Import Project="$(RepositoryEngineeringDir)testing\tests.targets" Condition="'$(EnableTestSupport)' == 'true'">

C:\Users\calope\source\repos\runtime8\eng\testing\tests.targets
============================================================================================================================================
-->
  <PropertyGroup>
    <RunScriptWindowsCmd Condition="'$(TargetOS)' == 'windows' and '$(RunScriptWindowsCmd)' == ''">true</RunScriptWindowsCmd>
    <RunScriptWindowsCmd Condition="'$(TargetOS)' != 'windows' and '$(RunScriptWindowsCmd)' == ''">false</RunScriptWindowsCmd>
  </PropertyGroup>
  <PropertyGroup Condition="'$(RunScriptInputName)' == ''">
    <RunScriptInputName Condition="'$(RunScriptWindowsCmd)' == 'true'">RunnerTemplate.cmd</RunScriptInputName>
    <RunScriptInputName Condition="'$(RunScriptWindowsCmd)' != 'true'">RunnerTemplate.sh</RunScriptInputName>
    <RunScriptInputName Condition="'$(BuildTestsOnHelix)' == 'true' and '$(TargetsAppleMobile)' == 'true'">AppleHelixRunnerTemplate.sh</RunScriptInputName>
    <RunScriptInputName Condition="'$(BuildTestsOnHelix)' != 'true' and '$(TargetsAppleMobile)' == 'true'">AppleRunnerTemplate.sh</RunScriptInputName>
    <RunScriptInputName Condition="'$(TargetOS)' == 'android'">AndroidRunnerTemplate.sh</RunScriptInputName>
    <RunScriptInputName Condition="'$(TargetOS)' == 'wasi' and '$(OS)' != 'Windows_NT' and '$(BuildAOTTestsOnHelix)' == 'true'">WasiRunnerAOTTemplate.sh</RunScriptInputName>
    <RunScriptInputName Condition="'$(TargetOS)' == 'wasi' and '$(OS)' != 'Windows_NT' and '$(BuildAOTTestsOnHelix)' != 'true'">WasiRunnerTemplate.sh</RunScriptInputName>
    <RunScriptInputName Condition="'$(TargetOS)' == 'wasi' and '$(OS)' == 'Windows_NT'">WasiRunnerTemplate.cmd</RunScriptInputName>
    <RunScriptInputName Condition="'$(TargetOS)' == 'browser' and '$(OS)' != 'Windows_NT' and '$(BuildAOTTestsOnHelix)' == 'true'">WasmRunnerAOTTemplate.sh</RunScriptInputName>
    <RunScriptInputName Condition="'$(TargetOS)' == 'browser' and '$(OS)' != 'Windows_NT' and '$(BuildAOTTestsOnHelix)' != 'true'">WasmRunnerTemplate.sh</RunScriptInputName>
    <RunScriptInputName Condition="'$(TargetOS)' == 'browser' and '$(OS)' == 'Windows_NT'">WasmRunnerTemplate.cmd</RunScriptInputName>
    <RunScriptInputName Condition="'$(TargetsLinuxBionic)' == 'true' and '$(RunScriptWindowsCmd)' != 'true'">BionicRunnerTemplate.sh</RunScriptInputName>
    <RunScriptInputName Condition="'$(TargetsLinuxBionic)' == 'true' and '$(RunScriptWindowsCmd)' == 'true'">BionicRunnerTemplate.cmd</RunScriptInputName>
    <InnerRunScriptInputName>BionicRunOnDevice.sh</InnerRunScriptInputName>
  </PropertyGroup>
  <PropertyGroup>
    <RunScriptInputPath Condition="'$(RunScriptInputPath)' == ''">$(MSBuildThisFileDirectory)$(RunScriptInputName)</RunScriptInputPath>
    <InnerRunScriptInputPath>$(MSBuildThisFileDirectory)$(InnerRunScriptInputName)</InnerRunScriptInputPath>
    <RunScriptOutputName Condition="'$(RunScriptWindowsCmd)' != 'true'">RunTests.sh</RunScriptOutputName>
    <RunScriptOutputName Condition="'$(BuildTestsOnHelix)' == 'true' and '$(TargetsAppleMobile)' == 'true'">build-apple-app.sh</RunScriptOutputName>
    <RunScriptOutputName Condition="'$(RunScriptWindowsCmd)' == 'true' or (('$(TargetOS)' == 'browser' or '$(TargetOS)' == 'wasi') and '$(OS)' == 'Windows_NT')">RunTests.cmd</RunScriptOutputName>
    <InnerRunScriptOutputName>$(AssemblyName).sh</InnerRunScriptOutputName>
    <RunScriptOutputPath>$([MSBuild]::NormalizePath('$(OutDir)', '$(RunScriptOutputName)'))</RunScriptOutputPath>
    <InnerRunScriptOutputPath>$([MSBuild]::NormalizePath('$(OutDir)', '$(InnerRunScriptOutputName)'))</InnerRunScriptOutputPath>
    <InnerRunEnvOutputPath>$([MSBuild]::NormalizePath('$(OutDir)', 'TestEnv.txt'))</InnerRunEnvOutputPath>
    <RunScriptHostDir Condition="'$(RunScriptWindowsCmd)' == 'true'">%RUNTIME_PATH%\</RunScriptHostDir>
    <RunScriptHostDir Condition="'$(RunScriptWindowsCmd)' != 'true'">$RUNTIME_PATH/</RunScriptHostDir>
    <RunScriptHost Condition="'$(RunScriptWindowsCmd)' == 'true'">$(RunScriptHostDir)dotnet.exe</RunScriptHost>
    <RunScriptHost Condition="'$(RunScriptWindowsCmd)' != 'true'">$(RunScriptHostDir)dotnet</RunScriptHost>
  </PropertyGroup>
  <PropertyGroup>
    <!-- Default and user defined categories -->
    <_withCategories Condition="'$(WithCategories)' != ''">;$(WithCategories.Trim(';'))</_withCategories>
    <_withoutCategories Condition="'$(WithoutCategories)' != ''">;$(WithoutCategories.Trim(';'))</_withoutCategories>
    <TestScope Condition="'$(TestScope)' == '' and '$(Outerloop)' == 'true'">all</TestScope>
    <_withCategories Condition="'$(TestScope)' == 'outerloop'">$(_withCategories);OuterLoop</_withCategories>
    <_withoutCategories Condition="'$(ArchiveTests)' == 'true'">$(_withoutCategories);IgnoreForCI</_withoutCategories>
    <_withoutCategories Condition="'$(TestScope)' == '' or '$(TestScope)' == 'innerloop'">$(_withoutCategories);OuterLoop</_withoutCategories>
    <_withoutCategories Condition="!$(_withCategories.Contains('failing'))">$(_withoutCategories);failing</_withoutCategories>
  </PropertyGroup>
  <!-- For both tests.mobile.targets and tests.wasm.targets -->
  <PropertyGroup>
    <_MonoAotCrossCompilerPath>$([MSBuild]::NormalizePath($(MonoAotCrossDir), 'mono-aot-cross'))</_MonoAotCrossCompilerPath>
    <_MonoAotCrossCompilerPath Condition="$([MSBuild]::IsOSPlatform('WINDOWS'))">$(_MonoAotCrossCompilerPath).exe</_MonoAotCrossCompilerPath>
  </PropertyGroup>
  <ItemGroup>
    <MonoAotCrossCompiler Include="$(_MonoAotCrossCompilerPath)" RuntimeIdentifier="$(TargetOS)-$(TargetArchitecture.ToLowerInvariant())" />
  </ItemGroup>
  <PropertyGroup>
    <ArchiveTestsAfterTargets>PrepareForRun</ArchiveTestsAfterTargets>
    <!-- For browser we need to hook up the target with DependsOnTargets in PublishTestAsSelfContained
    because we do a Publish which runs after Build, if we run after PrepareForRun we would generated
    an empty zip because we haven't published the selfcontained app.  -->
    <ArchiveTestsAfterTargets Condition="'$(TargetOS)' == 'browser' or '$(TargetOS)' == 'wasi' or '$(TestSingleFile)' == 'true'" />
  </PropertyGroup>
  <!-- Archive test binaries. -->
  <Target Name="ArchiveTests" Condition="'$(ArchiveTests)' == 'true' and '$(IgnoreForCI)' != 'true' and ('$(TargetsMobile)' != 'true' or '$(TargetOS)' == 'browser' or '$(TargetOS)' == 'wasi' or '$(BuildTestsOnHelix)' == 'true')" AfterTargets="$(ArchiveTestsAfterTargets)" DependsOnTargets="GenerateRunScript;ZipTestArchive" />
  <Target Name="ZipTestArchive" Condition="'$(TargetsAppleMobile)' != 'true'">
    <Error Condition="'$(TestArchiveTestsDir)' == ''" Text="TestArchiveTestsDir property to archive the test folder must be set." />
    <PropertyGroup>
      <_ZipSourceDirectory>$(OutDir)</_ZipSourceDirectory>
      <_ZipSourceDirectory Condition="'$(TargetOS)' == 'browser' or '$(TargetOS)' == 'wasi' or '$(TestSingleFile)' == 'true'">$(BundleDir)</_ZipSourceDirectory>
    </PropertyGroup>
    <MakeDir Directories="$(TestArchiveTestsDir)" />
    <ZipDirectory SourceDirectory="$(_ZipSourceDirectory)" DestinationFile="$([MSBuild]::NormalizePath('$(TestArchiveTestsDir)', '$(TestProjectName).zip'))" Overwrite="true" />
    <!-- delete the BundleDir and PublishDir in CI builds to save disk space on build agents since they're no longer needed -->
    <RemoveDir Condition="'$(ContinuousIntegrationBuild)' == 'true'" Directories="$(BundleDir)" ContinueOnError="WarnAndContinue" />
    <RemoveDir Condition="'$(ContinuousIntegrationBuild)' == 'true' and '$(OS)' != 'Windows_NT'" Directories="$(PublishDir)" ContinueOnError="WarnAndContinue" />
  </Target>
  <UsingTask TaskName="GenerateRunScript" AssemblyFile="$(InstallerTasksAssemblyPath)" />
  <Target Name="GenerateRunScript">
    <PropertyGroup>
      <!-- RSP file support. -->
      <RunScriptCommand Condition="'$(RunScriptWindowsCmd)' != 'true' and (('$(TargetOS)' != 'browser' and '$(TargetOS)' != 'wasi') or '$(OS)' != 'Windows_NT')">$(RunScriptCommand) $RSP_FILE</RunScriptCommand>
      <RunScriptCommand Condition="'$(RunScriptWindowsCmd)' == 'true' or (('$(TargetOS)' == 'browser' or '$(TargetOS)' == 'wasi') and '$(OS)' == 'Windows_NT')">$(RunScriptCommand) %RSP_FILE%</RunScriptCommand>
      <!-- Escape potential user input. -->
      <RunScriptCommand>$([MSBuild]::Escape('$(RunScriptCommand)'))</RunScriptCommand>
    </PropertyGroup>
    <!-- Set $(TestDebugger) to eg c:\debuggers\windbg.exe to run tests under a debugger. -->
    <PropertyGroup Condition="'$(TestDebugger)' != ''">
      <RunScriptCommand Condition="!$(TestDebugger.Contains('devenv'))">$(TestDebugger) $(RunScriptCommand)</RunScriptCommand>
      <RunScriptCommand Condition=" $(TestDebugger.Contains('devenv'))">$(TestDebugger) /debugexe $(RunScriptCommand)</RunScriptCommand>
    </PropertyGroup>
    <ItemGroup>
      <!--
        If the PreExecutionTestScript property is set, then it should be set to the full path to a script that will be directly incorporated
        into the generated runtests script, immediately before the test is run. This can be used to set a number of JIT stress modes,
        for example. It is intended that this be as late as possible in the generated script, as close as possible to the running of the
        test. That is why this doesn't appear higher in this file. The idea is that if the included script alters managed code behavior, such as
        setting various JIT stress modes, we don't want those changes to affect any other managed code invocation (such as test infrastructure
        written in managed code).
      -->
      <RunScriptCommands Condition="'$(PreExecutionTestScript)' != ''" Include="$([System.IO.File]::ReadAllText('$(PreExecutionTestScript)'))" />
      <RunScriptCommands Include="$(RunScriptCommand)" />
      <!-- Do not put anything between this and the GenerateRunScript invocation. -->
      <RunScriptCommands Include="@(PostRunScriptCommands)" />
    </ItemGroup>
    <PropertyGroup Condition="'$(RunScriptOutputDirectory)' != ''">
      <RunScriptOutputPath>$([MSBuild]::NormalizePath('$(RunScriptOutputDirectory)', '$(RunScriptOutputName)'))</RunScriptOutputPath>
    </PropertyGroup>
    <GenerateRunScript RunCommands="@(RunScriptCommands)" SetCommands="@(SetScriptCommands)" TemplatePath="$(RunScriptInputPath)" OutputPath="$(RunScriptOutputPath)" />
    <Exec Condition="'$(TargetOS)' != 'windows' and '$(OS)' != 'Windows_NT'" Command="chmod +x $(RunScriptOutputPath)" />
    <Copy Condition="'$(TargetsLinuxBionic)' == 'true'" SourceFiles="$(InnerRunScriptInputPath)" DestinationFiles="$(InnerRunScriptOutputPath)" />
    <Exec Condition="'$(TargetsLinuxBionic)' == 'true' and '$(OS)' != 'Windows_NT'" Command="chmod +x $(InnerRunScriptOutputPath)" />
    <PropertyGroup Condition="'$(TargetsLinuxBionic)' == 'true'">
      <_AndroidArchitecture Condition="'$(TargetArchitecture)' == 'x64'">x86_64</_AndroidArchitecture>
      <_AndroidArchitecture Condition="'$(TargetArchitecture)' == 'x86'">x86</_AndroidArchitecture>
      <_AndroidArchitecture Condition="'$(TargetArchitecture)' == 'arm64'">arm64-v8a</_AndroidArchitecture>
      <_AndroidArchitecture Condition="'$(TargetArchitecture)' == 'arm'">armeabi-v7a</_AndroidArchitecture>
      <_BionicTestEnv>
ASSEMBLY_NAME=$(AssemblyName).dll
TEST_ARCH=$(_AndroidArchitecture)
      </_BionicTestEnv>
    </PropertyGroup>
    <WriteLinesToFile Condition="'$(TargetsLinuxBionic)' == 'true'" File="$(InnerRunEnvOutputPath)" Overwrite="true" Lines="$(_BionicTestEnv)" Encoding="ascii" />
  </Target>
  <Target Name="RunTests">
    <PropertyGroup Condition="'$(TargetsMobile)' != 'true'">
      <RunTestsCommand>"$(RunScriptOutputPath)"</RunTestsCommand>
      <!-- Use runtime path only for the live built shared framework (NetCoreAppCurrent). -->
      <RunTestsCommand Condition="'$(TargetFrameworkIdentifier)' == '.NETCoreApp' and&#xD;&#xA;                                  $([MSBuild]::VersionGreaterThanOrEquals($(TargetFrameworkVersion), '$(NETCoreAppCurrentVersion)'))">$(RunTestsCommand) --runtime-path "$(NetCoreAppCurrentTestHostPath.TrimEnd('\/'))"</RunTestsCommand>
      <RunTestsCommand Condition="'$(TestRspFile)' != '' and '$(RuntimeFlavor)' != 'Mono'">$(RunTestsCommand) --rsp-file "$(TestRspFile)"</RunTestsCommand>
    </PropertyGroup>
    <PropertyGroup Condition="'$(TargetsMobile)' == 'true'">
      <RunTestsCommand>"$(RunScriptOutputPath)" $(AssemblyName) $(TargetArchitecture) $(TargetOS) $(TestProjectName)</RunTestsCommand>
      <RunTestsCommand Condition="'$(TargetsAppleMobile)' == 'true'">$(RunTestsCommand) $(Configuration) $(AdditionalXHarnessArguments)</RunTestsCommand>
      <RunTestsCommand Condition="'$(TargetOS)' == 'android'">$(RunTestsCommand) $(AdditionalXHarnessArguments)</RunTestsCommand>
      <RunTestsCommand Condition="'$(TargetOS)' == 'browser'">"$(RunScriptOutputPath)" $(JSEngine) $(AssemblyName).dll $(Scenario)</RunTestsCommand>
    </PropertyGroup>
    <!-- Invoke the run script with the test host as the runtime path. -->
    <Exec Command="$(RunTestsCommand)" ContinueOnError="true" IgnoreExitCode="true" IgnoreStandardErrorWarningFormat="true">
      <Output PropertyName="TestRunExitCode" TaskParameter="ExitCode" />
    </Exec>
    <PropertyGroup Condition="'$(TestRunExitCode)' != '0'">
      <TestResultsPath>$(OutputPath)$(TestResultsName)</TestResultsPath>
      <TestRunErrorMessage>One or more tests failed while running tests from '$(TestProjectName)'.</TestRunErrorMessage>
      <TestRunErrorMessage Condition="Exists('$(TestResultsPath)')">$(TestRunErrorMessage) Please check $(TestResultsPath) for details!</TestRunErrorMessage>
    </PropertyGroup>
    <Error Condition="'$(TestRunExitCode)' != '0'" Text="$(TestRunErrorMessage)" />
  </Target>
  <!--<Import Project="$(MSBuildThisFileDirectory)tests.mobile.targets" Condition="'$(TargetsMobile)' == 'true'" />-->
  <!--<Import Project="$(MSBuildThisFileDirectory)tests.singlefile.targets" Condition="'$(TestSingleFile)' == 'true'" />-->
  <!--
============================================================================================================================================
  <Import Project="$(MSBuildThisFileDirectory)xunit\xunit.targets" Condition="'$(TestFramework)' == 'xunit'">

C:\Users\calope\source\repos\runtime8\eng\testing\xunit\xunit.targets
============================================================================================================================================
-->
  <ItemGroup>
    <!-- Upgrade the NETStandard.Library transitive xunit dependency to avoid transitive 1.x NS dependencies. -->
    <PackageReference Include="NETStandard.Library" Version="$(NetStandardLibraryVersion)" Condition="'$(TargetFrameworkIdentifier)' == '.NETCoreApp'" />
  </ItemGroup>
  <PropertyGroup Condition="'$(TargetFrameworkIdentifier)' == '.NETFramework'">
    <AutoGenerateBindingRedirects Condition="'$(AutoGenerateBindingRedirects)' == ''">true</AutoGenerateBindingRedirects>
    <GenerateBindingRedirectsOutputType Condition="'$(GenerateBindingRedirectsOutputType)' == ''">true</GenerateBindingRedirectsOutputType>
  </PropertyGroup>
  <!-- Run target (F5) support. -->
  <PropertyGroup>
    <RunWorkingDirectory Condition="'$(RunWorkingDirectory)' == ''">$(OutDir)</RunWorkingDirectory>
  </PropertyGroup>
  <PropertyGroup Condition="'$(TargetFrameworkIdentifier)' == '.NETCoreApp' and '$(RunCommand)' == ''">
    <RunCommand>$(DotNetTool)</RunCommand>
    <RunArguments>test $(TargetPath) --settings $(OutDir).runsettings</RunArguments>
  </PropertyGroup>
  <PropertyGroup Condition="'$(TargetFrameworkIdentifier)' == '.NETFramework'">
    <RunCommand>$(DevEnvDir)Extensions\TestPlatform\vstest.console.exe</RunCommand>
    <RunArguments>$(TargetPath) --settings:$(OutDir).runsettings</RunArguments>
  </PropertyGroup>
  <!--
============================================================================================================================================
  <Import Project="$(MSBuildThisFileDirectory)xunit.console.targets">

C:\Users\calope\source\repos\runtime8\eng\testing\xunit\xunit.console.targets
============================================================================================================================================
-->
  <PropertyGroup>
    <CopyLocalLockFileAssemblies>true</CopyLocalLockFileAssemblies>
    <TestResultsName>testResults.xml</TestResultsName>
    <UseXunitExcludesTxtFile Condition="'$(TargetOS)' == 'android' or '$(TargetOS)' == 'ios' or '$(TargetOS)' == 'iossimulator' or '$(TargetOS)' == 'tvos' or '$(TargetOS)' == 'tvossimulator' or '$(TargetOS)' == 'maccatalyst'">true</UseXunitExcludesTxtFile>
  </PropertyGroup>
  <PropertyGroup Condition="'$(TargetsMobile)' != 'true' and '$(TestSingleFile)' != 'true'">
    <_depsFileArgument Condition="'$(GenerateDependencyFile)' == 'true'">--depsfile $(AssemblyName).deps.json</_depsFileArgument>
    <RunScriptCommand Condition="'$(TargetFrameworkIdentifier)' == '.NETCoreApp'">"$(RunScriptHost)" exec --runtimeconfig $(AssemblyName).runtimeconfig.json $(_depsFileArgument) xunit.console.dll</RunScriptCommand>
    <RunScriptCommand Condition="'$(TargetFrameworkIdentifier)' == '.NETFramework'">xunit.console.exe</RunScriptCommand>
    <RunScriptCommand>$(RunScriptCommand) $(TargetFileName)</RunScriptCommand>
    <RunScriptCommand>$(RunScriptCommand) -xml $(TestResultsName)</RunScriptCommand>
    <RunScriptCommand>$(RunScriptCommand) -nologo</RunScriptCommand>
    <RunScriptCommand Condition="'$(ArchiveTests)' == 'true'">$(RunScriptCommand) -nocolor</RunScriptCommand>
    <RunScriptCommand Condition="'$(TargetFrameworkIdentifier)' == '.NETFramework' and '$(TestDisableAppDomain)' == 'true'">$(RunScriptCommand) -noappdomain</RunScriptCommand>
    <RunScriptCommand Condition="'$(TestDisableParallelization)' == 'true'">$(RunScriptCommand) -maxthreads 1</RunScriptCommand>
    <RunScriptCommand Condition="'$(XUnitShowProgress)' == 'true'">$(RunScriptCommand) -verbose</RunScriptCommand>
  </PropertyGroup>
  <PropertyGroup Condition="'$(UseXunitExcludesTxtFile)' != 'true'">
    <!-- Add local and global options to the argument stack. -->
    <RunScriptCommand Condition="'$(XUnitMethodName)' != ''">$(RunScriptCommand) -method $(XUnitMethodName)</RunScriptCommand>
    <RunScriptCommand Condition="'$(XUnitClassName)' != ''">$(RunScriptCommand) -class $(XUnitClassName)</RunScriptCommand>
    <!-- Add to run argument string -->
    <RunScriptCommand>$(RunScriptCommand)$(_withCategories.Replace(';', ' -trait category='))</RunScriptCommand>
    <RunScriptCommand>$(RunScriptCommand)$(_withoutCategories.Replace(';', ' -notrait category='))</RunScriptCommand>
    <!-- User passed in options. -->
    <RunScriptCommand Condition="'$(XUnitOptions)' != ''">$(RunScriptCommand) $(XUnitOptions)</RunScriptCommand>
  </PropertyGroup>
  <PropertyGroup Condition="'$(UseXunitExcludesTxtFile)' == 'true'">
    <XunitExcludesTxtFileContent>$(_withoutCategories.Replace(';', '%0dcategory='))</XunitExcludesTxtFileContent>
  </PropertyGroup>
  <PropertyGroup Condition="'$(TestSingleFile)' == 'true'">
    <!-- Pass the results file -->
    <RunScriptCommand>$(RunScriptCommand) -xml $(TestResultsName)</RunScriptCommand>
  </PropertyGroup>
  <ItemGroup Condition="'$(TestSingleFile)' != 'true'">
    <PackageReference Include="Microsoft.DotNet.XUnitConsoleRunner" Version="$(MicrosoftDotNetXUnitConsoleRunnerVersion)" Condition="'$(TargetFrameworkIdentifier)' == '.NETCoreApp'" />
    <PackageReference Include="xunit.runner.console" Version="$(XUnitVersion)" GeneratePathProperty="true" Condition="'$(TargetFrameworkIdentifier)' == '.NETFramework'" />
  </ItemGroup>
  <!-- Overwrite the runner config file with the app local one. -->
  <Target Name="OverwriteDesktopTestRunnerConfigs" Condition="'$(TargetFrameworkIdentifier)' == '.NETFramework' and&#xD;&#xA;                     '$(GenerateAppConfigurationFile)' == 'true' and&#xD;&#xA;                     '@(AppConfigWithTargetPath)' != ''" AfterTargets="CopyFilesToOutputDirectory">
    <ItemGroup>
      <_testRunnerConfigDestFile Include="$(TargetDir)xunit.console.exe.config" />
    </ItemGroup>
    <Copy SourceFiles="@(AppConfigWithTargetPath)" DestinationFiles="@(_testRunnerConfigDestFile)" SkipUnchangedFiles="true" />
  </Target>
  <!-- ResolveAssemblyReferences is the target that populates ReferenceCopyLocalPaths which is what is copied to output directory. -->
  <Target Name="CopyRunnerToOutputDirectory" BeforeTargets="ResolveAssemblyReferences" Condition="'$(TargetsMobile)' != 'true' or '$(BundleXunitRunner)' == 'true'">
    <ItemGroup>
      <!-- Copy test runner to output directory -->
      <None Include="$([System.IO.Path]::GetDirectoryName('$(XunitConsole472Path)'))\*" Exclude="$([System.IO.Path]::GetDirectoryName('$(XunitConsole472Path)'))\xunit.console.*exe.config;&#xD;&#xA;                     $([System.IO.Path]::GetDirectoryName('$(XunitConsole472Path)'))\xunit.console.x86.exe;&#xD;&#xA;                     $([System.IO.Path]::GetDirectoryName('$(XunitConsole472Path)'))\xunit.abstractions.dll" Condition="'$(TargetFrameworkIdentifier)' == '.NETFramework' and '$(XunitConsole472Path)' != ''" CopyToOutputDirectory="PreserveNewest" Visible="false" />
      <_xunitConsoleNetCoreExclude Condition="'$(GenerateDependencyFile)' != 'true' and '$(XunitConsoleNetCore21AppPath)' != ''" Include="$([System.IO.Path]::GetDirectoryName('$(XunitConsoleNetCore21AppPath)'))\xunit.console.deps.json" />
      <None Include="$([System.IO.Path]::GetDirectoryName('$(XunitConsoleNetCore21AppPath)'))\*" Exclude="@(_xunitConsoleNetCoreExclude)" Condition="'$(TargetFrameworkIdentifier)' == '.NETCoreApp' and '$(XunitConsoleNetCore21AppPath)' != ''" CopyToOutputDirectory="PreserveNewest" Visible="false" />
    </ItemGroup>
    <!-- Workaround for https://github.com/xunit/xunit/issues/1651 -->
    <ItemGroup Condition="'$(ArchiveTests)' != 'true'">
      <None Remove="$(Pkgxunit_runner_visualstudio)\build\net452\xunit.runner.utility.net452.dll" />
      <None Remove="$(Pkgxunit_runner_visualstudio)\build\net452\xunit.runner.reporters.net452.dll" />
      <None Remove="$(Pkgxunit_runner_visualstudio)\build\netcoreapp2.1\xunit.runner.utility.netcoreapp10.dll" />
      <None Remove="$(Pkgxunit_runner_visualstudio)\build\netcoreapp2.1\xunit.runner.reporters.netcoreapp10.dll" />
    </ItemGroup>
  </Target>
  <!--
============================================================================================================================================
  </Import>

C:\Users\calope\source\repos\runtime8\eng\testing\xunit\xunit.targets
============================================================================================================================================
-->
  <!--
============================================================================================================================================
  </Import>

C:\Users\calope\source\repos\runtime8\eng\testing\tests.targets
============================================================================================================================================
-->
  <!-- Main test targets -->
  <Target Name="Test" DependsOnTargets="$(TestDependsOn)" />
  <!--
============================================================================================================================================
  <Import Project="$(MSBuildThisFileDirectory)outerBuild.targets" Condition="'$(IsCrossTargetingBuild)' == 'true'">

C:\Users\calope\source\repos\runtime8\eng\testing\outerBuild.targets
============================================================================================================================================
-->
  <Target Name="Test" DependsOnTargets="GetProjectWithBestTargetFrameworks">
    <MSBuild Projects="@(InnerBuildProjectsWithBestTargetFramework)" Targets="Test" />
  </Target>
  <Target Name="VSTest" DependsOnTargets="GetProjectWithBestTargetFrameworks">
    <MSBuild Projects="@(InnerBuildProjectsWithBestTargetFramework)" Targets="VSTest" />
  </Target>
  <!--
============================================================================================================================================
  </Import>

C:\Users\calope\source\repos\runtime8\eng\testing\tests.targets
============================================================================================================================================
-->
  <!--
============================================================================================================================================
  </Import>

C:\Users\calope\source\repos\runtime8\src\libraries\Directory.Build.targets
============================================================================================================================================
-->
  <!--<Import Project="$(RepositoryEngineeringDir)testing\linker\trimmingTests.targets" Condition="'$(IsPublishedAppTestProject)' == 'true'" />-->
  <!--
============================================================================================================================================
  <Import Project="$(RepositoryEngineeringDir)testing\runtimeConfiguration.targets">

C:\Users\calope\source\repos\runtime8\eng\testing\runtimeConfiguration.targets
============================================================================================================================================
-->
  <PropertyGroup>
    <!-- By default copy the test runtime config file for executable test projects (+ test support projects). -->
    <IsTestProjectOrExecutable Condition="'$(IsTestProject)' == 'true' or&#xD;&#xA;                                          '$(OutputType.ToLower())' == 'exe'">true</IsTestProjectOrExecutable>
    <GenerateRuntimeConfigurationFiles Condition="'$(GenerateRuntimeConfigurationFiles)' == '' and&#xD;&#xA;                                                  '$(TargetFrameworkIdentifier)' == '.NETCoreApp' and&#xD;&#xA;                                                  '$(IsTestProjectOrExecutable)' == 'true'">true</GenerateRuntimeConfigurationFiles>
    <GenerateAppConfigurationFile Condition="'$(IsTestProjectOrExecutable)' == 'true' and&#xD;&#xA;                                             '$(TargetFrameworkIdentifier)' == '.NETFramework'">true</GenerateAppConfigurationFile>
    <AppConfig Condition="'$(GenerateAppConfigurationFile)' == 'true'">$(MSBuildThisFileDirectory)netfx.exe.config</AppConfig>
  </PropertyGroup>
  <!--
============================================================================================================================================
  </Import>

C:\Users\calope\source\repos\runtime8\src\libraries\Directory.Build.targets
============================================================================================================================================
-->
  <!--
============================================================================================================================================
  <Import Project="$(RepositoryEngineeringDir)testing\runsettings.targets" Condition="'$(EnableRunSettingsSupport)' == 'true'">

C:\Users\calope\source\repos\runtime8\eng\testing\runsettings.targets
============================================================================================================================================
-->
  <PropertyGroup>
    <RunSettingsInputFilePath>$(MSBuildThisFileDirectory).runsettings</RunSettingsInputFilePath>
    <RunSettingsIntermediateOutputFilePath>$(ArtifactsObjDir)$(TargetOS)-$(Configuration)-$(TargetArchitecture).runsettings</RunSettingsIntermediateOutputFilePath>
    <RunSettingsAppOutputFilePath>$(OutDir).runsettings</RunSettingsAppOutputFilePath>
    <CreateIntermediateRunSettingsFile Condition="'$(CreateIntermediateRunSettingsFile)' == ''">false</CreateIntermediateRunSettingsFile>
    <RunSettingsOutputFilePath Condition="'$(CreateIntermediateRunSettingsFile)' == 'true'">$(RunSettingsIntermediateOutputFilePath)</RunSettingsOutputFilePath>
    <RunSettingsOutputFilePath Condition="'$(CreateIntermediateRunSettingsFile)' != 'true'">$(RunSettingsAppOutputFilePath)</RunSettingsOutputFilePath>
    <VsCodeRunSettingsOutputFilePath>$(ArtifactsObjDir)vscode/.runsettings</VsCodeRunSettingsOutputFilePath>
    <!-- Set RunSettingsFilePath property which is read by VSTest. -->
    <RunSettingsFilePath Condition="Exists('$(RunSettingsAppOutputFilePath)')">$(RunSettingsAppOutputFilePath)</RunSettingsFilePath>
    <!-- Use an intermediate runsettings file if the app hasn't been built yet to enable VSTest discovery. -->
    <RunSettingsFilePath Condition="'$(RunSettingsFilePath)' == '' and Exists('$(RunSettingsIntermediateOutputFilePath)')">$(RunSettingsIntermediateOutputFilePath)</RunSettingsFilePath>
    <PrepareForRunDependsOn>GenerateRunSettingsFile;$(PrepareForRunDependsOn)</PrepareForRunDependsOn>
  </PropertyGroup>
  <PropertyGroup>
    <_testFilter Condition="'$(_withCategories)' != ''">$(_withCategories.Replace(';', '&amp;amp;category='))</_testFilter>
    <_testFilter Condition="'$(_withoutCategories)' != ''">$(_testFilter)$(_withoutCategories.Replace(';', '&amp;amp;category!='))</_testFilter>
    <_testFilter>$(_testFilter.Trim('&amp;amp;'))</_testFilter>
  </PropertyGroup>
  <Target Name="GenerateRunSettingsFile" DependsOnTargets="SetupCoverageFilter">
    <PropertyGroup>
      <RunSettingsFileContent>$([System.IO.File]::ReadAllText('$(RunSettingsInputFilePath)'))</RunSettingsFileContent>
      <RunSettingsFileContent Condition="'$(TestDisableParallelization)' == 'true'">$(RunSettingsFileContent.Replace('$$MAXCPUCOUNT$$', '1'))</RunSettingsFileContent>
      <RunSettingsFileContent Condition="'$(TestDisableParallelization)' != 'true'">$(RunSettingsFileContent.Replace('$$MAXCPUCOUNT$$', '0'))</RunSettingsFileContent>
      <!-- Arm64 is currently not a known TargetPlatform value in VSTEST: https://github.com/microsoft/vstest/issues/2566 -->
      <RunSettingsFileContent Condition="'$(TargetArchitecture)' != 'arm64'">$(RunSettingsFileContent.Replace('$$TARGETPLATFORM$$', '<TargetPlatform>$(TargetArchitecture)</TargetPlatform>'))</RunSettingsFileContent>
      <RunSettingsFileContent Condition="'$(TargetArchitecture)' == 'arm64'">$(RunSettingsFileContent.Replace('$$TARGETPLATFORM$$', ''))</RunSettingsFileContent>
      <RunSettingsFileContent>$(RunSettingsFileContent.Replace('$$COVERAGE_INCLUDE$$', '$(CoverageIncludeFilter)')
                                                      .Replace('$$COVERAGE_EXCLUDEBYFILE$$', '$(CoverageExcludeByFileFilter)')
                                                      .Replace('$$COVERAGE_INCLUDEDIRECTORY$$', '$(CoverageIncludeDirectoryFilter)')
                                                      .Replace('$$COVERAGE_ENABLED$$', '$([MSBuild]::ValueOrDefault('$(Coverage)', 'false'))')
                                                      .Replace('$$DISABLEPARALLELIZATION$$', '$([MSBuild]::ValueOrDefault('$(TestDisableParallelization)', 'false'))')
                                                      .Replace('$$DISABLEAPPDOMAIN$$', '$([MSBuild]::ValueOrDefault('$(TestDisableAppDomain)', 'false'))')
                                                      .Replace('$$TESTCASEFILTER$$', '$(_testFilter)')
                                                      .Replace('$$DOTNETHOSTPATH$$', '$(NetCoreAppCurrentTestHostPath)$([System.IO.Path]::GetFileName('$(DotNetTool)'))'))</RunSettingsFileContent>
    </PropertyGroup>
    <WriteLinesToFile File="$(RunSettingsOutputFilePath)" Lines="$(RunSettingsFileContent)" WriteOnlyWhenDifferent="true" Overwrite="true" />
    <WriteLinesToFile File="$(VsCodeRunSettingsOutputFilePath)" Lines="$(RunSettingsFileContent)" WriteOnlyWhenDifferent="true" Overwrite="true" Condition="'$(CreateVsCodeRunSettingsFile)' == 'true'" />
    <!-- Set RunSettingsFilePath property which is read by VSTest. -->
    <PropertyGroup>
      <RunSettingsFilePath>$(RunSettingsOutputFilePath)</RunSettingsFilePath>
    </PropertyGroup>
  </Target>
  <!--
============================================================================================================================================
  </Import>

C:\Users\calope\source\repos\runtime8\src\libraries\Directory.Build.targets
============================================================================================================================================
-->
  <!--
============================================================================================================================================
  <Import Project="$(RepositoryEngineeringDir)testing\coverage.targets" Condition="'$(EnableRunSettingsSupport)' == 'true' or '$(EnableCoverageSupport)' == 'true'">

C:\Users\calope\source\repos\runtime8\eng\testing\coverage.targets
============================================================================================================================================
-->
  <Target Name="SetupCoverageFilter" DependsOnTargets="ResolveReferences">
    <!--
      We need to filter the data to only the assembly being tested. Otherwise we will gather tons of data about other assemblies.
      If the code being tested is part of the runtime itself, it requires special treatment.
    -->
    <PropertyGroup Condition="'$(AssemblyBeingTested)' == '' and '$(CreateIntermediateRunSettingsFile)' != 'true'">
      <_ProjectDirectoryUnderSourceDir>$(MSBuildProjectDirectory.SubString($(LibrariesProjectRoot.Length)))</_ProjectDirectoryUnderSourceDir>
      <AssemblyBeingTested>$(_ProjectDirectoryUnderSourceDir.SubString(0, $(_ProjectDirectoryUnderSourceDir.IndexOfAny("\\/"))))</AssemblyBeingTested>
    </PropertyGroup>
    <!--
      By default, code coverage data is only gathered for the assembly being tested.
      CoverageAssemblies can be passed in to the build to gather coverage on additional assemblies.
    -->
    <ItemGroup>
      <CoverageInclude Include="$(AssemblyBeingTested)" />
      <CoverageInclude Include="System.Private.CoreLib" Condition="'$(TestRuntime)' == 'true'" />
      <CoverageInclude Include="@(AssembliesBeingTested)" />
      <CoverageInclude Include="$(CoverageAssemblies)" Condition="'$(CoverageAssemblies)' != ''" />
      <CoverageInclude Include="$(AssemblyName)" Condition="'$(CoverageIncludeTests)' == 'true'" />
      <!-- Include analyzer assemblies which are referenced via the P2P protocol. -->
      <CoverageInclude Include="@(ReferencePath-&gt;WithMetadataValue('OutputItemType', 'Analyzer')-&gt;WithMetadataValue('ReferenceSourceTarget', 'ProjectReference')-&gt;Metadata('Filename'))" />
    </ItemGroup>
    <PropertyGroup>
      <CoverageIncludeFilter>@(CoverageInclude -> '[%(Identity)]*', ',')</CoverageIncludeFilter>
    </PropertyGroup>
    <PropertyGroup Condition="'@(CoverageExcludeByFile)' != ''">
      <CoverageExcludeByFileFilter>@(CoverageExcludeByFile -> '%(Identity)', ',')</CoverageExcludeByFileFilter>
    </PropertyGroup>
    <PropertyGroup Condition="'@(CoverageIncludeDirectory)' != ''">
      <CoverageIncludeDirectoryFilter>@(CoverageIncludeDirectory -> '$(NetCoreAppCurrentTestHostPath)%(Identity)', ',')</CoverageIncludeDirectoryFilter>
    </PropertyGroup>
  </Target>
  <!-- TODO remove when https://github.com/coverlet-coverage/coverlet/issues/834 is fixed. -->
  <Target Name="AddCoverageCommand" BeforeTargets="GenerateRunScript" DependsOnTargets="SetupCoverageFilter" Condition="'$(Coverage)' == 'true' and '$(TargetFrameworkIdentifier)' == '.NETCoreApp'">
    <PropertyGroup>
      <CoverageOutputPath Condition="'$(CoverageOutputPath)' == ''">coverage.opencover.xml</CoverageOutputPath>
      <CoverageReportInputPath Condition="'$(CoverageReportInputPath)' == ''">$(CoverageOutputPath)</CoverageReportInputPath>
      <CoverageReportDir Condition="'$(CoverageReportDir)' == ''">$([MSBuild]::NormalizeDirectory('$(OutDir)', 'report'))</CoverageReportDir>
      <RunScriptCommand>"$(DotNetTool)" coverlet "$(TargetFileName)" --target "$(RunScriptHost)" --targetargs "$(RunScriptCommand.Replace('"$(RunScriptHost)"', ''))" --format "opencover" --output "$(CoverageOutputPath)" --verbosity "normal" --use-source-link --does-not-return-attribute DoesNotReturn</RunScriptCommand>
      <RunScriptCommand Condition="'@(CoverageExcludeByFile)' != ''">$(RunScriptCommand) --exclude-by-file @(CoverageExcludeByFile -> '"%(Identity)"', ' --exclude-by-file ')</RunScriptCommand>
      <RunScriptCommand Condition="'@(CoverageIncludeDirectory)' != ''">$(RunScriptCommand) --include-directory @(CoverageIncludeDirectory -> '"$(RunScriptHostDir)%(Identity)"', ' --include-directory ')</RunScriptCommand>
      <RunScriptCommand Condition="'$(CoverageIncludeTests)' == 'true'">$(RunScriptCommand) --include-test-assembly</RunScriptCommand>
      <RunScriptCommand>$(RunScriptCommand) --include @(CoverageInclude -> '"[%(Identity)]*"', ' --include ')</RunScriptCommand>
      <CoverageReportCommandLine>"$(DotNetTool)" reportgenerator "-reports:$(CoverageReportInputPath)" "-targetdir:$(CoverageReportDir.TrimEnd('\/'))" "-reporttypes:Html" "-verbosity:Info"</CoverageReportCommandLine>
    </PropertyGroup>
    <!-- Skip generating individual reports if a full report is generated. -->
    <ItemGroup Condition="'$(BuildAllProjects)' != 'true' and '$(SkipCoverageReport)' != 'true'">
      <PostRunScriptCommands Include="$(CoverageReportCommandLine)" />
    </ItemGroup>
  </Target>
  <!-- Build a coverage report if building an individual library with Coverage true. -->
  <Target Name="GenerateCoverageReport" Condition="'$(Coverage)' == 'true' and '$(BuildAllProjects)' != 'true' and '$(SkipCoverageReport)' != 'true'" AfterTargets="VSTest">
    <ItemGroup Condition="'$(CoverageReportInputPath)' == ''">
      <CoverageOutputFile Include="$(OutDir)TestResults\*\coverage.opencover.xml" />
    </ItemGroup>
    <PropertyGroup>
      <CoverageReportInputPath Condition="'$(CoverageReportInputPath)' == ''">%(CoverageOutputFile.Identity)</CoverageReportInputPath>
      <CoverageReportTypes Condition="'$(CoverageReportTypes)' == ''">Html</CoverageReportTypes>
      <CoverageReportVerbosity Condition="'$(CoverageReportVerbosity)' == ''">Info</CoverageReportVerbosity>
      <CoverageReportDir Condition="'$(CoverageReportDir)' == ''">$([MSBuild]::NormalizeDirectory('$(OutDir)', 'TestResults', 'report'))</CoverageReportDir>
      <CoverageReportCommand>"$(DotNetTool)" reportgenerator "-reports:$(CoverageReportInputPath)" "-targetdir:$(CoverageReportDir.TrimEnd('\/'))" "-reporttypes:$(CoverageReportTypes)" "-verbosity:$(CoverageReportVerbosity)"</CoverageReportCommand>
    </PropertyGroup>
    <Exec Command="$(CoverageReportCommand)" />
  </Target>
  <!--
    Clean the test results directory to guarantee that a report is generated from the
    newest coverage results file.
    Tracking issue https://github.com/microsoft/vstest/issues/2378.
  -->
  <Target Name="ClearTestResults" BeforeTargets="VSTest" Condition="'$(Coverage)' == 'true'">
    <RemoveDir Directories="$(OutDir)TestResults" />
  </Target>
  <!--
============================================================================================================================================
  </Import>

C:\Users\calope\source\repos\runtime8\src\libraries\Directory.Build.targets
============================================================================================================================================
-->
  <!--<Import Project="$(RepositoryEngineeringDir)slngen.targets" Condition="'$(IsSlnGen)' == 'true'" />-->
  <!--<Import Project="$(RepositoryEngineeringDir)illink.targets" Condition="'$(IsSourceProject)' == 'true'" />-->
  <!--<Import Project="$(RepositoryEngineeringDir)AvoidRestoreCycleOnSelfReference.targets" Condition="'$(AvoidRestoreCycleOnSelfReference)' == 'true'" />-->
  <!--
============================================================================================================================================
  <Import Project="$(RepositoryEngineeringDir)nativeSanitizers.targets">

C:\Users\calope\source\repos\runtime8\eng\nativeSanitizers.targets
============================================================================================================================================
-->
  <!-- Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. -->
  <PropertyGroup Condition="'$(TargetArchitecture)' == 'x64'">
    <LLVMArchitectureSuffix>x86_64</LLVMArchitectureSuffix>
  </PropertyGroup>
  <PropertyGroup Condition="'$(TargetArchitecture)' == 'x86'">
    <LLVMArchitectureSuffix>i386</LLVMArchitectureSuffix>
  </PropertyGroup>
  <PropertyGroup Condition="'$(TargetArchitecture)' == 'arm'">
    <LLVMArchitectureSuffix>armhf</LLVMArchitectureSuffix>
  </PropertyGroup>
  <PropertyGroup Condition="'$(TargetArchitecture)' == 'arm64'">
    <LLVMArchitectureSuffix>arm64</LLVMArchitectureSuffix>
  </PropertyGroup>
  <ItemGroup Condition="'$(TargetOS)' != 'windows'">
    <LinkerArg Include="-fsanitize=$(EnableNativeSanitizers)" />
  </ItemGroup>
  <ItemGroup Condition="'$(TargetOS)' == 'osx'">
    <SanitizerRuntimeToCopy Condition="$(EnableNativeSanitizers.Contains('address'))" Include="libclang_rt.asan_osx_dynamic.dylib" />
  </ItemGroup>
  <ItemGroup Condition="'$(TargetOS)' == 'windows'">
    <SanitizerRuntimeToCopy Condition="$(EnableNativeSanitizers.Contains('address'))" Include="clang_rt.asan_dynamic-$(LLVMArchitectureSuffix).dll" />
  </ItemGroup>
  <!--
============================================================================================================================================
  </Import>

C:\Users\calope\source\repos\runtime8\src\libraries\Directory.Build.targets
============================================================================================================================================
-->
  <ItemGroup Condition="'$(UseTargetFrameworkPackage)' != 'false'">
    <PackageReference Include="Microsoft.DotNet.Build.Tasks.TargetFramework" Version="$(MicrosoftDotNetBuildTasksTargetFrameworkVersion)" PrivateAssets="all" IsImplicitlyDefined="true" />
  </ItemGroup>
  <!-- Reference the GenFacades package when the assembly is a partial facade or a PNSE throwing. -->
  <ItemGroup Condition="'$(IsPartialFacadeAssembly)' == 'true' or&#xD;&#xA;                        '$(GeneratePlatformNotSupportedAssembly)' == 'true' or&#xD;&#xA;                        '$(GeneratePlatformNotSupportedAssemblyMessage)' != ''">
    <PackageReference Include="Microsoft.DotNet.GenFacades" Version="$(MicrosoftDotNetGenFacadesVersion)" PrivateAssets="all" IsImplicitlyDefined="true" />
  </ItemGroup>
  <!-- GenFacades target that is intentionally empty since we no longer need it. -->
  <Target Name="ResolveMatchingContract" />
  <!--
    Do not clean binplace assets in the ref targeting pack to avoid incremental build failures
    when the SDK tries to resolve the assets from the FrameworkList.
  -->
  <Target Name="RemoveTargetingPackIncrementalClean" Condition="'@(AdditionalCleanDirectories)' != ''" BeforeTargets="IncrementalCleanAdditionalDirectories;&#xD;&#xA;                         CleanAdditionalDirectories">
    <ItemGroup>
      <AdditionalCleanDirectories Remove="@(AdditionalCleanDirectories)" Condition="'%(Identity)' == '$(MicrosoftNetCoreAppRefPackRefDir)'" />
    </ItemGroup>
  </Target>
  <!-- Adds Nullable annotation attributes to C# non .NETCoreApp builds. -->
  <ItemGroup Condition="'$(Nullable)' != '' and&#xD;&#xA;                        '$(Nullable)' != 'disable' and&#xD;&#xA;                        '$(MSBuildProjectExtension)' == '.csproj' and&#xD;&#xA;                        '$(TargetFrameworkIdentifier)' != '.NETCoreApp'">
    <Compile Include="$(CoreLibSharedDir)System\Diagnostics\CodeAnalysis\NullableAttributes.cs" Link="System\Diagnostics\CodeAnalysis\NullableAttributes.cs" />
  </ItemGroup>
  <!-- If a tfm doesn't target .NETCoreApp but uses the platform support attributes, then we include the
       System.Runtime.Versioning*Platform* annotation attribute classes in the project as internal.

       If a project has specified assembly-level SupportedOSPlatforms or UnsupportedOSPlatforms,
       we can infer the need without having IncludePlatformAttributes set. -->
  <PropertyGroup>
    <IncludePlatformAttributes Condition="'$(IncludePlatformAttributes)' == '' and ('$(SupportedOSPlatforms)' != '' or '$(UnsupportedOSPlatforms)' != '')">true</IncludePlatformAttributes>
  </PropertyGroup>
  <ItemGroup Condition="'$(IncludePlatformAttributes)' == 'true' and '$(TargetFrameworkIdentifier)' != '.NETCoreApp'">
    <Compile Include="$(CoreLibSharedDir)System\Runtime\Versioning\PlatformAttributes.cs" Link="System\Runtime\Versioning\PlatformAttributes.cs" />
  </ItemGroup>
  <!-- Adds ObsoleteAttribute to projects that need to apply downlevel Obsoletions with DiagnosticId and UrlFormat -->
  <Choose>
    <When Condition="'$(IncludeInternalObsoleteAttribute)' == 'true' and '$(TargetFrameworkIdentifier)' != '.NETCoreApp'">
      <ItemGroup>
        <Compile Include="$(CoreLibSharedDir)System\ObsoleteAttribute.cs" Link="System\ObsoleteAttribute.cs" />
      </ItemGroup>
      <PropertyGroup>
        <!-- Suppress CS0436 to allow ObsoleteAttribute to be internally defined and used in netstandard -->
        <NoWarn>$(NoWarn);CS0436</NoWarn>
      </PropertyGroup>
    </When>
  </Choose>
  <ItemGroup Condition="'$(IncludeIndexRangeTypes)' == 'true' and '$(TargetFrameworkIdentifier)' != '.NETCoreApp'">
    <Compile Include="$(CoreLibSharedDir)System\Index.cs" Link="System\Index.cs" />
    <Compile Include="$(CoreLibSharedDir)System\Range.cs" Link="System\Range.cs" />
    <Compile Include="$(CoreLibSharedDir)System\Numerics\Hashing.cs" Link="System\Numerics\Hashing.cs" />
  </ItemGroup>
  <PropertyGroup>
    <SkipLocalsInit Condition="'$(SkipLocalsInit)' == '' and '$(MSBuildProjectExtension)' == '.csproj' and '$(IsNETCoreAppSrc)' == 'true' and '$(TargetFrameworkIdentifier)' == '.NETCoreApp'">true</SkipLocalsInit>
  </PropertyGroup>
  <!--Instructs compiler not to emit .locals init, using SkipLocalsInitAttribute.-->
  <Choose>
    <When Condition="'$(SkipLocalsInit)' == 'true'">
      <PropertyGroup>
        <!-- This is needed to use the SkipLocalsInitAttribute. -->
        <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
      </PropertyGroup>
      <ItemGroup>
        <Compile Include="$(CommonPath)SkipLocalsInit.cs" Link="Common\SkipLocalsInit.cs" />
      </ItemGroup>
    </When>
  </Choose>
  <!--
============================================================================================================================================
  </Import>

C:\Program Files\dotnet\sdk\8.0.110\Microsoft.Common.CrossTargeting.targets
============================================================================================================================================
-->
  <!--
============================================================================================================================================
  </Import>

C:\Program Files\dotnet\sdk\8.0.110\Microsoft.CSharp.CrossTargeting.targets
============================================================================================================================================
-->
  <!--
============================================================================================================================================
  </Import>

C:\Program Files\dotnet\sdk\8.0.110\Microsoft.CSharp.targets
============================================================================================================================================
-->
  <!--
============================================================================================================================================
  <Import Project="$(MSBuildToolsPath)\Microsoft.Managed.After.targets">

C:\Program Files\dotnet\sdk\8.0.110\Microsoft.Managed.After.targets
============================================================================================================================================
-->
  <!--
***********************************************************************************************
WARNING:  DO NOT MODIFY this file unless you are knowledgeable about MSBuild and have
          created a backup copy.  Incorrect changes to this file will make it
          impossible to load or build your projects from the command-line or the IDE.

This file defines common build logic for all managed languaged: C#, VisualBasic, F#
It is imported after the common targets have been imported.

Copyright (C) Microsoft Corporation. All rights reserved.
***********************************************************************************************
-->
  <!-- Define crosstargeting for static graph, so it can identify inner and outer build graph nodes -->
  <PropertyGroup>
    <InnerBuildProperty>TargetFramework</InnerBuildProperty>
    <InnerBuildPropertyValues>TargetFrameworks</InnerBuildPropertyValues>
  </PropertyGroup>
  <PropertyGroup Condition="'$(IsGraphBuild)' == 'true'">
    <AddTransitiveProjectReferencesInStaticGraph Condition="'$(AddTransitiveProjectReferencesInStaticGraph)' == '' and '$(UsingMicrosoftNETSdk)' == 'true' and '$(DisableTransitiveProjectReferences)' != 'true'">true</AddTransitiveProjectReferencesInStaticGraph>
  </PropertyGroup>
  <ItemGroup Condition="'$(IsGraphBuild)' == 'true'">
    <!-- WPF projects generate a project with a random name at build time and then build the project via the IBuildEngine callbacks.
        Detect WPF, and exclude the generated project from static graph isolation constraint checking.
        Escape the item to avoid eager evaluation of the wildcards.
    -->
    <GraphIsolationExemptReference Condition="'$(UseWPF)' == 'true' or '@(Page)' != '' or '@(ApplicationDefinition)' != '' or '@(XamlPage)' != '' or '@(XamlAppDef)' != ''" Include="$([MSBuild]::Escape('$(MSBuildProjectDirectory)\$(MSBuildProjectName)*_wpftmp$(MSBuildProjectExtension)'))" />
  </ItemGroup>
  <!--
      Properties for extension of ProjectReferenceTargets.
      Append any current value which may have been provided in a Directory.Build.props since the intent was likely to append, not prepend.
  -->
  <PropertyGroup Condition="'$(IsGraphBuild)' == 'true' and '$(IsCrossTargetingBuild)' != 'true'">
    <!-- Empty case is for builds which do not import the target files that set BuildProjectReferences -->
    <_MainReferenceTargetForBuild Condition="'$(BuildProjectReferences)' == '' or '$(BuildProjectReferences)' == 'true'">.projectReferenceTargetsOrDefaultTargets</_MainReferenceTargetForBuild>
    <_MainReferenceTargetForBuild Condition="'$(_MainReferenceTargetForBuild)' == ''">GetTargetPath</_MainReferenceTargetForBuild>
    <ProjectReferenceTargetsForBuild>$(_MainReferenceTargetForBuild);GetNativeManifest;$(_RecursiveTargetForContentCopying);$(ProjectReferenceTargetsForBuild)</ProjectReferenceTargetsForBuild>
    <!-- Publish has the same logic as Build for the main reference target except it also takes $(NoBuild) into account. -->
    <_MainReferenceTargetForPublish Condition="'$(NoBuild)' == 'true'">GetTargetPath</_MainReferenceTargetForPublish>
    <_MainReferenceTargetForPublish Condition="'$(NoBuild)' != 'true'">$(_MainReferenceTargetForBuild)</_MainReferenceTargetForPublish>
    <ProjectReferenceTargetsForPublish>GetTargetFrameworks;$(_MainReferenceTargetForPublish);GetNativeManifest;GetCopyToPublishDirectoryItems;$(ProjectReferenceTargetsForPublish)</ProjectReferenceTargetsForPublish>
    <!-- When DeployOnBuild=true, the Publish target is hooked to the Build target -->
    <ProjectReferenceTargetsForBuild Condition="'$(DeployOnBuild)' == 'true'">$(ProjectReferenceTargetsForBuild);$(ProjectReferenceTargetsForPublish)</ProjectReferenceTargetsForBuild>
    <ProjectReferenceTargetsForRebuild Condition="'$(DeployOnBuild)' == 'true'">$(ProjectReferenceTargetsForRebuild);$(ProjectReferenceTargetsForPublish)</ProjectReferenceTargetsForRebuild>
    <ProjectReferenceTargetsForGetCopyToPublishDirectoryItems>GetCopyToPublishDirectoryItems;$(ProjectReferenceTargetsForGetCopyToPublishDirectoryItems)</ProjectReferenceTargetsForGetCopyToPublishDirectoryItems>
  </PropertyGroup>
  <PropertyGroup Condition="'$(IsGraphBuild)' == 'true' and '$(IsCrossTargetingBuild)' == 'true'">
    <ProjectReferenceTargetsForBuild>.default;$(ProjectReferenceTargetsForBuild)</ProjectReferenceTargetsForBuild>
  </PropertyGroup>
  <PropertyGroup Condition="'$(IsGraphBuild)' == 'true'">
    <ProjectReferenceTargetsForClean>Clean;$(ProjectReferenceTargetsForClean)</ProjectReferenceTargetsForClean>
    <ProjectReferenceTargetsForRebuild>$(ProjectReferenceTargetsForClean);$(ProjectReferenceTargetsForBuild);$(ProjectReferenceTargetsForRebuild)</ProjectReferenceTargetsForRebuild>
  </PropertyGroup>
  <ItemGroup Condition="'$(IsGraphBuild)' == 'true'">
    <ProjectReferenceTargets Include="Build" Targets="$(ProjectReferenceTargetsForBuildInOuterBuild)" Condition=" '$(ProjectReferenceTargetsForBuildInOuterBuild)' != '' " OuterBuild="true" />
    <ProjectReferenceTargets Include="Build" Targets="GetTargetFrameworks" OuterBuild="true" SkipNonexistentTargets="true" Condition="'$(IsCrossTargetingBuild)' != 'true'" />
    <ProjectReferenceTargets Include="Build" Targets="$(ProjectReferenceTargetsForBuild)" Condition=" '$(ProjectReferenceTargetsForBuild)' != '' " />
    <ProjectReferenceTargets Include="Clean" Targets="$(ProjectReferenceTargetsForCleanInOuterBuild)" Condition=" '$(ProjectReferenceTargetsForCleanInOuterBuild)' != '' " OuterBuild="true" />
    <ProjectReferenceTargets Include="Clean" Targets="GetTargetFrameworks" OuterBuild="true" SkipNonexistentTargets="true" Condition="'$(IsCrossTargetingBuild)' != 'true'" />
    <ProjectReferenceTargets Include="Clean" Targets="$(ProjectReferenceTargetsForClean)" Condition=" '$(ProjectReferenceTargetsForClean)' != '' " />
    <!--
     Note: SkipNonexistentTargets="true" on the following three items means that an outer build node's call to its existent GetTargetFrameworks target will fail if its inner build nodes don't define GetTargetFrameworksWithPlatformForSingleTargetFrameworks.
     This is necessary since the P2P protocol cannot express the targets called from the outer build to the inner build.
     -->
    <ProjectReferenceTargets Include="Build" Targets="GetTargetFrameworksWithPlatformForSingleTargetFramework" SkipNonexistentTargets="true" Condition="'$(IsCrossTargetingBuild)' != 'true'" />
    <ProjectReferenceTargets Include="Clean" Targets="GetTargetFrameworksWithPlatformForSingleTargetFramework" SkipNonexistentTargets="true" Condition="'$(IsCrossTargetingBuild)' != 'true'" />
    <ProjectReferenceTargets Include="Rebuild" Targets="GetTargetFrameworksWithPlatformForSingleTargetFramework" SkipNonexistentTargets="true" Condition="'$(IsCrossTargetingBuild)' != 'true'" />
    <ProjectReferenceTargets Include="Rebuild" Targets="$(ProjectReferenceTargetsForRebuild)" Condition=" '$(ProjectReferenceTargetsForRebuild)' != '' " />
    <ProjectReferenceTargets Include="Publish" Targets="$(ProjectReferenceTargetsForPublish)" Condition=" '$(ProjectReferenceTargetsForPublish)' != '' " />
    <ProjectReferenceTargets Include="GetCopyToPublishDirectoryItems" Targets="$(ProjectReferenceTargetsForGetCopyToPublishDirectoryItems)" Condition=" '$(ProjectReferenceTargetsForGetCopyToPublishDirectoryItems)' != '' " />
  </ItemGroup>
  <!--
============================================================================================================================================
  </Import>

C:\Program Files\dotnet\sdk\8.0.110\Microsoft.CSharp.targets
============================================================================================================================================
-->
  <!--
============================================================================================================================================
  </Import>

C:\Program Files\dotnet\sdk\8.0.110\Sdks\Microsoft.NET.Sdk\Sdk\Sdk.targets
============================================================================================================================================
-->
  <!--
============================================================================================================================================
  <Import Project="$(MSBuildThisFileDirectory)..\targets\Microsoft.NET.Sdk.CrossTargeting.targets" Condition="'$(IsCrossTargetingBuild)' == 'true'">

C:\Program Files\dotnet\sdk\8.0.110\Sdks\Microsoft.NET.Sdk\targets\Microsoft.NET.Sdk.CrossTargeting.targets
============================================================================================================================================
-->
  <!--
***********************************************************************************************
Microsoft.NET.Sdk.targets

WARNING:  DO NOT MODIFY this file unless you are knowledgeable about MSBuild and have
          created a backup copy.  Incorrect changes to this file will make it
          impossible to load or build your projects from the command-line or the IDE.

Copyright (c) .NET Foundation. All rights reserved.
***********************************************************************************************
-->
  <!--
============================================================================================================================================
  <Import Project="Microsoft.NET.Sdk.Common.targets">

C:\Program Files\dotnet\sdk\8.0.110\Sdks\Microsoft.NET.Sdk\targets\Microsoft.NET.Sdk.Common.targets
============================================================================================================================================
-->
  <!--
***********************************************************************************************
Microsoft.NET.Sdk.Common.targets

WARNING:  DO NOT MODIFY this file unless you are knowledgeable about MSBuild and have
          created a backup copy.  Incorrect changes to this file will make it
          impossible to load or build your projects from the command-line or the IDE.

Copyright (c) .NET Foundation. All rights reserved.
***********************************************************************************************
-->
  <!-- This file is imported by both cross-targeting and inner builds. Set properties that need to be available to both here. -->
  <PropertyGroup xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <MicrosoftNETBuildTasksDirectoryRoot>$(MSBuildThisFileDirectory)..\tools\</MicrosoftNETBuildTasksDirectoryRoot>
    <MicrosoftNETBuildTasksTFM Condition=" '$(MSBuildRuntimeType)' == 'Core'">net8.0</MicrosoftNETBuildTasksTFM>
    <MicrosoftNETBuildTasksTFM Condition=" '$(MicrosoftNETBuildTasksTFM)' == ''">net472</MicrosoftNETBuildTasksTFM>
    <MicrosoftNETBuildTasksDirectory>$(MicrosoftNETBuildTasksDirectoryRoot)$(MicrosoftNETBuildTasksTFM)\</MicrosoftNETBuildTasksDirectory>
    <MicrosoftNETBuildTasksAssembly>$(MicrosoftNETBuildTasksDirectory)Microsoft.NET.Build.Tasks.dll</MicrosoftNETBuildTasksAssembly>
    <!--
          Hardcoded list of known implicit packages that are added to project from default SDK targets implicitly.
          Should be re-visited when multiple TFM support is added to Dependencies logic.
    -->
    <DefaultImplicitPackages>Microsoft.NETCore.App;NETStandard.Library</DefaultImplicitPackages>
  </PropertyGroup>
  <!--
     Some versions of Microsoft.NET.Test.Sdk.targets change the OutputType after we've set _IsExecutable and
     HasRuntimeOutput default in Microsoft.NET.Sdk.BeforeCommon.targets. Refresh these value here for backwards
     compatibilty with that.
   -->
  <PropertyGroup xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <_IsExecutable Condition="'$(OutputType)' == 'Exe' or '$(OutputType)'=='WinExe'">true</_IsExecutable>
    <HasRuntimeOutput Condition="'$(_UsingDefaultForHasRuntimeOutput)' == 'true'">$(_IsExecutable)</HasRuntimeOutput>
  </PropertyGroup>
  <PropertyGroup Condition="'$(DotnetCliToolTargetFramework)' == ''" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <!-- Maximum supported target framework for DotnetCliProjectTools is .NET Core 2.2 -->
    <DotnetCliToolTargetFramework>netcoreapp2.2</DotnetCliToolTargetFramework>
  </PropertyGroup>
  <PropertyGroup xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <IncludeBuildOutput Condition=" '$(PackAsTool)' == 'true' ">false</IncludeBuildOutput>
    <PackageType Condition=" '$(PackAsTool)' == 'true' ">DotnetTool</PackageType>
    <RuntimeIdentifiers Condition=" '$(PackAsTool)' == 'true' ">$(RuntimeIdentifiers);$(PackAsToolShimRuntimeIdentifiers)</RuntimeIdentifiers>
  </PropertyGroup>
  <PropertyGroup Condition="'$(EnablePreviewFeatures)' == 'true' And '$(IsNetCoreAppTargetingLatestTFM)' == 'true'" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <LangVersion>Preview</LangVersion>
  </PropertyGroup>
  <UsingTask TaskName="NETSdkError" AssemblyFile="$(MicrosoftNETBuildTasksAssembly)" xmlns="http://schemas.microsoft.com/developer/msbuild/2003" />
  <UsingTask TaskName="NETSdkWarning" AssemblyFile="$(MicrosoftNETBuildTasksAssembly)" xmlns="http://schemas.microsoft.com/developer/msbuild/2003" />
  <UsingTask TaskName="NETSdkInformation" AssemblyFile="$(MicrosoftNETBuildTasksAssembly)" xmlns="http://schemas.microsoft.com/developer/msbuild/2003" />
  <UsingTask TaskName="ShowPreviewMessage" AssemblyFile="$(MicrosoftNETBuildTasksAssembly)" xmlns="http://schemas.microsoft.com/developer/msbuild/2003" />
  <!--
============================================================================================================================================
  </Import>

C:\Program Files\dotnet\sdk\8.0.110\Sdks\Microsoft.NET.Sdk\targets\Microsoft.NET.Sdk.CrossTargeting.targets
============================================================================================================================================
-->
  <!--
============================================================================================================================================
  <Import Project="$(MSBuildThisFileDirectory)Microsoft.NET.Sdk.SourceLink.targets" Condition="'$(SuppressImplicitGitSourceLink)' != 'true'">

C:\Program Files\dotnet\sdk\8.0.110\Sdks\Microsoft.NET.Sdk\targets\Microsoft.NET.Sdk.SourceLink.targets
============================================================================================================================================
-->
  <!--
***********************************************************************************************
Microsoft.NET.Sdk.SourceLink.targets

WARNING:  DO NOT MODIFY this file unless you are knowledgeable about MSBuild and have
          created a backup copy.  Incorrect changes to this file will make it
          impossible to load or build your projects from the command-line or the IDE.

Copyright (c) .NET Foundation. All rights reserved.
***********************************************************************************************
-->
  <!-- C++ projects currently do not import Microsoft.NET.Sdk.props. -->
  <!--<Import Project="$(MSBuildThisFileDirectory)Microsoft.NET.Sdk.SourceLink.props" Condition="'$(_SourceLinkPropsImported)' != 'true'" />-->
  <PropertyGroup xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <!-- Workaround for https://github.com/Microsoft/msbuild/issues/3294. -->
    <_SourceLinkSdkSubDir>build</_SourceLinkSdkSubDir>
    <_SourceLinkSdkSubDir Condition="'$(IsCrossTargetingBuild)' == 'true'">buildMultiTargeting</_SourceLinkSdkSubDir>
    <!-- Workaround for https://github.com/dotnet/sdk/issues/36585 (Desktop XAML targets do not produce correct #line directives) -->
    <EmbedUntrackedSources Condition="'$(EmbedUntrackedSources)' == '' and '$(ImportFrameworkWinFXTargets)' != 'true'">true</EmbedUntrackedSources>
  </PropertyGroup>
  <!--
============================================================================================================================================
  <Import Project="$(MSBuildThisFileDirectory)..\..\Microsoft.Build.Tasks.Git\build\Microsoft.Build.Tasks.Git.targets">

C:\Program Files\dotnet\sdk\8.0.110\Sdks\Microsoft.Build.Tasks.Git\build\Microsoft.Build.Tasks.Git.targets
============================================================================================================================================
-->
  <!-- Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the License.txt file in the project root for more information. -->
  <UsingTask TaskName="Microsoft.Build.Tasks.Git.LocateRepository" AssemblyFile="$(MicrosoftBuildTasksGitAssemblyFile)" />
  <UsingTask TaskName="Microsoft.Build.Tasks.Git.GetUntrackedFiles" AssemblyFile="$(MicrosoftBuildTasksGitAssemblyFile)" />
  <PropertyGroup>
    <!--
      Sets the scope of git repository configuration. By default (no scope specified) configuration is read from environment variables
      and system and global user git/ssh configuration files. 
      
      If "local" is specified the configuration is only read from the configuration files local to the repository (or work tree).
      In addition, any use of home relative paths in these configuration files (paths that start with '~/') is disallowed.
      
      By default, the scope is restricted to "local" when building in CI (ContinuousIntegrationBuild is true) to avoid introducing 
      dependencies on CI machine state into the build.
    -->
    <GitRepositoryConfigurationScope Condition="'$(GitRepositoryConfigurationScope)' == '' and '$(ContinuousIntegrationBuild)' == 'true'">local</GitRepositoryConfigurationScope>
  </PropertyGroup>
  <Target Name="InitializeSourceControlInformationFromSourceControlManager">
    <!--
      Reports a warning if the given project doesn't belong to a repository under source control,
      unless the targets were implicily imported from an SDK without a package reference.
    -->
    <Microsoft.Build.Tasks.Git.LocateRepository Path="$(MSBuildProjectDirectory)" RemoteName="$(GitRepositoryRemoteName)" ConfigurationScope="$(GitRepositoryConfigurationScope)" NoWarnOnMissingInfo="$(PkgMicrosoft_Build_Tasks_Git.Equals(''))">
      <Output TaskParameter="RepositoryId" PropertyName="_GitRepositoryId" />
      <Output TaskParameter="Url" PropertyName="ScmRepositoryUrl" />
      <Output TaskParameter="Roots" ItemName="SourceRoot" />
      <Output TaskParameter="RevisionId" PropertyName="SourceRevisionId" Condition="'$(SourceRevisionId)' == ''" />
    </Microsoft.Build.Tasks.Git.LocateRepository>
    <PropertyGroup>
      <RepositoryType Condition="'$(RepositoryType)' == ''">git</RepositoryType>
    </PropertyGroup>
  </Target>
  <!--
    Embed files to the PDB that either do not belong to any of the SourceRoot directories
    or belong to the source repository, but are explicitly ignored (excluded) from source control.
  -->
  <Target Name="SetEmbeddedFilesFromSourceControlManagerUntrackedFiles" DependsOnTargets="InitializeSourceControlInformationFromSourceControlManager">
    <Microsoft.Build.Tasks.Git.GetUntrackedFiles RepositoryId="$(_GitRepositoryId)" ConfigurationScope="$(GitRepositoryConfigurationScope)" ProjectDirectory="$(MSBuildProjectDirectory)" Files="@(Compile)" Condition="'$(_GitRepositoryId)' != ''">
      <Output TaskParameter="UntrackedFiles" ItemName="EmbeddedFiles" />
    </Microsoft.Build.Tasks.Git.GetUntrackedFiles>
  </Target>
  <!--
============================================================================================================================================
  </Import>

C:\Program Files\dotnet\sdk\8.0.110\Sdks\Microsoft.NET.Sdk\targets\Microsoft.NET.Sdk.SourceLink.targets
============================================================================================================================================
-->
  <!--
============================================================================================================================================
  <Import Project="$(MSBuildThisFileDirectory)..\..\Microsoft.SourceLink.Common\$(_SourceLinkSdkSubDir)\Microsoft.SourceLink.Common.targets">

C:\Program Files\dotnet\sdk\8.0.110\Sdks\Microsoft.SourceLink.Common\buildMultiTargeting\Microsoft.SourceLink.Common.targets
============================================================================================================================================
-->
  <!-- Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the License.txt file in the project root for more information. -->
  <!--
============================================================================================================================================
  <Import Project="..\build\InitializeSourceControlInformation.targets">

C:\Program Files\dotnet\sdk\8.0.110\Sdks\Microsoft.SourceLink.Common\build\InitializeSourceControlInformation.targets
============================================================================================================================================
-->
  <!-- Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the License.txt file in the project root for more information. -->
  <UsingTask TaskName="Microsoft.SourceLink.Common.SourceLinkHasSingleProvider" AssemblyFile="$(_MicrosoftSourceLinkCommonAssemblyFile)" />
  <Target Name="_SourceLinkHasSingleProvider">
    <!--
      If there is a single SourceLink provider we can use Repository URL to infer repository host.
      If the project references multiple SourceLink providers the user needs to specify hosts explicitly (ImplicitHost will be empty)
      as we do not know which providers should be used to produce SourceLink URL for the repository.
      
      Reports an error if there is no SourceLinkUrlInitializerTargets is empty (no SourceLink provider is referenced).
    -->
    <Microsoft.SourceLink.Common.SourceLinkHasSingleProvider ProviderTargets="$(SourceLinkUrlInitializerTargets)">
      <Output TaskParameter="HasSingleProvider" PropertyName="SourceLinkHasSingleProvider" />
    </Microsoft.SourceLink.Common.SourceLinkHasSingleProvider>
  </Target>
  <!--
    Triggers InitializeSourceControlInformationFromSourceControlManager target defined by a source control package Microsoft.Build.Tasks.{Git|Tfvc|...}.
    
    Notes: No error is reported if InitializeSourceControlInformation is not defined.
  -->
  <Target Name="_InitializeSourceControlInformationFromSourceControlManager" DependsOnTargets="InitializeSourceControlInformationFromSourceControlManager;_SourceLinkHasSingleProvider;$(SourceControlManagerUrlTranslationTargets);SourceControlManagerPublishTranslatedUrls" BeforeTargets="InitializeSourceControlInformation" Condition="'$(EnableSourceControlManagerQueries)' == 'true'" />
  <Target Name="SourceControlManagerPublishTranslatedUrls">
    <PropertyGroup>
      <!--
        If the project already sets RepositoryUrl use it. Such URL is considered final and translations are not applied.
      -->
      <PrivateRepositoryUrl Condition="'$(PrivateRepositoryUrl)' == ''">$(RepositoryUrl)</PrivateRepositoryUrl>
      <PrivateRepositoryUrl Condition="'$(PrivateRepositoryUrl)' == ''">$(ScmRepositoryUrl)</PrivateRepositoryUrl>
    </PropertyGroup>
    <ItemGroup>
      <SourceRoot Update="@(SourceRoot)">
        <RepositoryUrl Condition="'%(SourceRoot.RepositoryUrl)' == ''">%(SourceRoot.ScmRepositoryUrl)</RepositoryUrl>
      </SourceRoot>
    </ItemGroup>
  </Target>
  <!--
============================================================================================================================================
  </Import>

C:\Program Files\dotnet\sdk\8.0.110\Sdks\Microsoft.SourceLink.Common\buildMultiTargeting\Microsoft.SourceLink.Common.targets
============================================================================================================================================
-->
  <!--
    Workaround for https://github.com/Microsoft/msbuild/issues/3294.
    Microsoft.Common.CrossTargeting.targets is missing InitializeSourceControlInformation definition.
  -->
  <Target Name="InitializeSourceControlInformation" />
  <PropertyGroup>
    <SourceControlInformationFeatureSupported>true</SourceControlInformationFeatureSupported>
  </PropertyGroup>
  <!--
============================================================================================================================================
  </Import>

C:\Program Files\dotnet\sdk\8.0.110\Sdks\Microsoft.NET.Sdk\targets\Microsoft.NET.Sdk.SourceLink.targets
============================================================================================================================================
-->
  <!--
============================================================================================================================================
  <Import Project="$(MSBuildThisFileDirectory)..\..\Microsoft.SourceLink.GitHub\build\Microsoft.SourceLink.GitHub.targets">

C:\Program Files\dotnet\sdk\8.0.110\Sdks\Microsoft.SourceLink.GitHub\build\Microsoft.SourceLink.GitHub.targets
============================================================================================================================================
-->
  <!-- Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the License.txt file in the project root for more information. -->
  <PropertyGroup>
    <_SourceLinkGitHubAssemblyFile Condition="'$(MSBuildRuntimeType)' != 'Core'">$(MSBuildThisFileDirectory)..\tools\net472\Microsoft.SourceLink.GitHub.dll</_SourceLinkGitHubAssemblyFile>
    <_SourceLinkGitHubAssemblyFile Condition="'$(MSBuildRuntimeType)' == 'Core'">$(MSBuildThisFileDirectory)..\tools\core\Microsoft.SourceLink.GitHub.dll</_SourceLinkGitHubAssemblyFile>
  </PropertyGroup>
  <UsingTask TaskName="Microsoft.SourceLink.GitHub.GetSourceLinkUrl" AssemblyFile="$(_SourceLinkGitHubAssemblyFile)" />
  <UsingTask TaskName="Microsoft.SourceLink.GitHub.TranslateRepositoryUrls" AssemblyFile="$(_SourceLinkGitHubAssemblyFile)" />
  <PropertyGroup>
    <SourceLinkUrlInitializerTargets>$(SourceLinkUrlInitializerTargets);_InitializeGitHubSourceLinkUrl</SourceLinkUrlInitializerTargets>
    <SourceControlManagerUrlTranslationTargets>$(SourceControlManagerUrlTranslationTargets);TranslateGitHubUrlsInSourceControlInformation</SourceControlManagerUrlTranslationTargets>
  </PropertyGroup>
  <Target Name="_InitializeGitHubSourceLinkUrl" Outputs="%(SourceRoot.Identity)">
    <!--
      The task calculates SourceLink URL for a given SourceRoot.

      If the SourceRoot is associated with a git repository with a recognized domain the <see cref="SourceLinkUrl"/>
      output property is set to the content URL corresponding to the domain, otherwise it is set to string "N/A".

      Recognized domains are specified via Hosts (initialized from SourceLinkGitHubHost item group).
      In addition, if SourceLinkHasSingleProvider is true an implicit host is parsed from RepositoryUrl.

      Example of SourceLinkGitHubHost items:

      <ItemGroup>
        <SourceLinkGitHubHost Include="github.com" ContentUrl="https://raw.githubusercontent.com"/>
        <SourceLinkGitHubHost Include="mygithub1.com" />           ContentUrl defaults to https://mygithub1.com/raw
        <SourceLinkGitHubHost Include="mygithub2.com:8080" />      ContentUrl defaults to https://mygithub2.com:8080/raw
      </ItemGroup>

      ContentUrl is optional. If not specified it defaults to "https://{domain}/raw".
    -->
    <Microsoft.SourceLink.GitHub.GetSourceLinkUrl RepositoryUrl="$(PrivateRepositoryUrl)" SourceRoot="@(SourceRoot)" Hosts="@(SourceLinkGitHubHost)" IsSingleProvider="$(SourceLinkHasSingleProvider)">
      <Output TaskParameter="SourceLinkUrl" PropertyName="_SourceLinkUrlToUpdate" />
    </Microsoft.SourceLink.GitHub.GetSourceLinkUrl>
    <ItemGroup>
      <!-- Only update the SourceLinkUrl metadata if the SourceRoot belongs to this source control -->
      <SourceRoot Update="%(Identity)" SourceLinkUrl="$(_SourceLinkUrlToUpdate)" Condition="'$(_SourceLinkUrlToUpdate)' != 'N/A'" />
    </ItemGroup>
  </Target>
  <!-- 
    We need to translate ssh URLs to https.
  -->
  <Target Name="TranslateGitHubUrlsInSourceControlInformation">
    <ItemGroup>
      <_TranslatedSourceRoot Remove="@(_TranslatedSourceRoot)" />
    </ItemGroup>
    <Microsoft.SourceLink.GitHub.TranslateRepositoryUrls RepositoryUrl="$(ScmRepositoryUrl)" SourceRoots="@(SourceRoot)" Hosts="@(SourceLinkGitHubHost)" IsSingleProvider="$(SourceLinkHasSingleProvider)">
      <Output TaskParameter="TranslatedRepositoryUrl" PropertyName="ScmRepositoryUrl" />
      <Output TaskParameter="TranslatedSourceRoots" ItemName="_TranslatedSourceRoot" />
    </Microsoft.SourceLink.GitHub.TranslateRepositoryUrls>
    <ItemGroup>
      <SourceRoot Remove="@(SourceRoot)" />
      <SourceRoot Include="@(_TranslatedSourceRoot)" />
    </ItemGroup>
  </Target>
  <!--
============================================================================================================================================
  </Import>

C:\Program Files\dotnet\sdk\8.0.110\Sdks\Microsoft.NET.Sdk\targets\Microsoft.NET.Sdk.SourceLink.targets
============================================================================================================================================
-->
  <!--
============================================================================================================================================
  <Import Project="$(MSBuildThisFileDirectory)..\..\Microsoft.SourceLink.GitLab\build\Microsoft.SourceLink.GitLab.targets">

C:\Program Files\dotnet\sdk\8.0.110\Sdks\Microsoft.SourceLink.GitLab\build\Microsoft.SourceLink.GitLab.targets
============================================================================================================================================
-->
  <!-- Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the License.txt file in the project root for more information. -->
  <PropertyGroup>
    <_SourceLinkGitLabAssemblyFile Condition="'$(MSBuildRuntimeType)' != 'Core'">$(MSBuildThisFileDirectory)..\tools\net472\Microsoft.SourceLink.GitLab.dll</_SourceLinkGitLabAssemblyFile>
    <_SourceLinkGitLabAssemblyFile Condition="'$(MSBuildRuntimeType)' == 'Core'">$(MSBuildThisFileDirectory)..\tools\core\Microsoft.SourceLink.GitLab.dll</_SourceLinkGitLabAssemblyFile>
  </PropertyGroup>
  <UsingTask TaskName="Microsoft.SourceLink.GitLab.GetSourceLinkUrl" AssemblyFile="$(_SourceLinkGitLabAssemblyFile)" />
  <UsingTask TaskName="Microsoft.SourceLink.GitLab.TranslateRepositoryUrls" AssemblyFile="$(_SourceLinkGitLabAssemblyFile)" />
  <PropertyGroup>
    <SourceLinkUrlInitializerTargets>$(SourceLinkUrlInitializerTargets);_InitializeGitLabSourceLinkUrl</SourceLinkUrlInitializerTargets>
    <SourceControlManagerUrlTranslationTargets>$(SourceControlManagerUrlTranslationTargets);TranslateGitLabUrlsInSourceControlInformation</SourceControlManagerUrlTranslationTargets>
  </PropertyGroup>
  <Target Name="_InitializeGitLabSourceLinkUrl" Outputs="%(SourceRoot.Identity)">
    <!--
      The task calculates SourceLink URL for a given SourceRoot.

      If the SourceRoot is associated with a git repository with a recognized domain the <see cref="SourceLinkUrl"/>
      output property is set to the content URL corresponding to the domain, otherwise it is set to string "N/A".

      Recognized domains are specified via Hosts (initialized from SourceLinkGitLabHost item group).
      In addition, if SourceLinkHasSingleProvider is true an implicit host is parsed from RepositoryUrl.

      Example of SourceLinkGitLabHost items:

      <ItemGroup>
        <SourceLinkGitLabHost Include="mygitlab1.com" ContentUrl="http://mygitlab1.com"/>
        <SourceLinkGitLabHost Include="mygitlab2.com" />           ContentUrl defaults to https://mygitlab2.com
        <SourceLinkGitLabHost Include="mygitlab3.com:8080" />      ContentUrl defaults to https://mygitlab3.com:8080
      </ItemGroup>

      ContentUrl is optional. If not specified it defaults to "https://{domain}" or "http://{domain}", based on the scheme of SourceRoot.RepositoryUrl.
    -->
    <Microsoft.SourceLink.GitLab.GetSourceLinkUrl RepositoryUrl="$(PrivateRepositoryUrl)" SourceRoot="@(SourceRoot)" Hosts="@(SourceLinkGitLabHost)" IsSingleProvider="$(SourceLinkHasSingleProvider)">
      <Output TaskParameter="SourceLinkUrl" PropertyName="_SourceLinkUrlToUpdate" />
    </Microsoft.SourceLink.GitLab.GetSourceLinkUrl>
    <ItemGroup>
      <!-- Only update the SourceLinkUrl metadata if the SourceRoot belongs to this source control -->
      <SourceRoot Update="%(Identity)" SourceLinkUrl="$(_SourceLinkUrlToUpdate)" Condition="'$(_SourceLinkUrlToUpdate)' != 'N/A'" />
    </ItemGroup>
  </Target>
  <!-- 
    We need to translate ssh URLs to https.
  -->
  <Target Name="TranslateGitLabUrlsInSourceControlInformation">
    <ItemGroup>
      <_TranslatedSourceRoot Remove="@(_TranslatedSourceRoot)" />
    </ItemGroup>
    <Microsoft.SourceLink.GitLab.TranslateRepositoryUrls RepositoryUrl="$(ScmRepositoryUrl)" SourceRoots="@(SourceRoot)" Hosts="@(SourceLinkGitLabHost)" IsSingleProvider="$(SourceLinkHasSingleProvider)">
      <Output TaskParameter="TranslatedRepositoryUrl" PropertyName="ScmRepositoryUrl" />
      <Output TaskParameter="TranslatedSourceRoots" ItemName="_TranslatedSourceRoot" />
    </Microsoft.SourceLink.GitLab.TranslateRepositoryUrls>
    <ItemGroup>
      <SourceRoot Remove="@(SourceRoot)" />
      <SourceRoot Include="@(_TranslatedSourceRoot)" />
    </ItemGroup>
  </Target>
  <!--
============================================================================================================================================
  </Import>

C:\Program Files\dotnet\sdk\8.0.110\Sdks\Microsoft.NET.Sdk\targets\Microsoft.NET.Sdk.SourceLink.targets
============================================================================================================================================
-->
  <!--
============================================================================================================================================
  <Import Project="$(MSBuildThisFileDirectory)..\..\Microsoft.SourceLink.AzureRepos.Git\build\Microsoft.SourceLink.AzureRepos.Git.targets">

C:\Program Files\dotnet\sdk\8.0.110\Sdks\Microsoft.SourceLink.AzureRepos.Git\build\Microsoft.SourceLink.AzureRepos.Git.targets
============================================================================================================================================
-->
  <!-- Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the License.txt file in the project root for more information. -->
  <PropertyGroup>
    <_SourceLinkAzureReposGitAssemblyFile Condition="'$(MSBuildRuntimeType)' != 'Core'">$(MSBuildThisFileDirectory)..\tools\net472\Microsoft.SourceLink.AzureRepos.Git.dll</_SourceLinkAzureReposGitAssemblyFile>
    <_SourceLinkAzureReposGitAssemblyFile Condition="'$(MSBuildRuntimeType)' == 'Core'">$(MSBuildThisFileDirectory)..\tools\core\Microsoft.SourceLink.AzureRepos.Git.dll</_SourceLinkAzureReposGitAssemblyFile>
  </PropertyGroup>
  <UsingTask TaskName="Microsoft.SourceLink.AzureRepos.Git.GetSourceLinkUrl" AssemblyFile="$(_SourceLinkAzureReposGitAssemblyFile)" />
  <UsingTask TaskName="Microsoft.SourceLink.AzureRepos.Git.TranslateRepositoryUrls" AssemblyFile="$(_SourceLinkAzureReposGitAssemblyFile)" />
  <PropertyGroup>
    <SourceLinkUrlInitializerTargets>$(SourceLinkUrlInitializerTargets);_InitializeAzureReposGitSourceLinkUrl</SourceLinkUrlInitializerTargets>
    <SourceControlManagerUrlTranslationTargets>$(SourceControlManagerUrlTranslationTargets);TranslateAzureReposGitUrlsInSourceControlInformation</SourceControlManagerUrlTranslationTargets>
  </PropertyGroup>
  <Target Name="_InitializeAzureReposGitSourceLinkUrl" Outputs="%(SourceRoot.Identity)">
    <!--
      The task calculates SourceLink URL for a given SourceRoot.

      If the SourceRoot is associated with a git repository with a recognized domain the <see cref="SourceLinkUrl"/>
      output property is set to the content URL corresponding to the domain, otherwise it is set to string "N/A".

      Recognized domains are specified via Hosts (initialized from SourceLinkAzureReposGitHost item group).
      In addition, if SourceLinkHasSingleProvider is true an implicit host is parsed from RepositoryUrl.

      ContentUrl is optional. If not specified it defaults to "https://{domain}" or "http://{domain}", based on the scheme of SourceRoot.RepositoryUrl.
    -->
    <Microsoft.SourceLink.AzureRepos.Git.GetSourceLinkUrl RepositoryUrl="$(PrivateRepositoryUrl)" SourceRoot="@(SourceRoot)" Hosts="@(SourceLinkAzureReposGitHost)" IsSingleProvider="$(SourceLinkHasSingleProvider)">
      <Output TaskParameter="SourceLinkUrl" PropertyName="_SourceLinkUrlToUpdate" />
    </Microsoft.SourceLink.AzureRepos.Git.GetSourceLinkUrl>
    <ItemGroup>
      <!-- Only update the SourceLinkUrl metadata if the SourceRoot belongs to this source control -->
      <SourceRoot Update="%(Identity)" SourceLinkUrl="$(_SourceLinkUrlToUpdate)" Condition="'$(_SourceLinkUrlToUpdate)' != 'N/A'" />
    </ItemGroup>
  </Target>
  <!-- 
    We need to translate ssh URLs to https.
  -->
  <Target Name="TranslateAzureReposGitUrlsInSourceControlInformation">
    <ItemGroup>
      <_TranslatedSourceRoot Remove="@(_TranslatedSourceRoot)" />
    </ItemGroup>
    <Microsoft.SourceLink.AzureRepos.Git.TranslateRepositoryUrls RepositoryUrl="$(ScmRepositoryUrl)" SourceRoots="@(SourceRoot)" Hosts="@(SourceLinkAzureReposGitHost)" IsSingleProvider="$(SourceLinkHasSingleProvider)">
      <Output TaskParameter="TranslatedRepositoryUrl" PropertyName="ScmRepositoryUrl" />
      <Output TaskParameter="TranslatedSourceRoots" ItemName="_TranslatedSourceRoot" />
    </Microsoft.SourceLink.AzureRepos.Git.TranslateRepositoryUrls>
    <ItemGroup>
      <SourceRoot Remove="@(SourceRoot)" />
      <SourceRoot Include="@(_TranslatedSourceRoot)" />
    </ItemGroup>
  </Target>
  <!--
============================================================================================================================================
  </Import>

C:\Program Files\dotnet\sdk\8.0.110\Sdks\Microsoft.NET.Sdk\targets\Microsoft.NET.Sdk.SourceLink.targets
============================================================================================================================================
-->
  <!--
============================================================================================================================================
  <Import Project="$(MSBuildThisFileDirectory)..\..\Microsoft.SourceLink.Bitbucket.Git\build\Microsoft.SourceLink.Bitbucket.Git.targets">

C:\Program Files\dotnet\sdk\8.0.110\Sdks\Microsoft.SourceLink.Bitbucket.Git\build\Microsoft.SourceLink.Bitbucket.Git.targets
============================================================================================================================================
-->
  <!-- Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the License.txt file in the project root for more information. -->
  <PropertyGroup>
    <_SourceLinkBitbucketAssemblyFile Condition="'$(MSBuildRuntimeType)' != 'Core'">$(MSBuildThisFileDirectory)..\tools\net472\Microsoft.SourceLink.Bitbucket.Git.dll</_SourceLinkBitbucketAssemblyFile>
    <_SourceLinkBitbucketAssemblyFile Condition="'$(MSBuildRuntimeType)' == 'Core'">$(MSBuildThisFileDirectory)..\tools\core\Microsoft.SourceLink.Bitbucket.Git.dll</_SourceLinkBitbucketAssemblyFile>
  </PropertyGroup>
  <UsingTask TaskName="Microsoft.SourceLink.Bitbucket.Git.GetSourceLinkUrl" AssemblyFile="$(_SourceLinkBitbucketAssemblyFile)" />
  <UsingTask TaskName="Microsoft.SourceLink.Bitbucket.Git.TranslateRepositoryUrls" AssemblyFile="$(_SourceLinkBitbucketAssemblyFile)" />
  <PropertyGroup>
    <SourceLinkUrlInitializerTargets>$(SourceLinkUrlInitializerTargets);_InitializeBitbucketGitSourceLinkUrl</SourceLinkUrlInitializerTargets>
    <SourceControlManagerUrlTranslationTargets>$(SourceControlManagerUrlTranslationTargets);TranslateBitbucketGitUrlsInSourceControlInformation</SourceControlManagerUrlTranslationTargets>
  </PropertyGroup>
  <Target Name="_InitializeBitbucketGitSourceLinkUrl" Outputs="%(SourceRoot.Identity)">
    <!--
      The task calculates SourceLink URL for a given SourceRoot.

      If the SourceRoot is associated with a git repository with a recognized domain the <see cref="SourceLinkUrl"/>
      output property is set to the content URL corresponding to the domain, otherwise it is set to string "N/A".

      Recognized domains are specified via Hosts (initialized from SourceLinkBitbucketGitHost item group).
      In addition, if SourceLinkHasSingleProvider is true an implicit host is parsed from RepositoryUrl.

      Example of SourceLinkBitbucketGitHost items:

      <ItemGroup>
        <SourceLinkBitbucketGitHost Include="bitbucket1.com" ContentUrl="http://bitbucket1.com"/>
        <SourceLinkBitbucketGitHost Include="bitbucket2.com" />           ContentUrl defaults to https://bitbucket2.com
        <SourceLinkBitbucketGitHost Include="bitbucket3.com:8080" />      ContentUrl defaults to https://bitbucket3.com:8080
      </ItemGroup>

      ContentUrl is optional. If not specified it defaults to "https://{domain}" or "http://{domain}", based on the scheme of SourceRoot.RepositoryUrl.
    -->
    <Microsoft.SourceLink.Bitbucket.Git.GetSourceLinkUrl RepositoryUrl="$(PrivateRepositoryUrl)" SourceRoot="@(SourceRoot)" Hosts="@(SourceLinkBitbucketGitHost)" IsSingleProvider="$(SourceLinkHasSingleProvider)">
      <Output TaskParameter="SourceLinkUrl" PropertyName="_SourceLinkUrlToUpdate" />
    </Microsoft.SourceLink.Bitbucket.Git.GetSourceLinkUrl>
    <ItemGroup>
      <!-- Only update the SourceLinkUrl metadata if the SourceRoot belongs to this source control -->
      <SourceRoot Update="%(Identity)" SourceLinkUrl="$(_SourceLinkUrlToUpdate)" Condition="'$(_SourceLinkUrlToUpdate)' != 'N/A'" />
    </ItemGroup>
  </Target>
  <!-- 
    We need to translate ssh URLs to https.
  -->
  <Target Name="TranslateBitbucketGitUrlsInSourceControlInformation">
    <ItemGroup>
      <_TranslatedSourceRoot Remove="@(_TranslatedSourceRoot)" />
    </ItemGroup>
    <Microsoft.SourceLink.Bitbucket.Git.TranslateRepositoryUrls RepositoryUrl="$(ScmRepositoryUrl)" SourceRoots="@(SourceRoot)" Hosts="@(SourceLinkBitbucketGitHost)" IsSingleProvider="$(SourceLinkHasSingleProvider)">
      <Output TaskParameter="TranslatedRepositoryUrl" PropertyName="ScmRepositoryUrl" />
      <Output TaskParameter="TranslatedSourceRoots" ItemName="_TranslatedSourceRoot" />
    </Microsoft.SourceLink.Bitbucket.Git.TranslateRepositoryUrls>
    <ItemGroup>
      <SourceRoot Remove="@(SourceRoot)" />
      <SourceRoot Include="@(_TranslatedSourceRoot)" />
    </ItemGroup>
  </Target>
  <!--
============================================================================================================================================
  </Import>

C:\Program Files\dotnet\sdk\8.0.110\Sdks\Microsoft.NET.Sdk\targets\Microsoft.NET.Sdk.SourceLink.targets
============================================================================================================================================
-->
  <!--
============================================================================================================================================
  </Import>

C:\Program Files\dotnet\sdk\8.0.110\Sdks\Microsoft.NET.Sdk\targets\Microsoft.NET.Sdk.CrossTargeting.targets
============================================================================================================================================
-->
  <!--
  ============================================================
                              Publish

   This is the Publish target for cross-targeting.
   Currently it is unsupported to publish for multiple target frameworks
   because users can specify the $(PublishDir), and publish would put
   multiple published applications in a single directory.
  ============================================================
   -->
  <Target Name="Publish" Condition=" '$(IsPublishable)' != 'false' " xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <ItemGroup>
      <_PublishTargetFrameworks Include="$(TargetFrameworks)" />
    </ItemGroup>
    <NETSdkError ResourceName="PublishUnsupportedWithoutTargetFramework" FormatArguments="@(_PublishTargetFrameworks, ', ')" />
  </Target>
  <!--
  ============================================================
                                      GetAllRuntimeIdentifiers

  Outer build implementation of GetAllRuntimeIdentifiers returns
  a union of all runtime identifiers used across inner and outer
  build evaluations.

  It is further set to run before '_GenerateRestoreProjectSpec'
  (note that running only 'Restore' is too late and will not work
  with solution level restore). This ensures that any conditioning
  of runtime  identifiers against TargetFramework does not prevent
  restore from providing  the necessary RID-specific assets for all
  inner builds.

  It also brings parity to VS vs. command line behavior in this
  scenario because VS passes all of the information from each
  configured inner build to restore, whereas command-line restore
  without this target would only use the runtime identifiers that
  are statically set in the outer evaluation.
  ============================================================
  -->
  <Target Name="GetAllRuntimeIdentifiers" Returns="$(RuntimeIdentifiers)" BeforeTargets="_GenerateRestoreProjectSpec" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <ItemGroup>
      <_GetAllRuntimeIdentifiersTargetFrameworks Include="$(TargetFrameworks)" />
      <_AllRuntimeIdentifiers Include="$(RuntimeIdentifiers);$(RuntimeIdentifier)" />
    </ItemGroup>
    <MSBuild Projects="$(MSBuildProjectFile)" Targets="GetAllRuntimeIdentifiers" Properties="TargetFramework=%(_GetAllRuntimeIdentifiersTargetFrameworks.Identity)">
      <Output ItemName="_AllRuntimeIdentifiers" TaskParameter="TargetOutputs" />
    </MSBuild>
    <PropertyGroup>
      <RuntimeIdentifiers>@(_AllRuntimeIdentifiers-&gt;Distinct())</RuntimeIdentifiers>
    </PropertyGroup>
  </Target>
  <!--
  ============================================================
                       GetPackagingOutputs

  Stub cross-targeting implementation of GetPackagingOutputs
  to allow project references from from projects that pull in
  Microsoft.AppxPackage.targets (UWP, PCL) to cross-targeted
  projects.

  Ultimately, the appx targets should be modified to use the
  same P2P TFM negotiation protocol as Microsoft.Common.targets
  so that they can forward to the TFM-specific GetPackagingOutputs
  of the appropriate inner build. This stub would not have any
  bad interaction with that change, which would happily bypass
  this implementation altogether.

  An empty GetPackagingOutputs is sufficient for the common
  case of a library with no special assets to contribute to
  the appx and is also equivalent to what is present in the
  single-targeted case unless WindowsAppContainer is not set
  to true.

  Furthermore, the appx targets currently use continue-on-error
  such that even without this, clean builds succeed but log an
  error and incremental builds silently succeed. As such, this
  simply removes a confounding error from successful clean
  builds.

  ============================================================
  -->
  <Target Name="GetPackagingOutputs" xmlns="http://schemas.microsoft.com/developer/msbuild/2003" />
  <!-- This exists as a workaround for https://github.com/Microsoft/msbuild/issues/3558 -->
  <PropertyGroup Condition="'$(DefaultProjectTypeGuid)' == ''" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <DefaultProjectTypeGuid Condition="'$(MSBuildProjectExtension)' == '.csproj'">{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}</DefaultProjectTypeGuid>
    <DefaultProjectTypeGuid Condition="'$(MSBuildProjectExtension)' == '.vbproj'">{F184B08F-C81C-45F6-A57F-5ABD9991F28F}</DefaultProjectTypeGuid>
    <!-- Note: F# sets DefaultProjectTypeGuid in the F# SDK -->
  </PropertyGroup>
  <!-- Default to the portable RID graph in the outer build as APICompat relies on it. -->
  <PropertyGroup Condition="'$(RuntimeIdentifierGraphPath)' == ''" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <!-- The portable RID graph should be in the same directory as the full RID graph -->
    <RuntimeIdentifierGraphPath>$([System.IO.Path]::GetDirectoryName($(BundledRuntimeIdentifierGraphFile)))/PortableRuntimeIdentifierGraph.json</RuntimeIdentifierGraphPath>
  </PropertyGroup>
  <!--
============================================================================================================================================
  </Import>

C:\Program Files\dotnet\sdk\8.0.110\Sdks\Microsoft.NET.Sdk\Sdk\Sdk.targets
============================================================================================================================================
-->
  <!--<Import Project="$(MSBuildThisFileDirectory)..\targets\Microsoft.NET.Sdk.targets" Condition="'$(IsCrossTargetingBuild)' != 'true'" />-->
  <!--
============================================================================================================================================
  <Import Project="$(MSBuildThisFileDirectory)..\targets\Microsoft.NET.ApiCompat.targets">

C:\Program Files\dotnet\sdk\8.0.110\Sdks\Microsoft.NET.Sdk\targets\Microsoft.NET.ApiCompat.targets
============================================================================================================================================
-->
  <!--
***********************************************************************************************
Microsoft.NET.ApiCompat.targets

WARNING:  DO NOT MODIFY this file unless you are knowledgeable about MSBuild and have
          created a backup copy.  Incorrect changes to this file will make it
          impossible to load or build your projects from the command-line or the IDE.

Copyright (c) .NET Foundation. All rights reserved.
***********************************************************************************************
-->
  <PropertyGroup Condition="'$(UseApiCompatPackage)' != 'true'">
    <DotNetApiCompatTaskAssembly Condition="'$(MSBuildRuntimeType)' != 'Core'">$(MSBuildThisFileDirectory)..\tools\net472\Microsoft.DotNet.ApiCompat.Task.dll</DotNetApiCompatTaskAssembly>
    <DotNetApiCompatTaskAssembly Condition="'$(MSBuildRuntimeType)' == 'Core'">$(MSBuildThisFileDirectory)..\tools\net8.0\Microsoft.DotNet.ApiCompat.Task.dll</DotNetApiCompatTaskAssembly>
  </PropertyGroup>
  <ItemGroup Condition="'$(EnablePackageValidation)' == 'true' and&#xD;&#xA;                        '$(DisablePackageBaselineValidation)' != 'true' and&#xD;&#xA;                        '$(PackageValidationBaselinePath)' == '' and&#xD;&#xA;                        '$(PackageValidationBaselineVersion)' != ''">
    <PackageDownload Include="$([MSBuild]::ValueOrDefault('$(PackageValidationBaselineName)', '$(PackageId)'))" Version="[$(PackageValidationBaselineVersion)]" />
  </ItemGroup>
  <!--<ImportGroup Condition="'$(UseApiCompatPackage)' != 'true'">-->
  <!--
============================================================================================================================================
  <Import Project="Microsoft.NET.ApiCompat.Common.targets">

C:\Program Files\dotnet\sdk\8.0.110\Sdks\Microsoft.NET.Sdk\targets\Microsoft.NET.ApiCompat.Common.targets
============================================================================================================================================
-->
  <!--
***********************************************************************************************
Microsoft.NET.ApiCompat.Common.targets

WARNING:  DO NOT MODIFY this file unless you are knowledgeable about MSBuild and have
          created a backup copy.  Incorrect changes to this file will make it
          impossible to load or build your projects from the command-line or the IDE.

Copyright (c) .NET Foundation. All rights reserved.
***********************************************************************************************
-->
  <!-- Expose the tasks for SDK consumption and for external use cases. -->
  <UsingTask TaskName="Microsoft.DotNet.ApiCompat.Task.ValidateAssembliesTask" AssemblyFile="$(DotNetApiCompatTaskAssembly)" />
  <UsingTask TaskName="Microsoft.DotNet.ApiCompat.Task.ValidatePackageTask" AssemblyFile="$(DotNetApiCompatTaskAssembly)" />
  <Target Name="CollectApiCompatInputs">
    <PropertyGroup Condition="'$(RoslynAssembliesPath)' == ''">
      <!-- If a custom roslyn assemblies path isn't provided, the opt-in switch 'ApiCompatUseRoslynToolsetPackagePath' is set to true and
           the roslyn toolset package is referenced, use the assemblies from that package. -->
      <_UseRoslynToolsetPackage Condition="'$(ApiCompatUseRoslynToolsetPackagePath)' == 'true' and '@(PackageReference-&gt;AnyHaveMetadataValue('Identity', 'Microsoft.Net.Compilers.Toolset'))' == 'true'">true</_UseRoslynToolsetPackage>
      <!-- CSharpCoreTargetsPath and VisualBasicCoreTargetsPath point to the same location, Microsoft.CodeAnalysis.CSharp and Microsoft.CodeAnalysis.VisualBasic
           are on the same directory as Microsoft.CodeAnalysis. So there is no need to distinguish between csproj or vbproj. -->
      <RoslynAssembliesPath Condition="'$(_UseRoslynToolsetPackage)' == 'true'">$([System.IO.Path]::GetDirectoryName('$(CSharpCoreTargetsPath)'))</RoslynAssembliesPath>
      <!-- Otherwise, default to the roslyn compiler provided by the SDK / Visual Studio. -->
      <RoslynAssembliesPath Condition="'$(_UseRoslynToolsetPackage)' != 'true'">$(RoslynTargetsPath)</RoslynAssembliesPath>
      <!-- The SDK stores the roslyn assemblies in the 'bincore' subdirectory. -->
      <RoslynAssembliesPath Condition="'$(MSBuildRuntimeType)' == 'Core'">$([System.IO.Path]::Combine('$(RoslynAssembliesPath)', 'bincore'))</RoslynAssembliesPath>
    </PropertyGroup>
    <!-- Respect legacy property and item names. -->
    <PropertyGroup>
      <ApiCompatGenerateSuppressionFile Condition="'$(ApiCompatGenerateSuppressionFile)' == ''">$(GenerateCompatibilitySuppressionFile)</ApiCompatGenerateSuppressionFile>
    </PropertyGroup>
    <ItemGroup>
      <ApiCompatSuppressionFile Include="$(CompatibilitySuppressionFilePath)" Condition="'@(ApiCompatSuppressionFile)' == '' and '$(CompatibilitySuppressionFilePath)' != ''" />
    </ItemGroup>
    <!-- END: Respect legacy property and item names. -->
    <PropertyGroup>
      <_apiCompatDefaultProjectSuppressionFile>$([MSBuild]::NormalizePath('$(MSBuildProjectDirectory)', 'CompatibilitySuppressions.xml'))</_apiCompatDefaultProjectSuppressionFile>
      <!-- Pass in a default suppression output file if non is supplied, and ApiCompatGenerateSuppressionFile is true. -->
      <ApiCompatSuppressionOutputFile Condition="'$(ApiCompatSuppressionOutputFile)' == '' and '$(ApiCompatGenerateSuppressionFile)' == 'true'">$(_apiCompatDefaultProjectSuppressionFile)</ApiCompatSuppressionOutputFile>
    </PropertyGroup>
    <!-- Pass in a default suppression file, if it exists. -->
    <ItemGroup Condition="'@(ApiCompatSuppressionFile)' == ''">
      <ApiCompatSuppressionFile Include="$(_apiCompatDefaultProjectSuppressionFile)" Condition="Exists($(_apiCompatDefaultProjectSuppressionFile))" />
    </ItemGroup>
  </Target>
  <!--
============================================================================================================================================
  </Import>

C:\Program Files\dotnet\sdk\8.0.110\Sdks\Microsoft.NET.Sdk\targets\Microsoft.NET.ApiCompat.targets
============================================================================================================================================
-->
  <!--
============================================================================================================================================
  <Import Project="Microsoft.NET.ApiCompat.ValidatePackage.targets">

C:\Program Files\dotnet\sdk\8.0.110\Sdks\Microsoft.NET.Sdk\targets\Microsoft.NET.ApiCompat.ValidatePackage.targets
============================================================================================================================================
-->
  <!--
***********************************************************************************************
Microsoft.NET.ApiCompat.ValidatePackage.targets

WARNING:  DO NOT MODIFY this file unless you are knowledgeable about MSBuild and have
          created a backup copy.  Incorrect changes to this file will make it
          impossible to load or build your projects from the command-line or the IDE.

Copyright (c) .NET Foundation. All rights reserved.
***********************************************************************************************
-->
  <PropertyGroup>
    <ApiCompatValidatePackageSemaphoreFile>$(IntermediateOutputPath)$(MSBuildThisFileName).semaphore</ApiCompatValidatePackageSemaphoreFile>
    <!-- Add any custom targets that need to run before package validation to the following property. -->
    <RunPackageValidationDependsOn>CollectApiCompatInputs;_GetReferencePathFromInnerProjects;$(RunPackageValidationDependsOn)</RunPackageValidationDependsOn>
  </PropertyGroup>
  <Target Name="RunPackageValidation" DependsOnTargets="$(RunPackageValidationDependsOn)" AfterTargets="Pack" Inputs="@(NuGetPackInput);&#xD;&#xA;                  @(ApiCompatSuppressionFile);&#xD;&#xA;                  $(ApiCompatSuppressionOutputFile)" Outputs="$(ApiCompatValidatePackageSemaphoreFile)" Condition="'$(EnablePackageValidation)' == 'true' and '$(IsPackable)' == 'true'">
    <PropertyGroup>
      <PackageValidationBaselineName Condition="'$(PackageValidationBaselineName)' == ''">$(PackageId)</PackageValidationBaselineName>
      <PackageValidationBaselinePath Condition="'$(PackageValidationBaselinePath)' == '' and '$(PackageValidationBaselineVersion)' != ''">$([MSBuild]::NormalizePath('$(NuGetPackageRoot)', '$(PackageValidationBaselineName.ToLower())', '$(PackageValidationBaselineVersion)', '$(PackageValidationBaselineName.ToLower()).$(PackageValidationBaselineVersion).nupkg'))</PackageValidationBaselinePath>
      <_packageValidationBaselinePath Condition="'$(DisablePackageBaselineValidation)' != 'true'">$(PackageValidationBaselinePath)</_packageValidationBaselinePath>
    </PropertyGroup>
    <ItemGroup>
      <_PackageTargetPath Include="@(NuGetPackOutput-&gt;WithMetadataValue('Extension', '.nupkg'))" Condition="!$([System.String]::new('%(Identity)').EndsWith('.symbols.nupkg'))" />
    </ItemGroup>
    <!-- PackageTargetPath isn't exposed by NuGet: https://github.com/NuGet/Home/issues/6671. -->
    <Microsoft.DotNet.ApiCompat.Task.ValidatePackageTask PackageTargetPath="@(_PackageTargetPath)" RuntimeGraph="$(RuntimeIdentifierGraphPath)" NoWarn="$(NoWarn)" RespectInternals="$(ApiCompatRespectInternals)" EnableRuleAttributesMustMatch="$(ApiCompatEnableRuleAttributesMustMatch)" ExcludeAttributesFiles="@(ApiCompatExcludeAttributesFile)" EnableRuleCannotChangeParameterName="$(ApiCompatEnableRuleCannotChangeParameterName)" RunApiCompat="$(RunApiCompat)" EnableStrictModeForCompatibleTfms="$(EnableStrictModeForCompatibleTfms)" EnableStrictModeForCompatibleFrameworksInPackage="$(EnableStrictModeForCompatibleFrameworksInPackage)" EnableStrictModeForBaselineValidation="$(EnableStrictModeForBaselineValidation)" GenerateSuppressionFile="$(ApiCompatGenerateSuppressionFile)" PreserveUnnecessarySuppressions="$(ApiCompatPreserveUnnecessarySuppressions)" PermitUnnecessarySuppressions="$(ApiCompatPermitUnnecessarySuppressions)" SuppressionFiles="@(ApiCompatSuppressionFile)" SuppressionOutputFile="$(ApiCompatSuppressionOutputFile)" BaselinePackageTargetPath="$(_packageValidationBaselinePath)" RoslynAssembliesPath="$(RoslynAssembliesPath)" PackageAssemblyReferences="@(PackageValidationReferencePath)" />
    <MakeDir Directories="$([System.IO.Path]::GetDirectoryName('$(ApiCompatValidatePackageSemaphoreFile)'))" />
    <Touch Files="$(ApiCompatValidatePackageSemaphoreFile)" AlwaysCreate="true" />
  </Target>
  <Target Name="GetReferencesForApiCompatValidatePackage" DependsOnTargets="FindReferenceAssembliesForReferences" Returns="@(ApiCompatAssemblyReferencesWithTargetFramework)">
    <ItemGroup>
      <ApiCompatAssemblyReferencesWithTargetFramework Include="$(TargetFramework)" TargetFrameworkMoniker="$(TargetFrameworkMoniker)" ReferencePath="@(ReferencePathWithRefAssemblies, ',')">
        <TargetPlatformMoniker Condition="'$(ApiCompatIgnoreTargetPlatformMoniker)' != 'true'">$(TargetPlatformMoniker)</TargetPlatformMoniker>
      </ApiCompatAssemblyReferencesWithTargetFramework>
    </ItemGroup>
  </Target>
  <!-- Depends on NuGet's _GetTargetFrameworksOutput target to calculate inner target frameworks. -->
  <Target Name="_GetReferencePathFromInnerProjects" DependsOnTargets="_GetTargetFrameworksOutput" Condition="'$(RunPackageValidationWithoutReferences)' != 'true'">
    <MSBuild Projects="$(MSBuildProjectFullPath)" Targets="GetReferencesForApiCompatValidatePackage" Properties="TargetFramework=%(_TargetFrameworks.Identity);&#xD;&#xA;                         BuildProjectReferences=false">
      <Output ItemName="PackageValidationReferencePath" TaskParameter="TargetOutputs" />
    </MSBuild>
  </Target>
  <!--
============================================================================================================================================
  </Import>

C:\Program Files\dotnet\sdk\8.0.110\Sdks\Microsoft.NET.Sdk\targets\Microsoft.NET.ApiCompat.targets
============================================================================================================================================
-->
  <!--</ImportGroup>-->
  <!--
============================================================================================================================================
  </Import>

C:\Program Files\dotnet\sdk\8.0.110\Sdks\Microsoft.NET.Sdk\Sdk\Sdk.targets
============================================================================================================================================
-->
  <!-- Import targets from NuGet.Build.Tasks.Pack package/Sdk -->
  <PropertyGroup Condition="'$(NuGetBuildTasksPackTargets)' == '' AND '$(ImportNuGetBuildTasksPackTargetsFromSdk)' != 'false'" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <NuGetBuildTasksPackTargets Condition="'$(IsCrossTargetingBuild)' == 'true'">$(MSBuildThisFileDirectory)..\..\NuGet.Build.Tasks.Pack\buildCrossTargeting\NuGet.Build.Tasks.Pack.targets</NuGetBuildTasksPackTargets>
    <NuGetBuildTasksPackTargets Condition="'$(IsCrossTargetingBuild)' != 'true'">$(MSBuildThisFileDirectory)..\..\NuGet.Build.Tasks.Pack\build\NuGet.Build.Tasks.Pack.targets</NuGetBuildTasksPackTargets>
    <ImportNuGetBuildTasksPackTargetsFromSdk>true</ImportNuGetBuildTasksPackTargetsFromSdk>
  </PropertyGroup>
  <!--
============================================================================================================================================
  <Import Project="$(NuGetBuildTasksPackTargets)" Condition="Exists('$(NuGetBuildTasksPackTargets)') AND '$(ImportNuGetBuildTasksPackTargetsFromSdk)' == 'true'">

C:\Program Files\dotnet\sdk\8.0.110\Sdks\NuGet.Build.Tasks.Pack\buildCrossTargeting\NuGet.Build.Tasks.Pack.targets
============================================================================================================================================
-->
  <!--
***********************************************************************************************
NuGet.Build.Tasks.Pack.targets

WARNING:  DO NOT MODIFY this file unless you are knowledgeable about MSBuild and have
          created a backup copy.  Incorrect changes to this file will make it
          impossible to load or build your projects from the command-line or the IDE.

Copyright (c) .NET Foundation. All rights reserved.
***********************************************************************************************
-->
  <!-- Load NuGet.Build.Tasks.Pack.dll, this can be overridden to use a different version with $(NuGetPackTaskAssemblyFile) -->
  <PropertyGroup Condition="$(NuGetPackTaskAssemblyFile) == ''" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <NuGetPackTaskAssemblyFile Condition="'$(MSBuildRuntimeType)' == 'Core'">..\CoreCLR\NuGet.Build.Tasks.Pack.dll</NuGetPackTaskAssemblyFile>
    <NuGetPackTaskAssemblyFile Condition="'$(MSBuildRuntimeType)' != 'Core'">..\Desktop\NuGet.Build.Tasks.Pack.dll</NuGetPackTaskAssemblyFile>
  </PropertyGroup>
  <!-- Tasks -->
  <UsingTask TaskName="NuGet.Build.Tasks.Pack.PackTask" AssemblyFile="$(NuGetPackTaskAssemblyFile)" xmlns="http://schemas.microsoft.com/developer/msbuild/2003" />
  <UsingTask TaskName="NuGet.Build.Tasks.Pack.GetPackOutputItemsTask" AssemblyFile="$(NuGetPackTaskAssemblyFile)" xmlns="http://schemas.microsoft.com/developer/msbuild/2003" />
  <UsingTask TaskName="NuGet.Build.Tasks.GetProjectTargetFrameworksTask" AssemblyFile="$(NuGetPackTaskAssemblyFile)" xmlns="http://schemas.microsoft.com/developer/msbuild/2003" />
  <UsingTask TaskName="NuGet.Build.Tasks.Pack.GetProjectReferencesFromAssetsFileTask" AssemblyFile="$(NuGetPackTaskAssemblyFile)" xmlns="http://schemas.microsoft.com/developer/msbuild/2003" />
  <UsingTask TaskName="NuGet.Build.Tasks.Pack.IsPackableFalseWarningTask" AssemblyFile="$(NuGetPackTaskAssemblyFile)" xmlns="http://schemas.microsoft.com/developer/msbuild/2003" />
  <PropertyGroup xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <PackageId Condition=" '$(PackageId)' == '' ">$(AssemblyName)</PackageId>
    <PackageVersion Condition=" '$(PackageVersion)' == '' ">$(Version)</PackageVersion>
    <IncludeContentInPack Condition="'$(IncludeContentInPack)'==''">true</IncludeContentInPack>
    <GenerateNuspecDependsOn>_LoadPackInputItems; _GetTargetFrameworksOutput; _WalkEachTargetPerFramework; _GetPackageFiles; $(GenerateNuspecDependsOn)</GenerateNuspecDependsOn>
    <PackageDescription Condition="'$(PackageDescription)'==''">$(Description)</PackageDescription>
    <PackageDescription Condition="'$(PackageDescription)'==''">Package Description</PackageDescription>
    <IsPackable Condition="'$(IsPackable)'=='' AND '$(IsTestProject)'=='true'">false</IsPackable>
    <IsPackable Condition="'$(IsPackable)'==''">true</IsPackable>
    <IncludeBuildOutput Condition="'$(IncludeBuildOutput)'==''">true</IncludeBuildOutput>
    <BuildOutputTargetFolder Condition="'$(BuildOutputTargetFolder)' == '' AND '$(IsTool)' == 'true'">tools</BuildOutputTargetFolder>
    <BuildOutputTargetFolder Condition="'$(BuildOutputTargetFolder)' == ''">lib</BuildOutputTargetFolder>
    <ContentTargetFolders Condition="'$(ContentTargetFolders)' == ''">content;contentFiles</ContentTargetFolders>
    <PackDependsOn>$(BeforePack); _IntermediatePack; GenerateNuspec; $(PackDependsOn)</PackDependsOn>
    <IsInnerBuild Condition="'$(TargetFramework)' != '' AND '$(TargetFrameworks)' != ''">true</IsInnerBuild>
    <SymbolPackageFormat Condition="'$(SymbolPackageFormat)' == ''">symbols.nupkg</SymbolPackageFormat>
    <AddPriFileDependsOn Condition="'$(MicrosoftPortableCurrentVersionPropsHasBeenImported)' == 'true'">DeterminePortableBuildCapabilities</AddPriFileDependsOn>
    <WarnOnPackingNonPackableProject Condition="'$(WarnOnPackingNonPackableProject)' == ''">false</WarnOnPackingNonPackableProject>
    <ImportNuGetBuildTasksPackTargetsFromSdk Condition="'$(ImportNuGetBuildTasksPackTargetsFromSdk)' == ''">false</ImportNuGetBuildTasksPackTargetsFromSdk>
    <DefaultAllowedOutputExtensionsInPackageBuildOutputFolder Condition="'$(DefaultAllowedOutputExtensionsInPackageBuildOutputFolder)' == ''">.dll; .exe; .winmd; .json; .pri; .xml</DefaultAllowedOutputExtensionsInPackageBuildOutputFolder>
    <AllowedOutputExtensionsInPackageBuildOutputFolder>$(DefaultAllowedOutputExtensionsInPackageBuildOutputFolder) ;$(AllowedOutputExtensionsInPackageBuildOutputFolder)</AllowedOutputExtensionsInPackageBuildOutputFolder>
    <AllowedOutputExtensionsInSymbolsPackageBuildOutputFolder Condition="'$(SymbolPackageFormat)' != 'snupkg'">.pdb; .mdb; $(AllowedOutputExtensionsInPackageBuildOutputFolder); $(AllowedOutputExtensionsInSymbolsPackageBuildOutputFolder)</AllowedOutputExtensionsInSymbolsPackageBuildOutputFolder>
    <AllowedOutputExtensionsInSymbolsPackageBuildOutputFolder Condition="'$(SymbolPackageFormat)' == 'snupkg'">.pdb</AllowedOutputExtensionsInSymbolsPackageBuildOutputFolder>
    <SuppressDependenciesWhenPacking Condition="'$(SuppressDependenciesWhenPacking)' == ''">false</SuppressDependenciesWhenPacking>
  </PropertyGroup>
  <PropertyGroup Condition="'$(NoBuild)' == 'true' or '$(GeneratePackageOnBuild)' == 'true'" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <GenerateNuspecDependsOn>$(GenerateNuspecDependsOn)</GenerateNuspecDependsOn>
  </PropertyGroup>
  <PropertyGroup Condition="'$(NoBuild)' != 'true' and '$(GeneratePackageOnBuild)' != 'true'" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <GenerateNuspecDependsOn>Build;$(GenerateNuspecDependsOn)</GenerateNuspecDependsOn>
  </PropertyGroup>
  <ItemGroup xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <ProjectCapability Include="Pack" />
  </ItemGroup>
  <ItemDefinitionGroup xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <BuildOutputInPackage>
      <TargetFramework>$(TargetFramework)</TargetFramework>
    </BuildOutputInPackage>
  </ItemDefinitionGroup>
  <PropertyGroup xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <RestoreOutputPath Condition="'$(RestoreOutputPath)' == ''">$(MSBuildProjectExtensionsPath)</RestoreOutputPath>
    <PackageOutputPath Condition="'$(PackageOutputPath)' == ''">$(BaseOutputPath)$(Configuration)\</PackageOutputPath>
    <NuspecOutputPath Condition="'$(NuspecOutputPath)' == ''">$(BaseIntermediateOutputPath)$(Configuration)\</NuspecOutputPath>
  </PropertyGroup>
  <!--
    ============================================================
    _GetAbsoluteOutputPathsForPack
    Gets the absolute output paths for Pack.
    ============================================================
  -->
  <Target Name="_GetAbsoluteOutputPathsForPack" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <ConvertToAbsolutePath Paths="$(RestoreOutputPath)">
      <Output TaskParameter="AbsolutePaths" PropertyName="RestoreOutputAbsolutePath" />
    </ConvertToAbsolutePath>
    <ConvertToAbsolutePath Paths="$(PackageOutputPath)">
      <Output TaskParameter="AbsolutePaths" PropertyName="PackageOutputAbsolutePath" />
    </ConvertToAbsolutePath>
    <ConvertToAbsolutePath Paths="$(NuspecOutputPath)">
      <Output TaskParameter="AbsolutePaths" PropertyName="NuspecOutputAbsolutePath" />
    </ConvertToAbsolutePath>
  </Target>
  <!--
    ============================================================
    _GetOutputItemsFromPack
    Gets the output '.nupkg' and '.nuspec' absolute file paths.
    ============================================================
  -->
  <Target Name="_GetOutputItemsFromPack" DependsOnTargets="_GetAbsoluteOutputPathsForPack" Returns="@(_OutputPackItems)" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <!-- 'PackageOutputAbsolutePath' and 'NuspecOutputAbsolutePath' will be provided by '_GetAbsoluteOutputPathsForPack' target -->
    <GetPackOutputItemsTask PackageOutputPath="$(PackageOutputAbsolutePath)" NuspecOutputPath="$(NuspecOutputAbsolutePath)" PackageId="$(PackageId)" PackageVersion="$(PackageVersion)" IncludeSymbols="$(IncludeSymbols)" IncludeSource="$(IncludeSource)" SymbolPackageFormat="$(SymbolPackageFormat)">
      <Output TaskParameter="OutputPackItems" ItemName="_OutputPackItems" />
    </GetPackOutputItemsTask>
  </Target>
  <!--
    ============================================================
    _GetTargetFrameworksOutput
    Read target frameworks from the project.
    ============================================================
  -->
  <Target Name="_GetTargetFrameworksOutput" Returns="@(_TargetFrameworks)" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <PropertyGroup>
      <_ProjectFrameworks />
    </PropertyGroup>
    <GetProjectTargetFrameworksTask ProjectPath="$(MSBuildProjectFullPath)" TargetFrameworks="$(TargetFrameworks)" TargetFramework="$(TargetFramework)" TargetFrameworkMoniker="$(TargetFrameworkMoniker)" TargetPlatformIdentifier="$(TargetPlatformIdentifier)" TargetPlatformVersion="$(TargetPlatformVersion)" TargetPlatformMinVersion="$(TargetPlatformMinVersion)">
      <Output TaskParameter="ProjectTargetFrameworks" PropertyName="_ProjectFrameworks" />
    </GetProjectTargetFrameworksTask>
    <ItemGroup Condition=" '$(_ProjectFrameworks)' != '' ">
      <_TargetFrameworks Include="$(_ProjectFrameworks.Split(';'))" />
    </ItemGroup>
  </Target>
  <!--
    ============================================================
    Pack
    Post Build Target
    ============================================================
  -->
  <Target Name="_PackAsBuildAfterTarget" AfterTargets="Build" Condition="'$(GeneratePackageOnBuild)' == 'true' AND '$(IsInnerBuild)' != 'true'" DependsOnTargets="Pack" xmlns="http://schemas.microsoft.com/developer/msbuild/2003" />
  <Target Name="_CleanPackageFiles" DependsOnTargets="_GetOutputItemsFromPack" AfterTargets="Clean" Condition="'$(GeneratePackageOnBuild)' == 'true' AND '$(IsInnerBuild)' != 'true'" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <ItemGroup>
      <_PackageFilesToDelete Include="@(_OutputPackItems)" />
    </ItemGroup>
    <Delete Files="@(_PackageFilesToDelete)" />
  </Target>
  <Target Name="_CalculateInputsOutputsForPack" DependsOnTargets="_GetOutputItemsFromPack" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <PropertyGroup Condition="$(ContinuePackingAfterGeneratingNuspec) == '' ">
      <ContinuePackingAfterGeneratingNuspec>false</ContinuePackingAfterGeneratingNuspec>
    </PropertyGroup>
    <ItemGroup>
      <!--This catches changes to properties-->
      <NuGetPackInput Include="$(MSBuildAllProjects)" />
      <NuGetPackInput Include="@(_PackageFiles)" />
      <NuGetPackInput Include="@(_PackageFilesToExclude)" />
      <NuGetPackInput Include="@(_BuildOutputInPackage->'%(FinalOutputPath)')" />
      <NuGetPackInput Include="@(_TargetPathsToSymbols->'%(FinalOutputPath)')" />
      <NuGetPackInput Include="@(_SourceFiles)" />
      <NuGetPackInput Include="@(_References)" />
      <NuGetPackOutput Include="@(_OutputPackItems)" />
    </ItemGroup>
  </Target>
  <!--
    ============================================================
    Pack
    Main entry point for packing packages
    ============================================================
  -->
  <Target Name="Pack" DependsOnTargets="$(PackDependsOn)" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <IsPackableFalseWarningTask Condition="'$(IsPackable)' == 'false' AND '$(WarnOnPackingNonPackableProject)' == 'true'" />
  </Target>
  <Target Name="_IntermediatePack" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <PropertyGroup>
      <ContinuePackingAfterGeneratingNuspec>true</ContinuePackingAfterGeneratingNuspec>
    </PropertyGroup>
  </Target>
  <Target Name="GenerateNuspec" Condition="'$(IsPackable)' == 'true'" Inputs="@(NuGetPackInput)" Outputs="@(NuGetPackOutput)" DependsOnTargets="$(GenerateNuspecDependsOn);_CalculateInputsOutputsForPack;_GetProjectReferenceVersions;_InitializeNuspecRepositoryInformationProperties" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <ConvertToAbsolutePath Condition="$(NuspecFile) != ''" Paths="$(NuspecFile)">
      <Output TaskParameter="AbsolutePaths" PropertyName="NuspecFileAbsolutePath" />
    </ConvertToAbsolutePath>
    <!-- Call Pack -->
    <PackTask PackItem="$(PackProjectInputFile)" PackageFiles="@(_PackageFiles)" PackageFilesToExclude="@(_PackageFilesToExclude)" PackageVersion="$(PackageVersion)" PackageId="$(PackageId)" Title="$(Title)" Authors="$(Authors)" Description="$(PackageDescription)" Copyright="$(Copyright)" RequireLicenseAcceptance="$(PackageRequireLicenseAcceptance)" LicenseUrl="$(PackageLicenseUrl)" ProjectUrl="$(PackageProjectUrl)" IconUrl="$(PackageIconUrl)" ReleaseNotes="$(PackageReleaseNotes)" Tags="$(PackageTags)" DevelopmentDependency="$(DevelopmentDependency)" BuildOutputInPackage="@(_BuildOutputInPackage)" ProjectReferencesWithVersions="@(_ProjectReferencesWithVersions)" TargetPathsToSymbols="@(_TargetPathsToSymbols)" TargetFrameworks="@(_TargetFrameworks)" FrameworksWithSuppressedDependencies="@(_FrameworksWithSuppressedDependencies)" AssemblyName="$(AssemblyName)" PackageOutputPath="$(PackageOutputAbsolutePath)" IncludeSymbols="$(IncludeSymbols)" IncludeSource="$(IncludeSource)" PackageTypes="$(PackageType)" IsTool="$(IsTool)" RepositoryUrl="$(RepositoryUrl)" RepositoryType="$(RepositoryType)" RepositoryBranch="$(RepositoryBranch)" RepositoryCommit="$(RepositoryCommit)" SourceFiles="@(_SourceFiles-&gt;Distinct())" NoPackageAnalysis="$(NoPackageAnalysis)" NoDefaultExcludes="$(NoDefaultExcludes)" MinClientVersion="$(MinClientVersion)" Serviceable="$(Serviceable)" FrameworkAssemblyReferences="@(_FrameworkAssemblyReferences)" ContinuePackingAfterGeneratingNuspec="$(ContinuePackingAfterGeneratingNuspec)" NuspecOutputPath="$(NuspecOutputAbsolutePath)" IncludeBuildOutput="$(IncludeBuildOutput)" BuildOutputFolders="$(BuildOutputTargetFolder)" ContentTargetFolders="$(ContentTargetFolders)" RestoreOutputPath="$(RestoreOutputAbsolutePath)" NuspecFile="$(NuspecFileAbsolutePath)" NuspecBasePath="$(NuspecBasePath)" NuspecProperties="$(NuspecProperties)" AllowedOutputExtensionsInPackageBuildOutputFolder="$(AllowedOutputExtensionsInPackageBuildOutputFolder)" AllowedOutputExtensionsInSymbolsPackageBuildOutputFolder="$(AllowedOutputExtensionsInSymbolsPackageBuildOutputFolder)" NoWarn="$(NoWarn)" WarningsAsErrors="$(WarningsAsErrors)" WarningsNotAsErrors="$(WarningsNotAsErrors)" TreatWarningsAsErrors="$(TreatWarningsAsErrors)" OutputFileNamesWithoutVersion="$(OutputFileNamesWithoutVersion)" InstallPackageToOutputPath="$(InstallPackageToOutputPath)" SymbolPackageFormat="$(SymbolPackageFormat)" PackageLicenseFile="$(PackageLicenseFile)" PackageLicenseExpression="$(PackageLicenseExpression)" PackageLicenseExpressionVersion="$(PackageLicenseExpressionVersion)" Readme="$(PackageReadmeFile)" Deterministic="$(Deterministic)" PackageIcon="$(PackageIcon)" />
  </Target>
  <!--
    Initialize Repository* properties from properties set by a source control package, if available in the project.
  -->
  <Target Name="_InitializeNuspecRepositoryInformationProperties" DependsOnTargets="InitializeSourceControlInformation" Condition="'$(SourceControlInformationFeatureSupported)' == 'true'" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <PropertyGroup>
      <!-- The project must specify PublishRepositoryUrl=true in order to publish the URL, in order to prevent inadvertent leak of internal URL. -->
      <RepositoryUrl Condition="'$(RepositoryUrl)' == '' and '$(PublishRepositoryUrl)' == 'true'">$(PrivateRepositoryUrl)</RepositoryUrl>
      <RepositoryCommit Condition="'$(RepositoryCommit)' == ''">$(SourceRevisionId)</RepositoryCommit>
    </PropertyGroup>
  </Target>
  <!--
    ============================================================
    _LoadPackGraphEntryPoints
    Find project entry point and load them into items.
    ============================================================
  -->
  <Target Name="_LoadPackInputItems" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <!-- Allow overriding items with PackProjectInputFile -->
    <PropertyGroup Condition="'$(PackProjectInputFile)' == ''">
      <PackProjectInputFile>$(MSBuildProjectFullPath)</PackProjectInputFile>
    </PropertyGroup>
  </Target>
  <Target Name="_GetProjectReferenceVersions" Condition="'$(NuspecFile)' == ''" DependsOnTargets="_GetAbsoluteOutputPathsForPack;$(GetPackageVersionDependsOn)" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <!-- 'RestoreOutputAbsolutePath' will be provided by '_GetAbsoluteOutputPathsForPack' target -->
    <ConvertToAbsolutePath Condition="'$(ProjectAssetsFile)' != ''" Paths="$(ProjectAssetsFile)">
      <Output TaskParameter="AbsolutePaths" PropertyName="ProjectAssetsFileAbsolutePath" />
    </ConvertToAbsolutePath>
    <GetProjectReferencesFromAssetsFileTask RestoreOutputAbsolutePath="$(RestoreOutputAbsolutePath)" ProjectAssetsFileAbsolutePath="$(ProjectAssetsFileAbsolutePath)">
      <Output TaskParameter="ProjectReferences" ItemName="_ProjectReferencesFromAssetsFile" />
    </GetProjectReferencesFromAssetsFileTask>
    <MSBuild Projects="@(_ProjectReferencesFromAssetsFile)" Targets="_GetProjectVersion" SkipNonexistentTargets="true" SkipNonexistentProjects="true" Properties="BuildProjectReferences=false;">
      <Output TaskParameter="TargetOutputs" ItemName="_ProjectReferencesWithVersions" />
    </MSBuild>
  </Target>
  <Target Name="_GetProjectVersion" DependsOnTargets="$(GetPackageVersionDependsOn)" Returns="@(_ProjectPathWithVersion)" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <ItemGroup>
      <_ProjectPathWithVersion Include="$(MSBuildProjectFullPath)">
        <ProjectVersion Condition="'$(PackageVersion)' != ''">$(PackageVersion)</ProjectVersion>
        <ProjectVersion Condition="'$(PackageVersion)' == ''">1.0.0</ProjectVersion>
      </_ProjectPathWithVersion>
    </ItemGroup>
  </Target>
  <Target Name="_WalkEachTargetPerFramework" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <MSBuild Condition="'$(IncludeBuildOutput)' == 'true'" Projects="$(MSBuildProjectFullPath)" Targets="_GetBuildOutputFilesWithTfm" Properties="TargetFramework=%(_TargetFrameworks.Identity);">
      <Output TaskParameter="TargetOutputs" ItemName="_BuildOutputInPackage" />
    </MSBuild>
    <MSBuild Condition="'$(TargetsForTfmSpecificContentInPackage)' != ''" Projects="$(MSBuildProjectFullPath)" Targets="_GetTfmSpecificContentForPackage" Properties="TargetFramework=%(_TargetFrameworks.Identity);">
      <Output TaskParameter="TargetOutputs" ItemName="_PackageFiles" />
    </MSBuild>
    <MSBuild Condition="'$(IncludeBuildOutput)' == 'true'" Projects="$(MSBuildProjectFullPath)" Targets="_GetDebugSymbolsWithTfm" Properties="TargetFramework=%(_TargetFrameworks.Identity);">
      <Output TaskParameter="TargetOutputs" ItemName="_TargetPathsToSymbols" />
    </MSBuild>
    <MSBuild Condition="'$(IncludeSource)' == 'true'" Projects="$(MSBuildProjectFullPath)" Targets="SourceFilesProjectOutputGroup" Properties="TargetFramework=%(_TargetFrameworks.Identity);&#xD;&#xA;                  BuildProjectReferences=false;">
      <Output TaskParameter="TargetOutputs" ItemName="_SourceFiles" />
    </MSBuild>
    <MSBuild Projects="$(MSBuildProjectFullPath)" Targets="_GetFrameworkAssemblyReferences" Properties="TargetFramework=%(_TargetFrameworks.Identity);&#xD;&#xA;                  BuildProjectReferences=false;">
      <Output TaskParameter="TargetOutputs" ItemName="_FrameworkAssemblyReferences" />
    </MSBuild>
    <MSBuild Projects="$(MSBuildProjectFullPath)" Targets="_GetFrameworksWithSuppressedDependencies" Properties="TargetFramework=%(_TargetFrameworks.Identity);&#xD;&#xA;                  BuildProjectReferences=false;">
      <Output TaskParameter="TargetOutputs" ItemName="_FrameworksWithSuppressedDependencies" />
    </MSBuild>
  </Target>
  <Target Name="_GetFrameworksWithSuppressedDependencies" Returns="@(_TfmWithDependenciesSuppressed)" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <ItemGroup>
      <_TfmWithDependenciesSuppressed Include="$(TargetFramework)" Condition="'$(SuppressDependenciesWhenPacking)' == 'true'" />
    </ItemGroup>
  </Target>
  <Target Name="_GetFrameworkAssemblyReferences" DependsOnTargets="ResolveReferences" Returns="@(TfmSpecificFrameworkAssemblyReferences)" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <ItemGroup>
      <TfmSpecificFrameworkAssemblyReferences Include="@(ReferencePath->'%(OriginalItemSpec)')" Condition="'%(ReferencePath.Pack)' != 'false' AND '%(ReferencePath.ResolvedFrom)' == '{TargetFrameworkDirectory}'">
        <TargetFramework>$(TargetFramework)</TargetFramework>
      </TfmSpecificFrameworkAssemblyReferences>
    </ItemGroup>
  </Target>
  <Target Name="_GetBuildOutputFilesWithTfm" DependsOnTargets="BuiltProjectOutputGroup;DocumentationProjectOutputGroup;SatelliteDllsProjectOutputGroup;_AddPriFileToPackBuildOutput;$(TargetsForTfmSpecificBuildOutput)" Returns="@(BuildOutputInPackage)" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <ItemGroup Condition="'$(IncludeBuildOutput)' == 'true'">
      <BuildOutputInPackage Include="@(SatelliteDllsProjectOutputGroupOutput);&#xD;&#xA;                            @(BuiltProjectOutputGroupOutput);&#xD;&#xA;                            @(DocumentationProjectOutputGroupOutput);&#xD;&#xA;                            @(_PathToPriFile)" />
    </ItemGroup>
  </Target>
  <Target Name="_GetTfmSpecificContentForPackage" DependsOnTargets="$(TargetsForTfmSpecificContentInPackage)" Returns="@(TfmSpecificPackageFileWithRecursiveDir)" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <!-- The below workaround needs to be done due to msbuild bug https://github.com/Microsoft/msbuild/issues/3121 -->
    <ItemGroup>
      <TfmSpecificPackageFileWithRecursiveDir Include="@(TfmSpecificPackageFile)">
        <NuGetRecursiveDir>%(TfmSpecificPackageFile.RecursiveDir)</NuGetRecursiveDir>
        <BuildAction>%(TfmSpecificPackageFile.BuildAction)</BuildAction>
      </TfmSpecificPackageFileWithRecursiveDir>
    </ItemGroup>
  </Target>
  <Target Name="_GetDebugSymbolsWithTfm" DependsOnTargets="DebugSymbolsProjectOutputGroup;$(TargetsForTfmSpecificDebugSymbolsInPackage)" Returns="@(_TargetPathsToSymbolsWithTfm)" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <ItemGroup Condition="'$(IncludeBuildOutput)' == 'true'">
      <_TargetPathsToSymbolsWithTfm Include="@(DebugSymbolsProjectOutputGroupOutput)">
        <TargetFramework>$(TargetFramework)</TargetFramework>
      </_TargetPathsToSymbolsWithTfm>
    </ItemGroup>
    <ItemGroup>
      <_TargetPathsToSymbolsWithTfm Include="@(TfmSpecificDebugSymbolsFile)" />
    </ItemGroup>
  </Target>
  <!--Projects with target framework like UWP, Win8, wpa81 produce a Pri file
    in their bin dir. This Pri file is not included in the BuiltProjectGroupOutput, and
    has to be added manually here.-->
  <Target Name="_AddPriFileToPackBuildOutput" Returns="@(_PathToPriFile)" DependsOnTargets="$(AddPriFileDependsOn)" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <ItemGroup Condition="'$(IncludeProjectPriFile)' == 'true'">
      <_PathToPriFile Include="$(ProjectPriFullPath)">
        <FinalOutputPath>$(ProjectPriFullPath)</FinalOutputPath>
        <TargetPath>$(ProjectPriFileName)</TargetPath>
      </_PathToPriFile>
    </ItemGroup>
  </Target>
  <!--
    ============================================================
    _GetPackageFiles
    Entry point for generating the project to project references.
    ============================================================
  -->
  <Target Name="_GetPackageFiles" Condition="$(IncludeContentInPack) == 'true'" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <ItemGroup>
      <_PackageFilesToExclude Include="@(Content)" Condition="'%(Content.Pack)' == 'false'" />
    </ItemGroup>
    <!-- Include PackageFiles and Content of the project being packed -->
    <ItemGroup>
      <_PackageFiles Include="@(Content)" Condition=" %(Content.Pack) != 'false' ">
        <BuildAction Condition="'%(Content.BuildAction)' == ''">Content</BuildAction>
      </_PackageFiles>
      <_PackageFiles Include="@(Compile)" Condition=" %(Compile.Pack) == 'true' ">
        <BuildAction Condition="'%(Compile.BuildAction)' == ''">Compile</BuildAction>
      </_PackageFiles>
      <_PackageFiles Include="@(None)" Condition=" %(None.Pack) == 'true' ">
        <BuildAction Condition="'%(None.BuildAction)' == ''">None</BuildAction>
      </_PackageFiles>
      <_PackageFiles Include="@(EmbeddedResource)" Condition=" %(EmbeddedResource.Pack) == 'true' ">
        <BuildAction Condition="'%(EmbeddedResource.BuildAction)' == ''">EmbeddedResource</BuildAction>
      </_PackageFiles>
      <_PackageFiles Include="@(ApplicationDefinition)" Condition=" %(ApplicationDefinition.Pack) == 'true' ">
        <BuildAction Condition="'%(ApplicationDefinition.BuildAction)' == ''">ApplicationDefinition</BuildAction>
      </_PackageFiles>
      <_PackageFiles Include="@(Page)" Condition=" %(Page.Pack) == 'true' ">
        <BuildAction Condition="'%(Page.BuildAction)' == ''">Page</BuildAction>
      </_PackageFiles>
      <_PackageFiles Include="@(Resource)" Condition=" %(Resource.Pack) == 'true' ">
        <BuildAction Condition="'%(Resource.BuildAction)' == ''">Resource</BuildAction>
      </_PackageFiles>
      <_PackageFiles Include="@(SplashScreen)" Condition=" %(SplashScreen.Pack) == 'true' ">
        <BuildAction Condition="'%(SplashScreen.BuildAction)' == ''">SplashScreen</BuildAction>
      </_PackageFiles>
      <_PackageFiles Include="@(DesignData)" Condition=" %(DesignData.Pack) == 'true' ">
        <BuildAction Condition="'%(DesignData.BuildAction)' == ''">DesignData</BuildAction>
      </_PackageFiles>
      <_PackageFiles Include="@(DesignDataWithDesignTimeCreatableTypes)" Condition=" %(DesignDataWithDesignTimeCreatableTypes.Pack) == 'true' ">
        <BuildAction Condition="'%(DesignDataWithDesignTimeCreatableTypes.BuildAction)' == ''">DesignDataWithDesignTimeCreatableTypes</BuildAction>
      </_PackageFiles>
      <_PackageFiles Include="@(CodeAnalysisDictionary)" Condition=" %(CodeAnalysisDictionary.Pack) == 'true' ">
        <BuildAction Condition="'%(CodeAnalysisDictionary.BuildAction)' == ''">CodeAnalysisDictionary</BuildAction>
      </_PackageFiles>
      <_PackageFiles Include="@(AndroidAsset)" Condition=" %(AndroidAsset.Pack) == 'true' ">
        <BuildAction Condition="'%(AndroidAsset.BuildAction)' == ''">AndroidAsset</BuildAction>
      </_PackageFiles>
      <_PackageFiles Include="@(AndroidResource)" Condition=" %(AndroidResource.Pack) == 'true' ">
        <BuildAction Condition="'%(AndroidResource.BuildAction)' == ''">AndroidResource</BuildAction>
      </_PackageFiles>
      <_PackageFiles Include="@(BundleResource)" Condition=" %(BundleResource.Pack) == 'true' ">
        <BuildAction Condition="'%(BundleResource.BuildAction)' == ''">BundleResource</BuildAction>
      </_PackageFiles>
    </ItemGroup>
  </Target>
  <!--
============================================================================================================================================
  </Import>

C:\Program Files\dotnet\sdk\8.0.110\Sdks\Microsoft.NET.Sdk\Sdk\Sdk.targets
============================================================================================================================================
-->
  <!--
============================================================================================================================================
  </Import>

C:\Users\calope\source\repos\runtime8\src\libraries\System.IO.Ports\tests\System.IO.Ports.Tests.csproj
============================================================================================================================================
-->
</Project>