*This blog post originally appeared on David Broman's blog on 12/23/2008*


_What follows is a long-lost blog entry that_ [_Jonathan Keljo_](http://blogs.msdn.com/jkeljo) _had been working on.  I brushed off some of the dust and am posting it here for your enjoyment.  Thank you, Jonathan!_

In CLR 2.0 we added a new HRESULT, CORPROF\_E\_UNSUPPORTED\_CALL\_SEQUENCE.  This HRESULT is returned from ICorProfilerInfo methods when called in an "unsupported way".  This "unsupported way" is primarily an issue with those nasty beasts, hijacking profilers (though read on for cases where non-hijacking profilers can see this HRESULT, too).  Hijacking profilers are those profilers that forcibly reset a thread's register context at completely arbitrary times to enter profiler code, and then usually to re-enter the CLR via ICorProfilerInfo.  Why is that so bad?  Well, for the sake of performance, lots of the IDs the profiling API gives out are just pointers to relevant data structures within the CLR. So lots of ICorProfilerInfo calls just rip information out of those data structures and pass them back. Of course, the CLR might be changing things in those structures as it runs, maybe (or maybe not) taking locks to do so.  Imagine the CLR was already holding (or attempting to acquire) such locks at the time the profiler hijacked the thread.  Now, the thread re-enters the CLR, trying to take more locks or inspect structures that were in the process of being modified, and are thus in an inconsistent state.  Deadlocks and AVs are easy to come by in such situations.

In general, if you're a non-hijacking profiler sitting inside an ICorProfilerCallback method and you're calling into ICorProfilerInfo, you're fine. For example, you get a ClassLoadFinished and you start asking for information about the class. You might be told that information isn't available yet (CORPROF\_E\_DATAINCOMPLETE) but the program won't deadlock or AV.  This class of calls into ICorProfilerInfo are called "synchronous", because they are made from within an ICorProfilerCallback method.

On the other hand, if you're hijacking or otherwise calling ICorProfilerInfo functions on a managed thread but **not** from within an ICorProfilerCallback method, that is considered an "asynchronous" call.  In v1.x you never knew what would happen in an asynchronous call. It might deadlock, it might crash, it might give a bogus answer, or it might give the right answer.

In 2.0 we've added some simple checks to help you avoid this problem. If you call an unsafe ICorProfilerInfo function asynchronously, instead of crossing its fingers and trying, it will fail with CORPROF\_E\_UNSUPPORTED\_CALL\_SEQUENCE.  The general rule of thumb is, nothing is safe to call asynchronously.  But here are the exceptions that are safe, and that we specifically allow to be called asynchronously:

- GetEventMask/SetEventMask
- GetCurrentThreadID
- GetThreadContext
- GetThreadAppDomain
- GetFunctionFromIP
- GetFunctionInfo/GetFunctionInfo2
- GetCodeInfo/GetCodeInfo2
- GetModuleInfo
- GetClassIDInfo/GetClassIDInfo2
- IsArrayClass
- SetFunctionIDMapper
- DoStackSnapshot

There are also a few things to keep in mind:

1. ICorProfilerInfo calls made from within the fast-path Enter/Leave callbacks are considered asynchronous.  (Though ICorProfilerInfo calls made from within the _slow_-path Enter/Leave callbacks are considered synchronous.)  See the blog entries [here](ELT - The Basics.md) and [here](http://blogs.msdn.com/jkeljo/archive/2005/08/11/450506.aspx) for more info on fast / slow path.
2. ICorProfilerInfo calls made from within instrumented code (i.e., IL you've rewritten to call into your profiler and then into ICorProfilerInfo) are considered asynchronous.
3. Calls made inside your FunctionIDMapper hook are considered to be synchronous.
4. Calls made on threads created by your profiler, are always considered to be synchronous.  (This is because there's no danger of conflicts resulting from interrupting and then re-entering the CLR on that thread, since a profiler-created thread was not in the CLR to begin with.)
5. Calls made inside a StackSnapshotCallback are considered to be synchronous iff the call to DoStackSnapshot was synchronous.

