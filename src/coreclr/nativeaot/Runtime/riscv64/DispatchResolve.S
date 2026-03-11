// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <unixasmmacros.inc>

LEAF_ENTRY RhpResolveInterfaceMethodFast, _TEXT

        // Load the MethodTable from the object instance in a0.
        // Trigger an AV if we're dispatching on a null this.
        // The exception handling infrastructure is aware of the fact that this is the first
        // instruction of RhpResolveInterfaceMethodFast and uses it to translate an AV here
        // to a NullReferenceException at the callsite.
        ld      zero, 0(a0)

        // t5 currently contains the indirection cell address.
        // Calling convention of the universal thunk is:
        //  t0: target address for the thunk to call
        //  t1: parameter of the thunk's target
        PREPARE_EXTERNAL_VAR RhpCidResolve, t0
        mv      t1, t5
        tail    C_FUNC(RhpUniversalTransitionTailCall)

LEAF_END RhpResolveInterfaceMethodFast, _TEXT
