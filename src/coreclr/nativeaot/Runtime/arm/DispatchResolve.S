// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.syntax unified
.thumb

#include <unixasmmacros.inc>

// Dispatching version of RhpResolveInterfaceMethod
LEAF_ENTRY RhpInterfaceDispatch, _TEXT

        // r12 contains the indirection cell address.
        // Store it in the red zone for the universal transition thunk.
        str         r12, [sp, #-8]
        PREPARE_EXTERNAL_VAR RhpCidResolve, r12
        str         r12, [sp, #-4]

        b           C_FUNC(RhpUniversalTransitionTailCall)

LEAF_END RhpInterfaceDispatch, _TEXT
