# .NET Core - Shared Package Store

## Introduction
To enable sharing of assemblies among all machine-wide .NET Core applications, a centralized shared assembly store is needed. It enables applications to be trimmed of the shared assemblies and frees the apps from carrying them. This document will describe such an assembly store and its lookup that applications can take advantage of during development and their deployment. It also describes commands that will augment `dotnet` to compose shared package store entries and publishing mechanisms through which apps can filter assemblies to reduce disk usage.

### Packages Store

The package store can be either a global system-wide folder or a dotnet.exe relative folder:

+ **Global**:
    - The `dotnet` root location -- on Windows, the folder is located in `C:\Program Files (x86)\`. See layout below.
    - In the `store/install` folder we expect packages to be installed ONLY through platform installers like MSI, pkg, deb, apt-get etc.
    - The package layout composed with the `dotnet store` command (details follow) are expected to be unzipped directly into the `store` folder. Note, that this unzip step is a manual action.

```
    - dotnet.exe
    - shared
        - netcoreapp2.0
            + 2.0.0-preview2-00001
    - store
        - install
            + refs
            + netcoreapp2.0
            + netcoreapp2.1
        + refs
        + netcoreapp2.0
        + netcoreapp2.1  
```

The layout within `netcoreapp*` folders is a NuGet cache layout.


### Composing a runtime (non-ref) package store

To compose the layout of the shared package store, we will use a dotnet command called `dotnet store`. We expect the *hosting providers* (ex: Antares) to use the command to prime their machines and framework authors who want to provide *pre-optimized package archives* create the compressed archive layouts.

The layout is composed from a list of package names and versions specified as xml: 

**Roslyn Example**
```xml
<Project Sdk="Microsoft.NET.Sdk">
 <ItemGroup>
    <PackageReference Include="Microsoft.CodeAnalysis" Version="1.3.2" />
    <PackageReference Include="Microsoft.CodeAnalysis.VisualBasic" Version="1.3.2" />
    <PackageReference Include=""Microsoft.CodeAnalysis.VisualBasic.Features" Version="1.3.2" />
  </ItemGroup>
</Project>
```


and issue a command like below:

```
dotnet store --manifest packages.xml --framework netcoreapp2.0 [--output C:\Foo] --runtime win7-x64 --framework-version 2.0.0-preview2-00001 [--no-optimize]

--framework          Specifies the TFM that the package store is applicable to
--output       The output directory to create the package store in (default: %USERPROFILE%\.dotnet or ~/.dotnet)

--skip-optimization  Do not perform crossgen of the assemblies after "restore" (optimize is the default)

--runtime          The runtime identifier of the target platform where these assemblies will be run
--framework-version   The Microsoft.NETCore.App package version that will be used to run the assemblies

```
NOTE: It is a requirement that `packages.xml` is of msbuild format, because it forms the entry point from which the rest of the SDK's functionality can be accessed

Hosting providers would create a `packages.xml` file corresponding to the packages that will be shared in their hosting environment and specify the file to `dotnet store`. The file can be on the file system or from an URL. The TFM argument is used in the shared package layout described above.

If `--optimize` is specified, we would precompile all the managed assets to native code in a temp folder before copying to the output folder. If crossgen is used, it would be the one acquired in the closure of the `Microsoft.NETCore.App` specified by the `--framework-version` option. Also, if no `--output` folder is specified, then the default is `~/.dotnet` or `%USERPROFILE%\.dotnet\`. The output asset files will be present in the following layout: `$HOME/.dotnet/packages/{tfm}/{package-name}/{package-version}/{asset-path}`.

The output folder will be consumed by the runtime by adding to the `DOTNET_SHARED_STORE` environment variable. See probe precedence below.

# Building apps with shared packages

The current mechanism to build applications that share assemblies is by not specifying a RID in the project file. Then, a portable app model is assumed and assemblies that are part of Microsoft.NETCore.App are found under the `dotnet` install root. With shared package store, applications have the ability to filter any set of packages from their publish output. Thus the decision of a portable or a standalone application is not made at the time of project authoring but is instead done at publish time. 

## Project Authoring
We will by default treat `Microsoft.NETCore.App` as though `type: platform` is always specified, thus requiring no explicit RID specification by the user. It will be an `ERROR` to specify a RID in the csproj file using the `<RuntimeIdentifier/>` tag.

## dotnet restore

Because RIDs are not available until publish time, the application is treated as though it is a present day portable app and a regular restore is performed.

## dotnet build

`dotnet build` should treat any project as though it is a portable app and produce `runtimeconfig.json` with the `Microsoft.NETCore.App` as the framework. In addition, the `runtimeconfig.json` file should also specify the TFM field as:

```json
"runtimeOptions": {
    "tfm": "netcoreapp2.0",
    "framework": {
        "version": "2.0.0",
        "name": "Microsoft.NETCore.App"
    }
}
```
Note that this is different from current behavior of `dotnet run` for an application that specifies the `<RuntimeIdentifier/>` tag in the csproj.
**Current Behavior:** Picks `M.N.A` assemblies out of the NuGet cache without taking advantage of optimizations available from the shared `Microsoft.NETCore.App`.
**New Behavior:** Picks `M.N.A` assemblies from the shared framework and the rest of them from the shared package store or the NuGet cache.

`dotnet build` can take advantage of the `refs` folder available at the `store/install/` folder from the `dotnet` root directory enabling the offline-restore-build scenario. Although we are designing to augment `dotnet build` in the future regarding the reference assemblies, for the scope of this work we'll focus only on runtime assemblies.

## Host probe precedence

The host will probe in the order described in [host-probing](host-probing.md) for `dotnet run` and application activations post `dotnet publish`.

## dotnet publish

Publish will be enhanced to support a filter profile file specified as xml. This file explicitly lists all asset packages that need to be trimmed out of the publish output. The following are examples of how various application types can be published.

Publish a portable app:

```dotnet publish```

Publish a standalone app for the current RID:

```dotnet publish --standalone```

Publish a standalone app for win7-x64 filtering out the publish profile:

```dotnet publish --runtime win7-x64 filter https://asp.net/core/dev/1.2.0/profile.xml```

