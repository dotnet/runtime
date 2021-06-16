Garbage Collection Design
=========================
Author: Maoni Stephens ([@maoni0](https://github.com/maoni0)) - 2015

Note: See _The Garbage Collection Handbook_ to learn more about garbage collection topics in general; for specific knowledge on the CLR GC please refer to the _Pro .NET Memory Management_ book. Both referenced in the resources section at the end of this document.

Component Architecture
======================

The 2 components that belong to GC are the allocator and the
collector. The allocator is responsible for getting more memory and triggering the collector when appropriate. The collector reclaims garbage, or the memory of objects that are no longer in use by the program.

There are other ways that the collector can get called, such as manually calling GC.Collect or the finalizer thread receiving an asynchronous notification of the low memory (which triggers the collector).

Design of Allocator
===================

The allocator gets called by the allocation helpers in the Execution Engine (EE), with the following information:

- Size requested
- Thread allocation context
- Flags that indicate things like whether this is a finalizable object or not

The GC does not have special treatment for different kinds of object types. It consults the EE to get the size of an object.

Based on the size, the GC divides objects into 2 categories: small
objects (< 85,000 bytes) and large objects (>= 85,000 bytes). In
principle, small and large objects can be treated the same way but
since compacting large objects is more expensive GC makes this
distinction.

When the GC gives out memory to the allocator, it does so in terms of allocation contexts. The size of an allocation context is defined by the allocation quantum.

- **Allocation contexts** are smaller regions of a given heap segment that are each dedicated for use by a given thread. On a single-processor (meaning 1 logical processor) machine, a single context is used, which is the generation 0 allocation context.
- The **Allocation quantum** is the size of memory that the allocator allocates each time it needs more memory, in order to perform object allocations within an allocation context. The allocation is typically 8k and the average size of managed objects are around 35 bytes, enabling a single allocation quantum to be used for many object allocations.

Large objects do not use allocation contexts and quantums. A single large object can itself be larger than these smaller regions of memory. Also, the benefits (discussed below) of these regions are specific to smaller objects. Large objects are allocated directly to a heap segment.

The allocator is designed to achieve the following:

- **Triggering a GC when appropriate:** The allocator triggers a GC when the allocation budget (a threshold set by the collector) is exceeded or when the allocator can no longer allocate on a given segment. The allocation budget and managed segments are discussed in more detail later.
- **Preserving object locality:** Objects allocated together on the same heap segment will be stored at virtual addresses close to each other.
- **Efficient cache usage:** The allocator allocates memory in _allocation quantum_ units, not on an object-by-object basis. It zeroes out that much memory to warm up the CPU cache because there will be objects immediately allocated in that memory. The allocation quantum is usually 8k.
- **Efficient locking:** The thread affinity of allocation contexts and quantums guarantee that there is only ever a single thread writing to a given allocation quantum. As a result, there is no need to lock for object allocations, as long as the current allocation context is not exhausted.
- **Memory integrity:** The GC always zeroes out the memory for newly allocated objects to prevent object references pointing at random memory.
- **Keeping the heap crawlable:** The allocator makes sure to make a free object out of left over memory in each allocation quantum. For example, if there is 30 bytes left in an allocation quantum and the next object is 40 bytes, the allocator will make the 30 bytes a free object and get a new allocation quantum.

Allocation APIs
---------------

     Object* GCHeap::Alloc(size_t size, DWORD flags);
     Object* GCHeap::Alloc(alloc_context* acontext, size_t size, DWORD flags);

The above functions can be used to allocate both small objects and
large objects. There is also a function to allocate directly on LOH:

     Object* GCHeap::AllocLHeap(size_t size, DWORD flags);

Design of the Collector
=======================

Goals of the GC
---------------

The GC strives to manage memory extremely efficiently and
require very little effort from people who write "managed code". Efficient means:

- GCs should occur often enough to avoid the managed heap containing a significant amount (by ratio or absolute count) of unused but allocated objects (garbage), and therefore use memory unnecessarily.
- GCs should happen as infrequently as possible to avoid using otherwise useful CPU time, even though frequent GCs would result in lower memory usage.
- A GC should be productive. If GC reclaims a small amount of memory, then the GC (including the associated CPU cycles) was wasted.
- Each GC should be fast. Many workloads have low latency requirements.
- Managed code developers shouldn't need to know much about the GC to achieve good memory utilization (relative to their workload).
– The GC should tune itself to satisfy different memory usage patterns.

Logical representation of the managed heap
------------------------------------------

The CLR GC is a generational collector which means objects are
logically divided into generations. When a generation _N_ is collected,
the survived objects are now marked as belonging to generation _N+1_. This
process is called promotion. There are exceptions to this when we
decide to demote or not promote.

For small objects the heap is divided into 3 generations: gen0, gen1
and gen2. For large objects there's one generation – gen3. Gen0 and gen1 are referred to as ephemeral (objects lasting for a short time) generations.

For the small object heap, the generation number represents the age – gen0
being the youngest generation. This doesn't mean all objects in gen0
are younger than any objects in gen1 or gen2. There are exceptions
which will be explained below. Collecting a generation means collecting
objects in that generation and all its younger generations.

In principle large objects can be handled the same way as small
objects but since compacting large objects is very expensive, they are treated differently. There is only one generation for large objects and
they are always collected with gen2 collections due to performance
reasons. Both gen2 and gen3 can be big, and collecting ephemeral generations (gen0 and gen1) needs to have a bounded cost.

Allocations are made in the youngest generation – for small objects this means always gen0 and for large objects this means gen3 since there's only one generation.

Physical representation of the managed heap
-------------------------------------------

The managed heap is a set of managed heap segments. A heap segment is a contiguous block of memory that is acquired by the GC from the OS. The heap segments are
partitioned into small and large object segments, given the distinction of small and large objects. On each heap the heap segments are chained together. There is at least one small object segment and one large segment - they are reserved when CLR is loaded.

There's always only one ephemeral segment in each small object heap, which is where gen0 and gen1 live. This segment may or may not include gen2
objects. In addition to the ephemeral segment, there can be zero, one or more additional segments, which will be gen2 segments since they only contain gen2 objects.

There are 1 or more segments on the large object heap.

A heap segment is consumed from the lower address to the higher
address, which means objects of lower addresses on the segment are
older than those of higher addresses. Again there are exceptions that
will be described below.

Heap segments can be acquired as needed. They are  deleted when they
don't contain any live objects, however the initial segment on the heap
will always exist. For each heap, one segment at a time is acquired,
which is done during a GC for small objects and during allocation time
for large objects. This design provides better performance because large objects are only collected with gen2 collections (which are relatively expensive).

Heap segments are chained together in order of when they were acquired. The last segment in the chain is always the ephemeral segment. Collected segments (no live objects) can be reused instead of deleted and instead become the new ephemeral segment. Segment reuse is only implemented for small object heap. Each time a large object is allocated, the whole large object heap is considered. Small object allocations only consider the ephemeral segment.

The allocation budget
---------------------

The allocation budget is a logical concept associated with each
generation. It is a size limit that triggers a GC for that
generation when exceeded.

The budget is a property set on the generation mostly based on the
survival rate of that generation. If the survival rate is high, the budget is made larger with the expectation that there will be a better ratio of dead to live objects next time there is a GC for that generation.

Determining which generation to collect
---------------------------------------

When a GC is triggered, the GC must first determine which generation to collect. Besides the allocation budget there are other factors that must be considered:

- Fragmentation of a generation – if a generation has high fragmentation, collecting that generation is likely to be productive.
- If the memory load on the machine is too high, the GC may collect
  more aggressively if that's likely to yield free space. This is important to
  prevent unnecessary paging (across the machine).
- If the ephemeral segment is running out of space, the GC may do more aggressive ephemeral collections (meaning doing more gen1's) to avoid acquiring a new heap segment.

The flow of a GC
----------------

Mark phase
----------

The goal of the mark phase is to find all live objects.

The benefit of a generational collector is the ability to collect just part of
the heap instead of having to look at all of the objects all the
time. When  collecting the ephemeral generations, the GC needs to find out which objects are live in these generations, which is information reported by the EE. Besides the objects kept live by the EE, objects in older generations
can also keep objects in younger generations live by making references
to them.

The GC uses cards for the older generation marking. Cards are set by JIT
helpers during assignment operations. If the JIT helper sees an
object in the ephemeral range it will set the byte that contains the
card representing the source location. During ephemeral collections, the GC can look at the set cards for the rest of the heap and only look at the objects that these cards correspond to.

Plan phase
---------

The plan phase simulates a compaction to determine the effective result. If compaction is productive the GC starts an actual compaction; otherwise it sweeps.

Relocate phase
--------------

If the GC decides to compact, which will result in moving objects, then  references to these objects must be updated. The relocate phase needs to find all references that point to objects that are in the
generations being collected. In contrast, the mark phase only consults live objects so it doesn't need to consider weak references.

Compact phase
-------------

This phase is very straight forward since the plan phase already
calculated the new addresses the objects should move to. The compact
phase will copy the objects there.

Sweep phase
-----------

The sweep phase looks for the dead space in between live objects. It creates free objects in place of these dead spaces. Adjacent dead objects are made into one free object. It places all of these free objects onto the _freelist_.

Code Flow
=========

Terms:

- **WKS GC:** Workstation GC
- **SVR GC:** Server GC

Functional Behavior
-------------------

### WKS GC with concurrent GC off

1. User thread runs out of allocation budget and triggers a GC.
2. GC calls SuspendEE to suspend managed threads.
3. GC decides which generation to condemn.
4. Mark phase runs.
5. Plan phase runs and decides if a compacting GC should be done.
6. If so relocate and compact phase runs. Otherwise, sweep phase runs.
7. GC calls RestartEE to resume managed threads.
8. User thread resumes running.

### WKS GC with concurrent GC on

This illustrates how a background GC is done.

1. User thread runs out of allocation budget and triggers a GC.
2. GC calls SuspendEE to suspend managed threads.
3. GC decides if background GC should be run.
4. If so background GC thread is woken up to do a background
   GC. Background GC thread calls RestartEE to resume managed threads.
5. Managed threads continue allocating while the background GC does its work.
6. User thread may run out of allocation budget and trigger an
   ephemeral GC (what we call a foreground GC). This is done in the same
   fashion as the "WKS GC with concurrent GC off" flavor.
7. Background GC calls SuspendEE again to finish with marking and then
   calls RestartEE to start the concurrent sweep phase while user threads
   are running.
8. Background GC is finished.

### SVR GC with concurrent GC off

1. User thread runs out of allocation budget and triggers a GC.
2. Server GC threads are woken up and call SuspendEE to suspend
   managed threads.
3. Server GC threads do the GC work (same phases as in workstation GC
   without concurrent GC).
4. Server GC threads call RestartEE to resume managed threads.
5. User thread resumes running.

### SVR GC with concurrent GC on

This scenario is the same as WKS GC with concurrent GC on, except the non background GCs are done on SVR GC threads.

Physical Architecture
=====================

This section is meant to help you follow the code flow.

User thread runs out of quantum and gets a new quantum via try_allocate_more_space.

try_allocate_more_space calls GarbageCollectGeneration when it needs to trigger a GC.

Given WKS GC with concurrent GC off, GarbageCollectGeneration is done all
on the user thread that triggered the GC. The code flow is:

     GarbageCollectGeneration()
     {
         SuspendEE();
         garbage_collect();
         RestartEE();
     }

     garbage_collect()
     {
         generation_to_condemn();
         gc1();
     }

     gc1()
     {
         mark_phase();
         plan_phase();
     }

     plan_phase()
     {
         // actual plan phase work to decide to
         // compact or not
         if (compact)
         {
             relocate_phase();
             compact_phase();
         }
         else
             make_free_lists();
     }

Given WKS GC with concurrent GC on (default case), the code flow for a background GC is

     GarbageCollectGeneration()
     {
         SuspendEE();
         garbage_collect();
         RestartEE();
     }

     garbage_collect()
     {
         generation_to_condemn();
         // decide to do a background GC
         // wake up the background GC thread to do the work
         do_background_gc();
     }

     do_background_gc()
     {
         init_background_gc();
         start_c_gc ();

         //wait until restarted by the BGC.
         wait_to_proceed();
     }

     bgc_thread_function()
     {
         while (1)
         {
             // wait on an event
             // wake up
             gc1();
         }
     }

     gc1()
     {
         background_mark_phase();
         background_sweep();
     }

Resources
=========

- [.NET CLR GC Implementation](https://raw.githubusercontent.com/dotnet/runtime/main/src/coreclr/gc/gc.cpp)
- [The Garbage Collection Handbook: The Art of Automatic Memory Management](http://www.amazon.com/Garbage-Collection-Handbook-Management-Algorithms/dp/1420082795)
- [Garbage collection (Wikipedia)](http://en.wikipedia.org/wiki/Garbage_collection_(computer_science))
- [Pro .NET Memory Management](https://prodotnetmemory.com/)
- [.NET GC Internals video series](https://www.youtube.com/playlist?list=PLpUkQYy-K8Y-wYcDgDXKhfs6OT8fFQtVm)
