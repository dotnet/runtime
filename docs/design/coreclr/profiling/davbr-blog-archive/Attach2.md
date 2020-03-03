*This blog post originally appeared on David Broman's blog on 1/18/2010*

***[Update 5/30/19]: The archived content below refers to an Attach mechanism that only worked on desktop, not on .Net Core. Please see [Profiler Attach on CoreCLR](../Profiler Attach on Coreclr.md) for profiler attach on CoreCLR***

In a previous [post](Attach.md), I outlined to all you profiler writers how to modify your profiler so it can attach to running processes, and what sorts of limitations your profiler will have when it attaches.  In this post, I answer the question, “My profiler is attached.  What should it do next?”

# Catch Up

A profiler that loads on startup of an application has the option to know the entire history of that application.  By requesting the appropriate callback events, the profiler can know all the classes and modules that have loaded, functions that have JITted, objects that have been allocated, etc.  However, a profiler that loads by attaching to an already-running application is a bit like Dorothy who lands in the middle of Oz and has no idea what’s going on.  She doesn’t have the luxury of arriving at the beginning of time, and watching everyone from the moment of their birth.  She runs into people after they’re fully grown, and is expected to deal gracefully—often by making friends with them.  It would not be socially acceptable for Dorothy to encounter an access violation upon meeting someone new.

[NoBirthAnnouncement](media/NoBirthAnnouncement.JPG)

Drawing by Magdalena Hermawan
 

There are two fundamental ways your profiler can catch up on the current state of an application:

- Lazy catch-up—as the profiler encounters new IDs, the profiler queries information about those IDs as it needs them, rather than assuming it has a full cache that’s always built up as the IDs are first created.  This is analogous to Dorothy meeting a new grown-up, and gracefully accepting the fact that that person exists. 
- Enumeration—for certain kinds of IDs, the profiler can (at attach time) request a complete list of the currently active IDs and query information about them at that time.  Sort of like Dorothy first going to the Oz City Hall and looking up the birth records for everyone. 

Lazy catch-up is fairly self-explanatory.  For example, if your sampling profiler encounters an IP in a FunctionID you’ve never seen before, just look up whatever info you need about that FunctionID the first time you encounter it, rather than assuming you’d already built up a cache when the function was first JITted.  And if you discover that FunctionID resides in a module you’ve never seen before, then just look up whatever info you need about that ModuleID at that point, rather than assuming you already have a complete cache of all modules.  Many of you are already doing something like this today if you support sampling against regular NGENd images (since you don’t get JIT notifications of those functions anyway).

Enumeration, on the other hand, has some caveats and is worthwhile to describe in more detail.

# Enumeration via Enum\* APIs

Some kinds of IDs have new enumerator methods as part of the profiling API.  In particular:

- ICorProfilerInfo3::EnumModules 
- ICorProfilerInfo3::EnumJITedFunctions 

Your profiler calls these methods, and they return a standard enumerator you use to iterate through all of the currently-loaded IDs of that type.  It’s worth noting that EnumJITedFunctions only enumerates FunctionIDs for which you would receive JITCompilationStarted/Finished events, and will not include FunctionIDs from NGENd modules.

The primary caveat with using these enumerators is that you’re iterating through a snapshot of the IDs while the process is active and running.  (Imagine Dorothy looking through a copy of birth records while babies are still getting born in Oz, who weren’t yet in the copy of records Dorothy is reading.)  This means there are races your profiler needs to be resilient to.

## Race #1: When to enumerate?  ProfilerAttachComplete()

As you may recall, once your profiler is attached to the process, the CLR calls InitializeForAttach() on your profiler.  After your profiler returns from InitializeForAttach(), the CLR turns on callbacks into your profiler.  So if your profiler requested COR\_PRF\_MONITOR\_MODULE\_LOADS (by calling SetEventMask() at some point inside your implementation of InitializeForAttach), then as modules start loading and unloading after InitializeForAttach() returns, your profiler will receive the corresponding events.  The thing is, “after InitializeForAttach() returns” is a vague phrase.  And modules can load or unload at totally arbitrary times with respect to the timing of when your profiler attaches and calls EnumModules().  The thing to avoid here is a hole: a ModuleID your profiler does not find in the enumeration, and for which your profiler receives no ModuleLoad event.  This can happen if your profiler calls the enumeration API too soon (i.e., before CLR has enabled event callbacks for your profiler).

Bad timeline (loading; enumerating too soon):

1. Profiler attaches 
2. Profiler calls EnumModules 
3. Module starts to load 
4. ModuleID is now enumerable 
5. ModuleLoadFinished event would fire here if events were enabled (but they’re not yet!) 
6. CLR enables events 

