Get .NET Core on Windows with NuGet
===================================

These instructions will lead you through acquiring .NET Core via NuGet and running a "Hello World" demo on Windows. The instructions use a particular set of paths. You'll need to adjust if you want to use a different set.

.NET Core is distributed as NuGet packages. You can acquire it via NuGet restore (these instructions) or the [.NET Version Manager](get-coreclr-windows-dnvm.md). Alternatively, you can [build from source](windows-instructions.md). 

You can see the [CoreCLR myget feed](https://www.myget.org/F/dotnet-core) @ the [dotnet-core gallery](https://www.myget.org/gallery/dotnet-core) page. These packages are not yet published to NuGet.org, but that will change soon.

NuGet Restore Packages
======================

Given that NuGet is the .NET Core distribution mechanism, you need a packages.config to restore the packages. The following packages.config is the most minimal one you can have for console apps. You will need to add packages if your app needs it Save this XML to ```c:\coreclr-demo\packages\packages.config```.

```
<?xml version="1.0" encoding="utf-8"?>
<packages>
  <package id="System.Console" version="4.0.0-beta-22703" />
  <package id="System.Diagnostics.Contracts" version="4.0.0-beta-22703" />
  <package id="System.Diagnostics.Debug" version="4.0.10-beta-22703" />
  <package id="System.Diagnostics.Tools" version="4.0.0-beta-22703" />
  <package id="System.Globalization" version="4.0.10-beta-22703" />
  <package id="System.IO" version="4.0.10-beta-22703" />
  <package id="System.IO.FileSystem.Primitives" version="4.0.0-beta-22703" />
  <package id="System.Reflection" version="4.0.10-beta-22703" />
  <package id="System.Resources.ResourceManager" version="4.0.0-beta-22703" />
  <package id="System.Runtime" version="4.0.20-beta-22703" />
  <package id="System.Runtime.Extensions" version="4.0.10-beta-22703" />
  <package id="System.Runtime.Handles" version="4.0.0-beta-22703" />
  <package id="System.Runtime.InteropServices" version="4.0.20-beta-22703" />
  <package id="System.Text.Encoding" version="4.0.10-beta-22703" />
  <package id="System.Text.Encoding.Extensions" version="4.0.10-beta-22703" />
  <package id="System.Threading" version="4.0.10-beta-22703" />
  <package id="System.Threading.Tasks" version="4.0.10-beta-22703" />
  <package id="Microsoft.NETCore.Runtime.CoreCLR.ConsoleHost-x64" version="1.0.0-beta-1.0.0-beta-22819" />
  <package id="Microsoft.NETCore.Runtime.CoreCLR-x64" version="1.0.0-beta-1.0.0-beta-22819"/>
</packages>
```

You will need to update the version numbers to acquire later versions of the NuGet packages. If you do, you'll need to update the copy commands later in the instructions to accomodate. NuGet supports wildcard versions, such as ```version="4.0.0-beta-*"```, which can be helpful.

Download the [NuGet client](https://nuget.org/nuget.exe) if you don't already have it in your path. You can grab it from: https://nuget.org/nuget.exe. Save it to ```c:\coreclr-demo```.

You need to restore the packages to the packages directory.

	C:\coreclr-demo>nuget restore packages\packages.config -Source https://www.myget.org/F/dotnet-core/ -PackagesDirectory packages

Write your App
==============

You need a Hello World application to run. You can write your own, if you'd like. Here's a very simple one:

	using System;

	public class Program
	{
	    public static void Main (string[] args)
	    {
	        Console.WriteLine("Hello, Windows");
	        Console.WriteLine("Love from CoreCLR.");
	    }   
	} 

Some people on the .NET Core team are partial to a demo console app on corefxlab repo which will print a picture for you. Download the [corefxlab demo](https://raw.githubusercontent.com/dotnet/corefxlab/master/demos/CoreClrConsoleApplications/HelloWorld/HelloWorld.cs) to ```C:\coreclr-demo```.

Compile your App
================

You need to build your app with csc, the .NET Framework C# compiler. It may be easier to do this step within the "Developer Command Prompt for VS2013", if csc is not in your path. You need to pass references to the contract assemblies you restored using NuGet to compile the app against the .NET Core surface area:

	C:\coreclr-demo>md app

	C:\coreclr-demo>csc /nostdlib /noconfig /r:packages\System.Runtime.4.0.20-beta-22703\lib\contract\System.Runtime.dll /r:packages\System.Console.4.0.0-beta-22703\lib\contract\System.Console.dll /out:app\HelloWorld.dll HelloWorld.cs

It might seem odd, but this command compiles your app to a DLL. That's intended.

Prepare the demo
================

You need to copy the NuGet package assemblies over to the app folder. You need to run a few commands, including a little batch magic.

	C:\coreclr-demo>for /f %k in ('dir /s /b packages\*.dll') do echo %k | findstr "\aspnetcore50" && copy /Y %k app

	C:\coreclr-demo>copy packages\Microsoft.NETCore.Runtime.CoreCLR-x64.1.0.0-beta-22819\lib\any~win\x64\* app

	C:\coreclr-demo>copy packages\Microsoft.NETCore.Runtime.CoreCLR.ConsoleHost-x64.1.0.0-beta-22819\native\win\x64\CoreConsole.exe app\HelloWorld.exe

This last step might be a bit surprising, copying ```CoreConsole.exe``` to MyApp.exe, in this case ```HelloWorld.exe```. This is closedly related to compiling the app, in the instructions above, to MyApp.dll, in this case to ```HelloWorld.dll```. 

We've grown very fond of creating and using managed EXEs that don't require a separate launcher with the .NET Framework on Windows. We wanted the same experience for .NET Core. To enable the experience, we created a launcher that expects a managed assembly of the same name, compiled with a static main method. As a case in point, if you run ```CoreConsole.exe``` without renaming it, it will expect a ```CoreConsole.dll```. The renaming step, which you see above, needs to match the main assembly, compiled as a DLL, and you get an experience that feels launcher-less.

Run the demo
============

You're ready to run Hello World! 

	C:\coreclr-demo>app\HelloWorld.exe
	Hello, Windows
	Love from CoreCLR.
	
Thanks for trying out CoreCLR. Feel free to try a more interesting demo.