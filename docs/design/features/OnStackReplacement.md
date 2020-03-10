# On Stack Replacement in the CLR

Design Sketch and Prototype Assessment

Andy Ayers

Initial: 7 July 2019 &mdash;
Revised: 25 February 2020

## Overview

On Stack Replacement allows the code executed by currently running methods to be
changed in the middle of method execution, while those methods are active "on
stack." This document describes design considerations and challenges involved in
implementing basic On Stack Replacement for the CLR, presents the results of
some prototype investigations, and describes how OSR might be used to re-host
Edit and Continue and support more general transitions like deoptimization.

* [Background](#1-Background)
* [Design Principles](#2-Design-Principles)
* [An Overview of OSR](#3-An-Overview-of-OSR)
* [Complications](#4-Complications)
* [The Prototype](#5-The-Prototype)
* [Edit and Continue](#6-Edit-and-Continue)
* [Deoptimization](#7-Deoptimization)
* [References](#8-References)

## 1. Background

On Stack Replacement (hereafter _OSR_) refers to a set of techniques for
migrating active stack frames from one version of code to another.

The two versions of the code involved in OSR may arise from different program
sources (as in Edit and Continue) or different approaches to compiling or
executing a single program (say, unoptimized code versus optimized code). The
goal of OSR is to transparently redirect execution from an old version of code
into a new version, even when in the middle of executing the old version.

Initial work on OSR was pioneered in Self [[1](#1)] as an approach for debugging
optimized code. But in the years since, OSR has mainly seen adoption on
platforms like Java [[2](#2), [3](#3)] and JavaScript that rely heavily on
adaptive recompilation of code.

The ability to adaptively recompile and switch code versions while methods are
running provides some key advantages:

* Platforms can offer both quick start up and excellent steady-state
  performance, interpreting or quickly jitting to enable initial method
  execution, and using OSR to update the methods with better performing or more
  completely compiled versions as needed.

* Platforms can take advantage of transient program facts and recover when those
  facts no longer become true. For example, a platform may compile virtual or
  interface calls as direct calls initially and use OSR to update to more
  general versions of code when overriding methods or other interface
  implementations arrive on scene.

The CLR already supports various mechanisms for changing the code for a method
in a runtime instance. Edit and Continue implements true OSR but is supported
only on some architectures, works only when code is running under a managed
debugger, and is supported only for unoptimized to unoptimized code. Profiler
rejit and tiered compilation can update code used in future invocations of
methods, but not code running in currently active methods.

In this document we will vary a bit from the literature and use OSR to refer
strictly to the case where we are transitioning execution **from** an
unoptimized code instance (either to another unoptimized instance or an
optimized instance). We will use _deoptimization_ (_deopt_) to describe the
transition from an optimized code instance to some other code instance
(typically to an unoptimized instance).

We envision OSR as a technology that will allow us to enable tiered compilation
by default: performance-critical applications will no longer risk seeing key
methods trapped in unoptimized tier0 code, and straightforwardly written
microbenchmarks (e.g. all code in main) will perform as expected, as no matter
how they are coded, they will be able to transition to optimized code.

OSR also provides key building blocks for an eventual implementation of deopt
and the ability of our platforms to make strong speculative bets in code
generation.

In addition, OSR will also allow us to experiment with so-called _deferred
compilation_, where the jit initially only compiles parts of methods that it
believes likely to execute (say, based on heuristics or prior runs). If an
uncompiled part of a method is reached at runtime, OSR can trigger recompilation
of the missing part or recompilation of the entire method.

The remainder of this document describes OSR in more detail, providing a design
sketch and some key design choice points, the results and insights gained from
creating a fully functional prototype, and a list of open issues and areas
requiring further investigation. We will also mention _deopt_ in passing and
describe why it presents a different and larger set of challenges.

## 2. Design Principles

As we consider proposals for implementing OSR, we will try and satisfy the
following design principles:

* Pay as you go. The costs of OSR should be limited to methods that can benefit
  from OSR, and where possible, paid largely when OSR actually happens.

* Impose few restrictions on optimized codegen. We should not have to restrict
  or dumb down optimized codegen to allow transitions to it via OSR

* Anticipate likely changes in jit codegen strategy. We should support enabling
  some optimizations (devirtualization, early branch pruning, some expression
  opts) at Tier0 without having to radically alter our approach.

* Design for testability. We should be able to force OSR transitions wherever
  possible and with alternative stress strategies.

* Full diagnostic experience. OSR should not inhibit user ability to debug or
  reason about logical behavior of their programs. OSR activities should be
  tracked via suitable eventing mechanisms.

## 3 An overview of OSR

OSR enables transitioning from older unoptimized code to new code
while the old code is active in some stack frames. An implementation
must come up with solutions to several related sub problems, which we
describe briefly here, and in more detail below.

* **Patchpoints** : Identify where in the original method OSR is possible. 
We will use the term _patchpoint_ to describe a particular location in a
method's code that supports OSR transitions.
* **Triggers** : Determine what will trigger an OSR transition
* **Alternatives** : Have means to prepare a suitable alternative code
version covering all or part of the method (loops, for instance), and
having one or possibly many entry points.
* **Transitions**: Remap the stack frame(s) as needed to carry out the
OSR transition

### 3.1 Patchpoints

A _patchpoint_ is a point in a version of code where OSR is possible.
Patchpoints are similar in many ways to GC safepoints. At a patchpoint, the live
state of the ongoing computation must be identifiable (for a GC safepoint, only
the live GC references need be so identified). All live registers and stack
slots must be enumerable, and logically described in terms of concepts visible
in the IL. Additional state like the return address, implicit arguments, and so
on must also be accounted for.

As with GC safepoints, patchpoints can be handled in a _fully interruptible_
manner where most any instruction boundary is a patchpoint, or a _partially
interruptible_ manner, where only some instruction boundaries are patchpoints.
Also, as with GC, it is acceptable (if suboptimal) to over-identify the live
state at a patch point. For instance, the live set can include values that never
end up being consumed by the new method (the upshot here is that we can simply
decide all the visible IL state is live everywhere, and so avoid running
liveness analysis in Tier0.)

Also, as with GC safepoints, it is desirable to keep the volume of information
that must be retained to describe patchpoints to a minimum. Most methods
executions will never undergo OSR transition and so the information generated
will never be consulted. To try and keep OSR a _pay as you go_ technique, it is
important that this information be cheap to generate and store.

#### 3.1.1 Choosing Patchpoints

Most commonly, patchpoints are chosen to be the places in the code that are
targets of loop back edges. This is a partially interruptible scheme. This
ensures that no loop in the method can iterate without hitting a patchpoint, and
so that the method itself cannot execute indefinitely between patchpoints. Note
by this rule, methods that do not contain any loops will not have any
patchpoints.

From a compilation standpoint, it would be ideal if patchpoints were also IL
stack empty points, as this tends to minimize and regularize the live state.
However, there is no guarantee that execution of a method will reach stack empty
points with any frequency. So, a fully general patchpoint mechanism must handle
the case where the evaluation stack is not empty. However, it may be acceptable
to only allow patchpoints at stack empty points, as loops that execute with
non-empty evaluation stacks are likely rare.

It is also beneficial if patchpoint selection works via a fairly simple set of
rules, and here we propose that using the set of _lexical back edges_ or
backwards branches in IL is a reasonable choice. These can be identified by a
single scan over a method's IL.

When generating unoptimized code, it is thus sufficient to note the target of
any backwards branch in IL, the set of those locations (filtered to just the
subset where the IL stack is empty) are the candidate patchpoints in the method.

We can also rely on the fact that in our current unoptimized code, no IL state
is kept in registers across IL stack empty points&mdash;all the IL state is
stored in the native stack frame. This means that each patchpoint's live state
description is the same&mdash;he set of stack frame locations holding the IL
state.

So, with the above restrictions, a single patchpoint descriptor suffices for the
entire method (analogous to the concept of _untracked_ GC lifetimes in the GC
info). Further, this information is a superset of the current GC info, so the
additional data needed to describe a patchpoint is simply the set of live non-GC
slots on the native stack frame.

[Note: more general schemes like _deopt_ will require something more
sophisticated.]

#### 3.1.2 Option I: non-stack empty patchpoints

If it turns out we must also allow patchpoints at non-stack empty points, then
some per-patchpoint state will be needed to map the logical state of the
evaluation stack into actual stack slots on the methods frame. This state will
vary from patchpoint to patchpoint.

#### 3.1.3 Option II: fully interruptible patchpoints

Patchpoints can be much more fine-grained, at any block boundary or even within
blocks, so long as the correspondence of the generated code to the inspiring IL
is well understood. However fine-grained patchpoints in our proposed version of
OSR do not seem to offer much in the way of advantages, given that we are also
proposing synchronous triggers and transitions, and transitioning from
unoptimized code. A fine-grained patchpoint mechanism would require more
metadata to describe each transition point.

#### 3.1.4 The Prototype

In the prototype, patchpoints are the set of IL boundaries in a method that are
stack-empty and the targets of lexical back edges. The live state of the
original method is just the IL-visible locals and arguments, plus a few special
values found in certain frames (GS Cookie, etc).

### 3.2 Triggers

When OSR is used to enable transfer control from an unoptimized method into
optimized code, the most natural trigger is a count of the number of times a
patchpoint in the method is reached. Once a threshold is reached at a
patchpoint, the system can begin preparation of the alternative code version
that will work for that patchpoint.

This counting can be done fairly efficiently, at least in comparison to the
ambient unoptimized code in the method, by using counters on the local frame.
When the threshold is reached, control can transfer to a local policy block;
this can check whether an alternative version needs to be prepared, is already
being prepared, or is ready for transfer. Since this policy logic is common to
all patchpoints it most likely should be encapsulated as a helper. In
pseudocode:

```
Patchpoint:   // each assigned a dense set of IDs

       if (++counter[ppID] > threshold) call PatchpointHelper(ppID)
```
The helper can use the return address to determine which patchpoint is making
the request. To keep overheads manageable, we might instead want to down-count
and pass the counter address to the helper.
```
Patchpoint:   // each assigned a dense set of IDs

       if (--counter[ppID] <= 0) call PatchpointHelper(ppID, &counter[ppID])
```
The helper logic would be similar to the following:
```
PatchpointHelper(int ppID, int* counter)
{
  void* patchpointSite = _ReturnAddress();
  PPState s = GetState(patchpointSite);

    switch (s)
    {
      case Unknown: 
        *counter = initialThreshold; 
        SetState(s, Active);
        return;

      case Active:  
        *counter = checkThreshold; 
        SetState(s, Pending);
        RequestAlternative(ppID);
        return;

      case Pending:
        *counter = checkThreshold;
        return;

      case Ready:   
         Transition(...); // does not return
     }
}
```
Here `RequestAlternative` would queue up a request to produce the alternative
code version; when that request completes the patchpoint state would be set to
Ready. So the cost for a patchpoint would be an initial helper call (to set the
Active threshold), then counting, then a second helper call (to request and set
the pending threshold), then counting, and, depending on how long the request
took, more callbacks in pending state.

Note that just because a patchpoint is hit often enough to reach Active state,
there is no guarantee that the patchpoint will be reached again in the future.
So, it is possible to trigger alternative version compilations that end up never
getting used, if those alternative versions are patchpoint specific. In a
pathological case a method might have an entire sequence of patchpoints that
reach Active state and trigger alternative versions, none of which ever get
used.

In this scheme, the local frame of the method would have one local counter per
patchpoint.

#### 3.2.1 Option I: one global counter per patchpoint

Instead of keeping the counters on the local frame, they could be kept in global
storage associated with the method, to give an absolute count of patchpoint
frequency over all invocations of the method. This would help trigger
transitions in methods in use across multiple threads or methods that are a weak
mixture of iteration and recursion. Because there would now be shared counter
state, we'd have to think though how to handle the concurrent access. Likely
we'd implement something like we do for IBC and have a method fetch and locally
cache the address of its counter vector locally in the prolog.

#### 3.2.2 Option II: shared counters

Alternatively, all patchpoints in a method could share one counter slot (either
local or global), this would save space but would lead to somewhat more frequent
callbacks into the runtime and slightly higher likelihood that useless
alternatives would be created.

#### 3.2.3 Option III: synchronous OSR

Independent of the counter scheme, the runtime could also block and
synchronously produce and then transition to the alternative version. This would
eliminate the potential for wasted alternates (though depending on other
choices, we still might produce multiple alternates for a method). It would also
hold up progress of the app, as the thread could just as well continue executing
the unoptimized code past the patchpoint. We might consider transitioning to
synchronous OSR selectively for methods that have a track record of generating
useless versions. This is entirely a runtime policy and would not impact jitted
codegen.

Note: If OSR is used for EnC or for _deopt_ when an invariant changes, then
synchronous transitions are required as in general, the old method cannot safely
execute past a patchpoint. If the delay from jitting code is a concern it may be
possible to fall back to an interpreter for a time while the new version of the
method is jitted, though this would require that the system also support
OSR-style transitions from interpreted methods to compiled methods...

#### 3.2.4 Option IV: share counter space with Tiered Compilation

A final option here is to use global counters and also add a counter at method
entry. The entry counter could be used for two purposes: first to trigger tiered
jitting of the entire method, and second, to help normalize the per-patchpoint
counters so as to provide relative profile weights for the blocks in the method
when it is rejitted (either via tiering or OSR). We note that the set of
observation points from patchpoint counters is fairly sparse (not as detailed as
what we get from IBC, say) but it may be sufficient to build a reasonable
profile.

#### 3.2.5 The Prototype

In the prototype OSR transitions are synchronous; there is one local patchpoint
counter per frame shared by all patchpoints; patchpoint IDs are IL offsets.

### 3.3 Alternative Versions

When a patchpoint is hit often enough, the runtime should produce an alternative
version of the code that can be transitioned to at that patchpoint.

There are several choice points for alternatives:

* Whether to tailor the alternative code specifically to that patchpoint or have
  the alternative handle multiple (or perhaps all) the patchpoints in a method.
  We'll call the former a single-entry alternative, and the latter
  multi-entry alternatives (and, in the limit, whole-method alternatives).

* Whether the alternative version encompasses the remainder of the method, or
  just some part of the method. We'll call these whole and partial
  alternatives.

* If a partial method alternative, whether the part of the method compiled
  includes the entire remainder of the method, or just some fragment that
  includes the patchpoint (say the enclosing loop nest).

* Whether or not the alternative entry points include the code to build up the
  alternative stack frames, or setup of the new frame happens via some runtime
  logic.

* Whether or not the alternate version is tailored to the actual runtime state
  at the point of the trigger. For instance, specific argument or local values,
  or actual types.

The partial alternatives obviously are special versions that can only be used by
OSR. The whole method alternative could also be conceivably used as the
optimized version of the method, but the additional entry points may result some
loss of optimizations. So, in general, the OSR alternatives are likely distinct
from the Tier-1 versions of methods and are used only for active frame
transitions. New calls to methods can be handled via the existing tiering
mechanisms.

[Note there are some interesting dynamics here that may warrant further
consideration. A method that is seldomly called with a hot loop will eventually
trigger both OSR (from the loop) and Tier1 recompilation (from the calls). We
might consider deferring tiered recompilation for such methods, as the
unoptimized versions can readily transition to OSR alternates in code that
matters for performance.]

Taken together there are various combinations of these alternatives that make
sense, and various tradeoffs to consider. We explore a few of these below.

#### 3.3.1 Option 1: Partial Method with Transition Prolog

In this option, the runtime invokes the jit with a method, IL offset, and the
original method mapping of stack frame state to IL state at that offset. The jit
uses the logical PC (IL offset) to determine the scope of the alternative
fragment. Here the scope is the IL in the method reachable from the patchpoint.

For the entry point it creates a specialized transition prolog that sets up a
normal frame, and takes the values of the locals from the old stack frame and
copies them to the new stack slots, and pushes& any live evaluation stack
arguments. Arguments passed in registers are restored to the right registers.
Control then transfers to the IL offset of the patchpoint. Any IL in the method
not reachable from the patchpoint is dead code and can be removed (including the
original method entry point). This new partial method is then jitted more or
less normally (modulo the generation of the special prolog).

It might be possible to express this new prolog in IL or something similar. At
any rate it seems likely the impact on the jit overall can be mostly localized
to the &importer and prolog generation stages and the rest of the jit would
operate more or less as it does today.

This alternative version can be used any time the original method reaches the
inspiring patchpoint.

#### 3.3.2 Option 2: Partial Tailored Method with Transition Prolog

If the runtime also passes the triggering stack frame to the jit, the jit can
incorporate the values in that frame (or information derived from the frame
values) into the alternative method codegen. This creates a tailored alternative
that can only be used at this patchpoint from this specific original method
invocation. The potential benefit here is that the code in the method may be
more optimizable with the additional context, and since OSR alternatives are
likely to be lightly used there may not be much downside to specializing exactly
for this trigger instance. This alternative likely implies synchronous OSR.

#### 3.3.3 Option 3: Full Method with Multiple Entry Points

Instead of generating an alternative that can only be used to transition from
one specific patchpoint, the alternative method can offer multiple entry points
to allow transition from some or all of the patchpoints in the original method.

Note: After thinking about this a bit more, I think we can implement this
variant without needing multiple prologs&mdash;instead we can pass the IL offset
of the OSR entry point as a hidden argument to the OSR method, and have a switch
on that argument in the first body block to jump to the right place in the
method. This might be a viable option to control the potential explosion of OSR
variants for methods with many patchpoints. This method would still be OSR
specific&mdash;that is, it could not also serve as a normally callable Tier1
method.

#### 3.3.4 Option 4: Method Fragment

If the alternative method is just a fragment of the entire method, then in
addition to a specialized entry point, the jit will have to create specialized
exit points that either transition back to the unoptimized method, or else use
synchronous OSR to invoke jitting of the method code that comes after the
fragment.

#### 3.3.5 Prototype

The prototype generates partial methods with transition prolog. Per 4.1 below,
the OSR method frame incorporates the (live portion of the) original method
frame instead of supplanting it.

### 3.4 Transitions

A transition can happen once an OSR capable method reaches a patchpoint where a
suitable alternative version is ready. Because transitions will likely require
changes in stack frame size it is much simpler to consider transitions only for
methods at the top of the stack. This means that methods that are invoked
recursively may be transitioned by OSR gradually as the stack unwinds.

Abstractly, the actual transition could work something like the following: the
runtime would copy the top stack frame into temporary storage, then carefully
unwind the current frame. Then the alternative method would be put in place and
invoked, being passed the copy of the original frame as an argument.

However, the presence of original frame addresses and values derived from those
addresses in the original frame's live state complicates matters (more on this
in [Section 4.1](#Addresses-of-Locals)). So the OSR method needs to ensure that
any "address-exposed" local ends up at the exact same stack location in the OSR
frame as it did in the original method frame. The simplest way to accomplish
this is to just leave the original frame in place, and have the OSR frame
"incorporate" it as part of its frame.

#### 3.4.1 The Prototype

The original method conditionally calls to the patchpoint helper at
patchpoints. The helper will return if there is no transition. 

For a transition, the helper will capture context and virtually unwind itself
and the original method from the stack to recover callee-save register values
live into the original method and then restore the callee FP and SP values into
the context (preserving the original method frame); then set the context IP to
the OSR method entry and restore context. OSR method will incorporate the
original method frame as part of its frame.

## 4 Complications

### 4.1 Addresses of Locals

If the live state at the patchpoint includes addresses of locals (or addresses
of arguments, if the OSR transition pushes a new frame), either these addresses
must be updated to properly reflect the new locations or the address-taken
locals must end up in the same relative location in the frame. The jit might
require some hardening to ensure that address of local is always properly
described at patchpoints.

Detection of address-taken locals (especially in a non-optimizing jit) may
require some attention. We frequently see `ldloca` in IL that is consumed in a
dereference before a stack empty point; such locals are transiently exposed but
their addresses would not be live at our proposed set of patchpoints (note
`ldflda` can cause similar issues if it exposes addresses if local struct
fields).

Arithmetic done on addresses of locals might not be stable across an OSR
transition (that is, different values could be obtained for a given piece of
code before and after the transition). While in general there is no guarantee
about the values produced by this kind of code it is not unreasonable to expect
that the value would not change over the lifetime of a given method's
execution. It is not clear how much code might depend on this.

This problem could be partially solved by requiring any address-taken local to
appear at the same stack location in the alternative method frame and by
requiring that the OSR frame supplant the original frame (this is how EnC
works). In that case all address-taken locals would be at the same address.
Ensuring that this is possible likely entails other restrictions like reserving
a maximally large register save area for the original method.

However, it seems simplest to just preserve the original method frame, or at
least the portion of it that contains the live state, and allow the OSR method
to access the original frame values, either as initial values or as the actual
homes for that state.

### 4.2 Localloc

Methods with localloc pose similar challenges to those posed by methods with
address taken locals. Room is made on the original method stack for the localloc
storage, and a native pointer to that storage is part of the live state of the
method. The live state may also include pointers and other values derived from
that address. So, the alternative version must use that same location; a
copy/fixup procedure to allow this storage to be relocated in some manner seems
impractical.

In addition, localloc makes describing the local frame more complex, as the size
of the frame and the location of particular bits of live state can vary.
Typically, the jit will use multiple frame pointers in a localloc frame to allow
for relative addressing.

In the most complex case, the original method will have executed one or more
locallocs before hitting the patchpoint, and the OSR variant will then execute
more locallocs. Such cases might require the OSR method to maintain 3 or more
frame pointers.

### 4.3 Funclets

When control is executing in a funclet there are effectively two activation
records on the stack that share a single frame: the parent frame and the
funclet frame. The funclet frame is largely a stub frame and most of the frame
state is kept in the parent frame. 

These two frames are not adjacent; they are separated by some number of runtime
frames. This means it is going to be difficult for our system to handle
patchpoints within funclets; even if we could update the code the funclet is
running we would not be able to update the parent frame.

The proposal here is to disallow patchpoints within funclets so that we do not
attempt OSR transitions when the top of stack frame is a funclet frame. One
hopes that performance critical loops rarely appear in catch or finally clauses.

EnC has similar restrictions.

### 4.4 GC

There is a brief window of time during the transition where there are GC live
values on both the original and alternative frames (and the original frame may
have been copied off-stack). Since the transition is done via a runtime helper,
it seems prudent to forbid GC during this part of the transition, which should
be relatively brief.

### 4.5 Diagnostics

Alternative methods will never be called &mdash; they are only transitioned to
by active original methods, so likely no special work is needed to make them
compatible with the current profiler guarantees for IL modifications ("new
invocations" of the method invoke the new version).

We may need to update the mechanisms that the runtime uses to notify profilers
of new native code versions of a method.

The jit will generate the same debug info mappings as it does today, and so the
debugging experience when debugging an alternative should be similar to the
experience debugging a Tier1 method. Likewise, the code publishing aspects
should be common, so for instance active breakpoints should get applied.

[Note: I have verified this on simple examples using the VS debugger; a source
breakpoint set in the original method is applied to the OSR method too.]

We need to decide what happens if the debugger tries to use SetIP on an OSR
method for an IL offset that is not within the range of IL compiled; likely
we'll just have to fail the request.

Breakpoints set at native code addresses won't transfer to the corresponding
points in OSR methods. We have the same issue with Tiered compilation already.

OSR (exclusive of EnC) will be disabled for debuggable code.

Debugging through an OSR transition (say a single-step that triggers OSR) may
require special consideration. This is something that needs further
investigation.

**Prototype: The OSR methods have somewhat unusual unwind records that may be
confusing the (Windbg) debugger stack trace.**

### 4.6 Proposed Tier-0 Optimizations

We have been assuming up until this point that the original method was not
optimized in any way, and so its live state is safely over-approximated by the
values of all locals, arguments, evaluation stack entries. This means that any
value truly live at a reachable patchpoint (capable of influencing future
computation) is included in the live set. The reported live set might well be
larger, of course. The alternative method will likely run liveness and pick from
this set only the values it sees as truly live.

This means that we can run optimizations in the original method so long as they
do not alter the computation of the over-approximated live set at any
patchpoint.

The proposed Tier0 optimizations fit into this category, so long as we restrict
patchpoints to stack-empty points: we may prune away unreachable code paths (say
from HW intrinsic checks or provably true or false predicate evaluations &mdash
;patchpoints in pruned sections would be unreachable) and simplify computations.
Optimizing expressions may reduce the truly live set but so long as all stores
to locals and args are kept live the base values needed for any alternate
version of the code will be available.

### 4.7 Alternative Method Optimizations

In options where the alternative method has multiple entry points, one must be
wary of early aggressive optimizations done when optimizing the alternative. The
original version of the method may hit a patchpoint while executing code that
can be optimized away by the more aggressive alternative method compiler (e.g.
it may be executing a series of type equality tests in a generic method that the
optimizing jit can evaluate at jit time). But with our simple patchpoint
recognition algorithm the alternate compiler can quickly verify that the
patchpoint IL offset is a viable entry point and ensure that the code at that
offset is not optimized away. If it turns out that the entry point code is
optimizable then we may choose to peel one iteration from the entry point loop
(because with our patchpoint strategy, execution in the alternate method will
immediately hit a loop top once it is out of the prolog) and allow the in-loop
versions to be optimized.

###  4.8 Prologs and Unwind

The alternative version of the method will, in all likelihood, need to save and
restore a different set of callee-saves registers than the original version. But
since the original stack frame has already saved some registers, the alternative
version prolog will either need to save a superset of those registers or else
restore the value of some registers in its prolog. So, the alternative version
needs to know which registers the original saved and where in the stack they are
stored.

If we want to preserve frame offsets for address-taken locals then we may face a
conflict as altering the number of callee save slots may alter frame offsets for
locals. One thought here is that we could perhaps implement a chained unwind
scheme, where there is an initial prolog that emulates the original version
prolog and duplicates its saves, and then a subsequent "shrink wrapped" prolog
&amp; epilog that saves any additional registers in a disjoint area.

**Prototype:** When it is time to transition, the patchpoint helper virtually
unwinds two frames from the stack&mdash;its own frame, and the frame for the
original method. So the unwound context restores the callee saves done by the
original method. That turns out to be sufficient.

You might think the helper would need to carefully save all the register state
on entry, but that's not the case. Because the original method is un-optimized,
there isn't any live IL state in registers across the call to the patchpoint
helper&mdash;all the live IL state for the method is on the original
frame&mdash;so the argument and caller-save registers are dead at the
patchpoint. Thus only part of register state that is significant for ongoing
computation is the callee-saves, which are recovered via virtual unwind, and the
frame and stack pointers of the original method, which are likewise recovered by
virtual unwind.

With this context in hand, the helper then "calls" the OSR method by restoring
the context. The OSR method performs its own callee-saves as needed, and
recovers the arguments/IL state from the original frame.

If we were to support patchpoints in optimized code things would be more
complicated.

### 4.9 Synchronous Methods

OSR methods only need add the code to release the synchronous method monitor.
This must still be done in a try-finally to ensure release even on exceptional
exit.

### 4.10 Profile Enter/Leave Hooks

OSR methods only need to support the method exit hook.

## 5 The Prototype

Based on the above, we developed a prototype implementation of OSR to gain
experience, gather data, and test out assumptions.

The prototype chose the following options:
* Patchpoints: lexical back edge targets that are stack empty and not in try
  regions; live state is all locals and args + specials (thus no liveness needed
  at Tier0)
* Trigger: one shared counter per frame. Initial value configurable at runtime.
  Patchpoints decrement the counter and conditionally call the runtime helper if
  the value is zero or negative.
* Alternatives: partial method tailored to each patchpoint. OSR method
  incorporates the original method frame.
* Transition: synchronous&mdash;once the patchpoint has been hit often enough a
  new alternative is jitted.

The prototype works for x64 on Windows and Linux, and can pass the basic (pri0)
tests suites with an aggressive transition policy (produce the OSR method and
transition the first time each patchpoint is hit).

### 5.1 Example Codegen

Consider the following simple method:
```C#
    public static int F(int from, int to)
    {
        int result = 0;
        for (int i = from; i < to; i++)
        {
            result += i;
        }
        return result;
    }

```
Normal (Tier0, x64 windows) codegen for the method is:
```asm
; Tier-0 compilation

G_M6138_IG01:
       55                   push     rbp
       4883EC10             sub      rsp, 16
       488D6C2410           lea      rbp, [rsp+10H]
       33C0                 xor      rax, rax
       8945FC               mov      dword ptr [rbp-04H], eax    // result
       8945F8               mov      dword ptr [rbp-08H], eax    // i
       894D10               mov      dword ptr [rbp+10H], ecx    // from
       895518               mov      dword ptr [rbp+18H], edx    // to

G_M6138_IG02:
       33C0                 xor      eax, eax
       8945FC               mov      dword ptr [rbp-04H], eax
       8B4510               mov      eax, dword ptr [rbp+10H]
       8945F8               mov      dword ptr [rbp-08H], eax
       EB11                 jmp      SHORT G_M6138_IG04

G_M6138_IG03:
       8B45FC               mov      eax, dword ptr [rbp-04H]
       0345F8               add      eax, dword ptr [rbp-08H]    // result += i
       8945FC               mov      dword ptr [rbp-04H], eax
       8B45F8               mov      eax, dword ptr [rbp-08H]
       FFC0                 inc      eax
       8945F8               mov      dword ptr [rbp-08H], eax

G_M6138_IG04:
       8B45F8               mov      eax, dword ptr [rbp-08H]
       3B4518               cmp      eax, dword ptr [rbp+18H]
       7CE7                 jl       SHORT G_M6138_IG03          // i < to ?
       8B45FC               mov      eax, dword ptr [rbp-04H]

G_M6138_IG05:
       488D6500             lea      rsp, [rbp]
       5D                   pop      rbp
       C3                   ret
```
with OSR enabled (and patchpoint counter initial value = 2), this becomes:
```asm
; Tier-0 compilation + Patchpoints

G_M6138_IG01:
       55                   push     rbp
       4883EC30             sub      rsp, 48
       488D6C2430           lea      rbp, [rsp+30H]
       33C0                 xor      rax, rax
       8945FC               mov      dword ptr [rbp-04H], eax    // result
       8945F8               mov      dword ptr [rbp-08H], eax    // i
       894D10               mov      dword ptr [rbp+10H], ecx    // from
       895518               mov      dword ptr [rbp+18H], edx    // to

G_M6138_IG02:
       33C9                 xor      ecx, ecx
       894DFC               mov      dword ptr [rbp-04H], ecx    // result = 0
       8B4D10               mov      ecx, dword ptr [rbp+10H]
       894DF8               mov      dword ptr [rbp-08H], ecx    // i = from
       C745F002000000       mov      dword ptr [rbp-10H], 2      // patchpointCounter = 2
       EB2D                 jmp      SHORT G_M6138_IG06

G_M6138_IG03:
       8B4DF0               mov      ecx, dword ptr [rbp-10H]    // patchpointCounter--
       FFC9                 dec      ecx
       894DF0               mov      dword ptr [rbp-10H], ecx
       837DF000             cmp      dword ptr [rbp-10H], 0      // ... > 0 ?
       7F0E                 jg       SHORT G_M6138_IG05         

G_M6138_IG04:           ;; bbWeight=0.01
       488D4DF0             lea      rcx, bword ptr [rbp-10H]    // &patchpointCounter
       BA06000000           mov      edx, 6                      // ilOffset
       E808CA465F           call     CORINFO_HELP_PATCHPOINT

G_M6138_IG05:
       8B45FC               mov      eax, dword ptr [rbp-04H]
       0345F8               add      eax, dword ptr [rbp-08H]
       8945FC               mov      dword ptr [rbp-04H], eax
       8B45F8               mov      eax, dword ptr [rbp-08H]
       FFC0                 inc      eax
       8945F8               mov      dword ptr [rbp-08H], eax

G_M6138_IG06:
       8B4DF8               mov      ecx, dword ptr [rbp-08H]
       3B4D18               cmp      ecx, dword ptr [rbp+18H]
       7CCB                 jl       SHORT G_M6138_IG03
       8B45FC               mov      eax, dword ptr [rbp-04H]

G_M6138_IG07:
       488D6500             lea      rsp, [rbp]
       5D                   pop      rbp
       C3                   ret
```
Because Tier0 is unoptimized code, the patchpoint sequence is currently
unoptimized. This leads to a moderate amount of code bloat in methods with
patchpoints. The overall code size impact of patchpoints (as measured by
`jit-diff`) is around 2%, but this is this is an understatement of the impact to
methods that have patchpoints, as most Tier0 methods won't require patchpoints.
This is something that can be improved.

The OSR method for this patchpoint is:
```asm
; Tier-1 compilation
; OSR variant for entry point 0x6

G_M6138_IG01:
       8B542450             mov      edx, dword ptr [rsp+50H]    // to
       8B4C2434             mov      ecx, dword ptr [rsp+34H]    // result
       8B442430             mov      eax, dword ptr [rsp+30H]    // i

G_M6138_IG02:           ;; bbWeight=8
       03C8                 add      ecx, eax
       FFC0                 inc      eax
       3BC2                 cmp      eax, edx
       7CF8                 jl       SHORT G_M6138_IG02

G_M6138_IG03:
       8BC1                 mov      eax, ecx

G_M6138_IG04:
       4883C438             add      rsp, 56
       5D                   pop      rbp
       C3                   ret
```
Here the live state is `result`, `i`, and `to`. These are kept in registers and
initialized in the prolog to the values they had in the original frame. The jit
request for the OSR method includes 'OSR_INFO" metadata describing the original
method frame, so the jit can compute the correct addresses for original frame
slots in the OSR method.

Because the OSR method is entered with the original method frame still active,
the OSR method has asymmetric prolog and epilog sequences. This is reflected in
the unwind data for the OSR method by recording a "phantom prolog" to account
for actions taken by the original method. These are at code offset 0 so happen
"instantaneously" when the method is entered.
```
  UnwindCodes:
    CodeOffset: 0x00 UnwindOp: UWOP_ALLOC_SMALL (2)     OpInfo: 6 * 8 + 8 = 56 = 0x38
    CodeOffset: 0x00 UnwindOp: UWOP_PUSH_NONVOL (0)     OpInfo: rbp (5)
```
By way of comparison, here is the full Tier-1 version of the method.
```asm
G_M6138_IG01:

G_M6138_IG02:
       33C0                 xor      eax, eax
       3BCA                 cmp      ecx, edx
       7D08                 jge      SHORT G_M6138_IG04

G_M6138_IG03:           ;; bbWeight=4
       03C1                 add      eax, ecx
       FFC1                 inc      ecx
       3BCA                 cmp      ecx, edx
       7CF8                 jl       SHORT G_M6138_IG03

G_M6138_IG04:
       C3                   ret
```
Note the inner loop codegen is very similar to the OSR variant. This is typical.
It is often possible to diff the Tier1 and OSR codegen and see that the latter
is just a partial version of the former, with different register usage and
different stack offsets.

### 5.2 More Complex Examples

If the OSR method needs to save and restore registers, then the epilog will have
two stack pointer adjustments: the first to reach the register save area on the
OSR frame, the second to reach the saved RBP and return address on the original
frame.

For example:
```asm
       4883C440             add      rsp, 64
       5B                   pop      rbx
       5E                   pop      rsi
       5F                   pop      rdi
       4883C448             add      rsp, 72
       5D                   pop      rbp
       C3                   ret      
```
with unwind info:
```
  UnwindCodes:
    CodeOffset: 0x07 UnwindOp: UWOP_ALLOC_SMALL (2)     OpInfo: 7 * 8 + 8 = 64 = 0x40
    CodeOffset: 0x03 UnwindOp: UWOP_PUSH_NONVOL (0)     OpInfo: rbx (3)
    CodeOffset: 0x02 UnwindOp: UWOP_PUSH_NONVOL (0)     OpInfo: rsi (6)
    CodeOffset: 0x01 UnwindOp: UWOP_PUSH_NONVOL (0)     OpInfo: rdi (7)
    CodeOffset: 0x00 UnwindOp: UWOP_ALLOC_SMALL (2)     OpInfo: 8 * 8 + 8 = 72 = 0x48
    CodeOffset: 0x00 UnwindOp: UWOP_PUSH_NONVOL (0)     OpInfo: rbp (5)
```

If the OSR method needs to save RBP, we may see two RBP restores in the epilog;
this does not appear to cause problems during execution, as the "last one wins"
when unwinding.

However, the debugger (at least windbg) may end up being confused; any tool
simply following the RBP chain will see the original frame is still "linked"
into the active stack.

### 5.3 PatchpointInfo

As noted above, when the jit is invoked to create the OSR method, it asks the
runtime for some extra data:
* The IL offset of the OSR entry point
* `PatchpointInfo`: a description of the original method frame

`PatchpointInfo` is produced by the jit when jitting the Tier0 method. It is
allocated by the runtime similarly to other codegen metadata like GC info and
unwind info and is likewise associated with the original method. When the
runtime helper decides to kick off an OSR jit, it sets things up so that the jit
can retrieve this data.

Since the `PatchpointInfo` is produced and consumed by the jit its format is
largely opaque to the runtime. It has the following general layout:
```C++
struct PatchpointInfo
{
    unsigned m_patchpointInfoSize;
    unsigned m_ilSize;
    unsigned m_numberOfLocals;
    int      m_fpToSpDelta;
    int      m_genericContextArgOffset;
    int      m_keptAliveThisOffset;
    int      m_securityCookieOffset;
    int      m_offsetAndExposureData[];
};
```
The key values are the `fpToSpDelta` which describes the extent of the original
frame, and the `offsetAndExposureData` which describe the offset of each local
on the original frame.

### 5.4 Performance Impact

The prototype is mainly intended to show that OSR can be used to improve startup
without compromising steady-state performance: with OSR, we can safely use the
quick jit for almost all methods.

We are currently evaluating the performance impact of OSR on some realistic
scenarios.

Initial data shows a general improvement in startup time, in particular for
applications where startup was impacted by disabling quick-jitting of methods
with loops (see dotnet/coreclr#24252).

### 5.5 Prototype Limitations and Workarounds

* x64 only
* Struct promotion is currently disabled for OSR methods
* No OSR for synchronous methods
* No OSR for methods with profiler hooks
* No OSR for methods with localloc
* No OSR from "handler" regions (catch/finally/filter)

The prototype trigger strategy is a hybrid: it has a per-frame local counter and
a per-patchpoint global counter (kept by the runtime). This is probably
something we need to re-assess.

## 6 Edit and Continue

As mentioned in the introduction, OSR is similar to Edit and Continue (EnC). EnC
transitions from an original unoptimized version of a method to a new
unoptimized version with slightly different IL. The CLR already supports EnC on
some platforms, and we briefly review the current implementation here. Our main
interest here is in edits to method IL for an active method, so we focus on that
aspect.

### 6.1 Current EnC Support

Method modification in EnC works roughly as follows. A process being debugged is
stopped at a breakpoint. The user makes a source edit and hits apply. The source
edit is vetted by the language compiler as suitable for EnC and metadata edits
are sent to the runtime via the debugger. For method modifications these edits
create a new version of method IL. Any subsequent invocations of that method
will use that new IL.

To update currently active versions, the debugger also adds special breakpoints
to the plausible patchpoints of the original method's native code. Execution
then resumes. When one of those special breakpoints is hit, an original
method's active frame is at the top of the stack, and the frame can be
transitioned over to the new version. The remapping is done by using the debug
information generated by the jit for both the old and new versions of the
method. As part of this the runtime verifies that locals remain at the same
addresses in the old and new stack frames (thus avoiding the complication noted
earlier in [Section 4.1](#Addresses-of-Locals)).

The jit is notified if a method is going to potentially be eligible for EnC and
takes some precautions to ensure the EnC transition can be handled by the
runtime: for instance, the jit will always save the same set of registers and
use a frame pointer.

So, for EnC, we see that:

- The viable patchpoints are determined by the debugger (via
  EnCSequencePointHelper). These are restricted to be stack empty points (since
  debug info will not describe contents of the evaluation stack) that are not in
  filter or handlers. They are a broader set than we envision needing for OSR.
- The necessary mapping information (stack frame layout, native to IL mapping,
  and native offsets of stack empty points) is present in the debug stream
  generated by the jit for the original method.
- The trigger is a set of special breakpoints placed into the original method
  native code by the debugger when an edit is applied to the method.
- When an EnC breakpoint is hit, the debugger can choose whether or not to
  initiate a transition.
- If the debugger initiates a transition, it is done synchronously: the new
  version of the method is jitted if necessary and the currently active frame is
  transitioned over to the new version, via ResumeInUpdatedFunction. Of interest
  here are the lower-level methods used here to update the frame:
  FixContextAndResume and FixContextForEnC.
- The alternative version is a full method and can be used to transition from
  any patchpoint in the original method.
- The jit modifies its codegen somewhat to facilitate the transition. It does
  not, however, explicitly model the alternate entry points.

## 7 Deoptimization

Up until this point we have been assuming the original method was not optimized
or was optimized in a manner that did not alter its reported live state.

More general optimizations break this property and so additional bookkeeping and
some restrictions on optimizations may be necessary to allow OSR transitions
from optimized code. We touch on this briefly below.

Optimizations can either increase or decrease live state.

For instance, unused computations can be removed, and unused local updates
("dead stores") can be skipped. Registers holding no longer live locals can be
reused for other values (as can stack slots, though the current jit does not do
this).

Other optimizations can increase the live state. The classic example is inlining
&mdash; a call to a method is expanded inline, and so at patchpoints within the
inline body, there are now arguments and locals to the original method, plus
arguments and locals to the inline method. If we wish to make an OSR transition
from such a patchpoint to say unoptimized code, we need to effectively undo the
inlining, creating two frames (or more generally N frames) in place of the
original frame, and two alternate methods (or N alternate methods).

The general solution is to first ensure that the live state never decreases. The
patchpoint locations are determined early, and any values truly live at a
patchpoint at that initial stage of compilation are forced to remain live at
that patchpoint always. So, some dead store elimination is inhibited, and some
forms of code motion are inhibited (e.g. one cannot sink a store to a local out
of a loop, as the patchpoint at loop top would not observe the updated value).

With all the "naive" state guaranteed live at a patchpoint, and any additions to
live state via inlining carefully tracked, one can transition from optimized
code via OSR.

Given the need to preserve address artifacts, this transition must be done
gradually&mdash;first creating a frame for the innermost inlined method that
extends the original frame, then, when this innermost method returns, creating a
frame for the next innermost inlined method, and so on, until finally the
root method frame returns and can clean up the optimized method frame as well.

Each of these (presumably, unoptimized) deopt target methods will need to be
custom-crafted to access the optimized method frame.

This same consideration makes it challenging to implement deopt fallbacks to
an interpreter; the interpreter will likewise need to keep some of its state
in the original method frame.

We currently don't have any need to transfer control out of jitted optimized
code (Tier1), though one could potentially imagine supporting this to better
debug optimized code. The really strong motivations for deoptimization may come
about when the system is optimizing based on "currently true" information that
has now become invalid.

## 8 References

1. <a id="1"></a> U. Holzle, C. Chambers and D. Ungar, "Debugging Optimized
   Code with Dynamic Deoptimization," in _ACM PLDI_, 1992.
2. <a id="2"></a> M. Paleczny, C. Vick and C. Click, "The Java Hotspot(tm)
   Server Compiler," in _USENIX Java Virtual Machine Research and
   Technology Symposium_, 2001.
3. <a id="3"></a> S. Fink and F. Qian, "Design, Implementation and
   Evaluation of Adaptive Recompilation with On-Stack Replacement," in _In
   International Symposium on Code Generation and Optimization (CGO)_, 2003.
