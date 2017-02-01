# How to patch dotnet CLI with latest CoreFX

This document provides the steps necessary to consume a nightly build of CoreFX
and CoreCLR.

Please note that these steps aren't necessary for official builds -- these steps
are specific to consuming nightlies and thus unsupported builds. Also note that
these steps are likely to change as we're simplifying this experience. Make
sure to consult this document often.

## Install prerequisites

1. Install CLI 2.0.0-alpha SDK
    - [Win 64-bit Latest](https://dotnetcli.blob.core.windows.net/dotnet/Sdk/master/dotnet-dev-win-x64.latest.exe)
    - [macOS 64-bit Latest](https://dotnetcli.blob.core.windows.net/dotnet/Sdk/master/dotnet-dev-osx-x64.latest.pkg)
    - [Others](https://github.com/dotnet/cli/blob/master/README.md#installers-and-binaries)

## Setup the project

1. Create a new project
    - Create a new folder for your app
    - Ensure you have a "2.0.0-*" CLI installed on your path. You can check with `dotnet --info`.
    - Create project file by running `dotnet new`

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
