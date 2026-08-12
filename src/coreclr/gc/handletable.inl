// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

//

#ifndef _HANDLETABLE_INL
#define _HANDLETABLE_INL

#ifndef DACCESS_COMPILE
FORCEINLINE void HndWriteBarrierWorker(OBJECTHANDLE handle, _UNCHECKED_OBJECTREF value)
{
    _ASSERTE(value != NULL);

    uint8_t* barrier = (uint8_t*)((uintptr_t)handle & HANDLE_SEGMENT_ALIGN_MASK);
    _ASSERTE(barrier);

    uintptr_t offset = (uintptr_t)handle & HANDLE_SEGMENT_CONTENT_MASK;
    _ASSERTE(offset >= HANDLE_HEADER_SIZE);

    offset = (offset - HANDLE_HEADER_SIZE) / (HANDLE_SIZE * HANDLE_HANDLES_PER_CLUMP);

    // Be careful to read and write the age byte via volatile operations. Otherwise the compiler has been
    // observed to translate the read + conditional write sequence below into an unconditional read/write
    // (utilizing a conditional register move to determine whether the write is an update or simply writes
    // back what was read). This is a legal transformation for non-volatile accesses but obviously leads to a
    // race condition where we can lose an update (see the comment below for the race condition).
    volatile uint8_t* pClumpAge = barrier + offset;

    if (*pClumpAge != 0)
    {
        int generation = GetConvertedGeneration(value);

        if (generation == 0)
        {
            *pClumpAge = 0;
            return;
        }

        uint32_t uType = HandleFetchType(handle);

#ifdef FEATURE_ASYNC_PINNED_HANDLES
        //OverlappedData need special treatment: because all user data pointed by it needs to be reported by this handle,
        //its age is consider to be min age of the user data, to be simple, we just make it 0
        if (uType == HNDTYPE_ASYNCPINNED)
        {
            generation = 0;
        }
#endif

        if (uType == HNDTYPE_DEPENDENT)
        {
            generation = 0;
        }

        if (*pClumpAge > (uint8_t)generation)
        {
            // We have to be careful here. HndWriteBarrier is not under any synchronization
            // Consider the scenario where 2 threads are hitting the line below at the same
            // time. Only one will win. If the winner has an older age than the loser, we
            // just created a potential GC hole  (The clump will not be reporting the
            // youngest handle in the clump, thus GC may skip the clump). To fix this
            // we just set the clump age to 0, which means that whoever wins the race
            // results are the same, as GC will always look at the clump
            *pClumpAge = (uint8_t)0;
        }
    }
}
#else
void HndWriteBarrierWorker(OBJECTHANDLE handle, _UNCHECKED_OBJECTREF value);
#endif // DACCESS_COMPILE

inline void HndWriteBarrier(OBJECTHANDLE handle, OBJECTREF objref)
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_MODE_COOPERATIVE;

    // unwrap the objectref we were given
    _UNCHECKED_OBJECTREF value = OBJECTREF_TO_UNCHECKED_OBJECTREF(objref);

    _ASSERTE (objref != NULL);

    HndWriteBarrierWorker(handle, value);
}

inline void HndAssignHandle(OBJECTHANDLE handle, OBJECTREF objref)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    // sanity
    _ASSERTE(handle);

    // unwrap the objectref we were given
    _UNCHECKED_OBJECTREF value = OBJECTREF_TO_UNCHECKED_OBJECTREF(objref);

    HndLogSetEvent(handle, value);

    // if we are doing a non-NULL pointer store then invoke the write-barrier
    if (value)
        HndWriteBarrierWorker(handle, value);

    // Store the pointer with release semantics so object field writes are visible
    // before the handle can publish the object to another thread.
    VolatileStore((_UNCHECKED_OBJECTREF *)handle, value);
}

// This is used by the GC before we actually construct the object so we cannot
// do the normal object verification.
inline void HndAssignHandleGC(OBJECTHANDLE handle, uint8_t* objref)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    // sanity
    _ASSERTE(handle);

    // unwrap the objectref we were given
    _UNCHECKED_OBJECTREF value = (_UNCHECKED_OBJECTREF)(Object*)objref;

    // if we are doing a non-NULL pointer store then invoke the write-barrier
    if (value)
        HndWriteBarrierWorker(handle, value);

    // Store the pointer with release semantics so object field writes are visible
    // before the handle can publish the object to another thread.
    VolatileStore((_UNCHECKED_OBJECTREF *)handle, value);
}

inline void* HndInterlockedCompareExchangeHandle(OBJECTHANDLE handle, OBJECTREF objref, OBJECTREF oldObjref)
{
    WRAPPER_NO_CONTRACT;

    // sanity
    _ASSERTE(handle);

    // unwrap the objectref we were given
    _UNCHECKED_OBJECTREF value = OBJECTREF_TO_UNCHECKED_OBJECTREF(objref);
    _UNCHECKED_OBJECTREF oldValue = OBJECTREF_TO_UNCHECKED_OBJECTREF(oldObjref);

    // if we are doing a non-NULL pointer store then invoke the write-barrier
    if (value)
        HndWriteBarrier(handle, objref);

    // store the pointer

    void* ret = Interlocked::CompareExchangePointer(reinterpret_cast<_UNCHECKED_OBJECTREF volatile*>(handle), value, oldValue);

    if (ret == oldValue)
        HndLogSetEvent(handle, value);

    return ret;
}

inline BOOL HndFirstAssignHandle(OBJECTHANDLE handle, OBJECTREF objref)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    // sanity
    _ASSERTE(handle);

    // unwrap the objectref we were given
    _UNCHECKED_OBJECTREF value = OBJECTREF_TO_UNCHECKED_OBJECTREF(objref);
    _UNCHECKED_OBJECTREF null = NULL;

    // store the pointer if we are the first ones here
    BOOL success = (NULL == Interlocked::CompareExchangePointer(reinterpret_cast<_UNCHECKED_OBJECTREF volatile*>(handle),
                                                                value,
                                                                null));

    // if we successfully did a non-NULL pointer store then invoke the write-barrier
    if (success)
    {
        if (value)
            HndWriteBarrier(handle, objref);

        HndLogSetEvent(handle, value);
    }

    // return our result
    return success;
}

#endif // _HANDLETABLE_INL
