# dotnet-pgo Spec
Utilize trace data for improving application performance

NOTE: This documentation page contains information on some features that are still work-in-progress.

## Intro

The dotnet-pgo tool is a cross-platform CLI global tool that enables conversion of traces of .NET Core applications collected via dotnet-trace, ETW, perfview, perfcollect, LTTNG to be used to improve the performance of an application or library.

## Installing dotnet-pgo

The first step is to install the dotnet-pgo CLI global tool.

```cmd
$ dotnet tool install --global dotnet-pgo
You can invoke the tool using the following command: dotnet-pgo
Tool 'dotnet-pgo' (version '6.0.47001') was successfully installed.
```

## Using dotnet-pgo to optimize an application

In order to use dotnet-pgo you will need to:

1. Collect traces of you application with at least the following trace setting. `Microsoft-Windows-DotNETRuntime:0x1C000080018:4`. For best results enable instrumented code generation.
2. Run the dotnet-pgo tool specifying the trace file collected above to create a mibc file.
3. Pass the mibc file to a `dotnet publish` command. Specify the file via the `ReadyToRunOptimizationData` property. Multiple files may be passed separated by `;` characters.

### Examples

Given a project located in the current directory named pgotest, an example workflow for using `dotnet-pgo` could be the following. The adjusted environment variables are used to enable instrumented code generation.

```
dotnet build -p:Configuration=Release

set COMPLUS_TieredPGO=1
set COMPLUS_TC_QuickJitForLoops=1
set COMPLUS_TC_CallCountThreshold=10000
set COMPLUS_ZapDisable=1

dotnet-trace collect --providers Microsoft-Windows-DotNETRuntime:0x1E000080018:4 -- bin\Release\net6.0\pgotest.exe

set COMPLUS_TieredPGO=
set COMPLUS_TC_QuickJitForLoops=
set COMPLUS_TC_CallCountThreshold=
set COMPLUS_ZapDisable=

dotnet-pgo --trace trace.nettrace --output trace.mibc

dotnet publish --runtime win-x64 -p:PublishReadyToRun=true -p:ReadyToRunOptimizationData=trace.mibc
```

## Command line reference for dotnet-pgo

dotnet-pgo supports two commands. The `create-mibc` is used to convert a trace into a Mibc file for usage by the .NET build. The `merge` command is used to merge multiple mibc files together to reduce usage complexity when an application uses several test scenarios.

```
usage: dotnet-pgo <command> [<args>]

    create-mibc    Transform a trace file into a Mibc profile data file.
    merge          Merge multiple Mibc profile data files into one file.
```

### `create-mibc` command
Transform a trace file into a Mibc profile data file.
```
usage: dotnet-pgo create-mibc [-t <arg>] [-o <arg>] [--pid <arg>] [--process-name [arg]] [--clr-instance-id <arg>] [-r <arg>...] [--exclude-events-before <arg>] [--exclude-events-after <arg>] [-v <arg>] [--compressed] [-h]

    -t, --trace <arg>                Specify the trace file to be parsed.
    -o, --output <arg>               Specify the output filename to be created.
    --pid <arg>                      The pid within the trace of the process to examine. If this is a multi-process trace, at least one of --pid or --process-name must be specified
    --process-name [arg]             The process name within the trace of the process to examine. If this is a multi-process trace, at least one of --pid or --process-name must be specified.
    --clr-instance-id <arg>          If the process contains multiple .NET runtimes, the instance ID must be specified.
    -r, --reference <arg>...         If a reference is not located on disk at the same location as used in the process, it may be specified with a --reference parameter. Multiple --reference parameters may be specified. The wild cards * and ? are supported by this option.
    --exclude-events-before <arg>    Exclude data from events before specified time. Time is specified as milliseconds from the start of the trace.
    --exclude-events-after <arg>     Exclude data from events after specified time. Time is specified as milliseconds from the start of the trace.
    -v, --verbosity <arg>            Adjust verbosity level. Supported levels are minimal, normal, detailed, and diagnostic.
    --compressed                     Generate compressed mibc
    -h, --help                       Display this usage message.
```

`dotnet-pgo create-mibc` is used to create mibc file from a trace. It supports nettrace, etl, netperf, and netperf.zip trace file formats. The `--trace-file` and `--output-file-name` switches must be specified.

If using trace file format that supports multiple process tracing, specify some combination of the `--pid`, `--process-name` and `--clr-instance-id` switches as needed. When run from the command line in situations where these arguments are needed, the valid args will be printed to the console. Usage of the nettrace file format is recommended for users attempting to run this tool as part of a CI system.

The `dotnet-pgo` tool is designed to run on a machine which has the same file system layout as the trace. If this is not the case, `dotnet-pgo` may not be able to find assemblies loaded by the application. In those cases, the `--reference` switch may be used to inform `dotnet-pgo` where to find assemblies that are part of the application.

The `--exclude-events-before` and `--exclude-events-after` switches can be used to control which parts of the execution are optimized.

The mibc file format may be compressed. This will require slightly more time to create and process, but it will produce a smaller binary if the mibc files needs to be distributed to multiple developers. Create a compressed mibc by using the `--compressed` switch.

### `merge` command
Merge multiple Mibc profile data files into one file.

```
usage: dotnet-pgo merge --input <arg>... --output-file-name <arg> [--exclude-reference <arg>] [--verbosity <arg>] [--compressed] [-h]

    -i, --input <arg>                Specify the trace file to be parsed.
    -o, --output-file-name <arg>     Specify the output filename to be created.
    --exclude-reference <arg>        Exclude references to the specified assembly from the output.
    -v, --verbosity <arg>            Adjust verbosity level. Supported levels are minimal, normal, detailed, and diagnostic.
    --compressed                     Generate compressed mibc
    -h, --help                       Display this usage message.
```

`dotnet-pgo merge` is used to merge multiple mibc files into one. This command is used to reduce the burden for distribution of multiple different mibc files.

At least one `--input` switch must be specified. Only one `--output-file-name` may be specified.

The `--exclude-reference` switch may be used to remove any references to the specified assembly from the output mibc.

The mibc file format may be compressed. This will require slightly more time to create and process, but it will produce a smaller binary if the mibc files needs to be distributed to multiple developers. Create a compressed mibc by using the `--compressed` switch.
