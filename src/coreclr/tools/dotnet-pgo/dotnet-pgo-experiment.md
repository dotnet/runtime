# Experiments towards a Profile Data pipeline for .NET
-----
The .NET Runtime has a long history of providing instrumentation based profile guided optimization
for use internally at Microsoft, and for scenarios involving extremely high value customers. To
this end the team built the IBC (instrumented block count) infrastructure into the runtime/ngen,
and IBCMerge as a tool for manipulating .ibc files. Over the last few years, the structure of these
technologies and tools has shown that they are not ideal for customer use or even internal use, and
a new tech would be valuable in this space.

To that end, I have produced this proposal for providing a new trace based profile guided optimization
for .NET. The proposal aims to solve the major issues with our current pgo technology, and leverage
the investments that the .NET team has made over the last few years to make it possible.

The design is based on the following principles.

1. Consumption of pgo data for use in an application shall be as simple as adding a single line to
a csproj file or build script.
2. Storage format of pgo data shall be version resilient.
3. Export of data from the runtime using the standard dotnet tracing capabilities instead of a custom hand rolled scheme.
4. Pgo data shall be gathered at a scenario level instead of a per output file level
5. Production of pgo data shall utilize the normal build of the application
6. Pgo data comes in tiers. Not all scenarios will call for capture of all data
7. Any high volume Pgo data shall support sampling, such that the vast majority of applications can continue to execute with instrumentation enabled
8. The proposal shall be designed around the principles of flowing information between various RyuJit operations. Any references to specific forms of data such as block count, or type feedback are not truly topical for this document, but serve as examples of the sort of data that might be transited through from compile to compile.

## Purpose of PGO
Profile guided optimization in .NET is used to provide benefits for 3 major concerns.

1. Startup Time
2. Application Size on Disk
3. Application Throughput Performance

Startup time for an application is primarily improved by avoiding the use of the JIT by ahead of time
compiling methods in the application. In addition a profile can allow determination of which methods
are hot vs cold, and group methods commonly used together with others. This has been the primary use
of pgo in .NET historically.

Pgo is used to address size on disk concerns of R2R binaries where the default R2R strategy is too
aggressive and produces binaries that are excessively large. The idea in that case is to only generate
the functions specifically referenced in some profile instead of every method the heuristic indicates
may be interesting.

Application throughput performance has historically been the primary use of pgo data for C++ compilers.
.NET has history with the use of instrumented per block counts, but this data is not generally processed
in an effective manner by the JIT. This proposal aims to revitalize efforts to make good use of profile
guided data to improve code quality. Over time, it is expected that not only will profile data be used at
build time, but that it will also be used to do runtime profile instrumentation.

# Proposal Contents
Profile guided optimization is a combination of effort across a swath of components.

