// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#include "common.h"

#include "CommonTypes.h"
#include "CommonMacros.h"
#include "daccess.h"
#include "PalRedhawkCommon.h"
#include "CommonMacros.inl"
#include "volatile.h"
#include "PalRedhawk.h"
#include "rhassert.h"

#include "slist.h"
#include "gcrhinterface.h"
#include "shash.h"
#include "RWLock.h"
#include "varint.h"
#include "holder.h"
#include "rhbinder.h"
#include "Crst.h"
#include "RuntimeInstance.h"
#include "event.h"
#include "regdisplay.h"
#include "StackFrameIterator.h"
#include "thread.h"
#include "threadstore.h"
#include "threadstore.inl"

#include "MethodTable.h"
#include "TypeManager.h"
#include "MethodTable.inl"
#include "ObjectLayout.h"

#include "GCMemoryHelpers.h"
#include "GCMemoryHelpers.inl"

#if defined(USE_PORTABLE_HELPERS)
EXTERN_C NATIVEAOT_API void* REDHAWK_CALLCONV RhpGcAlloc(MethodTable *pEEType, uint32_t uFlags, uintptr_t numElements, void * pTransitionFrame);

struct gc_alloc_context
{
    uint8_t*         alloc_ptr;
    uint8_t*         alloc_limit;
    __int64        alloc_bytes; //Number of bytes allocated on SOH by this context
    __int64        alloc_bytes_loh; //Number of bytes allocated on LOH by this context
    void*          gc_reserved_1;
    void*          gc_reserved_2;
    int            alloc_count;
};

//
// Allocations
//
COOP_PINVOKE_HELPER(Object *, RhpNewFast, (MethodTable* pEEType))
{
    ASSERT(!pEEType->HasFinalizer());

    Thread * pCurThread = ThreadStore::GetCurrentThread();
    gc_alloc_context * acontext = pCurThread->GetAllocContext();
    Object * pObject;

    size_t size = pEEType->get_BaseSize();

    uint8_t* alloc_ptr = acontext->alloc_ptr;
    ASSERT(alloc_ptr <= acontext->alloc_limit);
    if ((size_t)(acontext->alloc_limit - alloc_ptr) >= size)
    {
        acontext->alloc_ptr = alloc_ptr + size;
        pObject = (Object *)alloc_ptr;
        pObject->set_EEType(pEEType);
        return pObject;
    }

    pObject = (Object *)RhpGcAlloc(pEEType, 0, size, NULL);
    if (pObject == nullptr)
    {
        ASSERT_UNCONDITIONALLY("NYI");  // TODO: Throw OOM
    }

    return pObject;
}

#define GC_ALLOC_FINALIZE    0x1 // TODO: Defined in gc.h
#define GC_ALLOC_ALIGN8_BIAS 0x4 // TODO: Defined in gc.h
#define GC_ALLOC_ALIGN8      0x8 // TODO: Defined in gc.h

COOP_PINVOKE_HELPER(Object *, RhpNewFinalizable, (MethodTable* pEEType))
{
    ASSERT(pEEType->HasFinalizer());

    size_t size = pEEType->get_BaseSize();

    Object * pObject = (Object *)RhpGcAlloc(pEEType, GC_ALLOC_FINALIZE, size, NULL);
    if (pObject == nullptr)
    {
        ASSERT_UNCONDITIONALLY("NYI");  // TODO: Throw OOM
    }

    return pObject;
}

