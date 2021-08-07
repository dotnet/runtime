# How to service a library

This document provides the steps necessary after modifying a library in a servicing branch (where "servicing branch" refers to any branch whose name begins with `release/`).

## Check if a package is generated

If a library's source project sets `<IsPackable>true</IsPackable>` a package is generated. If the library's source project doesn't set `<IsPackable>true</IsPackable>`, the library likely is part of the shared framework (to check for that, look into the `NetCoreAppLibrary.props` file). If it is, then there is nothing that needs to be done here.

## Determine PackageVersion

Each package has a property called `PackageVersion`. When you make a change to a library & ship it, the `PackageVersion` must be bumped. This property could either be in one of two places inside your library's source folder: `Directory.Build.Props`, or in the the source project (under src/). It's also possible that the property is in neither of those files, in which case we're using the default version which is calculated based on the version properties in the [Versions.props](https://github.com/dotnet/runtime/blob/95147163dac477da5177f5c5402ae9b93feb5c89/eng/Versions.props#L6-L8) file. (IMPORTANT - make sure to check the default version from the branch that you're making changes to, not from main). You'll need to increment this package version. If the property is already present in your library's source folder, just increment the patch version by 1 (e.g. `4.6.0` -> `4.6.1`). If it's not present, add it to the  library's `Directory.Build.props`, where it is equal to the patch version in `Version.props`, but with the patch version incremented by one.

Note that it's possible that somebody else has already incremented the package version since our last release. If this is the case, you don't need to increment it yourself. To confirm, check [Nuget.org](https://www.nuget.org/) to see if there is already a published version of your package with the same package version. If so, the `PackageVersion` must be incremented. If not, there's still a possibility that the version should be bumped, in the event that we've already built the package with its current version in an official build, but haven't released it publicly yet. If you see that the `PackageVersion` has been changed in the last 1-2 months, but don't see a matching package in Nuget.org, contact somebody on the servicing team for guidance.

## Determine AssemblyVersion

A library's assembly version is controlled by the property `AssemblyVersion`. If the property exists, it's either located in the library's source project or in its `Directory.Build.props` file. If it doesn't exist, it uses the [centrally defined version](https://github.com/dotnet/runtime/blob/1fb151f63dca644347a5c608d7ab17f7cb8e1ccb/eng/Versions.props#L14) and you want to add the property to the library's `Directory.Build.props` file. For servicing events you will want to increment the revision by 1 (e.g `4.0.0.0` -> `4.0.0.1`) if the library ships in its own package and has an asset that is applicable to .NET Framework. To determine if it applies to .NET Framework you should check to see if there are any `netstandard` or `net4x` target frameworks in the source project and if there are, it has assets that apply.

The reason we need to increment the assembly version for things running on .NET Framework is because of the way binding works there. If there are two assemblies with the same assembly version the loader will essentially pick the first one it finds and use that version so applications don't have full control over using the later build with a particular fix included. This is worse if someone puts the older assembly in the GAC as the GAC will always win for matching assembly versions so an application couldn't load the newer one because it has the same assembly version.

If this library ships both inbox on a platform and in its own library package then we need to keep the reference assembly version pinned to the same version that was shipped inbox on. In those cases we generally need to condition the AssemblyVersion in the library `ref` project like such:

```
    <!-- Must match version supported by frameworks which support 4.0.* inbox.
         Can be removed when API is added and this assembly is versioned to 4.1.* -->
    <AssemblyVersion Condition="!$(TargetFramework.StartsWith('net4'))' != 'true'">4.0.3.0</AssemblyVersion>
```
Where the `AssemblyVersion` is set to the old version before updating. To determine if the library ships inbox you can look at the list in [NetCoreAppLibrary.props](https://github.com/dotnet/runtime/blob/95147163dac477da5177f5c5402ae9b93feb5c89/src/libraries/NetCoreAppLibrary.props#L1).

If the library is part of a Aspnetcore or .NET targeting pack then we cannot increment the assembly version. For Aspnetcore, You can examine the ```<IsAspNetCoreApp>true</IsAspNetCoreApp>``` property in the library`s ```Directory.Build.props```
For .Net you can examine the list in ```NetCoreAppLibrary.props```

If the library is part of a targeting pack and also contains an asset applicable to the .NET Framework then we will increment the assembly version for that asset.
eg.
```
<AssemblyVersion Condition="$(TargetFramework.StartsWith('net4'))">6.0.0.1</AssemblyVersion>
```

## Test your changes

All that's left is to ensure that your changes have worked as expected. To do so, execute the following steps:

1. From a clean copy of your branch, run `build.cmd/sh libs -allconfigurations`

2. Check in `bin\packages\Debug` for the existence of your package, with the appropriate package version.

3. Try installing the built package in a test application, testing that your changes to the library are present & working as expected.
   To install your package add your local packages folder as a feed source in VS or your nuget.config and then add a PackageReference to the specific version of the package you built then try using the APIs.

## Approval Process

All the servicing change must go through an approval process. Please create your PR using [this template](https://raw.githubusercontent.com/dotnet/runtime/main/.github/PULL_REQUEST_TEMPLATE/servicing_pull_request_template.md). You should also add `servicing-consider` label to the pull request and bring it to the attention of the engineering lead responsible for the area.
