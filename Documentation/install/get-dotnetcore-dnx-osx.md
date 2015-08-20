Get the .NET Core DNX SDK on OS X
=================================

These instructions will lead you through acquiring the .NET Core DNX SDK via the [.NET Version Manager (DNVM)](https://github.com/aspnet/dnvm)  and running a "Hello World" demo on OS X. The instructions use a particular set of paths. You'll need to adjust if you want to use a different set.

These instructions are for .NET Core console apps. If you want to try out ASP.NET 5 on top of .NET Core - which is a great idea - check out the [ASP.NET 5 instructions](https://github.com/aspnet/home).

.NET Core NuGet packages and the .NET Core DNX SDKs are available on the [ASP.NET 'vnext' myget feed](https://www.myget.org/F/aspnetvnext), which you can more easily view on [gallery](https://www.myget.org/gallery/aspnetvnext) for the feed.

You can also [build from source](../building/osx-instructions.md). 

Installing DNVM
===============

You need DNVM to acquire a (or multiple) .NET Execution Environment (DNX). DNVM is simply a script, which doesn't depend on .NET. On OS X the best way to get DNVM is to use [Homebrew](http://www.brew.sh). If you don't have Homebrew installed then follow the [Homebrew installation instructions](http://www.brew.sh). Once you have Homebrew then run the following commands:

	brew tap aspnet/dnx
	brew update
	brew install dnvm

You will likely need to register the dnvm command:

	source dnvm.sh

Installing the .NET Core DNX SDK
================================

You first need to acquire the Mono DNX. It includes a specfic version of Mono, and is needed to use the DNX tools that are not yet supported on .NET Core. Mono is the default DNX, so you can acquire it via `dnvm upgrade`.

	dnvm upgrade -u

Next, acquire the latest .NET Core DNX SDK.

	dnvm install latest -r coreclr -u

You can see the currently installed DNX versions with `dnvm list` (your display may vary as new versions of the DNX are published):

	dnvm list

```
Active Version              Runtime Arch Location             Alias
------ -------              ------- ---- --------             -----
  *    1.0.0-beta7-12364    coreclr x64  ~/.dnx/runtimes
       1.0.0-beta7-12364    mono         ~/.dnx/runtimes      default
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
        Console.WriteLine("Hello, OS X");
        Console.WriteLine("Love from CoreCLR.");
    }
}
```

Some people on the .NET Core team are partial to a demo console app on corefxlab repo which will print a picture for you. Download the [corefxlab demo](https://raw.githubusercontent.com/dotnet/corefxlab/master/demos/CoreClrConsoleApplications/HelloWorld/HelloWorld.cs) to the demo directory.

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

You need to restore packages for your app, based on your project.json, with `dnu restore`. You will need to run this command under the Mono DNX. Make sure that you are using that one.

	dnvm use 1.0.0-beta7-12364 -r mono
	dnu restore

You can run your app with .NET Core, although make sure to switch to that DNX.

    dnvm use 1.0.0-beta7-12364 -r coreclr
	dnx run

	Hello, OSX
	Love from CoreCLR.
