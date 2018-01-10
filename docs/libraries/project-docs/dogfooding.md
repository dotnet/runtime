# How to get up and running on .NET Core

This document provides the steps necessary to consume a nightly build of
.NET Core runtime and SDK.

Please note that these steps are likely to change as we're simplifying
this experience. Make sure to consult this document often.

## Install prerequisites

1. Acquire the latest nightly .NET Core SDK by downloading the zip or tarball listed in https://github.com/dotnet/cli/blob/master/README.md#installers-and-binaries (for example, https://dotnetcli.blob.core.windows.net/dotnet/Sdk/master/dotnet-sdk-latest-win-x64.zip ) into a new folder.

2. By default, the dotnet CLI will use the globally installed SDK if it matches the major/minor version you request and has a higher revision. To force it to use the locally installed SDK, you must set an environment variable `DOTNET_MULTILEVEL_LOOKUP=0` in your shell. You can use `dotnet --info` to verify what version of the Shared Framework it is using.

3. Reminder: if you are using a local copy of the dotnet CLI, take care that when you type `dotnet` you do not inadvertently pick up a different copy that you may have in your path. On Windows, for example, if you use a Developer Command Prompt, a global copy may be in the path, so use the fully qualified path to your local `dotnet`. If you receive an error "The current .NET SDK does not support targeting .NET Core 2.1." then you may be executing an older `dotnet`.

After setting up dotnet you can verify you are using the newer version by executing `dotnet --info` -- the version should be greater than 2.2.0-*  (dotnet CLI is currently numbered 2.2.0-* not 2.1.0-* ). Here is an example output at the time of writing:
```
>dotnet.exe --info
.NET Command Line Tools (2.2.0-preview1-007460)

Product Information:
 Version:            2.2.0-preview1-007460
 Commit SHA-1 hash:  173cc035e4

Runtime Environment:
 OS Name:     Windows
 OS Version:  10.0.16299
 OS Platform: Windows
 RID:         win10-x64
 Base Path:   F:\dotnet\sdk\2.2.0-preview1-007460\

Microsoft .NET Core Shared Framework Host

  Version  : 2.1.0-preview1-25825-07
  Build    : 4c165c13bd390adf66f9af30a088d634d3f37a9d
```

