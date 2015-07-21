Debugging CoreCLR
=================

These instructions will lead you through debugging CoreCLR on Windows. They will be expanded to support Linux and OS X when we have good instructions for that.

Debugging CoreCLR on Windows
============================

1. Perform a build of the repo.
2. Open <repo_root>\binaries\Cmake\CoreCLR.sln in VS.
3. Right click the INSTALL project and choose ‘Set as StartUp Project’
4. Bring up the properties page for the INSTALL project
5. Select Configuration Properties->Debugging from the left side tree control
6. Set Command=`$(SolutionDir)..\product\$(Platform)\$(Configuration)\corerun.exe`
	1. This points to the folder where the built runtime binaries are present.
7. Set Command Arguments=`<managed app you wish to run>` (e.g. HelloWorld.exe)
8. Set Working Directory=`$(SolutionDir)..\product\$(Platform)\$(Configuration)`
	1. This points to the folder containing CoreCLR binaries.
9. Press F11 to start debugging at wmain in corerun (or set a breakpoint in source and press F5 to run to it)
	1. As an example, set a breakpoint for the EEStartup function in ceemain.cpp to break into CoreCLR startup.

Steps 1-8 only need to be done once, and then (9) can be repeated whenever you want to start debugging. The above can be done with Visual Studio 2013.

Debugging CoreCLR on Linux
==========================

Currently only lldb is supported by the SOS plugin. gdb can be used to debug the coreclr code but with no SOS support. Visual Studio 2015 RTM remote debugging isn't currently supported.

1. Perform a build of the coreclr repo.
2. Install the corefx managed assemblies to the binaries directory.
3. cd to build's binaries: `cd ~/coreclr/bin/Product/Linux.x64.Debug`
4. Start lldb (the version the plugin was built with, currently 3.6): `lldb-3.6 corerun HelloWorld.exe linux`
5. Now at the lldb command prompt, load SOS plugin: `plugin load libsosplugin.so`
6. Launch program: `process launch -s`
7. To stop annoying breaks on SIGUSR1/SIGUSR2 signals used by the runtime run: `process handle -s false SIGUSR1 SIGUSR2`
8. Get to a point where coreclr is initialized by setting a breakpoint (i.e. `breakpoint set -n LoadLibraryExW` and then `process continue`) or stepping into the runtime.
9. Run a SOS command like `sos ClrStack` or `sos VerifyHeap`.  The command name is case sensitive.

You can combine steps 4-8 and pass everything on the lldb command line:

`lldb-3.6 -o "plugin load libsosplugin.so" -o "process launch -s" -o "process handle -s false SIGUSR1 SIGUSR2" -o "breakpoint set -n LoadLibraryExW" corerun HelloWorld.exe linux`

SOS commands supported by the lldb plugin:

    IP2MD
    DumpStackObjects
    DumpMD
    DumpClass
    DumpMT
    DumpArray
    DumpObj
    PrintException
    DumpModule
    DumpDomain
    DumpAssembly
    ThreadState
    Threads
    FindAppDomain
    DumpLog
    Token2EE
    Name2EE
    ClrStack
    BPMD
    VerifyHeap
    DumpHeap

Debugging Mscorlib and/or managed application
=============================================

To step into and debug managed code of Mscorlib.dll (or the managed application being executed by the runtime you built), using Visual Studio, is something that will be supported with Visual Studio 2015. We are actively working to enable this support. 

Until then, you can use [WinDbg](https://msdn.microsoft.com/en-us/library/windows/hardware/ff551063(v=vs.85).aspx) and [SOS](https://msdn.microsoft.com/en-us/library/bb190764(v=vs.110).aspx) (an extension to WinDbg to support managed debugging) to step in and debug the generated managed code. This is what we do on the .NET Runtime team as well :)
