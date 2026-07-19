# Microsoft.DotNet.HotReload.Utils.Generator.BuildTool #

Generate deltas as part of an MSBuild project.

## How to use it ##

Starting with an existing SDK-style project, add:

```xml
  <ItemGroup>
    <PackageReference Include="Microsoft.DotNet.HotReload.Utils.Generator.BuildTool" Version="..." />
  </ItemGroup>

  <PropertyGroup>
    <DeltaScript>deltascript.json</DeltaScript>
  </PropertyGroup>
```

Where the `deltascript.json` file contains the changes to be applied:

```json
{"changes":
    [
        {"document": "relativePath/to/file.cs", "update": "relativePath/to/file_v1.cs"},
        {"document": "file2.cs", "update": "file2_v2.cs"},
        {"document": "relativePath/to/file.cs", "update": "relativePath/to/file_v3.cs"}
    ]
}
```

The tool will run as part of the build after the `Build` target and generate `.dmeta`, `.dil` and `.dpdb` files in `$(OutputPath)`.
