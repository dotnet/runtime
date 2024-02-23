# Dynamic Allocation Sampling

## Problem space - statistically correct allocation sampling

As of today, the CLR raises the AllocationTick event each time ~100 KB of objects are allocated with a payload containing the type, size and address of the last allocated object and the cumulated allocated size since the last event.

Using a constant threshold makes the distribution of allocated object not statistically representative of the real allocations. Also, for performance reasons, the minimum allocation granularity is the allocation context so it is not possible to trigger this event from within these ~8 KB ranges.

For profiling tools, trying to get back sizes per allocated type based on sampling gives random values.


## Solution

The main problem comes from the way the sampling is triggered, based on a constant threshold and without the opportunity to "see" allocations within an allocation context.

TODO: describe the Poisson process mathematical usage

In order to cover the full geometric distribution, it is needed to be able to detect small allocation within an allocation context. This is why the fast path code needs to exit when sampling is needed but without too much impact: if the required allocation size fits within the allocation context, the execution resumes the fast path after the sampled allocation is notified. Otherwise, the current slow path runs to allocate a new allocation context.

In both cases, a new sampling limit is computed: if it ends up outside of the allocation context, its value will be set to the allocation context **alloc_limit**.

It is possible to expose the sampled allocations in different ways:
1. Add a new function to ICorProfilerCallback: this requires to implement a profiler. Note that attaching a profiler with a sampling probability of 1 would allow a 100% allocation recording possibility without the need to restart the application like it is today with **ICorProfilerCallback::ObjectAllocated**.

2. Emit a new event through EventPipe: out of process tools are able to collect the information. For example, dotnet-trace could collect the events in a trace that will be analyzed later in Perfview. Third parties would be able to create new tools to provide information on the fly while events are received.

The second options provides better opportunities to build tools and integrate into the existing ones such as Perfview.

**NOTE: if the keyword/verbosity is not enabled, there is no change with the current fast path.**

## Event - Keyword, verbosity and payload definition

A new *AllocationSampling* event (id=303) will be emitted by the .NET runtime provider under the *AllocationSamplingKeyword*=0x80000000000 keyword with **informational** verbosity.

**TODO: should this event still be emitted under the GCKeyword?
In that case, it should be renaned GCAllocationSampling with id=210**

The payload should contain the following information:
```xml  <template tid="AllocationSampling">
    <data name="AllocationKind" inType="win:UInt32" map="GCAllocationKindMap" />
    <data name="ClrInstanceID" inType="win:UInt16" />
    <data name="AllocationAmount64" inType="win:UInt64" outType="win:HexInt64" />
    <data name="TypeID" inType="win:Pointer" />
    <data name="TypeName" inType="win:UnicodeString" />
    <data name="HeapIndex" inType="win:UInt32" />
    <data name="Address" inType="win:Pointer" />
    <data name="ObjectSize" inType="win:UInt64" outType="win:HexInt64" />

    <UserData>
      <AllocationSampling xmlns="myNs">
        <AllocationKind> %1 </AllocationKind>
        <ClrInstanceID> %2 </ClrInstanceID>
        <TypeID> %3 </TypeID>
        <TypeName> %4 </TypeName>
        <HeapIndex> %5 </HeapIndex>
        <Address> %6 </Address>
        <ObjectSize> %7 </ObjectSize>
      </AllocationSampling>
    </UserData>
</template>
```
**NOTE: If it is possible to dynamically change the sampling rate, it could be interesting to add it to the payload**

**TODO: I'm not sure we will be able to know it if a profiler API is provided to change it dynamically. The current value might not be the one used to compute the threshold of the sampled allocation**

The existing *AllocationTick* event will not be changed.


## Implementation details
The VM has been updated to extract the sampling trigger from the GC unlike what is done for the AllocationTick event.

**1) VM changes**

The major change is the addition of the **alloc_sampling** field to the Execution Engine allocation context used in the fast path.
When the allocation of an object requires a new allocation context, a sampling threshold is computed following an geometric distribution:
- if this size fits inside the allocation context retrieved from the GC, **alloc_sampling** will be equal to **alloc_ptr + size**,
- if not, **alloc_sampling** will be equal to **alloc_limit**.

The fast path code checks the required allocation size against **alloc_sampling - alloc_ptr** to execute the slow path:
- if **alloc_sampling == alloc_limit**, no sampling event is emitted
- if there is not enough space in the allocation context (i.e. **alloc_limit - alloc_ptr < required allocation size**), a new allocation context is retrieved from the GC and a new sampling threshold is computed
- otherwise, a new sampling threshold is computed

**TODO: For LOH and POH, since the current thread allocation context is not used (only its alloc_bytes_uoh field will be updated), 2 global thresholds should be used instead.**


**2) GC internal changes**

There is no planned change in the GC.
The only impact of this change is to recompute the value of the **alloc_sampling** if the GC updates an allocation context. For example, this is happening at the end of a collection when the **alloc_ptr**/**alloc_limit** fields are updated.

**3) Other changes**

The new *AllocationSampling* event needs to be defined in ClrEtwAll.man.

TODO: is there anything to do in ClrEtwAllMeta.lst?


Perfview should be updated to recognize the new event and leverage its payload for more accurate memory allocations analysis.



## Future APIs to take advantage of the dynamic allocation sampling

Due to the dynamic nature of allocations in an application, the sampling rate based on the mean value of the geometric distribution might need to be adjusted. Different reasons exist to allow a profiler to change this mean value over time:
- in case of a limited number of allocations, it could be interesting to increase the sampling rate (i.e. reducing the distribution mean) to get a more correct representation of the allocations
- in case of a large number of allocations, the frequency of the sampling could be to high and start to impact the performance of the application

The following function could be added to ICorProfilerInfo:
```cpp
ICorProfilerInfo::SetSamplingMean(size_t meanSize)
{
    // The meanSize parameter corresponds to the mean of the geometric
    // distribution we are looking for.
    // this alignment.
    ...
}
```

Setting this value should not impact the current thresholds of the existing allocation contexts: it will only impact the computation of the trigger threshold for the new allocation contexts.

**This is not in the scope of the current implementation**

