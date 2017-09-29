Debugging CoreCLR
=================

These instructions will lead you through debugging CoreCLR on Windows and Linux. They will be expanded to support OS X when we have good instructions for that.

Debugging CoreCLR on Windows
============================

1. Perform a build of the repo.
2. Open Solution \<reporoot\>\bin\obj\Windows_NT.\<platform\>.\<configuration\>\CoreCLR.sln in VS. \<platform\> and \<configuration\> are based
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

### Using SOS with windbg or cdb on Windows ###

If you know the path of the `sos.dll` for the version of your runtime, load it like `.load c:\path\to\sos\sos.dll`. Use can use the `lm` command to find the path of the "coreclr.dll" module. `.loadby sos coreclr` should also work.

For more information on SOS commands click [here](https://msdn.microsoft.com/en-us/library/bb190764(v=vs.110).aspx).

Debugging CoreCLR on OS X
==========================

To use lldb on OS X, you first need to build it and the SOS plugin on the machine you intend to use it. See the instructions in [building lldb](buildinglldb.md). The rest of instructions on how to use lldb for Linux on are the same.

Debugging CoreCLR on Linux
==========================

Only lldb is supported by the SOS plugin. gdb can be used to debug the coreclr code but with no SOS support. Visual Studio 2015 RTM remote debugging isn't currently supported.

1. Perform a build of the coreclr repo.
2. Install the corefx managed assemblies to the binaries directory.
3. cd to build's binaries: `cd ~/coreclr/bin/Product/Linux.x64.Debug`
4. Start lldb (the version the plugin was built with, currently 3.9): `lldb-3.9 corerun HelloWorld.exe linux`
5. Now at the lldb command prompt, load SOS plugin: `plugin load libsosplugin.so`
6. Launch program: `process launch -s`
7. To stop annoying breaks on SIGUSR1/SIGUSR2 signals used by the runtime run: `process handle -s false SIGUSR1 SIGUSR2`
8. Get to a point where coreclr is initialized by setting a breakpoint (i.e. `breakpoint set -n LoadLibraryExW` and then `process continue`) or stepping into the runtime.
9. Run a SOS command like `sos ClrStack` or `sos VerifyHeap`.  The command name is case sensitive.

You can combine steps 4-8 and pass everything on the lldb command line:

`lldb-3.9 -o "plugin load libsosplugin.so" -o "process launch -s" -o "process handle -s false SIGUSR1 SIGUSR2" -o "breakpoint set -n LoadLibraryExW" corerun HelloWorld.exe linux`

### SOS commands ###

This is the full list of commands currently supported by SOS. lldb is case-sensitive unlike windbg.

	Type "soshelp <functionname>" for detailed info on that function.

	Object Inspection                  Examining code and stacks
	-----------------------------      -----------------------------
	DumpObj (dumpobj)                  Threads (clrthreads)
	DumpArray                          ThreadState
	DumpStackObjects (dso)             IP2MD (ip2md)
	DumpHeap (dumpheap)                u (clru)
	DumpVC                             DumpStack (dumpstack)
	GCRoot (gcroot)                    EEStack (eestack)
	PrintException (pe)                ClrStack (clrstack)
	                                   GCInfo
	                                   EHInfo
	                                   bpmd (bpmd)

	Examining CLR data structures      Diagnostic Utilities
	-----------------------------      -----------------------------
	DumpDomain                         VerifyHeap
	EEHeap (eeheap)                    FindAppDomain
	Name2EE (name2ee)                  DumpLog (dumplog)
	DumpMT (dumpmt)                    CreateDump (createdump)
	DumpClass (dumpclass)
	DumpMD (dumpmd)
	Token2EE
	DumpModule (dumpmodule)
	DumpAssembly
	DumpRuntimeTypes
	DumpIL (dumpil)
	DumpSig
	DumpSigElem

    Examining the GC history           Other
    -----------------------------      -----------------------------
    HistInit (histinit)                FAQ
    HistRoot (histroot)                Help (soshelp)
    HistObj  (histobj)
    HistObjFind (histobjfind)
    HistClear (histclear)

### Aliases ###

By default you can reach all the SOS commands by using: _sos [command\_name]_
However the common commands have been aliased so that you don't need the SOS prefix:

    bpmd            -> sos bpmd
    clrstack        -> sos ClrStack
    clrthreads      -> sos Threads
    clru            -> sos U
    createdump      -> sos CreateDump
	dso             -> sos DumpStackObjects
    dumpclass       -> sos DumpClass
    dumpheap        -> sos DumpHeap
    dumpil          -> sos DumpIL
    dumplog         -> sos DumpLog
    dumpmd          -> sos DumpMD
    dumpmodule      -> sos DumpModule
    dumpmt          -> sos DumpMT
    dumpobj         -> sos DumpObj
    dumpstack       -> sos DumpStack     
    eeheap          -> sos EEHeap
    eestack         -> sos EEStack
    gcroot          -> sos GCRoot
    histinit        -> sos HistInit
    histroot        -> sos HistRoot
    histobj         -> sos HistObj
    histobjfind     -> sos HistObjFind
    histclear       -> sos HistClear
    ip2md           -> sos IP2MD
    name2ee         -> sos Name2EE
    pe              -> sos PrintException
    soshelp         -> sos Help

### Debugging core dumps with lldb

It is also possible to debug .NET Core crash dumps using lldb and SOS. In order to do this, you need all of the following:

- The crash dump file. We have a service called "Dumpling" which collects, uploads, and archives crash dump files during all of our CI jobs and official builds.
- On Linux, there is an utility called `createdump` (see [doc](https://github.com/dotnet/coreclr/blob/master/Documentation/botr/xplat-minidump-generation.md "doc")) that can be setup to generate core dumps when a managed app throws an unhandled exception or faults.
- Matching coreclr/corefx runtime bits from the crash. To get these, you should either:
  - Download the matching Jenkins archive onto your repro machine.
  - Check out the coreclr and corefx repositories at the appropriate commit and re-build the necessary portions.
  - You can also download the matching "symbols" nuget package from myget.org. There is a "Download Symbols" button in the myget UI for this purpose.
- lldb version 3.9. The SOS plugin (i.e. libsosplugin.so) provided is now built for lldb 3.9.

Once you have everything listed above, you are ready to start debugging. You need to specify an extra parameter to lldb in order for it to correctly resolve the symbols for libcoreclr.so. Use a command like this:

```
lldb-3.9 -O "settings set target.exec-search-paths <runtime-path>" --core <core-file-path> <host-path>
```

- `<runtime-path>`: The path containing libcoreclr.so.dbg, as well as the rest of the runtime and framework assemblies.
- `<core-file-path>`: The path to the core dump you are attempting to debug.
- `<host-path>`: The path to the dotnet or corerun executable, potentially in the `<runtime-path>` folder.

lldb should start debugging successfully at this point. You should see stacktraces with resolved symbols for libcoreclr.so. At this point, you can run `plugin load <libsosplugin.so-path>`, and begin using SOS commands, as above.

Using Visual Studio Code
========================

- Install [Visual Studio Code](https://code.visualstudio.com/)
- Install the [C# Extension](https://marketplace.visualstudio.com/items?itemName=ms-vscode.csharp)
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
