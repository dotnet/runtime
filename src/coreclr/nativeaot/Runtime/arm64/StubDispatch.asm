;; Licensed to the .NET Foundation under one or more agreements.
;; The .NET Foundation licenses this file to you under the MIT license.

#include "AsmMacros.h"

    TEXTAREA

#ifdef FEATURE_CACHED_INTERFACE_DISPATCH

    EXTERN RhpCidResolve
    EXTERN RhpUniversalTransition_DebugStepTailCall

    ;; Macro that generates code to check a single cache entry.
    MACRO
        CHECK_CACHE_ENTRY $entry
        ;; Check a single entry in the cache.
        ;;  x9   : Cache data structure. Also used for target address jump.
        ;;  x10  : Instance MethodTable*
        ;;  x11  : Indirection cell address, preserved
        ;;  x12  : Trashed
        ldr     x12, [x9, #(OFFSETOF__InterfaceDispatchCache__m_rgEntries + ($entry * 16))]
        cmp     x10, x12
        bne     %ft0
        ldr     x9, [x9, #(OFFSETOF__InterfaceDispatchCache__m_rgEntries + ($entry * 16) + 8)]
        br      x9
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

        ;; Load the MethodTable from the object instance in x0.
        ALTERNATE_ENTRY RhpInterfaceDispatchAVLocation$entries
        ldr     x10, [x0]

    GBLA CurrentEntry
CurrentEntry SETA 0

    WHILE CurrentEntry < $entries
        CHECK_CACHE_ENTRY CurrentEntry
CurrentEntry SETA CurrentEntry + 1
    WEND

        ;; x11 still contains the indirection cell address.
        b RhpInterfaceDispatchSlow

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

;;
;; Stub dispatch routine for dispatch to a vtable slot
;;
    LEAF_ENTRY RhpVTableOffsetDispatch
        ;; x11 contains the interface dispatch cell address.
        ;; load x12 to point to the vtable offset (which is stored in the m_pCache field).
        ldr     x12, [x11, #OFFSETOF__InterfaceDispatchCell__m_pCache]

        ;; Load the MethodTable from the object instance in x0, and add it to the vtable offset
        ;; to get the address in the vtable of what we want to dereference
        ldr     x13, [x0]
        add     x12, x12, x13

        ;; Load the target address of the vtable into x12
        ldr     x12, [x12]

        br      x12
    LEAF_END RhpVTableOffsetDispatch

;;
;; Cache miss case, call the runtime to resolve the target and update the cache.
;; Use universal transition helper to allow an exception to flow out of resolution.
;;
    LEAF_ENTRY RhpInterfaceDispatchSlow
        ;; x11 contains the interface dispatch cell address.
        ;; Calling convention of the universal thunk is:
        ;;  xip0: target address for the thunk to call
        ;;  xip1: parameter of the thunk's target
        ldr     xip0, =RhpCidResolve
        mov     xip1, x11
        b       RhpUniversalTransition_DebugStepTailCall
    LEAF_END RhpInterfaceDispatchSlow

#endif // FEATURE_CACHED_INTERFACE_DISPATCH

    END
