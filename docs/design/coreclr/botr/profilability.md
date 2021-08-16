Implementing Profilability
==========================

This document describes technical details of adding profilability to a CLR feature.  This is targeted toward devs who are modifying the profiling API so their feature can be profilable.

Philosophy
==========

Contracts
---------

Before delving into the details on which contracts should be used in the profiling API, it's useful to understand the overall philosophy.

A philosophy behind the default contracts movement throughout the CLR (outside of the profiling API) is to encourage the majority of the CLR to be prepared to deal with "aggressive behavior" like throwing or triggering.  Below you'll see that this goes hand-in-hand with the recommendations for the callback (ICorProfilerCallback) contracts, which generally prefer the more permissive ("aggressive") of the contract choices.  This gives the profiler the most flexibility in what it can do during its callback (in terms of which CLR calls it can make via ICorProfilerInfo).

However, the Info functions (ICorProfilerInfo) below are just the opposite: they're preferred to be restrictive rather than permissive.  Why?  Because we want these to be safe for the profiler to call from as many places as possible, even from those callbacks that are more restrictive than we might like (e.g., callbacks that for some reason must be GC\_NOTRIGGER).

Also, the preference for more restrictive contracts in ICorProfilerInfo doesn't contradict the overall CLR default contract philosophy, because it is expected that there will be a small minority of CLR functions that need to be restrictive.  ICorProfilerInfo is the root of call paths that fall into this category.  Since the profiler may be calling into the CLR at delicate times, we want these calls to be as unobtrusive as possible.  These are not considered mainstream functions in the CLR, but are a small minority of special call paths that need to be careful.

So the general guidance is to use default contracts throughout the CLR where possible.  But when you need to blaze a path of calls originating from a profiler (i.e., from ICorProfilerInfo), that path will need to have its contracts explicitly specified, and be more restrictive than the default.

Performance or ease of use?
---------------------------

Both would be nice.  But if you need to make a trade-off, favor performance.  The profiling API is meant to be a light-weight, thin, in-process layer between the CLR and a profiling DLL.  Profiler writers are few and far between, and are mostly quite sophisticated developers.  Simple validation of inputs by the CLR is expected.  But we only go so far.  For example, consider all the profiler IDs.  They're just casted pointers of C++ EE object instances that are called into directly (AppDomain\*, MethodTable\*, etc.).  A Profiler provides a bogus ID?  The CLR AVs!  This is expected.  The CLR does not hash IDs, in order to validate a lookup . Profilers are assumed to know what they are doing.

That said, I'll repeat: simple validation of inputs by the CLR is expected.  Things like checking for NULL pointers, that classes requested for inspection have been initialized, "parallel parameters" are consistent (e.g., an array pointer parameter must be non-null if its size parameter is nonzero), etc.

ICorProfilerCallback
====================

This interface comprises the callbacks made by the CLR into the profiler to notify the profiler of interesting events.  Each callback is wrapped in a thin method in the EE that handles locating the profiler's implementation of ICorProfilerCallback(2), and calling its corresponding method.

Profilers subscribe to events by specifying the corresponding flag in a call to ICorProfilerInfo::SetEventMask().  The profiling API stores these choices and exposes them to the CLR through specialized inline functions (CORProfiler\*) that mask against the bit corresponding to the flag.   Then, sprinkled throughout the CLR, you'll see code that calls the ICorProfilerCallback wrapper to notify the profiler of events as they happen, but this call is conditional on the flag being set (determined by calling the specialized inline function):

    {
        //check if profiler set flag, pin profiler
        BEGIN_PIN_PROFILER(CORProfilerTrackModuleLoads());

        //call the wrapper around the profiler's callback implementation
        g_profControlBlock.pProfInterface->ModuleLoadStarted((ModuleID) this);

        //unpin profiler
        END_PIN_PROFILER();
    }

To be clear, the code above is what you'll see sprinkled throughout the code base.  The function it calls (in this case ModuleLoadStarted()) is our wrapper around the profiler's callback implementation (in this case ICorProfilerCallback::ModuleLoadStarted()).  All of our wrappers appear in a single file (vm\EEToProfInterfaceImpl.cpp), and the guidance provided in the sections below relate to those wrappers; not to the above sample code that calls the wrappers.

