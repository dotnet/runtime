*This blog post originally appeared on David Broman's blog on 2/3/2010*


I described how profilers may attach to already-running processes in other posts ([#1](Attach.md) and [#2](Attach2.md)).  In this post I’m writing about how profilers that are already loaded may detach from a running process before that process exits.  Like Profiler Attach, this is a new feature available starting with CLR V4.

The Detach feature allows a profiler that the user is finished with to be unloaded.  That means the application may return to its usual behavior and performance characteristics, without a profiler loaded and doing stuff.  Also, since only one profiler may be loaded at a time, detaching a profiler makes room for a different (or the same) profiler to be loaded later on when the user wishes to do more diagnostics.

## Limitations

Not every V4 profiler is allowed to detach from a running process.  The general rule is that a profiler which has caused an irreversible impact in the process it’s profiling should _not_ attempt to detach.  The CLR catches the following cases:

- Profiler set immutable flags (COR\_PRF\_MONITOR\_IMMUTABLE) via SetEventMask. 
- Profiler performed IL rewriting via SetILFunctionBody 
- Profiler used the Enter/Leave/Tailcall methods to add callouts to its probes 

If the profiler attempts to detach after doing any of the above, the CLR will disallow the attempt (see below for details).

That said, there are still other irreversible things the profiler might do to a process (which would also make detaching a bad idea).  Imagine a profiler that allocates memory without cleaning up after itself, creates threads without waiting for them to exit, uses metadata APIs to modify aspects of the running managed code, etc.  Profiler writers need to use good judgment when considering whether to allow their profilers to detach from running processes.  You don’t want to give your customers the experience of noticing the app they profile always behaves weirdly after detaching your profiler.  So do not use the detach feature unless you’ve thought through the ramifications and can ensure the profiler does not leave the application in a noticeably different state.

By the way, you may notice I said nothing about a profiler needing to load via attach in order for it to be able to use the detach feature.  In fact, any profiler that loads on startup of the application (i.e., via environment variables and not via the AttachProfiler API) is perfectly welcome to use the detach feature—so long as it does not leave an impact on the process as per above.

## How Detaching Works

There’s one, deceptively simple-looking method the profiler calls to detach itself from the running process.  However, detaching is a big responsibility, and profiler writers need to give thoughtful consideration to doing it properly.  The CLR does its part to ensure it doesn’t accidentally call into the profiler via Profiling API methods after the CLR unloads the profiler DLL.  However, if the profiler has set into motion extra threads, Windows callbacks, timer interrupts, etc., then the profiler must “undo” all of these things before it attempts to detach from the running process.  Basically, any way for control to re-enter the profiler DLL must be disabled before detaching, or else your users will experience crashes after trying to detach your profiler.

So, the sequence works like this:

1. The profiler **deactivates all the ways control could enter the profiler** (aside from the CLR Profiling API itself).  This means removing any Windows callbacks, timer interrupts, hijacking, disabling any other components that may try to call into the profiler DLL, etc.  The profiler must also wait for all threads that it has created (e.g., a sampling thread, inter-process communication threads, a ForceGC thread, etc.) to exit, except for the one thread the profiler will use to call RequestProfilerDetach().  Any threads created by the CLR, of course, should not be tampered with. 
  - Your profiler must block here until all those ways control can enter your profiler DLL have truly been deactivated (e.g., just setting a flag to disable sampling may not be enough if your sampling thread is currently performing a sample already in progress).  You must coordinate with all components of your profiler so that your profiler DLL knows that everything is verifiably deactivated, and all profiler-created threads have exited (except for the one thread the profiler will use to call RequestProfilerDetach()). 
2. If the profiler will use a thread of its own creation to call RequestProfilerDetach() (which is the typical way this API will be called), that thread must own a reference onto the profiler’s DLL, via its own **LoadLibrary()** call that it makes on the profiler DLL.  This can either be done when the thread starts up, or now, or sometime in between.  But that reference must be added at some point before calling RequestProfilerDetach(). 
3. Profiler calls ICorProfilerInfo3:: **RequestProfilerDetach** (). 
  - (A) This causes the CLR to (synchronously) set internal state to avoid making any further calls into the profiler via the ICorProfilerCallback\* interfaces, and to refuse any calls from the profiler into ICorProfilerInfo\* interfaces (such calls will now fail early with CORPROF\_E\_PROFILER\_DETACHING). 
  - (B) The CLR also (asynchronously) begins a period safety check on another thread to determine when all pre-existing calls into the profiler via the ICorProfilerCallback\* interfaces have returned. 
  - Note: It is expected that your profiler will not make any more “unsolicited” calls back into the CLR via any interfaces (ICorProfilerInfo\*, hosting, metahost, metadata, etc.).  By “unsolicited”, I’m referring to calls that didn’t originate from the CLR via ICorProfilerCallback\*.  In other words, it’s ok for the profiler to continue to do its usual stuff in its implementation of ICorProfilerCallback methods (which may include calling into the CLR via ICorProfilerInfo\*), as the CLR will wait for those outer ICorProfilerCallback methods to return as per 3B.  But the profiler must not make any other calls into the CLR (i.e., that are not sandwiched inside an ICorProfilerCallback call).  You should already have deactivated any component of your profiler that would make such unsolicited calls in step 1. 
4. Assuming the above RequestProfilerDetach call was made on a profiler-created thread, that thread must now call [**FreeLibraryAndExitThread**](http://msdn.microsoft.com/en-us/library/ms683153(VS.85).aspx)**()**.  (Note: that’s a specialized Windows API that combines FreeLibrary() and ExitThread() in such a way that races can be avoided—do not call FreeLibrary() and ExitThread() separately.) 
5. On another thread, the CLR continues its **period safety checks** from 3B above.  Eventually the CLR determines that there are no more ICorProfilerCallback\* interface calls currently executing, and it is therefore safe to unload the profiler. 
6. The CLR calls ICorProfilerCallback3:: **ProfilerDetachSucceeded**.  The profiler can use this signal to know that it’s about to be unloaded.  It’s expected that the profiler will do very little in this callback—probably just notifying the user that the profiler is about to be unloaded.  Any cleanup the profiler needs to do should already have been done during step 1. 
7. CLR makes the necessary number of **Release** () calls on ICorProfilerCallback3.  The reference count should go down to 0 at this point, and the profiler may deallocate any memory it had previously allocated to support its callback implementation. 
8. CLR calls **FreeLibrary** () on the profiler DLL.  This should be the last reference to the profiler’s DLL, and your DLL will now be unloaded. 
  - Note: in some cases, it’s theoretically possible that step 4 doesn’t happen until _after_ this step, in which case the last reference to the profiler’s DLL will actually be released by your profiler’s thread that called RequestProfilerDetach and then FreeLibraryAndExitThread.  That’s because steps 1-4 happen on your profiler’s thread, and steps 5-8 happen on a dedicated CLR thread (for detaching profilers) sometime after step 3 is completed.  So there’s a race between step 4 and all of steps 5-8.  There’s no harm in this, so long as you’re playing nice by doing your own LoadLibrary and FreeLibraryAndExitThread as described above. 
9. The CLR adds an Informational entry to the Application Event Log noting that the profiler has been unloaded.  The CLR is now ready to service any profiler attach requests. 

## RequestProfilerDetach

Let’s dive a little deeper into the method you call to detach your profiler:

`HRESULT RequestProfilerDetach([in] DWORD dwExpectedCompletionMilliseconds);`

 

First off, you’ll notice this is on ICorProfilerInfo3, the interface your profiler DLL uses, in the same process as your profilee.  Although the AttachProfiler API is called from outside the process, this detach method is called from in-process.  Why?  Well, the general rule with profilers is that _everything_ is done in-process.  Attach is an exception because your profiler isn’t in the process yet.  You need to somehow trigger your profiler to load, and you can’t do that from a process in which you have no code executing yet!  So Attach is sort of a boot-strapping API that has to be called from a process of your own making.

Once your profiler DLL is up and running, it is in charge of everything, from within the same process as the profilee.  And detach is no exception.  Now with that said, it’s probably typical that your profiler will detach in response to an end user action—probably via some GUI that you ship that runs in its own process.  So a case could be made that the CLR team could have made your life easier by providing an out-of-process way to do a detach, so that your GUI could easily trigger a detach, just as it triggered the attach.  However, you could make that same argument about all the ways you might want to control a profiler via a GUI, such as these commands:

- Do a GC now and show me the heap 
- Dial up or down the sampling frequency 
- Change which instrumented methods should log their invocations 
- Start / stop monitoring exceptions 
- etc. 

The point is, if you have a GUI to control your profiler, then you probably already have an inter-process mechanism for the GUI to communicate with your profiler DLL.  So think of “detach” as yet one more command your GUI will send to your profiler DLL.

Ok, fine, so your profiler DLL is the one to call RequestProfilerDetach.  What should it specify for “dwExpectedCompletionMilliseconds”?  The purpose of this parameter is for the profiler to give a guess as to how long the CLR should expect to wait until all control has exited the profiler, thus ensuring success of the CLR’s periodic safety checks (step 5).  So consider all of your callback implementations and what they do.  Pick the “longest” one—the one that does the most processing or blocking or complex calls back into the CLR via ICorProfilerInfo or other interfaces.  Roughly how long will that callback implementation take?  That’s the value (in milliseconds) that you specify for this parameter.

The CLR uses that value in its Sleep() statement that sits between each periodic safety check done as part of step 5.  Although the CLR reserves the right to change the details of this algorithm, currently during step 5 the CLR sleeps dwExpectedCompletionMilliseconds before checking whether all callback methods have popped off all stacks.  If they haven’t, the CLR will sleep an additional dwExpectedCompletionMilliseconds (for a total sleep time of 2\*dwExpectedCompletionMilliseconds) and try again.  If callback methods are still on any stacks, then the CLR degrades to a steady-state of sleeping for 10 minutes and retrying, repeating until the profiler may be unloaded.

Until the profiler can be unloaded, it will be considered “loaded” (though deactivated in the sense that no new callback methods will be called).  This prevents any new profiler from attaching.

 

Ok, that wraps up how detaching works.  If you remember only one thing from this post, remember that it’s really easy to cause an application you profile to AV after your profiler unloads if you’re not careful.  While the CLR tracks outgoing ICorProfilerCallback\* calls, it does not track any other way that control can enter your profiler DLL.  _Before_ your profiler calls RequestProfilerDetach:

- You must take care to deactivate all other ways control can enter your profiler DLL 
- Your profiler must block until all those other ways control can enter your profiler DLL have verifiably been deactivated 

