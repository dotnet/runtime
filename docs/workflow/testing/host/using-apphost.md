# Using a local build of `apphost`

When building a .NET application, [`apphost`](../../../design/features/host-components.md#entry-point-hosts) is used as the executable for the application. It is renamed to match the application and updated to be associated with the application's managed `.dll`. The .NET SDK looks for the `apphost` to use by looking for `Microsoft.NETCore.App.Host` packages installed alongside under `<dotnet_root>/packs` with a matching OS, architecture, and version. If no match is found, it downloads the matching NuGet package.

To make the SDK use a specific `apphost` when building a project, set the [`AppHostSourcePath` property](https://github.com/dotnet/sdk/blob/f106bca2c28aeb4de8cafa8ff818bd8613908964/src/Tasks/Microsoft.NET.Build.Tasks/targets/Microsoft.NET.Sdk.FrameworkReferenceResolution.targets#L295) to the full path to your local `apphost` binary - for example, `<repo_root>/artifacts/bin/<os>-<arch>.<configuration>/corehost/apphost[.exe]`.

```xml
<PropertyGroup>
  <AppHostSourcePath>[full_path_to_apphost]</AppHostSourcePath>
</PropertyGroup>
```

For single-file, set the [`SingleFileHostSourcePath` property](https://github.com/dotnet/sdk/blob/f106bca2c28aeb4de8cafa8ff818bd8613908964/src/Tasks/Microsoft.NET.Build.Tasks/targets/Microsoft.NET.Sdk.FrameworkReferenceResolution.targets#L305) to the full path to your local `singlefilehost` binary - for example, `<repo_root>/artifacts/bin/<os>-<arch>.<configuration>/corehost/singlefilehost[.exe]`

```xml
<PropertyGroup>
  <PublishSingleFile>true</PublishSingleFile>
  <SingleFileHostSourcePath>[full_path_to_singlefilehost]</SingleFileHostSourcePath>
</PropertyGroup>
```

Building and publishing your project should now use the `apphost`/`singlefilehost` that you have specified.

Alternatives to this method include copying the desired apphost to the appropriate `<dotnet_root>/packs` and NuGet cache directories or building the NuGet packages locally and configuring the application to use them via a NuGet.config and the `KnownAppHostPack` item.

## Pointing at a local .NET root

For a [framework-dependent application](https://docs.microsoft.com/dotnet/core/deploying/#publish-framework-dependent), you can set the `DOTNET_ROOT` environment variable to point at a local .NET layout.

The [libraries tests](../libraries/testing.md) construct and use such a layout based on your local runtime and libraries build as part of the `libs.pretest` subset. To use that layout, set `DOTNET_ROOT=<repo_root>/artifacts/bin/testhost/net7.0-<os>-<configuration>-<arch>`. Note that the host components (`hostfxr`, `hostpolicy`) in that layout are not from the local build.