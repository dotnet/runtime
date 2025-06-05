// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.intel_syntax noprefix
#include "asmconstants.h"
#include "unixasmmacros.inc"

//
// Object* __fastcall New(MethodTable *pMT)
//
// Allocate non-array object, slow path.
//
LEAF_ENTRY New, _TEXT

    PUSH_COOP_PINVOKE_FRAME eax

    push        eax
    push        0
    push        0
    push        ecx

    CHECK_STACK_ALIGNMENT
    call        C_FUNC(GcAlloc)

    add         esp, 16

    POP_COOP_PINVOKE_FRAME
    ret

LEAF_END New, _TEXT

//
// Object* __fastcall NewMaybeFrozen(MethodTable *pMT)
//
// Allocate non-array object, may be on frozen heap.
//
NESTED_ENTRY NewMaybeFrozen, _TEXT, NoHandler

    PUSH_COOP_PINVOKE_FRAME eax

    sub         esp, 4
    push        eax
    push        0
    push        ecx

    CHECK_STACK_ALIGNMENT
    call        C_FUNC(GcAllocMaybeFrozen)

    add         esp, 16

    POP_COOP_PINVOKE_FRAME
    ret

NESTED_END NewMaybeFrozen, _TEXT

//
// Object* __fastcall NewMaybeFrozen(MethodTable *pMT, INT_PTR size)
//
// Allocate array object, may be on frozen heap.
//
NESTED_ENTRY NewArrayMaybeFrozen, _TEXT, NoHandler

    PUSH_COOP_PINVOKE_FRAME eax

    sub         esp, 4
    push        eax
    push        edx
    push        ecx

    CHECK_STACK_ALIGNMENT
    call        C_FUNC(GcAllocMaybeFrozen)

    add         esp, 16

    POP_COOP_PINVOKE_FRAME
    ret

NESTED_END NewArrayMaybeFrozen, _TEXT

//
// void __fastcall RhExceptionHandling_FailedAllocation(MethodTable *pMT, bool isOverflow)
//
NESTED_ENTRY RhExceptionHandling_FailedAllocation, _TEXT, NoHandler

    PUSH_COOP_PINVOKE_FRAME eax

    sub         esp, 4
    push        eax
    push        edx
    push        ecx

    CHECK_STACK_ALIGNMENT
    call        C_FUNC(RhExceptionHandling_FailedAllocation_Helper)

    add         esp, 16

    POP_COOP_PINVOKE_FRAME
    ret

NESTED_END RhExceptionHandling_FailedAllocation, _TEXT
