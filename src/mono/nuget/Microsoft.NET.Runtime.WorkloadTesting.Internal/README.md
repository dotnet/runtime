Tasks, and targets to support workload testing in `dotnet` repositories.

# Relevant msbuild properties:

- `$(InstallWorkloadForTesting)` - required
- `$(BuiltNuGetsDir)` - required
- `$(DotNetInstallArgumentsForWorkloadsTesting)` - required
- `$(TestUsingWorkloads)` - optional
- `$(SkipTempDirectoryCleanup)` - optional
- `$(VersionBandForManifestPackages)` - optional
- `$(ExtraWorkloadInstallCommandArguments)` - optional

## items

- `@(DefaultPropertiesForNuGetBuild)`
