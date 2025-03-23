// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.intel_syntax noprefix
#include "AsmMacros_Shared.h"

#ifdef FEATURE_CACHED_INTERFACE_DISPATCH

// trick to avoid PLT relocation at runtime which corrupts registers
#define REL_C_FUNC(name) C_FUNC(name)@gotpcrel


// Macro that generates a stub consuming a cache with the given number of entries.
.macro DEFINE_INTERFACE_DISPATCH_STUB entries

LEAF_ENTRY RhpInterfaceDispatch\entries, _TEXT

        // r11 currently contains the indirection cell address.
        // load r10 to point to the cache block.
        mov     r10, [r11 + OFFSETOF__InterfaceDispatchCell__m_pCache]

        // Load the MethodTable from the object instance in rdi.
        ALTERNATE_ENTRY RhpInterfaceDispatchAVLocation\entries
        mov     rax, [rdi]

        CurrentOffset = OFFSETOF__InterfaceDispatchCache__m_rgEntries

        // For each entry in the cache, see if its MethodTable type matches the MethodTable in rax.
        // If so, call the second cache entry.  If not, skip the InterfaceDispatchCacheEntry.
        .rept \entries
            cmp     rax, [r10 + CurrentOffset]
            jne     0f
            jmp     [r10 + CurrentOffset + 8]
        0:
            CurrentOffset = CurrentOffset + 16
        .endr

        // r11 still contains the indirection cell address.

        jmp     C_FUNC(RhpInterfaceDispatchSlow)
LEAF_END RhpInterfaceDispatch\entries, _TEXT

.endm // DEFINE_INTERFACE_DISPATCH_STUB



// Define all the stub routines we currently need.
//
// The mrt100dbi requires these be exported to identify mrt100 code that dispatches back into managed.
// If you change or add any new dispatch stubs, please also change slr.def and dbi\process.cpp CordbProcess::GetExportStepInfo
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

// Initial dispatch on an interface when we don't have a cache yet.
LEAF_ENTRY RhpInitialInterfaceDispatch, _TEXT
ALTERNATE_ENTRY RhpInitialDynamicInterfaceDispatch
        // Trigger an AV if we're dispatching on a null this.
        // The exception handling infrastructure is aware of the fact that this is the first
        // instruction of RhpInitialInterfaceDispatch and uses it to translate an AV here
        // to a NullReferenceException at the callsite.
        cmp     byte ptr [rdi], 0

        // Just tail call to the cache miss helper.
        jmp     C_FUNC(RhpInterfaceDispatchSlow)

LEAF_END RhpInitialInterfaceDispatch, _TEXT


#endif // FEATURE_CACHED_INTERFACE_DISPATCH