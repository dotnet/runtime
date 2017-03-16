# How to get up and running on .NET Core 2.0

This document provides the steps necessary to consume a nightly build of
.NET Core 2.0 runtime and SDK.

Please note that these steps are likely to change as we're simplifying
this experience. Make sure to consult this document often.

## Install prerequisites

1. Acquire the latest nightly .NET Core SDK 2.0

- [Win 64-bit Latest Zip](https://dotnetcli.blob.core.windows.net/dotnet/Sdk/master/dotnet-dev-win-x64.latest.zip) [Installer](https://dotnetcli.blob.core.windows.net/dotnet/Sdk/master/dotnet-dev-win-x64.latest.exe)
- [macOS 64-bit Latest Tar](https://dotnetcli.blob.core.windows.net/dotnet/Sdk/master/dotnet-dev-osx-x64.latest.tar.gz) [Installer](https://dotnetcli.blob.core.windows.net/dotnet/Sdk/master/dotnet-dev-osx-x64.latest.pkg)
- [Others](https://github.com/dotnet/cli/blob/master/README.md#installers-and-binaries)

To setup the SDK download the zip and extract it somewhere and add the root folder to your path or always fully
qualify the path to dotnet in the root of this folder for all the instructions in this document.

Note: Installer will put dotnet globally in your path which you might not want for dogfooding daily toolsets.

After setting up dotnet you can verify you are using the newer version by:

`dotnet --info` -- the version should be greater than 2.0.0-*

## Setup the project

1. Create a new project
    - Create a new folder for your app
    - Create project file by running `dotnet new console`

2. Restore packages so that you're ready to play:

```
$ dotnet restore
```

## Consume the new build

Edit your `Program.cs` to consume the new APIs, for example:

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

## Advanced Scenario - Using a nightly build of Microsoft.NETCore.App

When using the above instructions, your application will run against the same
.NET Core 2.0 runtime that comes with the SDK. That works fine to get up and
running quickly. However, there are times when you need to use a nightly build
of Microsoft.NETCore.App which hasn't made its way into the SDK yet. To enable
this, there are two options you can take.

### Option 1: Framework-dependent

This is the default case for applications - running against an installed .NET Core
runtime.

0. You still need to install the prerequisite .NET Core SDK from above.
1. Also, install the specific .NET Core runtime you require:
    - https://github.com/dotnet/core-setup#daily-builds
    - Remember the version number you picked, you'll need it below
2. Modify your .csproj to reference the nightly build of Microsoft.NETCore.App

```XML
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>netcoreapp2.0</TargetFramework>
    <RuntimeFrameworkVersion>2.0.0-beta-xyz-00</RuntimeFrameworkVersion> <!-- this line -->
  </PropertyGroup>
```

```
$ dotnet restore
$ dotnet run
```

### Option 2: Self-contained

In this case, the .NET Core runtime will be published along with your application.

0. You still need to install the prerequisite .NET Core SDK from above.
1. Modify your .csproj to reference the nightly build of Microsoft.NETCore.App *and*
make it self-contained

```XML
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>netcoreapp2.0</TargetFramework>
    <RuntimeFrameworkVersion>2.0.0-beta-xyz-00</RuntimeFrameworkVersion> <!-- pick nightly build -->
    <RuntimeIdentifier>win7-x64</RuntimeIdentifier> <!-- make self-contained -->
  </PropertyGroup>
```

```
$ dotnet restore
$ dotnet publish
$ bin\Debug\netcoreapp2.0\win7-x64\publish\App.exe
```

Note #1: There is a [bug](https://github.com/dotnet/sdk/issues/791) with `dotnet run` and
self-contained applications. If you `dotnet run` and see an error `The library 'hostpolicy.dll'
required to execute the application...`, you've hit this bug.

Note #2: On non-Windows platforms, self-contained applications aren't runnable by default. You will
see an error "Permission denied" when running the application. This is because of a
[breaking change in the .NET Core runtime](https://github.com/dotnet/corefx/issues/15516) between 1.0 and 2.0.
Either this breaking change needs to be fixed, or [NuGet will have to workaround the change]
(https://github.com/NuGet/Home/issues/4424).

To workaround this issue, run `chmod u+x bin/Debug/netcoreapp2.0/RID/publish/App` before executing
your application.

## Using your local CoreFx build

To use your local built corefx packages you will need to be a self-contained application and so you will
need to follow the "Self-contained" steps from above. Once you can successfully restore, build, publish,
and run a self-contained application you need the following steps to consume your local built package.

#### 1 - Get the Version number of the CoreFx package you built.

Look for a package named `Microsoft.Private.CoreFx.NETCoreApp.<version>.nupkg` under `corefx\bin\packages\Debug` (or Release if you built a release version of corefx).

Once you find the version number (for this example assume it is `4.4.0-beta-25102-0`) you need to add the following line to your project file:

```
  <ItemGroup>
    <PackageReference Include="Microsoft.Private.CoreFx.NETCoreApp" Version="4.4.0-beta-25102-0" />
  </ItemGroup>
```

#### 2 - Add your bin directory to the Nuget feed list

By default the dogfooding dotnet SDK will create a Nuget.Config file next to your project, if it doesn't
you can create one. Your config file will need a source for your local corefx package directory as well
as a reference to our nightly dotnet-core feed on myget:

```xml
<configuration>
  <packageSources>
    <add key="local coreclr" value="D:\git\corefx\bin\packages\Debug" />
    <add key="dotnet-core" value="https://dotnet.myget.org/F/dotnet-core/api/v3/index.json" />
  </packageSources>
</configuration>

```
Obviously **you need to update path in the XML to be the path to output directory for your build**.

On Windows you also have the alternative of modifying the Nuget.Config
at `%HOMEPATH%\AppData\Roaming\Nuget\Nuget.Config` (`~/.nuget/NuGet/NuGet.Config` on Linux) with the new location.
This will allow your new runtime to be used on any 'dotnet restore' run by the current user.
Alternatively you can skip creating this file and pass the path to your package directory using
the -s SOURCE qualifer on the dotnet restore command below. The important part is that somehow
you have told the tools where to find your new package.

Once have made these modifications you will need to rerun the restore and publish as such.

```
dotnet restore
dotnet publish
```
Now your publication directory should contain your local built CoreFx binaries.

#### 3 - Consuming updated packages

One possible problem with the technique above is that Nuget assumes that distinct builds have distinct version numbers.
Thus if you modify the source and create a new NuGet package you must give it a new version number and use that in your
application's project. Otherwise the dotnet.exe tool will assume that the existing version is fine and you
won't get the updated bits. This is what the Minor Build number is all about. By default it is 0, but you can
give it a value by setting the BuildNumberMinor environment variable.
```bat
    set BuildNumberMinor=3
```
before packaging. You should see this number show up in the version number (e.g. 4.4.0-beta-25102-03).

As an alternative you can delete the existing copy of the package from the Nuget cache. For example on
windows (on Linux substitute ~/ for %HOMEPATH%) you could delete
```bat
     %HOMEPATH%\.nuget\packages\Microsoft.Private.CoreFx.NETCoreApp\4.4.0-beta-25102-0
```
which should make things work (but is fragile, confirm file timestamps that you are getting the version you expect)

### Consuming individual library packages

The instructions above were only about updates to the binaries that are part of Microsoft.NETCore.App, if you want to test a package
for library that ships in its own nuget package you can follow the same steps above but instead add a package reference to the
individual library package from your `bin\packages\Debug` folder.

## Consuming non-NetStandard assets in a .NET Core 2.0 application

Currently if you reference a NuGet package that does not have a NETStandard asset in your .NET Core 2.0 application, you will hit package
incompatibility errors when trying to restore packages. You can resolve this issue by adding `PackageTargetFallback` property
(MSBuild equivalent of `imports`) to your .csproj:

```XML
  <PackageTargetFallback>$(PackageTargetFallback);net45</PackageTargetFallback>
```

Note that this can fix the problem if the package is actually compatible with netcoreapp2.0 (meaning it does not use types/APIs
that are not available in netcoreapp2.0)

For final release, we are considering modifying NuGet behavior to automatically consume the non-netstandard asset if there is no netstandard available.


## Creating a .NET Core 2.0 console application from Visual Studio 2017

File > New > Project > Console App (.NET Core)

By default, Visual Studio creates a netcoreapp1.1 application. After installing the prerequisites mentioned above, you will
need to modify your .csproj to target netcoreapp2.0 and reference the nightly build of Microsoft.NETCore.APP

```XML
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>netcoreapp2.0</TargetFramework> <!-- this line -->
    <RuntimeFrameworkVersion>2.0.0-beta-xyz-00</RuntimeFrameworkVersion> <!-- this line -->
  </PropertyGroup>
```

In a future update to Visual Studio, it will no longer be necessary to make this edit.