The problem is that the profiler calls EnumModules too early.  If your profiler only calls EnumModules after CLR enables events, then you’re assured of either seeing a ModuleID via EnumModules or via a ModuleLoad event.  In the above scenario, your profiler might as well have never done enumeration at all, since it will still not be notified of the ModuleID before it comes across that ModuleID in action later on.  It gets even worse for modules that unload:

Bad timeline (unloading; enumerating too soon):

1. Module loads 
2. ModuleID is now enumerable 
3. Profiler attaches 
4. Profiler calls EnumModules (includes the ModuleID) 
5. Module starts to unload 
6. ModuleUnloadStarted event would fire here if events were enabled (but they’re not yet!) 
7. CLR enables events 

In the above case, the profiler discovers a ModuleID via EnumModules, but has no idea that the module is now in the process of unloading.  So the profiler might query information about the stale ModuleID, potentially causing an AV.  Again, this is caused because the profiler called the enumeration API too soon (i.e., before the CLR enabled event callbacks).

The solution is for the profiler to call enumeration APIs only after events have been enabled.  Since events are enabled at some point “after InitializeForAttach() returns”, it was necessary for the CLR to provide a new API to notify the profiler that event callbacks have actually been enabled: ICorProfilerCallback3::ProfilerAttachComplete().  **The best place for your profiler to call the enumeration APIs is inside its implementation of ProfilerAttachComplete.**   Since events are enabled _just before_ the CLR calls ProfilerAttachComplete, your profiler is assured that events are enabled by the time it calls the enumeration API (from inside ProfilerAttachComplete).  This eliminates any potential holes in catch-up information your profiler queries.

## Race #2: Duplicates

When your profiler calls the Enum\* methods, the CLR creates a snapshot of all “enumerable” IDs of the specified type, and gives your profiler an enumerator over those.  In the CLR we had a choice.  We could either consider an ID to be “enumerable” before or after the corresponding load finished event (or JITCompilationFinished event) would normally be issued.  Consider for a moment what we _didn’t_ do.  We didn’t consider IDs to be enumerable after the event.  If so, that would have led to holes.  A profiler could have attached and grabbed an enumeration in the middle and never been notified about the ID.

Bad timeline (loading):

1. Module starts to load 
2. ModuleLoadFinished event would fire here if events were enabled (but they’re not yet—no profiler is attached!) 
3. Profiler attaches 
4. CLR enables events, calls ProfilerAttachComplete() 
5. Profiler calls EnumModules 
6. ModuleID is now enumerable 