COOP_PINVOKE_HELPER(Array *, RhpNewArray, (MethodTable * pArrayEEType, int numElements))
{
    Thread * pCurThread = ThreadStore::GetCurrentThread();
    gc_alloc_context * acontext = pCurThread->GetAllocContext();
    Array * pObject;

    if (numElements < 0)
    {
        ASSERT_UNCONDITIONALLY("NYI");  // TODO: Throw overflow
    }

    size_t size;
#ifndef HOST_64BIT
    // if the element count is <= 0x10000, no overflow is possible because the component size is
    // <= 0xffff, and thus the product is <= 0xffff0000, and the base size is only ~12 bytes
    if (numElements > 0x10000)
    {
        // Perform the size computation using 64-bit integeres to detect overflow
        uint64_t size64 = (uint64_t)pArrayEEType->get_BaseSize() + ((uint64_t)numElements * (uint64_t)pArrayEEType->get_ComponentSize());
        size64 = (size64 + (sizeof(uintptr_t)-1)) & ~(sizeof(uintptr_t)-1);

        size = (size_t)size64;
        if (size != size64)
        {
            ASSERT_UNCONDITIONALLY("NYI");  // TODO: Throw overflow
        }
    }
    else
#endif // !HOST_64BIT
    {
        size = (size_t)pArrayEEType->get_BaseSize() + ((size_t)numElements * (size_t)pArrayEEType->get_ComponentSize());
        size = ALIGN_UP(size, sizeof(uintptr_t));
    }

    uint8_t* alloc_ptr = acontext->alloc_ptr;
    ASSERT(alloc_ptr <= acontext->alloc_limit);
    if ((size_t)(acontext->alloc_limit - alloc_ptr) >= size)
    {
        acontext->alloc_ptr = alloc_ptr + size;
        pObject = (Array *)alloc_ptr;
        pObject->set_EEType(pArrayEEType);
        pObject->InitArrayLength((uint32_t)numElements);
        return pObject;
    }

    pObject = (Array *)RhpGcAlloc(pArrayEEType, 0, size, NULL);
    if (pObject == nullptr)
    {
        ASSERT_UNCONDITIONALLY("NYI");  // TODO: Throw OOM
    }

    return pObject;
}

COOP_PINVOKE_HELPER(String *, RhNewString, (MethodTable * pArrayEEType, int numElements))
{
    // TODO: Implement. We tail call to RhpNewArray for now since there's a bunch of TODOs in the places
    // that matter anyway.
    return (String*)RhpNewArray(pArrayEEType, numElements);
}

#endif
#if defined(USE_PORTABLE_HELPERS)
#if defined(FEATURE_64BIT_ALIGNMENT)

GPTR_DECL(MethodTable, g_pFreeObjectEEType);

COOP_PINVOKE_HELPER(Object *, RhpNewFinalizableAlign8, (MethodTable* pEEType))
{
    Object * pObject = nullptr;
    /* Not reachable as finalizable types are never align8 */ ASSERT_UNCONDITIONALLY("UNREACHABLE");
    return pObject;
}

#ifndef HOST_64BIT
COOP_PINVOKE_HELPER(Object *, RhpNewFastAlign8, (MethodTable* pEEType))
{
    ASSERT(!pEEType->HasFinalizer());

    Thread* pCurThread = ThreadStore::GetCurrentThread();
    gc_alloc_context* acontext = pCurThread->GetAllocContext();
    Object* pObject;

    size_t size = pEEType->get_BaseSize();
    size = (size + (sizeof(uintptr_t) - 1)) & ~(sizeof(uintptr_t) - 1);

    uint8_t* result = acontext->alloc_ptr;

    int requiresPadding = ((uint32_t)result) & 7;
    size_t paddedSize = size;
    if (requiresPadding)
    {
        if(paddedSize > SIZE_MAX - 12)
        {
            ASSERT_UNCONDITIONALLY("NYI");  // TODO: Throw overflow
        }
        paddedSize += 12;
    }

    uint8_t* alloc_ptr = acontext->alloc_ptr;
    ASSERT(alloc_ptr <= acontext->alloc_limit);
    if ((size_t)(acontext->alloc_limit - alloc_ptr) >= paddedSize)
    {
        acontext->alloc_ptr = alloc_ptr + paddedSize;
        if (requiresPadding)
        {
            Object* dummy = (Object*)alloc_ptr;
            dummy->set_EEType(g_pFreeObjectEEType);
            alloc_ptr += 12; // if result + paddedSize was ok, then cant overflow
        }
        pObject = (Object *)alloc_ptr;
        pObject->set_EEType(pEEType);
        return pObject;
    }

    pObject = (Object*)RhpGcAlloc(pEEType, GC_ALLOC_ALIGN8, size, NULL);
    if (pObject == nullptr)
    {
        ASSERT_UNCONDITIONALLY("NYI");  // TODO: Throw OOM
    }

    return pObject;
}

