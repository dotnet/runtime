# MonoAOTCompiler MSBuild Task NuPkg
The `MonoAOTCompiler` MSBuild task is also useful outside the context of `dotnet/runtime`. The task is made available through a NuGet Package containing the `MonoAOTCompiler.dll` assembly produced from building `MonoAOTCompiler.csproj`. To use the task in a project, reference the NuGet package, with the appropriate nuget source.

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
    <PackageReference Include="Microsoft.NET.Runtime.MonoAOTCompiler.Task" Version="<desired-dotnet-6-sdk-version>" />
</ItemGroup>

<!-- Use the MonoAOTCompiler task in a target -->
<Target>
    <MonoAOTCompiler 
        CompilerBinaryPath="$(CompilerBinaryPath)"
        Assemblies="@(Assemblies)"
        <!-- Other parameters -->
        >
        <Output TaskParameter="CompiledAssemblies" ItemName="CompiledAssemblies" />
    </MonoAOTCompiler>
</Target>
```
