*This blog post originally appeared on David Broman's blog on 10/12/2011*

This post is organized in chronological order, telling what your profiler should be doing at the following times in the process:

- Startup Time 
- ModuleLoadFinished Time 
- RequestReJIT Time 
- Actual ReJIT Time 
- RequestRevert Time 

 

## Startup Time

The first thing your profiler will do is get itself loaded on startup of a managed application—the old environment variable way, not the new attach way.  I’m sure you’ve already read up on the [limitations](ReJIT - Limitations.md)!

Inside your profiler’s Initialize() method, it will of course call SetEventMask().  In that call, your profiler must include ( **COR\_PRF\_ENABLE\_REJIT | COR\_PRF\_DISABLE\_ALL\_NGEN\_IMAGES** ) in the bitmask.  COR\_PRF\_ENABLE\_REJIT is required to use any of the ReJIT APIs later on (they’ll fail immediately otherwise).  COR\_PRF\_DISABLE\_ALL\_NGEN\_IMAGES causes the CLR’s assembly loader to ignore all NGENd images (even NGEN /Profile images), and thus all code will be JITted from scratch, and all classes loaded from scratch.  If you try to be tricky and specify only COR\_PRF\_ENABLE\_REJIT (without COR\_PRF\_DISABLE\_ALL\_NGEN\_IMAGES), then SetEventMask will fail.  Conversely, though, you’re perfectly welcome to specify COR\_PRF\_DISABLE\_ALL\_NGEN\_IMAGES without COR\_PRF\_ENABLE\_REJIT if you want.

At this time you will likely want to set other flags that control optimizations, particularly **inlining** (COR\_PRF\_DISABLE\_OPTIMIZATIONS, COR\_PRF\_DISABLE\_INLINING), or at least subscribe to the inlining callbacks (COR\_PRF\_MONITOR\_JIT\_COMPILATION).

Typically, your profiler will also create a new thread at this point, call it your “ **ReJIT Thread** ”.  The expected use-case of ReJIT is to perform instrumentation “on demand”, triggered by some user action (like fiddling with dials in your profiler’s out-of-process GUI).  As such, you’ll need an unmanaged thread of your own creation to receive and act on these requests from out-of-process.  Perhaps you already have such a thread to service other kinds of requests.  It’s perfectly acceptable for such a thread to now also act as your ReJIT Thread.

## ModuleLoadFinished Time

### 

### 

###  

### Metadata Changes

As each module loads, you will likely need to add metadata so that your future ReJITs will have the tokens they need.  What you do here heavily depends on the kind of instrumentation you want to do.  I’m assuming you’re doing instrumentation that adds some calls from the user code into brand new profiler helper methods you will add somewhere.  If you plan to instrument mscorlib, you will likely want to add those profiler helper methods into mscorlib (remember, mscorlib is not allowed to contain an AssemblyRef that points to any other assembly!).  Otherwise, perhaps you plan to ship a managed helper assembly that will sit on your user’s disk, and all your profiler helper methods will reside in this on-disk managed helper assembly.

So…

IF the module loading is mscorlib AND you plan to **add your profiler helper methods** into mscorlib, THEN use the metadata APIs now to add those methods.

IF the module loading contains methods that you might possibly ever want to instrument, THEN use the metadata APIs to **add any AssemblyRefs, TypeRefs, MemberRefs, etc.** , which point to your profiler helper methods, that you might possibly need later when you potentially instrument methods from this loading module.  The guiding principle here is that metadata changes may be done at ModuleLoadFinished time, and not later.  So you need to assume you might possibly want to ReJIT methods in the loading module _eventually_, and proactively add to the loading module whatever metadata you will eventually need (should you actually perform the ReJIT later), and add that metadata _now_, just in case.

### Re-Request Prior ReJITs

This won’t make much sense until you’ve read the next section, but I’m placing it here to keep it in chronological order.  If you’ve made a prior call to RequestReJIT for an unshared (non-domain-neutral) ModuleID, AND if you want that request to apply to the mdMethodDef that appears in all other unshared copies of the module, AND if you’re inside ModuleLoadFinished for the load of a new ModuleID that is just such a new unshared copy of the module, THEN you’ll want to explicitly call RequestReJIT on this newly-loaded ModuleID with that mdMethodDef.  Note that this is optional—if you want to treat AppDomains differently and want, say, only one unshared copy of the function to be ReJITted, then you’re perfectly welcome to cause that behavior and not to call RequestReJIT on any new ModuleIDs relating to the module.  Come back and re-read those last two sentences after you’ve read the next section.

