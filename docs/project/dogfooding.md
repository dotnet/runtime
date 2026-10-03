# Dogfooding daily builds of .NET

This document provides the steps necessary to consume a latest development build of .NET runtime and SDK.
Example below is for 11.0.

## Obtaining daily builds of NuGet packages

If you are only looking to get fixes for an individual NuGet package, and don't need a preview version of the entire runtime, you can add the development package feed to your `NuGet.config` file.  The easiest way to do this is by using the dotnet CLI:

**(Recommended)** Create a local NuGet.Config file for your solution, if don't already have one.  Using a local NuGet.Config file will enable the development feed as a package source for projects in the current directory only.
```
dotnet new nugetconfig
```

Next, add the package source to NuGet.Config with the [dotnet nuget add source](https://learn.microsoft.com/dotnet/core/tools/dotnet-nuget-add-source) command:
```
dotnet nuget add source -n dotnet11 https://dnceng.pkgs.visualstudio.com/public/_packaging/dotnet11/nuget/v3/index.json
```

Then, you will be able to add the latest prerelease version of the desired package to your project.

**Example:** To add version 11.0-alpha.1.24556.8 of the System.Data.OleDb package, use the [dotnet add package](https://learn.microsoft.com/dotnet/core/tools/dotnet-add-package) command:
```
dotnet add package System.Data.OleDb -v 11.0-alpha.1.24556.8
```

To use daily builds of the entire runtime, follow the steps given in the rest of this document instead.

## Configuring upstream feeds in ADO

If you're using private Azure DevOps feeds for your projects, you might need to add the preview feed through the Azure-specific feed URI format, which is `azure-feed://organization/optionalProject/feed@view`. In this case, you can add the .NET development package feed as follows:

```
azure-feed://dnceng/public/dotnet11@Local
```

## Install prerequisites

1. Acquire the latest development .NET SDK by downloading and extracting a zip/tarball or using an installer from the [latest builds table in dotnet/sdk](https://github.com/dotnet/sdk#installing-the-sdk) (for example, https://aka.ms/dotnet/11.0/daily/dotnet-sdk-win-x64.zip).

2. If you are using a local copy of the dotnet CLI, take care that when you type `dotnet` you do not inadvertently pick up a different copy that you may have in your path. On Windows, for example, if you use a Command Prompt, a global copy may be in the path, so use the fully qualified path to your local `dotnet` (e.g. `C:\dotnet\dotnet.exe`). If you receive an error "error NETSDK1045:  The current .NET SDK does not support targeting .NET 11.0." then you may be executing an older `dotnet`.

After setting up dotnet you can verify you are using the current preview version by executing `dotnet --info`. Here is an example output:
```
>dotnet --info
.NET SDK:
 Version:   11.0.100-alpha.1.24556.7
 Commit:    fc127ac5a4

Runtime Environment:
 OS Name:     Windows
 OS Version:  10.0.22616
 OS Platform: Windows
 RID:         win-x64
 Base Path:   C:\Program Files\dotnet\sdk\11.0.100-alpha.1.24556.7\

global.json file:
  Not found

Host:
  Version:      11.0.0-alpha.1.24556.3
  Architecture: x64
  Commit:       47d9c43ab1

.NET SDKs installed:
  11.0.100-alpha.1.24556.7 [C:\Program Files\dotnet\sdk]

.NET runtimes installed:
  Microsoft.NETCore.App 11.0.0-alpha.1.24556.7 [C:\Program Files\dotnet\shared\Microsoft.NETCore.App]

Download .NET:
  https://aka.ms/dotnet-download

Learn about .NET Runtimes and SDKs:
  https://aka.ms/dotnet/runtimes-sdk-info

```

3. Our daily builds are uploaded to development feed, not NuGet - so ensure the development feed is in your nuget configuration in case you need other packages that aren't included in the download. For example, on Windows you could edit `%userprofile%\appdata\roaming\nuget\nuget.config` or on Linux edit `~/.nuget/NuGet/NuGet.Config` to add these lines:
```xml
<packageSources>
    <add key="dotnet11" value="https://dnceng.pkgs.visualstudio.com/public/_packaging/dotnet11/nuget/v3/index.json" />
    ...
</packageSources>
```
(Documentation for configuring feeds is [here](https://learn.microsoft.com/nuget/consume-packages/configuring-nuget-behavior).)

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
