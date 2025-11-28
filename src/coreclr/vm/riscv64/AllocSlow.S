// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "asmconstants.h"
#include "unixasmmacros.inc"

//
// Object* RhpNew(MethodTable *pMT)
//
// Allocate non-array object, slow path.
//
LEAF_ENTRY RhpNew, _TEXT

    li          a1, 0
    tail        C_FUNC(RhpNewObject)

LEAF_END RhpNew, _TEXT

//
// Object* RhpNewMaybeFrozen(MethodTable *pMT)
//
// Allocate non-array object, may be on frozen heap.
//
NESTED_ENTRY RhpNewMaybeFrozen, _TEXT, NoHandler

    PUSH_COOP_PINVOKE_FRAME a2

    li          a1, 0
    call        C_FUNC(RhpGcAllocMaybeFrozen)

    POP_COOP_PINVOKE_FRAME
    EPILOG_RETURN

NESTED_END RhpNewMaybeFrozen, _TEXT

//
// Object* RhpNewMaybeFrozen(MethodTable *pMT, INT_PTR size)
//
// Allocate array object, may be on frozen heap.
//
NESTED_ENTRY RhpNewArrayMaybeFrozen, _TEXT, NoHandler

    PUSH_COOP_PINVOKE_FRAME a2

    call        C_FUNC(RhpGcAllocMaybeFrozen)

    POP_COOP_PINVOKE_FRAME
    EPILOG_RETURN

NESTED_END RhpNewArrayMaybeFrozen, _TEXT

//
// void RhExceptionHandling_FailedAllocation(MethodTable *pMT, bool isOverflow)
//
NESTED_ENTRY RhExceptionHandling_FailedAllocation, _TEXT, NoHandler

    PUSH_COOP_PINVOKE_FRAME a2

    call        C_FUNC(RhExceptionHandling_FailedAllocation_Helper)

    POP_COOP_PINVOKE_FRAME
    EPILOG_RETURN

NESTED_END RhExceptionHandling_FailedAllocation, _TEXT
