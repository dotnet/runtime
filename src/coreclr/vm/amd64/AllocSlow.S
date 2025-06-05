// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.intel_syntax noprefix
#include "asmconstants.h"
#include "unixasmmacros.inc"

//
// Object* New(MethodTable *pMT)
//
// Allocate non-array object, slow path
//
LEAF_ENTRY New, _TEXT

    mov         rsi, 0
    jmp         C_FUNC(NewObject)

LEAF_END New, _TEXT

//
// Object* NewMaybeFrozen(MethodTable *pMT)
//
// Allocate non-array object, may be on frozen heap
//
NESTED_ENTRY NewMaybeFrozen, _TEXT, NoHandler

    PUSH_COOP_PINVOKE_FRAME rdx

    mov         rsi, 0
    call        C_FUNC(GcAllocMaybeFrozen)

    POP_COOP_PINVOKE_FRAME
    ret

NESTED_END NewMaybeFrozen, _TEXT

//
// Object* NewArrayMaybeFrozen(MethodTable *pMT, INT_PTR size)
//
// Allocate array object, may be on frozen heap
//
NESTED_ENTRY NewArrayMaybeFrozen, _TEXT, NoHandler

    PUSH_COOP_PINVOKE_FRAME rdx

    call        C_FUNC(GcAllocMaybeFrozen)

    POP_COOP_PINVOKE_FRAME
    ret

NESTED_END NewArrayMaybeFrozen, _TEXT

//
// void RhExceptionHandling_FailedAllocation(MethodTable *pMT, bool isOverflow)
//
NESTED_ENTRY RhExceptionHandling_FailedAllocation, _TEXT, NoHandler
    
    PUSH_COOP_PINVOKE_FRAME rdx
    
    call        C_FUNC(RhExceptionHandling_FailedAllocation_Helper)

    POP_COOP_PINVOKE_FRAME
    ret

NESTED_END RhExceptionHandling_FailedAllocation, _TEXT
