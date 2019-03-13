Recommended reading to better understand this document:
[.NET Standard](https://github.com/dotnet/standard/blob/master/docs/faq.md)
| [Project-Guidelines](https://github.com/dotnet/corefx/blob/master/Documentation/coding-guidelines/project-guidelines.md)
| [Package-Projects](https://github.com/dotnet/corefx/blob/master/Documentation/coding-guidelines/package-projects.md)

# Add APIs
- [Determining versions and targets](#determining-versions-and-targets)
- [Making the changes in repo](#making-the-changes-in-repo)
- [FAQ](#faq)

## Determining versions and targets

1. [Determine what library](#determine-what-library) the API goes into.
2. [Determine the target framework](#determine-target-framework) for the library that will contain the API.
3. [Determine the version](#determine-library-version) for the library that will contain the API.

### Determine what library
- Propose a library for exposing it as part of the [API review process](http://aka.ms/apireview).
- Keep in mind the API might be exposed in a reference assembly that
doesn't match the identity of the implementation. There are many reasons for this but
the primary reason is to abstract the runtime assembly identities across
different platforms while sharing a common API surface and allowing us to refactor
the implementation without compat concerns in future releases.

### Determine target framework
`netstandard` or `netcoreapp` is the target framework version currently under development.

- If the library is [part of netstandard](#isnetstandard)
  - Your target framework should be `netstandard`
  - If it is a new API only available on .NET Core then it will be added to `netcoreapp`
- If the library is not part of netstandard
  - If package dependencies are changed then your target framework should be the minimum target framework that supports all your package dependencies.
  - If your package depends directly on runtime changes or library changes that ship with the runtime (i.e. System.Private.CoreLib) then your target framework should be `netstandard`.
  - When targeting `netstandardX` your new API must be supported by all target frameworks that map to that netstandard version (see [mapping table](https://github.com/dotnet/corefx/blob/master/Documentation/architecture/net-platform-standard.md#mapping-the-net-platform-standard-to-platforms)). If not bump the version to the minimum netstandard version that supports this API on all frameworks that map to that netstandard version.

### Determine library version
- If targeting netstandard
  - Ensure minor version of the assembly is bumped since last stable package release
- If targeting netcoreapp
  - No assembly version bump necessary

## Making the changes in repo

**If changing the library version**
  - Update the `AssemblyVersion` property in `<Library>\Directory.Build.props` (ex: [System.Runtime\Directory.Build.props](https://github.com/dotnet/corefx/blob/master/src/System.Runtime/Directory.Build.props#L4)) to the version determined above.

**If changing the target group**
- Update both the `Configurations` property in the library's csproj file and the `BuildConfigurations` property in the library's Configurations.props file.

**Update pkg**
 - If changing the target framework
    - Update `SupportedFramework` metadata on the ref ProjectReference to declare the set of concrete platforms you expect your library to support. (see [Specific platform mappings](https://github.com/dotnet/corefx/blob/master/Documentation/architecture/net-platform-standard.md#nuget)). Generally will be a combination of netcoreapp2.x, netfx46x, uap10.x, and/or `$(AllXamarinFrameworks)`.
  - If assembly or package version is updated the package index needs to be updated by running
    `dotnet msbuild <Library>/pkg/<Library>.pkgproj /t:UpdatePackageIndex`

**Update tests**
  - Set `TargetGroup` which will generally match the `TargetGroup` in the src library build configuration. (ex: [System.Runtime\tests\Configurations.props](https://github.com/dotnet/corefx/blob/master/src/System.Runtime/tests/Configurations.props#L3))
  - Add new test code following [conventions](https://github.com/dotnet/corefx/blob/master/Documentation/coding-guidelines/project-guidelines.md#code-file-naming-conventions) for new files to that are specific to the new target framework.
  - To run just the new test configuration run `dotnet msbuild <Library>.csproj /t:RebuildAndTest /p:TargetGroup=<TargetGroup>`

## FAQ
_**<a name="isnetstandard">Is your API part of netstandard?</a>**_

Use [apisof.net](https://apisof.net) to identify the support matrix of a specific API.

_**What is the difference between being part of netstandard and building against netstandard?**_

Things that are part of netstandard can only change when we release a new version of a platform
that supports the higher version of netstandard. Whereas things that build against netstandard and
ship in independent packages can be changed without an update to the platform that it is running on.
That gives more flexibility to add API to things that build against netstandard because it does not
require a platform update to consume.

_**How do I consume APIs from another package that aren't yet published?**_

If you are adding APIs across multiple packages at the same time. You can temporarily add a direct
ProjectReference from the ref\csproj to the ref\csproj, src\csproj to the ref\csproj, and/or tests\csproj to pkg\pkgproj. Once a new set of packages have been published these ProjectReferences should be removed.

_**What to do if you are moving types down into a lower contract?**_

If you are moving types down you need to version both contracts at the same time and temporarily use
project references across the projects. You also need to be sure to leave type-forwards in the places
where you removed types in order to maintain back-compat.
