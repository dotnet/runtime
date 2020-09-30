# Pinned Heap

## Problem space - the pinning story and its perf drawbacks

The way to pin an object in .NET is by pinning an already allocated object via mechanisms like a pinned GC handle. This means how pins are positioned relative to non pinned objects can be unpredictable. Obviously we advise users to be careful about pinning because it simply hinders GC’s ability to move objects around. Even if every user tried their best to be careful with pinning, as long as you don’t have full control over all the code that does the pinning, you can still run into problems. For example if you make a library call and that library pins objects on your behalf.

We have done a lot of work to combat pinning in GC such as

*  Demotion, meaning we try to keep the pinned objects in gen0 so the free spaces in between them can be reused immediately to satisfy gen0 allocations.

*  POPO (Promote Only Pinned Objects) which actually breaks up a plug if it includes both pinned and non pinned objects so we don’t end up pinning the whole plug if survived pinned objects are adjacent to non pinned objects that might occupy large amounts of memory.

We also have provided framework utilities like <span style="color:red">`PinnableBufferCache`</span> (which only exists in .NET Framework) and modified various framework components to use it.

With these efforts we were able to mitigate perf problems caused by pinned by a lot. But at the end of the day, due to the nature of “pinning already allocated objects”, we cannot eliminate the situation of interleaved pinned and non pinned objects and that can still cause perf problems for GC. And the long lived scattered pins are the most problematic. They can be separated into 2 categories –

<span style="color:blue">**Long lived scattered pins in young generations, especially gen0.**</span>

However, For example, we could have a bunch of long lived pins that get demoted to gen0 with lots of free spaces in between them. And we may not want to reuse all the free spaces (ie, fill all the gen0 free spaces before triggering a GC) because we wouldn’t want to make gen0 pause times too high. We have seen many profiles where the gen0 size is quite significant with a very high fragmentation ratio due to pinning.

<span style="color:blue">**Long lived scattered pins in old generations**</span>

Nowadays we aim to do background GCs most of the time. And since background GCs do not compact, pinning or not makes no difference. But when we do want to compact gen2 this becomes a problem.

We do have plans to make the heap into regions instead of segments which would mitigate this situation because we can repurpose the regions with pins for other generations if needed but while it’s easy to repurpose a region with pins that belongs to an ephemeral generation as a gen2 region, the other way is not so easy because we’d have to care about setting cards. And since we know that these are long lived pins, it actually wasn’t desirable to even demote them to gen0 in the first place – we just did that so we could reuse the free spaces between them sooner.

Another issue, even with regions, is that of course the pins will always mean we’d have to keep the whole region there instead of just release the memory of the region back to the OS (sure we could decommit the pages without the pins in the region – obviously that means more bookkeeping). Having regions that are mostly empty and only occupied by a few pins means we delay releasing memory of these regions.

## Solution

The problem with pinned objects is that even a small number of them can have a significant impact on perf due to their positions relative to non pinned objects. And we do make the assumption that there are way fewer pinned objects than non pinned ones. So it makes sense to group pinned ones in their own group so they do not interleave with non pinned ones and we call this group the Pinned Heap, like how we group small and large objects together, ie, SOH (Small Object Heap) and LOH (Large Object Heap).

In order to group pinned objects together we have a couple of options –

1) We allow pinning at allocation time so when such an object is allocated we put it on the pinned heap.

2) We allow migrating non pinned objects to the pinned heap at the time they need to be pinned.

Option 1) is a much cleaner solution. Of course it also imposes a limitation which is the user needs to have control over the objects they want to pin and is ok with pinning it at allocation time. This is not always the case. However, we believe that this feature is the most important for framework components which often take care of the underlying pinning and they are in general ok with this limitation so we will go with option 1). If we ever decide that it’s important to do 2) we can add it in the future.

An object allocated on the pinned heap can be referenced by other objects normally. It’s pinned by the virtue of being on the pinned heap so we do not distinguish between a pinned reference (like a pinned GC handle) and a non pinned reference. This means as long as the object is still referenced, it will stay on the pinned heap and will not be moved.

## API - allocating an array on the pinned heap

