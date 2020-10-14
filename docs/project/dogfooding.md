# How to get up and running on .NET Core

This document provides the steps necessary to consume a nightly build of
.NET runtime and SDK.

Please note that these steps are likely to change as we're simplifying
this experience. Make sure to consult this document often.

## Obtaining nightly builds of NuGet packages

If you are only looking to get fixes for an individual NuGet package, and don't need a preview version of the entire runtime, you can add the nightly build package feed to your `NuGet.config` file.  The easiest way to do this is by using the dotnet CLI:

**(Recommended)** Create a local NuGet.Config file for your solution, if don't already have one.  Using a local NuGet.Config file will enable the nightly feed as a package source for projects in the current directory only.
```
dotnet new nugetconfig
```

Next, add the package source to NuGet.Config with the [dotnet nuget add source](https://docs.microsoft.com/en-us/dotnet/core/tools/dotnet-nuget-add-source) command:
```
dotnet nuget add source -n dotnet5 https://dnceng.pkgs.visualstudio.com/public/_packaging/dotnet5/nuget/v3/index.json
```

Then, you will be able to add the latest prerelease version of the desired package to your project.

**Example:** To add version 5.0.0-preview.1.20120.5 of the System.Data.OleDb package, use the [dotnet add package](https://docs.microsoft.com/en-us/dotnet/core/tools/dotnet-add-package) command:
```
dotnet add package System.Data.OleDb -v 5.0.0-preview.1.20120.5
```

To use nightly builds of the entire runtime, follow the steps given in the rest of this document instead.

## Install prerequisites

1. Acquire the latest nightly .NET SDK by downloading the zip or tarball listed in https://github.com/dotnet/core-sdk#installers-and-binaries (for example, https://dotnetcli.blob.core.windows.net/dotnet/Sdk/master/dotnet-sdk-latest-win-x64.zip ) into a new folder, for instance `C:\dotnet`.

2. By default, the dotnet CLI will use the globally installed SDK if it matches the major/minor version you request and has a higher revision. To force it to use the locally installed SDK, you must set an environment variable `DOTNET_MULTILEVEL_LOOKUP=0` in your shell. You can use `dotnet --info` to verify what version of the Shared Framework it is using.

3. Reminder: if you are using a local copy of the dotnet CLI, take care that when you type `dotnet` you do not inadvertently pick up a different copy that you may have in your path. On Windows, for example, if you use a Command Prompt, a global copy may be in the path, so use the fully qualified path to your local `dotnet` (e.g. `C:\dotnet\dotnet.exe`). If you receive an error "error NETSDK1045:  The current .NET SDK does not support targeting .NET Core 5.0." then you may be executing an older `dotnet`.

After setting up dotnet you can verify you are using the dogfooding version by executing `dotnet --info`. Here is an example output at the time of writing:
```
>dotnet --info
.NET Core SDK (reflecting any global.json):
 Version:   5.0.100-preview.1.20167.6
 Commit:    00255dd10b

Runtime Environment:
 OS Name:     Windows
 OS Version:  10.0.18363
 OS Platform: Windows
 RID:         win10-x64
 Base Path:   c:\dotnet\sdk\5.0.100-preview.1.20167.6\

Host (useful for support):
  Version: 5.0.0-preview.1.20120.5
  Commit:  3c523a6a7a

.NET Core SDKs installed:
  5.0.100-preview.1.20167.6 [c:\dotnet\sdk]

.NET Core runtimes installed:
  Microsoft.AspNetCore.App 5.0.0-preview.1.20124.5 [c:\dotnet\shared\Microsoft.AspNetCore.App]
  Microsoft.NETCore.App 5.0.0-preview.1.20120.5 [c:\dotnet\shared\Microsoft.NETCore.App]
  Microsoft.WindowsDesktop.App 5.0.0-preview.1.20127.5 [c:\dotnet\shared\Microsoft.WindowsDesktop.App]

To install additional .NET Core runtimes or SDKs:
  https://aka.ms/dotnet-download
```

4. Our nightly builds are uploaded to dotnet-blob feeds, not NuGet - so ensure the .NET Core blob feed is in your nuget configuration in case you need other packages from .NET Core that aren't included in the download. For example, on Windows you could edit `%userprofile%\appdata\roaming\nuget\nuget.config` or on Linux edit `~/.nuget/NuGet/NuGet.Config` to add these lines:
```xml
<packageSources>
    <add key="dotnet5" value="https://dnceng.pkgs.visualstudio.com/public/_packaging/dotnet5/nuget/v3/index.json" />
    <add key="gRPC repository" value="https://grpc.jfrog.io/grpc/api/nuget/v3/grpc-nuget-dev" />
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
.NET runtime that comes with the SDK. That works fine to get up and
running quickly. However, there are times when you need to use a nightly build
of Microsoft.NETCore.App which hasn't made its way into the SDK yet. To enable
this, there are two options you can take.

### Option 1: Framework-dependent

This is the default case for applications - running against an installed .NET runtime.

1. You still need to install the prerequisite .NET SDK from above.
2. Optionally, install the specific .NET runtime you require:
    - https://github.com/dotnet/core-sdk#installers-and-binaries
3. Modify your .csproj to reference the nightly build of Microsoft.NETCore.App

```XML
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <!-- Ensure that the target framework is correct e.g. 'net5.0' -->
    <TargetFramework>net5.0</TargetFramework>
    <!-- modify version in this line with one reported by `dotnet --info` under ".NET runtimes installed" -> Microsoft.NETCore.App -->
    <RuntimeFrameworkVersion>5.0.0-preview.1.20120.5</RuntimeFrameworkVersion>
  </PropertyGroup>
```

```
$ dotnet restore
$ dotnet run
```

### Option 2: Self-contained

In this case, the .NET runtime will be published along with your application.

1. You still need to install the prerequisite .NET SDK from above.
2. Modify your .csproj to reference the nightly build of Microsoft.NETCore.App *and*
make it self-contained by adding a RuntimeIdentifier (RID).

```XML
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <!-- Ensure that the target framework is correct e.g. 'net5.0' -->
    <TargetFramework>net5.0</TargetFramework>
    <!-- modify build in this line with version reported by `dotnet --info` as above under ".NET runtimes installed" -> Microsoft.NETCore.App -->
    <!-- moreover, this can be any valid Microsoft.NETCore.App package version from https://dotnetfeed.blob.core.windows.net/dotnet-core/index.json -->
    <RuntimeFrameworkVersion>5.0.0-preview.1.20120.5</RuntimeFrameworkVersion>
    <RuntimeIdentifier>win-x64</RuntimeIdentifier> <!-- RID to make it self-contained -->
  </PropertyGroup>
```

```
$ dotnet restore
$ dotnet publish
$ bin\Debug\net5.0\win-x64\publish\App.exe
```

## More Advanced Scenario - Using your local CoreFx build

If you built corefx locally with `build -allconfigurations` after building binaries it will build NuGet packages containing them. You can use those in your projects.

To use your local built corefx packages you will need to be a self-contained application and so you will
need to follow the "Self-contained" steps from above. Once you can successfully restore, build, publish,
and run a self-contained application you need the following steps to consume your local built package.

#### 1 - Get the Version number of the CoreFx package you built.

Look for a package named `Microsoft.Private.CoreFx.NETCoreApp.<version>.nupkg` under `corefx\artifacts\packages\Debug` (or Release if you built a release version of corefx).

Once you find the version number (for this example assume it is `4.6.0-dev.18626.1`) you need to add the following line to your project file:

```
  <ItemGroup>
    <PackageReference Include="Microsoft.Private.CoreFx.NETCoreApp" Version="4.6.0-dev.18626.1" />
  </ItemGroup>
```

Because assets in `Microsoft.Private.CoreFx.NETCoreApp` conflict with the normal `Microsoft.NETCore.App` package,
you need to tell the tooling to use the assets from your local package. To do this, add the following property to your project file:

```xml
  <PropertyGroup>
    <PackageConflictPreferredPackages>Microsoft.Private.CoreFx.NETCoreApp;runtime.win-x64.Microsoft.Private.CoreFx.NETCoreApp;$(PackageConflictPreferredPackages)</PackageConflictPreferredPackages>
  </PropertyGroup>
```

Replacing the RID (`win-x64` in this case) in `runtime.win-x64.Microsoft.Private.CoreFx.NETCoreApp` with the RID of your current build.

Note these instructions above were only about updates to the binaries that are part of Microsoft.NETCore.App, if you want to test a package for library that ships in its own nuget package you can follow the same steps above but instead add a package reference to that package instead of "Microsoft.Private.CoreFx.NETCoreApp".

#### 2 - Add your bin directory to the Nuget feed list

By default the dogfooding dotnet SDK will create a Nuget.Config file next to your project, if it doesn't
you can create one. Your config file will need a source for your local corefx package directory as well
as a reference to our nightly dotnet-core blob feed. The Nuget.Config file content should be:

```xml
<configuration>
  <packageSources>
    <add key="local coreclr" value="D:\git\corefx\artifacts\packages\Debug" /> <!-- Change this to your own output path -->
    <add key="dotnetcore-feed" value="https://dotnetfeed.blob.core.windows.net/dotnet-core/index.json" />
  </packageSources>
</configuration>
```
Be sure to correct the path to your build output above.

You also have the alternative of modifying the Nuget.Config
at `%HOMEPATH%\AppData\Roaming\Nuget\Nuget.Config` (Windows) or `~/.nuget/NuGet/NuGet.Config` (Linux) with the new location.
This will allow your new runtime to be used on any 'dotnet restore' run by the current user.
Alternatively you can skip creating this file and pass the path to your package directory using
the -s SOURCE qualifier on the dotnet restore command below. The important part is that somehow
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
before packaging. You should see this number show up in the version number (e.g. 4.6.0-dev.18626.1).

Alternatively just delete the existing copy of the package from the Nuget cache. For example on
windows (on Linux substitute ~/ for %HOMEPATH%) you could delete
```bat
     %HOMEPATH%\.nuget\packages\Microsoft.Private.CoreFx.NETCoreApp\4.6.0-dev.18626.1
     %HOMEPATH%\.nuget\packages\runtime.win-x64.microsoft.private.corefx.netcoreapp\4.6.0-dev.18626.1
```
which should make `dotnet restore` now pick up the new copy.

<!-- BEGIN links to include in table -->

[win-x64-badge-6.0.X-coreclr]: https://aka.ms/dotnet/net6/dev/Runtime/sharedfx_win-x64_Release_version_badge.svg
[win-x64-version-6.0.X-coreclr]: https://aka.ms/dotnet/net6/dev/Runtime/productVersion.txt
[win-x64-installer-6.0.X-coreclr]: https://aka.ms/dotnet/net6/dev/Runtime/dotnet-runtime-win-x64.exe
[win-x64-installer-checksum-6.0.X-coreclr]: https://aka.ms/dotnet/net6/dev/Runtime/dotnet-runtime-win-x64.exe.sha512
[win-x64-zip-6.0.X-coreclr]: https://aka.ms/dotnet/net6/dev/Runtime/dotnet-runtime-win-x64.zip
[win-x64-zip-checksum-6.0.X-coreclr]: https://aka.ms/dotnet/net6/dev/Runtime/dotnet-runtime-win-x64.zip.sha512
[win-x64-nethost-zip-6.0.X-coreclr]: https://aka.ms/dotnet/net6/dev/Runtime/dotnet-nethost-win-x64.zip
[win-x64-symbols-zip-6.0.X-coreclr]: https://aka.ms/dotnet/net6/dev/Runtime/dotnet-runtime-symbols-win-x64.zip

[win-x86-badge-6.0.X-coreclr]: https://aka.ms/dotnet/net6/dev/Runtime/sharedfx_win-x86_Release_version_badge.svg
[win-x86-version-6.0.X-coreclr]: https://aka.ms/dotnet/net6/dev/Runtime/productVersion.txt
[win-x86-installer-6.0.X-coreclr]: https://aka.ms/dotnet/net6/dev/Runtime/dotnet-runtime-win-x86.exe
[win-x86-installer-checksum-6.0.X-coreclr]: https://aka.ms/dotnet/net6/dev/Runtime/dotnet-runtime-win-x86.exe.sha512
[win-x86-zip-6.0.X-coreclr]: https://aka.ms/dotnet/net6/dev/Runtime/dotnet-runtime-win-x86.zip
[win-x86-zip-checksum-6.0.X-coreclr]: https://aka.ms/dotnet/net6/dev/Runtime/dotnet-runtime-win-x86.zip.sha512
[win-x86-nethost-zip-6.0.X-coreclr]: https://aka.ms/dotnet/net6/dev/Runtime/dotnet-nethost-win-x86.zip
[win-x86-symbols-zip-6.0.X-coreclr]: https://aka.ms/dotnet/net6/dev/Runtime/dotnet-runtime-symbols-win-x86.zip

[win-arm64-badge-6.0.X-coreclr]: https://aka.ms/dotnet/net6/dev/Runtime/sharedfx_win-arm64_Release_version_badge.svg
[win-arm64-version-6.0.X-coreclr]: https://aka.ms/dotnet/net6/dev/Runtime/productVersion.txt
[win-arm64-installer-6.0.X-coreclr]: https://aka.ms/dotnet/net6/dev/Runtime/dotnet-runtime-win-arm64.exe
[win-arm64-installer-checksum-6.0.X-coreclr]: https://aka.ms/dotnet/net6/dev/Runtime/dotnet-runtime-win-arm64.exe.sha512
[win-arm64-zip-6.0.X-coreclr]: https://aka.ms/dotnet/net6/dev/Runtime/dotnet-runtime-win-arm64.zip
[win-arm64-zip-checksum-6.0.X-coreclr]: https://aka.ms/dotnet/net6/dev/Runtime/dotnet-runtime-win-arm64.zip.sha512
[win-arm64-nethost-zip-6.0.X-coreclr]: https://aka.ms/dotnet/net6/dev/Runtime/dotnet-nethost-win-arm64.zip
[win-arm64-symbols-zip-6.0.X-coreclr]: https://aka.ms/dotnet/net6/dev/Runtime/dotnet-runtime-symbols-win-arm64.zip

[osx-x64-badge-6.0.X-coreclr]: https://aka.ms/dotnet/net6/dev/Runtime/sharedfx_osx-x64_Release_version_badge.svg
[osx-x64-version-6.0.X-coreclr]: https://aka.ms/dotnet/net6/dev/Runtime/productVersion.txt
[osx-x64-installer-6.0.X-coreclr]: https://aka.ms/dotnet/net6/dev/Runtime/dotnet-runtime-osx-x64.pkg
[osx-x64-installer-checksum-6.0.X-coreclr]: https://aka.ms/dotnet/net6/dev/Runtime/dotnet-runtime-osx-x64.pkg.sha512
[osx-x64-targz-6.0.X-coreclr]: https://aka.ms/dotnet/net6/dev/Runtime/dotnet-runtime-osx-x64.tar.gz
[osx-x64-targz-checksum-6.0.X-coreclr]: https://aka.ms/dotnet/net6/dev/Runtime/dotnet-runtime-osx-x64.tar.gz.sha512
[osx-x64-nethost-targz-6.0.X-coreclr]: https://aka.ms/dotnet/net6/dev/Runtime/dotnet-nethost-osx-x64.tar.gz
[osx-x64-symbols-targz-6.0.X-coreclr]: https://aka.ms/dotnet/net6/dev/Runtime/dotnet-runtime-symbols-osx-x64.tar.gz

[osx-arm64-badge-6.0.X-coreclr]: https://aka.ms/dotnet/net6/dev/Runtime/sharedfx_osx-arm64_Release_version_badge.svg
[osx-arm64-version-6.0.X-coreclr]: https://aka.ms/dotnet/net6/dev/Runtime/productVersion.txt
[osx-arm64-installer-6.0.X-coreclr]: https://aka.ms/dotnet/net6/dev/Runtime/dotnet-runtime-osx-arm64.pkg
[osx-arm64-installer-checksum-6.0.X-coreclr]: https://aka.ms/dotnet/net6/dev/Runtime/dotnet-runtime-osx-arm64.pkg.sha512
[osx-arm64-targz-6.0.X-coreclr]: https://aka.ms/dotnet/net6/dev/Runtime/dotnet-runtime-osx-arm64.tar.gz
[osx-arm64-targz-checksum-6.0.X-coreclr]: https://aka.ms/dotnet/net6/dev/Runtime/dotnet-runtime-osx-arm64.tar.gz.sha512
[osx-arm64-nethost-targz-6.0.X-coreclr]: https://aka.ms/dotnet/net6/dev/Runtime/dotnet-nethost-osx-arm64.tar.gz
[osx-arm64-symbols-targz-6.0.X-coreclr]: https://aka.ms/dotnet/net6/dev/Runtime/dotnet-runtime-symbols-osx-arm64.tar.gz

[linux-x64-badge-6.0.X-coreclr]: https://aka.ms/dotnet/net6/dev/Runtime/sharedfx_linux-x64_Release_version_badge.svg
[linux-x64-version-6.0.X-coreclr]: https://aka.ms/dotnet/net6/dev/Runtime/productVersion.txt
[linux-x64-targz-6.0.X-coreclr]: https://aka.ms/dotnet/net6/dev/Runtime/dotnet-runtime-linux-x64.tar.gz
[linux-x64-targz-checksum-6.0.X-coreclr]: https://aka.ms/dotnet/net6/dev/Runtime/dotnet-runtime-linux-x64.tar.gz.sha512
[linux-x64-nethost-targz-6.0.X-coreclr]: https://aka.ms/dotnet/net6/dev/Runtime/dotnet-nethost-linux-x64.tar.gz
[linux-x64-symbols-targz-6.0.X-coreclr]: https://aka.ms/dotnet/net6/dev/Runtime/dotnet-runtime-symbols-linux-x64.tar.gz

[linux-arm-badge-6.0.X-coreclr]: https://aka.ms/dotnet/net6/dev/Runtime/sharedfx_linux-arm_Release_version_badge.svg
[linux-arm-version-6.0.X-coreclr]: https://aka.ms/dotnet/net6/dev/Runtime/productVersion.txt
[linux-arm-targz-6.0.X-coreclr]: https://aka.ms/dotnet/net6/dev/Runtime/dotnet-runtime-linux-arm.tar.gz
[linux-arm-targz-checksum-6.0.X-coreclr]: https://aka.ms/dotnet/net6/dev/Runtime/dotnet-runtime-linux-arm.tar.gz.sha512
[linux-arm-nethost-targz-6.0.X-coreclr]: https://aka.ms/dotnet/net6/dev/Runtime/dotnet-nethost-linux-arm.tar.gz
[linux-arm-symbols-targz-6.0.X-coreclr]: https://aka.ms/dotnet/net6/dev/Runtime/dotnet-runtime-symbols-linux-arm.tar.gz

[linux-arm64-badge-6.0.X-coreclr]: https://aka.ms/dotnet/net6/dev/Runtime/sharedfx_linux-arm64_Release_version_badge.svg
[linux-arm64-version-6.0.X-coreclr]: https://aka.ms/dotnet/net6/dev/Runtime/productVersion.txt
[linux-arm64-targz-6.0.X-coreclr]: https://aka.ms/dotnet/net6/dev/Runtime/dotnet-runtime-linux-arm64.tar.gz
[linux-arm64-targz-checksum-6.0.X-coreclr]: https://aka.ms/dotnet/net6/dev/Runtime/dotnet-runtime-linux-arm64.tar.gz.sha512
[linux-arm64-nethost-targz-6.0.X-coreclr]: https://aka.ms/dotnet/net6/dev/Runtime/dotnet-nethost-linux-arm64.tar.gz
[linux-arm64-symbols-targz-6.0.X-coreclr]: https://aka.ms/dotnet/net6/dev/Runtime/dotnet-runtime-symbols-linux-arm64.tar.gz

[deb-badge-6.0.X-coreclr]: https://aka.ms/dotnet/net6/dev/Runtime/sharedfx_ubuntu.14.04-x64_Release_version_badge.svg
[deb-version-6.0.X-coreclr]: https://aka.ms/dotnet/net6/dev/Runtime/productVersion.txt
[deb-apphost-pack-6.0.X-coreclr]: https://aka.ms/dotnet/net6/dev/Runtime/dotnet-apphost-pack-x64.deb
[deb-apphost-pack-checksum-6.0.X-coreclr]: https://aka.ms/dotnet/net6/dev/Runtime/dotnet-apphost-pack-x64.deb.sha512
[deb-host-6.0.X-coreclr]: https://aka.ms/dotnet/net6/dev/Runtime/dotnet-host-x64.deb
[deb-runtime-deps-6.0.X-coreclr]: https://aka.ms/dotnet/net6/dev/Runtime/dotnet-runtime-deps-x64.deb
[deb-runtime-deps-checksum-6.0.X-coreclr]: https://aka.ms/dotnet/net6/dev/Runtime/dotnet-runtime-deps-x64.deb.sha512
[deb-host-checksum-6.0.X-coreclr]: https://aka.ms/dotnet/net6/dev/Runtime/dotnet-host-x64.deb.sha512
[deb-hostfxr-6.0.X-coreclr]: https://aka.ms/dotnet/net6/dev/Runtime/dotnet-hostfxr-x64.deb
[deb-hostfxr-checksum-6.0.X-coreclr]: https://aka.ms/dotnet/net6/dev/Runtime/dotnet-hostfxr-x64.deb.sha512
[deb-sharedfx-6.0.X-coreclr]: https://aka.ms/dotnet/net6/dev/Runtime/dotnet-runtime-x64.deb
[deb-sharedfx-checksum-6.0.X-coreclr]: https://aka.ms/dotnet/net6/dev/Runtime/dotnet-runtime-x64.deb.sha512
[deb-targeting-pack-6.0.X-coreclr]: https://aka.ms/dotnet/net6/dev/Runtime/dotnet-targeting-pack-x64.deb
[deb-targeting-pack-checksum-6.0.X-coreclr]: https://aka.ms/dotnet/net6/dev/Runtime/dotnet-targeting-pack-x64.deb.sha512

[rhel7-badge-6.0.X-coreclr]: https://aka.ms/dotnet/net6/dev/Runtime/sharedfx_rhel.7-x64_Release_version_badge.svg
[rhel7-version-6.0.X-coreclr]: https://aka.ms/dotnet/net6/dev/Runtime/productVersion.txt
[rhel7-runtime-deps-checksum-6.0.X-coreclr]: https://aka.ms/dotnet/net6/dev/Runtime/dotnet-runtime-deps-centos.7-x64.rpm
[rhel7-runtime-deps-checksum-6.0.X-coreclr]: https://aka.ms/dotnet/net6/dev/Runtime/dotnet-runtime-deps-centos.7-x64.rpm.sha512
[rhel7-apphost-pack-6.0.X-coreclr]: https://aka.ms/dotnet/net6/dev/Runtime/dotnet-apphost-pack-x64.rpm
[rhel7-apphost-pack-checksum-6.0.X-coreclr]: https://aka.ms/dotnet/net6/dev/Runtime/dotnet-apphost-pack-x64.rpm.sha512
[rhel7-host-6.0.X-coreclr]: https://aka.ms/dotnet/net6/dev/Runtime/dotnet-host-x64.rpm
[rhel7-host-checksum-6.0.X-coreclr]: https://aka.ms/dotnet/net6/dev/Runtime/dotnet-host-x64.rpm.sha512
[rhel7-hostfxr-6.0.X-coreclr]: https://aka.ms/dotnet/net6/dev/Runtime/dotnet-hostfxr-x64.rpm
[rhel7-hostfxr-checksum-6.0.X-coreclr]: https://aka.ms/dotnet/net6/dev/Runtime/dotnet-hostfxr-x64.rpm.sha512
[rhel7-sharedfx-6.0.X-coreclr]: https://aka.ms/dotnet/net6/dev/Runtime/dotnet-runtime-x64.rpm
[rhel7-sharedfx-checksum-6.0.X-coreclr]: https://aka.ms/dotnet/net6/dev/Runtime/dotnet-runtime-x64.rpm.sha512
[rhel7-targeting-pack-6.0.X-coreclr]: https://aka.ms/dotnet/net6/dev/Runtime/dotnet-targeting-pack-x64.rpm
[rhel7-targeting-pack-checksum-6.0.X-coreclr]: https://aka.ms/dotnet/net6/dev/Runtime/dotnet-targeting-pack-x64.rpm.sha512

[centos-7-badge-6.0.X-coreclr]: https://aka.ms/dotnet/net6/dev/Runtime/sharedfx_centos.7-x64_Release_version_badge.svg
[centos-7-version-6.0.X-coreclr]: https://aka.ms/dotnet/net6/dev/Runtime/productVersion.txt
[centos-7-runtime-deps-6.0.X-coreclr]: https://aka.ms/dotnet/net6/dev/Runtime/dotnet-runtime-deps-centos.7-x64.rpm
[centos-7-runtime-deps-checksum-6.0.X-coreclr]: https://aka.ms/dotnet/net6/dev/Runtime/dotnet-runtime-deps-centos.7-x64.rpm.sha512
[centos-7-apphost-pack-6.0.X-coreclr]: https://aka.ms/dotnet/net6/dev/Runtime/dotnet-apphost-pack-x64.rpm
[centos-7-apphost-pack-checksum-6.0.X-coreclr]: https://aka.ms/dotnet/net6/dev/Runtime/dotnet-apphost-pack-x64.rpm.sha512
[centos-7-host-6.0.X-coreclr]: https://aka.ms/dotnet/net6/dev/Runtime/dotnet-host-x64.rpm
[centos-7-host-checksum-6.0.X-coreclr]: https://aka.ms/dotnet/net6/dev/Runtime/dotnet-host-x64.rpm.sha512
[centos-7-hostfxr-6.0.X-coreclr]: https://aka.ms/dotnet/net6/dev/Runtime/dotnet-hostfxr-x64.rpm
[centos-7-hostfxr-checksum-6.0.X-coreclr]: https://aka.ms/dotnet/net6/dev/Runtime/dotnet-hostfxr-x64.rpm.sha512
[centos-7-sharedfx-6.0.X-coreclr]: https://aka.ms/dotnet/net6/dev/Runtime/dotnet-runtime-x64.rpm
[centos-7-sharedfx-checksum-6.0.X-coreclr]: https://aka.ms/dotnet/net6/dev/Runtime/dotnet-runtime-x64.rpm.sha512
[centos-7-targeting-pack-6.0.X-coreclr]: https://aka.ms/dotnet/net6/dev/Runtime/dotnet-targeting-pack-x64.rpm
[centos-7-targeting-pack-checksum-6.0.X-coreclr]: https://aka.ms/dotnet/net6/dev/Runtime/dotnet-targeting-pack-x64.rpm.sha512

[fedora-27-badge-6.0.X-coreclr]: https://aka.ms/dotnet/net6/dev/Runtime/sharedfx_fedora.27-x64_Release_version_badge.svg
[fedora-27-version-6.0.X-coreclr]: https://aka.ms/dotnet/net6/dev/Runtime/productVersion.txt
[fedora-27-runtime-deps-6.0.X-coreclr]: https://aka.ms/dotnet/net6/dev/Runtime/dotnet-runtime-deps-fedora.27-x64.rpm
[fedora-27-runtime-deps-checksum-6.0.X-coreclr]: https://aka.ms/dotnet/net6/dev/Runtime/dotnet-runtime-deps-fedora.27-x64.rpm.sha512
[fedora-27-apphost-pack-6.0.X-coreclr]: https://aka.ms/dotnet/net6/dev/Runtime/dotnet-apphost-pack-x64.rpm
[fedora-27-apphost-pack-checksum-6.0.X-coreclr]: https://aka.ms/dotnet/net6/dev/Runtime/dotnet-apphost-pack-x64.rpm.sha512
[fedora-27-host-6.0.X-coreclr]: https://aka.ms/dotnet/net6/dev/Runtime/dotnet-host-x64.rpm
[fedora-27-host-checksum-6.0.X-coreclr]: https://aka.ms/dotnet/net6/dev/Runtime/dotnet-host-x64.rpm.sha512
[fedora-27-hostfxr-6.0.X-coreclr]: https://aka.ms/dotnet/net6/dev/Runtime/dotnet-hostfxr-x64.rpm
[fedora-27-hostfxr-checksum-6.0.X-coreclr]: https://aka.ms/dotnet/net6/dev/Runtime/dotnet-hostfxr-x64.rpm.sha512
[fedora-27-sharedfx-6.0.X-coreclr]: https://aka.ms/dotnet/net6/dev/Runtime/dotnet-runtime-x64.rpm
[fedora-27-sharedfx-checksum-6.0.X-coreclr]: https://aka.ms/dotnet/net6/dev/Runtime/dotnet-runtime-x64.rpm.sha512
[fedora-27-targeting-pack-6.0.X-coreclr]: https://aka.ms/dotnet/net6/dev/Runtime/dotnet-targeting-pack-x64.rpm
[fedora-27-targeting-pack-checksum-6.0.X-coreclr]: https://aka.ms/dotnet/net6/dev/Runtime/dotnet-targeting-pack-x64.rpm.sha512

[sles-12-badge-6.0.X-coreclr]: https://aka.ms/dotnet/net6/dev/Runtime/sharedfx_sles.12-x64_Release_version_badge.svg
[sles-12-version-6.0.X-coreclr]: https://aka.ms/dotnet/net6/dev/Runtime/productVersion.txt
[sles-12-runtime-deps-6.0.X-coreclr]: https://aka.ms/dotnet/net6/dev/Runtime/dotnet-runtime-deps-sles.12-x64.rpm
[sles-12-runtime-deps-checksum-6.0.X-coreclr]: https://aka.ms/dotnet/net6/dev/Runtime/dotnet-runtime-deps-sles.12-x64.rpm.sha512
[sles-12-apphost-pack-6.0.X-coreclr]: https://aka.ms/dotnet/net6/dev/Runtime/dotnet-apphost-pack-x64.rpm
[sles-12-apphost-pack-checksum-6.0.X-coreclr]: https://aka.ms/dotnet/net6/dev/Runtime/dotnet-apphost-pack-x64.rpm.sha512
[sles-12-host-6.0.X-coreclr]: https://aka.ms/dotnet/net6/dev/Runtime/dotnet-host-x64.rpm
[sles-12-host-checksum-6.0.X-coreclr]: https://aka.ms/dotnet/net6/dev/Runtime/dotnet-host-x64.rpm.sha512
[sles-12-hostfxr-6.0.X-coreclr]: https://aka.ms/dotnet/net6/dev/Runtime/dotnet-hostfxr-x64.rpm
[sles-12-hostfxr-checksum-6.0.X-coreclr]: https://aka.ms/dotnet/net6/dev/Runtime/dotnet-hostfxr-x64.rpm.sha512
[sles-12-sharedfx-6.0.X-coreclr]: https://aka.ms/dotnet/net6/dev/Runtime/dotnet-runtime-x64.rpm
[sles-12-sharedfx-checksum-6.0.X-coreclr]: https://aka.ms/dotnet/net6/dev/Runtime/dotnet-runtime-x64.rpm.sha512
[sles-12-targeting-pack-6.0.X-coreclr]: https://aka.ms/dotnet/net6/dev/Runtime/dotnet-targeting-pack-x64.rpm
[sles-12-targeting-pack-checksum-6.0.X-coreclr]: https://aka.ms/dotnet/net6/dev/Runtime/dotnet-targeting-pack-x64.rpm.sha512

[OpenSUSE-42-badge-6.0.X-coreclr]: https://aka.ms/dotnet/net6/dev/Runtime/sharedfx_opensuse.42-x64_Release_version_badge.svg
[OpenSUSE-42-version-6.0.X-coreclr]: https://aka.ms/dotnet/net6/dev/Runtime/productVersion.txt
[OpenSUSE-42-runtime-deps-6.0.X-coreclr]: https://aka.ms/dotnet/net6/dev/Runtime/dotnet-runtime-deps-opensuse.42-x64.rpm
[OpenSUSE-42-runtime-deps-checksum-6.0.X-coreclr]: https://aka.ms/dotnet/net6/dev/Runtime/dotnet-runtime-deps-opensuse.42-x64.rpm.sha512
[OpenSUSE-42-apphost-pack-6.0.X-coreclr]: https://aka.ms/dotnet/net6/dev/Runtime/dotnet-apphost-pack-x64.rpm
[OpenSUSE-42-apphost-pack-checksum-6.0.X-coreclr]: https://aka.ms/dotnet/net6/dev/Runtime/dotnet-apphost-pack-x64.rpm.sha512
[OpenSUSE-42-host-6.0.X-coreclr]: https://aka.ms/dotnet/net6/dev/Runtime/dotnet-host-x64.rpm
[OpenSUSE-42-host-checksum-6.0.X-coreclr]: https://aka.ms/dotnet/net6/dev/Runtime/dotnet-host-x64.rpm.sha512
[OpenSUSE-42-hostfxr-6.0.X-coreclr]: https://aka.ms/dotnet/net6/dev/Runtime/dotnet-hostfxr-x64.rpm
[OpenSUSE-42-hostfxr-checksum-6.0.X-coreclr]: https://aka.ms/dotnet/net6/dev/Runtime/dotnet-hostfxr-x64.rpm.sha512
[OpenSUSE-42-sharedfx-6.0.X-coreclr]: https://aka.ms/dotnet/net6/dev/Runtime/dotnet-runtime-x64.rpm
[OpenSUSE-42-sharedfx-checksum-6.0.X-coreclr]: https://aka.ms/dotnet/net6/dev/Runtime/dotnet-runtime-x64.rpm.sha512
[OpenSUSE-42-targeting-pack-6.0.X-coreclr]: https://aka.ms/dotnet/net6/dev/Runtime/dotnet-targeting-pack-x64.rpm
[OpenSUSE-42-targeting-pack-checksum-6.0.X-coreclr]: https://aka.ms/dotnet/net6/dev/Runtime/dotnet-targeting-pack-x64.rpm.sha512

[linux-musl-x64-badge-6.0.X-coreclr]: https://aka.ms/dotnet/net6/dev/Runtime/sharedfx_linux-musl-x64_Release_version_badge.svg
[linux-musl-x64-version-6.0.X-coreclr]: https://aka.ms/dotnet/net6/dev/Runtime/productVersion.txt
[linux-musl-x64-targz-6.0.X-coreclr]: https://aka.ms/dotnet/net6/dev/Runtime/dotnet-runtime-linux-musl-x64.tar.gz
[linux-musl-x64-targz-checksum-6.0.X-coreclr]: https://aka.ms/dotnet/net6/dev/Runtime/dotnet-runtime-linux-musl-x64.tar.gz.sha512
[linux-musl-x64-nethost-targz-6.0.X-coreclr]: https://aka.ms/dotnet/net6/dev/Runtime/dotnet-nethost-linux-musl-x64.tar.gz
[linux-musl-x64-symbols-targz-6.0.X-coreclr]: https://aka.ms/dotnet/net6/dev/Runtime/dotnet-runtime-symbols-linux-musl-x64.tar.gz

[linux-musl-arm-badge-6.0.X-coreclr]: https://aka.ms/dotnet/net6/dev/Runtime/sharedfx_linux-musl-arm_Release_version_badge.svg
[linux-musl-arm-version-6.0.X-coreclr]: https://aka.ms/dotnet/net6/dev/Runtime/productVersion.txt
[linux-musl-arm-targz-6.0.X-coreclr]: https://aka.ms/dotnet/net6/dev/Runtime/dotnet-runtime-linux-musl-arm.tar.gz
[linux-musl-arm-targz-checksum-6.0.X-coreclr]: https://aka.ms/dotnet/net6/dev/Runtime/dotnet-runtime-linux-musl-arm.tar.gz.sha512
[linux-musl-arm-nethost-targz-6.0.X-coreclr]: https://aka.ms/dotnet/net6/dev/Runtime/dotnet-nethost-linux-musl-arm.tar.gz
[linux-musl-arm-symbols-targz-6.0.X-coreclr]: https://aka.ms/dotnet/net6/dev/Runtime/dotnet-runtime-symbols-linux-musl-arm.tar.gz

[linux-musl-arm64-badge-6.0.X-coreclr]: https://aka.ms/dotnet/net6/dev/Runtime/sharedfx_linux-musl-arm64_Release_version_badge.svg
[linux-musl-arm64-version-6.0.X-coreclr]: https://aka.ms/dotnet/net6/dev/Runtime/productVersion.txt
[linux-musl-arm64-targz-6.0.X-coreclr]: https://aka.ms/dotnet/net6/dev/Runtime/dotnet-runtime-linux-musl-arm64.tar.gz
[linux-musl-arm64-targz-checksum-6.0.X-coreclr]: https://aka.ms/dotnet/net6/dev/Runtime/dotnet-runtime-linux-musl-arm64.tar.gz.sha512
[linux-musl-arm64-nethost-targz-6.0.X-coreclr]: https://aka.ms/dotnet/net6/dev/Runtime/dotnet-nethost-linux-musl-arm64.tar.gz
[linux-musl-arm64-symbols-targz-6.0.X-coreclr]: https://aka.ms/dotnet/net6/dev/Runtime/dotnet-runtime-symbols-linux-musl-arm64.tar.gz

<!-- END links to include in table -->
