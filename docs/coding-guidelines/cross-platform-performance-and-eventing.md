# .NET Cross-Plat Performance and Eventing Design

## Introduction

As we bring up CoreCLR on the Linux and OS X platforms, it’s important that we determine how we’ll measure and analyze performance on these platforms.  On Windows we use an event based model that depends on ETW, and we have a good amount of tooling that builds on this approach.  Ideally, we can extend this model to Linux and OS X and re-use much of the Windows tooling.

# Requirements

Ideally, we'd like to have the following functionality on each OS that we bring-up:

- Collection of machine-wide performance data including CPU sampling, threading information (e.g. context switches), and OS specific events / system call tracing.
- Collection of CLR-specific events that we have today exposed as ETW events.
- Collection of EventSource events by a default OS-specific collector, as we do today with ETW on Windows.
- User-mode call stacks for both performance and tracing data.
- Portability of traces across machines, so that analysis can occur off box.
- Data viewable on collection OS.
- Stretch: Data can be understood by TraceEvent, which opens up interesting analysis scenarios.
	- Using PerfView and existing tooling.
	- Ability to use CAP (Automated analysis) on non-Windows data.

# Scoping to the Current Landscape

Given that we’ve built up a rich set of functionality on Windows, much of which depends on ETW and is specific to the OS, we’re going to see some differences across the other operating systems.

Our goal should be to do the best job that we can to enable data collection and analysis across the supported operating systems by betting on the right technologies, such that as the landscape across these operating systems evolve, .NET is well positioned to take advantage of the changes without needing to change the fundamental technology choices that we’ve made.  While this choice will likely result in some types of investigations being more difficult due to absent features that we depend upon on Windows, it is likely to position us better for the future and align us with the OS communities.

# Linux

## Proposed Design

Given that the performance and tracing tool space on Linux is quite fragmented, there is not one tool that meets all of our requirements.  As such, we'll use two tools when necessary to gather both performance data and tracing data.

**For performance data collection we'll use perf_events**, an in-tree performance tool that provides access to hardware counters, software counters and system call tracing.  Perf_event will be the primary provider of system-wide performance data such as CPU sampling and context switches.

**For tracing we'll use LTTng**.  LTTng supports usermode tracing with no kernelspace requirements.  It allows for strongly typed static events with PID and TID information.  The system is very configurable and allows for enabling and disabling of individual events.

## Tools Considered

### Perf_Events

#### Pros

