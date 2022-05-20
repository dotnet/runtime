# OSR Details and Debugging

This document describes some of the rationale behind OSR, things you
may need to think about when OSR is enabled, and some debugging
techniques that you can use when things go wrong.

It is primarily aimed at JIT developers but may prove useful to a
broader audience.

## Background

OSR (On Stack Replacement) describes the ability of the runtime to
transfer control from one native code version to another while there
are active stack frames for the method.

The primary goal of OSR is to allow the runtime to initially jit most
methods without optimization, and then transition control (via OSR) if
and when the invocations of those methods become compute
intensive. During a transition the active invocations are rewritten
"on stack" to have the proper frame shape and code references.

In this way OSR is similar to tiering, but tiering can only change
between native code versions when a method is called. So, for
instance, if a user puts all their code inside loops in `Main` then
tiering will never have an opportunity to update the code being run,
as `Main` is only ever called once.

OSR is also in many ways similar to EnC (Edit and Continue), though
there are key differences.  EnC transitions are initiated by user
edits via the debugger, and typically this means the IL version of the
method has changed. OSR transitions are mediated by the runtime, and
the transitioned-to methods represent different native code
compilation of the same IL code version.

In our particular implementation OSR is used to transition from a
Tier0 (or "quick") jitted (and hence unoptimized) native code
version to an OSR version that is optimized. OSR versions of the
native code for a method are unusual in many ways; we discuss this
further below.

For more detail on the OSR design, see the References section.

### Tiered Compilation

OSR is built on top of Tiered Compilation, which we briefly recap.

Tiered Compilation is a runtime and jit feature that allows methods to
be initially jitted quickly, without optimization, and then be
rejitted later on with optimization, if the methods are frequently
called. This allows applications to start up quickly as less time
is spent jitting up front, but still obtain full steady-state
performance over time.

There are different polices that apply to determine which methods
are handled via tiered compilation, and how they are optimized.
* The normal case is that a method is first quickly jitted (at Tier0)
and then rejitted (at Tier1) (on a background thread) once the
method has been called frequently.
* If a method has been prejitted then the prejitted code serves
the place of the Tier0 code.
* Some methods bypass tiering and are immediately optimized
the first time they are jitted:
  * Methods marked with the `AggressiveOptimization` attribute
  * Dynamic methods and methods from collectible assemblies
  * By default in .NET 3.0, 5.0, 6.0: methods with loops.
This policy is controlled by `DOTNET_TC_QuickJitForLoops`;
 see next section.

### Dynamic and Full PGO

Dynamic PGO was introduced in the .NET 6.0 release. It also relies
on tiered compilation.

In Dynamic PGO, any method jitted at Tier0 has additional
instrumentation code added to the Tier0 version to collect profile
data.  This data is made available to JIT when the method is
recompiled at Tier1.

Full PGO is an offshoot of Dynamic PGO where we force all methods
to pass through Tier0.

### Quick Jit For Loops (aka `QJFL`)

Tiered compilation was introduced in the .NET Core 3.0 release.

Initially, all methods that got jitted went through Tier0. But
during the development cycle we got a fair amount of feedback that
this had adverse performance impacts as performance-sensitive user
code could be trapped in the Tier0 version.

In response to this we changed the default behavior: methods with
loops would be initially optimized and not participate in tiered jitting.
We often use the shorthand `QJFL=0` to describe this behavior.

While `QJFL=0` addressed the immediate problems users had with tiered
compilation, it had some downsides:

* Not all methods with loops are performance sensitive, so in many
cases we saw increased startup JIT time without any steady state
benefit.  In some applications where startup JIT time is significant
the impact was on the order of 20%.

* In fact, early optimization of methods may lead to lower
steady-state performance, because (a) class initializers may not yet
have run, so early optimized methods must run class initializer checks
and also cannot examine readonly static fields; (b) Dynamic PGO can
only operate on methods that pass through Tier0.

One can change the default behavior by setting QJFL to 1; some
startup-sensitive applications (e.g., `Powershell`) do this.

### Rationale for Enabling OSR

OSR allows the runtime to jit most methods at Tier0, and to transition
from Tier0 code when and if needed, and so allows us to change the
default to `QJFL=1`.

Enabling OSR will lead to:
* better startup perf in general (upwards of 20% in some cases)
* improved steady state perf
* improved perf from Dynamic PGO (closing the gap between Dynamic
 PGO and Full PGO)

