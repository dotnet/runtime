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
#include "shash.h"
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

#include "GCMemoryHelpers.inl"

#if defined(USE_PORTABLE_HELPERS)
EXTERN_C NATIVEAOT_API void* REDHAWK_CALLCONV RhpGcAlloc(MethodTable *pEEType, uint32_t uFlags, uintptr_t numElements, void * pTransitionFrame);

static Object* AllocateObject(MethodTable* pEEType, uint32_t uFlags, uintptr_t numElements)
{
    Object* pObject = (Object*)RhpGcAlloc(pEEType, uFlags, numElements, nullptr);
    if (pObject == nullptr)
    {
        ASSERT_UNCONDITIONALLY("NYI");  // TODO: Throw OOM
    }

    return pObject;
}

struct gc_alloc_context
{
    uint8_t* alloc_ptr;
    uint8_t* alloc_limit;
};

//
// Allocations
//
COOP_PINVOKE_HELPER(Object *, RhpNewFast, (MethodTable* pEEType))
{
    ASSERT(!pEEType->HasFinalizer());

    Thread * pCurThread = ThreadStore::GetCurrentThread();
    gc_alloc_context * acontext = pCurThread->GetAllocContext();
    size_t size = pEEType->GetBaseSize();

    uint8_t* alloc_ptr = acontext->alloc_ptr;
    ASSERT(alloc_ptr <= acontext->alloc_limit);
    if ((size_t)(acontext->alloc_limit - alloc_ptr) >= size)
    {
        acontext->alloc_ptr = alloc_ptr + size;
        Object* pObject = (Object *)alloc_ptr;
        pObject->set_EEType(pEEType);
        return pObject;
    }

    return AllocateObject(pEEType, 0, 0);
}

#define GC_ALLOC_FINALIZE    0x1 // TODO: Defined in gc.h
#define GC_ALLOC_ALIGN8_BIAS 0x4 // TODO: Defined in gc.h
#define GC_ALLOC_ALIGN8      0x8 // TODO: Defined in gc.h

COOP_PINVOKE_HELPER(Object *, RhpNewFinalizable, (MethodTable* pEEType))
{
    ASSERT(pEEType->HasFinalizer());
    return AllocateObject(pEEType, GC_ALLOC_FINALIZE, 0);
}

COOP_PINVOKE_HELPER(Array *, RhpNewArray, (MethodTable * pArrayEEType, int numElements))
{
    Thread * pCurThread = ThreadStore::GetCurrentThread();
    gc_alloc_context * acontext = pCurThread->GetAllocContext();

    if (numElements < 0)
    {
        ASSERT_UNCONDITIONALLY("NYI");  // TODO: Throw overflow
    }

#ifndef HOST_64BIT
    // if the element count is <= 0x10000, no overflow is possible because the component size is
    // <= 0xffff, and thus the product is <= 0xffff0000, and the base size is only ~12 bytes
    if (numElements > 0x10000)
    {
        // Overflow here should result in an OOM. Let the slow path take care of it.
        return (Array*)AllocateObject(pArrayEEType, 0, numElements);
    }
#endif // !HOST_64BIT

    size_t size = (size_t)pArrayEEType->GetBaseSize() + ((size_t)numElements * (size_t)pArrayEEType->RawGetComponentSize());
    size = ALIGN_UP(size, sizeof(uintptr_t));

    uint8_t* alloc_ptr = acontext->alloc_ptr;
    ASSERT(alloc_ptr <= acontext->alloc_limit);
    if ((size_t)(acontext->alloc_limit - alloc_ptr) >= size)
    {
        acontext->alloc_ptr = alloc_ptr + size;
        Array* pObject = (Array*)alloc_ptr;
        pObject->set_EEType(pArrayEEType);
        pObject->InitArrayLength((uint32_t)numElements);
        return pObject;
    }

    return (Array*)AllocateObject(pArrayEEType, 0, numElements);
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
    return AllocateObject(pEEType, GC_ALLOC_FINALIZE | GC_ALLOC_ALIGN8, 0);
}