COOP_PINVOKE_HELPER(Object*, RhpNewFastMisalign, (MethodTable* pEEType))
{
    Thread* pCurThread = ThreadStore::GetCurrentThread();
    gc_alloc_context* acontext = pCurThread->GetAllocContext();
    Object* pObject;

    size_t size = pEEType->get_BaseSize();
    uint8_t* result = acontext->alloc_ptr;

    int requiresPadding = (((uint32_t)result) & 7) != 4;
    size_t paddedSize = size;
    if (requiresPadding)
    {
        if(paddedSize > SIZE_MAX - 12)
        {
            ASSERT_UNCONDITIONALLY("NYI");  // TODO: Throw overflow
        }
        paddedSize += 12;
    }
    uint8_t* alloc_ptr = acontext->alloc_ptr;
    ASSERT(alloc_ptr <= acontext->alloc_limit);
    if ((size_t)(acontext->alloc_limit - alloc_ptr) >= paddedSize)
    {
        acontext->alloc_ptr = alloc_ptr + paddedSize;
        if (requiresPadding)
        {
            Object* dummy = (Object*)alloc_ptr;
            dummy->set_EEType(g_pFreeObjectEEType);
            alloc_ptr += 12; // if result + paddedSize was ok, then cant overflow
        }
        pObject = (Object *)alloc_ptr;
        pObject->set_EEType(pEEType);
        return pObject;
    }

    pObject = (Object*)RhpGcAlloc(pEEType, GC_ALLOC_ALIGN8 | GC_ALLOC_ALIGN8_BIAS, size, NULL);
    if (pObject == nullptr)
    {
        ASSERT_UNCONDITIONALLY("NYI");  // TODO: Throw OOM
    }

    return pObject;
}

COOP_PINVOKE_HELPER(Array *, RhpNewArrayAlign8, (MethodTable * pArrayEEType, int numElements))
{
    Thread* pCurThread = ThreadStore::GetCurrentThread();
    gc_alloc_context* acontext = pCurThread->GetAllocContext();
    Array* pObject;

    if (numElements < 0)
    {
        ASSERT_UNCONDITIONALLY("NYI");  // TODO: Throw overflow
    }

    size_t size;

    uint32_t baseSize = pArrayEEType->get_BaseSize();
    // if the element count is <= 0x10000, no overflow is possible because the component size is
    // <= 0xffff, and thus the product is <= 0xffff0000, and the base size is only ~12 bytes
    if (numElements > 0x10000)
    {
        // Perform the size computation using 64-bit integeres to detect overflow
        uint64_t size64 = (uint64_t)baseSize + ((uint64_t)numElements * (uint64_t)pArrayEEType->get_ComponentSize());
        size64 = (size64 + (sizeof(uintptr_t) - 1)) & ~(sizeof(uintptr_t) - 1);

        size = (size_t)size64;
        if (size != size64)
        {
            ASSERT_UNCONDITIONALLY("NYI");  // TODO: Throw overflow
        }
    }
    else
    {
        size = (size_t)baseSize + ((size_t)numElements * (size_t)pArrayEEType->get_ComponentSize());
        size = ALIGN_UP(size, sizeof(uintptr_t));
    }
    uint8_t* result = acontext->alloc_ptr;
    int requiresAlignObject = ((uint32_t)result) & 7;
    size_t paddedSize = size;
    if (requiresAlignObject)
    {
        if(paddedSize > SIZE_MAX - 12)
        {
            ASSERT_UNCONDITIONALLY("NYI");  // TODO: Throw overflow
        }
        paddedSize += 12;
    }
    uint8_t* alloc_ptr = acontext->alloc_ptr;
    ASSERT(alloc_ptr <= acontext->alloc_limit);
    if ((size_t)(acontext->alloc_limit - alloc_ptr) >= paddedSize)
    {
        acontext->alloc_ptr = alloc_ptr + paddedSize;
        if (requiresAlignObject)
        {
            Object* dummy = (Object*)alloc_ptr;
            dummy->set_EEType(g_pFreeObjectEEType);
            alloc_ptr += 12; // if result + paddedSize was ok, then cant overflow
        }
        pObject = (Array*)alloc_ptr;
        pObject->set_EEType(pArrayEEType);
        pObject->InitArrayLength((uint32_t)numElements);
        return pObject;
    }

    pObject = (Array*)RhpGcAlloc(pArrayEEType, GC_ALLOC_ALIGN8, size, NULL);
    if (pObject == nullptr)
    {
        ASSERT_UNCONDITIONALLY("NYI");  // TODO: Throw OOM
    }

    return pObject;
}
#endif // !HOST_64BIT
#endif // defined(HOST_ARM) || defined(HOST_WASM)

