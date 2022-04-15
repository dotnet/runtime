# Packaging

Libraries can be packaged in one or more of the following ways: as part of the .NETCore shared framework, part of a transport package, or as a NuGet package.

## .NETCoreApp shared framework

To add a library to the .NETCoreApp shared framework, that library's `AssemblyName` should be added to [NetCoreAppLibrary.props](../../src/libraries/NetCoreAppLibrary.props)

The library should have both a `ref` and `src` project. Its reference assembly will be included in the targeting pack (also called ref pack) for the Microsoft.NETCore.App shared framework, and its implementation assembly will be included in the runtime pack.

Including a library in the shared framework only includes the best applicable TargetFramework build of that library: `$(NetCoreAppCurrent)` if it exists, but possibly `netstandard2.1` or another if that is best. If a library has builds for other frameworks those will only be shipped if the library also produces a [Nuget package](#nuget-package).

In some occasions we may want to include a library in the shared framework, but not expose it publicly. The library should be named in a way to discourage use at runtime, for example using the `System.Private` prefix. We should avoid hiding arbitrary public libraries as it complicates deployment and servicing.

Libraries included in the shared framework should ensure all direct and transitive assembly references are also included in the shared framework. This will be validated as part of the build and errors raised if any dependencies are unsatisfied.

Source generators and analyzers can be included in the shared framework by specifying `IsNetCoreAppAnalyzer`.  These projects should specify `AnalyzerLanguage` as mentioned [below](#analyzers--source-generators).

Removing a library from the shared framework is a breaking change and should be avoided.

## Transport package

Transport packages are non-shipping packages that dotnet/runtime produces in order to share binaries with other repositories.

### Microsoft.Internal.Runtime.**[TargetRepositoryName]**.Transport

Such transport packages represent the set of libraries which are produced in dotnet/runtime and ship in target repo's shared framework (i.e. Microsoft.AspNetCore.App and Microsoft.WindowsDesktop.App). We produce a transport package so that we can easily share reference, implementation and analyzer assemblies that might not be present in NuGet packages that also ship.

To add a library to the target's shared framework, that library should be listed in the `AspNetCoreAppLibrary` or `WindowsDesktopAppLibrary` section in `NetCoreAppLibrary.props`.

Source generators and analyzers can be included in the package by adding them to the `Microsoft.Internal.Runtime.**TARGET**.Transport.proj` as an AnalyzerReference. The analyzer projects should specify `AnalyzerLanguage` as mentioned [below](#analyzers--source-generators).

Libraries included in this transport package should ensure all direct and transitive assembly references are also included in either the target's shared framework or the Microsoft.NETCore.App shared framework. This is not validated in dotnet/runtime at the moment: https://github.com/dotnet/runtime/issues/52562

Removing a library from this transport package is a breaking change and should be avoided.

## NuGet package

Libraries to be packaged must set `IsPackable` to true. By default, all `libraries/*/src` projects are considered for packaging.

Package versions and shipping state should be controlled using the properties defined by the [Arcade SDK](https://github.com/dotnet/arcade/blob/master/Documentation/ArcadeSdk.md#project-properties-defined-by-the-sdk). Typically libraries should not need to explicitly set any of these properties.

Most metadata for packages is controlled centrally in the repository and individual projects may not need to make any changes to these. One property is required to be set in each project: `PackageDescription`. This should be set to a descriptive summary of the purpose of the package, and a list of common entry-point types for the package: to aide in search engine optimization. Example:
```xml
<PackageDescription>Logging abstractions for Microsoft.Extensions.Logging.

Commonly Used Types:
Microsoft.Extensions.Logging.ILogger
Microsoft.Extensions.Logging.ILoggerFactory
Microsoft.Extensions.Logging.ILogger&lt;TCategoryName&gt;
Microsoft.Extensions.Logging.LogLevel
Microsoft.Extensions.Logging.Logger&lt;T&gt;
Microsoft.Extensions.Logging.LoggerMessage
Microsoft.Extensions.Logging.Abstractions.NullLogger</PackageDescription>
```

Package content can be defined using any of the publicly defined Pack inputs: https://docs.microsoft.com/en-us/nuget/reference/msbuild-targets

### Build props / targets and other content

Build props and targets may be needed in NuGet packages. To define these, author a build folder in your src project and place the necessary props/targets in this subfolder. You can then add items to include these in the package by defining `Content` items and setting `PackagePath` as follows:
```xml
  <ItemGroup>
    <Content Include="build\netstandard2.0\$(MSBuildProjectName).props" PackagePath="%(Identity)" />
    <Content Include="build\netstandard2.0\$(MSBuildProjectName).targets" PackagePath="%(Identity)" />
  </ItemGroup>
```

### Analyzers / source generators

Some packages may wish to include a companion analyzer or source-generator with their library. Analyzers are much different from normal library contributors: their dependencies shouldn't be treated as nuget package dependencies, their TargetFramework isn't applicable to the project they are consumed in (since they run in the compiler). To facilitate this, we've defined some common infrastructure for packaging Analyzers.

To include an analyzer in a package, simply add an `AnalyzerReference` item to the project that produces the package that should contain the analyzer
```xml
  <ItemGroup> 
    <AnalyzerReference Include="..\gen\System.Banana.Generators.csproj" />
  </ItemGroup>
```

In the analyzer project make sure to do the following. Ensure it only targets `netstandard2.0` since this is a requirement of the compiler. Enable localization by setting `UsingToolXliff`. Set the `AnalyzerLanguage` property to either `cs` or `vb` if the analyzer is specific to that language. By default the analyzer will be packaged as language-agnostic. Avoid any dependencies in Analyzer projects that aren't already provided by the compiler.
```xml
  <PropertyGroup>
    <TargetFramework>netstandard2.0</TargetFramework>
    <UsingToolXliff>true</UsingToolXliff>
    <AnalyzerLanguage>cs</AnalyzerLanguage>
  </PropertyGroup>
```

In order to mitigate design-time/build-time performance issues with source generators, we generate build logic to allow the end user to disable the source generator from the package. By default, the MSBuild property an end user can set is named `Disable{PackageId}SourceGenerator`. If a package needs a custom property name, this can be overriden by setting the following property in the project that produces the package
```xml
  <PropertyGroup>
    <DisableSourceGeneratorPropertyName>CustomPropertyName</DisableSourceGeneratorPropertyName>
  </PropertyGroup>
```

### NETStandard Compatibility Error infrastructure
For libraries that support .NETStandard, the _.NETStandard Compatibility packaging infrastructure_ makes sure that out-of-support target frameworks like _netcoreapp3.1_ or _net461_ are unsupported by the produced package. That enables library authors to support .NETStandard but explicitly not support unsupported .NETStandard compatible target frameworks.

The infrastructure generates a targets file that throws a user readable Error when msbuild invokes a project with an unsupported target framework. In addition to the targets file, placeholder files `_._` are placed into the minimum supported .NETStandard compatible target framework's package folder (as time of writing `net6.0` and `net462`), so that the generated targets files don't apply for that and any newer/compatible target framework. Example:

```
buildTransitive\net461\Microsoft.Extensions.Configuration.UserSecrets.targets            <- This file is generated and throws an Error
buildTransitive\net462\_._
buildTransitive\netcoreapp2.0\Microsoft.Extensions.Configuration.UserSecrets.targets     <- This file is generated and throws an Error
buildTransitive\net6.0\_._ 
```

Whenever a library wants to author their own set of props and targets files (i.e. for source generators) and the above mentioned infrastructure kicks in (because the library targets .NETStandard), such files **must be included not only for the .NETStandard target framework but also for the specific minimum supported target frameworks**. The _.NETStandard Compatibility packaging infrastructure_ then omits the otherwise necessary placeholder files. Example:

```
buildTransitive\netstandard2.0\Microsoft.Extensions.Configuration.UserSecrets.targets    <- This file is hand authored and doesn't throw an error
buildTransitive\net461\Microsoft.Extensions.Configuration.UserSecrets.targets            <- This file is generated and throws an Error
buildTransitive\net462\Microsoft.Extensions.Configuration.UserSecrets.targets            <- This file is hand authored and doesn't throw an error
buildTransitive\netcoreapp2.0\Microsoft.Extensions.Configuration.UserSecrets.targets     <- This file is generated and throws an Error
buildTransitive\net6.0\Microsoft.Extensions.Configuration.UserSecrets.targets            <- This file is hand authored and doesn't throw an error
```

The above layout is achieved via the following item declaration in the project file. In that case, the hand authored msbuild props and/or targets files are located in a buildTransitive folder in the project tree. Note that the trailing directory separators are required.

```xml
<ItemGroup>
    <Content Include="buildTransitive\$(MSBuildProjectName).*"
             PackagePath="buildTransitive\netstandard2.0\;
                          buildTransitive\$(NetFrameworkMinimum)\;
                          buildTransitive\$(NetCoreAppMinimum)\" />
</ItemGroup>
```