#ifndef HOST_64BIT
COOP_PINVOKE_HELPER(Object*, RhpNewFastAlign8, (MethodTable* pEEType))
{
    ASSERT(!pEEType->HasFinalizer());

    Thread* pCurThread = ThreadStore::GetCurrentThread();
    gc_alloc_context* acontext = pCurThread->GetAllocContext();

    size_t size = pEEType->GetBaseSize();
    size = (size + (sizeof(uintptr_t) - 1)) & ~(sizeof(uintptr_t) - 1);

    uint8_t* alloc_ptr = acontext->alloc_ptr;
    int requiresPadding = ((uint32_t)alloc_ptr) & 7;
    size_t paddedSize = size;
    if (requiresPadding)
    {
        paddedSize += 12;
    }

    ASSERT(alloc_ptr <= acontext->alloc_limit);
    if ((size_t)(acontext->alloc_limit - alloc_ptr) >= paddedSize)
    {
        acontext->alloc_ptr = alloc_ptr + paddedSize;
        if (requiresPadding)
        {
            Object* dummy = (Object*)alloc_ptr;
            dummy->set_EEType(g_pFreeObjectEEType);
            alloc_ptr += 12;
        }
        Object* pObject = (Object *)alloc_ptr;
        pObject->set_EEType(pEEType);
        return pObject;
    }

    return AllocateObject(pEEType, GC_ALLOC_ALIGN8, 0);
}

COOP_PINVOKE_HELPER(Object*, RhpNewFastMisalign, (MethodTable* pEEType))
{
    Thread* pCurThread = ThreadStore::GetCurrentThread();
    gc_alloc_context* acontext = pCurThread->GetAllocContext();

    size_t size = pEEType->GetBaseSize();

    uint8_t* alloc_ptr = acontext->alloc_ptr;
    int requiresPadding = (((uint32_t)alloc_ptr) & 7) != 4;
    size_t paddedSize = size;
    if (requiresPadding)
    {
        paddedSize += 12;
    }

    ASSERT(alloc_ptr <= acontext->alloc_limit);
    if ((size_t)(acontext->alloc_limit - alloc_ptr) >= paddedSize)
    {
        acontext->alloc_ptr = alloc_ptr + paddedSize;
        if (requiresPadding)
        {
            Object* dummy = (Object*)alloc_ptr;
            dummy->set_EEType(g_pFreeObjectEEType);
            alloc_ptr += 12;
        }
        Object* pObject = (Object *)alloc_ptr;
        pObject->set_EEType(pEEType);
        return pObject;
    }

    return AllocateObject(pEEType, GC_ALLOC_ALIGN8 | GC_ALLOC_ALIGN8_BIAS, 0);
}

COOP_PINVOKE_HELPER(Array*, RhpNewArrayAlign8, (MethodTable* pArrayEEType, int numElements))
{
    Thread* pCurThread = ThreadStore::GetCurrentThread();
    gc_alloc_context* acontext = pCurThread->GetAllocContext();

    if (numElements < 0)
    {
        ASSERT_UNCONDITIONALLY("NYI");  // TODO: Throw overflow
    }

    // if the element count is <= 0x10000, no overflow is possible because the component size is
    // <= 0xffff, and thus the product is <= 0xffff0000, and the base size is only ~12 bytes
    if (numElements > 0x10000)
    {
        // Overflow here should result in an OOM. Let the slow path take care of it.
        return (Array*)AllocateObject(pArrayEEType, GC_ALLOC_ALIGN8, numElements);
    }

    uint32_t baseSize = pArrayEEType->GetBaseSize();
    size_t size = (size_t)baseSize + ((size_t)numElements * (size_t)pArrayEEType->RawGetComponentSize());
    size = ALIGN_UP(size, sizeof(uintptr_t));

    uint8_t* alloc_ptr = acontext->alloc_ptr;
    int requiresAlignObject = ((uint32_t)alloc_ptr) & 7;
    size_t paddedSize = size;
    if (requiresAlignObject)
    {
        paddedSize += 12;
    }

    ASSERT(alloc_ptr <= acontext->alloc_limit);
    if ((size_t)(acontext->alloc_limit - alloc_ptr) >= paddedSize)
    {
        acontext->alloc_ptr = alloc_ptr + paddedSize;
        if (requiresAlignObject)
        {
            Object* dummy = (Object*)alloc_ptr;
            dummy->set_EEType(g_pFreeObjectEEType);
            alloc_ptr += 12;
        }
        Array* pObject = (Array*)alloc_ptr;
        pObject->set_EEType(pArrayEEType);
        pObject->InitArrayLength((uint32_t)numElements);
        return pObject;
    }

    return (Array*)AllocateObject(pArrayEEType, GC_ALLOC_ALIGN8, numElements);
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
