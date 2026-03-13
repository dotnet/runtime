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

        // TODO: monomorphic fast path, you can use r1/r2 as scratch registers

        // restore original value of r1 and r2
        ldr         r1, [sp, #-8]
        ldr         r2, [sp, #-4]

        str         r12, [sp, #-8]
        PREPARE_EXTERNAL_VAR RhpCidResolve, r12
        str         r12, [sp, #-4]

        b           C_FUNC(RhpUniversalTransitionTailCall)

LEAF_END RhpInterfaceDispatch, _TEXT
