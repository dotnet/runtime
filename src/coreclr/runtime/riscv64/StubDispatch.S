// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "AsmMacros_Shared.h"

#ifdef FEATURE_CACHED_INTERFACE_DISPATCH

    .extern CidResolve
    .extern UniversalTransition_DebugStepTailCall

    // Macro that generates code to check a single cache entry.
    .macro CHECK_CACHE_ENTRY entry
        // Load cache entry data into a temporary register
        ld    t6, (OFFSETOF__InterfaceDispatchCache__m_rgEntries + (\entry * 16))(t0)

        // Compare with MethodTable* in t1
        bne   t1, t6, 0f

        // Load the target address from the cache entry
        ld    t0, (OFFSETOF__InterfaceDispatchCache__m_rgEntries + (\entry * 16) + 8)(t0)

        // Jump to the address in t0
        jr  t0

    0:
    .endm

    //
    // Macro that generates a stub consuming a cache with the given number of entries.
    //
    .macro DEFINE_INTERFACE_DISPATCH_STUB entries

        NESTED_ENTRY InterfaceDispatch\entries, _TEXT, NoHandler

            // t5 holds the indirection cell address. Load the cache pointer.
            ld  t0, OFFSETOF__InterfaceDispatchCell__m_pCache(t5)  // Using a1 as an alternative base register

            // Load the MethodTable from the object instance in a0.
            ALTERNATE_ENTRY InterfaceDispatchAVLocation\entries
            ld  t1, 0(a0)

        .global CurrentEntry
        .set CurrentEntry, 0

        .rept \entries
            CHECK_CACHE_ENTRY CurrentEntry
            .set CurrentEntry, CurrentEntry + 1
        .endr

            // t0 still contains the indirection cell address.
            tail  C_FUNC(InterfaceDispatchSlow)

        NESTED_END InterfaceDispatch\entries, _TEXT

    .endm

    //
    // Define all the stub routines we currently need.
    //
    DEFINE_INTERFACE_DISPATCH_STUB 1
    DEFINE_INTERFACE_DISPATCH_STUB 2
    DEFINE_INTERFACE_DISPATCH_STUB 4
    DEFINE_INTERFACE_DISPATCH_STUB 8
    DEFINE_INTERFACE_DISPATCH_STUB 16
    DEFINE_INTERFACE_DISPATCH_STUB 32
    DEFINE_INTERFACE_DISPATCH_STUB 64

    //
    // Initial dispatch on an interface when we don't have a cache yet.
    //
    LEAF_ENTRY InitialInterfaceDispatch, _TEXT
    ALTERNATE_ENTRY InitialDynamicInterfaceDispatch
        // Trigger an AV if we're dispatching on a null this.
        // The exception handling infrastructure is aware of the fact that this is the first
        // instruction of InitialInterfaceDispatch and uses it to translate an AV here
        // to a NullReferenceException at the callsite.
        lw zero, 0(a0)

        // Just tail call to the cache miss helper.
        tail       C_FUNC(InterfaceDispatchSlow)
    LEAF_END InitialInterfaceDispatch, _TEXT

    //
    // Cache miss case, call the runtime to resolve the target and update the cache.
    // Use universal transition helper to allow an exception to flow out of resolution.
    //
    LEAF_ENTRY InterfaceDispatchSlow, _TEXT
        // t5 contains the interface dispatch cell address.
        // Calling convention of the universal thunk is:
        //  t0: target address for the thunk to call
        //  t1: parameter of the thunk's target
        PREPARE_EXTERNAL_VAR CidResolve, t0
        mv t1, t5
        tail       C_FUNC(UniversalTransition_DebugStepTailCall)
    LEAF_END InterfaceDispatchSlow, _TEXT

#endif // FEATURE_CACHED_INTERFACE_DISPATCH
