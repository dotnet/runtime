Get the .NET Core DNX SDK on Windows
====================================

These instructions will lead you through acquiring the .NET Core DNX SDK via the [.NET Version Manager (DNVM)](https://github.com/aspnet/dnvm)  and running a "Hello World" demo on Windows. The instructions use a particular set of paths. You'll need to adjust if you want to use a different set.

These instructions are for .NET Core console apps. If you want to try out ASP.NET 5 on top of .NET Core - which is a great idea - check out the [ASP.NET 5 instructions](https://github.com/aspnet/home).

.NET Core NuGet packages and the .NET Core DNX SDKs are available on the [ASP.NET 'vnext' myget feed](https://www.myget.org/F/aspnetvnext), which you can more easily view on [gallery](https://www.myget.org/gallery/aspnetvnext) for the feed.

You can also acquire .NET Core directly via [NuGet restore](get-dotnetcore-windows.md) or [build from source](../building/windows-instructions.md). 

Installing DNVM
===============

You need DNVM as a starting point. DNVM enables you to acquire a (or multiple) .NET Execution Environment (DNX). DNVM is simply a script, which doesn't depend on .NET. You can install it via a PowerShell command. You can find alternate DNVM install instructions at the [ASP.NET Home repo](https://github.com/aspnet/home).

	C:\coreclr-demo> @powershell -NoProfile -ExecutionPolicy unrestricted -Command "&{$Branch='dev';iex ((new-object net.webclient).DownloadString('https://raw.githubusercontent.com/aspnet/Home/dev/dnvminstall.ps1'))}"

You must close your command-prompt and start a new one in order for the user-wide environment variables to take effect.

You can see the currently installed DNX versions with `dnvm list`, which will display an empty set of installed runtimes.

	C:\coreclr-demo> dnvm list

Installing a .NET Core DNX
==========================

It's easy to install the latest .NET Core-based DNX, using the `dnvm install` command. The `-u` (or `-Unstable`) parameter installs latest unstable version.

	C:\coreclr-demo> dnvm install -r coreclr latest -u

This will install the 32-bit version of .NET Core. If you want the 64-bit version, you can specify processor architecture:

	C:\coreclr-demo> dnvm install -r coreclr -arch x64 latest -u

You can see the currently installed DNX versions with `dnvm list` (your display may vary as new versions of the DNX are published):

	C:\coreclr-demo>dnvm list

```
Active Version           Runtime Architecture Location                       Alias
------ -------           ------- ------------ --------                       -----
        1.0.0-beta7-12364 coreclr x86          C:\Users\rlander\.dnx\runtimes
        1.0.0-beta7-12364 coreclr x64          C:\Users\rlander\.dnx\runtimes
```

You can choose which of these DNXs you want to use with `dnvm use`, with similar arguments.

```
C:\coreclr-demo>dnvm use -r coreclr -arch x86 1.0.0-beta7-12364
Adding C:\Users\rlander\.dnx\runtimes\dnx-coreclr-win-x86.1.0.0-beta7-12364\bin
to process PATH

C:\coreclr-demo>dnvm list

Active Version           Runtime Architecture Location                       Alias
------ -------           ------- ------------ --------                       -----
   *    1.0.0-beta7-12364 coreclr x86          C:\Users\rlander\.dnx\runtimes
        1.0.0-beta7-12364 coreclr x64          C:\Users\rlander\.dnx\runtimes
```

Write your App
==============

You need a Hello World application to run. You can write your own, if you'd like. Here's a very simple one:

```csharp
using System;

public class Program
{
    public static void Main (string[] args)
    {
        Console.WriteLine("Hello, Windows");
        Console.WriteLine("Love from CoreCLR.");
    }
}
```

Some people on the .NET Core team are partial to a demo console app on corefxlab repo which will print a picture for you. Download the [corefxlab demo](https://raw.githubusercontent.com/dotnet/corefxlab/master/demos/CoreClrConsoleApplications/HelloWorld/HelloWorld.cs) to `C:\coreclr-demo`.

You need a `project.json` that matches your app. Use this one. It will work for both of the apps provided/referenced above. Save the project.json beside your app.

```
{
    "version": "1.0.0-*",
    "dependencies": {
    },
    "frameworks" : {
        "dnx451" : { },
        "dnxcore50" : {
            "dependencies": {
                "System.Console": "4.0.0-beta-*"
            }
        }
    }
}
```

Run your App
============

You need to restore packages for your app, based on your project.json, with `dnu restore`.

	C:\coreclr-demo> dnu restore

You can run your app with the DNX command.

	C:\coreclr-demo> dnx run
	Hello, Windows
	Love from CoreCLR.
