CLR Threading Overview
======================

Managed vs. Native Threads
==========================

Managed code executes on "managed threads," which are distinct from the native threads provided by the operating system. A native thread is a thread of execution of native code on a physical machine; a managed thread is a virtual thread of execution on the CLR's virtual machine.

Just as the JIT compiler maps "virtual" IL instructions into native instructions that execute on the physical machine, the CLR's threading infrastructure maps "virtual" managed threads onto the native threads provided by the operating system.

At any given time, a managed thread may or may not be assigned to a native thread for execution. For example, a managed thread that has been created (via "new System.Threading.Thread") but not yet started (via System.Threading.Thread.Start) is a managed thread that has not yet been assigned to a native thread. Similarly, a managed thread may, in principle, move between multiple native threads over the course of its execution, though in practice the CLR does not currently support this.

The public Thread interface available to managed code intentionally hides the details of the underlying native threads. because:

- Managed threads are not necessarily mapped to a single native thread (and may not be mapped to a native thread at all).
- Different operating systems expose different abstractions for native threads.
- In principle, managed threads are "virtualized".

The CLR provides equivalent abstractions for managed threads, implemented by the CLR itself. For example, it does not expose the operating system's thread-local storage (TLS) mechanism, but instead provides managed "thread-static" variables. Similarly, it does not expose the native thread's "thread ID," but instead provides a "managed thread ID" which is generated independently of the OS. However, for diagnostic purposes, some details of the underlying native thread may be obtained via types in the System.Diagnostics namespace.

Managed threads require additional functionality typically not needed by native threads. First, managed threads hold GC references on their stacks, so the CLR must be able to enumerate (and possibly modify) these references every time a GC occurs. To do this, the CLR must "suspend" each managed thread (stop it at a point where all of its GC references can be found).  Second, when an AppDomain is unloaded, the CLR must ensure that no thread is executing code in that AppDomain. This requires the ability to force a thread to unwind out of that AppDomain. The CLR does this by injecting a ThreadAbortException into such threads.

Data Structures
===============

Every managed thread has an associated Thread object, defined in [threads.h][threads.h]. This object tracks everything the VM needs to know about the managed thread. This includes things that are _necessary_, such as the thread's current GC mode and Frame chain, as well as many things that are allocated per-thread simply for performance reasons (such as some fast arena-style allocators).

All Thread objects are stored in the ThreadStore (also defined in [threads.h][threads.h]), which is a simple list of all known Thread objects. To enumerate all managed threads, one must first acquire the ThreadStoreLock, then use ThreadStore::GetAllThreadList to enumerate all Thread objects. This list may include managed threads which are not currently assigned to native threads (for example, they may not yet be started, or the native thread may already have exited).

[threads.h]: ../../../../src/coreclr/vm/threads.h

Each managed thread that is currently assigned to a native thread is reachable via a native thread-local storage (TLS) slot on that native thread. This allows code that is executing on that native thread to get the corresponding Thread object, via GetThread().

Additionally, many managed threads have a _managed_ Thread object (System.Threading.Thread) which is distinct from the native Thread object. The managed Thread object provides methods for managed code to interact with the thread, and is mostly a wrapper around functionality offered by the native Thread object. The current managed Thread object is reachable (from managed code) via Thread.CurrentThread.

In a debugger, the SOS extension command "!Threads" can be used to enumerate all Thread objects in the ThreadStore.

Thread Lifetimes
================

A managed thread is created in the following situations:

