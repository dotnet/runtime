Build CoreCLR on Linux
======================

This guide will walk you through building CoreCLR on Linux and running Hello World.  We'll start by showing how to set up your environment from scratch.

Environment
===========

These instructions are written assuming the Ubuntu 14.04 LTS, since that's the distro the team uses. Pull Requests are welcome to address other environments as long as they don't break the ability to use Ubuntu 14.04 LTS.

There have been reports of issues when using other distros or versions of Ubuntu (e.g. [Issue 95](https://github.com/dotnet/coreclr/issues/95)). If you're on another distribution, consider using docker's `ubuntu:14.04` image.

Minimum RAM required to build is 1GB. The build is known to fail on 512 MB VMs ([Issue 536](https://github.com/dotnet/coreclr/issues/536)).

Toolchain Setup
---------------

Install the following packages for the toolchain: 

- cmake 
- llvm-3.5 
- clang-3.5 
- lldb-3.6
- lldb-3.6-dev 
- libunwind8 
- libunwind8-dev
- gettext
- libicu-dev
- liblttng-ust-dev
- libcurl4-openssl-dev
- libssl-dev
- uuid-dev

In order to get lldb-3.6 on Ubuntu 14.04, we need to add an additional package source:

```
ellismg@linux:~$ echo "deb http://llvm.org/apt/trusty/ llvm-toolchain-trusty-3.6 main" | sudo tee /etc/apt/sources.list.d/llvm.list
ellismg@linux:~$ wget -O - http://llvm.org/apt/llvm-snapshot.gpg.key | sudo apt-key add -
ellismg@linux:~$ sudo apt-get update
```

Then install the packages you need:

```
ellismg@linux:~$ sudo apt-get install cmake llvm-3.5 clang-3.5 lldb-3.6 lldb-3.6-dev libunwind8 libunwind8-dev gettext libicu-dev liblttng-ust-dev libcurl4-openssl-dev libssl-dev uuid-dev
```

You now have all the required components.

Git Setup
---------

This guide assumes that you've cloned the corefx and coreclr repositories into `~/git/corefx` and `~/git/coreclr` on your Linux machine and the corefx and coreclr repositories into `D:\git\corefx` and `D:\git\coreclr` on Windows. If your setup is different, you'll need to pay careful attention to the commands you run. In this guide, I'll always show what directory I'm in on both the Linux and Windows machine.

Install Mono
------------

If you don't already have Mono installed on your system, use the [installation instructions](http://www.mono-project.com/docs/getting-started/install/linux/).

For example, for Ubuntu, you do the following:

```
ellismg@linux:~$ sudo apt-key adv --keyserver hkp://keyserver.ubuntu.com:80 --recv-keys 3FA7E0328081BFF6A14DA29AA6A19B38D3D831EF
ellismg@linux:~$ echo "deb http://download.mono-project.com/repo/debian wheezy main" | sudo tee /etc/apt/sources.list.d/mono-xamarin.list
ellismg@linux:~$ sudo apt-get update
ellismg@linux:~$ sudo apt-get install mono-devel
```

Note that Debian requires a second package repository for libgdiplus (this is not applicable to Ubuntu):
```
echo "deb http://download.mono-project.com/repo/debian wheezy-libjpeg62-compat main" | sudo tee -a /etc/apt/sources.list.d/mono-xamarin.list
```

Set the maximum number of file-handles
--------------------------------------

To ensure that your system can allocate enough file-handles for the corefx build, add `fs.file-max = 100000` to `/etc/sysctl.conf`, and then run `sudo sysctl -p`.

Build the Runtime and Microsoft Core Library
=============================================

To build the runtime on Linux, run build.sh from the root of the coreclr repository:

```
ellismg@linux:~/git/coreclr$ ./build.sh
```

After the build is completed, there should some files placed in `bin/Product/Linux.x64.Debug`.  The ones we are interested in are:

* `corerun`: The command line host.  This program loads and starts the CoreCLR runtime and passes the managed program you want to run to it.
* `libcoreclr.so`: The CoreCLR runtime itself.
* `mscorlib.dll`: Microsoft Core Library (requires Mono).

In order to keep everything tidy, let's create a new directory for the runtime and copy the runtime and corerun into it.

```
ellismg@linux:~/git/coreclr$ mkdir -p ~/coreclr-demo/runtime
ellismg@linux:~/git/coreclr$ cp bin/Product/Linux.x64.Debug/corerun ~/coreclr-demo/runtime
ellismg@linux:~/git/coreclr$ cp bin/Product/Linux.x64.Debug/libcoreclr.so ~/coreclr-demo/runtime
ellismg@linux:~/git/coreclr$ cp bin/Product/Linux.x64.Debug/mscorlib.dll ~/coreclr-demo/runtime
ellismg@linux:~/git/coreclr$ cp bin/Product/Linux.x64.Debug/System.Globalization.Native.so ~/coreclr-demo/runtime
```

Build the Framework
===================

```
ellismg@linux:~/git/corefx$ ./build.sh
```

For the purposes of Hello World, you need to copy a few required files to the demo folder.

```
ellismg@linux:~/git/corefx$ cp bin/Linux.x64.Debug/Native/*.so ~/coreclr-demo/runtime
ellismg@linux:~/git/corefx$ cp bin/Linux.AnyCPU.Debug/System.Console/System.Console.dll ~/coreclr-demo/runtime
ellismg@linux:~/git/corefx$ cp bin/Linux.AnyCPU.Debug/System.Diagnostics.Debug/System.Diagnostics.Debug.dll ~/coreclr-demo/runtime
```

The runtime directory should now look like the following:

```
matell@linux:~$ ls ~/coreclr-demo/runtime/
corerun                       System.Globalization.Native.so
libcoreclr.so                 System.Native.so
mscorlib.dll                  System.Net.Http.Native.so
System.Console.dll            System.Security.Cryptography.Native.so
System.Diagnostics.Debug.dll
```

Download Dependencies
=====================

The rest of the assemblies you need to run are presently just facades that point to mscorlib.  We can pull these dependencies down via NuGet (which currently requires Mono).

Create a folder for the packages:

```
ellismg@linux:~$ mkdir ~/coreclr-demo/packages
ellismg@linux:~$ cd ~/coreclr-demo/packages
```

Download the NuGet Client
-------------------------

Grab NuGet (if you don't have it already)

```
ellismg@linux:~/coreclr-demo/packages$ curl -L -O https://nuget.org/nuget.exe
```
Download NuGet Packages
-----------------------

With Mono and NuGet in hand, you can use NuGet to get the required dependencies. 

Make a `packages.config` file with the following text. These are the required dependencies of this particular app. Different apps will have different dependencies and require a different `packages.config` - see [Issue #480](https://github.com/dotnet/coreclr/issues/480).

```
<?xml version="1.0" encoding="utf-8"?>
<packages>
  <package id="System.Collections" version="4.0.0-beta-22703" />
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
ellismg@linux:~/coreclr-demo/packages$ mono nuget.exe restore -Source https://www.myget.org/F/dotnet-corefx/ -PackagesDirectory .
```

NOTE: This assumes you installed Mono from the mono-project.com packages. If you have built your own please see this comment in [Issue #602](https://github.com/dotnet/coreclr/issues/602#issuecomment-88203778)

Finally, you need to copy over the assemblies to the runtime folder.  You don't want to copy over System.Console.dll or System.Diagnostics.Debug however, since the version from NuGet is the Windows version.  The easiest way to do this is with a little find magic:

```
ellismg@linux:~/coreclr-demo/packages$ find . -wholename '*/aspnetcore50/*.dll' -exec cp -n {} ~/coreclr-demo/runtime \;
```

Compile an App
==============

Now you need a Hello World application to run.  You can write your own, if you'd like.  Personally, I'm partial to the one on corefxlab which will draw Tux for us.

```
ellismg@linux:~$ cd ~/coreclr-demo/runtime
ellismg@linux:~/coreclr-demo/runtime$ curl -O https://raw.githubusercontent.com/dotnet/corefxlab/master/demos/CoreClrConsoleApplications/HelloWorld/HelloWorld.cs
```

Then you just need to build it, with `mcs`, the Mono C# compiler. FYI: The Roslyn C# compiler will soon be available on Linux.  Because you need to compile the app against the .NET Core surface area, you need to pass references to the contract assemblies you restored using NuGet:

```
ellismg@linux:~/coreclr-demo/runtime$ mcs /nostdlib /noconfig /r:../packages/System.Console.4.0.0-beta-22703/lib/contract/System.Console.dll /r:../packages/System.Runtime.4.0.20-beta-22703/lib/contract/System.Runtime.dll HelloWorld.cs
```

Run your App
============

You're ready to run Hello World!  To do that, run corerun, passing the path to the managed exe, plus any arguments.  The HelloWorld from corefxlab will print Tux if you pass "linux" as an argument, so:

```
ellismg@linux:~/coreclr-demo/runtime$ ./corerun HelloWorld.exe linux
```

Over time, this process will get easier. We will remove the dependency on having to compile managed code on Windows. For example, we are working to get our NuGet packages to include both the Windows and Linux versions of an assembly, so you can simply nuget restore the dependencies. 

Pull Requests to enable building CoreFX on Linux via Mono would be very welcome. A sample that builds Hello World on Linux using the correct references but via XBuild or MonoDevelop would also be great! There's still a lot of work ahead, so if you're interested in helping, we're ready for you!
