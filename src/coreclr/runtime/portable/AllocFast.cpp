// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

#include <fcall.h>
#include <gcinterface.h>
#include <vars.hpp>

extern void RhExceptionHandling_FailedAllocation(MethodTable *pMT, bool isOverflow);
EXTERN_C Object* RhpGcAlloc(MethodTable* pMT, uint32_t uFlags, uintptr_t numElements, void * pTransitionFrame);

static Object* AllocateObject(MethodTable* pMT, uint32_t uFlags, INT_PTR numElements)
{
    FCALL_CONTRACT;
    Object* obj = RhpGcAlloc(pMT, uFlags, numElements, nullptr);
    if (obj == NULL)
    {
        RhExceptionHandling_FailedAllocation(pMT, false /* isOverflow */);
    }

    return obj;
}

EXTERN_C FCDECL2(Object*, RhpNewVariableSizeObject, MethodTable* pMT, INT_PTR numElements)
{
    WRAPPER_NO_CONTRACT;
    return AllocateObject(pMT, 0, numElements);
}

static Object* NewArrayFastCore(MethodTable* pMT, INT_PTR size)
{
    FCALL_CONTRACT;
    _ASSERTE(pMT != NULL);
    if (size < 0 || size > INT32_MAX)
    {
        RhExceptionHandling_FailedAllocation(pMT, true /* isOverflow */);
        return nullptr;
    }

    Thread* thread = GetThread();
    ee_alloc_context* cxt = thread->GetEEAllocContext();

    size_t sizeInBytes = (size_t)pMT->GetBaseSize() + ((size_t)size * (size_t)pMT->RawGetComponentSize());
    sizeInBytes = ALIGN_UP(sizeInBytes, sizeof(void*));

    uint8_t* alloc_ptr = cxt->getAllocPtr();
    _ASSERTE(alloc_ptr <= cxt->getAllocLimit());
    if ((size_t)(cxt->getAllocLimit() - alloc_ptr) >= sizeInBytes)
    {
        cxt->setAllocPtr(alloc_ptr + sizeInBytes);
        PtrArray* pObject = (PtrArray *)alloc_ptr;
        pObject->SetMethodTable(pMT);
        pObject->SetNumComponents((INT32)size);
        return pObject;
    }

    return AllocateObject(pMT, 0, size);
}

#if defined(FEATURE_64BIT_ALIGNMENT)
static Object* NewArrayFastAlign8Core(MethodTable* pMT, INT_PTR size)
{
    FCALL_CONTRACT;
    _ASSERTE(pMT != NULL);

    if (size < 0 || size > INT32_MAX)
    {
        RhExceptionHandling_FailedAllocation(pMT, true /* isOverflow */);
        return nullptr;
    }

    Thread* thread = GetThread();
    ee_alloc_context* cxt = thread->GetEEAllocContext();

    size_t sizeInBytes = (size_t)pMT->GetBaseSize() + ((size_t)size * (size_t)pMT->RawGetComponentSize());
    sizeInBytes = ALIGN_UP(sizeInBytes, sizeof(void*));

    uint8_t* alloc_ptr = cxt->getAllocPtr();
    bool requiresAlignObject = !IS_ALIGNED(alloc_ptr, sizeof(int64_t));
    size_t paddedSize = sizeInBytes;
    if (requiresAlignObject)
    {
        // We are assuming that allocation of minimal object flips the alignment
        paddedSize += MIN_OBJECT_SIZE;
    }

    _ASSERTE(alloc_ptr <= cxt->getAllocLimit());
    if ((size_t)(cxt->getAllocLimit() - alloc_ptr) >= paddedSize)
    {
        cxt->setAllocPtr(alloc_ptr + paddedSize);
        if (requiresAlignObject)
        {
            Object* dummy = (Object*)alloc_ptr;
            dummy->SetMethodTable(g_pFreeObjectMethodTable);
            alloc_ptr += MIN_OBJECT_SIZE;
        }
        PtrArray* pObject = (PtrArray *)alloc_ptr;
        pObject->SetMethodTable(pMT);
        pObject->SetNumComponents((INT32)size);
        return pObject;
    }

    return AllocateObject(pMT, GC_ALLOC_ALIGN8, size);
}

EXTERN_C FCDECL2(Object*, RhpNewArrayFastAlign8, MethodTable* pMT, INT_PTR size)
{
    FCALL_CONTRACT;
    _ASSERTE(pMT != NULL);

    // if the element count is <= 0x10000, no overflow is possible because the component size is
    // <= 0xffff, and thus the product is <= 0xffff0000, and the base size is only ~12 bytes
    if (size > 0x10000)
    {
        // Overflow here should result in an OOM. Let the slow path take care of it.
        return AllocateObject(pMT, GC_ALLOC_ALIGN8, size);
    }

    return NewArrayFastAlign8Core(pMT, size);
}
#endif // FEATURE_64BIT_ALIGNMENT

EXTERN_C FCDECL2(Object*, RhpNewArrayFast, MethodTable* pMT, INT_PTR size)
{
    FCALL_CONTRACT;
    _ASSERTE(pMT != NULL);

#ifndef HOST_64BIT
    // if the element count is <= 0x10000, no overflow is possible because the component size is
    // <= 0xffff, and thus the product is <= 0xffff0000, and the base size is only ~12 bytes
    if (size > 0x10000)
    {
        // Overflow here should result in an OOM. Let the slow path take care of it.
        return AllocateObject(pMT, 0, size);
    }
#endif // !HOST_64BIT

    return NewArrayFastCore(pMT, size);
}

EXTERN_C FCDECL2(Object*, RhpNewPtrArrayFast, MethodTable* pMT, INT_PTR size)
{
    WRAPPER_NO_CONTRACT;
    return RhpNewArrayFast(pMT, size);
}

EXTERN_C FCDECL1(Object*, RhpNewFast, MethodTable* pMT)
{
    PORTABILITY_ASSERT("RhpNewFast is not yet implemented");
    return nullptr;
}

EXTERN_C FCDECL1(Object*, RhpNewFastAlign8, MethodTable* pMT)
{
    PORTABILITY_ASSERT("RhpNewFastAlign8 is not yet implemented");
    return nullptr;
}

EXTERN_C FCDECL1(Object*, RhpNewFastMisalign, MethodTable* pMT)
{
    PORTABILITY_ASSERT("RhpNewFastMisalign is not yet implemented");
    return nullptr;
}

#define MAX_STRING_LENGTH 0x3FFFFFDF

EXTERN_C FCDECL2(Object*, RhNewString, MethodTable* pMT, INT_PTR stringLength)
{
    FCALL_CONTRACT;
    _ASSERTE(pMT != NULL);

    if (stringLength > MAX_STRING_LENGTH)
    {
        RhExceptionHandling_FailedAllocation(pMT, false);
    }

    return NewArrayFastCore(pMT, stringLength);
}
