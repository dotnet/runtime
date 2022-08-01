# Compiling with Native AOT

Please consult [documentation](https://docs.microsoft.com/dotnet/core/deploying/native-aot) for instructions how to compile and publish application.

The rest of this document covers advanced topics only.


## Using daily builds

For using daily builds, you need to make sure the `nuget.config` file for your project contains the following package sources under the `<packageSources>` element:
```xml
<add key="dotnet7" value="https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet7/nuget/v3/index.json" />
<add key="nuget" value="https://api.nuget.org/v3/index.json" />
```

If your project has no `nuget.config` file, it may be created by running
```bash
> dotnet new nugetconfig
```

from the project's root directory. New package sources must be added after the `<clear />` element if you decide to keep it.

Once you have added the package sources, add a reference to the ILCompiler package either by running
```bash
> dotnet add package Microsoft.DotNet.ILCompiler -v 7.0.0-*
```

or by adding the following element to the project file:
```xml
  <ItemGroup>
    <PackageReference Include="Microsoft.DotNet.ILCompiler" Version="7.0.0-*" />
  </ItemGroup>
```

## Cross-architecture compilation

Native AOT toolchain allows targeting ARM64 on an x64 host and vice versa for both Windows and Linux. Cross-OS compilation, such as targeting Linux on a Windows host, is not supported. To target win-arm64 on a Windows x64 host, in addition to the `Microsoft.DotNet.ILCompiler` package reference, also add the `runtime.win-x64.Microsoft.DotNet.ILCompiler` package reference to get the x64-hosted compiler:
```xml
<PackageReference Include="Microsoft.DotNet.ILCompiler; runtime.win-x64.Microsoft.DotNet.ILCompiler" Version="7.0.0-preview.2.22103.2" />
```

Note that it is important to use _the same version_ for both packages to avoid potential hard-to-debug issues (use the latest version from the [dotnet7](https://dev.azure.com/dnceng/public/_artifacts/feed/dotnet7/NuGet/Microsoft.DotNet.ILCompiler/7.0.0-preview.2.22103.2/versions)). After adding the package reference, you may publish for win-arm64 as usual:
```bash
> dotnet publish -r win-arm64 -c Release
```

Similarly, to target linux-arm64 on a Linux x64 host, in addition to the `Microsoft.DotNet.ILCompiler` package reference, also add the `runtime.linux-x64.Microsoft.DotNet.ILCompiler` package reference to get the x64-hosted compiler:
```xml
<PackageReference Include="Microsoft.DotNet.ILCompiler; runtime.linux-x64.Microsoft.DotNet.ILCompiler" Version="7.0.0-preview.2.22103.2" />
```

You also need to specify the sysroot directory for Clang using the `SysRoot` property. For example, assuming you are using one of ARM64-targeting [Docker images](../../../../docs/workflow/building/coreclr/linux-instructions.md#Docker-Images) employed for cross-compilation by this repo, you may publish for linux-arm64 with the following command:
```bash
> dotnet publish -r linux-arm64 -c Release -p:CppCompilerAndLinker=clang-9 -p:SysRoot=/crossrootfs/arm64
```

You may also follow [cross-building instructions](../../../../docs/workflow/building/coreclr/cross-building.md) to create your own sysroot directory.