## How OSR Works

### Enabling OSR

OSR is enabled by setting
`DOTNET_TC_QuickJitForLoops=1` (aka `QJFL=1`)
`DOTNET_TC_OnStackReplacement=1` (aka `OSR=1`)

and is influenced by
`DOTNET_TieredPGO=1` (`BBINSTR=1` at Tier0)

### Choosing which methods are "OSR Eligible"

The runtime makes a JIT request at Tier0, and passes the current
values of QJFL, OSR, and BBINSTR.

The JIT does an initial IL scan for the method; during this scan it
checks for several things:
* if there is any lexically backwards IL branch (`compHasBackwardsBranch`)
* if there is any lexically backwards branch in catch, filter, finally,
or fault region (aka "loop in handler").
* if there is any `localloc`
* if the method is a reverse PInvoke
* if there is any `tail.` prefixed call.

If `QJFL=0 && compHasBackwardsBranch`, the JIT changes the
optimization level to full opt and notifies the runtime that the
method is no longer going to participate in tiered compilation.
This is the default behavior in older releases.

If `QJFL=1 && OSR==0 && !compHasBackwardsBranch`, the JIT compiles the
method at Tier0.

If `QJFL=1 && OSR==1 && compHasBackwardsBranch`, the JIT checks to see
if the method has a loop in a handler, localloc, or is a reverse
PInvoke.  If not, the method is "OSR Eligible". If so, the JIT will
switch the method to optimized.

If there is a `tail.` prefixed call and BBINSTR=0, the JIT will
override all the above and switch the method to optimized.

### Jitting of OSR Eligible Methods at Tier0

During importation, the jit looks for blocks that are the targets of
the lexical back edges. It marks these as needing patchpoints. Note these
points are required to be stack empty points in valid IL.

During the patchpoint phase, if any block was marked, the jit adds a
new integer local to the method (the patchpoint counter) and adds IR
to initialize the counter on method entry to the value of
`DOTNET_TC_OnStackReplacement_InitialCounter` (by default this is
0x1000).

At each marked block the JIT adds code to decrement the counter and
conditionally invoke `CORINFO_HELP_PATCHPOINT` if the counter value is zero
or negative. The helper arguments are the IL offset of the block and
the address of the counter.

The remainder of compilation proceeds normally, but when the method is
finished, the JIT creates a patchpoint descriptor to note the method's
frame size, virtual offset of all IL locals, and the offset of some
other key frame information (generics context, etc.). This descriptor
is passed back to the runtime, where it is stored alongside the
method's debug info.

We rely on the fact that Tier0 methods are not optimized and do not
keep any IL state in registers at stack empty points, and there is a
one to one mapping between IL state and stack slots. Thus a single
patchpoint descriptor suffices for all the patchpoints in the method
and no liveness analysis is required to determine what it should
contain.

A Tier0 method may contain many patchpoints. In our current
implementation the runtime may create one OSR method per patchpoint.
Each OSR method may comprise most or all of the original method. So in the worst case we can have a lot of OSR codegen, but it is currently thought such cases
will be rare. We may need to implement some policy to avoid this.

### Execution of Tier0 Code with Patchpoints

When a Tier0 method with Patchpoints is called, it counts down the
per-call patchpoint counter each time a patchpoint is reached.

If the counter reaches zero the code invokes the
`CORINFO_HELP_PATCHPOINT` runtime helper. The helper then uses the
return address of the helper call as a key into a patchpoint info
table.

The first time the patchpoint is seen by the runtime it adds an entry
to the table. The local counter in the method is reloaded with a value
determined by `DOTNET_OSR_CounterBump` (currently 0x1000).  If a
patchpoint at a given offset is hit more than `DOTNET_OSR_HitLimit` times
(currently 0x10) then the runtime will create an OSR method for that
offset.

Note this means that a single call can trigger OSR creation after
(0x1000 x 0x11) ~ 69,000 executions at that offset. This number was
chosen to try and balance the perf loss from continued execution of
the Tier0 code versus the one time cost to create the optimized OSR
version and the expected perf win from subsequent execution in the OSR
code.  It may need some adjusting.

