# How to service a library

This document provides the steps necessary after modifying a library in a servicing branch (where "servicing branch" refers to any branch whose name begins with `release/`).

## Check for existence of a .pkgproj

Your first step is to determine whether or not your library has its own package. To do this, go into the root folder for the library you've made changes to. If there is a `pkg` folder there (which should have a `.pkgproj` file inside of it), your library does have its own package. If there is no `pkg` folder there, the library is the part of the shared framework. If it is, then there is nothing that needs to be done.

For example, if you made changes to [Microsoft.Win32.Primitives](https://github.com/dotnet/runtime/tree/main/src/libraries/Microsoft.Win32.Primitives), then you have a `.pkgproj`, and will have to follow the steps in this document. However, if you made changes to [System.Collections](https://github.com/dotnet/runtime/tree/main/src/libraries/System.Collections), then you don't have a `.pkgproj`, and you do not need to do any further work for servicing.

## Determine PackageVersion

Each package has a property called `PackageVersion`. When you make a change to a library & ship it, the `PackageVersion` must be bumped. This property could either be in one of two places inside your library's source folder: `Directory.Build.Props` (or dir.props), or the `.pkgproj`. It's also possible that the property is in neither of those files, in which case we're using the default version from [Packaging.props](https://github.com/dotnet/runtime/blob/main/eng/packaging.props#L25) (IMPORTANT - make sure to check the default version from the branch that you're making changes to, not from main). You'll need to increment this package version. If the property is already present in your library's source folder, just increment the patch version by 1 (e.g. `4.6.0` -> `4.6.1`). If it's not present, add it to the  library's `Directory.Build.props`, where it is equal to the version in `Packaging.props`, with the patch version incremented by 1.

Note that it's possible that somebody else has already incremented the package version since our last release. If this is the case, you don't need to increment it yourself. To confirm, check [Nuget.org](https://www.nuget.org/) to see if there is already a published version of your package with the same package version. If so, the `PackageVersion` must be incremented. If not, there's still a possibility that the version should be bumped, in the event that we've already built the package with its current version in an official build, but haven't released it publicly yet. If you see that the `PackageVersion` has been changed in the last 1-2 months, but don't see a matching package in Nuget.org, contact somebody on the servicing team for guidance.

## Determine AssemblyVersion

Each library has a property called `AssemblyVersion` which will be in either the library `.csproj` or in `Directory.Build.Props`. For servicing events you will want to increment the revision by 1 (e.g `4.0.0.0` -> `4.0.0.1`) in the library's `Directory.Build.Props` if the library ships in its own package and has an asset that is applicable to .NET Framework. To determine if it applies to .NET Framework you should check to see if there are any `netstandard` or `net4x` configurations in the ref and/or src project, if there are then it has assets that apply.

The reason we need to increment the assembly version for things running on .NET Framework is because of the way binding works there. If there are two assemblies with the same assembly version the loader will essentially pick the first one it finds and use that version so applications don't have full control over using the later build with a particular fix included. This is worse if someone puts the older assembly in the GAC as the GAC will always win for matching assembly versions so an application couldn't load the newer one because it has the same assembly version.

If this library ships both inbox on a platform and in its own library package then we need to keep the reference assembly version pinned to the same version that was shipped inbox on. In those cases we generally need to condition the AssemblyVersion in the library `ref` project like such:

```
    <!-- Must match version supported by frameworks which support 4.0.* inbox.
         Can be removed when API is added and this assembly is versioned to 4.1.* -->
    <AssemblyVersion Condition="!$(TargetFramework.StartsWith('net4'))' != 'true'">4.0.3.0</AssemblyVersion>
```
Where the `AssemblyVersion` is set to the old version before updating. To determine if the library ships inbox you can look at for `InboxOnTargetFramework` item groups or `TreatAsOutOfBox` suppressions in the pkgproj for the library.

If the library is part of a Aspnetcore or .NET targeting pack then we cannot increment the assembly version. For Aspnetcore, You can examine the ```<IsAspNetCoreApp>true</IsAspNetCoreApp>``` property in the library`s ```Directory.Build.Props```
For .Net you can examine the list in ```NetCoreAppLibrary.Props```

If the library is part of a targeting pack and also contains an asset applicable to the .NET Framework then we will increment the assembly version for that asset.
eg.
```
<AssemblyVersion Condition="$(TargetFramework.StartsWith('net4'))">5.0.0.1</AssemblyVersion>
<SkipValidatePackage>true</SkipValidatePackage>
```

## Update the package index

If you incremented the `AssemblyVersion` in the last step, you'll also need to add an entry to [packageIndex.json](https://github.com/dotnet/runtime/blob/main/src/libraries/pkg/Microsoft.Private.PackageBaseline/packageIndex.json). Find the entry for your library in that file (again, making sure you're in the correct release branch), then find the subsection labeled `AssemblyVersionInPackageVersion`. There, add an entry that maps your new `AssemblyVersion` to your new `PackageVersion`. For an example, see [this PR](https://github.com/dotnet/runtime/commit/d0e4dcc7ebf008e7b6835cafbd03878c3a0e75f8#diff-ec9fd7a62cb0c494d86029014940382cR107), where we bumped the `PackageVersion` of `Microsoft.Diagnostics.Tracing.EventSource` from `2.0.0` to `2.0.1`, and bumped the `AssemblyVersion` from `2.0.0.0` to `2.0.1.0`. Therefore, we added an entry to `packageIndex.json` of the form `"2.0.1.0": "2.0.1"`.
The baseline version for the assembly in the package index should also be incremented so that all the other packages can use this servicing version for package dependencies.

## Add your package to libraries-packages.proj

In order to ensure that your package gets built, you need to add it to [libraries-packages.proj](https://github.com/dotnet/runtime/blob/main/src/libraries/libraries-packages.proj). In the linked example, we were building `System.Drawing.Common`. All you have to do is add a `Project` block inside the linked ItemGroup that matches the form of the linked example, but with `System.Drawing.Common` replaced by your library's name. Again, make sure to do this in the right servicing branch.

## Test your changes

All that's left is to ensure that your changes have worked as expected. To do so, execute the following steps:

1. From a clean copy of your branch, run `build.cmd -allconfigurations`

2. Check in `bin\packages\Debug` for the existence of your package, with the appropriate package version.

3. Try installing the built package in a test application, testing that your changes to the library are present & working as expected.
   To install your package add your local packages folder as a feed source in VS or your nuget.config and then add a PackageReference to the specific version of the package you built then try using the APIs.

## Approval Process

All the servicing change must go through an approval process. Please create your PR using [this template](https://raw.githubusercontent.com/dotnet/runtime/main/.github/PULL_REQUEST_TEMPLATE/servicing_pull_request_template.md). You should also add `servicing-consider` label to the pull request and bring it to the attention of the engineering lead responsible for the area.
