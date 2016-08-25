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

To debug managed code, ensure you have installed atleast [Visual Studio 2015 Update 3](https://www.visualstudio.com/en-us/news/releasenotes/vs2015-update3-vs).

Make sure that you install "VC++ Tools". By default, they will not be installed.

To build for Arm32, you need to have [Windows SDK for Windows 10](https://developer.microsoft.com/en-us/windows/downloads) installed. 

Visual Studio Express is not supported.

CMake
-----

The CoreCLR repo build has been validated using CMake 3.5.2. 

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

Powershell version must be 3.0 or higher. This should be the case for Windows 8 and later builds.
- Windows 7 SP1 can install Powershell version 4 [here](https://www.microsoft.com/en-us/download/details.aspx?id=40855).

Git Setup
---------

Clone the CoreCLR and CoreFX repositories (either upstream or a fork).

```bat
C:\git>git clone https://github.com/dotnet/coreclr
C:\git>git clone https://github.com/dotnet/corefx
```

This guide assumes that you've cloned the CoreCLR and CoreFX repositories into C:\git using the default repo names. If your setup is different, you'll need to pay attention to the commands you run. The guide will always show you the current directory.

The repository is configured to allow Git to make the right decision about handling CRLF. Specifically, if you are working on **Windows**, please ensure that **core.autocrlf** is set to **true**. On **non-Windows** platforms, please set it to **input**.

Demo directory
--------------

In order to keep everything tidy, create a new directory for the files that you will build or acquire.

```bat
c:\git>mkdir \coreclr-demo\runtime
c:\git>mkdir \coreclr-demo\ref
```

Build the Runtime
=================

To build CoreCLR, run `build.cmd` from the root of the coreclr repository. This will do a x64/Debug build of CoreCLR, its native components, mscorlib.dll, and the tests.

	C:\git\coreclr>build -rebuild

	[Lots of build spew]

	Repo successfully built.

	Product binaries are available at C:\git\coreclr\bin\Product\Windows_NT.x64.debug
	Test binaries are available at C:\git\coreclr\bin\tests\Windows_NT.x64.debug

**Note:** To avoid building the tests, pass the 'skiptestbuild' option to build.

**build -?** will list supported parameters.

Check the build output.

- Product binaries will be dropped in `bin\Product\<OS>.<arch>.<flavor>` folder. 
- A NuGet package, Microsoft.Dotnet.CoreCLR, will be created under `bin\Product\<OS>.<arch>.<flavor>\.nuget` folder. 
- Test binaries will be dropped under `bin\Tests\<OS>.<arch>.<flavor>` folder

You will see several files. The interesting ones are:

- `corerun`: The command line host. This program loads and starts the CoreCLR runtime and passes the managed program you want to run to it.
- `coreclr.dll`:  The CoreCLR runtime itself.
- `mscorlib.dll`: The core managed library for CoreCLR, which contains all of the fundamental data types and functionality.

Copy these files into the demo directory.

```bat
C:\git\coreclr>copy bin\Product\Windows_NT.x64.debug\clrjit.dll \coreclr-demo\runtime
C:\git\coreclr>copy bin\Product\Windows_NT.x64.debug\CoreRun.exe \coreclr-demo\runtime
C:\git\coreclr>copy bin\Product\Windows_NT.x64.debug\coreclr.dll \coreclr-demo\runtime
C:\git\coreclr>copy bin\Product\Windows_NT.x64.debug\mscorlib.dll \coreclr-demo\runtime
C:\git\coreclr>copy bin\Product\Windows_NT.x64.debug\System.Private.CoreLib.dll \coreclr-demo\runtime
```

Build the Framework
===================

Build the framework out of the corefx directory.

	c:\git\corefx>build.cmd

	[Lots of build spew]

    0 Warning(s)
    0 Error(s)
	Time Elapsed 00:03:14.53
	Build Exit Code = 0

It's also possible to add -rebuild to build.cmd to force it to delete the previously built assemblies.

For the purposes of this demo, you need to copy a few required assemblies to the demo folder.

```bat
C:\git\corefx>copy bin\Windows_NT.AnyCPU.Debug\System.Console\System.Console.dll \coreclr-demo\runtime
C:\git\corefx>copy bin\Windows_NT.AnyCPU.Debug\System.Diagnostics.Debug\System.Diagnostics.Debug.dll \coreclr-demo\runtime
C:\git\corefx>copy bin\AnyOS.AnyCPU.Debug\System.IO\System.IO.dll \coreclr-demo\runtime
C:\git\corefx>copy bin\AnyOS.AnyCPU.Debug\System.IO.FileSystem.Primitives\System.IO.FileSystem.Primitives.dll \coreclr-demo\runtime
C:\git\corefx>copy bin\AnyOS.AnyCPU.Debug\System.Runtime\System.Runtime.dll \coreclr-demo\runtime
C:\git\corefx>copy bin\AnyOS.AnyCPU.Debug\System.Runtime.InteropServices\System.Runtime.InteropServices.dll \coreclr-demo\runtime
C:\git\corefx>copy bin\AnyOS.AnyCPU.Debug\System.Text.Encoding\System.Text.Encoding.dll \coreclr-demo\runtime
C:\git\corefx>copy bin\AnyOS.AnyCPU.Debug\System.Text.Encoding.Extensions\System.Text.Encoding.Extensions.dll \coreclr-demo\runtime
C:\git\corefx>copy bin\AnyOS.AnyCPU.Debug\System.Threading\System.Threading.dll \coreclr-demo\runtime
C:\git\corefx>copy bin\AnyOS.AnyCPU.Debug\System.Threading.Tasks\System.Threading.Tasks.dll \coreclr-demo\runtime
```

You also need to copy reference assemblies, which will be used during compilation.

```bat
C:\git\corefx>copy bin\ref\System.Runtime\4.0.0.0\System.Runtime.dll \coreclr-demo\ref
C:\git\corefx>copy bin\ref\System.Console\4.0.0.0\System.Console.dll \coreclr-demo\ref
```

Compile the Demo
================

Now you need a Hello World application to run. You can write your own, if you'd like. Here's a very simple one:

```C#
using System;

public class Program
{
    public static void Main()
    {
        Console.WriteLine("Hello, Windows");
        Console.WriteLine("Love from CoreCLR.");
    }
}
```

Personally, I'm partial to the one on corefxlab which will print a picture for you. Download the [corefxlab demo](https://raw.githubusercontent.com/dotnet/corefxlab/master/demos/CoreClrConsoleApplications/HelloWorld/HelloWorld.cs) to `\coreclr-demo`.

Then you just need to build it, with csc, the .NET Framework C# compiler. It may be easier to do this step within the "Developer Command Prompt for VS2015", if csc is not in your path. Because you need to compile the app against the .NET Core surface area, you need to pass references to the contract assemblies you restored using NuGet:

```bat
csc /nostdlib /noconfig /r:ref\System.Runtime.dll /r:ref\System.Console.dll /out:runtime\hello.exe hello.cs
```

Run the demo
============

You're ready to run Hello World! To do that, run corerun, passing the path to the managed exe, plus any arguments. In this case, no arguments are necessary.

```bat
C:\coreclr-demo>cd runtime
C:\coreclr-demo\runtime>CoreRun.exe hello.exe
```

If `CoreRun.exe` fails for some reason, you will see an empty output. To diagnose the issue, you can use `/v` to switch verbose mode on: `CoreRun.exe /v hello.exe`.

Over time, this process will get easier. Thanks for trying out CoreCLR. Feel free to try a more interesting demo.
