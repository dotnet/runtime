Performance Tracing on Linux
============================

When a performance problem is encountered on Linux, these instructions can be used to gather detailed information about what was happening on the machine at the time of the performance problem.

CoreCLR supports two different mechanisms for tracing .NET applications on Linux: EventPipe and LTTng. They both have tools built by the .NET team, namely dotnet-trace (which uses EventPipe) and PerfCollect (which uses LTTng). Here are some notable differences between the two tools to help you decide which to use:

1. PerfCollect leverages LTTng, which is a tracing framework built for the Linux kernel, so it can only be used on Linux. dotnet-trace is OS agnostic, so you can use it the same way across Windows/macOS and Linux.

2. PerfCollect uses [perf](https://perf.wiki.kernel.org/index.php/Main_Page), which gives you native callstacks. dotnet-trace can only give you managed callstack.

3. PerfCollect has a machine-wide scope, so it can be used to capture events from multiple processes running on the same machine. dotnet-trace is specific to a single runtime instance.

4. PerfCollect can be started prior to the process start, whereas dotnet-trace can only be attached after the process has started and runtime has set up the necessary internal data structures to allow attach.

5. PerfCollect supports .NET Core 2.1 or later. dotnet-trace supports .NET Core 3.0 or later.

# LTTng and PerfCollect (.NET Core 2.1 or later) #

## Required Tools ##
- **perfcollect**: Bash script that automates data collection.
	- Available at <https://aka.ms/perfcollect>.
- **PerfView**: Windows-based performance tool that can also analyze trace files collected with Perfcollect.
	- Available at <https://aka.ms/perfview>.

## Preparing Your Machine ##
Follow these steps to prepare your machine to collect a performance trace.

1. Download Perfcollect.

	> ```bash
	> curl -OL https://aka.ms/perfcollect
	> ```

2. Make the script executable.

	> ```bash
	> chmod +x perfcollect
	> ```

3. Install tracing prerequisites - these are the actual tracing libraries.  For details on prerequisites, see [below](#prerequisites).

	> ```bash
	> sudo ./perfcollect install
	> ```

## Collecting a Trace ##
1. Have two shell windows available - one for controlling tracing, referred to as **[Trace]**, and one for running the application, referred to as **[App]**.
2. **[App]** Setup the application shell - this enables tracing configuration inside of CoreCLR.

    > ```bash
	> export DOTNET_PerfMapEnabled=1
	> export DOTNET_EnableEventLog=1
	> ```

   Note:
   DOTNET_PerfMapEnabled will cause the .NET runtime to write a file containing symbolic information for managed code to the disk. Depending on the performance of your disk and the amount of managed code in the application this could have a significant performance overhead.

3. **[Trace]** Start collection.

	> ```bash
	> sudo ./perfcollect collect sampleTrace
	> ```

	Expected Output:

	> ```bash
	> Collection started.  Press CTRL+C to stop.
	> ```

4. **[App]** Run the app - let it run as long as you need to in order to capture the performance problem.  Generally, you don't need very long.  As an example, for a CPU investigation, 5-10 seconds of the high CPU situation is usually enough.

	> ```bash
	> dotnet run
	> ```

5. **[Trace]** Stop collection - hit CTRL+C.

	> ```bash
	> ^C
	> ...STOPPED.
	>
	> Starting post-processing. This may take some time.
	>
	> Generating native image symbol files
	> ...SKIPPED
	> Saving native symbols
	> ...FINISHED
	> Exporting perf.data file
	> ...FINISHED
	> Compressing trace files
	> ...FINISHED
	> Cleaning up artifacts
	> ...FINISHED
	>
	> Trace saved to sampleTrace.trace.zip
	> ```

	The compressed trace file is now stored in the current working directory.

## Resolving Framework Symbols ##
Framework symbols need to be manually generated at the time the trace is collected.  They are different than app-level symbols because the framework is pre-compiled while apps are just-in-time-compiled.  For code like the framework that was precompiled to native
code, you need a special tool called crossgen that knows how to generate the mapping from the native code to the name of the
methods.

Perfcollect can handle most of the details for you, but it needs to have the crossgen tool and by default this is NOT part of
the standard .NET distribution.   If it is not there it warns you and refers you to these instructions.   To fix things you
need to fetch EXACTLY the right version of crossgen for the runtime you happen to be
using.  If you place the crossgen tool in the same directory as the .NET Runtime DLLs (e.g. libcoreclr.so), then perfcollect can
find it and add the framework symbols to the trace file for you.

Normally when you create a .NET application, it just generates the DLL for the code you wrote, using a shared copy of the runtime
for the rest.   However you can also generate what is called a 'self-contained' version of an application and this contains all
runtime DLLs.  It turns out that the crossgen tool is part of the Nuget package that is used to create these self-contained apps, so
one way of getting the right crossgen tool is to create a self contained package of any application.

So you could do the following
   >```bash
   > mkdir helloWorld
   > cd helloWorld
   > dotnet new console
   > dotnet publish --self-contained -r linux-x64
   >```
Which creates a new helloWorld application and builds it as a self-contained app.    The only subtlety here is that if you have
multiple versions of the .NET Runtime installed, the instructions above will use the latest.  As long as your app also uses
the latest (likely) then these instructions will work without modification.

As a side effect of creating the self-contained application the dotnet tool will download a nuget package
called runtime.linux-x64.microsoft.netcore.app and place it in
the directory ~/.nuget/packages/runtime.linux-x64.microsoft.netcore.app/VERSION, where VERSION is the version number of
your .NET Core runtime (e.g. 2.1.0).   Under that is a tools directory and inside there is the crossgen tool you need.
Starting with .NET Core 3.0, the package location is ~/.nuget/packages/microsoft.netcore.app.runtime.linux-x64/VERSION.

The crossgen tool needs to be put next to the runtime that is actually used by your application.   Typically your app uses the shared
version of .NET Core that is installed at /usr/share/dotnet/shared/Microsoft.NETCore.App/VERSION where VERSION is the
version number of the .NET Runtime.   This is a shared location, so you need to be super-user to modify it.   If the
VERSION is 2.1.0 the commands to update crossgen would be
   >```bash
   > sudo bash
   > cp ~/.nuget/packages/runtime.linux-x64.microsoft.netcore.app/2.1.0/tools/crossgen /usr/share/dotnet/shared/Microsoft.NETCore.App/2.1.0
   >```

Once you have done this, perfcollect will use crossgen to include framework symbols.  The warning that perfcollect used to
issue should go away.   This only has to be one once per machine (until you update your runtime).

### Alternative: Turn off use of precompiled code ###

If you don't have the ability to update the .NET Runtime (to add crossgen), or if the above procedure did not work
for some reason, there is another approach to getting framework symbols.   You can tell the runtime to simply
not use the precompiled framework code.   The code will be Just in time compiled and the special crossgen tool
is not needed.   This works, but will increase startup time for your code by something like a second or two.  If you
can tolerate that (you probably can), then this is an alternative.   You were already setting environment variables
in order to get symbols, you simply need to add one more.
	> ```bash
	> export DOTNET_ReadyToRun=0
	> ```
With this change you should get the symbols for all .NET code.

## Getting Symbols For the Native Runtime ##

Most of the time you are interested in your own code, which perfcollect resolves by default.   Sometimes it is very
useful to see what is going on inside the .NET Framework DLLs (which is what the last section was about), but sometimes
what is going on in the NATIVE runtime dlls (typically libcoreclr.so), is interesting.  perfcollect will resolve the
symbols for these when it converts its data, but ONLY if the symbols for these native DLLs are present (and are beside
the library they are for).

There is a global command called [dotnet symbol](https://github.com/dotnet/symstore/blob/master/src/dotnet-symbol/README.md#symbol-downloader-dotnet-cli-extension) which does this.   This tool was mostly desiged to download symbols
for debugging, but it works for perfcollect as well.  There are two steps to getting the symbols:

   1. Install dotnet symbol.
   2. Download the symbols.

To install dotnet symbol issue the command
```
     dotnet tool install -g dotnet-symbol
```

To download symbols for **all native libraries** (including .NET runtime/framework as well as any other installed frameworks like ASP.NET) and store them next to them:

```
    sudo dotnet symbol --recurse-subdirectories --symbols '/usr/share/dotnet/*.so'
```

## Collecting in a Docker Container ##
Perfcollect can be used to collect data for an application running inside a Docker container.  The main thing to know is that collecting a trace requires elevated privileges because the [default seccomp profile](https://docs.docker.com/engine/security/seccomp/) blocks a required syscall - perf_events_open.

In order to use the instructions in this document to collect a trace, spawn a new shell inside the container that is privileged.

>```bash
>docker exec -it --privileged <container_name> /artifacts/bash
>```

Even though the application hosted in the container isn't privileged, this new shell is, and it will have all the privileges it needs to collect trace data.  Now, simply follow the instructions in [Collecting a Trace](#collecting-a-trace) using the privileged shell.

If you want to try tracing in a container, we've written a [demo Dockerfile](https://raw.githubusercontent.com/microsoft/perfview/master/src/perfcollect/docker-demo/Dockerfile) that installs all of the performance tracing pre-requisites, sets the environment up for tracing, and starts a sample CPU-bound app.

## Filtering ##
Filtering is implemented on Windows through the latest mechanisms provided with the [EventSource](https://msdn.microsoft.com/en-us/library/system.diagnostics.tracing.eventsource(v=vs.110).aspx) class.

On Linux those mechanisms are not available yet. Instead, there are two environment variables that exist just on linux to do some basic filtering.

* DOTNET_EventSourceFilter – filter event sources by name
* DOTNET_EventNameFilter – filter events by name

Setting one or both of these variables will only enable collecting events that contain the name you specify as a substring. Strings are treated as case insensitive.

## Viewing a Trace ##
Traces are best viewed using PerfView on Windows.  Note that we're currently looking into porting the analysis pieces of PerfView to Linux so that the entire investigation can occur on Linux.

### Open the Trace File ###
1. Copy the trace.zip file from Linux to a Windows machine.
2. Download PerfView from <https://aka.ms/perfview>.
3. Run PerfView.exe

	> ```cmd
	> PerfView.exe <path to trace.zip file>
	> ```

### Select a View ###
PerfView will display the list of views that are supported based on the data contained in the trace file.

- For CPU investigations, choose **CPU stacks**.
- For very detailed GC information, choose **GCStats**.
- For per-process/module/method JIT information, choose **JITStats**.
- If there is not a view for the information you need, you can try looking for the events in the raw events view.  Choose **Events**.

For more details on how to interpret views in PerfView, see help links in the view itself, or from the main window in PerfView choose **Help->Users Guide**.

## Extra Information ##
This information is not strictly required to collect and analyze traces, but is provided for those who are interested.

### Prerequisites ###
Perfcollect will alert users to any prerequisites that are not installed and offer to install them.  Prerequisites can be installed automatically by running:

>```bash
>sudo ./perfcollect install
>```

The current prerequisites are:

1. perf: Also known as perf_event, the Linux Performance Events sub-system and companion user-mode collection/viewer application.  perf is part of the Linux kernel source, but is not usually installed by default.
2. LTTng: Stands for "Linux Tracing Toolkit Next Generation", and is used to capture event data emitted at runtime by CoreCLR.  This data is then used to analyze the behavior of various runtime components such as the GC, JIT and thread pool.



# EventPipe and dotnet-trace (.NET Core 3.0 or later)

## Intro ##
EventPipe is a new cross-platform tracing mechanism we built into the runtime from .NET Core 3.0. It works the same across all platforms we support (Windows, macOS, and Linux), and we have built various diagnostics tools on top of it. dotnet-trace is a dotnet CLI tool that allows you to trace your .NET application using EventPipe.

## Installing dotnet-trace ##
dotnet-trace can be installed by using the dotnet CLI:
```
dotnet tool install --global dotnet-trace
```

## Collecting a trace ##
To see which .NET processes are available for collecting traces on, you can run the following command to get their process IDs (PID):
```
dotnet-trace ps
```

Once you know the PID of the process you want to collect traces, you can run the following command to start tracing:
```
dotnet-trace collect --process-id <PID>
```

## Viewing the Trace ##
The resulting trace can be viewed in [PerfView](https://aka.ms/perfview) on Windows. Alternatively on Linux/macOS, it can be viewed on [SpeedScope](https://speedscope.app) if you convert the trace format to speedscope by passing `--format speedscope` argument when collecting the trace.

## More Information ##
To read more about how to use dotnet-trace, please refer to the [dotnet-trace documentation](https://github.com/dotnet/diagnostics/blob/main/documentation/dotnet-trace-instructions.md).

