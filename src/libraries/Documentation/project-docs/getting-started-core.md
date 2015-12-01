Getting Started Writing a .NET Core app and Class Library
=========================================================

These instructions are basic and a work in progress. They will be improving a lot over time. These instructions are current for Visual Studio 2015 Update 1.

Once you've followed the steps in this document, use the following sample to see the changes you need to make to your project: https://github.com/dotnet/corefxlab/tree/master/samples/NetCoreSample.

Installing the tools
====================

1. Install Visual Studio 2015 - [https://www.visualstudio.com/downloads/download-visual-studio-vs](https://www.visualstudio.com/downloads/download-visual-studio-vs)
<br>Ensure you're running Visual Studio 2015 Update 1 (in Help->About in Visual Studio, you should see version 14.0.24020.00 or higher. If not, update in Tools->Extensions and Updates.)
2. Check "Universal Windows App Development Tools" in the initial install or modify the install to include it.

![VS Install](https://dotnetdocs.blob.core.windows.net/getting-started/vs-install.png)

Create a New Class Library
==========================

1. File > New

![New Project](https://dotnetdocs.blob.core.windows.net/getting-started/new-project.png)

2. Select ".NET Framework 4.6" and "ASP.NET Core 5.0"

![Portable](https://dotnetdocs.blob.core.windows.net/getting-started/portable.png)

3. At this point the project should compile

Add support for the Platform Standard
---------------------------

Currently to enable leveraging packages that target the Platform Standard you need do to some manual steps

1. Manually edit your csproj file by right clicking on the project and select unload and then right click and select edit. Remove this line:
```
  <Import Project="$(MSBuildToolsPath)\Microsoft.CSharp.targets" />
```
And replace it with this:
```
  <Import Project="$(MSBuildExtensionsPath32)\Microsoft\Portable\$(TargetFrameworkVersion)\Microsoft.Portable.CSharp.targets" />
```
Add this at the end of the file, before the closing Project tag:
```
  <PropertyGroup>
    <NuGetTargetMoniker>.NETPlatform,Version=v5.4</NuGetTargetMoniker>
  </PropertyGroup>
```

2. Save and close your project file and then reload it.

3. Open project.json file and change "dotnet" to "dotnet5.4".

![project.json](https://dotnetdocs.blob.core.windows.net/getting-started/project-json.png)
 
4. Save it.
5. At this point your project should be in a buildable state and targeting generation 5.4.

Update your packages to the latest
----------------------------------
To update to the RC1 packages, adjust your project.json as follows:
  ```
  "dependencies": {
    "Microsoft.NETCore": "5.0.1-beta-23516",
    "Microsoft.NETCore.Portable.Compatibility": "1.0.1-beta-23516"
  },
  ```
To trigger the update to the latest version of the meta-packages right click on references and chose "Manage Nuget Packages":
  1. Click "Updates" tab
  2. Check the "Include prerelease" check-box
  3. Click the checkboxes for the packages, and click "Update"
  
![NuGet Package Manager](https://dotnetdocs.blob.core.windows.net/getting-started/nuget-package-manager.png)


Advanced: Updating and using nuget.exe
======================================

Future changes to the CoreCLR/CoreFX packages may require newer versions of Nuget than are currently in Visual Studio. In that case, you will need to follow these directions:

1. Download the latest nuget.exe commandline client.  This is required until a new NuGet Visual Studio extension is available.  Browse to https://www.myget.org/gallery/nugetbuild and download the `NuGet.Commandline` package.  Rename the extension from `.nupkg` to `.zip` and extract nuget.exe from the tools folder.
2. Disable NuGet's built-in package restore since that won't be using the latest nuget.exe we just downloaded.  Goto Tools | NuGet Package Manager | Package Manager Settings | General.
![Disable Restore](https://cloud.githubusercontent.com/assets/8228359/11126436/d3b9b9ca-8923-11e5-9de1-f6fcdc46ebbd.png)
3. Setup ".NET Core Dev Feed" package source -> "http://myget.org/F/dotnet-core". Goto Tools | NuGet Package Manager | Package Manager Settings | Package Sources.
![NuGet Feed](https://dotnetdocs.blob.core.windows.net/getting-started/nuget-feed.png)
4. In the Nuget Package Manager, above, follow the same steps, choosing '.NET Core Dev Feed' instead of the normal Nuget.org feed.
5. Run 'nuget.exe restore project.lock.json' 