Because 2 comes before 6, it’s possible for a profiler to attach and grab an enumeration in the middle, and thus never hear about a ModuleID (even though the profiler avoided Race #1 from the previous section).  Again, an even worse problem occurs for module unloading.  Suppose the CLR were to change an ID’s enumerable status to false after sending the unload event.  That would also lead to holes:

Bad timeline (unloading):

1. Module loads, event would fire if profiler were attached (but it’s not), then ModuleID becomes enumerable 
2. Module starts to unload 
3. ModuleUnloadStarted event would fire here if events were enabled (but they’re not yet—no profiler is attached!) 
4. Profiler attaches 
5. CLR enables events, calls ProfilerAttachComplete() 
6. Profiler calls EnumModules (ModuleID is still enumerable, so profiler discovers ModuleID at this point) 
7. ModuleID is no longer enumerable 

Because 3 comes before 7, a profiler could attach in the middle, grab an enumeration, discover the ModuleID via the enumeration, and have no idea that module was in the process of unloading.  If the profiler were to use that ModuleID later on, an AV could result.  The above led to the following golden rule:

| **Golden rule: An ID’s enumerability status shall change _before_ the corresponding load/unload event is fired.** |

In other words, an ID becomes enumerable _before_ the LoadFinished (or JITCompilationFinished) event.  And an ID ceases to be enumerable _before_ the UnloadStarted event.  Or you can think of it as, “The event is always last”.  This eliminates any potential holes.  So to be even more explicit, here’s the enumerability vs. event ordering:

1. ID available in enumerations snapped now 
2. LoadFinished 
3. ID no longer in enumerations snapped now 
4. UnloadStarted 

If an ID is present, the profiler will discover the ID via the enumerator or a LoadFinished event (or both).  If an ID is not present, the profiler will either not see the ID via the enumerator or will see an UnloadStarted event (or both).  In all cases, the event is more recent, and so the profiler should always trust an event over an enumeration that was generated prior.  (More on that last point later.)

The astute reader will notice that what we’ve done here is trade one race for another.  We’ve eliminated holes, but the cost is that the profiler must deal with duplicates.  For example:

Good timeline (loading with duplicate):

1. Module starts to load 
2. ModuleID is now enumerable 
3. Profiler attaches 
4. CLR enables events, calls ProfilerAttachComplete() 
5. Profiler calls EnumModules 
6. Profiler receives ModuleLoadFinished 

At first it might seem a little strange.  The enumerator contains the ModuleID, so the profiler sees that the module is loaded.  But then the profiler receives a ModuleLoadFinished event, which might seem odd, since the enumerator implied the module was already loaded.  This is what I mean by “duplicate”—the profiler is notified of a ModuleID twice (once via the enumeration, and once via the event).  The profiler will need to be resilient to this.  Although it’s a bit awkward, it’s better than the alternative of a hole, since the profiler would have no way to know the hole occurred.  Unloading has a similar situation:

Good timeline (unloading with duplicate):

1. Module loads, event would have fired if profiler were attached (but it’s not), ModuleID becomes enumerable 
2. Module starts to unload 
3. ModuleID is no longer enumerable 
4. Profiler attaches 
5. CLR enables events, calls ProfilerAttachComplete() 
6. Profiler calls EnumModules 
7. Profiler receives ModuleUnloadStarted event 

In step 6, the profiler does not see the unloading ModuleID (since it’s no longer enumerable).  But in step 7 the profiler is notified that the ModuleID is unloading.  Perhaps it’s a bit awkward that the profiler would be told that a seemingly nonexistent ModuleID is unloading.  But again, this is better than the alternative, where a profiler finds an unloading ID in the enumeration, and is never told that the ModuleID got unloaded.  One more case that’s worthwhile to bring out occurs when we move the profiler attach a bit earlier in the sequence.

Good timeline (unloading without duplicate):

1. Module loads, event would fire if profiler were attached, ModuleID becomes enumerable 
2. Module starts to unload 
3. Profiler attaches 
4. CLR enables events, calls ProfilerAttachComplete() 
5. Profiler calls EnumModules (ModuleID is still present in the enumeration) 
6. ModuleID is no longer enumerable 
7. Profiler receives ModuleUnloadStarted event 

Here the profiler discovers the ModuleID exists in step 5 (as the ModuleID is still enumerable at that point), but the profiler almost immediately after discovers that the module is unloading in step 7.  As stated above, events are more recent, and should always take precedence over enumerations that were generated prior.  This could get a bit tricky, though, as the profiler generates an enumeration before it iterates over the enumeration.  In the above sequence, the enumeration is generated in step 5.  However, the profiler could be iterating though the generated enumeration for quite some time, and might not come across the unloading ModuleID until after step 7 (multiple threads means fun for everyone!).  For this reason, it’s important for the profiler to give precedence to events that occur after the enumeration was _generated_, even though iteration over that enumeration might occur later.

# Catching Up on the State of GC Heap

If you’re writing a memory profiler, you likely have code that responds to the various GC events.  In order for your memory profiler to attach to a running process, it needs to deal gracefully with the fact that it does not yet have a cache of objects on the heap.  One straightforward way for the profiler to deal with this is to force a GC at attach time.

## GC Already in Progress

Remember that your profiler attaches at a completely arbitrary point during process execution, possibly while a GC is already in progress.  This means that, once the profiler has enabled callback events, the profiler may start seeing GC callbacks (e.g., MovedReferences, ObjectReferences) from the middle of that GC, without seeing a GarbageCollectionStarted() first.  Your profiler should be resilient to this situation, preferably by ignoring GC callbacks until the first full GC and / or profiler-induced GC begins.  The profiler can do this by ignoring all GC callbacks until it sees the first GarbageCollectionStarted() callback OR by ignoring all GC callbacks until it sees the first GarbageCollectionStarted() callback after calling ForceGC().

## Inducing Your First GC

It may be beneficial to program your profiler such that, upon attaching to the process, the profiler induces a first full GC automatically—call this the “catch-up” GC.  This will allow your profiler to use events like RootReferences2 and ObjectReferences during that initial “catch-up” GC, in order to build up its cache of objects from scratch.  After that initial catch-up GC, your profiler should then be able to deal with successive GCs the usual way.

It’s worth reiterating a limitation I stated in the first attach post (linked above): the ObjectAllocated() callback is unavailable to profilers that attach to running processes.  Therefore, any logic your profiler has that assumes it gets all the ObjectAllocated() callbacks will need to be addressed.  Any objects newly allocated since the last GC may still be unknown to your profiler until it comes across their references via GC callbacks during the next GC (unless your profiler comes across those objects in other ways—example: as parameters to methods you hook with the Enter/Leave/Tailcall probes).

 

OK, that about covers the first steps your profiler should take once it attaches to a running process.  It will either need to use lazy catch-up or the catch-up enumerations (or, quite likely, a combination of both).  When using the enumerations, be careful to avoid holes (by calling the enumeration methods from inside ProfilerAttachComplete()), and be resilient to receiving information duplicated across the enumeration and the load / unload events.  For memory profilers, be wary of GCs already in progress at the time your profiler attaches, and consider inducing your own GC at attach-time to build your initial cache of GC objects.

