Tasks, and targets to support workload testing in `dotnet` repositories.

# Relevant msbuild properties:

- `$(InstallWorkloadForTesting)` - required
- `$(BuiltNuGetsDir)` - required
- `$(DotNetInstallArgumentsForWorkloadsTesting)` - required

- `$(TestUsingWorkloads)` - optional
- `$(SkipTempDirectoryCleanup)` - optional
- `$(VersionBandForManifestPackages)` - optional
- `$(ExtraWorkloadInstallCommandArguments)` - optional
- `$(WorkloadInstallCommandOutputImportance)` - optional

- `$(TemplateNuGetConfigPathForWorkloadTesting)` - optional

## `$(PackageSourceNameForBuiltPackages)` - optional

`<add key="<$sourceName>" value="file:///..." />`

Defaults to `nuget-local`.

## `$(NuGetConfigPackageSourceMappingsForWorkloadTesting)` - optional

For a value of `*Aspire*;Foo*`, a package source mapping will be added to the local nuget source
added for built nugets:

```xml
  <packageSourceMapping>
    <packageSource key="nuget-local">
      <package pattern="*Aspire*" />
      <package pattern="Foo*" />
    </packageSource>
    ...
```

# items

- `@(DefaultPropertiesForNuGetBuild)`
