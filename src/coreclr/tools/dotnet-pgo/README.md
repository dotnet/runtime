dotnet-pgo tool
===========================

This directory contains the complete source code for the experimental dotnet-pgo tool and associated libraries

This tool is used to produce .jittrace files which are used to time-shift JIT compilation from later in the process to earlier in the process, or to produce .mibc files which are used as input to the crossgen2 tool.

The general notion is to collect a trace of the application timeframe which is to be optimized using either dotnet trace, or perfview. Then run the dotnet-pgo tool to post-process the trace data into a profile data file that can be consumed by either the application at runtiome (.jittrace file) or the AOT crossgen2 compiler (.mibc file).

## Building the dotnet-pgo tool
Run dotnet build from the root directory

## Consuming a .jittrace file
Copy the code in the Microsoft.Diagnostics.JitTrace directory into the application, and then follow the following steps.

```
using Microsoft.Diagnostics.JitTrace
...
static void FunctionCalledAtStartup()
{
    JitTraceRuntime.Prepare(@"Somefile.jittrace");
}
```

## Consuming a .mibc file
Invoke the `crossgen2` tool with the --mibc option, passing the .mibc file created by dotnet-pgo.

## Example tracing commands used to generate the input to this tool:
Note, this tool requires MethodDetails events which are produced by the .NET 5.0 runtime, or by modifying the .NET Core 3 runtime to produce the event.

- Capture events from process 73060 where we capture both JIT and R2R events using EventPipe tracing
```
"dotnet trace collect -p 73060 --providers Microsoft-Windows-DotNETRuntime:0x6000080018:5"
```

- Capture events from process 73060 where we capture only JIT events using EventPipe tracing
```
"dotnet trace collect -p 73060 --providers Microsoft-Windows-DotNETRuntime:0x4000080018:5"
```

- Capture Jit and R2R events via perfview of all processes running using ETW tracing
```
"perfview collect -LogFile:logOfCollection.txt -DataFile:jittrace.etl -Zip:false -merge:false -providers:Microsoft-Windows-DotNETRuntime:0x6000080018:5"
```

## Example commands for using dotnet-pgo
- Given the etlfile jittrace.etl produced from perfview collect, create a matching jittrace file named jitdata.jittrace based on the data in the process named jittracetest that ran during collection of the etw data. While processing, print out all of the events processed, and warnings for methods which could not be processed.
```
H:\git\jittrace\src\Tools\dotnet-pgo\bin\Debug\netcoreapp3.0\dotnet-pgo.exe --trace-file  jittracewithlog.etl --process-name jittracetest --output-file-name withlog.jittrace --pgo-file-type jittrace --display-processed-events true
```