The OSR method is created synchronously on the thread that hits the
hit limit threshold. If a second thread arrives while the OSR method
is being created it will simply have its counter reloaded and continue
executing Tier0 code for the time being. Eventually it may come back.

Once the method is ready the initiating thread is transitioned to the
OSR method; any subsequently arriving executions from other active
frames will likewise transition as they hit the patchpoint.

The OSR method does not return control to the Tier0 method (it is a
"full continuation" of the Tier0 method at the patchpoint).

### Creation of the OSR Method

The OSR method is specific to a Tier0 method at a specific IL
offset. When the runtime decides to create an OSR method it invokes
the jit and passes a special OSR flag and the IL offset.

This compilation is similar to a normal optimized compilation, with
a few twists:
* Importation starts at the specified IL offset and pulls in all the
code reachable from that point. Typically (but not always) the method entry (IL offset 0) is unreachable
and so the OSR method only imports a subset of the full method.
* Control normally branches from the first block (`fgFirstBB`) to the block at the OSR IL offset (the "OSR entry").
Special care is needed when the OSR entry is in the middle of a try or in a nested try.
* If dynamic PGO is enabled the OSR method will read the PGO data captured by the Tier0
method, but we also instrument the OSR method to ensure any Tier1 version we produce
later on sees all the PGO data it would have seen if we'd forcibly kept the method at Tier0.

The JIT may need to import the entry block if it can be reached by tail
recursion to loop optimizations, even if there no explicit IL branch
to offset 0 in the IL reachable from the OSR IL offset. This was the
source of a number of subtle bugs -- the call site that eventually may
loop back to the method entry could come from an inlinee, or via
devirtualization, etc.

For the most part the jit treats an OSR method just like any other
optimized jitted method. But aside from importation, frame layout,
PGO, and prolog/epilog codegen details bleed through in only a handful
of places.

One of these is that an OSR method will never need to zero init any locals or see undefined values for locals; all this is dealt with by the Tier0 frame. (The OSR method may still need to zero temps it has allocated).

#### Frame Layout

For an OSR method, the OSR frame logically "incorporates" the Tier0 method frame. Because the OSR method has different register saves and different temporaries, there is an OSR specific portion of the frame that extends beyond the Tier0 frame.

On x64, Tier0 frames are always RBP frames. On Arm64, Tier0 frames can be any of the 5 frame types the jit can create.

On x64 we currently have the frame pointer (if needed) refer to the base of the OSR extension. On arm64 the frame pointer refers to the base of the Tier0 frame. This divergence is historical and we should likely fix x64 to follow the same plan as arm64.

Locals corresponding to Tier0 args or locals (aka `lvaIsOSRLocal`) that have stack homes in OSR methods will use the Tier0 slot for that home in the OSR method. So it is not uncommon for the OSR method to be accessing parts of the Tier0 frame throughout its execution. Those slots are reported to GC as necessary.

But often much of the Tier0 frame is effectively dead after the transition and execution of the OSR method's prolog, as the Tier0 live state is enregistered by the OSR method.

#### OSR Prolog

The OSR prolog is conceptually similar to a normal method prolog, with a few key difference.

When an OSR method is entered, all callee-save registers have the values they had when the Tier0 method was called, but the values in argument registers are unknown (and almost certainly not the args passed to the Tier0 method). The OSR method must initialize any live-in enregistered args or locals from the corresponding slots on the Tier0 frame. This happens in `genEnregisterOSRArgsAndLocals`.

If the OSR method needs to report a generics context it uses the Tier0 frame slot; we ensure this is possible by forcing a Tier0 method with patchpoints to always report its generics context.

An OSR method does not need to register function entry callbacks as it is never called; similarly it does not need to acquire the synchronous method monitor, as the Tier0 frame will have already done that.

#### OSR Epilog

The OSR epilog does the following:
* undoes the OSR contribution to the frame (SP add)
* restores the callee-saved registers saved by the OSR prolog
* undoes the Tier0 frame (SP add)
* restores any Tier0 callees saves needed (x64 only, restores RBP)
* returns to the Tier0/OSR method's caller

This epilog has a non-standard format because of the two SP adjustments.
This is currently breaking x64 epilog unwind.

