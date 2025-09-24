// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

#include <fcall.h>

extern void RhExceptionHandling_FailedAllocation(MethodTable *pMT, bool isOverflow);
EXTERN_C Object* RhpGcAlloc(MethodTable* pMT, uint32_t uFlags, uintptr_t numElements, void * pTransitionFrame);

EXTERN_C FCDECL2(Object*, RhpNewVariableSizeObject, MethodTable* pMT, INT_PTR numElements)
{
    Object* obj = RhpGcAlloc(pMT, 0, numElements, nullptr);
    if (obj == NULL)
    {
        RhExceptionHandling_FailedAllocation(pMT, false /* isOverflow */);
    }

    return obj;
}

static Object* _RhpNewArrayFastCore(MethodTable* pMT, INT_PTR size)
{
    FCALL_CONTRACT;
    _ASSERTE(pMT != NULL);
    _ASSERTE(size < INT32_MAX);

    Thread* thread = GetThread();
    ee_alloc_context* cxt = thread->GetEEAllocContext();

    size_t sizeInBytes = (size_t)pMT->GetBaseSize() + ((size_t)size * (size_t)pMT->RawGetComponentSize());
    sizeInBytes = ALIGN_UP(sizeInBytes, sizeof(void*));

    uint8_t* alloc_ptr = cxt->getAllocPtr();
    ASSERT(alloc_ptr <= cxt->getAllocLimit());
    if ((size_t)(cxt->getAllocLimit() - alloc_ptr) >= sizeInBytes)
    {
        cxt->setAllocPtr(alloc_ptr + sizeInBytes);
        PtrArray* pObject = (PtrArray *)alloc_ptr;
        pObject->SetMethodTable(pMT);
        pObject->SetNumComponents((INT32)size);
        return pObject;
    }

    return RhpNewVariableSizeObject(pMT, size);
}

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
        return RhpNewVariableSizeObject(pMT, size);
    }
#endif // !HOST_64BIT

    return _RhpNewArrayFastCore(pMT, size);
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

EXTERN_C FCDECL2(Object*, RhpNewArrayFastAlign8, MethodTable* pMT, INT_PTR size)
{
    PORTABILITY_ASSERT("RhpNewArrayFastAlign8 is not yet implemented");
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
        return NULL;
    }

    return _RhpNewArrayFastCore(pMT, stringLength);
}