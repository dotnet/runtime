Build CoreCLR on OS X
=====================

These instructions will lead you through building CoreCLR and running a "Hello World" demo on OS X.

Environment
===========

These instructions were validated on OS X Yosemite, although they probably work on earlier versions. The instructions makes use of both OS X and Windows machines, since parts of the .NET Core developer environment are not yet supported on OS X. Once those parts are supported on OS X, the Windows-specific instructions will be replaced.

If your machine has Command Line Tools for XCode 6.3 installed, you'll need to update them to the 6.3.1 version or higher in order to successfully build. There was an issue with the headers that shipped with version 6.3 that was subsequently fixed in 6.3.1.

Git Setup
---------

Clone the CoreCLR and CoreFX repositories (either upstream or a fork).

    dotnet-mbp:git richlander$ git clone https://github.com/dotnet/coreclr
    Cloning into 'coreclr'...
    remote: Counting objects: 16526, done.
    remote: Compressing objects: 100% (21/21), done.
    remote: Total 16526 (delta 8), reused 0 (delta 0), pack-reused 16505
    Receiving objects: 100% (16526/16526), 26.26 MiB | 9.87 MiB/s, done.
    Resolving deltas: 100% (7679/7679), done.
    Checking connectivity... done.

    dotnet-mbp:git richlander$ git clone https://github.com/dotnet/corefx
    Cloning into 'corefx'...
    remote: Counting objects: 41610, done.
    remote: Compressing objects: 100% (14/14), done.
    remote: Total 41610 (delta 5), reused 0 (delta 0), pack-reused 41596
    Receiving objects: 100% (41610/41610), 23.97 MiB | 311.00 KiB/s, done.
    Resolving deltas: 100% (25680/25680), done.
    Checking connectivity... done

This guide assumes that you've cloned the coreclr and corefx repositories into `~/git/coreclr` and `~/git/corefx` on your OS X machine and the coreclr repository into `C:\git\coreclr` on Windows. If your setup is different, you'll need to pay careful attention to the commands you run. In this guide, I'll always show you the current directory on both the OS X and Windows machine.

CMake
-----