4. Our nightly builds are uploaded to MyGet, not NuGet - so ensure the .NET Core MyGet feed is in your nuget configuration in case you need other packages from .NET Core that aren't included in the download. For example, on Windows you could edit `%userprofile%\appdata\roaming\nuget\nuget.config` or on Linux edit `~/.nuget/NuGet/NuGet.Config` to add this line:
```xml
<packageSources>
    <add key="myget.dotnetcore" value="https://dotnet.myget.org/F/dotnet-core/api/v3/index.json" />
    ...
</packageSources>    
```
(Documentation for configuring feeds is [here](https://docs.microsoft.com/en-us/nuget/consume-packages/configuring-nuget-behavior).)

## Setup the project

1. Create a new project
    - Create a new folder for your app and change to that folder
    - Create project file by running `dotnet new console`

2. Restore packages so that you're ready to play:

```
$ dotnet restore
```

## Consume the new build

```
$ dotnet run
```

Rinse and repeat!

## Advanced Scenario - Using a nightly build of Microsoft.NETCore.App

When using the above instructions, your application will run against the same
.NET Core runtime that comes with the SDK. That works fine to get up and
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
    <TargetFramework>netcoreapp2.1</TargetFramework>
    <RuntimeFrameworkVersion>2.1.0-preview1-25825-07</RuntimeFrameworkVersion> <!-- modify build in this line -->
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
    <TargetFramework>netcoreapp2.1</TargetFramework>
    <RuntimeFrameworkVersion>2.1.0-preview1-25825-07</RuntimeFrameworkVersion> <!-- modify build in this line -->
    <RuntimeIdentifier>win-x64</RuntimeIdentifier> <!-- make self-contained -->
  </PropertyGroup>
```

```
$ dotnet restore
$ dotnet publish
$ bin\Debug\netcoreapp2.1\win-x64\publish\App.exe
```

## More Advanced Scenario - Using your local CoreFx build

If you built corefx locally with `build -allconfigurations` after building binaries it will build NuGet packages containing them. You can use those in your projects.

To use your local built corefx packages you will need to be a self-contained application and so you will
need to follow the "Self-contained" steps from above. Once you can successfully restore, build, publish,
and run a self-contained application you need the following steps to consume your local built package.

#### 1 - Get the Version number of the CoreFx package you built.

Look for a package named `Microsoft.Private.CoreFx.NETCoreApp.<version>.nupkg` under `corefx\bin\packages\Debug` (or Release if you built a release version of corefx).

Once you find the version number (for this example assume it is `4.5.0-preview1-25830-0`) you need to add the following line to your project file:

```
  <ItemGroup>
    <PackageReference Include="Microsoft.Private.CoreFx.NETCoreApp" Version="4.5.0-preview1-25830-0" />
  </ItemGroup>
```

Because assets in `Microsoft.Private.CoreFx.NETCoreApp` conflict with the normal `Microsoft.NETCore.App` package,
you need to tell the tooling to use the assets from your local package. To do this, add the following property to your project file:

```xml
  <PropertyGroup>
    <PackageConflictPreferredPackages>Microsoft.Private.CoreFx.NETCoreApp;runtime.win-x64.Microsoft.Private.CoreFx.NETCoreApp;$(PackageConflictPreferredPackages)</PackageConflictPreferredPackages>
  </PropertyGroup>
```

Replacing the RID in `runtime.win-x64.Microsoft.Private.CoreFx.NETCoreApp` with the RID of your current build.

Note these instructions above were only about updates to the binaries that are part of Microsoft.NETCore.App, if you want to test a package for library that ships in its own nuget package you can follow the same steps above but instead add a package reference to that package instead of "Microsoft.Private.CoreFx.NETCoreApp".

#### 2 - Add your bin directory to the Nuget feed list

By default the dogfooding dotnet SDK will create a Nuget.Config file next to your project, if it doesn't
you can create one. Your config file will need a source for your local corefx package directory as well
as a reference to our nightly dotnet-core feed on myget. The Nuget.Config file content should be:

```xml
<configuration>
  <packageSources>
    <add key="local coreclr" value="D:\git\corefx\bin\packages\Debug" /> <!-- Change this to your own output path -->
    <add key="dotnet-core" value="https://dotnet.myget.org/F/dotnet-core/api/v3/index.json" />
  </packageSources>
</configuration>
```
Be sure to correct the path to your build output above.

You also have the alternative of modifying the Nuget.Config
at `%HOMEPATH%\AppData\Roaming\Nuget\Nuget.Config` (Windows) or `~/.nuget/NuGet/NuGet.Config` (Linux) with the new location.
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

#### 3 - Consuming subsequent code changes by overwriting the binary (Alternative 1)

To apply changes you subsequently make in your source tree, it's usually easiest to just overwrite the binary in the publish folder. Build the assembly containing your change as normal, then overwrite the assembly in your publish folder and running the app will pick up that binary. This relies on the fact that all the other binaries still match what is in your bin folder so everything works together.

#### 3 - Consuming subsequent code changes by rebuilding the package (Alternative 2)

This is more cumbersome than just overwriting the binaries, but is more correct.

First note that Nuget assumes that distinct builds have distinct version numbers.
Thus if you modify the source and create a new NuGet package you must give it a new version number and use that in your
application's project. Otherwise the dotnet.exe tool will assume that the existing version is fine and you
won't get the updated bits. This is what the Minor Build number is all about. By default it is 0, but you can
give it a value by setting the BuildNumberMinor environment variable.
```bat
    set BuildNumberMinor=3
```
before packaging. You should see this number show up in the version number (e.g. 4.5.0-preview1-25830-03).

Alternatively just delete the existing copy of the package from the Nuget cache. For example on
windows (on Linux substitute ~/ for %HOMEPATH%) you could delete
```bat
     %HOMEPATH%\.nuget\packages\Microsoft.Private.CoreFx.NETCoreApp\4.5.0-preview1-25830-0
     %HOMEPATH%\.nuget\packages\runtime.win-x64.microsoft.private.corefx.netcoreapp\4.5.0-preview1-25830-0
```
which should make `dotnet restore` now pick up the new copy.

