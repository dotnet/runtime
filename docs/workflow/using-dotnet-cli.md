
# Using your .NET Runtime build with .NET SDK

This walkthrough explains how to run your own app against your local build using only the .NET SDK.

For other walkthroughs see:

- [Using Your Build - Update from raw build output](./testing/using-your-build.md)
- [Using CoreRun To Run .NET Application](./testing/using-corerun.md)
- [Dogfooding .NET SDK](https://github.com/dotnet/runtime/blob/master/docs/project/dogfooding.md).

## Prerequisites

All paths in examples below are Windows-style but the procedure is otherwise exactly the same on Unix.

1. Successfully built this repository including the shared framework package and thus have files of the form shown below. From now on we call this folder your NuGet package folder.

```
    <your-repo-root>\artifacts\packages\<configuration>\Shipping\
```

If you don't have this folder, you may have built binaries but not packages. Try building from the root with a command like `build.cmd clr+libs+host+packs -rc release`.

2. Acquired the latest nightly .NET SDK from [here](https://github.com/dotnet/installer) and added its root folder to your [path](requirements/windows-requirements.md#adding-to-the-default-path-variable)

## First Run

### 1. Create new folder for the app

`mkdir helloWorld`

From now on all instructions relate to this folder as "app folder".

### 2. Create NuGet.Config file

The build script creates NuGet packages and puts them to `artifacts\packages\<configuration>\Shipping\`. .NET SDK has no idea about its existence and we need to tell it where to search for the packages.

Please run `dotnet new nugetconfig` in the app folder and replace the created `NuGet.Config` file content with:

```xml
<configuration>
  <config>
    <!-- CHANGE THIS PATH BELOW to any empty folder. NuGet will cache things here, and that's convenient because you can delete it to reset things -->
    <add key="globalPackagesFolder" value="c:\packages" />
  </config>
  <packageSources>
    <!--To inherit the global NuGet package sources remove the <clear/> line below -->
    <clear />
    <!-- This feed is for any packages you didn't build. See https://github.com/dotnet/installer#installers-and-binaries -->
    <add key="dotnet6" value="https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet6/nuget/v3/index.json" />
    <!-- CHANGE THIS PATH BELOW to your local output path -->
    <add key="local runtime" value="C:\runtime\artifacts\packages\Release\Shipping\" />
  </packageSources>
</configuration>
```

### 3. Create and update the Project file

Please run `dotnet new console` in the app folder and update the created `.csproj` file:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net6.0</TargetFramework>
    <RuntimeIdentifier>win-x64</RuntimeIdentifier>
  </PropertyGroup>

  <ItemGroup>
    <FrameworkReference Update="Microsoft.NETCore.App" RuntimeFrameworkVersion="6.0.0-dev" />
  </ItemGroup>

</Project>
```

**You have to set the correct values for `RuntimeIdentifier` (RI) and `RuntimeFrameworkVersion`.**

You can generally figure that out by looking at the packages you found in your output.
In our example you will see there is a package with the name `Microsoft.NETCore.App.Runtime.win-x64.6.0.0-dev.nupkg`

```
Microsoft.NETCore.App.Runtime.win-x64.6.0.0-dev.nupkg
                              ^-RI--^ ^version^
```

### 4. Change Program.cs

To make sure that you run against your local build of this repo please change your `Main` method in `Program.cs` file to:

```cs
static void Main(string[] args)
{
    var coreAssemblyInfo = System.Diagnostics.FileVersionInfo.GetVersionInfo(typeof(object).Assembly.Location);
    Console.WriteLine($"Hello World from .NET {coreAssemblyInfo.ProductVersion}");
    Console.WriteLine($"The location is {typeof(object).Assembly.Location}");
}
```

### 5. Publish

Now is the time to publish. The publish step will trigger restore and build. You can iterate on build by calling `dotnet build` as
needed.

```bat
dotnet publish
```

Make sure that restoring done by `dotnet publish` installed the explicit version of the Runtime that you have specified:

```
c:\runtime\helloworld>dotnet publish
Microsoft (R) Build Engine version 16.7.0-preview-20360-03+188921e2f for .NET
Copyright (C) Microsoft Corporation. All rights reserved.

  Determining projects to restore...
  Restored c:\runtime\helloworld\helloworld.csproj (in 114 ms).
  You are using a preview version of .NET. See: https://aka.ms/dotnet-core-preview
  helloworld -> c:\runtime\helloworld\bin\Debug\net6.0\win-x64\helloworld.dll
  helloworld -> c:\runtime\helloworld\bin\Debug\net6.0\win-x64\publish\
```

#### Troubleshooting Publish

If you see something like the message below it means that it has failed to restore your local runtime packages. Check your `NuGet.config` file and paths used in it.

```
c:\runtime\helloworld>dotnet publish
Microsoft (R) Build Engine version 16.7.0-preview-20360-03+188921e2f for .NET
Copyright (C) Microsoft Corporation. All rights reserved.

  Determining projects to restore...
c:\runtime\helloworld\helloworld.csproj : error NU1102: Unable to find package Microsoft.NETCore.App.Runtime.win-x64 with version (= 6.0.0-does-not-exist)
c:\runtime\helloworld\helloworld.csproj : error NU1102:   - Found 25 version(s) in nuget [ Nearest version: 6.0.0-preview.1.20120.5 ]
c:\runtime\helloworld\helloworld.csproj : error NU1102:   - Found 1 version(s) in local runtime [ Nearest version: 6.0.0-dev ]
c:\runtime\helloworld\helloworld.csproj : error NU1102: Unable to find package Microsoft.NETCore.App.Host.win-x64 with version (= 6.0.0-does-not-exist)
c:\runtime\helloworld\helloworld.csproj : error NU1102:   - Found 27 version(s) in nuget [ Nearest version: 6.0.0-preview.1.20120.5 ]
c:\runtime\helloworld\helloworld.csproj : error NU1102:   - Found 1 version(s) in local runtime [ Nearest version: 6.0.0-dev ]
  Failed to restore c:\runtime\helloworld\helloworld.csproj (in 519 ms).
```

If you see error messages like these below, it means it has failed to restore other packages you need that you didn't build. Check your `NuGet.config` file includes the other feed.
```
c:\runtime\helloworld\helloworld.csproj : error NU1101: Unable to find package Microsoft.WindowsDesktop.App.Runtime.win-x64. No packages exist with this id in source(s): local runtime
c:\runtime\helloworld\helloworld.csproj : error NU1101: Unable to find package Microsoft.AspNetCore.App.Runtime.win-x64. No packages exist with this id in source(s): local runtime
```

If you see error messages like this, it means you do not have a new enough SDK. Please visit https://github.com/dotnet/installer#installers-and-binaries and install a newer SDK.

```
C:\Program Files\dotnet\sdk\5.0.100\Sdks\Microsoft.NET.Sdk\targets\Microsoft.NET.TargetFrameworkInference.targets(141,5): error NETSDK1045: The current .NET SDK does not support targeting .NET Core 6.0.  Either target .NET Core 5.0 or lower, or use a version of the .NET SDK that supports .NET Core 6.0. [c:\runtime\helloworld\helloworld.csproj]
```

### 6. Run the app

After you publish you will find all the binaries needed to run your application under `bin\Debug\net6.0\win-x64\publish\`.
To run the application simply run the EXE that is in this publish directory (it is the name of the app, or specified in the project file).

```
.\bin\Debug\net6.0\win-x64\publish\HelloWorld.exe
```

Running the app should tell you the version and where the location of System.Private.CoreLib in the publish directory:

```
Hello World from .NET 6.0.0-dev
The location is c:\runtime\helloworld\bin\Debug\net6.0\win-x64\publish\System.Private.CoreLib.dll
```

**Congratulations! You have just run your first app against your local build of this repo**

## How to then consume updated packages

Once you have successfully consumed a package, you probably want to make changes, update the package, and have your app consume it again.

#### 1. Update BuildNumberMinor Environment Variable

For NuGet to get the updated package it needs to have a distinct version number. 
When you modify the source and create a new NuGet package you must give it a new version number and use that in your
application's project. Otherwise the dotnet.exe tool will assume that the existing version is fine and you
won't get the updated bits. We use the minor build number for this. By default it is 0, but you can
give it a value by setting the BuildNumberMinor environment variable.
```bat
    set BuildNumberMinor=3
```
before packaging. You should see this number show up in the version number (e.g. 3.0.0-preview1-26210-03).

As an alternative you can delete the existing copy of the package from the Nuget cache. For example on
windows (on Linux substitute ~/ for %HOMEPATH%) you could delete
```bat
     %HOMEPATH%\.nuget\packages\runtime.win-x64.Microsoft.NETCore.Runtime.CoreCLR\6.0.0-preview1-26210-0
```
which should make things work (but is fragile, confirm file timestamps that you are getting the version you expect)

#### 2. Get the Version number of the CoreCLR package you built.

Get this by simply listing the name of the `runtime.win-x64.Microsoft.NETCore.Runtime.CoreCLR` you built.

```bat
    dir artifacts\bin\coreclr\Product\windows.x64.Release\.nuget\pkg
```

and you will get name of the which looks something like this

```
    runtime.win-x64.Microsoft.NETCore.Runtime.CoreCLR.6.0.0-preview1-26210-3.nupkg
```

This gets us the version number, in the above case it is 6.0.0-preview1-26210-3. We will
use this in the next step.

#### 3. Update the references to your runtime package

Edit your `.csproj` file and change the versions:

```
<PropertyGroup>
    <RuntimeFrameworkVersion>6.0.0-preview1-26210-3</RuntimeFrameworkVersion>
</PropertyGroup>

<ItemGroup>
  <PackageReference Include="runtime.win-x64.Microsoft.NETCore.Runtime.CoreCLR" Version="6.0.0-preview1-26210-3" />
  <PackageReference Include="runtime.win-x64.Microsoft.NETCore.Jit" Version="6.0.0-preview1-26210-3" />
</ItemGroup>
```

#### 4. Restore and publish

Once have made these modifications you will need to rerun the restore and publish as such.

```
dotnet restore
dotnet publish
```

Now your publication directory should contain your local built CoreCLR builds.
