// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "common.h"

#include "CommonTypes.h"
#include "CommonMacros.h"
#include "daccess.h"
#include "PalLimitedContext.h"
#include "CommonMacros.inl"
#include "volatile.h"
#include "Pal.h"
#include "debugmacros.h"

#include "slist.h"
#include "shash.h"
#include "holder.h"
#include "binder.h"
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
EXTERN_C void* GcAlloc(MethodTable *pEEType, uint32_t uFlags, uintptr_t numElements, void * pTransitionFrame);

static Object* AllocateObject(MethodTable* pEEType, uint32_t uFlags, uintptr_t numElements)
{
    Object* pObject = (Object*)GcAlloc(pEEType, uFlags, numElements, nullptr);
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
FCIMPL1(Object *, NewFast, MethodTable* pEEType)
{
    ASSERT(!pEEType->HasFinalizer());

    Thread * pCurThread = ThreadStore::GetCurrentThread();
    gc_alloc_context * acontext = pCurThread->GetAllocContext();
    size_t size = pEEType->GetBaseSize();

    uint8_t* alloc_ptr = acontext->alloc_ptr;
    uint8_t* combined_limit = pCurThread->GetEEAllocContext()->GetCombinedLimit();
    ASSERT(alloc_ptr <= combined_limit);
    if ((size_t)(combined_limit - alloc_ptr) >= size)
    {
        acontext->alloc_ptr = alloc_ptr + size;
        Object* pObject = (Object *)alloc_ptr;
        pObject->set_EEType(pEEType);
        return pObject;
    }

    return AllocateObject(pEEType, 0, 0);
}
FCIMPLEND

#define GC_ALLOC_FINALIZE    0x1 // TODO: Defined in gc.h
#define GC_ALLOC_ALIGN8_BIAS 0x4 // TODO: Defined in gc.h
#define GC_ALLOC_ALIGN8      0x8 // TODO: Defined in gc.h

FCIMPL1(Object *, NewFinalizable, MethodTable* pEEType)
{
    ASSERT(pEEType->HasFinalizer());
    return AllocateObject(pEEType, GC_ALLOC_FINALIZE, 0);
}
FCIMPLEND

FCIMPL2(Array *, NewArrayFast, MethodTable * pArrayEEType, int numElements)
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
    uint8_t* combined_limit = pCurThread->GetEEAllocContext()->GetCombinedLimit();
    ASSERT(alloc_ptr <= combined_limit);
    if ((size_t)(combined_limit - alloc_ptr) >= size)
    {
        acontext->alloc_ptr = alloc_ptr + size;
        Array* pObject = (Array*)alloc_ptr;
        pObject->set_EEType(pArrayEEType);
        pObject->InitArrayLength((uint32_t)numElements);
        return pObject;
    }

    return (Array*)AllocateObject(pArrayEEType, 0, numElements);
}
FCIMPLEND

FCIMPL2(String *, RhNewString, MethodTable * pArrayEEType, int numElements)
{
    // TODO: Implement. We tail call to NewArrayFast for now since there's a bunch of TODOs in the places
    // that matter anyway.
    return (String*)NewArrayFast(pArrayEEType, numElements);
}
FCIMPLEND

#endif
#if defined(USE_PORTABLE_HELPERS)
#if defined(FEATURE_64BIT_ALIGNMENT)

GPTR_DECL(MethodTable, g_pFreeObjectEEType);

FCIMPL1(Object *, NewFinalizableAlign8, MethodTable* pEEType)
{
    return AllocateObject(pEEType, GC_ALLOC_FINALIZE | GC_ALLOC_ALIGN8, 0);
}
FCIMPLEND

#ifndef HOST_64BIT
FCIMPL1(Object*, NewFastAlign8, MethodTable* pEEType)
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

    uint8_t* combined_limit = pCurThread->GetEEAllocContext()->GetCombinedLimit();
    ASSERT(alloc_ptr <= combined_limit);
    if ((size_t)(combined_limit - alloc_ptr) >= paddedSize)
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
FCIMPLEND

FCIMPL1(Object*, NewFastMisalign, MethodTable* pEEType)
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

    uint8_t* combined_limit = pCurThread->GetEEAllocContext()->GetCombinedLimit();
    ASSERT(alloc_ptr <= combined_limit);
    if ((size_t)(combined_limit - alloc_ptr) >= paddedSize)
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
FCIMPLEND

FCIMPL2(Array*, NewArrayFastAlign8, MethodTable* pArrayEEType, int numElements)
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

    uint8_t* combined_limit = pCurThread->GetEEAllocContext()->GetCombinedLimit();
    ASSERT(alloc_ptr <= combined_limit);
    if ((size_t)(combined_limit - alloc_ptr) >= paddedSize)
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
FCIMPLEND
#endif // !HOST_64BIT
#endif // defined(HOST_ARM) || defined(HOST_WASM)

