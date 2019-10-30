
# Using your .NET Core Runtime Build with dotnet cli

This walkthrough explains how to run against your local CoreCLR build using `dotnet cli` only.

For other walkthroughs see:

- [Using Your Build - Update CoreCLR from raw binary output](UsingYourBuild.md)
- [Using CoreRun To Run .NET Core Application](UsingCoreRun.md)
- [Dogfooding .NET Core SDK](https://github.com/dotnet/corefx/blob/master/Documentation/project-docs/dogfooding.md).

## Prerequisites

1. Successfully built CoreCLR repository and thus have files of the form shown below. From now on we call this folder NuGet package folder.

```
    bin\Product\<OS>.<arch>.<flavor>\.nuget\pkg\runtime.<OS>-<arch>.Microsoft.NETCore.Runtime.CoreCLR.<version>.nupkg
```

2. Acquired the latest nightly .NET Core SDK from [here](https://github.com/dotnet/cli/blob/master/README.md#installers-and-binaries) and added it's root folder to your [path](../building/windows-instructions.md#adding-to-the-default-path-variable)

## First Run

### 1. Create new folder for the app

`mkdir helloWorld`

From now on all instructions relate to this folder as "app folder".

### 2. Create NuGet.Config file

The build script creates NuGet packages and puts them to `bin\Product\<OS>.<arch>.<flavor>\.nuget\pkg\`. dotnet cli has no idea about its existence and we need to tell it where to search for the packages.

Please run `dotnet new nugetconfig` in the app folder and update the created `NuGet.Config` file:

* **set path to local CoreCLR NuGet folder!!**
* add address to dotnet core tools NuGet feed


```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <!--To inherit the global NuGet package sources remove the <clear/> line below -->
    <clear />

    <add key="local CoreCLR" value="C:\coreclr\bin\Product\Windows_NT.x64.Debug\.nuget\pkg" /> <!-- CHANGE THIS PATH to your local output path -->
    <add key="dotnet-core" value="https://dotnet.myget.org/F/dotnet-core/api/v3/index.json" /> <!-- link to corefx NuGet feed -->
  </packageSources>
</configuration>

```

### 3. Create and update the Project file

Please run `dotnet new console` in the app folder and update the created `.csproj` file:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>netcoreapp3.0</TargetFramework>
    <RuntimeIdentifier>win-x64</RuntimeIdentifier>
    <RuntimeFrameworkVersion>3.0.0-preview1-26210-0</RuntimeFrameworkVersion>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="runtime.win-x64.Microsoft.NETCore.Runtime.CoreCLR" Version="3.0.0-preview1-26210-0" />
    <PackageReference Include="runtime.win-x64.Microsoft.NETCore.Jit" Version="3.0.0-preview1-26210-0" />
  </ItemGroup>

</Project>
```

**You have to set the correct values for `RuntimeIdentifier` (RI), `RuntimeFrameworkVersion` and versions of both packages.**

You can generally figure that out by looking at the packages you found in your output. 
In our example you will see there is a package with the name `runtime.win-x64.Microsoft.NETCore.Runtime.CoreCLR.3.0.0-preview1-26210-0.nupkg`

```
runtime.win-x64.Microsoft.NETCore.Runtime.CoreCLR.3.0.0-preview1-26210-0.nupkg
       ^--RI---^                                 ^--------version-------^  
```

### 4. Change Program.cs

To make sure that you run against your local coreclr build please change your `Main` method in `Program.cs` file to:

```cs
static void Main(string[] args)
{
	var coreAssemblyInfo = System.Diagnostics.FileVersionInfo.GetVersionInfo(typeof(object).Assembly.Location);
	Console.WriteLine($"Hello World from Core {coreAssemblyInfo.ProductVersion}");
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
PS C:\coreclr\helloWorld> dotnet publish
  Restoring packages for C:\coreclr\helloWorld\helloWorld.csproj...
  Installing runtime.win-x64.Microsoft.NETCore.Runtime.CoreCLR 3.0.0-preview1-26210-
```

If you see something like the message below it means that it has failed to restore your local runtime packages. In such case double check your `NuGet.config` file and paths used in it.

```
C:\coreclr\helloWorld\helloWorld.csproj : warning NU1603: helloWorld depends on runtime.win-x64.Microsoft.NETCore.Runtime.CoreCLR (>= 3.0.0-preview1-26210-0) but runtime.win-x64.Microsoft.NETCore.Runtime.CoreCLR 3.0.0-preview1-26210-0 was not found. An approximate best match of runtime.win-x64.Microsoft.NETCore.Runtime.CoreCLR 3.0.0-preview2-25501-02 was resolved.
```

### 6. Run the app

After you publish you will find all the binaries needed to run your application under `bin\Debug\netcoreapp3.0\win-x64\publish\`.
To run the application simply run the EXE that is in this publish directory (it is the name of the app, or specified in the project file).

```
.\bin\Debug\netcoreapp3.0\win-x64\publish\HelloWorld.exe
```

Running the app should tell you the version and which user and machine build the assembly as well as the commit hash of the code
at the time of building:

```
Hello World from Core 4.6.26210.0 @BuiltBy: adsitnik-MININT-O513E3V @SrcCode: https://github.com/dotnet/coreclr/tree/3d6da797d1f7dc47d5934189787a4e8006ab3a04
The location is C:\coreclr\helloWorld\bin\Debug\netcoreapp3.0\win-x64\publish\System.Private.CoreLib.dll
```

**Congratulations! You have just run your first app against local CoreCLR build!** 

## Update CoreCLR using runtime nuget package

Updating CoreCLR from raw binary output is easier for quick one-off testing but using the nuget package is better
for referencing your CoreCLR build in your actual application because of it does not require manual copying of files
around each time the application is built and plugs into the rest of the tool chain. This set of instructions will cover
the further steps needed to consume the runtime nuget package.

#### 1. Update BuildNumberMinor Environment Variable

One possible problem with this technique is that Nuget assumes that distinct builds have distinct version numbers.
Thus if you modify the source and create a new NuGet package you must give it a new version number and use that in your
application's project. Otherwise the dotnet.exe tool will assume that the existing version is fine and you
won't get the updated bits. This is what the Minor Build number is all about. By default it is 0, but you can
give it a value by setting the BuildNumberMinor environment variable.
```bat
    set BuildNumberMinor=3
```
before packaging. You should see this number show up in the version number (e.g. 3.0.0-preview1-26210-03).

As an alternative you can delete the existing copy of the package from the Nuget cache.   For example on
windows (on Linux substitute ~/ for %HOMEPATH%) you could delete
```bat
     %HOMEPATH%\.nuget\packages\runtime.win-x64.Microsoft.NETCore.Runtime.CoreCLR\3.0.0-preview1-26210-0
```
which should make things work (but is fragile, confirm file timestamps that you are getting the version you expect)

#### 2. Get the Version number of the CoreCLR package you built.

Get this by simply listing the name of the `runtime.win-x64.Microsoft.NETCore.Runtime.CoreCLR` you built.

```bat
    dir bin\Product\Windows_NT.x64.Release\.nuget\pkg
```

and you will get name of the which looks something like this

```
    runtime.win-x64.Microsoft.NETCore.Runtime.CoreCLR.3.0.0-preview1-26210-3.nupkg
```

This gets us the version number, in the above case it is 3.0.0-preview1-26210-3. We will
use this in the next step.

#### 3. Update the references to your runtime package

Edit your `.csproj` file and change the versions:

```
<PropertyGroup>
    <RuntimeFrameworkVersion>3.0.0-preview1-26210-3</RuntimeFrameworkVersion>
</PropertyGroup>

<ItemGroup>
  <PackageReference Include="runtime.win-x64.Microsoft.NETCore.Runtime.CoreCLR" Version="3.0.0-preview1-26210-3" />
  <PackageReference Include="runtime.win-x64.Microsoft.NETCore.Jit" Version="3.0.0-preview1-26210-3" />
</ItemGroup>
```

#### 4. Restore and publish

Once have made these modifications you will need to rerun the restore and publish as such.

```
dotnet restore
dotnet publish
```

Now your publication directory should contain your local built CoreCLR builds.
