// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <unixasmmacros.inc>

// Dispatching version of RhpResolveInterfaceMethod
LEAF_ENTRY RhpInterfaceDispatch, _TEXT

        // Load the MethodTable from the object instance in x0.
        // Trigger an AV if we're dispatching on a null this.
        // The exception handling infrastructure is aware of the fact that this is the first
        // instruction of RhpInterfaceDispatch and uses it to translate an AV here
        // to a NullReferenceException at the callsite.
        ldr     x12, [x0]

        // x11 currently contains the indirection cell address.
        // Load-acquire ensures that if we observe the cached MethodTable,
        // the subsequent load of Code will see the value written before it.
        ldar    x13, [x11]              // load-acquire cached MethodTable
        cmp     x13, x12                // is this the monomorphic MethodTable?
        bne     LOCAL_LABEL(SlowPath)

        ldr     x9, [x11, #8]           // load the cached monomorphic resolved code address
        br      x9

LOCAL_LABEL(SlowPath):
        PREPARE_EXTERNAL_VAR RhpCidResolve, xip0
        mov     xip1, x11
        b       C_FUNC(RhpUniversalTransitionTailCall)

LEAF_END RhpInterfaceDispatch, _TEXT