CoreCLR has a dependency on CMake for the build. You can download it from [CMake downloads](http://www.cmake.org/download/).

Alternatively, you can install CMake from [Homebrew](http://brew.sh/).

    dotnet-mbp:~ richlander$ brew install cmake

Mono
----

[Mono](http://www.mono-project.com/) is needed in order to run NuGet.exe and to build CoreFX. NuGet will add .NET Core support at some point soon. You can download it from the [Mono downloads](http://www.mono-project.com/docs/getting-started/install/mac/) page.

In order to build mscorlib (directions follow) you will need to raise the limit on the number of open files allowed otherwise you may see an error. To do this execute the following command from the shell:

    ulimit -n 2048

ICU
---
ICU (International Components for Unicode) is also required to build and run. It can be obtained via [Homebrew](http://brew.sh/) with `brew install icu4c`.

OpenSSL
-------
The CoreFX cryptography libraries are built on OpenSSL. The version of OpenSSL included on OS X (0.9.8) has gone out of support, and a newer version is required. A supported version can be obtained via [Homebrew](http://brew.sh).

    brew install openssl
    brew link --force openssl

Demo directory
--------------

In order to keep everything tidy, create a new directory for all the files that you will build or acquire.

    dotnet-mbp:~ richlander$ mkdir -p ~/coreclr-demo/runtime
    dotnet-mbp:~ richlander$ mkdir -p ~/coreclr-demo/packages
    dotnet-mbp:~ richlander$ cd ~/coreclr-demo/

NuGet
-----

NuGet is required to acquire any .NET assembly dependency that is not built by these instructions.

    dotnet-mbp:coreclr-demo richlander$ curl -L -O https://nuget.org/nuget.exe

Build the Runtime and Microsoft Core Library
============================================

To Build CoreCLR, run build.sh from the root of the coreclr repo.

    dotnet-mbp:~ richlander$ cd ~/git/coreclr
    dotnet-mbp:coreclr richlander$ ./build.sh

    [Lots of stuff before this]
    Repo successfully built.
    Product binaries are available at /Users/richlander/git/coreclr/bin/Product/OSX.x64.Debug


Type `./build.sh -?` to see the full set of build options.

Check the build output.

    dotnet-mbp:coreclr richlander$ ls bin/Product/OSX.x64.Debug/

You will see several files. The interesting ones are:

- `corerun`: The command line host. This program loads and starts the CoreCLR runtime and passes the managed program you want to run to it.
- `libcoreclr.dylib`: The CoreCLR runtime itself.
- `mscorlib.dll`: Microsoft Core Library (requires Mono).

Copy the runtime and corerun into the demo directory.

    dotnet-mbp:coreclr richlander$ cp bin/Product/OSX.x64.Debug/corerun ~/coreclr-demo/runtime/
    dotnet-mbp:coreclr richlander$ cp bin/Product/OSX.x64.Debug/libcoreclr.dylib ~/coreclr-demo/runtime/
	dotnet-mbp:coreclr richlander$ cp bin/Product/OSX.x64.Debug/mscorlib.dll ~/coreclr-demo/runtime/
	dotnet-mbp:coreclr richlander$ cp bin/Product/OSX.x64.Debug/System.Globalization.Native.dylib ~/coreclr-demo/runtime/

Build the Framework
===================

    dotnet-mbp:corefx richlander$ ./build.sh

For the purposes of this demo, you need to copy a few required files to the demo folder.

	dotnet-mbp:corefx richlander$ cp bin/OSX.x64.Debug/Native/*.dylib ~/coreclr-demo/runtime
    dotnet-mbp:corefx richlander$ cp bin/OSX.AnyCPU.Debug/System.Console/System.Console.dll ~/coreclr-demo/runtime
    dotnet-mbp:corefx richlander$ cp bin/OSX.AnyCPU.Debug/System.Diagnostics.Debug/System.Diagnostics.Debug.dll ~/coreclr-demo/runtime

The runtime directory should now look like the following:

    dotnet-mbp:~ richlander$ ls ~/coreclr-demo/runtime/
    System.Console.dll                 System.Security.Cryptography.Native.dylib
    System.Diagnostics.Debug.dll       corerun
	System.Globalization.Native.dylib  libcoreclr.dylib
	System.Native.dylib                mscorlib.dll
	System.Net.Http.Native.dylib

Download NuGet Packages
=======================

The rest of the assemblies you need to run are presently just facades that point to mscorlib.  We can pull these dependencies down via NuGet (which currently requires Mono).

Make a `packages/packages.config` file with the following text. These are the required dependencies of this particular app. Different apps will have different dependencies and require a different `packages.config` - see [Issue #480](https://github.com/dotnet/coreclr/issues/480).

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

And restore your packages.config file:

    dotnet-mbp:~ richlander$ cd ~/coreclr-demo
    dotnet-mbp:coreclr-demo richlander$ mono nuget.exe restore packages/packages.config -Source https://www.myget.org/F/dotnet-corefx/ -PackagesDirectory packages

Finally, you need to copy the assemblies over to the runtime folder.  You don't want to copy over System.Console.dll or System.Diagnostics.Debug however, since the version from NuGet is the Windows version.  The easiest way to do this is with a little find magic:

    dotnet-mbp:coreclr-demo richlander$ find . -wholename '*/aspnetcore50/*.dll' -exec cp -n {} ~/coreclr-demo/runtime \;

Compile an App
==============

Now you need a Hello World application to run.  You can write your own, if you'd like. Here's a very simple one:

    using System;

    public class Program
    {
        public static void Main (string[] args)
        {
            Console.WriteLine("Hello, OS X");
            Console.WriteLine("Love from CoreCLR.");
        }   
    } 

Personally, I'm partial to the one on corefxlab which will print a  picture for you.

    dotnet-mbp:coreclr-demo richlander$ curl -O https://raw.githubusercontent.com/dotnet/corefxlab/master/demos/CoreClrConsoleApplications/HelloWorld/HelloWorld.cs


Then you just need to build it, with `mcs`, the Mono C# compiler. FYI: The Roslyn C# compiler will soon be available on OS X.  Because you need to compile the app against the .NET Core surface area, you need to pass references to the contract assemblies you restored using NuGet:

    dotnet-mbp:coreclr-demo richlander$ mcs /nostdlib /noconfig /r:packages/System.Console.4.0.0-beta-22703/lib/contract/System.Console.dll /r:packages/System.Runtime.4.0.20-beta-22703/lib/contract/System.Runtime.dll -out:runtime/HelloWorld.exe HelloWorld.cs 

Run your App
============

You're ready to run Hello World!  To do that, run corerun, passing the path to the managed exe, plus any arguments.  The HelloWorld from corefxlab will print a special fruit if you pass "mac" as an argument, so:

    dotnet-mbp:coreclr-demo richlander$ cd runtime/
    dotnet-mbp:runtime richlander$ ./corerun HelloWorld.exe mac


Over time, this process will get easier. We will remove the dependency on having to compile managed code on Windows. For example, we are working to get our NuGet packages to include  Windows, Linux and OS X versions of an assembly, so you can simply nuget restore the dependencies. 

Pull Requests to enable building CoreFX and mscorlib on OS X via Mono would be very welcome. A sample that builds Hello World on OS X using the correct references but via XBuild or MonoDevelop would also be great! Some of our processes (e.g. the mscorlib build) rely on Windows specific tools, but we want to figure out how to solve these problems for OS X as well. There's still a lot of work ahead, so if you're interested in helping, we're ready for you!