Publishing a portable app for win7-x64 filtering out the publish profile:

```dotnet publish filter https://asp.net/core/dev/1.2.0/profile.xml```

Publishing a Windows level portable application (a.k.a rid-specific portable app) i.e., filter all non-windows RIDs but split the bin directory on windows RIDs (win8, win7 and win10, for example):

```dotnet publish filter https://asp.net/core/dev/1.2.0/win.profile.xml```

To chain multiple profiles, specify `filter` multiple times.

Note that the `profile.xml` specifies exact RID-specific or IL packages to filter out of `dotnet publish` output -- i.e., the physical files will be filtered out of the `dotnet publish` output. The `deps.json` (logical assets) file will still contain the entries specified in the `profile.xml` file.

## Scenarios

### ASP .NET
+ If authoring shared package installers (eager cache):
    - Start from a clean directory and a list of packages in `packages.xml` file.
    - Use `dotnet store` to produce the layout in the directory.
    - Make MSI/pkg/deb and zips of this layout.
    - Publish the `profile.xml` file that users can use in sync with the installers.
    - Developer/Deployment-admin installs the MSIs/zips to the deployment machines.
+ If letting app deployers cache shared packages (lazy cache):
    - Publish `packages.xml` file that can be used to perform `dotnet store`.
    - Publish `profile.xml` file that can perform publish filtering.
    - Deployment-admin issues `dotnet store` when running the app.
+ Developer/Deployment-admin issues `dotnet publish filter profile.xml` to produce an ASP.NET app without containing shared components.
   - Or developer/Deployment-admin issues `dotnet run` after installing MSIs or zips.

### Antares
+ Antares produces the layout using a list of packages and `dotnet store` in a folder.
+ This folder is then chained into environment variable: `DOTNET_SHARED_STORE`.
+ When building app from source, issue `dotnet run` to pick up the shared packages.
+ When publishing an app to run, issue `dotnet publish filter profile.xml` with Antares profile.

### Roslyn
+ Roslyn packages are part of the packages directory
+ Apps that use Roslyn must explicitly take a dependency on Roslyn and not through M.N.A.
+ Apps can choose to skip publishing these DLLs using the Roslyn profile file that we'd publish.
+ Apps are deployed on another machine where the runtime is installed.
+ Apps will run as Roslyn packages are part of the runtime installer.

### Hosting Primers
+ I host apps that are already published
    - Use `dotnet store` and produce layout or unzip an earlier layout.
    - Set `DOTNET_SHARED_STORE` to point to layout.
    - Nature of publish:
        + Using my hosting profile
          + App publish directory doesn't contain the filtered files picked from layout.
        + Without my hosting profile
          + Assemblies from app publish directory are overridden (*status quo*)

+ I build user apps from source
    - dotnet store
    - Zip the layout
    - Deploy on hosting servers
    - Publish apps with filter

## Work Items

### Core-Setup

+ Change name of `Microsoft.NETCore.App` to `netcoreapp2.0` in the dotnet root. Note that this has to maintain compat.
+ Move the Roslyn assemblies out of the shared framework.
+ Build installers on Windows, Ubuntu and OSX for the Roslyn (shared) packages.
+ Implement host probing for the user and global package stores.
+ Incorporate changes in host for the TFM changes.

### CLI

+ Make CLI consume Roslyn dependencies out of the shared packages root as these are not part of M.N.A anymore.
+ Make `dotnet restore` restore as though project is `type: platform`.
+ Make `dotnet build` treat projects as though they are `type: platform`.
+ `dotnet publish filter` support.
+ `dotnet store` full implementation.