- Kernel level tracing of hardware counters (CPU samples, context switches, etc.), software counters and system calls.
- Machine-wide or process-wide.  No process attach required.
- Collection of owned processes without elevated permissions.
- Provides user-mode stack traces via frame pointers and libunwind.
- Extensible support for [JIT symbol resolution](https://git.kernel.org/cgit/linux/kernel/git/namhyung/linux-perf.git/tree/tools/perf/Documentation/jit-interface.txt).
- In-tree: Basically available for almost every distro.
- Data is stored in perf tool file format (perf.data) – can be opened by a viewer such as “perf report”.

#### Cons

- No user-mode static tracing.  Only dynamic user-mode tracing, using “breakpoints” with no event payloads.

### LTTng

#### Pros

- User-mode static tracing with no kernel modules required.
- Strongly-typed static event support.
- No pre-registration of static event types required.  Events can be enabled before they are known to LTTng.
- System call tracing supported with optional kernel module.  User-mode does not require a kernel module.
- Machine-wide or process-wide.  No process attach required.
- Collection of owned processes without elevated permissions.
- Events can be tagged with context such as PID, TID.
- Out-of-tree but binaries available for many common distros.
- Data stored in Common Trace Format – designed for interop.

#### Cons

- No built-in callstack collection.

### SystemTap

#### Pros

- Supports User-mode static tracing including call stacks.
- Static tracing does not require pre-registration of the event or payload definition when the app starts, which makes EventSource support simple.
- Out-of-tree but binaries available for many common distros.

#### Cons

- Complex kernel module is generated and compiled on-the-fly based on the tracing script.
- Static tracing includes a fixed set of static tracing APIs with limited overloads (e.g. int, string).  Can’t consider it strongly typed tracing.
- User-mode stack trace support requires debug information to support unwinding.  No story for JIT compiled code.
- User-mode stack traces are only supported on x86 and x64.
- Data is stored as unstructured text.

### DTrace4Linux

#### Pros

- Would allow for tracing code and collection script re-use across Linux and OS X.

#### Cons

- Source only – no binary redist.
- Small subset of actual DTrace functionality.
- One person’s work rather than many contributions from the community.

### FTrace

#### Pros

- High performance function tracer.
- In-tree: Basically available for almost every distro.

#### Cons

- Tracing in kernel-mode only.
- No performance data capture.

### Extended Berkeley Packet Filter (eBPF)

#### Pros

- Should support user-mode static tracing.
- Possible integration with perf_event.

#### Cons

- Not currently available – currently being integrated into the kernel.
- Final featureset not clear yet.

## Infrastructure Bring-Up Action Items

- Investigate: Determine if clock skew across the trace files will be an issue.
- Investigate: Are traces portable, or do they have to be opened on collection machine?
- Investigate: Do we need rundown or can we use /tmp/perf-$pid.map?  How does process/module rundown work?
- Implement: Enable frame pointers on JIT compiled code and helpers to allow stacks to be walked.  (PR # [468](https://github.com/dotnet/coreclr/pull/468))
- Implement: Stack walking using existing stackwalker (libunwind and managed code).
- Implement: JIT/NGEN call frame resolution - /tmp/perf-$pid.map
- Implement: Trace collection tool.
	- Responsible for handling all of the complexity.
	- Start and stop tracing based on information requested.
	- **OPEN ISSUE:** Handle rundown if required.
	- Collect any information needed for off-box viewing (e.g. /tmp/perf-$pid.map).
	- Compress into one archive that can be copied around easily.
- Implement: Viewing of data in PerfView

# OS X

## Proposed Design

On OS X, the performance tooling space is much less fragmented than Linux.  However, this also means that there are many fewer options.

**For performance data collection and tracing, we’ll use Instruments.**  Instruments is the Apple-built and supported performance tool for OS X.  It has a wide range of collection abilities including CPU sampling, context switching, system call tracing, power consumption, memory leaks, etc.  It also has support for custom static and dynamic tracing using DTrace as a back-end, which we can take advantage of to provide a logging mechanism for CLR events and EventSource.

Unfortunately, there are some features that Instruments/DTrace do not provide, such as resolution of JIT compiled call frames.  Given the existing tooling choices, and the profiler preferences of the OS X community of developers, it likely makes the most sense to use Instruments as our collection and analysis platform, even though it does not support the full set of features that we would like.  It’s also true that the number of OS X specific performance issues is likely to be much smaller than the set of all performance issues, which means that in many cases, Windows or Linux can be used, which will provide a more complete story for investigating performance issues.

## Tools Considered

### Instruments

#### Pros

- Available for all recent versions of OS X.
- Provided free by Apple as part of XCode.
- Wide range of performance collection options, both using a GUI and on the command line.
- Can be configured to have relatively low overhead at collection time (unfortunately not the default).
- Supports static and dynamic tracing via DTrace probes.
- Supports machine wide and process specific collection.
- Supports kernel and user-mode call stack collection.

#### Cons

- No support for JIT compiled frame resolution.
- Closed source - no opportunities for contribution of "missing" features.
- Closed file format - likely difficult to open a trace in PerfView.

### DTrace

#### Pros

- In-box as part of the OS.
- Supports static tracing using header files generated by dtrace.
- Supports dynamic tracing and limited argument capture.
- Supports kernel and user-mode call stack collection.

#### Cons

- No support for JIT compiled frame resolution - Third party call stack frame resolution feature (jstack) does not work on OS X.
- Minimal to no investment - DTrace only kept functional for Instruments scenarios.
- No opportunities for contribution of "missing" features.

## Infrastructure Bring-Up Action Items

- Implement: Enable frame pointers on JIT compiled code and helpers to allow stacks to be walked.  (PR # [468](https://github.com/dotnet/coreclr/pull/468))
- Implement: Trace collection tool
	- NOTE: Use deferred mode to minimize overhead.
	- Investigate: Using iprofiler to collect data instead of the instruments UI.

# CLR Events

On Windows, the CLR has a number of ETW events that are used for diagnostic and performance purposes.  These events need to be enabled on Linux and OS X so that we can collect and use them for performance investigations.

## Platform Agnostic Action Items

- Implement: Abstract ETW calls to an inline-able platform abstraction layer.
	- **OPEN ISSUE:** Can / should we re-use PAL?
- Implement: Stack walker event implementation for x-plat – this is likely the same code for both Linux and OS X.

## Linux Action Items

- Implement: Build mechanics to translate ETW manifest into LTTng tracepoint definitions.
- Implement: Generate calls to tracepoints in the PAL (see above).

## OS X Action Items

- Implement: Build mechanics to translate ETW manifest into DTrace probe definitions.
- Implement: Generate calls to probes in PAL (see above).

# EventSource Proposal

Ideally, EventSource operates on Linux and OS X just like it does on Windows.  Namely, there is no special registration of any kind that must occur.  When an EventSource is initialized, it does everything necessary to register itself with the appropriate logging system (ETW, LTTng, DTrace), such that its events are stored by the logging system when configured to do so.

EventSource should emit events to the appropriate logging system on each operating system.  Ideally, we can support the following functionality on all operating systems:

- No pre-registration of events or payload definitions.
- Enable/disable individual events or sets of events.
- Strongly typed payload fields.

**Supporting all of these requirements will mean a significant investment.**  Today, LTTng and DTrace support all of these requirements, but do so for tracepoints that are defined statically at compile time.  This is done by providing tooling that takes a tool specific manifest and generates C code that can then be compiled into the application.

As an example of the kind of work we’ll need to do: LTTng generates helpers that are then called as C module constructors and destructors to register and unregister tracepoint provider definitions.  If we want to provide the same level of functionality for EventSource events, we’ll need to understand the generated code and then write our own helpers and register/unregister calls.

While doing this work puts us in an ideal place from a performance and logging verbosity point-of-view, we should make sure that the work done is getting us the proper amount of benefit (e.g. is pay-for-play).  As such, **we should start with a much simpler design, and move forward with this more complex solution once we’ve proven that the benefit is clear**.

## Step # 1: Static Event(s) with JSON Payload

As a simple stop-gap solution to get EventSource support on Linux and OS X, we can implement a single EventSource event (or one event per verbosity) that is used to emit all EventSource events regardless of the EventSource that emits them.  The payload will be a JSON string that represents the arguments of the event.

## Step # 2: Static Event Generation with Strongly-Typed Payloads

Once we have basic EventSource functionality working, we can continue the investigation into how we’d register/unregister and use strongly typed static tracepoints using LTTng and DTrace, and how we’d call them when an EventSource fires the corresponding event.

## Compatibility Concerns

In general, we should be transparent about this plan, and not require any compatibility between the two steps other than to ensure that our tools continue to work as we transition.

## Step # 1 Bring-Up Action Items

- Implement: A static EventSource tracepoint / probe as a CLR event.
- Implement: JSON serialization of the event payload.
- Implement: EventListener implementation for each platform that calls out to the tracepoint / probe.

# Proposed Priorities

Given the significant work required to bring all of this infrastructure up, this is likely to be a long-term investment.  As such, it makes sense to aim at the most impactful items first, and continually evaluate where we are along the road.

## Scenarios

We’ll use the following scenarios when defining priorities:

- P1: Performance analysis in support of bring-up of the .NET runtime and framework on Linux and OS X.
- P2: Performance analysis of ASP.NET running on .NET Core on Linux and OS X.

To support these scenarios, we need the following capabilities:

- P1: Collection and analysis of CPU, threading, syscalls, native memory.  Support for JIT compiled call frame resolution.
- P2: Collection and analysis of managed memory, managed thread pool, async, causality, JIT events.

We expect that the following assumptions will hold for the majority of developers and applications:

- Development occurs on Windows or OS X.
- Application deployment occurs on Windows or Linux.

## Work Items

### Priority 1

- Enable basic performance data collection on Linux with perf_events:
	- Implement a collection script that makes collection easy for anyone.
	- Enable JIT compiled code resolution for call stacks in perf_event.

### Priority 2

- Enable more advanced performance data collection for runtime components on Linux:
	- CLR in-box event support – Emits diagnostic / performance events for GC, JIT, ThreadPool, etc.
	- Linux EventSource support – Support for FrameworkEventSource, Tasks, Async Causality, and custom EventSource implementations.
	- Data collection on Linux via LTTng.

### Future:

- Enable Linux traces to be analyzed using PerfView / TraceEvent on Windows.
- Evaluate options for viewing Linux traces on OS X.
- Enable more advanced performance data collection for runtime components on OS X via CLR in-box events and EventSource.
