// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "AsmMacros_Shared.h"

#ifdef FEATURE_CACHED_INTERFACE_DISPATCH

#if defined(__APPLE__)
    // Currently the build is failing without this due to an issue if the first method in the assembly file has an alternate entry at the start of the file.
    // Fix, but adding an empty, unused method
    LEAF_ENTRY RhpStubDispatchDoNotFailToBuild, _TEXT
       ret
    LEAF_END RhpStubDispatchDoNotFailToBuild, _TEXT
#endif

    // Macro that generates code to check a single cache entry.
    // \adj is the base adjustment already applied to x9.
    .macro CHECK_CACHE_ENTRY entry, adj=0
        // Check a single entry in the cache.
        //  x9   : Cache data structure (possibly adjusted for far entries)
        //  x10  : Instance MethodTable*
        //  x11  : Indirection cell address, preserved
        //  x12, x13  : Trashed
        //
        // Use ldp to load both m_pInstanceType and m_pTargetCode in a single instruction.
        // On ARM64 two separate ldr instructions can be reordered across a control dependency,
        // which means a concurrent atomic cache entry update (via stlxp) could be observed as a
        // torn read (new type, old target). ldp is single-copy atomic for the pair on FEAT_LSE2
        // hardware (ARMv8.4+). The cbz guard ensures correctness on pre-LSE2 hardware too:
        // a torn read can only produce a zero target (entries go from 0,0 to type,target),
        // so we treat it as a cache miss.
        ldp     x12, x13, [x9, #(OFFSETOF__InterfaceDispatchCache__m_rgEntries + (\entry * 16) - \adj)]
        cmp     x10, x12
        bne     0f
#ifndef TARGET_APPLE
        // Apple ARM64 platforms have FEAT_LSE2, making ldp single-copy atomic for the pair.
        cbz     x13, 0f
#endif
        br      x13
0:
    .endm

//
// Macro that generates a stub consuming a cache with the given number of entries.
//
    .macro DEFINE_INTERFACE_DISPATCH_STUB entries

    NESTED_ENTRY "RhpInterfaceDispatch\entries", _TEXT, NoHandler

        // x11 holds the indirection cell address. Load the cache pointer.
        ldr     x9, [x11, #OFFSETOF__InterfaceDispatchCell__m_pCache]

        // Load the MethodTable from the object instance in x0.
#ifdef TARGET_APPLE
// Apple's linker has issues which break unwind info if
// an ALTERNATE_ENTRY is present in the middle of a function see https://github.com/dotnet/runtime/pull/114982#discussion_r2083272768
.cfi_endproc
#endif
        ALTERNATE_ENTRY RhpInterfaceDispatchAVLocation\entries
#ifdef TARGET_APPLE
.cfi_startproc
#endif
        ldr     x10, [x0]

    .global CurrentEntry
    .set CurrentEntry, 0
    .set BaseAdj, 0

    .rept \entries
        // ldp's signed offset must be in [-512,504] for 64-bit register pairs.
        // When the offset would exceed that, re-base x9 once so subsequent entries stay in range.
    .if (OFFSETOF__InterfaceDispatchCache__m_rgEntries + (CurrentEntry * 16) - BaseAdj) > 504
        add     x9, x9, #(OFFSETOF__InterfaceDispatchCache__m_rgEntries + (CurrentEntry * 16) - BaseAdj)
        .set BaseAdj, (OFFSETOF__InterfaceDispatchCache__m_rgEntries + (CurrentEntry * 16))
    .endif
        CHECK_CACHE_ENTRY CurrentEntry, BaseAdj
        .set CurrentEntry, CurrentEntry + 1
    .endr

        // x11 still contains the indirection cell address.
        b C_FUNC(RhpInterfaceDispatchSlow)

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
        ldr     xzr, [x0]

        // Just tail call to the cache miss helper.
        b C_FUNC(RhpInterfaceDispatchSlow)
    LEAF_END RhpInitialInterfaceDispatch, _TEXT

#endif // FEATURE_CACHED_INTERFACE_DISPATCH
