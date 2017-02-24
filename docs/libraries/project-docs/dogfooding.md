# How to get up and running on .NET Core 2.0

This document provides the steps necessary to consume a nightly build of
.NET Core 2.0 runtime and SDK.

Please note that these steps are likely to change as we're simplifying
this experience. Make sure to consult this document often.

## Install prerequisites

1. Install the latest nightly .NET Core SDK 2.0
    - [Win 64-bit Latest](https://dotnetcli.blob.core.windows.net/dotnet/Sdk/master/dotnet-dev-win-x64.latest.exe)
    - [macOS 64-bit Latest](https://dotnetcli.blob.core.windows.net/dotnet/Sdk/master/dotnet-dev-osx-x64.latest.pkg)
    - [Others](https://github.com/dotnet/cli/blob/master/README.md#installers-and-binaries)

## Setup the project

1. Create a new project
    - Create a new folder for your app
    - Ensure you have a "2.0.0-*" CLI installed on your path. You can check with `dotnet --info`.
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