For users who want to allocate their objects pinned, we provide [a new API](https://github.com/dotnet/runtime/issues/27146) to allocate such an object. The API declaration is the following:

```csharp
class GC
{
    // generation: -1 means to let GC decide (equivalent to regular new T[])
    // 0 means to allocate in gen0
    // GC.MaxGeneration means to allocate in the oldest generation
    //
    // pinned: true means you want to pin this object that you are allocating
    // otherwise it's not pinned.
    //
    // alignment: only supported if pinned is true.
    // -1 means you don't care which means it'll use the default alignment.
    // otherwise specify a power of 2 value that's >= pointer size
    // the beginning of the payload of the object (&array[0]) will be aligned with
    // this alignment.
    static T[] AllocateArray<T>(int length,
                                int generation = -1,
                                bool pinned = false,
                                int alignment = -1);
}
```

Note that for pinned heap we are only implementing the relevant part of the API, meaning only 2 arguments will be interpreted – length and pinned. Since this is a user facing feature we impose the following limitations on the objects allocated by this API which will make the implementation cleaner and the perf better while not hindering the majority of the usage:

1) As the name suggests, this API only allows array allocations. Allowing to allocate a non array object is certainly possible but at this time we do not see much value in it so for the first version of this feature we will only allow to allocate arrays.

2) The array cannot contain any references. An object has to be of a blittable type when referenced by a pinned GC handle which means the majority of our usage of pinning is already on blittable types. This means we do not need to trace objects on the pinned heap which is a nice perf characteristic.

## Implementation details

We do not foresee the amount of allocations or frequency of allocations like we see on SOH on the pinned heap. The guidance we’ve been giving our users to only pin when necessary will remain.

If we are solving the problem of long lived pinned objects fragmenting the heap, and providing that users can pin the objects at allocation time, it makes sense to provide a pinned heap. The implementation for this feature is divided into the following parts –

**1) GC internal changes. This feature touches the following**

<span style="color:blue">**_Generation for the pinned heap_**</span>

Similar to LOH which is represented by its own generation in GC, the pinned heap will get its own generation. Note that the LOH generation is special in that its numeric value does not mean it's older than the generations with smaller values. It's a special generation and the pinned heap's generation will follow the same pattern - its numeric value is merely used to index into the <span style="color:red">`generation_table`</span>/<span style="color:red">`dynamic_data_table`</span> arrays. Which generation it logically belongs to (meaning which generation of GC collects the pinned heap) is up to our implementation.

<span style="color:blue">**_Initialization and segment list organization_**</span>

When GC is initialized it reserves the initial segments for SOH and LOH with <span style="color:red">`get_initial_segment`</span>. We can do the same thing for the pinned heap but with a smaller segment size and it will be the first segment for the pinned heap generation. However do note that fragmentation may be a problem on the pinned heap, similar to LOH when we don’t compact it. With the bucketed free list and collecting often enough this may not be a big problem. I would expect it to be a smaller risk than LOH as the buffer sizes that components pin usually aren’t as varied as large objects.

We have various flags for our segments like <span style="color:red">`heap_segment_flags_readonly`</span>. We’d want to add a new one for segments on the pinned heap and use it to filter out phases that should not look at the pinned heap segments. I expect most places will be automatically taken care of as the pinned heap has its own generation.

<span style="color:blue">**_Pinned heap lock_**</span>

For SOH/LOH we have <span style="color:red">`more_space_lock_soh`</span>/<span style="color:red">`more_space_lock_loh`</span> respectively. We will introduce a <span style="color:red">`more_space_lock_ph`</span> for pinned heap.

There’s a question of whether it’s worth keeping an allocation context for allocations on the pinned heap or just do what we do with LOH where each allocation occupies its own alloc context. It’s fine to start with the former.

<span style="color:blue">**_Inside a GC_**</span>