## RequestReJIT Time

Now imagine your user has turned some dial on your out-of-process GUI, to request that some functions get instrumented (or re-instrumented (or re-re-instrumented (or …))).  This results in a signal sent to your in-process profiler component.  Your ReJIT Thread now knows it must call **RequestReJIT**.  You can call this API once in bulk for a list of functions to ReJIT.  Note that functions are expressed in terms of ModuleID + mdMethodDef metadata tokens.  A few things to note about this:

- You request that all instantiations of a generic function (or function on a generic class) get ReJITted with a single ModuleID + mdMethodDef pair.  You cannot request a specific instantiation be ReJITted, or provide instantiation-specific IL.  This is nothing new, as classic first-JIT-instrumentation should never be customized per instantiation either.  But the ReJIT API is designed with this restriction in mind, as you’ll see later on. 
- ModuleID is specific to one AppDomain for unshared modules, or the SharedDomain for shared modules.  Thus: 
  - If ModuleID is shared, then your request will simultaneously apply to all domains using the shared copy of this module (and thus function) 
  - If ModuleID is unshared, then your request will apply only to the single AppDomain using this module (and function) 
  - Therefore, if you want this ReJIT request to apply to _all unshared copies_ of this function: 
    - You’ll need to include all such ModuleIDs in this request. 
    - And… any _future_ unshared loads of this module will result in new ModuleIDs.  So as those loads happen, you’ll need to make further calls to RequestReJIT with the new ModuleIDs to ensure those copies get ReJITted as well. 
    - This is optional, and only need be done if you truly want this ReJIT request to apply to all unshared copies of the function.  You’re perfectly welcome to ReJIT only those unshared copies you want (and / or the shared copy). 
    - Now you can re-read the “Re-Request Prior ReJITs” section above.  :-) 

## 

###  

### More on AppDomains

This whole shared / multiple unshared business can get confusing.  So to bring it home, consider your user.  If your user expresses instrumentation intent at the level of a class/method name, then you pretty much want to ReJIT every copy of that function (all unshared copies plus the shared copy).  But if your user expresses instrumentation intent at the level of a class/method name _plus AppDomain_ (think one single AppPool inside ASP.NET), then you’d only want to ReJIT the copy of the function that resides in the single ModuleID associated with that AppDomain.

The SharedDomain can make that last alternative tricky, though.  Because if the ModuleID ends up belonging to the SharedDomain, and you ReJIT a method in that ModuleID, then all AppDomains that share that module will see your instrumentation (whether you want them to or not).  This is due to the very nature of SharedDomain / domain-neutrality.  There’s only one shared copy of this function to instrument, so if two domains share the function, they both see it, either with or without instrumentation.  It doesn’t make sense to instrument the function from the point of view of only one of those two domains.

### Pre-ReJIT

Obviously, the main coolness of RequestReJIT is that you can call it with a function that has already been JITted.  But one of the niceties of RequestReJIT is that you don’t actually have to wait until a function is first JITted to use it.  You can request a ReJIT on a function that has never been JITted before (I call this “Pre-ReJIT”).  Indeed, with generics, there’s no way to know if all the instantiations that will ever be used in an AppDomain have been JITted or not.  There may always be some important instantiation that has not been JITted yet.  RequestReJIT takes all this into account as follows:

If a function (or generic instantiation) has already been JITted, it is marked for ReJIT next time it is called.

If a function (or generic instantiation) has not yet been JITted, then it is marked internally for “Pre-ReJIT”.  This means that once it is called, its original (non-instrumented) IL gets JIT-compiled as usual.  Immediately after, it is then ReJITted.  In this way, a Pre-ReJIT request works exactly like a ReJIT request.  Original IL is compiled first, and then instrumented IL is compiled later.  This ensures we can easily “revert” back to the original code at a later time using the same revert mechanism.  (See below.)

## Actual ReJIT Time

You may have noticed that you have read a whole lot of words so far, but we haven’t yet provided the instrumented IL to the CLR.  This is because the function hasn’t ReJITted yet.  You’ve only _requested_ that it be ReJITted.  But the actual ReJITting happens the next time the function is called.   Until then, any threads already executing inside functions you requested to be ReJITted _stay_ in those functions, and don’t see the instrumented code until they return and call the functions again.  Once a function is finally called for the first time after its RequestReJIT, you get some callbacks.

