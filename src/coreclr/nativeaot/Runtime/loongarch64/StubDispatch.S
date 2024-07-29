// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <unixasmmacros.inc>
#include "AsmOffsets.inc"

#ifdef FEATURE_CACHED_INTERFACE_DISPATCH

    .extern RhpCidResolve
    .extern RhpUniversalTransition_DebugStepTailCall

    // Macro that generates code to check a single cache entry.
    .macro CHECK_CACHE_ENTRY entry
        // Check a single entry in the cache.
        //  t0  : Cache data structure. Also used for target address jump.
        //  t1  : Instance MethodTable*
        //  t2  : Indirection cell address, preserved
        //  t3  : Trashed
        ld.d  $t3, $t0, (OFFSETOF__InterfaceDispatchCache__m_rgEntries + (\entry * 16))
        bne  $t1, $t3, 0f
        ld.d  $t0, $t0, (OFFSETOF__InterfaceDispatchCache__m_rgEntries + (\entry * 16) + 8)
        jirl  $r0, $t0, 0
0:
    .endm

//
// Macro that generates a stub consuming a cache with the given number of entries.
//
    .macro DEFINE_INTERFACE_DISPATCH_STUB entries

    NESTED_ENTRY "RhpInterfaceDispatch\entries", _TEXT, NoHandler

        // t2 holds the indirection cell address. Load the cache pointer.
        ld.d  $t0, $t8, OFFSETOF__InterfaceDispatchCell__m_pCache

        // Load the MethodTable from the object instance in a0.
        ALTERNATE_ENTRY RhpInterfaceDispatchAVLocation\entries
        ld.d  $t1, $a0, 0

    .global CurrentEntry
    .set CurrentEntry, 0

    .rept \entries
        CHECK_CACHE_ENTRY CurrentEntry
        .set CurrentEntry, CurrentEntry + 1
    .endr

        // t2 still contains the indirection cell address.
        b  C_FUNC(RhpInterfaceDispatchSlow)

    NESTED_END "RhpInterfaceDispatch\entries", _TEXT

    .endm

//
// Define all the stub routines we currently need.
//
// If you change or add any new dispatch stubs, exception handling might need to be aware because it refers to the
// *AVLocation symbols defined by the dispatch stubs to be able to unwind and blame user code if a NullRef happens
// during the interface dispatch.
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
    LEAF_ENTRY RhpInitialInterfaceDispatch, _TEXT
    ALTERNATE_ENTRY RhpInitialDynamicInterfaceDispatch
        // Trigger an AV if we're dispatching on a null this.
        // The exception handling infrastructure is aware of the fact that this is the first
        // instruction of RhpInitialInterfaceDispatch and uses it to translate an AV here
        // to a NullReferenceException at the callsite.
        ld.d  $zero, $a0, 0

        // Just tail call to the cache miss helper.
        b  C_FUNC(RhpInterfaceDispatchSlow)
    LEAF_END RhpInitialInterfaceDispatch, _TEXT

//
// Stub dispatch routine for dispatch to a vtable slot
//
    LEAF_ENTRY RhpVTableOffsetDispatch, _TEXT
        // t2 contains the interface dispatch cell address.
        // load t3 to point to the vtable offset (which is stored in the m_pCache field).
        ld.d  $t3, $t2, OFFSETOF__InterfaceDispatchCell__m_pCache

        // Load the MethodTable from the object instance in a0, and add it to the vtable offset
        // to get the address in the vtable of what we want to dereference
        ld.d  $t4, $a0, 0
        add.d  $t3, $t3, $t4

        // Load the target address of the vtable into t3
        ld.d  $t3, $t3, 0

        jirl  $r0, $t3, 0
    LEAF_END RhpVTableOffsetDispatch, _TEXT

//
// Cache miss case, call the runtime to resolve the target and update the cache.
// Use universal transition helper to allow an exception to flow out of resolution.
//
    LEAF_ENTRY RhpInterfaceDispatchSlow, _TEXT
        // t2 contains the interface dispatch cell address.
        // Calling convention of the universal thunk is:
        //  t7: target address for the thunk to call
        //  t8: parameter of the thunk's target
        PREPARE_EXTERNAL_VAR RhpCidResolve, $t7
        b  C_FUNC(RhpUniversalTransition_DebugStepTailCall)
    LEAF_END RhpInterfaceDispatchSlow, _TEXT

#endif // FEATURE_CACHED_INTERFACE_DISPATCH