What we need to do for the pinned heap is similar to what we do to compact LOH but simpler (so can look at plan_loh/compact_loh for examples.  What’s in front of an object start look likes this (each field is ptr-size):

<span style="color:red">`G | R | P | S | MT`</span>

G - gap, set by <span style="color:red">`set_gap_size`</span>

R – reloc, set by <span style="color:red">`set_node_relocation_distance`</span>, for pinned heap this is always 0

P – pair (where we store the tree info), not needed for pinned heap as we will not be going through the normal reloc/compact/sweep phase.

S – syncblk

MT - method table pointer, this is where the object start is

LOH plan leaves the marked/pinned bits untouched as they will be cleared during <span style="color:red">`compact_loh`</span>. We can do the same thing for the pinned heap (let’s call it <span style="color:red">`plan_ph`</span>) and all we need is to set the reloc distance to 0 since we are not moving anything.

After GC has done the normal reloc/compact/sweep phase, we can go through the pinned heap and thread our free list out of free objects, similar to <span style="color:red">`compact_loh`</span> except we don’t need to copy anything – all we need is to call <span style="color:red">`thread_gap`</span>. Since we are calling normal sweep phase <span style="color:red">`make_free_lists`</span>, we can call this <span style="color:red">`make_free_lists_ph`</span>.

It’s possible to merge pinned heap’s plan and sweep into one phase but this would require more (and scattered) changes. Considering the low perf requirements for this feature for V1.0 we will not do this.

<span style="color:blue">**_When to collect_**</span>

LOH is only collected during gen2 GCs. Since the pinned heap is supposed to be small we can consider collecting it in gen1 GCs as well. Or have something more dynamic like based on its size relative to the rest of the heap.

<span style="color:blue">**_Accounting_**</span>

The pinned heap is part of the GC heap so when the heap size is requested pinned heap size should be included, eg, <span style="color:red">`GC.GetTotalMemory`</span> should include sizes of objects on the pinned heap.

For generational accounting the pinned heap should be considered its own generation as it has a different perf characteristic from the other generations. For example, it wouldn’t make sense for <span style="color:red">`generation_free_list_space`</span> for pinned heap to be one of the current generations. This way we can also avoid changing the current accounting, eg, <span style="color:red">`decide_on_compacting`</span>/<span style="color:red">`generation_to_condemn`</span> which cares about generational accounting.

<span style="color:blue">**_Free list management_**</span>

This is again very similar to what we do today with LOH allocations which uses a bucketed free list (<span style="color:red">`loh_alloc_list`</span>). We’d want different bucket sizes/probably different # of buckets. This also means

<span style="color:blue">**_BGC consideration_**</span>

Since pinned heap will not have objects with references we will not need to go through <span style="color:red">`revisit_written_pages`</span> for it (which automatically happens as it only goes through gen2/3 segments today). We can let <span style="color:red">`background_sweep`</span> take care of sweeping on pinned heap by including its generation.

<span style="color:blue">**_HardLimit consideration_**</span>

Today HardLimit requires us to reserve all segments when the GC is initialized. And since we cannot predict how much SOH allocation vs LOH allocation we’d have we just reserve 1x for SOH and actually 2x for LOH (we use 1x for LOH when you use large pages). Obviously we do not want to reserve 1x for pinned heap. Since pinned heap should be very small compared to SOH, we could reserve a smaller segment size. Although the reserve size doesn’t matter that much and commit is automatically kept track of. We do need to think more carefully for the large pages case as we need to commit upfront.

**2) BCL/VM changes.**

**3) Framework component changes**

On Desktop CLR we introduced a <span style="color:red">`PinnableBufferCache`</span> that was used by a few framework components which would be good candidates for investigation purpose. Since we are doing the work on coreclr we don’t have those changes but we can do a code review to see whether it’s seamless to use the new API.

## Future APIs to take advantage of the pinned heap

**Batched allocations**

It might make sense to provide an additional API to allocate an array of these objects of the same type/size (eg, it’s common that a pinned buffer pool implementation would want to allocate a batch of <span style="color:red">`byte[512]`</span> and <span style="color:red">`byte[1024]`</span> objects) so we can avoid making an FCall everytime we need to allocate such an object but this is of course assuming the cost of an FCall is significant compared to the runtime portion of the allocation work which we should measure first.

**Alignment support**

However, there is another scenario for high perf that could warrant a generational pinned heap which is objects with a specified alignment mostly [for SIMD operations](https://github.com/dotnet/runtime/issues/22990) or aligning on cache lines to avoid false sharing. This scenario doesn’t imply the object always needs to be pinned, however being pinned does mean they would be convenient for interop. For example, matrix multiplication with SIMD where the matrix is also used in native code. It also isn’t clear the object is necessarily long lived, however we do make the assumption that the amount of memory occupied by pinned objects should be small compared to non pinned objects so treating all of them as part of gen2 is acceptable. A different design option would be to allocate these on the normal heap but keep their alignment when compacting but this makes compaction slower.

For alignment support, we can also limit it to a few specific alignments if it makes the implementation noticeably easier – 16, 32, 64, 128 bytes, and possibly page size (unclear if this is really needed).

