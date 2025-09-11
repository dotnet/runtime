// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

#include <fcall.h>

EXTERN_C void* RhpGcAlloc(CORINFO_CLASS_HANDLE typeHnd_, uint32_t uFlags, uintptr_t numElements, void * pTransitionFrame);

void RhExceptionHandling_FailedAllocation(MethodTable *pMT, bool isOverflow)
{
    PORTABILITY_ASSERT("RhExceptionHandling_FailedAllocation is not yet implemented");
}

static Object* _RhpNewObject(CORINFO_CLASS_HANDLE typeHnd_, uint32_t allocFlags)
{
    Object* obj = (Object*)RhpGcAlloc(typeHnd_, allocFlags, 0, nullptr);
    if (obj == NULL)
    {
        RhExceptionHandling_FailedAllocation((MethodTable*)typeHnd_, 0); // never returns
        return nullptr; // unreachable
    }

    return obj;
}

EXTERN_C FCDECL1(Object*, RhpNew, CORINFO_CLASS_HANDLE typeHnd_)
{
    return _RhpNewObject(typeHnd_, 0);
}

EXTERN_C FCDECL1(Object*, RhpNewMaybeFrozen, CORINFO_CLASS_HANDLE typeHnd_)
{
    PORTABILITY_ASSERT("RhpNewMaybeFrozen is not yet implemented");
    return nullptr;
}

EXTERN_C FCDECL2(Object*, RhpNewArrayMaybeFrozen, CORINFO_CLASS_HANDLE typeHnd_, INT_PTR size)
{
    PORTABILITY_ASSERT("RhpNewArrayMaybeFrozen is not yet implemented");
    return nullptr;
}