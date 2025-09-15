// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

#include <fcall.h>

EXTERN_C FCDECL1(Object*, RhpNew, CORINFO_CLASS_HANDLE typeHnd_)
{
    PORTABILITY_ASSERT("RhpNew is not yet implemented");
    return nullptr;
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

void RhExceptionHandling_FailedAllocation(MethodTable *pMT, bool isOverflow)
{
    PORTABILITY_ASSERT("RhExceptionHandling_FailedAllocation is not yet implemented");
}