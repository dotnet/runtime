*This blog post originally appeared on David Broman's blog on 12/11/2007*


This is the first of some tips to help you debug your profiler.  Note that these tips assume you're using CLR 2.x (see [this entry](https://docs.microsoft.com/en-us/archive/blogs/davbr/versions-of-microsoft-net-framework-clr-and-your-profiler) for info on how CLR version numbers map to .NET Framework version numbers).  In today's post, I address a frequent question from profiler developers and users: "Why didn't my profiler load?".

## Event log (Windows only)

In the Application event log, you'll see entries if the CLR attempts, but fails, to load and initialize your profiler.  So this is a nice and easy place to look first, as the message may well make it obvious what went wrong.

## Weak link in the chain?

The next step is to carefully retrace this chain to make sure everything is registered properly:

Environment variables --\> Registry --\> Profiler DLL on File system.

The first link in this chain is to check the environment variables inside the process that should be profiled.  If you're running the process from a command-prompt, you can just try a "set co" from the command prompt:

```
C:\> set co
 (blah blah, other vars beginning with "co")
```

```
CORECLR_ENABLE_PROFILING=0x1
CORECLR_PROFILER={C5F90153-B93E-4138-9DB7-EB7156B07C4C}
```

If your scenario doesn't allow you to just run the process from a command prompt, like say an asp.net scenario, you may want to attach a debugger to the process that's supposed to be profiled, or use IFEO (HKEY\_LOCAL\_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options) to force a debugger to start when the worker process starts.  In the debugger, you can then use "!peb" to view the environment block, which will include the environment variables.

Once you verify CORECLR\_ENABLE\_PROFILING and CORECLR\_PROFILER are ok, it's time to search the registry for the very same GUID set in your CORECLR\_PROFILER environment variable.  You should find it at a path like this:

HKEY\_LOCAL\_MACHINE\SOFTWARE\Classes\CLSID\{C5F90153-B93E-4138-9DB7-EB7156B07C4C}

If the registry has the GUID value, it's finally time to check out your file system.  Go under the InprocServer32 subkey under the GUID:

HKEY\_LOCAL\_MACHINE\SOFTWARE\Classes\CLSID\{C5F90153-B93E-4138-9DB7-EB7156B07C4C}\InprocServer32

and look at the default value data.  It should be a full path to your profiler's DLL.  Verify it's accurate.  If not, perhaps you didn't properly run regsvr32 against your profiler, or maybe your profiler's **DllRegisterServer** had problems.

## Time for a debugger

If the above investigation indicates everything's ok, then your profiler is properly registered and your environment is properly set up, but something bad must be happening at run time.  You'll want symbols for the CLR, which are freely available via Microsoft's symbol server.  If you set this environment variable, you can ensure windbg will always use the symbol server:

set \_NT\_SYMBOL\_PATH=srv\*C:\MySymbolCache\*http://msdl.microsoft.com/download/symbols

Feel free to add more paths (separate them via ";") so you can include your profiler's symbols as well.  Now, from a command-prompt that has your Cor\_Enable\_Profiling and COR\_PROFILER variables set, run windbg against the executable you want profiled.  The debuggee will inherit the environment, so the profiling environment variables will be propagated to the debuggee.

Note: The following contains implementation details of the runtime.  While these details are useful as a debugging aid, your profiler code cannot make assumptions about them.  These implementation details are subject to change at whim.

Once windbg is running, try setting this breakpoint:

bu mscordbc!EEToProfInterfaceImpl::CreateProfiler

Now go!  If you hit that breakpoint, that verifies the CLR has determined that a profiler has been requested to load from the environment variables, but the CLR has yet to read the registry.  Let's see if your DLL actually gets loaded.  You can use

sxe ld _NameOfYourProfiler_.dll

or even set a breakpoint inside your Profiler DLL's **DllMain.**   Now go, and see if your profiler is getting loaded.  If you can verify your profiler's DLL is getting loaded, then you now know your registry is pointing to the proper path, and any static dependencies your profiler has on other DLLs have been resolved.  But will your profiler COM object get instantiated properly?  Set breakpoints in your class factory ( **DllGetClassObject** ) and your profiler COM object's **QueryInterface** to see if you can spot problems there.  For example, if your profiler only works against CLR 1.x, then the CLR's call into your QueryInterface will fail, since you don't implement ICorProfilerCallback2.

If you're still going strong, set a breakpoint in your profiler's **Initialize** () callback.  Failures here are actually a popular cause for activation problems.  Inside your Initialize() callback, your profiler is likely calling QueryInterface for the ICorProfilerInfoX interface of your choice, and then calling SetEventMask, and doing other initialization-related tasks, like calling SetEnterLeaveFunctionHooks(2).  Do any of these fail?  Is your Initialize() callback returning a failure HRESULT?

Hopefully by now you've isolated the failure point.  If not, and your Initialize() is happily returning S\_OK, then your profiler is apparently loading just fine.  At least it is when you're debugging it.  :-)
