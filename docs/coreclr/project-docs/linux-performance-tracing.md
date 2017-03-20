Performance Tracing on Linux
============================

When a performance problem is encountered on Linux, these instructions can be used to gather detailed information about what was happening on the machine at the time of the performance problem.

# Required Tools #
- **perfcollect**: Bash script that automates data collection.
	- Available at <http://aka.ms/perfcollect>.
- **PerfView**: Windows-based performance tool that can also analyze trace files collected with Perfcollect.
	- Available at <http://aka.ms/perfview>.

# Preparing Your Machine #
Follow these steps to prepare your machine to collect a performance trace.

1. Download Perfcollect.

	> ```bash
	> curl -OL http://aka.ms/perfcollect
	> ```

2. Make the script executable.

	> ```bash
	> chmod +x perfcollect
	> ```

3. Install tracing prerequisites - these are the actual tracing libraries.  For details on prerequisites, see [below](#prerequisites).

	> ```bash
	> sudo ./perfcollect install
	> ```

# Collecting a Trace #
1. Have two shell windows available - one for controlling tracing, referred to as **[Trace]**, and one for running the application, referred to as **[App]**.
2. **[App]** Setup the application shell - this enables tracing configuration inside of CoreCLR.

	> ```bash 
	> export COMPlus_PerfMapEnabled=1
	> export COMPlus_EnableEventLog=1
	> ```

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

# Collecting in a Docker Container #
Perfcollect can be used to collect data for an application running inside a Docker container.  The main thing to know is that collecting a trace requires elevated privileges because the [default seccomp profile](https://docs.docker.com/engine/security/seccomp/) blocks a required syscall - perf_events_open.

In order to use the instructions in this document to collect a trace, spawn a new shell inside the container that is privileged.

>```bash
>docker exec -it --privileged <container_name> /bin/bash
>```

Even though the application hosted in the container isn't privileged, this new shell is, and it will have all the privileges it needs to collect trace data.  Now, simply follow the instructions in [Collecting a Trace](#collecting-a-trace) using the privileged shell.

If you want to try tracing in a container, we've written a [demo Dockerfile](https://raw.githubusercontent.com/dotnet/corefx-tools/master/src/performance/perfcollect/docker-demo/Dockerfile) that installs all of the performance tracing pre-requisites, sets the environment up for tracing, and starts a sample CPU-bound app.

# Filtering #
Filtering is implemented on Windows through the latest mechanisms provided with the [EventSource](https://msdn.microsoft.com/en-us/library/system.diagnostics.tracing.eventsource(v=vs.110).aspx) class. 

On Linux those mechanisms are not available yet. Instead, there are two environment variables that exist just on linux to do some basic filtering. 

* COMPLUS_EventSourceFilter – filter event sources by name
* COMPLUS_EventNameFilter – filter events by name

Setting one or both of these variables will only enable collecting events that contain the name you specify as a substring. Strings are treated as case insensitive. 

# Viewing a Trace #
Traces are best viewed using PerfView on Windows.  Note that we're currently looking into porting the analysis pieces of PerfView to Linux so that the entire investigation can occur on Linux.

## Open the Trace File ##
1. Copy the trace.zip file from Linux to a Windows machine.
2. Download PerfView from <http://aka.ms/perfview>.
3. Run PerfView.exe

	> ```cmd
	> PerfView.exe <path to trace.zip file>
	> ```

## Select a View ##
PerfView will display the list of views that are supported based on the data contained in the trace file.

- For CPU investigations, choose **CPU stacks**.
- For very detailed GC information, choose **GCStats**.
- For per-process/module/method JIT information, choose **JITStats**.
- If there is not a view for the information you need, you can try looking for the events in the raw events view.  Choose **Events**. 

For more details on how to interpret views in PerfView, see help links in the view itself, or from the main window in PerfView choose **Help->Users Guide**.

# Extra Information #
This information is not strictly required to collect and analyze traces, but is provided for those who are interested.

## Prerequisites ##
Perfcollect will alert users to any prerequisites that are not installed and offer to install them.  Prerequisites can be installed automatically by running:

>```bash
>sudo ./perfcollect install
>```

The current prerequisites are:

1. perf: Also known as perf_event, the Linux Performance Events sub-system and companion user-mode collection/viewer application.  perf is part of the Linux kernel source, but is not usually installed by default.
2. LTTng: Stands for "Linux Tracing Toolkit Next Generation", and is used to capture event data emitted at runtime by CoreCLR.  This data is then used to analyze the behavior of various runtime components such as the GC, JIT and thread pool.
