Debugging CoreCLR
=================

These instructions will lead you through debugging CoreCLR on Windows. They will be expanded to support Linux and OS X when we have good instructions for that.

Debugging CoreCLR on Windows
============================

1. Perform a build of the repo.
2. Open \<repo_root\>\bin\obj\Windows_NT.\<platform\>.\<configuration\>\CoreCLR.sln in VS. \<platform\> and \<configurtion\> are based
    on type of build you did. By default they are 'x64' and 'Debug'.
3. Right click the INSTALL project and choose ‘Set as StartUp Project’
4. Bring up the properties page for the INSTALL project
5. Select Configuration Properties->Debugging from the left side tree control
6. Set Command=`$(SolutionDir)..\..\product\Windows_NT.$(Platform).$(Configuration)\corerun.exe`
	1. This points to the folder where the built runtime binaries are present.
7. Set Command Arguments=`<managed app you wish to run>` (e.g. HelloWorld.exe)
8. Set Working Directory=`$(SolutionDir)..\..\product\Windows_NT.$(Platform).$(Configuration)`
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

    bpmd
    ClrStack
    DumpStackObjects
    DumpMD
    DumpClass
    DumpMT
    DumpArray
    DumpObj
    DumpAssembly
    DumpDomain
    DumpHeap
    DumpLog
    DumpModule
    DumpRuntimeTypes
    DumpVC
    EEHeap
    EHInfo
    FindAppDomain
    GCRoot
    GCInfo
    Help
    IP2MD
    Name2EE
    PrintException
    ThreadState
    Threads
    Token2EE
    VerifyHeap

There are some aliases for the most common commands:

    bpmd            -> sos bpmd
    clrstack        -> sos ClrStack
    clrthreads      -> sos Threads
    dumpheap        -> sos DumpHeap
    dumplog         -> sos DumpLog
    dumpmd          -> sos DumpMD
    dumpmt          -> sos DumpMT
    dumpobj         -> sos DumpObj
    dso             -> sos DumpStackObjects
    eeheap          -> sos EEHeap
    gcroot          -> sos GCRoot
    ip2md           -> sos IP2MD
    name2ee         -> sos Name2EE
    pe              -> sos PrintException
    soshelp         -> sos Help

Problems and limitations of lldb and sos:

Many of the sos commands like clrstack or dso don't work on core dumps because lldb doesn't 
return the actual OS thread id for a native thread. The "setsostid" command can be used to work
around this lldb bug. Use the "clrthreads" to find the os tid and the lldb command "thread list"
to find the thread index (#1 for example) for the current thread (* in first column). The first
setsostid argument is the os tid and the second is the thread index: "setsosid ecd5 1".

The "gcroot" command either crashes lldb 3.6 or returns invalid results. Works fine with lldb 3.7.

Loading Linux core dumps with lldb 3.7 doesn't work. lldb 3.7 loads OSX and FreeBSD core dumps 
just fine.

For more information on SOS commands see: https://msdn.microsoft.com/en-us/library/bb190764(v=vs.110).aspx

Debugging Mscorlib and/or managed application
=============================================

To step into and debug managed code of Mscorlib.dll (or the managed application being executed by the runtime you built), using Visual Studio, is something that will be supported with Visual Studio 2015. We are actively working to enable this support. 

Until then, you can use [WinDbg](https://msdn.microsoft.com/en-us/library/windows/hardware/ff551063(v=vs.85).aspx) and [SOS](https://msdn.microsoft.com/en-us/library/bb190764(v=vs.110).aspx) (an extension to WinDbg to support managed debugging) to step in and debug the generated managed code. This is what we do on the .NET Runtime team as well :)
