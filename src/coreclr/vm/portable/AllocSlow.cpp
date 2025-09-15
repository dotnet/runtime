// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

#include <fcall.h>

EXTERN_C Object* RhpGcAlloc(MethodTable* pMT, uint32_t uFlags, uintptr_t numElements, void * pTransitionFrame);
EXTERN_C Object* RhpGcAllocMaybeFrozen(MethodTable* pMT, uintptr_t numElements, TransitionBlock* pTransitionBlock);

void RhExceptionHandling_FailedAllocation(MethodTable *pMT, bool isOverflow)
{
    PORTABILITY_ASSERT("RhExceptionHandling_FailedAllocation is not yet implemented");
}

EXTERN_C FCDECL1(Object*, RhpNew, MethodTable* pMT)
{
    Object* obj = RhpGcAlloc(pMT, 0, 0, nullptr);
    if (obj == NULL)
    {
        RhExceptionHandling_FailedAllocation(pMT, false /* isOverflow */);
        return nullptr;
    }

    return obj;
}

EXTERN_C FCDECL1(Object*, RhpNewMaybeFrozen, CORINFO_CLASS_HANDLE typeHnd_)
{
    Object* obj = RhpGcAllocMaybeFrozen((MethodTable*)typeHnd_, 0, nullptr);
    if (obj == NULL)
    {
        RhExceptionHandling_FailedAllocation((MethodTable*)typeHnd_, 0);
        return nullptr;
    }

    return obj;
}

EXTERN_C FCDECL2(Object*, RhpNewArrayMaybeFrozen, CORINFO_CLASS_HANDLE typeHnd_, INT_PTR size)
{
    Object* obj = RhpGcAllocMaybeFrozen((MethodTable*)typeHnd_, size, nullptr);
    if (obj == NULL)
    {
        RhExceptionHandling_FailedAllocation((MethodTable*)typeHnd_, 0);
        return nullptr;
    }

    return obj;
}