Getting Started Writing a .NET Core app and Class Library
=========================================================

These instructions are basic and a work in progress. They will be improving a lot over time.

Once you've followed the steps in this document, use the following sample to see the changes you need to make to your project: https://github.com/dotnet/corefxlab/tree/master/samples/ClassLib.

Installing the tools
====================

1. Install Visual Studio 2015 - [https://www.visualstudio.com/downloads/download-visual-studio-vs](https://www.visualstudio.com/downloads/download-visual-studio-vs)
2. Check "Universal Windows App Development Tools" in the initial install or modify the install to include it.

![VS Install](https://dotnetdocs.blob.core.windows.net/getting-started/vs-install.png)

3. Download the latest nuget.exe commandline client.  This is required until a new NuGet Visual Studio extension is available.  Browse to https://www.myget.org/gallery/nugetbuild and download the `NuGet.Commandline` package.  Rename the extension from `.nupkg` to `.zip` and extract nuget.exe from the tools folder.

4. Disable NuGet's built-in package restore since that won't be using the latest nuget.exe we just downloaded.  Goto Tools | NuGet Package Manager | Package Manager Settings | General.

![Disable Restore](https://cloud.githubusercontent.com/assets/8228359/11126436/d3b9b9ca-8923-11e5-9de1-f6fcdc46ebbd.png)

5. Setup ".NET Core Dev Feed" package source -> "http://myget.org/F/dotnet-core". Goto Tools | NuGet Package Manager | Package Manager Settings | Package Sources.

![NuGet Feed](https://dotnetdocs.blob.core.windows.net/getting-started/nuget-feed.png)

Note: Eventually these packages will be available on nuget.org and so this feed will not be interesting for anyone that doesn't want daily dev builds from the corefx/coreclr repos.

Create a New Class Library
==========================

1. File > New

![New Project](https://dotnetdocs.blob.core.windows.net/getting-started/new-project.png)

2. Select ".NET Framework 4.6" and "ASP.NET Core 5.0"

![Portable](https://dotnetdocs.blob.core.windows.net/getting-started/portable.png)

3. At this point the project should compile

Add support for generations
---------------------------

Currently to enable generations you need do to some manual steps

1. Manually edit your csproj file by right clicking on the project and select unload and then right click and select edit.  Add this to the bottom of your project file:

  <PropertyGroup>
    <NuGetTargetMoniker>.NETPlatform,Version=v5.4</NuGetTargetMoniker>
  </PropertyGroup>

![Project Group](https://dotnetdocs.blob.core.windows.net/getting-started/project-group.png)

2. Save and close your project file and then reload it.

3. Open project.json file and change "dotnet" to "dotnet5.4".

![project.json](https://dotnetdocs.blob.core.windows.net/getting-started/project-json.png)
 
4. Save it.
5. At this point your project should be in a buildable state and targeting generation 5.4.

Update your packages to the latest
----------------------------------

1. Now if you want to update to the latest version of the meta-packages right click on references and chose "Manage Nuget Packages".
  a. Click "Updates" tab
  b. Check the "Include prerelease" check-box
  c. Select ".NET Core Dev Feed" as the package source.
  d. The NuGet Visual Studio add-in to support these latest packages is not yet available so we cannot install from this UI.  Instead, take note of the versions to update and do so manually in the project.json.
![NuGet Package Manager](https://dotnetdocs.blob.core.windows.net/getting-started/nuget-package-manager.png)
  ```
  "dependencies": {
    "Microsoft.NETCore": "5.0.1-beta-23506",
    "Microsoft.NETCore.Portable.Compatibility": "1.0.1-beta-23506"
  },
  ```

  e. Run `nuget.exe restore project.json` from the command line to download the new packages and write the package information to `project.lock.json`.
2. Your project should now be in a buildable state again with the latest version of our library packages.