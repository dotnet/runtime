This guide will walk you through building CoreCLR on Linux and running Hello World.  We'll start by showing how to set up your environment from scratch.  If you're in a hurry and want to use Docker, we have some sample images at the end of this document

# From Scratch

Before starting, you need to set up your development machine.  We use Ubuntu 14.04 LTS as our primary environment currently, so these instructions are written assuming you are using that Distro.  Pull Requests are welcome to address other environments as long as they don't break the ability to use Ubuntu 14.04 LTS.

There have been reports of issues when using other distros or versions of Ubuntu (e.g. [Issue 95](https://github.com/dotnet/coreclr/issues/95)) so if you're on another distribution, consider using docker's ```ubuntu:14.04``` image.

We install the following packages to get our toolchain: ```cmake llvm-3.5 clang-3.5 lldb-3.5```.  On Ubuntu 14.04 they can be installed with ```apt-get```:

```sudo apt-get install cmake llvm-3.5 clang-3.5 lldb-3.5```  

This gives us the 3.5 version of the llvm toolchain and version 2.8.12.2 of cmake.

This guide assumes that you've cloned the coreclr repository into ```~/git/coreclr``` on your Linux machine and the corefx and coreclr repositories into ```D:\git\corefx``` and ```D:\git\coreclr``` on Windows. If your setup is different, you'll need to pay careful attention to the commands you run. In this guide, I'll always show what directory I'm in on both the Linux and Windows machine.

To build the runtime on Linux, run build.sh from the root of the coreclr repository:

```
ellismg@linux:~/git/coreclr$ ./build.sh
```

After the build is completed, there should be three files placed in ```binaries/Product/amd64/debug```:

* ```corerun```: The command line host.  This program loads and starts the CoreCLR runtime and passes the managed program you want to run to it.
* ```libcoreclr.so```: The CoreCLR runtime itself.
* ```libmscordaccore.so```: The DAC, a component that helps interface debuggers to CoreCLR.  This is unused for now but will eventually be used by our SOS support on Linux.

In order to keep everything tidy, let's create a new directory for the runtime and copy the runtime and corerun into it.

```
ellismg@linux:~/git/coreclr$ mkdir -p ~/coreclr-demo/runtime
ellismg@linux:~/git/coreclr$ cp binaries/Product/amd64/debug/corerun ~/coreclr-demo/runtime
ellismg@linux:~/git/coreclr$ cp binaries/Product/amd64/debug/libcoreclr.so ~/coreclr-demo/runtime
```

Today, we don't support building the managed components of the runtime on Linux yet, so you'll need to have a Windows machine with clones of both the CoreCLR and CoreFX projects.

Furthmore, our build system does not segment output based on the target platform yet, so in order to build the managed components for Linux, you'll first want to remove any versions of the assemblies you have built before.

```
D:\git\corefx> rmdir /q /s bin
D:\git\coreclr> rmdir /q /s binaries
```

We build ```mscorlib.dll``` out of the coreclr repository and the rest of the framework that we'll need out of the corefx repository.  For mscorlib (from a regular command prompt window) run:

```
D:\git\coreclr> build.cmd unixmscorlib
```

The output is placed in ```binaries\intermediates\amd64\Debug\mscorlib.dll```.  You'll want to copy this to the runtime folder on your Linux machine. (e.g. ```~/coreclr-demo/runtime```)

For the rest of the framework, we need to pass some special parameters to build.cmd when building out of the CoreFX repository.  Note that this repository must be built from a [Developer Command Prompt for VS2013](https://msdn.microsoft.com/en-us/library/ms229859%28v=vs.110%29.aspx):

```
D:\git\corefx> build.cmd /p:OS=Unix /p:SkipTests=true
```

It's also possible to add ```/t:rebuild``` to the build.cmd to force it to delete the previously built assemblies.  This is an option if you want to switch between building the framework for Windows and Linux and don't want to delete the entire binaries directory between builds.

For the purposes of Hello World, we need to copy over just ```bin\Debug\System.Console\System.Console.dll``` into the runtime folder on Linux. (e.g ```~/coreclr-demo/runtime```).

After you've done these steps, the runtime directory on Linux should look like this:

```
matell@linux:~$ ls ~/coreclr-demo/runtime/
corerun  libcoreclr.so  mscorlib.dll  System.Console.dll
```

```System.Console.dll``` depends on some assemblies, which are presently just facades that point to mscorlib.  We can pull these dependencies down via NuGet running on Mono.

If you don't already have Mono installed on your system, you can follow [installation instructions](http://www.mono-project.com/docs/getting-started/install/linux/)

At a high level, you do the following:

```
ellismg@linux:~$ sudo apt-key adv --keyserver keyserver.ubuntu.com --recv-keys 3FA7E0328081BFF6A14DA29AA6A19B38D3D831EF
ellismg@linux:~$ echo "deb http://download.mono-project.com/repo/debian wheezy main" | sudo tee /etc/apt/sources.list.d/mono-xamarin.list
ellismg@linux:~$ sudo apt-get update
ellismg@linux:~$ sudo apt-get install mono-devel
```

With Mono in hand, we can use NuGet to get our dependencies.  We'll place all the NuGet packages together:

```
ellismg@linux:~$ mkdir ~/coreclr-demo/packages
ellismg@linux:~$ cd ~/coreclr-demo/packages
```

Make a ```packages.config``` file with the following text:

```
<?xml version="1.0" encoding="utf-8"?>
<packages>
  <package id="System.Console" version="4.0.0-beta-22512" />
  <package id="System.Diagnostics.Contracts" version="4.0.0-beta-22512" />
  <package id="System.Diagnostics.Debug" version="4.0.10-beta-22512" />
  <package id="System.Diagnostics.Tools" version="4.0.0-beta-22512" />
  <package id="System.Globalization" version="4.0.10-beta-22512" />
  <package id="System.IO" version="4.0.10-beta-22512" />
  <package id="System.IO.FileSystem.Primitives" version="4.0.0-beta-22512" />
  <package id="System.Reflection" version="4.0.10-beta-22512" />
  <package id="System.Resources.ResourceManager" version="4.0.0-beta-22512" />
  <package id="System.Runtime" version="4.0.20-beta-22512" />
  <package id="System.Runtime.Extensions" version="4.0.10-beta-22512" />
  <package id="System.Runtime.Handles" version="4.0.0-beta-22512" />
  <package id="System.Runtime.InteropServices" version="4.0.20-beta-22512" />
  <package id="System.Text.Encoding" version="4.0.10-beta-22512" />
  <package id="System.Text.Encoding.Extensions" version="4.0.10-beta-22512" />
  <package id="System.Threading" version="4.0.0-beta-22512" />
  <package id="System.Threading.Tasks" version="4.0.10-beta-22512" />
</packages>
```

Then grab NuGet (if you don't have it already)

```
ellismg@linux:~/coreclr-demo/packages$ curl -L -O https://nuget.org/nuget.exe
```

And restore your packages.config file:

```
ellismg@linux:~/coreclr-demo/packages$ mono nuget.exe restore -Source https://www.myget.org/F/dotnet-corefx/ -PackagesDirectory .
```

Finally, we need to copy over the assemblies to the runtime folder.  We don't want to copy over System.Console.dll however, since the version from NuGet is the Windows version.  The easiest way to do this is with a little find magic:

```
ellismg@linux:~/coreclr-demo/packages$ find . -wholename '*/aspnetcore50/*.dll' -exec cp -n {} ~/coreclr-demo/runtime \;
```

Now we need a Hello World application that we can run.  You can write your own, if you'd like.  Personally, I'm partial to the one on corefxlab which will draw Tux for us.

```
ellismg@linux:~$ cd ~/coreclr-demo/runtime
ellismg@linux:~/coreclr-demo/runtime$ curl -O https://raw.githubusercontent.com/dotnet/corefxlab/master/demos/CoreClrConsoleApplications/HelloWorld/HelloWorld.cs
```

Then we just need to build it, with ```mcs```.  Because we need to compile it against the .NET Core surface area, we have to pass references to the contract assemblies we restored using NuGet:

```
ellismg@linux:~/coreclr-demo/runtime$ mcs /nostdlib /noconfig /r:../packages/System.Console.4.0.0-beta-22512/lib/contract/System.Console.dll /r:../packages/System.Runtime.4.0.20-beta-22512/lib/contract/System.Runtime.dll HelloWorld.cs
```

Once that's complete, we're ready to run Hello World!  To do that, we run corerun passing the path to the managed exe we want to run plus any arguments.  The HelloWorld from corefxlab will print Tux if we pass "linux" as an argument, so:

```
ellismg@linux:~/coreclr-demo/runtime$ ./corerun HelloWorld.exe linux
```

Over time, we want this process to get easier. We would like to remove the dependency on having to compile managed code on Windows. For example, we are working to get our NuGet packages to include both the Windows and Linux versions of an assembly, so you can simply nuget restore the dependencies. Pull Requests to allow building CoreFX and mscorlib on Linux via Mono would be very welcome. A sample that builds Hello World on Linux using the correct references but via XBuild or MonoDevelop would also be great! Some of our processes (e.g. the mscorlib build) rely on Windows specific tools, but we want to figure out how to solve these problems for Linux as well. There's still a lot of work ahead, so if you're interested in helping, we're ready for you!

# Using Docker

If you don't want to do the above, or you can't (since you don't have access to Windows in order to build the managed components), I've produced a few helpful docker images to play around with.  There are not any plans to update these long term, but they should be useful in order to try things out.

* ```ellismg/coreclr-demo:runtime-only``` Contains the runtime and managed components plus hello world.  Everything is located at ```/root/coreclr-demo```
* ```ellismg/coreclr-demo:dev-environment``` Contains the managed components as well as the development tools for building the native runtime plus mono for building managed code locally.  Note this doesn't include the reference assemblies and other files in the packages directory since they can be downloaded on Linux using NuGet on mono, however a sample packages.config is present in ```/root/coreclr-demo/packages```.  In order to get this to work, you'll need to clone the git repository and build CoreCLR yourself and place the output in ```/root/coreclr-demo/runtime```.  You'll also need to go through the NuGet restore steps above, and build HelloWorld.exe yourself, but you don't need access to a Windows machine as the docker image already contains mscorlib.dll and System.Console.dll.