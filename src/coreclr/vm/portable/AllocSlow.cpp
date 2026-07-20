// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

#include <fcall.h>
#include "MiscNativeHelpers.h"

// WASM-TODO: make fcalls follow the new calling convention https://github.com/dotnet/runtime/issues/123437
EXTERN_C Object* RhpGcAlloc(MethodTable* pMT, uint32_t uFlags, uintptr_t numElements, TransitionBlock* pTransitionBlock);
EXTERN_C Object* RhpGcAllocMaybeFrozen(MethodTable* pMT, uintptr_t numElements, TransitionBlock* pTransitionBlock);
EXTERN_C void RhExceptionHandling_FailedAllocation_Helper(MethodTable* pMT, bool isOverflow, TransitionBlock* pTransitionBlock);

EXTERN_C FCDECL1(Object*, RhpNew, MethodTable* pMT);
FCIMPL1(Object*, RhpNew, MethodTable* pMT)
{
    PREPARE_TRANSITION_ARG();
    return RhpGcAlloc(pMT, 0, 0, TRANSITION_ARG_PARAM);
}
FCIMPLEND

EXTERN_C FCDECL1(Object*, RhpNewMaybeFrozen, MethodTable* pMT);
FCIMPL1(Object*, RhpNewMaybeFrozen, MethodTable* pMT)
{
    PREPARE_TRANSITION_ARG();
    return RhpGcAllocMaybeFrozen(pMT, 0, TRANSITION_ARG_PARAM);
}
FCIMPLEND

EXTERN_C FCDECL2(Object*, RhpNewArrayMaybeFrozen, MethodTable* pMT, INT_PTR size);
FCIMPL2(Object*, RhpNewArrayMaybeFrozen, MethodTable* pMT, INT_PTR size)
{
    PREPARE_TRANSITION_ARG();
    return RhpGcAllocMaybeFrozen(pMT, size, TRANSITION_ARG_PARAM);
}
FCIMPLEND