1. Trace data format (For exporting instrumentation data from the runtime through the trace collection tool into the trace processing tool.
2. PGO file format (for storage of pgo data between instrumentation and Aot compilation)
3. ReadyToRun file format (some PGO data is relevant to a Tier 1 compilation such as block counts)

And there are a series of components that need to be modified

1. Instrumenting runtime (coreclr)
2. Instrumenting jit (clrjit)
3. Trace processing tool (dotnet-pgo)
4. AOT compilation tool (crossgen2)
6. Consuming runtime (coreclr)
7. Diagnostic tools (r2rdump, dotnet-pgo)

## Conceptual model of `InstrumentationData`
`InstrumentationData` is information used to optimize code in a manner which cannot be determined
statically, and instead is determined through instrumentation of the code. The format of this data
is expected to be defined by the JIT team, and be specific to the probes inserted, and may very well
change over time. It is composed of two sections

1. The descriptor used to describe the probes, this is fixed at JIT time, and describes the meaning of the data.
2. The data gathered as counts, and values that will be used to perform further optimization.

Both of these data blocks are able to contain type and method data, where the concept is that it is
most likely useful to describe methods in the descriptor, and there are known cases such as TypeFeedback
where it would be useful to describe types in the data section (such as for devirtualization and such),
but there are also plausible cases for gathering each kind of data in both section, so the format will
be made general to support both. Instrumentation Data shall have a version number independent of the
general R2R versioning scheme. The intention is for this form of `InstrumentationData` to become
usable for both out of line instrumentation as described in this document, as well as only tiered
compilation rejit scenarios with in process profiling.

## Trace data format
Runtime instrumentation will be accomplished through 4 events, 2 of which are already existing

1. Which methods are used by a process is exposed through the combination of the `R2REntryPoint` event as well as the `JitStarted` event. These methods are in place today, and do not need augmentation
2.  New events to report instrumentation data and counts.
- `InstrumentationDesc` shall consist of an 8 byte identifier for the instrumentation, a 4 byte instrumentation data format version number, followed by 3 byte arrays.
  - The 8 byte identifier has no particular meaning other than to associate an InstrumentationDesc with followon InstrumentationData events.
  - The first byte array or `DescShape` array contains 2 bits for each 8 byte sized region in the `Desc` array. A bit pattern of `00` indicates simple byte data, a `01` represents a `TypeHandle`, `10` represents a `MethodDesc`, and `11` is reserved for future use.
  - The second byte array or `Desc` array describes the instrumentation point. The first 8 byte sized region of the data must be a `MethodDesc` or `TypeHandle`, and followon data is used to describe the other data associated with the Instrumentation. For instance, there may be a hash of the IL code so that a future compiler may identify if the IL code has changed from build to build, and there may be a description of the IL ranges associated with offsets into the `InstrumentationData` event. This is a general purpose format. This data must have a length that is aligned to 8 byte boundaries.
  - The third byte array `DataShape` describes the shape of the actual instrumentation data. It follows the same rules as the `DescShape` array, but is used to describe the data to be captured in future `InstrumentationData` events.
- `InstrumentationData` shall consist of a 8 byte identifier that matches up with an identifier in the `InstrumentationDesc` as well as a byte array of captured instrumentation data. A new `InstrumentationData` event shall override the data for a previously generated event. This data must have a length that is aligned to 8 byte boundaries.
- Any type or method in data must be emitted via a `BulkType` or `MethodDetails` event before the instrumentation event is emitted.

This format is intended to be a long-lived format which can be extended over time as necessary, but without needing to update the associated tooling and infrastructure.

## MIBC File format
The PGO file format is designed with the goal of supporting the trace data format efficiently, and support
 compilation which only needs to read a portion of the file at a time. The proposal is to produce a .NET PE
 file and zip it into a container to reduce size on disk. The proposed file extension is .mibc.  (Within
 the container zip, the internal .NET PE file shall be named `SomeFileName.mibc.dll`.

In the PE file there shall be at least one global function, and it shall have a name of `AssemblyDictionary`. This function shall have a series of il instructions of the form
```
ldstr mibcGroupName
ldtoken mibcGroupMethod
pop
```

Where the presence of a `pop` instruction indicates that the group is complete, and a `ldstr` instruction
begins a group. Each group has a name of the format `<DefiningAssembly>;<OtherAssembly1>;<OtherAssembly2>;`
where there may be 0->Infinite other assemblies. As an example `System.Collections.Concurrent;` would be a
group where the contents are defined in `System.Collections.Concurrent` and where the components are not
defined in multiple assemblies. As a different example, `System.Private.CoreLib;System.Collections.Concurrent;System.Collections.Immutable;`
would be a different example, used for cases where the object being described is defined in `System.Private.CoreLib`,
but is reliant on definitions from the other assemblies as well. This would be the place to describe methods
of `List<ConcurrentDictionary<int, ImmutableArray<bool>>`. Through this grouping mechanism, a compiler will
be able to ignore the entries in the profile data format which do not relate to the compilation, and thus
avoid wasting excess time parsing portions of the file which are not relevant to the compilation.

In each `mibcGroupMethod` the data about an individual method/type entry is to be recorded. Similarly to the
`AssemblyDictionary` this is an encoding of il instructions designed to hold the relevant data. In this case
the data is encoded as follows.

```
ldtoken methodOrTypeInProfileData
Any series of instructions that does not involve the pop, ldstr "InstrumentationDesc", ldstr "InstrumentationData", or ldstr "InstrumentationEnd" instructions.
ldstr "FunctionTouchOrder"
ldc.i4 <index>
ldstr "InstrumentationDesc"
ldc.i4 <InstrumentationVersionNumber>
  A series of ldtoken and ldc.i8 instructions to represent the InstrumentationDesc. Only ldc.i8 instructions
  may be used to represent raw bytes, more efficient encodings such as ldc.i4 followed by conv.i8 are not supported
ldstr "InstrumentationData"
  A series of ldtoken and ldc.i8 instructions to represent the last InstrumentationData in the trace. Only ldc.i8
  instructions may be used to represent raw bytes, more efficient encodings such as ldc.i4 followed by conv.i8 are not supported
ldstr "InstrumentationEnd"
Any series of instructions that does not involve the pop, ldstr "InstrumentationDesc", ldstr "InstrumentationData", or ldstr "InstrumentationEnd" instructions.
pop
```

The `InstrumentationData` is optional, and may not be present in the file.

The `FunctionTouchOrder` is optional, and may not be present in the file.

## R2R File Format
Profile data shall be encoded into the R2R FileFormat in a new section named `READYTORUN_SECTION_PROFILEDATA`.
This section shall hold a version number, and a single `NativeHashtable` that contains a mapping from type/method
to the pair of Desc and Data. TODO define how Desc and Data are encoded. The intention is to store exactly the
same data as is stored in the PGO data file, except that the instrumentation data version must be the same for
all data chunks.

## Instrumenting Runtime
The runtime shall be responsible for choosing when to execute instrumentation, allocating the tracing buffers
and actually reporting the data through the tracing pipeline. Tracing shall leverage recent work by Kount in
which low tier code is given a small stub which executes before method execution. At the moment, this stub
increments a call counter, and then invokes either tier 0 code or the prestub. My intention is to tie into this
infrastructure and use it to drive instrumentation behavior.

One idea on how to do so would be to build 2 copies
of the code, 1 of which is the instrumented version, and the other is the normal code. On some configurable interval,
such as once every 1, 2, 4, 8 or 16 calls, the instrumented version would be called. On exponentially growing intervals,
such as on the 32nd, 64th, 128th, 256th, 512th, etc call, it would call a function to emit the trace data for
the instrumented method. (To support reporting instrumentation for both the uninstantiated and instantiated
forms of generics, a given method may be associated with multiple instrumentation data payloads.) This sampling
scheme should allow applications to execute with reasonable performance even with instrumentation enabled.
Due to the double JIT compilation, it is expected that application launch times will be slowed, but hopefully
not by too much. Configuration of instrumentation shall be done via environment variable. (In theory this could
be done via the tracing apis, but it is likely a significant security vulnerability, and thus I do not believe
its appropriate to send such data without positive confirmation that it should be sent.) This concept of a sampling
instrumenting JIT is intended to provide fairly excellent performance while running instrumented, and may be
a reasonable model for moving to a more dynamic system in the future. @AndyAyersMS has pointed out that this approach
has substantial difficulties for providing extremely precise data, but I am of the opinion it would be good enough
to show benefit, and as we develop the profile data further we may choose to adjust this policy, or have anther.

Another policy would be less startup focussed, and instead focus on attempting to gather improvements to steady state
performance. The runtime could use the sampling instrumented scheme as above, but instead of applying the scheme at
initial jit time, it could apply it based on a stack sampling profile of the process, and instrument only the methods found
via a sampling profiler. I expect this approach would be more valuable for performing dynamic recompilation and adaptive
optimization than for static AOT based profile data.

## Instrumenting JIT
The jit shall be responsible for the format and contents of instrumentation data. When compiled with instrumentation
enabled, it is expected to generate an instrumentation descriptor, and report it across the jitinterface, so that the
runtime may send the InstrumentationDesc event, and allocate the instrumentation region. As an initial implementation,
the data gathered is expected to be simple block count data, but as the data format allows for transmittal of `TypeHandle`
and `MethodDesc` data, it should be possible to add support for capturing information such as virtual function dispatch
type data as well, or other related information.

In addition the JIT is responsible for parsing the instrumentation data. It shall support an api that allows multiple
instrumentation data chunks to be passed to it, and should support merging/parsing the combination of all the data.

Finally, once parsed and merged, the JIT when producing an R2R image is responsible for producing a merged copy of pgo
data that may be embedded into the R2R file format for possible consumption by the runtime JIT.

## Trace processing tool
The trace processing tool is responsible for reading the trace files as produced by perfview/dotnet trace, and
producing .MIBC files. The process should be a straightforward format translation for instrumentation data. The
`FunctionTouchOrder` and existence of the method shall be based on the `JitStarted` and `R2EEntryPoint` events.

## AOT Compilation tool
AOT compilation shall use the profile guided data in several ways.
- Any function mentioned in a scenario shall be considered to be warm which shall influence code layout.
- The `FunctionTouchOrder` shall be used as an input into codelayout ordering in the final file.

Processing PGO `InstrumentationData` in the AOT compilation tool shall be a cooperative process between the managed
components of the compiler and the jit. The managed component shall provide to the jit the unpackaged Instrumentation
data for the method being compiled, and for both the uninstantiated method and instantiated method instrumentations
as are present. The jit is responsible for merging these multiple data sources.

In addition the JIT may optionally choose to generate a profile guided data block for association with the precompiled
code for use in re-jit scenarios, and information about related method code layout for the code, and optionally a
portion of the function body which is to be placed into a cold code section. The intention here it to allow some
algorithm such as Pettis-Hansen or a more modern variant (eg https://research.fb.com/wp-content/uploads/2017/01/cgo2017-hfsort-final1.pdf)
to be used to optimize code layout.

## Consuming Runtime/JIT
If present in an R2R file, when a method is rejitted, the runtime shall provide a means for the jit to see instrumentation
data from either previous compiles in process, and/or from the R2R file. This shall provide a means for the JIT to choose
whether or not the method should be recompiled, or possibly to inform it about optimization opportunities that are
too expensive to compute at jit time, but could be computed by the AOT compiler, or other such ideas.

As a means of doing this, options such as the following will be given to the jit to provide custom behavior.
1. Ignore the profile data and rejit.
2. Declare that the prejitted code is good enough as it is.
3. Use the profile data during rejit.

The intention is that these options will provide an opportunity to improve our tiered compilation story with profile data.
I expect that the set of possibilities here is fairly long. For instance, one other option might be for the tiered compilation
infrastructure to integrate with the jit such that methods are re-jitted in some sort of priority order, another possibility
would be to use this as a means for adaptive or speculative optimization.

## Diagnostic Tools
The tools r2rdump and dotnet-pgo shall provide a means for dumping their inputs. For most forms of data this is
fairly straightforward, but for `InstrumentationData`, there shall be a common dump tool written in managed code
that can provide a human readable dump of the data. r2rdump, dotnet-pgo, and possibly sos will all be able to share
this codebase for examination of the data structures in r2r files, traces, and runtime environments respectively.
