// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.intel_syntax noprefix
#include <unixasmmacros.inc>

// Dispatching version of RhpResolveInterfaceMethod
LEAF_ENTRY RhpInterfaceDispatch, _TEXT

        // Load the MethodTable from the object instance in rdi.
        // Trigger an AV if we're dispatching on a null this.
        // The exception handling infrastructure is aware of the fact that this is the first
        // instruction of RhpInterfaceDispatch and uses it to translate an AV here
        // to a NullReferenceException at the callsite.
        mov     r10, [rdi]

        // r11 currently contains the indirection cell address.
        cmp     qword ptr [r11], r10 // is this the monomorphic MethodTable?
        jne     1f

        mov     rax, [r11 + 8] // load the cached monomorphic resolved code address into rax
        jmp     rax

1:
        // r11 contains indirection cell address
        lea     r10, [C_VAR(RhpCidResolve)]
        jmp     C_FUNC(RhpUniversalTransitionTailCall)

LEAF_END RhpInterfaceDispatch, _TEXT
