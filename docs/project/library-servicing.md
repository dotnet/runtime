# How to service a library

This document provides the steps necessary after modifying a library in a servicing branch (where "servicing branch" refers to any branch whose name begins with `release/`).

## Check for existence of a .pkgproj

Your first step is to determine whether or not your library has its own package. To do this, go into the root folder for the library you've made changes to. If there is a `pkg` folder there (which should have a `.pkgproj` file inside of it), your library does have its own package. If there is no `pkg` folder there, the library is the part of the shared framework. If it is, then there is nothing that needs to be done.

For example, if you made changes to [Microsoft.Win32.Primitives](https://github.com/dotnet/runtime/tree/master/src/libraries/Microsoft.Win32.Primitives), then you have a `.pkgproj`, and will have to follow the steps in this document. However, if you made changes to [System.Collections](https://github.com/dotnet/runtime/tree/master/src/libraries/System.Collections), then you don't have a `.pkgproj`, and you do not need to do any further work for servicing.

## Determine ServicingVersion

When you make a change to a library & ship it during the servicing release, the `ServicingVersion` must be bumped. This property is found in the library's `Directory.Build.props`. It's also possible that the property is not in that file, in which case we're using the default version. You'll need to increment this servicing version. If the property is already present in your library's source folder, just increment the patch version by 1. If it's not present, add it to the library's `Directory.Build.props` and set it to 1.

Note that it's possible that somebody else has already incremented the servicing version since our last release. If this is the case, you don't need to increment it yourself. To confirm, check [Nuget.org](https://www.nuget.org/) to see if there is already a published version of your package with the same servicing version. If so, the `ServicingVersion` must be incremented. If not, there's still a possibility that the version should be bumped, in the event that we've already built the package with its current version in an official build, but haven't released it publicly yet. If you see that the `ServicingVersion` has been changed in the last 1-2 months, but don't see a matching package in Nuget.org, contact somebody on the servicing team for guidance.

## Update the package index

The baseline version for the assembly in the package index should also be incremented so that all the other packages can use this servicing version for package dependencies.
The package version should also be added to the stable versions list.

## Add your package to libraries-packages.proj

In order to ensure that your package gets built, you need to add it to [libraries-packages.proj](https://github.com/dotnet/runtime/blob/master/src/libraries/libraries-packages.proj). In the linked example, we were building `System.Drawing.Common`. All you have to do is add a `Project` block inside the linked ItemGroup that matches the form of the linked example, but with `System.Drawing.Common` replaced by your library's name. Again, make sure to do this in the right servicing branch.

## Test your changes

All that's left is to ensure that your changes have worked as expected. To do so, execute the following steps:

1. From a clean copy of your branch, run `build.cmd -allconfigurations`

2. Check in `bin\packages\Debug` for the existence of your package, with the appropriate package version.

3. Try installing the built package in a test application, testing that your changes to the library are present & working as expected.
   To install your package add your local packages folder as a feed source in VS or your nuget.config and then add a PackageReference to the specific version of the package you built then try using the APIs.

## Approval Process

All the servicing change must go through an approval process. Please create your PR using [this template](https://github.com/dotnet/runtime/compare/template?expand=1&template=servicing_pull_request_template.md). You should also add `servicing-consider` label to the pull request and bring it to the attention of the engineering lead responsible for the area.