The macro BEGIN\_PIN\_PROFILER evaluates the expression passed as its argument.  If the expression is TRUE, then the profiler is pinned into memory (meaning the profiler will not be able to detach from the process) and the code between the BEGIN\_PIN\_PROFILER and END\_PIN\_PROFILER macros is executed.  If the expression is FALSE, all code between the BEGIN\_PIN\_PROFILER and END\_PIN\_PROFILER macros is skipped.  For more information about the BEGIN\_PIN\_PROFILER and END\_PIN\_PROFILER macros, find their definition in the code base and read the comments there.

Contracts
---------

Each and every callback wrapper must have some common gunk at the top.  Here's an example:

    CONTRACTL
    {
        // Yay!
        NOTHROW;

        // Yay!
        GC_TRIGGERS;

        // Yay!
        MODE_PREEMPTIVE;

        // Yay!
        CAN_TAKE_LOCK;

        // Yay!
        ASSERT_NO_EE_LOCKS_HELD();
    }
    CONTRACTL_END;
    CLR_TO_PROFILER_ENTRYPOINT((LF_CORPROF,
                            LL_INFO10,
                            "**PROF: useful logging text here.\n"));

Important points:

- You must explicitly specify a value for the throws, triggers, mode, take\_lock, and ASSERT\_NO\_EE\_LOCKS\_HELD() (latter required on callbacks only). This allows us to keep our documentation for profiler-writers accurate.
- Each contract must have its own comment (see below for specific details on contracts)

There's a "preferred" value for each contract type.  If possible, use that and comment it with "Yay!" so that others who copy / paste your code elsewhere will know what's best.  If it's not possible to use the preferred value, comment why.

Here are the preferred values for callbacks.

| Preferred | Why | Details |
| --------- | --- | ------- |
| NOTHROW   | Allows callback to be issued from any CLR context.  Since Infos should be NOTHROW as well, this shouldn't be a hardship for the profiler.   | Note that you will get throws violations if the profiler calls a THROWS Info function from here, even though the profiler encloses the call in a try/catch (because our contract system can't see the profiler's try/catch).  So you'll need to insert a CONTRACT\_VIOLATION(ThrowsViolation) scoped just before the call into the profiler. |
| GC\_TRIGGERS | Gives profiler the most flexibility in the Infos it can call. | If the callback is made at a delicate time where protecting all the object refs would be error-prone or significantly degrade performance, use GC\_NOTRIGGER (and comment of course!). |
| MODE\_PREEMPTIVE if possible, otherwise MODE\_COOPERATIVE | MODE\_PREEMPTIVE gives profiler the most flexibility in the Infos it can call (except when coop is necessary due to ObjectIDs).  Also, MODE\_PREEMPTIVE is a preferred "default" contract throughout the EE, and forcing callbacks to be in preemptive encourages use of preemptive elsewhere in the EE. | MODE\_COOPERATIVE is fair if you're passing ObjectID parameters to the profiler.  Otherwise, specify MODE\_PREEMPTIVE.  The caller of the callback should hopefully already be in preemptive mode anyway.  If not, rethink why not and potentially change the caller to be in preemptive.  Otherwise, you will need to use a GCX\_PREEMP() macro before calling the callback. |
| CAN\_TAKE\_LOCK | Gives profiler the most flexibility in the Infos it can call | Nothing further, your honor. |
| ASSERT\_NO\_EE\_LOCKS\_HELD() | Gives profiler even more flexibility on Infos it can call, as it ensures no Info could try to retake a lock or take an out-of-order lock (since no lock is taken to "retake" or destroy ordering) | This isn't actually a contract, though the contract block is a convenient place to put this, so you don't forget.  As with the contracts, if this cannot be specified, comment why. |