1. Managed code explicitly asks the CLR to create a new thread via System.Threading.Thread.
2. The CLR creates the managed thread directly (see ["special threads"](#special-threads) below).
3. Native code calls managed code on a native thread which is not yet associated with a managed thread (via "reverse p/invoke" or COM interop).
4. A managed process starts (invoking its Main method on the process' Main thread).

In cases #1 and #2, the CLR is responsible for creating a native thread to back the managed thread. This is not done until the thread is actually _started_. In such cases, the native thread is "owned" by the CLR; the CLR is responsible for the native thread's lifetime. In these cases, the CLR is aware of the existence of the thread by virtue of the fact that the CLR created it in the first place.

In cases #3 and #4, the native thread already existed prior to the creation of the managed thread, and is owned by code external to the CLR. The CLR is not responsible for the native thread's lifetime. The CLR becomes aware of these threads the first time they attempt to call managed code.

When a native thread dies, the CLR is notified via its DllMain function. This happens inside of the OS "loader lock," so there is little that can be done (safely) while processing this notification. So rather than destroying the data structures associated with the managed thread, the thread is simply marked as "dead" and signals the finalizer thread to run. The finalizer thread then sweeps through the threads in the ThreadStore and destroys any that are both dead _and_ unreachable via managed code.

Suspension
==========

The CLR must be able to find all references to managed objects in order to perform a GC. Managed code is constantly accessing the GC heap, and manipulating references stored on the stack and in registers. The CLR must ensure that all managed threads are stopped (so they aren't modifying the heap) to safely and reliably find all managed objects. It only stops at _safe points_, when registers and stack locations can be inspected for live references.

Another way of putting this is that the GC heap, and every thread's stack and register state, are "shared state," accessed by multiple threads. As with most shared state, some sort of "lock" is required to protect it. Managed code must hold this lock while accessing the heap, and can only release the lock at safe points.

The CLR refers to this "lock" as the thread's "GC mode." A thread which is in "cooperative mode" holds its lock; it must "cooperate" with the GC (by releasing the lock) in order for a GC to proceed. A thread which is in "preemptive" mode does not hold its lock – the GC may proceed "preemptively" because the thread is known to not be accessing the GC heap.

A GC may only proceed when all managed threads are in "preemptive" mode (not holding the lock). The process of moving all managed threads to preemptive mode is known as "GC suspension" or "suspending the Execution Engine (EE)."

A naïve implementation of this "lock" would be for each managed thread to actually acquire and release a real lock around each access to the GC heap. Then the GC would simply attempt to acquire the lock on each thread; once it had acquired all threads' locks, it would be safe to perform the GC.

However, this naïve approach is unsatisfactory for two reasons. First, it would require managed code to spend a lot of time acquiring and releasing the lock (or at least checking whether the GC was attempting to acquire the lock – known as "GC polling.") Second, it would require the JIT to emit "GC info" describing the layout of the stack and registers for every point in JIT'd code; this information would consume large amounts of memory.

We refined this naïve approach by separating JIT'd managed code into "partially interruptible" and "fully interruptible" code. In partially interruptible code, the only safe points are calls to other methods, and explicit "GC poll" locations where the JIT emits code to check whether a GC is pending. GC info need only be emitted for these locations. In fully interruptible code, every instruction is a safe point, and the JIT emits GC info for every instruction – but it does not emit GC polls. Instead, fully interruptible code may be "interrupted" by hijacking the thread (a process which is discussed later in this document). The JIT chooses whether to emit fully- or partially-interruptible code based on heuristics to find the best tradeoff between code quality, size of the GC info, and GC suspension latency.

Given the above, there are three fundamental operations to define: entering cooperative mode, leaving cooperative mode, and suspending the EE.

Entering Cooperative Mode
-------------------------

A thread enters cooperative mode by calling Thread::DisablePreemptiveGC. This acquires the "lock" for the current thread, as follows:

1. If a GC is in progress (the GC holds the lock) then block until the GC is complete.
2. Mark the thread as being in cooperative mode. No GC may proceed until the thread reenters preemptive mode.

These two steps proceed as if they were atomic.

Entering Preemptive Mode
------------------------

A thread enters preemptive mode (releases the lock) by calling Thread::EnablePreemptiveGC. This simply marks the thread as no longer being in cooperative mode, and informs the GC thread that it may be able to proceed.

Suspending the EE
-----------------

When a GC needs to occur, the first step is to suspend the EE. This is done by GCHeap::SuspendEE, which proceeds as follows:

1. Set a global flag (g\_fTrapReturningThreads) to indicate that a GC is in progress. Any threads that attempt to enter cooperative mode will block until the GC is complete.
2. Find all threads currently executing in cooperative mode. For each such thread, attempt to hijack the thread and force it to leave cooperative mode.
3. Repeat until no threads are running in cooperative mode.

Hijacking
---------

Hijacking for GC suspension is done by Thread::SysSuspendForGC. This method attempts to force any managed thread that is currently running in cooperative mode, to leave cooperative mode at a "safe point." It does this by enumerating all managed threads (walking the ThreadStore), and for each managed thread currently running in cooperative mode.

1. Suspend the underlying native thread. This is done with the Win32 SuspendThread API. This API forcibly stops the thread from running, at some random point in its execution (not necessarily a safe point).
2. Get the current CONTEXT for the thread, via GetThreadContext. This is an OS concept; CONTEXT represents the current register state of the thread. This allows us to inspect its instruction pointer, and thus determine what type of code it is currently executing.
3. Check again if the thread is in cooperative mode, as it may have already left cooperative mode before it could be suspended. If so, the thread is in dangerous territory: the thread may be executing arbitrary native code, and must be resumed immediately to avoid deadlocks.
4. Check if the thread is running managed code. It is possible that it is executing native VM code in cooperative mode (see Synchronization, below), in which case the thread must be immediately resumed as in the previous step.
5. Now the thread is suspended in managed code. Depending on whether that code is fully- or partially-interruptible, one of the following is performed:
  * If fully interruptible, it is safe to perform a GC at any point, since the thread is, by definition, at a safe point. It is reasonable to leave the thread suspended at this point (because it's safe) but various historical OS bugs prevent this from working, because the CONTEXT retrieved earlier may be corrupt). Instead, the thread's instruction pointer is overwritten, redirecting it to a stub that will capture a more complete CONTEXT, leave cooperative mode, wait for the GC to complete, reenter cooperative mode, and restore the thread to its previous state.
  * If partially-interruptible, the thread is, by definition, not at a safe point. However, the caller will be at a safe point (method transition). Using that knowledge, the CLR "hijacks" the top-most stack frame's return address (physically overwrite that location on the stack) with a stub similar to the one used for fully-interruptible code. When the method returns, it will no longer return to its actual caller, but rather to the stub (the method may also perform a GC poll, inserted by the JIT, before that point, which will cause it to leave cooperative mode and undo the hijack).

ThreadAbort / AppDomain-Unload
==============================

In order to unload an AppDomain, the CLR must ensure that no thread is running in that AppDomain. To accomplish this, all managed threads are enumerated, and "abort" any threads which have stack frames belonging to the AppDomain being unloaded. A ThreadAbortException is "injected" into the running thread, which  causes the thread to unwind (executing backout code along the way) until it is no longer executing in the AppDomain, at which point the ThreadAbortException is translated into an AppDomainUnloaded exception.

ThreadAbortException is a special type of exception. It can be caught by user code, but the CLR ensures that the exception will be rethrown after the user's exception handler is executed. Thus ThreadAbortException is sometimes referred to as "uncatchable," though this is not strictly true.

A ThreadAbortException is typically 'thrown' by simply setting a bit on the managed thread marking it as "aborting." This bit is checked by various parts of the CLR (most notably, every return from a p/invoke) and often times setting this bit is all that is needed to get the thread aborted in a timely manner.

However, if the thread is, for example, executing a long-running managed loop, it may never check this bit. To get such a thread to abort faster, the thread is "hijacked" and forced to raise a ThreadAbortException. This hijacking is done in the same way as GC suspension, except that the stubs that the thread is redirected to will cause a ThreadAbortException to be raised, rather than waiting for a GC to complete.

This hijacking means that a ThreadAbortException can be raised at essentially any arbitrary point in managed code. This makes it extremely difficult for managed code to deal successfully with a ThreadAbortException. It is therefore unwise to use this mechanism for any purpose other than AppDomain-Unload, which ensures that any state corrupted by the ThreadAbort will be cleaned up along with the AppDomain.

Synchronization: Managed
========================

Managed code has access to many synchronization primitives, collected within the System.Threading namespace. These include wrappers for native OS primitives like Mutex, Event, and Semaphore objects, as well as some abstractions such as Barriers and SpinLocks. However, the primary synchronization mechanism used by most managed code is System.Threading.Monitor, which provides a high-performance locking facility on _any managed object_, and additionally provides "condition variable" semantics for signaling changes in the state protected by a lock.

Monitor is implemented as a "hybrid lock;" it has features of both a spin-lock and a kernel-based lock like a Mutex. The idea is that most locks are held only briefly, so it takes less time to simply spin-wait for the lock to be released, than it would to make a call into the kernel to block the thread. It is important not to waste CPU cycles spinning, so if the lock has not been acquired after a brief period of spinning, the implementation falls back to blocking in the kernel.

Because any object may potentially be used as a lock/condition variable, every object must have a location in which to store the lock information. This is done with "object headers" and "sync blocks."

The object header is a machine-word-sized field that precedes every managed object. It is used for many purposes, such as storing the object's hash code. One such purpose is holding the object's lock state. If more per-object data is needed than will fit in the object header, we "inflate" the object by creating a "sync block."

Sync blocks are stored in the Sync Block Table, and are addressed by sync block indexes. Each object with an associated sync block has the index of that index in the object's object header.

The details of object headers and sync blocks are defined in [syncblk.h][syncblk.h]/[.cpp][syncblk.cpp].

[syncblk.h]: ../../../../src/coreclr/vm/syncblk.h
[syncblk.cpp]: ../../../../src/coreclr/vm/syncblk.cpp

If there is room on the object header, Monitor stores the managed thread ID of the thread that currently holds the lock on the object (or zero (0) if no thread holds the lock). Acquiring the lock in this case is a simple matter of spin-waiting until the object header's thread ID is zero, and then atomically setting it to the current thread's managed thread ID.

If the lock cannot be acquired in this manner after some number of spins, or the object header is already being used for other purposes, a sync block must be created for the object. This has additional data, including an event that can be used to block the current thread, allowing us to stop spinning and efficiently wait for the lock to be released.

An object that is used as a condition variable (via Monitor.Wait and Monitor.Pulse) must always be inflated, as there is not enough room in the sync block to hold the required state.

Synchronization: Native
=======================

The native portion of the CLR must also be aware of threading, as it will be invoked by managed code on multiple threads. This requires native synchronization mechanisms, such as locks, events, etc.

The ITaskHost API allows a host to override many aspects of managed threading, including thread creation, destruction, and synchronization. The ability of a host to override native synchronization means that VM code can generally not use native synchronization primitives (Critical Sections, Mutexes, Events, etc.) directly, but rather must use the VM's wrappers over these.

Additionally, as described above, GC suspension is a special kind of "lock" that affects nearly every aspect of the CLR. Native code in the VM may enter "cooperative" mode if it must manipulate GC heap objects, and thus the "GC suspension lock" becomes one of the most important synchronization mechanisms in native VM code, as well as managed.

The major synchronization mechanisms used in native VM code are the GC mode, and Crst.

GC Mode
-------

As discussed above, all managed code runs in cooperative mode, because it may manipulate the GC heap. Generally, native code does not touch managed objects, and thus runs in preemptive mode. But some native code in the VM must access the GC heap, and thus must run in cooperative mode.

Native code generally does not manipulate the GC mode directly, but rather uses two macros: GCX\_COOP and GCX\_PREEMP.  These enter the desired mode, and erect "holders" to cause the thread to revert to the previous mode when the scope is exited.

It is important to understand that GCX\_COOP effectively acquires a lock on the GC heap. No GC may proceed while the thread is in cooperative mode. And native code cannot be "hijacked" as is done for managed code, so the thread will remain in cooperative mode until it explicitly switches back to preemptive mode.

Thus entering cooperative mode in native code is discouraged. In cases where cooperative mode must be entered, it should be kept to as short a time as possible. The thread should not be blocked in this mode, and in particular cannot generally acquire locks safely.

Similarly, GCX\_PREEMP potentially _releases_ a lock that had been held by the thread. Great care must be taken to ensure that all GC references are properly protected before entering preemptive mode.

The [Rules of the Code](../../../coding-guidelines/clr-code-guide.md) document describes the disciplines needed to ensure safety around GC mode switches.

Crst
----

Just as Monitor is the preferred locking mechanism for managed code, Crst is the preferred mechanism for VM code. Like Monitor, Crst is a hybrid lock that is aware of hosts and GC modes. Crst also implements deadlock avoidance via "lock leveling," described in the [Crst Leveling chapter of the BotR](../../../coding-guidelines/clr-code-guide.md#2.6.4).

It is generally illegal to acquire a Crst while in cooperative mode, though exceptions are made where absolutely necessary.

Special Threads
===============

In addition to managing threads created by managed code, the CLR creates several "special" threads for its own use.

Finalizer Thread
----------------

This thread is created in every process that runs managed code. When the GC determines that a finalizable object is no longer reachable, it places that object on a finalization queue. At the end of a GC, the finalizer thread is signaled to process all finalizers currently in this queue. Each object is then dequeued, one by one, and its finalizer is executed.

This thread is also used to perform various CLR-internal housekeeping tasks, and to wait for notifications of some external events (such as a low-memory condition, which signals the GC to collect more aggressively). See GCHeap::FinalizerThreadStart for the details.

GC Threads
----------

When running in "concurrent" or "server" modes, the GC creates one or more background threads to perform various stages of garbage collection in parallel. These threads are wholly owned and managed by the GC, and never run managed code.

Debugger Thread
---------------

The CLR maintains a single native thread in each managed process, which performs various tasks on behalf of attached managed debuggers.

AppDomain-Unload Thread
-----------------------

This thread is responsible for unloading AppDomains. This is done on a separate, CLR-internal thread, rather than the thread that requests the AD-unload, to a) provide guaranteed stack space for the unload logic, and b) allow the thread that requested the unload to be unwound out of the AD, if needed.

ThreadPool Threads
------------------

The CLR's ThreadPool maintains a collection of managed threads for executing user "work items."  These managed threads are bound to native threads owned by the ThreadPool. The ThreadPool also maintains a small number of native threads to handle functions like "thread injection," timers, and "registered waits."
