// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <unixasmmacros.inc>
#include "AsmOffsets.inc"

#ifdef FEATURE_CACHED_INTERFACE_DISPATCH

    .extern RhpCidResolve
    .extern RhpUniversalTransition_DebugStepTailCall

//
// Stub dispatch routine for dispatch to a vtable slot
//
    LEAF_ENTRY RhpVTableOffsetDispatch, _TEXT
        // x11 contains the interface dispatch cell address.
        // load x12 to point to the vtable offset (which is stored in the m_pCache field).
        ldr     x12, [x11, #OFFSETOF__InterfaceDispatchCell__m_pCache]

        // Load the MethodTable from the object instance in x0, and add it to the vtable offset
        // to get the address in the vtable of what we want to dereference
    ALTERNATE_ENTRY RhpVTableOffsetDispatchAVLocation
        ldr     x13, [x0]
        add     x12, x12, x13

        // Load the target address of the vtable into x12
        ldr     x12, [x12]

        br      x12
    LEAF_END RhpVTableOffsetDispatch, _TEXT

//
// Cache miss case, call the runtime to resolve the target and update the cache.
// Use universal transition helper to allow an exception to flow out of resolution.
//
    LEAF_ENTRY RhpInterfaceDispatchSlow, _TEXT
        // x11 contains the interface dispatch cell address.
        // Calling convention of the universal thunk is:
        //  xip0: target address for the thunk to call
        //  xip1: parameter of the thunk's target
        PREPARE_EXTERNAL_VAR RhpCidResolve, xip0
        mov xip1, x11
        b       C_FUNC(RhpUniversalTransition_DebugStepTailCall)
    LEAF_END RhpInterfaceDispatchSlow, _TEXT

#endif // FEATURE_CACHED_INTERFACE_DISPATCH
