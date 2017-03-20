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
`<latest>` is the target framework version currently under development. Currently netcoreapp1.1 and netstandard1.7 (note netstandard1.7 is a placeholder for netstandard2.0).

- If the library is [part of netstandard](#isnetstandard)
  - Your target framework should be `netstandard<latest>`
  - If it is a new API only available on .NET Core then it will be added to `netcoreapp<latest>`
- If the library is not part of netstandard
  - If package dependencies are changed then your target framework should be the minimum target framework that supports all your package dependencies.
  - If your package depends directly on runtime changes or library changes that ship with the runtime
    (i.e. System.Private.CoreLib) then your target framework should be `netstandard<latest>` (hint: usually indicated by needing the latest Microsoft.TargetingPack.Private.* package)
  - If your package depends on changes in a native shim package then your target framework should be
    `netstandard<latest>`. (ex: something under https://github.com/dotnet/corefx/tree/master/src/Native)
  - When targeting `netstandardX` your new API must be supported by all target frameworks that
    map to that netstandard version (see [mapping table](https://github.com/dotnet/corefx/blob/master/Documentation/architecture/net-platform-standard.md#mapping-the-net-platform-standard-to-platforms)). If not bump the version to the minimum netstandard version that supports this API on all frameworks that map to that netstandard version.
- If the library is not part of netstandard and not building against netstandard
  - All the rules for a library that is not part of netstandard apply but instead of having a target framework of
    `netstandard<latest>` it will have a target framework of `<framework><latest>`. Example `net<latest>` for desktop or `netcoreapp<latest>` for .NET Core.
- It is not uncommon for a library to target the latest versions of multiple frameworks when adding new APIs (ex: https://github.com/dotnet/corefx/blob/master/src/System.Runtime/ref/System.Runtime.builds)

### Determine library version
- If targeting netstandard
  - Ensure minor version of the assembly is bumped since last stable package release
- If targeting netcoreapp
  - No assembly version bump necessary

## Making the changes in repo

**If changing the library version**
  - Update the `AssemblyVersion` property in `<Library>\dir.props` (ex: [System.Runtime\dir.props](https://github.com/dotnet/corefx/blob/master/src/System.Runtime/dir.props#L4)) to the version determined above.

**Update ref**
  - If changing target framework
    - Update the `NugetTargetMoniker` property in `<Library>\ref\<Library>.csproj` (ex: [ref\System.Runtime.csproj](https://github.com/dotnet/corefx/blob/master/src/System.Runtime/ref/System.Runtime.csproj#L8))
    - Update frameworks section in `<Library>\ref\project.json` to add a new target framework or replace an existing one (ex: [System.Runtime\ref\project.json](https://github.com/dotnet/corefx/blob/master/src/System.Runtime/ref/project.json#L4))
  - Add your API to `<Library>\ref\<Library>.cs`. Use GenAPI to normalize the file.
  - If adding an additional target framework for the ref (ex: adding a .NET Core specific API) you may need
  to add a `<Library>\ref\<Library>.builds` for the new configuration (ex: [ref/System.Runtime.Extensions.builds](https://github.com/dotnet/corefx/blob/master/src/System.Runtime.Extensions/ref/System.Runtime.Extensions.builds#L6)).

**Update src**
  - If changing target framework
    - Update the `NugetTargetMoniker` property in `<Library>\src\<Library>.csproj` (ex: [src\System.Runtime.csproj](https://github.com/dotnet/corefx/blob/master/src/System.Runtime/src/System.Runtime.csproj#L12))
    - Update frameworks section in `<Library>\src\project.json` to add a new target framework or replace an existing one (ex: [System.Runtime\src\project.json](https://github.com/dotnet/corefx/blob/master/src/System.Runtime/src/project.json#L3))
    - Update build configurations in `<Library>\src\<Library>.builds` if necessary (see [Project configuration conventions](https://github.com/dotnet/corefx/blob/master/Documentation/coding-guidelines/project-guidelines.md#project-configuration-conventions))
      - The default build configuration(i.e. TargetGroup excluded) should build the latest version
      - If you need a live build of the older versions they need to be added as a new configuration
        - Typically only done for libraries not in netstandard that have larger amounts of code
          that we want to update outside of servicing events. For any configurations that are removed
          we will still harvest the old assets from the last stable package an reship them again.
  - Make your code changes.

**Update pkg**
 - If changing the target framework
    - Update `SupportedFramework` metadata on the ref ProjectReference to declare the set of
      concrete of platforms you expect your library to support. (see [Specific platform mappings](https://github.com/dotnet/corefx/blob/master/Documentation/architecture/net-platform-standard.md#nuget)). Generally
      will be a combination of netcoreapp1.x, netfx46x, uap10.x, and/or `$(AllXamarinFrameworks)`.
    - If package is not split (i.e. only one pkgproj, ex [pkg\System.Diagnostics.Process.pkgproj](https://github.com/dotnet/corefx/blob/master/src/System.Diagnostics.Process/pkg/System.Diagnostics.Process.pkgproj)) then it should already have a `ProjectReference` the `<Library>\src\<Library>.builds` file and so there is no additional changes needed in the pkgproj.
    - If package is RID split (i.e. has multple pkgprojs, ex: [pkg\any\System.Runtime.pkgproj](https://github.com/dotnet/corefx/blob/master/src/System.Runtime/pkg/any/System.Runtime.pkgproj) and [pkg\aot\System.Runtime](https://github.com/dotnet/corefx/blob/master/src/System.Runtime/pkg/aot/System.Runtime.pkgproj))
      - Update the ProjectReferences in the respective pkgproj's to align with the build configurations in the
      `<Library>\src\<Library>.builds` file. This may entail adding new references or removing references according to whatever changes were made to the .builds file.
  - If assembly or package version is updated the package index needs to be updated by running
    `msbuild <Library>/pkg/<Library>.pkgproj /t:UpdatePackageIndex`

**Update tests**
  - If changing target framework
    - Update the `NugetTargetMoniker` property in `<Library>\tests\<Library>.csproj` (ex: [tests\System.Runtime.csproj](https://github.com/dotnet/corefx/blob/master/src/System.Runtime/tests/System.Runtime.Tests.csproj#L13)). Add a `'$(TargetGroup)' == ''` condition if missing.
    - Update frameworks section in `<Library>\tests\project.json` to add a new target framework or replace an existing one (ex: [System.Runtime\tests\project.json](https://github.com/dotnet/corefx/blob/master/src/System.Runtime/tests/project.json#L30))
    - Add new build configuration in `<Library>\tests\<Library>.builds` (ex: [tests\System.Runtime.builds](https://github.com/dotnet/corefx/blob/master/src/System.Runtime/tests/System.Runtime.Tests.builds#L6))
      - Set `TargetGroup` which will generally match the `TargetGroup` in the src library build configruation. (ex: [tests\System.Runtime.builds](https://github.com/dotnet/corefx/blob/master/src/System.Runtime/tests/System.Runtime.Tests.builds#L7))
      - Set `TestTFMs` metadata to a list of target frameworks to run on, will generally match the `SupportedFramework` metadata in the pkgproj (ex: [tests\System.Runtime.builds](https://github.com/dotnet/corefx/blob/master/src/System.Runtime/tests/System.Runtime.Tests.builds#L8))
      - If `TestTFMs` is empty it defaults to [netcoreapp1.0](https://github.com/dotnet/corefx/commit/57af41ef1439ad2e443e42d03d55d41613e4c02e#diff-cd0fc5e0bad8102e1a45aa7575bdd102R155)
  - Add new test code following [conventions](https://github.com/dotnet/corefx/blob/master/Documentation/coding-guidelines/project-guidelines.md#code-file-naming-conventions) for new files to that are specific to the new target framework.
  - To run just the new test configuration run `msbuild <Library>.csproj /t:RebuildAndTest /p:TargetGroup=<TargetGroup>`

## FAQ
_**<a name="isnetstandard">Is your API part of netstandard?</a>**_

- For netstandard2.0 refer to https://github.com/dotnet/standard/tree/master/netstandard/ref, and if any Type is included in any of the *.cs files then it is part of netstandard.
- For earlier netstandard versions refer to list at https://github.com/dotnet/corefx/blob/master/pkg/NETStandard.Library/NETStandard.Library.packages.targets. If the assembly is part of that list then it is in netstandard.

_**What is the difference between being part of netstandard and building against netstandard?**_

Things that are part of netstandard can only change when we release a new version of a platform
that supports the higher version of netstandard. Whereas things that build against netstandard and
ship in independent packages can be changed without an update to the platform that it is running on.
That gives more flexibility to add API to things that build against netstandard because it does not
require a platform update to consume.

_**What changes require me to update my target framework in my library package?**_

*Target framework of dependencies*
  - If any changes (ref or src) require a higher target framework of one of the dependencies then your library package target framework must be increased to match the new minimum version that supports all package dependencies.

*Independently shipped dependencies*
  - If any changes (ref or src) requires a newer runtime/platform change that ships independently(this should generally only apply to libraries that are part of netstandard). Some example dependencies include:
    - Runtime (aka RID) split package dependencies
      - Dependencies on runtime changes
      - Dependencies on runtime libraries (i.e. in Microsoft.TargetingPack.Private.* packages)
      - Dependencies on new native shim changes
    - .NET Native Shared Library

_**How do I consume APIs from another package that aren't yet published?**_

If you are adding APIs across multiple packages at the same time. You can temporarily add a direct
ProjectReference from the ref\csproj to the ref\csproj, src\csproj to the ref\csproj, and/or tests\csproj to pkg\pkgproj. Once a new set of packages have been published these ProjectReferences should be removed.

_**What to do if you are moving types down into a lower contract?**_

If you are moving types down you need to version both contracts at the same time and temporarily use
project references across the projects. You also need to be sure to leave type-forwards in the places
where you removed types in order to maintain back-compat.

### Potential clean-up work that can be done
- Remove old build configurations - The older build configurations will automatically be harvested from
the last stable packages and thus can be removed.
- Remove import statements - If not referencing any pre-netstandard stable packages the [imports of dotnet5.x](https://github.com/dotnet/corefx/blob/master/src/System.Diagnostics.Process/src/project.json#L28) are no longer needed and can be removed. We should also remove any dead target frameworks sections.
- Remove all non-conditionsed `<AssemblyVersion>` properties from csproj's as it should be defined in library\dir.props.
