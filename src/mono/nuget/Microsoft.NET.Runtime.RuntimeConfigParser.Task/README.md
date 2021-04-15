# RuntimeConfigParser MSBuild Task NuPkg
The `RuntimeConfigParser` MSBuild task is also useful outside the context of `dotnet/runtime`. The task is made available through a NuGet Package containing the `RuntimeConfigParser.dll` assembly produced from building `RuntimeConfigParser.csproj`. To use the task in a project, reference the NuGet package, with the appropriate nuget source.

## NuGet.config
```
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <add key="dotnet6" value="https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet6/nuget/v3/index.json" />
  </packageSources>
</configuration>
```

## In the project file
```
<!-- Import the NuGet package into the project -->
<ItemGroup>
    <PackageReference Include="Microsoft.NET.Runtime.RuntimeConfigParser.Task" Version="<desired-dotnet-6-sdk-version>" />
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
