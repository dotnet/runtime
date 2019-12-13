*This blog post originally appeared on David Broman's blog on 10/17/2005*


Generally, corerror.h tells you all you need to know about what kinds of HRESULTs to expect back from DoStackSnapshot.  However, there are some fringe cases where you can get back an HRESULT that's not as descriptive as you might like.

### E\_FAIL

I don't much like E\_FAIL.  If DoStackSnapshot fails, you will typically see a more descriptive, custom HRESULT.  However, there are regrettably a few ways DoStackSnapshot can fail where you'll see the dreaded E\_FAIL instead.  From your code's point of view, you shouldn't assume E\_FAIL will always imply one of the cases below (or conversely that each of these cases will always result in E\_FAIL).  But this is just good stuff to know as you develop and debug your profiler, so you don't get blindsided.  
  
1) No managed frames on stack  
  
If you call DoStackSnapshot when there are no managed functions on your target thread's stack, you can get E\_FAIL.  For example, if you try to walk the stack of a target thread very early on in its execution, there simply might not be any managed frames there yet.  Or, if you try to walk the stack of the finalizer thread while it's waiting to do work, there will certainly be no managed frames on its stack.  It's also possible that walking a stack with no managed frames on it will yield S\_OK instead of E\_FAIL (e.g., if the target thread is jit-compiling the first managed function to be called on that thread).  Again, your code probably doesn't need to worry about all these cases.  If we call your StackSnapshotCallback for a managed frame, you can trust that frame is there.  If we don't call your StackSnapshotCallback, you can assume there are no managed frames on the stack.

2) OS kernel handling a hardware exception  
  
This one is less likely to happen, but it certainly can.  When an app throws a hardware exception (e.g., divide by 0), the offending thread enters the Windows kernel.  The kernel spends some time recording the thread's current user-mode register context, modifying some registers, and moving the instruction pointer to the user-mode exception dispatch routine.  At this point the thread is ready to reenter user-mode.  But if you are unlucky enough to call DoStackSnapshot while the target thread is still in the kernel doing this stuff, you will get E\_FAIL.

3) Detectably bad seed

If you seed the stack walk with a bogus seed register context, we try to be nice.  Before reading memory pointed to by the registers we run some heuristics to ensure all is on the up and up.  If we find discrepancies, we will fail the stack walk and return E\_FAIL.  If we don't find discrepancies until it's too late and we AV (first-chance), then we'll catch the AV and return E\_UNEXPECTED.

### CORPROF\_E\_STACKSNAPSHOT\_ABORTED

Generally, this HRESULT means that your profiler requested to abort the stack walk in its StackSnapshotCallback.  However, you can also see this HRESULT if the CLR aborted the stack walk on your behalf due to a rare scenario on 64 bit architectures.

One of the beautiful things about running 64-bit Windows is that you can get the Windows OS to perform (native) stack walks for you.  Read up on [RtlVirtualUnwind](http://msdn.microsoft.com/library/default.asp?url=/library/en-us/debug/base/rtlvirtualunwind.asp) if you're unfamiliar with this.  The Windows OS has a critical section to protect a block of memory used to help perform this stack walk.  So what would happen if:

- The OS's exception handling code causes a thread to walk its own stack 
- The thread therefore enters this critical section 
- Your profiler (via DoStackSnapshot) suspends this thread while the thread is still inside the critical section 
- DoStackSnapshot uses RtlVirtualUnwind to help walk this suspended thread 
- RtlVirtualUnwind (executing on the current thread) tries to enter the critical section (already owned by suspended target thread) 

If your answer was "deadlock", congratulations!   DoStackSnapshot has some code that tries to avoid this scenario, by aborting the stack walk before the deadlock can occur.  When this happens, DoStackSnapshot will return CORPROF\_E\_STACKSNAPSHOT\_ABORTED.  Note that this whole scenario is pretty rare, and only happens on WIN64.

