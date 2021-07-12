# Mono Runtime Host support targets

This Sdk provides additional tasks and targets for workloads hosting the MonoVM .NET runtime.

## component-manifest.targets

See https://github.com/dotnet/runtime/blob/main/docs/design/mono/components.md

## RuntimeConfigParserTask
The `RuntimeConfigParserTask` task converts a json `runtimeconfig.json` to a binary blob for MonoVM's `monovm_runtimeconfig_initialize` API.
To use the task in a project, reference the NuGet package, with the appropriate nuget source.

### NuGet.config
```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <add key="dotnet6" value="https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet6/nuget/v3/index.json" />
  </packageSources>
</configuration>
```

### In the project file
```xml
<!-- Import the NuGet package into the project -->
<ItemGroup>
    <PackageReference Include="Microsoft.NET.Runtime.MonoTargets.Sdk" Version="<desired-dotnet-6-sdk-version>" />
</ItemGroup>

<!-- Use the RuntimeConfigParser task in a target -->
<Target>
    <RuntimeConfigParserTask
        RuntimeConfigFile="$(Path_to_runtimeconfig.json_file)"
        OutputFile="$(Path_to_generated_binary_file)"
        RuntimeConfigReservedProperties="@(runtime_properties_reserved_by_host)">
    </RuntimeConfigParserTask>
</Target>
```