IF this is the first generic instantiation to ReJIT, for a given RequestReJIT call (or this is not a generic at all), THEN:

- CLR calls **GetReJITParameters**
  - This callback passes an ICorProfilerFunctionControl to your profiler.  Inside your implementation of GetReJITParameters (and no later!) you may call into ICorProfilerFunctionControl to provide the instrumented IL and codegen flags that the CLR should use during the ReJIT 
  - Therefore it is here where you may: 
    - Call GetILFunctionBody 
    - Add any new LocalVarSigTokens to the function’s module’s metadata.  (You may not do any other metadata modifications here, though!) 
    - Rewrite the IL to your specifications, passing it to ICorProfilerFunctionControl::SetILFunctionBody. 
  - You may NOT call ICorProfilerInfo::SetILFunctionBody for a ReJIT!  This API still exists if you want to do classic first-JIT IL rewriting only. 
  - Note that GetReJITParameters expresses the function getting compiled in terms of the ModuleID + mdMethodDef pair you previously specified to RequestReJIT, and _not_ in terms of a FunctionID.  As mentioned before, you may not provide instantiation-specific IL! 

And then, for all ReJITs (regardless of whether they are for the first generic instantiation or not):

- CLR calls **ReJITCompilationStarted** 
- CLR calls **ReJITCompilationFinished** 

These callbacks express the function getting compiled in terms of FunctionID + ReJITID.  (ReJITID is simply a disambiguating value so that each ReJITted version of a function instantiation can be uniquely identified via FunctionID + ReJITID.)  Your profiler doesn’t need to do anything in the above callbacks if it doesn’t want to.  They just notify you that the ReJIT is occurring, and get called for each generic instantiation (or non-generic) that gets ReJITted.

And of course, for any calls to these functions after they have been ReJITted, there are no further ReJIT compilations or callbacks to your profiler.  This ReJITted version is now the current and only version for all new calls to the function.

### Versions

Your profiler is welcome to call RequestReJIT again on these functions, and the cycle starts again.  The next time a call comes in, they’ll get ReJITted again, and you’ll provide instrumented IL at that time, as usual.  At any given time, only the most recently ReJITted version of a function is active and in use for new calls.  But any prior calls still inside previously ReJITted (or original) versions of the function stay in that version until they return.

## RequestRevert Time

Eventually your user may turn the dial back down, and request that the original, un-instrumented, version of the function be reinstated.  When this happens, your profiler receives this signal from out-of-proc using your nifty cross-proc communication channel, and your ReJIT Thread calls **RequestRevert**.

At this time, the CLR sets the original version of the function that it JITted the first time as being the _current_ version for all future calls.  Any prior calls still executing in various ReJITted versions of the function remain where they’re at until they return.  All new calls go into the version originally JITted (from the original IL).

Note that RequestRevert allows you to revert back to the original JITted IL, and not back to some previous ReJITted version of the IL.  If you want to revert back to a previous ReJITted version of the IL, you’ll need to do so manually, by using RequestReJIT instead, and providing that IL explicitly to the CLR.

## Errors

If there are any errors with performing the ReJIT, you will be notified by the dedicated callback ICorProfilerCallback4::ReJITError().  Errors can happen at a couple times:

- RequestReJIT Time: These are fundamental errors with the request itself.  This can include bad parameter values, requesting to ReJIT dynamic (Ref.Emit) code, out of memory, etc.  If errors occur here, you’ll get a callback to your implementation of ReJITError(), sandwiched inside your call to RequestReJIT on your ReJIT Thread. 
- Actual ReJIT Time: These are errors we don’t encounter until actually trying to ReJIT the function itself.  When these later errors occur, your implementation of ReJITError() is called on whatever CLR thread encountered the error. 

You’ll note that ReJITError can provide you not only the ModuleID + mdMethodDef pair that caused the error, but optionally a FunctionID as well.  Depending on the nature of the error occurred, the FunctionID may be available, so that your profiler may know the exact generic instantiation involved with the error.  If FunctionID is null, then the error was fundamental to the generic function itself (and thus occurred for all instantiations).

 

Ok, that about covers it on how your profiler is expected to use ReJIT.  As you can see, there are several different tasks your profiler needs to do at different times to get everything right.  But I trust you, you’re smart.

