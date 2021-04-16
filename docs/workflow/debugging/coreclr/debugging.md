Debugging CoreCLR
=================

These instructions will lead you through debugging CoreCLR.

SOS has moved to the diagnostics repo. For more information on SOS, installation and how to use it click [here](https://github.com/dotnet/diagnostics#net-core-diagnostics-repo).

Debugging CoreCLR on Windows
============================

1. Open the CoreCLR solution in Visual Studio.
   - Method 1: Use the build scripts to open the solution.
      1. Run `./build.cmd -vs coreclr.sln -a <platform> -c <configuration>`. This will create and launch the CoreCLR solution in VS for the specified architecture and configuration.
   - Method 2: Manually build and open the solution.
      1. Perform a build of the repo with the `-msbuild` flag.
      2. Open solution `\<reporoot>\artifacts\obj\coreclr\windows.\<platform>.\<configuration>\ide\CoreCLR.sln` in Visual Studio. `<platform>` and `<configuration>` are based
    on type of build you did. By default they are 'x64' and 'Debug'.
2. Right-click the INSTALL project and choose ‘Set as StartUp Project’
3. Bring up the properties page for the INSTALL project
4. Select Configuration Properties->Debugging from the left side tree control
5. Set Command=`$(SolutionDir)\..\..\..\bin\coreclr\windows.$(Platform).$(Configuration)\corerun.exe`
    1. This points to the folder where the built runtime binaries are present.
6. Set Command Arguments=`<managed app you wish to run>` (e.g. HelloWorld.dll)
7. Set Working Directory=`$(SolutionDir)\..\..\..\bin\coreclr\windows.$(Platform).$(Configuration)`
    1. This points to the folder containing CoreCLR binaries.
8. Set Environment=`CORE_LIBRARIES=$(SolutionDir)\..\..\..\bin\runtime\<current tfm>-windows-$(Configuration)-$(Platform)`,
    where '\<current tfm\>' is the target framework of current branch, for example `netcoreapp3.1` `net5.0`.
    1. This points to the folder containing core libraries except `System.Private.CoreLib`.
    2. This step can be skipped if you are debugging CLR tests that references only `System.Private.CoreLib`.
    Otherwise, it's required to debug a realworld application that references anything else, including `System.Runtime`.
9.  Right-click the INSTALL project and choose 'Build'
    1. This will load necessary information from cmake to Visual Studio.
10. Press F11 to start debugging at wmain in corerun (or set a breakpoint in source and press F5 to run to it)
    1. As an example, set a breakpoint for the EEStartup function in ceemain.cpp to break into CoreCLR startup.

Steps 1-9 only need to be done once as long as there's been no changes to the CMake files in the repository. Afterwards, step 10 can be repeated whenever you want to start debugging. The above can be done with Visual Studio 2019 as writing.
Keeping with latest version of Visual Studio is recommended.

### Using SOS with windbg or cdb on Windows ###

See the SOS installation instructions [here](https://github.com/dotnet/diagnostics/blob/master/documentation/installing-sos-windows-instructions.md).

For more information on SOS commands click [here](https://github.com/dotnet/diagnostics/blob/master/documentation/sos-debugging-extension-windows.md).

Debugging CoreCLR on Linux and macOS
====================================

See the SOS installation instructions [here](https://github.com/dotnet/diagnostics/blob/master/documentation/installing-sos-instructions.md). After SOS is installed, it will automatically be loaded by lldb.

Only lldb is supported by SOS. Gdb can be used to debug the coreclr code but with no SOS support.

1. Perform a build of the coreclr repo.
2. Install the corefx managed assemblies to the binaries directory.
3. cd to build's binaries: `cd ./artifacts/bin/coreclr/Linux.x64.Debug`
4. Start lldb: `lldb corerun HelloWorld.exe linux`
6. Launch program: `process launch -s`
7. To stop annoying breaks on SIGUSR1/SIGUSR2 signals used by the runtime run: `process handle -s false SIGUSR1 SIGUSR2`
8. Get to a point where coreclr is initialized by setting a breakpoint (i.e. `breakpoint set -n LoadLibraryExW` and then `process continue`) or stepping into the runtime.
9. Run a SOS command like `clrstack` or `sos VerifyHeap`.  The command name is case sensitive.

See https://docs.microsoft.com/en-us/dotnet/core/diagnostics/dotnet-sos for information on how to install SOS.

**Note:** _corerun_ is a simple host that does not support resolving NuGet dependencies. It relies on libraries being locatable via the `CORE_LIBRARIES` environment variable or present in the same directory as the corerun executable. The instructions above are equally applicable to the _dotnet_ host, however - e.g. for step 4 `lldb-3.9 dotnet bin/Debug/netcoreapp2.1/MvcApplication.dll` will let you debug _MvcApplication_ in the same manner.

### Debugging core dumps with lldb

See this [link](https://github.com/dotnet/diagnostics/blob/master/documentation/debugging-coredump.md) in the diagnostics repo.

Disabling Managed Attach/Debugging
==================================

The "COMPlus_EnableDiagnostics" environment variable can be used to disable managed debugging. This prevents the various OS artifacts used for debugging like the named pipes and semaphores on Linux/MacOS and shared memory on Windows from being created.

    export COMPlus_EnableDiagnostics=0

Debugging Crossgen2
==================================

Debugging Crossgen2 is described in [this](debugging-crossgen2.md) document.

Using Visual Studio Code
========================

- Install [Visual Studio Code](https://code.visualstudio.com/)
- Install the [C# Extension](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csharp)
- Open the folder containing the source you want to debug in VS Code
- Open the debug window: `ctrl-shift-D` or click on the button on the left
- Click the gear button at the top to create a launch configuration, select `.NET Core` from the selection dropdown
- In the `.NET Core Launch (console)` configuration do the following
  - delete the `preLaunchTask` property
  - set `program` to the full path to corerun in the test directory
  - set `cwd` to the test directory
  - set `args` to the command line arguments to pass to the test
    - something like: `[ "xunit.console.netcore.exe", "<test>.dll", "-notrait", .... ]`
- Set a breakpoint and launch the debugger, inspecting variables and call stacks will now work

Using Visual Studio
===================

- Install [Visual Studio](https://visualstudio.microsoft.com/vs/)
- Use File->Open Project (not open file) and select the binary you want to use as your host (typically dotnet.exe or corerun.exe)
- Open the project properties for the new project that was just created and set:
  - Arguments: Make this match whatever arguments you would have used at the command-line. For example if you would have run "dotnet.exe exec Foo.dll", then set arguments = "exec Foo.dll"
      (Note: you probably want 'dotnet exec' rather than 'dotnet run' because the run verb is implemented to launch the app in a child-process and the debugger won't be attached to that child process)
  - Working Directory: Make this match whatever you would have used on the command-line
  - Debugger Type: Set this to either 'Managed (CoreCLR)' or 'Native Only' depending on whether you want to debug the C+ or native code respectively.
  - Environment: Add any environment variables you would have added at the command-line. You may also consider adding COMPlus_ZapDisable=1 and COMPlus_ReadyToRun=0 which disable NGEN and R2R pre-compilation respectively and allow the JIT to create debuggable code. This will give you a higher quality C# debugging experience inside the runtime framework assemblies, at the cost of somewhat lower app performance.
- For managed debugging, there are some settings in Debug->Options, Debugging->General that might be useful:
  - Uncheck 'Just My Code'. This will allow you debug into the framework libraries.
  - Check 'Enable .NET Framework Source Stepping.' This will configure the debugger to download symbols and source automatically for runtime framework binaries. If you built the framework yourself this may be irrelevant because you already have all the source on your machine but it doesn't hurt.
  - Check 'Suppress JIT optimzation on module load'. This tells the debugger to tell the .NET runtime JIT to generate debuggable code even for modules that may not have been compiled in a 'Debug' configuration by the C# compiler. This code is slower, but it provides much higher fidelity breakpoints, stepping, and local variable access. It is the same difference you see when debugging .NET apps in the 'Debug' project configuration vs. the 'Release' project configuration.