FCIMPL0(void, InitialDynamicInterfaceDispatch)
{
    ASSERT_UNCONDITIONALLY("NYI");
}
FCIMPLEND

FCIMPL0(void, InterfaceDispatch1)
{
    ASSERT_UNCONDITIONALLY("NYI");
}
FCIMPLEND

FCIMPL0(void, InterfaceDispatch2)
{
    ASSERT_UNCONDITIONALLY("NYI");
}
FCIMPLEND

FCIMPL0(void, InterfaceDispatch4)
{
    ASSERT_UNCONDITIONALLY("NYI");
}
FCIMPLEND

FCIMPL0(void, InterfaceDispatch8)
{
    ASSERT_UNCONDITIONALLY("NYI");
}
FCIMPLEND

FCIMPL0(void, InterfaceDispatch16)
{
    ASSERT_UNCONDITIONALLY("NYI");
}
FCIMPLEND

FCIMPL0(void, InterfaceDispatch32)
{
    ASSERT_UNCONDITIONALLY("NYI");
}
FCIMPLEND

FCIMPL0(void, InterfaceDispatch64)
{
    ASSERT_UNCONDITIONALLY("NYI");
}
FCIMPLEND

// @TODO Implement UniversalTransition
EXTERN_C void * ReturnFromUniversalTransition;
void * ReturnFromUniversalTransition;

// @TODO Implement UniversalTransition_DebugStepTailCall
EXTERN_C void * ReturnFromUniversalTransition_DebugStepTailCall;
void * ReturnFromUniversalTransition_DebugStepTailCall;

#if !defined (HOST_ARM64)
FCIMPL2(void, AssignRef, Object ** dst, Object * ref)
{
    // @TODO: USE_PORTABLE_HELPERS - Null check
    *dst = ref;
    InlineWriteBarrier(dst, ref);
}
FCIMPLEND

FCIMPL2(void, CheckedAssignRef, Object ** dst, Object * ref)
{
    // @TODO: USE_PORTABLE_HELPERS - Null check
    *dst = ref;
    InlineCheckedWriteBarrier(dst, ref);
}
FCIMPLEND
#endif

FCIMPL3(Object *, CheckedLockCmpXchg, Object ** location, Object * value, Object * comparand)
{
    Object * ret = (Object *)PalInterlockedCompareExchangePointer((void * volatile *)location, value, comparand);
    InlineCheckedWriteBarrier(location, value);
    return ret;
}
FCIMPLEND

FCIMPL2(Object *, CheckedXchg, Object ** location, Object * value)
{
    // @TODO: USE_PORTABLE_HELPERS - Null check
    Object * ret = (Object *)PalInterlockedExchangePointer((void * volatile *)location, value);
    InlineCheckedWriteBarrier(location, value);
    return ret;
}
FCIMPLEND

FCIMPL1(HRESULT, RhAllocateThunksMapping, void ** ppThunksSection)
{
    return E_FAIL;
}
FCIMPLEND

FCIMPL0(void *, GetThunksBase)
{
    return NULL;
}
FCIMPLEND

FCIMPL0(int, GetNumThunkBlocksPerMapping)
{
    ASSERT_UNCONDITIONALLY("NYI");
    return 0;
}
FCIMPLEND

FCIMPL0(int, GetNumThunksPerBlock)
{
    ASSERT_UNCONDITIONALLY("NYI");
    return 0;
}
FCIMPLEND

FCIMPL0(int, GetThunkSize)
{
    ASSERT_UNCONDITIONALLY("NYI");
    return 0;
}
FCIMPLEND

FCIMPL1(void*, GetThunkDataBlockAddress, void* pThunkStubAddress)
{
    ASSERT_UNCONDITIONALLY("NYI");
    return NULL;
}
FCIMPLEND

FCIMPL1(void*, GetThunkStubsBlockAddress, void* pThunkDataAddress)
{
    ASSERT_UNCONDITIONALLY("NYI");
    return NULL;
}
FCIMPLEND

FCIMPL0(int, GetThunkBlockSize)
{
    ASSERT_UNCONDITIONALLY("NYI");
    return 0;
}
FCIMPLEND

FCIMPL0(void *, RhGetCommonStubAddress)
{
    ASSERT_UNCONDITIONALLY("NYI");
    return NULL;
}
FCIMPLEND

FCIMPL0(void *, RhGetCurrentThunkContext)
{
    ASSERT_UNCONDITIONALLY("NYI");
    return NULL;
}
FCIMPLEND

FCIMPL0(void, GcPoll)
{
    // TODO: implement
}
FCIMPLEND

#endif
