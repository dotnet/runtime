;; Licensed to the .NET Foundation under one or more agreements.
;; The .NET Foundation licenses this file to you under the MIT license.

include AsmMacros.inc


EXTERN RhpCidResolve : PROC
EXTERN RhpUniversalTransitionTailCall : PROC
EXTERN RhpUniversalTransitionGuardedTailCall : PROC

EXTERN g_pDispatchCache : QWORD

;; Macro that generates an interface dispatch stub.
;; DispatchName: the name of the dispatch entry point
;; TransitionTarget: the UniversalTransition variant to jump to
INTERFACE_DISPATCH macro DispatchName, TransitionTarget

LEAF_ENTRY DispatchName, _TEXT

        ;; Load the MethodTable from the object instance in rcx.
        ;; Trigger an AV if we're dispatching on a null this.
        ;; The exception handling infrastructure is aware of the fact that this is the first
        ;; instruction of the dispatch stub and uses it to translate an AV here
        ;; to a NullReferenceException at the callsite.
        mov     r10, [rcx]

        ;; r11 contains indirection cell address
        lea     r10, RhpCidResolve
        jmp     TransitionTarget

LEAF_END DispatchName, _TEXT

        endm

        INTERFACE_DISPATCH RhpInterfaceDispatch, RhpUniversalTransitionTailCall
        INTERFACE_DISPATCH RhpInterfaceDispatchGuarded, RhpUniversalTransitionGuardedTailCall

end
