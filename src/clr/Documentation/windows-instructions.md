Build CoreCLR on Windows
========================

These instructions will lead you through building CoreCLR and running a "Hello World" demo on Windows. 

Environment
===========

You must install several components to build the CoreCLR and CoreFX repos.These instructions were tested on Windows 7+. 

Visual Studio
-------------

Visual Studio must be installed. Supported versions:

- [Visual Studio Community 2013](http://go.microsoft.com/fwlink/?LinkId=517284) - **Free** for Open Source development!
- [Visual Studio 2013](http://www.visualstudio.com/downloads/download-visual-studio-vs) (Pro, Premium, Ultimate)

Visual Studio Express is not supported. Visual Studio 2015 isn't supported yet.

**Known Issues**

The DIA SDK gets incorrectly installed when VS 2013 is installed after VS 2012. To [workaround this issue](http://support.microsoft.com/kb/3035999)), copy `%program files (x86)%\Microsoft Visual Studio 11.0\DIA SDK` to  `%program files (x86)%\Microsoft Visual Studio 12.0\DIA SDK`. You can then build CoreCLR.

CMake
-----

The CoreCLR build relies on CMake for the build. We are currently using CMake 3.0.2, although later versions likely work.

- Install [CMake](http://www.cmake.org/download) for Windows.
- Add it to the PATH environment variable.

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

Download the [NuGet client](https://nuget.org/nuget.exe) and copy to c:\coreclr-demo. Alteratively, you can make download nuget.exe, put it somewhere else and make it part of your path.

Build the Runtime
=================

To build CoreCLR, run `build.cmd` from the root of the coreclr repository. This will do a clean x64/Debug build of CoreCLR, its native components, mscorlib and the tests. 

	C:\git\coreclr>build clean

	[Lots of build spew]

	Repo successfully built.

	Product binaries are available at C:\git\coreclr\binaries\Product\Windows_NT.x64.debug
	Test binaries are available at C:\git\coreclr\binaries\tests\Windows_NT.x64.debug

**build /?** will list supported parameters.

Check the build output.

- Product binaries will be dropped in `Binaries\Product\<arch>\<flavor>` folder. 
- A NuGet package, Microsoft.Dotnet.CoreCLR, will be created under `Binaries\Product\<arch>\<flavor>\.nuget` folder. 
- Test binaries will be dropped under `Binaries\Tests\<arch>\<flavor>` folder

You will see several files. The interesting ones are:

- `corerun`: The command line host. This program loads and starts the CoreCLR runtime and passes the managed program you want to run to it.
- `coreclr.dll`:  The CoreCLR runtime itself.
- `mscorlib.dll`: The core managed library for CoreCLR, which contains all of the fundamental data types and functionality.

Copy these files into the demo directory.

	C:\git\coreclr>copy binaries\Product\Windows_NT.x64.debug\CoreRun.exe \coreclr-demo\runtime
	C:\git\coreclr>copy binaries\Product\Windows_NT.x64.debug\coreclr.dll \coreclr-demo\runtime
	C:\git\coreclr>copy binaries\Product\Windows_NT.x64.debug\mscorlib.dll \coreclr-demo\runtime

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

	C:\git\corefx>copy bin\Windows.AnyCPU.Debug\System.Console\System.Console.dll \coreclr-demo
	C:\git\corefx>copy bin\Windows.AnyCPU.Debug\System.Diagnostics.Debug\System.Diagnostics.Debug.dll \coreclr-demo

The runtime directory should now look like the following:

	c:\git\corefx>dir \coreclr-demo

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

	CoreRun.exe HelloWorld.exe

Over time, this process will get easier. Thanks for trying out CoreCLR. Feel free to try a more interesting demo.