*This blog post originally appeared on David Broman's blog on 12/29/2011*


As mentioned in [this post](Debugging - SOS and IDs.md), ObjectIDs are really pointers to managed objects on the GC heap.  And as you know, objects get collected or move around on the heap during GCs.  So how do you safely work with ObjectIDs?

The overall guidance is that if you plan to dereference an ObjectID or pass it to an ICorProfilerInfo(2,3,4) method, then you must do so either:

1. From inside a GC, from a thread doing the GC (e.g., in response to one of the GC callbacks, in which case you're guaranteed that the GC is blocked by this call), OR
2. From a callback that gave you the ObjectID (in which case you're guaranteed that the GC is blocked by the callback that gave you the ObjectID)

Of course, taking an ObjectID that you were given and caching it away somewhere is a big no-no, unless you take pains to update all the ObjectIDs in your cache on every GC, by using the SurvivingReferences/MovedReferences callbacks.  And even with a well-updated cache such as this, the CLR will still require that you pass ObjectIDs to Info methods only in the above two circumstances, or else you will receive an error HRESULT.  This extra checking was added in .NET 4.0.

The reason for this is that some code paths in the CLR assume that their thread is already blocking the GC (to ensure referenced objects stay put), but there was no enforcement in place to ensure this.  So we added this enforcement in .NET 4.0.  Without checks like this, it could be possible to cause nondeterministic GC heap corruptions or to reference the wrong memory.  For example, calling GetObjectSize on a thread that you create (i.e., not a manage thread) does not intrinsically block the GC, and thus is considered unsafe.