COOP_PINVOKE_HELPER(void, RhpInitialDynamicInterfaceDispatch, ())
{
    ASSERT_UNCONDITIONALLY("NYI");
}

COOP_PINVOKE_HELPER(void, RhpInterfaceDispatch1, ())
{
    ASSERT_UNCONDITIONALLY("NYI");
}

COOP_PINVOKE_HELPER(void, RhpInterfaceDispatch2, ())
{
    ASSERT_UNCONDITIONALLY("NYI");
}

COOP_PINVOKE_HELPER(void, RhpInterfaceDispatch4, ())
{
    ASSERT_UNCONDITIONALLY("NYI");
}

COOP_PINVOKE_HELPER(void, RhpInterfaceDispatch8, ())
{
    ASSERT_UNCONDITIONALLY("NYI");
}

COOP_PINVOKE_HELPER(void, RhpInterfaceDispatch16, ())
{
    ASSERT_UNCONDITIONALLY("NYI");
}

COOP_PINVOKE_HELPER(void, RhpInterfaceDispatch32, ())
{
    ASSERT_UNCONDITIONALLY("NYI");
}

COOP_PINVOKE_HELPER(void, RhpInterfaceDispatch64, ())
{
    ASSERT_UNCONDITIONALLY("NYI");
}

COOP_PINVOKE_HELPER(void, RhpVTableOffsetDispatch, ())
{
    ASSERT_UNCONDITIONALLY("NYI");
}

// @TODO Implement UniversalTransition
EXTERN_C void * ReturnFromUniversalTransition;
void * ReturnFromUniversalTransition;

// @TODO Implement UniversalTransition_DebugStepTailCall
EXTERN_C void * ReturnFromUniversalTransition_DebugStepTailCall;
void * ReturnFromUniversalTransition_DebugStepTailCall;

#endif // USE_PORTABLE_HELPERS

// @TODO Implement CallDescrThunk
EXTERN_C void * ReturnFromCallDescrThunk;
#ifdef USE_PORTABLE_HELPERS
void * ReturnFromCallDescrThunk;
#endif

#if defined(USE_PORTABLE_HELPERS)
//
// Return address hijacking
//
COOP_PINVOKE_HELPER(void, RhpGcStressHijack, ())
{
    ASSERT_UNCONDITIONALLY("NYI");
}

COOP_PINVOKE_HELPER(void, RhpGcProbeHijack, ())
{
    ASSERT_UNCONDITIONALLY("NYI");
}

#endif // defined(USE_PORTABLE_HELPERS) || defined(TARGET_UNIX)

#if defined(USE_PORTABLE_HELPERS)

#if !defined (HOST_ARM64)
COOP_PINVOKE_HELPER(void, RhpAssignRef, (Object ** dst, Object * ref))
{
    // @TODO: USE_PORTABLE_HELPERS - Null check
    *dst = ref;
    InlineWriteBarrier(dst, ref);
}

