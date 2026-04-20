// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "common.h"
#include "CommonTypes.h"
#include "CommonMacros.h"
#include "daccess.h"
#include "CommonMacros.inl"
#include "Pal.h"
#include "rhassert.h"

#include "slist.h"
#include "rhbinder.h"
#include "regdisplay.h"
#include "StackFrameIterator.h"
#include "thread.h"
#include "threadstore.h"
#include "threadstore.inl"
#include "thread.inl"

#include "MethodTable.h"
#include "TypeManager.h"
#include "MethodTable.inl"
#include "ObjectLayout.h"

//
// WASM-specific allocators: we define them to use a shadow stack argument to avoid saving it on the fast path.
//
extern "C" void* RhpGcAlloc(MethodTable* pEEType, uint32_t uFlags, uintptr_t numElements, void* pTransitionFrame);

// Note that the emulated exception handling model requires us to call all managed methods that may/will throw
// only in the tail-like position so that control can immediately return to the caller in case of an exception.
extern "C" void RhExceptionHandling_FailedAllocation(void* pShadowStack, MethodTable* pEEType, bool isOverflow);

// Automatic finalization.
extern "C" void RhpPInvoke(PInvokeTransitionFrame* pFrame);
extern "C" void RhpPInvokeReturn(PInvokeTransitionFrame* pFrame);
extern bool g_FinalizationRequestPending;
void FinalizeFinalizableObjects();

static Object* AllocateObject(void* pShadowStack, MethodTable* pEEType, uint32_t uFlags, uintptr_t numElements)
{
    // Save the current shadow stack before calling into GC; we may need to scan it for live references.
    PInvokeTransitionFrame* pFrame = (PInvokeTransitionFrame*)pShadowStack;
    Thread* pThread = ThreadStore::GetCurrentThread();
    Object* obj = (Object*)RhpGcAlloc(pEEType, uFlags, numElements, pFrame);

#ifndef FEATURE_WASM_MANAGED_THREADS
    if (g_FinalizationRequestPending)
    {
        GCFrameRegistration gc; // GC-protect our exposed object.
        if (obj != nullptr)
        {
            gc.m_pThread = pThread;
            gc.m_pObjRefs = (void**)&obj;
            gc.m_numObjRefs = 1;
            gc.m_MaybeInterior = 0;
            pThread->PushGCFrameRegistration(&gc);
        }

        // "FinalizeFinalizableObjects" runs in preemptive mode.
        RhpPInvoke(pFrame);
        FinalizeFinalizableObjects();
        RhpPInvokeReturn(pFrame);

        if (obj != nullptr)
        {
            pThread->PopGCFrameRegistration(&gc);
        }
    }
#endif // !FEATURE_WASM_MANAGED_THREADS

    if (obj == nullptr)
    {
        RhExceptionHandling_FailedAllocation(pShadowStack, pEEType, /* isOverflow */ false);
    }

    return obj;
}

static void ThrowOverflowException(void* pShadowStack, MethodTable* pEEType)
{
    RhExceptionHandling_FailedAllocation(pShadowStack, pEEType, /* isOverflow */ true);
}

struct gc_alloc_context
{
    uint8_t* alloc_ptr;
    uint8_t* alloc_limit;
};

#define GC_ALLOC_FINALIZE    0x1 // TODO: Defined in gc.h
#define GC_ALLOC_ALIGN8_BIAS 0x4 // TODO: Defined in gc.h
#define GC_ALLOC_ALIGN8      0x8 // TODO: Defined in gc.h

//
// Allocations
//
FCIMPL1(Object*, RhpNewFast, MethodTable* pEEType)
{
    ASSERT(!pEEType->HasFinalizer());

    Thread* pCurThread = ThreadStore::GetCurrentThread();
    gc_alloc_context* acontext = pCurThread->GetAllocContext();
    size_t size = pEEType->GetBaseSize();

    uint8_t* alloc_ptr = acontext->alloc_ptr;
    ASSERT(alloc_ptr <= acontext->alloc_limit);
    if ((size_t)(acontext->alloc_limit - alloc_ptr) >= size)
    {
        acontext->alloc_ptr = alloc_ptr + size;
        Object* pObject = (Object*)alloc_ptr;
        pObject->SetMethodTable(pEEType);
        return pObject;
    }

    return AllocateObject(pShadowStack, pEEType, 0, 0);
}
FCIMPLEND

FCIMPL1(Object*, RhpNewFinalizable, MethodTable* pEEType)
{
    ASSERT(pEEType->HasFinalizer());
    return AllocateObject(pShadowStack, pEEType, GC_ALLOC_FINALIZE, 0);
}
FCIMPLEND

