;; Licensed to the .NET Foundation under one or more agreements.
;; The .NET Foundation licenses this file to you under the MIT license.

#include "AsmMacros.h"

    TEXTAREA

#ifdef FEATURE_CACHED_INTERFACE_DISPATCH

    EXTERN RhpCidResolve
    EXTERN RhpUniversalTransition_DebugStepTailCall

;;
;; Stub dispatch routine for dispatch to a vtable slot
;;
    LEAF_ENTRY RhpVTableOffsetDispatch
        ;; x11 contains the interface dispatch cell address.
        ;; load x12 to point to the vtable offset (which is stored in the m_pCache field).
        ldr     x12, [x11, #OFFSETOF__InterfaceDispatchCell__m_pCache]

        ;; Load the MethodTable from the object instance in x0, and add it to the vtable offset
        ;; to get the address in the vtable of what we want to dereference
    ALTERNATE_ENTRY RhpVTableOffsetDispatchAVLocation
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
