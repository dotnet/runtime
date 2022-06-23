What Every CLR Developer Must Know Before Writing Code
===

Written in 2006, by:

- Rick Byers ([@RByers](https://github.com/RByers))
- Jan Kotas ([@jkotas](https://github.com/jkotas))
- Mike Stall ([@mikestall](https://github.com/mikestall))
- Rudi Martin ([@Rudi-Martin](https://github.com/Rudi-Martin))

# Contents

* [1 Why you must read this document](#1)
  * [1.1 Rules of the Code](#1.1)
  * [1.2 How do I &lt;insert common task&gt;?](#1.2)
* [2 Rules of the Code (Unmanaged)](#2)
  * [2.1 Is your code GC-safe?](#2.1)
    * [2.1.1 How GC holes are created](#2.1.1)
    * [2.1.2 Your First GC hole](#2.1.2)
    * [2.1.3 Use GCPROTECT_BEGIN to keep your references up to date](#2.1.3)
    * [2.1.4 Don't do nonlocal returns from within GCPROTECT blocks](#2.1.4)
    * [2.1.5 Do not GCPROTECT the same location twice](#2.1.5)
    * [2.1.6 Protecting multiple OBJECTREF's](#2.1.6)
    * [2.1.7 Use OBJECTHANDLES for non-scoped protection](#2.1.7)
    * [2.1.8 Use the right GC Mode – Preemptive vs. Cooperative](#2.1.8)
    * [2.1.9 Use OBJECTREF to refer to object references as it does automatic sanity checking](#2.1.9)
    * [2.1.10 How to know if a function can trigger a GC](#2.1.10)
      * [2.1.10.1 GC_NOTRIGGER/TRIGGERSGC on a scope](#2.1.10.1)
  * [2.2 Are you using holders to track your resources?](#2.2)
    * [2.2.1 What are holders and why are they important?](#2.2.1)
    * [2.2.2 An example of holder usage:](#2.2.2)
    * [2.2.3 Common Features of Holders](#2.2.3)
    * [2.2.4 Where do I find a holder?](#2.2.4)
    * [2.2.5 Can I bake my own holder?](#2.2.5)
    * [2.2.6 What if my backout code throws an exception?](#2.2.6)
    * [2.2.7 Pay attention to holder initialization semantics](#2.2.7)
    * [2.2.8 Some generally useful prebaked holders](#2.2.8)
      * [2.2.8.1 New'ed memory](#2.2.8.1)
      * [2.2.8.2 New'ed array](#2.2.8.2)
      * [2.2.8.3 COM Interface Holder](#2.2.8.3)
      * [2.2.8.4 Critical Section Holder](#2.2.8.4)
  * [2.3 Does your code follow our OOM rules?](#2.3)
    * [2.3.1 What is OOM and why is it important?](#2.3.1)
    * [2.3.2 Documenting where OOM's can happen](#2.3.2)
      * [2.3.2.1 Functions that handle OOM's internally](#2.3.2.1)
      * [2.3.2.2 OOM state control outside of contracts](#2.3.2.2)
      * [2.3.2.3 Remember...](#2.3.2.3)
  * [2.4 Are you using SString and/or the safe string manipulation functions?](#2.4)
    * [2.4.1 SString](#2.4.1)
  * [2.5 Are you using safemath.h for pointer and memory size allocations?](#2.5)
  * [2.6 Are you using the right type of Critical Section?](#2.6)
    * [2.6.1 Use only the official synchronization mechanisms](#2.6.1)
    * [2.6.2 Using Crsts](#2.6.2)
    * [2.6.3 Creating Crsts](#2.6.3)
    * [2.6.4 Entering and Leaving Crsts](#2.6.4)
    * [2.6.5 Other Crst Operations](#2.6.5)
    * [2.6.6 Advice on picking a level for your Crst](#2.6.6)
    * [2.6.7 Can waiting on a Crst generate an exception?](#2.6.7)
    * [2.6.8 CRITSECT_UNSAFE Flags](#2.6.8)
    * [2.6.9 Bypassing leveling (CRSTUNORDEREDnordered)](#2.6.9)
    * [2.6.10 So what are the prerequisites and side-effects of entering a Crst?](#2.6.10)
    * [2.6.11 Using Events and Waitable Handles](#2.6.11)
    * [2.6.12 Do not get clever with "lockless" reader-writer data structures](#2.6.12)
    * [2.6.13 Yes, your thread could be running non-preemptively!](#2.6.13)
    * [2.6.14 Dos and Don'ts for Synchronization](#2.6.14)
  * [2.7 Are you making hidden assumptions about the order of memory writes?](#2.7)
  * [2.8 Is your code compatible with managed debugging?](#2.8)
  * [2.9 Does your code work on 64-bit?](#2.9)
    * [2.9.1 Primitive Types](#2.9.1)
  * [2.10 Does your function declare a CONTRACT?](#2.10)
    * [2.10.1 What can be said in a contract?](#2.10.1)
      * [2.10.1.1 THROWS/NOTHROW](#2.10.1.1)
      * [2.10.1.2 INJECT_FAULT(handler-stmt)/FORBID_FAULT](#2.10.1.2)
      * [2.10.1.3 GC_TRIGGERS/GC_NOTRIGGER](#2.10.1.3)
      * [2.10.1.4 MODE_PREEMPTIVE/ MODE_COOPERATIVE/ MODE_ANY](#2.10.1.4)
      * [2.10.1.5 LOADS_TYPE(loadlevel)](#2.10.1.5)
      * [2.10.1.6 CAN_TAKE_LOCK / CANNOT_TAKE_LOCK](#2.10.1.6)
      * [2.10.1.7 EE_THREAD_REQUIRED / EE_THREAD_NOT_REQUIRED](#2.10.1.7)
      * [2.10.1.8 PRECONDITION(expr)](#2.10.1.8)
      * [2.10.1.9 POSTCONDITION(expr)](#2.10.1.9)
    * [2.10.2 Is order important?](#2.10.2)
    * [2.10.3 Using the right form of contract](#2.10.3)
    * [2.10.4 When is it safe to use a runtime contract?](#2.10.4)
    * [2.10.5 Do not make unscoped changes to the ClrDebugState](#2.10.5)
    * [2.10.6 For more details...](#2.10.6)
  * [2.11 Is your code DAC compliant?](#2.11)

# <a name="1"></a>1 Why you must read this document

Like most large codebases, the CLR codebase has many internal invariants and an extensive debug build infrastructure for detecting problems. Clearly, it is important that developers working on the CLR understand these rules and conventions.

The information contained here is considered the minimum set of knowledge required of developers who work on any part of the CLR. This is the document we wished we had all throughout the CLR's history, especially when fixing a bug that could have been prevented had this information been more readily available.

This document is divided into the following sections.

## <a name="1.1"></a>1.1 Rules of the Code

This is the most important section. Think of the chapter headings as a checklist to use while designing and writing your code. This section is divided into sections for managed and unmanaged code as they face quite different issues.

Rules can either be imposed by invariants or team policy.

"Invariants" are actual semantic rules imposed by the architecture, e.g. the GC-safe use of managed object references in unmanaged code. There's nothing discretionary about these. Violate these and you've introduced a customer-visible bug.

"Team Policy" rules are rules we've established as "good practices" – for example, the rule that every function must declare a contract. While a missing contract here or there isn't a shipstopper, violating these rules is still heavily frowned upon and you should expect a bug filed against you unless you can supply a very compelling reason why your code needs an exemption.

Team policy rules are not necessarily less important than invariants. For example, the rule to use [safemath.h][safemath.h] rather that coding your own integer overflow check is a policy rule. But because it deals with security, we'd probably treat it as higher priority than a very obscure (non-security) related bug.

[safemath.h]: https://github.com/dotnet/runtime/blob/main/src/coreclr/inc/safemath.h

One type of rule you won't find here are purely syntactic "code formatting" rules such as brace placement. While there is value in uniform stylistic conventions, we don't want to "lay down the law" on these to the extent that we do for the more semantic-oriented issues covered here. The rules included in this document are here because breaking them would do one of the following:

- Introduce an actual bug.
- Significantly increase the risk of a serious bug slipping through.
- Frustrate our automated bug-detection infrastructure.

## <a name="1.2"></a>1.2 How do I &lt;insert common task&gt;?

The chapter headings in this section can be regarded as a FAQ. If you have a specific need, look here for "best practices" guidance on how to get something. Also, if you're thinking of adding yet another hash table implementation to the code base, check here first as there's a good chance there's already existing code that can be adapted or used as is.

This section will also be divided into managed and unmanaged sections.

# <a name="2"></a>2 Rules of the Code (Unmanaged)

## <a name="2.1"></a>2.1 Is your code GC-safe?

### <a name="2.1.1"></a>2.1.1 How GC holes are created

The term "GC hole" refers to a special class of bugs that bedevils the CLR. The GC hole is a pernicious bug because it is easy to introduce by accident, repros rarely and is very tedious to debug. A single GC hole can suck up weeks of dev and test time.

One of the major features of the CLR is the Garbage Collection system. That means that allocated objects, as seen by a managed application, are never freed explicitly by the programmer. Instead, the CLR periodically runs a Garbage Collector (GC). The GC discards objects that are no longer in use. Also, the GC compacts the heap to avoid unused holes in memory. Therefore, a managed object does not have a fixed address. Objects move around according to the whims of the garbage collector.

To do its job, the GC must be told about every reference to every GC object. The GC must know about every stack location, every register and every non-GC data structure that holds a pointer to a GC object. These external pointers are called "root references."

Armed with this information, the GC can find all objects directly referenced from outside the GC heap. These objects may in turn, reference other objects – which in turn reference other objects and so on. By following these references, the GC finds all reachable ("live") objects. All other objects are, by definition, unreachable and therefore discarded. After that, the GC may move the surviving objects to reduce memory fragmentation. If it does this, it must, of course, update all existing references to the moved object.

Any time a new object is allocated, a GC may occur. GC can also be explicitly requested by calling the GarbageCollect function directly. GC's do not happen asynchronously outside these events but since other running threads can trigger GC's, your thread must act as if GC's _are_ asynchronous unless you take specific steps to synchronize with the GC. More on that later.

A GC hole occurs when code inside the CLR creates a reference to a GC object, neglects to tell the GC about that reference, performs some operation that directly or indirectly triggers a GC, then tries to use the original reference. At this point, the reference points to garbage memory and the CLR will either read out a wrong value or corrupt whatever that reference is pointing to.

### <a name="2.1.2"></a>2.1.2 Your First GC hole

The code fragment below is the simplest way to introduce a GC hole into the system.

	//OBJECTREF is a typedef for Object*.

	{
	     MethodTable *pMT = g_pObjectClass->GetMethodTable();

	     OBJECTREF a = AllocateObject(pMT);
	     OBJECTREF b = AllocateObject(pMT);

	     //WRONG!!! "a" may point to garbage if the second
	     //"AllocateObject" triggered a GC.
	     DoSomething (a, b);
	}

All it does is allocate two managed objects, and then does something with them both.

This code compiles fine, and if you run simple pre-checkin tests, it will probably "work." But this code will crash eventually.

Why? If the second call to AllocateObject() triggers a GC, that GC discards the object instance you just assigned to "a". This code, like all C++ code inside the CLR, is compiled by a non-managed compiler and the GC cannot know that "a" holds a root reference to an object you want kept live.

This point is worth repeating. The GC has no intrinsic knowledge of root references stored in local variables or non-GC data structures maintained by the CLR itself. You must explicitly tell the GC about them.

### <a name="2.1.3"></a>2.1.3 Use GCPROTECT_BEGIN to keep your references up to date

Here's how to fix our buggy code fragment.

	#include "frames.h"
	{
	     MethodTable *pMT = g_pObjectClass->GetMethodTable();

	    //RIGHT
	    OBJECTREF a = AllocateObject(pMT);

	    GCPROTECT_BEGIN(a);
	    OBJECTREF b = AllocateObject(pMT);

	    DoSomething (a, b);

	    GCPROTECT_END();
	}

Notice the addition of the line GCPROTECT_BEGIN(a). GCPROTECT_BEGIN is a macro whose argument is any OBJECTREF-typed storage location (it has to be an expression that can you can legally apply the address-of (&) operator to.) GCPROTECT_BEGIN tells the GC two things:

- The GC is not to discard any object referred to by the reference stored in local "a".
- If the GC moves the object referred to by "a", it must update "a" to point to the new location.

Now, if the second AllocateObject() triggers a GC, the "a" object will still be around afterwards, and the local variable "a" will still point to it. "a" may not contain the same address as before, but it will point to the same object. Hence, DoSomething() receives the correct data.

Note that we didn't similarly protect 'b" because the caller has no use for "b" after DoSomething returns. Furthermore, there's no point in keeping "b" updated because DoSomething receives a copy of the reference (don't confuse with "copy of the object"), not the reference itself. If DoSomething internally causes GC as well, it is DoSomething's responsibility to protect its own copies of "a" and "b".

Having said that, no one should complain if you play it safe and GCPROTECT "b" as well. You never know when someone might add code later that makes the protection necessary.

Every GCPROTECT_BEGIN must have a matching GCPROTECT_END, which terminates the protected status of "a". As an additional safeguard, GCPROTECT_END overwrites "a" with garbage so that any attempt to use "a" afterward will fault. GCPROTECT_BEGIN introduces a new C scoping level that GCPROTECT_END closes, so if you use one without the other, you'll probably experience severe build errors.

### <a name="2.1.4"></a>2.1.4 Don't do nonlocal returns from within GCPROTECT blocks

Never do a "return", "goto" or other non-local return from between a GCPROTECT_BEGIN/END pair. This will leave the thread's frame chain corrupted.

One exception: it is explicitly allowed to leave a GCPROTECT block by throwing a managed exception (usually via the COMPlusThrow() function). The exception subsystem knows about GCPROTECT and correctly fixes up the frame chain as it unwinds.

Why is GCPROTECT not implemented via a C++ smart pointer? The GCPROTECT macro originates in .NET Framework v1. All error handling was done explicitly at that time, without any use C++ exception handling or stack allocated holders.

### <a name="2.1.5"></a>2.1.5 Do not GCPROTECT the same location twice

The following is illegal and will cause some sort of crash:

	// WRONG: Can't GCPROTECT twice.
	OBJECTREF a = AllocateObject(...);
	GCPROTECT_BEGIN(a);
	GCPROTECT_BEGIN(a);

It'd be nice if the GC was robust enough to ignore the second, unnecessary GCPROTECT but I've been assured many times that this isn't possible.

Don't confuse the reference with a copy of the reference. It's not illegal to protect the same reference twice. What is illegal is protecting the same _copy_ of the reference twice. Hence, the following is legal:

	OBJECTREF a = AllocateObject(...);
	GCPROTECT_BEGIN(a);
	DoSomething(a);
	GCPROTECT_END();

	void DoSomething(OBJECTREF a)
	{
	    GCPROTECT_BEGIN(a);
	    GCPROTECT_END();
	}

### <a name="2.1.6"></a>2.1.6 Protecting multiple OBJECTREF's

You can protect multiple OBJECTREF locations using one GCPROTECT. Group them all into a structure and pass the structure to GCPROTECT_BEGIN. GCPROTECT_BEGIN applies a sizeof to determine how many locations you want to protect. Do not mix any non-OBJECTREF fields into the struct!

### <a name="2.1.7"></a>2.1.7 Use OBJECTHANDLES for non-scoped protection

GCPROTECT_BEGIN is very handy, as we've seen, but its protection is limited to a C++ nesting scope. Suppose you need to store a root reference inside a non-GC data structure that lives for an arbitrary amount of time?

The solution is the OBJECTHANDLE. OBJECTHANDLE allocates a location from special blocks of memory that are known explicitly to the GC. Any root reference stored in this location will automatically keep the object live and be updated to reflect object moves. You can retrieve the correct reference by indirecting the location.

Handles are implemented through several layers of abstraction – the "official" interface for public use is the one described here and is exposed through [objecthandle.h][objecthandle.h]. Don't confuse this with [handletable.h][handletable.h] which contains the internals. The CreateHandle() api allocates a new location. ObjectFromHandle() dereferences the handle and returns an up-to-date reference. DestroyHandle() frees the location.

[objecthandle.h]: https://github.com/dotnet/runtime/blob/main/src/coreclr/gc/objecthandle.h
[handletable.h]: https://github.com/dotnet/runtime/blob/main/src/coreclr/gc/handletable.h

The following code fragment shows how handles are used. In practice, of course, people use GCPROTECT rather than handles for situations this simple.

	{
	    MethodTable *pMT = g_pObjectClass->GetMethodTable();

	    //Another way is to use handles. Handles would be
	    // wasteful for a case this simple but are useful
	    // if you need to protect something for the long
	    // term.
	    OBJECTHANDLE ah;
	    OBJECTHANDLE bh;

	    ah = CreateHandle(AllocateObject(pMT));
	    bh = CreateHandle(AllocateObject(pMT));

	    DoSomething (ObjectFromHandle(ah),
	                 ObjectFromhandle(bh));

	    DestroyHandle(bh);
	    DestroyHandle(ah);
	}

There are actually several flavors of handles. This section lists the most common ones. ([objecthandle.h][objecthandle.h] contains the complete list.)

- **HNDTYPE_STRONG**: This is the default and acts like a normal reference. Created by calling CreateHandle(OBJECTREF).
- **HNDTYPE_WEAK_LONG**: Tracks an object as long as one strong reference to its exists but does not itself prevent the object from being GC'd. Created by calling CreateWeakHandle(OBJECTREF).
- **HNDTYPE_PINNED**: Pinned handles are strong handles which have the added property that they prevent an object from moving during a garbage collection cycle. This is useful when passing a pointer to object innards out of the runtime while GC may be enabled.

NOTE: PINNING AN OBJECT IS EXPENSIVE AS IT PREVENTS THE GC FROM ACHIEVING OPTIMAL PACKING OF OBJECTS DURING EPHEMERAL COLLECTIONS. THIS TYPE OF HANDLE SHOULD BE USED SPARINGLY!

### <a name="2.1.8"></a>2.1.8 Use the right GC Mode – Preemptive vs. Cooperative

Earlier, we implied that GC doesn't occur spontaneously. This is true... for a given thread. But the CLR is multithreaded. Even if your thread does all the right things, it has no control over other threads.

Consider two possible ways to schedule GC:

- **Preemptive**: Any thread that needs to do a GC can do one without regard for the state of other threads. The other threads run concurrently with the GC.
- **Cooperative**: A thread can only start a GC once all other threads agree to allow the GC. The thread attempting the GC is blocked until all other threads reach a state of agreement.

Both have their strengths and drawbacks. Preemptive mode sounds attractive and efficient except for one thing: it completely breaks our previously discussed GC-protection mechanism. Consider the following code fragment:

	OBJECTREF a = AllocateObject(...)
	GCPROTECT_BEGIN(a);
	DoSomething(a);

Now, while the compiler can generate any valid code for this, it's very likely it will look something like this:

	call	AllocateObject
	mov	[A],eax  ;;store result in "a"
	... code for GCPROTECT_BEGIN omitted...
	push	[A]        ;push argument to DoSomething
	call	DoSomething

This is supposed to be work correctly in every case, according to the semantics of GCPROTECT. However, suppose just after the "push" instruction, another thread gets the time-slice, starts a GC and moves the object A. The local variable A will be correctly updated – but the copy of A which we just pushed as an argument to DoSomething() will not. Hence, DoSomething() will receive a pointer to the old location and crash. Clearly, preemptive GC alone will not suffice for the CLR.

How about the alternative: cooperative GC? With cooperative GC, the above problem doesn't occur and GCPROTECT works as intended. Unfortunately, the CLR has to interop with legacy unmanaged code as well. Suppose a managed app calls out to the Win32 MessageBox api which waits for the user to click a button before returning. Until the user does this, all managed threads in the same process are blocked from GC. Not good.

Because neither policy alone suffices for the CLR, the CLR supports both: and you, as a developer, are responsible for switching the threads accordingly. Note that the GC-scheduling mode is a property of an individual thread; not a global system property.

Put precisely: as long as a thread is in cooperative mode, it is guaranteed that a GC will only occur when your thread triggers an object allocation, calls out to interruptible managed code or explicitly requests a GC. All other threads are blocked from GC. As long as your thread is in preemptive mode, then you must assume that a GC can be started any time (by some other thread) and is running concurrently with your thread.

A good rule of thumb is this: a CLR thread runs in cooperative mode any time it is running managed code or any time it needs to manipulate object references in any way. An Execution Engine (EE) thread that is running in preemptive mode is usually running unmanaged code; i.e. it has left the managed world. Process threads that have never entered CLR in any way are effectively running in preemptive mode. Much of the code inside CLR runs in cooperative mode.

While you are running in preemptive mode, OBJECTREF's are strictly hands-off; their values are completely unreliable. In fact, the checked build asserts if you even touch an OBJECTREF in preemptive mode. In cooperative mode, you are blocking other threads from GC so you must avoid long or blocking operations. Also be aware of any critical sections or semaphores you wait on. They must not guard sections that themselves trigger GC.

**Setting the GC mode:** The preferred way to set the GC mode are the GCX_COOP and GCX_PREEMP macros. These macros operate as holders. That is, you declare them at the start of the block of code you want to execute in a certain mode. Upon any local or non-local exit out of that scope, a destructor automatically restores the original mode.

	{ // always open a new C++ scope to switch modes
	    GCX_COOP();
	    Code you want run in cooperative mode
	} // leaving scope automatically restores original mode

It's perfectly legal to invoke GCX_COOP() when the thread is already in cooperative mode. GCX_COOP will be a NOP in that case. Likewise for GCX_PREEMP.

GCX_COOP and GCX_PREEMP will never throw an exception and return no error status.

There is a special case for purely unmanaged threads (threads that have no Thread structure created for them.) Such threads are considered "permanently preemptive." Hence, GCX_COOP will assert if called on such a thread while GCX_PREEMP will succeed as a NOP.

There are a couple of variants for special situations:

- **GCX_MAYBE_\*(BOOL)**: This version only performs the switch if the boolean parameter is TRUE. Note that the mode restore at the end of the scope still occurs whether or not you passed TRUE. (Of course, this is only important if the mode got switched some other way inside the scope. Usually, this shouldn't happen.)
- **GCX_\*_THREAD_EXISTS(Thread\*)**: If you're concerned about the repeated GetThread() and null Thread checks inside this holder, use this "performance" version which lets you cache the Thread pointer and pass it to all the GCX_\* calls. You cannot use this to change the mode of another thread. You also cannot pass NULL here.

To switch modes multiple times in a function, you must introduce a new scope for each switch. You can also call GCX_POP(), which performs a mode restore prior to the end of the scope. (The mode restore will happen again at the end of the scope, however. Since mode restore is idempotent, this shouldn't matter.) Do not, however, do this:

	{
	     GCX_COOP();
	     ...
	     GCX_PREEMP():	//WRONG!
	}

You will get a compile error due to a variable being redeclared in the same scope.

While the holder-based macros are the preferred way to switch modes, sometimes one needs to leave a mode changed beyond the end of the scope. For those situations, you may use the "raw" unscoped functions:

	GetThread()->DisablePreemptiveGC();   // switch to cooperative mode
	GetThread()->EnablePreemptiveGC();	// switch to preemptive mode

There is no automatic mode-restore with these functions so the onus is on you to manage the lifetime of the mode. Also, mode changes cannot be nested. You will get an assert if you try to change to a mode you're already in. The "this" argument must be the currently executing thread. You cannot use this to change the mode of another thread.

**Key Takeaway:** Use GCX_COOP/PREEMP rather than unscoped calls to DisablePreemptiveGC() whenever possible.

**Testing/asserting the GC mode:**

You can assert the need to be in a particular mode in the contract by using one of the following:

	CONTRACTL
	{
	    MODE_COOPERATIVE
	}
	CONTRACTL_END

	CONTRACTL
	{
	    MODE_PREEMPTIVE
	}
	CONTRACTL_END

There are also standalone versions:

	{
	    GCX_ASSERT_COOP();
	}

	{
	    GCX_ASSERT_PREEMP();
	}

You'll notice that the standalone versions are actually holders rather than simple statements. The intention was that these holders would assert again on scope exit to ensure that any backout holders are correctly restoring the mode. However, that exit check was disabled initially with the idea of enabling it eventually once all the backout code was clean. Unfortunately, the "eventually" has yet to arrive. As long as you use the GCX holders to manage mode changes, this shouldn't really be a problem.

### <a name="2.1.9"></a>2.1.9 Use OBJECTREF to refer to object references as it does automatic sanity checking

The checked build inserts automatic sanity-checking every single time an OBJECTREF is manipulated. Under the retail build, OBJECTREF is defined as a pointer exactly as you'd expect. But under the checked build, OBJECTREF is defined as a "smart-pointer" class that sanity-checks the pointer on every operation. Also, the current thread is validated to be in cooperative GC mode.

Thus, the following code fragment:

	OBJECTREF uninitialized;
	DoSomething(uninitialized);

will produce the following assert:

	"Detected use of a corrupted OBJECTREF. Possible GC hole."

This is because the default constructor for OBJECTREF initializes to 0xcccccccc. When you pass "uninitialized" to DoSomething(), this invokes the OBJECT copy constructor which notices that the source of the copy is a bad pointer (0xcccccccc). This causes the assert.

OBJECTREF's pointer mimicry isn't perfect. In certain cases, the checked build refuses to build legal-seeming constructs. We just have to work around this. A common case is casting an OBJECTREF to either a void* or a STRINGREF (we actually define a whole family of OBJECTREF-like pointers, for various interesting subclasses of objects.) The construct:

	//WRONG
	OBJECTREF o =  ...;
	LPVOID pv = (LPVOID)o;

compiles fine under retail but breaks under checked. The usual workaround is something like this:

	pv = (LPVOID)OBJECTREFToObject(o);

### <a name="2.1.10"></a>2.1.10 How to know if a function can trigger a GC

The GC behavior of every function in the source base must be documented in its contract. Every function must have a contract that declares one of the following:

	// If you call me, assume a GC can happen
	void Noisy()
	{
	    CONTRACTL
	    {
	        GC_TRIGGERS;
	    }
	    CONTRACTL_END
	}

or

	// If you call me and the thread is in cooperative mode, I guarantee no GC
	// will occur.
	void Quiet()
	{
	    CONTRACTL
	    {
	        GC_NOTRIGGER;
	    }
	    CONTRACTL_END
	}

A GC_NOTRIGGER function cannot:

- Allocate managed memory
- Call managed code
- Enter a GC-safe point
- Toggle the GC mode <sup>[1]</sup>
- Block for long periods of time
- Synchronize with the GC
- Explicitly trigger a GC (duh)
- Call any other function marked GC_TRIGGERS
- Call any other code that does these things

[1] With one exception: GCX_COOP (which effects a preemp->coop->preemp roundtrip) is permitted. The rationale is that GCX_COOP becomes a NOP if the thread was cooperative to begin with so it's safe to allow this (and necessary to avoid some awkward code in our product.)

**Note that for GC to be truly prevented, the caller must also ensure that the thread is in cooperative mode.** Otherwise, all the precautions above are in vain since any other thread can start a GC at any time. Given that, you might be wondering why cooperative mode is not part of the definition of GC_NOTRIGGER. In fact, there is a third thread state called GC_FORBID which is exactly that: GC_NOTRIGGER plus forced cooperative mode. As its name implies, GC_FORBID _guarantees_ that no GC will occur on any thread.

Why do we use GC_NOTRIGGERS rather than GC_FORBID? Because forcing every function to choose between GC_TRIGGERS and GC_FORBID is too inflexible given that some callers don't actually care about GC. Consider a simple class member function that returns the value of a field. How should it be declared? If you choose GC_TRIGGERS, then the function cannot be legally called from a GC_NOTRIGGER function even though this is perfectly safe. If you choose GC_FORBID, then every caller must switch to cooperative mode to invoke the function just to prevent an assert. Thus, GC_NOTRIGGER was created as a middle ground and has become far more pervasive and useful than GC_FORBID. Callers who actually need GC stopped will have put themselves in cooperative mode anyway and in those cases, GC_NOTRIGGER actually becomes GC_FORBID. Callers who don't care can just call the function and not worry about modes.

**Note:** There is no GC_FORBID keyword defined for contracts but you can simulate it by combining GC_NOTRIGGER and MODE_COOPERATIVE.

**Important:** The notrigger thread state is implemented as a counter rather than boolean. This is unfortunate as this should not be necessary and exposes us to nasty ref-counting style bugs. What is important that contracts intentionally do not support unscoped trigger/notrigger transitions. That is, a GC_NOTRIGGER inside a contract will **increment** the thread's notrigger count on entry to the function but on exit, **it will not decrement the count , instead it will restore the count from a saved value.** Thus, any _net_ changes in the trigger state caused within the body of the function will be wiped out. This is good unless your function was designed to make a net change to the trigger state. If you have such a need, you'll just have to work around it somehow because we actively discourage such things in the first place. Ideally, we'd love to replace that counter with a Boolean at sometime.

#### <a name="2.1.10.1"></a>2.1.10.1 GC_NOTRIGGER/TRIGGERSGC on a scope

Sometimes you want to mark a scope rather than a function. For that purpose, GC_TRIGGERS and TRIGGERSGC also exist as standalone holders. These holders are also visible to the static contract scanner.

	{
	    TRIGGERSGC();
	}

	{
	    GCX_NOTRIGGER();
	}

One difference between the standalone TRIGGERSGC and the contract GC_TRIGGERS: the standalone version also performs a "phantom" GC that poisons all unreachable OBJECTREFs. The contract version does not do this mainly for checked build perf concerns.

## <a name="2.2"></a>2.2 Are you using holders to track your resources?

### <a name="2.2.1"></a>2.2.1 What are holders and why are they important?

The CLR team has coined the name **holder** to refer to the infrastructure that encapsulates the common grunt work of writing robust **backout code**. **Backout code** is code that deallocate resources or restore CLR data structure consistency when we abort an operation due to an error or an asynchronous event. Oftentimes, the same backout code will execute in non-error paths for resources allocated for use of a single scope, but error-time backout is still needed even for longer lived resources.

Way back in V1, error paths were _ad-hoc._ Typically, they flowed through "fail:" labels where the backout code was accumulated.

Due to the no-compromise robustness requirements that the CLR Hosting model (with SQL Server as the initial customer) imposed on us in the .NET Framework v2 release, we have since become much more formal about backout. One reason is that we like to write backout that will execute if you leave the scope because of an exception. We also want to centralize policy regarding exceptions occurring inside backout. Finally, we want an infrastructure that will discourage developer errors from introducing backout bugs in the first place.

Thus, we have centralized cleanup around C++ destructor technology. Instead of declaring a HANDLE, you declare a HandleHolder. The holder wraps a HANDLE and its destructor closes the handle no matter how control leaves the scope. We have already implemented standard holders for common resources (arrays, memory allocated with C++ new, Win32 handles and locks.) The Holder mechanism is extensible so you can add new types of holders as you need them.

### <a name="2.2.2"></a>2.2.2 An example of holder usage

The following shows explicit backout vs. holders:

**Wrong**

	HANDLE hFile = ClrCreateFile(szFileName, GENERIC_READ, ...);
	if (hFile == INVALID_HANDLE_VALUE) {
	    COMPlusThrow(...);
	}

	DWORD dwFileLen = SafeGetFileSize(hFile, 0);
	if (dwFileLen == 0xffffffff) {
	    CloseHandle(hFile);
	    COMPlusThrow(...);
	}
	CloseHandle(hFile);
	return S_OK;

**Right**

	HandleHolder hFile(ClrCreateFile(szFileName, GENERIC_READ, ...));
	if (hFile == INVALID_HANDLE_VALUE)
	    COMPlusThrow(...);

	DWORD dwFileLen = SafeGetFileSize(hFile, 0);
	if (dwFileLen == 0xffffffff)
	    COMPlusThrow(...);

	return S_OK;

The difference is that hFile is now a HandleHolder rather than a HANDLE and that there are no more explicit CloseHandle calls. That call is now implicit in the holder's destructor and executes no matter how control leaves the scope.

HandleHolder exposes operator overloads so it can be passed to APIs expecting HANDLEs without casting and be compared to INVALID_HANDLE_VALUE. The wrapper knows that INVALID_HANDLE_VALUE is special and won't attempt to close it. The holder also has some safety features. If you declare it without initializing it, it will be autoinitialized to INVALID_HANDLE_VALUE. If you assign a new value to the holder, the current value will be destructed before it is overwritten.

Suppose you want to auto-close the handle if an error occurs but keep the handle otherwise? Call SuppressRelease() on the holder object. The underlying handle can still be pulled out of the holder but the destructor will no longer close it.

**Wrong:**

	HANDLE hFile = ClrCreateFile(szFileName, GENERIC_READ, ...);
	if (hFile == INVALID_HANDLE_VALUE) {
	    COMPlusThrow(...);
	}
	if (FAILED(SomeOperation())) {
	    CloseHandle(hFile);
	    COMPlusThrow(...);
	}
	return hFile;

**Right:**

	HandleHolder hFile = ClrCreateFile(szFileName, GENERIC_READ, ...);
	if (hFile == INVALID_HANDLE_VALUE) {
	    COMPlusThrow(...);
	}
	if (FAILED(SomeOperation())) {
	    COMPlusThrow(...);
	}
	// No failures allowed after this!
	hFile.SuppressRelease();
	return hFile;

### <a name="2.2.3"></a>2.2.3 Common Features of Holders

All holders, no matter how complex or simple, offer these basic services:

- When the holder goes out of scope, via an exception or normal flow, it invokes a RELEASE function supplied by the holder's designer. The RELEASE function is responsible for the cleanup.
- A holder declared without an explicit initializer will be initialized to a default value. The precise value of the default is supplied by the holder's designer.
- Holders know about "null" values. The holder guarantees never to call RELEASE or ACQUIRE on a null value. The designer can specify any number of null values or no null value at all.
- Holders expose a public SuppressRelease() method which eliminates the auto-release in the destructor. Use this for conditional backout.
- Holders also support an ACQUIRE method when a resource can be meaningfully released and reacquired (e.g. locks.)

In addition, some holders derive from the Wrapper class. Wrappers are like holders but also implement operator overloads for type casting, assignment, comparison, etc. so that the holder proxies the object smart-pointer style. The HandleHolder object is actually a wrapper.

### <a name="2.2.4"></a>2.2.4 Where do I find a holder?

First, look for a prebaked holder that does what you want. Some common ones are described below.

If no existing holder fits your need, make one. If it's your first holder, start by reading [src\inc\holder.h][holder.h]. Decide if you want a holder or a wrapper. If you don't do much with a resource except acquire and release it, use a holder. Otherwise, you want the wrapper since its overloaded operators make it much easier to replace the resource variable with the wrapper.

[holder.h]: https://github.com/dotnet/runtime/blob/main/src/coreclr/inc/holder.h

Instantiate the holder or wrapper template with the required parameters. You must supply the data type being managed, the RELEASE function, the default value for uninitialized constructions, the IS_NULL function and the ACQUIRE function. Unless you're implementing a critical section holder, you can probably supply a NOP for ACQUIRE . Most resources can't be meaningfully released and reacquired so it's easier to allocate the resource outside the holder and pass it in through its constructor. For convenience, [holder.h][holder.h] defines a DoNothing<Type> template that creates a NOP ACQUIRE function for any given resource type. There are also convenience templates for writing RELEASE functions. See [holder.h][holder.h] for their definitions and examples of their use.

Publish the holder in the most global header file possible. [src\inc\holder.h][holder.h] is ideal for OS-type resources. Otherwise, put it in the header file that owns the type being managed.

### <a name="2.2.5"></a>2.2.5 Can I bake my own holder?

When we first put holders into the code, we encouraged developers to inherit from the base holder class rather than writing their own. But the reality has been that many holders only need destruction and SuppressRelease() and it's proven easier for developers to write them from scratch rather than try to master the formidable C++ template magic that goes on in [holder.h][holder.h] It is better that you write your own holders than give up the design pattern altogether because you don't want to tackle [holder.h].

But however you decide to implement it, if you call your object a "holder", please make sure its external behavior conforms to the conventions listed above in "Common Features of Holders."

### <a name="2.2.6"></a>2.2.6 What if my backout code throws an exception?

All holders wrap an implicit NOTHROW contract around your backout code. Thus, you must write your backout code only using primitives that are guaranteed not to throw. If you absolutely have no choice but to violate this (say, you're calling Release() on a COM object that you didn't write), you must catch the exception yourself.

This may sound draconian but consider the real implications of throwing out of your backout code. Backout code, by definition, is code that must complete when throwing out of a block. If it didn't complete, there is no way to salvage the situation and still meet our reliability goals. Either something leaked or CLR state was left inconsistent.

Often, you can avoid failures in backout code by designing a better data structure. For example, implementers of common data structures such as hash tables and collections should provide backout holders for undoing operations as inserts. When creating globally visible data structures such as EEClass objects, you should initialize the object in private and allocate everything needed before "publishing it." In some cases, this may require significant rethinking of your data structures and code. But the upshot is that you won't have to undo global data structure changes in backout code.

### <a name="2.2.7"></a>2.2.7 Pay attention to holder initialization semantics

Holders consistently release on destruction – that's their whole purpose. Sadly, we are not so consistent when it comes the initialization semantics. Some holders, such as the Crst holder, do an implicit Acquire on initialization. Others, such as the ComHolder do not (initializing a ComHolder does _not_ do an AddRef.) The BaseHolder class constructor leaves it up to the holder designer to make the choice. This is an easy source of bugs so pay attention to this.

### <a name="2.2.8"></a>2.2.8 Some generally useful prebaked holders

#### <a name="2.2.8.1"></a>2.2.8.1 New'ed memory

**Wrong:**

	Foo *pFoo = new Foo();
	delete pFoo;

**Right:**

	NewHolder<Foo> pFoo = new Foo();

#### <a name="2.2.8.2"></a>2.2.8.2 New'ed array

**Wrong:**

   	Foo *pFoo = new Foo[30];
	delete pFoo;

**Right:**

	NewArrayHolder<Foo> pFoo = new Foo[30];

#### <a name="2.2.8.3"></a>2.2.8.3 COM Interface Holder

**Wrong:**

	IFoo *pFoo = NULL;
	FunctionToGetRefOfFoo(&pFoo);
	pFoo->Release();

**Right:**

	ComHolder<IFoo> pFoo;  // declaring ComHolder does not insert AddRef!
	FunctionToGetRefOfFoo(&pFoo);

#### <a name="2.2.8.4"></a>2.2.8.4 Critical Section Holder

**Wrong:**

	pCrst->Enter();
	pCrst->Leave();

**Right:**

	{
	    CrstHolder(pCrst);	//implicit Enter
	}			       		//implicit Leave

## <a name="2.3"></a>2.3 Does your code follow our OOM rules?

### <a name="2.3.1"></a>2.3.1 What is OOM and why is it important?

OOM stands for "Out of Memory." The CLR must be fully robust in the face of OOM errors. For us, OOM is not an obscure corner case. SQL Server runs its processes in low-memory conditions as normal practice. OOM exceptions are a regular occurrence when hosted under SQL Server and we are required to handle every single one correctly.

This means that:

- Any operation that fails due to an OOM must allow future retries. This means any changes to global data structures must be rolled back and OOM exceptions cannot be cached.
- OOM failures must be distinguishable from other error results. OOM's must never be transformed into some other error code. Doing so may cause some operations to cache the error and return the same error on each retry.
- Every function must declare whether or not it can generate an OOM error. We cannot write OOM-safe code if we have no way to know what calls can generate OOM's. This declaration is done by the INJECT_FAULT and FORBID_FAULT contract annotations.

### <a name="2.3.2"></a>2.3.2 Documenting where OOM's can happen

Sometimes, a code sequence requires that no opportunities for OOM occur. Backout code is the most common example. This can become hard to maintain if the code calls out to other functions. Because of this, it is very important that every function document in its contract whether or not it can fail due to OOM. We do this using the (poorly named) INJECT_FAULT and FORBID_FAULT annotations.

To document that a function _can_ fail due to OOM:

**Runtime-based (preferred)**

	void AllocateThingie()
	{
	    CONTRACTL
	    {
	        INJECT_FAULT(COMPlusThrowOM(););
	    }
	    CONTRACTL_END
	}

**Static**

	void AllocateThingie()
	{
	    STATIC_CONTRACT_FAULT;
	}

To document that a function _cannot_ fail due to OOM:

**Runtime-based (preferred)**

	BOOL IsARedObject()
	{
	    CONTRACTL
	    {
	        FORBID_FAULT;
	    }
	    CONTRACTL_END
	}

**Static**

	BOOL IsARedObject()
	{
	    STATIC_CONTRACT_FORBID_FAULT;
	}

INJECT_FAULT()'s argument is the code that executes when the function reports an OOM. Typically this is to throw an OOM exception or return E_OUTOFMEMORY. The original intent for this was for our OOM fault injection test harness to insert simulated OOM's at this point and execute this line. At the moment, this argument is ignored but we may still employ this fault injection idea in the future so please code it appropriately.

The CLR asserts if you invoke an INJECT_FAULT function under the scope of a FORBID_FAULT. All our allocation functions, including the C++ new operator, are declared INJECT_FAULT.

#### <a name="2.3.2.1"></a>2.3.2.1 Functions that handle OOM's internally

Sometimes, a function handles an internal OOM without needing to notify the caller. For example, perhaps the additional memory was used to implement an internal cache but your function can still do its job without it. Or perhaps the function is a logging function in which case, it can silently NOP – the caller doesn't care. In such cases, wrap the allocation in the FAULT_NOT_FATAL holder which temporarily lifts the FORBID_FAULT state.

	{
	    FAULT_NOT_FATAL();
	    pv = new Foo();
	}

FAULT_NOT_FATAL() is almost identical to a CONTRACT_VIOLATION() but the name indicates that it is by design, not a bug. It is analogous to TRY/CATCH for exceptions.

#### <a name="2.3.2.2"></a>2.3.2.2 OOM state control outside of contracts

If you wish to set the OOM state for a scope rather than a function, use the FAULT_FORBID() holder. To test the current state, use the ARE_FAULTS_FORBIDDEN() predicate.

#### <a name="2.3.2.3"></a>2.3.2.3 Remember...

- Do not use INJECT_FAULT to indicate the possibility of non-OOM errors such as entries not existing in a hash table or a COM object not supporting an interface. INJECT_FAULT indicates OOM errors and no other type.
- Be very suspicious if your INJECT_FAULT() argument is anything other than throwing an OOM exception or returning E_OUTOFMEMORY. OOM errors must distinguishable from other types of errors so if you're merely returning NULL without indicating the type of error, you'd better be a simple memory allocator or some other function that will never fail for any reason other than an OOM.
- THROWS and INJECT_FAULT correlate strongly but are independent. A NOTHROW/INJECT_FAULT combo might indicate a function that returns HRESULTs including E_OUTOFMEMORY. A THROWS/FORBID_FAULT however indicate a function that can throw an exception but not an OutOfMemoryException. While theoretically possible, such a contract is probably a bug.

## <a name="2.4"></a>2.4 Are you using SString and/or the safe string manipulation functions?

The native C implementation of strings as raw char* buffers is a well-known breeding ground for buffer overflow bugs. While acknowledging that there's still a ton of legacy char*'s in the code, new code and new data structures should use the SString class rather than raw C strings whenever possible.

### <a name="2.4.1"></a>2.4.1 SString

SString is the abstraction to use for unmanaged strings in CLR code. It is important that as much code is possible uses the SString abstraction rather than raw character arrays, because of the danger of buffer overrun related to direct manipulation of arrays. Code which does not use SString must be manually reviewed for the possibility of buffer overrun or corruption during every security review.

This section will provide an overview for SString. For specific details on methods and use, see the file [src\inc\sstring.h][sstring.h]. SString has been in use in our codebase for quite a few years now so examples of its use should be easy to find.

[sstring.h]: https://github.com/dotnet/runtime/blob/main/src/coreclr/inc/sstring.h

An SString object represents a Unicode string. It has its own buffer which it internally manages. The string buffer is typically not referenced directly by user code; instead the string is manipulated indirectly by methods defined on SString. Ultimately there are several ways to get at the raw string buffer if such functionality is needed to interface to existing APIs. But these should be used only when absolutely necessary.

When SStrings are used as local variables, they are typically used via the StackSString type, which uses a bit of stack space as a preallocated buffer for optimization purposes. When SStrings are use in structures, the SString type may be used directly (if it is likely that the string will be empty), or through the InlineSString template, which allows an arbitrary amount of preallocated space to be declared inline in the structure with the SString. Since InlineSStrings and StackSStrings are subtypes of SString, they have the same API, and can be passed wherever an SString is required.

As parameters, SStrings should always be declared as reference parameters. Similarly, SStrings as return value should also use a "by reference" style.

An SString's contents can be initialized by a "raw" string, or from another SString. A WCHAR based string is assumed to be Unicode, but for a CHAR based string, you must specify the encoding by explicitly invoking one of the tagged constructors with the appropriate encoding (currently Utf8, Ansi, or Console).

In addition, you can specially call out string literals with the Literal tag – this allows the SString implementation to make some additional optimizations during copying and allocation operations. It's important to only use this take for actual read-only compiler literals. Never use them for other strings which might be freed or modified in the future, even if you believe the SString's lifetime will be shorter than the buffer's.

SStrings' contents are typically operated on through use of one of the iterators provided. A SString::CIterator (obtained from a const SString), is used to look at but not change the string. A SString::Iterator (obtained from a non-const SString) should be used when the string will be modified. Note that it is a slightly heavier operation to create a non-const Iterator, so you should use CIterator when appropriate.

Either kind of iterator acts like (through the magic of C++ operator overloading) a pointer into the string buffer. Performance is also similar, although it may be slightly reduced. An iterator also has similar lifetime constraints to a buffer pointer – if an SString changes sizes, the existing iterators on it cease to be valid. (Fortunately the iterator infrastructure provides some explicit checks to aid in enforcement of this constraint, in a checked build.)

If you need to use the string in the context of an external API (either to get the string's contents to pass out, or to use the SString as a buffer to receive a return result.), you may use one of the conversion APIs. Read-only use of the buffer is provided via a simple API; however if you need to write to the string's buffer, you must use an Open/Close call pair around the operation.

For easy creation of an SString for a string literal, use the SL macro. This can be used around either a normal (ASCII characters only) or wide string constant.

## <a name="2.5"></a>2.5 Are you using safemath.h for pointer and memory size allocations?

Integer overflow bugs are an insidious source of buffer overrun vulnerabilities.Here is a simple example of how such a bug can occur:

	void *pInput = whatever;
	UINT32 cbSizeOfData = GetSizeOfData();
	UINT32 cbAllocSize = SIZE_OF_HEADER + cbSizeOfData;
	void *pBuffer = Allocate(cbAllocSize);
	memcpy(pBuffer + SIZE_OF_HEADER, pInput, cbSizeOfData);

If GetSizeOfData() obtains its result from untrusted data, it could return a huge value just shy of UINT32_MAX. Adding SIZE_OF_HEADER causes a silent overflow, resulting in a very small (and incorrect) value being passed to Allocate() which dutifully returns a short buffer. The memcpy, however, copies a huge number of bytes and overflows the buffer.

The source of the bug is clear. The code should have checked if adding SIZE_OF_HEADER and cbSizeOfData overflowed before passing it to Allocate().

We have now standardized on an infrastructure for performing overflow-safe arithmetic on key operations such as calculating allocation sizes. This infrastructure lives in [clr\src\inc\safemath.h][safemath.h].

The _safe_ version of the above code follows:

	#include "safemath.h"

	void *pInput = whatever;
	S_UINT32 cbSizeOfData = S_UINT32(GetSizeOfData());
	S_UINT32 cbAllocSize =  S_UINT32(SIZE_OF_HEADER) + cbSizeOfData;
	if (cbAllocSize.IsOverflow())
	{
	    return E_OVERFLOW;
	}
	void *pBuffer = Allocate(cbAllocSize.Value());
	memcpy(pBuffer + SIZE_OF_HEADER, pInput, cbSizeOfData);

As you can see, the transformation consists of the following:

- Replace the raw C++ integer type with the "S_" version.
- Do the arithmetic as usual.
- Call IsOverflow() on the _final_ result to see if an overflow occurred anytime during the calculations. It's not necessary to check intermediate results if multiple arithmetic operations are chained. [Safemath.h][safemath.h] will propagate the overflow state through the entire chain of operations.
- If IsOverflow() returned false, then call Value() on the final result to get the raw integer back. Otherwise, there's no value to be returned – invoke your error handling code.

As you'd expect, Value() asserts if IsOverflow() is true.

As you might _not_ expect, Value() also asserts if you never called IsOverflow() to check – whether or not the result actually overflowed. This guarantees you won't forget the IsOverflow() check. If you didn't check, Value() won't give you the result.

Currently, the "S_" types are available only for unsigned ints and SIZE_T. Check in [safemath.h][safemath.h] for what's currently defined. Also, only addition and multiplication are supported although other operations could be added if needed.

**Key Takeaway: Use safemath.h for computing allocation sizes and pointer offsets.** Don't rely on the fact that the caller may have already validated the data. You never know what new paths might be added to your vulnerable code.

**Key Takeaway: If you're working on existing code that does dynamic memory allocation, check for this bug.**

**Key Takeaway: Do not roll your own overflow checks. Always use safemath.h.** Writing correct overflow-safe arithmetic code is harder than you might think (take a look at the implementation in [safemath.h][safemath.h] if you don't believe me.) Every unauthorized version is another security hotspot that has to be watched carefully. If safemath.h doesn't support the functionality you need, please get the functionality added to safemath.h rather than creating a new infrastructure.

**Key Takeaway: Don't let premature perf concerns stop you from using safemath.h.** Despite the apparently complexity, the optimized codegen for this helper is very efficient and in most cases, at least as efficient as any hand-rolled version you might be tempted to create.

**Note:** If you've worked on other projects that use the SafeInt class, you might be wondering why we don't do that here. The reason is that we needed something that could be used easily from exception-intolerant code.

## <a name="2.6"></a>2.6 Are you using the right type of Critical Section?

Synchronization in the CLR is challenging because we must support the strong requirements of the CLR Hosting API. This has two implications:

- Hosting availability goals require that we eliminate all races and deadlocks. We need to maintain a healthy process under significant load for weeks and months at a time. Miniscule races will eventually be revealed.
- Hosting requires that we often execute on non-preemptively scheduled threads. If we block a non-preemptively scheduled thread, we idle a CPU and possibly deadlock the process.

### <a name="2.6.1"></a>2.6.1 Use only the official synchronization mechanisms

First, the most important rule. If you learn nothing else here, learn this:

> DO NOT BUILD YOUR OWN LOCK.

A CLR host must be able to detect and break deadlocks. To do this, it must know at all times who owns locks and who is waiting to acquire a lock. If you bypass a host using your own mechanisms, or if even you use a host's events to simulate a lock, you will defeat a host's ability to trace and break deadlocks. You must also eschew the OS synchronization services such as CRITICAL_SECTION.

We have the following approved synchronization mechanisms in the CLR:

1. **Crst:** This is our replacement for the Win32 CRITICAL_SECTION. We should be using Crst's pretty much everywhere we need a lock in the CLR.
2. **Events:** A host can provide event handles that replace the Win32 events.
3. **InterlockedIncrement/Decrement/CompareExchange:** These operations may be used for lightweight ref-counting and initialization scenarios.

Make sure you aren't using events to build the equivalent of a critical section. The problem with this is that we cannot identify the thread that "owns" the critical section and hence, the host cannot trace and break deadlocks. In general, if you're creating a situation that could result in a deadlock, even if only due to bad user code, you must ensure that a CLR host can detect and break the deadlock.

### <a name="2.6.2"></a>2.6.2 Using Crsts

The Crst class ([crst.h][crst.h]) is a replacement for the standard Win32 CRITICAL_SECTION. It has all the properties and features of a CRITICAL_SECTION, plus a few extra nice features. We should be using Crst's pretty much everywhere we need a lock in the CLR.

Crst's are also used to implement our locking hierarchy. Every Crst is placed into a numbered group, or _level_. A thread can only request a Crst whose level is lower than any Crst currently held by the thread. I.e., if a thread currently holds a level 3 Crst, it can try to enter a level 2 Crst, but not a level 4 Crst, nor a different level 3 Crst. This prevents the cyclic dependencies that lead to deadlocks.

We used to assign levels manually, but this leads to problems when it comes time to add a new Crst type or modify an existing one. Since the assignment of levels essentially flattens the dependencies between Crst types into one linear sequence we have lost information on which Crst types really depend on each other (i.e. which types ever interact by being acquired simultaneously on one thread and in which order). This made it hard to determine where to rank a new lock in the sequence.

Instead we now record the explicit dependencies as a set of rules in the src\inc\CrstTypes.def file and use a tool to automatically assign compatible levels to each Crst type. See CrstTypes.def for a description of the rule syntax and other instructions for updating Crst types.

[crst.h]: https://github.com/dotnet/runtime/blob/main/src/coreclr/vm/crst.h

### <a name="2.6.3"></a>2.6.3 Creating Crsts

To create a Crst:

	Crst *pcrst = new Crst(type [, flags]);

Where "type" is a member of the CrstType enumeration (defined in the automatically generated src\inc\CrstTypes.h file). These types indicate the usage of the Crst, particularly with regard to which other Crsts may be obtained simultaneously, There is a direct mapping for the CrstType to a level (see CrstTypes.h) though the reverse is not true.

Don't create static instances of Crsts<sup>[2]</sup>. Use CrstStatic class for this purpose, instead.

Simply define a CrstStatic as a static variable, then initialize the CrstStatic when appropriate:

	g_GlobalCrst.Init(type"tag", level);

A CrstStatic must be destroyed with the Destroy() method as follows:

	g_GlobalCrst.Destroy();

[2]: In fact, you should generally avoid use of static instances that require construction and destruction. This can have an impact on startup time, it can affect our shutdown robustness, and it will eventually limit our ability to recycle the CLR within a running process.

### <a name="2.6.4"></a>2.6.4 Entering and Leaving Crsts

To enter or leave a crst, you must wrap the crst inside a CrstHolder. All operations on crsts are available only through the CrstHolder. To enter the crst, create a local CrstHolder and pass the crst as an argument. The crst is automatically released by the CrstHolder's destructor when control leaves the scope either normally or via an exception:

	{
	    CrstHolder ch(pcrst);	// implicit enter

	    ... do your thing... may also throw...

	}							// implicit leave

**You can only enter and leave Crsts in preemptive GC mode.** Attempting to enter a Crst in cooperative mode will forcibly switch your thread into preemptive mode.

If you need a Crst that you can take in cooperative mode, you must pass a special flag to the Crst constructor to do so. See the information about CRITSECT_UNSAFE_\* flags below. You will also find information about why it's preferable not to take Crsts in cooperative mode.

You can also manually acquire and release crsts by calling the appropriate methods on the holder:

	{
	    CrstHolder ch(pcrst);	// implicit enter

	    ...
	    ch.Release();		// temporarily leave
	    ...
	    ch.Acquire();		// temporarily enter

	}					// implicit leave

Note that holders do not let you nest Acquires or Releases. You will get an assert if you try. Introduce a new scope and a new holder if you need to do this.

If you need to create a CrstHolder without actually entering the critical section, pass FALSE to the holder's "take" parameter like this:

	{
	    CrstHolder ch(pcrst, FALSE);	// no implicit enter

	    ...
	}									// no implicit leave

If you want to exit the scope without leaving the Crst, call SuppressRelease() on the holder:

	{
	    CrstHolder ch(pcrst);	// implicit enter
	    ch.SuppressRelease();
	}							// no implicit leave

### <a name="2.6.5"></a>2.6.5 Other Crst Operations

If you want to validate that you own no other locks at the same or lower level, assert the debug-only IsSafeToTake() method:

	_ASSERTE(pcrst->IsSafeToTake());

Entering a crst always calls IsSafeToTake() for you but calling it manually is useful for functions that acquire a lock only some of the time.

### <a name="2.6.6"></a>2.6.6 Advice on picking a level for your Crst

The point of giving your critical section a level is to help us prevent deadlocks by detecting cycles early in the development process. We try to group critical sections that protect low-level data structures and don't use other services into the lower levels, and ones that protect higher-level data structures and broad code paths into higher levels.

If your lock is only protecting a single data structure, and if the methods accessing that data structure don't call into other CLR services that could also take locks, then you should give your lock the lowest possible level. Using the lowest level ensures that someone can't come along later and modify the code to start taking other locks without violating the leveling. This will force us to consider the implications of taking other locks while holding your lock, and in the end will lead to better code.

If your lock is protecting large sections of code that call into many other parts of the CLR, then you need to give your lock a level high enough to encompass all the locks that will be taken. Again, try to pick a level as low as possible.

Add a new definition for your level rather than using an existing definition, even if there is an existing definition with the level you need. Giving each lock its own level in the enum will allow us to easily change the levels of specific locks at a later time.

### <a name="2.6.7"></a>2.6.7 Can waiting on a Crst generate an exception?

It depends.

If you initialize the crst as CRST_HOST_BREAKABLE, any attempt to acquire the lock can trigger an exception (intended to kill your thread to break the deadlock.) Otherwise, you are guaranteed not to get an exception or failure. Regardless of the flag setting, releasing a lock will never fail.

You can only use a non host-breakable lock if you can guarantee that the lock will never participate in a deadlock. If you cannot guarantee this, you must use a host-breakable lock and handle the exception. Otherwise, a CLR host will not be able to break deadlocks cleanly.

There are several ways we enforce this.

1. A lock that is CRST_UNSAFE_SAMELEVEL must be HOST_BREAKABLE: SAMELEVEL allows multiple locks at the same level to be taken in any order. This sidesteps the very deadlock avoidance that leveling provides.
2. You cannot call managed code while holding a non-hostbreakable lock. We assume that you can't guarantee what the managed code will do. Thus, you can't guarantee that the managed code won't acquire user locks, which don't participate at all in the leveling scheme. User locks can be acquired in any order and before or after any internal CLR locks. Hence, you cannot guarantee that the lock won't participate in a deadlock cycle along with the user locks.

You may be wondering why we invest so much effort into the discipline of deadlock avoidance, and then also require everyone to tolerate deadlock breaking by the host. Sometimes we are unhosted, so we must avoid deadlocks. Some deadlocks involve user code (like class constructors) and cannot be avoided. Some exceptions from lock attempts are due to resource constraints, rather than deadlocks.

### <a name="2.6.8"></a>2.6.8 CRITSECT_UNSAFE Flags

By default, Crsts can only be acquired and released in preemptive GC mode and threads can only own one lock at any given level at a given time. Some locks need to bypass these restrictions. To do so, you must pass the appropriate flag when you create the critical section. (This is the optional third parameter to the Crst constructor.)

**CRST_UNSAFE_COOPGC**

If you pass this flag, it says that your Crst will always be taken in Cooperative GC mode. This is dangerous because you cannot allow a GC to occur while the lock is held<sup>[3]</sup>. Entering a coop mode lock puts your thread in ForbidGC mode until you leave the lock. For handy reference, some of the things you can't do in ForbidGC mode are:

- Allocate managed memory
- Call managed code
- Enter a GC-safe point
- Toggle the GC mode
- Block for long periods of time
- Synchronize with the GC
- Call any other code that does these things

**CRST_UNSAFE_ANYMODE**

If you pass this flag, your Crst can be taken in either Cooperative or Preemptive mode. The thread's mode will not change as a result of taking the lock, however, it will be placed in a GCNoTrigger state. We have a set of assertions to try to ensure that you don't cause problems with the GC due to this freedom. These assertions are the famous "Deadlock situation" messages from our V1 code base. However, it's important to realize that these assertions do not provide full safety, because they rely on code coverage to catch your mistakes.

Note that CRST_UNSAFE_COOPGC and CRST_UNSAFE_ANYMODE are mutually exclusive despite being defined as "or'able" bits.

**CRST_UNSAFE_SAMELEVEL**

All Crsts are ordered to avoid deadlock. The CRST_UNSAFE_SAMELEVEL flag weakens this protection by allowing multiple Crsts at the same level to be taken in any order. This is almost always a bug.

I know of one legitimate use of this flag. It is the Crst that protects class construction (.cctors). The application can legally create cycles in class construction. The CLR has rules for breaking these cycles by allowing classes to see uninitialized data under well-defined circumstances.

In order to use CRST_UNSAFE_SAMELEVEL, you should write a paragraph explaining why this is a legal use of the flag. Add this explanation as a comment to the constructor of your Crst.

Under no circumstances may you use CRST_UNSAFE_SAMELEVEL for a non-host-breakable lock.

[3] More precisely, you cannot allow a GC to block your thread at a GC-safe point. If it does, the GC could deadlock because the GC thread itself blocks waiting for a third cooperative mode thread to reach its GC-safe point... which it can't do because it's trying to acquire the very lock that your first thread owns. This wouldn't be an issue if acquiring a coop-mode lock was itself a GC-safe point. But too much code relies on this not being a GC-safe point to fix this easily

### <a name="2.6.9"></a>2.6.9 Bypassing leveling (CRSTUNORDEREDnordered)

CrstUnordered (used in rules inside CrstTypes.def) is a special level that says that the lock does not participate in any of the leveling required for deadlock avoidance. This is the most heinous of the ways you can construct a Crst. Though there are still some uses of this in the CLR, it should be avoided by any means possible.

### <a name="2.6.10"></a>2.6.10 So what _are_ the prerequisites and side-effects of entering a Crst?

The following matrix lists the effective contract and side-effects of entering a crst for all combinations of CRST_HOST_BREAKABLE and CRST_UNSAFE_\* flags. The SAMELEVEL flag has no effect on any of these parameters.

|                     | Default                                                                                   | CRST_HOST_BREAKABLE                                                                      |
| ------------------- | ----------------------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------- |
| Default             | NOTHROW<br> FORBID_FAULT<br>GC_TRIGGERS<br>MODE_ANY<br>(switches thread to preemptive)    | THROWS<br>INJECT_FAULT<br>GC_TRIGGERS<br>MODE_ANY<br>(switches thread to preemptive)     |
| CRST_UNSAFE_COOPGC  | NOTHROW<br>FORBID_FAULT<br>GC_NOTRIGGER<br>MODE_COOP<br>(puts thread in GCNoTrigger mode) | THROWS<br>INJECT_FAULT<br>GC_NOTRIGGER<br>MODE_COOP<br>(puts thread in GCNoTrigger mode) |
| CRST_UNSAFE_ANYMODE | NOTHROW<br>FORBID_FAULT<br>GC_NOTRIGGER<br>MODE_ANY<br>(puts thread in GCNoTrigger mode)  | THROWS<br>INJECT_FAULT<br>GC_NOTRIGGER<br>MODE_ANY<br>(puts thread in GCNoTrigger mode)  |

### <a name="2.6.11"></a>2.6.11 Using Events and Waitable Handles

In typical managed app scenarios, services like WszCreateEvent are thin wrappers over OS services like ::CreateEvent. But in hosted scenarios, these calls may be redirected through an abstraction layer to the host. If that's the case, they may return handles that behave somewhat like OS events, but do not support coordination with unmanaged code. Nor can we provide WaitForMultipleHandles support on these handles. You are strictly limited to waiting on a single handle.

If you need to coordinate with unmanaged code, or if you need to do WaitForMultipleHandles ANY/ALL, you will have to avoid WszCreateEvent. If you really know what you are doing, go directly to the OS to obtain these handles. Everyone else should seek advice from someone who thoroughly understands the implications to our host. Obviously the general rule is that everyone should go through our hosted abstraction.

Sometimes you might find yourself building the equivalent of a critical section, but using an event directly. The problem here is that we cannot identify the thread that owns the lock, because the owner isn't identified until they "leave'" the lock by calling SetEvent or Pulse. Consider whether a Crst might be more appropriate.

### <a name="2.6.12"></a>2.6.12 Do not get clever with "lockless" reader-writer data structures

Earlier, we had several hashtable structures that attempted to be "clever" and allow lockless reading. Of course, these structures didn't take into account multiprocessors and the other memory models. Even on single-proc x86, stress uncovered exotic race conditions. This wasted a lot of developer time debugging stress crashes.

We finally stopped being clever and added proper synchronization, with no serious perf degradation.

So if you are tempted to get clever in this way again, **stop and do something else until the urge passes.**

### <a name="2.6.13"></a>2.6.13 Yes, your thread could be running non-preemptively!

Under hosted scenarios, your thread could actually be scheduled non-preemptively (do not confuse this with "GC preemptive mode.".) Blocking a thread without yielding back to the host could have consequences ranging from CPU starvation (perf) to an actual deadlock. You are particularly vulnerable when calling OS apis that block.

Unfortunately, there is no official list of "safe" OS apis. The safest approach is to stick to the officially approved synchronization mechanisms documented in this chapter and be extra careful when invoking OS api.

### <a name="2.6.14"></a>2.6.14 Dos and Don'ts for Synchronization

- Don't build your own lock or use OS locks. Only use Crst or host events and waitable handles. A host must know who owns what to detect and break deadlocks.
- Don't use events to simulate locks or any other synchronization mechanism that could lead to deadlocks. Again, if a host doesn't know about a deadlock situation, it can't break it.
- Don't use a CRITICAL_SECTION anywhere inside the CLR. Use Crst. One exception. If there are bootstrap or shutdown issues that require synchronization beyond the period when the CLR is initialized, you may use CRITICAL_SECTION (e.g. g_LockStartup).
- Do pick the lowest possible level for your Crst.
- Don't create static instances of Crst. Use CrstStatic instead.
- Do assert IsSafeToTake() if your function only takes a crst some of the time.
- Do use the default Crst rather than the CRST_UNSAFE_\* alternatives. They're  named that for a reason.
- Do choose correctly between host-breakable and non-breakable crsts. Crsts that don't protect calls to managed code and participate fully in the leveling scheme can be non-breakable. Otherwise, you must use breakable.
- Don't take locks in cooperative mode if you can avoid it. This can delay or stall the GC. You are in a ForbidGC region the entire time you hold the lock.
- Don't block a thread without yielding back to the host. Your "thread" may actually be a nonpreemptive thread. Always stick to the approved synchronization primitives.
- Do document your locking model. If your locking model involves protecting a resource with a critical section, maybe you don't have to mention that in a comment. But if you have an elaborate mechanism where half your synchronization comes from GC guarantees and being in cooperative mode, while the other half is based on taking a spin lock in preemptive mode – then you really need to write this down. Nobody (not even you) can debug or maintain your code unless you have left a detailed comment.

## <a name="2.7"></a>2.7 Are you making hidden assumptions about the order of memory writes?

_Issues: X86 processors have a very predictable memory order that 64-bit chips or multiprocs don't observe. We've gotten burned in the past because of attempts to be clever at writing thread-safe data structures without crsts. The best advice here is "don't be so clever, the perf improvements usually don't justify the risk." (look for Vance's writeup on memory models for a start.) _

## <a name="2.8"></a>2.8 Is your code compatible with managed debugging?

The managed debugging services have some very unique properties in the CLR, and take a heavy dependency on the rest of the system. This makes it very easy to break managed debugging without even touching a line of debugger code. Here are some key trivia and tips to help you play well with the managed-debugging services.

Be aware of things that make the debugger subsystem different than other subsystems:

- The debugger runs mostly out-of-process.
- The debugger generally inspects things at a very intimate level. For example, the debugger can see private fields, the offsets of those fields, and what registers an object may be stored in.
- The debugger needs to be able to stop and synchronize the debuggee, in a similar way as the GC. That means all those GC-contracts, GC-triggers, GC-toggling, etc, may heavily affect the debugger's synchronization too.
- Whereas most subsystems can just patiently wait for a GC to complete, the debugger will need to do complicated work during a GC-suspension.

Here are some immediate tips for working well with the managed-debugging services:

- Check if you need to DAC-ize your code for debugging! DACizing means adding special annotations so that the debugger can re-use your code to read key CLR data structures from out-of-process. This is especially applicable for code that inspects runtime data structures (running callstacks; inspecting a type; running assembly or module lists; enumerating jitted methods; doing IP2MD lookups; etc). Code that will never be used by the debugger does not have to be DAC-ized. However, when in doubt, it's safest to just DAC-ize your code.
- Don't disassemble your own code. Breakpoints generally work by writing a "break opcode" (int3 on x86) into the instruction stream. Thus when you disassemble your code, you may get the breakpoint opcode instead of your own original opcode. Currently, we have to workaround this by having all runtime disassembly ask the debugger if there's a break opcode at the targeted address, and that's painful.
- Avoid self-modifying code. Avoid this for the same reasons that you shouldn't disassemble your own code. If you modify your own code, that would conflict with the debugger adding breakpoints there.
- Do not change behavior when under the debugger. An app should behave identically when run outside or under the debugger. This is absolutely necessary else we get complaints like "my program only crashes when run under the debugger". This is also necessary because somebody may attach a debugger to an app after the fact. Specific examples of this:
  - Don't assume that just because an app is under the debugger that somebody is trying to debug it.
  - Don't add additional run-time error checks when under the debugger. For example, avoid code like:  if ((IsDebuggerPresent() && (argument == null)) { throw MyException(); }
  - Avoid massive perf changes when under the debugger. For example, don't use an interpreted stub just because you're under the debugger. We then get bugs like [my app is 100x slower when under a debugger](https://docs.microsoft.com/en-us/archive/blogs/jmstall/psa-pinvokes-may-be-100x-slower-under-the-debugger).
  - Avoid algorithmic changes. For example, do not make the JIT generate non-optimized code just because an app is under the debugger. Do not make the loader policy resolve to a debuggable-ngen image just because an app is under the debugger.
- Separate your code into a) side-effect-free (non-mutating) read-only accessors and b) functions that change state. The motivation is that the debugger needs to be able to read-state in a non-invasive way. For example, don't just have GetFoo() that will lazily create a Foo if it's not available. Instead, split it out like so:
  - GetFoo() - fails if a Foo does not exist. Being non-mutating, this should also be GC_NOTRIGGER. Non-mutating will also make it much easier to DAC-ize. This is what the debugger will call.
  - and GetOrCreateFoo() that is built around GetFoo(). The rest of the runtime can call this.
  - The debugger can then just call GetFoo(), and deal with the failure accordingly.
- If you add a new stub (or way to call managed code), make sure that you can source-level step-in (F11) it under the debugger. The debugger is not psychic. A source-level step-in needs to be able to go from the source-line before a call to the source-line after the call, or managed code developers will be very confused. If you make that call transition be a giant 500 line stub, you must cooperate with the debugger for it to know how to step-through it. (This is what StubManagers are all about. See [src\vm\stubmgr.h](https://github.com/dotnet/runtime/blob/main/src/coreclr/vm/stubmgr.h)). Try doing a step-in through your new codepath under the debugger.
- **Beware of timeouts** : The debugger may completely suspend your process at arbitrary points. In most cases, the debugger will do the right thing (and suspend your timeout too), but not always. For example, if you have some other process waiting for info from the debuggee, it [may hit a timeout](https://docs.microsoft.com/en-us/archive/blogs/jmstall/why-you-sometimes-get-a-bogus-contextswitchdeadlock-mda-under-the-debugger).
- **Use CLR synchronization primitives (like Crst)**. In addition to all the reasons listed in the synchronization section, the CLR-aware primitives can cooperate with the debugging services. For example:
  - The debugger needs to know when threads are modifying sensitive data (which correlates to when the threads lock that data).
  - Timeouts for CLR synchronization primitives may operate better in the face of being debugged.
- **Optimized != Non-debuggable:** While performance is important, you should make sure your perf changes do not break the debugger. This is especially important in stepping, which requires the debugger to know exactly where we are going to execute managed code. For example, when we started using IL stubs for reverse pinvoke calls in the .NET Framework 2, the debugger was no longer notified that a thread was coming back to managed code, which broke stepping. You can probably find a way to make your feature area debuggable without sacrificing performance.

**Examples of dependencies** : Here's a random list of ways that the debugger depends on the rest of the runtime.

- Debugger must be able to inspect CLR data-structures, so your code must be DAC-ized. Examples include: running a module list, walking the thread list, taking a callstack, recognizing stubs, and doing an IP2MD lookup. You can break the debugger by just breaking the DAC (changing DAC-ized code so that it is no longer dac-ized correctly).
- Type-system: Debugger must be able to traverse the type-system.
- Need notifications from VM: loader, exception, jit-complete, etc.
- Anything that affects codegen (Emit, Dynamic language, IBC)s: the debugger needs to know where the code is and how it's laid out.
- GC, threading – debugger must be GC-aware. For example, we must protect the user from trying to inspect the GC-heap in the middle of a GC. The debugger must also be able to do a synchronization that may compete with a GC-synchronization.
- Step-in through a stub: Any time you add a new stub or new way of calling managed code, you might break stepping.
- Versioning: You could write a debugger in managed code targeting CLR version X, but debugging a process that's loaded CLR version Y. Now that's some versioning nightmares.

## <a name="2.9"></a>2.9 Does your code work on 64-bit?

### <a name="2.9.1"></a>2.9.1 Primitive Types

Because the CLR is ultimately compiled on several different platforms, we have to be careful about the primitive types which are used in our code. Some compilers can have slightly different declarations in standard header files, and different processor word sizes can require values to have different representations on different platforms.

Because of this, we have gathered definition all of the "blessed" CLR types in a single header file, [clrtypes.h](https://github.com/dotnet/runtime/blob/main/src/coreclr/inc/clrtypes.h). In general, you should only use primitive types which are defined in this file. As an exception, you may use built-in primitive types like int and short when precision isn't particularly interesting.

The types are grouped into several categories.

- Fixed representation integral types. (INT8, UINT8, INT16, UINT16, INT32, UINT32, INT64, UINT64) These typedefs will always have the same representation on each platform. Each type is named with the number of bits in the representation.
- Pointer sized integral types. (SIZE_T, SSIZE_T) These types will change size on platforms, depending on the native pointer size. Use SIZE_T whenever you need to cast pointers to and from integral types. SSIZE_T is the signed version of SIZE_T; use it if you are computing a difference of two arbitrary pointers.
- Large count-sized integral types (COUNT_T, SCOUNT_T) These are used when you would normally use a SIZE_T or SSIZE_T on a 32 bit machine, but you know you won't ever need more than 32 bits, even on a 64 bit machine. Use this type where practical to avoid bloated data sizes.
- Semantic content types: (BOOL, BYTE). Use these types to indicate additional semantic context to an integral type. BYTE indicates "raw data", and BOOL indicates a value which can be either TRUE or FALSE.
- Character data types (CHAR, SCHAR, UCHAR, WCHAR, ASCII, ANSI, UTF8). These have fixed sizes and represent single characters in strings. CHAR may be either signed or unsigned. Note that CHAR/SCHAR/UCHAR specify no semantics about character set; use ASCII, ANSI, and UTF8 to indicate when a specific encoding is used.It is worth mentioning that manipulation of strings as raw character arrays is discouraged; instead code should use the SString class wherever possible.
- Pointer to executable code PCODE. Use these for any pointers to (managed) executable code.

All standard integral types have *_MIN and *_MAX values declared as well.

## <a name="2.10"></a>2.10 Does your function declare a CONTRACT?

Every function in the CLR must declare a contract. A contract enumerates important behavioral facts such as whether a function throws or whether it can trigger gc. It also a general container for expressing preconditions and postconditions specific to that function.

Contracts help determine which functions can safely invoke others. These constraints are enforced in two ways:

- Statically, using a special tool that analyzes callgraphs and flags violations.
- Runtime assertions.

These two approaches are complementary. Static analysis is always preferable but the tool cannot reliably find all call paths and can not check custom preconditions. Runtime checks are only as good as our code coverage.

Here is a typical contract:

	LPVOID Foo(char *name, Blob *pBlob)
	{
	    CONTRACTL
	    {
	        THROWS;                                     // This function may throw
	        INJECT_FAULT(COMPlusThrowOM());             // This function may fail due to OOM
	        GC_TRIGGERS;                                // This function may trigger a GC
	        MODE_COOPERATIVE;                           // Must be in GC-cooperative mode to call
	        CAN_TAKE_LOCK;                              // This function may take a Crst, spinlock, etc.
	        EE_THREAD_REQUIRED;                         // This function expects an EE Thread object in the TLS
	        PRECONDITION(CheckPointer(name));           // Invalid to pass NULL
	        PRECONDITION(CheckPointer(pBlob, NULL_OK)); // Ok to pass NULL
	    }
	    CONTRACTL_END;

	    ...
	}

There are several flavors of contracts. This example shows the most common type (CONTRACTL, where "L" stands for "lite.")

At runtime (on a checked build), the contract does the following:

At the start of Foo(), it validates that it's safe to throw, safe to generate an out of memory error, safe to trigger gc, that the GC mode is cooperative, and that your preconditions are true.

On a retail build, CONTRACT expands to nothing.

### <a name="2.10.1"></a>2.10.1 What can be said in a contract?

As you can see, a contract is a laundry list of "items" that either assert some requirement on the current thread state or impose a requirement on downstream callees. The following is a whirlwind tour of the supported annotations. The nuances of each one are explained in more detail in their individual chapters.

#### <a name="2.10.1.1"></a>2.10.1.1 THROWS/NOTHROW

Declares whether an exception can be thrown out of this function. Declaring **NOTHROW** puts the thread in a NOTHROW state for the duration of the function call. You will get an assert if you throw an exception or call a function declared THROWS. An EX_TRY/EX_CATCH construct however will lift the NOTHROW state for the duration of the TRY body.

#### <a name="2.10.1.2"></a>2.10.1.2 INJECT_FAULT(_handler-stmt_)/FORBID_FAULT

This is a poorly named item. INJECT_FAULT declares that the function can **fail** due to an out of memory (OOM) condition. FORBID_FAULT means that the function promises never to fail due to OOM. FORBID_FAULT puts the thread in a FORBID_FAULT state for the duration of the function call. You will get an assert if you allocate memory (even with the C++ new operator) or call a function declared INJECT_FAULT.

#### <a name="2.10.1.3"></a>2.10.1.3 GC_TRIGGERS/GC_NOTRIGGER

Declares whether the function is allowed to trigger a GC. GC_NOTRIGGER puts the thread in a NOTRIGGER state where any call to a GC_TRIGGERS function will assert.

**Observation:** THROWS does not necessarily imply GC_TRIGGERS. COMPlusThrow does not trigger GC.

#### <a name="2.10.1.4"></a>2.10.1.4 MODE_PREEMPTIVE/ MODE_COOPERATIVE/ MODE_ANY

This item asserts that the thread is in a particular mode or declares that the function is mode-agnostic. It does not change the state of the thread in any way.

#### <a name="2.10.1.5"></a>2.10.1.5 LOADS_TYPE(_loadlevel_)

This item asserts that the function may invoke the loader and cause a type to loaded up to (and including) the indicated loadlevel. Valid load levels are taken from ClassLoadLevel enumerationin [classLoadLevel.h](https://github.com/dotnet/runtime/blob/main/src/coreclr/vm/classloadlevel.h).

The CLR asserts if any attempt is made to load a type past the current limit set by LOADS_TYPE. A call to any function that has a LOADS_TYPE contract is treated as an attempt to load a type up to that limit.

#### <a name="2.10.1.6"></a>2.10.1.6 CAN_TAKE_LOCK / CANNOT_TAKE_LOCK

These declare whether a function or callee takes any kind of EE or user lock: Crst, SpinLock, readerwriter, clr critical section, or even your own home-grown spin lock (e.g., ExecutionManager::IncrementReader).

In TLS we keep track of the current intent (whether to lock), and actual reality (what locks are actually taken). Enforcement occurs as follows:

[contract.h]: https://github.com/dotnet/runtime/blob/main/src/coreclr/inc/contract.h

- SCAN
  - A CANNOT_TAKE_LOCK function calling a CAN_TAKE_LOCK function is illegal (just like THROWS/NOTHROWS)
- Dynamic checking:
  - A CANNOT_TAKE_LOCK function calling a CAN_TAKE_LOCK function is illegal
  - *_LOCK_TAKEN / *_LOCK_RELEASED macros (contract.h):
    - Sprinkled at all places we take/release actual or conceptual locks
    - Asserts if taking a lock in a CANNOT_TAKE_LOCK scope
    - Keeps count of locks currently taken by thread
    - Remembers stack of lock pointers for diagnosis
  - ASSERT_NO_EE_LOCKS_HELD(): Handy way for you to verify no locks are held right now on this thread (i.e., lock count == 0)

#### <a name="2.10.1.7"></a>2.10.1.7 EE_THREAD_REQUIRED / EE_THREAD_NOT_REQUIRED

These declare whether a function or callee deals with the case "GetThread() == NULL".

EE_THREAD_REQUIRED simply asserts that GetThread() != NULL.

EE_THREAD_NOT_REQUIRED is a noop by default. You must "set COMPlus_EnforceEEThreadNotRequiredContracts=1" for this to be enforced. Setting the envvar forces a C version of GetThread() to be used, instead of the optimized assembly versions. This C GetThread() always asserts in an EE_THREAD_NOT_REQUIRED scope regardless of whether there actually is an EE Thread available or not. The reason is that if you claim you don't require an EE Thread, then you have no business asking for it (even if you get lucky and there happens to be an EE Thread available).

Of course, there are exceptions to this. In particular, if there is a clear code path for GetThread() == NULL, then it's ok to call GetThread() in an EE_THREAD_NOT_REQUIRED scope. You declare your intention by using GetThreadNULLOk():

	Thread* pThread = GetThreadNULLOk();
	if (pThread != NULL)
	{
	    pThread->m_dwAVInRuntimeImplOkayCount++;
	}

Rule: You should only use GetThreadNULLOk if it is patently obvious from the call site that NULL is dealt with directly. Obviously, this would be bad:

	GetThreadNULLOk()->BeginCriticalRegion();

This is also frowned upon, as it's unclear whether a NULL Thread is handled:

	MyObj myObj(GetThreadNULLOk());

In more complex situations, a caller may be able to vouch for an EE Thread's existence, while its callee cannot. So you can set up a scope that temporarily stops doing the EE_THREAD_NOT_REQUIRED verification as follows:

	CONTRACTL
	{
	    EE_THREAD_NOT_REQUIRED;
	} CONTRACTL_END;

	Thread* pThread = GetThreadNULLOk();
	if (pThread == NULL)
	    return;

	// We know there's an EE Thread now, so it's safe to call GetThread()
	// and expect a non-NULL return.
	BEGIN_GETTHREAD_ALLOWED;
	CallCodeThatRequiresThread();
	END_GETTHREAD_ALLOWED;

BEGIN/END_GETTHREAD_ALLOWED simply instantiate a holder that temporarily disables the assert on each GetThread() call. A non-holder version is also available which can generate less code if you're wrapping a NOTHROW region: BEGIN/END_GETTHREAD_ALLOWED_IN_NO_THROW_REGION. In fact, GetThreadNULLOk() is implemented by just calling GetThread() from within a BEGIN/END_GETTHREAD_ALLOWED_IN_NO_THROW_REGION block.

You should only use BEGIN/END_GETTHREAD_ALLOWED(_IN_NO_THROW_REGION) if:

- It is provably impossible for GetThread() to ever return NULL from within that scope, or
- All code within that scope directly deals with GetThread()==NULL.

If the latter is true, it's generally best to push BEGIN/END_GETTHREAD_ALLOWED down the callee chain so all callers benefit.

#### <a name="2.10.1.8"></a>2.10.1.8 PRECONDITION(_expr_)

This is pretty self-explanatory. It is basically an **_ASSERTE.** Both _ASSERTE's and PRECONDITIONS are used widely in the codebase. The expression can evaluate to either a Boolean or a Check.

#### <a name="2.10.1.9"></a>2.10.1.9 POSTCONDITION(_expr_)

This is an expression that's tested on a _normal_ function exit. It will not be tested if an exception is thrown out of the function. Postconditions can access the function's locals provided that the locals were declared at the top level scope of the function. C++ objects will not have been destructed yet.

Because of the limitations of our macro infrastructure, this item imposes some syntactic ugliness into the function. More on this below.

### <a name="2.10.2"></a>2.10.2 Is order important?

Preconditions and postconditions will execute in the order declared. The "intrinsic" items will execute before any preconditions regardless of where they appear.

### <a name="2.10.3"></a>2.10.3 Using the right form of contract.

Contracts come in several forms:

- CONTRACTL: This is the most common type. It does runtime checks as well as being visible to the static scanner. It is suitable for all runtime contracts except those that use postconditions. When in doubt, use this form.
- CONTRACT(returntype): This is an uglier version that's needed if you include a POSTCONDITION. You must supply the correct function return type for this form and it cannot be "void" (use CONTRACT_VOID instead.) You must also use the special RETURN macro rather than the normal return keyword.
- CONTRACT_VOID: Use this if you need a postcondition and the return type is void. CONTRACT(void) will not work.
- STATIC_CONTRACT_\*: This form generates no runtime code but still emits the hidden tags visible to the static contract scanner. Use this only if checked build perf would suffer greatly by putting a runtime contract there or if for some technical reason, the runtime-based contract is not possible..
- LIMITED_METHOD_CONTRACT: A static contract equivalent to NOTHROW/GC_NOTRIGGER/FORBID_FAULT/MODE_ANY/CANNOT_TAKE_LOCK. Use this form only for trivial one-liner functions. Remember it does not do runtime checks so it should not be used for complex functions.
- WRAPPER_NO_CONTRACT: A static no-op contract for functions that trivially wrap another. This was invented back when we didn't have static contracts and we now wish it hadn't been invented. Please don't use this in new code.

### <a name="2.10.4"></a>2.10.4 When is it safe to use a runtime contract?

Contracts do not require that current thread have a Thread structure. Even those annotations that explicitly check Thread bits (the GC and MODE annotations) will correctly handle the NULL ThreadState case.

Contracts can and are used outside of the files that build CLR. However, the GC_TRIGGERS and MODE family of items is not available outside of CLR.

You cannot use runtime contracts if:

- Your code is callable from the implementation of FLS (Fiber Local Storage). This may result in an infinite recursion as the contract infrastructure itself uses FLS.
- Your code makes a net change to the ClrDebugState. Only the contract infrastructure should be doing this but see below for more details.

### <a name="2.10.5"></a>2.10.5 Do not make unscoped changes to the ClrDebugState.

The ClrDebugState is the per-thread data structure that houses all of the flag bits set and tested by contracts (i.e. NOTHROW, NOTRIGGER.). You should never modify this data directly. Always go through contracts or the specific holders (such as GCX_NOTRIGGER.)

This data is meant to be changed in a scoped manner only. In particular, the CONTRACT destructor always restores the _entire_ ClrDebugState from a copy saved on function entry. This means that any net changes made by the function body itself will be wiped out when the function exits via local _or_ non-local control. The same caveat is true for holders such as GCX_NOTRIGGER.

### <a name="2.10.6"></a>2.10.6 For more details...

See the big block comment at the start of [src\inc\contract.h][contract.h].

## <a name="2.11"></a>2.11 Is your code DAC compliant?

At a high level, DAC is a technique to enable execution of CLR algorithms from out-of-process (eg. on a memory dump). Core CLR code is compiled in a special mode (with DACCESS_COMPILE defined) where all pointer dereferences are intercepted.

Various tools (most notably the debugger and SOS) rely on portions of the CLR code being properly "DACized". Writing code in this way can be tricky and error-prone. Use the following references for more details:

- The best documentation is in the code itself. See the large comments at the top of [src\inc\daccess.h](https://github.com/dotnet/runtime/blob/main/src/coreclr/inc/daccess.h).
