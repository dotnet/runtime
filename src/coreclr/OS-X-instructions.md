These instructions will lead you through building CoreCLR and running "Hello World" on OS X.

Environment
===========
These instructions assume OS X Yosemite.

CoreCLR has a dependency on CMake for the build. You can download it from [CMake downloads](http://www.cmake.org/download/).

Alternatively, you can install CMake from [Homebrew](http://brew.sh/).

    brew install cmake

Build CoreCLR
=============

1. Clone CoreCLR.
    - `git clone https://github.com/dotnet/coreclr`
2. Build CoreCLR, from the root of the coreclr repo.
    - `./build.sh`
    - Type `./build.sh -?` to see the build options.

    ```
    Repo successfully built.
	Product binaries are available at /Users/richlander/Source/coreclr/binaries/Product/mac.x64.debug
	```

3. Check the build output.
    - `ls binaries/Product/mac.x64.debug/`

    ```
    corerun			libcoreclr.dylib	libmscordaccore.dylib
	inc				libdebug-pal.a		libsos.dylib
	```

Build mscorlib
==============

Build an mscorlib to run. Only the Windows build can do this currently. The team is working on fixing that, captured by [issue 463](https://github.com/dotnet/coreclr/issues/463).

1. Go to a Windows machine

2. Validate that your machine satisfies the [Windows pre-requisites](https://github.com/dotnet/coreclr/wiki/Windows-instructions).

3. Clone CoreCLR
    - `git clone https://github.com/dotnet/coreclr`

4. Build mscorlib, from the root of the coreclr repo.
    - `build.cmd unixmscorlib`

5. Check the build output.
    - `dir binaries\intermediates\Unix.x64.debug`

6. Copy mscorlib.dll to your OS X machine.
    - Use a thumbdrive, a syncing program or OneDrive or DropBox.

6. Copy mscorlib.dll to [repo-root]/binaries/Product/mac.x64.debug/, on the OS X machine.
    - This is the same place as the build output, from the build step.

Compile Hello World
===================

1. Write a Hello World program. Save it.

	```
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

2. Compile on OS X with the Mono C# compiler.
    - Install Mono from the [Mono downloads](http://www.mono-project.com/download/).
    - `dmcs -nostdlib -r:mscorlib.dll helloworld.cs`
    - Note that the [Roslyn compiler](https://github.com/dotnet/roslyn) will soon be available on Mac OS X.

3. Check the build output.
    - You should find a helloworld.exe in the appropriate location, given the compilation option you chose.

Run the program
===============

CoreCLR use a runner to run programs, unlike the way .NET programs run on Windows. This is more similar to how other development platforms work.

1. Navigate back to the build output directory on the OS X machine.
    - `cd [repo-root]/binaries/Product/mac.x64.debug`
2. Run the CoreCLR runner - corerun - to see options.
    - `./corerun`
3. Start program.
    - `./corerun -c . helloworld.exe`

    ```
    Hello, OS X
	Love from CoreCLR.
	```

Credit to Frank A. Krueger for providing the first published Mac OS X instructions @ [Building and Running .NET’s CoreCLR on OS X](http://praeclarum.org/post/110552954728/building-and-running-nets-coreclr-on-os-x).