COOP_PINVOKE_HELPER(void, RhpCheckedAssignRef, (Object ** dst, Object * ref))
{
    // @TODO: USE_PORTABLE_HELPERS - Null check
    *dst = ref;
    InlineCheckedWriteBarrier(dst, ref);
}
#endif

COOP_PINVOKE_HELPER(Object *, RhpCheckedLockCmpXchg, (Object ** location, Object * value, Object * comparand))
{
    // @TODO: USE_PORTABLE_HELPERS - Null check
    Object * ret = (Object *)PalInterlockedCompareExchangePointer((void * volatile *)location, value, comparand);
    InlineCheckedWriteBarrier(location, value);
    return ret;
}

COOP_PINVOKE_HELPER(Object *, RhpCheckedXchg, (Object ** location, Object * value))
{
    // @TODO: USE_PORTABLE_HELPERS - Null check
    Object * ret = (Object *)PalInterlockedExchangePointer((void * volatile *)location, value);
    InlineCheckedWriteBarrier(location, value);
    return ret;
}

COOP_PINVOKE_HELPER(int32_t, RhpLockCmpXchg32, (int32_t * location, int32_t value, int32_t comparand))
{
    // @TODO: USE_PORTABLE_HELPERS - Null check
    return PalInterlockedCompareExchange(location, value, comparand);
}

COOP_PINVOKE_HELPER(int64_t, RhpLockCmpXchg64, (int64_t * location, int64_t value, int64_t comparand))
{
    // @TODO: USE_PORTABLE_HELPERS - Null check
    return PalInterlockedCompareExchange64(location, value, comparand);
}

#endif // USE_PORTABLE_HELPERS

COOP_PINVOKE_HELPER(void, RhpMemoryBarrier, ())
{
    PalMemoryBarrier();
}

#if defined(USE_PORTABLE_HELPERS)
EXTERN_C NATIVEAOT_API void* __cdecl RhAllocateThunksMapping()
{
    return NULL;
}

COOP_PINVOKE_HELPER(void *, RhpGetThunksBase, ())
{
    return NULL;
}

COOP_PINVOKE_HELPER(int, RhpGetNumThunkBlocksPerMapping, ())
{
    ASSERT_UNCONDITIONALLY("NYI");
    return 0;
}

COOP_PINVOKE_HELPER(int, RhpGetNumThunksPerBlock, ())
{
    ASSERT_UNCONDITIONALLY("NYI");
    return 0;
}

COOP_PINVOKE_HELPER(int, RhpGetThunkSize, ())
{
    ASSERT_UNCONDITIONALLY("NYI");
    return 0;
}

COOP_PINVOKE_HELPER(void*, RhpGetThunkDataBlockAddress, (void* pThunkStubAddress))
{
    ASSERT_UNCONDITIONALLY("NYI");
    return NULL;
}

COOP_PINVOKE_HELPER(void*, RhpGetThunkStubsBlockAddress, (void* pThunkDataAddress))
{
    ASSERT_UNCONDITIONALLY("NYI");
    return NULL;
}

COOP_PINVOKE_HELPER(int, RhpGetThunkBlockSize, ())
{
    ASSERT_UNCONDITIONALLY("NYI");
    return 0;
}

COOP_PINVOKE_HELPER(void, RhCallDescrWorker, (void * callDescr))
{
    ASSERT_UNCONDITIONALLY("NYI");
}

COOP_PINVOKE_HELPER(void *, RhGetCommonStubAddress, ())
{
    ASSERT_UNCONDITIONALLY("NYI");
    return NULL;
}

COOP_PINVOKE_HELPER(void *, RhGetCurrentThunkContext, ())
{
    ASSERT_UNCONDITIONALLY("NYI");
    return NULL;
}

COOP_PINVOKE_HELPER(void, RhpGcPoll, ())
{
    // TODO: implement
}

#endif
