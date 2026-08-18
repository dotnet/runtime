;; Licensed to the .NET Foundation under one or more agreements.
;; The .NET Foundation licenses this file to you under the MIT license.

#include "AsmMacros_Shared.h"

    TEXTAREA

#ifdef FEATURE_CACHED_INTERFACE_DISPATCH

    EXTERN RhpInterfaceDispatchSlow

    ;; Macro that generates code to check a single cache entry.
    MACRO
        CHECK_CACHE_ENTRY $entry, $adj
        ;; Check a single entry in the cache.
        ;;  x9   : Cache data structure (possibly adjusted for far entries)
        ;;  x10  : Instance MethodTable*
        ;;  x11  : Indirection cell address, preserved
        ;;  x12, x13  : Trashed
        ;;  $adj : Base adjustment already applied to x9
        ;;
        ;; Use ldp to load both m_pInstanceType and m_pTargetCode in a single instruction.
        ;; On ARM64 two separate ldr instructions can be reordered across a control dependency,
        ;; which means a concurrent atomic cache entry update (via stlxp) could be observed as a
        ;; torn read (new type, old target). ldp is single-copy atomic for the pair on FEAT_LSE2
        ;; hardware (ARMv8.4+). The cbz guard ensures correctness on pre-LSE2 hardware too:
        ;; a torn read can only produce a zero target (entries go from 0,0 to type,target),
        ;; so we treat it as a cache miss.
        ldp     x12, x13, [x9, #(OFFSETOF__InterfaceDispatchCache__m_rgEntries + ($entry * 16) - $adj)]
        cmp     x10, x12
        bne     %ft0
        cbz     x13, %ft0
        br      x13
0
    MEND


;;
;; Macro that generates a stub consuming a cache with the given number of entries.
;;
    MACRO
        DEFINE_INTERFACE_DISPATCH_STUB $entries

    NESTED_ENTRY RhpInterfaceDispatch$entries

        ;; x11 holds the indirection cell address. Load the cache pointer.
        ldr     x9, [x11, #OFFSETOF__InterfaceDispatchCell__m_pCache]

        ;; Validate x9 is a cache pointer matching the expected cache size.
        ;; This compensates for a race where the stub was updated to expect a larger cache
        ;; but we loaded a stale (smaller or non-cache) m_pCache value.
        ;; On detection, re-dispatch through the indirection cell to retry with the current
        ;; stub and cache pair.
        tst     x9, #IDC_CACHE_POINTER_MASK
        bne     CidRaceRetry_$entries
        ldr     w12, [x9, #OFFSETOF__InterfaceDispatchCache__m_cEntries]
        cmp     w12, #$entries
        bne     CidRaceRetry_$entries

        ;; Load the MethodTable from the object instance in x0.
        ALTERNATE_ENTRY RhpInterfaceDispatchAVLocation$entries
        ldr     x10, [x0]

    GBLA CurrentEntry
    GBLA BaseAdj
CurrentEntry SETA 0
BaseAdj SETA 0

    WHILE CurrentEntry < $entries
        ;; ldp's signed offset must be in [-512,504] for 64-bit register pairs.
        ;; When the offset would exceed that, re-base x9 once so subsequent entries stay in range.
        IF (OFFSETOF__InterfaceDispatchCache__m_rgEntries + (CurrentEntry * 16) - BaseAdj) > 504
        add     x9, x9, #(OFFSETOF__InterfaceDispatchCache__m_rgEntries + (CurrentEntry * 16) - BaseAdj)
BaseAdj SETA (OFFSETOF__InterfaceDispatchCache__m_rgEntries + (CurrentEntry * 16))
        ENDIF
        CHECK_CACHE_ENTRY CurrentEntry, BaseAdj
CurrentEntry SETA CurrentEntry + 1
    WEND

        ;; x11 still contains the indirection cell address.
        b RhpInterfaceDispatchSlow

CidRaceRetry_$entries
        ;; Race detected: re-dispatch through the indirection cell
        ldr     x9, [x11]
        br      x9

    NESTED_END RhpInterfaceDispatch$entries

    MEND

;;
;; Define all the stub routines we currently need.
;;
;; If you change or add any new dispatch stubs, exception handling might need to be aware because it refers to the
;; *AVLocation symbols defined by the dispatch stubs to be able to unwind and blame user code if a NullRef happens
;; during the interface dispatch.
;;
    DEFINE_INTERFACE_DISPATCH_STUB 1
    DEFINE_INTERFACE_DISPATCH_STUB 2
    DEFINE_INTERFACE_DISPATCH_STUB 4
    DEFINE_INTERFACE_DISPATCH_STUB 8
    DEFINE_INTERFACE_DISPATCH_STUB 16
    DEFINE_INTERFACE_DISPATCH_STUB 32
    DEFINE_INTERFACE_DISPATCH_STUB 64


;;
;; Initial dispatch on an interface when we don't have a cache yet.
;;
    LEAF_ENTRY RhpInitialInterfaceDispatch
    ALTERNATE_ENTRY RhpInitialDynamicInterfaceDispatch
        ;; Trigger an AV if we're dispatching on a null this.
        ;; The exception handling infrastructure is aware of the fact that this is the first
        ;; instruction of RhpInitialInterfaceDispatch and uses it to translate an AV here
        ;; to a NullReferenceException at the callsite.
        ldr     xzr, [x0]

        ;; Just tail call to the cache miss helper.
        b RhpInterfaceDispatchSlow
    LEAF_END RhpInitialInterfaceDispatch

#endif // FEATURE_CACHED_INTERFACE_DISPATCH

    END
