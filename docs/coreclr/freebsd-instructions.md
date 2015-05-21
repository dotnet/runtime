Build CoreCLR on FreeBSD
======================

This guide will walk you through building CoreCLR on FreeBSD and running Hello World.  We'll start by showing how to set up your environment from scratch.

Environment
===========

These instructions are written assuming FreeBSD 10.1-RELEASE, since that's the release the team uses.

These instructions assume you use the binary package tool `pkg` (analog to `apt-get` or `yum` on Linux) to install the environment. Compiling the dependencies from source using the ports tree might work too, but is untested.

Minimum RAM required to build is 1GB. The build is known to fail on 512 MB VMs ([Issue 536](https://github.com/dotnet/coreclr/issues/536)).

Toolchain Setup
---------------

Install the following packages for the toolchain: 

- bash
- cmake
- llvm35 (should be installed automatically as dependency of clang35)
- clang35
- libunwind
- gettext

Note: LLDB is not yet available in the pkg repository on FreeBSD. The LLDB plugin is currently not being build on FreeBSD. Instructions for this will be added later.

To install the packages you need:

`janhenke@freebsd-frankfurt:~ % sudo pkg install bash cmake clang35 libunwind gettext`

You now have all the required components.

Debugging CoreCLR (Optional)
----------------------------

Note: This step is *optional* and is not required to build CoreCLR itself. If you intend on hacking or debugging the CoreCLR source code, you need to follow these steps. You must follow these steps *before* starting the build itself.

In order to debug CoreCLR you will also need to install LLDB, the LLVM debugger. LLDB is still in the process of being ported to FreeBSD, so no official packages exist (see the FreeBSD LLDB Wiki page for more information on what has been ported so far: https://wiki.freebsd.org/lldb). However, it is possible to manually download and install LLDB from the LLVM source tree by following the instructions below: 

Firstly, install the following packages: python ninja swig13 git (in addition to the packages above), i.e.

```janhenke@freebsd-frankfurt:~ % sudo pkg install python ninja swig13 git```

Then, run the install script in ~/coreclr/src/pal/tools: 

```janhenke@freebsd-frankfurt:~ % ~/coreclr/src/pal/tools/freebsd-install-lldb.sh```

Note: LLDB will run su in order to install the LLDB build to /usr/local/include. 

(Optional) If you wish to run sudo instead of su, you can change line 29-31 to:

```sudo $NINJA lldb install```


You now have all the required components to debug CoreCLR installed.


Git Setup
---------

This guide assumes that you've cloned the coreclr repository into `~/git/coreclr` on your FreeBSD machine and the corefx and coreclr repositories into `D:\git\corefx` and `D:\git\coreclr` on Windows. If your setup is different, you'll need to pay careful attention to the commands you run. In this guide, I'll always show what directory I'm in on both the FreeBSD and Windows machine.

Build the Runtime
=================

To build the runtime on FreeBSD, run build.sh from the root of the coreclr repository:

```
janhenke@freebsd-frankfurt:~/git/coreclr % ./build.sh
```

Note: FreeBSD 10.1-RELEASE system's Clang/LLVM is 3.4, the minimum version to compile CoreCLR runtime is 3.5. You may need to specify `./build.sh clang3.5` to find Clang 3.5.

After the build is completed, there should some files placed in `bin/Product/FreeBSD.x64.Debug`.  The ones we are interested in are:

* `corerun`: The command line host.  This program loads and starts the CoreCLR runtime and passes the managed program you want to run to it.
* `libcoreclr.so`: The CoreCLR runtime itself.
* `libcoreclrpal.so`: The platform abstraction library for the CoreCLR runtime. This library is temporary and the functionality will be merged back into `libcoreclr.so`

In order to keep everything tidy, let's create a new directory for the runtime and copy the runtime and corerun into it.

```
janhenke@freebsd-frankfurt:~/git/coreclr % mkdir -p ~/coreclr-demo/runtime
janhenke@freebsd-frankfurt:~/git/coreclr % cp bin/Product/FreeBSD.x64.Debug/corerun ~/coreclr-demo/runtime
janhenke@freebsd-frankfurt:~/git/coreclr % cp bin/Product/FreeBSD.x64.Debug/libcoreclr*.so ~/coreclr-demo/runtime
```

Build the Framework 
===================

We don't _yet_ have support for building managed code on FreeBSD, so you'll need a Windows machine with clones of both the CoreCLR and CoreFX projects.

You will build `mscorlib.dll` out of the coreclr repository and the rest of the framework that out of the corefx repository.  For mscorlib (from a regular command prompt window) run:

```
D:\git\coreclr> build.cmd linuxmscorlib
```

The output is placed in `bin\Product\Linux.x64.Debug\mscorlib.dll`.  You'll want to copy this to the runtime folder on your FreeBSD machine. (e.g. `~/coreclr-demo/runtime`)

For the rest of the framework, you need to pass some special parameters to build.cmd when building out of the CoreFX repository.

```
D:\git\corefx> build.cmd /p:OSGroup=Linux /p:SkipTests=true
```

Note: We are using the Linux build currently, as CoreFX does not yet know about FreeBSD.

It's also possible to add `/t:rebuild` to the build.cmd to force it to delete the previously built assemblies.

For the purposes of Hello World, you need to copy over both `bin\Linux.AnyCPU.Debug\System.Console\System.Console.dll` and `bin\Linux.AnyCPU.Debug\System.Diagnostics.Debug\System.Diagnostics.Debug.dll`  into the runtime folder on FreeBSD. (e.g `~/coreclr-demo/runtime`).

After you've done these steps, the runtime directory on FreeBSD should look like this:

```
janhenke@freebsd-frankfurt:~/git/coreclr % ls ~/coreclr-demo/runtime/
System.Console.dll  System.Diagnostics.Debug.dll  corerun  libcoreclr.so  libcoreclrpal.so  mscorlib.dll
```

Download Dependencies
=====================

The rest of the assemblies you need to run are presently just facades that point to mscorlib.  We can pull these dependencies down via NuGet (which currently requires Mono).

Create a folder for the packages:

```
janhenke@freebsd-frankfurt:~/git/coreclr % mkdir ~/coreclr-demo/packages
janhenke@freebsd-frankfurt:~/git/coreclr % cd ~/coreclr-demo/packages
```

Install Mono
------------

If you don't already have Mono installed on your system, use the pkg tool again:

```
janhenke@freebsd-frankfurt:~/coreclr-demo/packages % sudo pkg install mono
```

Download the NuGet Client
-------------------------

Grab NuGet (if you don't have it already)

```
janhenke@freebsd-frankfurt:~/coreclr-demo/packages % curl -L -O https://nuget.org/nuget.exe
```
Download NuGet Packages
-----------------------

With Mono and NuGet in hand, you can use NuGet to get the required dependencies.

Make a `packages.config` file with the following text. These are the required dependencies of this particular app. Different apps will have different dependencies and require a different `packages.config` - see [Issue #480](https://github.com/dotnet/coreclr/issues/480).

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
</packages>

```

And restore your packages.config file:

```
janhenke@freebsd-frankfurt:~/coreclr-demo/packages % mono nuget.exe restore -Source https://www.myget.org/F/dotnet-corefx/ -PackagesDirectory .
```

NOTE: This assumes you already installed the default CA certs. If you have problems downloading the packages please see [Issue #602](https://github.com/dotnet/coreclr/issues/602#issuecomment-88203778). The command for FreeBSD is:

```
janhenke@freebsd-frankfurt:~/coreclr-demo/packages % mozroots --import --sync
```

Finally, you need to copy over the assemblies to the runtime folder.  You don't want to copy over System.Console.dll or System.Diagnostics.Debug however, since the version from NuGet is the Windows version.  The easiest way to do this is with a little find magic:

```
janhenke@freebsd-frankfurt:~/coreclr-demo/packages % find . -wholename '*/aspnetcore50/*.dll' -exec cp -n {} ~/coreclr-demo/runtime \;
```

Compile an App
==============

Now you need a Hello World application to run.  You can write your own, if you'd like.  Personally, I'm partial to the one on corefxlab which will draw Tux for us.

```
janhenke@freebsd-frankfurt:~/coreclr-demo/packages % cd ~/coreclr-demo/runtime
janhenke@freebsd-frankfurt:~/coreclr-demo/runtime % curl -O https://raw.githubusercontent.com/dotnet/corefxlab/master/demos/CoreClrConsoleApplications/HelloWorld/HelloWorld.cs
```

Then you just need to build it, with `mcs`, the Mono C# compiler. FYI: The Roslyn C# compiler will soon be available on FreeBSD.  Because you need to compile the app against the .NET Core surface area, you need to pass references to the contract assemblies you restored using NuGet:

```
janhenke@freebsd-frankfurt:~/coreclr-demo/runtime % mcs /nostdlib /noconfig /r:../packages/System.Console.4.0.0-beta-22703/lib/contract/System.Console.dll /r:../packages/System.Runtime.4.0.20-beta-22703/lib/contract/System.Runtime.dll HelloWorld.cs
```

Run your App
============

You're ready to run Hello World!  To do that, run corerun, passing the path to the managed exe, plus any arguments.  The HelloWorld from corefxlab will print a daemon if you pass "freebsd" as an argument, so:

```
janhenke@freebsd-frankfurt:~/coreclr-demo/runtime % ./corerun HelloWorld.exe freebsd
```

If all works, you should be greeted by a friendly daemon you know well.

Over time, this process will get easier. We will remove the dependency on having to compile managed code on Windows. For example, we are working to get our NuGet packages to include both the Windows and FreeBSD versions of an assembly, so you can simply nuget restore the dependencies. 

Pull Requests to enable building CoreFX and mscorlib on FreeBSD via Mono would be very welcome. A sample that builds Hello World on FreeBSD using the correct references but via XBuild or MonoDevelop would also be great! Some of our processes (e.g. the mscorlib build) rely on Windows specific tools, but we want to figure out how to solve these problems for FreeBSD as well. There's still a lot of work ahead, so if you're interested in helping, we're ready for you!


Run the test suite
==================

If you've made changes to the CoreCLR PAL code, you might want to run the PAL tests directly to validate your changes.
This can be done after a clean build, without any other dependencies.

```bash
janhenke@freebsd-frankfurt:~/coreclr % ./build.sh && src/pal/tests/palsuite/runpaltests.sh ~/coreclr/bin/obj/FreeBSD.x64.Debug ~/coreclr/bin/paltestout
```

This should run all the tests associated with the PAL.

**Note:** For FreeBSD all PAL tests may not pass at this point.