Note: EE\_THREAD\_NOT\_REQUIRED / EE\_THREAD\_REQUIRED need **not** be specified for callbacks.  GC callbacks cannot specify "REQUIRED" anyway (no EE Thread might be present), and it is only interesting to consider these on the Info functions (profiler &#8594; CLR).

Entrypoint macros
-----------------

As in the example above, after the contracts there should be an entrypoint macro.  This takes care of logging, marking on the EE Thread object that we're in a callback, removing stack guard, and doing some asserts.  There are a few variants of the macro you can use:

    CLR_TO_PROFILER_ENTRYPOINT

This is the preferred and typically-used macro.

Other macro choices may be used **but you must comment** why the above (preferred) macro cannot be used.

    *_FOR_THREAD_*

These macros are used for ICorProfilerCallback methods that specify a ThreadID parameter whose value may not always be the _current_ ThreadID.  You must specify the ThreadID as the first parameter to these macros.  The macro will then use your ThreadID rather than GetThread(), to assert that the callback is currently allowed for that ThreadID (i.e., that we have not yet issued a ThreadDestroyed() for that ThreadID).

ICorProfilerInfo
================

This interface comprises the entrypoints used by the profiler to call into the CLR.

Synchronous / Asynchronous
--------------------------

Each Info call is classified as either synchronous or asynchronous.  Synchronous functions must be called from within a callback, whereas asynchronous functions are safe to be called at any time.

### Synchronous

The vast majority of Info calls are synchronous: They can only be called by a profiler while it is executing inside a Callback.  In other words, an ICorProfilerCallback must be on the stack for it to be legal to call a synchronous Info function.  This is tracked by a bit on the EE Thread object.  When a Callback is made, we set the bit.  When the callback returns, we reset the bit.  When a synchronous Info function is called, we test the bit—if it's not set, disallow the call.

#### Threads without an EE Thread

Because the above bit is tracked using the EE Thread object, only Info calls made on threads containing an EE Thread object have their "synchronous-ness" enforced.  Any Info call made on a non-EE Thread thread is immediately considered legal.  This is generally fine, as it's mainly the EE Thread threads that build up complex contexts that would be problematic to reenter.  Also, it's ultimately the profiler's responsibility to ensure correctness.  As described above, for performance reasons, the profiling API historically keeps its correctness checks down to a bare minimum, so as not to increase the weight.  Typically, Info calls made by a profiler on a non-EE Thread fall into these categories:

- An Info call made during a GC callback on a thread doing a server.
- An Info call made on a thread of the profiler's creation, such as a sampling thread (which therefore would have no CLR code on the stack).

#### Enter / leave hooks

If a profiler requests enter / leave hooks and uses the fast path (i.e., direct function calls from the jitted code to the profiler with no intervening profiling API code), then any call to an Info function from within its enter / leave hooks will be considered asynchronous.  Again, this is for pragmatic reasons.  If profiling API code doesn't get a chance to run (for performance), then we have no opportunity to set the EE Thread bit stating that we're executing inside a callback.  This means a profiler is restricted to calling only asynchronous-safe Info functions from within its enter / leave hook.  This is typically acceptable, as a profiler concerned enough with perf that it requires direct function calls for enter / leave will probably not be calling any Info functions from within its enter / leave hooks anyway.

The alternative is for the profiler to set a flag specifying that it wants argument or return value information, which forces an intervening profiling API C function to be called to prepare the information for the profiler's Enter / Leave hooks.  When such a flag is set, the profiling API sets the EE Thread bit from inside this C function that prepares the argument / return value information from the profiler.  This enables the profiler to call synchronous Info functions from within its Enter / Leave hook.

### Asynchronous

Asynchronous Info functions are those that are safe to be called anytime (from a callback or not).  There are relatively few asynchronous Info functions.  They are what a hijacking sampling profiler (e.g., Visual Studio profiler) might want to call from within one of its samples.  It is critical that an Info function labeled as asynchronous be able to execute from any possible call stack.  A thread could be interrupted while holding any number of locks (spin locks, thread store lock, OS heap lock, etc.), and then forced by the profiler to reenter the runtime via an asynchronous Info function.  This can easily cause deadlock or data corruption.  There are two ways an asynchronous Info function can ensure its own safety:

- Be very, very simple. Don't take locks, don't trigger a GC, don't access data that could be inconsistent, etc. OR
- If you need to be more complex than that, have sufficient checks at the top to ensure locks, data structures, etc., are in a safe state before proceeding.
    - Often, this includes asking whether the current thread is currently inside a forbid suspend thread region, and bailing with an error if it is, though this is not a sufficient check in all cases.
    - DoStackSnapshot is an example of a complex asynchronous function. It uses a combination of checks (including asking whether the current thread is currently inside a forbid suspend thread region) to determine whether to proceed or bail.

Contracts
---------

Each and every Info function must have some common gunk at the top.  Here's an example:

    CONTRACTL
    {
        // Yay!
        NOTHROW;

        // Yay!
        GC_NOTRIGGER;

        // Yay!
        MODE_ANY;

        // Yay!
        EE_THREAD_NOT_REQUIRED;

        // Yay!
        CANNOT_TAKE_LOCK;
    }
    CONTRACTL_END;
    PROFILER_TO_CLR_ENTRYPOINT_SYNC((LF_CORPROF,
                                     LL_INFO1000,
                                     "**PROF: EnumModuleFrozenObjects 0x%p.\n",
                                     moduleID));

Here are the "preferred" values for each contract type.  Note these are mostly different from the preferred values for Callbacks!  If that confuses you, reread section 2.

| Preferred | Why | Details |
| --------- | --- | ------- |
| NOTHROW | Makes it easier for profiler to call; profiler doesn't need its own try / catch. | If your callees are NOTHROW then use NOTHROW.  Otherwise, it's actually better to mark yourself as THROWS than to set up your own try / catch.  The profiler can probably do this more efficiently by sharing a try block among multiple Info calls. |
| GC\_NOTRIGGER | Safer for profiler to call from more situations | Go out of your way not to trigger.  If an Info function _might_ trigger (e.g., loading a type if it's not already loaded), ensure there's a way, if possible, for the profiler to specify _not_ to take the trigger path (e.g., fAllowLoad parameter that can be set to FALSE), and contract that conditionally. |
| MODE\_ANY | Safer for profiler to call from more situations | MODE\_COOPERATIVE is fair if your parameters or returns are ObjectIDs.  Otherwise, MODE\_ANY is strongly preferred. |
| CANNOT\_TAKE\_LOCK | Safer for profiler to call from more situations | Ensure your callees don't lock.  If they must, comment exactly what locks are taken. |
| Optional:EE\_THREAD\_NOT\_REQUIRED | Allows profiler to use this Info fcn from GC callbacks and from profiler-spun threads (e.g., sampling thread). | These contracts are not yet enforced, so it's fine to just leave it blank.  If you're pretty sure your Info function doesn't need (or call anyone who needs) a current EE Thread, you can specify EE\_THREAD\_NOT\_REQUIRED as a hint for later when the thread contracts are enforced. |

Here's an example of commented contracts in a function that's not as "yay" as the one above:

    CONTRACTL
    {
        // ModuleILHeap::CreateNew throws
        THROWS;

        // AppDomainIterator::Next calls AppDomain::Release which can destroy AppDomain, and
        // ~AppDomain triggers, according to its contract.
        GC_TRIGGERS;

        // Need cooperative mode, otherwise objectId can become invalid
        if (GetThreadNULLOk() != NULL) { MODE_COOPERATIVE;  }

        // Yay!
        EE_THREAD_NOT_REQUIRED;

        // Generics::GetExactInstantiationsFromCallInformation eventually
        // reads metadata which causes us to take a reader lock.
        CAN_TAKE_LOCK;
    }
    CONTRACTL_END;

Entrypoint macros
-----------------

After the contracts, there should be an entrypoint macro.  This takes care of logging and, in the case of a synchronous function, consulting callback state flags to enforce it's really called synchronously.  Use one of these, depending on whether the Info function is synchronous, asynchronous, or callable only from within the Initialize callback:

- PROFILER\_TO\_CLR\_ENTRYPOINT\_**SYNC** _(typical choice)_
- PROFILER\_TO\_CLR\_ENTRYPOINT\_**ASYNC**
- PROFILER\_TO\_CLR\_ENTRYPOINT\_CALLABLE\_ON\_INIT\_ONLY

As described above, asynchronous Info methods are rare and carry a higher burden.  The preferred contracts above are even "more preferred" if the method is asynchronous, and these 2 are outright required: GC\_NOTRIGGER & MODE\_ANY.  CANNOT\_TAKE\_LOCK, while even more preferred in an async than sync function, is not always possible.  See _Asynchronous_ section above for what to do.

Files You'll Modify
===================

It's pretty straightforward where to go, to add or modify methods, and code inspection is all you'll need to figure it out.  Here are the places you'll need to go.

corprof.idl
-----------

All profiling API interfaces and types are defined in [src\inc\corprof.idl](https://github.com/dotnet/runtime/blob/main/src/coreclr/inc/corprof.idl). Go here first to define your types and methods.

EEToProfInterfaceImpl.\*
-----------------------

Wrapper around the profiler's implementation of ICorProfilerCallback is located at [src\vm\EEToProfInterfaceImpl.\*](https://github.com/dotnet/runtime/tree/main/src/coreclr/vm).

ProfToEEInterfaceImpl.\*
-----------------------

Implementation of ICorProfilerInfo is located at [src\vm\ProfToEEInterfaceImpl.\*](https://github.com/dotnet/runtime/tree/main/src/coreclr/vm).
