# How to patch dotnet CLI with latest CoreFX

This document provides the steps necessary to consume a nightly build of CoreFX
and CoreCLR.

Please note that these steps aren't necessary for official builds -- these steps
are specficic to consuming nightlies and thus unsupported builds. Also note that
these steps are likely to change as we're simplifying this experience. Make
sure to consult this document often.

## Update framework and CLI

1. Add the CoreFX feed by editing
   `C:\Users\XXX\AppData\Roaming\NuGet\NuGet.Config` to add this line:
    ```
    <add key="CoreFX" value="https://dotnet.myget.org/F/dotnet-core/api/v3/index.json" />
    ```
	or, if you have `nuget.exe` in your path, this will work too:
    ```
	nuget sources Add "foo" https://dotnet.myget.org/F/dotnet-core/api/v3/index.json
    ```

2. Pick a package version from
   <https://dotnet.myget.org/feed/dotnet-core/package/nuget/Microsoft.NETCore.App>
   feed. Presumably you want the latest version.

3. Get CLI preview4+:
    1. Install CLI preview 4 SDK from here:
        - <https://github.com/dotnet/cli>
        - <https://dotnetcli.blob.core.windows.net/dotnet/Sdk/rel-1.0.0/dotnet-dev-win-x64.latest.exe>
    2. Install 1.0.3 SDK from here:
       - <https://go.microsoft.com/fwlink/?LinkID=836281>
       - Currently necessary for the CLI itself to run.
       - Eventually it will be chained-in by the SDK making this step
         unncessary.
       - The issue tracking this work is filed
         [here](https://github.com/dotnet/cli/issues/5194).

4. Verify that it works
    1. Create a new app with `dotnet new` - don't reuse an old one, it must
       match the SDK
    2. In your app, add a call to some recently added API, to prove this works.
    3. For example, this won't compile before .NET Core 1.2:
    ```
    System.Net.WebUtility.HtmlDecode("&amp;", Console.Out);
    ```
    4. Edit your `csproj` as explained below
    5. Run `dotnet restore`
    6. Run `dotnet run`

10. Optionally, `dotnet publish` to make a standalone exe

## Consuming the build

In order to consume the latest build, you'll need to update your `.csproj`
as follows:

1. Update `TargetFramework`, add `RuntimeIdentifier` as below (ideally
   `dotnet.exe` would infer your current architecture but it currently doesn't)
2. Update package reference to match version selected above, as below

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
