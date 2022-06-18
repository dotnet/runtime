// TODO: elaborate further on many sections   
// TODO: need a lot more examples. 



# CLR memory model

## ECMA vs. CLR memory models.
ECMA 335 standard defines a very weak memory model. After two decades the desire to have a flexible model did not result in considerable benefits due to hardware being more strict. On the other hand programming against ECMA model requires extra complexity to handle scenarios that are hard to comprehend and not possible to test.

In the course of multiple releases CLR implementation settled around a memory model that is a practical compromise between what can be implemented efficiently on the current hardware, while staying reasonably approachable by the developers. This document rationalizes the invariants provided and expected by the CLR runtime in its current implementation with expectation of that being carried to future releases.

The memory model is generally the same among different runtime implementations such as .Net FX, CoreCLR, Mono, NativeAOT. When discrepancies do happen they will be called out.

## Alignment
When managed by CLR runtime, variables of built-in primitive types are *properly aligned* according to the data type size. This applies to both heap and stack allocated memory.

1-byte, 2-byte, 4-byte variables are stored at 1-byte, 2-byte, 4-byte boundary, respectively.  
8-byte variables are 8-byte aligned on 64 bit platforms.  
Native-sized integer types and pointers have alignment that matches their size on the given platform.  

## Atomic memory accesses.
Memory accesses to *properly aligned* data of primitive types are always atomic. The value that is observed is always a result of complete read and write operations.

## Unmanaged memory access. 
As pointers can point to any addressable memory, operations with pointers may violate guarantees provided by the runtime and expose undefined or platform-specific behavior.  
**Example:** memory accesses through pointers which are *not properly aligned* may be not atomic or cause faults depending on the platform and hardware configuration.   

Although rare, unaligned access is a realistic scenario and thus there is some limited support for unaligned memory accesses, such as:  
* `.unaligned` IL prefix 
* `Unsafe.ReadUnaligned`, `Unsafe.WriteUnaligned` and ` Unsafe.CopyBlockUnaligned` helpers.

These facilities ensure fault-free access to potentially unaligned variables, but do not ensure atomicity.

As of this writing there is no specific support for operating with incoherent memory, device memory or similar. Passing non-ordinary memory to the runtime by the means of pointer operations or native interop will result in Undefined Behavior.

## Sideeffects and optimizations of memory accesses.
CLR assumes that the sideeffects of memory reads and writes include only changing and observing values at specified memory locations. This applies to all reads and writes - volatile or not. **This is different from ECMA model.**

As a consequence: 
* Speculative writes are not allowed. 
* Reads cannot be introduced.
* Unused reads can be elided.
* Adjacent reads from the same location can be coalesced.
* Adjacent writes to the same location can be coalesced.

## Thread-local memory accesses.
It may be possible for an optimizing compiler to prove that some data is accessible only by a single thread. In such case it is permitted to perform further optimizations such as duplicating or removal of memory accesses.  

## Order of memory operations.
* **Ordinary memory accesses**  
The effects of ordinary reads and writes can be reordered as long as that preserves single-thread consistency. Such reordering can happen both due to code generation strategy of the compiler or due to weak memory ordering in the hardware. 

* **Volatile reads** have "acquire semantics" - no read or write that is later in the program order may be speculatively executed ahead of a volatile read.  
  Operations with acquire semantics:  
     - IL load instructions with `.volatile` prefix when instruction supports such prefix
     - `System.Threading.Volatile.Read`
     - `System.Thread.VolatileRead`
     - Acquiring a lock (`System.Threading.Monitor.Enter` or entering a synchronized method)

* **Volatile writes** have "release semantics" - the effects of a volatile write will not be observable before effects of all previous, in program order, reads and writes become observable.  
  Operations with release semantics:
     - IL store instructions with `.volatile` prefix when such prefix is supported
     - `System.Threading.Volatile.Write`
     - `System.Thread.VolatileWrite`
     - Releasing a lock (`System.Threading.Monitor.Exit` or leaving a synchronized method)

Note that volatile semantics does not by itself imply that operation is atomic or has any effect on how soon the operation is committed to the coherent memory. It only specifies the order of effects when they eventually become observable.

`.volatile` and `.unaligned` IL prefixes can be combined where both are permitted.

// TODO: `cpblk` and `initblk`

It may be possible for an optimizing compiler to prove that some data is accessible only by a single thread. In such case it is permitted to omit volatile semantics when accessing such data.

* **Full-fence operations**  
  Full-fence operations have "full-fence semantics" - effects of reads and writes must be observable no later or no earlier than a full-fence operation according to their relative program order.  
  Operations with full-fence semantics:
     - `System.Thread.MemoryBarrier`
     - `System.Threading.Interlocked` methods

## Process-wide barrier
Process-wide barrier has full-fence semantics with an additional guarantee that each thread in the program effectively performs a full fence at arbitrary point synchronized with the process-wide barrier in such a way that effects of writes that precede both barriers are observable by memory operations that follow the barriers.  

The actual implementation may vary depending on the platform. For example interrupting the execution of every core in the current process' affinity mask could be a suitable implementation.

## Object assignment
Object assignment to a location potentially accessible by other threads is a release with respect to write operations to the instance’s fields and metadata.
The motivation is to ensure that storing an object reference to shared memory acts as a "committing point" to all modifications that are reachable through the instance reference. It also guarantees that a freshly allocated instance is valid (i.e. method table and necessary flags are set) when other threads, including background GC threads are able to access the instance.  
The reading thread does not need to perform an acquiring read before accessing the content of an instance since all supported platforms honor ordering of data-dependent reads. 

However, the ordering sideeffects of reference assignement should not be used for general ordering purposes because: 
-	ordinary reference assignments are still treated as ordinary assignments and could be reordered by the compiler. 
-	an optimizing compiler can omit the release semantics if it can prove that the instance is not shared with other threads.  

## Instance constructors
CLR does not specify any ordering effects to the instance constructors.

## Static constructors
All side effects of static constructor execution must happen before accessing any member of the type.

//TODO: is this a case when RunClassConstructor is called?

## Exceptions
//TODO: Synchronous, asynchronous, thread.abort. Anything to do with memory model?

## Hardware considerations
Current CoreCLR and libraries implementation makes a few expectations about the hardware memory model. These conditions are present on all currently supported platforms and transparently passed to the user of the runtime. The future supported platforms will likely support these too as the large body of preexisting software will make it burdensome to break common assumptions. 

* Naturally aligned reads and writes with sizes up to the platform pointer size are atomic.   
That applies even for locations targeted by overlapping aligned reads and writes of different sizes.  
**Example:** a read of a 4-byte aligned int32 variable will yield a value that existed prior some write or after some write. It will never be a mix of before/after bytes.

*	The memory is cache-coherent and writes to a single location will be seen by all cores in the same order (multicopy atomic).  
**Example:** when the same location is updated with values in ascending order (like 1,2,3,4,...), no observer will see a descending sequence.

*	It may be possible for a thread to see its own writes before they appear to other cores (store buffer forwarding), as long as the single-thread consistency is not violated.

*	The memory managed by the runtime is ordinary memory (not device register file or the like) and the only sideeffects of memory operations are storing and reading of values.

*	It is possible to implement release consistency memory model.  
Either the platform defaults to release consistency or stronger (i.e. x64 is TSO, which is stronger), or provides means to implement release consistency via fencing operations.

*	Memory ordering honors data dependency  
**Example:** reading a field, will not use a cached value fetched from the location of the field prior obtaining a reference to the instance.  
(Some versions of Alpha processors did not support this, most current architectures do)
