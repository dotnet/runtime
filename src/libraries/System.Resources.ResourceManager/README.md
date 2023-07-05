# System.Resources.ResourceManager
Provides classes and interfaces that allow developers to create, store, and manage various culture-specific resources used in an application. One of the most important classes of this namespace is the [`ResourceManager`](https://learn.microsoft.com/dotnet/api/system.resources.resourcemanager) class, It allows the user to access and control resources stored in the main assembly or in resource satellite assemblies. In .NET core a resource file is compiled and embedded automatically with MSBuild.

Documentation can be found at https://learn.microsoft.com/dotnet/api/system.resources.

## Contribution Bar
- [x] [We only consider fixes to maintain or improve quality](../../libraries/README.md#primary-bar)
- [x] [We consider PRs that target this library for new source code analyzers](../../libraries/README.md#secondary-bars)

This is the primary assembly for resources in .NET hasn't changed much in the past years.

See the [Help Wanted](https://github.com/dotnet/runtime/issues?q=is%3Aissue+is%3Aopen+label%3A%22help+wanted%22+label%3Aarea-System.Resources) issues.

## Source

* CoreClr-specific: [../../coreclr/System.Private.CoreLib/src/System/Resources](../../coreclr/System.Private.CoreLib/src/System/Resources)
* Mono-specific: [../../mono/System.Private.CoreLib/src/System/Resources](../../mono/System.Private.CoreLib/src/System/Resources)
* Shared between CoreClr and Mono: [../System.Private.CoreLib/src/System/Resources](../System.Private.CoreLib/src/System/Resources)

## Deployment
[System.Resources.ResourceManager](https://www.nuget.org/packages/System.Resources.ResourceManager) is included in the shared framework. The package does not need to be installed into any project compatible with .NET Standard 2.0.