// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.syntax unified
.thumb

#include <unixasmmacros.inc>

// Dispatching version of RhpResolveInterfaceMethod
LEAF_ENTRY RhpInterfaceDispatch, _TEXT

        // r12 currently contains the indirection cell address. But we need more scratch registers and
        // we may A/V on a null this. Store r1 and r2 in red zone.
        str         r1, [sp, #-8]
        str         r2, [sp, #-4]

        // Load the MethodTable from the object instance in r0.
        // The label marks the location of a potential nullref for the unwinder.
        GLOBAL_LABEL RhpInterfaceDispatchAVLocation
        ldr         r1, [r0]

        // r12 contains the indirection cell address.
        ldr         r2, [r12]               // load cached MethodTable
        cmp         r2, r1                  // is this the monomorphic MethodTable?
        bne         LOCAL_LABEL(SlowPath)

        // Fast path: restore r1/r2 before jumping to cached code.
        ldr         r1, [sp, #-8]
        ldr         r2, [sp, #-4]
        // dmb ensures that the Code load below sees the value written before
        // MethodTable. ARM32 has no load-acquire instruction (LDAR is ARMv8 only).
        dmb
        ldr         r12, [r12, #4]          // load the cached monomorphic resolved code address
        bx          r12

LOCAL_LABEL(SlowPath):
        // restore original value of r1 and r2
        ldr         r1, [sp, #-8]
        ldr         r2, [sp, #-4]

        str         r12, [sp, #-8]
        PREPARE_EXTERNAL_VAR RhpCidResolve, r12
        str         r12, [sp, #-4]

        b           C_FUNC(RhpUniversalTransitionTailCall)

LEAF_END RhpInterfaceDispatch, _TEXT