FCIMPL2(Array*, RhpNewArrayFast, MethodTable* pArrayEEType, int numElements)
{
    Thread* pCurThread = ThreadStore::GetCurrentThread();
    gc_alloc_context* acontext = pCurThread->GetAllocContext();

    if (numElements < 0)
    {
        ThrowOverflowException(pShadowStack, pArrayEEType);
        return nullptr;
    }

#ifndef HOST_64BIT
    // if the element count is <= 0x10000, no overflow is possible because the component size is
    // <= 0xffff, and thus the product is <= 0xffff0000, and the base size is only ~12 bytes
    if (numElements > 0x10000)
    {
        // Overflow here should result in an OOM. Let the slow path take care of it.
        return (Array*)AllocateObject(pShadowStack, pArrayEEType, 0, numElements);
    }
#endif // !HOST_64BIT

    size_t size = (size_t)pArrayEEType->GetBaseSize() + ((size_t)numElements * (size_t)pArrayEEType->RawGetComponentSize());
    size = ALIGN_UP(size, sizeof(uintptr_t));

    uint8_t* alloc_ptr = acontext->alloc_ptr;
    ASSERT(alloc_ptr <= acontext->alloc_limit);
    if ((size_t)(acontext->alloc_limit - alloc_ptr) >= size)
    {
        acontext->alloc_ptr = alloc_ptr + size;
        Array* pObject = (Array *)alloc_ptr;
        pObject->SetMethodTable(pArrayEEType);
        pObject->SetNumComponents((uint32_t)numElements);
        return pObject;
    }

    return (Array*)AllocateObject(pShadowStack, pArrayEEType, 0, numElements);
}
FCIMPLEND

FCIMPL2(Array*, RhpNewPtrArrayFast, MethodTable* pArrayEEType, int numElements)
{
    // TODO: Implement. We call RhpNewArrayFast for now.
    return (Array*)RhpNewArrayFast(pShadowStack, pArrayEEType, numElements);
}
FCIMPLEND

FCIMPL2(String*, RhNewString, MethodTable* pArrayEEType, int numElements)
{
    // TODO: Implement. We call RhpNewArray for now since there's a bunch of TODOs in the places that matter anyway.
    return (String*)RhpNewArrayFast(pShadowStack, pArrayEEType, numElements);
}
FCIMPLEND

#if defined(FEATURE_64BIT_ALIGNMENT)
GPTR_DECL(MethodTable, g_pFreeObjectEEType);

FCIMPL1(Object*, RhpNewFinalizableAlign8, MethodTable* pEEType)
{
    return AllocateObject(pShadowStack, pEEType, GC_ALLOC_FINALIZE | GC_ALLOC_ALIGN8, 0);
}
FCIMPLEND

FCIMPL1(Object*, RhpNewFastAlign8, MethodTable* pEEType)
{
    ASSERT(!pEEType->HasFinalizer());

    Thread* pCurThread = ThreadStore::GetCurrentThread();
    gc_alloc_context* acontext = pCurThread->GetAllocContext();

    size_t size = pEEType->GetBaseSize();
    size = ALIGN_UP(size, sizeof(uintptr_t));

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
            dummy->SetMethodTable(g_pFreeObjectEEType);
            alloc_ptr += 12;
        }
        Object* pObject = (Object*)alloc_ptr;
        pObject->SetMethodTable(pEEType);
        return pObject;
    }

    return AllocateObject(pShadowStack, pEEType, GC_ALLOC_ALIGN8, 0);
}
FCIMPLEND

FCIMPL1(Object*, RhpNewFastMisalign, MethodTable* pEEType)
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
            dummy->SetMethodTable(g_pFreeObjectEEType);
            alloc_ptr += 12;
        }
        Object* pObject = (Object *)alloc_ptr;
        pObject->SetMethodTable(pEEType);
        return pObject;
    }

    return AllocateObject(pShadowStack, pEEType, GC_ALLOC_ALIGN8 | GC_ALLOC_ALIGN8_BIAS, 0);
}
FCIMPLEND

FCIMPL2(Array*, RhpNewArrayFastAlign8, MethodTable* pArrayEEType, int numElements)
{
    Thread* pCurThread = ThreadStore::GetCurrentThread();
    gc_alloc_context* acontext = pCurThread->GetAllocContext();

    if (numElements < 0)
    {
        ThrowOverflowException(pShadowStack, pArrayEEType);
        return nullptr;
    }

    // if the element count is <= 0x10000, no overflow is possible because the component size is
    // <= 0xffff, and thus the product is <= 0xffff0000, and the base size is only ~12 bytes
    if (numElements > 0x10000)
    {
        // Overflow here should result in an OOM. Let the slow path take care of it.
        return (Array*)AllocateObject(pShadowStack, pArrayEEType, GC_ALLOC_ALIGN8, numElements);
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
            dummy->SetMethodTable(g_pFreeObjectEEType);
            alloc_ptr += 12;
        }
        Array* pObject = (Array*)alloc_ptr;
        pObject->SetMethodTable(pArrayEEType);
        pObject->SetNumComponents((uint32_t)numElements);
        return pObject;
    }

    return (Array*)AllocateObject(pShadowStack, pArrayEEType, GC_ALLOC_ALIGN8, numElements);
}
FCIMPLEND
#endif // FEATURE_64BIT_ALIGNMENT
