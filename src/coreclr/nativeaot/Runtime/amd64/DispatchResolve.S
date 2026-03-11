// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.intel_syntax noprefix
#include <unixasmmacros.inc>

LEAF_ENTRY RhpResolveInterfaceMethodFast, _TEXT

        // Load the MethodTable from the object instance in rdi.
        // Trigger an AV if we're dispatching on a null this.
        // The exception handling infrastructure is aware of the fact that this is the first
        // instruction of RhpResolveInterfaceMethodFast and uses it to translate an AV here
        // to a NullReferenceException at the callsite.
        mov     r10, [rdi]

        // r11 currently contains the indirection cell address.
        lea     r10, [C_VAR(RhpCidResolve)]
        jmp     C_FUNC(RhpUniversalTransitionTailCall)

LEAF_END RhpResolveInterfaceMethodFast, _TEXT