(NOTE: this was fixed as described [here](https://github.com/dotnet/runtime/blob/main/docs/design/features/OSRX64EpilogRedesign.md))

On Arm64 we have epilog unwind codes and the second SP adjust does not appear to cause problems.

#### Funclets in OSR Methods

OSR funclets are more or less normal funclets.

On Arm64, to satisfy PSPSym reporting constraints, the funclet frame must be padded to include the Tier0 frame size. This is conceptually similar to the way the funclet frames also pad for homed varargs arguments -- in both cases the padded space is never used, it is just there to ensure the PSPSym ends up at the same caller-SP relative offset for the main function and any funclet.

#### OSR Unwind Info

On x64 the prolog unwind includes a phantom SP adjustment at offset 0 for the Tier0 frame.

As noted above the two SP adjusts in the x64 epilog are currently causing problems if we try and unwind in the epilog. Unwinding in the prolog and method body seems to work correctly; the unwind codes properly describe what needs to be done.

On arm64 there are unwind codes for epilogs and this problem does not seem to arise.

When an OSR method is active, stack frames will show just that method and not the Tier0 method.

#### OSR GC Info

OSR GC info is standard. The only unusual aspect is that some special offsets (generics context, etc.) may refer to slots in the Tier0 frame.

### Execution of an OSR Method

OSR methods are never called directly; they can only be invoked by `CORINFO_HELP_PATCHPOINT` when called from a Tier0 method with patchpoints.

On x64, to preserve proper stack alignment, the runtime helper will "push" a phantom return address on the stack (x64 methods assume SP is aligned 8 mod 16 on entry). This is not necessary on arm64 as calls do not push to the stack.

When the OSR method returns, it cleans up both its own stack and the
Tier0 method stack.

Note if a Tier0 method is recursive and has loops there can be some interesting dynamics. After a sufficient amount of looping an OSR method will be created, and the currently active Tier0 instance will transition to the OSR method. When the OSR method makes a recursive call, it will invoke the Tier0 method, which will then fairly quickly transition to the OSR version just created.

## Debugging OSR

### Seeing which OSR methods are created

* `DOTNET_DumpJittedMethods=1` will specially mark OSR methods with the inspiring IL offsets.

For example, running a libraries test with some stressful OSR settings, there ended up being 699 OSR methods jitted out of 160675 total methods. Grepping for OSR in the dump output, the last few lines were:

```
Compiling 32408 System.Text.Json.Serialization.Converters.ObjectDefaultConverter`1[WrapperForPoint_3D][System.Text.Json.Serialization.Tests.WrapperForPoint_3D]::OnTryRead, IL size = 850, hash=0x5a693818 Tier1-OSR @0x5f
Compiling 32411 System.Text.Json.Serialization.Tests.ConverterForPoint3D::Read, IL size = 40, hash=0x294c33b5 Tier1-OSR @0xf
Compiling 32412 System.Text.Json.Serialization.Converters.ObjectDefaultConverter`1[WrapperForPoint_3D][System.Text.Json.Serialization.Tests.WrapperForPoint_3D]::OnTryWrite, IL size = 757, hash=0x1ed8b727 Tier1-OSR @0x60
Compiling 32629 System.Text.Json.Serialization.Converters.ObjectWithParameterizedConstructorConverter`1[KeyValuePair`2][System.Collections.Generic.KeyValuePair`2[System.__Canon,System.__Canon]]::ReadConstructorArgumentsWithContinuation, IL size = 192, hash=0x7ab2e686 Tier1-OSR @0x0
Compiling 32655 System.Text.Json.Serialization.Converters.ObjectWithParameterizedConstructorConverter`1[Point_3D_Struct][System.Text.Json.Serialization.Tests.Point_3D_Struct]::ReadConstructorArgumentsWithContinuation, IL size = 192, hash=0xb37dcd36 Tier1-OSR @0x0
```
Here the annotations like `@0x5f` tell you the IL offset for the particular OSR method.

As an aside, seeing patchpoints at offset `@0x0` is a pretty clear indication that the example was run with random patchpoints enabled, as IL offset 0 is rarely the start of a loop.

Also note that an OSR method will always be invoked immediately after it is jitted. So each of the OSR methods above was executed at least once.

### Controlling which methods are OSR Eligible

* `DOTNET_TC_QuickJitForLoops=0` -- no methods are OSR eligible, all get switched to optimized
* `DOTNET_TC_OnStackReplacement=0` -- no methods are OSR eligible, all get jitted at Tier0
* `DOTNET_JitEnableOsrRange=...` -- use method hash to refine which methods are OSR eligible
* `DOTNET_JitEnablePatchpointRange=...` -- use method hash to refine which Tier0 methods get patchpoints

Binary searching using the `Enable` controls has proven to be a reliable
way to track down issues in OSR codegen.

On the same example as above, now run with `DOTNET_JitEnableOsrRange=00000000-7FFFFFFF` there were 249 OSR methods created. Methods with hashes outside this range that would have been handled by OSR were instead immediately optimized and bypassed tiering. So you can systematically reduce the set of OSR methods created to try and find the method or methods that are causing a test failure.

If you are not familiar with range syntax, ranges values are in hex, and entries can be intervals (as above), singletons, or unions of intervals and singletons, e.g.,
```
DOTNET_JitEnableOsrRange=00000000-3FFFFFFF,067a3f68,F0000000-FFFFFFFF
```
I find I rarely need to use anything other than a single range.

### Changing rate at which OSR methods get created

These config values can alter the runtime policy and cause OSR methods to be created more eagerly or less eagerly:

* `DOTNET_OSR_HitCount=N` -- alter the number of times the runtime sees helper call at a given offset until
it creates an OSR method. A value of 0 or 1 eagerly creates OSR methods.
* `DOTNET_OSR_CounterBump=N` -- alter the local counter reload value done by the runtime. A low value means
Tier0 methods with patchpoints will call back into the runtime more often.
* `DOTNET_TC_OnStackReplacement_InitialCounter=N` -- alter the initial counter value the jit bakes into the Tier0 method.
A low value means the initial call into the runtime will happen more quickly.

Note setting all 3 of these to 0 or 1 will cause OSR methods to be
created the first time a patchpoint is hit.

If a method has multiple (say two) patchpoints, it may require some
fiddling with these settings to ensure that both OSR versions get
created in a given run.

### Tracing Runtime Policy

Setting

* `DOTNET_LogEnable=1`
* `DOTNET_LogFacility=0x00400000`
* `DOTNET_LogLevel=5`
* and say `DOTNET_LogToConsole=1`

will log runtime behavior from calls to `CORINFO_HELP_PATCHPOINT` from Tier0 methods.

For example:
```
TID 4bdc2: Jit_Patchpoint: patchpoint [17] (0x0000FFFF1E5F3BB8) hit 1 in Method=0x0000FFFF1EAD9130M (Xunit.JsonDeserializer::DeserializeInternal) [il offset 45] (limit 2)
TID 4bdc2: Jit_Patchpoint: patchpoint [17] (0x0000FFFF1E5F3BB8) TRIGGER at count 2
TID 4bdc2: JitPatchpointWorker: creating OSR version of Method=0x0000FFFF1EAD9130M (Xunit.JsonDeserializer::DeserializeInternal) at offset 45
TID 4bdc2: Jit_Patchpoint: patchpoint [17] (0x0000FFFF1E5F3BB8) TRANSITION to ip 0x0000FFFF1E5F6820
TID 4bdc2: Jit_Patchpoint: patchpoint [18] (0x0000FFFF1E5F6BE0) hit 1 in Method=0x0000FFFF1EB03B58M (Xunit.JsonBoolean::.ctor) [il offset 0] (limit 2)
TID 4bdc2: Jit_Patchpoint: patchpoint [18] (0x0000FFFF1E5F6BE0) TRIGGER at count 2
TID 4bdc2: JitPatchpointWorker: creating OSR version of Method=0x0000FFFF1EB03B58M (Xunit.JsonBoolean::.ctor) at offset 0
TID 4bdc2: Jit_Patchpoint: patchpoint [18] (0x0000FFFF1E5F6BE0) TRANSITION to ip 0x0000FFFF1E5F6D00
```
Here the number in brackets `[17]` is the number of distinct patchpoints that have called the runtime helper; from the runtime side this serves as a sort of patchpoint ID.

A `hit` is just a call from an Tier0 method to the helper. A `TRIGGER` is a hit that now has reached the `DOTNET_OSR_HitCount` limit, and at this point an OSR method is created. A `TRANSITION` is the transition of control from the Tier0 method to the OSR method.

You can use the following config settings to further alter runtime policy:
* `DOTNET_OSR_LowId`
* `DOTNET_OSR_HighId`
These collectively form an inclusive range describing which patchpoint IDs will
be allowed to `TRIGGER` (and therefore `TRANSITION`).

So you can also use this to control which OSR methods are created by the runtime, without altering JIT behavior.

### Changing where patchpoints are placed in Tier0 code

* `DOTNET_JitOffsetOnStackReplacement=offset` -- only place patchpoints at given IL offset (and if stack empty)
* `DOTNET_JitRandomOnStackReplacement=val` -- in addition to normal OSR patchpoints, place patchpoints randomly
 at stack empty points at the starts of non-handler blocks (value is likelihood (percentage), in hex, so 0 will not add any extra, 0x64 (== 100 decimal), will
add as many extra as possible)

The latter is used by OSR stress (in conjunction with low values for
the policy config) to create large numbers of OSR methods.

### Controlling what gets dumped

Enabling jit dumps via name or hash can lead to multiple jit
compilations being dumped: one for Tier0, one for each OSR method, and
one for Tier1).  You can control this via:

* `DOTNET_JitDumpTier0=0` -- suppress dumping all Tier0 jit requests
* `DOTNET_JitDumpAtOSROffset=N` -- only dump OSR jit requests at indicated IL offset

### Observing OSR from ETW

OSR methods are marked specially in the ETW MethodJitting events. This
value is not yet parsed in tracevent/perfview/SOS.

In SOS, OSR version of methods are currently marked as "unknown tier".

### OSR when Debugging

When single-stepping at source level through Tier0 code with
patchpoints, the transition to OSR will happen behind the scenes.

Because OSR code is optimized and Tier0 code is not, you may see a
sudden degradation in what the debugger displays if you step at the
exact point the OSR transition happens. Breakpoints applied at Tier0
will be reapplied to the OSR method, though they may not "take" if the
source to IL to native mapping information is missing.

If you step at assembly level you can see the invocation of the
patchpoint helper, etc.

OSR methods show up normally in the stack, unless execution is paused
in the OSR epilog (working on this). You will only see one entry for
a Tier0 method that's transitioned to an OSR method.

## OSR and Performance

As noted above, the combination of `QJFL=1` and `OSR=1` will typically provide better startup and comparable of better steady-state. But it's also possible
to spend considerable time in OSR methods (e.g., the all-in-`Main` benchmark).

Generally speaking the performance of an OSR method should be comparable to the equivalent Tier1 method. In practice we see variations of +/- 20% or so. There are a number or reasons for this:
* OSR methods are often a subset of the full Tier1 method, and in many cases just comprise one loop. The JIT can often generate much better code for a single loop in isolation than a single loop in a more complex method.
* A few optimizations are disabled in OSR methods, notably struct promotion.
* OSR methods may only see fractional PGO data (as parts of the Tier0 method may not have executed yet). The JIT doesn't cope very well yet with this sort of partial PGO coverage.

### Impact on BenchmarkDotNet Results

BenchmarkDotNet (BDN) typically does a pretty good job of ensuring that measurements are made on the most optimized version of a method. Usually this is the Tier1 version. As such, we don't expect to see OSR have much impact on BDN derived results.

However, one can configure BenchmarkDotNet in ways that make it more likely that it will measure OSR methods, or some combination of OSR and Tier1 -- in particular reducing the number of warmup iterations. By default a method must be called 30 times to be rejitted at Tier1. So if BDN benchmark is run with less than 30 iterations, there's a chance it may not be measuring Tier1 code for all methods. And when OSR is enabled, this will often mean measuring OSR method perf.

This happens more often with benchmarks that either (a) do a lot of internal looping to boost the elapsed time of a measurement interval, rather than relying on BDN to determine the appropriate iteration strategy, or (b) are very long running, so that BDN determines that it does not need to run many iterations to obtain measurements of "significant" duration (250 ms or so).

In the performance repo configurations we reduce the number of warmup iterations and have some benchmarks that fall into both categories above. We'll likely need to make some adjustment to these benchmarks to ensure that we're measuring Tier1 code performance.

## References

* [OSR Design Document](https://github.com/dotnet/runtime/blob/main/docs/design/features/OnStackReplacement.md). May be a bit dated in places.
* [OSR Next Steps Issue](https://github.com/dotnet/runtime/issues/33658). Has a lot of information on issues encountered during bring-up, current limitations, and ideas for things we might revisit.