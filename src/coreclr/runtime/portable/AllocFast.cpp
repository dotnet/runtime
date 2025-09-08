// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

#include <fcall.h>

EXTERN_C FCDECL2(Object*, RhpNewVariableSizeObject, CORINFO_CLASS_HANDLE typeHnd_, INT_PTR size)
{
    PORTABILITY_ASSERT("RhpNewVariableSizeObject is not yet implemented");
    return nullptr;
}

static Object* _RhpNewArrayFastCore(CORINFO_CLASS_HANDLE typeHnd_, INT_PTR size)
{
    FCALL_CONTRACT;

    MethodTable* pMT = (MethodTable*)typeHnd_;
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
        pObject->SetMethodTableAndNumComponents(pMT, (INT32)size);
        return pObject;
    }

    return OBJECTREFToObject(AllocateSzArray(pMT, (INT32)size));
}

EXTERN_C FCDECL2(Object*, RhpNewArrayFast, CORINFO_CLASS_HANDLE typeHnd_, INT_PTR size)
{
    FCALL_CONTRACT;
    _ASSERTE(typeHnd_ != NULL);
    _ASSERTE(size < INT32_MAX);

    MethodTable* pMT = (MethodTable*)typeHnd_;

#ifndef HOST_64BIT
    // if the element count is <= 0x10000, no overflow is possible because the component size is
    // <= 0xffff, and thus the product is <= 0xffff0000, and the base size is only ~12 bytes
    if (size > 0x10000)
    {
        // Overflow here should result in an OOM. Let the slow path take care of it.
        return OBJECTREFToObject(AllocateSzArray(pMT, (INT32)size));
    }
#endif // !HOST_64BIT

    return _RhpNewArrayFastCore(typeHnd_, size);
}

EXTERN_C FCDECL2(Object*, RhpNewPtrArrayFast, CORINFO_CLASS_HANDLE typeHnd_, INT_PTR size)
{
    WRAPPER_NO_CONTRACT;
    return RhpNewArrayFast(typeHnd_, size);
}

EXTERN_C FCDECL1(Object*, RhpNewFast, CORINFO_CLASS_HANDLE typeHnd_)
{
    PORTABILITY_ASSERT("RhpNewFast is not yet implemented");
    return nullptr;
}

EXTERN_C FCDECL2(Object*, RhpNewArrayFastAlign8, CORINFO_CLASS_HANDLE typeHnd_, INT_PTR size)
{
    PORTABILITY_ASSERT("RhpNewArrayFastAlign8 is not yet implemented");
    return nullptr;
}

EXTERN_C FCDECL1(Object*, RhpNewFastAlign8, CORINFO_CLASS_HANDLE typeHnd_)
{
    PORTABILITY_ASSERT("RhpNewFastAlign8 is not yet implemented");
    return nullptr;
}

EXTERN_C FCDECL1(Object*, RhpNewFastMisalign, CORINFO_CLASS_HANDLE typeHnd_)
{
    PORTABILITY_ASSERT("RhpNewFastMisalign is not yet implemented");
    return nullptr;
}

void RhExceptionHandling_FailedAllocation(MethodTable *pMT, bool isOverflow)
{
    PORTABILITY_ASSERT("RhExceptionHandling_FailedAllocation is not yet implemented");
}

#define MAX_STRING_LENGTH 0x3FFFFFDF

EXTERN_C FCDECL2(Object*, RhNewString, CORINFO_CLASS_HANDLE typeHnd_, INT_PTR stringLength)
{
    FCALL_CONTRACT;
    _ASSERTE(typeHnd_ != NULL);

    MethodTable* pMT = (MethodTable*)typeHnd_;

    if (stringLength > MAX_STRING_LENGTH)
    {
        RhExceptionHandling_FailedAllocation(pMT, false);
        return NULL;
    }

    return _RhpNewArrayFastCore(typeHnd_, stringLength);
}