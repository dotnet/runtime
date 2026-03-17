;; Licensed to the .NET Foundation under one or more agreements.
;; The .NET Foundation licenses this file to you under the MIT license.

#include "AsmMacros.h"

    TEXTAREA

    EXTERN RhpCidResolve
    EXTERN RhpUniversalTransitionTailCall
    EXTERN RhpUniversalTransitionGuardedTailCall

    EXTERN __guard_dispatch_icall_fptr

;; Macro that generates an interface dispatch stub.
;; DispatchName: the name of the dispatch entry point
;; Guarded: if non-empty, validate indirect call targets using Control Flow Guard
    MACRO
        INTERFACE_DISPATCH $DispatchName, $Guarded

    LEAF_ENTRY $DispatchName, _TEXT

        ;; Load the MethodTable from the object instance in x0.
        ;; Trigger an AV if we're dispatching on a null this.
        ;; The exception handling infrastructure is aware of the fact that this is the first
        ;; instruction of the dispatch stub and uses it to translate an AV here
        ;; to a NullReferenceException at the callsite.
        ldr     x12, [x0]

        ;; x11 currently contains the indirection cell address.
        ;; Load-acquire ensures that if we observe the cached MethodTable,
        ;; the subsequent load of Code will see the value written before it.
        ldar    x13, [x11]              ;; load-acquire cached MethodTable
        cmp     x13, x12                ;; is this the monomorphic MethodTable?
        bne     %ft0

        ldr     x9, [x11, #8]           ;; load the cached monomorphic resolved code address
    IF "$Guarded" != ""
        adrp    x16, __guard_dispatch_icall_fptr
        ldr     x16, [x16, __guard_dispatch_icall_fptr]
        br      x16
    ELSE
        br      x9
    ENDIF

0
        ldr     xip0, =RhpCidResolve
        mov     xip1, x11
    IF "$Guarded" != ""
        b       RhpUniversalTransitionGuardedTailCall
    ELSE
        b       RhpUniversalTransitionTailCall
    ENDIF

    LEAF_END $DispatchName

    MEND

    INTERFACE_DISPATCH RhpInterfaceDispatch
    INTERFACE_DISPATCH RhpInterfaceDispatchGuarded, Guarded

    END
