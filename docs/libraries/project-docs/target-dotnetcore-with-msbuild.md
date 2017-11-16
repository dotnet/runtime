Using MSBuild to build .NET Core projects
=========================================

The .NET Core tooling is going to [move from project.json to MSBuild based projects](https://blogs.msdn.microsoft.com/dotnet/2016/05/23/changes-to-project-json/).
We expect the first version of the .NET Core tools that use MSBuild to ship along with Visual Studio "15".  However, it is possible to use MSBuild for .NET Core
projects today, and this page shows how.

We recommend that most people targeting .NET Core with *new* projects today use the default tooling experience with project.json.  This is because we haven't yet added
support to MSBuild for a lot of the benefits that project.json has, because a lot of the ASP.NET based tooling will not work with MSBuild today, and because when we
do release .NET Core tooling which uses MSBuild, it will be able to automatically convert from project.json to MSBuild based projects.

You may want to use MSBuild to target .NET Core for existing projects that already use MSBuild that you want to port to .NET Core, or if you are using
MSBuild's extensibility in your build for scenarios that are not well supported for project.json projects.

Prerequisites
=============

- [Visual Studio 2015 Update 3 RC](https://www.visualstudio.com/downloads/visual-studio-prerelease-downloads#sec1) or higher
- [.NET Core tools for Visual Studio](https://www.visualstudio.com/downloads/download-visual-studio-vs)
- NuGet Visual Studio extension [v3.5.0-beta](https://dist.nuget.org/visualstudio-2015-vsix/v3.5.0-beta/NuGet.Tools.vsix) or later

Creating a library targeting .NET Core
======================================

- File > New > Project > Class Library (Portable)

  ![New Project](https://dotnetdocs.blob.core.windows.net/getting-started/new-project.png)

- Select ".NET Framework 4.6" and "ASP.NET Core 1.0"

  ![Portable targets dialog](pcl-targets-dialog-net46-aspnetcore10.png)

- In the "Library" tab of the project properties, click on the "Target .NET Platform Standard" link, and click "Yes" in the dialog that is shown
- In the `project.json` file:
    - Change the version number of the `NETStandard.Library` package to `1.5.0-rc2-24027` (this is the .NET Core RC2 version of the package).
    - Add the below `imports` definition inside the `netstandard1.5` framework definition.  This will allow your project to reference .NET Core compatible
      NuGet packages that haven't been updated to target .NET Standard

        ```json
        "netstandard1.5": {
            "imports": [ "dnxcore50", "portable-net452" ]
        }
        ```

Creating a .NET Core console application
========================================
Building a console application for .NET Core requires some customization of the MSBuild build process.  A sample project for a .NET Core console application
is [CoreApp](https://github.com/dotnet/corefxlab/tree/master/samples/NetCoreSample/CoreApp) in the [corefxlab](https://github.com/dotnet/corefxlab) repo.
Another good option is to start with [coretemplate](https://github.com/mellinoe/coretemplate), which uses separate MSBuild targets files to target .NET Core
instead of putting the changes directly in the project file.  

It is also possible to start by creating a project in Visual Studio and modify it to target .NET Core.  The instructions below show the minimal steps to get this working.
In contrast to CoreApp or coretemplate, a project created this way won't include configurations for targeting Linux and Mac OS.

Creating a .NET Core console application from Visual Studio
===========================================================

- File > New > Project > Console Application
- In "Build" tab of the project properties, select "All Configurations" and change the "Platform Target" to "x64"
- Delete the `app.config` file from the project
- Add the following project.json file to the project:

    ```json
    {
        "dependencies": {
            "Microsoft.NETCore.App": "1.0.0-rc2-3002702"
        },
        "runtimes": {
            "win7-x64": { },
            "ubuntu.14.04-x64": { },
            "osx.10.10-x64": { }
        },
        "frameworks": {
            "netcoreapp1.0": {
                "imports": [ "dnxcore50", "portable-net452" ]
            }
        }
    }
    ```

- Open the project's XML for editing (in Visual Studio, right click on the project -> Unload Project, right click again -> Edit MyProj.csproj)
    - Remove all the default `Reference` items (to `System`, `System.Core`, etc.)
    - Add the following properties to the first `PropertyGroup` in the project:

        ```xml
        <TargetFrameworkIdentifier>.NETCoreApp</TargetFrameworkIdentifier>
        <TargetFrameworkVersion>v1.0</TargetFrameworkVersion>
        <BaseNuGetRuntimeIdentifier>win7</BaseNuGetRuntimeIdentifier>
        <NoStdLib>true</NoStdLib>
        <NoWarn>$(NoWarn);1701</NoWarn>
        ```

    - Add the following at the end of the file (after the import of `Microsoft.Portable.CSharp.Targets`:

        ```xml
        <PropertyGroup>
            <!-- We don't use any of MSBuild's resolution logic for resolving the framework, so just set these two
                    properties to any folder that exists to skip the GetReferenceAssemblyPaths task (not target) and
                    to prevent it from outputting a warning (MSB3644).
                -->
            <_TargetFrameworkDirectories>$(MSBuildThisFileDirectory)</_TargetFrameworkDirectories>
            <_FullFrameworkReferenceAssemblyPaths>$(MSBuildThisFileDirectory)</_FullFrameworkReferenceAssemblyPaths>

            <!-- MSBuild thinks all EXEs need binding redirects, not so for CoreCLR! -->
            <AutoUnifyAssemblyReferences>true</AutoUnifyAssemblyReferences>
            <AutoGenerateBindingRedirects>false</AutoGenerateBindingRedirects>

            <!-- Set up debug options to run with host, and to use the CoreCLR debug engine -->
            <StartAction>Program</StartAction>
            <StartProgram>$(TargetDir)dotnet.exe</StartProgram>
            <StartArguments>$(TargetPath)</StartArguments>
            <DebugEngines>{2E36F1D4-B23C-435D-AB41-18E608940038}</DebugEngines>
        </PropertyGroup>
        ```

    - Close the .csproj file, and reload the project in Visual Studio

- You should be able to run your program with F5 in Visual Studio, or from the command line in the output folder with `dotnet MyApp.exe` 