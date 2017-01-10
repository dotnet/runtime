# How to patch dotnet CLI with latest CoreFX

This document provides the steps necessary to consume a nightly build of CoreFX
and CoreCLR.

Please note that these steps aren't necessary for official builds -- these steps
are specific to consuming nightlies and thus unsupported builds. Also note that
these steps are likely to change as we're simplifying this experience. Make
sure to consult this document often.

## Install prerequisites

1. Install CLI preview 4 SDK
    - [Win 64-bit Latest](https://dotnetcli.blob.core.windows.net/dotnet/Sdk/rel-1.0.0/dotnet-dev-win-x64.latest.exe)
    - [macOS 64-bit Latest](https://dotnetcli.blob.core.windows.net/dotnet/Sdk/rel-1.0.0/dotnet-dev-osx-x64.latest.dmg)
    - [Others](https://github.com/dotnet/cli)
2. Install 1.0.3 SDK from here:
    - [Win 64-bit](https://go.microsoft.com/fwlink/?LinkID=836281)
    - [macOS 64-bit](https://go.microsoft.com/fwlink/?LinkID=836275)
    - [Others](https://www.microsoft.com/net/download/core?v=lts)
    - Currently necessary for the CLI itself to run.
    - Eventually it will be chained-in by the SDK making this step
      unncessary.
    - The issue tracking this work is filed
      [here](https://github.com/dotnet/cli/issues/5194).

## Setup the project

1. Create a new project
    - Creat a new folder for your app
    - Create projec file by running `dotnet new`
2. Add the CoreFX MyGet feed to your NuGet configuration.
    - You can do this globally but we recommend not doing this as this might
      affect other projects on your machine and you probably don't want that.
    - Instead, add a `nuget.config` that is local to your project. You can
      just put it next to the `.csproj` file.
      See the [NuGet docs](https://docs.nuget.org/ndocs/consume-packages/configuring-nuget-behavior)
      for details.
      ```xml
      <configuration>
        <packageSources>
          <add key="CoreFX" value="https://dotnet.myget.org/F/dotnet-core/api/v3/index.json" />
        </packageSources>
      </configuration>
      ```
3. Select the nightly build from our feed
    - <https://dotnet.myget.org/feed/dotnet-core/package/nuget/Microsoft.NETCore.App>
    - Presumably you want the latest version.

In order to consume the latest build, you'll need to update your `.csproj`
as follows:

1. Update `TargetFramework`, add `RuntimeIdentifier` as below (ideally
   `dotnet.exe` would infer your current architecture but it currently doesn't)
2. Update package reference to match version selected above, as below

*Note:* We're currently still using `netcoreapp1.2`. Eventually this will be
renamed to `netcoreapp2.0`.

```xml
<Project Sdk="Microsoft.NET.Sdk" ToolsVersion="15.0">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>netcoreapp1.2</TargetFramework>
    <RuntimeIdentifier>win10-x64</RuntimeIdentifier>
  </PropertyGroup>

  <!-- Make sure to use Update, not Include! -->
  <ItemGroup>
    <PackageReference Update="Microsoft.NETCore.App" Version="1.2.0-beta-001285-00" />
  </ItemGroup>

</Project>
```

Restore packages so that you're ready to play:

```
$ dotnet restore
```

## Consume the new build

Edit your `Program.cs` to consume the new APIs, for exampe:

```CSharp
using System;
using System.Net;

class Program
{
    static void Main(string[] args)
    {
        WebUtility.HtmlDecode("&amp;", Console.Out);
        Console.WriteLine();
        Console.WriteLine("Hello World!");
    }
}
```

Run the bits:

```
$ dotnet run
```

Rinse and repeat!
