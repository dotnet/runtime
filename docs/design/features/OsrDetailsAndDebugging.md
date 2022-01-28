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
when the invocations of those methods become compute
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
method has changed. OSR transition are mediated by the runtime, and
the transitioned-to methods represent different native code
compilation of the same IL code version.

In our particular implementation OSR is used to transition from a
Tier0 (or "quick") jitted (and hence unoptimized) native code
version to an OSR version that is optimized. OSR versions of the
native code for a method are unusual in many ways; we discuss this
further below.

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
This policy is controlled by `DOTNET_COMPlus_TC_QuickJitForLoops`;
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
the impact was on the order or 20%.

* In fact, early optimization of methods may lead to lower
steady-state performance, because (a) class initializers may not yet
have run, so early optimized methods must run class initializer checks
and also cannot examine readonly static fields; (b) Dynamic PGO can
only operate on methods that pass through Tier0.

One can change the default behavior by setting QJFL to 1; some
startup-sensitive applications (eg `Powershell`) do this.

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
`DOTNET_TieredPGO=1` (`BBINSTR=1` at TIER0)

### Choosing which methods are "OSR Eligible"

The runtime makes a JIT request at TIER0, and passes the current
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
other key frame information (generics context, etc). This descriptor
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
Each OSR method may comprise most or all of the original method. So worst case we can have a lot of OSR codegen, but it is currently thought such cases
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
patchpoint at a given offset is hit more than `DOTNET_HitLimit` times
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
executing Tier0 code for the time being. Eventually it may come back

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
* Control normally branches from fgFirstBB to the BB at the OSR IL offset (the "osr entry").
Special care is needed when the osr entry is in the middle of a try or in a nested try.
* If dynamic PGO is enabled the OSR method will read the PGO data captured by the Tier0
method, but we also instrument the OSR method to ensure any Tier1 version we produce
later on sees all the PGO data it would have seen if we'd forcibly kept the method at Tier0.

The JIT may need to import the entry if it can be reached by tail
recursion to loop optimizations, even if there no explicit IL branch
to offset 0 in the IL reachable from the OSR Il offset. This was the
source of a number of subtle bugs -- the call site that eventually may
loop back to the method entry could come from an inlinee, or via
devirtualization, etc.

For the most part the jit treats an OSR method just like any other
optimized jitted method. But aside from importation, frame layout,
pgo, and prolog/epilog codegen details bleed through in only a handful
of places.

#### Frame Layout

(forthcoming)

#### OSR Prolog

(forthcoming)

#### OSR Epilog

(forthcoming)

#### Funclets in OSR Methods

(forthcoming)

### Execution of an OSR Method

OSR methods are never called directly; they can only be invoked by `CORINFO_HELP_PATCHPOINT` when called from a Tier0 method with patchpoints.

When the OSR method returns, it cleans up both its own stack and the
Tier0 method stack.

Note if a Tier0 method is recursive and has loops there can be some interesting dynamics. After a sufficient amount of looping an OSR method will be created, and the currently active Tier0 instance will transition to the OSR method. When th OSR method makes a recursive call, it will invoke the Tier0 method, which will then fairly quickly transition to the OSR version just created.

## Debugging OSR

### Seeing which OSR methods are created

* `DOTNET_DumpJittedMethods=1` will specially mark OSR methods with the inspiring IL offsets.

### Tracing Runtime Policy

* `DOTNET_LogEnable=1`
* `DOTNET_LogFacility=0x00400000`
* `DOTNET_LogLevel=5`
* and say `DOTNET_LogToConsole=1`

will log runtime behavior from calls to `CORINFO_HELP_PATCHPOINT` from Tier0 methods.

### Controlling which methods are OSR Eligible

* `DOTNET_TC_QuickJitForLoops=0` -- no methods are OSR eligible, all get switched to optimized
* `DOTNET_TC_OnStackReplacement=0` -- no methods are OSR eligible, all get jitted at Tier0
* `DOTNET_JitEnableOsrRange=...` -- use method hash to refine which methods are OSR eligible
* `DOTNET_JitEnablePatchpointRange=...` -- use method hash to refine which Tier0 methods get patchpoints

Binary searching using the `Enable` controls has proven to be a reliable 
way to track down issues in OSR codegen.

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

### Changing where patchpoints are placed in Tier0 code

* `DOTNET_JitOffsetOnStackReplacement=offset` -- only place patchpoints at given IL offset (and if stack empty)
* `DOTNET_JitRandomOnStackReplacement=val` -- in addition to normal OSR patchpoints, place patchpoints randomly
 at stack empty points at the starts of non-handler blocks (value is likelihood, in hex, so 0 will not add any extra, 0x64 will
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








