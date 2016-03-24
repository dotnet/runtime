Build CoreCLR on Windows
========================

These instructions will lead you through building CoreCLR and running a "Hello World" demo on Windows. 

Environment
===========

You must install several components to build the CoreCLR and CoreFX repos. These instructions were tested on Windows 7+.

Visual Studio
-------------

Visual Studio must be installed. Supported versions:

- [Visual Studio 2015](https://www.visualstudio.com/downloads/visual-studio-2015-downloads-vs) (Community, Professional, Enterprise)

Visual Studio Express is not supported.

CMake
-----

The CoreCLR build relies on CMake for the build. We are currently using CMake 3.0.2, although later versions likely work.

- Install [CMake](http://www.cmake.org/download) for Windows.
- Add it to the PATH environment variable.

Python
---------
Python is used in the build system. We are currently using python 2.7.9, although
any recent (2.4+) version of Python should work, including Python 3.
- Install [Python](https://www.python.org/downloads/) for Windows.
- Add it to the PATH environment variable.

PowerShell
----------
PowerShell is used in the build system. Ensure that it is accessible via the PATH environment variable. Typically this is %SYSTEMROOT%\System32\WindowsPowerShell\v1.0\.

Git Setup
---------

Clone the CoreCLR and CoreFX repositories (either upstream or a fork).

    C:\git>git clone https://github.com/dotnet/coreclr
    C:\git>git clone https://github.com/dotnet/corefx

This guide assumes that you've cloned the CoreCLR and CoreFX repositories into C:\git using the default repo names. If your setup is different, you'll need to pay attention to the commands you run. The guide will always show you the current directory.

The repository is configured to allow Git to make the right decision about handling CRLF. Specifically, if you are working on **Windows**, please ensure that **core.autocrlf** is set to **true**. On **non-Windows** platforms, please set it to **input**.

Demo directory
--------------

In order to keep everything tidy, create a new directory for the files that you will build or acquire.

	c:\git>mkdir \coreclr-demo\runtime
	c:\git>mkdir \coreclr-demo\packages

NuGet
-----

NuGet is required to acquire any .NET assembly dependency that is not built by these instructions.

Download the [NuGet client](https://nuget.org/nuget.exe) and copy to c:\coreclr-demo. Alternatively, you can download nuget.exe, put it somewhere else, and add it to your PATH.

Build the Runtime
=================

To build CoreCLR, run `build.cmd` from the root of the coreclr repository. This will do a clean x64/Debug build of CoreCLR, its native components, mscorlib.dll, and the tests.

	C:\git\coreclr>build clean

	[Lots of build spew]

	Repo successfully built.

	Product binaries are available at C:\git\coreclr\bin\Product\Windows_NT.x64.debug
	Test binaries are available at C:\git\coreclr\bin\tests\Windows_NT.x64.debug

**Note:** To avoid building the tests, pass the 'skiptestbuild' option to build.

**build /?** will list supported parameters.

Check the build output.

- Product binaries will be dropped in `bin\Product\<OS>.<arch>.<flavor>` folder. 
- A NuGet package, Microsoft.Dotnet.CoreCLR, will be created under `bin\Product\<OS>.<arch>.<flavor>\.nuget` folder. 
- Test binaries will be dropped under `bin\Tests\<OS>.<arch>.<flavor>` folder

You will see several files. The interesting ones are:

- `corerun`: The command line host. This program loads and starts the CoreCLR runtime and passes the managed program you want to run to it.
- `coreclr.dll`:  The CoreCLR runtime itself.
- `mscorlib.dll`: The core managed library for CoreCLR, which contains all of the fundamental data types and functionality.

Copy these files into the demo directory.

	C:\git\coreclr>copy bin\Product\Windows_NT.x64.debug\CoreRun.exe \coreclr-demo\runtime
	C:\git\coreclr>copy bin\Product\Windows_NT.x64.debug\coreclr.dll \coreclr-demo\runtime
	C:\git\coreclr>copy bin\Product\Windows_NT.x64.debug\mscorlib.dll \coreclr-demo\runtime

Build the Framework
===================

Build the framework out of the corefx directory.

	c:\git\corefx>build.cmd

	[Lots of build spew]

    0 Warning(s)
    0 Error(s)
	Time Elapsed 00:03:14.53
	Build Exit Code = 0

It's also possible to add /t:rebuild to build.cmd to force it to delete the previously built assemblies.

For the purposes of this demo, you need to copy a few required assemblies to the demo folder.

	C:\git\corefx>copy bin\Windows_NT.AnyCPU.Debug\System.Console\System.Console.dll \coreclr-demo
	C:\git\corefx>copy bin\Windows_NT.AnyCPU.Debug\System.Diagnostics.Debug\System.Diagnostics.Debug.dll \coreclr-demo

The runtime directory should now look like the following:

	c:\git\corefx>dir \coreclr-demo

```
 Directory of C:\coreclr-demo

05/15/2015  03:58 PM    <DIR>          .
05/15/2015  03:58 PM    <DIR>          ..
05/15/2015  02:43 PM    <DIR>          packages
05/15/2015  03:36 PM    <DIR>          runtime
05/15/2015  02:44 PM         1,664,512 nuget.exe
05/15/2015  03:37 PM            51,712 System.Console.dll
05/15/2015  03:37 PM            21,504 System.Diagnostics.Debug.dll
```

Restore NuGet Packages
======================

You need to restore/download the rest of the demo dependencies via NuGet, as they are not yet part of the CoreFX repo. At present, these NuGet dependencies contain facades (type forwarders) that point to mscorlib.

Make a packages/packages.config file with the following XML. These packages are the required dependencies of this particular app. Different apps will have different dependencies and require different packages.config - see [Issue #480](https://github.com/dotnet/coreclr/issues/480).

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
	</packages>

And restore the packages with the packages.config:

	C:\coreclr-demo>nuget restore packages\packages.config -Source https://www.myget.org/F/dotnet-corefx/ -PackagesDirectory packages

Compile the Demo
================

Now you need a Hello World application to run. You can write your own, if you'd like. Here's a very simple one:

	using System;

	public class Program
	{
	    public static void Main (string[] args)
	    {
	        Console.WriteLine("Hello, Windows");
	        Console.WriteLine("Love from CoreCLR.");
	    }   
	} 

Personally, I'm partial to the one on corefxlab which will print a picture for you. Download the [corefxlab demo](https://raw.githubusercontent.com/dotnet/corefxlab/master/demos/CoreClrConsoleApplications/HelloWorld/HelloWorld.cs) to `\coreclr-demo`.

Then you just need to build it, with csc, the .NET Framework C# compiler. It may be easier to do this step within the "Developer Command Prompt for VS2013", if csc is not in your path. Because you need to compile the app against the .NET Core surface area, you need to pass references to the contract assemblies you restored using NuGet:

	C:\coreclr-demo>csc /nostdlib /noconfig /r:packages\System.Runtime.4.0.20-beta-2
	2703\lib\contract\System.Runtime.dll /r:packages\System.Console.4.0.0-beta-22703
	\lib\contract\System.Console.dll /out:runtime\HelloWorld.exe HelloWorld.cs

Run the demo
============

You need to copy the NuGet package assemblies over to the runtime folder. 
The easiest way to do this is with a little batch magic. Say "no" to any requests to overwrite files, to avoid overwriting the CoreFX files you just built.

	for /f %k in ('dir /s /b packages\*.dll') do echo %k | findstr "\aspnetcore50" && copy /-Y %k runtime

You're ready to run Hello World! To do that, run corerun, passing the path to the managed exe, plus any arguments. In this case, no arguments are necessary.

	C:\coreclr-demo>cd runtime
	C:\coreclr-demo\runtime>CoreRun.exe HelloWorld.exe

Over time, this process will get easier. Thanks for trying out CoreCLR. Feel free to try a more interesting demo.
