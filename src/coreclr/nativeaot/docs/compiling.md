# Compiling with Native AOT

This document explains how to compile and publish your project using Native AOT toolchain. First, please _ensure that [pre-requisites](prerequisites.md) are installed_. If you are starting a new project, you may find the [HelloWorld sample](../../samples/HelloWorld/README.md) directions useful.

## Add ILCompiler package reference

To use Native AOT with your project, you need to add a reference to the ILCompiler NuGet package containing the Native AOT compiler and runtime. Make sure the `nuget.config` file for your project contains the following package sources under the `<packageSources>` element:
```xml
<add key="dotnet-experimental" value="https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet-experimental/nuget/v3/index.json" />
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

## Compile and publish your app

Use the `dotnet publish` command to compile and publish your app:
```bash
> dotnet publish -r <RID> -c <Configuration>
```

where `<Configuration>` is your project configuration (such as Debug or Release) and `<RID>` is the runtime identifier reflecting your host OS and architecture (one of win-x64, linux-x64, osx-x64). For example, to publish the Release build of your app for Windows x64, run the following command:
```bash
> dotnet publish -r win-x64 -c Release
```

If the compilation succeeds, the native executable will be placed under the `bin/<Configuration>/net5.0/<RID>/publish/` path relative to your project's root directory.

## Cross-architecture compilation

Native AOT toolchain allows targeting ARM64 on an x64 host and vice versa for both Windows and Linux. Cross-OS compilation, such as targeting Linux on a Windows host, is not supported. To target win-arm64 on a Windows x64 host, in addition to the `Microsoft.DotNet.ILCompiler` package reference, also add the `runtime.win-x64.Microsoft.DotNet.ILCompiler` package reference to get the x64-hosted compiler:
```xml
<PackageReference Include="Microsoft.DotNet.ILCompiler; runtime.win-x64.Microsoft.DotNet.ILCompiler" Version="7.0.0-alpha.1.21423.2" />
```

Note that it is important to use _the same version_ for both packages to avoid potential hard-to-debug issues (use the latest version from the [dotnet-experimental feed](https://dev.azure.com/dnceng/public/_packaging?_a=package&feed=dotnet-experimental&package=Microsoft.DotNet.ILCompiler&protocolType=NuGet)). After adding the package reference, you may publish for win-arm64 as usual:
```bash
> dotnet publish -r win-arm64 -c Release
```

Similarly, to target linux-arm64 on a Linux x64 host, in addition to the `Microsoft.DotNet.ILCompiler` package reference, also add the `runtime.linux-x64.Microsoft.DotNet.ILCompiler` package reference to get the x64-hosted compiler:
```xml
<PackageReference Include="Microsoft.DotNet.ILCompiler; runtime.linux-x64.Microsoft.DotNet.ILCompiler" Version="7.0.0-alpha.1.21423.2" />
```

You also need to specify the sysroot directory for Clang using the `SysRoot` property. For example, assuming you are using one of ARM64-targeting [Docker images](../workflow/building/coreclr/linux-instructions.md#Docker-Images) employed for cross-compilation by this repo, you may publish for linux-arm64 with the following command:
```bash
> dotnet publish -r linux-arm64 -c Release -p:CppCompilerAndLinker=clang-9 -p:SysRoot=/crossrootfs/arm64
```

You may also follow [cross-building instructions](../workflow/building/coreclr/cross-building.md) to create your own sysroot directory.
