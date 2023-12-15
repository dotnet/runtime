Profiled Ahead-Of-Time Compilation on Mono
===

# Background

Mobile applications built using .NET typically leverage the Mono runtime to load and execute native machine code. The native machine code is generated from common intermediate language (CIL) assemblies using the Mono compiler with a variety of compilation strategies available including ahead-of-time (AOT) compilation, just-in-time (JIT) compilation, and interpreter. In addition to these strategies, .NET 7 introduced Profiled Ahead-Of-Time Compilation on Mono, a combination of AOT compilation and profile-guided-optimization (PGO) that leverages "profiles" to select which CIL code to AOT rather than an all-or-nothing approach. These profiles are obtained through [tracing](https://github.com/dotnet/runtime/blob/main/docs/design/mono/diagnostics-tracing.md) previous runs of an application, and the resulting trace is analyzed by the [dotnet-pgo tool](https://github.com/dotnet/runtime/blob/main/docs/design/features/dotnet-pgo.md) to generate a profile that tells the Mono AOT Compiler which methods to AOT.

# How it works

The advantages of Profiled AOT stem from its flexibility to AOT select code paths, leaving the rest to be compiled on the fly by the JIT compiler or Mono Interpreter. With an analysis of an application's trace, a record capturing a sequence of events during the application's execution, profiles can be generated to tailor optimizations for each application and environment. For example, profiles may target frequently executed (hot) code paths, minimizing the amount of runtime compilations and reducing application size, which are especially important in environments where full AOT compilation would strain storage space. Moreover, profiles may target startup code to optimize startup performance.

Within .NET, traces can be collected by [diagnostic tooling](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/eventpipe#tools-that-use-eventpipe) that use the [EventPipe](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/eventpipe) runtime component. [Existing diagnostic tooling only supports `NamedPipes`/`UnixDomainSockets`](https://github.com/dotnet/runtime/blob/main/docs/design/mono/diagnostics-tracing.md), so the [diagnostics tool dotnet-dsrouter](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/dotnet-dsrouter) is required to bridge the EventPipe-based diagnostic tooling with .NET applications on mobile platforms and other remote sandboxed environments.

Events collected by EventPipe-based diagnostic tooling are emitted with a [`.nettrace` file format](https://github.com/microsoft/perfview/blob/main/src/TraceEvent/EventPipe/EventPipeFormat.md). Among the [various events supported by Mono runtime](https://github.com/dotnet/runtime/blob/main/src/mono/mono/eventpipe/gen-eventing-event-inc.lst), [method jitting and method loading](https://github.com/dotnet/runtime/blob/096b2499fe6939d635c35edaa607a180eb578fbb/src/mono/mono/eventpipe/gen-eventing-event-inc.lst#L39-L41) are crucial to [inform the Mono AOT Compiler what methods to AOT](https://github.com/dotnet/runtime/blob/6b67caaedfbfeaf7707478e50ccc9e8bc929e591/src/mono/mono/mini/aot-compiler.c#L13818-L13880). To collect a trace containing such events, it is imperative that dotnet-trace is provided either the appropriate [event provider](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/well-known-event-providers) with [keyword flags](https://github.com/dotnet/runtime/blob/c59aef7622c9a2499abb1b7d262ed0c90f4b0c7f/src/coreclr/vm/ClrEtwAll.man#L14-L92) through `--providers` or the appropriate list of keywords through `--clrevents`. That way, the [events relevant to the keywords are captured](https://github.com/dotnet/runtime/blob/c59aef7622c9a2499abb1b7d262ed0c90f4b0c7f/src/coreclr/vm/ClrEtwAll.man#L3133). In the example workflows below, `--providers Microsoft-Windows-DotNETRuntime:0x1F000080018:5` is used.

Profiles [ingested by the Mono AOT Compiler](https://github.com/dotnet/runtime/blob/6b67caaedfbfeaf7707478e50ccc9e8bc929e591/src/tasks/AotCompilerTask/MonoAOTCompiler.cs#L174) are generated through .NET runtime's [`dotnet-pgo` tool](https://github.com/dotnet/runtime/blob/main/docs/design/features/dotnet-pgo.md). As such, profiles passed to the Mono AOT Compiler are expected to adhere to the [`.mibc` file format](https://github.com/dotnet/runtime/blob/main/src/coreclr/tools/dotnet-pgo/dotnet-pgo-experiment.md#mibc-file-format). The Mono AOT Compiler [reads `.mibc` profiles](https://github.com/dotnet/runtime/blob/c59aef7622c9a2499abb1b7d262ed0c90f4b0c7f/src/mono/mono/mini/aot-compiler.c#L14085-L14162) to determine [which methods to AOT](https://github.com/dotnet/runtime/blob/6b67caaedfbfeaf7707478e50ccc9e8bc929e591/src/mono/mono/mini/aot-compiler.c#L13818-L13880) when compiling CIL assemblies.

## Mono Profiled AOT Compilation

The Mono AOT Compiler can [directly ingest `.mibc` profiles](https://github.com/dotnet/runtime/pull/70194) to AOT compile methods contained within the profile. As the Mono AOT Compiler already had logic to AOT compile profile methods (from the legacy mono profiler), resolving [`MonoMethod`s](https://github.com/dotnet/runtime/blob/18cb172309570de25a2df8660ec2a6e3d0db610b/src/mono/mono/metadata/class-internals.h#L67) from the `.mibc` profile and [adding them for compilation](https://github.com/dotnet/runtime/blob/18cb172309570de25a2df8660ec2a6e3d0db610b/src/mono/mono/mini/aot-compiler.c#L13842-L13846) was sufficient.

As the [`.mibc` profile](https://github.com/dotnet/runtime/blob/main/src/coreclr/tools/dotnet-pgo/dotnet-pgo-experiment.md#mibc-file-format) is a Portable Executable (PE) file, the Mono AOT Compiler leverages several mono methods to resolve the profile data as `MonoMethod`s by:

1. Opening the `.mibc` profile as a `MonoImage` to load the `AssemblyDictionary` as a `MonoMethod`.
2. Iterating through the `AssemblyDictionary`'s IL instructions to discover tokens corresponding to `mibcGroupMethod`s within the profile.
3. Resolving each `mibcGroupMethod` discovered as a `MonoMethod`.
4. Iterating through the `mibcGroupmethod`'s IL instructions to discover tokens corresponding to method/type tokens within the `mibcGroupMethod`.
5. Resolving `MethodRef` and `MethodSpec` tokens as `MonoMethod`s corresponding to the actual method to be profile AOT'd, and inserting them into the profile methods hash table.

# Example Workflows

## Android -- Running through the [Android Profiled AOT Functional Test](https://github.com/dotnet/runtime/tree/main/src/tests/FunctionalTests/Android/Device_Emulator/AOT_PROFILED)

### Requirements:
- [Prerequisites](https://github.com/dotnet/runtime/blob/main/docs/workflow/testing/libraries/testing-android.md#prerequisites)
- [Building mono and libs](https://github.com/dotnet/runtime/blob/main/docs/workflow/testing/libraries/testing-android.md#building-libs-and-tests-for-android)
- [dotnet-dsrouter](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/dotnet-dsrouter)
- [dotnet-trace](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/dotnet-trace)

### Tracing (if not using provided .nettrace/.mibc files)
(informational) [Understanding diagnostics_tracing runtime component](https://github.com/dotnet/runtime/blob/main/docs/design/mono/diagnostics-tracing.md)

1. Startup dotnet-dsrouter to bridge device/emulator with diagnostic tooling either in a separate window or launch background instance with a `&` at the end.
```C#
dotnet-dsrouter client-server -tcps 127.0.0.1:9001 -ipcc ~/myport --verbose debug
```

2. Startup dotnet-trace tool to collect a .nettrace in a separate window
```C#
dotnet-trace collect --providers Microsoft-Windows-DotNETRuntime:0x1F000080018:5 --diagnostic-port ~/myport
```

3. Run the Android application enabling `RuntimeComponents` + `DiagnosticPorts` and disabling `NetTraceFilePath` + `ProfiledAOTProfilePaths` in the `.csproj`. These properties are consumed in the Android [build](https://github.com/dotnet/runtime/blob/main/src/mono/msbuild/android/build/AndroidBuild.targets).
```Shell
./dotnet.sh build /t:Test /p:TargetOS=android /p:TargetArchitecture=arm64 /p:Configuration=Debug src/tests/FunctionalTests/Android/Device_Emulator/AOT_PROFILED/Android.Device_Emulator.Aot_Profiled.Test.csproj
```

### Profiled AOT

1. Run the Android application supplying either the default or your own `.nettrace file` in `NetTraceFilePath` and/or `.mibc files` in `ProfiledAOTProfilePaths` and disabling `RuntimeComponents` + `DiagnosticPorts` in the `.csproj`. These properties are consumed in the Android [build](https://github.com/dotnet/runtime/blob/main/src/mono/msbuild/android/build/AndroidBuild.targets).
```Shell
./dotnet.sh build /t:Test /p:TargetOS=android /p:TargetArchitecture=arm64 /p:Configuration=Debug src/tests/FunctionalTests/Android/Device_Emulator/AOT_PROFILED/Android.Device_Emulator.Aot_Profiled.Test.csproj
```
