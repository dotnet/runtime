// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <unixasmmacros.inc>

// Dispatching version of RhpResolveInterfaceMethod
LEAF_ENTRY RhpInterfaceDispatch, _TEXT

        // Load the MethodTable from the object instance in a0.
        // Trigger an AV if we're dispatching on a null this.
        // The exception handling infrastructure is aware of the fact that this is the first
        // instruction of RhpInterfaceDispatch and uses it to translate an AV here
        // to a NullReferenceException at the callsite.
        ld.d    $zero, $a0, 0

        // $t8 currently contains the indirection cell address.
        // Calling convention of the universal thunk is:
        //  $t7: target address for the thunk to call
        //  $t8: parameter of the thunk's target
        PREPARE_EXTERNAL_VAR RhpCidResolve, $t7
        b       C_FUNC(RhpUniversalTransitionTailCall)

LEAF_END RhpInterfaceDispatch, _TEXT
