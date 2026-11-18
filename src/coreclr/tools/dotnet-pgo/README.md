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

## Creating a .mibc file from a method list

The `create-mibc-from-method-list` command creates a deterministic profile root set without collecting
a trace. This is an exploratory, general-purpose way to construct profiles; it is not required for
automatic hardware-intrinsic rooting in partial no-JIT compilations.

The input is a JSON object with the following properties:

- `runtime`, `os`, and `architecture`: non-empty strings written to the MIBC configuration.
- `methods`: a non-empty array of method entries.
- `methods[].type`: the assembly-qualified declaring type name. Constructed generic types must also
  use assembly-qualified generic arguments.
- `methods[].name`: the metadata method name.
- `methods[].parameterTypes`: optional assembly-qualified parameter type names used to select an
  overload. Omit only when the name and generic arity identify exactly one method.
- `methods[].genericArguments`: optional assembly-qualified type names for a closed generic method
  instantiation.

All referenced assemblies, including `System.Private.CoreLib`, must be passed with `--reference`.
Open generic types and methods are not supported. Duplicate resolved methods are rejected.

```json
{
  "runtime": "CoreCLR",
  "os": "linux",
  "architecture": "x64",
  "methods": [
    {
      "type": "Example.ProfileRoots, Example",
      "name": "Parse",
      "parameterTypes": [
        "System.String, System.Private.CoreLib"
      ]
    },
    {
      "type": "Example.ProfileRoots, Example",
      "name": "Create",
      "genericArguments": [
        "System.Int32, System.Private.CoreLib"
      ],
      "parameterTypes": [
        "System.Int32, System.Private.CoreLib"
      ]
    }
  ]
}
```

```console
dotnet-pgo create-mibc-from-method-list \
  --method-list methods.json \
  --reference System.Private.CoreLib.dll \
  --reference Example.dll \
  --output methods.mibc
```

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
